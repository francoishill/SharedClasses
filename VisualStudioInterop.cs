using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Net;
using System.Web;
using CookComputing.XmlRpc;
using SharedClasses;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

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

public class VisualStudioInterop
{
	public static readonly string cProjectsRootDir = @"C:\Francois\Dev\VSprojects";

	private static bool FindMsbuildPath4(out string msbuildpathOrerror)
	{
		msbuildpathOrerror = "";
		string rootfolder = @"C:\Windows\Microsoft.NET\Framework64";
		if (!Directory.Exists(rootfolder)) msbuildpathOrerror = "Dir not found for msbuild: " + rootfolder;
		else
		{
			//Should eventually make this code to look for newest version (not only 4)
			//string newestdir = "";
			foreach (string dir in Directory.GetDirectories(rootfolder))
				if (dir.Split('\\')[dir.Split('\\').Length - 1].ToLower().StartsWith("v4.") && dir.Split('\\')[dir.Split('\\').Length - 1].Contains('.'))
				{
					msbuildpathOrerror = dir;
					return true;
				}
			msbuildpathOrerror = "Could not find folder v4... in directory " + rootfolder;
		}
		return false;
	}

	//private static string GetPlatformTargetString(PlatformTarget platformTarget)
	//{
	//	switch (platformTarget)
	//	{
	//		case PlatformTarget.x86:
	//			return "x86";
	//		case PlatformTarget.x64:
	//			return "x64";
	//		case PlatformTarget.AnyCPU:
	//			return "Any CPU";
	//		default:
	//			return "Any CPU";
	//	}
	//}

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

	public enum BuildType { Rebuild, Build };
	public enum ProjectConfiguration { Debug, Release };
	public enum PlatformTarget { x86, x64, AnyCPU };
	public static bool BuildVsProjectReturnNewversionString(string projName, string csprojFilename, string slnFilename, bool SolutionTrueProjectFalse, BuildType buildType, ProjectConfiguration configuration, PlatformTarget platformTarget, bool AutomaticallyUpdateRevision, Object textfeedbackSenderObject, TextFeedbackEventHandler textFeedbackEvent, out string newversionString, out string currentversionString)
	{
		bool errorOccurred = false;
		ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
		{
			var proj = new VSBuildProject_NonAbstract(projName, SolutionTrueProjectFalse ? slnFilename : csprojFilename);
			List<string> csprojpaths;
			bool buildSuccess = proj.PerformBuild(
				(ms, msgtype) =>
				{
					TextFeedbackEventArgs.RaiseSimple(textFeedbackEvent, ms,
						msgtype == FeedbackMessageTypes.Error ? TextFeedbackType.Error
						: msgtype == FeedbackMessageTypes.Status ? TextFeedbackType.Subtle
						: msgtype == FeedbackMessageTypes.Success ? TextFeedbackType.Subtle
						: msgtype == FeedbackMessageTypes.Warning ? TextFeedbackType.Noteworthy
						: TextFeedbackType.Subtle);
				},
				out csprojpaths);
			if (buildSuccess)
				TextFeedbackEventArgs.RaiseSimple(textFeedbackEvent,
					"Successfully built project " + projName, TextFeedbackType.Success);
			errorOccurred = !buildSuccess;
		},
		true);

