using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;

namespace SharedClasses
{
	public partial class AutoUpdatingForm : Form
	{
		/*Additional dependencies and sample code:
		Class: fastJSON
		Class: ThreadingInterop
		Class: UserMessages
		Class: WebInterop
		*/

		private Action exitApplicationAction;
		private const string ftpUsername = "ownapps";
		private const string ftpPassword = "ownappsverylongpassword";

		private PublishDetails newerversionDetails;
		private AutoUpdatingForm(string currentVersion, PublishDetails newerversionDetails, Action exitApplicationAction)
		{
			InitializeComponent();
			this.newerversionDetails = newerversionDetails;
			this.Text = "Update available for " + newerversionDetails.ApplicationName;

			this.labelMessage.Text = newerversionDetails.ApplicationName + " has an update available";
			this.labelCurrentVersion.Text = "Current version is " + currentVersion;
			this.labelNewVersion.Text = string.Format(
				"Newest version online is {0} ({1} kBs to be downloaded)",
				newerversionDetails.ApplicationVersion,
				GetKilobytesFromBytes(newerversionDetails.SetupSize));
			this.exitApplicationAction = exitApplicationAction;
		}

		private static double GetKilobytesFromBytes(long bytes, int decimals = 3)
		{
			return Math.Round((double)bytes / (double)1024, decimals);
		}

		//true=uptodate,false=neweravailable,null=errorCheckMessage
		public static bool? IsApplicationUpToDate(string ApplicationName, string installedVersion, out string errorIfNull, out PublishDetails detailsIfNewer)
		{
			detailsIfNewer = null;//Only details if newer version available
			PublishDetails onlineAppDetails = new PublishDetails();
			string errIfFail;
			bool populatesuccess = WebInterop.PopulateObjectFromOnline(
				PublishDetails.OnlineJsonCategory,
				ApplicationName + PublishDetails.LastestVersionJsonNamePostfix,
				onlineAppDetails,
				out errIfFail,
				TimeSpan.FromSeconds(10));
			if (populatesuccess)
			{
				//return CompareVersions(installedVersion, onlineAppDetails.ApplicationVersion);
				string onlineVersion = onlineAppDetails.ApplicationVersion;
				string versionsConcatenated = string.Format("InstalledVersion = {0}, OnlineVersion = {1}", installedVersion ?? "", onlineVersion ?? "");
				if (string.IsNullOrWhiteSpace(installedVersion) || string.IsNullOrWhiteSpace(onlineVersion))
				{
					errorIfNull = "InstalledVersion AND/OR OnlineVersion is empty: " + versionsConcatenated;
					return null;
				}
				string[] installedSplitted = installedVersion.Split('.');
				string[] onlineSplitted = onlineVersion.Split('.');
				if (installedSplitted.Length != onlineSplitted.Length)
				{
					errorIfNull = "InstalledVersion and OnlineVersion not in same format: " + versionsConcatenated;
					return null;
				}

				int tmpint;
				bool fail = false;
				installedSplitted.ToList().ForEach((s) => { if (!int.TryParse(s, out tmpint)) fail = true; });
				onlineSplitted.ToList().ForEach((s) => { if (!int.TryParse(s, out tmpint)) fail = true; });
				if (fail)
				{
					errorIfNull = "InstalledVersion and OnlineVersion must have integers between dots: " + versionsConcatenated;
					return null;
				}

				//if (installedAppVersion.Equals(onlineVersion, StringComparison.InvariantCultureIgnoreCase))
				//    return VersionComparison.UpToDate;

				for (int i = 0; i < installedSplitted.Length; i++)
				{
					int tmpInstalledInt;
					int tmpOnlineInt;
					tmpInstalledInt = int.Parse(installedSplitted[i]);
					tmpOnlineInt = int.Parse(onlineSplitted[i]);

					if (tmpInstalledInt == tmpOnlineInt)
						continue;
					if (tmpInstalledInt > tmpOnlineInt)
					{
						errorIfNull = "InstalledVersion is newer than OnlineVersion: " + versionsConcatenated;
						return null;
					}
					else
					{
						errorIfNull = null;
						detailsIfNewer = onlineAppDetails;
						return false;
					}
				}
				errorIfNull = null;
				return true;
			}
			else
			{
				if (errIfFail == WebInterop.cErrorIfNotFoundOnline)
					errorIfNull = "Update information not stored online yet for " + ApplicationName + ".";
				else
					errorIfNull = errIfFail;
				return null;
			}
		}

