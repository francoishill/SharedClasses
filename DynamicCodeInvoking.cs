using System;
using System.Collections.Generic;
using System.Reflection;
using CookComputing.XmlRpc;

public class DynamicCodeInvoking
{
	public struct RunCodeReturnStruct
	{
		public bool Success;
		public string ErrorMessage;
		public object MethodInvokeResultingObject;
	}

	/// <summary>
	/// Run code dynamically by defining type and command. Example: DynamicCodeInvoking.RunCode(typeof(MessageBox), new Type[] { typeof(string) }, "Show", "Hallo");
	/// </summary>
	/// <param name="type">Type of class which to get the static Method in</param>
	/// <param name="commandParameterTypes">The parameter types to control which call to a overloaded method to use</param>
	/// <param name="methodName">The method name to call</param>
	/// <param name="parameterValues">The values of the method to use.</param>
	/// <returns>The error string, Empty string if no error.</returns>
	public static RunCodeReturnStruct RunCodeFromStaticClass(string AssemblyQualifiedName, string[] commandParameterTypesfullnames, string methodName, params object[] parameterValues)
	{
		try
		{
			List<Type> parameterTypeList = new List<Type>();
			foreach (string typestring in commandParameterTypesfullnames)
				parameterTypeList.Add(Type.GetType(typestring, true));
			//Type typeToObtainMethodFrom = Assembly.Load("system.windows.forms, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089").GetType(FullnameOfType, true);
			Type typeToObtainMethodFrom = Type.GetType(AssemblyQualifiedName, true);
			for (int i = 0; i < parameterTypeList.Count; i++)
				if (parameterTypeList[i].IsEnum && Enum.IsDefined(parameterTypeList[i], parameterValues[i]))
					parameterValues[i] = Enum.Parse(parameterTypeList[i], parameterValues[i].ToString());
			MethodInfo mi = 
				parameterTypeList.Count > 0
				? typeToObtainMethodFrom.GetMethod(methodName, parameterTypeList.ToArray())
				: typeToObtainMethodFrom.GetMethod(methodName);
			object returnObj = mi.Invoke(null, parameterValues);
			return new RunCodeReturnStruct()
			{
				Success = true,
				ErrorMessage = "",
				MethodInvokeResultingObject = returnObj
			};
		}
		catch (Exception exc)
		{
			return new RunCodeReturnStruct()
			{
				Success = false,
				ErrorMessage = "Could not perform command: " + exc.Message + Environment.NewLine + exc.StackTrace,
				MethodInvokeResultingObject = null
			};
		}
	}

	public static void GetParameterListAndTypesStringArray(out string[] TypeStringArray, out object[] ParametersModified, params object[] Parameters)
	{
		List<string> tmpList = new List<string>();
		foreach (object obj in Parameters)
			tmpList.Add(obj.GetType().AssemblyQualifiedName);
		TypeStringArray = tmpList.ToArray();

		List<object> tmpParameterList = new List<object>();
		foreach (object obj in Parameters)
			tmpParameterList.Add(
				obj.GetType().IsEnum
				? obj.ToString()
				: obj);
		ParametersModified = tmpParameterList.ToArray();
	}
}