using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Collections.Specialized;
using System.Web;

namespace SharedClasses
{
	public struct DownloadRange
	{
		public int FromBytePosition;
		public int? ToBytePosition;
		public DownloadRange(int FromBytePosition, int? ToBytePosition = null)
		{
			this.FromBytePosition = FromBytePosition;
			this.ToBytePosition = ToBytePosition;
		}
	}

	public class TransferMonitoring :  IDisposable
	{
		public enum TransferSpeedFormats { KBperSec, MBperSec, GBperSec };
		public bool transferIsComplete = false;

		private long maximumBytes;
		private long currentCompleteByteCount;
		private Stopwatch _stopwatchrunning;
		private Stopwatch StopwatchRunning//TODO: Not thread safe yet...
		{
			get { if (_stopwatchrunning == null) _stopwatchrunning = new Stopwatch(); return _stopwatchrunning; }
		}

		public void StartProgressMonitoring(long maximumBytes, Action<int, double> actionOnProgress)
		{
			if (actionOnProgress == null) actionOnProgress = delegate { };
			this.maximumBytes = maximumBytes;
			StopwatchRunning.Restart();//Already created in its property, can now just start/restart
			actionOnProgress(0, maximumBytes);
		}

		public void UpdateProgressAndGetStats(long currentCompleteByteCount, out int progressPercentage, out double bytesPerSecond, TransferSpeedFormats? convertToOtherFormat = null)
		{
			this.currentCompleteByteCount = currentCompleteByteCount;
			progressPercentage = (int)Math.Truncate(100D * (double)currentCompleteByteCount / (double)maximumBytes);
			bytesPerSecond = (double)currentCompleteByteCount / (double)StopwatchRunning.Elapsed.TotalSeconds;
			if (convertToOtherFormat.HasValue)
				bytesPerSecond = ConvertBytesPerSecondTo(bytesPerSecond, convertToOtherFormat.Value);
		}
		public void UpdateProgressAndGetStats(long currentCompleteByteCount, Action<int, double> actionOnProgress)
		{
			if (actionOnProgress == null) actionOnProgress = delegate { };
			int progperc;
			double bytespersec;
			UpdateProgressAndGetStats(currentCompleteByteCount, out progperc, out bytespersec);
			actionOnProgress(progperc, bytespersec);
		}

		public double ConvertBytesPerSecondTo(double bytesPerSec, TransferSpeedFormats format)
		{
			switch (format)
			{
				case TransferSpeedFormats.KBperSec:
					return bytesPerSec / (1024D);
				case TransferSpeedFormats.MBperSec:
					return bytesPerSec / (1024D * 1024D);
				case TransferSpeedFormats.GBperSec:
					return bytesPerSec / (1024D * 1024D * 1024D);
				default:
					throw new Exception("Cannot convert bytes per second to unsupported format: " + format);
			}
		}

		public void Dispose()
		{
			if (_stopwatchrunning != null)
				_stopwatchrunning = null;
		}
	}

	//TODO: Requires timeout support
	public abstract class FileTransferProtocols<T> where T : new()
	{
		private static T instance;
		private static object lockingObject = new Object();
		public static T Instance
		{
			get
			{
				if (instance == null)
				{
					lock (lockingObject)
					{
						if (instance == null)
							instance = new T();//PropertyInterceptor<T>.Create();
					}
				}
				return instance;
			}
		}

		public abstract bool? DownloadFile(string fullLocalDestinationPath, string onlineFullUrl, Action<string, FeedbackMessageTypes> actionOnMessage = null, Action<int, double> actionOnProgress = null, DownloadRange? rangeToDownload = null, NetworkCredential credentials = null);
		public abstract bool? GetFileSize(string onlineFullUrl, out long outFileSize, Action<string, FeedbackMessageTypes> actionOnMessage = null, NetworkCredential credentials = null);
	}

