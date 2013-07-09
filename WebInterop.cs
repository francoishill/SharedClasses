using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace SharedClasses
{
	public sealed class WebInterop
	{
		/// <summary>
		/// Post data to php, maximum length of data is 8Mb
		/// </summary>
		/// <param name="url">The url of the php, do not include the ?</param>
		/// <param name="data">The data, i.e. "name=koos&surname=koekemoer". Note to not include the ?</param>
		/// <returns>Returns the data received from the php (usually the "echo" statements in the php.</returns>
		public static bool PostPHP(string url, string escapedData, out string ResponseOrError)
		{
			string vystup = "";
			try
			{
				if (null == escapedData)
					escapedData = "";
				escapedData = escapedData.Replace("+", "[|]");
				//Our postvars
				byte[] buffer = Encoding.ASCII.GetBytes(escapedData);
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
				if (!exc.Message.ToUpper().StartsWith("The remote name could not be resolved:".ToUpper()))
				{
					ResponseOrError = "Post php: " + exc.Message;
					return false;
				}
				else
				{
					ResponseOrError = "Post php remote name: " + exc.Message;
					return false;
				}
			}
			ResponseOrError = vystup;
			return true;
		}

		public static bool PostPHP(string url, Dictionary<string, string> data, out string ResponseOrError)
		{
			string tmpdata = "";
			foreach (var key in data.Keys)
				tmpdata +=
					(tmpdata.Length > 0 ? "&" : "")
					+ Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(data[key]);
			return PostPHP(url, tmpdata, out ResponseOrError);
		}

		public const string cErrorIfNotFoundOnline = "Value does not exist online";
		public const string NORESULT_STRING = "[NO RESULT]";
		public const string rootUrl = "http://fjh.dyndns.org";

		public enum OnlineOperations { GetModifiedTime, GetValue, SetValue };

		public static string GetOperationUri(OnlineOperations onlineOperation)
		{
			switch (onlineOperation)
			{
				case OnlineOperations.GetModifiedTime:
					return rootUrl + "/json/getdatemodified";
				case OnlineOperations.GetValue:
					return rootUrl + "/json/getvalue";
				case OnlineOperations.SetValue:
					return rootUrl + "/json/setvalue";
				default:
					return "";
			}
		}

		public static bool GetModifiedTimeFromOnline(string category, string name, out DateTime ModifiedTimeOut, out string errorStringIfFail)
		{
			JSON.Instance.SerializeNullValues = true;
			JSON.Instance.ShowReadOnlyProperties = true;
			JSON.Instance.UseUTCDateTime = true;
			JSON.Instance.UsingGlobalTypes = false;

			string response;
			if (WebInterop.PostPHP(
				GetOperationUri(OnlineOperations.GetModifiedTime),
				new Dictionary<string, string>() { { "category", category }, { "name", name } },
				out response))
			{
				if (response.Equals(NORESULT_STRING, StringComparison.InvariantCultureIgnoreCase))
				{
					errorStringIfFail = cErrorIfNotFoundOnline;
					ModifiedTimeOut = DateTime.MinValue;
					return false;
				}
				else
				{
					DateTime dt;
					if (DateTime.TryParseExact(response, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
					{
						ModifiedTimeOut = dt;
						errorStringIfFail = null;
						return true;
					}
					else
					{
						ModifiedTimeOut = DateTime.MinValue;
						errorStringIfFail = "Cannot convert response to double: " + response;
						return false;
					}
				}
			}
			else
			{
				ModifiedTimeOut = DateTime.MinValue;
				errorStringIfFail = response;
				return false;
			}
		}

		public static bool PopulateObjectFromOnline(string category, string name, object objectToPopulate, out string errorStringIfFail)
		{
			JSON.Instance.SerializeNullValues = true;
			JSON.Instance.ShowReadOnlyProperties = true;
			JSON.Instance.UseUTCDateTime = true;
			JSON.Instance.UsingGlobalTypes = false;

			string response;
			if (WebInterop.PostPHP(
				GetOperationUri(OnlineOperations.GetValue),//rootUrl + "/json/getvaluepretty",
				new Dictionary<string, string>() { { "category", category }, { "name", name } },
				out response))
			{
				if (response.Equals(NORESULT_STRING, StringComparison.InvariantCultureIgnoreCase))
				{
					errorStringIfFail = cErrorIfNotFoundOnline;
					return false;
				}
				else
				{
					errorStringIfFail = null;
					JSON.Instance.FillObject(objectToPopulate, response);
					//objectToPopulate = JSON.Instance.ToObject<Settings>(response);
					return true;
				}
			}
			else
			{
				errorStringIfFail = response;
				return false;
			}
		}

		public static string GetJsonStringFromObject(object obj, bool Beautify)
		{
			string tmpJson = JSON.Instance.ToJSON(obj, false);
			if (Beautify)
				tmpJson = JSON.Instance.Beautify(tmpJson);
			return tmpJson;
		}

		public static bool SaveObjectOnline(string category, string name, object obj, out string errorStringIfFailElseJsonString)
		{
			JSON.Instance.SerializeNullValues = true;
			JSON.Instance.ShowReadOnlyProperties = true;
			JSON.Instance.UseUTCDateTime = true;
			JSON.Instance.UsingGlobalTypes = false;

			string response;
			//string newvalue = JSON.Instance.Beautify(JSON.Instance.ToJSON(obj, false));
			string newvalue = GetJsonStringFromObject(obj, false);
			if (WebInterop.PostPHP(
				GetOperationUri(OnlineOperations.SetValue),
				new Dictionary<string, string>() { { "category", category }, { "name", name }, { "jsonstring", newvalue } },
				out response))
			{
				if (response.Equals("1") || response.Equals("0"))
				{
					errorStringIfFailElseJsonString = newvalue;
					return true;
				}
				else
				{
					errorStringIfFailElseJsonString = response;
					return false;
				}
			}
			else
			{
				errorStringIfFailElseJsonString = response;
				return false;
			}
		}
	}
}