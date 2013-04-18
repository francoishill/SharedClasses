using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;

namespace SharedClasses
{
	public class WebAppsInterop
	{
		private const int cExpectedKeyLength = 24;
		private const int cExpectedSecretLength = 24;

		private Action<string> ActionOnError;
		private TextFeedbackEventHandler textfeedbackHandler = (sn, msg) => { UserMessages.ShowInfoMessage(msg.FeedbackType.ToString() + ": " + msg.FeedbackText); };

		//private const string rootUrl = "http://localhost";
		//private const string rootUrl = "http://localhost:8081";
		private const string rootUrl = "http://firepuma.com";

		private string AppName;
		private string AppKey;
		private string AppSecret;
		private string AccessToken;
		private string AccessSecret;

		public WebAppsInterop(string AppName, Action<string> ActionOnError, out bool somethingWentWrongInConcstructor)
		{
			somethingWentWrongInConcstructor = false;
			this.ActionOnError = ActionOnError;

			this.AppName = AppName;

			string appkey, appsecret;
			bool successGetAppkey = AskforAppkeyAndAppSecret(out appkey, out appsecret);
			if (successGetAppkey)
			{
				this.AppKey = appkey;
				this.AppSecret = appsecret;
			}
			else
			{
				somethingWentWrongInConcstructor = true;
				return;
			}

			if (!ObtainAccessTokenAndSecret())
			{
				somethingWentWrongInConcstructor = true;
				return;
			}
		}

		private bool ObtainAccessTokenAndSecret()
		{
			string accessToken, accessSecret;
			bool successGetAccessToken = GetAccessTokenAndSecret(out accessToken, out accessSecret);
			if (successGetAccessToken)
			{
				this.AccessToken = accessToken;
				this.AccessSecret = accessSecret;
				return true;
			}
			return false;
		}

		private string ConstructUrlToGetRequestToken()
		{
			string appkeyHex = EncodeAndDecodeInterop.EncodeStringHex(AppKey, ActionOnError);
			string appsecretEncryptedWithSelf = EncryptionInterop.SimpleTripleDesEncrypt(this.AppSecret, this.AppSecret)
				.ToLower();

			return string.Format("{0}/auth_apps/getrequesttoken/{1}/{2}",
				rootUrl, appkeyHex, appsecretEncryptedWithSelf);
		}

		private string ConstructAuthorizeUrl(string requestToken, string requestSecret, string callbackUrl)
		{
			string requestTokenHex = EncodeAndDecodeInterop.EncodeStringHex(requestToken, ActionOnError);
			string requestSecreteEncryptedWithSelf = EncryptionInterop.SimpleTripleDesEncrypt(requestSecret, requestSecret)
				.ToLower();
			string callbackUrlHex = EncodeAndDecodeInterop.EncodeStringHex(callbackUrl, ActionOnError);

			return string.Format("{0}/auth_apps/authorize/{1}/{2}/{3}",
				rootUrl, requestTokenHex, requestSecreteEncryptedWithSelf, callbackUrlHex);
		}

		private string ConstructUrlToConvertRequestTokenIntoAccessToken(string requestToken, string requestSecret)
		{
			string requestTokenHex = EncodeAndDecodeInterop.EncodeStringHex(requestToken, ActionOnError);
			string requestSecreteEncryptedWithSelf = EncryptionInterop.SimpleTripleDesEncrypt(requestSecret, requestSecret)
				.ToLower();

			return string.Format("{0}/auth_apps/getaccesstokenfromrequesttoken/{1}/{2}",
				rootUrl, requestTokenHex, requestSecreteEncryptedWithSelf);
		}

		private string ConstructAppTaskUrl(string taskname)
		{
			return string.Format("{0}/{1}/{2}",
				rootUrl, this.AppName, taskname);
		}

		private bool IsAuthorizedAndHasAccessToken()
		{
			return
				this.AccessToken != null
				&& this.AccessSecret != null;
		}

