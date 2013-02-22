using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace SharedClasses
{
	public static class TortoiseProcInterop
	{
		private enum VersioningClient { Subversion, Git };
		public enum TortoiseSvnCommands { Log, Commit, Update };
		public enum TortoiseGitCommands { Log, Pull, Commit, Push };

		private const string cTortoiseSvnPath = @"C:\Program Files\TortoiseSVN\bin\TortoiseProc.exe";
		private const string cTortoiseGitPath = @"C:\Program Files\TortoiseGit\bin\TortoiseGitProc.exe";

		public static Process Subversion_StartTortoiseProc(TortoiseSvnCommands tortoiseCommand, string dir)
		{
			return Process.Start(
				cTortoiseSvnPath,
				"/command:" + tortoiseCommand.ToString().ToLower()
				+ @" /path:""" + dir + @"""");
		}
		public static Process Git_StartTortoiseProc(TortoiseGitCommands tortoiseCommand, string dir)
		{
			return Process.Start(
				cTortoiseGitPath,
				"/command:" + tortoiseCommand.ToString().ToLower()
				+ @" /path:""" + dir + @"""");
		}

		public static string GetExtraSvnParams()
		{
			string tmpLocalAppdataPath = SettingsInterop.LocalAppdataPath("Tmp");
			bool isApacheNOTrunningAsService =
				tmpLocalAppdataPath.Split('\\')[1].Equals("Users", StringComparison.InvariantCultureIgnoreCase)
				|| tmpLocalAppdataPath.Split('\\')[1].Equals("Documents and Settings", StringComparison.InvariantCultureIgnoreCase);

			string extraSvnParams =
				string.Format(
					" --no-auth-cache --username {0} --password {1}",
					SettingsSimple.SvnCredentials.Instance.Username,
					SettingsSimple.SvnCredentials.Instance.Password);

			return "--trust-server-cert --non-interactive" + (isApacheNOTrunningAsService ? "" : extraSvnParams);
		}

		public const string cSvnPath = @"C:\Program Files\TortoiseSVN\bin\svn.exe";
		public const string cGitPath = @"C:\Program Files (x86)\Git\bin\git.exe";
		private static readonly Predicate<string> cSubversionIgnoreOutputPredicate =
			(outstr) =>
			{
				return
					outstr.StartsWith("Performing status on external item", StringComparison.InvariantCultureIgnoreCase)
					|| outstr.StartsWith("X       ", StringComparison.InvariantCultureIgnoreCase)
					|| outstr.StartsWith("Status against revision", StringComparison.InvariantCultureIgnoreCase);
			};
		private static readonly Predicate<string> cGitIgnoreOutputPredicate =
			(outstr) =>
			{
				return false;//We use "git status --short" which will have no lines if no changes
			};

		private static Predicate<string> GetPredicateForIgnoringOutputString(VersioningClient client)
		{
			return
					client == VersioningClient.Subversion
					? cSubversionIgnoreOutputPredicate
					: cGitIgnoreOutputPredicate;
		}

		private static bool CheckFolderChanges(VersioningClient client, string dir, out string changesText)
		{
			List<string> outputs, errors;
			int exitCode;

			string exePath =
				client == VersioningClient.Subversion
				? cSvnPath
				: cGitPath;

			string cmdlineArgs =
				client == VersioningClient.Subversion
				? string.Format("status --show-updates {0} \"{1}\"", GetExtraSvnParams(), dir)
				: string.Format("status --short \"{0}\"", dir);

			//Only need credentials if this if apache called this EXE (like for the BuildTestSystem) and apache is running as service
			bool? runResult = ProcessesInterop.RunProcessCatchOutput(
				new ProcessStartInfo(exePath, cmdlineArgs) { WorkingDirectory = dir },
				out outputs, out errors,
				out exitCode);
			if (runResult == true)//Ran and had no output
			{
				changesText = null;
				return false;
			}
			else if (runResult == false)//Error
			{
				changesText = string.Join(Environment.NewLine, outputs.Concat(errors));
				return true;//It does not actually have changes, but we return the error
			}
			else//Ran with output/errors
			{
				changesText = string.Empty;
				if (errors.Count > 0)
					changesText += "Errors: " + string.Join(Environment.NewLine, errors);

				foreach (var outStr in outputs)
				{
					bool ignoreOutStr = false;
					//foreach (string s in cSubversion_FilterList_StartsWith)
					//if (outStr.StartsWith(s, StringComparison.InvariantCultureIgnoreCase))
					if (GetPredicateForIgnoringOutputString(client)(outStr))
					{
						ignoreOutStr = true;
						break;
					}
					if (!ignoreOutStr)
					{
						if (changesText.Length > 0)
							changesText += Environment.NewLine;
						changesText += outStr.Replace(dir, "...");
					}
				}
				return !string.IsNullOrWhiteSpace(changesText);
			}
		}

		public static bool CheckFolderSubversionChanges(string dir, out string changesText)
		{
			return CheckFolderChanges(VersioningClient.Subversion, dir, out changesText);
		}

		public static bool CheckFolderGitChanges(string dir, out string changesText)
		{
			return CheckFolderChanges(VersioningClient.Git, dir, out changesText);
		}

		private static bool CheckFolderDiff(VersioningClient client, string dir, out string diffText)
		{
			List<string> outputs, errors;
			int exitCode;

			string exePath =
				client == VersioningClient.Subversion
				? cSvnPath
				: cGitPath;

			string cmdlineArgs =
				client == VersioningClient.Subversion
				? string.Format("diff {0} \"{1}\"", GetExtraSvnParams(), dir)
				: string.Format("diff --name-status \"{0}\"", dir);

			//Only need credentials if this if apache called this EXE (like for the BuildTestSystem) and apache is running as service
			bool? runResult = ProcessesInterop.RunProcessCatchOutput(
				new ProcessStartInfo(exePath, cmdlineArgs) { WorkingDirectory = dir },
				out outputs, out errors,
				out exitCode);
			if (runResult == true)//Ran and had no output
			{
				diffText = null;
				return false;
			}
			else if (runResult == false)//Error
			{
				diffText = string.Join(Environment.NewLine, outputs.Concat(errors));
				return true;//It does not actually have changes, but we return the error
			}
			else//Ran with output/errors
			{
				diffText = string.Empty;
				if (errors.Count > 0)
					diffText += "Errors: " + string.Join(Environment.NewLine, errors);
				foreach (var outStr in outputs)
				{
					bool ignoreOutStr = false;
					//foreach (string s in cSubversion_FilterList_StartsWith)
					//if (outStr.StartsWith(s, StringComparison.InvariantCultureIgnoreCase))
					if (GetPredicateForIgnoringOutputString(client)(outStr))
					{
						ignoreOutStr = true;
						break;
					}
					if (!ignoreOutStr)
					{
						if (diffText.Length > 0)
							diffText += Environment.NewLine;
						diffText += outStr.Replace(dir, "...");
					}
				}
				return !string.IsNullOrWhiteSpace(diffText);
			}
		}

		public static bool CheckFolderSubversionDiff(string dir, out string diffText)
		{
			return CheckFolderDiff(VersioningClient.Subversion, dir, out diffText);
		}

		public static bool CheckFolderGitDiff(string dir, out string diffText)
		{
			return CheckFolderDiff(VersioningClient.Git, dir, out diffText);
		}

		private static char[] WhiteSpaceChars = new char[] { ' ', '\n', '\t', '\r' };
		/// <summary>
		/// Used after getting changesText from method CheckFolderSubversionChanges().
		/// </summary>
		/// <param name="statusText">The status text obtained from method CheckFolderSubversionChanges().</param>
		/// <returns></returns>
		public static bool Subversion_HasLocalChanges(string statusText)
		{
			if (string.IsNullOrWhiteSpace(statusText))
				return false;
			return statusText.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
				.Count(l => l.StartsWith("M              ")
					|| l.StartsWith("A              ")
					|| l.StartsWith("D              ")
					|| l.StartsWith("?              ")) > 0;
		}

		/// <summary>
		/// Used after getting changesText from method CheckFolderSubversionChanges().
		/// </summary>
		/// <param name="statusText">The status text obtained from method CheckFolderSubversionChanges().</param>
		/// <returns></returns>
		public static bool Subversion_HasRemoteChanges(string statusText)
		{
			if (string.IsNullOrWhiteSpace(statusText))
				return false;
			return statusText.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
				.Count(l => l.StartsWith("        *        ")
					|| l.TrimStart(WhiteSpaceChars).StartsWith("*        ")) > 0;
		}
	}
}