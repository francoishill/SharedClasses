using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CookComputing.XmlRpc;
using SharedClasses;

public class DynamicCodeInvoking
{
	public const string VOID_RESULTSTRING = "{VOIDRESULT}";

	[Serializable]
	public struct RunCodeReturnStruct
	{
		public bool Success;
		public string ErrorMessage;
		public string ResultingTypeString;
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
				ResultingTypeString = (returnObj == null ? typeof(string).AssemblyQualifiedName : returnObj.GetType().AssemblyQualifiedName),
				MethodInvokeResultingObject = (returnObj == null ? VOID_RESULTSTRING : (returnObj.GetType().IsEnum ? returnObj.ToString() : returnObj))
			};
		}
		catch (Exception exc)
		{
			return new RunCodeReturnStruct()
			{
				Success = false,
				ErrorMessage = "Could not perform command: " + exc.Message + Environment.NewLine + exc.StackTrace,
				ResultingTypeString = typeof(string).AssemblyQualifiedName,
				MethodInvokeResultingObject = ""
			};
		}
	}

	public static DynamicCodeInvoking.RunCodeReturnStruct RunSelectedFunction(Dictionary<string, ParameterNameAndType> dict, string AssemblyQualifiedName, string MethodName, bool ShowResultMessage = true)
	{
		try
		{
			//SharedClassesSettings.EnsureAllSharedClassesSettingsNotNullCreateDefault();
			Iclientside_DynamicCodeInvokingServerClass proxy = XmlRpcProxyGen.Create<Iclientside_DynamicCodeInvokingServerClass>();
			proxy.Url = GlobalSettings.TracXmlRpcInteropSettings.Instance.GetCombinedUrlForConnectingToDynamicInvokationServer();//.GetCombinedUrlForDynamicInvokationServer();
			//XmlRpcInterop.Tracer tracer = new XmlRpcInterop.Tracer();
			//tracer.Attach(proxy);

			List<object> objectList = new List<object>();
			foreach (string key in dict.Keys)
				objectList.Add(dict[key].Value);
			string[] TypeStringArray;
			object[] ParameterList;
			GetParameterListAndTypesStringArray(out TypeStringArray, out ParameterList, objectList.ToArray());

			RunCodeReturnStruct resultObj = proxy.RunCodeDynamically(
				AssemblyQualifiedName,
				TypeStringArray,
				MethodName,
				ParameterList
				);
			if (Type.GetType(resultObj.ResultingTypeString).IsEnum)
				resultObj.MethodInvokeResultingObject = Enum.Parse(Type.GetType(resultObj.ResultingTypeString), resultObj.MethodInvokeResultingObject.ToString());
			if (resultObj.Success)
			{
				string successMsg = "Successfully performed command: ";
				if (resultObj.MethodInvokeResultingObject is string && resultObj.MethodInvokeResultingObject.ToString() == DynamicCodeInvoking.VOID_RESULTSTRING)
					successMsg += "Void method, no result returned";
				else if (resultObj.MethodInvokeResultingObject is string)
					successMsg += resultObj.MethodInvokeResultingObject as string;
				else if (resultObj.MethodInvokeResultingObject is string[])
					foreach (string s in resultObj.MethodInvokeResultingObject as string[])
						successMsg += Environment.NewLine + s;
				else if (resultObj.MethodInvokeResultingObject is List<string>)
					foreach (string s in resultObj.MethodInvokeResultingObject as List<string>)
						successMsg += Environment.NewLine + s;
				else if (resultObj.MethodInvokeResultingObject is object[])
					foreach (object o in resultObj.MethodInvokeResultingObject as object[])
						if (o != null)
							successMsg += Environment.NewLine + o.ToString();
						else successMsg += resultObj.MethodInvokeResultingObject.ToString();
				else if (resultObj.MethodInvokeResultingObject is byte[])
				{
					if (ShowResultMessage && UserMessages.Confirm("Array of bytes received as response, save this to a file now?", DefaultYesButton: true))
					{
						SaveFileDialog sfd = new SaveFileDialog();
						sfd.Title = "Select the file to save the array of bytes to";
						sfd.Filter = "All files (*.*)|*.*";
						if (sfd.ShowDialog() == DialogResult.OK)
						{
							try
							{
								System.IO.File.WriteAllBytes(sfd.FileName, resultObj.MethodInvokeResultingObject as byte[]);
								System.Diagnostics.Process.Start("explorer", @"/select, """ + sfd.FileName + @"""");
								successMsg = "File successfully saved to file: " + sfd.FileName;
							}
							catch (Exception exc)
							{
								UserMessages.ShowErrorMessage("Could not write byte array to file: " + exc.Message);
								successMsg = "Unable to write byte array to file: " + exc.Message;
							}
						}
					}
					else
						successMsg = "Byte array received, user chose to not save to file.";
				}
				else
				{
					successMsg += resultObj.MethodInvokeResultingObject.ToString();
					if (ShowResultMessage)
						UserMessages.ShowWarningMessage("Unrecognized type = " + resultObj.MethodInvokeResultingObject.GetType().ToString());
				}
				if (ShowResultMessage)
					UserMessages.ShowInfoMessage(successMsg);
				return resultObj;
			}
		}
		catch (Exception exc)
		{
			UserMessages.ShowErrorMessage("Could not perform dynamic method remotely: " + Environment.NewLine + exc.Message + Environment.NewLine + Environment.NewLine + exc.StackTrace);
		}

		RunCodeReturnStruct rc = new RunCodeReturnStruct();
		rc.Success = false;
		return rc;
	}

	public static void RunBlockOfCodeNow(string block)
	{
		try
		{
			//SharedClassesSettings.EnsureAllSharedClassesSettingsNotNullCreateDefault();
			Iclientside_DynamicCodeInvokingServerClass proxy = XmlRpcProxyGen.Create<Iclientside_DynamicCodeInvokingServerClass>();
			proxy.Url = GlobalSettings.TracXmlRpcInteropSettings.Instance.GetCombinedUrlForDynamicInvokationServer();
			//XmlRpcInterop.Tracer tracer = new XmlRpcInterop.Tracer();
			//tracer.Attach(proxy);

			RunCodeReturnStruct resultObj = proxy.RunBlockOfCode(
				block
				);
			if (Type.GetType(resultObj.ResultingTypeString).IsEnum)
				resultObj.MethodInvokeResultingObject = Enum.Parse(Type.GetType(resultObj.ResultingTypeString), resultObj.MethodInvokeResultingObject.ToString());
			if (resultObj.Success)
			{
				string successMsg = "Successfully performed command: ";
				if (resultObj.MethodInvokeResultingObject is string && resultObj.MethodInvokeResultingObject.ToString() == DynamicCodeInvoking.VOID_RESULTSTRING)
					successMsg += "Void method, no result returned";
				else if (resultObj.MethodInvokeResultingObject is string[])
					foreach (string s in resultObj.MethodInvokeResultingObject as string[])
						successMsg += Environment.NewLine + s;
				else if (resultObj.MethodInvokeResultingObject is List<string>)
					foreach (string s in resultObj.MethodInvokeResultingObject as List<string>)
						successMsg += Environment.NewLine + s;
				else successMsg += resultObj.MethodInvokeResultingObject.ToString();
				UserMessages.ShowInfoMessage(successMsg);
			}
		}
		catch (Exception exc)
		{
			UserMessages.ShowErrorMessage("Could not perform dynamic method remotely: " + Environment.NewLine + exc.Message + Environment.NewLine + Environment.NewLine + exc.StackTrace);
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

	public static class DynamicTypeBuilder
	{
		public class Property
		{
			public string FieldName;
			public Type FieldType;
			public Property(string FieldName, Type FieldType)
			{
				this.FieldName = FieldName;
				this.FieldType = FieldType;
			}
		}

		public static object CreateNewObject(string TypeName, List<Property> yourListOfFields)
		{
			var myType = CompileResultType(TypeName, yourListOfFields);
			return Activator.CreateInstance(myType);
		}
		private static Type CompileResultType(string TypeName, List<Property> yourListOfFields)
		{
			TypeBuilder tb = GetTypeBuilder(TypeName);
			ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

			// NOTE: assuming your list contains Property objects with fields FieldName(string) and FieldType(Type)
			foreach (var field in yourListOfFields)
				CreateProperty(tb, field.FieldName, field.FieldType);

			Type objectType = tb.CreateType();
			return objectType;
		}

		private static TypeBuilder GetTypeBuilder(string TypeName)
		{
			var typeSignature = TypeName;
			var an = new AssemblyName(typeSignature);
			AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
			TypeBuilder tb = moduleBuilder.DefineType(
				typeSignature,
				TypeAttributes.Public |
					TypeAttributes.Class |
					TypeAttributes.AutoClass |
					TypeAttributes.AnsiClass |
					TypeAttributes.BeforeFieldInit |
					TypeAttributes.AutoLayout,
				null);
			return tb;
		}

		private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
		{
			FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

			PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, System.Reflection.PropertyAttributes.HasDefault, propertyType, null);
			MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
			ILGenerator getIl = getPropMthdBldr.GetILGenerator();

			getIl.Emit(OpCodes.Ldarg_0);
			getIl.Emit(OpCodes.Ldfld, fieldBuilder);
			getIl.Emit(OpCodes.Ret);

			MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
						MethodAttributes.Public |
						MethodAttributes.SpecialName |
						MethodAttributes.HideBySig,
						null, new[] { propertyType });

			ILGenerator setIl = setPropMthdBldr.GetILGenerator();
			System.Reflection.Emit.Label modifyProperty = setIl.DefineLabel();
			System.Reflection.Emit.Label exitSet = setIl.DefineLabel();

			setIl.MarkLabel(modifyProperty);
			setIl.Emit(OpCodes.Ldarg_0);
			setIl.Emit(OpCodes.Ldarg_1);
			setIl.Emit(OpCodes.Stfld, fieldBuilder);

			setIl.Emit(OpCodes.Nop);
			setIl.MarkLabel(exitSet);
			setIl.Emit(OpCodes.Ret);

			propertyBuilder.SetGetMethod(getPropMthdBldr);
			propertyBuilder.SetSetMethod(setPropMthdBldr);
		}
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
			return AllUniqueSimpleTypesInCurrentAssembly.OrderBy(t => t.FullName).ToList();
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

	private static ObservableCollection<ClassWithStaticMethods> _uniqueTypes = null;
	public static ObservableCollection<ClassWithStaticMethods> UniqueTypes
	{
		get
		{
			if (_uniqueTypes != null)
				return _uniqueTypes;
			_uniqueTypes = new ObservableCollection<ClassWithStaticMethods>();
			foreach (Type type in GetAllUniqueSimpleTypesInCurrentAssembly)
				_uniqueTypes.Add(new ClassWithStaticMethods(type));
			return _uniqueTypes;

			//	if (tmpList.Contains(type.ToString())) tmpDuplicateList.Add(type.ToString());
			//	else tmpList.Add(type.ToString());
			//foreach (string dup in tmpDuplicateList)
			//	tmpList.RemoveAll((s) => s == dup);
			//AllUniqueSimpleTypeStringsInCurrentAssembly = tmpList;
			//AllUniqueSimpleTypeStringsInCurrentAssembly.Sort();
			//return AllUniqueSimpleTypeStringsInCurrentAssembly;
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

		public string MethodName
		{
			get { return methodInfo.Name; }
		}

		private Dictionary<string, ParameterNameAndType> hashTableOfParameters = null;
		public Dictionary<string, ParameterNameAndType> HashTableOfParameters
		{
			get
			{
				if (hashTableOfParameters != null)
					return hashTableOfParameters;
				hashTableOfParameters = new Dictionary<string, ParameterNameAndType>();
				foreach (ParameterInfo parInfo in methodInfo.GetParameters())
				{
					hashTableOfParameters.Add(parInfo.Name, new ParameterNameAndType(parInfo.Name, parInfo.ParameterType));//.DefaultValue);//GetDefault(.ParameterType));
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

	public static Script StaticScript;
	public static RunCodeReturnStruct RunBlockOfCode(string block)
	{
		try
		{
			if (StaticScript == null) StaticScript = new Script();
			StaticScript.Code = block;
			StaticScript.Run(null);
			return new RunCodeReturnStruct()
			{
				Success = true,
				ErrorMessage = "Command performed successfully",
				ResultingTypeString = typeof(string).AssemblyQualifiedName,
				MethodInvokeResultingObject = ""
			};
		}
		catch (Exception exc)
		{
			return new RunCodeReturnStruct()
			{
				Success = false,
				ErrorMessage = "Could not run block of code: " + exc.Message + Environment.NewLine + exc.StackTrace,
				ResultingTypeString = typeof(string).AssemblyQualifiedName,
				MethodInvokeResultingObject = ""
			};
		}
	}

	public class Script
	{
		public Script()
		{
			code = "";
		}

		public String Code
		{
			get { return code; }
			set
			{
				if (value == null) value = "";
				if (code == value) return;
				if (state != Stat.Stopped) throw new Exception("Script still running");
				Reset();
				code = value;
			}
		}

		public enum Stat
		{
			Stopped = 0,
			Running = 1,
			Step = 2,
			StepIn = 3,
			StepOut = 4,
			Halt = 5,
		}
		public Stat State
		{
			get { return state; }
			set
			{
				if (state != value)
				{
					state = value;
					if (StateChanged != null) StateChanged(this, EventArgs.Empty);
				}
			}
		}
		public bool DebugInfos
		{
			get { return flags[1]; }
			set
			{
				if (state != Stat.Stopped) throw new Exception("Script still running");
				Reset(); flags[1] = value;
			}
		}

		void marke(int i)
		{
			lastmark = i;

			if ((markers[i] & 0x10000000) == 0)
			{
				if (state == Stat.Running)
					return;

				if (state == Stat.Halt)
					return;

				if (state == Stat.Stopped)
					throw new Exception("");

				if (state == Stat.Step)
					if (level > lastlevel)
						return;

				if (state == Stat.StepOut)
					if (level >= lastlevel)
						return;
			}

			lastlevel = level;
			int ti = markers[i] & ~0x10000000, tn = markers[i + 1];

			if (DebugEvent != null) DebugEvent(this, ti, tn, "halt");

		}
		static void levels(int d) { level += d; }
		static int level, lastlevel;

		int Run(object test, Stat cmd)
		{
			if (State == Stat.Halt)
			{
				if (test != null) return 1;
				State = cmd;
				return 1;
			}
			if (State != Stat.Stopped) return 0;
			if (test != null) return 1;

			if (this.methodes == null)
			{
				try { new Compiler(this); }
				catch (CompilerException e) { Throw(e.i, e.n, e.Message); }
			}

			try
			{
				State = cmd; lastmark = -1; lastlevel = level = 0;
				afterctor = false;
				methodes[0].Invoke(this, new object[] { this });
				afterctor = true;
			}
			catch (Exception e)
			{
				for (; e.InnerException != null; e = e.InnerException) ;
				if (!String.IsNullOrEmpty(e.Message))
				{
					if ((lastmark >= 0) && (markers != null)) ShowView(markers[lastmark] & ~0x10000000, markers[lastmark + 1]);
					MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				Reset();
			}

			if (events == null)
				State = Stat.Stopped;

			return 1;
		}

		public int Run(object test) { return Run(test, Stat.Running); }
		public int Step(object test) { return Run(test, Stat.Step); }
		public int StepIn(object test) { return Run(test, Stat.StepIn); }
		public int StepOut(object test) { return Run(test, Stat.StepOut); }
		public int TestFran(object test)
		{
			this.Code = "using System.Windows.Forms; MessageBox.Show(\"Hello World!\");";
			Run(null);
			return 0;
		}
		public int Stop(object test)
		{
			if (State == Stat.Stopped) return 0;
			if (test != null) return 1;
			Reset();
			return 1;
		}

		void ShowView(int i, int n)
		{
			if (DebugEvent != null) DebugEvent(this, i, n, null);
		}
		void Throw(int i, int n, String s)
		{
			ShowView(i, n);
			throw (new Exception(s + "\r\n'" + code.Substring(i, n) + "'"));
		}

		private object getvar(int i) { return vars[i]; }
		private void setvar(int i, object p) { vars[i] = p; }

		#region EventHandling
		private DynamicMethod getmeth(int i)
		{
			DynamicMethod m1 = methodes[i];
			DynamicMethod m2 = new DynamicMethod("", m1.ReturnType, (from p in m1.GetParameters() select p.ParameterType).ToArray(), typeof(Script));
			ILGenerator il = m2.GetILGenerator();
			il.BeginExceptionBlock();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Call, m1);
			il.BeginCatchBlock(typeof(Exception));
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, typeof(Script).GetMethod("exev", BindingFlags.Instance | BindingFlags.NonPublic));
			il.EndExceptionBlock();
			il.Emit(OpCodes.Ret);
			return m2;
		}
		class EventBox
		{
			public EventBox(object p, RuntimeMethodHandle h, Delegate e)
			{
				obj = p;
				hremove = h;
				ev = e;
			}
			public void Stop()
			{
				if (ev == null) return;
				MethodBase m = MethodInfo.GetMethodFromHandle(hremove);
				m.Invoke(obj, new object[] { ev });
				ev = null;
			}
			public bool Match(object p, RuntimeMethodHandle h, Delegate e)
			{
				if ((obj == p) && (hremove == h) && (e.Target == ev.Target))
				{
					return true;
				}
				return false;
			}
			object obj;
			RuntimeMethodHandle hremove;
			public Delegate ev;
		}

		List<EventBox> events;
		private void notev(object p, RuntimeMethodHandle hremove, Delegate e, bool add)
		{
			if (add)
			{
				if (events == null) events = new List<EventBox>();
				events.Add(new EventBox(p, hremove, e));
			}
			else
			{
				for (int i = 0; i < events.Count; i++)
					if (events[i].Match(p, hremove, e))
					{
						events.RemoveAt(i--);
						if (afterctor && (events.Count == 0))
							State = Stat.Stopped;
						break;
					}
			}
		}
		private void exev()
		{
			StackTrace st = new StackTrace();
			var m = st.GetFrame(1).GetMethod();
			for (int i = 0; i < events.Count; i++)
				if (events[i].ev.Method == m)
				{
					events[i].Stop();
					events.RemoveAt(i--);
					if (afterctor && (events.Count == 0))
						State = Stat.Stopped;
					break;
				}
		}
		#endregion

		void Reset()
		{
			if (events != null) foreach (var p in events) p.Stop();
			if (state == Stat.Halt) { State = Stat.Stopped; return; }
			State = Stat.Stopped;
			methodes = null;
			vars = null;
			markers = null;
			events = null;
		}

		public int[] Markers
		{
			get { return markers; }
		}

		String code;
		Flags32 flags;
		DynamicMethod[] methodes;
		object[] vars;
		int[] markers;
		Stat state;
		int lastmark;
		public event EventHandler StateChanged;
		public delegate void DebugEventHandler(Script sender, int textpos, int textlength, string message);
		public event DebugEventHandler DebugEvent;
		bool afterctor;

		class Compiler
		{
			public Compiler(Script script)
			{
				site = script;
				Token body = new Token(script.Code);
				if (script.DebugInfos) markers = new List<int>();
				methods.Add(new DynamicMethod("", typeof(void), new Type[] { typeof(Script) }, typeof(Script)));//.Module));
				methodbodys.Add(body);
				PreScann(body);

				for (int i = 0; i < methods.Count; i++)
				{
					method = methods[i]; //Type ttt = method.GetType();
					il = method.GetILGenerator(); lastop = OpCodes.Nop;
					nparams = vars.Count;
					if (i > 0)
					{
						ParameterInfo[] a = method.GetParameters();
						for (int t = 0; t < a.Length; t++) vars.Add(new Var(a[t].Name, a[t].ParameterType, t, true));
						nparams = varscope = vars.Count;
					}
					CompileBlock(methodbodys[i], Flags.CanMakeVars | (i == 0 ? Flags.GlobalScope : Flags.None));
					if (lastop != OpCodes.Ret)
					{
						if (method.ReturnType != typeof(void))
							methodbodys[i].Except("Missing retun value");
						Emit(OpCodes.Ret);
					}
					vars.RemoveRange(nglobals, vars.Count - nglobals);
					locals.Clear();
				}
				script.methodes = methods.ToArray();
				script.vars = new object[nglobals]; //GetTypesFromVars(0, nglobals);
				script.markers = markers != null ? markers.ToArray() : null;
			}

			void PreScann(Token body)
			{
				for (Token block = body.NextBlock(); !block.IsEmpty; block = body.NextBlock())
				{
					//Console.WriteLine("'" + block + "'");
					Token token = block.NextToken();
					if (token.IsEmpty) { continue; } // {...}
					if (token.Equals("using")) { usings.Add(block.ToString()); continue; }
					if (token.Equals("var")) { continue; }
					if (token.Equals("if")) { continue; }
					if (token.Equals("for")) { continue; }
					if (token.Equals("foreach")) { continue; }
					if (block.FirstChar == '=') { continue; }
					if (block.FirstChar == '(') { continue; }

					Token name = block.NextToken();
					if (block.FirstChar == '(') // 'void Test(...'
					{
						if (block.LastChar != '}') body.Except("Missing }");
						if (name.IsEmpty) name.Except("Missing name");
						Type type = GetUsingType(token);
						block.SkipLeft();
						vars.Add(new Var(null, typeof(Script), 0));
						for (; block.FirstChar != ')'; )
						{
							Token vt = block.NextToken(); if (vt.IsEmpty) vt.Except("Missing type");
							Token vn = block.NextToken(); if (vn.IsEmpty) vn.Except("Missing name"); if (!vn.IsValidVarName) vn.Except("Invalid name");
							Type vtype = GetUsingType(vt);
							if (GetVar(vn) >= 0) vn.Except("Duplicate name");
							vars.Add(new Var(vn.ToString(), vtype, 0));
							if (block.FirstChar == ')') break;
							if (block.FirstChar != ',') vt.Except("Missing ,");
							block.SkipLeft();
						}
						block.SkipLeft();
						DynamicMethod method = new DynamicMethod(name.ToString(), type, GetVarTypes(0), typeof(Script));//.Module);
						for (int t = 1, n = vars.Count; t < n; t++) method.DefineParameter(t + 1, ParameterAttributes.None, vars[t].name);
						vars.Clear();
						methods.Add(method);
						methodbodys.Add(block);
						continue;
					}
				}
				if (!body.IsEmpty) body.Except("Missing ;");
			}

			void CompileBlock(Token body, Flags flags)
			{
				if ((body.FirstChar == '{') && (body.LastChar == '}'))
				{
					body.SkipIn();
					int ab = vars.Count; int oldvarscope = varscope; varscope = vars.Count;
					CompileBlock(body, Flags.CanMakeVars);
					vars.RemoveRange(ab, vars.Count - ab); varscope = oldvarscope;
					return;
				}

				for (Token block = body.NextBlock(); ; block = body.NextBlock())
				{
					if (block.IsEmpty)
					{
						if (!(((flags & Flags.AllowParse) != Flags.None) && !body.IsEmpty)) break;
						Token T = block; block = body; body = T;
					}
					//Console.WriteLine("'" + block + "'");
					Token segemt = block, token = segemt.NextToken();
					if (token.Equals("using")) continue;
					#region {...}
					if (token.IsEmpty && (block.FirstChar == '{')) { CompileBlock(block, Flags.None); continue; }
					#endregion
					#region try
					if (token.Equals("try"))
					{
						if (segemt.FirstChar != '{') segemt.Except("Missing {");
						Token catchblock = body.NextBlock();
						if (!catchblock.NextToken().Equals("catch")) catchblock.Except("Missing catch");

						il.BeginExceptionBlock();

						CompileBlock(segemt, Flags.None);

						il.BeginCatchBlock(typeof(Exception));

						int nv = vars.Count;
						if (catchblock.FirstChar == '(')
						{
							Token tok = catchblock.NextItem(')'); tok.SkipIn();
							Token ttype = tok.NextTokenGen();
							Type type = GetUsingType(ttype);
							if (!((type == typeof(Exception)) || type.IsSubclassOf(typeof(Exception)))) ttype.Except("Invalid type");
							if (!tok.IsValidVarName) tok.Except("Invalid name");
							int i = GetLocal(type);
							vars.Add(new Var(tok.ToString(), type, i, true));
							Emit(OpCodes.Stloc, i);
						}
						else
						{
							Emit(OpCodes.Pop);
						}
						if (catchblock.FirstChar != '{') segemt.Except("Missing {");

						CompileBlock(catchblock, Flags.None);

						il.EndExceptionBlock();

						vars.RemoveRange(nv, vars.Count - nv);

						continue;
					}
					#endregion
					#region if
					if (token.Equals("if"))
					{
						if (segemt.FirstChar != '(') segemt.Except("Missing (");
						Token tok = segemt.NextItem(')'); if (tok.IsEmpty) tok.Except("Missing value");
						tok.SkipIn(); AddMarker(block.NextItem(')'));
						Type type = Parse(tok);
						if (type != typeof(bool)) tok.Except("Illegal type");
						var iffalse = il.DefineLabel();
						Emit(OpCodes.Brfalse, iffalse);
						CompileBlock(segemt, Flags.AllowParse);
						Token elsebody = body, elseblock = elsebody.NextBlock();
						if (!elseblock.IsEmpty && elseblock.NextToken().Equals("else"))
						{
							var iftrue = il.DefineLabel();
							Emit(OpCodes.Br, iftrue);
							il.MarkLabel(iffalse);
							CompileBlock(elseblock, Flags.AllowParse);
							il.MarkLabel(iftrue); body = elsebody;
							continue;
						}
						il.MarkLabel(iffalse);
						continue;
					}
					#endregion
					#region for
					if (token.Equals("for"))
					{
						if (segemt.FirstChar != '(') segemt.Except("Missing (");
						Token tc = segemt.NextItem(')'); if (tc.IsEmpty) tc.Except("Missing value");
						tc.SkipIn(); Token ta = tc.NextItem(';'), tb = tc.NextItem(';');
						int oldvarscope = varscope; varscope = vars.Count; int ab = vars.Count;
						System.Reflection.Emit.Label lnext = il.DefineLabel();
						System.Reflection.Emit.Label old_break = lbreak; lbreak = il.DefineLabel();
						System.Reflection.Emit.Label old_conti = lconti; lconti = il.DefineLabel();
						ParseLine(ta, Flags.CanMakeVars);
						il.MarkLabel(lnext);
						AddMarker(tb);
						if (Parse(tb) != typeof(bool)) tb.Except("Type mismatch");
						Emit(OpCodes.Brfalse, lbreak);
						CompileBlock(segemt, Flags.AllowParse);
						il.MarkLabel(lconti);
						ParseLine(tc, Flags.None);
						Emit(OpCodes.Br, lnext);
						il.MarkLabel(lbreak);
						lbreak = old_break; lconti = old_conti;
						vars.RemoveRange(ab, vars.Count - ab); varscope = oldvarscope;
						continue;
					}
					#endregion
					#region foreach
					if (token.Equals("foreach"))
					{
						if (segemt.FirstChar != '(') segemt.Except("Missing (");
						Token tc = segemt.NextItem(')'); if (tc.IsEmpty) tc.Except("Missing value");
						tc.SkipIn(); Token tcc = tc;
						if (tc.SplitRight("in", ref token) < 0) tc.Except("'in' expected");
						Token var = tc.NextToken();
						Type vartype = null;
						if (!var.Equals("var")) vartype = GetUsingType(var);
						var type = Parse(token);
						var methgetenum = type.GetMethod("GetEnumerator"); if (methgetenum == null) token.Except("Invalid value");
						var enumer = methgetenum.ReturnType;
						var methCurrent = enumer.GetProperty("Current").GetGetMethod();
						var methMove = enumer.GetMethod("MoveNext");
						var methDispose = enumer.GetMethod("Dispose");
						if (methMove == null)
						{
							methMove = typeof(System.Collections.IEnumerator).GetMethod("MoveNext");
							methDispose = typeof(System.IDisposable).GetMethod("Dispose");
						}
						//methDispose = null; //!!!!
						Emit(OpCodes.Callvirt, methgetenum);
						int enumindex = GetLocal(enumer); vars.Add(new Var("", enumer, enumindex, true));
						Emit(OpCodes.Stloc, enumindex);
						if (vartype == null) vartype = type.IsArray ? type.GetElementType() : methCurrent.ReturnType;
						int index = GetLocal(vartype);
						vars.Add(new Var(tc.ToString(), vartype, index, true));
						var load = enumer.IsValueType ? OpCodes.Ldloca : OpCodes.Ldloc;
						/////////
						System.Reflection.Emit.Label old_break = lbreak; lbreak = il.DefineLabel();
						System.Reflection.Emit.Label old_conti = lconti; lconti = il.DefineLabel();
						if (methDispose != null) il.BeginExceptionBlock();
						il.MarkLabel(lconti);
						AddMarker(tcc);
						Emit(load, enumindex);
						Emit(OpCodes.Call, methMove);
						Emit(OpCodes.Brfalse, lbreak);
						Emit(load, enumindex);
						Emit(OpCodes.Call, methCurrent);
						Convert(tcc, methCurrent.ReturnType, vartype, Flags.ConvertCast);
						Emit(OpCodes.Stloc, index);
						CompileBlock(segemt, Flags.AllowParse);
						Emit(OpCodes.Br, lconti);
						il.MarkLabel(lbreak);
						if (methDispose != null)
						{
							il.BeginFinallyBlock();
							Emit(load, enumindex);
							//il.Emit(OpCodes.Constrained, enumer);
							il.Emit(OpCodes.Callvirt, methDispose);
							il.EndExceptionBlock();
						}
						lbreak = old_break;
						lconti = old_conti;
						/////////
						vars.RemoveRange(vars.Count - 2, 2);
						continue;
					}
					#endregion
					#region break
					if (token.Equals("break"))
					{
						if (lbreak == null) token.Except("No target");
						AddMarker(token);
						Emit(OpCodes.Br, lbreak);
						continue;
					}
					#endregion
					#region continue
					if (token.Equals("continue"))
					{
						if (lconti == null) token.Except("No target");
						AddMarker(token);
						Emit(OpCodes.Br, lconti);
						continue;
					}
					#endregion
					#region return
					if (token.Equals("return"))
					{
						AddMarker(block);
						if (method.ReturnType != typeof(void))
							Convert(segemt, Parse(segemt), method.ReturnType, Flags.None);
						Emit(OpCodes.Ret);
						//if(!body.IsEmpty) body.Except("Unreachebal code");
						return;
					}
					#endregion
					#region 'void Test(...'
					if (Token.IsLetter(segemt.FirstChar))
					{
						segemt.NextToken();
						if (segemt.FirstChar == '(') // 
						{
							if ((flags & Flags.GlobalScope) == Flags.None)
								segemt.Except("Local functions not supported");
							continue;
						}
					}
					#endregion
					ParseLine(block, flags);
				}
				if (!body.IsEmpty) body.Except("Missing ;");
			}

			void ParseLine(Token line, Flags flags)
			{
				if ((flags & Flags.CanMakeVars) != Flags.None)
				{
					Token test = line, item = test.NextTokenGen();

					if (item.IsValidTypeName && (Token.IsLetter(test.FirstChar) || (test.FirstChar == '[')) && (GetVar(item) < 0))
					{
						Type type = null;
						if (!item.Equals("var"))
						{
							type = GetUsingType(item);
							if (type == typeof(void)) item.Except("Invalid type");

							if (test.FirstChar == '[')
							{
								Token b = test.NextItem(']'); if (b.IsEmpty) item.Except("Missing ]");
								b.SkipIn(); if (!b.IsEmpty) b.Except("Invalid value");
								type = type.MakeArrayType();
							}

						}
						bool global = (flags & Flags.GlobalScope) != 0;

						for (item = test.NextItem(',', '<', '>'); !item.IsEmpty; item = test.NextItem(','))
						{
							Token arith = item; Token name = arith.NextToken();
							if (name.IsEmpty) name.Except("Missing name"); if (!name.IsValidVarName) name.Except("Invalid name");
							int i = GetVar(name);
							if ((i >= 0) && (global && (i < nglobals)) || (!global && (i >= varscope)))
								name.Except("Duplicate name");

							int index = -1; bool inited = false;
							if (arith.FirstChar == '=')
							{
								arith.SkipLeft();
								AddMarker(item);
								if (global)
								{
									Emit(OpCodes.Ldarg_0);
									Emit(OpCodes.Ldc_I4, nglobals);
								}

								Type ret = Parse(arith);

								if (type == null) type = ret; else Convert(item, ret, type, Flags.None);

								if (global)
								{
									if (type.IsValueType) Emit(OpCodes.Box, type);
									Emit(OpCodes.Call, typeof(Script).GetMethod("setvar", BindingFlags.Instance | BindingFlags.NonPublic));
								}
								else
								{
									Emit(OpCodes.Stloc, index = GetLocal(type));//il.DeclareLocal(type).LocalIndex);
								}
								inited = true;
							}
							else
							{
								if (type == null) item.Except("Value required");
								if (!global) index = GetLocal(type);// il.DeclareLocal(type).LocalIndex;
							}

							vars.Add(new Var(name.ToString(), type, index, inited));
							if (index == -1) nparams = ++nglobals;
						}

						return;
					}
				}

				for (Token item = line.NextItem(','); !item.IsEmpty; item = line.NextItem(','))
				{
					AddMarker(item);
					if (Parse(item, Flags.NoRet) != null)
						Emit(OpCodes.Pop);
				}
			}
			void Parse(Token body, Type type)
			{
				//Convert(body, Parse(body), type, Flags.None);
				if (Parse(body) != type) body.Except("Illegal conversion " + type + " required");
			}
			Type Parse(Token body)
			{
				return Parse(body, Flags.None);
			}
			Type PreParse(Token body)
			{
				typescan++; Type t = Parse(body); typescan--;
				return t;
			}

			Type ParseAssign(Token body, Token a, Token b, int x, Flags flags)
			{
				bool withret = (flags & Flags.NoRet) == Flags.None;
				int retindex = 0; Type res = null;
				if (a.LastChar == ']')
				{
					Token root = a.PrevItem('['); if (a.FirstChar != '[') body.Except("Missing [");
					Type type = Parse(root);
					if (!type.IsArray)
					{
						a.SkipIn(); Type indextype = PreParse(a);
						PropertyInfo pi = type.GetProperty("Item", new Type[] { indextype });
						if (pi != null)
						{
							res = pi.PropertyType;
							if (typescan == 0)
							{
								int tempobj = 0, tempindex = 0;
								if (x > 0) { Emit(OpCodes.Dup); Emit(OpCodes.Stloc, tempobj = GetLocal(typeof(object))); }
								Convert(a, Parse(a), indextype, Flags.None);
								if (x > 0)
								{
									Emit(OpCodes.Dup); Emit(OpCodes.Stloc, tempindex = GetLocal(indextype));
									Emit(OpCodes.Ldloc, tempobj);
									Emit(OpCodes.Ldloc, tempindex);
									Emit(OpCodes.Call, pi.GetGetMethod());
								}
								res = null; if (b.Length > 0) res = Parse(b); else { Emit(OpCodes.Ldc_I4_1); res = typeof(int); }
								Convert(b, res, res, Flags.None);
								if (x > 0) EmitEX(body, res, x);
								if (withret) { Emit(OpCodes.Dup); Emit(OpCodes.Stloc, retindex = GetLocal(res.IsValueType ? res : typeof(object))); }
								Emit(OpCodes.Call, pi.GetSetMethod());
							}
							if (withret) { Emit(OpCodes.Ldloc, retindex); return res; }
							return null;
						}
						body.Except("Illegal conversion");
					}

					res = type.GetElementType();
					if (typescan == 0)
					{
						int tempobj = 0, tempindex = 0;
						if (x > 0) { Emit(OpCodes.Dup); Emit(OpCodes.Stloc, tempobj = GetLocal(typeof(object))); }
						a.SkipIn(); Parse(a, typeof(int));
						if (x > 0)
						{
							Emit(OpCodes.Dup); Emit(OpCodes.Stloc, tempindex = GetLocal(typeof(int)));
							Emit(OpCodes.Ldloc, tempobj);
							Emit(OpCodes.Ldloc, tempindex);
							Emit(OpCodes.Ldelem, res);
						}
						if (b.Length > 0) Parse(b, res); else Emit(OpCodes.Ldc_I4_1);
						if (x > 0) EmitEX(body, res, x);
						if (withret) { Emit(OpCodes.Dup); Emit(OpCodes.Stloc, retindex = GetLocal(res.IsValueType ? res : typeof(object))); }
						Emit(OpCodes.Stelem, res);
					}
					if (withret) { Emit(OpCodes.Ldloc, retindex); return res; }
					return null;
				}

				Token c = b;
				if (a.SplitRight(".", ref c) >= 0)
				{
					var name = c.ToString();

					int i = GetVar(a);
					if (i >= 0 && i < nglobals)
					{
						Type xtype = vars[i].type;
						if (xtype.IsValueType)
						{
							Emit(OpCodes.Ldarg_0); Emit(OpCodes.Ldc_I4, i);
							Emit(OpCodes.Call, typeof(Script).GetMethod("getvar", BindingFlags.Instance | BindingFlags.NonPublic));
							Emit(OpCodes.Unbox_Any, xtype);
							int temp = GetLocal(xtype);
							Emit(OpCodes.Stloc, temp);
							Emit(OpCodes.Ldloca, temp);
							if (!SaveValue(ref body, ref b, name, xtype)) b.Except("Unknow property");
							Emit(OpCodes.Ldarg_0); Emit(OpCodes.Ldc_I4, i);
							Emit(OpCodes.Ldloc, temp); Emit(OpCodes.Box, xtype);
							Emit(OpCodes.Call, typeof(Script).GetMethod("setvar", BindingFlags.Instance | BindingFlags.NonPublic));
							return null;
						}
					}

					Type type = Parse(a, Flags.AllowNull | Flags.ByRef);
					if (type == null) type = TestUsingType(a);
					if (type == null) a.Except("Syntax");

					if (SaveValue(ref body, ref b, name, type))
						return null;

					#region events
					if ((x == 1) || (x == 2)) // Application.Idle += OnIdle(...
					{
						EventInfo ei = type.GetEvent(name);
						if (ei != null)
						{
							bool stat = ei.GetRemoveMethod().IsStatic;
							int i1 = 0; if (!stat) { i1 = GetLocal(type); Emit(OpCodes.Dup); Emit(OpCodes.Stloc, i1); }
							res = Parse(b);
							if (res != ei.EventHandlerType) body.Except("Type missmatch " + ei.EventHandlerType + " " + res);
							int i2 = GetLocal(typeof(object)); Emit(OpCodes.Dup); Emit(OpCodes.Stloc, i2);
							Emit(OpCodes.Call, x == 1 ? ei.GetAddMethod() : ei.GetRemoveMethod());
							///////////////
							Emit(OpCodes.Ldarg_0);
							if (stat) Emit(OpCodes.Ldnull); else Emit(OpCodes.Ldloc, i1);
							Emit(OpCodes.Ldtoken, ei.GetRemoveMethod());
							Emit(OpCodes.Ldloc, i2);
							Emit(OpCodes.Ldc_I4, x & 1);
							Emit(OpCodes.Call, typeof(Script).GetMethod("notev", BindingFlags.Instance | BindingFlags.NonPublic));//, BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic));
							///////////////
							return null;
						}
						b.Except("Unknown event");
					}
					#endregion
					c.Except("Unknown property");
				}
				else
				{
					int i = GetVar(a);
					if (i >= 0)
					{
						Type type = vars[i].type;
						if (i < nglobals)
						{
							Emit(OpCodes.Ldarg_0);
							Emit(OpCodes.Ldc_I4, i);
						}

						if (x > 0) Parse(a);

						if (b.Length > 0) res = Parse(b); else { Emit(OpCodes.Ldc_I4_1); res = typeof(int); }
						if (res != type) body.Except("Illegal conversion");
						if (x > 0) EmitEX(body, type, x);

						if (withret) { Emit(OpCodes.Dup); Emit(OpCodes.Stloc, retindex = GetLocal(res.IsValueType ? res : typeof(object))); }

						if (i < nglobals)
						{
							if (type.IsValueType) Emit(OpCodes.Box, type);
							Emit(OpCodes.Call, typeof(Script).GetMethod("setvar", BindingFlags.Instance | BindingFlags.NonPublic));
						}
						else
						{
							Emit(i < nparams ? OpCodes.Starg : OpCodes.Stloc, vars[i].index);
						}
						Var tv = vars[i]; tv.inited = true; vars[i] = tv;
						if (withret) { Emit(OpCodes.Ldloc, retindex); return res; }
						return null;
					}
				}
				a.Except("Unknown name");
				return null;
			}

			private bool SaveValue(ref Token body, ref Token value, string name, Type type)
			{
				Type res;
				var prop = type.GetProperty(name); // rect.Width = 7
				if (prop != null)
				{
					MethodInfo mi = prop.GetSetMethod();
					if (value.Length > 0) res = Parse(value); else { Emit(OpCodes.Ldc_I4_1); res = typeof(int); }
					res = UpCast(res, prop.PropertyType);
					if (res != prop.PropertyType) body.Except("Illegal conversion");
					Emit(OpCodes.Call, mi);
					return true;
				}

				var field = type.GetField(name); // P.X = 7
				if (field != null)
				{
					res = Parse(value);
					res = UpCast(res, field.FieldType);
					if (res != field.FieldType) body.Except("Illegal conversion");
					Emit(OpCodes.Stfld, field);
					return true;
				}

				// matrix.Translate(1,0,0);
				// ...

				return false;
			}
			//
			Type Parse(Token body, Flags flags)
			{
				if (!body.IsValidVarName)
				{
					#region new typeof throw
					{
						Token line = body, name = line.NextToken();
						while (line.FirstChar == '(') line.SkipIn();
						#region new
						if (name.Equals("new"))
						{
							Token typename = line.NextTokenGen();
							if (!typename.IsValidTypeName) typename.Except("Invalid type name");
							Type type = GetUsingType(typename);
							if (line.FirstChar == '[')
							{
								Token ind = line.NextItem(']'); if (ind.IsEmpty) line.Except("Missing ]");
								if (typescan == 0)
								{
									ind.SkipIn(); Parse(ind, typeof(int));
									Emit(OpCodes.Newarr, type);
								}
								return type.MakeArrayType();
							}
							if (typescan == 0)
							{
								if (line.FirstChar != '(') body.Except("Missing (");
								line.SkipIn();
								Type[] ctypes = PreScannArgs(line);

								if (ctypes.Length == 1)
									if (ctypes[0] == typeof(DynamicMethod)) // new EventHandler(...
									{
										if (typescan == 0)
										{
											Parse(line);
											Emit(OpCodes.Ldtoken, type);
											Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
											Emit(OpCodes.Ldarg_0);
											Emit(OpCodes.Call, typeof(DynamicMethod).GetMethod("CreateDelegate", new Type[] { typeof(Type), typeof(Object) }));
										}
										return type;
									}

								ConstructorInfo ctor = type.GetConstructor(ctypes);
								if (ctor == null) body.Except("Constructor");
								if (ctypes.Length > 0) ScannArgs(line, ctypes, ctor.GetParameters(), 0);
								Emit(OpCodes.Newobj, ctor);
							}
							return type;
						}
						#endregion
						#region typeof
						if (name.Equals("typeof"))
						{
							if (typescan == 0)
							{
								Emit(OpCodes.Ldtoken, GetUsingType(line));
								Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
							}
							return typeof(Type);
						}
						#endregion
						#region throw
						if (name.Equals("throw"))
						{
							if (Parse(line) != typeof(Exception)) line.Except("Missing exception");
							Emit(OpCodes.Throw);
							return null;
						}
						#endregion

					}
					#endregion
					#region '...++' '..--'
					if (body.EndsWith("++") || body.EndsWith("--"))
					{
						Token v = body; v.SkipRight(); v.SkipRight();
						Token vv = v, vvv = vv.PrevItem('#');
						if (vv.Length > 0) { if (Parse(v, Flags.NoRet) != null) Emit(OpCodes.Pop); }
						return ParseAssign(body, vv.Length > 0 ? vv : v, new Token(), body.LastChar == '+' ? 1 : 2, Flags.None);
					}
					#endregion
					Token a = body, b = a; int x;
					#region SplitRight "=;+=;-=;*=;/="
					if ((x = a.SplitRight("=;+=;-=;*=;/=;%=;|=;&=;^=", ref b)) >= 0)
						return ParseAssign(body, a, b, x, x > 0 ? Flags.NoRet : Flags.None);
					#endregion
					#region SplitRight "as;is"
					if ((x = a.SplitRight("as;is", ref b)) >= 0)
					{
						Type t1 = GetUsingType(b);
						if (typescan == 0)
						{
							Convert(body, Parse(a), t1, Flags.ConvertAs);
							if (x == 1)
							{
								Emit(OpCodes.Ldnull);
								Emit(OpCodes.Cgt);
							}
						}
						return x == 1 ? typeof(bool) : t1;
					}
					#endregion
					#region SplitRight "==;!="
					if ((x = a.SplitRight("==;!=", ref b)) >= 0)
					{
						if (typescan == 0)
						{
							Type t3 = PreParse(b);
							Type t1 = Parse(a); t1 = UpCast(t1, t3);
							Type t2 = Parse(b);
							if (t1 == typeof(string))
							{
								Emit(OpCodes.Call, t1.GetMethod(x == 1 ? "op_Inequality" : "op_Equality"));
								return typeof(bool);
							}
							Emit(OpCodes.Ceq);
							if (x == 1) { Emit(OpCodes.Ldc_I4_0); Emit(OpCodes.Ceq); }
						}
						return typeof(bool);
					}
					#endregion
					#region SplitRight "?"
					if ((x = a.SplitRight("?", ref b)) >= 0)
					{
						Token c = b.NextItem(':'); if (c.IsEmpty) body.Except("Missing :");
						Type tt1 = PreParse(c), tt2 = PreParse(b);
						if (tt1 != tt2)
						{
							if (tt1.IsValueType || tt2.IsValueType) body.Except("Illegal conversion");
							tt1 = b.Equals("null") ? tt1 : (c.Equals("null") ? tt2 : (tt1.IsSubclassOf(tt2) ? tt2 : tt1));
						}
						if (typescan == 0)
						{
							System.Reflection.Emit.Label m1 = il.DefineLabel(), m2 = il.DefineLabel();
							Parse(a, typeof(bool));
							Emit(OpCodes.Brfalse, m1);
							Parse(c);
							Emit(OpCodes.Br, m2); il.MarkLabel(m1);
							Parse(b);
							il.MarkLabel(m2);
						}
						return tt1;
					}
					#endregion
					#region SplitRight "&&;||"
					if ((x = a.SplitRight("&&;||", ref b)) >= 0)
					{
						if (typescan == 0)
						{
							System.Reflection.Emit.Label m1 = il.DefineLabel(), m2 = il.DefineLabel();
							Parse(a, typeof(bool)); Emit(x == 0 ? OpCodes.Brfalse : OpCodes.Brtrue, m1);
							Parse(b, typeof(bool)); Emit(x == 0 ? OpCodes.Brfalse : OpCodes.Brtrue, m1);
							Emit(x == 0 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
							Emit(OpCodes.Br, m2);
							il.MarkLabel(m1);
							Emit(x == 0 ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1);
							il.MarkLabel(m2);
						}
						return typeof(bool);
					}
					#endregion
					#region SplitRight "|;&;^"
					if ((x = a.SplitRight("|;&;^", ref b)) >= 0)
					{
						Convert(body, Parse(a), typeof(int), Flags.None);
						Convert(body, Parse(b), typeof(int), Flags.None);
						Emit(x == 0 ? OpCodes.Or : (x == 1 ? OpCodes.And : OpCodes.Xor));
						return typeof(int);
					}
					#endregion
					#region SplitRight "<<;>>"
					if ((x = a.SplitRight("<<;>>", ref b)) >= 0)
					{
						if (typescan == 0)
						{
							Parse(a, typeof(int)); Parse(b, typeof(int));
							Emit(x == 0 ? OpCodes.Shl : OpCodes.Shr);
						}
						return typeof(int);
					}
					#endregion
					#region SplitRight "<;>;<=;>="
					if ((x = a.SplitRight("<;>;<=;>=", ref b)) >= 0)
					{
						if (typescan == 0)
						{
							Type t3 = PreParse(b);
							Type t1 = Parse(a); t1 = UpCast(t1, t3);
							Type t2 = Parse(b);
							Convert(body, t2, t1, Flags.None); if (!t1.IsPrimitive) body.Except("Illegale conversion");
							if (x > 1)
							{
								Emit((x & 1) == 1 ? OpCodes.Clt : OpCodes.Cgt);
								Emit(OpCodes.Ldc_I4_0); Emit(OpCodes.Ceq);
								return typeof(bool);
							}
							Emit((x & 1) == 0 ? OpCodes.Clt : OpCodes.Cgt);
						}
						return typeof(bool);
					}
					#endregion
					#region SplitRight "+;-"
					if ((x = a.SplitRight("+;-", ref b)) >= 0)
					{
						Type t3 = PreParse(b);
						Type t1 = Parse(a); t1 = UpCast(t1, t3);
						if ((x == 0) && (t3 == typeof(string)) && t1.IsValueType) Emit(OpCodes.Box, t1);
						Type t2 = Parse(b);
						if ((x == 0) && ((t1 == typeof(string)) || (t2 == typeof(string))))
						{
							if (t2.IsValueType) Emit(OpCodes.Box, t2);
							Emit(OpCodes.Call, typeof(string).GetMethod("Concat", new Type[] { t1, t2 }));
							return typeof(string);
						}
						Convert(body, t2, t1, Flags.None); if (!t1.IsPrimitive) body.Except("Illegale conversion");
						Emit(x == 0 ? OpCodes.Add : OpCodes.Sub);
						return t1;
					}
					#endregion
					#region SplitRight "*;/;%"
					if ((x = a.SplitRight("*;/;%", ref b)) >= 0)
					{
						Type t3 = PreParse(b);
						Type t1 = Parse(a); t1 = UpCast(t1, t3);
						Type t2 = Parse(b);
						Convert(body, t2, t1, Flags.None);
						if (!t1.IsPrimitive)
						{
							if (x == 0)
							{
								var m = t1.GetMethod("op_Multiply");
								if (m != null) { Emit(OpCodes.Call, m); return t1; }
							}
							body.Except("Illegale conversion");
						}
						Emit(x == 0 ? OpCodes.Mul : (x == 1 ? OpCodes.Div : OpCodes.Rem));
						return t1;
					}
					#endregion
					#region "!... ~..."
					if (a[0] == '!')
					{
						a.SkipLeft(); if (Parse(a) != typeof(bool)) body.Except("Illegale conversion");
						Emit(OpCodes.Ldc_I4_0); Emit(OpCodes.Ceq);
						return typeof(bool);
					}
					if (a[0] == '~')
					{
						a.SkipLeft(); if (Parse(a) != typeof(int)) body.Except("Illegale conversion");
						Emit(OpCodes.Not);
						return typeof(int);
					}
					#endregion
					#region "++... --..."
					if (a.StartsWith("++") || a.StartsWith("--"))
					{
						a.SkipLeft(); a.SkipLeft();
						return ParseAssign(body, a, new Token(), body.FirstChar == '+' ? 1 : 2, Flags.None);
					}
					#endregion
					#region "+... -..."
					if ((a[0] == '+') || (a[0] == '-'))
					{
						b = a; b.SkipLeft();
						if (Token.IsLetter(b[0]))
						{
							Type type = Parse(b); if (!type.IsPrimitive) body.Except("Illegale conversion");
							if (a[0] == '-') Emit(OpCodes.Neg);
							return type;
						}
					}
					#endregion
					#region (int)i
					if (a.FirstChar == '(')// && (a.LastChar != ')'))
					{
						var t = a; b = t.NextItem(')');
						if (t.IsEmpty) { b.SkipIn(); return Parse(b); }
						b.SkipIn();
						if (b.IsValidTypeName)
						{
							Type t1 = GetUsingType(b);
							if (typescan == 0) Convert(body, Parse(t), t1, Flags.ConvertCast);
							return t1;
						}
					}
					#endregion
					#region SplitRight "."
					if (a.SplitRight(".", ref b) >= 0)
					{
						Type type = Parse(a, Flags.AllowNull | Flags.ByRef);
						if (type == null)
						{
							if ((type = TestUsingType(a)) == null)
							{
								if ((flags & Flags.AllowNull) == Flags.None) a.Except("Unknown type");
								return null;
							}
							return QueryObject(type, b, true, Flags.None);
						}
						return QueryObject(type, b, false, Flags.None);
					}
					#endregion
				}
				if (!Token.IsLetter(body.FirstChar))
				{
					#region "..."
					if (body.FirstChar == '"')
					{
						if (typescan == 0)
						{
							if (body.LastChar != '"') body.Except("Missing \"");
							Emit(OpCodes.Ldstr, Regex.Unescape(site.code.Substring(body.Position + 1, body.Length - 2)));
						}
						return typeof(String);
					}
					#endregion
					#region 1234
					if (body.Contains('.'))
					{
						if (body.LastChar == 'f')
						{
							if (typescan == 0)
							{
								body.SkipRight();
								float fval; if (!float.TryParse(body.ToString(),
									NumberStyles.Number | NumberStyles.AllowExponent,
									CultureInfo.InvariantCulture.NumberFormat,
									out fval)) body.Except("Invalid number");
								Emit(OpCodes.Ldc_R4, fval);
							}
							return typeof(float);
						}
						if (typescan == 0)
						{
							double dval; if (!double.TryParse(body.ToString(),
								NumberStyles.Number | NumberStyles.AllowExponent,
								CultureInfo.InvariantCulture.NumberFormat,
								out dval)) body.Except("Invalid number");
							Emit(OpCodes.Ldc_R8, dval);
						}
						return typeof(double);
					}
					if (typescan == 0)
					{
						int ival; NumberStyles style = NumberStyles.Integer;
						if ((body[0] == '0') && (body[1] == 'x'))
						{
							body.SkipLeft(); body.SkipLeft();
							style = NumberStyles.AllowHexSpecifier;
						}
						if (!int.TryParse(body.ToString(), style, null, out ival)) body.Except("Invalid number");
						Emit(OpCodes.Ldc_I4, ival);
					}
					return typeof(int);
					#endregion
				}
				#region load var
				if (body.IsValidVarName)
				{
					if (body.Equals("this")) { Emit(OpCodes.Ldarg_0); return typeof(Script); }
					if (body.Equals("true")) { Emit(OpCodes.Ldc_I4, 1); return typeof(bool); }
					if (body.Equals("false")) { Emit(OpCodes.Ldc_I4, 0); return typeof(bool); }
					if (body.Equals("null")) { Emit(OpCodes.Ldnull); return typeof(object); }

					int i = GetVar(body);
					if (i >= 0)
					{
						if (!vars[i].inited) body.Except("Never value assigned");
						Type type = vars[i].type;
						if (i < nglobals)
						{
							Emit(OpCodes.Ldarg_0);
							Emit(OpCodes.Ldc_I4, i);
							Emit(OpCodes.Call, typeof(Script).GetMethod("getvar", BindingFlags.Instance | BindingFlags.NonPublic));
							if (type.IsValueType) Emit(OpCodes.Unbox_Any, type);
							return type;
						}
						if (i < nparams)
						{
							Emit(((flags & Flags.ByRef) != 0) && type.IsValueType ? OpCodes.Ldarga : OpCodes.Ldarg, vars[i].index);
							return type;
						}
						Emit(((flags & Flags.ByRef) != 0) && type.IsValueType ? OpCodes.Ldloca : OpCodes.Ldloc, vars[i].index);
						return type;
					}
				}
				#endregion
				return QueryObject(null, body, false, flags);
			}
			//
			Type[] PreScannArgs(Token args)
			{
				typescan++; lastop = OpCodes.Nop;
				int ab = vars.Count;
				for (Token ar = args, item = ar.NextItem(','); !item.IsEmpty; item = ar.NextItem(','))
					vars.Add(new Var(null, Parse(item), -2));
				Type[] types = GetVarTypes(ab);
				vars.RemoveRange(ab, vars.Count - ab);
				typescan--;
				return types;
			}

			void ScannArgs(Token args, Type[] types, ParameterInfo[] paras, int offs)
			{
				int ntypes = types.Length;
				if (paras.Length != ntypes + offs) args.Except("Invalid param count");
				for (int i = 0; i < ntypes; i++)
				{
					Type to = paras[i + offs].ParameterType;
					Token item = args.NextItem(',');
					Type from = Parse(item);
					from = UpCast(from, to);
					if (from != to)
					{
						if (from.IsValueType && !to.IsValueType)
						{
							Emit(OpCodes.Box, from);
						}
						else
						{
							if (lastop == OpCodes.Ldnull)
								continue;
							if (!from.IsSubclassOf(to))
								item.Except("Invalid param " + (i + 1));
						}
					}
				}
			}

			Type QueryObject(Type typeon, Token body, bool isclass, Flags flags)
			{
				Type classtype = typeon == null ? typeof(Script) : typeon;
				#region function(...)
				if (body.LastChar == ')')
				{
					Token args = body, name = args.NextToken();

					if (args.FirstChar != '(') body.Except("Missing (");
					args.SkipIn();
					/*
					if (typeon == null)
					{
						if (name.Equals("typeof"))
						{
							if (typescan == 0)
							{
								Type type = GetUsingType(args);
								Emit(OpCodes.Ldtoken, type);
								Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
							}
							return typeof(Type);
						}
					}
					*/
					Type[] types = PreScannArgs(args);
					int ntypes = types.Length;

					MethodInfo methodinfo = null; int offs = 0;

					if (typeon == null)
					{
						for (int i = methods.Count - 1; i > 0; i--)
							if (name.Equals(methods[i].Name))
							{
								ParameterInfo[] mm = methods[i].GetParameters();
								if (types.Length == mm.Length - 1)
								{
									Emit(OpCodes.Ldarg_0);
									methodinfo = methods[i]; offs = 1;
									break;
								}
							}
					}

					if (methodinfo == null)
					{
						methodinfo = classtype.GetMethod(name.ToString(), types);
						if (methodinfo == null) body.Except("Unknown funtion");
					}

					if ((typeon == null) && !methodinfo.IsStatic) Emit(OpCodes.Ldarg_0);

					if (isclass)
						if (!methodinfo.IsStatic)
							body.Except("Nonstatic method");
					if (typescan == 0)
					{
						if (types.Length > 0)
							ScannArgs(args, types, methodinfo.GetParameters(), offs);
						if (markers != null) { il.Emit(OpCodes.Ldc_I4_1); il.Emit(OpCodes.Call, typeof(Script).GetMethod("levels", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)); }
						Emit(OpCodes.Call, methodinfo);
						if (markers != null) { il.Emit(OpCodes.Ldc_I4_M1); il.Emit(OpCodes.Call, typeof(Script).GetMethod("levels", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)); }
					}
					return methodinfo.ReturnType != typeof(void) ? methodinfo.ReturnType : null;
				}
				#endregion
				#region array[...]
				if (body.LastChar == ']')
				{
					Token index = body, left = index.PrevItem('['); index.SkipIn();
					Type lefttype = typeon != null ? QueryObject(typeon, left, isclass, Flags.None) : Parse(left);
					if (!lefttype.IsArray)
					{
						Type indextype = PreParse(index);
						PropertyInfo pi = lefttype.GetProperty("Item", new Type[] { indextype });
						if (pi != null)
						{
							if (typescan == 0)
							{
								Convert(index, Parse(index), indextype, Flags.None);
								Emit(OpCodes.Call, pi.GetGetMethod());
							}
							return pi.PropertyType;
						}
						index.Except("Missing array");
					}
					Type elemtype = lefttype.GetElementType();
					if (typescan == 0)
					{
						Convert(index, Parse(index), typeof(int), Flags.None);
						Emit(OpCodes.Ldelem, elemtype);
					}
					return elemtype;
				}
				#endregion
				#region property
				String s = body.ToString();
				var prop = classtype.GetProperty(s);
				if (prop != null)
				{
					MethodInfo mi = prop.GetGetMethod();
					if (isclass)
					{
						if (!mi.IsStatic) body.Except("Nonstatic property");
						Emit(OpCodes.Call, mi);
						return prop.PropertyType;
					}
					if (typescan == 0)
					{
						if ((typeon == null) && !mi.IsStatic) Emit(OpCodes.Ldarg_0);
						if (classtype.IsValueType && !classtype.IsPrimitive)
						{
							int temp = GetLocal(classtype); // 
							Emit(OpCodes.Stloc, temp);
							Emit(OpCodes.Ldloca, temp);
						}
						Emit(OpCodes.Call, mi);
					}
					return prop.PropertyType;
				}
				#endregion
				#region field
				if (typeon != null)
				{
					FieldInfo field = typeon.GetField(s);
					if (field != null)
					{
						if (field.IsLiteral)
						{
							Object val = field.GetValue(null);
							if (field.FieldType == typeof(double)) Emit(OpCodes.Ldc_R8, (double)val);
							else
								if (field.FieldType == typeof(float)) Emit(OpCodes.Ldc_R4, (float)val);
								else
									Emit(OpCodes.Ldc_I4, (int)val);
							return field.FieldType;
						}
						Emit(OpCodes.Ldfld, field);
						return field.FieldType;
					}
				}
				#endregion

				if (typeon == null)
					for (int i = 1; i < methods.Count; i++)
						if (methods[i].Name == s)
						{
							Emit(OpCodes.Ldarg_0);
							Emit(OpCodes.Ldc_I4, i);
							Emit(OpCodes.Call, typeof(Script).GetMethod("getmeth", BindingFlags.Instance | BindingFlags.NonPublic));
							return typeof(DynamicMethod);
						}

				if ((flags & Flags.AllowNull) == Flags.None) body.Except("Unknown property");
				return null;
			}
			/*
			private void UpCast(ref Type from, Type to)
			{
				if (from == to) return;
				if (!from.IsValueType || !to.IsValueType) return;
				TypeCode ta = Type.GetTypeCode(from), tb = Type.GetTypeCode(to);
				if ((int)ta > (int)tb) return;
				switch (tb)
				{
					case TypeCode.Single: Emit(OpCodes.Conv_R4); from = to; return;
					case TypeCode.Double: Emit(OpCodes.Conv_R8); from = to; return;
				}
			}
			*/
			private Type UpCast(Type from, Type to)
			{
				if (from == to) return from;
				if (!from.IsValueType || !to.IsValueType) return from;
				TypeCode ta = Type.GetTypeCode(from), tb = Type.GetTypeCode(to);
				if ((int)ta > (int)tb) return from;
				switch (tb)
				{
					case TypeCode.SByte: Emit(OpCodes.Conv_I1); return to;
					case TypeCode.Int16: Emit(OpCodes.Conv_I2); return to;
					case TypeCode.Int32: Emit(OpCodes.Conv_I4); return to;
					case TypeCode.Byte: Emit(OpCodes.Conv_U1); return to;
					case TypeCode.UInt16: Emit(OpCodes.Conv_U2); return to;
					case TypeCode.UInt32: Emit(OpCodes.Conv_U4); return to;
					case TypeCode.UInt64: Emit(OpCodes.Conv_U8); return to;
					case TypeCode.Single: Emit(OpCodes.Conv_R4); return to;
					case TypeCode.Double: Emit(OpCodes.Conv_R8); return to;
				}
				return from;

				/*
				if (from == typeof(int))
				{
					if (to == typeof(float)) { Emit(OpCodes.Conv_R4); return to; }
					if (to == typeof(double)) { Emit(OpCodes.Conv_R8); return to; }
				}
				return from;
				*/
			}

			void Convert(Token token, Type from, Type to, Flags flags)
			{
				if (from == null) token.Except("No value");
				if (from == to) return;
				if ((from.IsPrimitive || from.IsEnum) && to.IsPrimitive)
				{
					if (to == typeof(byte)) { Emit(OpCodes.Conv_I1); return; }
					if (to == typeof(short)) { Emit(OpCodes.Conv_I2); return; }
					if (to == typeof(int)) { Emit(OpCodes.Conv_I4); return; }
					if (to == typeof(bool)) { Emit(OpCodes.Conv_I4); return; }
					if (to == typeof(float)) { Emit(OpCodes.Conv_R4); return; }
					if (to == typeof(double)) { Emit(OpCodes.Conv_R8); return; }
				}
				else
				{
					if (!from.IsPrimitive && !to.IsPrimitive)
					{
						if (lastop == OpCodes.Ldnull) // ToolBar buildTask = null;
							return;

						if ((flags & Flags.ConvertAs) != Flags.None)
						{
							Emit(OpCodes.Isinst, to);
							return;
						}

						if ((flags & Flags.ConvertCast) != Flags.None)
						{
							Emit(OpCodes.Castclass, to);
							return;
						}

						if (from.IsSubclassOf(to))
						{
							Emit(OpCodes.Castclass, to);
							return;
						}
					}

					if (from.IsPrimitive && to.IsEnum)
					{
						if (from == typeof(int)) return;
					}

					if ((flags & Flags.ConvertCast) != 0)
						if (!from.IsPrimitive && to.IsPrimitive)
						{
							Emit(OpCodes.Unbox_Any, to);
							return;
						}

				}
				token.Except("Illegale conversion " + from + " to " + to);
			}

			int GetLocal(Type type)
			{
				if (typescan > 0) return -1;
				System.Diagnostics.Debug.Assert(nparams >= nglobals);

				for (int i = locals.Count - 1; i >= 0; i--)
					if (locals[i] == type)
					{
						bool inuse = false;
						for (int t = nparams; t < vars.Count; t++)
							if (vars[t].index == i)
							{
								inuse = true;
								break;
							}
						if (!inuse)
							return i;
					}

				int l = il.DeclareLocal(type).LocalIndex;
				locals.Add(type); System.Diagnostics.Debug.Assert(l == locals.Count - 1);
				return l;
			}
			int GetVar(Token s)
			{
				for (int i = vars.Count - 1; i >= 0; i--)
					if (s.Equals(vars[i].name))
						return i;
				return -1;
			}
			Type[] GetVarTypes(int ab)
			{
				int n = vars.Count - ab; if (n == 0) return Type.EmptyTypes;
				Type[] a = new Type[n];
				for (int i = 0; i < n; i++) a[i] = vars[ab + i].type;
				return a;
			}
			void EmitEX(Token token, Type type, int x)
			{
				if (!type.IsPrimitive) token.Except("Invalid operation");
				switch (x)
				{
					case 1: Emit(OpCodes.Add); break;
					case 2: Emit(OpCodes.Sub); break;
					case 3: Emit(OpCodes.Mul); break;
					case 4: Emit(OpCodes.Div); break;
					case 5: Emit(OpCodes.Rem); break;
					case 6: Emit(OpCodes.Or); break;
					case 7: Emit(OpCodes.And); break;
					case 8: Emit(OpCodes.Xor); break;
				}
			}

			struct Token
			{
				public Token(String text)
				{
					text.CopyTo(0, s = new char[n = text.Length], 0, n); i = 0;
					Flatten(); TrimLeft(); TrimRight();
				}
				public override string ToString()
				{
					return s != null ? new String(s, i, n) : "";
				}
				public bool Equals(String text)
				{
					if (text == null) return false;
					if (n != text.Length) return false;
					for (int t = 0; t < n; t++) if (text[t] != s[t + i]) return false;
					return true;
				}
				public bool Contains(char c)
				{
					for (int t = 0; t < n; t++) if (s[i + t] == c) return true;
					return false;
				}
				public Token NextBlock()
				{
					for (int ni = i + n, t = i, k = 0, k2 = 0; t < ni; t++)
					{
						char c = s[t];
						if (c == '{') { k++; continue; }
						if (c == '}')
						{
							k--;
							if (k == 0)
							{
								Token v = this; v.n = t + 1 - i; i += v.n; n -= v.n; TrimLeft();
								return v;
							}
							if (k < 0) { Token v = this; v.i = t; v.n = 1; v.Except("Missing {"); }
							continue;
						}
						if (k > 0) continue;
						if (c == '(') { k2++; continue; }
						if (c == ')') { k2--; if (k2 < 0) { Token v = this; v.i = t; v.n = 1; v.Except("Missing ("); } continue; }
						if (k2 > 0) continue;
						if (c == ';')
						{
							Token v = this; v.n = t - i; i += v.n; n -= v.n; v.TrimRight(); SkipLeft();
							return v;
						}
					}
					return new Token();
				}
				public Token NextToken()
				{
					int k = i; for (int ni = i + n; (k < ni) && IsLetterOrNumber(s[k]); k++) ;
					Token t = this; t.n = k - i; i = k; n -= t.n; TrimLeft();
					return t;
				}
				public Token NextTokenGen()
				{
					int k = i;
					for (int ni = i + n; k < ni; k++)
						if (!IsLetterOrNumber(s[k]))
						{
							int kk = k; for (; (kk < ni) && (s[kk] == ' '); kk++) ;
							if (s[kk] == '<')
							{
								for (int l = kk, kkk = 0; l < ni; l++)
								{
									if (s[l] == '<') { kkk++; continue; }
									if (s[l] == '>')
									{
										kkk--;
										if (kkk == 0)
										{
											k = l + 1;
											break;
										}
									}

								}

							}
							break;
						}
					Token t = this; t.n = k - i; i = k; n -= t.n; TrimLeft();
					return t;
				}
				public Token NextItem(char C)
				{
					return NextItem(C, '{', '}');
				}
				public Token NextItem(char C, char c1, char c2)
				{
					Token v = this;
					for (int ni = i + n, t = i, k = 0; t < ni; t++)
					{
						char c = s[t];
						if ((c == '(') || (c == '[') || (c == c1)) { k++; continue; }
						if ((c == ')') || (c == ']') || (c == c2))
						{
							k--;
							if ((c == C) && (k == 0))
							{
								t++; v.n = t - i; i += v.n - 1; n -= v.n - 1; SkipLeft();
								return v;
							}
							continue;
						}
						if (k != 0) continue;
						if (c == C)
						{
							v.n = t - i; i += v.n; n -= v.n; v.TrimRight(); SkipLeft();
							return v;
						}
					}
					i += v.n; n = 0; return v;
				}
				public Token PrevItem(char C)
				{
					Token v = this;
					for (int ni = i + n, t = ni - 1, k = 0; t >= i; t--)
					{
						char c = s[t];
						if ((c == ')') || (c == ']') || (c == '}')) { k++; continue; }
						if ((c == '(') || (c == '[') || (c == '{'))
						{
							k--;
							if ((c == C) && (k == 0))
							{
								v.n = t - i; i += v.n - 1; n -= v.n - 1; SkipLeft();
								return v;
							}
							continue;
						}
						if (k != 0) continue;
						if (((C == '#') && (IsOperator(c))) || ((C != '#') && (c == C)))
						{
							v.n = t - i; i += v.n; n -= v.n; v.TrimRight(); SkipLeft();
							return v;
						}

					}
					i += v.n; n = 0; return v;
				}
				public int SplitRight(String ss, ref Token v)
				{
					for (int k = 0, ni = i + n, t = ni - 1, ns = ss.Length; t > i; t--)
					{
						char c = s[t];
						if ((c == ')') || (c == ']') || (c == '}')) { k++; continue; }
						if ((c == '(') || (c == '[') || (c == '{')) { k--; continue; }
						if ((k > 0) || (c == ' ')) continue;
						for (int ind = 0, a = 0, b = 0; b <= ns; b++)
							if ((b == ns) || (ss[b] == ';'))
							{
								int ab = b - a; System.Diagnostics.Debug.Assert((ab == 1) || (ab == 2));
								if ((ss[b - 1] == c) && ((ab == 1) || (s[t - 1] == ss[a])))
								{
									if (ab == 1) // exceptions
									{
										if (c == '=')
										{
											char cv = s[t - 1];
											if (IsOperator(cv))
											{
												if (cv == c) t--;
												goto next;
											}
										}

										if (c == '.')
										{
											int l = t; for (; (l < ni - 1) && (s[l + 1] == ' '); l++) ;
											if (char.IsNumber(s[l + 1]))
												goto next;
										}
										if ((c == '+') || (c == '-'))
										{
											char cv = ' '; for (int l = t - 1; l >= i; l--) if (s[l] != ' ') { cv = s[l]; break; }
											if (IsOperator(cv))
												goto next;
										}

									}
									if (ab == 2) // exceptions
									{
										if (char.IsLetter(c)) // as is
										{
											if (s[t + 1] != ' ') goto next;
											if (s[t - 2] != ' ') goto next;
										}
									}
									v = this; v.i = t + 1; v.n = ni - t - 1; v.TrimLeft();
									n = t - i - ab + 1; TrimRight();
									return ind;
								}
							next: a = b + 1; ind++;
							}
					}
					return -1;
				}
				public void SkipLeft()
				{
					if (n == 0) Except("Syntax");
					i++; n--; TrimLeft();
				}
				public void SkipRight()
				{
					if (n == 0) Except("Syntax");
					n--; TrimRight();
				}
				public void SkipIn() { SkipLeft(); SkipRight(); }
				public char this[int t]
				{
					get { return n > 0 ? s[i + t] : ' '; }
				}
				public char FirstChar
				{
					get { return n > 0 ? s[i] : ' '; }
				}
				public char LastChar
				{
					get { return n > 0 ? s[i + n - 1] : ' '; }
				}
				public bool StartsWith(String ss)
				{
					int l = ss.Length; if (n < l) return false;
					for (int t = 0; t < l; t++) if (ss[t] != s[i + t]) return false;
					return true;
				}
				public bool EndsWith(String ss)
				{
					int l = ss.Length; if (n < l) return false;
					for (int t = 0, x = i + n - l; t < l; t++) if (ss[t] != s[x + t]) return false;
					return true;
				}

				public bool IsEmpty
				{
					get { return n == 0; }
				}
				public bool IsValidVarName
				{
					get
					{
						if (n == 0) return false;
						if (!(IsLetter(s[i]) || (s[i] == '_'))) return false;
						for (int t = 1; t < n; t++) if (!(IsLetterOrNumber(s[i + t]))) return false;
						return true;
					}
				}
				public bool IsValidTypeName
				{
					get
					{
						if (IsValidVarName) return true;
						if (LastChar == '>')
						{
							Token a = this, b = a.NextToken();
							return (a.FirstChar == '<') && b.IsValidVarName;
						}
						return false;
					}
				}
				public int Length
				{
					get { return n; }
					set { n = value; }
				}
				public int Position
				{
					get { return i; }
				}
				void TrimLeft()
				{
					for (; (n > 0) && (s[i] == ' '); i++, n--) ;
				}
				void TrimRight()
				{
					for (; (n > 0) && (s[i + n - 1] == ' '); n--) ;
				}
				void Flatten()
				{
					for (int k = 0; k < n; k++)
					{
						char c = s[k];
						if (k < n - 1)
						{
							if ((c == '\'') || (c == '"'))
							{
								int t = k + 1;
								for (; (t < n) && (s[t] != c); t++)
								{
									if (s[t] < 32) { Token v = this; v.i = k; v.n = t - k; v.Except("Missing " + c); }
									if (s[t] == '\\') s[t++] = ' ';
									s[t] = ' ';
								}
								k = t;
								continue;
							}
							if (c == '/')
							{
								if (s[k + 1] == '/')
								{
									for (; k < n; )
									{
										if (s[k] == 13) { s[k] = ' '; break; }
										s[k++] = ' ';
									}
									continue;
								}
								if (s[k + 1] == '*')
								{
									int t = k + 2; for (; (t < n) && !((s[t - 2] == '*') && (s[t - 1] == '/')); t++) ;
									for (; k < t; ) s[k++] = ' '; k--;
									continue;
								}
							}
						}
						if (c < 32) s[k] = ' ';
					}
				}
				public void Except(String s)
				{
					throw new CompilerException(i, n, s);
				}
				public static bool IsLetter(char c)
				{
					return char.IsLetter(c) || (c == '_');
				}
				public static bool IsLetterOrNumber(char c)
				{
					return IsLetter(c) || char.IsNumber(c);
				}
				static bool IsOperator(char c)
				{
					switch (c)
					{
						case '=':
						case '+':
						case '-':
						case '*':
						case '/':
						case '%':
						case '^':
						case '~':
						case '|':
						case '&':
						case '<':
						case '>':
						case '!':
							return true;
					}
					return false;
				}
				char[] s; int i, n;
			}

			[Flags]
			enum Flags
			{
				None = 0,
				GlobalScope = 1,
				CanMakeVars = 2,
				AllowParse = 4,
				ConvertAs = 8,
				ConvertCast = 16,
				AllowNull = 32,
				NoRet = 64,
				ByRef = 128,
			}

			struct Var
			{
				public Var(String n, Type t, int i) { name = n; type = t; index = i; inited = false; }
				public Var(String n, Type t, int i, bool inited) { name = n; type = t; index = i; this.inited = inited; }
				public String name;
				public Type type;
				public int index;
				public bool inited;
			}

			void Emit(OpCode op) { lastop = op; if (typescan != 0) return; TraceOpCode(op, null); il.Emit(op); }
			void Emit(OpCode op, byte v) { lastop = op; if (typescan != 0) return; TraceOpCode(op, v); il.Emit(lastop = op, v); }
			void Emit(OpCode op, ushort v) { lastop = op; if (typescan != 0) return; TraceOpCode(op, v); il.Emit(lastop = op, v); }
			void Emit(OpCode op, int i)
			{
				if (typescan != 0) return;
				if (op == OpCodes.Ldc_I4)
					switch (i)
					{
						case 0: Emit(OpCodes.Ldc_I4_0); return;
						case 1: Emit(OpCodes.Ldc_I4_1); return;
						case 2: Emit(OpCodes.Ldc_I4_2); return;
						case 3: Emit(OpCodes.Ldc_I4_3); return;
						case 4: Emit(OpCodes.Ldc_I4_4); return;
						case 5: Emit(OpCodes.Ldc_I4_5); return;
						case 6: Emit(OpCodes.Ldc_I4_6); return;
						case 7: Emit(OpCodes.Ldc_I4_7); return;
						case 8: Emit(OpCodes.Ldc_I4_8); return;
						case -1: Emit(OpCodes.Ldc_I4_M1); return;
						default: lastop = op; TraceOpCode(op, i); il.Emit(lastop = op, i); return;
					}
				if (op == OpCodes.Ldarg)
					switch (i)
					{
						case 0: Emit(OpCodes.Ldarg_0); return;
						case 1: Emit(OpCodes.Ldarg_1); return;
						case 2: Emit(OpCodes.Ldarg_2); return;
						case 3: Emit(OpCodes.Ldarg_3); return;
						default: { byte c = (byte)i; if (i == c) { Emit(OpCodes.Ldarg_S, c); return; } Emit(OpCodes.Ldarg, (ushort)i); return; }
					}
				if (op == OpCodes.Ldloc)
					switch (i)
					{
						case 0: Emit(OpCodes.Ldloc_0); return;
						case 1: Emit(OpCodes.Ldloc_1); return;
						case 2: Emit(OpCodes.Ldloc_2); return;
						case 3: Emit(OpCodes.Ldloc_3); return;
						default: { byte c = (byte)i; if (i == c) { Emit(OpCodes.Ldloc_S, c); return; } Emit(OpCodes.Ldloc, (ushort)i); return; }
					}
				if (op == OpCodes.Stloc)
					switch (i)
					{
						case 0: Emit(OpCodes.Stloc_0); return;
						case 1: Emit(OpCodes.Stloc_1); return;
						case 2: Emit(OpCodes.Stloc_2); return;
						case 3: Emit(OpCodes.Stloc_3); return;
						default: { byte c = (byte)i; if (i == c) { Emit(OpCodes.Stloc_S, c); return; } Emit(OpCodes.Stloc, (ushort)i); return; }
					}
				if (op == OpCodes.Starg) { byte c = (byte)i; if (i == c) { Emit(OpCodes.Starg_S, c); return; } Emit(OpCodes.Starg, (ushort)i); return; }
				if (op == OpCodes.Ldarga) { byte c = (byte)i; if (i == c) { Emit(OpCodes.Ldarga_S, c); return; } Emit(OpCodes.Ldarga, (ushort)i); return; }
				if (op == OpCodes.Ldloca) { byte c = (byte)i; if (i == c) { Emit(OpCodes.Ldloca_S, c); return; } Emit(OpCodes.Ldloca, (ushort)i); return; }
			}
			void Emit(OpCode op, Type v) { lastop = op; if (typescan != 0) return; TraceOpCode(op, v); il.Emit(op, v); }
			void Emit(OpCode op, FieldInfo v) { lastop = op; if (typescan != 0) return; TraceOpCode(op, v); il.Emit(op, v); }
			void Emit(OpCode op, String v) { lastop = op; if (typescan != 0) return; TraceOpCode(op, v); il.Emit(op, v); }
			void Emit(OpCode op, MethodInfo method)
			{
				lastop = op; if (typescan != 0) return;
				TraceOpCode(op, method);
				il.Emit(op, method);
			}
			void Emit(OpCode op, float v) { lastop = op; if (typescan != 0) return; TraceOpCode(op, v); il.Emit(op, v); }
			void Emit(OpCode op, double v) { lastop = op; if (typescan != 0) return; TraceOpCode(op, v); il.Emit(op, v); }
			void Emit(OpCode op, System.Reflection.Emit.Label v) { lastop = op; if (typescan != 0) return; TraceOpCode(op, v); il.Emit(op, v); }
			void Emit(OpCode op, ConstructorInfo v) { lastop = op; if (typescan != 0) return; TraceOpCode(op, v); il.Emit(op, v); }

			[Conditional("TraceOpCode")]
			void TraceOpCode(OpCode op, Object p)
			{
				Console.WriteLine("    " + op + (p != null ? " " + p.ToString() : ""));
			}

			static String[] asmnames;
			static Dictionary<String, Type> typecach = new Dictionary<String, Type>();
#if(true)
			Type GetUsingTypeV(String typename)
			{
				Type type;
				if (typecach.TryGetValue(typename, out type))
					return type;

				if (asmnames == null)
				{
					Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
					AssemblyName[] asms = asm.GetReferencedAssemblies();
					asmnames = new string[asms.Length + 1];
					asmnames[0] = asm.FullName;
					for (int i = 0; i < asms.Length; i++) asmnames[i + 1] = asms[i].FullName;
				}

				for (int i = usings.Count - 1; i >= 0; i--)
					for (int t = 0; t < asmnames.Length; t++)
					{
						type = Type.GetType(usings[i] + "." + typename + ", " + asmnames[t]);
						if (type != null)
						{
							typecach.Add(typename, type);
							return type;
						}
					}
				return null;
			}
#else
      Type _GetUsingTypeV(String typename)
      {

        Type type = Type.GetType(typename);
        if (type != null)
          return type;

        for (int i = usings.Count-1; i >= 0; i--)
        {
          type = Type.GetType(usings[i] + "." + typename);
          if (type != null)
            return type;
        }
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = usings.Count - 1; i >= 0; i--)
          for (int k = 0, l = assemblies.Length; k < l; k++)
          {
            
            type = assemblies[k].GetType(usings[i] + "." + typename);
            if (type != null)
              return type;
          }

        return null;
      }
#endif
			Type TestUsingType(Token token)
			{
				if (token.LastChar == '>')
				{
					Token name = token.NextToken(); token.SkipIn();
					List<Type> types = new List<Type>();
					for (Token t = token.NextItem(','); !t.IsEmpty; t = token.NextItem(','))
						types.Add(GetUsingType(t));
					Type type = GetUsingTypeV(name.ToString() + "`" + types.Count);
					if (type == null) name.Except("Unknown type");
					type = type.MakeGenericType(types.ToArray());
					if (type == null) name.Except("Arguments");
					return type;
				}

				if (token.Equals("void")) return typeof(void);
				if (token.Equals("int")) return typeof(int);
				if (token.Equals("float")) return typeof(float);
				if (token.Equals("double")) return typeof(double);
				if (token.Equals("bool")) return typeof(bool);
				if (token.Equals("byte")) return typeof(byte);
				if (token.Equals("string")) return typeof(string);
				if (token.Equals("object")) return typeof(object);

				return GetUsingTypeV(token.ToString());
			}
			Type GetUsingType(Token token)
			{
				Type t = TestUsingType(token);
				if (t == null) token.Except("Unknown type");
				return t;
			}

			void AddMarker(Token token)
			{
#if TraceOpCode
        Console.WriteLine("  " + token.ToString());
#endif
				if (markers != null)
				{
					if (typescan != 0) return;
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Ldc_I4, markers.Count);
					il.Emit(OpCodes.Call, typeof(Script).GetMethod("marke", BindingFlags.Instance | BindingFlags.NonPublic));
					markers.Add(token.Position);
					markers.Add(token.Length);
				}
			}

			Script site;
			List<string> usings = new List<String>();
			List<DynamicMethod> methods = new List<DynamicMethod>();
			List<Token> methodbodys = new List<Token>();
			List<Var> vars = new List<Var>();
			List<Type> locals = new List<Type>();
			List<int> markers;

			ILGenerator il;
			DynamicMethod method;
			int nglobals, nparams, varscope, typescan;
			OpCode lastop;
			System.Reflection.Emit.Label lbreak;
			System.Reflection.Emit.Label lconti;

		}

		class CompilerException : Exception
		{
			public CompilerException(int i, int n, String s)
				: base(s)
			{
				this.i = i;
				this.n = n;
			}
			public int i, n;
		}

		struct Flags32
		{
			public bool this[int i]
			{
				get { return (flags & i) == i; }
				set { flags = (flags & ~i) | (value ? i : 0); }
			}
			public int Value
			{
				get { return flags; }
				set { flags = value; }
			}
			internal int GetRange(int mask)
			{
				return (flags & mask) >> LSB(mask);
			}
			internal void SetRange(int mask, int v)
			{
				flags = (flags & ~mask) | ((v << LSB(mask)) & mask);
			}
			int LSB(int n)
			{
				int pos = 31;
				if ((n & 0xffff) != 0) pos -= 16; else n >>= 16;
				if ((n & 0x00ff) != 0) pos -= 8; else n >>= 8;
				if ((n & 0x000f) != 0) pos -= 4; else n >>= 4;
				if ((n & 0x0003) != 0) pos -= 2; else n >>= 2;
				if ((n & 0x0001) != 0) pos -= 1;
				return pos;
			}
			int flags;
		}

	}
}

public class ClassWithAllAssemblies : INotifyPropertyChanged
{
	private AssemblyClass[] _assemblies;

	public AssemblyClass[] Assemblies { get { return _assemblies ?? (_assemblies = System.AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.FullName).Select(a => new AssemblyClass(a)).ToArray()); } }
	private System.Windows.Visibility _visibility;
	public System.Windows.Visibility Visibility { get { return _visibility; } set { _visibility = value; OnPropertyChanged("Visibility"); } }

	public ClassWithAllAssemblies() { }

	public event PropertyChangedEventHandler PropertyChanged = new PropertyChangedEventHandler(delegate { });
	public void OnPropertyChanged(string propertyName) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
}

public class AssemblyClass : INotifyPropertyChanged
{
	private ClassWithStaticMethods[] _types;

	public Assembly ThisAssembly { get; private set; }
	public ClassWithStaticMethods[] Types { get { return _types ?? (_types = ThisAssembly.GetTypes().OrderBy(t => t.Name).Select(t => new ClassWithStaticMethods(t)).ToArray()); } }
	private System.Windows.Visibility _visibility;
	public System.Windows.Visibility Visibility { get { return _visibility; } set { _visibility = value; OnPropertyChanged("Visibility"); } }

	public AssemblyClass(Assembly ThisAssembly)
	{
		this.ThisAssembly = ThisAssembly;
	}

	public event PropertyChangedEventHandler PropertyChanged = new PropertyChangedEventHandler(delegate { });
	public void OnPropertyChanged(string propertyName) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
}

public class ClassWithStaticMethods : INotifyPropertyChanged
{
	private MethodClass[] _methods;

	public Type ClassType { get; private set; }
	public MethodClass[] Methods { get { return _methods ?? (_methods = ClassType.GetMethods().Where(mi => mi.IsStatic && mi.IsPublic).OrderBy(mi => mi.Name).Select<MethodInfo, MethodClass>(mi => new MethodClass(mi, this)).ToArray()); } }
	//public MethodInfo SelectedMethod { get; private set; }
	private System.Windows.Visibility _visibility;
	public System.Windows.Visibility Visibility { get { return _visibility; } set { _visibility = value; OnPropertyChanged("Visibility"); } }

	public ClassWithStaticMethods(Type ClassType)
	{
		this.ClassType = ClassType;
	}

	public event PropertyChangedEventHandler PropertyChanged = new PropertyChangedEventHandler(delegate { });
	public void OnPropertyChanged(string propertyName) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
}

public class MethodClass : INotifyPropertyChanged
{
	private ParameterInfo[] _parameters;

	public ClassWithStaticMethods ParentClass { get; private set; }
	public MethodInfo Methodinfo { get; private set; }
	public ParameterInfo[] Parameters { get { return _parameters ?? (_parameters = Methodinfo.GetParameters()); } }
	public DictionaryPropertyGridAdapter PropertyGridAdapter { get; private set; }
	//private System.Windows.Visibility _visibility;
	public System.Windows.Visibility Visibility { get { return System.Windows.Visibility.Visible; } }//return _visibility; } set { _visibility = value; OnPropertyChanged("Visibility"); } }

	private bool _isselected;
	public bool IsSelected { get { return _isselected; } set { _isselected = value; OnPropertyChanged("IsSelected"); } }

	public bool LastSuccess { get; private set; }
	public string LastFailureErrorMessage { get; private set; }
	public string LastResultTypeString { get; private set; }
	public object LastResultObject { get; private set; }
	public DateTime LastResultTime { get; private set; }
	private DynamicCodeInvoking.RunCodeReturnStruct _lastresult;
	public DynamicCodeInvoking.RunCodeReturnStruct LastResult
	{
		get { return _lastresult; }
		private set
		{
			_lastresult = value; OnPropertyChanged("LastResult");
			LastResultTypeString = value.ResultingTypeString; OnPropertyChanged("LastResultTypeString");
			LastResultObject = value.MethodInvokeResultingObject; OnPropertyChanged("LastResultObject");
			LastResultTime = DateTime.Now; OnPropertyChanged("LastResultTime");
			LastSuccess = value.Success; OnPropertyChanged("LastSuccess");
			LastFailureErrorMessage = value.ErrorMessage; OnPropertyChanged("LastFailureErrorMessage");
		}
	}

	public object GetParameterValue(ParameterInfo parameterInfo)
	{
		return PropertyGridAdapter._dictionary[parameterInfo.Name].Value;
	}

	public MethodClass(MethodInfo Methodinfo, ClassWithStaticMethods ParentClass)
	{
		this.ParentClass = ParentClass;
		this.Methodinfo = Methodinfo;
		Dictionary<string, ParameterNameAndType> tmpdict = new Dictionary<string, ParameterNameAndType>();
		foreach (ParameterInfo pi in Parameters)
			tmpdict.Add(pi.Name, new ParameterNameAndType(pi.Name, pi.ParameterType));
		PropertyGridAdapter = new DictionaryPropertyGridAdapter(tmpdict);
	}
	public MethodClass(Dictionary<string, ParameterNameAndType> Parameters, string ParentClass_AssemblyQualifiedName, string MethodName)
	{
		PropertyGridAdapter = new DictionaryPropertyGridAdapter(Parameters);
		//Must improve the way to obtain a type
		this.ParentClass = new ClassWithStaticMethods(DynamicCodeInvoking.GetTypeFromSimpleString(ParentClass_AssemblyQualifiedName.Substring(0, ParentClass_AssemblyQualifiedName.IndexOf(','))));

		bool MethodFound = false;
		foreach (var m in this.ParentClass.Methods)
			if (m.Methodinfo.Name == MethodName)
			{
				if (m.Parameters.Length != Parameters.Count)
					continue;

				var tmpkeys = Parameters.Keys.ToArray();
				bool ParameterMismatch = false;
				for (int i = 0; i < Parameters.Count; i++)
					if (Parameters[tmpkeys[i]].Name != m.Parameters[i].Name
						|| Parameters[tmpkeys[i]].type != m.Parameters[i].ParameterType)
						ParameterMismatch = true;
				if (!ParameterMismatch)
				{
					MethodFound = true;
					this.Methodinfo = m.Methodinfo;
					break;
				}
			}
		if (!MethodFound)
			UserMessages.ShowWarningMessage("Unable to find metdho for " + MethodName);
	}

	public DynamicCodeInvoking.RunCodeReturnStruct Run()
	{
		LastResult =
			DynamicCodeInvoking.RunSelectedFunction(
			this.PropertyGridAdapter._dictionary,
			this.ParentClass.ClassType.AssemblyQualifiedName,
			this.Methodinfo.Name,
			false);
		return LastResult;
	}

	public override string ToString()
	{
		return Methodinfo.ToString();
	}

	public event PropertyChangedEventHandler PropertyChanged = new PropertyChangedEventHandler(delegate { });
	public void OnPropertyChanged(string propertyName) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
}