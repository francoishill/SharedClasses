using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

namespace SharedClasses
{
	public static class ReflectionInterop
	{
		public static class DynamicTypeBuilder
		{
			public static void AddMethodDynamically(TypeBuilder myTypeBld,
									string mthdName,
									Type[] mthdParams,
									Type returnType,
									string mthdAction)
			{
				MethodBuilder myMthdBld = myTypeBld.DefineMethod(
														mthdName,
														MethodAttributes.Public |
														MethodAttributes.Static,
														returnType,
														mthdParams);
				ILGenerator ILout = myMthdBld.GetILGenerator();
				int numParams = mthdParams.Length;
				for (byte x = 0; x < numParams; x++)
				{
					ILout.Emit(OpCodes.Ldarg_S, x);
				}
				if (numParams > 1)
				{
					for (int y = 0; y < (numParams - 1); y++)
					{
						switch (mthdAction)
						{
							case "A": ILout.Emit(OpCodes.Add);
								break;
							case "M": ILout.Emit(OpCodes.Mul);
								break;
							default: ILout.Emit(OpCodes.Add);
								break;
						}
					}
				}
				ILout.Emit(OpCodes.Ret);
			}

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

			public static TypeBuilder GetTypeBuilder(string TypeName)
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

		//public static Type getEventArgsType(EventInfo eventType)
		//{
		//    Type t = eventType.EventHandlerType;
		//    MethodInfo m = t.GetMethod("Invoke");

		//    var parameters = m.GetParameters();
		//    return parameters[1].ParameterType;
		//}

		//public static void HookIntoStaticEvent(Type mainClassType, EventInfo[] evts, string StaticEventName)
		//{
		//    foreach (var ev in evts)
		//    {
		//        Type argsType = getEventArgsType(ev);

		//        MethodInfo hook = mainClassType.GetMethod("On" + StaticEventName);
		//        MethodInfo boundEventhandler = hook.MakeGenericMethod(new[] { argsType });

		//        Delegate handler = Delegate.CreateDelegate(ev.EventHandlerType, boundEventhandler);

		//        ev.AddEventHandler(null, handler);
		//    }
		//}
	}
}