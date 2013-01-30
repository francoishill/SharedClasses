using System;
using System.Net;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.Web;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.IO;

namespace SharedClasses
{
	public static class PhpUploadingInterop
	{
		//private const string phpUrl = "https://localhost/uploadownapps.php";
		//private const string phpUrl = "https://ftpviahttp.getmyip.com/uploadownapps.php";

		//public enum PhpActions { GetFileSize, UploadFile, CreateDirectory, CheckFileExists, DeleteFile };
		public static int? PhpGetFileSize(string relativePath, out string errorIfFailed, Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> actionOnSSLcertificateValidation)
		{
			int stillNeedsTimeoutSupportForAllFunctionsIn_PhpUploadingInterop;

			ServicePointManager.ServerCertificateValidationCallback += (snder, certif, chain, sslPolicyErrors) => actionOnSSLcertificateValidation(snder, certif, chain, sslPolicyErrors);
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
						errorIfFailed = "File does not exist online, relative path = " + relativePath;
						return null;
					}
					else if (!int.TryParse(responseString, out tmpint))
					{
						errorIfFailed = "Response received but cannot parse to int, response = " + responseString;
						return null;
					}
					else
					{
						errorIfFailed = null;
						return tmpint;
					}
				}
			}
			catch (Exception exc)
			{
				errorIfFailed = exc.Message;
				return null;
			}
			finally
			{
				ServicePointManager.ServerCertificateValidationCallback = null;
			}
		}

		/// <summary>
		/// //true=success, false=error, null=existedalready
		/// </summary>
		/// <param name="localFileToUpload"></param>
		/// <param name="relativePath"></param>
		/// <param name="errorIfFailed"></param>
		/// <param name="onProgress_Percentage_BytesPerSec"></param>
		/// <param name="MustCancel"></param>
		/// <param name="onCancelledCallback"></param>
		/// <param name="actionOnSSLcertificateValidation"></param>
		/// <returns></returns>
		public static bool? PhpUploadFile(string localFileToUpload, string relativePath, out string errorIfFailed, Action<int, double> onProgress_Percentage_BytesPerSec, ref bool MustCancel, Action onCancelledCallback, Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> actionOnSSLcertificateValidation)
		{
			//Check first if file exists, otherwise the file will be completely uploaded and then the
			//server will return saying it already existed (its just the way php works).
			string err;
			bool? existsResult = PhpCheckIfFileExists(relativePath, out err, actionOnSSLcertificateValidation);
			if (existsResult.HasValue && existsResult.Value == true)//Exists already
			{
				errorIfFailed = "File already existed online";
				return null;//Means existed already
			}
			else if (!existsResult.HasValue)
			{
				if (!err.Equals("directorynotfound"))
				{
					errorIfFailed = err;
					return false;
				}
				else
				{
					string createdirerr;
					bool? createdirResult = PhpCreateDirectory(relativePath.Substring(0, relativePath.Replace('\\', '/').LastIndexOf('/')), out createdirerr, actionOnSSLcertificateValidation);
					if (createdirResult == false)//Could not create (error)
					{
						errorIfFailed = createdirerr;
						return false;
					}
				}
			}

			if (onProgress_Percentage_BytesPerSec == null)
				onProgress_Percentage_BytesPerSec = delegate { };

			ServicePointManager.ServerCertificateValidationCallback += (snder, certif, chain, sslPolicyErrors) => actionOnSSLcertificateValidation(snder, certif, chain, sslPolicyErrors);

			bool oldExpect100Continue = System.Net.ServicePointManager.Expect100Continue;
			ServicePointManager.Expect100Continue = false;
			try
			{
				using (WebClient client = new WebClient())
				{
					client.Headers[HttpRequestHeader.ContentType] = "binary/octet-stream";
					client.Headers[HttpRequestHeader.ContentMd5] = localFileToUpload.FileToMD5Hash();

					//NameValueCollection inputs = new NameValueCollection();
					//inputs.Add("task", "getfilesize");
					//inputs.Add("relativepath", relativePath);

					Stopwatch stopwatchFromUploadStart = Stopwatch.StartNew();
					byte[] responseBytes = null;
					string ifFailedError = null;
					double lastBytesPerSec = 0;
					client.UploadFileCompleted += (sn, ev) =>
					{
						if (!ev.Cancelled)
						{
							if (ev.Error == null)
							{
								ifFailedError = null;
								responseBytes = ev.Result;
							}
							else
								ifFailedError = ev.Error.InnerException.Message;
						}
						stopwatchFromUploadStart.Stop();

						Logging.LogInfoToFile(
							string.Format("Upload duration for file '{0}' is {1:0.###} seconds ({2:0.###} kB/s) to upload {3} bytes",
								localFileToUpload,
								stopwatchFromUploadStart.Elapsed.TotalSeconds,
								lastBytesPerSec / 1024D,
								new FileInfo(localFileToUpload).Length),
							Logging.ReportingFrequencies.Daily,
							"SharedClasses", "PhpUploadingInterop");

						stopwatchFromUploadStart = null;
					};
					client.UploadProgressChanged += (sn, ev) =>
					{
						if (stopwatchFromUploadStart != null)
							lastBytesPerSec = (double)ev.BytesSent / (double)stopwatchFromUploadStart.Elapsed.TotalSeconds;
						onProgress_Percentage_BytesPerSec(
							(int)Math.Truncate((double)100 * (double)ev.BytesSent / (double)ev.TotalBytesToSend),
							lastBytesPerSec);
					};

					client.UploadFileAsync(
						new Uri(SettingsSimple.HomePcUrls.Instance.PhpUploadUrl.TrimEnd('/') + "?task=uploadfile&relativepath=" + HttpUtility.UrlEncode(relativePath)),
						"POST",
						localFileToUpload);

					while (responseBytes == null)
						if (MustCancel)
						{
							client.CancelAsync();
							break;
						}

					if (MustCancel)
					{
						if (onCancelledCallback != null)
							onCancelledCallback();
						errorIfFailed = "User cancelled upload";
						return false;
					}
					if (ifFailedError != null)
					{
						errorIfFailed = ifFailedError;
						return false;
					}

					string responseString = System.Text.Encoding.UTF8.GetString(responseBytes, 0, responseBytes.Length);
					//fileexisted OR directorynotfound OR fileuploaded OR Error:errorstring
					if (responseString.Equals("fileexisted", StringComparison.InvariantCultureIgnoreCase))
					{
						errorIfFailed = "File already existed online";
						return null;//Existed already
					}
					else if (responseString.Equals("directorynotfound", StringComparison.InvariantCultureIgnoreCase))
					{
						errorIfFailed = "Directory not found for uploading file, please create first: " + relativePath;
						return false;
					}
					else if (!responseString.Equals("fileuploaded", StringComparison.InvariantCultureIgnoreCase))
					{
						errorIfFailed = "Error uploading file: " + responseString;
						return false;
					}
					{
						errorIfFailed = null;
						return true;
					}
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

				ServicePointManager.Expect100Continue = oldExpect100Continue;
			}
		}

		/// <summary>
		/// true=successfully created,null=alreadyexisted,false=could not create
		/// </summary>
		/// <param name="relativePath"></param>
		/// <param name="errorIfFailed"></param>
		/// <param name="actionOnSSLcertificateValidation"></param>
		/// <returns></returns>
		public static bool? PhpCreateDirectory(string relativePath, out string errorIfFailed, Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> actionOnSSLcertificateValidation)
		{
			ServicePointManager.ServerCertificateValidationCallback += (snder, certif, chain, sslPolicyErrors) => actionOnSSLcertificateValidation(snder, certif, chain, sslPolicyErrors);
			try
			{
				using (WebClient client = new WebClient())
				{
					NameValueCollection inputs = new NameValueCollection();
					inputs.Add("task", "createdirectory");
					inputs.Add("relativepath", relativePath);
					var responseBytes = client.UploadValues(SettingsSimple.HomePcUrls.Instance.PhpUploadUrl, inputs);

					string responseString = System.Text.Encoding.UTF8.GetString(responseBytes, 0, responseBytes.Length);
					if (responseString.Equals("directoryexisted", StringComparison.InvariantCultureIgnoreCase))
					{
						errorIfFailed = "Directory already existed online: " + relativePath;
						return null;
					}
					else if (!responseString.Equals("directorycreated", StringComparison.InvariantCultureIgnoreCase))
					{
						errorIfFailed = "Error creating directory online: " + responseString;
						return false;
					}
					else
					{
						errorIfFailed = null;
						return true;
					}
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

		/// <summary>
		/// //true=exists,false=notexists,null=error
		/// </summary>
		/// <param name="relativePath"></param>
		/// <param name="errorIfFailed"></param>
		/// <param name="actionOnSSLcertificateValidation"></param>
		/// <returns></returns>
		public static bool? PhpCheckIfFileExists(string relativePath, out string errorIfFailed, Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> actionOnSSLcertificateValidation)
		{
			ServicePointManager.ServerCertificateValidationCallback += (snder, certif, chain, sslPolicyErrors) => actionOnSSLcertificateValidation(snder, certif, chain, sslPolicyErrors);
			try
			{
				using (WebClient client = new WebClient())
				{
					NameValueCollection inputs = new NameValueCollection();
					inputs.Add("task", "checkfileexists");
					inputs.Add("relativepath", relativePath);
					var responseBytes = client.UploadValues(SettingsSimple.HomePcUrls.Instance.PhpUploadUrl, inputs);

					string responseString = System.Text.Encoding.UTF8.GetString(responseBytes, 0, responseBytes.Length);
					if (responseString.Equals("filenotfound", StringComparison.InvariantCultureIgnoreCase))
					{
						errorIfFailed = null;
						return false;
					}
					else if (responseString.Equals("directorynotfound", StringComparison.InvariantCultureIgnoreCase))
					{
						errorIfFailed = "directorynotfound";
						return null;
					}
					else if (responseString.Equals("fileexists", StringComparison.InvariantCultureIgnoreCase))
					{
						errorIfFailed = null;
						return true;
					}
					else if (responseString.Equals("existsbutisdirectory", StringComparison.InvariantCultureIgnoreCase))
					{
						errorIfFailed = "Check if file exists, but path specified is an existing directory: " + relativePath;
						return null;
					}
					else
					{
						errorIfFailed = "Unknown response for 'checkfileexists': " + responseString;
						return null;
					}
				}
			}
			catch (Exception exc)
			{
				errorIfFailed = exc.Message;
				return null;
			}
			finally
			{
				ServicePointManager.ServerCertificateValidationCallback = null;
			}
		}

		/// <summary>
		/// true=deleted success,null=file did not exist,false=error
		/// </summary>
		/// <param name="relativePath"></param>
		/// <param name="errorIfFailed"></param>
		/// <returns></returns>
		public static bool? PhpDeleteFile(string relativePath, out string errorIfFailed, Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> actionOnSSLcertificateValidation)
		{
			ServicePointManager.ServerCertificateValidationCallback += (snder, certif, chain, sslPolicyErrors) => actionOnSSLcertificateValidation(snder, certif, chain, sslPolicyErrors);
			try
			{
				using (WebClient client = new WebClient())
				{
					NameValueCollection inputs = new NameValueCollection();
					inputs.Add("task", "deletefile");
					inputs.Add("relativepath", relativePath);
						var responseBytes = client.UploadValues(SettingsSimple.HomePcUrls.Instance.PhpUploadUrl, inputs);

						string responseString = System.Text.Encoding.UTF8.GetString(responseBytes, 0, responseBytes.Length);
						if (responseString.Equals("filenotfound", StringComparison.InvariantCultureIgnoreCase))
						{
							errorIfFailed = "File did not exist: " + relativePath;
							return null;
						}
						else if (responseString.Equals("existsbutisdirectory", StringComparison.InvariantCultureIgnoreCase))
						{
							errorIfFailed = "Could not delete file as it exists but is a directory: " + relativePath;
							return false;
						}
						else if (!responseString.Equals("filedeleted", StringComparison.InvariantCultureIgnoreCase))
						{
							errorIfFailed = "Could not delete file: " + responseString;
							return false;
						}
						else
						{
							errorIfFailed = null;
							return true;
						}
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