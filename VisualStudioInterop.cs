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

public class VisualStudioInterop
{
	//private const string defaultBaseUri = "fjh.dyndns.org/";
	//private const string defaultBaseUri = "127.0.0.1";
	//private const string defaultRootUriForVsPublishing = "ftp://" + defaultBaseUri + "/francois/websites/firepuma/ownapplications";
	//private const string defaultRootUriAFTERvspublishing = "http://" + defaultBaseUri + "/ownapplications";

	//TODO: Make it easy to add todo item to a specific c# file if visual studio is not open
	//Maybe for instance popup with a prompt window and the user chooses the file and then types the description of the todo item.
	private static bool FindMsbuildPath4(out string msbuildpathOrerror)
	{
		msbuildpathOrerror = "";
		string rootfolder = @"C:\Windows\Microsoft.NET\Framework64";
		if (!Directory.Exists(rootfolder)) msbuildpathOrerror = "Dir not found for msbuild: " + rootfolder;
		else
		{
			//TODO: Should eventually make this code to look for newest version (not only 4)
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

	private enum BuildType { Rebuild, Build };
	private enum ProjectConfiguration { Debug, Release };
	private enum PlatformTarget { x86, x64 };
	private static string BuildVsProjectReturnNewversionString(string projName, string csprojFilename, string slnFilename, bool SolutionTrueProjectFalse, BuildType buildType, ProjectConfiguration configuration, PlatformTarget platformTarget, bool AutomaticallyUpdateRevision, Object textfeedbackSenderObject, TextFeedbackEventHandler textFeedbackEvent = null)
	{
		string msbuildpath;
		if (!FindMsbuildPath4(out msbuildpath))
		{
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Unable to find msbuild path: " + msbuildpath);
			return null;
		}
		//Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox,
		//"msbuild /t:publish /p:configuration=release /p:buildenvironment=DEV /p:applicationversion=" + newversionstring + " \"" + csprojFileName + "\"");
		while (msbuildpath.EndsWith("\\")) msbuildpath = msbuildpath.Substring(0, msbuildpath.Length - 1);
		msbuildpath += "\\msbuild.exe";

		bool errorOccurred = false;
		ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
		{
			ProcessStartInfo startinfo = new ProcessStartInfo(msbuildpath,
				"/t:" + buildType.ToString().ToLower() +
				" /p:configuration=" + configuration.ToString().ToLower() +
				" /p:AllowUnsafeBlocks=true" +
				" /p:PlatformTarget=" + platformTarget.ToString() +
				" \"" + (SolutionTrueProjectFalse ? slnFilename : csprojFilename) + "\"");

			//startinfo.UseShellExecute = false;
			//startinfo.CreateNoWindow = false;
			startinfo.UseShellExecute = false;
			startinfo.CreateNoWindow = true;
			startinfo.RedirectStandardOutput = true;
			startinfo.RedirectStandardError = true;

			List<string> OutputLog = new List<string>();
			System.Diagnostics.Process msbuildproc = new Process();
			msbuildproc.OutputDataReceived += delegate(object sendingProcess, DataReceivedEventArgs outLine)
			{
				//if (outLine.Data != null && outLine.Data.Trim().Length > 0)
				//  Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Msbuild output: " + outLine.Data);
				//else appendLogTextbox("Svn output empty");
				//Console.WriteLine(outLine.Data);
				if (outLine.Data != null) OutputLog.Add(outLine.Data);
			};
			msbuildproc.ErrorDataReceived += delegate(object sendingProcess, DataReceivedEventArgs errorLine)
			{
				if (errorLine.Data != null && errorLine.Data.Trim().Length > 0)
				{
					errorOccurred = true;
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Msbuild error: " + errorLine.Data);
				}
				//else appendLogTextbox("Svn error empty");
			};
			msbuildproc.StartInfo = startinfo;

			if (msbuildproc.Start())
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Started building for " + projName + ", please wait...");
			else TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Error: Could not start SVN process.");

			msbuildproc.PriorityBoostEnabled = true;
			msbuildproc.BeginOutputReadLine();
			msbuildproc.BeginErrorReadLine();

			msbuildproc.WaitForExit();

			string errorscountString = "";
			string warningscountString = "";
			foreach (string line in OutputLog)
				if (line.ToLower().EndsWith("Error(s)".ToLower()))
					errorscountString = line;
				else if (line.ToLower().EndsWith("Warning(s)".ToLower()))
					warningscountString = line;
			if (errorscountString.Length == 0)
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Could not find errors count line in output log", TextFeedbackType.Noteworthy);
			else
			{
				int errorcount;
				if (!int.TryParse(errorscountString.ToLower().Replace("error(s)", "").Trim(), out errorcount))
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Could not obtain error count from error(s) line = " + errorscountString, TextFeedbackType.Noteworthy);
				else
				{
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, errorscountString, errorcount > 0 ? TextFeedbackType.Error : TextFeedbackType.Subtle);
					if (errorcount > 0)
					{
						errorOccurred = true;
						if (UserMessages.Confirm(errorscountString + " have occurred with the build process, open the log file now?"))
						{
							string tempfile = Path.GetTempPath() + "msbuildOutputLog.txt";
							File.WriteAllLines(tempfile, OutputLog);
							System.Diagnostics.Process.Start(tempfile);
						}
					}
				}
			}
			if (warningscountString.Length == 0)
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Could not find warnings count line in output log", TextFeedbackType.Noteworthy);
			else
			{
				int warningcount;
				if (!int.TryParse(warningscountString.ToLower().Replace("warning(s)", "").Trim(), out warningcount))
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Could not obtain warning count from error(s) line = " + warningscountString, TextFeedbackType.Noteworthy);
				else
				{
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, warningscountString, warningcount > 0 ? TextFeedbackType.Noteworthy : TextFeedbackType.Subtle);
				}
			}
			//MessageBox.Show(msbuildproc.ExitCode.ToString());
		},
		WaitUntilFinish: true,
		ThreadName: "MsBuild Thread");

