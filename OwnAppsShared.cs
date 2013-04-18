using System;
using System.IO;

namespace SharedClasses
{
	public static class OwnAppsShared
	{
		public static string AppNameIfIsDllAndNotExePath = null;

		public static string GetAppFullPath() { return Environment.GetCommandLineArgs()[0]; }
		public static string GetApplicationName()
		{
			if (AppNameIfIsDllAndNotExePath != null)
				return AppNameIfIsDllAndNotExePath;//If we are running an "embedded" app, which is basically a DLL but loaded from an EXE

			string applicationName = Path.GetFileNameWithoutExtension(GetAppFullPath());
			if (applicationName.EndsWith(".vshost", StringComparison.InvariantCultureIgnoreCase))
				applicationName = applicationName.Substring(0, applicationName.Length - ".vshost".Length);
			return applicationName;
		}

		public static void ExitAppWithExitCode(int exitCode = 0)
		{
			ResourceUsageTracker.FlushAllCurrentLogLines();
			Environment.Exit(exitCode);
		}
	}
}