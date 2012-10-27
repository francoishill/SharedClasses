using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SharedClasses
{
	public partial class UserMessageWithTextbox : Form
	{
		private UserMessageWithTextbox(string userMessage, string textForTextbox, string title)
		{
			InitializeComponent();

			label1.Text = userMessage;
			textBox1.Text = textForTextbox;
			this.Text = title;
		}

		public static void ShowUserMessageWithTextbox(string userMessage, string textForTextbox, string title = "Message", IWin32Window window = null)
		{
			var tmpForm = new UserMessageWithTextbox(userMessage, textForTextbox, title);
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
	}
}