		if (!errorOccurred)
		{
			const string apprevstart = "<ApplicationRevision>";
			const string apprevend = "</ApplicationRevision>";
			const string appverstart = "<ApplicationVersion>";
			const string appverend = "</ApplicationVersion>";

			int apprevision = -1;
			int apprevlinenum = -1;
			string appversion = "";
			int appverlinenum = -1;
			List<string> newFileLines = new List<string>();
			StreamReader sr = new StreamReader(csprojFilename);
			try { while (!sr.EndOfStream) newFileLines.Add(sr.ReadLine()); }
			finally { sr.Close(); }

			for (int i = 0; i < newFileLines.Count; i++)
			{
				string line = newFileLines[i].ToLower().Trim();

				if (line.StartsWith(apprevstart.ToLower()) && line.EndsWith(apprevend.ToLower()))
				{
					int tmpint;
					if (int.TryParse(line.Substring(apprevstart.Length, line.Length - apprevstart.Length - apprevend.Length), out tmpint))
					{
						apprevlinenum = i;
						apprevision = tmpint;
					}
					else TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Could not obtain revision int from string: " + line);
				}
				else if (line.StartsWith(appverstart.ToLower()) && line.EndsWith(appverend.ToLower()))
				{
					appverlinenum = i;
					appversion = line.Substring(appverstart.Length, line.Length - appverstart.Length - appverend.Length);
				}
			}
			if (apprevision == -1) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Unable to obtain app revision");
			else if (appversion.Trim().Length == 0) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Unable to obtain app version string");
			else
			{
				string oldversionstring = appversion.Substring(0, appversion.LastIndexOf('.') + 1) + apprevision;
				if (AutomaticallyUpdateRevision)
				{
					bool autoIncreaseRevision = appversion.Contains("%2a");
					int newrevisionnum = apprevision + 1;
					newFileLines[apprevlinenum] = newFileLines[apprevlinenum].Substring(0, newFileLines[apprevlinenum].IndexOf(apprevstart) + apprevstart.Length)
						+ newrevisionnum
						+ newFileLines[apprevlinenum].Substring(newFileLines[apprevlinenum].IndexOf(apprevend));
					//Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, newFileLines[apprevlinenum]);

					string newversionstring = appversion.Substring(0, appversion.LastIndexOf('.') + 1) + newrevisionnum;
					if (!autoIncreaseRevision)
						newFileLines[appverlinenum] = newFileLines[appverlinenum].Substring(0, newFileLines[appverlinenum].IndexOf(appverstart) + appverstart.Length)
							+ appversion.Substring(0, appversion.LastIndexOf('.') + 1) + (apprevision + 1)
							+ newFileLines[appverlinenum].Substring(newFileLines[appverlinenum].IndexOf(appverend));
					//Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, newFileLines[appverlinenum]);

					StreamWriter sw = new StreamWriter(csprojFilename);
					try
					{
						foreach (string line in newFileLines)
							sw.WriteLine(line);
					}
					finally { sw.Close(); }

					return newversionstring;
				}
				else return oldversionstring;
			}
		}
		return null;
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

