using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

public class SvnInterop
{
	public enum SvnCommand { Commit, Update, Status };

	public static void PerformSvn(TextBox messagesTextbox, string svnargs, SvnCommand svnCommand)
	{
		string projnameOrDir = svnargs.Split(';')[0];//projnameAndlogmessage.Split(';')[0];
		string logmessage = null;
		if (svnCommand == SvnCommand.Commit)
		{
			logmessage = svnargs.Split(';')[1];//projnameAndlogmessage.Split(';')[1];
			logmessage = logmessage.Replace("\\", "\\\\");
			logmessage = logmessage.Replace("\"", "\\\"");
		}
		try
		{
			string projDir =
					Directory.Exists(projnameOrDir) ? projnameOrDir :
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Visual Studio 2010\Projects\" + projnameOrDir;//"";
			string svnpath = @"C:\Program Files\TortoiseSVN\bin\svn.exe";// "svn";

			if (!File.Exists(svnpath)) Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Error: svn.exe does not exists: " + svnpath);
			else if (!Directory.Exists(projDir)) Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Error: folder not found: " + projDir);
			else
			{
				ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				{
					string processArguments =
											svnCommand ==
						SvnCommand.Commit ? "commit -m\"" + logmessage + "\" \"" + projDir + "\""
						: svnCommand == SvnCommand.Update ? "update \"" + projDir + "\""
						: svnCommand == SvnCommand.Status ? "status --show-updates \"" + projDir + "\""
						: "";

					ProcessStartInfo start = new ProcessStartInfo(svnpath, processArguments);//"commit -m\"" + logmessage + "\" \"" + projDir + "\"");
					start.UseShellExecute = false;
					start.CreateNoWindow = true;
					start.RedirectStandardOutput = true;
					start.RedirectStandardError = true;
					System.Diagnostics.Process svnproc = new Process();
					svnproc.OutputDataReceived += delegate(object sendingProcess, DataReceivedEventArgs outLine)
					{
						if (outLine.Data != null && outLine.Data.Trim().Length > 0) Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Svn output: " + outLine.Data);
						//else appendLogTextbox("Svn output empty");
					};
					svnproc.ErrorDataReceived += delegate(object sendingProcess, DataReceivedEventArgs outLine)
					{
						if (outLine.Data != null && outLine.Data.Trim().Length > 0) Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Svn error: " + outLine.Data);
						//else appendLogTextbox("Svn error empty");
					};
					svnproc.StartInfo = start;

					string performingPleasewaitMsg = 
							svnCommand == SvnCommand.Commit ? "Performing svn commit, please wait..."
						: svnCommand == SvnCommand.Update ? "Performing svn update, please wait..."
						: svnCommand == SvnCommand.Status ? "Check status of svn (local and server), please wait..."
						: "";
					if (svnproc.Start())
						Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, performingPleasewaitMsg);
					else Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Error: Could not start SVN process.");

					svnproc.BeginOutputReadLine();
					svnproc.BeginErrorReadLine();

					svnproc.WaitForExit();
				});
			}
		}
		catch (Exception exc)
		{
			Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Exception on running svn: " + exc.Message);
		}
	}
}