using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Serialization;
using DynamicDLLsInterop;
//using System.Windows.Forms;

namespace SharedClasses
{
	[AttributeUsage(AttributeTargets.All)]
	public class SettingAttribute : Attribute
	{
		// Private fields.
		public string UserPrompt { get; private set; }
		public bool DoNoSaveToFile { get; private set; }
		public bool IsEncrypted { get; private set; }
		public string EncryptedPropertyName { get; private set; }//The public encrypted property name, like if the current property is Password, this variable (EncryptedPropertyName) will be PasswordEncrypted
		public bool IgnoredByPropertyInterceptor_EncryptingAnother { get; private set; }
		public bool RequireFacialAutorisationEverytime { get; private set; }

		public SettingAttribute(string UserPrompt, bool DoNoSaveToFile = false, bool IsEncrypted = false, bool RequireFacialAutorisationEverytime = true, string EncryptedPropertyName = null, bool IgnoredByPropertyInterceptor_EncryptingAnother = false)
		{
			this.UserPrompt = UserPrompt;
			this.DoNoSaveToFile = DoNoSaveToFile;
			this.IsEncrypted = IsEncrypted;
			this.RequireFacialAutorisationEverytime = RequireFacialAutorisationEverytime;
			this.EncryptedPropertyName = EncryptedPropertyName;
			this.IgnoredByPropertyInterceptor_EncryptingAnother = IgnoredByPropertyInterceptor_EncryptingAnother;
		}

		//public string UserPrompt { get { return UserPrompt; } }//public virtual string UserPrompt { get { return UserPrompt; } }
		//public bool DoNoSaveToFile { get { return DoNoSaveToFile; } }
	}

	public sealed class SharedClassesSettings
	{
		//public static string RootApplicationNameForSharedClasses = "SharedClasses";

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

	//public sealed class TempClass
	//{
	//	private static bool settingsAlreadyInitialized = false;
	//	public TempClass()
	//	{
	//		if (!settingsAlreadyInitialized)
	//		{
	//			settingsAlreadyInitialized = true;
	//			EnsureAllSettingsAreInitialized();
	//		}
	//	}

	//	public static void EnsureAllSettingsAreInitialized()
	//	{
	//		foreach (Type type in typeof(GlobalSettings).GetNestedTypes(BindingFlags.Public))
	//			if (!type.IsAbstract && type.BaseType == typeof(GenericSettings))
	//			{
	//				PropertyInfo[] staticProperties = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
	//				foreach (PropertyInfo spi in staticProperties)
	//					if (type == spi.PropertyType)
	//					{
	//						//MessageBox.Show("Static = " + spi.Name + ", of type = " + spi.PropertyType.Name);
	//						PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
	//						foreach (PropertyInfo pi in properties)
	//							pi.GetValue(spi.GetValue(null));
	//						//MessageBox.Show(pi.Name + " = " + pi.GetValue(spi.GetValue(null)).ToString());
	//					}
	//			}
	//	}
	//}

	public abstract class GenericSettings : MarshalByRefObject, IInterceptorNotifiable, INotifyPropertyChanged// : IGenericSettings
	{
		//private TempClass tc = new TempClass();//Leave this here as it ensures all settings are initialized

		private static bool AuthorizationWasDoneOnce = false;

		public static string RootApplicationNameForSharedClasses = "SharedClasses";
		private static EncodeAndDecodeInterop.EncodingType EncodingType = EncodeAndDecodeInterop.EncodingType.ASCII;

		public static string Encrypt(string OriginalString, string PropertyName)
		{
			//TODO: Later on looking at using more secure encoding/encryption
			return EncodeAndDecodeInterop.EncodeString(OriginalString, GenericSettings.EncodingType);
		}

		public static string Decrypt(string OriginalString, string PropertyName, bool RequireFacialAutorisationEverytime)
		{
			if (!RequireFacialAutorisationEverytime && AuthorizationWasDoneOnce)
			{
				AuthorizationWasDoneOnce = true;
				return EncodeAndDecodeInterop.DecodeString(OriginalString, GenericSettings.EncodingType);
			}
			else
			{
				string manualPasswordEnterd;
				bool? confirmedFaceDetection = ConfirmUsingFaceDetection.ConfirmUsingFacedetection(GlobalSettings.FaceDetectionInteropSettings.Instance.FaceName, "Face detection for '" + PropertyName + "'", TimeOutSeconds_nullIfNever: GlobalSettings.FaceDetectionInteropSettings.Instance.TimeOutSecondsBeforeAutoFailing, PasswordFromTextbox: out manualPasswordEnterd);
				if (confirmedFaceDetection == true)//Face was confirmed
				{
					AuthorizationWasDoneOnce = true;
					return EncodeAndDecodeInterop.DecodeString(OriginalString, GenericSettings.EncodingType);
				}
				else if (confirmedFaceDetection == null)//Face was not recognized but manual password was entered
				{
					AuthorizationWasDoneOnce = true;
					return manualPasswordEnterd;
				}
				else// if (confirmedFaceDetection == false)//Window was cancelled or timeout has ocurred
				{
					return null;
				}
			}
		}