	//TODO: Start building own publishing platform (FTP, the html page, etc)
	public static string PerformPublish(Object textfeedbackSenderObject, string projName, out string versionString, bool HasPlugins, bool AutomaticallyUpdateRevision = false, bool OpenFolderAndSetupFileAfterSuccessfullNSIS = true, bool WriteIntoRegistryForWindowsAutostartup = true, TextFeedbackEventHandler textFeedbackEvent = null)
	{
		versionString = "";
		string projDir =
					Directory.Exists(projName) ? projName :
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Visual Studio 2010\Projects\" + projName;
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
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Attempting to MsBuild project " + projName);
			string newversionstring = BuildVsProjectReturnNewversionString(
				projName,
				csprojFileName,
				slnFileName,
				true,
				BuildType.Rebuild,
				ProjectConfiguration.Release,
				PlatformTarget.x86,//.x64,
				AutomaticallyUpdateRevision,
				textfeedbackSenderObject,
				textFeedbackEvent);
			if (newversionstring == null)
			{
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Could not obtain version string for project " + projName);
				return null;
			}
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(
				textfeedbackSenderObject,
				textFeedbackEvent,
				AutomaticallyUpdateRevision
				? "Updated revision of " + projName + " to " + newversionstring + ", attempting to publish..."
				: "Using current revision of " + projName + " (" + newversionstring + "), attempting to publish...");

			versionString = newversionstring;

			string nsisFileName = WindowsInterop.LocalAppDataPath + @"\FJH\NSISinstaller\NSISexports\" + projName + "_" + newversionstring + ".nsi";
			string resultSetupFileName = Path.GetDirectoryName(nsisFileName) + "\\" + NsisInterop.GetSetupNameForProduct(InsertSpacesBeforeCamelCase(projName), newversionstring);
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
					//TODO: This is awesome, after installing with NSIS you can type appname in RUN and it will open
					List<string> list = NsisInterop.CreateOwnappNsis(
					projName,
					InsertSpacesBeforeCamelCase(projName),
					newversionstring,//Should obtain (and increase) product version from csproj file
					"http://fjh.dyndns.org/ownapplications/" + projName.ToLower(),
					projName + ".exe",
					null,
					true,
					NsisInterop.NSISclass.DotnetFrameworkTargetedEnum.DotNet4client,
					WriteIntoRegistryForWindowsAutostartup,
					HasPlugins);
					foreach (string line in list)
						sw1.WriteLine(line);
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Successfully created NSIS file: " + nsisFileName);
				}

