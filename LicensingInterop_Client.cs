using System;
using System.Security.Cryptography;
using Rhino.Licensing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using System.Threading;
using System.Diagnostics;
using System.Management;//Requires System.Management.dll
using System.Text;//Requires the Rhino.Licensing.dll and the log4net.dll

namespace SharedClasses
{
	public static class LicensingInterop_Client
	{
		public const int cApplicationExitCodeIfLicenseFailedValidation = 77;
		public const int cApplicationExitCodeIfOnlineLicenseConfirmationFailed = 88;
		public const int cApplicationExitCodeIfCachedFingerprintChanged = 99;

		private static string _DuplicateInsertSpacesBeforeCamelCase(this string str)//Also in StringExtensions
		{
			if (str == null) return str;
			for (int i = str.Length - 1; i >= 1; i--)
			{
				if (str[i].ToString().ToUpper() == str[i].ToString())
					str = str.Insert(i, " ");
			}
			return str;
		}
		private static string _DuplicateGetApplicationExePathFromApplicationName(string applicationName)//Also in PublishInterop
		{
			return Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
				applicationName._DuplicateInsertSpacesBeforeCamelCase(),
				applicationName + ".exe");
		}

		private static string GetLicenseFilePath(string applicationName)
		{
			return SettingsInterop.GetFullFilePathInLocalAppdata("license.lic", "Licenses", applicationName);
		}

		private static string GetPublicKeyFilePath(string applicationName)
		{
			return SettingsInterop.GetFullFilePathInLocalAppdata("public.key", "Licenses", applicationName);
		}

		private static string GetCachedMachineSignatureFilePath()
		{
			return SettingsInterop.GetFullFilePathInLocalAppdata("machinesignature", "Licenses");
		}
		public static string GetThisPcMachineSignature()
		{
			//We cache the signature because it can delay the app startup time as it
			//takes a short while to obtain the machine fingerprint

			string tmpcachedSignaturePath = GetCachedMachineSignatureFilePath();
			FileAttributes fileAttributes = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReadOnly;
			if (File.Exists(tmpcachedSignaturePath))
			{
				//On seperate thread ensure the cached signature was not tampered with
				ThreadingInterop.DoAction(delegate
				{
					string fingerPrint = MachineFingerPrint.GetFingerPrint();
					if (!fingerPrint.Equals(File.ReadAllText(tmpcachedSignaturePath)))
					{
						File.SetAttributes(tmpcachedSignaturePath, FileAttributes.Normal);
						File.WriteAllText(tmpcachedSignaturePath, fingerPrint);
						File.SetAttributes(tmpcachedSignaturePath, fileAttributes);
						string appname = OwnAppsShared.GetApplicationName();
						string tempfile = Path.Combine(Path.GetTempPath(), appname + " fingerprint error.txt");
						File.WriteAllText(tempfile,
							"Fingerprint error for '" + appname + "'"
							+ Environment.NewLine
							+ "The cached machine signature is different to the actual signature, file regenerated with actual signature."
							+ Environment.NewLine + Environment.NewLine
							+ "Application has exited.");
						Process.Start(tempfile);
						ResourceUsageTracker.FlushAllCurrentLogLines();
						OwnAppsShared.ExitAppWithExitCode(cApplicationExitCodeIfCachedFingerprintChanged);
					}
				},
				false);
				return File.ReadAllText(tmpcachedSignaturePath);
			}
			else
			{
				string fingerPrint = MachineFingerPrint.GetFingerPrint();
				File.WriteAllText(tmpcachedSignaturePath, fingerPrint);
				File.SetAttributes(tmpcachedSignaturePath, fileAttributes);
				return fingerPrint;
			}
			//return Environment.MachineName + "/" + Environment.UserName + "/" + SettingsInterop.GetComputerGuidAsString();
		}

		public static bool GetLicenseAndPublicKeyFromLicenseServer(string uniqueSignature, out string licenseKeyOrError, out string publicKey)
		{
			string result = PhpInterop.PostPHP(null,
				"http://fjh.dyndns.org/licensing/getlicense",//"http://firepuma.com/licensing/getlicense"
				"uniquesignature=" + LicensingInterop_Shared.EncryptStringForPhpServer(uniqueSignature, err => UserMessages.ShowErrorMessage(err)));

			if (string.IsNullOrWhiteSpace(result))
			{
				licenseKeyOrError = "Could not get license from server, response from server was empty";
				publicKey = null;
				return false;
			}
			else if (result.IndexOf(LicensingInterop_Shared.cErrorMessagePrefix) != -1
				|| result.IndexOf(LicensingInterop_Shared.cErrorMessagePostfix) != -1)
			{
				licenseKeyOrError = result
					.Replace(LicensingInterop_Shared.cErrorMessagePrefix, "")
					.Replace(LicensingInterop_Shared.cErrorMessagePostfix, "");
				publicKey = null;
				return false;
			}
			else if (result.IndexOf("<license", StringComparison.InvariantCultureIgnoreCase) == -1)//Invalid error
			{
				licenseKeyOrError = "Could not get license from server, unrecognized error received: " + result;
				publicKey = null;
				return false;
			}
			else//success
			{
				string[] splitted = result.Split(new string[] { LicensingInterop_Shared.cSplitBetweenLicenseAndPublicKey }, StringSplitOptions.RemoveEmptyEntries);
				if (splitted.Length != 2)
				{
					licenseKeyOrError = "Could not obtain license from server, unable to parse result from server";
					publicKey = null;
					return false;
				}
				licenseKeyOrError = splitted[0];
				publicKey = splitted[1];
				return true;
			}
		}

