using System;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace SharedClasses
{
	public static class AppTypeIndependant
	{
		public enum ApplicationType { Console, WinForm, WPF };
		public static ApplicationType GetApplicationType(bool PreventCallFromWinformsFormConstructor = false)
		{
			if (PreventCallFromWinformsFormConstructor)
			{
				StackTrace st = new StackTrace();
				var callstackConstructors = st.GetFrames().Where(sf => sf.GetMethod() != null && sf.GetMethod().IsConstructor).ToArray();
				if (callstackConstructors.Length > 0)//Was called from a constructor
				{
					var lastConstructor = callstackConstructors.Last();
					if (lastConstructor.GetMethod() != null
						&& lastConstructor.GetMethod().DeclaringType != null
						&& lastConstructor.GetMethod().DeclaringType.BaseType != null)
					{
						Type formType = ReflectionInterop.GetTypeFromSimpleString("System.Windows.Forms.Form");
						if (lastConstructor.GetMethod().DeclaringType.BaseType == formType)
						{
							//Was called from Winforms constructor, the messageLoop was not initiated yet, so must call it form the Form.Load event
							AppTypeIndependant.ShowErrorMessage(
								"This is a forms application, please call the GetApplicationType method from the Form.Load event instead of from the Form constructor. "
								+ "Application will now exit. See the current StackTrace below:" + new StackTrace().ToString());
							Environment.Exit(0);
						}
					}
				}
			}

			Type formsApplicationType = ReflectionInterop.GetTypeFromSimpleString("System.Windows.Forms.Application");
			if (formsApplicationType != null)//The System.Windows.Forms assembly is referenced, now check if it is a winforms app
			{
				PropertyInfo messageLoopProperty = formsApplicationType.GetProperty("MessageLoop");
				if (messageLoopProperty != null)
				{
					//Check the value of "System.Windows.Forms.Application.MessageLoop"
					object boolMessageLoop = messageLoopProperty.GetValue(null, new object[0]);//Get the static value
					if (boolMessageLoop is bool && (bool)boolMessageLoop == true)
						return ApplicationType.WinForm;
				}
			}

			Type wpfApplicationType = ReflectionInterop.GetTypeFromSimpleString("System.Windows.Application");
			if (wpfApplicationType != null)//The PresentationFramework assembly is referenced, now check if it is a WPF app
			{
				PropertyInfo currentApplicationProperty = wpfApplicationType.GetProperty("Current");
				if (currentApplicationProperty != null)
				{
					//Check the value of "System.Windows.Application.Current"
					object currentValue = currentApplicationProperty.GetValue(null, new object[0]);
					if (currentValue != null)//It is a WPF app
						return ApplicationType.WPF;
				}
			}

			//We assume that it is a Console application, could not determine that it is a WPF/winforms app
			return ApplicationType.Console;
		}

		public static void ShowErrorMessage(string errorMsg)
		{
			var apptype = GetApplicationType(false);
			bool ignoreAppType = false;
		recheckAndIgnoreApptype:
			if (apptype == ApplicationType.WinForm || ignoreAppType)
			{
				Type winformsMessageBoxType = ReflectionInterop.GetTypeFromSimpleString("System.Windows.Forms.MessageBox");
				if (winformsMessageBoxType != null)
				{
					MethodInfo showMethod = winformsMessageBoxType.GetMethod("Show", new Type[] { typeof(string) });
					if (showMethod != null)
					{
						showMethod.Invoke(null, new object[] { "Error: " + errorMsg });
						return;
					}
				}
			}

		if (apptype == ApplicationType.WPF || ignoreAppType)//Do not use "else if" otherwise will not use this code if "ignoreAppType == true"
			{
				Type wpfMessageBoxType = ReflectionInterop.GetTypeFromSimpleString("System.Windows.MessageBox");
				if (wpfMessageBoxType != null)
				{
					MethodInfo showMethod = wpfMessageBoxType.GetMethod("Show", new Type[] { typeof(string) });
					if (showMethod != null)
					{
						showMethod.Invoke(null, new object[] { "Error: " + errorMsg });
						return;
					}
				}
			}

			if (!ignoreAppType)//Retry once if no MessageBox was shown yet
			{
				ignoreAppType = true;
				goto recheckAndIgnoreApptype;
			}

			//No MessageBox class was found
			string tmpTextFile = Path.Combine(Path.GetTempPath(), "TempError.txt");
			File.WriteAllText(tmpTextFile, "Unable to get MessageBox class from WPF/forms application, using this text file instead. The following error has occurred:"
				+ Environment.NewLine + errorMsg);
			Process.Start(tmpTextFile);
		}
	}
}