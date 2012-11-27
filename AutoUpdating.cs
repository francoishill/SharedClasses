using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

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
			SkippingBecauseIsDebugEndingWithVshostExe = 9,
			InstalledVersionNewerThanOnline = 11,
		}

		//private static readonly string cAutoUpdaterAppExePath =
		//    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
		//    @"Auto Updater\AutoUpdater.exe");
		private const string ifUpToDateStartString = "Up to date:";

		//private static bool isUpToDate = false;
		public static void CheckForUpdates(Action<string> ActionIfUptoDate_Versionstring = null, Action<string> ActionOnError = null, bool SeparateThreadDoNotWait = true, bool autoInstallIfUpdateFound = true)
		{
			//If running from Visual Studio paths
			if (Environment.GetCommandLineArgs()[0].StartsWith(@"C:\Francois\Dev\VSprojects", StringComparison.InvariantCultureIgnoreCase)
				|| Environment.GetCommandLineArgs()[0].StartsWith(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Visual Studio 2010\Projects"), StringComparison.InvariantCultureIgnoreCase))
				return;

			bool isCheckingForAutoUpdater = false;
			string fullExePath = Environment.GetCommandLineArgs()[0];
			string ApplicationName = FileVersionInfo.GetVersionInfo(fullExePath).ProductName;
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
							"checkforupdatesilently \"" + fullExePath + "\""
							+ " " + FileVersionInfo.GetVersionInfo(fullExePath).FileVersion
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
								////A WPFNotification got shown, no need to show more
								//NO notification shown, just install it silently
								runresult = ProcessesInterop.RunProcessCatchOutput(
									new ProcessStartInfo(
										autoupdaterFilepath,
											"installlatestsilently \"" + fullExePath + "\""
											+ (isCheckingForAutoUpdater ? " " + cCalledItsselfThirdParameter : "")),//Pass extra commandline argument if is checking for ittsself (AutoUpdater)
									out outputs,
									out errors,
									out exitcode);
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

		public static void InstallLatest(string applicationName, Action<string> ActionOnError, Action<string> actionOnComplete = null)
		{
			if (actionOnComplete == null) actionOnComplete = delegate { };

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
							"installlatestsilently \"" + applicationName + "\""),
						//"installlatest \"" + applicationName + "\""),
						out outputs,
						out errors,
						out exitcode);

					var combinedOutputs = outputs.Concat(errors.Select(e => "ERROR: " + e)).ToList();
					if (runresult.HasValue && runresult.Value == true)//Ran but with errors/output
						combinedOutputs.RemoveAll(s => string.IsNullOrWhiteSpace(s));

					if (combinedOutputs.Count > 0)
						ActionOnError(string.Join(Environment.NewLine, combinedOutputs));

					actionOnComplete(applicationName);
				},
				false);
			}
		}

		//true=up to date, false=newer version available, null=could not check
		public static bool? CheckForUpdatesSilently(string ApplicationName, string InstalledVersion, out string errorIfNull, out MockPublishDetails onlineVersionDetails)
		{
			var autoupdaterFilepath = RegistryInterop.GetAppPathFromRegistry("AutoUpdater.exe");

			if (autoupdaterFilepath == null)
			{
				errorIfNull = "AutoUpdater not installed, could not find AutoUpdater.exe in App Paths of Regsitry.";
				onlineVersionDetails = null;
				return null;
			}
			else
			{
				//string tmpErr = null;
				//PublishDetails tmpOnlineDetails = null;
				//bool? returnVal = true;
				//ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				//{

				List<string> outputs;
				List<string> errors;
				int exitcode;
				bool? runresult = ProcessesInterop.RunProcessCatchOutput(
					new ProcessStartInfo(
						autoupdaterFilepath,
						"checkforupdatesilently \"" + ApplicationName + "\" \"" + InstalledVersion + "\""),
					out outputs,
					out errors,
					out exitcode);

				if (runresult != false)//Actually ran the AutoUpdater app
				{
					if (exitcode != (int)ExitCodes.UpToDateExitCode)//Application is not up to date, or has error
					{
						if (exitcode == (int)ExitCodes.UnableToCheckForUpdatesErrorCode)
						{
							errorIfNull = string.Join(Environment.NewLine, errors.RemoveAll(s => string.IsNullOrWhiteSpace(s)));
							onlineVersionDetails = null;
							return null;
						}
						else if (exitcode == (int)ExitCodes.NewVersionAvailableExitCode)
						{
							errorIfNull = null;
							onlineVersionDetails = new MockPublishDetails();
							string jsonStr = outputs[0];
							JSON.SetDefaultJsonInstanceSettings();
							JSON.Instance.FillObject(onlineVersionDetails, jsonStr);
							return false;
						}
						else if (exitcode == (int)ExitCodes.InstalledVersionNewerThanOnline)
						{
							errorIfNull = "Installed version is newer than online version.";
							onlineVersionDetails = new MockPublishDetails();
							return null;
						}
						else
						{
							errorIfNull = "Unknown exit code returned for checking updates for " + ApplicationName + ": " + exitcode;
							onlineVersionDetails = null;
							return null;
						}
					}
					else
					{
						errorIfNull = null;
						onlineVersionDetails = null;
						return true;
					}
				}
				else
				{
					if (runresult.HasValue && runresult.Value == true)//Ran but with errors/output
						errors.RemoveAll(s => string.IsNullOrWhiteSpace(s));
					errorIfNull = string.Join(Environment.NewLine, errors);
					onlineVersionDetails = null;
					return null;
				}
				//},
				//false);
			}
		}

		public class MockPublishDetails
		{
			public string ApplicationVersion;
		}
	}
}