				//DONE TODO: Must make provision if pc (to do building and compiling of NSIS scripts), does not have the DotNetChecker.dll plugin for NSIS
				bool DotNetCheckerDllFileFound = false;
				string DotNetCheckerFilenameEndswith = "dotnetchecker.dll";
				string dotnetCheckerDllPath = @"C:\Program Files (x86)\NSIS\Plugins\DotNetChecker.dll";

				System.Reflection.Assembly objAssembly = System.Reflection.Assembly.GetExecutingAssembly();
				string[] myResources = objAssembly.GetManifestResourceNames();
				foreach (string reso in myResources)
					if (reso.ToLower().EndsWith(DotNetCheckerFilenameEndswith))
					{
						DotNetCheckerDllFileFound = true;
						if (!File.Exists(dotnetCheckerDllPath))
						{
							DotNetCheckerDllFileFound = true;
							Stream stream = objAssembly.GetManifestResourceStream(reso);
							int length = (int)stream.Length;
							byte[] bytesOfDotnetCheckerDLL = new byte[length];
							stream.Read(bytesOfDotnetCheckerDLL, 0, length);
							stream.Close();
							FileStream fileStream = new FileStream(dotnetCheckerDllPath, FileMode.Create);
							fileStream.Write(bytesOfDotnetCheckerDLL, 0, length);
							fileStream.Close();
							bytesOfDotnetCheckerDLL = null;
						}
					}

