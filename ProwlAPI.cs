using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Text;

namespace SharedClasses
{
	public static class ProwlAPI
	{
		public static string DefaultApiKey = "6fa888aaf5f801edd5520fb1e7996447beb414dd";

		public enum Priority { VeryLow, Moderate, Normal, High, Emergency };

		private static int PriorityToInt(Priority priority)
		{
			switch (priority)
			{
				case Priority.VeryLow:
					return -2;
				case Priority.Moderate:
					return -1;
				case Priority.Normal:
					return 0;
				case Priority.High:
					return 1;
				case Priority.Emergency:
					return 2;
				default:
					return 0;
			}
		}

#if NET40
		private static int GetInt(this Priority priority)
		{
			return PriorityToInt(priority);
		}
//#elif NET35
//#else NET20
		//
#endif

		//public static void SendProwlNow(string apiKey, string applicationName, string Event, string description, Priority priority, string callbackUrl = "")
		//{
		//	string baseUrl = "https://prowl.weks.net/publicapi/add";
		//	string urlData = "apikey=" + apiKey;
		//	urlData += "&application=" + applicationName;
		//	urlData += "&event=" + Event;
		//	urlData += "&description=" + description;
		//	urlData += "&priority=" + PriorityToInt(priority);
		//	if (!string.IsNullOrEmpty(callbackUrl))
		//		urlData += "&url=" + callbackUrl;

		//	ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
		//	{
		//		PostPHP(baseUrl, urlData);
		//	});
		//}

		private static List<string> ListOfSentNotificationsWaitingForResponse = new List<string>();
		public static void SendNotificationUntilResponseFromiDevice(string apiKey, string uniqueName, TimeSpan NotificationInterval, Priority priority = Priority.Emergency)
		{
			//{
			//string tempDir = Path.GetTempPath().TrimEnd('\\') + "\\" + "WaitingProwlNotifications";
			//if (!Directory.Exists(tempDir))
			//	Directory.CreateDirectory(tempDir);
			//string tmpFilename = tempDir + "\\" + uniqueName;
			//foreach (char invalidchar in Path.GetInvalidPathChars())
			//	tmpFilename = tmpFilename.Replace(invalidchar.ToString(), "");

			//if (!ListOfSentNotificationsWaitingForResponse.Contains(tmpFilename))
			if (!ListOfSentNotificationsWaitingForResponse.Contains(uniqueName))
			{
				//ListOfSentNotificationsWaitingForResponse.Add(tmpFilename);
				ListOfSentNotificationsWaitingForResponse.Add(uniqueName);
				//Add support here for checking if was successful
				string createdWaitResponse = PostPHP("http://fjh.dyndns.org/csharp/createwaitresponse/" + uniqueName, "");
				//if (!File.Exists(tmpFilename))
				//	File.Create(tmpFilename).Close();

				ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				{
					DateTime startTime = DateTime.Now;
					bool response = true;
					//while (File.Exists(tmpFilename))
					while (response)
					{
						//string tickCheckPath = tmpFilename;
						//SendProwlNow(
						//	apiKey,
						//	"WaitForResponse",
						//	"Waiting",
						//	"Waiting for response for " + uniqueName + "C# ProwlAPI.cs, sent time: " + DateTime.Now.ToLongTimeString(),
						//	priority,
						//	"http://fjh.dyndns.org/csharp/acknowledgewaitresponse/" + Path.GetFileName(tickCheckPath));

						response =
							PostPHP("http://fjh.dyndns.org/csharp/sendprowlnotification/" + uniqueName, "").Trim()
							== "1";
						while (DateTime.Now.Subtract(startTime).TotalMilliseconds < NotificationInterval.TotalMilliseconds)
							Application.DoEvents();
						startTime = DateTime.Now;
					}

					//ListOfSentNotificationsWaitingForResponse.Remove(tmpFilename);
					ListOfSentNotificationsWaitingForResponse.Remove(uniqueName);
				},
				false);
			}
		}

		/// <summary>
		/// Post data to php, maximum length of data is 8Mb
		/// </summary>
		/// <param name="url">The url of the php, do not include the ?</param>
		/// <param name="data">The data, i.e. "name=koos&surname=koekemoer". Note to not include the ?</param>
		/// <returns>Returns the data received from the php (usually the "echo" statements in the php.</returns>
		public static string PostPHP(string url, string data)
		{
			string vystup = "";
			try
			{
				data = data.Replace("+", "[|]");
				//Our postvars
				byte[] buffer = Encoding.ASCII.GetBytes(data);
				//Initialisation, we use localhost, change if appliable
				HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(url);
				//Our method is post, otherwise the buffer (postvars) would be useless
				WebReq.Method = "POST";
				//We use form contentType, for the postvars.
				WebReq.ContentType = "application/x-www-form-urlencoded";
				//The length of the buffer (postvars) is used as contentlength.
				WebReq.ContentLength = buffer.Length;
				//We open a stream for writing the postvars
				Stream PostData = WebReq.GetRequestStream();
				//Now we write, and afterwards, we close. Closing is always important!
				PostData.Write(buffer, 0, buffer.Length);
				PostData.Close();
				//Get the response handle, we have no true response yet!
				HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();
				//Let's show some information about the response
				//System.Windows.Forms.MessageBox.Show(WebResp.StatusCode.ToString());
				//System.Windows.Forms.MessageBox.Show(WebResp.Server);

				//Now, we read the response (the string), and output it.
				Stream Answer = WebResp.GetResponseStream();
				StreamReader _Answer = new StreamReader(Answer);
				vystup = _Answer.ReadToEnd();

				//Congratulations, you just requested your first POST page, you
				//can now start logging into most login forms, with your application
				//Or other examples.
				string tmpresult = vystup.Trim() + "\n";
			}
			catch (Exception exc)
			{
				UserMessages.ShowWarningMessage("Unable to do post php query: " + exc.Message);
				//if (!exc.Message.ToUpper().StartsWith("The remote name could not be resolved:".ToUpper()))
				//	//LoggingClass.AddToLogList(UserMessages.MessageTypes.PostPHP, exc.Message);
				//	appendLogTextbox("Post php: " + exc.Message);
				//else //LoggingClass.AddToLogList(UserMessages.MessageTypes.PostPHPremotename, exc.Message);
				//	appendLogTextbox("Post php remote name: " + exc.Message);
				//SysWinForms.MessageBox.Show("Error (092892): " + Environment.NewLine + exc.Message, "Exception error", SysWinForms.MessageBoxButtons.OK, SysWinForms.MessageBoxIcon.Error);
			}
			return vystup;
		}
	}
}