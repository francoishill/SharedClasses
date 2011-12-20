using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Windows.Forms;
using UriProtocol = VisualStudioInteropSettings.UriProtocol;

[AttributeUsage(AttributeTargets.All)]
public class SettingAttribute : Attribute
{
	// Private fields.
	private string userPrompt;

	public SettingAttribute(string userPrompt)
	{
		this.userPrompt = userPrompt;
	}

	public virtual string UserPrompt
	{
		get { return userPrompt; }
	}
}

public sealed class SharedClassesSettings
{
	public static string RootApplicationNameForSharedClasses = "SharedClasses";

	//private static VisualStudioInteropSettings _visualStudioInterop;
	//private static object syncRootVisualStudioInterop = new Object();
	//public static VisualStudioInteropSettings VisualStudioInterop
	//{
	//	get
	//	{
	//		if (_visualStudioInterop == null)
	//		{
	//			lock (syncRootVisualStudioInterop)
	//			{
	//				if (_visualStudioInterop == null)
	//					_visualStudioInterop = new VisualStudioInteropSettings();
	//			}
	//		}
	//		return _visualStudioInterop;
	//	}
	//}
	//public static NetworkInteropSettings _networkInteropSettings;
	//private static object syncRootNetworkInteropSettings = new Object();
	//public static NetworkInteropSettings NetworkInteropSettings
	//{
	//	get
	//	{
	//		if (_networkInteropSettings == null)
	//		{
	//			lock (syncRootNetworkInteropSettings)
	//			{
	//				if (_networkInteropSettings == null)
	//					_networkInteropSettings = new NetworkInteropSettings();
	//			}
	//		}
	//		return _networkInteropSettings;
	//	}
	//}
	//public static TracXmlRpcInteropSettings _tracXmlRpcInteropSettings;
	//private static object syncRootTracXmlRpcInteropSettings = new Object();
	//public static TracXmlRpcInteropSettings TracXmlRpcInteropSettings
	//{
	//	get
	//	{
	//		if (_tracXmlRpcInteropSettings == null)
	//		{
	//			lock (syncRootTracXmlRpcInteropSettings)
	//			{
	//				if (_tracXmlRpcInteropSettings == null)
	//					_tracXmlRpcInteropSettings = new TracXmlRpcInteropSettings();
	//			}
	//		}
	//		return _tracXmlRpcInteropSettings;
	//	}
	//}

	//private static bool WasAlreadyCalledEnsureAllSharedClassesSettingsNotNullCreateDefault = false;

	//public static void EnsureAllSharedClassesSettingsNotNullCreateDefault()
	//{
	//	if (WasAlreadyCalledEnsureAllSharedClassesSettingsNotNullCreateDefault)
	//		return;

	//	//typeof(Sha

	//	//PropertyInfo[] propertyInfos = typeof(SharedClassesSettings).GetProperties(BindingFlags.Public | BindingFlags.Static);
	//	//foreach (PropertyInfo pi in propertyInfos)
	//	//	if (pi.PropertyType.BaseType == typeof(GenericSettings))
	//	//		(pi.GetValue(null) as GenericSettings).LoadFromFile(RootApplicationNameForSharedClasses);

	//	//FieldInfo[] fieldInfos = typeof(SharedClassesSettings).GetFields(BindingFlags.Public | BindingFlags.Static);
	//	//foreach (FieldInfo fi in fieldInfos)
	//	//	if (fi.FieldType.BaseType == typeof(GenericSettings))
	//	//		(fi.GetValue(null) as GenericSettings).LoadFromFile(null, "Appname");

	//	//VisualStudioInteropSettings.LoadFromFile(typeof(VisualStudioInteropSettings)RootApplicationNameForSharedClasses);
	//	//SetObjectDefaultIfNull<VisualStudioInteropSettings>(ref _visualStudioInterop);
	//	//SetObjectDefaultIfNull<NetworkInteropSettings>(ref _networkInteropSettings);
	//	//SetObjectDefaultIfNull<TracXmlRpcInteropSettings>(ref _tracXmlRpcInteropSettings);

	//	//foreach (FieldInfo fi in typeof(SharedClassesSettings).GetFields(BindingFlags.Public | BindingFlags.Static))
	//	//	if (fi.GetValue(null) == null)
	//	//		UserMessages.ShowWarningMessage("SharedClassesSettings does not have value for field: " + fi.Name);

