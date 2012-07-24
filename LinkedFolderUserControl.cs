using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace SharedClasses
{
	public partial class LinkedFolderUserControl : UserControl
	{
		public LinkedFolderUserControl() { }

		public LinkedFolderUserControl(string localRootDir = null, string ftpRootUrl = null, string ftpUsername = null, string ftpPassword = null, List<string> excludedFolders = null)
		{
			InitializeComponent();

			textBoxLocalRootDirectory.Text = localRootDir ?? "";
			textBoxFtpRootUrl.Text = ftpRootUrl ?? "";
			textBoxFtpUsername.Text = ftpUsername ?? "";
			textBoxFtpPassword.Text = ftpPassword ?? "";
			textBoxExcludedFolders.Text = excludedFolders == null ? "" : string.Join(Environment.NewLine, excludedFolders);
		}

		private void buttonBrowseForLocalFolder_Click(object sender, EventArgs e)
		{
			string selectedPath = FileSystemInterop.SelectFolder(
				"Please select the local folder",
				Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.IndexOf('\\')));
			if (selectedPath != null)
				textBoxLocalRootDirectory.Text = selectedPath.TrimEnd('\\');
		}

		private bool ConfirmToUseAnonymousUsername()
		{
			bool confirmed = UserMessages.Confirm("FTP username is empty, use anonymous username?", GetThisUsercontrolName());
			if (confirmed)
				textBoxFtpUsername.Text = "anonymous";
			return confirmed;
		}

		private bool ConfirmToAutomaticallyConvertToRelativePaths()
		{
			bool confirmed = UserMessages.Confirm("There are some paths in the excluded list which are not RELATIVE paths, convert them automatically?", GetThisUsercontrolName());
			if (confirmed)
			{
				var paths = GetExcludedPathsFromText(textBoxExcludedFolders.Text);
				//foreach (string path in paths)
				for (int i = 0; i < paths.Length; i++)
				{
					if (paths[i].Contains(':'))
					{
						string rootPath = textBoxLocalRootDirectory.Text.TrimEnd('\\');
						if (paths[i].StartsWith(rootPath + "\\", StringComparison.InvariantCultureIgnoreCase))
							paths[i] = paths[i].Substring(rootPath.Length + 1);
						else
						{
							UserMessages.ShowWarningMessage(string.Format("Cannot get relative path from \"{0}\" when the local root directory is \"{1}\"", paths[i], rootPath));
							return false;
						}
					}
				}
				textBoxExcludedFolders.Text = string.Join(Environment.NewLine, paths);
				Application.DoEvents();
			}
			return confirmed;
		}

		private string[] GetExcludedPathsFromText(string text)
		{
			return text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		}

		public bool ValidateInputs()
		{
			if (string.IsNullOrWhiteSpace(textBoxLocalRootDirectory.Text))
				UserMessages.ShowWarningMessage("Please enter (or choose) a local root directory.", "Error for " + GetThisUsercontrolName());
			else if (!Directory.Exists(textBoxLocalRootDirectory.Text))
				UserMessages.ShowWarningMessage("Cannot find local directory: " + textBoxLocalRootDirectory.Text, "Error for " + GetThisUsercontrolName());
			else if (string.IsNullOrWhiteSpace(textBoxFtpRootUrl.Text))
				UserMessages.ShowWarningMessage("Please enter FTP url.", "Error for " + GetThisUsercontrolName());
			else if (string.IsNullOrWhiteSpace(textBoxFtpUsername.Text) && !ConfirmToUseAnonymousUsername())
				UserMessages.ShowWarningMessage("Please enter FTP username.", "Error for " + GetThisUsercontrolName());
			else if (string.IsNullOrWhiteSpace(textBoxFtpPassword.Text) && !UserMessages.Confirm("FTP password is empty, continue with blank password?", GetThisUsercontrolName()))
				UserMessages.ShowWarningMessage("Please enter FTP password.", "Error for " + GetThisUsercontrolName());
			else if (!ValidateExcludedPathsAllRelative() && !ConfirmToAutomaticallyConvertToRelativePaths())
				UserMessages.ShowWarningMessage("Please ensure all the paths are relative before continuing.");
			else return true;

			return false;
		}

		private bool ValidateExcludedPathsAllRelative()
		{
			var paths = GetExcludedPathsFromText(textBoxExcludedFolders.Text);
			return paths.Count(p => p.Contains(':')) == 0;
		}

		private void buttonBrowseForLocalFolder_Click_1(object sender, EventArgs e)
		{
			string folder = FileSystemInterop.SelectFolder("Please select the local root folder for " + GetThisUsercontrolName());
			if (folder != null)
				textBoxLocalRootDirectory.Text = folder;
		}

		private string GetThisUsercontrolName()
		{
			return groupBox1.Text;
		}
	}
}
