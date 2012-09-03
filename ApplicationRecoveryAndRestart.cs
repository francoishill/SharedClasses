using System;
using System.IO;
using Microsoft.WindowsAPICodePack.ApplicationServices;
using MS.WindowsAPICodePack.Internal;

/// <summary>
/// Need to add a reference to the Microsoft.WindowsAPICodePack.dll in SharedClasses\DLLs
/// </summary>
public class ApplicationRecoveryAndRestart
{
	public static Action FunctionToPerformOnCrash;

	public const string CrashReportsDirectory = @"C:\Francois\Crash reports";
	public const string SavetofileDateFormat = @"yyyy MM dd \a\buildTask HH mm ss";

	public static void RegisterApplicationRecoveryAndRestart(Action functionToPerformOnCrash, Action callbackWhenApplicationIsRestartReadyAfter60seconds, Action<string> actionOnError)
	{
		//Note the functionToPerformOnCrash will be performed event if 60 seconds has not passed

		//Make sure WindowsAPICodePack dll is present for application recovery/restart
		string dllpath = Path.Combine(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]), "Microsoft.WindowsAPICodePack.dll");
		if (!File.Exists(dllpath))
		{
			actionOnError("Cannot start 'StandaloneUploader' withouth Microsoft.WindowsAPICodePack.dll: " + Environment.NewLine + dllpath);
			Environment.Exit(0);
		}

		if (!CoreHelpers.RunningOnVista && !CoreHelpers.RunningOnWin7)
		{
			return;
		}

		FunctionToPerformOnCrash = functionToPerformOnCrash;

		// register for Application Restart
		ApplicationRestartRecoveryManager.RegisterForApplicationRestart(
				new RestartSettings("/restart", RestartRestrictions.NotOnPatch | RestartRestrictions.NotOnReboot));

		// register for Application Recovery
		//RecoverySettings recoverySettings =  new RecoverySettings(new RecoveryData(PerformRecovery, null), 0);
		//ApplicationRestartRecoveryManager.RegisterForApplicationRecovery(recoverySettings);
		RecoveryData data = new RecoveryData(new RecoveryCallback(PerformRecovery), null);
		RecoverySettings settings = new RecoverySettings(data, 0);

		ApplicationRestartRecoveryManager.RegisterForApplicationRecovery(settings);

		if (callbackWhenApplicationIsRestartReadyAfter60seconds != null)
		{
			System.Threading.Timer timer = new System.Threading.Timer(
				delegate
				{
					callbackWhenApplicationIsRestartReadyAfter60seconds();
				},
				null,
				(int)TimeSpan.FromMinutes(1).TotalMilliseconds,
				System.Threading.Timeout.Infinite);
		}
	}

	/// <summary>
	/// Performs recovery by saving the state 
	/// </summary>
	/// <param name="parameter">Unused.</param>
	/// <returns>Unused.</returns>
	private static int PerformRecovery(object parameter)
	{
		try
		{
			ApplicationRestartRecoveryManager.ApplicationRecoveryInProgress();

			if (FunctionToPerformOnCrash != null)
				FunctionToPerformOnCrash.Invoke();

			ApplicationRestartRecoveryManager.ApplicationRecoveryFinished(true);
		}
		catch
		{
			ApplicationRestartRecoveryManager.ApplicationRecoveryFinished(false);
		}
		return 0;
	}

	public static void UnregisterApplicationRecoveryAndRestart()
	{
		if (!CoreHelpers.RunningOnVista)
		{
			return;
		}

		ApplicationRestartRecoveryManager.UnregisterApplicationRestart();
		ApplicationRestartRecoveryManager.UnregisterApplicationRecovery();
	}

	public static void TestCrash(bool showWarningFirst, Func<string, bool> functionToConfirm)
	{
		if (!showWarningFirst || functionToConfirm("Program will now perform a crash to set RecoveryAndRestart, are you sure?"))
			//MessageBox.Show("Program will now perform a crash to set RecoveryAndRestart, are you sure?", "Confirm", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
			Environment.FailFast("Testing the Application Restart Recovery");
	}

	public static string getFileNameWithDate(string ApplicationName)
	{
		return ApplicationName + " (" + DateTime.Now.ToString(SavetofileDateFormat) + ").log";
	}

	public static void WriteCrashReportFile(string ApplicationName, string crashMessage)
	{
		string dir = CrashReportsDirectory;
		if (dir.EndsWith("\\")) dir = dir.Substring(0, dir.Length - 1);
		if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
		string fullFilePath = dir + "\\" + getFileNameWithDate(ApplicationName);
		File.WriteAllText(fullFilePath, crashMessage);
	}
}