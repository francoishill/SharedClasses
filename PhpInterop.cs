using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Net;
using System.IO;
using System.Windows.Forms;

public class PhpInterop
{
	//private const string ServerAddress = "http://localhost";
	//private const string ServerAddress = "https://fjh.co.za";
	public static readonly string ServerAddress = "http://firepuma.com";
	public static readonly string doWorkAddress = ServerAddress + "/desktopapp";
	public static readonly string Username = "f";
	public static readonly string Password = "f";

	public const string MySQLdateformat = "yyyy-MM-dd HH:mm:ss";

	public static string GetPrivateKey(Object textfeedbackSenderObject, string ServerAddress, string Username, string Password, TextFeedbackEventHandler textFeedbackEvent = null)
	{
		try
		{
			//toolStripStatusLabelCurrentStatus.Text = "Obtaining pvt key...";
			string tmpkey = null;

			ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			{
				tmpkey = PhpInterop.PostPHP(textfeedbackSenderObject, ServerAddress + "/generateprivatekey.php", "username=" + Username + "&password=" + Password, textFeedbackEvent);
			});

			string tmpSuccessKeyString = "Success: Key=";
			if (tmpkey != null && tmpkey.Length > 0 && tmpkey.ToUpper().StartsWith(tmpSuccessKeyString.ToUpper()))
			{
				tmpkey = tmpkey.Substring(tmpSuccessKeyString.Length).Replace("\n", "").Replace("\r", "");
				//toolStripStatusLabelCurrentStatus.Text = tmpkey;
			}
			return tmpkey;
		}
		catch (Exception exc)
		{
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Obtain private key exception: " + exc.Message);
			//TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Obtain private key exception: " + exc.Message);
			return null;
		}
	}

	/// <summary>
	/// Post data to php, maximum length of data is 8Mb
	/// </summary>
	/// <param name="url">The url of the php, do not include the ?</param>
	/// <param name="data">The data, i.e. "name=koos&surname=koekemoer". Note to not include the ?</param>
	/// <returns>Returns the data received from the php (usually the "echo" statements in the php.</returns>
	public static string PostPHP(Object textfeedbackSenderObject, string url, string data, TextFeedbackEventHandler textFeedbackEvent = null)
	{
		string vystup = "";
		ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
		{
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
				if (!exc.Message.ToUpper().StartsWith("The remote name could not be resolved:".ToUpper()))
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Post php: " + exc.Message);
				//LoggingClass.AddToLogList(UserMessages.MessageTypes.PostPHP, exc.Message);
				//TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Post php: " + exc.Message);
				else //LoggingClass.AddToLogList(UserMessages.MessageTypes.PostPHPremotename, exc.Message);
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Post php remote name: " + exc.Message);
					//TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Post php remote name: " + exc.Message);
				//SysWinForms.MessageBox.Show("Error (092892): " + Environment.NewLine + exc.Message, "Exception error", SysWinForms.MessageBoxButtons.OK, SysWinForms.MessageBoxIcon.Error);
			}
		});
		return vystup;
	}

	public static void AddTodoItemFirepuma(Object textfeedbackSenderObject, string ServerAddress, string doWorkAddress, string Username, string Password, string Category, string Subcat, string Items, string Description, bool Completed, DateTime Due, DateTime Created, int RemindedCount, bool StopSnooze, int AutosnoozeInterval, TextFeedbackEventHandler textFeedbackEvent = null)
	{
		TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Adding new item, please wait...");
		//TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Adding new item, please wait...");
		bool successfulAdd = PerformDesktopAppDoTask(
			textfeedbackSenderObject,
			ServerAddress,
			doWorkAddress,
			Username,
			Password,
			"addtolist",
			new List<string>()
              {
                  Category,
                  Subcat,
                  Items,
                  Description,
                  Due.ToString(MySQLdateformat),
                  StopSnooze ? "1" : "0",
                  AutosnoozeInterval.ToString()
              },
			true,
			"1",
			false,
			textFeedbackEvent);
		if (successfulAdd)
		{
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Successfully added todo item.");
			//TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Successfully added todo item.");
			//textBox1.Text = "";
		}
	}

	public static bool AddBtwTextFirepuma(Object textfeedbackSenderObject, string btwtext, TextFeedbackEventHandler textFeedbackEvent)
	{
		TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Sending btw text, please wait...");
		string responsestr  = PhpInterop.PostPHP(textfeedbackSenderObject, "http://firepuma.com/btw/directadd/f/" + PhpInterop.PhpEncryption.StringToHex(btwtext), "");

		TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, responsestr);
		return responsestr.ToLower().StartsWith("success:");
		//form1.textBox1.Text = "";
		//textBox1.Text = "";
	}

	private static bool PerformDesktopAppDoTask(Object textfeedbackSenderObject, string ServerAddress, string doWorkAddress, string UsernameIn, string Password, string TaskName, List<string> ArgumentList, bool CheckForSpecificResult = false, string SuccessSpecificResult = "", bool MustWriteResultToLogsTextbox = false, TextFeedbackEventHandler textFeedbackEvent = null)
	{
		string result = GetResultOfPerformingDesktopAppDoTask(textfeedbackSenderObject, ServerAddress, doWorkAddress, UsernameIn, Password, TaskName, ArgumentList, MustWriteResultToLogsTextbox, textFeedbackEvent);
		if (CheckForSpecificResult && result == SuccessSpecificResult)
			return true;
		return false;
	}

	private static string GetResultOfPerformingDesktopAppDoTask(Object textfeedbackSenderObject, string ServerAddress, string doWorkAddress, string Username, string Password, string TaskName, List<string> ArgumentList, bool MustWriteResultToLogsTextbox = false, TextFeedbackEventHandler textFeedbackEvent = null)
	{
		string tmpkey = GetPrivateKey(textfeedbackSenderObject, ServerAddress, Username, Password, textFeedbackEvent);
		TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Obtained private key");

		if (tmpkey != null)
		{
			HttpWebRequest addrequest = null;
			HttpWebResponse addresponse = null;
			StreamReader input = null;

			try
			{
				if (Username != null && Username.Length > 0
															 && tmpkey != null && tmpkey.Length > 0)
				{
					string encryptedstring;
					string decryptedstring = "";
					bool mustreturn = false;
					ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
					{
						string ArgumentListTabSeperated = "";
						foreach (string s in ArgumentList)
							ArgumentListTabSeperated += (ArgumentListTabSeperated.Length > 0 ? "\buildTask" : "") + s;

						string tmpRequest = doWorkAddress + "/dotask/" +
								PhpInterop.PhpEncryption.SimpleTripleDesEncrypt(Username, "123456789abcdefghijklmno") + "/" +
								PhpInterop.PhpEncryption.SimpleTripleDesEncrypt(TaskName, tmpkey) + "/" +
								PhpInterop.PhpEncryption.SimpleTripleDesEncrypt(ArgumentListTabSeperated, tmpkey);
						addrequest = (HttpWebRequest)WebRequest.Create(tmpRequest);// + "/");
						//appendLogTextbox(addrequest.RequestUri.ToString());
						try
						{
							addresponse = (HttpWebResponse)addrequest.GetResponse();
							input = new StreamReader(addresponse.GetResponseStream());
							encryptedstring = input.ReadToEnd();
							//appendLogTextbox("Encrypted response: " + encryptedstring);

							decryptedstring = PhpInterop.PhpEncryption.SimpleTripleDesDecrypt(encryptedstring, tmpkey);
							//appendLogTextbox("Decrypted response: " + decryptedstring);
							decryptedstring = decryptedstring.Replace("\0", "").Trim();
							//MessageBox.Show(this, decryptedstring);
							if (MustWriteResultToLogsTextbox) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Result for " + TaskName + ": " + decryptedstring);
							mustreturn = true;
						}
						catch (Exception exc) { MessageBox.Show("Exception:" + exc.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error); }
					});
					if (mustreturn) return decryptedstring;
				}
			}
			catch (Exception exc)
			{
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Obtain php: " + exc.Message);
			}
			finally
			{
				if (addresponse != null) addresponse.Close();
				if (input != null) input.Close();
			}
		}
		return null;
	}

	public class PhpEncryption
	{
		public static string SimpleTripleDesEncrypt(string Data, string keystring)
		{
			byte[] key = Encoding.ASCII.GetBytes(keystring);
			byte[] iv = Encoding.ASCII.GetBytes("password");
			byte[] data = Encoding.ASCII.GetBytes(Data);
			byte[] enc = new byte[0];
			TripleDES tdes = TripleDES.Create();
			tdes.IV = iv;
			tdes.Key = key;
			tdes.Mode = CipherMode.CBC;
			tdes.Padding = PaddingMode.Zeros;
			ICryptoTransform ict = tdes.CreateEncryptor();
			enc = ict.TransformFinalBlock(data, 0, data.Length);
			return ByteArrayToString(enc);
		}

		public static string SimpleTripleDesDecrypt(string Data, string keystring)
		{
			byte[] key = Encoding.ASCII.GetBytes(keystring);
			byte[] iv = Encoding.ASCII.GetBytes("password");
			byte[] data = StringToByteArray(Data);
			byte[] enc = new byte[0];
			TripleDES tdes = TripleDES.Create();
			tdes.IV = iv;
			tdes.Key = key;
			tdes.Mode = CipherMode.CBC;
			tdes.Padding = PaddingMode.Zeros;
			ICryptoTransform ict = tdes.CreateDecryptor();
			enc = ict.TransformFinalBlock(data, 0, data.Length);
			return Encoding.ASCII.GetString(enc);
		}

		public static string ByteArrayToString(byte[] ba)
		{
			string hex = BitConverter.ToString(ba);
			return hex.Replace("-", "");
		}

		public static string StringToHex(string stringIn)
		{
			return PhpEncryption.ByteArrayToString(Encoding.Default.GetBytes(stringIn));
		}

		public static byte[] StringToByteArray(String hex)
		{
			int NumberChars = hex.Length;
			byte[] bytes = new byte[NumberChars / 2];
			for (int i = 0; i < NumberChars; i += 2)
				bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			return bytes;
		}

		public static string HexToString(string hexIn)
		{
			return Encoding.Default.GetString(StringToByteArray(hexIn));
		}
	}
}