		private string lastResult = null;
		public bool GetPostResultOfApp_AndDecrypt(string taskName, NameValueCollection data_unencrypted, out string resultOrError, bool autoReauthorizeIfAccessTokenInvalid = true)
		{
			lastResult = null;

			if (!IsAuthorizedAndHasAccessToken())
			{
				resultOrError = "AccessToken not obtained yet.";
				return false;
			}

			var parameters = new StringBuilder();

			bool hasPresetPostData = data_unencrypted != null && data_unencrypted.Count > 0;
			if (hasPresetPostData)
			{
				foreach (var key in data_unencrypted.Keys)
				{
					parameters.AppendFormat("{0}={1}&",
						HttpUtility.UrlEncode(key.ToString()),
						HttpUtility.UrlEncode(EncryptionInterop.SimpleTripleDesEncrypt(data_unencrypted[key.ToString()], this.AccessSecret))
						);
				}

				parameters.Length -= 1;
			}

			string accessSecretEncryptedWithSelf = EncryptionInterop.SimpleTripleDesEncrypt(this.AccessSecret, this.AccessSecret)
				.ToLower();
			if (hasPresetPostData)
				parameters.Append("&");
			parameters.AppendFormat(
				"accessToken={0}&accessSecretEncryptedWithSelf={1}",
				HttpUtility.UrlEncode(this.AccessToken),
				accessSecretEncryptedWithSelf);

		retryAction:
			resultOrError = PhpInterop.PostPHP(
				null,
				this.ConstructAppTaskUrl(taskName),
				parameters.ToString(),
				textfeedbackHandler,
				autoreplacePlusWithBrackettedPipe: false);
			if (resultOrError != null)
				resultOrError = resultOrError.Trim();

			lastResult = resultOrError;

			if (!string.IsNullOrWhiteSpace(resultOrError)
				&& resultOrError.StartsWith("ERROR:"))//Would have been the case of PostPHP method caught an Exception
				return false;
			else
			{
				if (this.IsLastResultSayingAccessTokenNotFound())
				{
					if (!autoReauthorizeIfAccessTokenInvalid)
					{
						resultOrError = "The AccessToken was not found on the server";
						return false;
					}
					ActionOnError("The AccessToken was not found on the server, please re-authorize.");
					this.DeleteAccessTokenAndReAuthorize();
					goto retryAction;
				}
				if (this.IsLastResultSayingAccessTokenNotFound())
				{
					if (!autoReauthorizeIfAccessTokenInvalid)
					{
						resultOrError = "The AccessToken was not found on the server";
						return false;
					}
					ActionOnError("The AccessToken has expired, please re-authorize.");
					this.DeleteAccessTokenAndReAuthorize();
					goto retryAction;
				}

				resultOrError = EncryptionInterop.SimpleTripleDesDecrypt(resultOrError, this.AccessSecret);
				return true;
			}
		}

		private const string cAccessTokenNotFound_ErrorMessage = "AccessToken does not exist.";
		public bool IsLastResultSayingAccessTokenNotFound()
		{
			return !string.IsNullOrEmpty(this.lastResult)
				&& this.lastResult.EndsWith(cAccessTokenNotFound_ErrorMessage, StringComparison.InvariantCultureIgnoreCase);
		}

		private const string cAccessTokenExpired_ErrorMessage = "AccessToken has expired.";
		public bool IsLastResultSayingAccessTokenExpired()
		{
			return !string.IsNullOrEmpty(this.lastResult)
				&& this.lastResult.EndsWith(cAccessTokenExpired_ErrorMessage, StringComparison.InvariantCultureIgnoreCase);
		}

		private string GetAppkeyFilePath()
		{
			return SettingsInterop.GetFullFilePathInLocalAppdata("appkey.tok", this.AppName);
		}

		private string GetAccessTokenFilePath()
		{
			return SettingsInterop.GetFullFilePathInLocalAppdata("accesstoken.tok", this.AppName);
		}

		private bool AskforAppkeyAndAppSecret(out string appKey, out string appSecret)
		{
			var filepath = GetAppkeyFilePath();
			if (File.Exists(filepath))
			{
				string fileContents = File.ReadAllText(filepath);
				try
				{
					var pipesplits = fileContents.Trim().Split('|');
					string savedAppKey = EncodeAndDecodeInterop.DecodeStringHex(pipesplits[0]);
					string savedAppSecret = EncodeAndDecodeInterop.DecodeStringHex(pipesplits[1]);
					appKey = savedAppKey;
					appSecret = savedAppSecret;
					return true;
				}
				catch (Exception exc)
				{
					ActionOnError(exc.Message);
					appKey = null;
					appSecret = null;
					return false;
				}
			}

			string tmpAppkey = null;
			string tmpAppsecret = null;

			bool succeededAskingAppkeyAndSecret = false;
			ThreadingInterop.DoAction(
				delegate
				{
					tmpAppkey = InputBoxWPF.Prompt("Please enter the AppKey for app '" + this.AppName + "'", "Enter AppKey");
					if (string.IsNullOrWhiteSpace(tmpAppkey))
						return;

					tmpAppsecret = InputBoxWPF.Prompt("Please enter the AppSecret for app '" + this.AppName + "' and AppKey " + tmpAppkey, "Enter AppSecret");
					if (string.IsNullOrWhiteSpace(tmpAppsecret))
						return;

					succeededAskingAppkeyAndSecret = true;
				},
				true,
				apartmentState: System.Threading.ApartmentState.STA);

			if (!succeededAskingAppkeyAndSecret)
			{
				appKey = null;
				appSecret = null;
				return false;
			}

			if (tmpAppkey.Length != cExpectedKeyLength)
			{
				ActionOnError(string.Format("AppKey should be exactly {0} characters long", cExpectedKeyLength));
				appKey = null;
				appSecret = null;
				return false;
			}
			if (tmpAppsecret.Length != cExpectedSecretLength)
			{
				ActionOnError(string.Format("AppSecret should be exactly {0} characters long", cExpectedSecretLength));
				appKey = null;
				appSecret = null;
				return false;
			}

			string appkeyHex = EncodeAndDecodeInterop.EncodeStringHex(tmpAppkey, this.ActionOnError);
			string appsecretHex = EncodeAndDecodeInterop.EncodeStringHex(tmpAppsecret, this.ActionOnError);
			File.WriteAllText(filepath, string.Format("{0}|{1}", appkeyHex, appsecretHex));
			appKey = tmpAppkey;
			appSecret = tmpAppsecret;
			return true;
		}