				if (!DotNetCheckerDllFileFound)
					UserMessages.ShowWarningMessage("Could not find DotNetChecker.dll in resources");

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
						if (OpenFolderAndSetupFileAfterSuccessfullNSIS)
						{
							TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Publish success, opening folder and running setup file...");
							Process.Start("explorer", "/select, \"" + resultSetupFileName + "\"");
							Process.Start(resultSetupFileName);
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

	public static string CreateHtmlPageReturnFilename(string projectName, string projectVersion, string setupFilename, List<string> BugsFixed, List<string> Improvements, List<string> NewFeatures)
	{
		string tempFilename = Path.GetTempPath() + "index.html";

		//string description = "";// "This is the description for " + projectName + ".";
		string bugsfixed = "";
		string improvements = "";
		string newfeatures = "";
		if (BugsFixed != null) foreach (string bug in BugsFixed) bugsfixed += "<li>" + bug + "</li>";
		if (Improvements != null) foreach (string improvement in Improvements) improvements += "<li>" + improvement + "</li>";
		if (NewFeatures != null) foreach (string newfeature in NewFeatures) newfeatures += "<li>" + newfeature + "</li>";

		bool HtmlFileFound = false;
		string HtmlFileName = "VisualStudioInterop (publish page).html";
		System.Reflection.Assembly objAssembly = System.Reflection.Assembly.GetExecutingAssembly();
		string[] myResources = objAssembly.GetManifestResourceNames();
		foreach (string reso in myResources)
			if (reso.ToLower().EndsWith(HtmlFileName.ToLower()))
			{
				HtmlFileFound = true;
				Stream stream = objAssembly.GetManifestResourceStream(reso);
				int length = (int)stream.Length;
				byte[] bytesOfPublishHtmlTemplateDLL = new byte[length];
				stream.Read(bytesOfPublishHtmlTemplateDLL, 0, length);
				stream.Close();
				FileStream fileStream = new FileStream(tempFilename, FileMode.Create);
				fileStream.Write(bytesOfPublishHtmlTemplateDLL, 0, length);
				fileStream.Close();
				string textOfFile = File.ReadAllText(tempFilename);
				textOfFile = textOfFile.Replace("{PageGeneratedDate}", DateTime.Now.ToString(@"dddd, dd MMMM yyyy \a\t HH:mm:ss"));
				textOfFile = textOfFile.Replace("{ProjectName}", projectName);
				textOfFile = textOfFile.Replace("{ProjectVersion}", projectVersion);
				textOfFile = textOfFile.Replace("{SetupFilename}", Path.GetFileName(setupFilename));
				//textOfFile = textOfFile.Replace("{DescriptionLiElements}", description);
				textOfFile = textOfFile.Replace("{BugsFixedList}", bugsfixed);
				textOfFile = textOfFile.Replace("{ImprovementList}", improvements);
				textOfFile = textOfFile.Replace("{NewFeaturesList}", newfeatures);

				File.WriteAllText(tempFilename, textOfFile);
				bytesOfPublishHtmlTemplateDLL = null;
			}

		if (!HtmlFileFound)
			UserMessages.ShowWarningMessage("Could not find Html file in resources: " + HtmlFileName);
		//using (StreamWriter sw = new StreamWriter(tempFilename, false))
		//{
		//	sw.WriteLine("<html>");
		//	sw.WriteLine("<head>");
		//	sw.WriteLine("<style>");
		//	sw.WriteLine("body { font-size: 24px; }");
		//	sw.WriteLine(".heading { color: blue; text-align: center; }");
		//	sw.WriteLine(".value { color: gray; text-align: center; }");
		//	sw.WriteLine(".downloadlink { color: orange; text-align: center; }");
		//	sw.WriteLine("</style>");
		//	sw.WriteLine("</head>");
		//	sw.WriteLine("<body>");
		//	sw.WriteLine(SurroundWithHtmlTag("Project: ", "label", "heading"));
		//	sw.WriteLine(SurroundWithHtmlTag(projName, "label", "value"));
		//	sw.WriteLine("</br>");
		//	sw.WriteLine(SurroundWithHtmlTag("Version: ", "label", "heading"));
		//	sw.WriteLine(SurroundWithHtmlTag(projVersion, "label", "value"));
		//	sw.WriteLine("</br>");
		//	sw.WriteLine("</br>");
		//	sw.WriteLine(string.Format("<a href='{0}' class='downloadlink'>{1}</a>", Path.GetFileName(setupFilename), "Download"));
		//	sw.WriteLine("</body>");
		//	sw.WriteLine("</html>");
		//	sw.Close();
		//}

		return tempFilename;
	}

	public async static void PerformPublishOnline(Object textfeedbackSenderObject, string projName, bool HasPlugins, bool AutomaticallyUpdateRevision = false, bool WriteIntoRegistryForWindowsAutostartup = true, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChanged = null)
	{
		string versionString;
		string publishedSetupPath = PerformPublish(textfeedbackSenderObject, projName, out versionString, HasPlugins, AutomaticallyUpdateRevision, false, WriteIntoRegistryForWindowsAutostartup, textFeedbackEvent);
		if (publishedSetupPath != null)
		{
			string validatedUrlsectionForProjname = HttpUtility.UrlPathEncode(projName).ToLower();

			//TODO: this (ServicePointManager.DefaultConnectionLimit) is actually very annoying, is there no other workaround?
			ServicePointManager.DefaultConnectionLimit = 10000;
			//System.Net.ServicePointManager.DefaultConnectionLimit = 1;
			//#pragma warning disable
			bool ThereIsNoProperItemsForBugsFixedEtcInNextFunction;
			//#pragma warning restore

			List<string> BugsFixed;
			List<string> Improvements;
			List<string> NewFeatures;
			//TODO: When pulling buglist/improvement etc from Trac repository for a project, must also check in the project's .csproj file whether it uses/references SharedClasses. Then it must also pull the changes from the SharedClasses Trac repository.
			GetChangeLogs(textfeedbackSenderObject, projName, out BugsFixed, out Improvements, out NewFeatures, textFeedbackEvent: textFeedbackEvent);

			string htmlFilePath = CreateHtmlPageReturnFilename(projName, versionString, publishedSetupPath, BugsFixed, Improvements, NewFeatures);

			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent,
				"Attempting Ftp Uploading of Setup file and index file for " + projName);
			await NetworkInterop.FtpUploadFiles(
				textfeedbackSenderObject,
				GlobalSettings.VisualStudioInteropSettings.Instance.GetCombinedUriForVsPublishing() + "/" + validatedUrlsectionForProjname,
				GlobalSettings.VisualStudioInteropSettings.Instance.FtpUsername,//NetworkInterop.ftpUsername,
				GlobalSettings.VisualStudioInteropSettings.Instance.FtpPassword,//NetworkInterop.ftpPassword,
				new string[] { publishedSetupPath, htmlFilePath },
				GlobalSettings.VisualStudioInteropSettings.Instance.GetCombinedUriForAFTERvspublishing() + "/" + validatedUrlsectionForProjname,
				textFeedbackEvent: textFeedbackEvent,
				progressChanged: progressChanged);
		}
	}

