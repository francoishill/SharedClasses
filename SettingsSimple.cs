using System;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting;
using System.Xml.Serialization;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Configuration;
using System.Reflection;
using System.Diagnostics;

[Obsolete("OnlineSettings was renamed to SettingsSimple, also contained in SharedClasses.SettingsSimple", true)]
public class OnlineSettings
{
}

namespace SharedClasses
{
	public interface IInterceptorNotifiable
	{
		void OnPropertySet(string propertyName);
		void OnPropertyGet(string propertyName);
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

	public static class SettingsSimple
	{
		//Use these 'On..Override' events to override the default behaviour of when stuff happens
		public static event ErrorEventHandler OnErrorOverrideShowingUsermessage = null;
		public static event ErrorEventHandler OnWarningOverrideShowingUsermessage = null;
		public static event EventHandler OnOnlineSettingsSavedSuccessfully = delegate { };

		private static void ShowErrorMessage(string errmsg)
		{
			if (OnErrorOverrideShowingUsermessage == null)
				UserMessages.ShowErrorMessage(errmsg);
			else
				OnErrorOverrideShowingUsermessage(null, new ErrorEventArgs(new Exception(errmsg)));
		}

		private static void ShowWarningMessage(string warnmsg)
		{
			if (OnWarningOverrideShowingUsermessage == null)
				UserMessages.ShowErrorMessage(warnmsg);
			else
				OnWarningOverrideShowingUsermessage(null, new ErrorEventArgs(new Exception(warnmsg)));
		}

		public class GuidAsCategoryPrefixAttribute : Attribute { }

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

							////Only handles string??
							//if (result != null && result.ReturnValue != null && result.ReturnValue.GetType().Name == typeof(List<string>).Name)// result.GetType().Name == typeof(List2<>).Name)
							//{
							//	//ShowWarningMessage(string.Format("Property {0} is of type List<string> and will not be saved when items are added"));
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
			private const string _settingsCategory = "globalsettings";
			[XmlIgnore]
			private string SettingsCategory
			{
				get
				{
					//var thisType = this.GetType();
					//string displayName = thisType.Name.Split('+')[thisType.Name.Split('+').Length - 1];
					//if (thisType.GetCustomAttributes(typeof(GuidAsCategoryPrefixAttribute), true).Length > 0)
					if (IsSeparateSettingsForEachPc)
						return SettingsInterop.GetComputerGuid() + "-" + _settingsCategory;
					else
						return _settingsCategory;
				}
			}
			private bool IsSeparateSettingsForEachPc { get { return this.GetType().GetCustomAttributes(typeof(GuidAsCategoryPrefixAttribute), true).Length > 0; } }

			private string SettingName { get { var thisType = this.GetType(); return thisType.Name.Split('+')[thisType.Name.Split('+').Length - 1]; } }
			//Add the computer GUID to the filename, otherwise it will not work correctly as the OnlineSettings are synced over DropBox
			private string SettingsFileName { get { return SettingName + (IsSeparateSettingsForEachPc ? "[" + SettingsInterop.GetComputerGuidAsFileName() + "]" : "") + SettingsInterop.SettingsFileExtension; } }
			private string SettingsFilePath { get { return SettingsInterop.GetFullFilePathInLocalAppdata(SettingsFileName, "SharedClasses", "OnlineCached", "FJH"); } }
			private string LocalCachedDateFilePath { get { return SettingsFilePath + ".mdate"; } }
			[XmlIgnore]
			private DateTime OnlineModifiedDate = DateTime.MinValue;
			[XmlIgnore]
			private DateTime LocalCachedModifiedDate { get { return GetLocalCahcedModifiedDate(); } }
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
						ShowErrorMessage("Error reading object online: " + errIfFail);
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
						ShowErrorMessage("Error while saving online: " + errIfFail);
					else
					{
						//UserMessages.ShowInfoMessage("Successfully saved online.");
						GetOnlineModifiedDate();
						SaveToLocalCache();
						OnOnlineSettingsSavedSuccessfully(this, new EventArgs());
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

				//Console.Out.WriteLine("Using file: " + SettingsFilePath);
				//Console.Out.Flush();
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
				var json =
					//JSON.Instance.Beautify(JSON.Instance.ToJSON(this, false));
					//There are issues with the Beautifying
					JSON.Instance.ToJSON(this, false);
				File.WriteAllText(SettingsFilePath, json);
				File.WriteAllText(LocalCachedDateFilePath, OnlineModifiedDate.ToString(LocalCacheDateFormat));
			}