		//public const string cRegisterAppArgumentName = "registerapp";

		private static bool registrationSucceeded = false;
		public static bool Client_ValidateLicense(out Dictionary<string, string> userPrivilages, Action<string> onError, string[] customCommandlineArgs = null)//, string publicKeyXml, string licenseFilepath)
		{
			string applicationName = OwnAppsShared.GetApplicationName();

			string[] args = customCommandlineArgs ?? Environment.GetCommandLineArgs();

			bool showMessageIfAlreadyRegistered = false;
			string predefinedEmail = null;
			string predefinedOrdercode = null;
			if (LicensingInterop_Shared.WasUrlProtocolUsedToSpecifyRegistrationCredentials(args, out predefinedEmail, out predefinedOrdercode))
				showMessageIfAlreadyRegistered = true;

			//Ensure that the expirationDate of a license also fails to validate if it is a Trial license and the trial period expired
			//I would assume that this is already the case

			if (onError == null) onError = delegate { };

			try
			{
				//Public key and License should already be on computer if application was 'registered'
				//
				string licenseFilepath = GetLicenseFilePath(applicationName);

				/*string publicKeyPath = Path.Combine(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]), LicensingInterop_Shared.cLicensePublicKeyFilename);//SettingsInterop.GetFullFilePathInLocalAppdata(LicensingInterop_Shared.cLicensePublicKeyFilename, "Licenses", applicationName);
				if (!File.Exists(publicKeyPath)
					&& !publicKeyPath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), StringComparison.InvariantCultureIgnoreCase)
					&& !publicKeyPath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), StringComparison.InvariantCultureIgnoreCase))
				{
					//We are not running from the default Program Files (or x86) directory, possibly running from VSProjects directory
					string possibleInstalledPublicKeyPath =
						Path.Combine(Path.GetDirectoryName(_DuplicateGetApplicationExePathFromApplicationName(applicationName)), LicensingInterop_Shared.cLicensePublicKeyFilename);
					if (File.Exists(possibleInstalledPublicKeyPath))
					{
						//The publicKey file does not exist in same dir as EXE,
						//but because we assume we might be running from the source code (VSProects)
						//we checked if the public key is found in the installed directory
						//and if we got here, it did exist so now we will use it
						publicKeyPath = possibleInstalledPublicKeyPath;
					}
				}

				if (!File.Exists(publicKeyPath))//Public key supposed to be installed with application
				{
					onError("Unable to find Public key, which is supposed to be installed with application '"
						+ applicationName + "'."
						+ Environment.NewLine + Environment.NewLine
						+ "File missing: " + publicKeyPath
						+ Environment.NewLine + Environment.NewLine
						+ "Please contact the developer, just click OK and complete the email addresses to the developer."
						+ Environment.NewLine + Environment.NewLine
						+ "The application will now exit.");
					DeveloperCommunication.RunMailto();
					userPrivilages = null;
					return false;
				}

				string publicKey = File.ReadAllText(publicKeyPath).Trim();*/

				string publicKeyPath = GetPublicKeyFilePath(applicationName);//Path.Combine(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]), LicensingInterop_Shared.cLicensePublicKeyFilename);
				string publicKey;//Not in file anymore but now
				if (!File.Exists(licenseFilepath))
				{
					registrationSucceeded = false;
					publicKey = null;//We have to set it otherwise code does not compile
					//We need to run this on a STA thread, otherwise our MainWindow cannot
					//open after successfully registered (we still wait for the thread to finish)
					Thread registerApplicationThread = new Thread(new ParameterizedThreadStart(delegate
					{
						string outLicenseXmlText;
						string outPublicKey;
						if (RegistrationWindow.RegisterApplication(applicationName, out outLicenseXmlText, out outPublicKey, predefinedEmail, predefinedOrdercode))
						{
							registrationSucceeded = true;
							File.WriteAllText(licenseFilepath, outLicenseXmlText);
							File.WriteAllText(publicKeyPath, outPublicKey);
						}
						publicKey = outPublicKey;
					}));
					registerApplicationThread.SetApartmentState(ApartmentState.STA);
					registerApplicationThread.Start();
					while (registerApplicationThread.IsAlive) { }

					if (!registrationSucceeded)
					{
						onError("Application did not register successfully.");
						userPrivilages = null;
						return false;
					}
				}
				else
				{
					if (showMessageIfAlreadyRegistered)
						UserMessages.ShowInfoMessage("Application is already registered, will now just ensure everything is still in tact.");

					if (File.Exists(publicKeyPath) && new FileInfo(publicKeyPath).Length == 0)
						File.Delete(publicKeyPath);//If it so happens that the public key is an empty file

					//License file exists, check if public key file exists
					if (File.Exists(publicKeyPath))
						publicKey = File.ReadAllText(publicKeyPath);
					else
					{
						string userOrderCodeReversed = InputBoxWPF.Prompt("Public key is missing on local machine, enter the OrderCode here (check email inbox) to retrieve the public key from the server again.", "Public key missing");
						if (string.IsNullOrWhiteSpace(userOrderCodeReversed))
							throw new Exception("Unable to validate license, public key is missing.");
						else
						{
							string tmppubkeyOrError, tmpPrivKey;
							if (LicensingInterop_Shared.GetPublicAndPrivateKey(userOrderCodeReversed, LicensingInterop_Client.GetThisPcMachineSignature(), out tmppubkeyOrError, out tmpPrivKey))
							{
								File.WriteAllText(publicKeyPath, tmppubkeyOrError);
								publicKey = tmppubkeyOrError;
							}
							else
								throw new Exception("Unable to retrieve public key from server (cannot validate license): " + tmppubkeyOrError);
						}
					}
				}

				var licenseValidator = new LicenseValidator(publicKey, licenseFilepath);
				licenseValidator.AssertValidLicense();//Validates the license exists and make sure user did not tamper with license file

				userPrivilages = new Dictionary<string, string>(licenseValidator.LicenseAttributes);
				if (!userPrivilages.ContainsKey(LicensingInterop_Shared.cMachineSignatureKeyName))
				{
					onError(
						"Cannot find machinename inside the license"
						+ "Please contact the developer, just click OK and complete the email addresses to the developer."
						+ Environment.NewLine + Environment.NewLine
						+ "The application will now exit.");
					DeveloperCommunication.RunMailto();
					userPrivilages = null;
					return false;
				}

				if (!userPrivilages[LicensingInterop_Shared.cMachineSignatureKeyName].Equals(GetThisPcMachineSignature(), StringComparison.InvariantCultureIgnoreCase))
				{//Machine names not the same
					onError(
						"Invalid license, the current hardware does not match the hardware signature for the issued license."
						+ "Please contact the developer, just click OK and complete the email addresses to the developer."
						+ Environment.NewLine + Environment.NewLine
						+ "The application will now exit.");
					DeveloperCommunication.RunMailto();
					userPrivilages = null;
					return false;
				}
				userPrivilages.Remove(LicensingInterop_Shared.cMachineSignatureKeyName);

				//Query the server to see if the license was actually issued by our server
				ValidateLicenseExistsOnServer_OnSeparateThread(licenseValidator, onError);
				return true;
			}
			catch (LicenseNotFoundException licExc)
			{
				//No license found for this publicKey and licenseXml
				onError("License not valid: " + licExc.Message);
				userPrivilages = null;
				return false;
			}
			catch (Exception exc)
			{
				onError("Error validating license: " + exc.Message);
				userPrivilages = null;
				return false;
			}
		}