		if (!errorOccurred)
		{
			string errifNull;
			string outNewVersion;
			string outCurrentVersion;
			if (!GetNewVersionStringFromAssemblyFile(csprojFilename, AutomaticallyUpdateRevision, true, out errifNull, out outNewVersion, out outCurrentVersion))
			{
				TextFeedbackEventArgs.RaiseSimple(textFeedbackEvent, errifNull, TextFeedbackType.Error);
				newversionString = null;
				currentversionString = null;
				return false;
			}
			newversionString = outNewVersion;
			currentversionString = outCurrentVersion;
			return true;
		}
		newversionString = null;
		currentversionString = null;
		return false;
	}

	public static string InsertSpacesBeforeCamelCase(string s)
	{
		if (s == null) return s;
		for (int i = s.Length - 1; i >= 1; i--)
		{
			if (s[i].ToString().ToUpper() == s[i].ToString())
				s = s.Insert(i, " ");
		}
		return s;
	}

	//DONE: Start building own publishing platform (FTP, the html page, etc)
	[Obsolete("Start using PublishInterop.PerformPublish()", true)]
	public static string PerformPublish(Object textfeedbackSenderObject, string projName, bool _64Only, out string publishedVersionString, bool HasPlugins, bool AutomaticallyUpdateRevision = false, bool InstallLocallyAfterSuccessfullNSIS = true, bool WriteIntoRegistryForWindowsAutostartup = true, TextFeedbackEventHandler textFeedbackEvent = null, bool SelectSetupIfSuccessful = false)
	{
		publishedVersionString = "";
		string projDir =
                    Directory.Exists(projName) ? projName :
						Path.Combine(cProjectsRootDir, projName);//Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Visual Studio 2010\Projects\" + projName;

		if (Directory.Exists(projName))
			projName = Path.GetFileNameWithoutExtension(projName);

		while (projDir.EndsWith("\\")) projDir = projDir.Substring(0, projDir.Length - 1);
		string projFolderName = projDir.Split('\\')[projDir.Split('\\').Length - 1];
		string csprojFileName = projDir + "\\" + projFolderName + ".csproj";
		string slnFileName = projDir + "\\" + projFolderName + ".sln";

		bool SolutionFileFound = false;
		if (File.Exists(slnFileName)) SolutionFileFound = true;

		bool ProjFileFound = false;
		if (File.Exists(csprojFileName)) ProjFileFound = true;
		else
		{
			csprojFileName = projDir + "\\" + projFolderName + "\\" + projFolderName + ".csproj";
			if (File.Exists(csprojFileName)) ProjFileFound = true;
		}

		if (!SolutionFileFound) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Could not find solution file (sln) in dir " + projDir);
		else if (!ProjFileFound) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Could not find project file (csproj) in dir " + projDir);
		else
		{
			TextFeedbackEventArgs.RaiseSimple(textFeedbackEvent,
				"Attempting to build project " + projName);

			string outNewVersionString;
			string outCurrentversionString;
			if (!BuildVsProjectReturnNewversionString(
					projName,
					csprojFileName,
					slnFileName,
					true,
					BuildType.Rebuild,
					ProjectConfiguration.Release,
					PlatformTarget.AnyCPU,//.x86,//.x64,
					AutomaticallyUpdateRevision,
					textfeedbackSenderObject,
					textFeedbackEvent,
					out outNewVersionString,
					out outCurrentversionString))
			{
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Could not obtain version string for project " + projName);
				return null;
			}
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(
					textfeedbackSenderObject,
					textFeedbackEvent,
					AutomaticallyUpdateRevision
					? "Project " + projName + " will be published as version " + outCurrentversionString + " but sourcecode updated to version " + outNewVersionString + ", attempting to publish..."
					: "Using current revision of " + projName + " (" + outCurrentversionString + "), attempting to publish...");

			publishedVersionString = outCurrentversionString;//

			string nsisFileName = WindowsInterop.LocalAppDataPath + @"\FJH\NSISinstaller\NSISexports\" + projName + "_" + outCurrentversionString + ".nsi";
			string resultSetupFileName = Path.GetDirectoryName(nsisFileName) + "\\" + NsisInterop.GetSetupNameForProduct(InsertSpacesBeforeCamelCase(projName), outCurrentversionString);
			bool successfullyCreatedNSISfile = false;
			ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			{
				if (!Directory.Exists(WindowsInterop.LocalAppDataPath + @"\FJH\NSISinstaller\NSISexports")) Directory.CreateDirectory(WindowsInterop.LocalAppDataPath + @"\FJH\NSISinstaller\NSISexports");
				using (StreamWriter sw2 = new StreamWriter(WindowsInterop.LocalAppDataPath + @"\FJH\NSISinstaller\NSISexports\DotNetChecker.nsh"))
				{
					sw2.Write(NsisInterop.DotNetChecker_NSH_file);
				}
				using (StreamWriter sw1 = new StreamWriter(nsisFileName))
				{
					string registryEntriesFilename = "RegistryEntries.json";
					string registryEntriesFilepath = Path.Combine(Path.GetDirectoryName(csprojFileName), "Properties", registryEntriesFilename);

					//DONE: This is awesome, after installing with NSIS you can type appname in RUN and it will open
					List<string> list = NsisInterop.CreateOwnappNsis(
						projName,
						InsertSpacesBeforeCamelCase(projName),
						outCurrentversionString,//Should obtain (and increase) product version from csproj file
						"http://fjh.dyndns.org/ownapplications/" + projName.ToLower(),
						projName + ".exe",
						RegistryInterop.GetRegistryAssociationItemFromJsonFile(registryEntriesFilepath,
							(mess, msgtype) =>
							{
								TextFeedbackType tft = TextFeedbackType.Subtle;
								switch (msgtype)
								{
									case FeedbackMessageTypes.Success: tft = TextFeedbackType.Success; break;
									case FeedbackMessageTypes.Error: tft = TextFeedbackType.Error; break;
									case FeedbackMessageTypes.Warning: tft = TextFeedbackType.Noteworthy; break;
									case FeedbackMessageTypes.Status: tft = TextFeedbackType.Subtle; break;
								}
								TextFeedbackEventArgs.RaiseSimple(textFeedbackEvent, mess, tft);
							}),
						null,
						true,
						NsisInterop.NSISclass.DotnetFrameworkTargetedEnum.DotNet4client,
						_64Only,
						WriteIntoRegistryForWindowsAutostartup,
						HasPlugins);
					foreach (string line in list)
						sw1.WriteLine(line);

					string startMsg = "Successfully created NSIS file: ";
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent,
							startMsg + nsisFileName,
							HyperlinkRangeIn: new Range(startMsg.Length, nsisFileName.Length, Range.LinkTypes.ExplorerSelect));
				}

				//DONE: Must make provision if pc (to do building and compiling of NSIS scripts), does not have the DotNetChecker.dll plugin for NSIS
				//bool DotNetCheckerDllFileFound = false;
				//string DotNetCheckerFilenameEndswith = "DotNetChecker.dll";
				//string dotnetCheckerDllPath = @"C:\Program Files (x86)\NSIS\Plugins\" + DotNetCheckerFilenameEndswith;

				string nsisDir = NsisInterop.GetNsisInstallDirectory();
				string dotnetCheckerDllPath = Path.Combine(nsisDir, "Plugins", "dotnetchecker.dll");

				if (!File.Exists(dotnetCheckerDllPath))
				{
					string downloadededPath = NetworkInterop.FtpDownloadFile(
							null,
							Path.GetDirectoryName(dotnetCheckerDllPath),
							SettingsSimple.OnlineAppsSettings.Instance.AppsDownloadFtpUsername,//GlobalSettings.VisualStudioInteropSettings.Instance.FtpUsername,
							SettingsSimple.OnlineAppsSettings.Instance.AppsDownloadFtpPassword,//GlobalSettings.VisualStudioInteropSettings.Instance.FtpPassword,
							SettingsSimple.PublishSettings.Instance.OnlineDotnetCheckerDllFileUrl,
							err => TextFeedbackEventArgs.RaiseSimple(textFeedbackEvent, err),
							textFeedbackEvent);
					if (downloadededPath == null)
						UserMessages.ShowWarningMessage("Could not find (or download) DotNetChecker.dll from URL: " + SettingsSimple.PublishSettings.Instance.OnlineDotnetCheckerDllFileUrl);
					else
						dotnetCheckerDllPath = downloadededPath;
				}
				//if (!GetEmbeddedResource_FirstOneEndingWith(DotNetCheckerFilenameEndswith, dotnetCheckerDllPath))
				//    UserMessages.ShowWarningMessage("Could not find " + DotNetCheckerFilenameEndswith + " in resources");

				string MakeNsisFilePath = @"C:\Program Files (x86)\NSIS\makensis.exe";
				if (!File.Exists(MakeNsisFilePath)) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Could not find MakeNsis.exe: " + MakeNsisFilePath);
				else
				{
					if (File.Exists(resultSetupFileName))
						File.Delete(resultSetupFileName);
					Process nsisCompileProc = Process.Start(MakeNsisFilePath, "\"" + nsisFileName + "\"");
					nsisCompileProc.WaitForExit();

					if (File.Exists(resultSetupFileName))
					{
						if (InstallLocallyAfterSuccessfullNSIS || SelectSetupIfSuccessful)
						{
							TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Publish success, opening folder and/or running setup file...");
							if (SelectSetupIfSuccessful)
								Process.Start("explorer", "/select, \"" + resultSetupFileName + "\"");
							if (InstallLocallyAfterSuccessfullNSIS)
							{
								Process curproc = Process.GetCurrentProcess();
								bool DoNotKillProcessAndInstall = projName.Equals(curproc.ProcessName, StringComparison.InvariantCultureIgnoreCase);
								if (projName.Equals("StandaloneUploader", StringComparison.InvariantCultureIgnoreCase))
									DoNotKillProcessAndInstall = true;

								if (!DoNotKillProcessAndInstall)
								{
									//Kill process if open
									Process[] openProcs = Process.GetProcessesByName(projName);
									if (openProcs.Length > 1)
									{
										if (UserMessages.Confirm("There are " + openProcs.Length + " processes with the name '" + projName + "', manually close the correct one or click yes to close all?"))
										{
											TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Killing all open processes named " + projName);
											foreach (Process proc in openProcs)
												proc.Kill();
										}
									}
									else if (openProcs.Length == 1)
									{
										TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Killing open process named {0}".Fmt(projName));
										openProcs[0].Kill();
									}
								}

								if (DoNotKillProcessAndInstall)
								{
									TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Launching setup for '{0}', not running silently because same application name as current.".Fmt(projName));
									Process.Start(resultSetupFileName);
								}
								else
								{
									TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Installing '{0}' silently.".Fmt(projName));
									var setupProc = Process.Start(resultSetupFileName, "/S");
									setupProc.WaitForExit();
									TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Launching '{0}'.".Fmt(projName));
									try { Process.Start(projName + ".exe"); }
									catch (Exception exc) { TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Error launching '{0}': {1}".Fmt(projName, exc.Message), TextFeedbackType.Error); }
								}
							}
						}
						successfullyCreatedNSISfile = true;
					}
					else TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Could not successfully create setup for " + projName);
					//else Process.Start("explorer", "/select, \"" + Path.GetDirectoryName(nsisFileName) + "\"");
				}
			});
			if (successfullyCreatedNSISfile)
				return resultSetupFileName;
		}
		return null;
	}

	private static string SurroundWithHtmlTag(string textToSurround, string tagName, string className = null)
	{
		return string.Format("<{0}{2}>{1}</{0}>", tagName, textToSurround, className == null ? "" : " class='" + className + "'");
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
		if (!GetEmbeddedResource_FirstOneEndingWith(HtmlTemplateFileName, tempFilename))
			UserMessages.ShowWarningMessage("Could not find Html file in resources: " + HtmlTemplateFileName);
		else
		{
			string textOfFile = File.ReadAllText(tempFilename);
			textOfFile = textOfFile.Replace("{PageGeneratedDate}", DateTime.Now.ToString(@"dddd, dd MMMM yyyy \a\t HH:mm:ss"));
			textOfFile = textOfFile.Replace("{ProjectName}", projectName);
			textOfFile = textOfFile.Replace("{ProjectVersion}", projectVersion);
			textOfFile = textOfFile.Replace("{SetupFilename}", Path.GetFileName(setupFilename));
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

	[Obsolete("Start using PublishInterop.PerformPublishOnline()", true)]
	public static void PerformPublishOnline(Object textfeedbackSenderObject, string projName, bool _64Only, bool HasPlugins, bool AutomaticallyUpdateRevision = false, bool WriteIntoRegistryForWindowsAutostartup = true, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChanged = null, bool OpenSetupFileAfterSuccessfullNSIS = true, bool OpenFolderAfterSuccessfullNSIS = false, bool OpenWebsite = true)
	{
		string publishedVersionString = null;
		string publishedSetupPath = null;

		List<string> BugsFixed = null;
		List<string> Improvements = null;
		List<string> NewFeatures = null;

		//this (ServicePointManager.DefaultConnectionLimit) is actually very annoying, is there no other workaround?
		ServicePointManager.DefaultConnectionLimit = 10000;

		//ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
		//{
		//	Parallel.Invoke(
		//		() =>
		//		{
		publishedSetupPath = PerformPublish(textfeedbackSenderObject, projName, _64Only, out publishedVersionString, HasPlugins, AutomaticallyUpdateRevision, OpenSetupFileAfterSuccessfullNSIS, WriteIntoRegistryForWindowsAutostartup, textFeedbackEvent, OpenFolderAfterSuccessfullNSIS);
		//		},
		//		() =>
		//		{
		//			//System.Net.ServicePointManager.DefaultConnectionLimit = 1;
		//			//#pragma warning disable
		//			//DONE: There is no proper items for bugs fixed etc in next function
		//			//bool ThereIsNoProperItemsForBugsFixedEtcInNextFunction;
		//			//#pragma warning restore

		//			//When pulling buglist/improvement etc from Trac repository for a project, must also check in the project's .csproj file whether it uses/references SharedClasses. Then it must also pull the changes from the SharedClasses Trac repository.
		//			GetChangeLogs(textfeedbackSenderObject, projName, out BugsFixed, out Improvements, out NewFeatures,
		//				textFeedbackEvent: textFeedbackEvent);
		//		});
		//});

		if (publishedSetupPath != null)
		{
			//ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			//{
			string validatedUrlsectionForProjname = HttpUtility.UrlPathEncode(projName).ToLower();

			string rootFtpUri = GlobalSettings.VisualStudioInteropSettings.Instance.GetCombinedUriForVsPublishing() + "/" + validatedUrlsectionForProjname;
			PublishDetails publishDetails = new PublishDetails(
					projName,
					publishedVersionString,
					new FileInfo(publishedSetupPath).Length,
					publishedSetupPath.FileToMD5Hash(),
					DateTime.Now,
					rootFtpUri + "/" + (new FileInfo(publishedSetupPath).Name));
			string errorStringIfFailElseJsonString;
			if (!WebInterop.SaveObjectOnline(PublishDetails.OnlineJsonCategory, projName + " - " + publishedVersionString, publishDetails, out errorStringIfFailElseJsonString))
			{
				TextFeedbackEventArgs.RaiseSimple(textFeedbackEvent, "Cannot save json online (" + projName + " - " + publishedVersionString + "), setup and index.html cancelled for project " + projName + ": " + errorStringIfFailElseJsonString, TextFeedbackType.Error);
				return;
			}
			if (!WebInterop.SaveObjectOnline(PublishDetails.OnlineJsonCategory, projName + PublishDetails.LastestVersionJsonNamePostfix, publishDetails, out errorStringIfFailElseJsonString))
			{
				TextFeedbackEventArgs.RaiseSimple(textFeedbackEvent, "Cannot save json online (" + projName + PublishDetails.LastestVersionJsonNamePostfix + "), setup and index.html cancelled for project " + projName + ": " + errorStringIfFailElseJsonString, TextFeedbackType.Error);
				return;
			}

			string htmlFilePath = CreateHtmlPageReturnFilename(
					projName,
					publishedVersionString,
					publishedSetupPath,
					BugsFixed,
					Improvements,
					NewFeatures,
					publishDetails);

			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent,
					"Attempting Ftp Uploading of Setup file and index file for " + projName);
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
					(err) => TextFeedbackEventArgs.RaiseSimple(textFeedbackEvent, err),
					dispname,
					UploadingProtocolTypes.Ownapps,
					localfilepath,
					/*rootFtpUri.TrimEnd('/') */validatedUrlsectionForProjname + "/" + Path.GetFileName(localfilepath),
					GlobalSettings.VisualStudioInteropSettings.Instance.FtpUsername,
					GlobalSettings.VisualStudioInteropSettings.Instance.FtpPassword,
					true))
					uploadsQueued = false;
			}

			if (uploadsQueued)
				TextFeedbackEventArgs.RaiseSimple(textFeedbackEvent, "All uploads successfully queued with StandaloneUploader", TextFeedbackType.Success);
			else
				TextFeedbackEventArgs.RaiseSimple(textFeedbackEvent, "Unable to queue all files with StandaloneUploader", TextFeedbackType.Error);

			/*if (!NetworkInterop.FtpUploadFiles(
				//Task uploadTask = NetworkInterop.FtpUploadFiles(
					textfeedbackSenderObject,
					rootFtpUri,
					GlobalSettings.VisualStudioInteropSettings.Instance.FtpUsername,//NetworkInterop.ftpUsername,
					GlobalSettings.VisualStudioInteropSettings.Instance.FtpPassword,//NetworkInterop.ftpPassword,
					localFiles.ToArray(),
					err => TextFeedbackEventArgs.RaiseSimple(textFeedbackEvent, err),
					OpenWebsite ? uriAfterUploading : null,
					textFeedbackEvent: textFeedbackEvent,
					progressChanged: progressChanged))
				uploaded = false;

			if (uploaded)
			{
				string startMsg = "All files uploaded, website is: ";
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent,
						startMsg + uriAfterUploading,
						HyperlinkRangeIn: new Range(startMsg.Length, uriAfterUploading.Length, Range.LinkTypes.OpenUrl));
			}
			else
				TextFeedbackEventArgs.RaiseSimple(textFeedbackEvent, textfeedbackSenderObject, "An error occurred, could not complete uploading of published files.", TextFeedbackType.Error);*/

			//uploadTask.Start();
			//uploadTask.Wait();
			//},
			//true);
		}
	}

	public static string GetTracXmlRpcHttpPathFromProjectName(string projectName)
	{
		foreach (string tmpuri in GlobalSettings.TracXmlRpcInteropSettings.Instance.GetListedXmlRpcUrls())//GlobalSettings.TracXmlRpcInteropSettings.Instance.ListedApplicationNames.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
			if (tmpuri.ToLower().Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Contains(projectName.ToLower()))
				return tmpuri;
		return null;
	}

	//DONE: Continue with implementing this XmlRpc of Trac into the projects that uses Trac
	public static void GetChangeLogs(object textfeedbackSenderObject, string ProjectName, out List<string> BugsFixed, out List<string> Improvements, out List<string> NewFeatures, string Username = null, string Password = null, TextFeedbackEventHandler textFeedbackEvent = null)
	{
		BugsFixed = new List<string>();
		Improvements = new List<string>();
		NewFeatures = new List<string>();
		string ProjectXmlRpcTracUri = GetTracXmlRpcHttpPathFromProjectName(ProjectName);
		if (string.IsNullOrWhiteSpace(ProjectXmlRpcTracUri))
		{
			//BugsFixed = Improvements = NewFeatures = new List<string>() { "No trac xmlrpc url specified for project " + ProjectName + ", no bugs found." };
			BugsFixed = new List<string>() { "No trac xmlrpc url specified for project " + ProjectName + ", no bugs found." };
			return;//return new List<string>() { "No trac xmlrpc url specified for project " + ProjectName + ", no bugs found." };
		}

		TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Obtaining all ticket descriptions and types from Trac server...", TextFeedbackType.Subtle);
		Dictionary<int, TracXmlRpcInterop.DescriptionAndTicketType> tmpIdsAndDescriptionsAndTicketTypes
            = TracXmlRpcInterop.GetAllTicketDescriptionsAndTypes(ProjectXmlRpcTracUri, Username, Password);
		TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Finished obtaining all ticket descriptions and types from Trac server.", TextFeedbackType.Subtle);

		//List<string> tmpList = new List<string>();
		//int[] ids = TracXmlRpcInterop.GetTicketIds(ProjectXmlRpcTracUri, Username, Password);

		List<string> tmpBugsFixed = new List<string>();
		List<string> tmpImprovements = new List<string>();
		List<string> tmpNewFeatures = new List<string>();
		ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
		{
			List<int> tmpKeysList = tmpIdsAndDescriptionsAndTicketTypes.Keys.ToList();
			Parallel.For(0, tmpKeysList.Count, (tmpIndex) =>
			{
				int i = tmpKeysList[tmpIndex];
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Obtaining changelogs for ticket " + i.ToString() + "...", TextFeedbackType.Subtle);
				List<TracXmlRpcInterop.ChangeLogStruct> changelogs = TracXmlRpcInterop.ChangeLogs(i, ProjectXmlRpcTracUri);
				foreach (TracXmlRpcInterop.ChangeLogStruct cl in changelogs)
					if (cl.Field == "comment" && !string.IsNullOrWhiteSpace(cl.NewValue))
						//This can be greatly improved
						if (tmpIdsAndDescriptionsAndTicketTypes[i].TicketType == TracXmlRpcInterop.TicketTypeEnum.Bug)
							tmpBugsFixed.Add("<e class='graycolor'>Ticket #" + i + ":</e> " + cl.NewValue + " <e class='graycolor'>[" + tmpIdsAndDescriptionsAndTicketTypes[i].Description + "]</e>");
						else if (tmpIdsAndDescriptionsAndTicketTypes[i].TicketType == TracXmlRpcInterop.TicketTypeEnum.Improvement)
							tmpImprovements.Add("<e class='graycolor'>Ticket #" + i + ":</e> " + cl.NewValue + " <e class='graycolor'>[" + tmpIdsAndDescriptionsAndTicketTypes[i].Description + "]</e>");
						else if (tmpIdsAndDescriptionsAndTicketTypes[i].TicketType == TracXmlRpcInterop.TicketTypeEnum.NewFeature)
							tmpNewFeatures.Add("<e class='graycolor'>Ticket #" + i + ":</e> " + cl.NewValue + " <e class='graycolor'>[" + tmpIdsAndDescriptionsAndTicketTypes[i].Description + "]</e>");
				//tmpList.Add("Ticket #" + i + ": '" + cl.Field + "' new value = " + cl.NewValue + ", old value = " + cl.OldValue);
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Finished obtaining changelogs for ticket " + i.ToString() + ".", TextFeedbackType.Subtle);
			});
		});
		BugsFixed = tmpBugsFixed;
		Improvements = tmpImprovements;
		NewFeatures = tmpNewFeatures;
		tmpBugsFixed = null;
		tmpImprovements = null;
		tmpNewFeatures = null;

		/*foreach (int i in tmpIdsAndDescriptionsAndTicketTypes.Keys)
		{
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Obtaining changelogs for ticket " + i.ToString() + "...", TextFeedbackType.Subtle);
				List<TracXmlRpcInterop.ChangeLogStruct> changelogs = TracXmlRpcInterop.ChangeLogs(i, ProjectXmlRpcTracUri);
				foreach (TracXmlRpcInterop.ChangeLogStruct cl in changelogs)
						if (cl.Field == "comment" && !string.IsNullOrWhiteSpace(cl.NewValue))
								//This can be greatly improved
								if (tmpIdsAndDescriptionsAndTicketTypes[i].TicketType == TracXmlRpcInterop.TicketTypeEnum.Bug)
										BugsFixed.Add("Ticket #" + i + ": " + cl.NewValue + "  (" + tmpIdsAndDescriptionsAndTicketTypes[i].Description + ")");
								else if (tmpIdsAndDescriptionsAndTicketTypes[i].TicketType == TracXmlRpcInterop.TicketTypeEnum.Improvement)
										Improvements.Add("Ticket #" + i + ": " + cl.NewValue + "  (" + tmpIdsAndDescriptionsAndTicketTypes[i].Description + ")");
								else if (tmpIdsAndDescriptionsAndTicketTypes[i].TicketType == TracXmlRpcInterop.TicketTypeEnum.NewFeature)
										NewFeatures.Add("Ticket #" + i + ": " + cl.NewValue + "  (" + tmpIdsAndDescriptionsAndTicketTypes[i].Description + ")");
				//tmpList.Add("Ticket #" + i + ": '" + cl.Field + "' new value = " + cl.NewValue + ", old value = " + cl.OldValue);
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Finished obtaining changelogs for ticket " + i.ToString() + ".", TextFeedbackType.Subtle);
		}*/
	}

	public static List<string> GetAllEmbeddedResourcesReturnFilePaths(Predicate<string> predicateToValidateOn, bool ShowErrorIfNoMatched = true)
	{
		List<string> tmplist = new List<string>();

		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			//Assembly objAssembly = Assembly.GetExecutingAssembly();
			try
			{
				string[] myResources = assembly.GetManifestResourceNames();
				foreach (string reso in myResources)
					if (predicateToValidateOn(reso))
					{
						Stream stream = assembly.GetManifestResourceStream(reso);
						int length = (int)stream.Length;
						byte[] bytesOfDotnetCheckerDLL = new byte[length];
						stream.Read(bytesOfDotnetCheckerDLL, 0, length);
						stream.Close();
						string tmpFilePath = Path.GetTempPath() + Path.GetFileName(reso);
						while (tmplist.Contains(tmpFilePath, StringComparer.InvariantCultureIgnoreCase))
							tmpFilePath = Path.GetDirectoryName(tmpFilePath) + "\\" + Path.GetFileNameWithoutExtension(tmpFilePath) + "_" + Path.GetExtension(tmpFilePath);
						FileStream fileStream = new FileStream(tmpFilePath, FileMode.Create);
						fileStream.Write(bytesOfDotnetCheckerDLL, 0, length);
						fileStream.Close();
						tmplist.Add(tmpFilePath);

						bytesOfDotnetCheckerDLL = null;
					}
			}
			catch { }
		}
		if (ShowErrorIfNoMatched && tmplist.Count == 0)
		{
			string callStack = "";
			StackTrace stackTrace = new StackTrace();           // get call stack
			StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)
			foreach (StackFrame stackFrame in stackFrames)
				callStack += Environment.NewLine + stackFrame.GetMethod().Name;   // write method name
			UserMessages.ShowWarningMessage("Could not find resource name. Call stack: " + callStack);
		}
		return tmplist;
	}

	public static bool GetEmbeddedResource(Predicate<string> predicateToValidateOn, string FileSaveLocation)
	{
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			//Assembly objAssembly = Assembly.GetExecutingAssembly();
			try
			{
				string[] myResources = assembly.GetManifestResourceNames();
				foreach (string reso in myResources)
					if (predicateToValidateOn(reso))
					{
						Stream stream = assembly.GetManifestResourceStream(reso);
						int length = (int)stream.Length;
						byte[] bytesOfDotnetCheckerDLL = new byte[length];
						stream.Read(bytesOfDotnetCheckerDLL, 0, length);
						stream.Close();
						FileStream fileStream = new FileStream(FileSaveLocation, FileMode.Create);
						fileStream.Write(bytesOfDotnetCheckerDLL, 0, length);
						fileStream.Close();
						bytesOfDotnetCheckerDLL = null;
						return true;
					}
			}
			catch { }
		}
		return false;
	}

	public static bool GetEmbeddedResource_FirstOneEndingWith(string EndOfFilename, string FileSaveLocation)
	{
		return GetEmbeddedResource(reso => reso.ToLower().EndsWith(EndOfFilename.ToLower()), FileSaveLocation);
	}
}