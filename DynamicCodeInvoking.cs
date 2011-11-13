using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;
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

	private static List<Type> AllUniqueSimpleTypesInCurrentAssembly = null;
	public static List<Type> GetAllUniqueSimpleTypesInCurrentAssembly
	{
		get
		{
			if (AllUniqueSimpleTypesInCurrentAssembly != null)
				return AllUniqueSimpleTypesInCurrentAssembly;
			AllUniqueSimpleTypesInCurrentAssembly = new List<Type>();
			Assembly[] appAssemblies = System.AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in appAssemblies)
				foreach (Type type in assembly.GetTypes())
					AllUniqueSimpleTypesInCurrentAssembly.Add(type);
			return AllUniqueSimpleTypesInCurrentAssembly;
		}
	}
	public static Type GetTypeFromSimpleString(string SimpleTypeString, bool IgnoreCase = false)
	{
		Type TypeIAmLookingFor = null;
		//List<Type> typeList = GetAllUniqueSimpleTypesInCurrentAssembly;
		foreach (Type type in GetAllUniqueSimpleTypesInCurrentAssembly)
			if (type.ToString().Equals(SimpleTypeString) || (IgnoreCase && type.ToString().ToLower().Equals(SimpleTypeString.ToLower())))
				TypeIAmLookingFor = type;
		return TypeIAmLookingFor;
	}

	private static List<string> AllUniqueSimpleTypeStringsInCurrentAssembly = null;
	public static List<string> GetAllUniqueSimpleTypeStringsInCurrentAssembly
	{
		get
		{
			if (AllUniqueSimpleTypeStringsInCurrentAssembly != null) return AllUniqueSimpleTypeStringsInCurrentAssembly;
			List<string> tmpList = new List<string>();
			List<string> tmpDuplicateList = new List<string>();
			//Assembly[] appAssemblies = System.AppDomain.CurrentDomain.GetAssemblies();
			//foreach (Assembly assembly in appAssemblies)
			//	foreach (Type type in assembly.GetTypes())
			foreach (Type type in GetAllUniqueSimpleTypesInCurrentAssembly)
					if (tmpList.Contains(type.ToString())) tmpDuplicateList.Add(type.ToString());
					else tmpList.Add(type.ToString());
			foreach (string dup in tmpDuplicateList)
				tmpList.RemoveAll((s) => s == dup);
			AllUniqueSimpleTypeStringsInCurrentAssembly = tmpList;
			AllUniqueSimpleTypeStringsInCurrentAssembly.Sort();
			return AllUniqueSimpleTypeStringsInCurrentAssembly;
		}
	}

	public class MethodDetailsClass
	{
		private string DisplayString;
		private MethodInfo _methodInfo;
		private MethodInfo methodInfo
		{
			get { return _methodInfo; }
			set
			{
				ParameterInfo[] parameterInfos = value.GetParameters();
				string parametersString = "";
				foreach (ParameterInfo pi in parameterInfos)
					parametersString += (parametersString.Length > 0 ? ", " : "")
						+ string.Join(" ", pi.ParameterType.Name, pi.Name);
				DisplayString = value.Name + " (" + parametersString + ")";
				_methodInfo = value;
			}
		}

		private Dictionary<string, PropertyNameAndType> hashTableOfParameters = null;
		public Dictionary<string, PropertyNameAndType> HashTableOfParameters
		{
			get
			{
				if (hashTableOfParameters != null)
					return hashTableOfParameters;
				hashTableOfParameters = new Dictionary<string, PropertyNameAndType>();
				foreach (ParameterInfo parInfo in methodInfo.GetParameters()){
					hashTableOfParameters.Add(parInfo.Name, new PropertyNameAndType(parInfo.Name, parInfo.ParameterType));//.DefaultValue);//GetDefault(.ParameterType));
				}
				return hashTableOfParameters;
			}
		}

		public MethodDetailsClass(MethodInfo methodInfoIn)
		{
			methodInfo = methodInfoIn;
		}

		public override string ToString()
		{
			return DisplayString;
		}
	}
}