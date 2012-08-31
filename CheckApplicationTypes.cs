using System;
using System.Linq;

namespace SharedClasses
{
	public static class CheckApplicationTypes
	{
		/// <summary>
		/// This method checks whether it is a WPF application with a StartupUri and then throws an error
		/// </summary>
		public static void CheckIfWpfAppHasStartupUri(Type mainFormType)
		{
			if (!UsesWpfWindow(mainFormType)) return;
			bool hasStartupUri = false;
			//Type type = InheritsFromWpfWindow(typeof(MainFormOrWindowType));
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				string @namespace = "System.Windows";
				var findSystemWindowsApplication = (from t in assembly.GetTypes()//.GetExecutingAssembly().GetTypes()
													where t.IsClass && t.Namespace == @namespace && t.FullName == "System.Windows.Application"
													select t).ToArray();
				foreach (var appType in findSystemWindowsApplication)
				{
					//System.Windows.Application.Current
					var currentAppProperty = appType.GetProperty("Current");
					if (currentAppProperty != null)
					{
						var val = currentAppProperty.GetValue(null, new object[0]);
						if (val != null)
						{
							var uriProperty = val.GetType().GetProperty("StartupUri");
							var startupUri = uriProperty.GetValue(val, new object[0]);
							if (startupUri != null)//Found a startup uri for a WPF application
								hasStartupUri = true;
						}
					}
				}
			}
			if (hasStartupUri)
				throw new Exception("Francois's exception: Please remove StartupUri from App.xaml, this causes unexpected behaviour");
		}
		private static bool UsesWpfWindow(Type mainFormType)
		{
			return InheritsFromWpfWindow(mainFormType) != null;
		}
		private static Type InheritsFromWpfWindow(Type type)
		{
			if (type != null && type.FullName == "System.Windows.Window")
				return type;
			else if (type.BaseType != null && InheritsFromWpfWindow(type.BaseType) != null)
				return InheritsFromWpfWindow(type.BaseType);
			return null;
		}
	}
}