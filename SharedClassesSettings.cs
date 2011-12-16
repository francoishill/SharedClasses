using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

using UriProtocol = VisualStudioInteropSettings.UriProtocol;

public class SharedClassesSettings
{
	private static string RootApplicationNameForSharedClasses = "SharedClasses";

	public static VisualStudioInteropSettings visualStudioInterop = new VisualStudioInteropSettings();// { get; set; }
	public static NetworkInteropSettings networkInteropSettings = new NetworkInteropSettings();// { get; set; }
	public static TracXmlRpcInteropSettings tracXmlRpcInteropSettings = new TracXmlRpcInteropSettings();

	private static bool WasAlreadyCalledEnsureAllSharedClassesSettingsNotNullCreateDefault = false;

	public static void EnsureAllSharedClassesSettingsNotNullCreateDefault()
	{
		if (WasAlreadyCalledEnsureAllSharedClassesSettingsNotNullCreateDefault)
			return;

		//VisualStudioInteropSettings.LoadFromFile(typeof(VisualStudioInteropSettings)RootApplicationNameForSharedClasses);
		SetObjectDefaultIfNull<VisualStudioInteropSettings>(ref visualStudioInterop);
		SetObjectDefaultIfNull<NetworkInteropSettings>(ref networkInteropSettings);
		SetObjectDefaultIfNull<TracXmlRpcInteropSettings>(ref tracXmlRpcInteropSettings);

		foreach (FieldInfo fi in typeof(SharedClassesSettings).GetFields(BindingFlags.Public | BindingFlags.Static))
			if (fi.GetValue(null) == null)
				UserMessages.ShowWarningMessage("SharedClassesSettings does not have value for field: " + fi.Name);
		WasAlreadyCalledEnsureAllSharedClassesSettingsNotNullCreateDefault = true;
	}

	private static object obj = new object();
	private static void SetObjectDefaultIfNull<T>(ref T Obj)
	{
		if (Obj == null) Obj = SettingsInterop.GetSettings<T>(RootApplicationNameForSharedClasses);
		if (Obj == null)//When the file does not exist
			Obj = (T)typeof(T).GetConstructor(new Type[] { }).Invoke(new object[] { });
		lock (obj)
			SettingsInterop.FlushSettings<T>(Obj, RootApplicationNameForSharedClasses);
	}
}

//interface IGenericSettings
//{
//	bool LoadFromFile();
//	void SetDefaultValues();
//}

public class GenericSettings// : IGenericSettings
{
	public static object LoadFromFile(Type ObjectType, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{
		return SettingsInterop.GetSettings(
			ObjectType,
			ApplicationName,
			SubfolderNameInApplication,
			CompanyName);
	}

	public void SetDefaultValues()
	{
		throw new NotImplementedException();
	}
}

//TODO: Check out INotifyPropertyChanged (in System.ComponentModel)
[Serializable]
public class VisualStudioInteropSettings : GenericSettings
{
	public enum UriProtocol { Http, Ftp }

	private string baseUri;
	public string BaseUri
	{
		get
		{
			if (baseUri == null)
				baseUri = UserMessages.Prompt("Please enter the base Uri for Visual Studio publishing, ie. code.google.com");
			return baseUri;
		}
		set { baseUri = value; }
	}
	private string relativeRootUriAFTERvspublishing;
	public string RelativeRootUriAFTERvspublishing
	{
		get
		{
			if (relativeRootUriAFTERvspublishing == null)
				relativeRootUriAFTERvspublishing = UserMessages.Prompt("Please enter Relative Root Uri for after Visual Studio publishing");
			return relativeRootUriAFTERvspublishing;
		}
		set { relativeRootUriAFTERvspublishing = value; }
	}
	private UriProtocol? uriProtocolForAFTERvspublishing;
	public UriProtocol? UriProtocolForAFTERvspublishing
	{
		get
		{
			if (uriProtocolForAFTERvspublishing == null)
				uriProtocolForAFTERvspublishing = UserMessages.PickItem<UriProtocol?>(Enum.GetValues(typeof(UriProtocol)), "Please enter Uri protocol for after Visual Studio publishing", UriProtocol.Http);
			return uriProtocolForAFTERvspublishing;
		}
		set { uriProtocolForAFTERvspublishing = value; }
	}
	private string relativeRootUriForVsPublishing;
	public string RelativeRootUriForVsPublishing
	{
		get
		{
			if (relativeRootUriForVsPublishing == null)
				relativeRootUriForVsPublishing = UserMessages.Prompt("Please enter Relative Root Uri Visual Studio Publishing");
			return relativeRootUriForVsPublishing;
		}
		set { relativeRootUriForVsPublishing = value; }
	}
	private UriProtocol? uriProtocolForVsPublishing;
	public UriProtocol? UriProtocolForVsPublishing
	{
		get
		{
			if (uriProtocolForVsPublishing == null)
				uriProtocolForVsPublishing = UserMessages.PickItem<UriProtocol?>(Enum.GetValues(typeof(UriProtocol)), "Please enter Uri protocol for Visual Studio Publishing", UriProtocol.Ftp);
			return uriProtocolForVsPublishing;
		}
		set { uriProtocolForVsPublishing = value; }
	}
	private string ftpUsername;
	public string FtpUsername
	{
		get
		{
			if (ftpUsername == null)
				ftpUsername = UserMessages.Prompt("Please enter ftp username for Visual Studio Upload", DefaultResponse: null);
			return ftpUsername;
		}
		set { ftpUsername = value; }
	}
	private string ftpPassword;
	public string FtpPassword
	{
		get
		{
			if (ftpPassword == null)
				ftpPassword = UserMessages.Prompt("Please enter ftp password for Visual Studio Upload, username " + FtpUsername, DefaultResponse: null);
			return ftpPassword;
		}
		set { ftpPassword = value; }
	}