	public sealed class FtpFileTransfer : FileTransferProtocols<FtpFileTransfer>
	{
		private FTPClient client;
		private class FTPClient : WebClient
		{
			protected override WebRequest GetWebRequest(System.Uri address)
			{
				FtpWebRequest req = (FtpWebRequest)base.GetWebRequest(address);
				req.UsePassive = true;// false;
				return req;
			}
		}

		/// <summary>
		/// Downloads a file and returns True if succeeded, False if error, Null if file does not exist
		/// </summary>
		/// <param name="fullLocalDestinationPath">The full file path to the local path where the file will be downloaded to.</param>
		/// <param name="onlineFullUrl">The full URL to the server file.</param>
		/// <param name="actionOnMessage">The action to take when a message is received.</param>
		/// <param name="actionOnProgress">The action to take when a progress feedback is received.</param>
		/// <param name="rangeToDownload">The range to download of the file, leave Null to download the complete file.</param>
		/// <param name="credentials">The credentials, if applicable, otherwise leave null.</param>
		/// <returns>True if succeeded, False if error, Null if file does not exist.</returns>
		public override bool? DownloadFile(string fullLocalDestinationPath, string onlineFullUrl, Action<string, FeedbackMessageTypes> actionOnMessage = null, Action<int, double> actionOnProgress = null, DownloadRange? rangeToDownload = null, NetworkCredential credentials = null)
		{
			//TODO: Support queueing of downloads inside this method
			if (actionOnMessage == null) actionOnMessage = delegate { };
			if (actionOnProgress == null) actionOnProgress = delegate { };

			int maxRetries = 5;
			int retryCount = 0;
			try
			{
				if (!Directory.Exists(Path.GetDirectoryName(fullLocalDestinationPath)))
					Directory.CreateDirectory(Path.GetDirectoryName(fullLocalDestinationPath));
				//using (FTPClient client = new FTPClient())
				using (client = new FTPClient())
				{
					if (credentials != null)
						client.Credentials = credentials;
					//client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1)");
					//foreach (string localFilename in localFilenames)
					//{
					long filesize;
					bool? getFilesizeResult = GetFileSize(onlineFullUrl, out filesize, actionOnMessage, credentials);

					if (!getFilesizeResult.HasValue)//File does not exist or error occurred
					{
						actionOnMessage("Ftp file does not exist: " + onlineFullUrl, FeedbackMessageTypes.Error);
						return null;
					}
					else if (getFilesizeResult.Value == false)
					{
						actionOnMessage("Could not download FTP file: " + onlineFullUrl, FeedbackMessageTypes.Error);
						return false;
					}

					string localFilepath
						//= localRootFolder.TrimEnd('\\') + "\\" + Path.GetFileName(onlineFileUrl.Replace("ftp://", "").Replace("/", "\\"));
						= fullLocalDestinationPath;//TODO: this changed from FtpDownloadFile

					using (var transferMonitor = new TransferMonitoring())
					{
						client.DownloadFileCompleted += (snder, evtargs) =>
						{
							transferMonitor.UpdateProgressAndGetStats(filesize, actionOnProgress);
							transferMonitor.transferIsComplete = true;
						};
						client.DownloadProgressChanged += (snder, evtargs) =>
						{
							if (!transferMonitor.transferIsComplete)
							{
								transferMonitor.UpdateProgressAndGetStats(evtargs.BytesReceived, actionOnProgress);
							}
						};

						retryhere:
						transferMonitor.StartProgressMonitoring(filesize, actionOnProgress);
						client.DownloadFileAsync(new Uri(onlineFullUrl), localFilepath);
						while (client.IsBusy)// !transferMonitor.transferIsComplete)
						{ }//{ Thread.Sleep(10); }// System.Windows.Forms.Application.DoEvents();

						//int tmptodo;
						//TODO: Checking file length = 0? What if its a blank/empty file??
						if (retryCount <= maxRetries && (!File.Exists(localFilepath) || new FileInfo(localFilepath).Length == 0))
						{
							if (File.Exists(localFilepath) && new FileInfo(localFilepath).Length == 0)
								if (filesize > 0)
								{
									retryCount++;
									transferMonitor.transferIsComplete = false;
									Thread.Sleep(1000);
									if (retryCount <= maxRetries && (!File.Exists(localFilepath) || new FileInfo(localFilepath).Length == 0))
										goto retryhere;
								}
						}
					}

					if (!File.Exists(localFilepath) || new FileInfo(localFilepath).Length != filesize)
					{
						bool fileExistedButWasEmpty = File.Exists(localFilepath) && new FileInfo(localFilepath).Length == 0;
						if (File.Exists(localFilepath))
							File.Delete(localFilepath);
						string errMsg = "Unable to download file";
						if (fileExistedButWasEmpty)
							errMsg += ", file download 'succeeded' but file was empty";
						errMsg += ":" + Environment.NewLine
							+ localFilepath + Environment.NewLine
							+ "downloaded from:" + Environment.NewLine
							+ onlineFullUrl;
						actionOnMessage(errMsg, FeedbackMessageTypes.Error);
						return false;
					}
					return true;
				}
			}
			catch (Exception exc)
			{
				if (exc.Message.ToLower().Contains("the operation has timed out"))
				{
					actionOnMessage("Download from ftp timed out, the System.Net.ServicePointManager.DefaultConnectionLimit has been reached", FeedbackMessageTypes.Error);
					/*if (UserMessages.Confirm("Download from ftp timed out, the System.Net.ServicePointManager.DefaultConnectionLimit has been reached, restart the application now?"))
						//Application.Restart();
						ApplicationRecoveryAndRestart.TestCrash(false);*/
				}
				actionOnMessage("Exception in transfer: " + exc.Message, FeedbackMessageTypes.Error);
				return false;
			}
		}

