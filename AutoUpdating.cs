using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace SharedClasses
{
	public static class AutoUpdating
	{
		public enum ExitCodes
		{
			UpToDateExitCode = 3,
			NewVersionAvailableExitCode = 5,
			UnableToCheckForUpdatesErrorCode = 7
		}

		private static readonly string cAutoUpdaterAppExePath =
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
			@"Auto Updater\AutoUpdater.exe");
		private const string ifUpToDateStartString = "Up to date:";

		private static Process proc;
		//private static bool isUpToDate = false;
		public static void CheckForUpdates(Action<string> ActionIfUptoDate_Versionstring, Action<string> ActionOnError = null)
		{
			if (!File.Exists(cAutoUpdaterAppExePath))
			{
				if (ActionOnError != null)
					ActionOnError("AutoUpdater not installed, file does not exist: " + cAutoUpdaterAppExePath);
			}
			else
			{
				List<string> errorList = new List<string>();
				ProcessStartInfo startInfo = new ProcessStartInfo(cAutoUpdaterAppExePath, "checkforupdates \"" + Environment.GetCommandLineArgs()[0] + "\"");
				proc = new Process();
				proc.StartInfo = startInfo;
				proc.StartInfo.UseShellExecute = false;
				proc.StartInfo.RedirectStandardOutput = true;
				proc.StartInfo.RedirectStandardError = true;
				//isUpToDate = false;
				proc.OutputDataReceived += (sn, outevt) =>
				{
					//if (outevt.Data != null)
					//    if (outevt.Data.StartsWith(ifUpToDateStartString, StringComparison.InvariantCultureIgnoreCase))
					//        isUpToDate = true;
				};
				proc.ErrorDataReceived += (sn, errevt) =>
				{
					errorList.Add(errevt.Data ?? "");
				};
				proc.Start();
				proc.BeginOutputReadLine();
				proc.BeginErrorReadLine();
				proc.Exited += (sn, ev) =>
				{
					errorList.RemoveAll(s => string.IsNullOrWhiteSpace(s));
					ExitCodes exitcode;
					if (Enum.TryParse<ExitCodes>(proc.ExitCode.ToString(), out exitcode))
					{
						switch (exitcode)
						{
							case ExitCodes.UpToDateExitCode:
								ActionIfUptoDate_Versionstring(FileVersionInfo.GetVersionInfo(Environment.GetCommandLineArgs()[0]).FileVersion);
								break;
							case ExitCodes.NewVersionAvailableExitCode:
								//A WPFNotification got shown, no need to show more
								break;
							case ExitCodes.UnableToCheckForUpdatesErrorCode:
								if (errorList.Count > 0 && ActionOnError != null)
									ActionOnError("Could not check for updates: " + string.Join(".  ", errorList));
								break;
							default:
								break;
						}
					}
				};
				proc.EnableRaisingEvents = true;
				//proc.WaitForExit();
				//if (isUpToDate)
				//    if (ActionIfUptoDate_Versionstring != null)
				//        ActionIfUptoDate_Versionstring(FileVersionInfo.GetVersionInfo(Environment.GetCommandLineArgs()[0]).FileVersion);
			}
		}
	}
}