	//	WasAlreadyCalledEnsureAllSharedClassesSettingsNotNullCreateDefault = true;
	//}

	//private static object obj = new object();
	//private static void SetObjectDefaultIfNull<T>(ref T Obj)
	//{
	//	if (Obj == null) Obj = SettingsInterop.GetSettings<T>(RootApplicationNameForSharedClasses);
	//	if (Obj == null)//When the file does not exist
	//		Obj = (T)typeof(T).GetConstructor(new Type[] { }).Invoke(new object[] { });
	//	lock (obj)
	//		SettingsInterop.FlushSettings<T>(Obj, RootApplicationNameForSharedClasses);
	//}
}

//interface IGenericSettings
//{
//	bool LoadFromFile();
//	void SetDefaultValues();
//}

//internal sealed partial class Settings : ApplicationSettingsBase
//{
//	private static Settings defaultInstance = (
//			(Settings)(ApplicationSettingsBase.Synchronized(new Settings()))
//			);
//	public static Settings Default
//	{
//		get
//		{
//			return defaultInstance;
//		}
//	}
//}

public abstract class GenericSettings : MarshalByRefObject, IInterceptorNotifiable// : IGenericSettings
{
	public abstract void LoadFromFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH");

	public abstract void FlushToFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH");

	//public void SetDefaultValues()
	//{
	//	throw new NotImplementedException();
	//}

	public virtual void OnPropertySet(string propertyName)
	{
		PropertyInfo pi = this.GetType().GetProperty(propertyName);
		SettingsInterop.FlushSettings(this.GetType(), this, SharedClassesSettings.RootApplicationNameForSharedClasses);
	}

	public virtual void OnPropertyGet(string propertyName)
	{
		//MessageBox.Show("Get: " + propertyName);
	}
}

//TODO: Check out INotifyPropertyChanged (in System.ComponentModel)
[Serializable]
public sealed class VisualStudioInteropSettings : GenericSettings
{
	private static volatile VisualStudioInteropSettings instance;
	private static object lockingObject = new Object();

	public static VisualStudioInteropSettings Instance
	{
		get
		{
			if (instance == null)
			{
				lock (lockingObject)
				{
					if (instance == null)
					{
						instance = Interceptor<VisualStudioInteropSettings>.Create();//new VisualStudioInteropSettings();
						instance.LoadFromFile(SharedClassesSettings.RootApplicationNameForSharedClasses);
					}
				}
			}
			return instance;
		}
	}

	public enum UriProtocol { Http, Ftp }

	[SettingAttribute("Please enter the base Uri for Visual Studio publishing, ie. code.google.com")]
	public string BaseUri { get; set; }

	public string RelativeRootUriAFTERvspublishing { get; set; }

	public UriProtocol? UriProtocolForAFTERvspublishing { get; set; }

	public string RelativeRootUriForVsPublishing { get; set; }

	[SettingAttribute("Please enter Uri protocol for Visual Studio Publishing")]
	public UriProtocol? UriProtocolForVsPublishing { get; set; }
		
	public string FtpUsername { get; set; }

	public string FtpPassword { get; set; }

	[Obsolete("Do not use constructor otherwise getting/setting of properties does not go through Interceptor. Use Interceptor<VisualStudioInteropSettings>.Create(); ")]
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

	public string GetCombinedUriForAFTERvspublishing()
	{
		return UriProtocolForAFTERvspublishing.ToString().ToLower() + "://" + BaseUri + RelativeRootUriAFTERvspublishing;
	}

	public string GetCombinedUriForVsPublishing()
	{
		return UriProtocolForVsPublishing.ToString().ToLower() + "://" + BaseUri + RelativeRootUriForVsPublishing;
	}

	public override void LoadFromFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{
		instance = Interceptor<VisualStudioInteropSettings>.Create(SettingsInterop.GetSettings<VisualStudioInteropSettings>(ApplicationName, SubfolderNameInApplication, CompanyName));
	}

	public override void FlushToFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{
		SettingsInterop.FlushSettings<VisualStudioInteropSettings>(instance, ApplicationName, SubfolderNameInApplication, CompanyName);
	}
}

[Serializable]
public class NetworkInteropSettings : GenericSettings
{
	private static volatile NetworkInteropSettings instance;
	private static object lockingObject = new Object();