		private bool GetAccessTokenAndSecret(out string accessToken, out string accessSecret)
		{
			string accessTokenFilepath = GetAccessTokenFilePath();
			if (File.Exists(accessTokenFilepath))
			{
				string fileContents = File.ReadAllText(accessTokenFilepath);
				try
				{
					var pipesplits = fileContents.Trim().Split('|');
					string savedAccessToken = EncodeAndDecodeInterop.DecodeStringHex(pipesplits[0]);
					string savedAccessSecret = EncodeAndDecodeInterop.DecodeStringHex(pipesplits[1]);
					accessToken = savedAccessToken;
					accessSecret = savedAccessSecret;
					return true;
				}
				catch (Exception exc)
				{
					ActionOnError(exc.Message);
					accessToken = null;
					accessSecret = null;
					return false;
				}
			}

			string urlToGetARequestToken = ConstructUrlToGetRequestToken();
			string requestTokenResponse = PhpInterop.PostPHP(null, urlToGetARequestToken, null, textfeedbackHandler, autoreplacePlusWithBrackettedPipe: false);

			string successStartStr = "Success:";//DO NOT CHANGE, should match in PHP
			if (!requestTokenResponse.StartsWith(successStartStr, StringComparison.InvariantCultureIgnoreCase))
			{
				ActionOnError("Invalid response to obtain RequestToken: " + requestTokenResponse);
				accessToken = null;
				accessSecret = null;
				return false;
			}

			requestTokenResponse = requestTokenResponse.Substring(successStartStr.Length);
			var pipeSplittedRequestTokenAndSecret = requestTokenResponse.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			if (pipeSplittedRequestTokenAndSecret.Length != 2)
			{
				ActionOnError("ERROR: " + requestTokenResponse);
				accessToken = null;
				accessSecret = null;
				return false;
			}

			string requestToken = EncryptionInterop.SimpleTripleDesDecrypt(pipeSplittedRequestTokenAndSecret[0], this.AppSecret);
			string requestSecret = EncryptionInterop.SimpleTripleDesDecrypt(pipeSplittedRequestTokenAndSecret[1], this.AppSecret);
			string callbackUrl = "http://callback/" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff");

			string authorizeUrl = ConstructAuthorizeUrl(requestToken, requestSecret, callbackUrl);
			bool authorizedSuccessfully = false;
			ThreadingInterop.DoAction(
				delegate
				{
					var win = new AuthorizeWebAppWindow(authorizeUrl, callbackUrl);
					var dialogResult = win.ShowDialog();
					if (dialogResult == true)
						authorizedSuccessfully = true;
				},
			true,
			apartmentState: System.Threading.ApartmentState.STA);

			if (!authorizedSuccessfully)
			{
				ActionOnError("User did not authorize app.");
				accessToken = null;
				accessSecret = null;
				return false;
			}

			string convertRequestTokenIntoAccessTokenUrl = ConstructUrlToConvertRequestTokenIntoAccessToken(requestToken, requestSecret);
			string accessTokenResponse = PhpInterop.PostPHP(
				null, convertRequestTokenIntoAccessTokenUrl, null, textfeedbackHandler, autoreplacePlusWithBrackettedPipe: false);

			if (!accessTokenResponse.StartsWith(successStartStr, StringComparison.InvariantCultureIgnoreCase))
			{
				ActionOnError("Invalid response to convert RequestToken into AccessToken: " + accessTokenResponse);
				accessToken = null;
				accessSecret = null;
				return false;
			}

			accessTokenResponse = accessTokenResponse.Substring(successStartStr.Length);
			var pipeSplittedAccessTokenAndSecret = accessTokenResponse.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			if (pipeSplittedAccessTokenAndSecret.Length != 2)
			{
				ActionOnError("ERROR: " + accessTokenResponse);
				accessToken = null;
				accessSecret = null;
				return false;
			}
			string newAccessToken = EncryptionInterop.SimpleTripleDesDecrypt(pipeSplittedAccessTokenAndSecret[0], requestSecret);
			string newAccessSecret = EncryptionInterop.SimpleTripleDesDecrypt(pipeSplittedAccessTokenAndSecret[1], requestSecret);

			string datetimeString = DateTime.Now.ToString("yyyy_MM_dd HH_mm_ss_fff");
			string fileContent = 
					EncodeAndDecodeInterop.EncodeStringHex(newAccessToken, ActionOnError)
				+ "|"
				+ EncodeAndDecodeInterop.EncodeStringHex(newAccessSecret, ActionOnError);
			File.WriteAllText(accessTokenFilepath, fileContent);

			accessToken = newAccessToken;
			accessSecret = newAccessSecret;
			return true;
		}

		public void DeleteAccessTokenAndReAuthorize()
		{
			var filepath = GetAccessTokenFilePath();
			if (File.Exists(filepath))
				File.Delete(filepath);

			ObtainAccessTokenAndSecret();
		}
	}
}