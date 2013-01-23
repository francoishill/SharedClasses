using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Net;

namespace SharedClasses
{
	public class NetworkInteropSimple
	{
		#region Http
		/*public static bool HttpDownloadFile(string fullLocalPath, string onlineFileUrl, Action<string, FeedbackMessageTypes> actionOnMessage, Action<int> actionOnProgressPercentage)
		{
			HttpWebRequest request;
			HttpWebResponse response = null;

			try
			{
				request = (HttpWebRequest)WebRequest.Create(onlineFileUrl);
				//
				//request.AddRange(0, 107046);
				//request.AddRange(107047, 214091);
				request.Timeout = 10000;
				request.AllowWriteStreamBuffering = false;
				response = (HttpWebResponse)request.GetResponse();
				Stream s = response.GetResponseStream();

				//Write to disk
				FileStream fs = new FileStream(fullLocalPath, FileMode.Create);
				byte[] read = new byte[256];
				int count = s.Read(read, 0, read.Length);
				while (count > 0)
				{
					fs.Write(read, 0, count);
					count = s.Read(read, 0, read.Length);
				}

				//Close everything
				fs.Close();
				s.Close();
				response.Close();
				return true;
			}

			catch (System.Net.WebException)
			{
				if (response != null)
					response.Close();
				return false;
			}
		}*/
		#endregion Http

		#region Ftp
		public static string FtpDownloadFile(string localRootFolder, string userName, string password, string onlineFileUrl, Action<string, FeedbackMessageTypes> actionOnMessage, Action<int> actionOnProgressPercentage)
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
					long filesize = FtpGetFileSize(onlineFileUrl, userName, password, actionOnMessage);

					if (filesize == -1)//File does not exist
					{
						string errMsg = "Ftp file does not exist: " + onlineFileUrl;
						actionOnMessage(errMsg, FeedbackMessageTypes.Error);
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
					actionOnMessage("Download from ftp timed out, the System.Net.ServicePointManager.DefaultConnectionLimit has been reached", FeedbackMessageTypes.Error);
				else
					actionOnMessage("Exception in transfer: " + exc.Message, FeedbackMessageTypes.Error);
			}
			return null;
		}

		public static long FtpGetFileSize(string fullFileUri, string userName, string password, Action<string, FeedbackMessageTypes> actionOnMessage)
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
				actionOnMessage("Cannot determine ftp file size for '" + fullFileUri + ex.Message, FeedbackMessageTypes.Error);
				return -2;//Cannot obtain size (could be internet connectivity, timeout, etc)
			}
		}
		#endregion Ftp
	}
}