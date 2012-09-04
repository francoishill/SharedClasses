using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace SharedClasses
{
	public static class AutoUpdating
	{
		public const string cCalledItsselfThirdParameter = "calleditsself";

		public enum ExitCodes
		{
			UpToDateExitCode = 3,
			NewVersionAvailableExitCode = 5,
			UnableToCheckForUpdatesErrorCode = 7,
			SkippingBecauseIsDebugEndingWithVshostExe = 9
		}

		//private static readonly string cAutoUpdaterAppExePath =
		//    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
		//    @"Auto Updater\AutoUpdater.exe");
		private const string ifUpToDateStartString = "Up to date:";

		//private static bool isUpToDate = false;
		public static void CheckForUpdates(Action<string> ActionIfUptoDate_Versionstring = null, Action<string> ActionOnError = null, bool SeparateThreadDoNotWait = true)
		{
			//If running from Visual Studio paths
			if (Environment.GetCommandLineArgs()[0].StartsWith(@"C:\Francois\Dev\VSprojects", StringComparison.InvariantCultureIgnoreCase)
				|| Environment.GetCommandLineArgs()[0].StartsWith(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Visual Studio 2010\Projects"), StringComparison.InvariantCultureIgnoreCase))
				return;

			bool isCheckingForAutoUpdater = false;
			string ApplicationName = FileVersionInfo.GetVersionInfo(Environment.GetCommandLineArgs()[0]).ProductName;
			if (ApplicationName != null && ApplicationName.Equals("AutoUpdater", StringComparison.InvariantCultureIgnoreCase))
				isCheckingForAutoUpdater = true;

			var autoupdaterFilepath = RegistryInterop.GetAppPathFromRegistry("AutoUpdater.exe");

			if (autoupdaterFilepath == null)
			{
				if (ActionOnError != null)
					ActionOnError("AutoUpdater not installed, could not find AutoUpdater.exe in App Paths of Regsitry.");
			}
			else
			{
				ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				{
					List<string> outputs;
					List<string> errors;
					int exitcode;
					bool? runresult = ProcessesInterop.RunProcessCatchOutput(
						new ProcessStartInfo(
							autoupdaterFilepath,
							"checkforupdates \"" + Environment.GetCommandLineArgs()[0] + "\""
								+ (isCheckingForAutoUpdater ? " " + cCalledItsselfThirdParameter : "")),//Pass extra commandline argument if is checking for ittsself (AutoUpdater)
						out outputs,
						out errors,
						out exitcode);

					if (runresult.HasValue && runresult.Value == true)//Ran but with errors/output
						errors.RemoveAll(s => string.IsNullOrWhiteSpace(s));
					ExitCodes parsedExitCode;
					if (Enum.TryParse<ExitCodes>(exitcode.ToString(), out parsedExitCode))
					{
						switch (parsedExitCode)
						{
							case ExitCodes.UpToDateExitCode:
								if (ActionIfUptoDate_Versionstring != null)
									ActionIfUptoDate_Versionstring(FileVersionInfo.GetVersionInfo(Environment.GetCommandLineArgs()[0]).FileVersion);
								break;
							case ExitCodes.NewVersionAvailableExitCode:
								//A WPFNotification got shown, no need to show more
								break;
							case ExitCodes.UnableToCheckForUpdatesErrorCode:
								if (errors.Count > 0 && ActionOnError != null)
									ActionOnError("Could not check for updates: " + string.Join(".  ", errors));
								break;
							default:
								break;
						}
					}
				},
				!SeparateThreadDoNotWait);
			}
		}
	}
}