		public string sEncrypt(string OriginalString, string PropertyName)
		{
			return GenericSettings.Encrypt(OriginalString, PropertyName);
		}

		public string sDecrypt(string OriginalString, string PropertyName, bool RequireFacialAutorisationEverytime)
		{
			return GenericSettings.Decrypt(OriginalString, PropertyName, RequireFacialAutorisationEverytime);
		}

		public static void EnsureAllSettingsAreInitialized()
		{
			foreach (Type type in typeof(GlobalSettings).GetNestedTypes(BindingFlags.Public))
				if (!type.IsAbstract && type.BaseType == typeof(GenericSettings))//Get all settings classes
				{
					PropertyInfo[] staticProperties = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
					foreach (PropertyInfo spi in staticProperties)
						if (type == spi.PropertyType)//Check to find the static "Instance" of the class
						{
							PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);//Find all properties of class
							foreach (PropertyInfo pi in properties)
							{
								object obj = spi.GetValue(null); 
								if (obj != null)
									pi.GetValue(obj);//This will invoke the _get method which will run through the InterceptorProxy and therefore ask userinput if the value is null
							}
						}
				}
		}

		public static void ShowAndEditAllSettings()
		{
			List<object> objList = new List<object>();
			foreach (Type type in typeof(GlobalSettings).GetNestedTypes(BindingFlags.Public))
				if (!type.IsAbstract && type.BaseType == typeof(GenericSettings))//Get all settings classes
				{
					PropertyInfo[] staticProperties = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
					foreach (PropertyInfo spi in staticProperties)
						if (type == spi.PropertyType)//Check to find the static "Instance" of the class
						{
							//object obj = spi.GetValue(null);
							objList.Add(spi.GetValue(null));
							//return;

							//PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);//Find all properties of class
							//foreach (PropertyInfo pi in properties)
							//	MessageBox.Show(type.Name + "." + pi.Name);
						}
				}

			objList.Add(TempClass.Instance);
			PropertiesEditor pe = new PropertiesEditor(objList);
			pe.ShowDialog();
			pe = null;
		}

		//TODO: Have a look at Lazy<> in c#, being able to initialize an object the first time it is used.
		public abstract void LoadFromFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH");

		public abstract void FlushToFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH");

		//public void SetDefaultValues()
		//{
		//	throw new NotImplementedException();
		//}

		public virtual void OnPropertySet(string propertyName)
		{
			//PropertyInfo pi = this.GetType().GetProperty(propertyName);
			SettingsInterop.FlushSettings(this.GetType(), this, RootApplicationNameForSharedClasses);
			OnPropertyChanged(propertyName);
		}

		public virtual void OnPropertyGet(string propertyName)
		{
			//MessageBox.Show("Get: " + propertyName);
		}

		public override string ToString()
		{
			return this.GetType().Name;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged(string PropertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
		}
	}

	public class TempClass
	{
		public static readonly TempClass Instance = new TempClass();

		public string LoadedPlugins { get { return string.Join(Environment.NewLine, DynamicDLLs.AllSuccessfullyLoadedDllFiles); } }
		public TempClass() { }
	}

	public class GlobalSettings
	{
		//TODO: Check out INotifyPropertyChanged (in System.ComponentModel)
		[Serializable]
		public sealed class FaceDetectionInteropSettings : GenericSettings
		{
			private static volatile FaceDetectionInteropSettings instance;
			private static object lockingObject = new Object();

			public static FaceDetectionInteropSettings Instance
			{
				get
				{
					if (instance == null)
					{
						lock (lockingObject)
						{
							if (instance == null)
							{
								instance = Interceptor<FaceDetectionInteropSettings>.Create();
								instance.LoadFromFile(RootApplicationNameForSharedClasses);
							}
						}
					}
					return instance;
				}
			}

			[Description("The name required for face detection (this is used to decrypt the encrypted passwords).")]
			//TODO: This must be greatly improved in the sense that the face "pictures" and their names are stored online for instance
			[Setting("Please enter the face name used for facial recognition to decrypt passwords")]
			public string FaceName { get; set; }