		/// <summary>
		/// Obtains the file size, true means it succeeded and size returned, false means error, null is file did not exist.
		/// </summary>
		/// <param name="onlineFullUrl">The full url to the server file.</param>
		/// <param name="outFileSize">The file to be returned if it succeeds.</param>
		/// <param name="actionOnMessage">The action to take when a message is received.</param>
		/// <param name="credentials">The credentials, if applicable, otherwise leave null.</param>
		/// <returns>True means it succeeded and size returned, False means error, Null is file did not exist.</returns>
		public override bool? GetFileSize(string onlineFullUrl, out long outFileSize, Action<string, FeedbackMessageTypes> actionOnMessage = null, NetworkCredential credentials = null)
		{
			if (actionOnMessage == null) actionOnMessage = delegate { };

			try
			{
				FtpWebRequest reqSize = (FtpWebRequest)FtpWebRequest.Create(new Uri(onlineFullUrl));
				if (credentials != null)
					reqSize.Credentials = credentials;
				reqSize.Method = WebRequestMethods.Ftp.GetFileSize;
				reqSize.UseBinary = true;

				FtpWebResponse loginresponse = (FtpWebResponse)reqSize.GetResponse();
				FtpWebResponse respSize = (FtpWebResponse)reqSize.GetResponse();
				respSize = (FtpWebResponse)reqSize.GetResponse();
				long size = respSize.ContentLength;

				respSize.Close();

				outFileSize = size;
				return true;
			}
			catch (WebException ex)
			{
				FtpWebResponse response = (FtpWebResponse)ex.Response;
				if (response.StatusCode ==
					FtpStatusCode.ActionNotTakenFileUnavailable)
				{
					response.Close();
					outFileSize = 0;
					return null;//-1;//Does not exist
				}
				response.Close();
				actionOnMessage("Cannot determine ftp file size for '" + onlineFullUrl + ex.Message, FeedbackMessageTypes.Error);
				outFileSize = 0;
				return false;//-2;//Cannot obtain size (could be internet connectivity, timeout, etc)
			}
		}
	}

	public sealed class PhpFileTransfer : FileTransferProtocols<PhpFileTransfer>
	{
		private const int cBufferSize = 1024 * 256;//8 KB
		private byte[] buffer = new byte[cBufferSize];