	public VisualStudioInteropSettings()
	{
		BaseUri = null;//"fjh.dyndns.org";//"127.0.0.1";
		RelativeRootUriAFTERvspublishing = null;//"/ownapplications";
		UriProtocolForAFTERvspublishing = null;//UriProtocol.Http;
		RelativeRootUriForVsPublishing = null;//"/francois/websites/firepuma/ownapplications";
		UriProtocolForVsPublishing = null;//UriProtocol.Ftp;
		FtpUsername = null;
		FtpPassword = null;
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

[Serializable]
public class NetworkInteropSettings : GenericSettings
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

[Serializable]
public class TracXmlRpcInteropSettings : GenericSettings
{
	private string username;
	public string Username
	{
		get
		{
			if (username == null)
				username = UserMessages.Prompt("Please enter ftp username for Trac XmlRpc", DefaultResponse: null);
			return username;
		}
		set { username = value; }
	}
	private string password;
	public string Password
	{
		get
		{
			if (password == null)
				password = UserMessages.Prompt("Please enter ftp password for Trac XmlRpc, username " + Username, DefaultResponse: null);
			return password;
		}
		set { password = value; }
	}

	private string dynamicInvokationServer_BasePath;
	public string DynamicInvokationServer_BasePath
	{
		get
		{
			//http://fjh.dyndns.org:5678/DynamicCodeInvoking/xmlrpc
			if (dynamicInvokationServer_BasePath == null)
				dynamicInvokationServer_BasePath = UserMessages.Prompt("Please enter the Base url of the Dynamic Invokation Server", DefaultResponse: null);
			return dynamicInvokationServer_BasePath;
		}
		set { dynamicInvokationServer_BasePath = value; }
	}

	private string dynamicInvokationServer_RelativePath;
	public string DynamicInvokationServer_RelativePath
	{
		get
		{
			//http://fjh.dyndns.org:5678/DynamicCodeInvoking/xmlrpc
			if (dynamicInvokationServer_RelativePath == null)
				dynamicInvokationServer_RelativePath = UserMessages.Prompt("Please enter the Relative url of the Dynamic Invokation Server", DefaultResponse: null);
			return dynamicInvokationServer_RelativePath;
		}
		set { dynamicInvokationServer_RelativePath = value; }
	}

	private int? dynamicInvokationServer_PortNumber;
	public int? DynamicInvokationServer_PortNumber
	{
		get
		{
			//http://fjh.dyndns.org:5678/DynamicCodeInvoking/xmlrpc
			if (dynamicInvokationServer_PortNumber == null)
			{
				string tmpStr = UserMessages.Prompt("Please enter the Port number of the Dynamic Invokation Server", DefaultResponse: null);
				if (tmpStr == null) return null;
				int tmpInt;
				if (!int.TryParse(tmpStr, out tmpInt)) return null;
				dynamicInvokationServer_PortNumber = tmpInt;
			}
			return dynamicInvokationServer_PortNumber;
		}
		set { dynamicInvokationServer_PortNumber = value; }
	}
	private List<string> listedXmlRpcUrls;
	public string ListedXmlRpcUrls
	{
		get
		{
			if (listedXmlRpcUrls == null || listedXmlRpcUrls.Count == 0)
				listedXmlRpcUrls = new List<string>(UserMessages.Prompt("Please enter a list of XmlRpc urls (comma separated)").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
			return listedXmlRpcUrls == null ? null : string.Join(",", listedXmlRpcUrls);
		}
		set { listedXmlRpcUrls = value == null ? null : new List<string>(value.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries)); }
	}

	public TracXmlRpcInteropSettings()
	{
		Username = null;
		Password = null;
		ListedXmlRpcUrls = null;
	}

	public TracXmlRpcInteropSettings(string UsernameIn, string PasswordIn, List<string> ListedXmlRpcUrlsIn)
	{
		Username = UsernameIn;
		Password = PasswordIn;
		listedXmlRpcUrls = ListedXmlRpcUrlsIn;
	}

	public string GetCominedUrlForDynamicInvokationServer()
	{
		return DynamicInvokationServer_BasePath + ":" + DynamicInvokationServer_PortNumber + DynamicInvokationServer_RelativePath;
	}
}