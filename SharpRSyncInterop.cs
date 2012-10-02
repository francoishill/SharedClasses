using System;
using System.Collections.Generic;
using System.IO;

namespace SharedClasses
{
	public class SharpRSyncInterop
	{
		private const string sharpRSyncPath = @"C:\Windows\sharprsync.exe";
		//private const string MetadataTablename = "metadata";

		public enum SharpRSyncCommands { Signature, Delta, Patch };
		public static bool DoSharpRSyncCommand(SharpRSyncCommands command, string file1, string file2, string file3, TextFeedbackEventHandler textFeedbackHandler)
		{
			if (!File.Exists(sharpRSyncPath))
			{
				NetworkInterop.FtpDownloadFile(
					null,
					Path.GetDirectoryName(sharpRSyncPath),
					SettingsSimple.OnlineAppsSettings.Instance.AppsDownloadFtpUsername,
					SettingsSimple.OnlineAppsSettings.Instance.AppsDownloadFtpPassword,
					SettingsSimple.AutoSyncSettings.Instance.OnlineSharpRSyncFileUrl,
					(err) => UserMessages.ShowErrorMessage(err),
					textFeedbackHandler);
			}

			try
			{
				List<string> outputs;
				List<string> errors;
				int exitcode;
				bool? result = SharedClasses.ProcessesInterop.RunProcessCatchOutput(
					new System.Diagnostics.ProcessStartInfo(
						sharpRSyncPath,
						file3 == null
						? string.Format("{0} \"{1}\" \"{2}\"", command.ToString(), file1, file2)
						: string.Format("{0} \"{1}\" \"{2}\" \"{3}\"", command.ToString(), file1, file2, file3)),
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
					TextFeedbackEventArgs.RaiseSimple(textFeedbackHandler, "There were errors when trying to " + command + " using SharpRSync: " + errMsgesConcated, TextFeedbackType.Error);
					return false;
				}
				else// if (result == false)//Unable to run process
				{
					TextFeedbackEventArgs.RaiseSimple(textFeedbackHandler, "Unable to patch using SharpRSync (" + sharpRSyncPath + "), could not start process", TextFeedbackType.Error);
					return false;
				}
			}
			catch (Exception exc)
			{
				UserMessages.ShowWarningMessage("Cannot run SharpRSync command '" + command.ToString() + "': " + exc.Message);
				return false;
			}
		}

		public static bool GenerateSignature(string inputfile, string signaturefile, TextFeedbackEventHandler textFeedbackHandler)
		{
			return DoSharpRSyncCommand(SharpRSyncCommands.Signature, inputfile, signaturefile, null, textFeedbackHandler);
		}

		public static bool MakePatch(string signaturefile, string inputfile, string deltafile, TextFeedbackEventHandler textFeedbackHandler)
		{
			return DoSharpRSyncCommand(SharpRSyncCommands.Delta, signaturefile, inputfile, deltafile, textFeedbackHandler);
		}

		public static bool ApplyPatch(string basefile, string deltafile, string outputfile, TextFeedbackEventHandler textFeedbackHandler)
		{
			return DoSharpRSyncCommand(SharpRSyncCommands.Patch, basefile, deltafile, outputfile, textFeedbackHandler);
		}
	}
}