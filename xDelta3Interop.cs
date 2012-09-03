using System;
using System.Collections.Generic;
using System.IO;

namespace SharedClasses
{
	public class xDelta3Interop
	{
		private const string xdelta3Path = @"C:\Windows\xdelta3.exe";
		//private const string MetadataTablename = "metadata";

		public enum XDelta3Command { MakePatch, ApplyPatch };
		public static bool DoXDelta3Command(XDelta3Command command, string file1, string file2, string file3, TextFeedbackEventHandler textFeedbackHandler)
		{
			if (!File.Exists(xdelta3Path))
			{
				NetworkInterop.FtpDownloadFile(
					null,
					Path.GetDirectoryName(xdelta3Path),
					OnlineSettings.OnlineAppsSettings.Instance.AppsDownloadFtpUsername,//GlobalSettings.VisualStudioInteropSettings.Instance.FtpUsername,
					OnlineSettings.OnlineAppsSettings.Instance.AppsDownloadFtpPassword,//GlobalSettings.VisualStudioInteropSettings.Instance.FtpPassword,
					OnlineSettings.AutoSyncSettings.Instance.OnlineXDeltaExeFileUrl,
					(err) => UserMessages.ShowErrorMessage(err),
					textFeedbackHandler);
			}

			try
			{
				string commandStr = "";
				//For usage of xdelta3.exe, say "xdelta3.exe -h"
				switch (command)
				{
					case XDelta3Command.MakePatch:
						commandStr = "-e -f -s";
						break;
					case XDelta3Command.ApplyPatch:
						commandStr = "-d -f -s";
						break;
				}

				List<string> outputs;
				List<string> errors;
				int exitcode;
				bool? result = SharedClasses.ProcessesInterop.RunProcessCatchOutput(
					new System.Diagnostics.ProcessStartInfo(
						xdelta3Path,
						string.Format("{0} \"{1}\" \"{2}\" \"{3}\"", commandStr, file1, file2, file3)),
					out outputs,
					out errors,
					out exitcode);

				if (result == true)//Successfully ran with no errors/output
					return true;
				else if (result == null)//Successfully ran, but had some errors/output
				{
					string errMsgesConcated = "";
					if (outputs.Count > 0)
						errMsgesConcated += string.Join("|", outputs);
					if (errors.Count > 0)
						errMsgesConcated += (errMsgesConcated.Length > 0 ? "|" : "") + string.Join("|", errors);
					TextFeedbackEventArgs.RaiseSimple(textFeedbackHandler, "There were errors when trying to " + command + " using xDelta3: " + errMsgesConcated, TextFeedbackType.Error);
					return false;
				}
				else// if (result == false)//Unable to run process
				{
					TextFeedbackEventArgs.RaiseSimple(textFeedbackHandler, "Unable to patch using xDelta3 (" + xdelta3Path + "), could not start process", TextFeedbackType.Error);
					return false;
				}
			}
			catch (Exception exc)
			{
				UserMessages.ShowWarningMessage("Cannot run xDelta3 command '" + command.ToString() + "': " + exc.Message);
				return false;
			}
		}

		public static bool MakePatch(string oldfile, string newfile, string deltafile, TextFeedbackEventHandler textFeedbackHandler)
		{
			return DoXDelta3Command(XDelta3Command.MakePatch, oldfile, newfile, deltafile, textFeedbackHandler);
		}

		public static bool ApplyPatch(string originalfile, string difffile, string patchedfile, TextFeedbackEventHandler textFeedbackHandler)
		{
			return DoXDelta3Command(XDelta3Command.ApplyPatch, originalfile, difffile, patchedfile, textFeedbackHandler);
		}
	}
}