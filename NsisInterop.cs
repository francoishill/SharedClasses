using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SectionDetails = NsisInterop.NSISclass.SectionGroupClass.SectionClass;
using ShortcutDetails = NsisInterop.NSISclass.SectionGroupClass.SectionClass.ShortcutDetails;
using FileToAddTextblock = NsisInterop.NSISclass.SectionGroupClass.SectionClass.FileToAddTextblock;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using SharedClasses;
using Microsoft.Win32;

public class NsisInterop
{
	//TODO: Add functionality to close (maybe with messagebox to confirm) the process being installed if its open
	public static string GetSetupNameForProduct(string PublishedName, string ProductVersion)
	{
		string SetupName = PublishedName;
		while (SetupName.Contains(' ')) SetupName = SetupName.Replace(" ", "");
		return "Setup_" + SetupName + "_" + ProductVersion.Replace('.', '_') + ".exe";
	}

	//public enum BuildTypeEnum { Debug, Release };
	public static List<string> CreateOwnappNsis(
		string VsProjectName,
		string ProductPublishedNameIn,
		string ProductVersionIn,
		//string ProductPublisherIn,
		string ProductWebsiteIn,
		string ProductExeNameIn,
		RegistryInterop.MainContextMenuItem contextMenuItems,
		NSISclass.LicensePageDetails LicenseDetails,
		//List<NSISclass.SectionGroupClass.SectionClass> sections,
		bool InstallForAllUsers,
		NSISclass.DotnetFrameworkTargetedEnum DotnetFrameworkTargetedIn,
		bool _64Only,
		bool WriteIntoRegistryForWindowsAutostartup,
		bool HasPlugins,
		string customSetupFilename = null)
	{
		NSISclass nsis = new NSISclass(
			ProductPublishedNameIn,
			ProductVersionIn,
			"Francois Hill",//ProductPublisherIn,
			ProductWebsiteIn,
			ProductExeNameIn,
			new NSISclass.Compressor(NSISclass.Compressor.CompressionModeEnum.lzma, true, true),//new NSISclass.Compressor(NSISclass.Compressor.CompressionModeEnum.bzip2, false, false),
			64,
			customSetupFilename == null ? GetSetupNameForProduct(ProductPublishedNameIn, ProductVersionIn) : customSetupFilename,
			NSISclass.LanguagesEnum.English,
			true,
			true,
			LicenseDetails,
			true,
			true,
			true,
			ProductExeNameIn,
			null,//No InstTypes at this stage
			"Francois Hill",
			DotnetFrameworkTargetedIn,
			_64Only
			//InstallForAllUsersIn: InstallForAllUsers
			);

		//string rootProjDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Visual Studio 2010\Projects";
		string subDirInProj = @"\bin\Release";
		string binariesDir = 
			Directory.Exists(PublishInterop.cProjectsRootDir.TrimEnd('\\') + @"\" + VsProjectName + subDirInProj)
			? PublishInterop.cProjectsRootDir.TrimEnd('\\') + @"\" + VsProjectName + subDirInProj
			: PublishInterop.cProjectsRootDir.TrimEnd('\\') + @"\" + VsProjectName + @"\" + VsProjectName + subDirInProj;
		List<string> SectionGroupLines = new List<string>();

		bool isAutoUpdater = ProductPublishedNameIn.Replace(" ", "").Equals("AutoUpdater", StringComparison.InvariantCultureIgnoreCase);

		SectionGroupLines.Add("Function .onInit");
		if (!isAutoUpdater)
			if (WriteIntoRegistryForWindowsAutostartup)
				SectionGroupLines.Add("StrCpy $state_autostartCheckbox 1");
		SectionGroupLines.Add("	!include \"x64.nsh\"");
		SectionGroupLines.Add("	; This checks if nsis is running under wow64 (since nsis is only 32bit)");
		SectionGroupLines.Add("	; hopefully this will be dependable in the future too...");
		SectionGroupLines.Add("	${If} ${RunningX64}");
		//SectionGroupLines.Add("		!insertmacro MUI_LANGDLL_DISPLAY");
		SectionGroupLines.Add("		SetRegView 64");
		SectionGroupLines.Add("	${Else}");
		if (_64Only)
		{
			SectionGroupLines.Add("		MessageBox MB_OK|MB_ICONSTOP \"You cannot run this version of $PRODUCT_NAME on your OS.$\\r$\\n\\");
			SectionGroupLines.Add("		  Please use a 64-bit OS or download a 32-bit version of PRODUCT_NAME.\"");
			SectionGroupLines.Add("		Quit");
		}
		else
		{
			SectionGroupLines.Add("		; Currently allows 32bit mode");
			SectionGroupLines.Add("		;MessageBox MB_OK|MB_ICONSTOP \"You cannot run this version of $PRODUCT_NAME on your OS.$\\r$\\n\\");
			SectionGroupLines.Add("		;  Please use a 64-bit OS or download a 32-bit version of PRODUCT_NAME.\"");
			SectionGroupLines.Add("		;Quit");
		}
		SectionGroupLines.Add("	${EndIf}");
		SectionGroupLines.Add("FunctionEnd");

		SectionGroupLines.Add(@"Section ""Full program"" SEC001");
		SectionGroupLines.Add(@"  SetShellVarContext all");
		SectionGroupLines.Add(@"  SetOverwrite on");
		SectionGroupLines.Add(@"	SetOutPath ""$INSTDIR""");
		SectionGroupLines.Add(@"  SetOverwrite on");
		SectionGroupLines.Add(@"  File /a /x *.pdb /x *.application /x *.vshost.* /x *.manifest" + MainProgram_FaceDetectionNsisExclusionList() + @" """ + binariesDir + @"\*.*""");

		//If NSISdl does not work right may be required to have inetc.dll, NSISdl is already part of NSIS installation`
		//string inetcDllPath = Path.Combine(nsisDir, "Plugins", "inetc.dll");

		string NsisUrlLibDllPath = Path.Combine(GetNsisInstallDirectory() ?? "c:\\zzzzzz", "Plugins", "NsisUrlLib.dll");
		if (!File.Exists(NsisUrlLibDllPath))
			UserMessages.ShowErrorMessage("NSIS will not compile, missing plugin: " + NsisUrlLibDllPath);

		SectionGroupLines.Add(@"IfFileExists ""$PROGRAMFILES\Auto Updater\AutoUpdater.exe"" AutoUpdaterFound");
		SectionGroupLines.Add(@"	  RetryDownload:");
		SectionGroupLines.Add(@"	  ;All following lines removed, gave error when trying to read content from a URL");
		SectionGroupLines.Add(@"	  ;NsisUrlLib::UrlOpen /NOUNLOAD """ + SharedClasses.SettingsSimple.HomePcUrls.Instance.AppsPublishingRoot + @"/json/getautoupdaterlatest"" ;Get content of this page (it returns the URL of newest setup package of AutoUpdater");
		SectionGroupLines.Add(@"	  ;Pop $7");
		SectionGroupLines.Add(@"	  ;MessageBox MB_OK $7");
		SectionGroupLines.Add(@"	  ;NsisUrlLib::IterateLine /NOUNLOAD ;Read the first line (which is the URL)");
		SectionGroupLines.Add(@"	  ;Pop $7 ;Place the URL read into variable $7");
		SectionGroupLines.Add(@"	  ;MessageBox MB_OK ""$7""");
		SectionGroupLines.Add(@"	  ;NSISdl::download ""$7"" tmpAutoUpdater_SetupLatest.exe ;Download the file at this URL");
		SectionGroupLines.Add(@"	  NSISdl::download """ + SharedClasses.SettingsSimple.HomePcUrls.Instance.AppsPublishingRoot + @"/downloadownapps.php?relativepath=autoupdater/AutoUpdater_SetupLatest.exe"" tmpAutoUpdater_SetupLatest.exe ; Download latest AutoUpdater Setup");
		SectionGroupLines.Add(@"	  Pop $R4 ;Read the result of the download");
		SectionGroupLines.Add(@"	  StrCmp $R4 ""success"" SuccessfullyDownloadedAutoUpdater");
		SectionGroupLines.Add(@"	  ;StrCmp $R4 ""cancel"" DownloadCanceled");
		SectionGroupLines.Add(@"	  ;IntCmp $R5 $R0 NoSuccess");
		SectionGroupLines.Add(@"	  ;;DetailPrint ""Download failed (error $R4)"" ;, trying with other mirror");
		SectionGroupLines.Add(@"	  MessageBox MB_RETRYCANCEL ""Download failed, retry download?"" IDRETRY RetryDownload");
		SectionGroupLines.Add(@"	  	DetailPrint ""Download unsuccessful for AutoUpdater (reason = $R4), ${PRODUCT_NAME} will not automatically be updated""");
		SectionGroupLines.Add(@"	  	;Abort ; causes installer to quit.");
		SectionGroupLines.Add(@"	  	goto AutoUpdaterDownloadSkipped");
		SectionGroupLines.Add(@"	  SuccessfullyDownloadedAutoUpdater:");
		SectionGroupLines.Add(@"	  ExecWait ""tmpAutoUpdater_SetupLatest.exe /S""");
		SectionGroupLines.Add(@"	  Delete tmpAutoUpdater_SetupLatest.exe");
		SectionGroupLines.Add(@"	  ;nsExec::Exec tmpAutoUpdater_SetupLatest.exe /S ;Install AutoUpdater silently");
		SectionGroupLines.Add(@"AutoUpdaterDownloadSkipped:");
		SectionGroupLines.Add(@"AutoUpdaterFound:");

		SectionGroupLines.Add(@"IfFileExists ""$PROGRAMFILES\Show No Callback Notification\ShowNoCallbackNotification.exe"" ShowNoCallbackNotificationFound");
		SectionGroupLines.Add(@"	  RetryDownload2:");
		SectionGroupLines.Add(@"	  NSISdl::download """ + SharedClasses.SettingsSimple.HomePcUrls.Instance.AppsPublishingRoot + @"/downloadownapps.php?relativepath=shownocallbacknotification/ShowNoCallbackNotification_SetupLatest.exe"" tmpShowNoCallbackNotification_SetupLatest.exe ; Download latest ShowNoCallbackNotification Setup");
		SectionGroupLines.Add(@"	  Pop $R4 ;Read the result of the download");
		SectionGroupLines.Add(@"	  StrCmp $R4 ""success"" SuccessfullyDownloadedShowNoCallbackNotification");
		SectionGroupLines.Add(@"	  MessageBox MB_RETRYCANCEL ""Download failed, retry download?"" IDRETRY RetryDownload2");
		SectionGroupLines.Add(@"	  	DetailPrint ""Download unsuccessful for ShowNoCallbackNotification (reason = $R4), ${PRODUCT_NAME} might not show some notifications properly""");
		SectionGroupLines.Add(@"	  	goto ShowNoCallbackNotificationDownloadSkipped");
		SectionGroupLines.Add(@"	  SuccessfullyDownloadedShowNoCallbackNotification:");
		SectionGroupLines.Add(@"	  ExecWait ""tmpShowNoCallbackNotification_SetupLatest.exe /S""");
		SectionGroupLines.Add(@"	  Delete tmpShowNoCallbackNotification_SetupLatest.exe");
		SectionGroupLines.Add(@"ShowNoCallbackNotificationDownloadSkipped:");
		SectionGroupLines.Add(@"ShowNoCallbackNotificationFound:");

		SectionGroupLines.Add(@"IfFileExists ""$PROGRAMFILES\Standalone Uploader\StandaloneUploader.exe"" StandaloneUploaderFound");
		SectionGroupLines.Add(@"	  RetryDownload3:");
		SectionGroupLines.Add(@"	  NSISdl::download """ + SharedClasses.SettingsSimple.HomePcUrls.Instance.AppsPublishingRoot + @"/downloadownapps.php?relativepath=standaloneuploader/StandaloneUploader_SetupLatest.exe"" tmpStandaloneUploader_SetupLatest.exe ; Download latest StandaloneUploader Setup");
		SectionGroupLines.Add(@"	  Pop $R4 ;Read the result of the download");
		SectionGroupLines.Add(@"	  StrCmp $R4 ""success"" SuccessfullyDownloadedStandaloneUploader");
		SectionGroupLines.Add(@"	  MessageBox MB_RETRYCANCEL ""Download failed, retry download?"" IDRETRY RetryDownload3");
		SectionGroupLines.Add(@"	  	DetailPrint ""Download unsuccessful for StandaloneUploader (reason = $R4), ${PRODUCT_NAME} might not upload some files""");
		SectionGroupLines.Add(@"	  	goto StandaloneUploaderDownloadSkipped");
		SectionGroupLines.Add(@"	  SuccessfullyDownloadedStandaloneUploader:");
		SectionGroupLines.Add(@"	  ExecWait ""tmpStandaloneUploader_SetupLatest.exe /S""");
		SectionGroupLines.Add(@"	  Delete tmpStandaloneUploader_SetupLatest.exe");
		SectionGroupLines.Add(@"StandaloneUploaderDownloadSkipped:");
		SectionGroupLines.Add(@"StandaloneUploaderFound:");

		//if (!isAutoUpdater)
		//{
		//No always start with windows if AutoUpdater
		if (!isAutoUpdater) SectionGroupLines.Add(@"${If} $state_autostartCheckbox <> 0");
		SectionGroupLines.Add(@"  WriteRegStr HKCU ""SOFTWARE\Microsoft\Windows\CurrentVersion\Run"" '${PRODUCT_NAME}' '$INSTDIR\${PRODUCT_EXE_NAME}'");
		if (!isAutoUpdater) SectionGroupLines.Add(@"${EndIf}");
		//}
		SectionGroupLines.Add(@"SectionEnd");

		//Section "Plugins" SEC002
		//	SetShellVarContext all
		//	SetOverwrite ifnewer
		//	SetOutPath "$INSTDIR\Plugins"
		//	SetOverwrite ifnewer
		//	File /a "C:\Users\francois\Documents\Visual Studio 2010\Projects\QuickAccess\QuickAccess\bin\Release\Plugins\*.*"
		//SectionEnd
		if (HasPlugins)
		{
			int startSectionNumber = 2;//SEC002
			string SolutionBaseDir = Path.Combine(PublishInterop.cProjectsRootDir, VsProjectName);//Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Visual Studio 2010\Projects\" + VsProjectName;

			SectionGroupLines.Add("");
			SectionGroupLines.Add(@"SectionGroup ""Plugins""");
			foreach (string baseDirForEachPluginProjects in Directory.GetDirectories(SolutionBaseDir, "*Plugin"))
			{
				string baseFolderNameForPlugin = Path.GetFileName(baseDirForEachPluginProjects);
				string pluginDllPath = baseDirForEachPluginProjects + @"\bin\Release";
				string pluginName = 
						(baseFolderNameForPlugin.ToLower().EndsWith("plugin")
						? baseFolderNameForPlugin.Substring(0, baseFolderNameForPlugin.Length - 6)
						: baseFolderNameForPlugin).InsertSpacesBeforeCamelCase();

				//SectionGroupLines.Add("");
				//foreach (string dllfile in Directory.GetFiles(PluginsDir, "*.dll"))
				//{
				//	string filenameWithoutExtension = Path.GetFileNameWithoutExtension(dllfile);
				//	string filenameExcludingLastPluginWord =
				//	VisualStudioInterop.InsertSpacesBeforeCamelCase(
				//		filenameWithoutExtension.ToLower().EndsWith("plugin")
				//		? filenameWithoutExtension.Substring(0, filenameWithoutExtension.Length - 6)
				//		: filenameWithoutExtension);
				//SectionGroupLines.Add("");
				SectionGroupLines.Add(NSISclass.Spacer + @"Section """ + pluginName + @""" SEC" + startSectionNumber++.ToString("000"));
				SectionGroupLines.Add(NSISclass.Spacer + @"  SetShellVarContext all");
				SectionGroupLines.Add(NSISclass.Spacer + @"  SetOverwrite on");
				SectionGroupLines.Add(NSISclass.Spacer + @"	SetOutPath ""$INSTDIR\Plugins""");
				SectionGroupLines.Add(NSISclass.Spacer + @"  SetOverwrite on");
				SectionGroupLines.Add(NSISclass.Spacer + @"  File /a /x *.pdb /x *.xml /x *Toolkit* /x *InterfaceFor* /x *CookComputing.XmlRpcV2.dll /x *MouseGestures.dll /x *System.Windows.Controls.WpfPropertyGrid.dll" + Plugins_FaceDetectionNsisExclusionList() + Plugins_Pdf2textExclusionList() + @" """ + pluginDllPath + @"\*.*""");
				SectionGroupLines.Add(NSISclass.Spacer + @"SectionEnd");
				SectionGroupLines.Add("");
				//}
			}
			if (SectionGroupLines[SectionGroupLines.Count - 1].Trim() == "")
				SectionGroupLines.RemoveAt(SectionGroupLines.Count - 1);
			SectionGroupLines.Add("SectionGroupEnd");

			//string PluginsDir = PublishedDir + @"\Plugins";
			//SectionGroupLines.Add("");
			//SectionGroupLines.Add(@"SectionGroup ""Plugins""");
			////SectionGroupLines.Add("");
			//foreach (string dllfile in Directory.GetFiles(PluginsDir, "*.dll"))
			//{
			//	string filenameWithoutExtension = Path.GetFileNameWithoutExtension(dllfile);
			//	string filenameExcludingLastPluginWord =
			//		VisualStudioInterop.InsertSpacesBeforeCamelCase(
			//		filenameWithoutExtension.ToLower().EndsWith("plugin")
			//		? filenameWithoutExtension.Substring(0, filenameWithoutExtension.Length - 6)
			//		: filenameWithoutExtension);
			//	//SectionGroupLines.Add("");
			//	SectionGroupLines.Add(NSISclass.Spacer + @"Section """ + filenameExcludingLastPluginWord + @""" SEC" + startSectionNumber++.ToString("000"));
			//	SectionGroupLines.Add(NSISclass.Spacer + @"  SetShellVarContext all");
			//	SectionGroupLines.Add(NSISclass.Spacer + @"  SetOverwrite ifnewer");
			//	SectionGroupLines.Add(NSISclass.Spacer + @"	SetOutPath ""$INSTDIR\Plugins""");
			//	SectionGroupLines.Add(NSISclass.Spacer + @"  SetOverwrite ifnewer");
			//	SectionGroupLines.Add(NSISclass.Spacer + @"  File /a """ + PluginsDir + @"\" + filenameWithoutExtension + @".*""");
			//	SectionGroupLines.Add(NSISclass.Spacer + @"SectionEnd");
			//	SectionGroupLines.Add("");
			//}
			//if (SectionGroupLines[SectionGroupLines.Count - 1].Trim() == "")
			//	SectionGroupLines.RemoveAt(SectionGroupLines.Count - 1);
			//SectionGroupLines.Add("SectionGroupEnd");

			//SectionGroupLines.Add(@"Section ""Plugins"" SEC002");
			//SectionGroupLines.Add(@"  SetShellVarContext all");
			//SectionGroupLines.Add(@"  SetOverwrite ifnewer");
			//SectionGroupLines.Add(@"	SetOutPath ""$INSTDIR\Plugins""");
			//SectionGroupLines.Add(@"  SetOverwrite ifnewer");
			//SectionGroupLines.Add(@"  File /a """ + PublishedDir + @"\Plugins\*.*""");
			//SectionGroupLines.Add(@"SectionEnd");
		}

		return nsis.GetAllLinesForNSISfile(
			SectionGroupLines,
			null,
			WriteIntoRegistryForWindowsAutostartup,
			HasPlugins,
			contextMenuItems);//SectionDescriptions);
	}

	public static string GetNsisInstallDirectory()
	{
		string subPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
		RegistryKey uninstallKey = null;
		RegistryKey nsisKey = null;

		foreach (RegistryView rv in Enum.GetValues(typeof(RegistryView)))
		{
			try
			{
				if (uninstallKey != null)
					uninstallKey.Close();
				uninstallKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, rv)
				 .OpenSubKey(subPath);
				if (uninstallKey.GetSubKeyNames().Contains("NSIS", StringComparer.InvariantCultureIgnoreCase))
				{
					nsisKey = uninstallKey.OpenSubKey("NSIS");
					break;
				}
				else
				{
					uninstallKey.Close();
					uninstallKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, rv)
						.OpenSubKey(subPath);
					if (uninstallKey.GetSubKeyNames().Contains("NSIS", StringComparer.InvariantCultureIgnoreCase))
					{
						nsisKey = uninstallKey.OpenSubKey("NSIS");
						break;
					}
				}
			}
			catch//(Exception exc)
			{
			}
		}
		uninstallKey.Close();