		public static string GetAppFullPath() { return Environment.GetCommandLineArgs()[0]; }
		public static string GetApplicationName() { return Path.GetFileNameWithoutExtension(GetAppFullPath()); }
		
		[Obsolete("Please use CheckForUpdates in AutoUpdating.cs, also then remove reference to AutoUpdatingForm.cs from project.", true)]
		public static void CheckForUpdates(Action exitApplicationAction, Action<string> ActionIfUptoDate_Versionstring, Action<string> ActionIfUnableToCheckForUpdates, bool ShowModally = true)//string ApplicationName, string InstalledVersion)
		//public static void CheckForUpdates(Action exitApplicationAction, bool ShowModally = true, Action<bool?, PublishDetails, PublishDetails> ActionAfterCheck_Uptodate_Currentdetails_OnlineDetails = null)//string ApplicationName, string InstalledVersion)
		{
			ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			{
				string appfullpath = GetAppFullPath();
				string ApplicationName = GetApplicationName();
				//var versionfileFullpath = appfullpath + ".version";
				var InstalledVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(appfullpath).FileVersion;//File.Exists(versionfileFullpath) ? File.ReadAllText(versionfileFullpath).Trim() : "";

				PublishDetails detailsIfNewer;
				string errIfFail;
				bool? uptodate = IsApplicationUpToDate(ApplicationName, InstalledVersion, out errIfFail, out detailsIfNewer);
				if (uptodate == null)
				{
					if (ActionIfUnableToCheckForUpdates != null)
						ActionIfUnableToCheckForUpdates(errIfFail);
					else
						WpfNotificationWindow.ShowNotification("Cannot check for updates of application " + ApplicationName + ": " + errIfFail,
						notificationType: ShowNoCallbackNotificationInterop.NotificationTypes.Warning,
						onCloseCallback_WasClickedToCallback: (closeobj, wasclicked) =>
						{
							WpfNotificationWindow.CloseNotificationWindow();
						},
						timeout: null);
				}
				//UserMessages.ShowWarningMessage("Unable to check for updates for \"" + ApplicationName + "\": " + Environment.NewLine + errIfFail);
				else if (uptodate == false)//Newer version available
				{
					var tmpform = new AutoUpdatingForm(InstalledVersion, detailsIfNewer, delegate
					{
						WpfNotificationWindow.CloseNotificationWindow();
						exitApplicationAction();
					});
					WpfNotificationWindow.ShowNotification(
						string.Format("New update (from {0} to {1}) available for {2} (click to update)", InstalledVersion, detailsIfNewer.ApplicationVersion, ApplicationName),
						ShowNoCallbackNotificationInterop.NotificationTypes.Success,
						null,						
						leftClickCallback: (frm) =>
						{
							AutoUpdatingForm thisform = frm as AutoUpdatingForm;
							if (ShowModally)
								thisform.ShowDialog();
							else
								thisform.Show();
						},
						leftClickCallbackArgument: tmpform,
						onCloseCallback_WasClickedToCallback: (closeobj, wasclicked) =>
						{
							WpfNotificationWindow.CloseNotificationWindow();
						});
				}
				else//Up to date
				{
					if (ActionIfUptoDate_Versionstring != null)
						ActionIfUptoDate_Versionstring(InstalledVersion);
					else
						WpfNotificationWindow.ShowNotification(ApplicationName + " is up to date, version " + InstalledVersion,
						notificationType: ShowNoCallbackNotificationInterop.NotificationTypes.Subtle,
						onCloseCallback_WasClickedToCallback: (closeobj, wasclicked) =>
						{
							WpfNotificationWindow.CloseNotificationWindow();
						},
						timeout: TimeSpan.FromSeconds(2));
				}
			},
			false);
		}

		private void linkLabelClickHereLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			bool restartDownloadRequired;
		restartDownload:
			restartDownloadRequired = false;
			labelStatus.Visible = true;
			labelStatus.Text = "Please wait, downloading...";
			progressBar1.Visible = true;
			progressBar1.Maximum = 100;

