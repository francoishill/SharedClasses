using System;
using System.Collections.Specialized;
using System.Net;
using System.IO;
using System.Text;
using System.Web;
using System.Windows;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace SharedClasses
{
	public static class PhpDownloadingInterop
	{
		//private const string PhpUrl = "https://localhost/downloadownapps.php";
		//private const string PhpUrl = "https://ftpviahttp.getmyip.com/downloadownapps.php";

		private const int cBufferSize = 1024 * 256;//8 KB
		private static byte[] buffer = new byte[cBufferSize];

		private static HttpWebRequest GetHttpWebRequest(String url, NameValueCollection nameValueCollection = null)
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

		/// <summary>
		/// true=success,false=error on C# side, null=error on server side
		/// </summary>
		/// <param name="downloadRelativePath">The relative path of the server directory, the root path is defined in the uploadownapps.php file</param>
		/// <param name="localDestinationFilePath"></param>
		/// <param name="rangeToDownload"></param>
		/// <param name="actionOnError"></param>
		/// <returns></returns>
		public static bool? PhpDownloadFile(string downloadRelativePath, string localDestinationFilePath, DownloadRange? rangeToDownload, out string errorIfFailed, Action<int, double> onProgress_Percentage_BytesPerSec, Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> actionOnSSLcertificateValidation)
		{
			if (onProgress_Percentage_BytesPerSec == null)
				onProgress_Percentage_BytesPerSec = delegate { };

			try
			{
				ServicePointManager.ServerCertificateValidationCallback += (snder, certif, chain, sslPolicyErrors) => actionOnSSLcertificateValidation(snder, certif, chain, sslPolicyErrors);
				NameValueCollection inputs = new NameValueCollection();
				inputs.Add("relativepath", downloadRelativePath);
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

				onProgress_Percentage_BytesPerSec(0, 0);

				Stopwatch durationDownloading = Stopwatch.StartNew();
				var stream = response.GetResponseStream();
				double lastBytesPerSec = 0;
				using (var fs = new FileStream(localDestinationFilePath, FileMode.Create))
				{
					long totalDownloadedBytes = 0;
					int readcount = stream.Read(buffer, 0, buffer.Length);
					while (readcount > 0)
					{
						totalDownloadedBytes += readcount;
						int downloadedPercentage = (int)Math.Truncate((double)100
								* (double)totalDownloadedBytes / (double)totalBytesToDownload);
						lastBytesPerSec = (double)totalDownloadedBytes / durationDownloading.Elapsed.TotalSeconds;
						onProgress_Percentage_BytesPerSec(downloadedPercentage, lastBytesPerSec);

						fs.Write(buffer, 0, readcount);
						fs.Flush();
						if (totalDownloadedBytes >= totalBytesToDownload)
							break;
						
						//Console.WriteLine("Total read = {0}, total to read = {1}, current read count = {2}", totalDownloadedBytes, totalBytesToDownload, readcount);
						readcount = stream.Read(buffer, 0, buffer.Length);
					}
				}
				durationDownloading.Stop();

				string rangeStringForLogging = "";
				if (rangeToDownload.HasValue)
					rangeStringForLogging = rangeToDownload.Value.FromBytePosition + "-"
						+ (rangeToDownload.Value.ToBytePosition.HasValue ? rangeToDownload.Value.ToBytePosition.Value : totalBytesToDownload - 1)
						+ "/" + actualFilesize;
				else
					rangeStringForLogging = "0-" + (totalBytesToDownload - 1) + "/" + actualFilesize;

				Logging.LogInfoToFile(
					string.Format("Download duration for file '{0}' is {1:0.###} seconds ({2:0.###} kB/s) to download {3} bytes, range = {4}",
						localDestinationFilePath,
						durationDownloading.Elapsed.TotalSeconds,
						lastBytesPerSec / 1024D,
						totalBytesToDownload,
						rangeStringForLogging),
					Logging.ReportingFrequencies.Daily,
					"SharedClasses", "PhpDownloadingInterop");

				//if (onProgressPercentage != null)
				//    onProgressPercentage(100);

				if (!rangeToDownload.HasValue)//If we did not request a section only
				{
					//Only check MD5 sum if we are downloading the complete file
					string localMd5 = localDestinationFilePath.FileToMD5Hash();
					if (!localMd5.Equals(expectedMD5, StringComparison.InvariantCultureIgnoreCase))
					{
						errorIfFailed = "File downloaded but MD5 checksum did not match";
						return null;
					}
				}

				errorIfFailed = null;
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
					errorIfFailed = serverResponseStringMessage;
					return null;//Error on server side
				}
				else
				{
					errorIfFailed = webexc.Message;
					return false;//Error on C# side
				}
			}
			catch (Exception exc)
			{
				errorIfFailed = exc.Message;
				return false;
			}
			finally
			{
				ServicePointManager.ServerCertificateValidationCallback = null;
			}
		}
	}
}