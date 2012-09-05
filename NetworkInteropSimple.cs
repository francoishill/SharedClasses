using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Net;

namespace SharedClasses
{
	public class NetworkInteropSimple
	{
		public static string FtpDownloadFile(string localRootFolder, string userName, string password, string onlineFileUrl, Action<string> actionOnError, Action<int> actionOnProgressPercentage)
		{
			int maxRetries = 5;
			int retryCount = 0;
			try
			{
				if (!Directory.Exists(localRootFolder))
					Directory.CreateDirectory(localRootFolder);
				using (System.Net.WebClient client = new System.Net.WebClient())
				{
					client.Credentials = new System.Net.NetworkCredential(userName, password);
					bool isComplete = false;
					long filesize = FtpGetFileSize(onlineFileUrl, userName, password, actionOnError);

					if (filesize == -1)//File does not exist
					{
						string errMsg = "Ftp file does not exist: " + onlineFileUrl;
						actionOnError(errMsg);
						actionOnError(errMsg);
						return null;
					}

					client.DownloadFileCompleted += (snder, evtargs) =>
					{
						actionOnProgressPercentage(100);
						isComplete = true;
					};
					client.DownloadProgressChanged += (snder, evtargs) =>
					{
						if (!isComplete)
						{
							int percentage = (int)Math.Truncate((double)100 * (double)evtargs.BytesReceived / (double)filesize);
							actionOnProgressPercentage(percentage);
						}
					};
					string localFilepath = localRootFolder.TrimEnd('\\') + "\\" + Path.GetFileName(onlineFileUrl.Replace("ftp://", "").Replace("/", "\\"));

				retryhere:
					client.DownloadFileAsync(new Uri(onlineFileUrl), localFilepath);
					while (!isComplete)
					{ }// Application.DoEvents();

					//int tmptodo;
					//TODO: Checking file length = 0? What if its a blank/empty file??
					if (retryCount <= maxRetries && (!File.Exists(localFilepath) || new FileInfo(localFilepath).Length == 0))
					{
						if (File.Exists(localFilepath) && new FileInfo(localFilepath).Length == 0)
							if (filesize > 0)
							{
								retryCount++;
								isComplete = false;
								Thread.Sleep(1000);
								if (retryCount <= maxRetries && (!File.Exists(localFilepath) || new FileInfo(localFilepath).Length == 0))
									goto retryhere;
							}
					}

					if (!File.Exists(localFilepath) || new FileInfo(localFilepath).Length != filesize)
					{
						if (File.Exists(localFilepath))
							File.Delete(localFilepath);
						return null;
					}
					return localFilepath;
				}
			}
			catch (Exception exc)
			{
				if (exc.Message.ToLower().Contains("the operation has timed out"))
					actionOnError("Download from ftp timed out, the System.Net.ServicePointManager.DefaultConnectionLimit has been reached");
				else
					actionOnError("Exception in transfer: " + exc.Message);
			}
			return null;
		}

		public static long FtpGetFileSize(string fullFileUri, string userName, string password, Action<string> actionOnError)
		{
			try
			{
				FtpWebRequest reqSize = (FtpWebRequest)FtpWebRequest.Create(new Uri(fullFileUri));
				reqSize.Credentials = new NetworkCredential(userName, password);
				reqSize.Method = WebRequestMethods.Ftp.GetFileSize;
				reqSize.UseBinary = true;

				FtpWebResponse loginresponse = (FtpWebResponse)reqSize.GetResponse();
				FtpWebResponse respSize = (FtpWebResponse)reqSize.GetResponse();
				respSize = (FtpWebResponse)reqSize.GetResponse();
				long size = respSize.ContentLength;

				respSize.Close();

				return size;
			}
			catch (WebException ex)
			{
				FtpWebResponse response = (FtpWebResponse)ex.Response;
				if (response.StatusCode ==
					FtpStatusCode.ActionNotTakenFileUnavailable)
				{
					response.Close();
					return -1;//Does not exist
				}
				response.Close();
				actionOnError("Cannot determine ftp file size for '" + fullFileUri + ex.Message);
				return -2;//Cannot obtain size (could be internet connectivity, timeout, etc)
			}
		}
	}
}