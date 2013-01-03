using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Net;
using System.Web;
using System.Reflection;

namespace SharedClasses
{
	public class PublishDetails
	{
		/* Additional dependencies for this file:
			Full framework
			Class: BuildingInterop
			Class: SharedClassesSettings
			Assembly: System.Web*/

		public const string OnlineJsonCategory = "Own Applications";
		public const string LastestVersionJsonNamePostfix = " - latest";

		public string ApplicationName;
		public string ApplicationVersion;
		public long SetupSize;
		public string MD5Hash;
		public DateTime PublishedDate;
		public string FtpUrl;
		//TODO: May want to add TracUrl here
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

	public static class PublishInterop
	{
		//public const string RootUrlForApps = "http://fjh.dyndns.org";//http://apps.getmyip.com
		//public const string RootUrlForApps = "https://fjh.dyndns.org";
		//public const string RootFtpUrlForAppsUploading = "ftp://fjh.dyndns.org";
		//TODO: Url (apps.getmyip.com) blocked at work, as IT to whitelist
		static int seeAboveTODOatRootUrlsForApps;

		public static readonly string cProjectsRootDir = @"C:\Francois\Dev\VSprojects";

		private static bool GetNewVersionStringFromAssemblyFile(string csprojFilename, bool AutomaticallyUpdateRevision, bool updateVersionInFile, out string errorIfNull, out string newVersionString, out string currentVersionString)
		{
			string assemblyInfoFilePath = Path.GetDirectoryName(csprojFilename).TrimEnd('\\') + "\\Properties\\AssemblyInfo.cs";
			if (!File.Exists(assemblyInfoFilePath))
			{
				errorIfNull = "Unable to read version from assembly file, file missing: " + assemblyInfoFilePath;
				newVersionString = null;
				currentVersionString = null;
				return false;
			}
			string fileContents = File.ReadAllText(assemblyInfoFilePath);
			//AssemblyVersion("1.0.0.0")
			string regex = @"(?<=AssemblyFileVersion[^\(]*\([^""]*"")[0-9]*\.[0-9]*\.[0-9]*\.[0-9]*(?=""[^""]*\))";
			var match = Regex.Match(fileContents, regex);
			if (match == null || !match.Success)
			{
				errorIfNull = "Could not find Regex match '" + regex + "' for AssemblyFileVersion in assembly file: " + assemblyInfoFilePath;
				newVersionString = null;
				currentVersionString = null;
				return false;
			}
			else
			{
				string currentVersion = fileContents.Substring(match.Index, match.Length);
				if (!AutomaticallyUpdateRevision)
				{
					errorIfNull = null;
					newVersionString = currentVersion;
					currentVersionString = currentVersion;
					return true;
				}
				string lastRevisionSection = currentVersion.Substring(currentVersion.LastIndexOf('.') + 1);
				int tmpint;
				if (!int.TryParse(lastRevisionSection, out tmpint))
				{
					errorIfNull = "Could not increase revision (unable to get int from " + lastRevisionSection + "), full current version string = " + currentVersion;
					newVersionString = null;
					currentVersionString = null;
					return false;
				}
				errorIfNull = null;
				string newVersionStr = currentVersion.Substring(0, currentVersion.LastIndexOf('.') + 1) + ++tmpint;
				if (updateVersionInFile)
				{
					string newFileContents = fileContents.Substring(0, match.Index)
						+ newVersionStr + fileContents.Substring(match.Index + match.Length);
					File.WriteAllText(assemblyInfoFilePath, newFileContents);
				}
				newVersionString = newVersionStr;
				currentVersionString = currentVersion;
				return true;
			}
		}

		public static string GetNsisExportsPath(string subFolder = null)
		{
			string localAppDatapath = CalledFromService.Environment_GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

			string nsisExportsPath = Path.Combine(localAppDatapath, @"FJH\NSISinstaller\NSISexports");
			if (!string.IsNullOrWhiteSpace(subFolder))
				return Path.Combine(nsisExportsPath, subFolder);
			else
				return nsisExportsPath;
		}