		private static void ShowServerConfirmLicenseError(string err)
		{
			string appname = OwnAppsShared.GetApplicationName();
			string tempfile = Path.Combine(Path.GetTempPath(), appname + " license error.txt");
			File.WriteAllText(tempfile,
				"License validation error for '" + appname + "'"
				+ Environment.NewLine
				+ err);
			Process.Start(tempfile);
		}

		private static Action<string> validateOnServerActionOnError = null;
		private static void ValidateLicenseExistsOnServer_OnSeparateThread(LicenseValidator LicenseValidator, Action<string> actionOnError)
		{
			if (actionOnError == null) actionOnError = delegate { };
			validateOnServerActionOnError = actionOnError;

			ThreadingInterop.PerformOneArgFunctionSeperateThread<LicenseValidator>(
				(licValidator) =>
				{
					Action<string> onError = validateOnServerActionOnError;

					string applicationName = OwnAppsShared.GetApplicationName()._DuplicateInsertSpacesBeforeCamelCase();
					string ownerEmail = licValidator.LicenseAttributes[LicensingInterop_Shared.cOwnerEmailKeyName];
					string ordernumberReversed = licValidator.LicenseAttributes[LicensingInterop_Shared.cOrderCodeKeyName];
					string machineSignature = licValidator.LicenseAttributes[LicensingInterop_Shared.cMachineSignatureKeyName];

					//At this point we know the license is valid but now also (on separate thread) confirm it exists on the server
					string result = PhpInterop.PostPHP(null,
						string.Format("http://firepuma.com/licensing/{0}/{1}/{2}/{3}/{4}",
							"confirmlicenseisindb",
							LicensingInterop_Shared.EncryptStringForPhpServer(applicationName, onError),
							LicensingInterop_Shared.EncryptStringForPhpServer(ownerEmail, onError),
							LicensingInterop_Shared.EncryptStringForPhpServer(ordernumberReversed, onError),
							LicensingInterop_Shared.EncryptStringForPhpServer(machineSignature, onError)),
						null);

					result = result ?? "";
					using (RegistryKey appLicensesRegKey = Registry.CurrentUser.OpenOrCreateWriteableSubkey(@"SOFTWARE\FJH\Licensing\" + licValidator.UserId.ToString()))
					{
						const string onlineQueryFailedRegValueName = "onlinecheckfailed";

						if (string.IsNullOrWhiteSpace(result))
						{
							int onlineFailedCount = 1;

							//Maybe add encryption to this OnlineCheckFailed registry value
							bool foundOnlineFailedRegVal = appLicensesRegKey.GetValueNames().Count(s => s.Equals(onlineQueryFailedRegValueName)) > 0;
							if (foundOnlineFailedRegVal)//Value was found, increase it
							{
								string onlineFailedCountValueStr = appLicensesRegKey.GetValue(onlineQueryFailedRegValueName).ToString();
								int tmpint;
								if (int.TryParse(onlineFailedCountValueStr, out tmpint))
									onlineFailedCount = tmpint + 1;
							}
							appLicensesRegKey.SetValue(onlineQueryFailedRegValueName, onlineFailedCount.ToString());

							if (onlineFailedCount >= 50)//After 50 times cannot use application anymore
							{
								ShowServerConfirmLicenseError("Could not confirm that license exists on server (empty response from server), application will now exit.");

								//The application exits before it reaches this ExitCode
								ResourceUsageTracker.FlushAllCurrentLogLines();
								OwnAppsShared.ExitAppWithExitCode(cApplicationExitCodeIfOnlineLicenseConfirmationFailed);
							}
							else if (onlineFailedCount >= 15)//Start annoying user after 15 times
							{
								//Will not break the application if no valid internet access, but just annoy the user

								//Maybe suggest to check whether the domain (where the license server sits on) is accessible from this machine (for instance domain fjh.dyndns.org)
								ShowServerConfirmLicenseError("Could not confirm that license existance on server (empty response from server), please ensure internet connectivity to stop showing this message.");
							}
						}
						else
						{
							//We got a response from server
							appLicensesRegKey.SetValue(onlineQueryFailedRegValueName, "0");

							result = result.Trim();
							if (!result.StartsWith(LicensingInterop_Shared.cLicenseExistsOnServerMessage, StringComparison.InvariantCultureIgnoreCase))
							{
								//Use contains, instead of StartsWith, because the string end up being [ERRORSTART]cLicenseNonExistingOnServerMessage[ERROREND]
								if (result.IndexOf(LicensingInterop_Shared.cLicenseNonExistingOnServerMessage, StringComparison.InvariantCultureIgnoreCase) != -1)
								{
									//License not found on server, delete the license file
									File.Delete(GetLicenseFilePath(applicationName));
									ShowServerConfirmLicenseError("License is not a valid issued license, please obtain a valid license. Application will now exit");
									OwnAppsShared.ExitAppWithExitCode(cApplicationExitCodeIfOnlineLicenseConfirmationFailed);
								}
								else
								{
									ShowServerConfirmLicenseError("Unknown error confirming license existance on server (application will now exit): " + result);
									OwnAppsShared.ExitAppWithExitCode(cApplicationExitCodeIfOnlineLicenseConfirmationFailed);
								}
							}
							//else SUCCESS
						}
					}
				},
				LicenseValidator,
				false);
		}

		public static string GetMotherBoardID()
		{
			string serial = "";
			try
			{
				using (ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
				using (ManagementObjectCollection moc = mos.Get())
				{
					foreach (ManagementObject mo in moc)
						serial = mo["SerialNumber"].ToString();
					return serial;
				}
			}
			catch (Exception) { return serial; }
		}
	}
}