using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Net;
using System.Web;

public class VisualStudioInterop
{
	private const string defaultRootUriForVsPublishing = "ftp://fjh.dyndns.org/francois/websites/firepuma/ownapplications";
	private const string defaultRootUriAFTERvspublishing = "http://fjh.dyndns.org/ownapplications";

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
	private static string BuildVsProjectReturnNewversionString(string projectFilename, BuildType buildType, ProjectConfiguration configuration, PlatformTarget platformTarget, bool AutomaticallyUpdateRevision, TextFeedbackEventHandler textFeedbackEvent = null)
	{
		string msbuildpath;
		if (!FindMsbuildPath4(out msbuildpath))
		{
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Unable to find msbuild path: " + msbuildpath);
			return null;
		}
		//Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox,
		//"msbuild /t:publish /p:configuration=release /p:buildenvironment=DEV /p:applicationversion=" + newversionstring + " \"" + csprojFileName + "\"");
		while (msbuildpath.EndsWith("\\")) msbuildpath = msbuildpath.Substring(0, msbuildpath.Length - 1);
		msbuildpath += "\\msbuild.exe";

		ProcessStartInfo startinfo = new ProcessStartInfo(msbuildpath,
			"/t:" + buildType.ToString().ToLower() +
			" /p:configuration=" + configuration.ToString().ToLower() +
			" /p:AllowUnsafeBlocks=true" +
			" /p:PlatformTarget=" + platformTarget.ToString() +
			" \"" + projectFilename + "\"");

		startinfo.UseShellExecute = false;
		startinfo.CreateNoWindow = false;
		startinfo.RedirectStandardOutput = true;
		startinfo.RedirectStandardError = true;
		System.Diagnostics.Process msbuildproc = new Process();
		msbuildproc.OutputDataReceived += delegate(object sendingProcess, DataReceivedEventArgs outLine)
		{
			//if (outLine.Data != null && outLine.Data.Trim().Length > 0)
			//  Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Msbuild output: " + outLine.Data);
			//else appendLogTextbox("Svn output empty");
		};
		bool errorOccurred = false;
		msbuildproc.ErrorDataReceived += delegate(object sendingProcess, DataReceivedEventArgs outLine)
		{
			if (outLine.Data != null && outLine.Data.Trim().Length > 0)
			{
				errorOccurred = true;
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Msbuild error: " + outLine.Data);
			}
			//else appendLogTextbox("Svn error empty");
		};
		msbuildproc.StartInfo = startinfo;

		if (msbuildproc.Start())
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Started building, please wait...");
		else TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Error: Could not start SVN process.");

		msbuildproc.BeginOutputReadLine();
		msbuildproc.BeginErrorReadLine();

		msbuildproc.WaitForExit();

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
			StreamReader sr = new StreamReader(projectFilename);
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
					else TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Could not obtain revision int from string: " + line);
				}
				else if (line.StartsWith(appverstart.ToLower()) && line.EndsWith(appverend.ToLower()))
				{
					appverlinenum = i;
					appversion = line.Substring(appverstart.Length, line.Length - appverstart.Length - appverend.Length);
				}
			}
			if (apprevision == -1) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Unable to obtain app revision");
			else if (appversion.Trim().Length == 0) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Unable to obtain app version string");
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

					StreamWriter sw = new StreamWriter(projectFilename);
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
	public static string PerformPublish(string projName, bool AutomaticallyUpdateRevision = false, bool OpenFolderAndSetupFileAfterSuccessfullNSIS = true, TextFeedbackEventHandler textFeedbackEvent = null)
	{
		string projDir =
					Directory.Exists(projName) ? projName :
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Visual Studio 2010\Projects\" + projName;
		while (projDir.EndsWith("\\")) projDir = projDir.Substring(0, projDir.Length - 1);
		string projFolderName = projDir.Split('\\')[projDir.Split('\\').Length - 1];
		string csprojFileName = projDir + "\\" + projFolderName + ".csproj";

		bool ProjFileFound = false;
		if (File.Exists(csprojFileName)) ProjFileFound = true;
		else
		{
			csprojFileName = projDir + "\\" + projFolderName + "\\" + projFolderName + ".csproj";
			if (File.Exists(csprojFileName)) ProjFileFound = true;
		}

		if (!ProjFileFound) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Could not find project file (csproj) in dir " + projDir);
		else
		{
			string newversionstring = BuildVsProjectReturnNewversionString(
				csprojFileName,
				BuildType.Rebuild,
				ProjectConfiguration.Release,
				PlatformTarget.x86,//.x64,
				AutomaticallyUpdateRevision);
			if (newversionstring == null) return null;

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
					true);
					foreach (string line in list)
						sw1.WriteLine(line);
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Successfully created NSIS file: " + nsisFileName);
				}

				//TODO: Must make provision if pc (to do building and compiling of NSIS scripts), does not have the DotNetChecker.dll plugin for NSIS
				string dotnetCheckerDllPath = @"C:\Program Files (x86)\NSIS\Plugins\DotNetChecker.dll";
				if (!File.Exists(dotnetCheckerDllPath))
				{
					System.Reflection.Assembly objAssembly = System.Reflection.Assembly.GetExecutingAssembly();
					string[] myResources = objAssembly.GetManifestResourceNames();
					foreach (string reso in myResources)
						if (reso.ToLower().EndsWith("dotnetchecker.dll"))
						{
							Stream stream = objAssembly.GetManifestResourceStream(reso);
							int length = (int)stream.Length;
							byte[] bytesOfDotnetCheckerDLL = new byte[length];
							stream.Read(bytesOfDotnetCheckerDLL, 0, length);
							stream.Close();
							FileStream fileStream = new FileStream(dotnetCheckerDllPath, FileMode.Create);
							fileStream.Write(bytesOfDotnetCheckerDLL, 0, length);
							fileStream.Close();
						}
				}

				
				string MakeNsisFilePath = @"C:\Program Files (x86)\NSIS\makensis.exe";
				if (!File.Exists(MakeNsisFilePath)) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Could not find MakeNsis.exe: " + MakeNsisFilePath);
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
							Process.Start("explorer", "/select, \"" + resultSetupFileName + "\"");
							Process.Start(resultSetupFileName);
						}
						successfullyCreatedNSISfile = true;
					}
					else Process.Start("explorer", "/select, \"" + Path.GetDirectoryName(nsisFileName) + "\"");
				}
			});
			if (successfullyCreatedNSISfile)
				return resultSetupFileName;
		}
		return null;
	}

	public static void PerformPublishOnline(string projName, bool AutomaticallyUpdateRevision = false, TextFeedbackEventHandler textFeedbackEvent = null)
	{
		string publishedSetupPath = PerformPublish(projName, AutomaticallyUpdateRevision, false, textFeedbackEvent);
		if (publishedSetupPath != null)
		{
			string validatedUrlsectionForProjname = HttpUtility.UrlPathEncode(projName).ToLower();

			NetworkInterop.FtpUploadFile(
				defaultRootUriForVsPublishing + "/" + validatedUrlsectionForProjname,
				NetworkInterop.ftpUsername,
				NetworkInterop.ftpPassword,
				publishedSetupPath,
				defaultRootUriAFTERvspublishing + "/" + validatedUrlsectionForProjname);
		}
	}
}