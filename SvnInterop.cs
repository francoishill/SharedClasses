using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using SharedClasses;
//using System.Windows.Forms;

public class SvnInterop
{
	public enum SvnCommand { Commit, Update, Status, StatusLocal };
	public enum MessagesTypes { Output, Error }

	public static  Dictionary<MessagesTypes, List<string>> PerformSubversionCommand(Object textfeedbackSenderObject, string svnargs, SvnCommand svnCommand, TextFeedbackEventHandler textFeedbackEvent = null)
	{
		Dictionary<MessagesTypes, List<string>> tmpReturnMessagesList = new Dictionary<MessagesTypes, List<string>>();
		tmpReturnMessagesList.Add(MessagesTypes.Output, new List<string>());
		tmpReturnMessagesList.Add(MessagesTypes.Error, new List<string>());

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
			string VS2010projectsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Visual Studio 2010\Projects";

			string projDir =
					Directory.Exists(projnameOrDir) ? projnameOrDir :
				VS2010projectsFolder + "\\" + projnameOrDir;//"";
			string svnpath = @"C:\Program Files\TortoiseSVN\bin\svn.exe";// "svn";

			List<string> listOfDirectoriesToCheckLocalStatusses = null;
			if (svnargs != null && svnargs.ToLower() == "all")
			{
				listOfDirectoriesToCheckLocalStatusses = new List<string>();
				foreach (string workingDir in Directory.GetDirectories(VS2010projectsFolder))
					listOfDirectoriesToCheckLocalStatusses.Add(workingDir);
			}

			if (!File.Exists(svnpath)) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Error: svn.exe does not exists: " + svnpath);
			else if (!Directory.Exists(projDir) && listOfDirectoriesToCheckLocalStatusses == null) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Error: folder not found: " + projDir);
			else
			{
				ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				{
					if (listOfDirectoriesToCheckLocalStatusses == null)
					{
						listOfDirectoriesToCheckLocalStatusses = new List<string>();
						listOfDirectoriesToCheckLocalStatusses.Add(projDir);
					}

					bool pleaseWaitAlreadyDisplayed = false;

					foreach (string tmpFolder in listOfDirectoriesToCheckLocalStatusses)
					{
						string humanfriendlyFoldername = tmpFolder;
						if (humanfriendlyFoldername.Contains('\\'))
							humanfriendlyFoldername = humanfriendlyFoldername.Split('\\')[humanfriendlyFoldername.Split('\\').Length - 1];

						string processArguments =
											svnCommand ==
							SvnCommand.Commit ? "commit -m\"" + logmessage + "\" \"" + tmpFolder + "\""
							: svnCommand == SvnCommand.Update ? "update \"" + tmpFolder + "\""
							: svnCommand == SvnCommand.Status ? "status --show-updates \"" + tmpFolder + "\""
							: svnCommand == SvnCommand.StatusLocal ? "status \"" + tmpFolder + "\""
							: "";

						ProcessStartInfo start = new ProcessStartInfo(svnpath, processArguments);//"commit -m\"" + logmessage + "\" \"" + projDir + "\"");
						start.UseShellExecute = false;
						start.CreateNoWindow = true;
						start.RedirectStandardOutput = true;
						start.RedirectStandardError = true;
						System.Diagnostics.Process svnproc = new Process();
						svnproc.OutputDataReceived += delegate(object sendingProcess, DataReceivedEventArgs outputLine)
						{
							if (outputLine.Data != null && outputLine.Data.Trim().Length > 0)
							{
								string outputText = outputLine.Data.Replace(VS2010projectsFolder, "...");
								TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, string.Format("Output for {0}: {1}", humanfriendlyFoldername, outputText), outputLine.Data.ToLower().Contains("Status against revision".ToLower()) ? TextFeedbackType.Subtle : TextFeedbackType.Noteworthy);
								tmpReturnMessagesList[MessagesTypes.Output].Add(humanfriendlyFoldername + ": " + outputText);
							}
							//else appendLogTextbox("Svn output empty");
						};
						svnproc.ErrorDataReceived += delegate(object sendingProcess, DataReceivedEventArgs errorLine)
						{
							if (errorLine.Data != null && errorLine.Data.Trim().Length > 0
								&& !errorLine.Data.ToLower().Contains("not a working copy"))
							{
								string errorText = errorLine.Data.Replace(VS2010projectsFolder, "...");
								TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, string.Format("Error for {0}: {1}", humanfriendlyFoldername, errorText));
								tmpReturnMessagesList[MessagesTypes.Error].Add(humanfriendlyFoldername + ": " + errorText);
							}
							//else appendLogTextbox("Svn error empty");
						};
						svnproc.StartInfo = start;

						string performingPleasewaitMsg = 
							svnCommand == SvnCommand.Commit ? "Performing svn commit, please wait..."
							: svnCommand == SvnCommand.Update ? "Performing svn update, please wait..."
							: svnCommand == SvnCommand.Status ? "Check status of svn (local and server), please wait..."
							: svnCommand == SvnCommand.StatusLocal ? "Check status of svn (local), please wait..."
							: "";
						if (!svnproc.Start())
							TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Error: Could not start SVN process for " + humanfriendlyFoldername);
						else if (!pleaseWaitAlreadyDisplayed)
						{
							pleaseWaitAlreadyDisplayed = true;
							TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, performingPleasewaitMsg);
						}

						svnproc.BeginOutputReadLine();
						svnproc.BeginErrorReadLine();

						svnproc.WaitForExit();
					}
				});
			}
		}
		catch (Exception exc)
		{
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Exception on running svn: " + exc.Message);
		}
		return tmpReturnMessagesList;
	}

	private static TextFeedbackEventHandler TextFeedbackEvent;
	private static object TextFeedbackSenderObject;
	private static System.Windows.Forms.Timer timer;
	private static bool WasTimerStarted = false;
	public static void StartMonitoringSubversionDirectories(TextFeedbackEventHandler textFeedbackEvent, object textFeedbackSenderObject = null)
	{
		if (!WasTimerStarted)
		{
			WasTimerStarted = true;
			TextFeedbackEvent = textFeedbackEvent;
			TextFeedbackSenderObject = textFeedbackSenderObject;
			timer = new System.Windows.Forms.Timer();
			timer.Interval = GlobalSettings.SubversionSettings.Instance.IntervalForMonitoring_Milliseconds ?? 240000;
			timer.Tick += new EventHandler(timer_Tick);
			timer.Start();
		}
	}

	private static bool IsPreviousMessageStillShowing = false;
	private static void timer_Tick(object sender, EventArgs e)
	{
		bool SubversionChangesFound = false;
		//System.Windows.Forms.MessageBox.Show("Test");
		foreach (string subversionDir in GlobalSettings.SubversionSettings.Instance.ListOfMonitoredSubversionDirectories)
		{
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(
				TextFeedbackSenderObject,
				TextFeedbackEvent,
				"Automatic checking of subversion status for: " + subversionDir,
				TextFeedbackType.Noteworthy);

			Dictionary<MessagesTypes, List<string>> tmpSubversionMessages =
				PerformSubversionCommand(TextFeedbackSenderObject, subversionDir, SvnCommand.Status, TextFeedbackEvent);
			if (tmpSubversionMessages[MessagesTypes.Output].Count(s => !s.ToLower().Contains("Status against revision".ToLower())) > 0
				|| tmpSubversionMessages[MessagesTypes.Error].Count > 0)
				SubversionChangesFound = true;
			//foreach (string outmsg in tmpSubversionMessages[MessagesTypes.Output])
			//	TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, TextFeedbackEvent, "Subversion message: " + outmsg, TextFeedbackType.Noteworthy);
			//foreach (string errmsg in tmpSubversionMessages[MessagesTypes.Error])
			//	TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, TextFeedbackEvent, "Subversion error: " + errmsg, TextFeedbackType.Error);
			
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(
				TextFeedbackSenderObject,
				TextFeedbackEvent,
				"Finished checking svn status for: " + subversionDir,
				TextFeedbackType.Success);
		}
		if (SubversionChangesFound)
		{
			if (!IsPreviousMessageStillShowing)
			{
				IsPreviousMessageStillShowing = true;
				UserMessages.ShowWarningMessage("Changes detected in subversion directories, see the log for more details");
				IsPreviousMessageStillShowing = false;
			}
		}
	}

	//public static void CheckStatusAllVisualStudio2010Projects()
	//{
	//	string svnPath = @"c:\program files\TortoiseSVN\bin\svn.exe";
	//	if (!File.Exists(svnPath))
	//	{
	//		UserMessages.ShowWarningMessage("Svn excecutable not found: " + svnPath);
	//		return;
	//	}
	//	string baseFolder = @"C:\Users\francois\Documents\Visual Studio 2010\Projects";//\SharedClasses";

	//	foreach (string folderOfWorkingCopy in Directory.GetDirectories(baseFolder))
	//	{
	//		//ProcessStartInfo startInfo = new ProcessStartInfo(svnPath, "status --show-updates \"" + folderOfWorkingCopy + "\"");
	//		ProcessStartInfo startInfo = new ProcessStartInfo(svnPath, "status \"" + folderOfWorkingCopy + "\"");
	//		startInfo.CreateNoWindow = true;
	//		startInfo.UseShellExecute = false;
	//		startInfo.RedirectStandardOutput = true;
	//		startInfo.RedirectStandardError = true;

	//		Process proc = new Process();
	//		proc.StartInfo = startInfo;

	//		proc.OutputDataReceived += (snder, evtargs) =>
	//		{
	//			if (evtargs.Data != null && evtargs.Data.Trim().Length > 0)
	//			{
	//				Console.WriteLine("Output: " + evtargs.Data);
	//				//if (this.InvokeRequired)
	//				//	this.Invoke((Action)delegate { listBox1.Items.Add("Output: " + evtargs.Data); });
	//				//else listBox1.Items.Add("Output: " + evtargs.Data);
	//			}
	//		};
	//		proc.ErrorDataReceived += (snder, evtargs) =>
	//		{
	//			if (evtargs.Data != null && evtargs.Data.Trim().Length > 0
	//				&& !evtargs.Data.ToLower().Contains("not a working copy"))
	//			{
	//				Console.WriteLine("Error: " + evtargs.Data);
	//				//if (this.InvokeRequired)
	//				//	this.Invoke((Action)delegate { listBox1.Items.Add("Error: " + evtargs.Data); });
	//				//else listBox1.Items.Add("Error: " + evtargs.Data);
	//			}
	//		};

	//		proc.Start();
	//		proc.BeginErrorReadLine();
	//		proc.BeginOutputReadLine();

	//		//proc.WaitForExit();
	//	}
	//}
}