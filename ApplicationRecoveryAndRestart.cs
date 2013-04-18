using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

///
/// Got information from MSDN
/// http://msdn.microsoft.com/en-us/library/cc303699.aspx
/// Title of page on MSDN (for incase URL expires)
/// Developing with Application Recovery and Restart
///

namespace SharedClasses
{
	public class OwnUnhandledExceptionHandler
	{
		public static List<Exception> UnhandledExceptions = new List<Exception>();

		public void HandleException(Exception exc)
		{
			UnhandledExceptions.Add(exc);
			//AppTypeIndependant.ShowErrorMessage("Unhandled caught:" + Environment.NewLine + exc.StackTrace);
		}

		//public void UnhandledExceptionHandler(object sender, ThreadExceptionEventArgs e)
		public void UnhandledExceptionHandler<TUnhandledExceptionType>(object sender, TUnhandledExceptionType e)
		{
			try
			{
				PropertyInfo exceptionProperty = e.GetType().GetProperty("Exception");
				if (exceptionProperty != null)
					HandleException((Exception)exceptionProperty.GetValue(e, new object[0]));
			}
			catch (Exception exc)
			{
				AppTypeIndependant.ShowErrorMessage("Unable to handle unhandled exception: " + exc.Message);
			}
			ApplicationRecoveryAndRestart.WriteCrashReportFile(Process.GetCurrentProcess().ProcessName);//System.IO.Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]));
			//OwnAppsShared.ExitAppWithExitCode();
			Environment.FailFast("Exit application due to Unhandled exception.");
		}

		public static void TestCrash(bool showWarningFirst, Func<string, bool> functionToConfirm = null)
		{
			if (!showWarningFirst || (functionToConfirm != null && functionToConfirm("Program will now perform a crash to set RecoveryAndRestart, are you sure?")))
				//MessageBox.Show("Program will now perform a crash to set RecoveryAndRestart, are you sure?", "Confirm", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
				Environment.FailFast("Testing the Application Restart Recovery.");
		}
	}

	/// <summary>
	/// Need to add a reference to the Microsoft.WindowsAPICodePack.dll in SharedClasses\DLLs
	/// </summary>
	public class ApplicationRecoveryAndRestart
	{
		public static Action FunctionToPerformOnCrash;

		public const string CrashReportsDirectory = @"C:\Francois\Crash reports";
		public const string SavetofileDateFormat = @"yyyy MM dd HH mm ss";

		private static OwnUnhandledExceptionHandler exceptionHandler = null;
		private static void RegisterUnhandledExceptionHandler()
		{
			var applicationType = AppTypeIndependant.GetApplicationType(true);
			if (applicationType == AppTypeIndependant.ApplicationType.WinForm)
			{
				Type formsApplicationType = ReflectionInterop.GetTypeFromSimpleString("System.Windows.Forms.Application", true);
				Type threadExceptionEventArgs = ReflectionInterop.GetTypeFromSimpleString("System.Threading.ThreadExceptionEventArgs");
				string eventName = "ThreadException";

				if (formsApplicationType != null && threadExceptionEventArgs != null)
				{
					var possibleUnhandledExceptionEventHandlers =
						(formsApplicationType.GetEvents() ?? new EventInfo[0])
						.Where(evtInfo => evtInfo.Name.Equals(eventName, StringComparison.InvariantCultureIgnoreCase)).ToArray();
					if (possibleUnhandledExceptionEventHandlers != null && possibleUnhandledExceptionEventHandlers.Length > 0)
					{
						EventInfo evt = possibleUnhandledExceptionEventHandlers.First();
						exceptionHandler = new OwnUnhandledExceptionHandler();

						MethodInfo miHandler = typeof(OwnUnhandledExceptionHandler).GetMethod("UnhandledExceptionHandler").MakeGenericMethod(threadExceptionEventArgs);
						if (miHandler != null)
						{
							Delegate d = Delegate.CreateDelegate(evt.EventHandlerType, exceptionHandler, miHandler);
							if (d != null)
							{
								MethodInfo addHandler = evt.GetAddMethod();
								Object[] addHandlerArgs = { d };
								addHandler.Invoke(exceptionHandler, addHandlerArgs);
							}
						}
					}
				}
			}
			else if (applicationType == AppTypeIndependant.ApplicationType.WPF)
			{
				//Application.Current.Dispatcher.UnhandledException += (sn, exc) => { MessageBox.Show("Err: " + exc.Exception.Message); };
				Type wpfDispatcherType = ReflectionInterop.GetTypeFromSimpleString("System.Windows.Threading.Dispatcher");
				Type dispatcherExceptionEventArgs = ReflectionInterop.GetTypeFromSimpleString("System.Windows.Threading.DispatcherUnhandledExceptionEventArgs");
				string eventName = "UnhandledException";

				if (wpfDispatcherType != null && dispatcherExceptionEventArgs != null)
				{
					var possibleUnhandledExceptionEventHandlers =
						(wpfDispatcherType.GetEvents() ?? new EventInfo[0])
						.Where(evtInfo => evtInfo.Name.Equals(eventName, StringComparison.InvariantCultureIgnoreCase)).ToArray();
					if (possibleUnhandledExceptionEventHandlers != null && possibleUnhandledExceptionEventHandlers.Length > 0)
					{
						EventInfo evt = possibleUnhandledExceptionEventHandlers.First();
						exceptionHandler = new OwnUnhandledExceptionHandler();

						MethodInfo miHandler = typeof(OwnUnhandledExceptionHandler).GetMethod("UnhandledExceptionHandler").MakeGenericMethod(dispatcherExceptionEventArgs);
						if (miHandler != null)
						{
							Delegate d = Delegate.CreateDelegate(evt.EventHandlerType, exceptionHandler, miHandler);
							if (d != null)
							{
								var currentDispatcherProperty = wpfDispatcherType.GetProperty("CurrentDispatcher");
								if (currentDispatcherProperty != null)
								{
									object currentDispatcherValue = currentDispatcherProperty.GetValue(null, new object[0]);
									if (currentDispatcherValue != null)
										evt.AddEventHandler(currentDispatcherValue, d);
								}
							}
						}
					}
				}
			}
		}

