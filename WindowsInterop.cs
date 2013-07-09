﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

public class WindowsInterop
{
	public static readonly string LocalAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
	public static readonly string MydocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

	public static void StartCommandPromptOrVScommandPrompt(TextBox messagesTextbox, string cmdpath, bool VisualStudioMode)
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
			else Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, @"Unable to start Visual Studio Command Prompt, cannot find file: """ + vsbatfile + @"""" + cmdpath);
		}
		else Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Folder does not exist, cannot start cmd: " + cmdpath);
	}

	public static void ShowAndActivateForm(Form form)
	{
		form.Visible = true;
		form.Activate();
		form.WindowState = FormWindowState.Normal;
	}
}