		public static string GetPublicKeyForApplicationLicense(string applicationName, Action<string> onError)
		{
			string result = PhpInterop.PostPHP(null,
					string.Format("http://fjh.dyndns.org/licensing/{0}/{1}",
						"getpublickey",
						EncodeAndDecodeInterop.EncodeStringHex(EncryptionInterop.SimpleTripleDesEncrypt(applicationName, LicensingInterop_Shared.cEncryptionKey_OnlineServerPhp), onError)),
					null);
			if (string.IsNullOrWhiteSpace(result))
			{
				onError("Could not obtain public key from server, the response from the server was empty");
				return null;
			}
			else if (result.StartsWith(LicensingInterop_Shared.cPublicKeyFoundOnServerStartMessage, StringComparison.InvariantCultureIgnoreCase))
				return result.Substring(LicensingInterop_Shared.cPublicKeyFoundOnServerStartMessage.Length);
			else
			{
				onError("Unknown error obtaining Public Key from server: " + result);
				return null;
			}
		}

		public const string cTempWebfolderName = "TempWeb";
		public static bool PerformPublish(string projName, bool _64Only, bool HasPlugins, bool AutomaticallyUpdateRevision, bool InstallLocallyAfterSuccessfullNSIS, bool StartupWithWindows, bool SelectSetupIfSuccessful, out string publishedVersionString, out string publishedSetupPath, Action<string, FeedbackMessageTypes> actionOnMessage, Action<int> actionOnProgressPercentage, bool placeSetupInTempWebFolder = false, string customSetupFilename = null)
		{
			if (!Directory.Exists(cProjectsRootDir)
				&& !Directory.Exists(projName)
				&& !File.Exists(projName))
			{
				actionOnMessage("Cannot find root project directory: " + cProjectsRootDir, FeedbackMessageTypes.Error);
				publishedVersionString = null;
				publishedSetupPath = null;
				return false;
			}

			string projDir =
                    Directory.Exists(projName) ? projName :
							Path.Combine(cProjectsRootDir, projName);

			if (Directory.Exists(projName))
				projName = Path.GetFileNameWithoutExtension(projName);

			actionOnMessage("Attempting to build project " + projName, FeedbackMessageTypes.Status);

			var projToBuild = new VSBuildProject_NonAbstract(projName);
			List<string> csprojPaths;
			//string errorIfNotNull;
			if (!projToBuild.PerformBuild(actionOnMessage, out csprojPaths))
			{
				publishedVersionString = null;
				publishedSetupPath = null;
				return false;
			}

			//If it reaches this point (after PerformBuild), there is at least one item in csprojPaths (otherwise would have returned false)
			if (csprojPaths.Count > 1)//Just checking we did not get multiple .csproj paths which matches name of .sln file
			{
				actionOnMessage("Multiple .csproj files found matching name of solution (" + projToBuild.SolutionFullpath + "):"
					+ Environment.NewLine + string.Join(Environment.NewLine, csprojPaths), FeedbackMessageTypes.Error);
				publishedVersionString = null;
				publishedSetupPath = null;
				return false;
			}

			string csprojFilename = csprojPaths[0];

			string errifNull;
			string outNewVersion;
			string outCurrentVersion;
			if (!GetNewVersionStringFromAssemblyFile(csprojFilename, AutomaticallyUpdateRevision, true, out errifNull, out outNewVersion, out outCurrentVersion))
			{
				actionOnMessage(errifNull, FeedbackMessageTypes.Error);
				publishedVersionString = null;
				publishedSetupPath = null;
				return false;
			}
			publishedVersionString = outCurrentVersion;

			actionOnMessage(
					AutomaticallyUpdateRevision
					? "Project " + projName + " will be published as version " + publishedVersionString + " but sourcecode updated to version " + outNewVersion + ", attempting to publish..."
					: "Using current revision of " + projName + " (" + publishedVersionString + "), attempting to publish...",
					FeedbackMessageTypes.Status);

			string tmpSubfolder = placeSetupInTempWebFolder ? cTempWebfolderName + "\\" : "";
			string nsisExportsRoot = GetNsisExportsPath();// Path.Combine(localAppDatapath, @"FJH\NSISinstaller\NSISexports").TrimEnd('\\');
			string nsisFileName = nsisExportsRoot + "\\" + tmpSubfolder + projName + "_" + publishedVersionString + ".nsi";
			publishedSetupPath = Path.GetDirectoryName(nsisFileName) + "\\"
				+ (customSetupFilename != null ? customSetupFilename : NsisInterop.GetSetupNameForProduct(projName.InsertSpacesBeforeCamelCase(), publishedVersionString));

			if (!Directory.Exists(Path.GetDirectoryName(nsisFileName)))
				Directory.CreateDirectory(Path.GetDirectoryName(nsisFileName));
			File.WriteAllText(Path.Combine(Path.GetDirectoryName(nsisFileName), @"DotNetChecker.nsh"),
				NsisInterop.DotNetChecker_NSH_file);

			string registryEntriesFilename = "RegistryEntries.json";
			string registryEntriesFilepath = Path.Combine(Path.GetDirectoryName(csprojFilename), "Properties", registryEntriesFilename);

			string binariesDir = NsisInterop.GetBinariesDirectoryPathFromVsProjectName(projName);//Only used to get public key
			string publicKeyForApplicationLicenseFilename = LicensingInterop_Shared.cLicensePublicKeyFilename;
			string publicKeyFilePath = Path.Combine(binariesDir, publicKeyForApplicationLicenseFilename);
			string publicKeyFromOnline = GetPublicKeyForApplicationLicense(
				Path.GetFileNameWithoutExtension(csprojFilename),//Application Name
				err => actionOnMessage(err, FeedbackMessageTypes.Error));
			if (publicKeyFromOnline != null)//If it is null, an error would have already been logged via actionOnMessage
				File.WriteAllText(publicKeyFilePath, publicKeyFromOnline);

			File.WriteAllLines(nsisFileName,
				NsisInterop.CreateOwnappNsis(
					projName,
					projName.InsertSpacesBeforeCamelCase(),
					publishedVersionString,//Should obtain (and increase) product version from csproj file
					SettingsSimple.HomePcUrls.Instance.AppsPublishingRoot + "/ownapplications/" + projName.ToLower(),//"http://fjh.dyndns.org/ownapplications/" + projName.ToLower(),
					projName + ".exe",
					RegistryInterop.GetRegistryAssociationItemFromJsonFile(registryEntriesFilepath, actionOnMessage),
					null,
					true,
					NsisInterop.NSISclass.DotnetFrameworkTargetedEnum.DotNet4client,
					_64Only,
					StartupWithWindows,
					HasPlugins,
					customSetupFilename));

			string startMsg = "Successfully created NSIS file: ";
			actionOnMessage(startMsg + nsisFileName, FeedbackMessageTypes.Success);

			string nsisDir = NsisInterop.GetNsisInstallDirectory();
			string dotnetCheckerDllPath = Path.Combine(nsisDir, "Plugins", "dotnetchecker.dll");

			if (!File.Exists(dotnetCheckerDllPath))
			{
				string downloadededPath = NetworkInteropSimple.FtpDownloadFile(
						Path.GetDirectoryName(dotnetCheckerDllPath),
						SettingsSimple.OnlineAppsSettings.Instance.AppsDownloadFtpUsername,//GlobalSettings.VisualStudioInteropSettings.Instance.FtpUsername,
						SettingsSimple.OnlineAppsSettings.Instance.AppsDownloadFtpPassword,//GlobalSettings.VisualStudioInteropSettings.Instance.FtpPassword,
						SettingsSimple.PublishSettings.Instance.OnlineDotnetCheckerDllFileUrl,
						actionOnMessage,
						actionOnProgressPercentage);
				if (downloadededPath == null)
					UserMessages.ShowWarningMessage("Could not find (or download) DotNetChecker.dll from URL: " + SettingsSimple.PublishSettings.Instance.OnlineDotnetCheckerDllFileUrl);
				else
					dotnetCheckerDllPath = downloadededPath;
			}

			string MakeNsisFilePath = Path.Combine(nsisDir, "makensis.exe");//@"C:\Program Files (x86)\NSIS\makensis.exe";
			if (!File.Exists(MakeNsisFilePath))
				actionOnMessage("Could not find MakeNsis.exe: " + MakeNsisFilePath, FeedbackMessageTypes.Error);
			else
			{
				if (File.Exists(publishedSetupPath))
					File.Delete(publishedSetupPath);
				Process nsisCompileProc = Process.Start(MakeNsisFilePath, "\"" + nsisFileName + "\"");
				nsisCompileProc.WaitForExit();

				try
				{
					File.Delete(publicKeyFilePath);
				}
				catch (Exception exc)
				{
					actionOnMessage("Failed to delete Public Key file: " + exc.Message, FeedbackMessageTypes.Error);
				}

				if (File.Exists(publishedSetupPath))
				{
					if (InstallLocallyAfterSuccessfullNSIS || SelectSetupIfSuccessful)
					{
						actionOnMessage("Publish success, opening folder and/or running setup file...", FeedbackMessageTypes.Success);
						if (SelectSetupIfSuccessful)
							Process.Start("explorer", "/select, \"" + publishedSetupPath + "\"");
						if (InstallLocallyAfterSuccessfullNSIS)
						{
							Process curproc = Process.GetCurrentProcess();
							bool DoNotKillProcessAndInstall = projName.Equals(curproc.ProcessName, StringComparison.InvariantCultureIgnoreCase);
							if (projName.Equals("StandaloneUploader", StringComparison.InvariantCultureIgnoreCase))
								DoNotKillProcessAndInstall = true;

							if (!DoNotKillProcessAndInstall)
								ProcessesInterop.KillProcess(projName, actionOnMessage);

							if (DoNotKillProcessAndInstall)
							{
								actionOnMessage("Launching setup for '{0}', not running silently because same application name as current.".Fmt(projName), FeedbackMessageTypes.Status);
								Process.Start(publishedSetupPath);
							}
							else
							{
								actionOnMessage("Installing '{0}' silently.".Fmt(projName), FeedbackMessageTypes.Status);
								var setupProc = Process.Start(publishedSetupPath, "/S");
								setupProc.WaitForExit();
								actionOnMessage("Launching '{0}'.".Fmt(projName), FeedbackMessageTypes.Status);
								try { Process.Start(projName + ".exe"); }
								catch (Exception exc) { actionOnMessage("Error launching '{0}': {1}".Fmt(projName, exc.Message), FeedbackMessageTypes.Error); }
							}
						}
					}
					return true;
				}
				else actionOnMessage("Could not successfully create setup for " + projName, FeedbackMessageTypes.Error);
			}
			return false;
		}