		private HttpWebRequest GetHttpWebRequest(String url, NameValueCollection nameValueCollection = null)
		{
			// Here we convert the nameValueCollection to POST data.
			// This will only work if nameValueCollection contains some items.
			var parameters = new StringBuilder();

			if (nameValueCollection != null && nameValueCollection.Count > 0)
			{
				foreach (var key in nameValueCollection.Keys)
				{
					parameters.AppendFormat("{0}={1}&",
						HttpUtility.UrlEncode(key.ToString()),
						HttpUtility.UrlEncode(nameValueCollection[key.ToString()]));
				}

				parameters.Length -= 1;
			}

			// Here we create the request and write the POST data to it.
			var request = (HttpWebRequest)HttpWebRequest.Create(url);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";

			using (var writer = new StreamWriter(request.GetRequestStream()))
			{
				writer.Write(parameters.ToString());
				writer.Flush();
			}

			return request;
		}

		public override bool? DownloadFile(string fullLocalDestinationPath, string downloadRelativePath, Action<string, FeedbackMessageTypes> actionOnMessage = null, Action<int, double> actionOnProgress = null, DownloadRange? rangeToDownload = null, NetworkCredential credentials = null)
		{
			if (actionOnMessage == null) actionOnMessage = delegate { };
			if (actionOnProgress == null) actionOnProgress = delegate { };

			try
			{
				//TODO: How to handle certificate validation??
				ServicePointManager.ServerCertificateValidationCallback += (snder, certif, chain, sslPolicyErrors)
					=> true;//actionOnSSLcertificateValidation(snder, certif, chain, sslPolicyErrors);
				NameValueCollection inputs = new NameValueCollection();
				inputs.Add("relativepath", downloadRelativePath.Replace('\\', '/'));
				var req = GetHttpWebRequest(SettingsSimple.HomePcUrls.Instance.PhpDownloadUrl, inputs);

				if (rangeToDownload.HasValue)
				{
					if (rangeToDownload.Value.ToBytePosition.HasValue)
						req.AddRange(rangeToDownload.Value.FromBytePosition, rangeToDownload.Value.ToBytePosition.Value);//Start and end
					else
						req.AddRange(rangeToDownload.Value.FromBytePosition);//Only start point, until end of file
				}

				HttpWebResponse response = (HttpWebResponse)req.GetResponse();

				long actualFilesize = response.ContentLength;
				long totalBytesToDownload = response.ContentLength;
				var contentrangelengthStr = response.GetResponseHeader("Content-RangeLength");
				var expectedMD5 = response.Headers[HttpResponseHeader.ContentMd5];
				if (!string.IsNullOrWhiteSpace(contentrangelengthStr))
				{
					long tmpint;
					if (long.TryParse(contentrangelengthStr, out tmpint))
						totalBytesToDownload = tmpint;
				}

				using (var transferMonitor = new TransferMonitoring())
				{
					transferMonitor.StartProgressMonitoring(totalBytesToDownload, actionOnProgress);

					var stream = response.GetResponseStream();
					using (var fs = new FileStream(fullLocalDestinationPath, FileMode.Create))
					{
						long totalDownloadedBytes = 0;
						int readcount = stream.Read(buffer, 0, buffer.Length);
						while (readcount > 0)
						{
							totalDownloadedBytes += readcount;
							transferMonitor.UpdateProgressAndGetStats(totalDownloadedBytes, actionOnProgress);

							fs.Write(buffer, 0, readcount);
							fs.Flush();
							if (totalDownloadedBytes == totalBytesToDownload)
								break;
							readcount = stream.Read(buffer, 0, buffer.Length);
						}
					}
				}

				string rangeStringForLogging = "";
				if (rangeToDownload.HasValue)
					rangeStringForLogging = rangeToDownload.Value.FromBytePosition + "-"
						+ (rangeToDownload.Value.ToBytePosition.HasValue ? rangeToDownload.Value.ToBytePosition.Value : totalBytesToDownload - 1)
						+ "/" + actualFilesize;
				else
					rangeStringForLogging = "0-" + (totalBytesToDownload - 1) + "/" + actualFilesize;

				//TODO: Not logging anymore?
				/*Logging.LogInfoToFile(
					string.Format("Download duration for file '{0}' is {1:0.###} seconds ({2:0.###} kB/s) to download {3} bytes, range = {4}",
						localDestinationFilePath,
						durationDownloading.Elapsed.TotalSeconds,
						lastBytesPerSec / 1024D,
						totalBytesToDownload,
						rangeStringForLogging),
					Logging.ReportingFrequencies.Daily,
					"SharedClasses", "PhpDownloadingInterop");*/

				//if (onProgressPercentage != null)
				//    onProgressPercentage(100);

				if (!rangeToDownload.HasValue)//If we did not request a section only
				{
					//Only check MD5 sum if we are downloading the complete file
					string localMd5 = fullLocalDestinationPath.FileToMD5Hash();
					if (!localMd5.Equals(expectedMD5, StringComparison.InvariantCultureIgnoreCase))
					{
						actionOnMessage("File downloaded but MD5 checksum did not match", FeedbackMessageTypes.Error);
						return false;
					}
				}

				return true;
			}
			catch (WebException webexc)
			{
				var resp = webexc.Response;
				if (resp != null)//Received message from server
				{
					string serverResponseStringMessage = "";
					var stream = resp.GetResponseStream();
					int readcount = stream.Read(buffer, 0, buffer.Length);
					while (readcount > 0)
					{
						serverResponseStringMessage = Encoding.Default.GetString(buffer, 0, readcount);
						readcount = stream.Read(buffer, 0, buffer.Length);
					}
					actionOnMessage(serverResponseStringMessage, FeedbackMessageTypes.Error);
					return false;//Error on server side
				}
				else
				{
					actionOnMessage(webexc.Message, FeedbackMessageTypes.Error);
					return false;//Error on C# side
				}
			}
			catch (Exception exc)
			{
				actionOnMessage(exc.Message, FeedbackMessageTypes.Error);
				return false;
			}
			finally
			{
				ServicePointManager.ServerCertificateValidationCallback = null;
			}
		}

