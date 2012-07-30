//================================================ C# code sample ================================================//

/* We use the HttpUtility class from the System.Web namespace
*
* If you see of the error "'HttpUtility' is not declared", you are probably
* using a newer version of Visual Studio. You need to navigate to
* Project | <Project name> Properties | Application,
* and select e.g. ".NET Framework 4" instead of ".NET Framework 4 Client Profile" as your 'Target framework'.
*
* Next, visit Project | Add reference, and select "System.Web" (specifically
* System.Web - not System.Web.<something>).
*/

using System.Net;
using System;
using System.Web;
using System.Text;
using System.IO;
using System.Collections;

namespace SMS {
	public class SendSMS {

		public static void SendMessageAndRetryIfFail(string username, string password, string number, string message ) {
			/*
			* Simply substitute your own username, password and phone number
			* below, and run the test code:
			*/
			//string username = "username";
			//string password = "password";
			/*
			* Your phone number, including country code, i.e. +44123123123 in this case:
			*/
			string msisdn   = number;//"447123123123";

			string url = "http://bulksms.2way.co.za:5567/eapi/submission/send_sms/2/2.0";
			
			/*
			* A 7-bit GSM SMS message can contain up to 160 characters (longer messages can be
			* achieved using concatenation).
			*
			* All non-alphanumeric 7-bit GSM characters are included in this example. Note that Greek characters,
			* and extended GSM characters (e.g. the caret "^"), may not be supported
			* to all networks. Please let us know if you require support for any characters that
			* do not appear to work to your network.
			*/
			string seven_bit_msg = message;//"Test message: all non-alphanumeric GSM characters: $@!#%&\",;:<>¡£¤¥§¿ÄÅÆÇÉÑÖØÜßàèéùìòå¿äöñüà\nGreek: ΩΘΔΦΓΛΩΠΨΣΘΞ";


			/*
			* A Unicode SMS can contain only 70 characters. Any Unicode character can be sent,
			* including those GSM and accented ISO-8859 European characters that are not
			* catered for by the GSM character set, but note that mobile phones are only able
			* to display certain Unicode characters, based on their geographic market.
			* Nonetheless, every mobile phone should be able to display at least the text
			* "Unicode test message:" from the test message below. Your browser may be able to
			* display more of the characters than your phone. The text of the message below is:
			*/
			//string unicode_msg = "Unicode test message: ☺ \nArabic: شصض\nChinese: 本网";

			/*
			* There's a number of 8 bit messages that one can send to a handset, the most popular of the lot is Service Indication(Wap Push).
			* Some other popular ones are vCards and vCalendars, headers may vary depending on which of message one opts to send.
			* The User Data Header(UDH) is solely responsible for determining which
			* type of messages will be sent to ones' handset. 
			* In a nutshell, SI(service indication) messages will have, in the final message body, both the UDH
			* and WSP(Wireless Session Protocol) appended to each other forming a prefix of the ASCII string of the Hex value
			* of the actual content. For example, suppose our intended Wap message body is as follows:
			*
			* <si><indication href="http://www.bulksms.com">Visit BulkSMS.com homepage</indication></si>
			*
			* Our headers will be -- UDH + WSP = FINAL_HEADER
			* '0605040B8423F0' + 'DC0601AE' = '0605040B8423F0DC0601AE'
			*
			* The UDH contains a destination port(0B84) and the origin port(23F0)
			* the WSP is broken down into the following:
			*
			* DC - Transaction ID (used to associate PDUs)
			* 06 - PDU type (push PDU)
			* 01 - HeadersLen (total of content-type and headers, i.e. zero headers)
			* AE - Content Type: application/vnd.wap.sic
			*
			* For this example our 8 bit message(what becomes our Wap Push) will look like this:
			* $msg = '0605040B8423F0DC0601AE02056a0045c60d0362756c6b736d732e636f6d00010356697369742042756c6b534d532e636f6d20686f6d6570616765000101';
			*
			* You will only get UDH for both vCard and vCalendar type of 8 bit messages and no WSP, which will look something to this effect:
			* '06050423F4000'
			*
			* Now moving on and considering the example above, we will need to determine the type of message and get the header(s)
			* there of.
			*
			* string msg_type = "wap_push";
			* string msg_body = "<si><indication href=\"http://www.bulksms.com\">Visit BulkSMS.com homepage</indication></si>";
			* string eight_bit_msg = get_headers( msg_type ) + xml_to_wbxml( msg_body );
			*/
			//string eight_bit_msg = "0605040B8423F0DC0601AE02056a0045c60d0362756c6b736d732e636f6d00010356697369742042756c6b534d532e636f6d20686f6d6570616765000101";

			Hashtable result;

			/*
			* Upon a transient (retryable) error, sleep this many seconds:
			*/
			int sleep_time = 3;
			/*
			* The time it takes to retry transient errors will increase with
			* each retry attempt:
			*/
			int retry_growth_factor = 8;
			int num_retries = 5;

			string data = seven_bit_message( username, password, msisdn, seven_bit_msg );
			
			for ( int x = 0; x < num_retries; x++ ) {
				result = send_sms( data, url );
				if ((int)result["success"] == 1 ) {
					AddLogMessage(formatted_server_response(result));
					//Console.WriteLine( formatted_server_response( result ) );
					break;
				}
				else {
					AddLogMessage(formatted_server_response(result));
					//Console.WriteLine( formatted_server_response( result ) );
				}

				System.Threading.Thread.Sleep(sleep_time);
				sleep_time *= retry_growth_factor;
			}

			/*sleep_time = 3;
			data = eight_bit_message( username, password, msisdn, eight_bit_msg );
			for ( int x = 0; x < num_retries; x++ ) {
				result = send_sms( data, url );
				if ((int)result["success"] == 1 ) {
					Console.WriteLine( formatted_server_response( result ) );
					break;
				}
				else {
					Console.WriteLine( formatted_server_response( result ) );
				}

				System.Threading.Thread.Sleep(sleep_time);
				sleep_time *= retry_growth_factor;
			}*/
			
			/*sleep_time = 3;
			data = unicode_message( username, password, msisdn, unicode_msg );
			for ( int x = 0; x < num_retries; x++ ) {
				result = send_sms( data, url );
				if ((int)result["success"] == 1 ) {
					Console.WriteLine( formatted_server_response( result ) );
					break;
				}
				else {
					Console.WriteLine( formatted_server_response( result ) );
				}

				System.Threading.Thread.Sleep(sleep_time);
				sleep_time *= retry_growth_factor;
			}*/
		}