			[Description("The tolerence for detecting (and matching names) to faces, must be between 0 - 5000, default = 3000.")]
			[Setting("Please enter the tolerance for Face detection (0 - 5000, default = 3000).")]
			public double? RecognitionTolerance { get; set; }

			[Description("The duration (in seconds) before automatically 'failing' face detection if no face was detected.")]
			[Setting("Please enter the timeout for Face detection (in seconds), this is the maximum allowed time for recognizing a face, it will fail thereafter.")]
			public int? TimeOutSecondsBeforeAutoFailing { get; set; }

			public override void LoadFromFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
			{
				instance = Interceptor<FaceDetectionInteropSettings>.Create(SettingsInterop.GetSettings<FaceDetectionInteropSettings>(ApplicationName, SubfolderNameInApplication, CompanyName));
			}

			public override void FlushToFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
			{
				SettingsInterop.FlushSettings<FaceDetectionInteropSettings>(instance, ApplicationName, SubfolderNameInApplication, CompanyName);
			}
		}

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
								instance.LoadFromFile(RootApplicationNameForSharedClasses);
							}
						}
					}
					return instance;
				}
			}

			//TODO: Inside the InputBox (dialogs shown to enter passwords, etc), must have a link for the user to click to "update" all unset properties of the settings.
			//DONE: User must be able to have a look at all settings used and change them (passwords must not be shown, only allow user to change it).
			//TODO: Must allow user to change the passords for the current application instance (not show them)
			public enum UriProtocol { Http, Ftp }

			[Category("My temp category")]
			[Description("The base Uri for both publishing and opening the published page after success (this should not end with a front-slash /).")]
			[SettingAttribute("Please enter the base Uri for Visual Studio publishing, ie. code.google.com")]
			public string BaseUri { get; set; }

			[Description("This is the section after the base url of the url used for opening the published webpage after successful publishing. It must start with a front-slash / (example: /ownapplications).")]
			public string RelativeRootUriAFTERvspublishing { get; set; }

			[Description("The uri protocol (Http or Ftp) used for opening the published webpage after successful publishing.")]
			public UriProtocol? UriProtocolForAFTERvspublishing { get; set; }

			[Description("This is the section after the base url of the visual studio publishing path. It must start with a front-slash / (example: /ownapplications).")]
			public string RelativeRootUriForVsPublishing { get; set; }

			[Description("The uri protocol (Http or Ftp) used for publishing visual studio application.")]
			[SettingAttribute("Please enter Uri protocol for Visual Studio Publishing")]
			public UriProtocol? UriProtocolForVsPublishing { get; set; }

			[Browsable(false)]
			public string FtpUsername { get; set; }

			[Browsable(false)]
			[Setting("Please enter ftp password user for Visual Studio publishing", true, true, false, "FtpPasswordEncrypted")]
			[XmlIgnore]//TODO: Must explicitly set the attribute as [XmlIgnore] otherwise if ANOTHER property is changed and the settings are flushed, the password will also be saved
			public string FtpPassword { get; set; }//{ get { return Decrypt(FtpPasswordEncrypted, ); } set { FtpPasswordEncrypted = Encrypt(value); } }//{ get; set; }
			[Browsable(false)]
			[Setting(null, true, IgnoredByPropertyInterceptor_EncryptingAnother: true)]
			[XmlElement("FtpPassword")]
			public string FtpPasswordEncrypted { get; set; }//{ return Encrypt(FtpPassword); } set { FtpPassword = Decrypt(value); } }

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
								instance.LoadFromFile(RootApplicationNameForSharedClasses);
							}
						}
					}
					return instance;
				}
			}

			[Description("The TTL (time-to-live) for the socket server.")]
			[Setting("Please enter value for the time-to-live (Ttl) of the server socket")]
			public short? ServerSocket_Ttl { get; set; }

			[Description("Whether NoDelay should be enabled on the socket server.")]
			[Setting("Enable NoDelay for the server socket?")]
			public bool? ServerSocket_NoDelay { get; set; }

			[Description("The buffersize for receiving data from the client.")]
			public int? ServerSocket_ReceiveBufferSize { get; set; }

			[Description("The buffersize for sending data to the client.")]
			public int? ServerSocket_SendBufferSize { get; set; }

			[Description("The maximum number of pending connections allowed on the server.")]
			public int? ServerSocket_MaxNumberPendingConnections { get; set; }

			[Description("The listening port used for a socket server.")]
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
								instance.LoadFromFile(RootApplicationNameForSharedClasses);
							}
						}
					}
					return instance;
				}
			}

			[Description("The username used for authentication Trac XmlRpc communication.")]
			[Setting("Please enter ftp username for Trac XmlRpc")]
			public string Username { get; set; }

			//TODO: Implement Username in UserPrompt message [Setting("Please enter ftp password for Trac XmlRpc, username " + Username)]
			[Browsable(false)]
			[Setting("Please enter ftp password for Trac XmlRpc, username ", true, true, false, "PasswordEncrypted")]
			[XmlIgnore]
			public string Password { get; set; }
			[Browsable(false)]
			[Setting(null, true, IgnoredByPropertyInterceptor_EncryptingAnother: true)]
			[XmlElement("Password")]
			public string PasswordEncrypted { get; set; }//{ get { return Encrypt(Password); } set { Password = Decrypt(value); } }

			//TODO: Check out Attribute = [NotifyParentProperty]
			[Description("The base url of the Dynamic Invoking server (example: http://localhost or http://mywebsite). It must not end with a slash.")]
			[Setting("Please enter the Base url of the Dynamic Invoking Server")]
			public string DynamicInvokationServer_BasePath { get; set; }

			[Description("This is the section after the base url of the dynamic invoking server. It must start with a front-slash / (example: /dynamicserver/xmlrpc).")]
			[Setting("Please enter the Relative url of the Dynamic Invokation Server")]
			public string DynamicInvokationServer_RelativePath { get; set; }

			[Description("This is the section after the base url use for connecting to the dynamic invoking server. This may be different than DynamicInvokationServer_RelativePath as it supports proxy bypass method via http port (80). It must start with a front-slash / (example: /dynamicserver/xmlrpc).")]
			[Setting("Please enter the Relative url for connecting to the Dynamic Invokation Server")]
			public string ClientProxyBypass_RelativePath { get; set; }

			[Description("The port number used for starting a server to allow Dynamic Invoking of code from a clientside pc.")]
			[Setting("Please enter the Port number of the Dynamic Invoking Server")]
			public int? DynamicInvokationServer_PortNumber { get; set; }

			[Description("The port number used for connecting to a Dynamic Invoking server, this may be port 80 and therefore different to DynamicInvokationServer_PortNumber as it supports proxy bypass method via http port (80).")]
			[Setting("Please enter the Port number for connecting to the Dynamic Invoking Server")]
			public int? ClientProxyBypass_PortNumber { get; set; }

			//TODO: Sort out how this list to string will work inside the PropertyInterceptor
			private List<string> listedXmlRpcUrls;
			[Description("A list of XmlRpc urls used for obtaining Trac ticketing information when publishing an application.")]
			public string ListedXmlRpcUrls
			{
				get
				{
					if (listedXmlRpcUrls == null || listedXmlRpcUrls.Count == 0)
						listedXmlRpcUrls = new List<string>(InputBoxWPF.Prompt("Please enter a list of XmlRpc urls (comma separated)").Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
					return listedXmlRpcUrls == null ? null : string.Join("|", listedXmlRpcUrls);
				}
				set { listedXmlRpcUrls = value == null ? null : new List<string>(value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)); }
			}
			public List<string> GetListedXmlRpcUrls() { return listedXmlRpcUrls ?? new List<string>(); }

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

			public string GetCombinedUrlForDynamicInvokationServer()
			{
				return DynamicInvokationServer_BasePath + ":" + DynamicInvokationServer_PortNumber + DynamicInvokationServer_RelativePath;
			}

			public string GetCombinedUrlForConnectingToDynamicInvokationServer()
			{
				return DynamicInvokationServer_BasePath + ":" + ClientProxyBypass_PortNumber + ClientProxyBypass_RelativePath;
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

		[Serializable]
		public class MouseGesturesSettings : GenericSettings
		{
			private static volatile MouseGesturesSettings instance;
			private static object lockingObject = new Object();

			public static MouseGesturesSettings Instance
			{
				get
				{
					if (instance == null)
					{
						lock (lockingObject)
						{
							if (instance == null)
							{
								instance = Interceptor<MouseGesturesSettings>.Create();//new TracXmlRpcInteropSettings();
								instance.LoadFromFile(RootApplicationNameForSharedClasses);
							}
						}
					}
					return instance;
				}
			}

			//public enum GestureDirection { Up, Down, Left, Right };
			//[Setting("Please define the list of gestures to use")]
			//public Dictionary<string, List<GestureDirection>> ListOfGesturesAndMessages { get; set; }
			private Dictionary<string, string> gesturesWithGesturePluginName;
			[Description("The list of gestures with a message to display, each on a new line (example: URDL=Hallo there you have gestured Up-Right-Down-Left).")]
			public string GesturesWithGesturePluginName
			{
				get
				{
					if (gesturesWithGesturePluginName == null)
						return null;

					string tmpstr = "";
					foreach (string key in gesturesWithGesturePluginName.Keys)
						tmpstr += (!string.IsNullOrWhiteSpace(tmpstr) ? "|" : "") + key + "=" + gesturesWithGesturePluginName[key];
					return tmpstr;
				}
				set
				{
					if (string.IsNullOrWhiteSpace(value))
						return;
					if (gesturesWithGesturePluginName != null) gesturesWithGesturePluginName.Clear();
					string[] pairs = value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string pair in pairs)
					{
						if (!pair.Contains("=") || pair.Split('=').Length != 2)
						{
							UserMessages.ShowWarningMessage("Cannot get gesture and message from: " + pair);
							continue;
						}
						string[] keyvalue = pair.Split('=');
						bool AllCharsIsUDLR = true;
						foreach (char chr in keyvalue[0].ToUpper().ToCharArray())
							if (chr != 'U' && chr != 'D' && chr != 'L' && chr != 'R')
							{
								AllCharsIsUDLR = false;
								UserMessages.ShowWarningMessage("Gesture may only consist of characters U, D, L, R. For example URDL (this means up-right-down-left: " + Environment.NewLine + keyvalue);
								break;
							}
						if (!AllCharsIsUDLR)
							continue;

						if (gesturesWithGesturePluginName == null)
							gesturesWithGesturePluginName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
						gesturesWithGesturePluginName.Add(keyvalue[0].ToUpper(), keyvalue[1]);
					}
				}
			}
			public Dictionary<string, string> GetGesturesWithGesturePluginName() { return gesturesWithGesturePluginName ?? new Dictionary<string, string>(); }

			public override void LoadFromFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
			{
				instance = Interceptor<MouseGesturesSettings>.Create(SettingsInterop.GetSettings<MouseGesturesSettings>(ApplicationName, SubfolderNameInApplication, CompanyName));
			}

			public override void FlushToFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
			{
				SettingsInterop.FlushSettings<MouseGesturesSettings>(instance, ApplicationName, SubfolderNameInApplication, CompanyName);
			}
		}

		[Serializable]
		public class SubversionSettings : GenericSettings
		{
			private static volatile SubversionSettings instance;
			private static object lockingObject = new Object();

			public static SubversionSettings Instance
			{
				get
				{
					if (instance == null)
					{
						lock (lockingObject)
						{
							if (instance == null)
							{
								instance = Interceptor<SubversionSettings>.Create();//new TracXmlRpcInteropSettings();
								instance.LoadFromFile(RootApplicationNameForSharedClasses);
							}
						}
					}
					return instance;
				}
			}

			//TODO: add custom types to XAML: http://blogs.msdn.com/b/mikehillberg/archive/2006/10/06/limitedgenericssupportinxaml.aspx
			private List<string> listOfMonitoredSubversionDirectories;
			[Description("The list of subversion directories to automatically check for any changes/updates.")]
			public string ListOfMonitoredSubversionDirectories
			{
				get
				{
					if (listOfMonitoredSubversionDirectories == null || listOfMonitoredSubversionDirectories.Count == 0)
						listOfMonitoredSubversionDirectories = new List<string>(InputBoxWPF.Prompt("Please enter a list of monitored Subversion directories").Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
					return listOfMonitoredSubversionDirectories == null ? null : string.Join("|", listOfMonitoredSubversionDirectories);
				}
				set { listOfMonitoredSubversionDirectories = value == null ? null : new List<string>(value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)); }
			}
			public List<string> GetListOfMonitoredSubversionDirectories() { return listOfMonitoredSubversionDirectories ?? new List<string>(); }

			[Description("The interval (in milliseconds) for checking the monitored subversion directories.")]
			public int? IntervalForMonitoring_Milliseconds { get; set; }

			public override void LoadFromFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
			{
				instance = Interceptor<SubversionSettings>.Create(SettingsInterop.GetSettings<SubversionSettings>(ApplicationName, SubfolderNameInApplication, CompanyName));
			}

			public override void FlushToFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
			{
				SettingsInterop.FlushSettings<SubversionSettings>(instance, ApplicationName, SubfolderNameInApplication, CompanyName);
			}
		}
	}
}