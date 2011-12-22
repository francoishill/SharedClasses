using System;
using System.Reflection;
namespace DynamicDLLsInterop
{
	public class DynamicDLLs
	{
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
	}
}