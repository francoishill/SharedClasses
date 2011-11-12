using System;
using System.Collections.Generic;
using System.Reflection;
using CookComputing.XmlRpc;

public class DynamicCodeInvoking
{
	/// <summary>
	/// Run code dynamically by defining type and command. Example: DynamicCodeInvoking.RunCode(typeof(MessageBox), new Type[] { typeof(string) }, "Show", "Hallo");
	/// </summary>
	/// <param name="type">Type of class which to get the static Method in</param>
	/// <param name="commandParameterTypes">The parameter types to control which call to a overloaded method to use</param>
	/// <param name="methodName">The method name to call</param>
	/// <param name="parameterValues">The values of the method to use.</param>
	public static string RunCodeFromStaticClass(string AssemblyQualifiedName, string[] commandParameterTypesfullnames, string methodName, params object[] parameterValues)
	{
		try
		{
			List<Type> parameterTypeList = new List<Type>();
			foreach (string typestring in commandParameterTypesfullnames)
				parameterTypeList.Add(Type.GetType(typestring, true));
			//Type typeToObtainMethodFrom = Assembly.Load("system.windows.forms, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089").GetType(FullnameOfType, true);
			Type typeToObtainMethodFrom = Type.GetType(AssemblyQualifiedName, true);
			MethodInfo mi = 
				parameterTypeList.Count > 0
				? typeToObtainMethodFrom.GetMethod(methodName, parameterTypeList.ToArray())
				: typeToObtainMethodFrom.GetMethod(methodName);
			mi.Invoke(null, parameterValues);
			return "";
		}
		catch (Exception exc)
		{
			return "Could not perform command: " + exc.Message + Environment.NewLine + exc.StackTrace;
		}
	}

}