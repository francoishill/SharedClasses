using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.WindowsAPICodePack.Taskbar;
using Microsoft.WindowsAPICodePack.Shell;//Requires DLL Microsoft.WindowsAPICodePack.Taskbar.Shell

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
			Class: Windows7JumpListsInterop
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

		private static void AddJumplistForNewfeaturesAndBugs_AlsoHandleCommandlineArguments()
		{
			if (Windows7JumpListsInterop.HandleCommandlineJumplistCommand())
				Environment.Exit(0);

			ThreadingInterop.DoAction(delegate
			{
				while (true)
				{
					try
					{
						Windows7JumpListsInterop.AddStandardUserTasksItems(JumpList.CreateJumpList());
						break;
					}
					catch (InvalidOperationException)
					{
						Thread.Sleep(10);//This may happen because the window is not created yet, so we continue to loop until the window is created
					}
					catch (Exception)
					{
						break;
					}
				}
			},
			false);

			//int maybeLookAtThisImplementingWindowsShellJumplistInsteadOfWindowsAPIcodepack;
			/*new System.Windows.Shell.JumpList().JumpItems.Add(new System.Windows.Shell.JumpTask()
			{
				ApplicationPath = ???
			});*/

		}

		private static readonly TimeSpan cDelayBeforeInitialCheck = TimeSpan.FromSeconds(10);
		private static readonly TimeSpan cCheckInterval = TimeSpan.FromSeconds(5);
		private static readonly TimeSpan cDurationAfterWhichToAutokillIfHighMemoryOrCpu = TimeSpan.FromMinutes(5.0);

		private const long cMemoryThresholdBytes = 250 * 1024 * 1024;
		
		private static DateTime? busyShowingHighMemoryUsageMessage = null;
		private static bool donotShowHighMemoryUsageMessages = false;
		private static void CheckMemoryUsage(Process process)
		{
			process.Refresh();

			//BytesToHumanfriendlyStringConverter.ConvertBytesToHumanreadableString(currentProcess.PrivateMemorySize64)
			if (process.PrivateMemorySize64 > cMemoryThresholdBytes)
			{
				if (!busyShowingHighMemoryUsageMessage.HasValue
					&& !donotShowHighMemoryUsageMessages)
				{
					busyShowingHighMemoryUsageMessage = DateTime.Now;
					ThreadingInterop.DoAction(delegate
					{
						string appname = LicensingInterop_Client.GetApplicationName();

						var result = HighResourceUsageWindow.ShowHighResourceUsageWindowReturnResult(
							string.Format(
								"WARNING!!! Confirm to terminate application '{0}' (cancel to not show message again).? Current Memory usage is above {1} (current memory usage is {2})."
								+ Environment.NewLine + Environment.NewLine
								+ "Application will automatically exit after {3} if no option is chosen.",
								appname,
								BytesToHumanfriendlyStringConverter.ConvertBytesToHumanreadableString(cMemoryThresholdBytes),
								BytesToHumanfriendlyStringConverter.ConvertBytesToHumanreadableString(process.PrivateMemorySize64),
								cDurationAfterWhichToAutokillIfHighMemoryOrCpu.ToString()),
							"Current time is " + DateTime.Now.ToString("HH:mm:ss"));

						if (result == HighResourceUsageWindow.ReturnResult.ForceCloseNow)
							Environment.Exit(0);
						else if (result == HighResourceUsageWindow.ReturnResult.IgnoreUntilClose)
							donotShowHighMemoryUsageMessages = true;

						busyShowingHighMemoryUsageMessage = null;
					},
					false,
					apartmentState: System.Threading.ApartmentState.STA);
				}
				else if (busyShowingHighMemoryUsageMessage.HasValue
					&& !donotShowHighMemoryUsageMessages)
				{
					if (DateTime.Now - busyShowingHighMemoryUsageMessage.Value > cDurationAfterWhichToAutokillIfHighMemoryOrCpu)
						Environment.Exit(0);
				}
			}
		}

		private const double cCPUthresholdPercentage = 3.0;//40.0;
		private const double cWarningSecondsIfAboveCPUThresholdForLongerThan = 10;

		private static Timer _memoryWatcherTimer = null;
		private static DateTime? _previousCheckedTime = null;
		private static double _previousTotalMilliseconds;
		private static int numberConsecutiveTimesCPUabove50 = 0;
		private static DateTime? busyShowingHighCpuUsageMessage = null;
		private static bool donotShowHighCpuUsageMessages = false;
		private static void CheckCpuLoad(Process process)
		{
			process.Refresh();

			if (_previousCheckedTime.HasValue)
			{
				double totalMillisecondsAddedAfterLastCheck = process.TotalProcessorTime.TotalMilliseconds - _previousTotalMilliseconds;
				TimeSpan durationAfterLastCheck = DateTime.Now - _previousCheckedTime.Value;
				double currentTotalCPUload = 100D * (totalMillisecondsAddedAfterLastCheck / durationAfterLastCheck.TotalMilliseconds);
				currentTotalCPUload = currentTotalCPUload / (double)Environment.ProcessorCount;

				//We are currently measuring the CURRENT cpu load, not the AVERAGE
				if (currentTotalCPUload > cCPUthresholdPercentage)
				{
					numberConsecutiveTimesCPUabove50++;
					if (cCheckInterval.TotalSeconds * (double)numberConsecutiveTimesCPUabove50 > cWarningSecondsIfAboveCPUThresholdForLongerThan)
					{
						if (!busyShowingHighCpuUsageMessage.HasValue
							&& !donotShowHighCpuUsageMessages)
						{
							busyShowingHighCpuUsageMessage = DateTime.Now;
							ThreadingInterop.DoAction(delegate
							{
								string appname = LicensingInterop_Client.GetApplicationName();
								double secondsTheCpuIsAboveThreshold = cCheckInterval.TotalSeconds * (double)numberConsecutiveTimesCPUabove50;
								var result = HighResourceUsageWindow.ShowHighResourceUsageWindowReturnResult(
									string.Format(
										"WARNING!!! Confirm to terminate application '{0}' (cancel to not show message again)? Current CPU load is above {1} (current load is {2}) for more than {3} seconds."
										+ Environment.NewLine + Environment.NewLine
										+ "Application will automatically exit after {4} if no option is chosen.",
										appname,
										cCPUthresholdPercentage,
										currentTotalCPUload.ToString("0.##"),
										secondsTheCpuIsAboveThreshold,
										cDurationAfterWhichToAutokillIfHighMemoryOrCpu.ToString()),
									"Current time is " + DateTime.Now.ToString("HH:mm:ss"));
								if (result == HighResourceUsageWindow.ReturnResult.ForceCloseNow)
									Environment.Exit(0);
								else if (result == HighResourceUsageWindow.ReturnResult.IgnoreUntilClose)
									donotShowHighCpuUsageMessages = true;

								busyShowingHighCpuUsageMessage = null;
							},
							false,
							apartmentState: System.Threading.ApartmentState.STA);
						}
						else if (busyShowingHighCpuUsageMessage.HasValue
							&& !donotShowHighCpuUsageMessages)
						{
							if (DateTime.Now - busyShowingHighCpuUsageMessage.Value > cDurationAfterWhichToAutokillIfHighMemoryOrCpu)
								Environment.Exit(0);
						}
					}
				}
				else
					numberConsecutiveTimesCPUabove50 = 0;

				Console.WriteLine("CPU load = {0}", currentTotalCPUload);
			}

			_previousCheckedTime = DateTime.Now;
			_previousTotalMilliseconds = process.TotalProcessorTime.TotalMilliseconds;
		}

		private static void RegisterMemoryAndCpuWatcher()
		{
			_memoryWatcherTimer = new Timer(
				delegate
				{
					var currentProcess = Process.GetCurrentProcess();
					CheckMemoryUsage(currentProcess);
					CheckCpuLoad(currentProcess);
				},
				null,
				cDelayBeforeInitialCheck,
				cCheckInterval);
		}

		public static string GetThisAppVersionString()
		{
			return FileVersionInfo.GetVersionInfo(Environment.GetCommandLineArgs()[0]).FileVersion;
		}

		private static string GetAutoUpdaterFilePath()
		{
			return RegistryInterop.GetAppPathFromRegistry("AutoUpdater.exe");
		}
		public static string GetDownloadlinkForLatestAutoUpdater()
		{
			return SharedClasses.SettingsSimple.HomePcUrls.Instance.AppsPublishingRoot + @"/downloadownapps.php?relativepath=autoupdater/AutoUpdater_SetupLatest.exe";
		}

		public static string GetApplicationOnlineUrl(string applicationName)
		{
			return SettingsSimple.HomePcUrls.Instance.AppsPublishingRoot + "/apps/" + applicationName.ToLower();//"http://fjh.dyndns.org/ownapplications/" + projName.ToLower(),
		}

		public static bool IsAutoUpdaterInstalled()
		{
			return GetAutoUpdaterFilePath() != null;
		}

		/// <summary>
		/// Place this method at the topmost of the entry point of the application (static void main() for Console+Winforms apps, and in override void OnStartup() for WPF)
		/// </summary>
		/// <param name="ActionForVersionIfUptoDate">The action to be taken if the application is up to date, call GetThisAppVersionString() to get the version string. Note it runs on a separate thread therefore we need this.</param>
		/// <returns></returns>
		public static Thread CheckForUpdates_ExceptionHandler(Action ActionIfUptoDate = null)
		{
			if (ActionIfUptoDate == null) ActionIfUptoDate = delegate { };

			RegisterUnhandledExceptionHandler();

			AddJumplistForNewfeaturesAndBugs_AlsoHandleCommandlineArguments();

			RegisterMemoryAndCpuWatcher();

			Thread checkForUpdatesThread = _checkForUpdates(ActionIfUptoDate);
			return checkForUpdatesThread;
		}

		private static Thread _checkForUpdates(Action ActionIfUptoDate)
		{
			//Step 2: Check for updates
			//If running from Visual Studio paths
			if (Environment.GetCommandLineArgs()[0].StartsWith(@"C:\Francois\Dev\VSprojects", StringComparison.InvariantCultureIgnoreCase)
				|| Environment.GetCommandLineArgs()[0].StartsWith(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Visual Studio 2010\Projects"), StringComparison.InvariantCultureIgnoreCase))
				return null;

			Thread checkForUpdatesSilentlyThread = ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			{
				string fullExePath = Environment.GetCommandLineArgs()[0];

				if (!IsAutoUpdaterInstalled())
				{
					//Only show message if we are using own PC, otherwise we just log it
					if (Directory.Exists(@"C:\Francois\Dev\VSprojects"))
						AppTypeIndependant.ShowErrorMessage("AutoUpdater not installed, could not find AutoUpdater.exe in App Paths of Regsitry.");

					Logging.LogWarningToFile(
						"AutoUpdater is not installed, could not check for updates.",
						Logging.ReportingFrequencies.Daily,
						"_NotCheckedForUpdates",
						Path.GetFileNameWithoutExtension(fullExePath));
					return;
				}

				string autoupdaterFilepath = GetAutoUpdaterFilePath();

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