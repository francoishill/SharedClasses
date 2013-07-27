using System;
using System.Collections.Generic;
using System.ComponentModel;
//using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharedClasses
{
	[ComVisible(true)]
	internal partial class InputBox : Form
	{
		public InputBox(String Message)
		{
			InitializeComponent();
			label_MESSAGE.Text = Message;
		}

		private void button_OK_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
		}

		private void InputBox_Load(object sender, EventArgs e)
		{
			this.Icon = Owner.Icon;
		}

		private void textBox_INPUT_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Escape) this.Close();
			else if (e.KeyChar == (char)Keys.Enter) this.DialogResult = System.Windows.Forms.DialogResult.OK;
		}

		private void InputBox_Shown(object sender, EventArgs e)
		{
			this.Activate();
			textBox_INPUT.Focus();
		}
	}

	/// <summary>
	/// Dialog box stuff.
	/// </summary>
	public static class DialogBoxStuff
	{
		/// <summary>
		/// And input dialog showing a message and requiring one input string from the user.
		/// </summary>
		/// <param name="Message">The message to show to the user.</param>
		/// <param name="ParentForm">The parent form for when starting in center of parent</param>
		/// <returns>The input string that the user entered.</returns>
		public static String InputDialog(String Message, String Title = null, Form ParentForm = null, int? overrideWidth = null, int? overrideHeight = null)
		{
			//InputBox iBox = new InputBox(ParentForm, MessageIn, InitialName, DescriptionVisible, InitialDescription, AutoCompleteList, PreventIllegalPathChars);
			//Form ownerForm = new Form();
			//ownerForm.Location = ParentForm.Location;
			//ownerForm.Size = ParentForm.Size;
			//ownerForm.StartPosition = FormStartPosition.CenterParent;
			//ownerForm.TopMost = true;

			InputBox iBox = new InputBox(Message);
			if (overrideWidth.HasValue)
				iBox.Width = overrideWidth.Value;
			if (overrideHeight.HasValue)
				iBox.Height = overrideHeight.Value;
			iBox.TopMost = true;
			if (Title != null && Title.Length > 0) iBox.Text = Title;

			DialogResult dr;
			if (ParentForm != null)
			{
				iBox.Icon = ParentForm.Icon;
				iBox.StartPosition = FormStartPosition.CenterParent;
				dr = iBox.ShowDialog(ParentForm);
			}
			else
			{
				dr = iBox.ShowDialog();
			}

			if (dr == DialogResult.OK) return iBox.textBox_INPUT.Text;
			else return "";
			//else return OtherInternal.ReturnError("Input box cancelled", "");
		}
	}
}