		if (nsisKey == null)//Could not find path in registry, some error occurred (could be because its called from apache/php which is running as a service so we are 'not logged in')
		{
			string progFilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NSIS");
			string progFilesX86Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "NSIS");
			if (Directory.Exists(progFilesDir))
				return progFilesDir;
			else if (Directory.Exists(progFilesX86Dir))
				return progFilesX86Dir;
		}

		if (nsisKey != null)
		{
			try
			{
				object installLocationObj = nsisKey.GetValue("InstallLocation", null);
				if (installLocationObj != null)
				{
					string directoryWhereNsisIsInstalled = installLocationObj.ToString();
					return directoryWhereNsisIsInstalled;
				}
			}
			finally
			{
				nsisKey.Close();
			}
		}
		return null;//Did not find NSIS directory in Registry
	}

	private static string MainProgram_FaceDetectionNsisExclusionList()
	{
		return " /x *cvextern*dll /x *opencv_*.dll";
		//string tmpstr = "";
		//foreach (string filename in FaceDetectionInterop.ListOfRequiredDllsInExeDir.Keys)
		//	tmpstr += " /x " + filename;
		//return tmpstr;
	}

	private static string Plugins_FaceDetectionNsisExclusionList()
	{
		return " /x *cvextern*.dll /x opencv_*.dll /x *Emgu.*.dll";
		//string tmpstr = "";
		//foreach (string filename in FaceDetectionInterop.ListOfRequiredDllsInExeDir.Keys)
		//	tmpstr += " /x " + filename;
		//return tmpstr;
	}

	private static string Plugins_Pdf2textExclusionList()
	{
		return " /x *IKVM.*.dll /x commons-logging.dll /x fontbox-*.dll /x pdfbox-*.dll";
	}

	public static string DotNetChecker_NSH_file
	{
		get
		{
			string FileContents =
					@"
				!macro CheckNetFramework FrameworkVersion
				Var /GLOBAL dotNetUrl
				Var /GLOBAL dotNetReadableVersion

				!define DOTNET40Full_URL 	""http://www.microsoft.com/downloads/info.aspx?na=41&srcfamilyid=0a391abd-25c1-4fc0-919f-b21f31ab88b7&srcdisplaylang=en&u=http%3a%2f%2fdownload.microsoft.com%2fdownload%2f9%2f5%2fA%2f95A9616B-7A37-4AF6-BC36-D6EA96C8DAAE%2fdotNetFx40_Full_x86_x64.exe""
				!define DOTNET40Client_URL	""http://www.microsoft.com/downloads/info.aspx?na=41&srcfamilyid=e5ad0459-cbcc-4b4f-97b6-fb17111cf544&srcdisplaylang=en&u=http%3a%2f%2fdownload.microsoft.com%2fdownload%2f5%2f6%2f2%2f562A10F9-C9F4-4313-A044-9C94E0A8FAC8%2fdotNetFx40_Client_x86_x64.exe""
				!define DOTNET35_URL		""http://download.microsoft.com/download/2/0/e/20e90413-712f-438c-988e-fdaa79a8ac3d/dotnetfx35.exe""
				!define DOTNET30_URL		""http://download.microsoft.com/download/2/0/e/20e90413-712f-438c-988e-fdaa79a8ac3d/dotnetfx35.exe""
				!define DOTNET20_URL		""http://www.microsoft.com/downloads/info.aspx?na=41&srcfamilyid=0856eacb-4362-4b0d-8edd-aab15c5e04f5&srcdisplaylang=en&u=http%3a%2f%2fdownload.microsoft.com%2fdownload%2f5%2f6%2f7%2f567758a3-759e-473e-bf8f-52154438565a%2fdotnetfx.exe""
				!define DOTNET11_URL		""http://www.microsoft.com/downloads/info.aspx?na=41&srcfamilyid=262d25e3-f589-4842-8157-034d1e7cf3a3&srcdisplaylang=en&u=http%3a%2f%2fdownload.microsoft.com%2fdownload%2fa%2fa%2fc%2faac39226-8825-44ce-90e3-bf8203e74006%2fdotnetfx.exe""
				!define DOTNET10_URL		""http://www.microsoft.com/downloads/info.aspx?na=41&srcfamilyid=262d25e3-f589-4842-8157-034d1e7cf3a3&srcdisplaylang=en&u=http%3a%2f%2fdownload.microsoft.com%2fdownload%2fa%2fa%2fc%2faac39226-8825-44ce-90e3-bf8203e74006%2fdotnetfx.exe""

				${If} ${FrameworkVersion} == ""40Full""
					StrCpy $dotNetUrl ${DOTNET40Full_URL}
					StrCpy $dotNetReadableVersion ""4.0 Full""
				${ElseIf} ${FrameworkVersion} == ""40Client""
					StrCpy $dotNetUrl ${DOTNET40Client_URL}
					StrCpy $dotNetReadableVersion ""4.0 ClientOnServerSide""
				${ElseIf} ${FrameworkVersion} == ""35""
					StrCpy $dotNetUrl ${DOTNET35_URL}
					StrCpy $dotNetReadableVersion ""3.5""
				${ElseIf} ${FrameworkVersion} == ""30""
					StrCpy $dotNetUrl ${DOTNET30_URL}
					StrCpy $dotNetReadableVersion ""3.0""
				${ElseIf} ${FrameworkVersion} == ""20""
					StrCpy $dotNetUrl ${DOTNET20_URL}
					StrCpy $dotNetReadableVersion ""2.0""
				${ElseIf} ${FrameworkVersion} == ""11""
					StrCpy $dotNetUrl ${DOTNET11_URL}
					StrCpy $dotNetReadableVersion ""1.1""
				${ElseIf} ${FrameworkVersion} == ""10""
					StrCpy $dotNetUrl ${DOTNET10_URL}
					StrCpy $dotNetReadableVersion ""1.0""
				${EndIf}
	
				DetailPrint ""Checking .NET Framework version...""

				Push $0
				Push $1
				Push $2
				Push $3
				Push $4
				Push $5
				Push $6
				Push $7

				DotNetChecker::IsDotNet${FrameworkVersion}Installed
				Pop $0
	
				${If} $0 == ""false""
					DetailPrint "".NET Framework $dotNetReadableVersion not found, download is required for program to run.""
					Goto NoDotNET
				${Else}
					DetailPrint "".NET Framework $dotNetReadableVersion found, no need to install.""
					Goto NewDotNET
				${EndIf}

			NoDotNET:
				MessageBox MB_YESNOCANCEL|MB_ICONEXCLAMATION \
				"".NET Framework not installed. Required version: $dotNetReadableVersion.$\nDownload .NET Framework $dotNetReadableVersion from www.microsoft.com?"" \
				/SD IDYES IDYES DownloadDotNET IDNO NewDotNET
				goto GiveUpDotNET ;IDCANCEL

			DownloadDotNET:
				DetailPrint ""Beginning download of .NET Framework $dotNetReadableVersion.""
				NSISDL::download $dotNetUrl ""$TEMP\dotnetfx.exe""
				DetailPrint ""Completed download.""

				Pop $0
				${If} $0 == ""cancel""
					MessageBox MB_YESNO|MB_ICONEXCLAMATION \
					""Download cancelled.  Continue Installation?"" \
					IDYES NewDotNET IDNO GiveUpDotNET
				${ElseIf} $0 != ""success""
					MessageBox MB_YESNO|MB_ICONEXCLAMATION \
					""Download failed:$\n$0$\n$\nContinue Installation?"" \
					IDYES NewDotNET IDNO GiveUpDotNET
				${EndIf}

				DetailPrint ""Pausing installation while downloaded .NET Framework installer runs.""
				ExecWait '$TEMP\dotnetfx.exe /q /c:""install /q""'

				DetailPrint ""Completed .NET Framework install/update. Removing .NET Framework installer.""
				Delete ""$TEMP\dotnetfx.exe""
				DetailPrint "".NET Framework installer removed.""
				goto NewDotNet

			GiveUpDotNET:
				Abort ""Installation cancelled by user.""

			NewDotNET:
				DetailPrint ""Proceeding with remainder of installation.""
				Pop $0
				Pop $1
				Pop $2
				Pop $3
				Pop $4
				Pop $5
				Pop $6
				Pop $7

			!macroend
			";
			FileContents = FileContents.Replace("\buildTask\buildTask\buildTask\buildTask\buildTask", "    ").Replace("\buildTask\buildTask\buildTask\buildTask", "  ").Replace("\buildTask\buildTask\buildTask", "");
			return FileContents;
		}
	}


	public class NSISclass
	{
		public enum DotnetFrameworkTargetedEnum : int
		{
			None,
			DotNet1_0,
			DotNet1_1,
			DotNet2_0,
			DotNet3_0,
			DotNet3_5,
			DotNet4client,
			DotNet4full
		}
		//[Flags]
		//public enum RequiredDotnetVersionsEnum : int
		//{
		//  None = 0, DotNet1_0 = 1, DotNet1_1 = 2, DotNet2_0 = 4, DotNet3_0 = 8, DotNet3_5 = 16, DotNet4client = 32, DotNet4full = 64,
		//  All = DotNet1_0 | DotNet1_1 | DotNet2_0 | DotNet3_0 | DotNet3_5 | DotNet4client | DotNet4full
		//}

		public static string Spacer = "  ";
		public string Empty = "";
		public string ProductName;
		public string ProductVersion;
		public string ProductPublisher;
		public string ProductWebsite;
		public string ProductExeName;

		public Compressor CompressorUsed;
		public int? CompressorDictSizeMegabytes;
		public string SetupFileName;
		public LanguagesEnum SetupLanguage;

		public Boolean UseUninstaller;
		public string InstallerIconPath;
		public string UninstallerIconPath;
		public Boolean ShowWelcomePage;

		public LicensePageDetails LicenseDialogDetailsUsed;
		public Boolean ShowComponentsPage;
		public Boolean ShowDirectoryPage;
		public Boolean UserMayChangeStartMenuName;

		public string FilePathToRunOnFinish;

		public List<string> InstTypes;

		public Boolean InstallForAllUsers;
		public string StartmenuFolderName;

		public DotnetFrameworkTargetedEnum DotnetFrameworkTargeted;
		public bool _64Only;

		public NSISclass() { }

		public NSISclass(
				string ProductNameIn,
				string ProductVersionIn,
				string ProductPublisherIn,
				string ProductWebsiteIn,
				string ProductExeNameIn,
				Compressor CompressorUsedIn,
				int? CompressorDictSizeMegabytesIn,
				string SetupFileNameIn,
				LanguagesEnum SetupLanguageIn,
			//LanguagesEnum LanguagesIn,
				Boolean UseUninstallerIn,
				Boolean ShowWelcomePageIn,
				LicensePageDetails LicenseDialogDetailsUsedIn,
				Boolean ShowComponentsPageIn,
				Boolean ShowDirectoryPageIn,
				Boolean UserMayChangeStartMenuNameIn,
				string FilePathToRunOnFinishIn,
				List<string> InstTypesIn,
				string StartmenuFolderNameIn,
				DotnetFrameworkTargetedEnum DotnetFrameworkTargetedIn,
				bool _64OnlyIn,
				string InstallerIconPathIn = @"${NSISDIR}\Contrib\Graphics\Icons\modern-install-blue-full.ico",
				string UninstallerIconPathIn = @"${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall-blue-full.ico",
				Boolean InstallForAllUsersIn = true)
		{
			ProductName = ProductNameIn;
			ProductVersion = ProductVersionIn;
			ProductPublisher = ProductPublisherIn;
			ProductWebsite = ProductWebsiteIn;
			ProductExeName = ProductExeNameIn;
			CompressorUsed = CompressorUsedIn;
			CompressorDictSizeMegabytes = CompressorDictSizeMegabytesIn;
			SetupFileName = SetupFileNameIn;

			int CountSetupLanguages = 0; foreach (LanguagesEnum testLanguageFound in Enum.GetValues(typeof(LanguagesEnum))) if (SetupLanguage.HasFlag(testLanguageFound)) CountSetupLanguages++;
			if (CountSetupLanguages > 1) MessageBox.Show("More than one setup language not allowed, first chosen (alphabetically) will be used", "More than one setup language", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			SetupLanguage = SetupLanguageIn;

			//Languages = LanguagesIn;
			UseUninstaller = UseUninstallerIn;
			InstallerIconPath = InstallerIconPathIn;
			UninstallerIconPath = UninstallerIconPathIn;

			ShowWelcomePage = ShowWelcomePageIn;
			LicenseDialogDetailsUsed = LicenseDialogDetailsUsedIn;

			ShowComponentsPage = ShowComponentsPageIn;
			ShowDirectoryPage = ShowDirectoryPageIn;
			UserMayChangeStartMenuName = UserMayChangeStartMenuNameIn;

			FilePathToRunOnFinish = FilePathToRunOnFinishIn;
			InstTypes = InstTypesIn;
			StartmenuFolderName = StartmenuFolderNameIn;

			DotnetFrameworkTargeted = DotnetFrameworkTargetedIn;
			_64Only = _64OnlyIn;

			InstallForAllUsers = InstallForAllUsersIn;
		}

		private enum SetShellVarContext { all, current };

		public enum LanguagesEnum : long
		{
			Afrikaans = 0x00000001,
			Albanian = 0x00000002,
			Arabic = 0x00000004,
			Basque = 0x00000008,
			Belarusian = 0x00000010,
			Bosnian = 0x00000020,
			Breton = 0x00000040,
			Bulgarian = 0x00000080,
			Catalan = 0x00000100,
			Croatian = 0x00000200,
			Czech = 0x00000400,
			Danish = 0x00000800,
			Dutch = 0x00001000,
			English = 0x00002000,
			Esperanto = 0x00004000,
			Estonian = 0x00008000,
			Farsi = 0x00010000,
			Finnish = 0x00020000,
			French = 0x00040000,
			Galician = 0x00080000,
			German = 0x00100000,
			Greek = 0x00200000,
			Hebrew = 0x00400000,
			Hungarian = 0x00800000,
			Icelandic = 0x01000000,
			Indonesian = 0x02000000,
			Irish = 0x04000000,
			Italian = 0x08000000,
			Japanese = 0x10000000,
			Korean = 0x20000000,
			Kurdish = 0x40000000,
			Latvian = 0x80000000,
			Lithuanian = 0x100000000,
			Luxembourgish = 0x200000000,
			Macedonian = 0x400000000,
			Malay = 0x800000000,
			Mongolian = 0x1000000000,
			Norwegian = 0x2000000000,
			NorwegianNynorsk = 0x4000000000,
			Polish = 0x8000000000,
			Portuguese = 0x10000000000,
			PortugueseBR = 0x20000000000,
			Romanian = 0x40000000000,
			Russian = 0x80000000000,
			Serbian = 0x100000000000,
			SerbianLatin = 0x200000000000,
			SimpChinese = 0x400000000000,
			Slovak = 0x800000000000,
			Slovenian = 0x1000000000000,
			Spanish = 0x2000000000000,
			SpanishInternational = 0x4000000000000,
			Swedish = 0x8000000000000,
			Thai = 0x10000000000000,
			TradChinese = 0x20000000000000,
			Turkish = 0x40000000000000,
			Ukrainian = 0x80000000000000,
			Uzbek = 0x100000000000000,
			Welsh = 0x200000000000000,
		}

		public class Compressor
		{
			public enum CompressionModeEnum { zlib, bzip2, lzma };

			public CompressionModeEnum CompressionMode;
			public Boolean Final;
			public Boolean Solid;

			public Compressor()
			{
				CompressionMode = CompressionModeEnum.bzip2;
				Final = false;
				Solid = false;
			}

			public Compressor(CompressionModeEnum CompressionModeIn, Boolean FinalIn, Boolean SolidIn)
			{
				CompressionMode = CompressionModeIn;
				Final = FinalIn;
				Solid = SolidIn;
			}
		}

		public class LicensePageDetails
		{
			public enum AcceptWith { Checkbox, Radiobuttons, Classic };

			public Boolean ShowLicensePage;
			public string LicenseFilePath;
			public AcceptWith acceptWith;

			public LicensePageDetails(Boolean ShowLicensePageIn, string LicenseFilePathIn = "", AcceptWith acceptWithIn = AcceptWith.Radiobuttons)
			{
				if (ShowLicensePageIn)
				{
					ShowLicensePage = ShowLicensePageIn;
					LicenseFilePath = LicenseFilePathIn;
					acceptWith = acceptWithIn;
				}
			}
		}

		public List<string> GetAllLinesForNSISfile(List<string> AllSectionGroupLines, List<string> AllSectionAndGroupDescriptions, bool UninstallWillDeleteProgramAutoRunInRegistry_CurrentUser, bool HasPlugins, RegistryInterop.MainContextMenuItem contextMenuItems)
		{
			List<string> tmpList = new List<string>();

			bool isAutoUpdater = ProductName.Replace(" ", "").Equals("AutoUpdater", StringComparison.InvariantCultureIgnoreCase);
			bool isShowNoCallbackNotification = ProductName.Replace(" ", "").Equals("ShowNoCallbackNotification", StringComparison.InvariantCultureIgnoreCase);
			bool isStandaloneUploader = ProductName.Replace(" ", "").Equals("StandaloneUploader", StringComparison.InvariantCultureIgnoreCase);

			tmpList.Add(@"; Script generated by the HM NIS Edit Script Wizard.");
			tmpList.Add("");
			tmpList.Add(@"; HM NIS Edit Wizard helper defines");
			tmpList.Add(@"!define PRODUCT_NAME """ + ProductName + @"""");
			tmpList.Add(@"!define PRODUCT_VERSION """ + ProductVersion + @"""");
			tmpList.Add(@"!define PRODUCT_PUBLISHER """ + ProductPublisher + @"""");
			tmpList.Add(@"!define PRODUCT_WEB_SITE """ + ProductWebsite + @"""");
			tmpList.Add(@"!define PRODUCT_DIR_REGKEY """ + @"Software\Microsoft\Windows\CurrentVersion\App Paths\" + ProductExeName + (ProductExeName.ToUpper().EndsWith(".EXE") ? "" : ".exe") + @"""");
			if (isShowNoCallbackNotification) tmpList.Add(@"!define PRODUCT_DIR_REGKEY_NOTIFICATIONSONLY """ + @"Software\Microsoft\Windows\CurrentVersion\App Paths\Notify.exe""");
			tmpList.Add(@"!define PRODUCT_EXE_NAME " + @"""" + ProductExeName + @"""");

			if (UseUninstaller)
			{
				tmpList.Add(@"!define PRODUCT_UNINST_KEY ""Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}""");
				tmpList.Add(@"!define PRODUCT_UNINST_ROOT_KEY ""HKLM""");
			}

			tmpList.Add(@"!define PRODUCT_STARTMENU_REGVAL ""NSIS:StartMenuDir""");
			tmpList.Add(@"!define PRODUCT_SETUP_FILENAME """ + (SetupFileName.ToLower().EndsWith(".exe") ? SetupFileName : SetupFileName + ".exe") + @"""");
			tmpList.Add("");

			tmpList.Add("Var /GLOBAL classesRootMainKey  ;used for reading Registry keys for install/uninstall");
			if (!isAutoUpdater && !isShowNoCallbackNotification && !isStandaloneUploader)
			{
				tmpList.Add("Var /GLOBAL autostartCheckbox");
				tmpList.Add("Var /GLOBAL state_autostartCheckbox");
			}
			tmpList.Add("");

			tmpList.Add(@";SetCompressor ""/SOLID"" lzma ;Seems to be using more space..");
			tmpList.Add(@"SetCompressor " + (CompressorUsed.Solid ? "/SOLID " : "") + (CompressorUsed.Final ? "/FINAL " : "") + CompressorUsed.CompressionMode.ToString());
			if (CompressorDictSizeMegabytes.HasValue)
				tmpList.Add(@"SetCompressorDictSize " + CompressorDictSizeMegabytes.Value.ToString());

			tmpList.Add("");
			tmpList.Add("BrandingText \"${PRODUCT_NAME} v${PRODUCT_VERSION} (NSIS 2.46)\"");
			tmpList.Add("");
			tmpList.Add(@"; MUI 1.67 compatible ------");
			tmpList.Add(@"!include ""MUI.nsh""");
			tmpList.Add("");
			tmpList.Add(@"; DotNetChecker checks and downloads dotnet version");
			tmpList.Add(@"!include ""DotNetChecker.nsh""");
			tmpList.Add("");

			tmpList.Add(";To use custom page with checkbox");
			tmpList.Add(@"!include ""nsDialogs.nsh""");
			tmpList.Add("");

			tmpList.Add(@"; MUI Settings");
			tmpList.Add(@"!define MUI_ABORTWARNING");
			tmpList.Add(@"!define MUI_ICON """ + InstallerIconPath + @"""");
			if (UseUninstaller) tmpList.Add(@"!define MUI_UNICON """ + UninstallerIconPath + @"""");
			tmpList.Add("");

			if (ShowWelcomePage)
			{
				tmpList.Add(@"; Welcome page");
				tmpList.Add(@"!insertmacro MUI_PAGE_WELCOME");
			}

			if (LicenseDialogDetailsUsed != null && LicenseDialogDetailsUsed.ShowLicensePage)
			{
				tmpList.Add(@"; License page");
				if (LicenseDialogDetailsUsed.acceptWith == LicensePageDetails.AcceptWith.Checkbox)
					tmpList.Add(@"!define MUI_LICENSEPAGE_CHECKBOX");
				else if (LicenseDialogDetailsUsed.acceptWith == LicensePageDetails.AcceptWith.Radiobuttons)
					tmpList.Add(@"!define MUI_LICENSEPAGE_RADIOBUTTONS");
				tmpList.Add(@"!insertmacro MUI_PAGE_LICENSE """ + LicenseDialogDetailsUsed.LicenseFilePath + @"""");
			}

			if (ShowComponentsPage)
			{
				tmpList.Add(@"; Components page");
				tmpList.Add(@"!insertmacro MUI_PAGE_COMPONENTS");
			}

			if (ShowDirectoryPage)
			{
				tmpList.Add(@"; Directory page");
				tmpList.Add(@"!insertmacro MUI_PAGE_DIRECTORY");
			}

			if (UserMayChangeStartMenuName)
			{
				tmpList.Add(@"; Start menu page");
				tmpList.Add(@"var ICONS_GROUP");
				tmpList.Add(@"!define MUI_STARTMENUPAGE_NODISABLE");
				tmpList.Add(@"!define MUI_STARTMENUPAGE_DEFAULTFOLDER """ + (StartmenuFolderName != null && StartmenuFolderName.Length > 0 ? StartmenuFolderName + "\\" : "") + @"${PRODUCT_NAME}""");
				tmpList.Add(@"!define MUI_STARTMENUPAGE_REGISTRY_ROOT ""${PRODUCT_UNINST_ROOT_KEY}""");
				tmpList.Add(@"!define MUI_STARTMENUPAGE_REGISTRY_KEY ""${PRODUCT_UNINST_KEY}""");
				tmpList.Add(@"!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME ""${PRODUCT_STARTMENU_REGVAL}""");
				tmpList.Add(@"!insertmacro MUI_PAGE_STARTMENU Application $ICONS_GROUP");
			}

			if (!isAutoUpdater && !isShowNoCallbackNotification && !isStandaloneUploader)
			{
				tmpList.Add(@";Checkbox if should autostart with windows");
				tmpList.Add(@"Function AutostartWithWindowsShow");
				tmpList.Add(@"		nsDialogs::Create /NOUNLOAD 1018");
				tmpList.Add(@"		${NSD_CreateCheckbox} 10u 10u 100% 10u ""&Startup with windows""");
				tmpList.Add(@"		Pop $autostartCheckbox");
				tmpList.Add(@"		${If} $state_autostartCheckbox <> 0");
				tmpList.Add(@"			  ${NSD_SetState} $autostartCheckbox 1");
				tmpList.Add(@"		${EndIf}");
				tmpList.Add(@"		SetCtlColors $autostartCheckbox "" """);
				tmpList.Add(@"		nsDialogs::Show");
				tmpList.Add(@"FunctionEnd");
				tmpList.Add(@"Function AutostartWithWindowsLeave");
				tmpList.Add(@"		${NSD_GetState} $autostartCheckbox $state_autostartCheckbox");
				tmpList.Add(@"FunctionEnd");
				tmpList.Add(@"Page custom AutostartWithWindowsShow AutostartWithWindowsLeave");
			}

			tmpList.Add(@"; Instfiles page");
			tmpList.Add(@"!insertmacro MUI_PAGE_INSTFILES");


			tmpList.Add(@"; Finish page");
			if (FilePathToRunOnFinish.Length > 0) tmpList.Add(@"!define MUI_FINISHPAGE_RUN ""$INSTDIR\${PRODUCT_EXE_NAME}""");
			tmpList.Add(@"!insertmacro MUI_PAGE_FINISH"); tmpList.Add("");

			if (UseUninstaller)
			{
				tmpList.Add(@"; Uninstaller pages");
				tmpList.Add(@"!insertmacro MUI_UNPAGE_INSTFILES"); tmpList.Add("");
			}

			String UsedLangueString = "";
			//String tmpStr = "";
			//foreach (String lang in Languages) tmpStr += @" """ + lang + @"""";
			tmpList.Add(@"; Language files");
			//tmpList.Add(@"!insertmacro MUI_LANGUAGE " + tmpStr);
			foreach (LanguagesEnum testLanguageFound in Enum.GetValues(typeof(LanguagesEnum)))
				if (SetupLanguage.HasFlag(testLanguageFound))
				{ tmpList.Add(@"!insertmacro MUI_LANGUAGE " + @"""" + testLanguageFound.ToString() + @""""); UsedLangueString = testLanguageFound.ToString(); break; }
			tmpList.Add("");
			tmpList.Add(@"; MUI end ------");
			tmpList.Add("");

			tmpList.Add(@"Name ""${PRODUCT_NAME} ${PRODUCT_VERSION}""");
			tmpList.Add(@"OutFile ""${PRODUCT_SETUP_FILENAME}""");

			//foreach (LanguagesEnum testLanguageFound in Enum.GetValues(typeof(LanguagesEnum)))
			//    if (Languages.HasFlag(testLanguageFound) && testLanguageFound.ToString().ToUpper() != UsedLangueString.ToUpper())
			//        tmpList.Add(@"LoadLanguageFile """ + @"${NSISDIR}\Contrib\Language files\" + testLanguageFound.ToString() + @".nlf""");

			tmpList.Add(@"InstallDir ""$PROGRAMFILES\${PRODUCT_NAME}""");
			tmpList.Add(@"InstallDirRegKey HKLM ""${PRODUCT_DIR_REGKEY}"" """"");
			tmpList.Add(@"ShowInstDetails show");
			if (UseUninstaller) tmpList.Add(@"ShowUnInstDetails show");
			tmpList.Add("");

			tmpList.Add(@"VIProductVersion ${PRODUCT_VERSION}");
			tmpList.Add(@"VIAddVersionKey ProductName ""${PRODUCT_NAME}""");
			tmpList.Add(@"VIAddVersionKey LegalCopyright francoishill.com");
			tmpList.Add(@"VIAddVersionKey FileDescription ""${PRODUCT_NAME} Application""");
			tmpList.Add(@"VIAddVersionKey FileVersion ${PRODUCT_VERSION}");
			tmpList.Add(@"VIAddVersionKey ProductVersion ${PRODUCT_VERSION}");
			tmpList.Add("");

			if (InstTypes != null)
				foreach (String instType in InstTypes)
					tmpList.Add(@"InstType """ + instType + @""""); tmpList.Add("");

			if (AllSectionGroupLines != null)
				foreach (string line in AllSectionGroupLines) tmpList.Add(line); tmpList.Add("");

			string SectionInAllInstTypes = "SectionIn";
			if (InstTypes != null)
				for (int i = 1; i <= InstTypes.Count; i++)
					SectionInAllInstTypes += " " + i.ToString();

			tmpList.Add(@"Section -AdditionalIcons");
			if (InstTypes != null && InstTypes.Count > 0) tmpList.Add(SectionInAllInstTypes);
			//tmpList.Add(Spacer + @"SetShellVarContext " + (InstallForAllUsers ? "all" : "current"));
			tmpList.Add(Spacer + @"SetShellVarContext " + "current");//Needs to be current user otherwise does not show in startmenu
			tmpList.Add(Spacer + @"!insertmacro MUI_STARTMENU_WRITE_BEGIN Application");
			if (ProductExeName.Length > 0) tmpList.Add(Spacer + @"CreateShortCut ""$SMPROGRAMS\$ICONS_GROUP\${PRODUCT_NAME}.lnk"" ""$INSTDIR\${PRODUCT_EXE_NAME}""");
			tmpList.Add(Spacer + @"WriteIniStr ""$INSTDIR\Website of ${PRODUCT_NAME}.url"" ""InternetShortcut"" ""URL"" ""${PRODUCT_WEB_SITE}""");
			if (ProductWebsite != null && ProductWebsite.Length > 0) tmpList.Add(Spacer + @"CreateShortCut ""$SMPROGRAMS\$ICONS_GROUP\Website of ${PRODUCT_NAME}.lnk"" ""$INSTDIR\Website of ${PRODUCT_NAME}.url"" """" ""$WINDIR\system32\SHELL32.dll"" 14");
			//tmpList.Add(Spacer + @"WriteIniStr ""$SMPROGRAMS\$ICONS_GROUP\${PRODUCT_NAME}.url"" ""InternetShortcut"" ""URL"" ""${PRODUCT_WEB_SITE}""");
			//tmpList.Add(Spacer + @"CreateShortCut ""$SMPROGRAMS\$ICONS_GROUP\${PRODUCT_NAME} website.lnk"" ""$INSTDIR\${PRODUCT_NAME}.url""");
			tmpList.Add(Spacer + @"CreateShortCut ""$SMPROGRAMS\$ICONS_GROUP\Uninstall ${PRODUCT_NAME}.lnk"" ""$INSTDIR\Uninstall ${PRODUCT_NAME}.exe""");
			tmpList.Add(Spacer + @"!insertmacro MUI_STARTMENU_WRITE_END");
			tmpList.Add(@"SectionEnd"); tmpList.Add("");

			tmpList.Add(@"Section -Post");
			if (InstTypes != null && InstTypes.Count > 0) tmpList.Add(SectionInAllInstTypes);
			tmpList.Add(Spacer + @"SetShellVarContext " + (InstallForAllUsers ? "all" : "current"));

			tmpList.Add(Spacer + ";Rewrite version file");
			tmpList.Add(Spacer + @"FileOpen $9 ""$INSTDIR\${PRODUCT_EXE_NAME}.version"" w ;Opens a Empty File an fills it");
			tmpList.Add(Spacer + @"FileWrite $9 ""${PRODUCT_VERSION}""");
			tmpList.Add(Spacer + @"FileClose $9 ;Closes the filled file");
			tmpList.Add(Spacer + ";Write application information to Registry");
			tmpList.Add(Spacer + @"WriteUninstaller ""$INSTDIR\Uninstall ${PRODUCT_NAME}.exe""");
			tmpList.Add(Spacer + @"WriteRegStr HKLM ""${PRODUCT_DIR_REGKEY}"" """" ""$INSTDIR\${PRODUCT_EXE_NAME}""");
			if (isShowNoCallbackNotification) tmpList.Add(Spacer + @"WriteRegStr HKLM ""${PRODUCT_DIR_REGKEY_NOTIFICATIONSONLY}"" """" ""$INSTDIR\${PRODUCT_EXE_NAME}""");
			tmpList.Add(Spacer + @"WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} ""${PRODUCT_UNINST_KEY}"" ""DisplayName"" ""$(^Name)""");
			tmpList.Add(Spacer + @"WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} ""${PRODUCT_UNINST_KEY}"" ""UninstallString"" ""$INSTDIR\Uninstall ${PRODUCT_NAME}.exe""");
			tmpList.Add(Spacer + @"WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} ""${PRODUCT_UNINST_KEY}"" ""DisplayIcon"" ""$INSTDIR\${PRODUCT_EXE_NAME}""");
			tmpList.Add(Spacer + @"WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} ""${PRODUCT_UNINST_KEY}"" ""DisplayVersion"" ""${PRODUCT_VERSION}""");
			tmpList.Add(Spacer + @"WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} ""${PRODUCT_UNINST_KEY}"" ""URLInfoAbout"" ""${PRODUCT_WEB_SITE}""");
			tmpList.Add(Spacer + @"WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} ""${PRODUCT_UNINST_KEY}"" ""Publisher"" ""${PRODUCT_PUBLISHER}""");

			if (contextMenuItems != null)
				foreach (var regAssociatedRegistryline in contextMenuItems.GetRegistryAssociationNsisLines((err) => UserMessages.ShowErrorMessage(err)))
					tmpList.Add(Spacer + regAssociatedRegistryline.Replace("((EXEPATH))", "$INSTDIR\\${PRODUCT_EXE_NAME}"));

			tmpList.Add(@"SectionEnd"); tmpList.Add("");

			if (DotnetFrameworkTargeted != DotnetFrameworkTargetedEnum.None)
			{
				tmpList.Add(@"Section -DotNetFramework");
				if (DotnetFrameworkTargeted == DotnetFrameworkTargetedEnum.DotNet4full)
					tmpList.Add(Spacer + @"!insertmacro CheckNetFramework 40Full ; if your application targets .NET 4.0 Full Framework");
				if (DotnetFrameworkTargeted == DotnetFrameworkTargetedEnum.DotNet4client)
					tmpList.Add(Spacer + @"!insertmacro CheckNetFramework 40Client ; if your application targets .NET 4.0 ClientOnServerSide Framework");
				if (DotnetFrameworkTargeted == DotnetFrameworkTargetedEnum.DotNet3_5)
					tmpList.Add(Spacer + @"!insertmacro CheckNetFramework 35 ; if your application targets .NET 3.5 Framework");
				if (DotnetFrameworkTargeted == DotnetFrameworkTargetedEnum.DotNet3_0)
					tmpList.Add(Spacer + @"!insertmacro CheckNetFramework 30 ; if your application targets .NET 3.0 Framework");
				if (DotnetFrameworkTargeted == DotnetFrameworkTargetedEnum.DotNet2_0)
					tmpList.Add(Spacer + @"!insertmacro CheckNetFramework 20 ; if your application targets .NET 2.0 Framework");
				if (DotnetFrameworkTargeted == DotnetFrameworkTargetedEnum.DotNet1_1)
					tmpList.Add(Spacer + @"!insertmacro CheckNetFramework 11 ; if your application targets .NET 1.1 Framework");
				if (DotnetFrameworkTargeted == DotnetFrameworkTargetedEnum.DotNet1_0)
					tmpList.Add(Spacer + @"!insertmacro CheckNetFramework 10 ; if your application targets .NET 1.0 Framework");
				tmpList.Add(@"SectionEnd"); tmpList.Add("");
			}

			//TODO: Should look at incorporating the kill process plugin to ask user weather to kill process or quit
			tmpList.Add(";Section -CheckMutexOpen");
			tmpList.Add(Spacer + ";System::Call 'kernel32::OpenMutex(i 0x100000, b 0, buildTask \"QuickAccess-{6EBAC5AC-BCF2-4263-A82C-F189930AEA30}\") i .R0'");
			tmpList.Add(Spacer + ";IntCmp $R0 0 notRunning");
			tmpList.Add(Spacer + ";System::Call 'kernel32::CloseHandle(i $R0)'");
			tmpList.Add(Spacer + ";MessageBox MB_YESNO|MB_ICONQUESTION \"QuickAccess is running. Please close it first then click YES.\" IDYES tryAgain");
			tmpList.Add(Spacer + ";Abort");
			tmpList.Add(Spacer + ";tryAgain:");
			tmpList.Add(Spacer + ";notRunning:");
			tmpList.Add(";SectionEnd");

			if (AllSectionAndGroupDescriptions != null && AllSectionAndGroupDescriptions.Count > 0)
			{
				tmpList.Add(@"; Section descriptions");
				tmpList.Add(@"!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN");
				foreach (string line in AllSectionAndGroupDescriptions) tmpList.Add(Spacer + line);
				tmpList.Add(@"!insertmacro MUI_FUNCTION_DESCRIPTION_END"); tmpList.Add("");
			}

			tmpList.Add(@"Function un.onUninstSuccess");
			tmpList.Add(Spacer + @"HideWindow");
			tmpList.Add(Spacer + @"MessageBox MB_ICONINFORMATION|MB_OK ""$(^Name) was successfully removed from your computer.""");
			tmpList.Add(@"FunctionEnd"); tmpList.Add("");

			tmpList.Add(@"Function un.onInit");
			tmpList.Add(Spacer + "${If} ${RunningX64}");
			tmpList.Add(Spacer + Spacer + "SetRegView 64");
			tmpList.Add(Spacer + "${EndIf}");
			tmpList.Add(Spacer + @"MessageBox MB_ICONQUESTION|MB_YESNO|MB_DEFBUTTON2 ""Are you sure you want to completely remove $(^Name) and all of its components?"" IDYES +2");
			tmpList.Add(Spacer + @"Abort");
			tmpList.Add(@"FunctionEnd"); tmpList.Add("");

			tmpList.Add(@"Section Uninstall");
			//tmpList.Add(Spacer + @"Delete ""$INSTDIR\${PRODUCT_NAME}.url""");
			//tmpList.Add(Spacer + @"Delete ""$INSTDIR\uninst.exe""");
			//tmpList.Add(Spacer + @"Delete ""$INSTDIR\${PRODUCT_EXE_NAME}"""); tmpList.Add("");

			tmpList.Add(Spacer + @"Delete ""$INSTDIR\*.*""");
			if (HasPlugins) tmpList.Add(Spacer + @"Delete ""$INSTDIR\Plugins\*.*""");
			tmpList.Add("");

			tmpList.Add(Spacer + @"SetShellVarContext all");
			tmpList.Add(Spacer + @"!insertmacro MUI_STARTMENU_GETFOLDER ""Application"" $ICONS_GROUP");
			tmpList.Add(Spacer + @"Delete ""$SMPROGRAMS\$ICONS_GROUP\*.*""");
			tmpList.Add(Spacer + @"Delete ""$DESKTOP\${PRODUCT_NAME}.lnk""");
			tmpList.Add(Spacer + @"RMDir ""$SMPROGRAMS\$ICONS_GROUP"""); tmpList.Add("");

			tmpList.Add(Spacer + @"SetShellVarContext current");
			tmpList.Add(Spacer + @"!insertmacro MUI_STARTMENU_GETFOLDER ""Application"" $ICONS_GROUP");
			tmpList.Add(Spacer + @"Delete ""$SMPROGRAMS\$ICONS_GROUP\*.*""");
			tmpList.Add(Spacer + @"Delete ""$DESKTOP\${PRODUCT_NAME}.lnk""");
			tmpList.Add(Spacer + @"RMDir ""$SMPROGRAMS\$ICONS_GROUP"""); tmpList.Add("");

			if (HasPlugins) tmpList.Add(Spacer + @"RMDir ""$INSTDIR\Plugins""");
			tmpList.Add(Spacer + @"RMDir ""$INSTDIR"""); tmpList.Add("");

			tmpList.Add(Spacer + @"DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} ""${PRODUCT_UNINST_KEY}""");
			tmpList.Add(Spacer + @"DeleteRegKey HKLM ""${PRODUCT_DIR_REGKEY}""");
			if (isShowNoCallbackNotification) tmpList.Add(Spacer + @"DeleteRegKey HKLM ""${PRODUCT_DIR_REGKEY_NOTIFICATIONSONLY}""");
			//if (UninstallWillDeleteProgramAutoRunInRegistry_CurrentUser)
			tmpList.Add(Spacer + @"DeleteRegValue HKCU ""SOFTWARE\Microsoft\Windows\CurrentVersion\Run"" '${PRODUCT_NAME}'");

			if (contextMenuItems != null)
				foreach (var regUnassociatedRegistryline in contextMenuItems.GetRegistryUnassociationNsisLines())
					tmpList.Add(Spacer + regUnassociatedRegistryline);

			tmpList.Add(Spacer + @"SetAutoClose true");
			tmpList.Add(@"SectionEnd");

			return tmpList;
		}

		public class SectionGroupClass
		{
			//public List<string> FullTextBlock;
			public string Name;
			public string Description;
			public string IDString = "";
			public Boolean UninstallerGroup;
			public Boolean DisplayInBold;
			public Boolean ExpandedByDefault;

			public SectionGroupClass() { }

			public SectionGroupClass(string NameIn, string DescriptionIn, Boolean UninstallerGroupIn, Boolean DisplayInBoldIn = true, Boolean ExpandedByDefaultIn = true)//, List<SectionGroupDetails> SectionGroupsIn, List<SectionDetails> SectionsIn)
			{
				Name = NameIn;
				Description = DescriptionIn;
				//IDString = IDStringIn;
				UninstallerGroup = UninstallerGroupIn;
				DisplayInBold = DisplayInBoldIn;
				ExpandedByDefault = ExpandedByDefaultIn;
			}

			public class SectionClass
			{
				public enum SetOverwriteEnum { on, off, Try, ifnewer, ifdiff };

				public string SectionName;
				public string SectionDescription;
				public string IDString = "";
				public string InstTypes_CommaSeperated;
				public SetOverwriteEnum SetOverwrite;
				public string SetOutPath;
				public readonly string EndLine = "SectionEnd";
				public Boolean UnselectedByDefault;
				public Boolean HiddenToUser;
				public Boolean SectionForUninstaller;
				public Boolean DisplaySectionWithBoldFont;
				public int ReserveDiskspaceForSection;

				public SectionClass() { }

				public SectionClass(
						string SectionNameIn,//
						string SectionDescriptionIn,
						string InstTypes_CommaSeperatedIn,
						SetOverwriteEnum SetOverwriteIn = SetOverwriteEnum.ifnewer,
						string SetOutPathIn = "$INSTDIR",
						Boolean UnselectedByDefaultIn = false,//
						Boolean HiddenToUserIn = false, Boolean SectionForUninstallerIn = false,//
						Boolean DisplaySectionWithBoldFontIn = false,//
						int ReserveDiskspaceForSectionIn = 0)
				{
					SectionName = SectionNameIn;
					SectionDescription = SectionDescriptionIn;
					InstTypes_CommaSeperated = InstTypes_CommaSeperatedIn;
					SetOverwrite = SetOverwriteIn;
					ReserveDiskspaceForSection = ReserveDiskspaceForSectionIn;

					SetOutPath = SetOutPathIn;
				}

				public string HeaderLine
				{
					get
					{
						return
								"Section" +
									 (UnselectedByDefault ? " /o" : "") + " \"" + (DisplaySectionWithBoldFont ? "!" : "") +
									 (HiddenToUser ? "-" : "") + (SectionForUninstaller ? "un." : "") +
									 SectionName + "\"" +
									 " " + IDString;// +
					}
				}

				private String RemoveQuoteChars(String InputString)
				{
					String tmpStr = InputString;
					if (tmpStr.StartsWith("\"")) tmpStr = tmpStr.Remove(0, 1);
					if (tmpStr.EndsWith("\"")) tmpStr = tmpStr.Substring(0, tmpStr.Length - 1);
					return tmpStr;
				}

				public class FileToAddTextblock
				{
					public enum ExecuteModeEnum { None, NormalNotDelete, NormalDoDelete, QuietNotDelete, QuietDoDelete };

					public string SetOutPath;
					public SetOverwriteEnum SetOverwrite;
					public ExecuteModeEnum ExecuteMode;
					public string FileNameOnly;
					public Boolean HideDetailsPrint;
					public string DetailPrint;

					//public FileToAddTextblock() { }

					/// <summary>
					/// Details for each file (directory) that will be included in the installation package
					/// </summary>
					/// <param name="SetShellVarContextIn">Must the file/dir be installed for all users or only current</param>
					/// <param name="SetOutPathIn">What is the destination directory for the file(s)  (can be built from enum DirectoryVariables)</param>
					/// <param name="SetOverwriteIn">How must overwrite be handled if the file exists in the destination directory</param>
					/// <param name="FileStrIn">The full file string remember /oname=X to specify different output name than original(File /r /x *.svn* /x *.tmp "..\..\..\Binaries\Win32\SRMS\Latest\GLS Shared\*.*")</param>
					/// <param name="ExecuteModeIn">If the file must be runned after installing to destination</param>
					/// <param name="SetDetailsPrintIn">Whether details must be hidden when copying or running the file</param>
					/// <param name="DetailPrintIn">The message to display to the user when copying or running the file</param>
					public FileToAddTextblock(//List<FileToAddLine> FileLineStringListIn,
							string SetOutPathIn = "$INSTDIR", SetOverwriteEnum SetOverwriteIn = SetOverwriteEnum.ifnewer,
							ExecuteModeEnum ExecuteModeIn = ExecuteModeEnum.None, string FileNameOnlyIn = "",
							Boolean HideDetailsPrintIn = false, string DetailPrintIn = "")
					{
						//FileLineStringList = FileLineStringListIn;
						SetOutPath = SetOutPathIn;
						SetOverwrite = SetOverwriteIn;
						ExecuteMode = ExecuteModeIn;
						FileNameOnly = FileNameOnlyIn;
						HideDetailsPrint = HideDetailsPrintIn;
						DetailPrint = DetailPrintIn;

						//TextBlockForFile = new List<string>();
					}

					public class FileOrDirToAddLine
					{
						public FileOrDirToAddLine() { }
						////public string FullLineString = "";
						//public string FilePath;
						//public Boolean PreserverAttributes;
						//public string RenameFileName;
						////public string DirPath;
						////public Boolean Recursive;
						////public string ExclusionList_CommaSeperated;
						////public Boolean FileTrue_DirectoryFalse;

						//public FileOrDirToAddLine() { }

						//public FileOrDirToAddLine(string FilePathIn, Boolean PreserverAttributesIn, string RenameFileNameIn = "")
						//{
						//    if (!File.Exists(FilePathIn)) MessageBox.Show("File does not exist: " + FilePathIn, "File not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						//    FilePath = FilePathIn;
						//    PreserverAttributes = PreserverAttributesIn;
						//    RenameFileName = RenameFileNameIn;
						//    //FileTrue_DirectoryFalse = true;
						//}

						////public FileToAddLine(string DirPathIn, string ExclusionList_CommaSeperatedIn, Boolean RecursiveIn)
						////{
						////    if (!Directory.Exists(DirPathIn)) MessageBox.Show("Directory does not exist: " + DirPathIn, "Dir not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						////    DirPath = DirPathIn;
						////    ExclusionList_CommaSeperated = ExclusionList_CommaSeperatedIn;
						////    Recursive = RecursiveIn;
						////    FileTrue_DirectoryFalse = false;
						////}

						//public string FullLineString
						//{
						//    get
						//    {
						//        //string tmpExclusionString = "";
						//        //if (ExclusionList_CommaSeperated != null && ExclusionList_CommaSeperated.Length > 0) foreach (string s in ExclusionList_CommaSeperated.Split(',')) tmpExclusionString += "/x " + s.Trim() + " ";

						//        return
						//            //FileTrue_DirectoryFalse ?
						//            "File " + (PreserverAttributes ? "/a " : "") + (RenameFileName.Length > 0 ? @"""" + "/oname=" + RenameFileName + @""" " : "") + @"""" + FilePath + @"""";
						//          //  :
						//          //  "File " +
						//          //(Recursive ? "/r " : "") +
						//          //tmpExclusionString +
						//          //@"""" + DirPath + @"""";
						//    }
						//}

						public class FileToAddLine : FileOrDirToAddLine
						{
							//public string FullLineString = "";
							public string FilePath;
							public Boolean PreserverAttributes;
							public string RenameFileName;
							//public string DirPath;
							//public Boolean Recursive;
							//public string ExclusionList_CommaSeperated;
							//public Boolean FileTrue_DirectoryFalse;

							public FileToAddLine() { }

							public FileToAddLine(string FilePathIn, Boolean PreserverAttributesIn = true, string RenameFileNameIn = "")
							{
								if (!File.Exists(FilePathIn)) MessageBox.Show("File does not exist: " + FilePathIn, "File not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
								FilePath = FilePathIn;
								PreserverAttributes = PreserverAttributesIn;
								RenameFileName = RenameFileNameIn;
								//FileTrue_DirectoryFalse = true;
							}

							//public FileToAddLine(string DirPathIn, string ExclusionList_CommaSeperatedIn, Boolean RecursiveIn)
							//{
							//    if (!Directory.Exists(DirPathIn)) MessageBox.Show("Directory does not exist: " + DirPathIn, "Dir not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
							//    DirPath = DirPathIn;
							//    ExclusionList_CommaSeperated = ExclusionList_CommaSeperatedIn;
							//    Recursive = RecursiveIn;
							//    FileTrue_DirectoryFalse = false;
							//}

							public string FullLineString
							{
								get
								{
									//string tmpExclusionString = "";
									//if (ExclusionList_CommaSeperated != null && ExclusionList_CommaSeperated.Length > 0) foreach (string s in ExclusionList_CommaSeperated.Split(',')) tmpExclusionString += "/x " + s.Trim() + " ";

									return
										//FileTrue_DirectoryFalse ?
											"File " + (PreserverAttributes ? "/a " : "") + (RenameFileName.Length > 0 ? @"""" + "/oname=" + RenameFileName + @""" " : "") + @"""" + FilePath + @"""";
									//  :
									//  "File " +
									//(Recursive ? "/r " : "") +
									//tmpExclusionString +
									//@"""" + DirPath + @"""";
								}
							}
						}

						public class DirToAddLine : FileOrDirToAddLine
						{
							//public string FullLineString = "";
							//public string FilePath;
							//public Boolean PreserverAttributes;
							//public string RenameFileName;
							public string DirPath;
							public Boolean Recursive;
							public string ExclusionList_CommaSeperated;
							//public Boolean FileTrue_DirectoryFalse;

							public DirToAddLine() { }

							//public DirToAddLine(string FilePathIn, Boolean PreserverAttributesIn, string RenameFileNameIn = "")
							//{
							//    if (!File.Exists(FilePathIn)) MessageBox.Show("File does not exist: " + FilePathIn, "File not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
							//    FilePath = FilePathIn;
							//    PreserverAttributes = PreserverAttributesIn;
							//    RenameFileName = RenameFileNameIn;
							//    FileTrue_DirectoryFalse = true;
							//}

							public DirToAddLine(string DirPathIn, string ExclusionList_CommaSeperatedIn, Boolean RecursiveIn)
							{
								if (!Directory.Exists(DirPathIn)) MessageBox.Show("Directory does not exist: " + DirPathIn, "Dir not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
								DirPath = DirPathIn;
								ExclusionList_CommaSeperated = ExclusionList_CommaSeperatedIn;
								Recursive = RecursiveIn;
								//FileTrue_DirectoryFalse = false;
							}

							public string FullLineString
							{
								get
								{
									string tmpExclusionString = "";
									if (ExclusionList_CommaSeperated != null && ExclusionList_CommaSeperated.Length > 0) foreach (string s in ExclusionList_CommaSeperated.Split(',')) tmpExclusionString += "/x " + s.Trim() + " ";

									return
										//FileTrue_DirectoryFalse ?
										//"File " + (PreserverAttributes ? "/a " : "") + (RenameFileName.Length > 0 ? @"""" + "/oname=" + RenameFileName + @""" " : "") + @"""" + FilePath + @""""
										//:
											"File " +
										(Recursive ? "/r " : "") +
										tmpExclusionString +
										@"""" + DirPath + @"""";
								}
							}
						}
					}

					public override string ToString()
					{
						return SetOutPath;
					}
				}

				public class ShortcutDetails
				{
					public string RelativeShortcutPath;
					public string FullFileLocation;

					public ShortcutDetails() { }

					/// <summary>
					/// Require only the full path of the shortcut .lnk file; and the (installed) file location (can be built from enum DirectoryVariables)
					/// </summary>
					/// <param name="RelativeShortcutPathIn">The full path of the shortcut .lnk file (can be built from enum DirectoryVariables)</param>
					/// <param name="FullFileLocationIn">The file location for which the shorcut is created (can be built from enum DirectoryVariables)</param>
					public ShortcutDetails(string RelativeShortcutPathIn, string FullFileLocationIn)
					{
						RelativeShortcutPath = RelativeShortcutPathIn;
						FullFileLocation = FullFileLocationIn;
					}

					public string FullShortcutLine
					{
						get
						{
							return
									"CreateShortCut " + @"""" + @"$SMPROGRAMS\$ICONS_GROUP\" + RelativeShortcutPath + @"""" + (FullFileLocation.Length > 0 ? @" """ + FullFileLocation + @"""" : "");
						}
					}

					public override string ToString()
					{
						return FullShortcutLine;
					}
				}
			}
		}

		public static List<string> GetDescriptionOfNode(TreeNode NodeIn)
		{
			List<string> tmpList = new List<string>();

			if (NodeIn.Tag is NSISclass.SectionGroupClass)
			{
				tmpList.Add(@"!insertmacro MUI_DESCRIPTION_TEXT ${" + (NodeIn.Tag as NSISclass.SectionGroupClass).IDString + @"} """ + (NodeIn.Tag as NSISclass.SectionGroupClass).Description + @"""");
				foreach (TreeNode subnode in NodeIn.Nodes)
					foreach (string line in GetDescriptionOfNode(subnode))
						tmpList.Add(line);
			}
			if (NodeIn.Tag is NSISclass.SectionGroupClass.SectionClass)
			{
				tmpList.Add(@"!insertmacro MUI_DESCRIPTION_TEXT ${" + (NodeIn.Tag as NSISclass.SectionGroupClass.SectionClass).IDString + @"} """ + (NodeIn.Tag as NSISclass.SectionGroupClass.SectionClass).SectionDescription + @"""");
			}

			return tmpList;
			//tmpList.Add(Spacer + @"!insertmacro MUI_DESCRIPTION_TEXT ${GRP01} ""All components of the program""");
		}

		public static List<string> GetStringListOfNode(TreeNode NodeIn)
		{
			List<string> tmpList = new List<string>();

			string LevelSpaces = "";
			for (int i = 0; i < NodeIn.Level; i++) LevelSpaces += Spacer;
			string HeaderLevelSpaces = LevelSpaces.Length > 0 ? LevelSpaces.Substring(Spacer.Length) : "";

			if (NodeIn.Tag is NSISclass)
			{
				List<string> tmpSectionGroupLines = new List<string>();
				foreach (TreeNode subnode in NodeIn.Nodes)
					foreach (string line in GetStringListOfNode(subnode))
						tmpSectionGroupLines.Add(line);

				List<string> tmpSectionDescriptionLines = new List<string>();
				foreach (TreeNode subnode in NodeIn.Nodes)
					foreach (string line in GetDescriptionOfNode(subnode))
						tmpSectionDescriptionLines.Add(line);

				return ((NSISclass)NodeIn.Tag).GetAllLinesForNSISfile(tmpSectionGroupLines, tmpSectionDescriptionLines, false, false, null);

				//foreach (string line in ((NSISclass)NodeIn.Tag).GetAllLinesForNSISfile(tmpSectionGroupLines, tmpSectionDescriptionLines))
				//    textBox_CURRENTNODETEXTBLOCK.Text += (textBox_CURRENTNODETEXTBLOCK.Text.Length > 0 ? Environment.NewLine : "") + line;
				//listBox1.Items.Add(line);
			}
			else if (NodeIn.Tag is NSISclass.SectionGroupClass)
			{
				NSISclass.SectionGroupClass group = (NSISclass.SectionGroupClass)NodeIn.Tag;
				tmpList.Add(HeaderLevelSpaces + "SectionGroup " + (group.ExpandedByDefault ? "/e " : "") + @"""" + (group.DisplayInBold ? "!" : "") + (group.UninstallerGroup ? "un." : "") + group.Name + @""" " + group.IDString);
				foreach (TreeNode subnode in NodeIn.Nodes) foreach (string subnodeLine in GetStringListOfNode(subnode)) tmpList.Add(LevelSpaces + subnodeLine);
				tmpList.Add(HeaderLevelSpaces + "SectionGroupEnd");
			}
			else if (NodeIn.Tag is NSISclass.SectionGroupClass.SectionClass)
			{
				NSISclass.SectionGroupClass.SectionClass section = (NSISclass.SectionGroupClass.SectionClass)NodeIn.Tag;
				//tmpList.Add(section.HeaderLine);
				//foreach (string line in section.SectionTextBlock) tmpList.Add(line);
				Boolean tmpInstallforAllUsers = true;
				if (GetMainParentOfNode(NodeIn).Tag is NSISclass) tmpInstallforAllUsers = ((NSISclass)GetMainParentOfNode(NodeIn).Tag).InstallForAllUsers;

				string SectionInInstTypes = "";
				if (section.InstTypes_CommaSeperated != null && section.InstTypes_CommaSeperated.Length > 0)
				{
					SectionInInstTypes = "SectionIn";
					foreach (string insttype in section.InstTypes_CommaSeperated.Split(','))
						SectionInInstTypes += " " + ((GetMainParentOfNode(NodeIn).Tag as NSISclass).InstTypes.IndexOf(insttype) + 1);
				}

				tmpList.Add(HeaderLevelSpaces + section.HeaderLine);
				if (section.InstTypes_CommaSeperated != null && section.InstTypes_CommaSeperated.Length > 0)
					tmpList.Add(LevelSpaces + SectionInInstTypes);
				tmpList.Add(LevelSpaces + @"SetShellVarContext " + (tmpInstallforAllUsers ? "all" : "current"));
				tmpList.Add(LevelSpaces + "SetOverwrite " + section.SetOverwrite.ToString().ToLower());
				if (section.SetOutPath != null && section.SetOutPath.Length > 0) tmpList.Add(LevelSpaces + "SetOutPath " + @"""" + section.SetOutPath + @"""");
				else MessageBox.Show("SetOutPath must be set for section: " + section.SectionName, "No SetOutPath", MessageBoxButtons.OK, MessageBoxIcon.Error);

				if (section.ReserveDiskspaceForSection > 0) tmpList.Add(LevelSpaces + "AddSize " + section.ReserveDiskspaceForSection.ToString());

				List<TreeNode> FileNodes = new List<TreeNode>();
				List<TreeNode> ShortcutNodes = new List<TreeNode>();
				List<TreeNode> OtherNodes = new List<TreeNode>();
				foreach (TreeNode subnode in NodeIn.Nodes)
					if (subnode.Tag is FileToAddTextblock) FileNodes.Add(subnode);
					else if (subnode.Tag is ShortcutDetails) ShortcutNodes.Add(subnode);
					else OtherNodes.Add(subnode);

				if (FileNodes.Count > 0) tmpList.Add("");
				foreach (TreeNode node in FileNodes)
					foreach (string nodeLine in GetStringListOfNode(node)) tmpList.Add(LevelSpaces + nodeLine);

				if (ShortcutNodes.Count > 0) tmpList.Add("");
				foreach (TreeNode node in ShortcutNodes)
					foreach (string nodeLine in GetStringListOfNode(node)) tmpList.Add(LevelSpaces + nodeLine);

				if (OtherNodes.Count > 0) tmpList.Add("");
				foreach (TreeNode node in OtherNodes)
					foreach (string nodeLine in GetStringListOfNode(node)) tmpList.Add(LevelSpaces + nodeLine);

				if (FileNodes.Count > 0 || ShortcutNodes.Count > 0 || OtherNodes.Count > 0) tmpList.Add("");
				tmpList.Add(HeaderLevelSpaces + "SectionEnd");
			}
			else if (NodeIn.Tag is FileToAddTextblock)
			{
				FileToAddTextblock fileToAdd = NodeIn.Tag as FileToAddTextblock;
				//TextBlockForFile = new List<string>();
				if (fileToAdd.SetOutPath.Length > 0) tmpList.Add(LevelSpaces + "SetOutPath " + @"""" + fileToAdd.SetOutPath + @"""");
				tmpList.Add(LevelSpaces + "SetOverwrite " + fileToAdd.SetOverwrite.ToString());

				if (fileToAdd.DetailPrint.Length > 0) tmpList.Add(LevelSpaces + "DetailPrint '" + fileToAdd.DetailPrint + "'");
				if (fileToAdd.HideDetailsPrint) tmpList.Add(LevelSpaces + "SetDetailsPrint none");

				//foreach (FileToAddLine fileline in fileToAdd.FileLineStringList)
				//    if (fileline.FullLineString.Length > 0) TextBlockForFile.Add(fileline.FullLineString);
				foreach (TreeNode subnode in NodeIn.Nodes) foreach (string subnodeLine in GetStringListOfNode(subnode)) tmpList.Add(LevelSpaces + subnodeLine);
				//foreach (TreeNode subnode in NodeIn.Nodes) GetStringListOfNode(subnode);
				//if (subSubnode.Tag is FileToAddTextblock.FileToAddLine)
				//{
				//    FileToAddTextblock.FileToAddLine fileToAddLine = subSubnode.Tag as FileToAddTextblock.FileToAddLine;
				//    if (fileToAddLine.FullLineString.Length > 0) tmpList.Add(fileToAddLine.FullLineString);
				//}

				if (fileToAdd.ExecuteMode != FileToAddTextblock.ExecuteModeEnum.None)
				{
					if (fileToAdd.FileNameOnly.Length > 0)
					{
						if (fileToAdd.ExecuteMode == FileToAddTextblock.ExecuteModeEnum.NormalDoDelete || fileToAdd.ExecuteMode == FileToAddTextblock.ExecuteModeEnum.NormalNotDelete)
							tmpList.Add(LevelSpaces + "ExecWait '" + fileToAdd.FileNameOnly + "'");
						if (fileToAdd.ExecuteMode == FileToAddTextblock.ExecuteModeEnum.QuietDoDelete || fileToAdd.ExecuteMode == FileToAddTextblock.ExecuteModeEnum.QuietNotDelete)
							tmpList.Add(LevelSpaces + "ExecWait '" + fileToAdd.FileNameOnly + "' /q");
						if (fileToAdd.ExecuteMode == FileToAddTextblock.ExecuteModeEnum.NormalDoDelete || fileToAdd.ExecuteMode == FileToAddTextblock.ExecuteModeEnum.QuietDoDelete)
							tmpList.Add(LevelSpaces + "Delete \"" + fileToAdd.SetOutPath + "\\" + fileToAdd.FileNameOnly + "\"");
					}
					else MessageBox.Show("FileNameOnly must be set to use ExecuteMode", "No FileName", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}

				if (fileToAdd.HideDetailsPrint) tmpList.Add(LevelSpaces + "SetDetailsPrint both");
			}
			else if (NodeIn.Tag is FileToAddTextblock.FileOrDirToAddLine)
			{
				//FileToAddTextblock.FileOrDirToAddLine fileToAddLine;
				if (NodeIn.Tag is FileToAddTextblock.FileOrDirToAddLine.FileToAddLine)
				{
					//fileToAddLine = NodeIn.Tag as FileToAddTextblock.FileOrDirToAddLine;
					if ((NodeIn.Tag as FileToAddTextblock.FileOrDirToAddLine.FileToAddLine).FullLineString.Length > 0) tmpList.Add(LevelSpaces + (NodeIn.Tag as FileToAddTextblock.FileOrDirToAddLine.FileToAddLine).FullLineString);
				}
				else
				{
					if ((NodeIn.Tag as FileToAddTextblock.FileOrDirToAddLine.DirToAddLine).FullLineString.Length > 0) tmpList.Add(LevelSpaces + (NodeIn.Tag as FileToAddTextblock.FileOrDirToAddLine.DirToAddLine).FullLineString);
				}
			}

			//foreach (FileToAddTextblock fta in FilesToAddTextblockList)
			//    foreach (string line in fta.TextBlockForFile)
			//        SectionTextBlock.Add(line);

			else if (NodeIn.Tag is ShortcutDetails)
			{
				ShortcutDetails shortcut = NodeIn.Tag as ShortcutDetails;
				tmpList.Add(LevelSpaces + shortcut.FullShortcutLine);
			}
			return tmpList;
		}

		public static TreeNode GetMainParentOfNode(TreeNode node)
		{
			if (node.Parent != null) return GetMainParentOfNode(node.Parent);
			else return node;
		}

		public static Boolean CheckNodeAndSubnodesForSectionWithoutInstType(TreeNode node)
		{
			if (node.Tag is NSISclass.SectionGroupClass.SectionClass)
				if ((node.Tag as NSISclass.SectionGroupClass.SectionClass).InstTypes_CommaSeperated.Length == 0)
					return true;
			foreach (TreeNode subnode in node.Nodes)
				if (CheckNodeAndSubnodesForSectionWithoutInstType(subnode))
					return true;
			return false;
		}

		public static Boolean WasInstTypesModified(List<string> OriginalList, List<string> NewList)
		{
			if (OriginalList == null && NewList == null) return false;
			if (OriginalList == null) return true;
			if (NewList == null) return true;
			if (OriginalList.Count != NewList.Count) return true;

			for (int i = 0; i < OriginalList.Count; i++)
				if (OriginalList[i] != NewList[i])
					return true;
			return false;
		}

		public static void ClearAllSectionInstTypesIfWasModified(TreeNode MainNode)
		{
			if (MainNode.Tag is NSISclass.SectionGroupClass.SectionClass)
				(MainNode.Tag as NSISclass.SectionGroupClass.SectionClass).InstTypes_CommaSeperated = "";
			for (int i = 0; i < MainNode.Nodes.Count; i++)
				ClearAllSectionInstTypesIfWasModified(MainNode.Nodes[i]);
		}

		public static class TemplateNSISnodes
		{
			public static TreeNode CSharpProject
			{
				get
				{
					Form tmpChooseAppForm = new Form();
					ListBox listBox1 = new ListBox();
					listBox1.Dock = DockStyle.Fill;
					listBox1.SelectedIndexChanged += delegate
					{
						if (listBox1.SelectedIndex != -1)
							tmpChooseAppForm.DialogResult = DialogResult.OK;
					};
					foreach (string s in GetOpenAppANDextractAppList())
						listBox1.Items.Add(new OwnAppListboxItem(Path.GetDirectoryName(s), s.Split('\\')[s.Split('\\').Length - 1].Replace(".exe", "")));

					tmpChooseAppForm.Name = "Choose app";
					tmpChooseAppForm.Size = new Size(300, 300);
					tmpChooseAppForm.StartPosition = FormStartPosition.CenterScreen;
					tmpChooseAppForm.Text = "Choose app";
					tmpChooseAppForm.TopMost = true;
					tmpChooseAppForm.Controls.Add(listBox1);

					if (tmpChooseAppForm.ShowDialog() == DialogResult.OK)
					{
						string DirectoryString = (listBox1.SelectedItem as OwnAppListboxItem).DirectoryPath;
						tmpChooseAppForm.Close();

						string AppNameIncludingEXEextension = "";
						foreach (string file in Directory.GetFiles(DirectoryString))
							if (file.ToUpper().EndsWith(".EXE"))
								AppNameIncludingEXEextension = file.Split('\\')[file.Split('\\').Length - 1];
						if (AppNameIncludingEXEextension.Length > 0)
						{
							string AppNameOnly = AppNameIncludingEXEextension.Substring(0, AppNameIncludingEXEextension.Length - 4);

							TreeNode subsubsubFileLineNode = new TreeNode(DirectoryString + "\\*.*");
							subsubsubFileLineNode.Tag = new NSISclass.SectionGroupClass.SectionClass.FileToAddTextblock.FileOrDirToAddLine.FileToAddLine(DirectoryString + "\\*.*");

							TreeNode subsubFileTextblockNode = new TreeNode("$INSTDIR");
							subsubFileTextblockNode.Tag = new NSISclass.SectionGroupClass.SectionClass.FileToAddTextblock();
							subsubFileTextblockNode.Nodes.Add(subsubsubFileLineNode);

							//TreeNode subsubShortcutNode = new TreeNode("Shortcut");
							//subsubShortcutNode.Tag = new NSISclass.SectionGroupClass.SectionClass.ShortcutDetails(AppNameOnly + ".lnk", "$INSTDIR\\" + AppNameIncludingEXEextension);

							TreeNode subSectionNode = new TreeNode("Full program");
							subSectionNode.Tag = new NSISclass.SectionGroupClass.SectionClass(
									"Full program",
									"The full package",
									"");
							//subSectionNode.Nodes.Add(subsubShortcutNode);
							subSectionNode.Nodes.Add(subsubFileTextblockNode);

							TreeNode tmpMainNode = new TreeNode(AppNameOnly);
							tmpMainNode.Tag = new NSISclass(
									AppNameOnly,
									"0.0",
									"Francois Hill",
									"www.francoishill.com",
									AppNameIncludingEXEextension,
									new Compressor(),
									64,
									AppNameOnly + "_Setup_0.0.exe",//" 1_0_0_0 setup.exe",
									LanguagesEnum.English,
									true,
									true,
									new LicensePageDetails(false),
									true,
									true,
									true,
									AppNameIncludingEXEextension,
									new List<string>() { },
									null,
									DotnetFrameworkTargetedEnum.DotNet4client,
									false);
							tmpMainNode.Nodes.Add(subSectionNode);

							return tmpMainNode;
						}
					}

					//if (fbd.ShowDialog(AlwaysOnTopForm) == DialogResult.OK)
					//{
					//    if (fbd.SelectedPath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + @"Visual Studio 2010\Projects\"))
					//    {
					//        TreeNode tmpNode = new TreeNode();

					//        AlwaysOnTopForm = null;
					//        return tmpNode;
					//    }
					//}
					return null;
				}
			}

			private class OwnAppListboxItem
			{
				public string DirectoryPath;//like c:\francois
				public string DisplayString;
				public OwnAppListboxItem(string DirectoryPathIn, string DisplayStringIn)
				{
					DirectoryPath = DirectoryPathIn;
					DisplayString = DisplayStringIn;
				}
				public override string ToString()
				{
					return DisplayString;
				}
			}

			private static List<string> GetOpenAppANDextractAppList()
			{
				List<string> tmpList = new List<string>();

				Form AlwaysOnTopForm = new Form();
				AlwaysOnTopForm.TopMost = true;
				string RootPath = null;
				if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Local Settings\Apps\2.0"))
				{
					foreach (String dir1 in Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Local Settings\Apps\2.0"))
					{
						String tmpFolderName = dir1.Split('\\')[dir1.Split('\\').Length - 1];
						if (tmpFolderName.Contains('.'))
						{
							foreach (String dir2 in Directory.GetDirectories(dir1))
							{
								tmpFolderName = dir2.Split('\\')[dir2.Split('\\').Length - 1];
								if (tmpFolderName.Contains('.'))
								{
									RootPath = dir2;
									break;
								}
							}
						}
					}
				}

				if (RootPath == null)
				{
					MessageBox.Show(AlwaysOnTopForm, "Root path not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return tmpList;
				}
				else
				{

					//AppFolderFullPathListExtractInstallFiles.Clear();
					//MessageBox.Show(RootPath);
					String[] Dirs = Directory.GetDirectories(RootPath);
					List<String> OnlyFolderNames = new List<string>();
					List<String> OnlyNewestFullPaths = new List<string>();
					List<String> tmpCodeList = new List<string>(); //list of the different codes i.e. the first 27 letters
					foreach (String fulldir in Dirs)
					{
						String foldername = fulldir.Split('\\')[fulldir.Split('\\').Length - 1];
						if (foldername.Length >= 27)
						{
							String tmpCode = foldername.Substring(0, 27);
							if (!tmpCodeList.Contains(tmpCode)) tmpCodeList.Add(tmpCode);
						}
					}
					foreach (String code in tmpCodeList)
					{
						List<String> fullDirsStartingWithCode = new List<string>();
						foreach (String dir in Dirs)
						{
							String tmpFolderName = dir.Split('\\')[dir.Split('\\').Length - 1];
							if (tmpFolderName.StartsWith(code) && !tmpFolderName.Contains("none") && !tmpFolderName.Contains("manifests"))
								fullDirsStartingWithCode.Add(dir);
						}

						DirectoryInfo tmpNewestDir = null;
						if (fullDirsStartingWithCode.Count != 0)
						{
							foreach (String dirStartWithCode in fullDirsStartingWithCode)
							{
								if (tmpNewestDir == null) tmpNewestDir = new DirectoryInfo(dirStartWithCode);
								else
								{
									DirectoryInfo tmpThisDir = new DirectoryInfo(dirStartWithCode);
									DirectoryInfo tmpCurrNewestDir = tmpNewestDir;

									if (DateTime.Compare(tmpThisDir.LastWriteTime, tmpCurrNewestDir.LastWriteTime) > 0) tmpNewestDir = new DirectoryInfo(dirStartWithCode);
								}
							}

							if (tmpNewestDir != null)
							{
								OnlyNewestFullPaths.Add(tmpNewestDir.FullName);
							}
							else MessageBox.Show("Invalid directory error", "Directory error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						}
					}

					//ToolStripMenuItem OpenAppItem = new ToolStripMenuItem("Open app");
					foreach (String newestpath in OnlyNewestFullPaths)
					{
						foreach (String file in Directory.GetFiles(newestpath))
						{
							if (file.ToUpper().EndsWith(".EXE"))
							{
								tmpList.Add(file);
							}
						}
					}
				}

				return tmpList;
			}
		}
	}
}