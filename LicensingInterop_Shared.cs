using System;
using System.Collections.Generic;

namespace SharedClasses
{
	/// <summary>
	/// Contains the shared code/constants between client (with application to be licensed) and server (which leases and revokes licenses)
	/// </summary>
	public static class LicensingInterop_Shared
	{
		public const string cRegisterApplicationFirstCommandlineArg = "registerapp";

		public const string cErrorMessagePrefix = "[ERRORSTART]";
		public const string cErrorMessagePostfix = "[ERROREND]";
		public const string cSplitBetweenLicenseAndPublicKey = "[PUBLICKEYTOFOLLOW]";

		//private const string cExpirationDateFormat = "s";//KEEP AS IS, also used in LicensingServer code, class IssuedLicense
		public const string cExpirationDateFormat = "yyyy-MM-ddTHH:mm:ss.fffffff";//KEEP AS IS, also used in LicensingServer code, class IssuedLicense

		public const string cEncryptionKey_OnlineServerPhp = "zvut8pqrd2efgys1cijklnox";//LEAVE AS IS, also used online in licensing.php controller
		public const string cEncryptionKey_EmailToDeveloper = "m590adqe4f8op6bcst71r23n";

		public const string cLicensePublicKeyFilename = "public.key";
		public const string cMachineSignatureKeyName = "machinesignature";
		public const string cOrderCodeKeyName = "ordernumber";//Note this stored order number will actually be the reversed order number
		public const string cOwnerEmailKeyName = "owneremail";

		public const string cLicenseExistsOnServerMessage = "LICENSE EXISTS:";
		public const string cLicenseNonExistingOnServerMessage = "Machine is not registered on server.";
		public const string cPublicKeyFoundOnServerStartMessage = "Public key found:";

		public static bool WasUrlProtocolUsedToSpecifyRegistrationCredentials(string[] args, out string emailOrNull, out string orderCodeOrNull)
		{
			if (args.Length < 3
				|| !args[1].Equals(LicensingInterop_Shared.cRegisterApplicationFirstCommandlineArg, StringComparison.InvariantCultureIgnoreCase))
			{
				emailOrNull = orderCodeOrNull = null;
				return false;
			}

			string[] emailPipeOrderCode = args[2].Substring(args[2].IndexOf(":") + 1).Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			if (emailPipeOrderCode.Length != 2)
			{
				emailOrNull = orderCodeOrNull = null;
				return false;
			}
			else
			{
				emailOrNull = emailPipeOrderCode[0];
				orderCodeOrNull = emailPipeOrderCode[1];
				return true;
			}
		}

		public static string EncryptStringForPhpServer(string originalString, Action<string> actionOnError)
		{
			return EncodeAndDecodeInterop.EncodeStringHex(EncryptionInterop.SimpleTripleDesEncrypt(originalString, LicensingInterop_Shared.cEncryptionKey_OnlineServerPhp), actionOnError);
		}

		public static string GetMachineUIDForApplicationAndMachine(string applicationName, string ownerEmail, string orderCodeReversed, string machineSignature, Action<string> onError)
		{
			return EncodeAndDecodeInterop.EncodeStringHex(EncryptionInterop.SimpleTripleDesEncrypt(
				applicationName + "|" + ownerEmail + "|" + orderCodeReversed + "|" + machineSignature,
				LicensingInterop_Shared.cEncryptionKey_EmailToDeveloper),
				onError);
		}

		public static bool DecryptMachineUIDForApplicationAndMachine(string uniqueSignatureEncrypted, out string applicationName, out string ownerEmail, out string orderCodeReversed, out string machineSignature, Action<string> onError)
		{
			try
			{
				string decryptedString = EncryptionInterop.SimpleTripleDesDecrypt(
					EncodeAndDecodeInterop.DecodeStringHex(uniqueSignatureEncrypted), LicensingInterop_Shared.cEncryptionKey_EmailToDeveloper)
					.Trim();
				string[] pipeSplitted = decryptedString.Trim().Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
				if (pipeSplitted.Length != 4)
				{
					onError("Unable to decrypt the unique signature");
					applicationName = null;
					ownerEmail = null;
					orderCodeReversed = null;
					machineSignature = null;
					return false;
				}
				applicationName = pipeSplitted[0].Trim();

				//Maybe we should validate the owner email address?
				ownerEmail = pipeSplitted[1].Trim();
				orderCodeReversed = pipeSplitted[2].Trim();
				machineSignature = pipeSplitted[3].Trim().Trim('\0');
				return true;
			}
			catch (Exception exc)
			{
				onError(exc.Message);
				applicationName = null;
				ownerEmail = null;
				orderCodeReversed = null;
				machineSignature = null;
				return false;
			}
		}

