using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

public partial class UserMessageBox : Form
{
	public static Dictionary<string, UserMessageBox> ListOfShowingMessages = new Dictionary<string, UserMessageBox>(StringComparer.InvariantCultureIgnoreCase);

	public MessageBoxButtons CurrentButtons;

	private static Dictionary<MessageBoxIcon, Image> _mappedIcons;
	private static Dictionary<MessageBoxIcon, Image> MappedIcons
	{
		get
		{
			if (_mappedIcons == null)
			{
				_mappedIcons = new Dictionary<MessageBoxIcon, Image>();
				_mappedIcons.Add(MessageBoxIcon.Error, SystemIcons.Error.ToBitmap());
				_mappedIcons.Add(MessageBoxIcon.Warning, SystemIcons.Warning.ToBitmap());
				_mappedIcons.Add(MessageBoxIcon.Information, SystemIcons.Information.ToBitmap());
				_mappedIcons.Add(MessageBoxIcon.Question, SystemIcons.Question.ToBitmap());
			}
			return _mappedIcons;
		}
	}

	private UserMessageBox()
	{
		InitializeComponent();
		labelCountRepeatedCount.Text = "0 times";
		EnsureClosingEventAttached();
	}

	private bool alreadyAttached = false;
	private void EnsureClosingEventAttached()
	{
		if (!alreadyAttached)
		{
			Application.OpenForms[0].FormClosing += delegate
			{
				var keys = ListOfShowingMessages.Keys.ToArray();
				for (int i = keys.Length - 1; i >= 0; i--)
					ListOfShowingMessages[keys[i]].Close();
			};
			alreadyAttached = true;
		}
	}

	public static DialogResult ShowUserMessage(IWin32Window owner, string Message, string Title, MessageBoxIcon icon, bool AlwaysOnTop)
	{
		if (ListOfShowingMessages.ContainsKey(Message))
		{
			Action showConfirmAction = delegate
			{
				ListOfShowingMessages[Message].labelCountRepeatedCount.Visible = true;
				int curCount;
				if (int.TryParse(ListOfShowingMessages[Message].labelCountRepeatedCount.Text.Substring(0, ListOfShowingMessages[Message].labelCountRepeatedCount.Text.IndexOf(' ')), out curCount))
					ListOfShowingMessages[Message].labelCountRepeatedCount.Text = (curCount + 1).ToString() + " times";
			};
			if (ListOfShowingMessages[Message].InvokeRequired)
				ListOfShowingMessages[Message].Invoke(showConfirmAction);
			else
				showConfirmAction();
			return DialogResult.Cancel;//Might cause issues as the 2nd, 3rd, 4th, etc occurance will always return Cancel
		}

		UserMessageBox umb = new UserMessageBox();
		ListOfShowingMessages.Add(Message, umb);

		if (icon == MessageBoxIcon.None)
			umb.pictureBox1.Visible = false;
		else
			umb.pictureBox1.Image = MappedIcons[icon];
		umb.CurrentButtons = MessageBoxButtons.OK;
		umb.labelMessage.Text = Message;
		umb.Text = Title;
		//umb.TopMost = AlwaysOnTop;
		return umb.ShowDialog(owner);
	}

	private void GlobalKeyDown(object sender, PreviewKeyDownEventArgs e)
	{
		if (e.KeyData == Keys.Escape)
			this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
	}

	private bool labelLocationChanged = false;
	private void UserMessageBox_SizeChanged(object sender, EventArgs e)
	{
		if (this.Size.Width != 138 || this.Size.Height != 170)
		{
			if (this.Size.Height > 170)
				if (!labelLocationChanged)
				{
					labelMessage.Location = new Point(labelMessage.Location.X, labelMessage.Location.Y - 9);
					labelLocationChanged = true;
				}
			if (CurrentButtons == MessageBoxButtons.OK)
			{
				Button acceptButton = new Button()
				{
					Text = "&OK",
					Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
					DialogResult = System.Windows.Forms.DialogResult.OK
				};
				this.panel2.Controls.Add(acceptButton);
				acceptButton.PreviewKeyDown += GlobalKeyDown;
				acceptButton.Location = new Point(this.panel2.Width - acceptButton.Width - 5, (this.panel2.Height - acceptButton.Height) / 2);
			}
		}
	}

	private void UserMessageBox_FormClosing(object sender, FormClosingEventArgs e)
	{
		ListOfShowingMessages.Remove(this.labelMessage.Text);
	}
}