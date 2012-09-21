using System;
using System.IO;
using System.Collections.Generic;

namespace SharedClasses
{
	public static class TesseractOCRinterop
	{
		private const string TesseractExePath = @"c:\windows\FJH\tesseract\tesseract.exe";
		private const string TesseractZipAdditionalDataRequired = @"c:\windows\FJH\tesseract\tessdata.7z";
		public static bool PerformOCR(string imageToRecognizeFilepath, out string outputFile, out List<string> allfeedbackCatched, Action<int, double> onProgress_Percentage_BytesPerSec)
		{
			allfeedbackCatched = new List<string>();

			if (!Directory.Exists(Path.GetDirectoryName(TesseractExePath)))
				Directory.CreateDirectory(Path.GetDirectoryName(TesseractExePath));
			if (!File.Exists(TesseractExePath))
			{
				string err;
				if (true != PhpDownloadingInterop.PhpDownloadFile(
					@"_TesseractOCR\tesseract.exe",
					TesseractExePath,
					null,
					out err,
					onProgress_Percentage_BytesPerSec,
					delegate { return true; }))
				{
					allfeedbackCatched.Add(err);
					outputFile = null;
					return false;
				}
			}
			if (!File.Exists(TesseractZipAdditionalDataRequired))
			{
				string err;
				if (true != PhpDownloadingInterop.PhpDownloadFile(
					@"_TesseractOCR\tessdata.7z",
					TesseractZipAdditionalDataRequired,
					null,
					out err,
					onProgress_Percentage_BytesPerSec,
					delegate { return true; }))
				{
					allfeedbackCatched.Add(err);
					outputFile = null;
					return false;
				}
			}
			string tmpunzipDir = Path.ChangeExtension(TesseractZipAdditionalDataRequired, null);
			if (!Directory.Exists(tmpunzipDir))
			{
				Directory.CreateDirectory(tmpunzipDir);
				string err;
				if (!ZipUnzipInterop.UnzipFile(TesseractZipAdditionalDataRequired, tmpunzipDir, out err, onProgress_Percentage_BytesPerSec))
				{
					allfeedbackCatched.Add(err);
					outputFile = null;
					return false;
				}
			}

			List<string> outputs;
			List<string> errors;
			int exitcode;
			outputFile = imageToRecognizeFilepath + ".ocr.txt";
			bool? result = ProcessesInterop.RunProcessCatchOutput(
				new System.Diagnostics.ProcessStartInfo(
					TesseractExePath,
					string.Format("\"{0}\" \"{1}\"",
						imageToRecognizeFilepath,
						outputFile.Substring(0, outputFile.Length - 4))),/*Do not use the .txt as the tesseract.exe automatically appends .txt to filenames*/
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
				allfeedbackCatched.Add("Error: There were errors when trying to recognize tet using tesseract.exe: " + errMsgesConcated);
				outputFile = null;
				return false;
			}
			else// if (result == false)//Unable to run process
			{
				allfeedbackCatched.Add("Error: Unable to perform using tesseract.exe: " + string.Join("|", errors));
				outputFile = null;
				return false;
			}
		}
	}
}