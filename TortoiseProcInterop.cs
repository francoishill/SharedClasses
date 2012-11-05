using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace SharedClasses
{
	public static class TortoiseProcInterop
	{
		public enum TortoiseCommands { Log, Commit, Update };
		public static Process StartTortoiseProc(TortoiseCommands tortoiseCommand, string dir)
		{
			return Process.Start(
				@"C:\Program Files\TortoiseSVN\bin\TortoiseProc.exe",
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

		public const string svnPath = @"C:\Program Files\TortoiseSVN\bin\svn.exe";
		private static string[] FilterList_StartsWith = new string[]
		{ 
			"Performing status on external item",
			"X       ",
			"Status against revision"
		};
		public static bool CheckFolderSubversionChanges(string dir, out string changesText)
		{
			List<string> outputs, errors;
			int exitCode;

			//Only need credentials if this if apache called this EXE (like for the BuildTestSystem) and apache is running as service
			bool? runResult = ProcessesInterop.RunProcessCatchOutput(
				new ProcessStartInfo(svnPath,
					"status --show-updates "
					+ GetExtraSvnParams()
					+ " \"" + dir + "\""),
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
					foreach (string s in FilterList_StartsWith)
						if (outStr.StartsWith(s, StringComparison.InvariantCultureIgnoreCase))
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

		public static bool CheckFolderSubversionDiff(string dir, out string diffText)
		{
			List<string> outputs, errors;
			int exitCode;

			//Only need credentials if this if apache called this EXE (like for the BuildTestSystem) and apache is running as service
			bool? runResult = ProcessesInterop.RunProcessCatchOutput(
				new ProcessStartInfo(svnPath,
					"diff "
					+ GetExtraSvnParams()
					+ " \"" + dir + "\""),
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
					foreach (string s in FilterList_StartsWith)
						if (outStr.StartsWith(s, StringComparison.InvariantCultureIgnoreCase))
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

		private static char[] WhiteSpaceChars = new char[] { ' ', '\n', '\t', '\r' };
		/// <summary>
		/// Used after getting changesText from method CheckFolderSubversionChanges().
		/// </summary>
		/// <param name="statusText">The status text obtained from method CheckFolderSubversionChanges().</param>
		/// <returns></returns>
		public static bool HasLocalChanges(string statusText)
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
		public static bool HasRemoteChanges(string statusText)
		{
			if (string.IsNullOrWhiteSpace(statusText))
				return false;
			return statusText.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
				.Count(l => l.StartsWith("        *        ")
					|| l.TrimStart(WhiteSpaceChars).StartsWith("*        ")) > 0;
		}
	}
}