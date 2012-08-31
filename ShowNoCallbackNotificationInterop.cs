using System;
using Microsoft.Win32;
using System.Diagnostics;

namespace SharedClasses
{
	public static class ShowNoCallbackNotificationInterop
	{
		public enum NotificationTypes { Subtle, Info, Success, Warning, Error }

		public static string GetExePathOfShowNoCallbackNotification(out string errorIfFailed)
		{
			string LMsubpath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\ShowNoCallbackNotification.exe";
			using (RegistryKey rk = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryInterop.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32)
				.OpenSubKey(LMsubpath))
			{
				if (rk != null)
				{
					errorIfFailed = null;
					try
					{
						return rk.GetValue(null).ToString();
					}
					catch (Exception exc)
					{
						errorIfFailed = exc.Message;
						return null;
					}
				}
				else
				{
					errorIfFailed = "Cannot open registry key [LocalMachine]\\" + LMsubpath;
					return null;
				}
			}
		}
		public static void ShowNotificationNoCallback_UsingExternalApp(Action<string> actionOnError, string message, string title = null, NotificationTypes notificationType = NotificationTypes.Info, int secondsToShow = 3)
		{
			string errIfFail;
			string notifAppExe = GetExePathOfShowNoCallbackNotification(out errIfFail);
			if (notifAppExe == null)
			{
				//ShowNotification("Cannot show Notification: " + errIfFail);
				if (actionOnError != null)
					actionOnError("Cannot show Notification: " + errIfFail);
			}
			else
				//c:\path\to\ShowNoCallbackNotification.exe "Title" "Message" NotificationType SecondsToShow
				Process.Start(
					notifAppExe,
					string.Format("\"{0}\" \"{1}\" {2} {3}",
						(title ?? GetAppNameForTitle()).Trim('\"'),
						message.Trim('\"'),
						notificationType.ToString(),
						secondsToShow));
		}
		private static string GetAppNameForTitle() { return System.IO.Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]); }
	}
}