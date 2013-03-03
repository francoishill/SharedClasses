using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace SharedClasses
{
	public static class ReflectionInterop
	{
		/// <summary>
		/// Do an operation on a instance of a class
		/// </summary>
		/// <typeparam name="T">The type of class to perform the operation on.</typeparam>
		/// <param name="instance">The instace of the type T.</param>
		/// <param name="propertiesOrFieldsAsExpressions">List of expressions which are simply a field/property, an example of one would be: inst => inst.PersonName</param>
		/// <param name="actionOnPropertyInfoWithValue">Action to perform on these properties, where its parameters will be the Instance, PropertyInfo, and propertyValue.</param>
		public static void DoForeachPropertOrField<T>(T instance, IEnumerable<Expression<Func<T, object>>> propertiesOrFieldsAsExpressions, Action<T, MemberInfo, object> actionOnPropertyInfoWithValue)
		//expr.Body.ToString().Substring(expectedStartString.Length)where T : new()
		{
			if (instance == null)
				throw new Exception("Instance may not be null for 'DoForeachPropertOrField'");

			foreach (var expr in propertiesOrFieldsAsExpressions)
			{
				Expression expressionToUse = expr.Body;

				if (expressionToUse is UnaryExpression)
				{
					Match insideConvertMatch = Regex.Match(expressionToUse.ToString(), @"(?<=Convert\()[a-zA-Z0-9_]+\.[a-zA-Z0-9_]+(?=\))");
					if (!insideConvertMatch.Success)
						throw new Exception("Invalid expression for 'DoForeachPropertOrField': " + expressionToUse.ToString());
					var expr2 = expressionToUse as UnaryExpression;
					expressionToUse = expr2.Operand;
				}

				if (expr.Parameters.Count != 1)
					throw new Exception("Each expression should have exactly one parameter.");

				string expectedStartString = string.Format("{0}.", expr.Parameters[0].Name);
				if (expressionToUse.GetType().Name.Equals("PropertyExpression")
					&& expressionToUse.ToString().StartsWith(expectedStartString))
				{
					string propertyName = expressionToUse.ToString().Substring(expectedStartString.Length);
					var pi = instance.GetType().GetProperty(propertyName);
					if (pi == null)
						throw new Exception("Cannot obtain property '" + propertyName + "' in instance of " + instance.GetType());

					object pValue = pi.GetValue(instance, null);
					actionOnPropertyInfoWithValue(instance, pi, pValue);
				}
				else if (expressionToUse.GetType().Name.Equals("FieldExpression")
					&& expressionToUse.ToString().StartsWith(expectedStartString))
				{
					string fieldName = expressionToUse.ToString().Substring(expectedStartString.Length);
					var fi = instance.GetType().GetField(fieldName);
					if (fi == null)
						throw new Exception("Cannot obtain field '" + fieldName + "' in instance of " + instance.GetType());
					object fValue = fi.GetValue(instance);
					actionOnPropertyInfoWithValue(instance, fi, fValue);
				}
				else
					throw new Exception("Body of expression is not a valid type, should be PropertyExpression or FieldExpression");
			}
		}

		/// <summary>
		/// Gets the Display Name for the property descriptor passed in
		/// </summary>
		/// <param name="descriptor"></param>
		/// <returns></returns>
		public static string GetPropertyDisplayName(object descriptor)
		{

			PropertyDescriptor pd = descriptor as PropertyDescriptor;
			if (pd != null)
			{
				// Check for DisplayName attribute and set the column header accordingly
				DisplayNameAttribute displayName = pd.Attributes[typeof(DisplayNameAttribute)] as DisplayNameAttribute;
				if (displayName != null && displayName != DisplayNameAttribute.Default)
				{
					return displayName.DisplayName;
				}

			}
			else
			{
				PropertyInfo pi = descriptor as PropertyInfo;
				if (pi != null)
				{
					// Check for DisplayName attribute and set the column header accordingly
					Object[] attributes = pi.GetCustomAttributes(typeof(DisplayNameAttribute), true);
					for (int i = 0; i < attributes.Length; ++i)
					{
						DisplayNameAttribute displayName = attributes[i] as DisplayNameAttribute;
						if (displayName != null && displayName != DisplayNameAttribute.Default)
						{
							return displayName.DisplayName;
						}
					}
				}
			}
			return null;
		}

		public static string GetPropertyDescription(object descriptor)
		{
			PropertyDescriptor pd = descriptor as PropertyDescriptor;
			if (pd != null)
			{
				// Check for DisplayName attribute and set the column header accordingly
				DescriptionAttribute displayName = pd.Attributes[typeof(DescriptionAttribute)] as DescriptionAttribute;
				if (displayName != null && displayName != DescriptionAttribute.Default)
				{
					return displayName.Description;
				}

			}
			else
			{
				PropertyInfo pi = descriptor as PropertyInfo;
				if (pi != null)
				{
					// Check for DisplayName attribute and set the column header accordingly
					Object[] attributes = pi.GetCustomAttributes(typeof(DescriptionAttribute), true);
					for (int i = 0; i < attributes.Length; ++i)
					{
						DescriptionAttribute displayName = attributes[i] as DescriptionAttribute;
						if (displayName != null && displayName != DescriptionAttribute.Default)
						{
							return displayName.Description;
						}
					}
				}
			}
			return null;
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
				{
					try
					{
						foreach (Type type in assembly.GetTypes())
						{
							try
							{
								AllUniqueSimpleTypesInCurrentAssembly.Add(type);
							}
							catch { }
						}
					}
					catch { }
				}
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