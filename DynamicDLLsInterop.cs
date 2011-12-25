using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using InterfaceForQuickAccessPlugin;

namespace DynamicDLLsInterop
{
	public class DynamicDLLs
	{
		public static List<IQuickAccessPluginInterface> PluginList = new List<IQuickAccessPluginInterface>();

		public static object InvokeDllMethodGetReturnObject(string FullPathToDll, string ClassName, string MethodName, object[] parameters)
		{
			Assembly u = Assembly.LoadFile(FullPathToDll);
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

		public static void LoadPluginsInDirectory(string DirectoryFullPath, int DelayLoadDurationMilliseconds = 0)
		{
			Action LoadAction = new Action(delegate
			{
				foreach (string dllFile in Directory.GetFiles(DirectoryFullPath, "*.dll"))
					LoadPlugin(dllFile);
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
					Assembly assembly = Assembly.LoadFile(PluginPath);
					foreach (Type type in assembly.GetTypes())//.DefinedTypes)
						if (!type.IsInterface && type.GetInterface(typeof(IQuickAccessPluginInterface).Name) != null)//Must not include the actual interface IQuickAccessPluginInterface
						{
							IQuickAccessPluginInterface interf = (IQuickAccessPluginInterface)type.GetConstructor(new Type[0]).Invoke(new object[0]);
							PluginList.Add(interf);
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