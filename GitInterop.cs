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

	public static bool? PerformGitCommand(string gitClonedFolder, GitCommand gitCommand, out string errorIfFailed, out List<string> outputs, out List<string> errors, string commitMessageOrRemoteName = null)
	{
		if (!Directory.Exists(gitClonedFolder))
		{
			errorIfFailed = "Cannot perform Git command, directory not found: " + gitClonedFolder;
			outputs = null;
			errors = null;
			return false;
		}

		Environment.CurrentDirectory = gitClonedFolder;

		string gitpath = @"C:\Program Files (x86)\Git\bin\git.exe";

		try
		{
			string processArguments =
				gitCommand ==
				GitCommand.Commit ? "commit -a -m\"" + commitMessageOrRemoteName + "\""
				: gitCommand == GitCommand.Push ? "push \"" + commitMessageOrRemoteName + "\""
				: gitCommand == GitCommand.Pull ? "pull \"" + commitMessageOrRemoteName + "\""
				: gitCommand == GitCommand.Status ? "status --short"
				: "";

			int exitcode;
			bool? runsuccess = ProcessesInterop.RunProcessCatchOutput(
				new ProcessStartInfo(gitpath, processArguments),
				out outputs,
				out errors,
				out exitcode);
			errorIfFailed = null;
			return runsuccess;
		}
		catch (Exception exc)
		{
			errorIfFailed = "Exception on running git: " + exc.Message;
			outputs = null;
			errors = null;
			return false;
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;
	public void OnPropertyChanged(string PropertyName)
	{
		if (PropertyChanged != null)
			PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
	}
}