			DateTime startTime = DateTime.Now;
			using (System.Net.WebClient client = new System.Net.WebClient())
			{
				client.Credentials = new System.Net.NetworkCredential(
					ftpUsername,//GlobalSettings.VisualStudioInteropSettings.Instance.FtpUsername,
					ftpPassword);//GlobalSettings.VisualStudioInteropSettings.Instance.FtpPassword);
				bool isComplete = false;
				client.DownloadFileCompleted += (sn, ev) =>
				{
					isComplete = true;
				};
				client.DownloadProgressChanged += (sn, ev) =>
				{
					if (!isComplete)
					{
						int progressPercentage = (int)Math.Round((double)100 * (double)ev.BytesReceived / (double)newerversionDetails.SetupSize);//ev.ProgressPercentage;
						double kiloBytesPerSecond = Math.Round(GetKilobytesFromBytes(ev.BytesReceived) / DateTime.Now.Subtract(startTime).TotalSeconds, 3);
						string statusMessage = string.Format(
									"Downloading {0}/{1} at {2} kB/s",
									ev.BytesReceived,
									newerversionDetails.SetupSize, //ev.TotalBytesToReceive,
									kiloBytesPerSecond);
						//if (this.InvokeRequired)
						//    this.Invoke((Action)delegate
						//    {
						//        progressBar1.Value = ev.ProgressPercentage;
						//        labelStatus.Text = statusMessage;
						//    });
						//else
						//{
						progressBar1.Value = progressPercentage;
						labelStatus.Text = statusMessage;

						if (ev.BytesReceived == newerversionDetails.SetupSize)
						{
							labelStatus.Text = string.Format("Download complete ({0} bytes)", ev.BytesReceived);
							progressBar1.Visible = false;
						}
						//}
					}
				};
				var downloadFilename = Path.GetTempPath().TrimEnd('\\') + "\\Setup_Newest_" + newerversionDetails.ApplicationName + ".exe";
				client.DownloadFileAsync(new Uri(newerversionDetails.FtpUrl), downloadFilename);
				while (!isComplete) { Application.DoEvents(); }
				if (downloadFilename.FileToMD5Hash() != newerversionDetails.MD5Hash)
				{
					if (UserMessages.Confirm("The downloaded file is corrupt (different MD5Hash), download it again?"))
						restartDownloadRequired = true;
				}
				else if (UserMessages.Confirm("The download is complete, do you want to close this application and install new version?"))
				{
					labelMessage.Text = "Please be patient, busy closing application to install download...";
					labelCurrentVersion.Visible = false;
					labelNewVersion.Visible = false;
					linkLabelClickHereLink.Visible = false;
					progressBar1.Visible = false;
					labelStatus.Visible = false;
					Application.DoEvents();
					Process.Start(downloadFilename);
					if (exitApplicationAction == null)
						Application.Exit();
					else
						exitApplicationAction();
					this.Close();
				}
				else
					Process.Start("explorer", "/select,\"" + downloadFilename + "\"");
			}

			if (restartDownloadRequired)
				goto restartDownload;
		}

		private void AutoUpdatingForm_Shown(object sender, EventArgs e)
		{
			this.BringToFront();
			this.TopMost = false;
			this.TopMost = true;
		}
	}

	//public class PublishDetails
	//{
	//    public const string OnlineJsonCategory = "Own Applications";
	//    public const string LastestVersionJsonNamePostfix = " - latest";

	//    public string ApplicationName;
	//    public string ApplicationVersion;
	//    public long SetupSize;
	//    public string MD5Hash;
	//    public DateTime PublishedDate;
	//    public string FtpUrl;
	//    //TODO: May want to add TracUrl here
	//    public PublishDetails() { }
	//    public PublishDetails(string ApplicationName, string ApplicationVersion, long SetupSize, string MD5Hash, DateTime PublishedDate, string FtpUrl)
	//    {
	//        this.ApplicationName = ApplicationName;
	//        this.ApplicationVersion = ApplicationVersion;
	//        this.SetupSize = SetupSize;
	//        this.MD5Hash = MD5Hash;
	//        this.PublishedDate = PublishedDate;
	//        this.FtpUrl = FtpUrl;
	//    }
	//    public string GetJsonString() { return WebInterop.GetJsonStringFromObject(this, true); }
	//}
}
