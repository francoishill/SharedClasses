using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;

namespace SharedClasses
{
	public class ProcessesInterop
	{
		private static Process GetRedirectingProcess(ProcessStartInfo startInfo, Action<object, string> onOutput, Action<object, string> onError, string WriteToStandardInputBeforeReadingText)
		{
			Process proc = new Process();

			if (WriteToStandardInputBeforeReadingText != null)
				startInfo.RedirectStandardInput = true;
			startInfo.RedirectStandardError = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.CreateNoWindow = true;
			startInfo.UseShellExecute = false;

			proc.StartInfo = startInfo;

			proc.OutputDataReceived += (s, e) => onOutput(s, e.Data);
			proc.ErrorDataReceived += (s, e) => onError(s, e.Data);

			return proc;
		}

		public static bool StartAndWaitProcessRedirectOutput(ProcessStartInfo startInfo, Action<object, string> onOutput, Action<object, string> onError, out int ExitCodeIfRan, string WriteToStandardInputBeforeReadingText = null)
		{
			Process proc = GetRedirectingProcess(startInfo, onOutput, onError, WriteToStandardInputBeforeReadingText);

			try
			{
				if (!proc.Start())
				{
					ExitCodeIfRan = -1;
					return false;
				}
			}
			catch (Exception exc)
			{
				onError(null, "Could not start '" + startInfo.FileName + "': " + exc.Message);
				ExitCodeIfRan = -1;
				return false;
			}

			proc.BeginErrorReadLine();
			proc.BeginOutputReadLine();
			if (WriteToStandardInputBeforeReadingText != null)
				proc.StandardInput.Write(WriteToStandardInputBeforeReadingText);
			proc.WaitForExit();
			ExitCodeIfRan = proc.ExitCode;
			proc.Dispose();
			proc = null;
			return true;
		}

		public static Process StartDontWaitProcessRedirectOutput(ProcessStartInfo startInfo, Action<object, string> onOutput, Action<object, string> onError, string WriteToStandardInputBeforeReadingText = null)
		{
			Process proc = GetRedirectingProcess(startInfo, onOutput, onError, WriteToStandardInputBeforeReadingText);
			if (!proc.Start())
				return null;

			proc.BeginErrorReadLine();
			proc.BeginOutputReadLine();
			if (WriteToStandardInputBeforeReadingText != null)
				proc.StandardInput.Write(WriteToStandardInputBeforeReadingText);
			return proc;
		}

		/// <summary>
		/// true=ran no output/error, false=could not run, null=ran with output/error
		/// </summary>
		/// <param name="startInfo">The Process StartInfo for the process to run.</param>
		/// <param name="outputs">The output strings that were redirected from the process</param>
		/// <param name="errors">The error strings that were redirected from the process.</param>
		/// <returns>True (ran successfully and had no output/errors), False (Could not run), Null (ran but had output/error feedback).</returns>
		public static bool? RunProcessCatchOutput(ProcessStartInfo startInfo, out List<string> outputs, out List<string> errors, out int ExitCodeIfRan, string WriteToStandardInputBeforeReadingText = null)
		{
			List<string> tmpoutputs = new List<string>();
			List<string> tmperrors = new List<string>();

			bool result = StartAndWaitProcessRedirectOutput(
				startInfo,
				(sn, outev) => { if (outev != null) tmpoutputs.Add(outev); },
				(sn, errev) => { if (errev != null) tmperrors.Add(errev); },
				out ExitCodeIfRan,
				WriteToStandardInputBeforeReadingText);
			outputs = tmpoutputs;
			errors = tmperrors;
			if (!result)
				return false;//Could not start

			if (outputs.Count > 0 || errors.Count > 0)
				return null;//Ran successful but has outputs/errors
			else return true;//Ran successful with no outputs/errors
		}
	}
}