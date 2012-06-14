using System;
using System.Linq;
using System.IO;
using System.Security.Cryptography;

namespace SharedClasses
{
	public static class StringExtensions_AutoUpdating
	{
		/// <summary>
		/// Function to get file impression in form of string from a file location.
		/// </summary>
		/// <param name="_fileName">File Path to get file impression.</param>
		/// <returns>Byte Array</returns>
		public static string FileToMD5Hash(this string _fileName)
		{
			if (!File.Exists(_fileName))
				return "[InvalidFilePath:" + _fileName + "]";
			using (var stream = new BufferedStream(File.OpenRead(_fileName), 1200000))
			{
				SHA256Managed sha = new SHA256Managed();
				byte[] checksum = sha.ComputeHash(stream);
				return BitConverter.ToString(checksum).Replace("-", string.Empty);
			}
		}
	}
	public class PublishDetails
	{
		public const string OnlineJsonCategory = "Own Applications";
		public const string LastestVersionJsonNamePostfix = " - latest";

		public string ApplicationName;
		public string ApplicationVersion;
		public long SetupSize;
		public string MD5Hash;
		public DateTime PublishedDate;
		public string FtpUrl;
		public PublishDetails() { }
		public PublishDetails(string ApplicationName, string ApplicationVersion, long SetupSize, string MD5Hash, DateTime PublishedDate, string FtpUrl)
		{
			this.ApplicationName = ApplicationName;
			this.ApplicationVersion = ApplicationVersion;
			this.SetupSize = SetupSize;
			this.MD5Hash = MD5Hash;
			this.PublishedDate = PublishedDate;
			this.FtpUrl = FtpUrl;
		}
		public string GetJsonString() { return WebInterop.GetJsonStringFromObject(this, true); }
	}

	public class AutoUpdating
	{
		//public enum VersionComparison { UpToDate, NewerAvailable, Invalid };
		//Invalid if app version is newer than online or not found online

		//true=uptodate,false=neweravailable,null=errorCheckMessage
		public static bool? IsApplicationUpToDate(string ApplicationName, string installedVersion, out string errorIfNull, out PublishDetails detailsIfNewer)
		{
			detailsIfNewer = null;//Only details if newer version available
			PublishDetails onlineAppDetails = new PublishDetails();
			string errIfFail;
			bool populatesuccess = WebInterop.PopulateObjectFromOnline(
				PublishDetails.OnlineJsonCategory,
				ApplicationName + PublishDetails.LastestVersionJsonNamePostfix,
				onlineAppDetails,
				out errIfFail);
			if (populatesuccess)
			{
				//return CompareVersions(installedVersion, onlineAppDetails.ApplicationVersion);
				string onlineVersion = onlineAppDetails.ApplicationVersion;
				string versionsConcatenated = string.Format("InstalledVersion = {0}, OnlineVersion = {1}", installedVersion ?? "", onlineVersion ?? "");
				if (string.IsNullOrWhiteSpace(installedVersion) || string.IsNullOrWhiteSpace(onlineVersion))
				{
					errorIfNull = "InstalledVersion AND/OR OnlineVersion is empty: " + versionsConcatenated;
					return null;
				}
				string[] installedSplitted = installedVersion.Split('.');
				string[] onlineSplitted = onlineVersion.Split('.');
				if (installedSplitted.Length != onlineSplitted.Length)
				{
					errorIfNull = "InstalledVersion and OnlineVersion not in same format: " + versionsConcatenated;
					return null;
				}

				int tmpint;
				bool fail = false;
				installedSplitted.ToList().ForEach((s) => { if (!int.TryParse(s, out tmpint)) fail = true; });
				onlineSplitted.ToList().ForEach((s) => { if (!int.TryParse(s, out tmpint)) fail = true; });
				if (fail)
				{
					errorIfNull = "InstalledVersion and OnlineVersion must have integers between dots: " + versionsConcatenated;
					return null;
				}

				//if (installedAppVersion.Equals(onlineVersion, StringComparison.InvariantCultureIgnoreCase))
				//    return VersionComparison.UpToDate;

				for (int i = 0; i < installedSplitted.Length; i++)
				{
					int tmpInstalledInt;
					int tmpOnlineInt;
					tmpInstalledInt = int.Parse(installedSplitted[i]);
					tmpOnlineInt = int.Parse(onlineSplitted[i]);

					if (tmpInstalledInt == tmpOnlineInt)
						continue;
					if (tmpInstalledInt > tmpOnlineInt)
					{
						errorIfNull = "InstalledVersion is newer than OnlineVersion: " + versionsConcatenated;
						return null;
					}
					else
					{
						errorIfNull = null;
						detailsIfNewer = onlineAppDetails;
						return false;
					}
				}
				errorIfNull = null;
				return true;
			}
			else
			{
				if (errIfFail == WebInterop.cErrorIfNotFoundOnline)
					errorIfNull = "Update information not stored online yet for " + ApplicationName + ".";
				else
					errorIfNull = errIfFail;
				return null;
			}
		}
	}
}