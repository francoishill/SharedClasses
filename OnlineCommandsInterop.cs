using System;

namespace SharedClasses
{
	public enum OnlineCommands { 
		/*Client_ConnectAndRegister, *//*Client_UploadFile, Client_RequestToDownloadFile,
		Server_ShowMessage, Server_TakeScreenshot, Server_StartReceivingFile*/
		ThisSide_TakeScreenshotAndSend, ThisSide_ReceiveScreenshot, ThisSide_DownloadFileFromUrl,
		ThisSide_SendFile,

		OtherSide_TakeScreenshot, OtherSide_ReceiveScreenshot, OtherSide_DownloadFileFromUrl};

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