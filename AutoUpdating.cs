using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Threading;

namespace SharedClasses
{
	public static class AutoUpdating
	{
		/*	Additional dependencies for this file:
			//Forms required for ThreadingInterop
			Minimum winforms
			Class: AppTypeIndependant
			Class: fastJSON
			Class: ProcessesInterop
			Class: RegistryInterop
			Class: ThreadingInterop
			WPF: Window: UnhandledExceptionsWindow
			Assembly: WindowsBase
			Assembly: PresentationCore
			Assembly: PresentationFramework
			Assembly: System.Xaml*/

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

		private static bool isCheckingForAutoUpdater()
		{
			string fullExePath = Environment.GetCommandLineArgs()[0];
			string ApplicationName = FileVersionInfo.GetVersionInfo(fullExePath).ProductName;
			if (ApplicationName != null && ApplicationName.Equals("AutoUpdater", StringComparison.InvariantCultureIgnoreCase))
				return true;
			return false;
		}

		private static bool alreadyRegisteredUnhandledExceptionHandler = false;
		private static void RegisterUnhandledExceptionHandler()
		{
			if (alreadyRegisteredUnhandledExceptionHandler) return;
			alreadyRegisteredUnhandledExceptionHandler = true;
			AppDomain.CurrentDomain.UnhandledException += (s, uexc) =>
			{
				Exception exc = uexc.ExceptionObject as Exception;
				if (exc != null)
				{
					Logging.LogExceptionToFile(
						exc,
						Logging.ReportingFrequencies.Secondly,
						Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]).Replace(".vshost.", ""),
						"Unhandled Exceptions");

					UnhandledExceptionsWindow.ShowUnHandledException(exc);
				}
			};
		}

		public static string GetThisAppVersionString()
		{
			return FileVersionInfo.GetVersionInfo(Environment.GetCommandLineArgs()[0]).FileVersion;
		}

		/// <summary>
		/// Place this method at the topmost of the entry point of the application (static void main() for Console+Winforms apps, and in override void OnStartup() for WPF)
		/// </summary>
		/// <param name="ActionForVersionIfUptoDate">The action to be taken if the application is up to date, call GetThisAppVersionString() to get the version string. Note it runs on a separate thread therefore we need this.</param>
		/// <returns></returns>
		public static Thread CheckForUpdates_ExceptionHandler(Action ActionIfUptoDate = null)
		{
			if (ActionIfUptoDate == null) ActionIfUptoDate = delegate { };

			//Step 1: Register event handler for UnhandledExceptions
			RegisterUnhandledExceptionHandler();

			//Step 2: Check for updates
			//If running from Visual Studio paths
			if (Environment.GetCommandLineArgs()[0].StartsWith(@"C:\Francois\Dev\VSprojects", StringComparison.InvariantCultureIgnoreCase)
				|| Environment.GetCommandLineArgs()[0].StartsWith(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Visual Studio 2010\Projects"), StringComparison.InvariantCultureIgnoreCase))
				return null;

			string fullExePath = Environment.GetCommandLineArgs()[0];
			var autoupdaterFilepath = RegistryInterop.GetAppPathFromRegistry("AutoUpdater.exe");

			if (autoupdaterFilepath == null)
			{
				AppTypeIndependant.ShowErrorMessage("AutoUpdater not installed, could not find AutoUpdater.exe in App Paths of Regsitry.");
				return null;
			}
			else
			{
				Thread checkForUpdatesSilentlyThread = ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				{
					List<string> outputs;
					List<string> errors;
					int exitcode;
					bool? runresult = ProcessesInterop.RunProcessCatchOutput(
						new ProcessStartInfo(
							autoupdaterFilepath,
							"checkforupdatesilently \"" + fullExePath + "\""
							+ " " + FileVersionInfo.GetVersionInfo(fullExePath).FileVersion
							+ (isCheckingForAutoUpdater() ? " " + cCalledItsselfThirdParameter : "")),//Pass extra commandline argument if is checking for ittsself (AutoUpdater)
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
								ActionIfUptoDate();
								break;
							case ExitCodes.NewVersionAvailableExitCode:
								////A WPFNotification got shown, no need to show more
								//NO notification shown, just install it silently
								runresult = ProcessesInterop.RunProcessCatchOutput(
									new ProcessStartInfo(
										autoupdaterFilepath,
											"installlatestsilently \"" + fullExePath + "\""
											+ (isCheckingForAutoUpdater() ? " " + cCalledItsselfThirdParameter : "")),//Pass extra commandline argument if is checking for ittsself (AutoUpdater)
									out outputs,
									out errors,
									out exitcode);
								break;
							case ExitCodes.UnableToCheckForUpdatesErrorCode:
								if (errors.Count > 0)
									AppTypeIndependant.ShowErrorMessage("Could not check for updates: " + string.Join(".  ", errors));
								break;
							default:
								break;
						}
					}
				},
				false);
				return checkForUpdatesSilentlyThread;
			}
		}

		//private static bool isUpToDate = false;
		/*[Obsolete("Method had been replaced by CheckForUpdates_ExceptionHandler", true)]
		public static Thread CheckForUpdates(Action<string> ActionIfUptoDate_Versionstring = null, Action<string> ActionOnError = null, bool SeparateThreadDoNotWait = true, bool autoInstallIfUpdateFound = true)
		{
			return null;
		}*/

		public static void InstallLatest(string applicationName, Action<string> ActionOnError, Action<string> actionOnComplete = null, bool installSilently = true)
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
							(installSilently ? "installlatestsilently" : "installlatest") + " \"" + applicationName + "\""),
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

		/*public static void CheckAllForUpdates(Action<string> onError)
		{
			if (onError == null) onError = delegate { };

			var autoupdaterFilepath = RegistryInterop.GetAppPathFromRegistry("AutoUpdater.exe");

			if (autoupdaterFilepath == null)
			{
				onError("AutoUpdater not installed, could not find AutoUpdater.exe in App Paths of Regsitry.");
				return;
			}
			else
			{
				List<string> outputs;
				List<string> errors;
				int exitcode;
				bool? runresult = ProcessesInterop.RunProcessCatchOutput(
					new ProcessStartInfo(autoupdaterFilepath, "checkandupdateall" + (isCheckingForAutoUpdater() ? " " + cCalledItsselfThirdParameter : "")),
					out outputs,
					out errors,
					out exitcode);

				if (runresult == false)//Could not start the process
				{
					if (runresult.HasValue && runresult.Value == true)//Ran but with errors/output
						errors.RemoveAll(s => string.IsNullOrWhiteSpace(s));
					onError(string.Join(Environment.NewLine, errors));
					return;
				}
			}
		}*/

		public class MockPublishDetails
		{
			public string ApplicationVersion;
		}
	}
}