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

		public const string cEncryptionKey = "zvut8pqrd2efgys1cijklnox";//LEAVE AS IS, also used online in licensing.php controller

		public const string cLicensePublicKeyFilename = "public.key";

		public const string cLicenseExistsOnServerMessage = "License exists.";
		public const string cLicenseNonExistingOnServerMessage = "License DOES NOT exist on server.";
		public const string cPublicKeyFoundOnServerStartMessage = "Public key found:";
	}
}