	public static string GetTracXmlRpcHttpPathFromProjectName(string projectName)
	{
		foreach (string tmpuri in GlobalSettings.TracXmlRpcInteropSettings.Instance.GetListedXmlRpcUrls())//GlobalSettings.TracXmlRpcInteropSettings.Instance.ListedXmlRpcUrls.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
			if (tmpuri.ToLower().Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Contains(projectName.ToLower()))
				return tmpuri;
		return null;
	}

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
		foreach (int i in tmpIdsAndDescriptionsAndTicketTypes.Keys)
		{
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Obtaining changelogs for ticket " + i.ToString() + "...", TextFeedbackType.Subtle);
			List<TracXmlRpcInterop.ChangeLogStruct> changelogs = TracXmlRpcInterop.ChangeLogs(i, ProjectXmlRpcTracUri);
			foreach (TracXmlRpcInterop.ChangeLogStruct cl in changelogs)
				if (cl.Field == "comment" && !string.IsNullOrWhiteSpace(cl.NewValue))
					//TODO: This can be greatly improved
					if (tmpIdsAndDescriptionsAndTicketTypes[i].TicketType == TracXmlRpcInterop.TicketTypeEnum.Bug)
						BugsFixed.Add("Ticket #e" + i + ": " + cl.NewValue + "  (" + tmpIdsAndDescriptionsAndTicketTypes[i].Description + ")");
					else if (tmpIdsAndDescriptionsAndTicketTypes[i].TicketType == TracXmlRpcInterop.TicketTypeEnum.Improvement)
						Improvements.Add("Ticket #" + i + ": " + cl.NewValue + "  (" + tmpIdsAndDescriptionsAndTicketTypes[i].Description + ")");
					else if (tmpIdsAndDescriptionsAndTicketTypes[i].TicketType == TracXmlRpcInterop.TicketTypeEnum.NewFeature)
						NewFeatures.Add("Ticket #" + i + ": " + cl.NewValue + "  (" + tmpIdsAndDescriptionsAndTicketTypes[i].Description + ")");
					//tmpList.Add("Ticket #" + i + ": '" + cl.Field + "' new value = " + cl.NewValue + ", old value = " + cl.OldValue);
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Finished obtaining changelogs for ticket " + i.ToString() + ".", TextFeedbackType.Subtle);
		}
	}

	//TODO: Continue with implementing this XmlRpc of Trac into the projects that uses Trac
	public static void TestTracXmlRpc(TextFeedbackEventHandler textFeedbackEvent = null)
	{
		//int[] ids = TracXmlRpcInterop.GetTicketIds();
		//foreach (int id in ids) MessageBox.Show("Id = " + id.ToString());

		//List<string> fieldLabels = TracXmlRpcInterop.GetFieldLables();
		//foreach (string label in fieldLabels) MessageBox.Show("Field label = " + label);

		//Dictionary<string, object> dict = TracXmlRpcInterop.GetFieldValuesOfTicket(3);
		//foreach (string key in dict.Keys) MessageBox.Show("Field = " + key);
	}
}