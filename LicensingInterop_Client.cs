using System;
using System.Security.Cryptography;
using Rhino.Licensing;
using System.Collections.Generic;
using System.IO;//Requires the Rhino.Licensing.dll and the log4net.dll

namespace SharedClasses
{
	public static class LicensingInterop_Client
	{
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

		public static bool Client_ValidateLicense(string applicationName, out Dictionary<string, string> userPrivilages, Action<string> onError)//, string publicKeyXml, string licenseFilepath)
		{
			int todoItem;
			//TODO: Ensure that the expirationDate of a license also fails to validate if it is a Trial license and the trial period expired
			//I would assume that this is already the case

			if (onError == null) onError = delegate { };

			try
			{
				//Public key and License should already be on computer if application was 'registered'
				//
				string publicKeyPath = Path.Combine(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]), LicensingInterop_Shared.cLicensePublicKeyFilename);//SettingsInterop.GetFullFilePathInLocalAppdata(LicensingInterop_Shared.cLicensePublicKeyFilename, "Licenses", applicationName);
				string licenseFilepath = SettingsInterop.GetFullFilePathInLocalAppdata("license.lic", "Licenses", applicationName);

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

				string result = PhpInterop.PostPHP(null,
					string.Format("http://fjh.dyndns.org/licensing/{0}/{1}/{2}/{3}/{4}/{5}/{6}",
						"confirmlicensewasissued",
						EncodeAndDecodeInterop.EncodeStringHex(EncryptionInterop.SimpleTripleDesEncrypt(licenseValidator.Name, LicensingInterop_Shared.cEncryptionKey), onError),
						EncodeAndDecodeInterop.EncodeStringHex(EncryptionInterop.SimpleTripleDesEncrypt(applicationName, LicensingInterop_Shared.cEncryptionKey), onError),
						EncodeAndDecodeInterop.EncodeStringHex(EncryptionInterop.SimpleTripleDesEncrypt(licenseValidator.LicenseAttributes["machinename"], LicensingInterop_Shared.cEncryptionKey), onError),
						EncodeAndDecodeInterop.EncodeStringHex(EncryptionInterop.SimpleTripleDesEncrypt(licenseValidator.ExpirationDate.ToString(LicensingInterop_Shared.cExpirationDateFormat), LicensingInterop_Shared.cEncryptionKey), onError),
						EncodeAndDecodeInterop.EncodeStringHex(EncryptionInterop.SimpleTripleDesEncrypt(licenseValidator.UserId.ToString(), LicensingInterop_Shared.cEncryptionKey), onError),
						EncodeAndDecodeInterop.EncodeStringHex(EncryptionInterop.SimpleTripleDesEncrypt(licenseValidator.LicenseType.ToString(), LicensingInterop_Shared.cEncryptionKey), onError)),
					null);

				if (string.IsNullOrWhiteSpace(result))
				{
					onError("Could not confirm that license exists on server, the response from the server was empty");
					userPrivilages = null;
					return false;
				}

				result = result.Trim();
				if (!result.StartsWith(LicensingInterop_Shared.cLicenseExistsOnServerMessage, StringComparison.InvariantCultureIgnoreCase))
				{
					if (result.StartsWith(LicensingInterop_Shared.cLicenseNonExistingOnServerMessage, StringComparison.InvariantCultureIgnoreCase))
					{
						onError("License does not exist on server");
						userPrivilages = null;
						return false;
					}
					else
					{
						onError("Unknown error obtaining whether the license was generated by the correct server: " + result);
						userPrivilages = null;
						return false;
					}
				}
				else
				{
					userPrivilages = new Dictionary<string, string>(licenseValidator.LicenseAttributes);
					userPrivilages.Remove("machinename");
					return true;
				}
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
	}
}