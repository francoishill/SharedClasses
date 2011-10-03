using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

public class Logging
{
	public static void appendLogTextbox_OfPassedTextbox(TextBox messagesTextbox, string str, NotifyIcon notifyicon = null)
	{
		//label1.Text = str;
		Control topParent = messagesTextbox;
		while (topParent != null && topParent != messagesTextbox.Parent) topParent = messagesTextbox.Parent;
		if (notifyicon != null) notifyicon.ShowBalloonTip(3000, "Message", str, ToolTipIcon.Info);
		ThreadingInterop.UpdateGuiFromThread(messagesTextbox, () =>
		{
			messagesTextbox.Text = str + (messagesTextbox.Text.Length > 0 ? Environment.NewLine : "") + messagesTextbox.Text;
		});
		Application.DoEvents();
	}
}