		private static void AddLogMessage(string message)
		{
			message = "[" + DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss") + "] " + message;
			string filepath = @"c:\_BulkSmsLogs.txt";
			if (!File.Exists(filepath))
				message = Environment.NewLine + message;
			File.AppendAllText(filepath, message);
		}

		public static string formatted_server_response( Hashtable result ) {
			string ret_string = "";
			if( (int)result["success"] == 1 ) {
				ret_string += "Success: batch ID " + (string)result["api_batch_id"] + "API message: " + (string)result["api_message"] + "\nFull details " + (string)result["details"];
			}
			else {
				ret_string += "Fatal error: HTTP status " + (string)result["http_status_code"] + " API status " + (string)result["api_status_code"] + " API message " + (string)result["api_message"] + "\nFull details " + (string)result["details"];
			}

			return ret_string;
		}

		public static Hashtable send_sms( string data, string url ) {
			string sms_result = Post(url, data);
			
			Hashtable result_hash = new Hashtable();

			string tmp = "";
			tmp += "Response from server: " + sms_result + "\n"; 
			string[] parts = sms_result.Split('|');

			string statusCode = parts[0];
			string statusString = parts[1];

			result_hash.Add("api_status_code", statusCode);
			result_hash.Add("api_message", statusString);

			if(parts.Length != 3) {
				tmp += "Error: could not parse valid return data from server.\n"; 
			}
			else {
				if ( statusCode.Equals("0") ) {
					result_hash.Add("success", 1);
					result_hash.Add("api_batch_id", parts[2]);
					tmp += "Message sent - batch ID " + parts[2] + "\n";
				}
				else if (statusCode.Equals("1")) {
					// Success: scheduled for later sending.
					result_hash.Add("success", 1);
					result_hash.Add("api_batch_id", parts[2]);
				}
				else {
					result_hash.Add("success", 0);
					tmp += "Error sending: status code " + parts[0] + " description: " + parts[1] + "\n";
				}
			}
			result_hash.Add("details", tmp);
			return result_hash;
		}

		public static string Post(string url, string data) {

			string result = null;
			try {
				byte[] buffer = Encoding.Default.GetBytes(data);

				HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(url);
				WebReq.Method = "POST";
				WebReq.ContentType = "application/x-www-form-urlencoded";
				WebReq.ContentLength = buffer.Length;
				Stream PostData = WebReq.GetRequestStream();

				PostData.Write(buffer, 0, buffer.Length);
				PostData.Close();
				HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();
				Console.WriteLine(WebResp.StatusCode);

				Stream Response = WebResp.GetResponseStream();
				StreamReader _Response = new StreamReader(Response);
				result =  _Response.ReadToEnd();
			}
			catch (Exception ex) {
				Console.WriteLine(ex.Message);
			}
			return result.Trim()+"\n";
		}

