using System;

namespace SharedClasses
{
	/// <summary>
	/// Contains the shared code/constants between client (with application to be licensed) and server (which leases and revokes licenses)
	/// </summary>
	public static class LicensingInterop_Shared
	{
		//private const string cExpirationDateFormat = "s";//KEEP AS IS, also used in LicensingServer code, class IssuedLicense
		public const string cExpirationDateFormat = "yyyy-MM-ddTHH:mm:ss.fffffff";//KEEP AS IS, also used in LicensingServer code, class IssuedLicense

		public const string cEncryptionKey_OnlineServerPhp = "zvut8pqrd2efgys1cijklnox";//LEAVE AS IS, also used online in licensing.php controller
		public const string cEncryptionKey_EmailToDeveloper = "m590adqe4f8op6bcst71r23n";

		public const string cLicensePublicKeyFilename = "public.key";
		public const string cMachineSignatureKeyName = "machinesignature";

		public const string cLicenseExistsOnServerMessage = "License exists.";
		public const string cLicenseNonExistingOnServerMessage = "License DOES NOT exist on server.";
		public const string cPublicKeyFoundOnServerStartMessage = "Public key found:";

		public static string GetMachineUIDForApplicationAndMachine(string applicationName, string ownerName, string machineSignature, Action<string> onError)
		{
			return EncodeAndDecodeInterop.EncodeStringHex(EncryptionInterop.SimpleTripleDesEncrypt(
				applicationName + "|" + ownerName + "|" + machineSignature,
				LicensingInterop_Shared.cEncryptionKey_EmailToDeveloper),
				onError);
		}

		public static bool DecryptMachineUIDForApplicationAndMachine(string uniqueSignatureEncrypted, out string applicationName, out string ownerName, out string machineSignature, Action<string> onError)
		{
			try
			{
				string decryptedString = EncryptionInterop.SimpleTripleDesDecrypt(
					EncodeAndDecodeInterop.DecodeStringHex(uniqueSignatureEncrypted), LicensingInterop_Shared.cEncryptionKey_EmailToDeveloper)
					.Trim();
				string[] pipeSplitted = decryptedString.Trim().Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
				if (pipeSplitted.Length != 3)
				{
					onError("Unable to decrypt the unique signature");
					applicationName = null;
					ownerName = null;
					machineSignature = null;
					return false;
				}
				applicationName = pipeSplitted[0].Trim();
				ownerName = pipeSplitted[1].Trim();
				machineSignature = pipeSplitted[2].Trim().Trim('\0');
				return true;
			}
			catch (Exception exc)
			{
				onError(exc.Message);
				applicationName = null;
				ownerName = null;
				machineSignature = null;
				return false;
			}
		}
	}
}