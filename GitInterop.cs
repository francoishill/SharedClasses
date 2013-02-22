using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharedClasses;
//using System.Windows.Forms;

public class GitInterop : INotifyPropertyChanged
{
	public enum GitCommand { Log, Pull, Commit, Push, Status };
	public enum MessagesTypes { Output, Error }

	public static Dictionary<MessagesTypes, List<string>> PerformGitCommand(Object textfeedbackSenderObject, string gitargs, GitCommand gitCommand, TextFeedbackEventHandler textFeedbackEvent = null)
	{
		Dictionary<MessagesTypes, List<string>> tmpReturnMessagesList = new Dictionary<MessagesTypes, List<string>>();
		tmpReturnMessagesList.Add(MessagesTypes.Output, new List<string>());
		tmpReturnMessagesList.Add(MessagesTypes.Error, new List<string>());

		string projnameOrDir = gitargs.Split(';')[0];//projnameAndlogmessage.Split(';')[0];
		string logmessage = null;
		if (gitCommand == GitCommand.Commit)
		{
			int semicolonIndex = gitargs.IndexOf(';');
			if (semicolonIndex != -1 && semicolonIndex != gitargs.Length - 1)
			{
				logmessage = gitargs.Substring(semicolonIndex + 1);//svnargs.Split(';')[1];//projnameAndlogmessage.Split(';')[1];
				logmessage = logmessage.Replace("\\", "\\\\");
				logmessage = logmessage.Replace("\"", "\\\"");
			}
		}
		try
		{
			string VS2010projectsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Visual Studio 2010\Projects";

			string projDir =
					Directory.Exists(projnameOrDir) ? projnameOrDir :
				VS2010projectsFolder + "\\" + projnameOrDir;//"";
			string svnpath = @"C:\Program Files (x86)\Git\bin\git.exe";

			List<string> listOfDirectoriesToCheckLocalStatusses = null;
			if (gitargs != null && gitargs.ToLower() == "all")
			{
				listOfDirectoriesToCheckLocalStatusses = new List<string>();
				foreach (string workingDir in Directory.GetDirectories(VS2010projectsFolder))
					listOfDirectoriesToCheckLocalStatusses.Add(workingDir);
			}

			if (!File.Exists(svnpath)) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Error: svn.exe does not exists: " + svnpath);
			else if (!Directory.Exists(projDir) && listOfDirectoriesToCheckLocalStatusses == null) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Error: folder not found: " + projDir);
			else
			{
				//ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				//{
				if (listOfDirectoriesToCheckLocalStatusses == null)
				{
					listOfDirectoriesToCheckLocalStatusses = new List<string>();
					listOfDirectoriesToCheckLocalStatusses.Add(projDir);
				}

				bool pleaseWaitAlreadyDisplayed = false;

				//foreach (string tmpFolder in listOfDirectoriesToCheckLocalStatusses)
				//{
				ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				{
					//Parallel.For uses multiple threads but also uses the current thread, therefore freezing the UI. Use ThreadingInterop.PerformVoidFunctionSeperateThread to overcome this
					Parallel.For(0, listOfDirectoriesToCheckLocalStatusses.Count, (tmpIndex) =>
					{
						//System.Threading.ThreadPool.QueueUserWorkItem(delegate
						//ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
						//{

						string tmpFolder = listOfDirectoriesToCheckLocalStatusses[tmpIndex];
						string humanfriendlyFoldername = tmpFolder;
						if (humanfriendlyFoldername.Contains('\\'))
							humanfriendlyFoldername = humanfriendlyFoldername.Split('\\')[humanfriendlyFoldername.Split('\\').Length - 1];

						string processArguments =
															gitCommand ==
							GitCommand.Commit ? "commit -a -m\"" + logmessage + "\" \"" + tmpFolder + "\""
							: gitCommand == GitCommand.Push ? "push \"" + tmpFolder + "\""
							: gitCommand == GitCommand.Pull ? "pull \"" + tmpFolder + "\""
							: gitCommand == GitCommand.Status ? "status --short \"" + tmpFolder + "\""
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
								TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(
									textfeedbackSenderObject,
									textFeedbackEvent,
									string.Format("Output for {0}: {1}", humanfriendlyFoldername, outputText),
									TextFeedbackType.Noteworthy);
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
							gitCommand == GitCommand.Commit ? "Performing svn commit, please wait..."
							: gitCommand == GitCommand.Push ? "Performing git push, please wait..."
							: gitCommand == GitCommand.Pull ? "Performing git pull, please wait..."
							: gitCommand == GitCommand.Status ? "Check status --short of git (local), please wait..."
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
					});
					//});
				});
				//}
				//});
			}
		}
		catch (Exception exc)
		{
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Exception on running svn: " + exc.Message);
		}
		return tmpReturnMessagesList;
	}

	public event PropertyChangedEventHandler PropertyChanged;
	public void OnPropertyChanged(string PropertyName)
	{
		if (PropertyChanged != null)
			PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
	}
}