using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Serialization;
using System.Linq;
using System.Runtime;
#if WPF && HAVEPLUGINS
using DynamicDLLsInterop;
#endif
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing.Design;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;


namespace SharedClasses
{
#if NOTUSEINTERCEPTOR
	public class Interceptor<T> where T : MarshalByRefObject, IInterceptorNotifiable, new()
	{
		public static T Create(T instance = null)
		{
			if (instance == null)
				return new T();
			return instance;
		}
	}
#endif

	public static class GenericCloner
	{
		public static T Clone<T>(this T source)
		{
			if (!typeof(T).IsSerializable)
			{
				throw new ArgumentException("The type must be serializable.", "source");
			}

			// Don't serialize a null object, simply return the default for that object
			if (Object.ReferenceEquals(source, null))
			{
				return default(T);
			}

			IFormatter formatter = new BinaryFormatter();
			Stream stream = new MemoryStream();
			using (stream)
			{
				formatter.Serialize(stream, source);
				stream.Seek(0, SeekOrigin.Begin);
				return (T)formatter.Deserialize(stream);
			}
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class SettingAttribute : Attribute
	{
		// Private fields.
		public string UserPrompt { get; private set; }
		public bool DoNoSaveToFile { get; private set; }
		public bool IsEncrypted { get; private set; }
		public string EncryptedPropertyName { get; private set; }//The public encrypted property name, like if the current property is Password, this variable (EncryptedPropertyName) will be PasswordEncrypted
		public bool IgnoredByPropertyInterceptor_EncryptingAnother { get; private set; }
		public bool RequireFacialAutorisationEverytime { get; private set; }

		public SettingAttribute()
		{

		}
		public SettingAttribute(string UserPrompt, bool DoNoSaveToFile, bool IsEncrypted, bool RequireFacialAutorisationEverytime, string EncryptedPropertyName, bool IgnoredByPropertyInterceptor_EncryptingAnother)
		{
			this.UserPrompt = UserPrompt;
			this.DoNoSaveToFile = DoNoSaveToFile;
			this.IsEncrypted = IsEncrypted;
			this.RequireFacialAutorisationEverytime = RequireFacialAutorisationEverytime;
			this.EncryptedPropertyName = EncryptedPropertyName;
			this.IgnoredByPropertyInterceptor_EncryptingAnother = IgnoredByPropertyInterceptor_EncryptingAnother;
		}

		public SettingAttribute(string UserPrompt)
		{
			this.UserPrompt = UserPrompt;
			this.DoNoSaveToFile = false;
			this.IsEncrypted = false;
			this.RequireFacialAutorisationEverytime = true;
			this.EncryptedPropertyName = null;
			this.IgnoredByPropertyInterceptor_EncryptingAnother = false;
		}
		public SettingAttribute(string UserPrompt, bool DoNoSaveToFile)
		{
			this.UserPrompt = UserPrompt;
			this.DoNoSaveToFile = DoNoSaveToFile;
			this.IsEncrypted = false;
			this.RequireFacialAutorisationEverytime = true;
			this.EncryptedPropertyName = null;
			this.IgnoredByPropertyInterceptor_EncryptingAnother = false;
		}
		public SettingAttribute(string UserPrompt, bool DoNoSaveToFile, bool IsEncrypted)
		{
			this.UserPrompt = UserPrompt;
			this.DoNoSaveToFile = DoNoSaveToFile;
			this.IsEncrypted = IsEncrypted;
			this.RequireFacialAutorisationEverytime = true;
			this.EncryptedPropertyName = null;
			this.IgnoredByPropertyInterceptor_EncryptingAnother = false;
		}
		public SettingAttribute(string UserPrompt, bool DoNoSaveToFile, bool IsEncrypted, bool RequireFacialAutorisationEverytime)
		{
			this.UserPrompt = UserPrompt;
			this.DoNoSaveToFile = DoNoSaveToFile;
			this.IsEncrypted = IsEncrypted;
			this.RequireFacialAutorisationEverytime = RequireFacialAutorisationEverytime;
			this.EncryptedPropertyName = null;
			this.IgnoredByPropertyInterceptor_EncryptingAnother = false;
		}
		public SettingAttribute(string UserPrompt, bool DoNoSaveToFile, bool IsEncrypted, bool RequireFacialAutorisationEverytime, string EncryptedPropertyName)
		{
			this.UserPrompt = UserPrompt;
			this.DoNoSaveToFile = DoNoSaveToFile;
			this.IsEncrypted = IsEncrypted;
			this.RequireFacialAutorisationEverytime = RequireFacialAutorisationEverytime;
			this.EncryptedPropertyName = EncryptedPropertyName;
			this.IgnoredByPropertyInterceptor_EncryptingAnother = false;
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

	public interface IInterceptorNotifiable
	{
		void OnPropertySet(string propertyName);
		void OnPropertyGet(string propertyName);
	}

	public abstract class GenericSettings : MarshalByRefObject, IInterceptorNotifiable, INotifyPropertyChanged// : IGenericSettings
	{
		//private TempClass tc = new TempClass();//Leave this here as it ensures all settings are initialized

		//private static bool AuthorizationWasDoneOnce = false;

		public static string RootApplicationNameForSharedClasses = "SharedClasses";
		public static EncodeAndDecodeInterop.EncodingType EncodingType = EncodeAndDecodeInterop.EncodingType.ASCII;

		public static string Encrypt(string OriginalString, string PropertyName)
		{
			//TODO: Later on looking at using more secure encoding/encryption
			return EncodeAndDecodeInterop.EncodeString(OriginalString, GenericSettings.EncodingType);
		}

		public static string Decrypt(string OriginalString, string PropertyName, bool RequireFacialAutorisationEverytime)
		{
			//if (
			//    (!RequireFacialAutorisationEverytime && AuthorizationWasDoneOnce)
			//    || (!(bool)GlobalSettings.FaceDetectionInteropSettings.Instance.RequireFaceAuthorizationForPasswordDecryption))
			//{
			//    AuthorizationWasDoneOnce = true;
			//    return EncodeAndDecodeInterop.DecodeString(OriginalString, GenericSettings.EncodingType);
			//}
			//else
			{
				//TODO: Removed face detection for now, has too many dependencies

				//string manualPasswordEnterd;
				//bool? confirmedFaceDetection = ConfirmUsingFaceDetection.ConfirmUsingFacedetection(GlobalSettings.FaceDetectionInteropSettings.Instance.FaceName, "Face detection for '" + PropertyName + "'", TimeOutSeconds_nullIfNever: GlobalSettings.FaceDetectionInteropSettings.Instance.TimeOutSecondsBeforeAutoFailing, PasswordFromTextbox: out manualPasswordEnterd);
				//if (confirmedFaceDetection == true)//Face was confirmed
				//{
				//AuthorizationWasDoneOnce = true;
				return EncodeAndDecodeInterop.DecodeString(OriginalString, GenericSettings.EncodingType);
				//}
				//else if (confirmedFaceDetection == null)//Face was not recognized but manual password was entered
				//{
				//	AuthorizationWasDoneOnce = true;
				//	return manualPasswordEnterd;
				//}
				//else// if (confirmedFaceDetection == false)//Window was cancelled or timeout has ocurred
				//{
				//	return null;
				//}
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
								object obj = spi.GetValue(null, new object[0]);
								if (obj != null)
									pi.GetValue(obj, new object[0]);//This will invoke the _get method which will run through the InterceptorProxy and therefore ask userinput if the value is null
							}
						}
				}
		}

#if WPF && HAVEPLUGINS
		public static void ShowAndEditAllSettings()
		{
			List<object> objList = new List<object>();
			foreach (Type type in typeof(GlobalSettings).GetNestedTypes(BindingFlags.Public))
				if (!type.IsAbstract && type.BaseType == typeof(GenericSettings))//Get all settings classes
				{
					PropertyInfo[] staticProperties = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
					foreach (PropertyInfo spi in staticProperties)
						if (type == spi.PropertyType)//Check to find the static "Instance" of the class
							objList.Add(spi.GetValue(null));
				}

			objList.Add(TempClass.Instance);
			PropertiesEditor pe = new PropertiesEditor(objList);
			pe.ShowDialog();
			pe = null;
		}
		public static void ShowAndEditAllOnlineSettings()
		{
			List<object> objList = new List<object>();

			Dictionary<IInterceptorNotifiable, Dictionary<PropertyInfo, object>> objectsAndPropertyValues = new Dictionary<IInterceptorNotifiable, Dictionary<PropertyInfo, object>>();

			foreach (Type type in typeof(OnlineSettings).GetNestedTypes(BindingFlags.Public))
				if (!type.IsAbstract && type.BaseType.Name == typeof(OnlineSettings.BaseOnlineClass<>).Name)//Get all settings classes
				{
					PropertyInfo[] staticProperties = type.BaseType.GetProperties(BindingFlags.Static | BindingFlags.Public);
					foreach (PropertyInfo spi in staticProperties)
						if (type == spi.PropertyType)//Check to find the static "Instance" of the class
						{
							var tmpobj = (IInterceptorNotifiable)spi.GetValue(null);
							objList.Add(tmpobj);

							var tmpPropertyValues = new Dictionary<PropertyInfo, object>();
							foreach (var prop in tmpobj.GetType().GetProperties())
							{
								tmpPropertyValues.Add(prop, prop.GetValue(tmpobj).Clone());
							}
							objectsAndPropertyValues.Add(tmpobj, tmpPropertyValues);
						}
				}
			PropertiesEditor pe = new PropertiesEditor(objList);
			pe.ShowDialog();
			pe = null;

			//Check if any of the properties changed.
			var keys = objectsAndPropertyValues.Keys.ToArray();
			var values = objectsAndPropertyValues.Values.ToArray();
			for (int i = 0; i < keys.Length; i++)
			{
				foreach (var prop in values[i].Keys)
				{
					object tmpobj = values[i][prop];
					object tmpobj2 = prop.GetValue(keys[i]);

					ComparisonResult compareResult = CompareObjectsByValue(tmpobj, tmpobj2);
					switch (compareResult)
					{
						case ComparisonResult.NullValue:
							UserMessages.ShowWarningMessage(string.Format("Cannot compare values, one/both null values. Obj1 = '{0}', Obj2 = '{1}'", (tmpobj == null ? "[NULL]" : tmpobj.ToString()), (tmpobj2 == null ? "[NULL]" : tmpobj2.ToString())));
							break;
						case ComparisonResult.DifferentTypes:
							UserMessages.ShowWarningMessage(string.Format("Cannot compare different types of '{0}' and '{1}'", tmpobj.GetType().ToString(), tmpobj2.GetType().ToString()));
							break;
						case ComparisonResult.Equal:
							//UserMessages.ShowInfoMessage("Equal");
							break;
						case ComparisonResult.NotEqual:
							//UserMessages.ShowWarningMessage("Changed: " + prop.Name);
							keys[i].OnPropertySet(prop.Name);
							break;
						case ComparisonResult.UnsupportedType:
							UserMessages.ShowWarningMessage(string.Format("Type unsupported for comparison, either Obj1 = '{0}' or Obj2 = '{1}'", tmpobj.GetType().ToString(), tmpobj2.GetType().ToString()));
							break;
					}
				}
			}
		}
#endif

		public enum ComparisonResult { Equal, NotEqual, UnsupportedType, DifferentTypes, NullValue };
		public static ComparisonResult CompareObjectsByValue(object obj1, object obj2)
		{
			if (obj1 == null || obj2 == null)
				return ComparisonResult.NullValue;
			if (obj1.GetType() != obj2.GetType())
				return ComparisonResult.DifferentTypes;

			if (obj1 is string || obj1 is char
						   || obj1 is bool
						   || obj1 is int || obj1 is long || obj1 is double || obj1 is decimal || obj1 is float
						   || obj1 is byte || obj1 is short || obj1 is sbyte || obj1 is ushort || obj1 is uint || obj1 is ulong
						   || obj1 is DateTime
						   || obj1 is Enum)
			{
				return obj1.Equals(obj2) ? ComparisonResult.Equal : ComparisonResult.NotEqual;
			}
			else if (obj1 is List<string>)
			{
				List<string> tmplist1 = obj1 as List<string>;
				List<string> tmplist2 = obj2 as List<string>;
				return tmplist1.SequenceEqual(tmplist2, StringComparer.InvariantCultureIgnoreCase) ? ComparisonResult.Equal : ComparisonResult.NotEqual;
			}
			return ComparisonResult.UnsupportedType;
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

#if WPF && HAVEPLUGINS
	public class TempClass
	{
		public static readonly TempClass Instance = new TempClass();

		public string LoadedPlugins { get { return string.Join(Environment.NewLine, DynamicDLLs.AllSuccessfullyLoadedDllFiles); } }
		public TempClass() { }
	}
#endif

	public class GlobalSettings
	{
#if CONSOLE
		public static string ReadConsole(string promptMessage)
		{
			Console.WriteLine(promptMessage);
			return Console.ReadLine();
		}
#endif

		[Serializable]
		public sealed class ApplicationManagerSettings : GenericSettings
		{
			private static volatile ApplicationManagerSettings instance;
			private static object lockingObject = new Object();

			public static ApplicationManagerSettings Instance
			{
				get
				{
					if (instance == null)
					{
						lock (lockingObject)
						{
							if (instance == null)
							{
								instance = Interceptor<ApplicationManagerSettings>.Create();
								instance.LoadFromFile(RootApplicationNameForSharedClasses);
							}
						}
					}
					return instance;
				}
			}

			//private List<string> listedApplicationNames;
			[Description("A tasklist of application names to be managed split with pipe character |.")]
			public string ListedApplicationNames { get; set; }
			[Description("A tasklist of application names to be started with windows, split with pipe character |.")]
			public string ListedStartupApplicationNames { get; set; }
			[Description("A list of excecutable commands, arguments come after a | character.")]
			public List<string> RunCommands { get; set; }
			//public string ListedApplicationNames
			//{
			//	get
			//	{
			//		if (listedApplicationNames == null || listedApplicationNames.Count == 0)
			//			listedApplicationNames = new List<string>(InputBoxWPF.Prompt("Please enter a tasklist of application names to manage.").Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
			//		return listedApplicationNames == null ? null : string.Join("|", listedApplicationNames);
			//	}
			//	set { listedApplicationNames = value == null ? null : new List<string>(value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)); }
			//}
			//public List<string> GetListedApplicationNames() { string tmp = ListedApplicationNames; tmp = null; return listedApplicationNames ?? new List<string>(); }
			public List<string> GetListedApplicationNames() { if (ListedApplicationNames == null) return new List<string>(); else return new List<string>(ListedApplicationNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)); }
			public List<string> GetListedStartupApplicationNames() { if (ListedStartupApplicationNames == null) return new List<string>(); else return new List<string>(ListedStartupApplicationNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)); }

			public override void LoadFromFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
			{
				instance = Interceptor<ApplicationManagerSettings>.Create(SettingsInterop.GetSettings<ApplicationManagerSettings>(ApplicationName, SubfolderNameInApplication, CompanyName));
			}

			public override void FlushToFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
			{
				SettingsInterop.FlushSettings<ApplicationManagerSettings>(instance, ApplicationName, SubfolderNameInApplication, CompanyName);
			}
		}

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

			[Description("Whether face detection is required to authorize decrypting passwords, otherwise they will just be decrypted.")]
			[Setting("Must any face authorization be required before decrypting passwords?")]
			public bool? RequireFaceAuthorizationForPasswordDecryption { get; set; }

			public override void LoadFromFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
			{
				instance = Interceptor<FaceDetectionInteropSettings>.Create(SettingsInterop.GetSettings<FaceDetectionInteropSettings>(ApplicationName, SubfolderNameInApplication, CompanyName));
			}

			public override void FlushToFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
			{
				SettingsInterop.FlushSettings<FaceDetectionInteropSettings>(instance, ApplicationName, SubfolderNameInApplication, CompanyName);
			}
		}

		///
		///See OnlineSettings below
		///
		//[Serializable]
		//public sealed class GoogleApiInteropSettings : GenericSettings
		//{
		//    private static volatile GoogleApiInteropSettings instance;
		//    private static object lockingObject = new Object();

		//    public static GoogleApiInteropSettings Instance
		//    {
		//        get
		//        {
		//            if (instance == null)
		//            {
		//                lock (lockingObject)
		//                {
		//                    if (instance == null)
		//                    {
		//                        instance = Interceptor<GoogleApiInteropSettings>.Create();
		//                        instance.LoadFromFile(RootApplicationNameForSharedClasses);
		//                    }
		//                }
		//            }
		//            return instance;
		//        }
		//    }

		//    [Description("Your google client ID associated with your installed application (https://code.google.com/apis/console/#access).")]
		//    [Setting("Please enter your google client ID associated with your installed application (https://code.google.com/apis/console/#access).")]
		//    public string ClientID { get; set; }

		//    [Browsable(false)]
		//    [Description("Your google client secret associated with your installed application (https://code.google.com/apis/console/#access).")]
		//    [Setting("Please enter your google client secret associated with your installed application (https://code.google.com/apis/console/#access).", true, true, false, "ClientSecretEncrypted")]
		//    [XmlIgnore]
		//    public string ClientSecret { get; set; }
		//    [Browsable(false)]
		//    [Setting(null, true, false, true, null, true)]
		//    [XmlElement("ClientSecret")]
		//    public string ClientSecretEncrypted { get; set; }

		//    [Description("Your google API key associated with your server app (https://code.google.com/apis/console/#access).")]
		//    [Setting("Please enter your google API key associated with your server app (https://code.google.com/apis/console/#access).")]
		//    public string ApiKey { get; set; }

		//    public override void LoadFromFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
		//    {
		//        instance = Interceptor<GoogleApiInteropSettings>.Create(SettingsInterop.GetSettings<GoogleApiInteropSettings>(ApplicationName, SubfolderNameInApplication, CompanyName));
		//    }

		//    public override void FlushToFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
		//    {
		//        SettingsInterop.FlushSettings<GoogleApiInteropSettings>(instance, ApplicationName, SubfolderNameInApplication, CompanyName);
		//    }
		//}


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
			//TODO: Now does not use facedetection/password to get decrypted value
			public string FtpPassword { get { return Decrypt(FtpPasswordEncrypted, "", true); } set { FtpPasswordEncrypted = Encrypt(value, ""); } }//{ get; set; }
			[Browsable(false)]
			[Setting(null, true, false, true, null, true)]
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
			//TODO: Now does not use facedetection/password to get decrypted value
			public string Password { get { return Decrypt(PasswordEncrypted, "", true); } set { PasswordEncrypted = Encrypt(value, ""); } }
			[Browsable(false)]
			[Setting(null, true, false, true, null, true)]
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

			//TODO: Sort out how this tasklist to string will work inside the PropertyInterceptor
			private List<string> listedXmlRpcUrls;
			[Description("A tasklist of XmlRpc urls used for obtaining Trac ticketing information when publishing an application.")]
			public string ListedXmlRpcUrls
			{
				get
				{
					if (listedXmlRpcUrls == null || listedXmlRpcUrls.Count == 0)
						listedXmlRpcUrls = new List<string>(
#if WPF
InputBoxWPF.Prompt(
#elif WINFORMS
DialogBoxStuff.InputDialog(
#elif CONSOLE
GlobalSettings.ReadConsole(
#endif
"Please enter a tasklist of XmlRpc urls (comma separated)").Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
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
				//ListedApplicationNames = null;
			}

			//public TracXmlRpcInteropSettings(string UsernameIn, string PasswordIn, List<string> ListedXmlRpcUrlsIn)
			//{
			//	Username = UsernameIn;
			//	Password = PasswordIn;
			//	listedApplicationNames = ListedXmlRpcUrlsIn;
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
			//[Setting("Please define the tasklist of gestures to use")]
			//public Dictionary<string, List<GestureDirection>> ListOfGesturesAndMessages { get; set; }
			private Dictionary<string, string> gesturesWithGesturePluginName;
			[Description("The tasklist of gestures with a message to display, each on a new line (example: URDL=Hallo there you have gestured Up-Right-Down-Left).")]
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
							//UserMessages.ShowWarningMessage("Cannot get gesture and message from: " + pair);
							continue;
						}
						string[] keyvalue = pair.Split('=');
						bool AllCharsIsUDLR = true;
						foreach (char chr in keyvalue[0].ToUpper().ToCharArray())
							if (chr != 'U' && chr != 'D' && chr != 'L' && chr != 'R')
							{
								AllCharsIsUDLR = false;
								//UserMessages.ShowWarningMessage("Gesture may only consist of characters U, D, L, R. For example URDL (this means up-right-down-left: " + Environment.NewLine + keyvalue);
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
			[Description("The tasklist of subversion directories to automatically check for any changes/updates.")]
			public string ListOfMonitoredSubversionDirectories
			{
				get
				{
					if (listOfMonitoredSubversionDirectories == null || listOfMonitoredSubversionDirectories.Count == 0)
#if WPF
						listOfMonitoredSubversionDirectories =
							new List<string>(InputBoxWPF.Prompt("Please enter a tasklist of monitored Subversion directories")
#elif WINFORMS
						listOfMonitoredSubversionDirectories =
							new List<string>(DialogBoxStuff.InputDialog("Please enter a list of monitored Subversion directories")
#elif CONSOLE
						Console.WriteLine("Please enter a list of monitored Subversion directories");
					listOfMonitoredSubversionDirectories = new List<string>(Console.ReadLine()
#endif

.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
					return listOfMonitoredSubversionDirectories == null ? null : string.Join("|", listOfMonitoredSubversionDirectories);
				}
				set { listOfMonitoredSubversionDirectories = value == null ? null : new List<string>(value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)); }
			}
			public List<string> GetListOfMonitoredSubversionDirectories() { return listOfMonitoredSubversionDirectories ?? new List<string>(); }

			private Dictionary<string, List<string>> groupedMonitoredList;
			[Description("The tasklist of monitored tasklist (divided into groups)")]
			public string GroupedMonitoredList
			{
				get
				{
					string promptResult = null;
					if (groupedMonitoredList == null || groupedMonitoredList.Count == 0)
					{
						promptResult =
#if WPF
 InputBoxWPF.Prompt(
#elif WINFORMS
 DialogBoxStuff.InputDialog(
#elif CONSOLE
 GlobalSettings.ReadConsole(
#endif

"Please enter a tasklist of grouped items in format Category|first\\path,seconds\\path|Second category|third\\path");
						groupedMonitoredList = new Dictionary<string, List<string>>();
						string[] splits = promptResult.Split('|');
						for (int i = 0; i < splits.Length / 2; i++)
							groupedMonitoredList.Add(splits[i * 2], new List<string>(splits[i * 2 + 1].Split(',')));
					}
					string onestring = "";
					if (groupedMonitoredList != null)
						foreach (string key in groupedMonitoredList.Keys)
						{
							onestring += (onestring.Length > 0 ? "|" : "") + key;
							for (int i = 0; i < groupedMonitoredList[key].Count; i++)
								onestring += (i > 0 ? "," : "") + groupedMonitoredList[key][i];
						}
					return onestring;
				}
				set
				{
					if (value == null)
						groupedMonitoredList = null;
					else
					{
						groupedMonitoredList = new Dictionary<string, List<string>>();
						string[] splits = value.Split('|');
						for (int i = 0; i < splits.Length / 2; i++)
							groupedMonitoredList.Add(splits[i * 2], new List<string>(splits[i * 2 + 1].Split(',')));
					}
				}
			}
			public Dictionary<string, List<string>> GetGroupedMonitoredList()
			{
				return groupedMonitoredList ?? new Dictionary<string, List<string>>();
			}

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

		[Serializable]
		public sealed class MovieOrganizerSettings : GenericSettings
		{
			private static volatile MovieOrganizerSettings instance;
			private static object lockingObject = new Object();

			public static MovieOrganizerSettings Instance
			{
				get
				{
					if (instance == null)
					{
						lock (lockingObject)
						{
							if (instance == null)
							{
								instance = Interceptor<MovieOrganizerSettings>.Create();
								instance.LoadFromFile(RootApplicationNameForSharedClasses);
							}
						}
					}
					return instance;
				}
			}

			[Description("Root directory of movies.")]
			public string MoviesRootDirectory { get; set; }

			public override void LoadFromFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
			{
				instance = Interceptor<MovieOrganizerSettings>.Create(SettingsInterop.GetSettings<MovieOrganizerSettings>(ApplicationName, SubfolderNameInApplication, CompanyName));
			}

			public override void FlushToFile(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
			{
				SettingsInterop.FlushSettings<MovieOrganizerSettings>(instance, ApplicationName, SubfolderNameInApplication, CompanyName);
			}
		}
	}

	//public class StringList : List<string> { }

	public static class OnlineSettings
	{
		//public static class List2<T> : ObservableCollection<T>, INotifyPropertyChanged
		//{
		//	public List2()
		//	{
		//		this.CollectionChanged += (s, e) =>
		//		{
		//			int i = 1;
		//		};
		//	}
		//}

		public class PropertyInterceptor<T> where T : MarshalByRefObject, IInterceptorNotifiable, new()
		{

			class InterceptorProxy : RealProxy
			{
				T proxy;
				T target;

				public InterceptorProxy(T target)
					: base(typeof(T))
				{
					this.target = target;
				}

				public override object GetTransparentProxy()
				{
					proxy = (T)base.GetTransparentProxy();
					return proxy;
				}

				public override IMessage Invoke(IMessage msg)
				{
					IMethodCallMessage call = msg as IMethodCallMessage;
					if (call != null)
					{
						//gotoRetryAfterUserSet:

						var result = InvokeMethod(call);
						if (call.MethodName.StartsWith("set_"))
						{
							string propName = call.MethodName.Substring(4);
							target.OnPropertySet(propName);
						}
						else if (call.MethodName.StartsWith("get_"))
						{
							string propName = call.MethodName.Substring(4);

							////TODO: Only handles string
							//if (result != null && result.ReturnValue != null && result.ReturnValue.GetType().Name == typeof(List<string>).Name)// result.GetType().Name == typeof(List2<>).Name)
							//{
							//	//UserMessages.ShowWarningMessage(string.Format("Property {0} is of type List<string> and will not be saved when items are added"));
							//	PropertyInfo pi = target.GetType().GetProperty(propName);
							//	List2<string> list = new List2<string>(result.ReturnValue as List<string>, propName);
							//	list.PropertyChanged += (s, e) => { target.OnPropertySet(e.PropertyName); };
							//	pi.SetValue(target, list);
							//	goto gotoRetryAfterUserSet;
							//}

							target.OnPropertyGet(propName);
						}
						return result;
					}
					else
					{
						throw new NotSupportedException();
					}
				}

				IMethodReturnMessage InvokeMethod(IMethodCallMessage callMsg)
				{
					return RemotingServices.ExecuteMessage(target, callMsg);
				}
			}

			public static T Create(T instance = null)
			{
				var interceptor = new InterceptorProxy(instance ?? new T());
				return (T)interceptor.GetTransparentProxy();
			}
		}

		public abstract class BaseOnlineClass<T> : MarshalByRefObject, IInterceptorNotifiable where T : BaseOnlineClass<T>, new()
		{
			private const string LocalCacheDateFormat = "yyyy-MM-dd HH:mm:ss";
			private const string SettingsCategory = "globalsettings";

			private string SettingName { get { var thisType = this.GetType(); return thisType.Name.Split('+')[thisType.Name.Split('+').Length - 1]; } }
			private string SettingsFileName { get { return SettingName + SettingsInterop.SettingsFileExtension; } }
			private string SettingsFilePath { get { return SettingsInterop.GetFullFilePathInLocalAppdata(SettingsFileName, "SharedClasses", "OnlineCached", "FJH"); } }
			private string LocalCachedDateFilePath { get { return SettingsFilePath + ".mdate"; } }
			[XmlIgnore]
			public DateTime OnlineModifiedDate = DateTime.MinValue;
			[XmlIgnore]
			public DateTime LocalCachedModifiedDate { get { return GetLocalCahcedModifiedDate(); } }
			private bool IsBusyComparingOnSeparateThread = false;
			private bool IsBusySavingOnSeparateThread = false;

			private static T instance;
			private static object lockingObject = new Object();
			public static T Instance
			{
				get
				{
					if (instance == null)
					{
						lock (lockingObject)
						{
							if (instance == null)
							{
								instance = PropertyInterceptor<T>.Create();
								instance.PopulateThis();
							}
						}
					}
					return instance;
				}
			}

			public bool? PopulateFromOnline()//true success, false error, null means not found online
			{
				string errIfFail;
				if (!WebInterop.PopulateObjectFromOnline(SettingsCategory, SettingName, this, out errIfFail))
				{
					if (errIfFail.Equals(WebInterop.cErrorIfNotFoundOnline, StringComparison.InvariantCultureIgnoreCase))
					{
						//Set default settings, should already be populated with default values
						return null;
					}
					else
					{
						UserMessages.ShowErrorMessage("Error reading object online: " + errIfFail);
						return false;
					}
				}

				GetOnlineModifiedDate();

				return true;
				//else
				//    UserMessages.ShowInfoMessage(string.Format("Successfully obtained MovieSettings, file extensions: {0}", string.Join(", ", MovieSettings.MovieFileExtensions)));
			}

			private bool? GetOnlineModifiedDate()//true success, false error, null means not found online
			{
				string errIfFail;
				DateTime dt;
				if (WebInterop.GetModifiedTimeFromOnline(SettingsCategory, SettingName, out dt, out errIfFail))
				{
					OnlineModifiedDate = dt;
					return true;
				}
				else if (errIfFail == WebInterop.cErrorIfNotFoundOnline)
				{
					OnlineModifiedDate = DateTime.MinValue.AddDays(1);
					return null;
				}
				return false;
			}

			private DateTime GetLocalCahcedModifiedDate()
			{
				if (!File.Exists(LocalCachedDateFilePath))
					return DateTime.MinValue;

				DateTime dt;
				var fileContents = File.ReadAllText(LocalCachedDateFilePath);
				if (DateTime.TryParseExact(fileContents, LocalCacheDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
					return dt;

				return DateTime.MinValue;
			}

			private object lockObj = new object();
			public bool SaveOnline()
			{
				while (IsBusyComparingOnSeparateThread)
				{ }//Application.DoEvents(); }

				lock (lockObj)
				{
					string errIfFail;
					if (!WebInterop.SaveObjectOnline(SettingsCategory, SettingName, this, out errIfFail))
						UserMessages.ShowErrorMessage("Error while saving online: " + errIfFail);
					else
					{
						//UserMessages.ShowInfoMessage("Successfully saved online.");
						GetOnlineModifiedDate();
						SaveToLocalCache();
						return true;
					}
					return false;
				}
			}
			public void SaveOnlineOnSeparateThread()
			{
				while (IsBusySavingOnSeparateThread)
				{ } //Application.DoEvents();

				IsBusySavingOnSeparateThread = true;
				ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				{
					SaveOnline();
					IsBusySavingOnSeparateThread = false;
				},
				false);
			}

			public bool GetLocalCached()
			{
				if (!File.Exists(SettingsFilePath)) return false;

				var fileJsondata = File.ReadAllText(SettingsFilePath);
				try
				{
					SetStaticJsonSettings();
					JSON.Instance.FillObject(this, fileJsondata);
					return true;
				}
				catch// (Exception exc)
				{ return false; }
			}

			public void SaveToLocalCache()
			{
				SetStaticJsonSettings();
				var json = JSON.Instance.Beautify(JSON.Instance.ToJSON(this, false));
				File.WriteAllText(SettingsFilePath, json);
				File.WriteAllText(LocalCachedDateFilePath, OnlineModifiedDate.ToString(LocalCacheDateFormat));
			}

			public void PopulateThis()
			{
				Application.EnableVisualStyles();
				if (this.GetLocalCached())
				{
					ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
					{
						IsBusyComparingOnSeparateThread = true;
						try
						{
							if (GetOnlineModifiedDate() != false)//Will run through this if not found online
							{
								if (!GetLocalCahcedModifiedDate().Equals(OnlineModifiedDate))
								{
									bool? populatedFromOnline = this.PopulateFromOnline();
									if (populatedFromOnline.HasValue == true)
										this.SaveToLocalCache();
									else if (populatedFromOnline == null)//Not stored online yet
									{
										//Weird scenario as it's cached but not online yet...so save cache online
										IsBusyComparingOnSeparateThread = false;
										SaveOnline();
									}
								}
							}
						}
						finally
						{
							IsBusyComparingOnSeparateThread = false;
						}
					},
					false);
				}
				else
				{
					bool? populatedFromOnline = this.PopulateFromOnline();
					if (populatedFromOnline == true)//Found online
					{
						this.SaveToLocalCache();
					}
					else if (populatedFromOnline == null)//Not found online
					{
						//if (!instance.SaveOnline())
						//    UserMessages.ShowErrorMessage("Could not save setting online: ");
						if (this.SaveOnline())
							this.SaveToLocalCache();
					}
					else if (populatedFromOnline == false)//Error occurred, not internet, etc
					{
						UserMessages.ShowWarningMessage("Warning: could not get cached NOR local settings, using defaults.");
					}
				}
			}

			private void SetStaticJsonSettings()
			{
				JSON.Instance.SerializeNullValues = true;
				JSON.Instance.ShowReadOnlyProperties = true;
				JSON.Instance.UseUTCDateTime = true;
				JSON.Instance.UsingGlobalTypes = false;
			}

			public void OnPropertySet(string propertyName)
			{
				SaveOnlineOnSeparateThread();
			}

			public void OnPropertyGet(string propertyName)
			{
				//Nothing needs to happen here
			}
		}

		public class MovieOrganizerSettings : BaseOnlineClass<MovieOrganizerSettings>
		{
			[Description("A list of file extensions of movies.")]
			public List<string> MovieFileExtensions { get; set; }
			[Description("A list of non-word characters, type them as one long string.")]
			public string NonWordChars { get; set; }
			[Description("A list of irrelevant phrases for movie names (be careful with this).")]
			public List<string> IrrelevantPhrases { get; set; }
			[Description("A list of irrelevant words, almost same as phrases but are removed after splitting the full name into words at the NonWordChars.")]
			public List<string> IrrelevantWords { get; set; }

			public MovieOrganizerSettings()//Defaults
			{
				this.MovieFileExtensions = new List<string>() { "asf", "3gp", "avi", "divx", "flv", "ifo", "mkv", "mp4", "mpeg", "mpg", "vob", "wmv" };
				this.NonWordChars = "&()`:[]_{} ,.-";
				this.IrrelevantPhrases = new List<string>() { "1 of 2", "2 of 2", "imagine sample", "ts imagine", "rio heist ts v3 imagine", "861_", "862_", "863_", "-illustrated", "brrip noir", "r5 line goldfish", "line x264 ac3 vision", "hive cm8", "flawl3ss sample", "line ac3", "t0xic ink", "r5 line readnfo imagine", "cam readnfo imagine", "ts devise", "line ltt", "r5.line.", "line.xvid" };
				this.IrrelevantWords = new List<string>() { "01", "02", "03", "1of2", "1989", "1996", "1997", "1998", "1999", "2000", "2003", "2004", "2005", "2006", "2007", "2008", "2009", "2010", "2011", "2012", "1080p", "1337x", "3xforum", "dvdscr", "noscr", "torentz", "www", "maxspeed", "ro", "axxo", "divx", "dvdrip", "xvid", "faf2009", "opt", "2hd", "hdtv", "vtv", "2of2", "cd1", "cd2", "imbt", "dmd", "ac3", "rc5", "eng", "fxg", "vaper", "brrip", "extratorrentrg", "ts", "20th", "h", "264", "newarriot", "jr", "r5", "x264", "bdrip", "hq", "cm8", "flawl3ss", "t0xic", "nydic", "dd", "avi", "sample", "ii", "rvj", "readnfo", "tfe", "vrxuniique", "ika", "ltrg", "tdc", "m00dy", "gfw", "noir", "nikonxp", "vmt", "ltt", "mxmg", "osht", "NewArtRiot", "qcf", "tnan", "ppvrip", "timpe", "rx" };
			}
		}

		public class GoogleApiInteropSettings : BaseOnlineClass<GoogleApiInteropSettings>
		{
			[Description("Your google client ID associated with your installed application (https://code.google.com/apis/console/#access).")]
			[Setting("Please enter your google client ID associated with your installed application (https://code.google.com/apis/console/#access).")]
			public string ClientID { get; set; }

			[Browsable(false)]
			[Description("Your google client secret associated with your installed application (https://code.google.com/apis/console/#access).")]
			[Setting("Please enter your google client secret associated with your installed application (https://code.google.com/apis/console/#access).", true, true, false, "ClientSecretEncrypted")]
			[XmlIgnore]
			public string ClientSecret { get; set; }
			[Browsable(false)]
			[Setting(null, true, false, true, null, true)]
			[XmlElement("ClientSecret")]
			public string ClientSecretEncrypted { get; set; }

			[Description("Your google API key associated with your server app (https://code.google.com/apis/console/#access).")]
			[Setting("Please enter your google API key associated with your server app (https://code.google.com/apis/console/#access).")]
			public string ApiKey { get; set; }
			public GoogleApiInteropSettings()//Defaults
			{
				this.ClientID = "";
				this.ClientSecret = "";
				this.ClientSecretEncrypted = "";
				this.ApiKey = "";
			}
		}

		//public class List2<T> : List<T>, INotifyPropertyChanged
		//{
		//	private string thisPropName;
		//	public List2(List<T> list, string thisPropertyName) : base(list) { this.thisPropName = thisPropertyName; }
		//	public new void Add(T item)
		//	{
		//		OnPropertyChanged(thisPropName);
		//	}

		//	public event PropertyChangedEventHandler PropertyChanged = new PropertyChangedEventHandler(delegate { });
		//	public void OnPropertyChanged(string propertyName) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
		//}

		public class PublishSettings : BaseOnlineClass<PublishSettings>
		{
			//[Editor(typeof(MyCollectionEditor), typeof(UITypeEditor))]
			[Description("A tasklist of application names to be added defaultly to the tasklist to pick.")]
			public List<string> ListedApplicationNames { get; set; }

			public PublishSettings()//Defaults
			{
				this.ListedApplicationNames = new List<string>()
                {
					"AutoConnectWifiAdhoc",
					"AddDependenciesCSharp",
					"MonitorSystem",
					"QuickAccess",
					"PublishOwnApps",
					"GenericTextFunctions",
					"AForgeMotionDetector",
					"TestingMonitorSubversion",
					"StartupTodoManager",
					"ApplicationManager",
					"MonitorClipboardDebugAssertions"
                };
			}
		}

		public class AutoSyncSettings : BaseOnlineClass<AutoSyncSettings>
		{
			[Description("")]
			public string OnlineXDeltaExeFileUrl { get; set; }

			public AutoSyncSettings()//Defaults
			{
				OnlineXDeltaExeFileUrl = "ftp://fjh.dyndns.org/francois/websites/firepuma/ownapplications/xDelta3.exe";
			}
		}

		public class SearchInFilesSettings : BaseOnlineClass<SearchInFilesSettings>
		{
			[Description("")]
			public List<string> ExcludeFileTypes { get; set; }

			public SearchInFilesSettings()//Defaults
			{
				ExcludeFileTypes = new List<string>() { ".exe", ".dll", ".pdb", ".png" };
			}
		} 
	}
}