		private enum OSMinimumVersion { VeryOld, MinimumXP, Minimum2003orXP64bit, MinimumVista, MinimumWin7 };
		private static OSMinimumVersion GetOSminimumVersion()
		{
			//+-----------------------------------------------------------------------------------------------------------------------------------------+
			//|           |   Windows    |   Windows    |   Windows    |Windows NT| Windows | Windows | Windows | Windows | Windows | Windows | Windows |
			//|           |     95       |      98      |     Me       |    4.0   |  2000   |   XP    |  2003   |  Vista  |  2008   |    7    | 2008 R2 |
			//+-----------------------------------------------------------------------------------------------------------------------------------------+
			//|PlatformID | Win32Windows | Win32Windows | Win32Windows | Win32NT  | Win32NT | Win32NT | Win32NT | Win32NT | Win32NT | Win32NT | Win32NT |
			//+-----------------------------------------------------------------------------------------------------------------------------------------+
			//|Major      |              |              |              |          |         |         |         |         |         |         |         |
			//| version   |      4       |      4       |      4       |    4     |    5    |    5    |    5    |    6    |    6    |    6    |    6    |
			//+-----------------------------------------------------------------------------------------------------------------------------------------+
			//|Minor      |              |              |              |          |         |         |         |         |         |         |         |
			//| version   |      0       |     10       |     90       |    0     |    0    |    1    |    2    |    0    |    0    |    1    |    1    |
			//+-----------------------------------------------------------------------------------------------------------------------------------------+
			if (Environment.OSVersion.Version.Major == 5)
			{
				if (Environment.OSVersion.Version.Minor == 1)
					return OSMinimumVersion.MinimumXP;
				else if (Environment.OSVersion.Version.Minor == 2)
					return OSMinimumVersion.Minimum2003orXP64bit;
			}
			else if (Environment.OSVersion.Version.Major >= 6)
			{
				if (Environment.OSVersion.Version.Minor >= 1)
					return OSMinimumVersion.MinimumWin7;
				else
					return OSMinimumVersion.MinimumVista;
			}
			return OSMinimumVersion.VeryOld;
		}

