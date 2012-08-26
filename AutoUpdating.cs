using System;
using System.IO;
using System.Diagnostics;

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
		private static bool isUpToDate = false;
		public static void CheckForUpdates(Action<string> ActionIfUptoDate_Versionstring = null)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo(cAutoUpdaterAppExePath, "checkforupdates \"" + Environment.GetCommandLineArgs()[0] + "\"");
			proc = new Process();
			proc.StartInfo = startInfo;
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardOutput = true;
			isUpToDate = false;
			proc.OutputDataReceived += (sn, outevt) =>
			{
				if (outevt.Data != null)
					if (outevt.Data.StartsWith(ifUpToDateStartString, StringComparison.InvariantCultureIgnoreCase))
						isUpToDate = true;
			};
			proc.Start();
			proc.BeginOutputReadLine();
			proc.Exited += (sn, ev) =>
			{
				ExitCodes exitcode;
				if (Enum.TryParse<ExitCodes>(proc.ExitCode.ToString(), out exitcode))
					System.Windows.Forms.MessageBox.Show("Autoupdater exit code: " + exitcode.ToString());
			};
			proc.EnableRaisingEvents = true;
			//proc.WaitForExit();
			//if (isUpToDate)
			//    if (ActionIfUptoDate_Versionstring != null)
			//        ActionIfUptoDate_Versionstring(FileVersionInfo.GetVersionInfo(Environment.GetCommandLineArgs()[0]).FileVersion);
		}
	}
}