		public static string character_resolve(string body) {
			Hashtable chrs = new Hashtable();
			chrs.Add('Ω', "Û");
			chrs.Add('Θ', "Ô");
			chrs.Add('Δ', "Ð");
			chrs.Add('Φ', "Þ");
			chrs.Add('Γ', "¬");
			chrs.Add('Λ', "Â");
			chrs.Add('Π', "º");
			chrs.Add('Ψ', "Ý");
			chrs.Add('Σ', "Ê");
			chrs.Add('Ξ', "±");

			string ret_str = "";
			foreach ( char c in body ) {
				if( chrs.ContainsKey( c ) ) {
					ret_str += chrs[ c ];
				}
				else {
					ret_str += c;
				}
			}
			return ret_str;
		}

		public static string seven_bit_message( string username, string password, string msisdn, string message ) {
			
			/********************************************************************
			* Construct data                                                    *
			*********************************************************************/
			/*
			* Note the suggested encoding for the some parameters, notably
			* the username, password and especially the message.  ISO-8859-1
			* is essentially the character set that we use for message bodies,
			* with a few exceptions for e.g. Greek characters. For a full list,
			* see: http://bulksms.vsms.net/docs/eapi/submission/character_encoding/
			*/

			string data = "";
			data += "username="  + HttpUtility.UrlEncode(username, System.Text.Encoding.GetEncoding("ISO-8859-1"));
			data += "&password=" + HttpUtility.UrlEncode(password, System.Text.Encoding.GetEncoding("ISO-8859-1"));
			data += "&message="  + HttpUtility.UrlEncode(character_resolve(message), System.Text.Encoding.GetEncoding("ISO-8859-1"));
			data += "&msisdn=" + msisdn;
			data += "&want_report=1";

			return data;
		}

		public static string unicode_message( string username, string password, string msisdn, string message ) {
			
			/********************************************************************
			* Construct data                                                    *
			*********************************************************************/
			/*
			* Note the suggested encoding for the some parameters, notably
			* the username, password and especially the message.  ISO-8859-1
			* is essentially the character set that we use for message bodies,
			* with a few exceptions for e.g. Greek characters. For a full list,
			* see: http://bulksms.vsms.net/docs/eapi/submission/character_encoding/
			*/
			
			
			string data = "";
			data += "username="  + HttpUtility.UrlEncode(username, System.Text.Encoding.GetEncoding("ISO-8859-1"));
			data += "&password=" + HttpUtility.UrlEncode(password, System.Text.Encoding.GetEncoding("ISO-8859-1"));
			data += "&message="  + stringToHex( message );
			data += "&msisdn=" + msisdn;
			data += "&dca=16bit";
			data += "&want_report=1";

			return data;
		}

		public static string eight_bit_message( string username, string password, string msisdn, string message ) {
			
			/********************************************************************
			* Construct data                                                    *
			*********************************************************************/
			/*
			* Note the suggested encoding for the some parameters, notably
			* the username, password and especially the message.  ISO-8859-1
			* is essentially the character set that we use for message bodies,
			* with a few exceptions for e.g. Greek characters. For a full list,
			* see: http://bulksms.vsms.net/docs/eapi/submission/character_encoding/
			*/
			
			/*
			* In the following $udh string, 0B84 is a destination port and 23F0 is the origin port.
			*/
			string udh = "0605040B8423F0";

			/*
			* $wsp_header is broken down into the following:
			*
			* DC - Transaction ID (used to associate PDUs)
			* 06 - PDU type (push PDU)
			* 01 - HeadersLen (total of content-type and headers, i.e. zero headers)
			* AE - Content Type: application/vnd.wap.sic 
			*/
			string wsp_header = "DC0601AE";
			string wap_push_message = udh + wsp_header + message;

			string data = "";
			data += "username="  + HttpUtility.UrlEncode(username, System.Text.Encoding.GetEncoding("ISO-8859-1"));
			data += "&password=" + HttpUtility.UrlEncode(password,  System.Text.Encoding.GetEncoding("ISO-8859-1"));
			data += "&message="  + wap_push_message;
			data += "&msisdn="   + msisdn;
			data += "&dca=8bit";
			data += "&want_report=1";

			return data;
		}
		
		public static string get_headers( string msg_type ) {
			string headers = "";
			if( msg_type == "wap_push" ) {
				string udh = "0605040B8423F0";
				string wsp = "DC0601AE";

				headers += udh + wsp;
			}
			else if( msg_type == "vCard" || msg_type == "vCalendar" ) {
				headers += "06050423F40000";
			}
			return headers;
		}

		public static string xml_to_string( string msg_body ) {
			//TODO
			/*
			* Code to convert 'msg_body' will go in here.
			*/

			return "conversion";
		}

		public static string stringToHex(string s){
			string hex = "";
			foreach (char c in s)
			{
				int tmp = c;
				hex += String.Format("{0:x4}", (uint)System.Convert.ToUInt32(tmp.ToString()));
			}
			return hex;
		}
	}
}



