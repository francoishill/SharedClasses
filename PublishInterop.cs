using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Net;
using System.Web;
using System.Reflection;
using System.Threading.Tasks;

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
		public DateTime? TracTicketsSinceDate;//The Trac ticketing system will be queried for 'recent' tickets using this as 'sinceDate'

		public TracXmlRpcInterop.ChangeLogs ChangeLogs;

		public PublishDetails() { }
		public PublishDetails(string ApplicationName, string ApplicationVersion, long SetupSize, string MD5Hash, DateTime PublishedDate, string FtpUrl, DateTime? TracTicketsSinceDate, TracXmlRpcInterop.ChangeLogs ChangeLogs)
		{
			this.ApplicationName = ApplicationName;
			this.ApplicationVersion = ApplicationVersion;
			this.SetupSize = SetupSize;
			this.MD5Hash = MD5Hash;
			this.PublishedDate = PublishedDate;
			this.FtpUrl = FtpUrl;
			this.TracTicketsSinceDate = TracTicketsSinceDate;
			this.ChangeLogs = ChangeLogs;
		}
		public string GetJsonString() { return WebInterop.GetJsonStringFromObject(this, true); }
	}

	public static class PublishInterop
	{
		//public const string RootUrlForApps = "http://fjh.dyndns.org";//http://apps.getmyip.com
		//public const string RootUrlForApps = "https://fjh.dyndns.org";
		//public const string RootFtpUrlForAppsUploading = "ftp://fjh.dyndns.org";

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
		public static bool PerformPublish(string projName, bool _64Only, bool HasPlugins, bool AutomaticallyUpdateRevision, bool InstallLocallyAfterSuccessfullNSIS, bool StartupWithWindows, bool SelectSetupIfSuccessful, out string publishedVersionString, out string publishedSetupPath, out DateTime publishDate, Action<string, FeedbackMessageTypes> actionOnMessage, Action<int> actionOnProgressPercentage, bool placeSetupInTempWebFolder = false, string customSetupFilename = null)
		{
			if (!Directory.Exists(cProjectsRootDir)
				&& !Directory.Exists(projName)
				&& !File.Exists(projName))
			{
				actionOnMessage("Cannot find root project directory: " + cProjectsRootDir, FeedbackMessageTypes.Error);
				publishedVersionString = null;
				publishedSetupPath = null;
				publishDate = DateTime.MinValue;
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
				publishDate = DateTime.MinValue;
				return false;
			}

			//If it reaches this point (after PerformBuild), there is at least one item in csprojPaths (otherwise would have returned false)
			if (csprojPaths.Count > 1)//Just checking we did not get multiple .csproj paths which matches name of .sln file
			{
				actionOnMessage("Multiple .csproj files found matching name of solution (" + projToBuild.SolutionFullpath + "):"
					+ Environment.NewLine + string.Join(Environment.NewLine, csprojPaths), FeedbackMessageTypes.Error);
				publishedVersionString = null;
				publishedSetupPath = null;
				publishDate = DateTime.MinValue;
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
				publishDate = DateTime.MinValue;
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

			string errorIfFailed;
			string registryEntriesFilepath = OwnAppsInterop.GetRegistryEntriesFullfilepath(projName, out errorIfFailed);
			if (registryEntriesFilepath == null)//This means an error occurred, not that the file does not exist
				actionOnMessage("Error occurred obtaining path to RegistryEntries: " + errorIfFailed, FeedbackMessageTypes.Error);
			if (!File.Exists(registryEntriesFilepath))
				registryEntriesFilepath = null;//We now set it to NULL if the file does not exist (do not throw error)

			/*
			We do not distribute the public key with the installation anymore, each license has its own public/private keys
			string binariesDir = NsisInterop.GetBinariesDirectoryPathFromVsProjectName(projName);//Only used to get public key
			string publicKeyForApplicationLicenseFilename = LicensingInterop_Shared.cLicensePublicKeyFilename;
			string publicKeyFilePath = Path.Combine(binariesDir, publicKeyForApplicationLicenseFilename);
			actionOnMessage("Obtaining public key for application, please be patient...", FeedbackMessageTypes.Status);
			string publicKeyFromOnline = GetPublicKeyForApplicationLicense(
				Path.GetFileNameWithoutExtension(csprojFilename),//Application Name
				err => actionOnMessage(err, FeedbackMessageTypes.Error));
			if (publicKeyFromOnline != null)//If it is null, an error would have already been logged via actionOnMessage
				File.WriteAllText(publicKeyFilePath, publicKeyFromOnline);*/

			publishDate = DateTime.Now;

			File.WriteAllLines(nsisFileName,
				NsisInterop.CreateOwnappNsis(
					projName,
					projName.InsertSpacesBeforeCamelCase(),
					publishedVersionString,//Should obtain (and increase) product version from csproj file
					AutoUpdating.GetApplicationOnlineUrl(projName),
					projName + ".exe",
					publishDate,
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

				/*try
				{
					File.Delete(publicKeyFilePath);
				}
				catch (Exception exc)
				{
					actionOnMessage("Failed to delete Public Key file: " + exc.Message, FeedbackMessageTypes.Error);
				}*/

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

		private static string GetValidatedUrlsectionForProjname(string projName)
		{
			return HttpUtility.UrlPathEncode(projName).ToLower();
		}

		public static bool PerformPublishOnline(string projName, bool _64Only, bool HasPlugins, bool AutomaticallyUpdateRevision, bool InstallLocallyAfterSuccessfullNSIS, bool StartupWithWindows, bool SelectSetupIfSuccessful, bool OpenWebsite, out string publishedVersionString, out string publishedSetupPath, out DateTime publishDate, Action<string, FeedbackMessageTypes> actionOnMessage, Action<int> actionOnProgressPercentage)
		{
			/*List<string> BugsFixed = null;
			List<string> Improvements = null;
			List<string> NewFeatures = null;*/

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
				out publishDate,
				actionOnMessage,
				actionOnProgressPercentage);

			if (!successPublish)
				return false;

			string validatedUrlsectionForProjname = GetValidatedUrlsectionForProjname(projName);

			//int changedTheFollowing;
			//DONE: The following was changed
			string relativeUrl = "/downloadownapps.php?relativepath=" + validatedUrlsectionForProjname;

			//string rootFtpUri = GlobalSettings.VisualStudioInteropSettings.Instance.GetCombinedUriForVsPublishing() + "/" + validatedUrlsectionForProjname;
			//string rootFtpUri = WebInterop.RootFtpUrlForAppsUploading.TrimEnd('/') + relativeUrl;
			string rootDownloadHttpUri = SharedClasses.SettingsSimple.HomePcUrls.Instance.AppsPublishingRoot.TrimEnd('/') + relativeUrl.TrimEnd('/');

			//DateTime publishDate = DateTime.Now;

			DateTime? tracTicketsSinceDate;//Use previous published version's publishedDate as the 'sinceDate' for trac tickets
			if (!ObtainPreviouslyPublishedDate(projName, actionOnMessage, out tracTicketsSinceDate))
				return false;//actionOnMessage already occurred inside 'ObtainPreviouslyPublishedDate'

			actionOnMessage("Busy obtaining Trac BugsFixes, Improvements and NewFeatures", FeedbackMessageTypes.Status);
			var changeLogs = GetChangeLogs(
				tracTicketsSinceDate,
				projName,
				actionOnMessage);
			if (changeLogs == null)
				return false;

			PublishDetails currentlyPublishDetails = new PublishDetails(
					projName,
					publishedVersionString,
					new FileInfo(publishedSetupPath).Length,
					publishedSetupPath.FileToMD5Hash(),
					publishDate,
					rootDownloadHttpUri + "/" + (new FileInfo(publishedSetupPath).Name),
					tracTicketsSinceDate.HasValue ? tracTicketsSinceDate : DateTime.MinValue,
					changeLogs);

			string errorStringIfFailElseJsonString;
			if (!WebInterop.SaveObjectOnline(PublishDetails.OnlineJsonCategory, projName + " - " + publishedVersionString, currentlyPublishDetails, out errorStringIfFailElseJsonString))
			{
				actionOnMessage("Cannot save json online (" + projName + " - " + publishedVersionString + "), setup and index.html cancelled for project " + projName + ": " + errorStringIfFailElseJsonString, FeedbackMessageTypes.Error);
				return false;
			}
			if (!WebInterop.SaveObjectOnline(PublishDetails.OnlineJsonCategory, projName + PublishDetails.LastestVersionJsonNamePostfix, currentlyPublishDetails, out errorStringIfFailElseJsonString))
			{
				actionOnMessage("Cannot save json online (" + projName + PublishDetails.LastestVersionJsonNamePostfix + "), setup and index.html cancelled for project " + projName + ": " + errorStringIfFailElseJsonString, FeedbackMessageTypes.Error);
				return false;
			}

			string tmperr;			
			List<string> listOfScreenshotsFullLocalPaths = GetScreenshotsFullpath(projName, out tmperr);
			if (listOfScreenshotsFullLocalPaths == null)
			{
				actionOnMessage("Cannot obtain screenshots: " + tmperr, FeedbackMessageTypes.Error);
				return false;
			}

			/*string htmlFilePath = CreateHtmlPageReturnFilename(
					projName,
					publishedVersionString,
					publishedSetupPath,
					changeLogs,
					out listOfScreenshotsFullLocalPaths,
					currentlyPublishDetails);

			if (htmlFilePath == null)
			{
				actionOnMessage("Could not obtain embedded HTML file", FeedbackMessageTypes.Error);
				return false;
			}*/


			//Upload image files
			if (listOfScreenshotsFullLocalPaths != null)
				foreach (var screenshotFile in listOfScreenshotsFullLocalPaths)
					QueueFileToUpload(projName, Path.GetFileNameWithoutExtension(screenshotFile), screenshotFile, "Screenshots/" + Path.GetFileName(screenshotFile));
			UploadIconAsFaviconIfExists(projName);

			actionOnMessage("Attempting Ftp Uploading of Setup file and index file for " + projName, FeedbackMessageTypes.Status);
			string uriAfterUploading = GlobalSettings.VisualStudioInteropSettings.Instance.GetCombinedUriForAFTERvspublishing() + "/" + validatedUrlsectionForProjname;

			//DONE: We removed the other apps from auto downloading (while NSIS installing) if not installed yet, just left AutoUpdater to remain
			bool isAutoUpdater = projName.Replace(" ", "").Equals("AutoUpdater", StringComparison.InvariantCultureIgnoreCase);
			/*bool isShowNoCallbackNotification = projName.Replace(" ", "").Equals("ShowNoCallbackNotification", StringComparison.InvariantCultureIgnoreCase);
			bool isStandaloneUploader = projName.Replace(" ", "").Equals("StandaloneUploader", StringComparison.InvariantCultureIgnoreCase);*/
			string clonedSetupFilepathIfAutoUpdater = 
					//Do not change this name, it is used in NSIS for downloading AutoUpdater if not installed yet
					Path.Combine(Path.GetDirectoryName(publishedSetupPath), "AutoUpdater_SetupLatest.exe");
			/*string clonedSetupFilepathIfShowNoCallbackNotification =
					//Do not change this name, it is used in NSIS for downloading AutoUpdater if not installed yet
					Path.Combine(Path.GetDirectoryName(publishedSetupPath), "ShowNoCallbackNotification_SetupLatest.exe");
			string clonedSetupFilepathIfStandaloneUploader =
					//Do not change this name, it is used in NSIS for downloading AutoUpdater if not installed yet
					Path.Combine(Path.GetDirectoryName(publishedSetupPath), "StandaloneUploader_SetupLatest.exe");*/

			if (isAutoUpdater)
				File.Copy(publishedSetupPath, clonedSetupFilepathIfAutoUpdater, true);
			/*if (isShowNoCallbackNotification)
				File.Copy(publishedSetupPath, clonedSetupFilepathIfShowNoCallbackNotification, true);
			if (isStandaloneUploader)
				-File.Copy(publishedSetupPath, clonedSetupFilepathIfStandaloneUploader, true);*/

			Dictionary<string, string> localFiles_DisplaynameFirst = new Dictionary<string, string>();
			localFiles_DisplaynameFirst.Add("Setup path for " + projName, publishedSetupPath);
			//localFiles_DisplaynameFirst.Add("index.html for " + projName, htmlFilePath);
			if (isAutoUpdater) localFiles_DisplaynameFirst.Add("Newest AutoUpdater setup", clonedSetupFilepathIfAutoUpdater);
			/*if (isShowNoCallbackNotification) localFiles_DisplaynameFirst.Add("Newest ShowNoCallbackNotification", clonedSetupFilepathIfShowNoCallbackNotification);
			if (isStandaloneUploader) localFiles_DisplaynameFirst.Add("Newest StandaloneUploader", clonedSetupFilepathIfStandaloneUploader);*/

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

		public static bool ObtainPreviouslyPublishedDate(string projName, Action<string, FeedbackMessageTypes> actionOnMessage, out DateTime? previouslyPublishedDate)
		{
			//DateTime? tracTicketsSinceDate = null;
			PublishDetails previouslyPublishedDetails = new PublishDetails();
			string tmperr;
			if (!WebInterop.PopulateObjectFromOnline(PublishDetails.OnlineJsonCategory, projName + PublishDetails.LastestVersionJsonNamePostfix, previouslyPublishedDetails, out tmperr))
			{
				if (tmperr.Equals(WebInterop.cErrorIfNotFoundOnline))
				{
					actionOnMessage("Not published before yet, obtaining all closed tickets as 'recently' closed (" + projName + PublishDetails.LastestVersionJsonNamePostfix + ").", FeedbackMessageTypes.Warning);
					previouslyPublishedDate = null;
					return true;
				}
				else
				{
					actionOnMessage("Cannot obtain previously published details from online (" + projName + PublishDetails.LastestVersionJsonNamePostfix + "), setup and index.html cancelled for project " + projName + ": " + tmperr, FeedbackMessageTypes.Error);
					previouslyPublishedDate = null;
					return false;
				}
			}
			else//We got our previously published details, now see if the field 'TracTicketsSinceDate' is NOT null, then use 'PublishedDate'
			{
				if (!previouslyPublishedDetails.TracTicketsSinceDate.HasValue)
				{
					actionOnMessage("Previously published details has a NULL 'TracTicketsSinceDate', obtaining all closed tickets as 'recently' closed (" + projName + PublishDetails.LastestVersionJsonNamePostfix + ").", FeedbackMessageTypes.Warning);
					previouslyPublishedDate = null;
					return true;
				}
				else
				{
					//Yes we use 'PublishedDate' instead of 'TracTicketsSinceDate',
					//by checking 'previouslyPublishedDetails.TracTicketsSinceDate.HasValue' we just make sure
					//that we have previously actually obtained the Trac tickets
					previouslyPublishedDate = previouslyPublishedDetails.PublishedDate
						.Subtract(TimeSpan.FromMinutes(1));//Just subtracting 1 minute as a safety-buffer
					return true;
				}
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

		private static bool QueueFileToUpload(string projName, string displayName, string localFilepath, string onlineFilenameOnly = null)
		{
			if (onlineFilenameOnly == null)
				onlineFilenameOnly = Path.GetFileName(localFilepath);

			return StandaloneUploaderInterop.UploadVia_StandaloneUploader_UsingExternalApp(
					err => UserMessages.ShowErrorMessage(err),
					displayName,
					UploadingProtocolTypes.Ownapps,
					localFilepath,
					GetValidatedUrlsectionForProjname(projName) + "/" + onlineFilenameOnly,
					SettingsSimple.OnlineAppsSettings.Instance.AppsUploadFtpUsername,
					SettingsSimple.OnlineAppsSettings.Instance.AppsUploadFtpPassword,
					true);
		}

		private static List<string> GetScreenshotsFullpath(string projectName, out string errorIfFailed)
		{
			string tmperr;
			var tmpCsprojAndAppType = OwnAppsInterop.GetCsprojFullpathFromApplicationName(projectName, out tmperr);
			if (tmperr != null)
			{
				errorIfFailed = tmperr;
				return null;
			}
			string csprojPath = tmpCsprojAndAppType.Value.Key;
			string screenshotsDirPath = Path.Combine(Path.GetDirectoryName(csprojPath), "ScreenShots");
			string[] screenshotsImagePaths = OwnAppsInterop.GetPicturesInDirectory(screenshotsDirPath);
			bool hasScreenShots = screenshotsImagePaths != null && screenshotsImagePaths.Length > 0;
			if (hasScreenShots)
			{
				var tmplist = new List<string>();

				List<string> listOfTabnames = new List<string>();
				List<string> listOfImages = new List<string>();
				int cnt = 1;
				foreach (var ss in screenshotsImagePaths)
				{
					//Each screenshot will be uploaded in 'Screenshots' subfolder of app, like QuickAccess/Screenshots/
					string ssFilenamOnly = Path.GetFileName(ss);
					listOfTabnames.Add(string.Format("<li><a href='#tabs-{0}'>{1}</a></li>", cnt, Path.GetFileNameWithoutExtension(ss)));
					listOfImages.Add(string.Format("<div id='tabs-{0}'><a href='Screenshots/{1}' class='preview' title='Click thumbnail to view full-size image'><img src='Screenshots/{1}' alt='{2}' /></a></div>", cnt, ssFilenamOnly, Path.GetFileNameWithoutExtension(ss)));

					tmplist.Add(ss);
					cnt++;
				}

				errorIfFailed = null;
				return tmplist;
			}
			else
			{
				errorIfFailed = null;
				return new List<string>();//No screenshots found
			}
		}

		public static string GetTracXmlRpcHttpPathFromProjectName(string projectName)
		{
			return TracXmlRpcInterop.ChangeLogs.GetTracXmlRpcUrlForApplication(projectName);
			/*foreach (string tmpuri in GlobalSettings.TracXmlRpcInteropSettings.Instance.GetListedXmlRpcUrls())//GlobalSettings.TracXmlRpcInteropSettings.Instance.ListedApplicationNames.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				if (tmpuri.ToLower().Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Contains(projectName.ToLower()))
					return tmpuri;
			return null;*/
		}

		private static string ReplaceNewlinesWithString(string originalString, string replaceLinesWithThisString)
		{
			return originalString.Replace("\r", "").Replace("\n", replaceLinesWithThisString);
		}

		public static TracXmlRpcInterop.ChangeLogs GetChangeLogs(DateTime? sinceDate, string ProjectName, Action<string, FeedbackMessageTypes> actionOnMessage, string Username = null, string Password = null)
		{
			string rootProjectXmlRpcTracUri = GetTracXmlRpcHttpPathFromProjectName(ProjectName);
			if (string.IsNullOrWhiteSpace(rootProjectXmlRpcTracUri))
			{
				//BugsFixed = Improvements = NewFeatures = new List<string>() { "No trac xmlrpc url specified for project " + ProjectName + ", no bugs found." };
				actionOnMessage("No trac xmlrpc url specified for project " + ProjectName + ", no bugs found.", FeedbackMessageTypes.Error);
				return null;//return new List<string>() { "No trac xmlrpc url specified for project " + ProjectName + ", no bugs found." };
			}

			actionOnMessage("Obtaining all ticket descriptions and types from Trac server...", FeedbackMessageTypes.Status);
			Dictionary<int, TracXmlRpcInterop.TracTicketDetails> tmpIdsAndDescriptionsAndTicketTypes =
				TracXmlRpcInterop.GetAllClosedTicketDescriptionsAndTypes(rootProjectXmlRpcTracUri, sinceDate, Username, Password);
			actionOnMessage("Finished obtaining all ticket descriptions and types from Trac server.", FeedbackMessageTypes.Success);

			var tmpBugsFixed = new Dictionary<int, TracXmlRpcInterop.TracTicketDetails>();
			var tmpImprovements = new Dictionary<int, TracXmlRpcInterop.TracTicketDetails>();
			var tmpNewFeatures = new Dictionary<int, TracXmlRpcInterop.TracTicketDetails>();
			ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			{
				List<int> tmpKeysList = tmpIdsAndDescriptionsAndTicketTypes.Keys.ToList();
				Parallel.For(0, tmpKeysList.Count, (tmpIndex) =>
				{
					int ticketID = tmpKeysList[tmpIndex];
					actionOnMessage("Obtaining changelogs for ticket " + ticketID.ToString() + "...", FeedbackMessageTypes.Status);
					List<TracXmlRpcInterop.ChangeLogStruct> changelogs = TracXmlRpcInterop.GetChangeLogs(ticketID, rootProjectXmlRpcTracUri);

					/*//We have a closed ticket, now get all the fields for it
					var fieldVals = TracXmlRpcInterop.GetFieldValuesOfTicket(i, ProjectXmlRpcTracUri);
					if (fieldVals != null)
					{
						int continueHere;
					}*/

					List<string> ticketComments = new List<string>();

					foreach (TracXmlRpcInterop.ChangeLogStruct cl in changelogs)
						if (cl.Field == "comment" && !string.IsNullOrWhiteSpace(cl.NewValue))
							//This can be greatly improved
							if (tmpIdsAndDescriptionsAndTicketTypes[ticketID].TicketType == TracXmlRpcInterop.TicketTypeEnum.Bug)
								ticketComments.Add(/*"<e class='graycolor'>Ticket #" + i + ":</e> " + */ReplaceNewlinesWithString(cl.NewValue, " ")/* + " <e class='graycolor'>[" + tmpIdsAndDescriptionsAndTicketTypes[i].Description + "]</e>"*/);
							else if (tmpIdsAndDescriptionsAndTicketTypes[ticketID].TicketType == TracXmlRpcInterop.TicketTypeEnum.Improvement)
								ticketComments.Add(/*"<e class='graycolor'>Ticket #" + i + ":</e> " + */ReplaceNewlinesWithString(cl.NewValue, " ")/* + " <e class='graycolor'>[" + tmpIdsAndDescriptionsAndTicketTypes[i].Description + "]</e>"*/);
							else if (tmpIdsAndDescriptionsAndTicketTypes[ticketID].TicketType == TracXmlRpcInterop.TicketTypeEnum.NewFeature)
								ticketComments.Add(/*"<e class='graycolor'>Ticket #" + i + ":</e> " + */ReplaceNewlinesWithString(cl.NewValue, " ")/* + " <e class='graycolor'>[" + tmpIdsAndDescriptionsAndTicketTypes[i].Description + "]</e>"*/);
					//tmpList.Add("Ticket #" + i + ": '" + cl.Field + "' new value = " + cl.NewValue + ", old value = " + cl.OldValue);

					tmpIdsAndDescriptionsAndTicketTypes[ticketID].TicketComments = ticketComments;
					string actionCompletedDescription = "";
					switch (tmpIdsAndDescriptionsAndTicketTypes[ticketID].TicketType)
					{
						case TracXmlRpcInterop.TicketTypeEnum.Bug:
							actionCompletedDescription = "Bug fixed";
							break;
						case TracXmlRpcInterop.TicketTypeEnum.Improvement:
							actionCompletedDescription = "Improvement";
							break;
						case TracXmlRpcInterop.TicketTypeEnum.NewFeature:
							actionCompletedDescription = "New feature";
							break;
						default:
							actionCompletedDescription = "[UNKNOWN TICKET TYPE: " + tmpIdsAndDescriptionsAndTicketTypes[ticketID].TicketType + "]";
							break;
					}

					/*string finalString = 
						actionCompletedDescription + ": "
						+ "[" + ReplaceNewlinesWithString(tmpIdsAndDescriptionsAndTicketTypes[ticketID].Summary, " ") + "]"
						+ ReplaceNewlinesWithString(tmpIdsAndDescriptionsAndTicketTypes[ticketID].Description, " ") + "."
						+ (ticketComments.Count > 0 ? " Comments: " + "|||" + string.Join("|||", ticketComments) : "");*/
					switch (tmpIdsAndDescriptionsAndTicketTypes[ticketID].TicketType)
					{
						case TracXmlRpcInterop.TicketTypeEnum.Bug:
							tmpBugsFixed.Add(ticketID, tmpIdsAndDescriptionsAndTicketTypes[ticketID]);
							break;
						case TracXmlRpcInterop.TicketTypeEnum.Improvement:
							tmpImprovements.Add(ticketID, tmpIdsAndDescriptionsAndTicketTypes[ticketID]);
							break;
						case TracXmlRpcInterop.TicketTypeEnum.NewFeature:
							tmpNewFeatures.Add(ticketID, tmpIdsAndDescriptionsAndTicketTypes[ticketID]);
							break;
						default:
							break;
					}

					actionOnMessage("Finished obtaining changelogs for ticket " + ticketID.ToString() + ".", FeedbackMessageTypes.Success);
				});
			},
			true);
			return new TracXmlRpcInterop.ChangeLogs(rootProjectXmlRpcTracUri, tmpBugsFixed, tmpImprovements, tmpNewFeatures);
		}

		private static string GetAppIconFullPath(string projectName)
		{
			string tmperr;
			var tmpCsprojAndAppType = OwnAppsInterop.GetCsprojFullpathFromApplicationName(projectName, out tmperr);
			if (tmperr != null)
			{
				UserMessages.ShowWarningMessage(tmperr);
				return null;
			}
			string csprojPath = tmpCsprojAndAppType.Value.Key;
			return Path.Combine(Path.GetDirectoryName(csprojPath), "app.ico");
		}

		private static void UploadIconAsFaviconIfExists(string projectName)
		{
			string appIconPath = GetAppIconFullPath(projectName);
			if (File.Exists(appIconPath))
				QueueFileToUpload(projectName, "Favicon for " + projectName, appIconPath, "favicon.ico");
		}

		private static string GetHtmlLinkForTicket(int ticketID, TracXmlRpcInterop.TracTicketDetails ticketDetails, Func<int, string> functionToGetTicketUrl)
		{
			return
				string.Format("<li title='{0}'><a href='{1}' target='_blank' class='tracticketlink'>[Ticket {2}]</a> {3}</li>",
					HttpUtility.HtmlEncode(ticketDetails.Description),
					functionToGetTicketUrl(ticketID),
					ticketID,
					HttpUtility.HtmlEncode(ticketDetails.Summary));
		}

		[Obsolete("Not used anymore, the webpage is now stored online and dynamically populated", true)]
		public static string CreateHtmlPageReturnFilename(string projectName, string projectVersion, string setupFilename, TracXmlRpcInterop.ChangeLogs Changelogs, out List<string> listOfScreenshotsFullLocalPaths, PublishDetails publishDetails = null)
		{
			listOfScreenshotsFullLocalPaths = null;
			return null;

			/*string thisappTempFolder = Path.Combine(Path.GetTempPath(), "TempWebpagesBeforeUploading", projectName);
			if (!Directory.Exists(thisappTempFolder))
				Directory.CreateDirectory(thisappTempFolder);
			string tempFilename = Path.Combine(thisappTempFolder, "index.html");

			//string description = "";// "This is the description for " + projectName + ".";
			string bugsfixed = "";
			string improvements = "";
			string newfeatures = "";
			if (Changelogs.BugsFixed != null)
				foreach (int bugTicketID in Changelogs.BugsFixed.Keys)
					bugsfixed += GetHtmlLinkForTicket(bugTicketID, Changelogs.BugsFixed[bugTicketID], Changelogs.GetTicketUrl);
			if (Changelogs.Improvements != null)
				foreach (int improvementTicketID in Changelogs.Improvements.Keys)
					improvements += GetHtmlLinkForTicket(improvementTicketID, Changelogs.Improvements[improvementTicketID], Changelogs.GetTicketUrl);
			if (Changelogs.NewFeatures != null)
				foreach (int newfeatureTicketID in Changelogs.NewFeatures.Keys)
					newfeatures += GetHtmlLinkForTicket(newfeatureTicketID, Changelogs.NewFeatures[newfeatureTicketID], Changelogs.GetTicketUrl);

			//bool HtmlFileFound = false;

			string HtmlTemplateFileName = "VisualStudioInterop (publish page).html";
			byte[] bytesOfHtmlFile;
			if (!Helpers.GetEmbeddedResource_FirstOneEndingWith(HtmlTemplateFileName, out bytesOfHtmlFile))
			{
				UserMessages.ShowWarningMessage("Could not find Html file in resources: " + HtmlTemplateFileName);
				listOfScreenshotsFullLocalPaths = null;
				return null;
			}
			else
			{
				File.WriteAllBytes(tempFilename, bytesOfHtmlFile);
				string textOfFile = File.ReadAllText(tempFilename);

				string appIconPath = GetAppIconFullPath(projectName);
				if (appIconPath != null)
					textOfFile = textOfFile.Replace("{ShortcutIconIfExists}", "<link rel='SHORTCUT ICON' href='favicon.ico'/>");
				textOfFile = textOfFile.Replace("{WebpageTitle}", projectName.InsertSpacesBeforeCamelCase());

				textOfFile = textOfFile.Replace("{PageGeneratedDate}", DateTime.Now.ToString(@"dddd, dd MMMM yyyy \a\t HH:mm:ss"));
				textOfFile = textOfFile.Replace("{ProjectName}", projectName.InsertSpacesBeforeCamelCase());
				textOfFile = textOfFile.Replace("{ProjectNameSmallCaps}", projectName.ToLower());
				textOfFile = textOfFile.Replace("{ProjectVersion}", projectVersion);
				//textOfFile = textOfFile.Replace("{SetupFilename}", Path.GetFileName(setupFilename));
				textOfFile = textOfFile.Replace("{SetupFilename}", "/downloadownapps.php?relativepath=" + projectName + "/" + Path.GetFileName(setupFilename));
				//textOfFile = textOfFile.Replace("{DescriptionLiElements}", description);
				//if (!string.IsNullOrWhiteSpace(bugsfixed)
				//    || !string.IsNullOrWhiteSpace(improvements)
				//    || !string.IsNullOrWhiteSpace(newfeatures))
				//{
				//textOfFile = textOfFile.Replace("{ChangelistCssDisplay}", "block");
				textOfFile = textOfFile.Replace("{BugsFixedList}", Changelogs.BugsFixed.Count == 0 ? "None." : bugsfixed);
				textOfFile = textOfFile.Replace("{ImprovementList}", Changelogs.Improvements.Count == 0 ? "None." : improvements);
				textOfFile = textOfFile.Replace("{NewFeaturesList}", Changelogs.NewFeatures.Count == 0 ? "None." : newfeatures);
				//}
				//else
				//{
				//    //textOfFile = textOfFile.Replace("{ChangelistCssDisplay}", "none");
				//    textOfFile = textOfFile.Replace("{BugsFixedList}", "");//So the {..} does not remain
				//    textOfFile = textOfFile.Replace("{ImprovementList}", "There are no changes recorded for this release.");
				//    textOfFile = textOfFile.Replace("{NewFeaturesList}", "");//So the {..} does not remain
				//}

				if (publishDetails != null)
				{
					textOfFile = textOfFile.Replace("{DownloadSize}", BytesToHumanfriendlyStringConverter.ConvertBytesToHumanreadableString(publishDetails.SetupSize));
					textOfFile = textOfFile.Replace("{PublishedDate}", publishDetails.PublishedDate.ToString("yyyy-MM-dd"));
					textOfFile = textOfFile.Replace("{JsonText}", publishDetails.GetJsonString());
				}
				else
					textOfFile = textOfFile.Replace("{JsonText}", "Could not obtain extra info from online database.");

				textOfFile = InsertScreenshotsIntoHtmlAndReturnScreenshotsFullpath(projectName, textOfFile, out listOfScreenshotsFullLocalPaths);

				File.WriteAllText(tempFilename, textOfFile);
			}

			return tempFilename;*/
		}
	}
}