	public static NetworkInteropSettings Instance
	{
		get
		{
			if (instance == null)
			{
				lock (lockingObject)
				{
					if (instance == null)
					{
						instance = Interceptor<NetworkInteropSettings>.Create();//new NetworkInteropSettings();
						instance.LoadFromFile(SharedClassesSettings.RootApplicationNameForSharedClasses);
					}
				}
			}
			return instance;
		}
	}

	public short? ServerSocket_Ttl { get; set; }
	public bool? ServerSocket_NoDelay { get; set; }
	public int? ServerSocket_ReceiveBufferSize { get; set; }
	public int? ServerSocket_SendBufferSize { get; set; }
	public int? ServerSocket_MaxNumberPendingConnections { get; set; }
	public int? ServerSocket_ListeningPort { get; set; }

	[Obsolete("Do not use constructor otherwise getting/setting of properties does not go through Interceptor. Use Interceptor<NetworkInteropSettings>.Create(); ")]
	public NetworkInteropSettings()
	{
		//ServerSocket_Ttl = 112;
		//ServerSocket_NoDelay = true;
		//ServerSocket_ReceiveBufferSize = 1024 * 1024 * 10;
		//ServerSocket_SendBufferSize = 1024 * 1024 * 10;
		//ServerSocket_MaxNumberPendingConnections = 100;
		//ServerSocket_ListeningPort = 11000;
	}

	public override void LoadFromFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{
		instance = Interceptor<NetworkInteropSettings>.Create(SettingsInterop.GetSettings<NetworkInteropSettings>(ApplicationName, SubfolderNameInApplication, CompanyName));
	}

	public override void FlushToFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{
		SettingsInterop.FlushSettings<NetworkInteropSettings>(instance, ApplicationName, SubfolderNameInApplication, CompanyName);
	}
}

[Serializable]
public class TracXmlRpcInteropSettings : GenericSettings
{
	private static volatile TracXmlRpcInteropSettings instance;
	private static object lockingObject = new Object();

	public static TracXmlRpcInteropSettings Instance
	{
		get
		{
			if (instance == null)
			{
				lock (lockingObject)
				{
					if (instance == null)
					{
						instance = Interceptor<TracXmlRpcInteropSettings>.Create();//new TracXmlRpcInteropSettings();
						instance.LoadFromFile(SharedClassesSettings.RootApplicationNameForSharedClasses);
					}
				}
			}
			return instance;
		}
	}

	[Setting("Please enter ftp username for Trac XmlRpc")]
	public string Username { get; set; }

	//TODO: Implement Username in UserPrompt message [Setting("Please enter ftp password for Trac XmlRpc, username " + Username)]
	[Setting("Please enter ftp password for Trac XmlRpc, username ")]
	public string Password { get; set; }

	[Setting("Please enter the Base url of the Dynamic Invokation Server")]
	public string DynamicInvokationServer_BasePath { get; set; }

	[Setting("Please enter the Relative url of the Dynamic Invokation Server")]
	public string DynamicInvokationServer_RelativePath { get; set; }

	[Setting("Please enter the Port number of the Dynamic Invokation Server")]
	public int? DynamicInvokationServer_PortNumber { get; set; }

	//TODO: Sort out how this list to string will work inside the PropertyInterceptor
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

	[Obsolete("Do not use constructor otherwise getting/setting of properties does not go through Interceptor. Use Interceptor<TracXmlRpcInteropSettings>.Create(); ")]
	public TracXmlRpcInteropSettings()
	{
		//Username = null;
		//Password = null;
		//ListedXmlRpcUrls = null;
	}

	//public TracXmlRpcInteropSettings(string UsernameIn, string PasswordIn, List<string> ListedXmlRpcUrlsIn)
	//{
	//	Username = UsernameIn;
	//	Password = PasswordIn;
	//	listedXmlRpcUrls = ListedXmlRpcUrlsIn;
	//}

	public string GetCominedUrlForDynamicInvokationServer()
	{
		return DynamicInvokationServer_BasePath + ":" + DynamicInvokationServer_PortNumber + DynamicInvokationServer_RelativePath;
	}

	public override void LoadFromFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{
		instance = Interceptor<TracXmlRpcInteropSettings>.Create(SettingsInterop.GetSettings<TracXmlRpcInteropSettings>(ApplicationName, SubfolderNameInApplication, CompanyName));
	}

	public override void FlushToFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{
		SettingsInterop.FlushSettings<TracXmlRpcInteropSettings>(instance, ApplicationName, SubfolderNameInApplication, CompanyName);
	}
}