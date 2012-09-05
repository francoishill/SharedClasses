using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SharedClasses
{
	public static class PublishInterop
	{
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

		public static string PerformPublish(string projName, bool _64Only, bool HasPlugins, bool AutomaticallyUpdateRevision, bool InstallLocallyAfterSuccessfullNSIS, bool WriteIntoRegistryForWindowsAutostartup, bool SelectSetupIfSuccessful, out string publishedVersionString, Action<string> actionOnError, Action<string> actionOnStatus, Action<int> actionOnProgressPercentage)
		{
			if (!Directory.Exists(cProjectsRootDir) && !Directory.Exists(projName) && !File.Exists(projName))
			{
				actionOnError("Cannot find root project directory: " + cProjectsRootDir);
				publishedVersionString = null;
				return null;
			}

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

			if (!SolutionFileFound) actionOnError("Could not find solution file (sln) in dir " + projDir);
			else if (!ProjFileFound) actionOnError("Could not find project file (csproj) in dir " + projDir);
			else
			{
				actionOnStatus("Attempting to build project " + projName);

				//string outNewVersionString;
				//string outCurrentversionString;
				var projToBuild = new VSBuildProject_NonAbstract(projName);
				string errorIfNotNull;
				if (!projToBuild.PerformBuild(out errorIfNotNull))
				{
					actionOnError(errorIfNotNull);
					return null;
				}

				int uncommentFollowing;
				/*string errifNull;
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

				actionOnStatus(
						AutomaticallyUpdateRevision
						? "Project " + projName + " will be published as version " + outCurrentversionString + " but sourcecode updated to version " + outNewVersionString + ", attempting to publish..."
						: "Using current revision of " + projName + " (" + outCurrentversionString + "), attempting to publish...");

				publishedVersionString = outCurrentversionString;//

				string localAppDatapath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

				string nsisFileName = Path.Combine(localAppDatapath, @"FJH\NSISinstaller\NSISexports\" + projName + "_" + outCurrentversionString + ".nsi");
				string resultSetupFileName = Path.GetDirectoryName(nsisFileName) + "\\" + NsisInterop.GetSetupNameForProduct(projName.InsertSpacesBeforeCamelCase(), outCurrentversionString);
				bool successfullyCreatedNSISfile = false;
				ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				{
					if (!Directory.Exists(Path.Combine(localAppDatapath, @"FJH\NSISinstaller\NSISexports")))
						Directory.CreateDirectory(Path.Combine(localAppDatapath, @"FJH\NSISinstaller\NSISexports"));
					using (StreamWriter sw2 = new StreamWriter(Path.Combine(localAppDatapath, @"FJH\NSISinstaller\NSISexports\DotNetChecker.nsh")))
					{
						sw2.Write(NsisInterop.DotNetChecker_NSH_file);
					}
					using (StreamWriter sw1 = new StreamWriter(nsisFileName))
					{
						string registryEntriesFilename = "RegistryEntries.json";
						string registryEntriesFilepath = Path.Combine(Path.GetDirectoryName(csprojFileName), "Properties", registryEntriesFilename);

						//TODO: This is awesome, after installing with NSIS you can type appname in RUN and it will open
						List<string> list = NsisInterop.CreateOwnappNsis(
							projName,
							projName.InsertSpacesBeforeCamelCase(),
							outCurrentversionString,//Should obtain (and increase) product version from csproj file
							"http://fjh.dyndns.org/ownapplications/" + projName.ToLower(),
							projName + ".exe",
							RegistryInterop.GetRegistryAssociationItemFromJsonFile(registryEntriesFilepath, actionOnError),
							null,
							true,
							NsisInterop.NSISclass.DotnetFrameworkTargetedEnum.DotNet4client,
							_64Only,
							WriteIntoRegistryForWindowsAutostartup,
							HasPlugins);
						foreach (string line in list)
							sw1.WriteLine(line);

						string startMsg = "Successfully created NSIS file: ";
						actionOnError(startMsg + nsisFileName);
					}

					//DONE TODO: Must make provision if pc (to do building and compiling of NSIS scripts), does not have the DotNetChecker.dll plugin for NSIS
					//bool DotNetCheckerDllFileFound = false;
					//string DotNetCheckerFilenameEndswith = "DotNetChecker.dll";
					//string dotnetCheckerDllPath = @"C:\Program Files (x86)\NSIS\Plugins\" + DotNetCheckerFilenameEndswith;

					string nsisDir = NsisInterop.GetNsisInstallDirectory();
					string dotnetCheckerDllPath = Path.Combine(nsisDir, "Plugins", "dotnetchecker.dll");

					if (!File.Exists(dotnetCheckerDllPath))
					{
						string downloadededPath = NetworkInteropSimple.FtpDownloadFile(
								Path.GetDirectoryName(dotnetCheckerDllPath),
								OnlineSettings.OnlineAppsSettings.Instance.AppsDownloadFtpUsername,//GlobalSettings.VisualStudioInteropSettings.Instance.FtpUsername,
								OnlineSettings.OnlineAppsSettings.Instance.AppsDownloadFtpPassword,//GlobalSettings.VisualStudioInteropSettings.Instance.FtpPassword,
								OnlineSettings.PublishSettings.Instance.OnlineDotnetCheckerDllFileUrl,
								actionOnError,
								actionOnProgressPercentage);
						if (downloadededPath == null)
							UserMessages.ShowWarningMessage("Could not find (or download) DotNetChecker.dll from URL: " + OnlineSettings.PublishSettings.Instance.OnlineDotnetCheckerDllFileUrl);
						else
							dotnetCheckerDllPath = downloadededPath;
					}
					//if (!GetEmbeddedResource_FirstOneEndingWith(DotNetCheckerFilenameEndswith, dotnetCheckerDllPath))
					//    UserMessages.ShowWarningMessage("Could not find " + DotNetCheckerFilenameEndswith + " in resources");

					string MakeNsisFilePath = @"C:\Program Files (x86)\NSIS\makensis.exe";
					if (!File.Exists(MakeNsisFilePath))
						actionOnError("Could not find MakeNsis.exe: " + MakeNsisFilePath);
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
								actionOnStatus("Publish success, opening folder and/or running setup file...");
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
												actionOnStatus("Killing all open processes named " + projName);
												foreach (Process proc in openProcs)
													proc.Kill();
											}
										}
										else if (openProcs.Length == 1)
										{
											actionOnStatus("Killing open process named {0}".Fmt(projName));
											openProcs[0].Kill();
										}
									}

									if (DoNotKillProcessAndInstall)
									{
										actionOnStatus("Launching setup for '{0}', not running silently because same application name as current.".Fmt(projName));
										Process.Start(resultSetupFileName);
									}
									else
									{
										actionOnStatus("Installing '{0}' silently.".Fmt(projName));
										var setupProc = Process.Start(resultSetupFileName, "/S");
										setupProc.WaitForExit();
										actionOnStatus("Launching '{0}'.".Fmt(projName));
										try { Process.Start(projName + ".exe"); }
										catch (Exception exc) { actionOnError("Error launching '{0}': {1}".Fmt(projName, exc.Message)); }
									}
								}
							}
							successfullyCreatedNSISfile = true;
						}
						else actionOnError("Could not successfully create setup for " + projName);
					}
				});
				if (successfullyCreatedNSISfile)
					return resultSetupFileName;*/
			}
			return null;
		}
	}
}