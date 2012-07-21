using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
#if WINFORMS
using System.Windows.Forms;
#endif

public class Logging
{
#if WINFORMS
	public static NotifyIcon staticNotifyIcon = null;

	public static void appendLogTextbox_OfPassedTextbox(TextBox messagesTextbox, string str, NotifyIcon notifyicon = null)
	{
		NotifyIcon notifyIconToUse = null;
		if (notifyicon != null) notifyIconToUse = notifyicon;
		else if (staticNotifyIcon != null) notifyIconToUse = staticNotifyIcon;
		//label1.Text = str;
		Control topParent = messagesTextbox;
		while (topParent != null && topParent != messagesTextbox.Parent) topParent = messagesTextbox.Parent;
		if (staticNotifyIcon != null) staticNotifyIcon.ShowBalloonTip(3000, "Message in textbox", str, ToolTipIcon.Info);
		
		ThreadingInterop.UpdateGuiFromThread(messagesTextbox, () =>
		{
			messagesTextbox.Text = str + (messagesTextbox.Text.Length > 0 ? Environment.NewLine : "") + messagesTextbox.Text;
		});
		Application.DoEvents();
	}
#endif

	public static void LogErrorShowInNotepad(string errorMessage)
	{
		string tmpFile = Path.GetTempFileName();
		File.WriteAllText(tmpFile, errorMessage);
		System.Diagnostics.Process.Start("notepad", "\"" + tmpFile + "\"");
	}
}