		private static Timer tmp60secondTimer;
		/// <summary>
		/// Register the application to recover and restart, remember to Unregister on main window/form close or application exit
		/// </summary>
		/// <param name="actionToBackupOnCrash">The action to be performed when a crash occurs, for instance saving the current data to disk.</param>
		/// <param name="actionOnRestarted">The action to be performed when the application has successfully recovered from a restart, for instance read the data again from disk which was saved on previous crash.</param>
		/// <param name="actionOnRestartReady">The action to be performed when the application is restart ready, this occurs after 60 seconds (1 minute).</param>
		public static void RegisterForRecoveryAndRestart(Action actionToBackupOnCrash, Action actionOnRestarted, Action actionOnRestartReady)
		{
			//Later look at Unregistering automatically on application exit, use Reflection because WPF/winforms will differ

			if (Environment.OSVersion.Version.Major < 6)//Not at least Vista (only supported from Vista)
			{
				AppTypeIndependant.ShowErrorMessage("Please note Application Recovery And Restart not supported in this version of windows, only supported from Vista and up");
				return;
			}

			if (actionToBackupOnCrash == null) actionToBackupOnCrash = delegate { };
			if (actionOnRestarted == null) actionOnRestarted = delegate { };
			if (actionOnRestartReady == null) actionOnRestartReady = delegate { };
			RegisterUnhandledExceptionHandler();

			// Create the delegate that will invoke the recovery method.
			//Win32Api.RecoveryDelegate recoveryCallback = 
			//    new Win32Api.RecoveryDelegate(RecoveryProcedure);
			uint pingInterval = 5000, flags = 0;

			// Register for recovery notification.
			int regReturn = Win32Api.RegisterApplicationRecoveryCallback(
				delegate
				{
					//Console.WriteLine("Recovery in progress for {0}", parameter);
					// Set up timer to notify WER that recovery work is in progress.
					Timer pinger = new Timer(delegate
					{
						bool isCancelled;
						Win32Api.ApplicationRecoveryInProgress(out isCancelled);
						if (isCancelled)
						{
							Console.WriteLine("Recovery has been canceled by user.");
							OwnAppsShared.ExitAppWithExitCode(2);
						}
					}, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

					WriteCrashReportFile(Process.GetCurrentProcess().ProcessName, true);

					actionToBackupOnCrash();

					// Indicate that recovery work is done.
					Console.WriteLine("Application shutting down...");
					Win32Api.ApplicationRecoveryFinished(true);
					return 0;
				},
				"",//parameter,
				pingInterval,
				flags);

			const string cmdlinePrefix = "/windowsrestart";
			var cmdLineArgs_ExcludingExePath =
				Environment.GetCommandLineArgs().Where(
				(el) =>
				{
					return 
						!el.Trim('"').Equals(Environment.GetCommandLineArgs()[0].Trim('"'))
						&& !el.Trim('"').Equals(cmdlinePrefix, StringComparison.InvariantCultureIgnoreCase);
				})
				.Select(s => "\"" + s.Trim('"') + "\"")
				.ToArray();
			string cmdLineStringForRecovery =
				string.Join(" ", cmdLineArgs_ExcludingExePath)
				+ " " + cmdlinePrefix;
			Win32Api.RegisterApplicationRestart(cmdLineStringForRecovery, (int)Win32Api.RestartRestrictions.None);

			if (Environment.GetCommandLineArgs().Select(s => s.Trim('"').ToLower()).Contains(cmdlinePrefix))
				//This instance was restarted
				actionOnRestarted();

			//Timer to elapse after 60 seconds (when app is restart ready)
			tmp60secondTimer = new Timer(
				delegate { actionOnRestartReady(); },
				null,
				TimeSpan.FromSeconds(62),//Add 2 seconds more than 60 just to be sure
				TimeSpan.FromMilliseconds(-1));
		}

		public static void UnregisterForRecoveryAndRestart()
		{
			Win32Api.UnregisterApplicationRecoveryCallback();
			Win32Api.UnregisterApplicationRestart();
		}

		public static string getFileNameWithDate(string ApplicationName)
		{
			return ApplicationName + " (" + DateTime.Now.ToString(SavetofileDateFormat) + ").log";
		}

		internal static void WriteCrashReportFile(string ApplicationName, bool forceWriteToDiskEvenIfNoUnhandledExceptions = false)
		{
			if (!forceWriteToDiskEvenIfNoUnhandledExceptions
				&& OwnUnhandledExceptionHandler.UnhandledExceptions.Count == 0)
				return;

			string dir = CrashReportsDirectory;
			if (dir.EndsWith("\\")) dir = dir.Substring(0, dir.Length - 1);
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
			string fullFilePath = dir + "\\" + getFileNameWithDate(ApplicationName);
			File.WriteAllText(fullFilePath,
				OwnUnhandledExceptionHandler.UnhandledExceptions.Count > 0
				? 
				"Unhandled exceptions: " + Environment.NewLine
					+ string.Join(Environment.NewLine + Environment.NewLine,
						OwnUnhandledExceptionHandler.UnhandledExceptions.Select(ex => ex.Message + Environment.NewLine + ex.StackTrace))
				: "No unhandled exceptions listed, unknown reason for application crash.");
			Process.Start("explorer", "/select,\"" + fullFilePath + "\"");
			OwnUnhandledExceptionHandler.UnhandledExceptions.Clear();
		}
	}
}