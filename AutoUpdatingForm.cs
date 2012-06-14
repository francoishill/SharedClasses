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

namespace SharedClasses
{
	public partial class AutoUpdatingForm : Form
	{
		private PublishDetails newerversionDetails;
		private AutoUpdatingForm(string currentVersion, PublishDetails newerversionDetails)
		{
			InitializeComponent();
			this.newerversionDetails = newerversionDetails;
			this.Text = "Update available for " + newerversionDetails.ApplicationName;

			this.labelMessage.Text = newerversionDetails.ApplicationName + " has an update available";
			this.labelCurrentVersion.Text = "Current version is " + currentVersion;
			this.labelNewVersion.Text = "Newest version online is " + newerversionDetails.ApplicationVersion;


		}

		public static void CheckForUpdates(bool ShowModally = true)//string ApplicationName, string InstalledVersion)
		{
			ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			{
				var appfullpath = Environment.GetCommandLineArgs()[0];
				//var appdir = Path.GetDirectoryName(appfullpath);
				var ApplicationName = Path.GetFileNameWithoutExtension(appfullpath);
				var versionfileFullpath = appfullpath + ".version";
				var InstalledVersion = File.Exists(versionfileFullpath) ? File.ReadAllText(versionfileFullpath).Trim() : "";

				PublishDetails detailsIfNewer;
				string errIfFail;
				bool? uptodate = AutoUpdating.IsApplicationUpToDate(ApplicationName, InstalledVersion, out errIfFail, out detailsIfNewer);
				if (uptodate == null)
					UserMessages.ShowWarningMessage("Unable to check for updates: " + errIfFail);
				else if (uptodate == false)
				{
					var tmpform = new AutoUpdatingForm(InstalledVersion, detailsIfNewer);
					if (ShowModally)
						tmpform.ShowDialog();
					else
						tmpform.Show();
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
					GlobalSettings.VisualStudioInteropSettings.Instance.FtpUsername,
					GlobalSettings.VisualStudioInteropSettings.Instance.FtpPassword);
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
						string statusMessage = string.Format(
									"Downloaded {0}/{1} at {2}",
									ev.BytesReceived,
									newerversionDetails.SetupSize, //ev.TotalBytesToReceive,
									ev.BytesReceived / DateTime.Now.Subtract(startTime).TotalSeconds);
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
					Application.Exit();
				}
				else
					Process.Start("explorer", "/select,\"" + downloadFilename + "\"");
			}

			if (restartDownloadRequired)
				goto restartDownload;
		}
	}
}
