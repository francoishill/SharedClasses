using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Windows;

public class WindowsInterop
{
	public static readonly string LocalAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
	public static readonly string MydocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

	public static void StartCommandPromptOrVScommandPrompt(string cmdpath, bool VisualStudioMode, TextFeedbackEventHandler textFeedbackEvent = null)
	{
		const string vsbatfile = @"c:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\vcvarsall.bat";

		string processArgs = 
								VisualStudioMode ? @"/k """ + vsbatfile + @""" x86"
			: "";

		if (Directory.Exists(cmdpath))
		{
			if (!VisualStudioMode || (VisualStudioMode && File.Exists(vsbatfile)))
			{
				Process proc = new Process();
				proc.StartInfo = new ProcessStartInfo("cmd", processArgs);
				proc.StartInfo.WorkingDirectory = cmdpath;
				proc.Start();
			}
			else TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, @"Unable to start Visual Studio Command Prompt, cannot find file: """ + vsbatfile + @"""" + cmdpath);
		}
		else TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Folder does not exist, cannot start cmd: " + cmdpath);
	}

	public static void ShowAndActivateForm(Form form)
	{
		form.Visible = true;
		form.Activate();
		form.WindowState = FormWindowState.Normal;
	}

	public static void ShowAndActivateWindow(Window window)
	{
		//window.Visibility = Visibility.Visible;
		window.Show();
		window.WindowState = WindowState.Normal;
		window.Activate();
	}
}