using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace SharedClasses
{
	public partial class NewLinkedFolder : Form
	{
		public NewLinkedFolder()
		{
			InitializeComponent();

			checkBoxTopmost.Checked = this.TopMost;
			checkBoxTopmost.CheckedChanged += delegate { this.TopMost = checkBoxTopmost.Checked; };

			foreach (Control con in this.Controls)
				con.KeyPress += new KeyPressEventHandler(global_KeyPress);

			this.HandleCreated += delegate
			{
				var workingArea = Screen.FromHandle(this.Handle).WorkingArea;
				this.MaximumSize = new Size(workingArea.Width - 100, workingArea.Height - 100);
				this.tableLayoutPanel1.MaximumSize = new Size(
					this.MaximumSize.Width - (this.Width - this.tableLayoutPanel1.Width),
					this.MaximumSize.Height - (this.Height - this.tableLayoutPanel1.Height)
					);
			};

			RemoveAllUsercontrols();
			//AddAnotherLinkedFolder();
		}

		public void RemoveAllUsercontrols()
		{
			tableLayoutPanel1.Controls.Clear();
		}

		public List<LinkedFolderUserControl> GetUsercontrols()
		{
			List<LinkedFolderUserControl> tmplist = new List<LinkedFolderUserControl>();
			foreach (Control con in this.tableLayoutPanel1.Controls)
			{
				LinkedFolderUserControl lu = con as LinkedFolderUserControl;
				if (lu == null) continue;
				tmplist.Add(lu);
			}
			return tmplist;
		}

		private void buttonBrowseForLocalFolder_Click(object sender, EventArgs e)
		{
			//string selectedPath = FileSystemInterop.SelectFolder(
			//    "Please select the local folder",
			//    Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.IndexOf('\\')));
			//if (selectedPath != null)
			//    textBoxLocalRootDirectory.Text = selectedPath.TrimEnd('\\');
		}

		private void buttonAddMore_Click(object sender, EventArgs e)
		{
			AddAnotherLinkedFolder();
		}

		public List<object> RemovedUsercontrolNonNullTags = new List<object>();
		public LinkedFolderUserControl AddAnotherLinkedFolder(string localRootDir = null, string ftpRootUrl = null, string ftpUsername = null, string ftpPassword = null, List<string> excludedFolders = null)
		{
			var tmpUsercontrol = new LinkedFolderUserControl(localRootDir, ftpRootUrl, ftpUsername, ftpPassword, excludedFolders);
			tmpUsercontrol.buttonRemoveSelf.Click += (s, e) =>
			{
				Button but = s as Button;
				if (s == null) return;
				var userc = GetUserControlOfButton(but);
				if (userc != null)
				{
					if (userc.Tag != null)
						RemovedUsercontrolNonNullTags.Add(userc.Tag);

					int tmpHeight = userc.Height;
					tableLayoutPanel1.Controls.Remove(userc);
					this.AutoSize = false;
					this.Height -= tmpHeight;
					this.AutoSize = true;
					RenumberUserControlTexts(null);
				}
			};
			//tmpUsercontrol.KeyPress += new KeyPressEventHandler(tmpUsercontrol_KeyPress);
			foreach (Control con in tmpUsercontrol.groupBox1.Controls)
				con.KeyPress += new KeyPressEventHandler(global_KeyPress);
			tableLayoutPanel1.Controls.Add(tmpUsercontrol);
			RenumberUserControlTexts(tmpUsercontrol);

			return tmpUsercontrol;
		}

		void global_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == 27)//Escape key
				this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		}

		private void RenumberUserControlTexts(LinkedFolderUserControl uc = null)
		{
			List<LinkedFolderUserControl> usercontrols = new List<LinkedFolderUserControl>();
			if (uc != null)
				usercontrols.Add(uc);
			else
				foreach (LinkedFolderUserControl con in GetUsercontrols())
					usercontrols.Add(con);

			foreach (LinkedFolderUserControl lu in usercontrols)
			{
				int newnum = tableLayoutPanel1.Controls.IndexOf(lu) + 1;
				lu.groupBox1.Text = "Linked folder " + newnum;
				//if (newnum > 1)
				lu.buttonRemoveSelf.Visible = true;
			}
		}

		private LinkedFolderUserControl GetUserControlOfButton(Button button)
		{
			Control parent = button.Parent;
			while (parent.Parent != null && !(parent.Parent is LinkedFolderUserControl))
				parent = parent.Parent;
			return parent.Parent as LinkedFolderUserControl;
		}

		private void NewLinkedFolder_SizeChanged(object sender, EventArgs e)
		{
			if (!this.IsHandleCreated)
				return;

			var wa = Screen.FromHandle(this.Handle).WorkingArea;
			this.Location = new Point(
				wa.Left + (wa.Width - this.Width) / 2,
				wa.Top + (wa.Height - this.Height) / 2
				);
		}

		private void buttonAccept_Click(object sender, EventArgs e)
		{
			if (ValidateAllInputs())
				this.DialogResult = System.Windows.Forms.DialogResult.OK;
		}

		private bool ValidateAllInputs()
		{
			foreach (var uc in GetUsercontrols())
				if (!uc.ValidateInputs())
					return false;

			return true;
		}
	}
}
