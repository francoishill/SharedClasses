using System;
using Microsoft.Win32;
using System.Diagnostics;

namespace SharedClasses
{
	public static class ShowNoCallbackNotificationInterop
	{
		public enum NotificationTypes { Subtle, Info, Success, Warning, Error }
		/*[Obsolete("Rather now use Notify instead", true)]
		public static void ShowNotificationNoCallback_UsingExternalApp(Action<string> actionOnError, string message, string title = null, NotificationTypes notificationType = NotificationTypes.Info, int secondsToShow = 3)
		{
		}*/

		/// <summary>
		/// Show a notification using the external application (ShowNoCallbackNotification.exe).
		/// </summary>
		/// <param name="actionOnError">What to do on an error (like if application not found).</param>
		/// <param name="message">The message of the notification.</param>
		/// <param name="title">The title of the notification.</param>
		/// <param name="notificationType">The type of notification (mainly determines the color).</param>
		/// <param name="secondsToShow">The number of seconds before the notification auto closes. Use -1 or -99 for infinite.</param>
		public static void Notify(Action<string> actionOnError, string message, string title = null, NotificationTypes notificationType = NotificationTypes.Info, int secondsToShow = 3)
		{
			if (actionOnError == null) actionOnError = delegate { };

			/*
			We do not use external process anymore, otherwise we must auto install it on each user's machine 
			string notifAppExe = RegistryInterop.GetAppPathFromRegistry("ShowNoCallbackNotification.exe");
			if (notifAppExe == null)
			{
				//ShowNotification("Cannot show Notification: " + errIfFail);
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
						secondsToShow));*/
		}
		private static string GetAppNameForTitle() { return System.IO.Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]); }
	}
}