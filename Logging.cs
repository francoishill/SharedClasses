using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuickAccess
{
	public class Logging
	{
		public static void appendLogTextbox_OfPassedTextbox(TextBox messagesTextbox, string str)
		{
			//label1.Text = str;
			Control topParent = messagesTextbox;
			while (topParent != null && topParent != messagesTextbox.Parent) topParent = messagesTextbox.Parent;
			if (topParent is Form1)// && (topParent as Form1).Visible)
				(topParent as Form1).notifyIcon1.ShowBalloonTip(3000, "Message", str, ToolTipIcon.Info);
			ThreadingInterop.UpdateGuiFromThread(messagesTextbox, () =>
			{
				messagesTextbox.Text = str + (messagesTextbox.Text.Length > 0 ? Environment.NewLine : "") + messagesTextbox.Text;
			});
			Application.DoEvents();
		}
	}
}
