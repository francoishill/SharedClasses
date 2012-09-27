using System;

namespace SharedClasses
{
	public enum OnlineCommands { 
		/*Client_ConnectAndRegister, *//*Client_UploadFile, Client_RequestToDownloadFile,
		Server_ShowMessage, Server_TakeScreenshot, Server_StartReceivingFile*/
		TakeScreenshotAndSend, DownloadFileFromUrl,
		SendFile,

		ReceiveScreenshot};

	public class OnlineCommandsInterop
	{
		public static OnlineCommands? GetOnlineServerCommandFromString(string enumStr, bool caseSensitive = false)
		{
			OnlineCommands com;
			if (!Enum.TryParse<OnlineCommands>(enumStr, !caseSensitive, out com))
				return null;
			else
				return com;
		}
	}
}