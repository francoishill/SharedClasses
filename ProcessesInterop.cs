using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace SharedClasses
{
	public class ProcessesInterop
	{
		private static Process GetRedirectingProcess(ProcessStartInfo startInfo, Action<object, DataReceivedEventArgs> onOutput, Action<object, DataReceivedEventArgs> onError)
		{
			Process proc = new Process();

			startInfo.RedirectStandardError = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.CreateNoWindow = true;
			startInfo.UseShellExecute = false;

			proc.StartInfo = startInfo;

			proc.OutputDataReceived += (s, e) => onOutput(s, e);
			proc.ErrorDataReceived += (s, e) => onError(s, e);

			return proc;
		}

		public static bool StartAndWaitProcessRedirectOutput(ProcessStartInfo startInfo, Action<object, DataReceivedEventArgs> onOutput, Action<object, DataReceivedEventArgs> onError)
		{
			Process proc = GetRedirectingProcess(startInfo, onOutput, onError);

			if (!proc.Start())
				return false;

			proc.BeginErrorReadLine();
			proc.BeginOutputReadLine();
			proc.WaitForExit();

			proc.Dispose();
			proc = null;
			return true;
		}

		public static Process StartDontWaitProcessRedirectOutput(ProcessStartInfo startInfo, Action<object, DataReceivedEventArgs> onOutput, Action<object, DataReceivedEventArgs> onError)
		{
			Process proc = GetRedirectingProcess(startInfo, onOutput, onError);
			if (!proc.Start())
				return null;

			proc.BeginErrorReadLine();
			proc.BeginOutputReadLine();
			return proc;
		}

		//true=ran no output/error, false=could not run, null=ran with output/error
		public static bool? StartProcessCatchOutput(ProcessStartInfo startInfo, out List<string> outputs, out List<string> errors)
		{
			List<string> tmpoutputs = new List<string>();
			List<string> tmperrors = new List<string>();

			bool result = StartAndWaitProcessRedirectOutput(
				startInfo,
				(sn, outev) => { if (outev.Data != null) tmpoutputs.Add(outev.Data); },
				(sn, errev) => { if (errev.Data != null) tmperrors.Add(errev.Data); });
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