		public override bool? GetFileSize(string relativePath, out long outFileSize, Action<string, FeedbackMessageTypes> actionOnMessage = null, NetworkCredential credentials = null)
		{
			if (actionOnMessage == null) actionOnMessage = delegate { };

			//TODO: How to handle certificate validation??
			ServicePointManager.ServerCertificateValidationCallback += (snder, certif, chain, sslPolicyErrors)
				=> true;//=> actionOnSSLcertificateValidation(snder, certif, chain, sslPolicyErrors);
			try
			{
				using (WebClient client = new WebClient())
				{
					NameValueCollection inputs = new NameValueCollection();
					inputs.Add("task", "getfilesize");
					inputs.Add("relativepath", relativePath);
					var responseBytes = client.UploadValues(SettingsSimple.HomePcUrls.Instance.PhpUploadUrl, inputs);

					string responseString = System.Text.Encoding.UTF8.GetString(responseBytes, 0, responseBytes.Length);
					int tmpint;
					if (responseString.Equals("filenotfound", StringComparison.InvariantCultureIgnoreCase))
					{
						actionOnMessage("File does not exist online, relative path = " + relativePath, FeedbackMessageTypes.Error);
						outFileSize = 0;
						return null;
					}
					else if (!int.TryParse(responseString, out tmpint))
					{
						actionOnMessage("Response received but cannot parse to int, response = " + responseString, FeedbackMessageTypes.Error);
						outFileSize = 0;
						return false;
					}
					else
					{
						outFileSize = tmpint;
						return true;
					}
				}
			}
			catch (Exception exc)
			{
				actionOnMessage(exc.Message, FeedbackMessageTypes.Error);
				outFileSize = 0;
				return false;
			}
			finally
			{
				ServicePointManager.ServerCertificateValidationCallback = null;
			}
		}
	}
}
