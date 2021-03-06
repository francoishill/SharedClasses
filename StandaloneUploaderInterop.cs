using System;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading;

namespace SharedClasses
{
	public class StandaloneUploaderInterop
	{
		public static bool UploadVia_StandaloneUploader_UsingExternalApp(Action<string> actionOnError, string DisplayName, UploadingProtocolTypes ProtocolType , string LocalPath, string FtpUrl, string FtpUsername, string FtpPassword, bool AutoOverwriteIfExists, bool WaitForExit = false)
		{
			string notifAppExe = RegistryInterop.GetAppPathFromRegistry("StandaloneUploader.exe");
			if (notifAppExe == null)
			{
				if (actionOnError != null)
					actionOnError("Cannot use StandaloneUploader, unable to find it in App Paths of Registry.");
				return false;
			}
			else
			{
				Process proc = new Process();
				proc.StartInfo = new ProcessStartInfo(notifAppExe,
					string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\"{6}",
						DisplayName,
						ProtocolType.ToString(),
						LocalPath,
						FtpUrl,
						FtpUsername,
						FtpPassword,
						AutoOverwriteIfExists ? " \"overwrite\"" : ""))
						{
							UseShellExecute = false
						};
				try
				{
					if (!proc.Start())
					{
						actionOnError("Cannot start process: " + notifAppExe);
						return false;
					}
				}
				catch (Exception exc)
				{
					actionOnError("Error qeueing for upload: " + exc.Message);
					return false;
				}

				try
				{
					if (WaitForExit)
						proc.WaitForExit();
					else
					{
						bool found = false;
						while (!found)
						{
							var procs = Process.GetProcessesByName(proc.ProcessName);
							if (procs.Length > 0)
								found = true;
							Thread.Sleep(500);
						}
					}
					return true;
				}
				catch (Exception exc)
				{
					actionOnError("An error occurred after starting process to upload files: " + exc.Message);
					return true;//We still return true because we assume the process actually started
				}
			}

		}
	}
}