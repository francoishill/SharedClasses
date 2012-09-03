using System;
using System.IO;
using System.Collections.Generic;

namespace SharedClasses
{
	public static class PdfToolkitInterop
	{
		private const string PdfTkExePath = @"c:\windows\pdftk.exe";
		public enum RotationTypes { _90clockwise, _180clockwise, _270clockwise };
		public static bool PerformRotation(string intputFile, string outputFile, RotationTypes rotation, out List<string> allfeedbackCatched)
		{
			allfeedbackCatched = new List<string>();
			if (!File.Exists(PdfTkExePath))
			{
				List<string> tmplist = new List<string>();
				NetworkInterop.FtpDownloadFile(
					null,
					Path.GetDirectoryName(PdfTkExePath),
					OnlineSettings.OnlineAppsSettings.Instance.AppsDownloadFtpUsername,//GlobalSettings.VisualStudioInteropSettings.Instance.FtpUsername,
					OnlineSettings.OnlineAppsSettings.Instance.AppsDownloadFtpPassword,//GlobalSettings.VisualStudioInteropSettings.Instance.FtpPassword,
					OnlineSettings.AutoSyncSettings.Instance.OnlinePdfTkExeFileUrl,
					(err) => UserMessages.ShowErrorMessage(err),
					(snder, textfeedback) => tmplist.Add(textfeedback.FeedbackType.ToString() + ":" + textfeedback.FeedbackText));
				allfeedbackCatched.AddRange(tmplist);
			}

			List<string> outputs;
			List<string> errors;
			string commandStr = null;
			//1-endW means page 1 to end, West direction rotation
			switch (rotation)
			{
				case RotationTypes._90clockwise:
					commandStr = "1-endE";
					break;
				case RotationTypes._180clockwise:
					commandStr = "1-endS";
					break;
				case RotationTypes._270clockwise:
					commandStr = "1-endW";
					break;
				default:
					break;
			}
			int exitcode;
			bool? result = ProcessesInterop.RunProcessCatchOutput(
				new System.Diagnostics.ProcessStartInfo(
					PdfTkExePath,
					string.Format("\"{0}\" cat {1} output \"{2}\"", intputFile, commandStr, outputFile)),
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
				allfeedbackCatched.Add("Error: There were errors when trying to " + commandStr + " using pdftk.exe: " + errMsgesConcated);
				return false;
			}
			else// if (result == false)//Unable to run process
			{
				allfeedbackCatched.Add("Error: Unable to perform using pdftk.exe: " + string.Join("|", errors));
				return false;
			}
		}
	}
}