			public void PopulateThis()
			{
				System.Windows.Forms.Application.EnableVisualStyles();
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
						ShowWarningMessage("Warning: could not get cached NOR local settings, using defaults.");
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

			public override string ToString()
			{
				return this.SettingName;
			}
		}

		public static bool GetListOfOnlineSettings(out List<object> outList, out Dictionary<IInterceptorNotifiable, Dictionary<PropertyInfo, object>> outPropertyList)
		{
			try
			{
				outList = new List<object>();

				outPropertyList = new Dictionary<IInterceptorNotifiable, Dictionary<PropertyInfo, object>>();

				foreach (Type type in typeof(SettingsSimple).GetNestedTypes(BindingFlags.Public))
					if (!type.IsAbstract && type.BaseType.Name == typeof(SettingsSimple.BaseOnlineClass<>).Name)//Get all settings classes
					{
						PropertyInfo[] staticProperties = type.BaseType.GetProperties(BindingFlags.Static | BindingFlags.Public);
						foreach (PropertyInfo spi in staticProperties)
							if (type == spi.PropertyType)//Check to find the static "Instance" of the class
							{
								var tmpobj = (IInterceptorNotifiable)spi.GetValue(null, new object[0]);
								outList.Add(tmpobj);

								var tmpPropertyValues = new Dictionary<PropertyInfo, object>();
								foreach (var prop in tmpobj.GetType().GetProperties())
								{
									tmpPropertyValues.Add(prop, prop.GetValue(tmpobj, new object[0]).Clone());
								}
								outPropertyList.Add(tmpobj, tmpPropertyValues);
							}
					}
				return true;
			}
			catch
			{
				outList = null;
				outPropertyList = null;
				return false;
			}
		}

		public static bool UseOnlineListAndSaveIfChanged(Action<List<object>> actionOnList, bool sortOnToString = true)
		{
			List<object> objList;
			Dictionary<IInterceptorNotifiable, Dictionary<PropertyInfo, object>> objectsAndPropertyValues;
			if (!SettingsSimple.GetListOfOnlineSettings(out objList, out objectsAndPropertyValues))
				return false;//Exit if could not get list

			if (sortOnToString)
				objList = objList.OrderBy(o => o.ToString()).ToList();

			actionOnList(objList);
			objList.Clear();
			objList = null;

			SettingsSimple.ProcessPropertyCompareToPrevious(objectsAndPropertyValues);
			return true;
		}

