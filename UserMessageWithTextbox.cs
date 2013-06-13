using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SharedClasses
{
	public partial class UserMessageWithTextbox : Form
	{
		string[] rootDirsUsedIfFilesSelected;//If the selected file is not a fullpath, compbine with these to allow multiple files
		private UserMessageWithTextbox(string userMessage, string textForTextbox, string title, string[] RootDirsUsedIfFilesSelected = null)
		{
			InitializeComponent();

			this.rootDirsUsedIfFilesSelected = RootDirsUsedIfFilesSelected;
			if (this.rootDirsUsedIfFilesSelected != null
				&& this.rootDirsUsedIfFilesSelected.Length > 0)
			{
				labelIfTheRootDirsAreSet.Visible = true;

				string rootDirsJoinedWithNewline = string.Join(Environment.NewLine, this.rootDirsUsedIfFilesSelected);
				toolTip1.SetToolTip(labelIfTheRootDirsAreSet, rootDirsJoinedWithNewline);

				string buttonTextPostfix = string.Format(" - {0} dirs", rootDirsUsedIfFilesSelected.Length); ;

				buttonGotoSelectedPath.Text += buttonTextPostfix;
				toolTip1.SetToolTip(buttonGotoSelectedPath, "Search for file in these directories:" + Environment.NewLine + rootDirsJoinedWithNewline);

				buttonOpenSelectedPath.Text += buttonTextPostfix;
				toolTip1.SetToolTip(buttonOpenSelectedPath, "Search for file in these directories:" + Environment.NewLine + rootDirsJoinedWithNewline);
			}
			else
			{
				buttonGotoSelectedPath.Visible = false;
				buttonOpenSelectedPath.Visible = false;
			}

			label1.Text = userMessage;
			textBox1.Text = textForTextbox;
			this.Text = title;
		}

		public static void ShowUserMessageWithTextbox(string userMessage, string textForTextbox, string title = "Message", IWin32Window window = null, string[] RootDirsUsedIfFilesSelected = null)
		{
			var tmpForm = new UserMessageWithTextbox(userMessage, textForTextbox, title, RootDirsUsedIfFilesSelected);
			if (window != null)
				tmpForm.ShowDialog(window);
			else
				tmpForm.ShowDialog();
			tmpForm.Dispose();
			tmpForm = null;
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			this.DialogResult = System.Windows.Forms.DialogResult.OK;
		}

		private bool GetSelectedPathFromText(out string[] selectedPaths, bool showErrorIfNotFound)
		{
			var selection = textBox1.SelectedText.Trim('\'', '"', ' ');

			if (!Directory.Exists(selection)
				&& !File.Exists(selection))
			{
				if (rootDirsUsedIfFilesSelected != null)
				{
					List<string> fullPathsWhichExist = new List<string>();
					foreach (var rootdir in rootDirsUsedIfFilesSelected)
					{
						try
						{
							if (!string.IsNullOrWhiteSpace(selection))
							{
								string fullPath = Path.Combine(rootdir, selection);
								if (Directory.Exists(fullPath)
									|| File.Exists(fullPath))
									fullPathsWhichExist.Add(fullPath);
							}
						}
						catch { }
					}

					if (fullPathsWhichExist.Count > 0)
					{
						selectedPaths = fullPathsWhichExist.ToArray();
						return true;
					}
				}

				if (showErrorIfNotFound)
					UserMessages.ShowErrorMessage("File/folder not found: " + selection);
				selectedPaths = null;
				return false;
			}
			else
			{
				selectedPaths = new string[] { selection };
				return true;
			}
		}

		private void buttonOpenSelectedPath_Click(object sender, EventArgs e)
		{
			string[] paths;
			if (!GetSelectedPathFromText(out paths, true))
				return;

			foreach (var path in paths)
				Process.Start(path);
		}

		private void buttonGotoSelectedPath_Click(object sender, EventArgs e)
		{
			string[] paths;
			if (!GetSelectedPathFromText(out paths, true))
				return;

			foreach (var path in paths)
				Process.Start("explorer", string.Format("/select,\"{0}\"", path));
		}

		private void RecheckIfFilepathSelected()
		{
			string[] paths;
			bool pathSelected = GetSelectedPathFromText(out paths, false);
			buttonGotoSelectedPath.Enabled = pathSelected;
			buttonOpenSelectedPath.Enabled = pathSelected;
		}

		private void textBox1_MouseUp(object sender, MouseEventArgs e)
		{
			RecheckIfFilepathSelected();
		}

		private void textBox1_KeyUp(object sender, KeyEventArgs e)
		{
			RecheckIfFilepathSelected();
		}
	}
}
