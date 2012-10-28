using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace SharedClasses
{
	public class MoveFromSvnToGit : INotifyPropertyChanged
	{
		public string SvnUrl { get; private set; }
		public bool StandardLayout { get; private set; }
		public int StartSvnRevisionNumber { get; private set; }
		public string LocalGitClonedFolder { get; private set; }
		public string RemoteGitRepo { get; private set; }
		public bool InitGitRepo { get; private set; }
		public string SvnServeDirIfRequiredToBeRun { get; private set; }

		private bool _isbuy;
		public bool IsBusy { get { return _isbuy; } private set { _isbuy = value; OnPropertyChanged("IsBusy"); } }

		public MoveFromSvnToGit(string SvnUrl, bool StandardLayout, int StartSvnRevisionNumber,
			string LocalGitClonedFolder, string RemoteGitRepo, bool InitGitRepo,
			string SvnServeDirIfRequiredToBeRun)
		{
			this.SvnUrl = SvnUrl;
			this.StandardLayout = StandardLayout;
			this.StartSvnRevisionNumber = StartSvnRevisionNumber;
			this.LocalGitClonedFolder = LocalGitClonedFolder;
			this.RemoteGitRepo = RemoteGitRepo;
			this.InitGitRepo = InitGitRepo;
			this.SvnServeDirIfRequiredToBeRun = SvnServeDirIfRequiredToBeRun;
			this.IsBusy = false;
		}

		public bool ValidateAll(Action onInvalidStartSvnRevisionNumber)
		{
			if (onInvalidStartSvnRevisionNumber == null) onInvalidStartSvnRevisionNumber = delegate { };

			if (string.IsNullOrWhiteSpace(this.SvnUrl))
			{
				UserMessages.ShowErrorMessage("Please enter a non-blank value for Svn Url");
				return false;
			}
			else if (string.IsNullOrWhiteSpace(this.LocalGitClonedFolder))
			{
				UserMessages.ShowErrorMessage("Please enter a non-blank value for Git Cloned Folder");
				return false;
			}
			else if (string.IsNullOrWhiteSpace(this.RemoteGitRepo))
			{
				UserMessages.ShowErrorMessage("Please enter a non-blank value for Remote Git Repo");
				return false;
			}

			if (StartSvnRevisionNumber < 0)
			{
				UserMessages.ShowWarningMessage("Enter valid valud for Start Svn Revision Number");
				onInvalidStartSvnRevisionNumber();
				return false;
			}
			if (SvnUrl.StartsWith("svn://localhost", StringComparison.InvariantCultureIgnoreCase)
				&& Process.GetProcessesByName("svnserve").Length == 0)
			{
				UserMessages.ShowErrorMessage("SvnUrl starts with 'svn://localhost', but no svnserve Processes found, this command will hence fail, please start svnserve first."
					+ Environment.NewLine
					+ Environment.NewLine + @"Most likely the svnserve.exe will sit in ProgramFiles\TortoiseSVN\bin or ProgramFile\Subversion\bin"
					+ Environment.NewLine
					+ Environment.NewLine + @"Working example is:"
					+ Environment.NewLine + @"svnserve.exe --daemon --root c:\francois\dev\repos\vsprojects"
					+ Environment.NewLine + "And then svn url can be:"
					+ Environment.NewLine + "svn://localhost/windowsstartupmanager/trunk");
				return false;
			}
			if (InitGitRepo)//If requested to INIT the Git repo, check not already existing
			{
				string gitReportToInit = RemoteGitRepo;
				if (gitReportToInit.Contains("://"))
				{
					UserMessages.ShowWarningMessage("Found the text '://' for the Remote Git repo, meaning  the repository is not local, not currently supported to init a remote git repo");
					return false;
				}
				if (Directory.Exists(gitReportToInit) && (Directory.GetFiles(gitReportToInit).Length > 0 || Directory.GetDirectories(gitReportToInit).Length > 0))
				{
					UserMessages.ShowWarningMessage("The Remote Git repo to INIT is a local directory but it is not empty, please ensure it is empty or the directory does not exist.");
					return false;
				}
				if (!Directory.Exists(gitReportToInit))
				{
					try
					{
						Directory.CreateDirectory(gitReportToInit);
						if (!Directory.Exists(gitReportToInit))
						{
							UserMessages.ShowWarningMessage("Unable to create Remote Git repo directory, cannot continue: " + gitReportToInit);
							return false;
						}
					}
					catch (Exception exc)
					{
						UserMessages.ShowWarningMessage("Unable to create Remote Git repo directory, cannot continue: " + exc.Message);
						return false;
					}
				}
			}
			return true;
		}

		public void MoveNow(Action<string> onAppendMessage, bool autoCloseSvnServeIfNeeded, bool separateThread = true, bool selectClonedRepoInExplorerOnSuccess = true)
		{
			Action<MoveFromSvnToGit> moveAction = (moveobj) =>
			{
				this.IsBusy = true;

				try
				{
					string remoteName = Path.GetFileNameWithoutExtension(moveobj.RemoteGitRepo);

					if (!Directory.Exists(moveobj.LocalGitClonedFolder))
						Directory.CreateDirectory(moveobj.LocalGitClonedFolder);

					if (!StartSvnServe(moveobj.SvnServeDirIfRequiredToBeRun, autoCloseSvnServeIfNeeded))
					{
						onAppendMessage("ERROR: Unable to start svnserve for dir: " + moveobj.SvnServeDirIfRequiredToBeRun + ", unable to use svn url: " + moveobj.SvnUrl);
						return;
					}

					List<string> commandListArguments = new List<string>();

					/*commandListArguments.Add(string.Format("svn init \"{0}\" \"{1}\"", textBoxSvnUrl.Text, textBoxLocalGitClonedFolder.Text));
					commandListArguments.Add(string.Format("svn fetch"));*/

					commandListArguments.Add(string.Format("svn clone{0} -r{1}:HEAD \"{2}\" \"{3}\"", moveobj.StandardLayout ? " -s" : "", moveobj.StartSvnRevisionNumber, moveobj.SvnUrl, moveobj.LocalGitClonedFolder));

					if (moveobj.InitGitRepo)
						commandListArguments.Add(string.Format("init --bare \"{0}\"", moveobj.RemoteGitRepo));
					//git remote add [-t <branch>] [-m <master>] [-f] [--tags|--no-tags] [--mirror=<fetch|push>] <name> <url>
					commandListArguments.Add(string.Format("remote add \"{0}\" \"{1}\"", remoteName, moveobj.RemoteGitRepo));
					commandListArguments.Add(string.Format("push \"{0}\" master", remoteName));

					bool allSuccess = true;
					foreach (var args in commandListArguments)
						if (!RunGitCommand(args, moveobj.LocalGitClonedFolder, onAppendMessage))
						{
							allSuccess = false;
							break;
						}

					if (allSuccess && selectClonedRepoInExplorerOnSuccess)
						Process.Start("explorer", moveobj.LocalGitClonedFolder);

					//isbusy = false;
				}
				finally
				{
					this.IsBusy = false;
				}
			};
			if (separateThread)
				ThreadingInterop.PerformOneArgFunctionSeperateThread<MoveFromSvnToGit>(
					moveAction,
					this,
					false);
			else
				moveAction(this);
		}

		public const string GitExePath = @"C:\Program Files (x86)\Git\bin\git.exe";
		private bool RunGitCommand(string arguments, string workingDir, Action<string> onAppendMessage)
		{
			if (onAppendMessage == null) onAppendMessage = delegate { };
			int exitCode;
			bool result = ProcessesInterop.StartAndWaitProcessRedirectOutput(
				new System.Diagnostics.ProcessStartInfo(GitExePath, arguments) { WorkingDirectory = workingDir },
				(obj, output) => { if (output != null) onAppendMessage(output); },
				(obj, error) => { if (error != null) onAppendMessage("ERROR: " + error); },
				out exitCode);
			if (result) onAppendMessage("SUCCESS: arguments = " + arguments);
			return result;
		}

		public static List<MoveFromSvnToGit> GetListInRootSvnDir(string rootSvnDir, string rootDirToCloneIn, string rootDirForGitRepos, bool autoCloseSvnServeIfNeeded, out List<string> skippedDirectoriesDueToHttps, Action<int> onProgress)
		{
			if (onProgress == null) onProgress = delegate { };
			int totalCount = Directory.GetDirectories(rootSvnDir).Count(dir => DirIsValidSvnPath(dir));
			onProgress(0);

			int doneCount = 0;
			List<MoveFromSvnToGit> tmplist = new List<MoveFromSvnToGit>();
			skippedDirectoriesDueToHttps = new List<string>();
			foreach (var subdir in Directory.GetDirectories(rootSvnDir))
			{
				if (!DirIsValidSvnPath(subdir))
					continue;
				try
				{
					string FolderNameOnly = Path.GetFileNameWithoutExtension(subdir);
					string svnCheckoutPath;
					bool isStandardLayout;
					bool failedBecauseWasHttps;
					string svnServeDirIfRequiredToBeRun;
					if (!GetSvnUrlFromCheckedOutDir(subdir, out svnCheckoutPath, out isStandardLayout, out failedBecauseWasHttps, out svnServeDirIfRequiredToBeRun))
					{
						if (failedBecauseWasHttps)
							skippedDirectoriesDueToHttps.Add(subdir);
						continue;
					}
					int svnFirstRevisionNumber = GetSvnFirstRevisionNumberOfDir(subdir);
					if (svnFirstRevisionNumber == -1)
						continue;
					var tmpMoveItem = new MoveFromSvnToGit(
						svnCheckoutPath,
						isStandardLayout,
						1,//??,
						Path.Combine(rootDirToCloneIn, FolderNameOnly),
						Path.Combine(rootDirForGitRepos, FolderNameOnly),
						true,
						svnServeDirIfRequiredToBeRun);
					tmplist.Add(tmpMoveItem);
				}
				finally
				{
					onProgress((int)Math.Truncate(100D * (double)++doneCount / (double)totalCount));
				}
			}
			return tmplist;
		}

		private static bool busyMovingAll = false;
		public static void MoveAllValidSvnCheckoutsInFolderToGitClones(
			string rootFolderToSearchForSvnCheckouts,
			string destinationFolderForGitClones,
			string destinationRootForGitRemoteRepos,
			Action<string> onAppendMessage,
			bool autoCloseSvnServeIfNeeded)
		{
			if (busyMovingAll)
				return;
			busyMovingAll = true;

			ThreadingInterop.DoAction(delegate
			{
				try
				{
					List<string> skippedDirectoriesDueToHttps;
					onAppendMessage("Starting to clone all SVN checkouts (as subfolders) in dir: " + rootFolderToSearchForSvnCheckouts);
					onAppendMessage("To destination (root folder) for git clones: " + destinationFolderForGitClones);
					onAppendMessage("Using root folder for local repos (instead of actual remote repos) to push the clones to: " + destinationRootForGitRemoteRepos);
					var listToMove = GetListInRootSvnDir(rootFolderToSearchForSvnCheckouts, destinationFolderForGitClones, destinationRootForGitRemoteRepos, autoCloseSvnServeIfNeeded, out skippedDirectoriesDueToHttps,
						null);
					foreach (var moveItem in listToMove)
					{
						if (!moveItem.ValidateAll(delegate { }))
						{
							UserMessages.ShowWarningMessage("Something went wrong, cannot move svn folder to git: " + moveItem.SvnUrl);
							continue;
						}
						moveItem.MoveNow(onAppendMessage, autoCloseSvnServeIfNeeded, false, false);
					}
					if (skippedDirectoriesDueToHttps.Count > 0)
					{
						UserMessageWithTextbox.ShowUserMessageWithTextbox(
							"There are issues to clone from svn https (SSL) url, cannot accept the certificates successfully, the following repo(s) were skipped:",
							string.Join(Environment.NewLine, skippedDirectoriesDueToHttps),
							"Skipped subfolders");
					}
					string completeMsg = "Completed cloning all svn folders to git clones";
					onAppendMessage(completeMsg);
					UserMessages.ShowInfoMessage(completeMsg + ", press OK and both the 'remote repos root' and 'root for checkouts' directories will open in explorer");
					Process.Start("explorer", destinationRootForGitRemoteRepos);
					Process.Start("explorer", destinationFolderForGitClones);
				}
				finally
				{
					busyMovingAll = false;
				}
			},
			false);
		}

		private static int GetSvnFirstRevisionNumberOfDir(string svnDir)
		{
			List<string> outputs, errors;
			int exitCode;
			bool? getSvnLogResult = ProcessesInterop.RunProcessCatchOutput(
				new ProcessStartInfo(SvnExePath, string.Format("log \"{0}\"", svnDir)),
				out outputs,
				out errors,
				out exitCode);
			if (getSvnLogResult == false)//Could not run
			{
				UserMessages.ShowErrorMessage("Unable to get svn first revision number from path (" + svnDir + "): " + string.Join(Environment.NewLine, outputs.Concat(errors)));
				return -1;
			}
			else if (getSvnLogResult == true)//Ran but no feedback, cannot obtain info
			{
				UserMessages.ShowErrorMessage("Unable to get svn first revision number from path (" + svnDir + "), no feedback returned from call to svn.exe");
				return -1;
			}
			else
			{
				for (int i = outputs.Count - 1; i >= 0; i--)//Start from end as the earlies revision will be given last
				{
					Match match = Regex.Match(outputs[i], @"(?<=r)[0-9]+(?= \| )");
					if (match.Success)
					{
						int tmpRevnum;
						if (int.TryParse(match.Value, out tmpRevnum))
							return tmpRevnum;
					}
				}
				UserMessages.ShowWarningMessage("Could not find a line matching format 'r123 | ' from the call to 'svn.exe log " + svnDir + "'");
				return -1;//No revision lines found
			}
		}

		private static bool DirIsValidSvnPath(string dir)
		{
			if (!Directory.Exists(dir))
				return false;
			return Directory.Exists(System.IO.Path.Combine(dir, ".svn"));
		}

		public const string SvnExePath = @"C:\Program Files\TortoiseSVN\bin\svn.exe";
		private static bool GetSvnUrlFromCheckedOutDir(string dirPath, out string svnUrl, out bool isStandardLayout, out bool failedBecauseWasHttps, out string svnServeDirIfRequiredToBeRun)
		{
			failedBecauseWasHttps = false;
			List<string> outputs, errors;
			int exitCode;
			bool? svnInfoResult = ProcessesInterop.RunProcessCatchOutput(
				new ProcessStartInfo(SvnExePath, string.Format("info \"{0}\"", dirPath)),
				out outputs,
				out errors,
				out exitCode);
			if (svnInfoResult == false)//Could not run
			{
				UserMessages.ShowErrorMessage("Unable to get svn url from path (" + dirPath + "): " + string.Join(Environment.NewLine, outputs.Concat(errors)));
				svnUrl = null;
				isStandardLayout = false;
				svnServeDirIfRequiredToBeRun = null;
				return false;
			}
			else if (svnInfoResult == true)//Ran but no feedback, cannot obtain info
			{
				UserMessages.ShowErrorMessage("Unable to get svn url from path (" + dirPath + "), no feedback returned from call to svn.exe");
				svnUrl = null;
				isStandardLayout = false;
				svnServeDirIfRequiredToBeRun = null;
				return false;
			}
			else
			{
				const string cUrlStart = "URL: ";
				const string cRepositoryRootStart = "Repository Root: ";
				var possibleURLs = outputs.Where(line => line.StartsWith(cUrlStart, StringComparison.InvariantCultureIgnoreCase)).ToArray();
				var possibleRepositoryRoots = outputs.Where(line => line.StartsWith(cRepositoryRootStart, StringComparison.InvariantCultureIgnoreCase)).ToArray();
				if (possibleURLs.Length == 0 || possibleURLs.Length > 1)
				{
					string tmpstr = possibleURLs.Length == 0 ? "cannot get line" : "multiple lines";
					UserMessages.ShowInfoMessage("Unable to determine the svn url, " + tmpstr + " starting with 'URL: ' from the call to 'svn.exe info', outputs AND errors of this call was:"
						+ Environment.NewLine + string.Join(Environment.NewLine, outputs.Concat(errors.Select(er => "ERROR: " + errors))));
					svnUrl = null;
					isStandardLayout = false;
					svnServeDirIfRequiredToBeRun = null;
					return false;
				}
				if (possibleRepositoryRoots.Length == 0)
				{
					string tmpstr = possibleRepositoryRoots.Length == 0 ? "cannot get line" : "multiple lines";
					UserMessages.ShowInfoMessage("Unable to determine the svn root url, " + tmpstr + " starting with 'Repository Root: ' from the call to 'svn.exe info', outputs AND errors of this call was:"
						+ Environment.NewLine + string.Join(Environment.NewLine, outputs.Concat(errors.Select(er => "ERROR: " + errors))));
					svnUrl = null;
					isStandardLayout = false;
					svnServeDirIfRequiredToBeRun = null;
					return false;
				}

				string checkoutUrl = possibleURLs[0].Substring(cUrlStart.Length);
				string repoRootUrl = possibleRepositoryRoots[0].Substring(cRepositoryRootStart.Length);
				if (checkoutUrl.StartsWith("https:", StringComparison.InvariantCultureIgnoreCase))
				{
					//UserMessages.ShowWarningMessage("There are issues to clone from svn https (SSL) url, cannot accept the certificates successfully, this repo will be skipped: "
					//    + Environment.NewLine + dirPath);
					svnUrl = null;
					isStandardLayout = false;
					failedBecauseWasHttps = true;
					svnServeDirIfRequiredToBeRun = null;
					return false;
				}
				else if (!ValidateAndCleanUrls(ref repoRootUrl, ref checkoutUrl, out svnServeDirIfRequiredToBeRun))
				{
					svnUrl = null;
					isStandardLayout = false;
					return false;
				}
				if (checkoutUrl == null || repoRootUrl == null)
				{
					svnUrl = null;
					isStandardLayout = false;
					return false;
				}
				if (checkoutUrl.TrimEnd('/', '\\').Equals(repoRootUrl.TrimEnd('/', '\\'), StringComparison.InvariantCultureIgnoreCase))
				{
					svnUrl = checkoutUrl;
					isStandardLayout = UserMessages.Confirm(
						"The root SVN repo url is the same as the checkedout url, is this repo a StandardLayour repo (trunk/tags/branches)?"
						+ Environment.NewLine + dirPath);
					return true;
				}
				else//Assuming its a non-standard layout because we checked out a suburl of the svn repo
				{
					svnUrl = checkoutUrl;
					isStandardLayout = false;
					return true;
				}
			}
		}

		private const string cFileUrlStart = "file:///";
		private static string ExtractFilePathFromSvnFileUrl(string fileUrl)
		{
			return fileUrl.Substring(cFileUrlStart.Length).Replace('/', '\\').TrimEnd('\\');
		}

		private static bool ValidateAndCleanUrls(ref string svnRootUrl, ref string svnCheckoutUrl, out string svnServeDirIfRequiredToBeRun)
		{
			//if svnRootUrl = file:///c:/francois/dev/repos/windowsstartupmanager
			//and svnCheckoutUrl = file:///c:/francois/dev/repos/windowsstartupmanager/trunk
			if (svnRootUrl.StartsWith(cFileUrlStart, StringComparison.InvariantCultureIgnoreCase))
			{
				string rootOfRepoLocalDirpath = ExtractFilePathFromSvnFileUrl(svnRootUrl);//c:\francois\dev\repos\windowsstartupmanager
				string rootOfRepoParentPath = Path.GetDirectoryName(rootOfRepoLocalDirpath);//c:\francois\dev\repos
				//if (!StartSvnServe(rootOfRepoParentPath, autoCloseSvnServeIfNeeded))
				//    return false;
				//else
				//{
				//SVN Server (svnserve.exe) running on the parent directory of the root URL
				svnServeDirIfRequiredToBeRun = rootOfRepoParentPath;
				svnRootUrl = "svn://localhost/"
					+ Path.GetFileName(rootOfRepoLocalDirpath);
				svnCheckoutUrl = "svn://localhost/"
					+ Path.GetFileName(rootOfRepoLocalDirpath)
					+ "/"
					+ ExtractFilePathFromSvnFileUrl(svnCheckoutUrl)
						.Substring(rootOfRepoLocalDirpath.Length).Trim('\\');
				return true;
				//}
			}
			else
			{
				svnServeDirIfRequiredToBeRun = null;
				return true;
			}
		}

		public const string SvnserveExePath = @"C:\Program Files\TortoiseSVN\bin\svnserve.exe";
		private static bool StartSvnServe(string reposRoot, bool autoCloseSvnServeIfNeeded)
		{
			Process[] currentRunningSvnServes = Process.GetProcessesByName("svnserve");
			if (!autoCloseSvnServeIfNeeded
				&& currentRunningSvnServes.Length > 0
				&& !UserMessages.Confirm(string.Format("There are already {0} process{1} running of svnserve.exe, close all now and automatically start with correct mapping for directory: {2}",
				currentRunningSvnServes.Length, currentRunningSvnServes.Length > 1 ? "es" : "", reposRoot)))
				return false;//Exit if there were already running svnserves and the user declined to kill them all
			if (currentRunningSvnServes.Length > 0)
			{
				foreach (var proc in currentRunningSvnServes)
				{
					try
					{
						proc.Kill();
					}
					catch (Exception exc)
					{
						UserMessages.ShowErrorMessage("Unable to kill svnserve processes, cannot continue with svn2git import: " + exc.Message);
						return false;
					}
				}
			}
			var procToRun = Process.Start(SvnserveExePath, "--daemon --root \"" + reposRoot + "\"");
			return procToRun != null;
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		public void OnPropertyChanged(string propertyName) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
	}
}