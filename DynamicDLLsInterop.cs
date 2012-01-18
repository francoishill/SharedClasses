using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using InterfaceForQuickAccessPlugin;
using ICommandWithHandler = InlineCommandToolkit.InlineCommands.ICommandWithHandler;
using OverrideToStringClass = InlineCommandToolkit.InlineCommands.OverrideToStringClass;

namespace DynamicDLLsInterop
{
	public class DynamicDLLs
	{
		public static List<IQuickAccessPluginInterface> PluginList = new List<IQuickAccessPluginInterface>();
		public static List<string> AllSuccessfullyLoadedDllFiles = new List<string>();

		public static bool HasUnreadMessages
		{
			get
			{
				foreach (IQuickAccessPluginInterface plugin in DynamicDLLs.PluginList)
				{
					if (plugin is OverrideToStringClass)
					{
						ICommandWithHandler comm = plugin as ICommandWithHandler;
						foreach (TextFeedbackType key in comm.NumberUnreadMessages.Keys.ToList())
							if (comm.NumberUnreadMessages[key] > 0)
								return true;
					}
				}
				return false;
			}
		}

		private static bool HasUnreadOfType(TextFeedbackType type)
		{
			foreach (IQuickAccessPluginInterface plugin in DynamicDLLs.PluginList)
				if (plugin is OverrideToStringClass)
				{
					ICommandWithHandler comm = plugin as ICommandWithHandler;
					if (comm.NumberUnreadMessages[type] > 0)
						return true;
				}
			return false;
		}
		
		//Error, Success, Noteworthy, Subtle
		public static bool HasUnreadErrorMessages { get { return HasUnreadOfType(TextFeedbackType.Error); } }

		public static bool HasUnreadSuccessMessages { get { return HasUnreadOfType(TextFeedbackType.Success); } }

		public static bool HasUnreadNoteworhyMessages { get { return HasUnreadOfType(TextFeedbackType.Noteworthy); } }

		public static bool HasUnreadSubtleMessages { get { return HasUnreadOfType(TextFeedbackType.Subtle); } }

		public static object InvokeDllMethodGetReturnObject(string FullPathToDll, string ClassName, string MethodName, object[] parameters)
		{
			Assembly u = Assembly.LoadFrom(FullPathToDll);//.LoadFile(FullPathToDll);
			Type t = u.GetType(ClassName);
			if (t == null)
			{
				foreach (Type type in u.GetTypes())
					if (type.Name.Split('+')[type.Name.Split('+').Length - 1].ToLower() == ClassName.ToLower())
					{
						if (t != null)
							UserMessages.ShowWarningMessage(
								"Duplicate ClassNames in dynamically loaded assembly:" + Environment.NewLine +
								"Dll path: " + FullPathToDll + Environment.NewLine +
								"Class name: " + ClassName + Environment.NewLine +
								"Fullname of used classname: " + t.FullName + Environment.NewLine +
								"Fullname of duplicate classname: " + type.FullName);
						else t = type;
					}
			}
			if (t != null)
			{
				MethodInfo m = t.GetMethod(MethodName);
				if (m != null)
				{
					if (parameters != null && parameters.Length >= 1)
					{
						object[] myparam = new object[1];
						myparam[0] = parameters;
						return m.Invoke(null, myparam);
					}
					else
						return m.Invoke(null, null);
				}
				else
					UserMessages.ShowWarningMessage(
						"Method not found in DLL" + Environment.NewLine +
						"Dll path: " + FullPathToDll + Environment.NewLine +
						"Class name: " + ClassName + Environment.NewLine +
						"Method name: " + MethodName);
			}
			else
				UserMessages.ShowWarningMessage(
					"Class not found in DLL" + Environment.NewLine +
					"Dll path: " + FullPathToDll + Environment.NewLine +
					"Class name: " + ClassName);
			return null;
		}

		private static List<string> DllNameExclusionList = new List<string>()
		{
			"InlineCommandToolkit.dll",
			"System.Windows.Controls.Input.Toolkit.dll",
			"System.Windows.Controls.WpfPropertyGrid.dll"
		};

		private static bool IsFileValid(string filePath)
		{
			foreach (string s in DllNameExclusionList)
				//This check is very important as this DLL is already loaded in the QuickAccess.exe and should not be loaded AGAIN via a plugin
				if (filePath.ToLower().EndsWith(s.ToLower()))
					return false;
			return true;
		}

		public static void LoadPluginsInDirectory(string DirectoryFullPath, int DelayLoadDurationMilliseconds = 0)
		{
			Action LoadAction = new Action(delegate
			{
				foreach (string dllFile in Directory.GetFiles(DirectoryFullPath, "*.dll"))
					if (IsFileValid(dllFile))
						LoadPlugin(dllFile);
				//else MessageBox.Show(dllFile);
			});

			if (DelayLoadDurationMilliseconds != 0)
			{
				Timer timer = new Timer();
				timer.Interval = DelayLoadDurationMilliseconds;
				timer.Tick += delegate
				{
					timer.Stop();
					timer.Dispose(); timer = null;
					LoadAction();
				};
				timer.Start();
			}
			else
				LoadAction();
		}

		public static void LoadPlugin(string PluginPath)
		{
			if (!File.Exists(PluginPath))
				UserMessages.ShowWarningMessage("Could not load plugin, file not found: " + PluginPath);
			else
			{
				ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				{
					try
					{
						if (AllSuccessfullyLoadedDllFiles.Count(s => Path.GetFileName(s.ToLower()) == Path.GetFileName(PluginPath.ToLower())) == 0)
						{
							Assembly assembly = Assembly.LoadFrom(PluginPath);//.LoadFile(PluginPath);
							AllSuccessfullyLoadedDllFiles.Add(PluginPath);
							foreach (Type type in assembly.GetTypes())//.DefinedTypes)
								if (!type.IsInterface && type.GetInterface(typeof(IQuickAccessPluginInterface).Name) != null)//Must not include the actual interface IQuickAccessPluginInterface
								{
									IQuickAccessPluginInterface interf = (IQuickAccessPluginInterface)type.GetConstructor(new Type[0]).Invoke(new object[0]);
									PluginList.Add(interf);
								}
						}
					}
					catch (Exception exc)
					{
						UserMessages.ShowErrorMessage("Error trying to load plugin (dll file): " + PluginPath + Environment.NewLine + exc.Message);
					}
				},
				ThreadName: "Load plugins thread");
			}
		}

		//public void Test()
		//{
		//	//TODO: Check out dynamic unloading assemblies using another AppDomain (http://www.codeproject.com/KB/cs/DotNet.aspx)
		//	// Construct and initialize settings for a second AppDomain.
		//	AppDomainSetup ads = new AppDomainSetup ();
		//	ads.ApplicationBase = System.Environment.CurrentDirectory; ads.DisallowBindingRedirects = false;
		//	ads.DisallowCodeDownload = true;
		//	ads.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
		//	// create the second AppDomain.
		//	AppDomain ad2 = AppDomain.CreateDomain ("AD #2", null, ads);
		//	System.Threading.mar MarshalByRefType marts = (MarshalByRefType) ad2.CreateInstanceAndUnwrap (exeAssembly, type of(MarshalByRefType).FullName );
		//	// Call a method on the object via the proxy, passing the // default AppDomain's friendly name in as a parameter.
		//	mbrt.SomeMethod (callingDomainName);
		//	// unload the second AppDomain. This deletes its object and // invalidates the proxy object.
		//	AppDomain.Unload (ad2);
		//}
	}
}