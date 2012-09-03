using System;
using Microsoft.Win32;
using System.Diagnostics;

namespace SharedClasses
{
	public static class ShowNoCallbackNotificationInterop
	{
		public enum NotificationTypes { Subtle, Info, Success, Warning, Error }
		public static void ShowNotificationNoCallback_UsingExternalApp(Action<string> actionOnError, string message, string title = null, NotificationTypes notificationType = NotificationTypes.Info, int secondsToShow = 3)
		{
			string notifAppExe = RegistryInterop.GetAppPathFromRegistry("ShowNoCallbackNotification.exe");
			if (notifAppExe == null)
			{
				//ShowNotification("Cannot show Notification: " + errIfFail);
				if (actionOnError != null)
					actionOnError("Cannot show Notification, cannot find ShowNoCallbackNotification.exe in App Paths of Regsitry.");
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