		//true=success, false=does not exist, null=error
		private static bool? ObtainPublicAndPrivateKeyFromOnline(string orderCodeReversed, string machineSignature, out string publicKeyXml_orerror, out string privateKeyXml)
		{
			List<string> caughtErrors = new List<string>();
			string result = PhpInterop.PostPHP(null,
				string.Format("https://firepuma.com/licensing/{0}/{1}/{2}",//"http://firepuma.com/licensing/{0}/{1}/{2}",
					"getpublicandprivatekeyfromdb",
					LicensingInterop_Shared.EncryptStringForPhpServer(orderCodeReversed, err => caughtErrors.Add(err)),
					LicensingInterop_Shared.EncryptStringForPhpServer(machineSignature, err => caughtErrors.Add(err))),
				null);
			if (caughtErrors.Count > 0)
			{
				publicKeyXml_orerror = "Error occurred obtaining public/private keys: " + string.Join(" | ", caughtErrors);
				privateKeyXml = null;
				return null;
			}

			const string successStartString = "Success:";
			const string splitBetweenPrivateAndPublicKeys = "[SPLITBETWEENPRIVATEANDPUBLICKEYS]";
			const string privateKeyDoesNotExistString = "Private key does not exist.";
			const string publicKeyDoesNotExistString = "Public key does not exist.";

			if (string.IsNullOrWhiteSpace(result))
			{
				publicKeyXml_orerror = "Could not obtain private/public keys, empty response received from server";
				privateKeyXml = null;
				return null;
			}
			else if (!result.StartsWith(successStartString, StringComparison.InvariantCultureIgnoreCase))
			{
				publicKeyXml_orerror = "Error obtaining private/public keys: " + result;
				privateKeyXml = null;
				return null;
			}
			else
			{
				string[] splitted = result.Substring(successStartString.Length).Split(new string[] { splitBetweenPrivateAndPublicKeys }, StringSplitOptions.RemoveEmptyEntries);
				if (splitted.Length != 2)
				{
					publicKeyXml_orerror = "Could not obtain private/public keys, unable to parse result from server";
					privateKeyXml = null;
					return null;
				}
				else if (splitted[0].Equals(publicKeyDoesNotExistString, StringComparison.InvariantCultureIgnoreCase)
					|| splitted[1].Equals(privateKeyDoesNotExistString, StringComparison.InvariantCultureIgnoreCase))
				{
					publicKeyXml_orerror = null;
					privateKeyXml = null;
					return false;//Does not exist
				}

				publicKeyXml_orerror = splitted[0];
				privateKeyXml = splitted[1];
				return true;
			}
		}

		public static bool GetPublicAndPrivateKey(string orderCodeReversed, string machineSignature, out string publicKeyXml_orerror, out string privateKeyXml)
		{
			try
			{
				//We do not store private/public keys locally on the licensing server anymore, just retreive them from firepuma db
				//string publicKeyPath = _getprivatekeyPath(applicationName, false);
				//string privateKeyPath = _getprivatekeyPath(applicationName, true);

				//if (File.Exists(publicKeyPath) && File.Exists(privateKeyPath))
				//{
				//    string filecontentPub = File.ReadAllText(publicKeyPath);
				//    string filecontentPri = File.ReadAllText(privateKeyPath);
				//    if (!string.IsNullOrWhiteSpace(filecontentPub) && !string.IsNullOrWhiteSpace(filecontentPri))
				//    {
				//        publicKeyXml_orerror = filecontentPub;
				//        privateKeyXml = filecontentPri;
				//        return true;
				//    }
				//}

				bool? obtainedPublicPrivateKeysOnlineSuccess = ObtainPublicAndPrivateKeyFromOnline(orderCodeReversed, machineSignature, out publicKeyXml_orerror, out privateKeyXml);
				if (!obtainedPublicPrivateKeysOnlineSuccess.HasValue)//Error occurred
					return false;//We could not obtain it from online, error occurred (not meaning they do not exist)
				else if (obtainedPublicPrivateKeysOnlineSuccess.Value == true)
					return true;//out Public/private key already set
				else//they do not exist online
				{
					publicKeyXml_orerror = "Public/private keys do not exist online, they should have been saved with the license XML";
					privateKeyXml = null;
					return false;
				}

				/*var rsa = new RSACryptoServiceProvider(2048);
				publicKeyXml_orerror = rsa.ToXmlString(false);
				privateKeyXml = rsa.ToXmlString(true);
				//File.WriteAllText(publicKeyPath, publicKeyXml_orerror);
				//File.WriteAllText(privateKeyPath, privateKeyXml);
				return true;*/
			}
			catch (Exception exc)
			{
				publicKeyXml_orerror = exc.Message;
				privateKeyXml = null;
				return false;
			}
		}
	}
}