		public static bool PerformPublishOnline(string projName, bool _64Only, bool HasPlugins, bool AutomaticallyUpdateRevision, bool InstallLocallyAfterSuccessfullNSIS, bool StartupWithWindows, bool SelectSetupIfSuccessful, bool OpenWebsite, out string publishedVersionString, out string publishedSetupPath, Action<string, FeedbackMessageTypes> actionOnMessage, Action<int> actionOnProgressPercentage)
		{
			List<string> BugsFixed = null;
			List<string> Improvements = null;
			List<string> NewFeatures = null;

			ServicePointManager.DefaultConnectionLimit = 10000;

			bool successPublish = PerformPublish(
				projName,
				_64Only,
				HasPlugins,
				AutomaticallyUpdateRevision,
				InstallLocallyAfterSuccessfullNSIS,
				StartupWithWindows,
				SelectSetupIfSuccessful,
				out publishedVersionString,
				out publishedSetupPath,
				actionOnMessage,
				actionOnProgressPercentage);

			if (!successPublish)
				return false;
			
			string validatedUrlsectionForProjname = HttpUtility.UrlPathEncode(projName).ToLower();

			int changedTheFollowing;

			string relativeUrl = "/downloadownapps.php?relativepath=" + validatedUrlsectionForProjname;

			//string rootFtpUri = GlobalSettings.VisualStudioInteropSettings.Instance.GetCombinedUriForVsPublishing() + "/" + validatedUrlsectionForProjname;
			//string rootFtpUri = WebInterop.RootFtpUrlForAppsUploading.TrimEnd('/') + relativeUrl;
			string rootDownloadHttpUri = SharedClasses.SettingsSimple.HomePcUrls.Instance.AppsPublishingRoot.TrimEnd('/') + relativeUrl.TrimEnd('/');
			
			PublishDetails publishDetails = new PublishDetails(
					projName,
					publishedVersionString,
					new FileInfo(publishedSetupPath).Length,
					publishedSetupPath.FileToMD5Hash(),
					DateTime.Now,
					rootDownloadHttpUri + "/" + (new FileInfo(publishedSetupPath).Name));
			string errorStringIfFailElseJsonString;
			if (!WebInterop.SaveObjectOnline(PublishDetails.OnlineJsonCategory, projName + " - " + publishedVersionString, publishDetails, out errorStringIfFailElseJsonString))
			{
				actionOnMessage("Cannot save json online (" + projName + " - " + publishedVersionString + "), setup and index.html cancelled for project " + projName + ": " + errorStringIfFailElseJsonString, FeedbackMessageTypes.Error);
				return false;
			}
			if (!WebInterop.SaveObjectOnline(PublishDetails.OnlineJsonCategory, projName + PublishDetails.LastestVersionJsonNamePostfix, publishDetails, out errorStringIfFailElseJsonString))
			{
				actionOnMessage("Cannot save json online (" + projName + PublishDetails.LastestVersionJsonNamePostfix + "), setup and index.html cancelled for project " + projName + ": " + errorStringIfFailElseJsonString, FeedbackMessageTypes.Error);
				return false;
			}

			string htmlFilePath = CreateHtmlPageReturnFilename(
					projName,
					publishedVersionString,
					publishedSetupPath,
					BugsFixed,
					Improvements,
					NewFeatures,
					publishDetails);

			if (htmlFilePath == null)
			{
				actionOnMessage("Could not obtain embedded HTML file", FeedbackMessageTypes.Error);
				return false;
			}

			actionOnMessage("Attempting Ftp Uploading of Setup file and index file for " + projName, FeedbackMessageTypes.Status);
			string uriAfterUploading = GlobalSettings.VisualStudioInteropSettings.Instance.GetCombinedUriForAFTERvspublishing() + "/" + validatedUrlsectionForProjname;

			bool isAutoUpdater = projName.Replace(" ", "").Equals("AutoUpdater", StringComparison.InvariantCultureIgnoreCase);
			bool isShowNoCallbackNotification = projName.Replace(" ", "").Equals("ShowNoCallbackNotification", StringComparison.InvariantCultureIgnoreCase);
			bool isStandaloneUploader = projName.Replace(" ", "").Equals("StandaloneUploader", StringComparison.InvariantCultureIgnoreCase);
			string clonedSetupFilepathIfAutoUpdater = 
					//Do not change this name, it is used in NSIS for downloading AutoUpdater if not installed yet
					Path.Combine(Path.GetDirectoryName(publishedSetupPath), "AutoUpdater_SetupLatest.exe");
			string clonedSetupFilepathIfShowNoCallbackNotification =
					//Do not change this name, it is used in NSIS for downloading AutoUpdater if not installed yet
					Path.Combine(Path.GetDirectoryName(publishedSetupPath), "ShowNoCallbackNotification_SetupLatest.exe");
			string clonedSetupFilepathIfStandaloneUploader =
					//Do not change this name, it is used in NSIS for downloading AutoUpdater if not installed yet
					Path.Combine(Path.GetDirectoryName(publishedSetupPath), "StandaloneUploader_SetupLatest.exe");

			if (isAutoUpdater)
				File.Copy(publishedSetupPath, clonedSetupFilepathIfAutoUpdater, true);
			if (isShowNoCallbackNotification)
				File.Copy(publishedSetupPath, clonedSetupFilepathIfShowNoCallbackNotification, true);
			if (isStandaloneUploader)
				File.Copy(publishedSetupPath, clonedSetupFilepathIfStandaloneUploader, true);

			Dictionary<string, string> localFiles_DisplaynameFirst = new Dictionary<string, string>();
			localFiles_DisplaynameFirst.Add("Setup path for " + projName, publishedSetupPath);
			localFiles_DisplaynameFirst.Add("index.html for " + projName, htmlFilePath);
			if (isAutoUpdater) localFiles_DisplaynameFirst.Add("Newest AutoUpdater setup", clonedSetupFilepathIfAutoUpdater);
			if (isShowNoCallbackNotification) localFiles_DisplaynameFirst.Add("Newest ShowNoCallbackNotification", clonedSetupFilepathIfShowNoCallbackNotification);
			if (isStandaloneUploader) localFiles_DisplaynameFirst.Add("Newest StandaloneUploader", clonedSetupFilepathIfStandaloneUploader);

			//bool uploaded = true;
			bool uploadsQueued = true;
			//Queue files in StandaloneUploader application
			foreach (var dispname in localFiles_DisplaynameFirst.Keys)
			{
				string localfilepath = localFiles_DisplaynameFirst[dispname];
				if (!StandaloneUploaderInterop.UploadVia_StandaloneUploader_UsingExternalApp(
					err => actionOnMessage(err, FeedbackMessageTypes.Error),
					dispname,
					UploadingProtocolTypes.Ownapps,
					localfilepath,
					//rootFtpUri.TrimEnd('/') + "/" + Path.GetFileName(localfilepath),
					validatedUrlsectionForProjname + "/" + Path.GetFileName(localfilepath),
					SettingsSimple.OnlineAppsSettings.Instance.AppsUploadFtpUsername,
					SettingsSimple.OnlineAppsSettings.Instance.AppsUploadFtpPassword,
					true))
					uploadsQueued = false;
			}

			if (uploadsQueued)
			{
				actionOnMessage("All uploads successfully queued with StandaloneUploader", FeedbackMessageTypes.Success);
				return true;
			}
			else
			{
				actionOnMessage("Unable to queue all files with StandaloneUploader", FeedbackMessageTypes.Error);
				return false;
			}
		}

