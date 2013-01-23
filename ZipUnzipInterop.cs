using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace SharedClasses
{
	public static class ZipUnzipInterop
	{
		//No support for 7zip 64bit yet, do not have the 64bit 7z.exe
		/*private const string _7zipDllNameOnly = "7z.dll";//{ get { return Environment.Is64BitProcess ? "7z64.dll" : "7z.dll"; } }
		private const string _7zipDllPath = @"C:\Windows\FJH\7zip\" + _7zipDllNameOnly;//{ get { return @"C:\Windows\FJH\7zip\" + _7zipDllNameOnly; } }*/
		private const string _7zipExeNameOnly = "7z.exe";
		/*private const string _7zipExePath = @"C:\Windows\FJH\7zip\" + _7zipExeNameOnly;
		public static bool Ensure7zipDllExists(out List<string> allfeedbackCatched, Action<int, double> onProgress_Percentage_BytesPerSec)
		{
			allfeedbackCatched = new List<string>();
			if (File.Exists(_7zipDllPath) && File.Exists(_7zipExePath))
				return true;

			if (!Directory.Exists(Path.GetDirectoryName(_7zipDllPath)))
				Directory.CreateDirectory(Path.GetDirectoryName(_7zipDllPath));
			string err;
			if (!File.Exists(_7zipDllPath))
			{
				if (true != PhpDownloadingInterop.PhpDownloadFile(
					@"_7zip\" + _7zipDllNameOnly,
					_7zipDllPath,
					null,
					out err,
					onProgress_Percentage_BytesPerSec,
					delegate { return true; }))
					allfeedbackCatched.Add(err);
			}
			if (!File.Exists(_7zipExePath))
			{
				if (true != PhpDownloadingInterop.PhpDownloadFile(
					   @"_7zip\" + _7zipExeNameOnly,
					   _7zipExePath,
					   null,
					   out err,
					   onProgress_Percentage_BytesPerSec,
					   delegate { return true; }))
					allfeedbackCatched.Add(err);
			}
			if (File.Exists(_7zipDllPath) && File.Exists(_7zipExePath))
				return true;
			else
				return false;
		}*/

		public static bool UnzipFile(string zipfilePath, string unzipFolderpath, out string errorIfFailed, Action<int, double> onProgress_Percentage_BytesPerSec)
		{
			//Does not dynamically download the 7z.exe and 7z.dll, will just inform user to install it

			//List<string> feedback;
			//if (!Ensure7zipDllExists(out feedback, onProgress_Percentage_BytesPerSec))
			//{
			//    errorIfFailed = string.Join("|", feedback);
			//    return false;
			//}

			try
			{
				List<string> outputs;
				List<string> errors;
				int exitcode;
				bool? result = ProcessesInterop.RunProcessCatchOutput(
					new System.Diagnostics.ProcessStartInfo(
						_7zipExeNameOnly,//_7zipExePath,
						string.Format("x \"{0}\" -o\"{1}\"",
							zipfilePath,
							unzipFolderpath.TrimEnd('\\'))),
					out outputs,
					out errors,
					out exitcode);

				if (result == true
					|| outputs.Contains("Everything is Ok", StringComparer.InvariantCultureIgnoreCase))//Successfully ran with no errors/output
				{
					errorIfFailed = null;
					return true;
				}
				else if (result == null)//Successfully ran, but had some errors/output
				{
					string errMsgesConcated = "";
					if (outputs.Count > 0)
						errMsgesConcated += string.Join("|", outputs);
					if (errors.Count > 0)
						errMsgesConcated += (errMsgesConcated.Length > 0 ? "|" : "") + string.Join("|", errors);
					errorIfFailed = "Error: There were errors when trying to recognize tet using 7zip.exe: " + errMsgesConcated;
					return false;
				}
				else// if (result == false)//Unable to run process
				{
					errorIfFailed = "Error: Unable to perform using 7zip.exe: " + string.Join("|", errors);
					return false;
				}
			}
			catch (Exception exc)
			{
				errorIfFailed = exc.Message;
				return false;
			}
		}
	}
}