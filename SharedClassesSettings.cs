using System;
using System.Reflection;
using System.Windows.Forms;
using UriProtocol = VisualStudioInteropSettings.UriProtocol;

public class SharedClassesSettings
{
	private static string RootApplicationNameForSharedClasses = "SharedClasses";

	public static VisualStudioInteropSettings visualStudioInterop;// { get; set; }
	public static NetworkInteropSettings networkInteropSettings;// { get; set; }

	private static void SetObjectDefaultIfNull<T>(ref T Obj)
	{
		if (Obj == null) Obj = SettingsInterop.GetSettings<T>(RootApplicationNameForSharedClasses);
		if (Obj == null)//When the file does not exist
		{
			Obj = (T)typeof(T).GetConstructor(new Type[] { }).Invoke(new object[] { });
			SettingsInterop.FlushSettings<T>(Obj, RootApplicationNameForSharedClasses);
		}
	}

	public static void EnsureAllSharedClassesSettingsNotNullCreateDefault()
	{
		SetObjectDefaultIfNull<VisualStudioInteropSettings>(ref visualStudioInterop);
		SetObjectDefaultIfNull<NetworkInteropSettings>(ref networkInteropSettings);

		foreach (FieldInfo fi in typeof(SharedClassesSettings).GetFields(BindingFlags.Public | BindingFlags.Static))
			if (fi.GetValue(null) == null)
				UserMessages.ShowWarningMessage("SharedClassesSettings does not have value for field: " + fi.Name);
	}
}

public class VisualStudioInteropSettings
{
	public enum UriProtocol { Http, Ftp }

	public string BaseUri { get; set; }
	public string RelativeRootUriAFTERvspublishing { get; set; }
	public UriProtocol? UriProtocolForAFTERvspublishing { get; set; }
	public string RelativeRootUriForVsPublishing { get; set; }
	public UriProtocol? UriProtocolForVsPublishing { get; set; }
	public string FtpUsername { get; set; }
	public string FtpPassword { get; set; }

	public VisualStudioInteropSettings()
	{
		BaseUri = "fjh.dyndns.org";//"127.0.0.1";
		RelativeRootUriAFTERvspublishing = "/ownapplications";
		UriProtocolForAFTERvspublishing = UriProtocol.Http;
		RelativeRootUriForVsPublishing = "/francois/websites/firepuma/ownapplications";
		UriProtocolForVsPublishing = UriProtocol.Ftp;
		FtpUsername = "francois";
		FtpPassword = "bokbokkie";//UserMessages.Prompt("Please enter ftp password", DefaultResponse: "");
	}
	public VisualStudioInteropSettings(string BaseUriIn, string RelativeRootUriAFTERvspublishingIn, UriProtocol UriProtocolForAFTERvspublishingIn, string RelativeRootUriForVsPublishingIn, UriProtocol UriProtocolForVsPublishingIn)
	{
		BaseUri = BaseUriIn;
		RelativeRootUriAFTERvspublishing = RelativeRootUriAFTERvspublishingIn;
		UriProtocolForAFTERvspublishing = UriProtocolForAFTERvspublishingIn;
		RelativeRootUriForVsPublishing = RelativeRootUriForVsPublishingIn;
		UriProtocolForVsPublishing = UriProtocolForVsPublishingIn;
	}

	public string GetCombinedUriForAFTERvspublishing()
	{
		return UriProtocolForAFTERvspublishing.ToString().ToLower() + "://" + BaseUri + RelativeRootUriAFTERvspublishing;
	}

	public string GetCombinedUriForVsPublishing()
	{
		return UriProtocolForVsPublishing.ToString().ToLower() + "://" + BaseUri + RelativeRootUriForVsPublishing;
	}
}

public class NetworkInteropSettings
{
	public short ServerSocket_Ttl { get; set; }
	public bool ServerSocket_NoDelay { get; set; }
	public int ServerSocket_ReceiveBufferSize { get; set; }
	public int ServerSocket_SendBufferSize { get; set; }
	public int ServerSocket_MaxNumberPendingConnections { get; set; }
	public int ServerSocket_ListeningPort { get; set; }

	public NetworkInteropSettings()
	{
		ServerSocket_Ttl = 112;
		ServerSocket_NoDelay = true;
		ServerSocket_ReceiveBufferSize = 1024 * 1024 * 10;
		ServerSocket_SendBufferSize = 1024 * 1024 * 10;
		ServerSocket_MaxNumberPendingConnections = 100;
		ServerSocket_ListeningPort = 11000;
	}
}