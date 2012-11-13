using System;
using System.Diagnostics;
using libsnarl;
using System.Collections.Generic;//If snarl is installed, found in COM references (snarl.library (V42))

namespace SharedClasses
{
	public static class SnarlInterop
	{
		private static Dictionary<string, SnarlApp> createdApps = new Dictionary<string, SnarlApp>();

		private static string GetDictionaryKeyFromAppProperties(string appName, string appDisplayName, string icon, string remoteHost, string password)
		{
			return string.Join("|", appName, appDisplayName, icon ?? "NOICON", remoteHost, password ?? "NOPASSWORD");
		}

		private static SnarlApp GetSnarlApp(out S_STATUS_CODE? codeIfFailed, string appName, string appDisplayName, string icon = null, string remoteHost = "localhost", string password = null)
		{
			//Just used so we dont re-create an app each time
			var dictionaryKey = GetDictionaryKeyFromAppProperties(appName, appDisplayName, icon, remoteHost, password);
			if (createdApps.ContainsKey(dictionaryKey))
			{
				codeIfFailed = null;
				return createdApps[dictionaryKey];
			}

			var notifClass = new NotificationClasses();
			var snarlApp = new SnarlApp();

			var setToResult = snarlApp.SetTo(remoteHost, appName, appDisplayName, icon, ref notifClass, password);
			if (setToResult != S_STATUS_CODE.S_SUCCESS)
			{
				codeIfFailed = null;
				return null;
			}
			else
			{
				codeIfFailed = setToResult;
				createdApps.Add(dictionaryKey, snarlApp);
				return snarlApp;
			}
		}

		public static S_STATUS_CODE RegisterSnarl(string appName, string appDisplayName, string icon = null, string remoteHost = "localhost", string password = null)
		{
			S_STATUS_CODE? outCodeIfFailed;
			var snarlApp = GetSnarlApp(out outCodeIfFailed, appName, appDisplayName, icon, remoteHost, password);
			if (snarlApp == null)
				return outCodeIfFailed.Value;
			return snarlApp.Register();

			/*try
			{
				Process.Start(
					@"C:\Program Files (x86)\full phat\Snarl\tools\heysnarl.exe",
					"register?app-sig=app/" + appName + "&title=" + appDisplayName);
			}
			catch { }*/
		}

		public static S_STATUS_CODE NotifySnarl(string appName, string appDisplayName, string title, string msg, int durationMilli = 1000, string icon = null, string remoteHost = "localhost", string password = null)
		{
			S_STATUS_CODE? outCodeIfFailed;
			var snarlApp = GetSnarlApp(out outCodeIfFailed, appName, appDisplayName, icon, remoteHost, password);
			if (snarlApp == null)
				return outCodeIfFailed.Value;
			return snarlApp.Notify(null, title, msg, icon, durationMilli, null, null);

			/*try
			{
				Process.Start(
					@"C:\Program Files (x86)\full phat\Snarl\tools\heysnarl.exe",
					"notify?app-sig=app/" + appName + "&title=" + title + "&text=" + msg);
			}
			catch { }*/
		}

		public static void UnregisterSnarl(string appName, string appDisplayName, string title, string msg, int durationMilli = 1000, string icon = null, string remoteHost = "localhost", string password = null)
		{
			S_STATUS_CODE? outCodeIfFailed;
			var snarlApp = GetSnarlApp(out outCodeIfFailed, appName, appDisplayName, icon, remoteHost, password);
			if (snarlApp == null)
				return;
			snarlApp.Unregister();

			/*try
			{
				Process.Start(
					@"C:\Program Files (x86)\full phat\Snarl\tools\heysnarl.exe",
					"unregister?app-sig=app/" + appName);
			}
			catch { }*/
		}
	}
}