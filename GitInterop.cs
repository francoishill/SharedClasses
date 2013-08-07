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
	public enum GitCommand { Log, Fetch, Pull, Commit, Push, Status, StatusShort };
	public enum MessagesTypes { Output, Error }

	//False=error/could not run. Null=ran successfully but had outputs/errors. True=succeeded, no output
	public static bool? PerformGitCommand(string gitClonedFolder, GitCommand gitCommand, out string errorIfFailed, out List<string> outputs, out List<string> errors, string commitMessage = null)
	{
		if (!Directory.Exists(gitClonedFolder))
		{
			errorIfFailed = "Cannot perform Git command, directory not found: " + gitClonedFolder;
			outputs = null;
			errors = null;
			return false;
		}

		Console.WriteLine("Current folder: " + gitClonedFolder);

		string gitpath = @"C:\Program Files (x86)\Git\bin\git.exe";
		if (!File.Exists(gitpath))
		{
			errorIfFailed = "Git EXE not found in this directory: " + gitpath;
			outputs = null;
			errors = null;
			return false;
		}

		try
		{
			string processArguments =
				gitCommand ==
				GitCommand.Commit ? "commit -a -m\"" + commitMessage + "\""
				: gitCommand == GitCommand.Push ? "push origin"//"push \"" + commitMessageOrRemoteName + "\""
				: gitCommand == GitCommand.Pull ? "pull origin"//"pull \"" + commitMessageOrRemoteName + "\""
				: gitCommand == GitCommand.Status ? "status"
				: gitCommand == GitCommand.StatusShort ? "status --short"
				: "";

			int exitcode;
			bool? runsuccess = ProcessesInterop.RunProcessCatchOutput(
				new ProcessStartInfo(gitpath, processArguments)
				{
					WorkingDirectory = gitClonedFolder
				},
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