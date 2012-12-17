using System;
using System.Security.Cryptography;
using Rhino.Licensing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using System.Threading;
using System.Diagnostics;
using System.Management;
using System.Text;//Requires the Rhino.Licensing.dll and the log4net.dll

namespace SharedClasses
{
	public static class LicensingInterop_Client
	{
		public const int cApplicationExitCodeIfLicenseFailedValidation = 77;
		public const int cApplicationExitCodeIfOnlineLicenseConfirmationFailed = 88;

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

		private static string GetApplicationName()
		{
			string applicationName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
			if (applicationName.EndsWith(".vshost", StringComparison.InvariantCultureIgnoreCase))
				applicationName = applicationName.Substring(0, applicationName.Length - ".vshost".Length);
			return applicationName;
		}

		private static string GetLicenseFilePath(string applicationName)
		{
			return SettingsInterop.GetFullFilePathInLocalAppdata("license.lic", "Licenses", applicationName);
		}


		public static string GetThisPcMachineSignature()
		{
			return MachineFingerPrint.GetFingerPrint();
			//return Environment.MachineName + "/" + Environment.UserName + "/" + SettingsInterop.GetComputerGuidAsString();
		}

		public static bool Client_ValidateLicense(out Dictionary<string, string> userPrivilages, Action<string> onError)//, string publicKeyXml, string licenseFilepath)
		{
			string applicationName = GetApplicationName();

			int todoItem;
			//TODO: Ensure that the expirationDate of a license also fails to validate if it is a Trial license and the trial period expired
			//I would assume that this is already the case

			if (onError == null) onError = delegate { };

			try
			{
				//Public key and License should already be on computer if application was 'registered'
				//
				string licenseFilepath = GetLicenseFilePath(applicationName);

				string publicKeyPath = Path.Combine(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]), LicensingInterop_Shared.cLicensePublicKeyFilename);//SettingsInterop.GetFullFilePathInLocalAppdata(LicensingInterop_Shared.cLicensePublicKeyFilename, "Licenses", applicationName);
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

				string publicKey = File.ReadAllText(publicKeyPath).Trim();
				if (!File.Exists(licenseFilepath))
				{
					string outLicenseXmlText;
					if (!RegistrationWindow.RegisterApplication(applicationName, publicKey, out outLicenseXmlText))
					{
						onError("Application did not register successfully.");
						userPrivilages = null;
						return false;
					}
					else
						File.WriteAllText(licenseFilepath, outLicenseXmlText);
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
			string appname = GetApplicationName();
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

					string applicationName = GetApplicationName();

					//At this point we know the license is valid but now also (on separate thread) confirm it exists on the server
					string result = PhpInterop.PostPHP(null,
						string.Format("http://fjh.dyndns.org/licensing/{0}/{1}/{2}/{3}/{4}/{5}/{6}",
							"confirmlicensewasissued",
							EncodeAndDecodeInterop.EncodeStringHex(EncryptionInterop.SimpleTripleDesEncrypt(licValidator.Name, LicensingInterop_Shared.cEncryptionKey_OnlineServerPhp), onError),
							EncodeAndDecodeInterop.EncodeStringHex(EncryptionInterop.SimpleTripleDesEncrypt(applicationName, LicensingInterop_Shared.cEncryptionKey_OnlineServerPhp), onError),
							EncodeAndDecodeInterop.EncodeStringHex(EncryptionInterop.SimpleTripleDesEncrypt(licValidator.LicenseAttributes[LicensingInterop_Shared.cMachineSignatureKeyName], LicensingInterop_Shared.cEncryptionKey_OnlineServerPhp), onError),
							EncodeAndDecodeInterop.EncodeStringHex(EncryptionInterop.SimpleTripleDesEncrypt(licValidator.ExpirationDate.ToString(LicensingInterop_Shared.cExpirationDateFormat), LicensingInterop_Shared.cEncryptionKey_OnlineServerPhp), onError),
							EncodeAndDecodeInterop.EncodeStringHex(EncryptionInterop.SimpleTripleDesEncrypt(licValidator.UserId.ToString(), LicensingInterop_Shared.cEncryptionKey_OnlineServerPhp), onError),
							EncodeAndDecodeInterop.EncodeStringHex(EncryptionInterop.SimpleTripleDesEncrypt(licValidator.LicenseType.ToString(), LicensingInterop_Shared.cEncryptionKey_OnlineServerPhp), onError)),
						null);

					result = result ?? "";
					using (RegistryKey appLicensesRegKey = Registry.CurrentUser.OpenOrCreateWriteableSubkey(@"SOFTWARE\FJH\Licensing\" + licValidator.UserId.ToString()))
					{
						const string onlineQueryFailedRegValueName = "onlinecheckfailed";

						if (string.IsNullOrWhiteSpace(result))
						{
							int onlineFailedCount = 1;

							int todoItem;
							//TODO: Maybe add encryption to this OnlineCheckFailed registry value
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
								ShowServerConfirmLicenseError("Could not confirm that license exists on server, application will now exit.");

								int todoExitCodeNotReached;
								//The application exist before it reaches this ExitCode
								Environment.Exit(cApplicationExitCodeIfOnlineLicenseConfirmationFailed);
							}
							else if (onlineFailedCount >= 15)//Start annoying user after 15 times
							{
								//Will not break the application if no valid internet access, but just annoy the user

								//TODO: Maybe suggest to check whether the domain (where the license server sits on) is accessible from this machine (for instance domain fjh.dyndns.org)
								ShowServerConfirmLicenseError("Could not confirm that license existance on server, please ensure internet connectivity to stop showing this message.");
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
									Environment.Exit(cApplicationExitCodeIfOnlineLicenseConfirmationFailed);
								}
								else
								{
									ShowServerConfirmLicenseError("Unknown error confirming license existance on server (application will now exit): " + result);
									Environment.Exit(cApplicationExitCodeIfOnlineLicenseConfirmationFailed);
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