		public static void ProcessPropertyCompareToPrevious(Dictionary<IInterceptorNotifiable, Dictionary<PropertyInfo, object>> objectsAndPropertyValues)
		{
			//Check if any of the properties changed.
			var keys = objectsAndPropertyValues.Keys.ToArray();
			var values = objectsAndPropertyValues.Values.ToArray();
			for (int i = 0; i < keys.Length; i++)
			{
				foreach (var prop in values[i].Keys)
				{
					object tmpobj = values[i][prop];
					object tmpobj2 = prop.GetValue(keys[i], new object[0]);

					string err;
					ComparisonResult compareResult = CompareObjects.CompareObjectsByValue(tmpobj, tmpobj2, out err);
					switch (compareResult)
					{
						case ComparisonResult.NullValue:
							ShowWarningMessage(string.Format("Cannot compare values, one/both null values. Obj1 = '{0}', Obj2 = '{1}'", (tmpobj == null ? "[NULL]" : tmpobj.ToString()), (tmpobj2 == null ? "[NULL]" : tmpobj2.ToString())));
							break;
						case ComparisonResult.DifferentTypes:
							ShowWarningMessage(string.Format("Cannot compare different types of '{0}' and '{1}'", tmpobj.GetType().ToString(), tmpobj2.GetType().ToString()));
							break;
						case ComparisonResult.Equal:
							//ShowInfoMessage("Equal");
							break;
						case ComparisonResult.NotEqual:
							//ShowWarningMessage("Changed: " + prop.Name);
							keys[i].OnPropertySet(prop.Name);
							break;
						case ComparisonResult.UnsupportedType:
							ShowWarningMessage(
								string.Format("Type unsupported for comparison, either Obj1 = '{0}' or Obj2 = '{1}', error message:{2}{3}",
									tmpobj.GetType().ToString(),
									tmpobj2.GetType().ToString(),
									Environment.NewLine,
									err));
							break;
					}
				}
			}
			//SharedClasses.GenericSettings.ShowAndEditAllOnlineSettings();
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
			[Description("The full URL to the online DotNetChecker.dll file.")]
			public string OnlineDotnetCheckerDllFileUrl { get; set; }
			[Description("The full URL to the online nsProcess.dll file.")]
			public string OnlineNsProcessDllFileUrl { get; set; }

			//[Editor(typeof(MyCollectionEditor), typeof(UITypeEditor))]
			[Description("A tasklist of application names to be added defaultly to the tasklist to pick.")]
			public List<string> ListedApplicationNames { get; set; }

			public PublishSettings()//Defaults
			{
				this.OnlineDotnetCheckerDllFileUrl = OnlineAppsSettings.Instance.RootFtpUrl.TrimEnd('/') + "/DotNetChecker.dll";
				this.OnlineNsProcessDllFileUrl = OnlineAppsSettings.Instance.RootFtpUrl.TrimEnd('/') + "/nsProcess.dll";

				this.ListedApplicationNames = new List<string>()
                {
					//"AutoConnectWifiAdhoc",
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
			[Description("The full URL to the online xdelta3.exe file.")]
			public string OnlineXDeltaExeFileUrl { get; set; }
			public string OnlinePdfTkExeFileUrl { get; set; }
			public string OnlineSharpRSyncFileUrl { get; set; }

			public AutoSyncSettings()//Defaults
			{
				this.OnlineXDeltaExeFileUrl = OnlineAppsSettings.Instance.RootFtpUrl.TrimEnd('/') + "/xDelta3.exe";
				this.OnlinePdfTkExeFileUrl = OnlineAppsSettings.Instance.RootFtpUrl.TrimEnd('/') + "/pdftk.exe";
				this.OnlineSharpRSyncFileUrl = OnlineAppsSettings.Instance.RootFtpUrl.TrimEnd('/') + "/sharprsync.exe";
			}
		}

		public class SearchInFilesSettings : BaseOnlineClass<SearchInFilesSettings>
		{
			[Description("Files types to exclude when searching through text in a file.")]
			public List<string> ExcludeFileTypes { get; set; }

			public SearchInFilesSettings()//Defaults
			{
				ExcludeFileTypes = new List<string>() { ".exe", ".dll", ".pdb", ".png" };
			}
		}

		public class OnlineAppsSettings : BaseOnlineClass<OnlineAppsSettings>
		{
			[Description("The root directory to the Root FTP Url.")]
			public string RootFtpUrl { get; set; }
			public string AppsDownloadFtpUsername { get; set; }
			public string AppsDownloadFtpPassword { get; set; }
			public string AppsUploadFtpUsername { get; set; }
			public string AppsUploadFtpPassword { get; set; }
			public List<string> AllowedListOfApplicationsToDownload { get; set; }//For the online site firepuma.com/apps (which must be listed, if they have a published folder yet - firepuma.com/ownapplications/[appname])

			public OnlineAppsSettings()
			{
				this.RootFtpUrl = "ftp://fjh.dyndns.org";//"ftp://fjh.dyndns.org/francois/websites/firepuma/ownapplications";
				this.AppsDownloadFtpUsername = "appsdownload";
				this.AppsDownloadFtpPassword = "appsdownload.pass123";
				this.AppsUploadFtpUsername = "ownappsupload";
				this.AppsUploadFtpPassword = "ownappsuploadpass123";
				this.AllowedListOfApplicationsToDownload = new List<string>()
				{
					"AdvancedClipboard",
					"AutoUpdater",
					"AutoUploadChangesToFtp",
					"CompareCSVs",
					"GenericTextFunctions",
					"MiniPopupTasks",
					"MoveSvnToGitWPF",
					"QuickAccess",
					"ShoppingList",
					"StandaloneUploader",
					"StartupTodoManager",
					"StickyNotes",
					"TaskbarShortcuts",
					"TestHoursWorkedCalculator",
					"TestingByteArrayDiff",
					"TestingMonitorSubversion",
					"TopmostSearchBox",
					"WindowsStartupManager"
				};
			}
		}

		[GuidAsCategoryPrefixAttribute]
		public class ApplicationManagerSettings : BaseOnlineClass<ApplicationManagerSettings>
		{
			[Serializable]
			public class RunCommand
			{
				public const int cDefaultDelayInSeconds = 2;

				public enum PathTypes { FullPath, OwnApp };
				public string DisplayName { get; set; }
				public string AppPath { get; set; }
				public string PathToProcessExe { get; set; }//Usually required with Portable apps
				public PathTypes PathType { get; set; }
				public string CommandlineArguments { get; set; }
				public bool WaitForUserInput { get; set; }
				public int DelayAfterStartSeconds { get; set; }
				public bool IsEnabled { get; set; }
				public bool IncludeInQuickClose { get; set; }

				public RunCommand() { }
				public RunCommand(string AppPath, string DisplayName, PathTypes PathType, bool WaitForUserInput = false, string CommandlineArguments = null, int DelayAfterStartSeconds = cDefaultDelayInSeconds, bool IsEnabled = true, bool IncludeInQuickClose = false)
				{
					this.AppPath = AppPath.Trim(' ', '\\', '"', '\'');
					this.DisplayName = DisplayName;
					this.PathType = PathType;
					this.WaitForUserInput = WaitForUserInput;
					this.CommandlineArguments = CommandlineArguments;
					this.DelayAfterStartSeconds = DelayAfterStartSeconds;
					this.IsEnabled = IsEnabled;
					this.IncludeInQuickClose = IncludeInQuickClose;
				}
				/// <summary>
				/// Supported formats are:
				/// c:\path\to\app.exe -arg1 arg2
				/// "c:\path\to\app.exe" arg1 -arg2 arg3
				/// c:\path\to\app.exe "arg1" "arg3"
				/// </summary>
				/// <param name="fullCommandLine">The full command line: path (with/without quotes), including all commandline arguments</param>
				/// <param name="displayName">The display name</param>
				/// <returns>Returns the newly created RunCommand if succeeded, otherwise null</returns>
				public static RunCommand CreateFromFullCommandline(string fullCommandLine, string displayName)
				{
					//fullCommandLine = fullCommandLine.Trim('\"');//Otherwise the last " gets removed for batch file command: cmd.exe /C "c:\path\to\batch.bat"
					int _ExeIndex = fullCommandLine.IndexOf(".exe", StringComparison.InvariantCultureIgnoreCase);
					if (_ExeIndex != -1)
					{
						int fullpathStartIndex = 0;
						if (fullCommandLine[fullpathStartIndex].Equals('"')
							|| fullCommandLine[fullpathStartIndex].Equals('\''))
							fullpathStartIndex++;
						string fullpath = fullCommandLine.Substring(
							fullpathStartIndex, _ExeIndex + ".exe".Length - fullpathStartIndex).Trim();

						string commandlineargs = null;
						int commandLineStartIndex = _ExeIndex + ".exe".Length;
						if (commandLineStartIndex < fullCommandLine.Length)
						{
							if (fullCommandLine[commandLineStartIndex].Equals('"')
								|| fullCommandLine[commandLineStartIndex].Equals('\''))
								commandLineStartIndex++;
							if (commandLineStartIndex < fullCommandLine.Length)
								commandlineargs = fullCommandLine.Substring(commandLineStartIndex).Trim();
						}
						return new RunCommand(fullpath, displayName, PathTypes.FullPath, false, commandlineargs);
					}
					else if (File.Exists(fullCommandLine))
						return new RunCommand(fullCommandLine, Path.GetFileName(fullCommandLine), PathTypes.FullPath);
					else if (Directory.Exists(fullCommandLine))
						return new RunCommand(fullCommandLine, Path.GetFileName(fullCommandLine), PathTypes.FullPath);
					ShowWarningMessage("Cannot obtain RunCommand from full Commanline: " + fullCommandLine);
					return null;
				}

				public override string ToString()
				{
					return string.Format("{0}: {1} [{2}]", DisplayName, AppPath, CommandlineArguments);
				}
			}
			public List<RunCommand> RunCommands { get; set; }
			public ApplicationManagerSettings()//Defaults
			{
				this.RunCommands = new List<RunCommand>()
				{
					new RunCommand("MonitorSystem", "Monitor System", RunCommand.PathTypes.OwnApp),
					new RunCommand("TestingMonitorSubversion", "Testing Monitor Subversion", RunCommand.PathTypes.OwnApp),
					new RunCommand("StartupTodoManager", "Startup Todo Manager", RunCommand.PathTypes.OwnApp),
					new RunCommand("QuickAccess", "Quick Access", RunCommand.PathTypes.OwnApp),
					new RunCommand(@"C:\Program Files (x86)\WizMouse\WizMouse.exe", "WizMouse", RunCommand.PathTypes.FullPath),
					new RunCommand(Path.Combine(CalledFromService.Environment_GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
						@"Google\Chrome\Application\chrome.exe"), "Google Chrome", RunCommand.PathTypes.FullPath, false, "-no-startup-window")
				};
			}
		}

		public class BuildTestSystemSettings : BaseOnlineClass<BuildTestSystemSettings>
		{
			public List<string> ListOfApplicationsToBuild { get; set; }
			public BuildTestSystemSettings()//Defaults
			{
				ListOfApplicationsToBuild = new List<string>()
				{
					"PublishOwnApps",
					"FileOperations",
					//"AutoConnectWifiAdhoc",
					"AddDependenciesCSharp",
					"MonitorSystem",
					"QuickAccess",
					"GenericTextFunctions",
					//"AForgeMotionDetector",
					"TestingMonitorSubversion",
					"StartupTodoManager",
					"ApplicationManager",
					"MonitorClipboardDebugAssertions",
					"KillWadiso6",
					"CodeSnippets",
					"AutoUploadChangesToFtp",
					"TestingByteArrayDiff",
					"TestHoursWorkedCalculator",
					"InstantMessengerClient",
					"InstantMessengerServer",
					"PublishFromCommandline",
					"StickyNotes",
					"MiniPopupTasks",
					"AutoUpdater",
					"WindowsStartupManager",
					"ShowNoCallbackNotification",
					"StandaloneUploader",
					"BuildTestSystem",
					"CompareCSVs",
					"MoveSvnToGitWPF",
					"TaskbarShortcuts"
				};
			}
			public static void EnsureDefaultItemsInList()
			{
				var tmpdefaultBuildTest = new BuildTestSystemSettings();
				var tmpInstanceList = BuildTestSystemSettings.Instance.ListOfApplicationsToBuild;
				bool allthere = true;
				foreach (var app in tmpdefaultBuildTest.ListOfApplicationsToBuild)
					if (!tmpInstanceList.Contains(app, StringComparer.InvariantCultureIgnoreCase))
					{
						allthere = false;
						tmpInstanceList.Add(app);
					}
				allthere = false;
				tmpInstanceList.Remove("AForgeMotionDetector");
				tmpInstanceList.Remove("AutoConnectWifiAdhoc");
				tmpInstanceList.Remove("CodeSnippets");
				tmpInstanceList.Remove("ApplicationManager");

				if (!allthere)
				{
					tmpInstanceList.Sort();
					BuildTestSystemSettings.Instance.ListOfApplicationsToBuild = tmpInstanceList;
				}
			}
		}

		public class AnalyseProjectsSettings : BaseOnlineClass<AnalyseProjectsSettings>
		{
			public List<string> ListOfApplicationsToAnalyse { get; set; }
			public AnalyseProjectsSettings()//Defaults
			{
				ListOfApplicationsToAnalyse = new List<string>()
				{
					"AddDependenciesCSharp",
					"AnalyseProjects",
					"AutoUpdater",
					"AutoUploadChangesToFtp",
					"BuildTestSystem",
					"CodeSnippets",
					"CompareCSVs",
					"FileOperations",
					"GenericTextFunctions",
					"InstantMessengerClient",
					"InstantMessengerServer",
					"KillWadiso6",
					"MiniPopupTasks",
					"MonitorClipboardDebugAssertions",
					"MonitorSystem",
					"MoveSvnToGitWPF",
					"PublishFromCommandline",
					"PublishOwnApps",
					"QuickAccess",
					"SettingsInterop",
					"ShoppingList",
					"ShowNoCallbackNotification",
					"StandaloneUploader",
					"StartupTodoManager",
					"StickyNotes",
					"TaskbarShortcuts",
					"TestHoursWorkedCalculator",
					"TestingByteArrayDiff",
					"TestingMonitorSubversion",
					"TopmostSearchBox",
					"WindowsStartupManager",
				};
			}
		}

		public class HomePcUrls : BaseOnlineClass<HomePcUrls>
		{
			public string JsonDataRoot { get; set; }
			public string PhpDownloadUrl { get; set; }
			public string PhpUploadUrl { get; set; }
			public string AppsPublishingRoot { get; set; }
			public string WebappsRoot { get; set; }

			public HomePcUrls()//Defaults
			{
				this.JsonDataRoot = "http://firepuma.com";//"http://json.getmyip.com";
				this.PhpDownloadUrl = "http://firepuma.com/downloadownapps.php";//"http://ftpviahttp.getmyip.com/downloadownapps.php";
				this.PhpUploadUrl = "http://firepuma.com/uploadownapps.php";//"http://ftpviahttp.getmyip.com/uploadownapps.php";
				this.AppsPublishingRoot = "http://firepuma.com";// "http://fjh.dyndns.org";
				this.WebappsRoot = "http://firepuma.com";
			}
		}

		public class SvnCredentials : BaseOnlineClass<SvnCredentials>
		{
			private const EncodeAndDecodeInterop.EncodingType encodingType = EncodeAndDecodeInterop.EncodingType.ASCII;

			public string Username { get; set; }
			[Browsable(false)]
			[XmlIgnore]
			public string Password
			{
				get { return EncodeAndDecodeInterop.DecodeString(this.PasswordEncrypted, encodingType); }
				set { this.PasswordEncrypted = EncodeAndDecodeInterop.EncodeString(value, EncodeAndDecodeInterop.EncodingType.ASCII); }
			}
			[Browsable(false)]
			[XmlElement("Password")]
			public string PasswordEncrypted { get; set; }

			public SvnCredentials()//Defaults
			{
			}
		}

		public class WebsiteKeys : BaseOnlineClass<WebsiteKeys>
		{
			public string Username { get; set; }
			public string ApiKey { get; set; }
			public Dictionary<string, string> AppSecrets { get; set; }

			public WebsiteKeys()//Defaults
			{
			}
		}
	}
}