		public static string GetApplicationExePathFromApplicationName(string applicationName)
		{
			return Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
				applicationName.InsertSpacesBeforeCamelCase(),
				applicationName + ".exe");
		}

		public static bool IsInstalled(string applicationName)
		{
			string appExePath = GetApplicationExePathFromApplicationName(applicationName);
			return File.Exists(appExePath);
		}

		public static string CreateHtmlPageReturnFilename(string projectName, string projectVersion, string setupFilename, List<string> BugsFixed, List<string> Improvements, List<string> NewFeatures, PublishDetails publishDetails = null)
		{
			string tempFilename = Path.GetTempPath() + "index.html";

			//string description = "";// "This is the description for " + projectName + ".";
			string bugsfixed = "";
			string improvements = "";
			string newfeatures = "";
			if (BugsFixed != null) foreach (string bug in BugsFixed) bugsfixed += "<li>" + bug + "</li>";
			if (Improvements != null) foreach (string improvement in Improvements) improvements += "<li>" + improvement + "</li>";
			if (NewFeatures != null) foreach (string newfeature in NewFeatures) newfeatures += "<li>" + newfeature + "</li>";

			//bool HtmlFileFound = false;

			string HtmlTemplateFileName = "VisualStudioInterop (publish page).html";
			byte[] bytesOfHtmlFile;
			if (!Helpers.GetEmbeddedResource_FirstOneEndingWith(HtmlTemplateFileName, out bytesOfHtmlFile))
			{
				UserMessages.ShowWarningMessage("Could not find Html file in resources: " + HtmlTemplateFileName);
				return null;
			}
			else
			{
				File.WriteAllBytes(tempFilename, bytesOfHtmlFile);
				string textOfFile = File.ReadAllText(tempFilename);
				textOfFile = textOfFile.Replace("{PageGeneratedDate}", DateTime.Now.ToString(@"dddd, dd MMMM yyyy \a\t HH:mm:ss"));
				textOfFile = textOfFile.Replace("{ProjectName}", projectName);
				textOfFile = textOfFile.Replace("{ProjectVersion}", projectVersion);
				//textOfFile = textOfFile.Replace("{SetupFilename}", Path.GetFileName(setupFilename));
				textOfFile = textOfFile.Replace("{SetupFilename}", "/downloadownapps.php?relativepath=" + projectName + "/" + Path.GetFileName(setupFilename));
				//textOfFile = textOfFile.Replace("{DescriptionLiElements}", description);
				textOfFile = textOfFile.Replace("{BugsFixedList}", bugsfixed);
				textOfFile = textOfFile.Replace("{ImprovementList}", improvements);
				textOfFile = textOfFile.Replace("{NewFeaturesList}", newfeatures);
				if (publishDetails != null)
					textOfFile = textOfFile.Replace("{JsonText}", publishDetails.GetJsonString());
				File.WriteAllText(tempFilename, textOfFile);
			}

			return tempFilename;
		}
	}
}