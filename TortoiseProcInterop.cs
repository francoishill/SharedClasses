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

		private const string svnPath = @"C:\Program Files\TortoiseSVN\bin\svn.exe";
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
			bool? runResult = ProcessesInterop.RunProcessCatchOutput(
				new ProcessStartInfo(svnPath, "status --show-updates \"" + dir + "\""),
				out outputs, out errors,
				out exitCode);
			if (runResult == true)
			{
				changesText = null;
				return false;
			}
			else if (runResult == false)//Error
			{
				changesText = string.Join(Environment.NewLine, outputs.Concat(errors));
				return true;
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
	}
}