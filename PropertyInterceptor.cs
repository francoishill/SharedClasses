using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using PropertyInterceptor;

namespace PropertyInterceptor
{
	public class UnsetPropertyDetail
	{
		public string PropertyName { get; private set; }
		public PropertyInfo PropertyInfo { get; private set; }
		public string DeclaringClassName { get { return PropertyInfo != null ? PropertyInfo.DeclaringType.Name : ""; } }
		public string UserPrompt { get; private set; }

		public UnsetPropertyDetail(string PropertyName, PropertyInfo PropertyInfo, string UserPrompt)
		{
			this.PropertyName = PropertyName;
			this.PropertyInfo = PropertyInfo;
			this.UserPrompt = UserPrompt;
		}

		public override string ToString()
		{
			return PropertyInfo.DeclaringType.Name + "." + PropertyName;
		}
	}

	public static class StaticPropertyInterceptor
	{
		private static bool BypassUserPrompt = false;
		public static bool IsBypassingUserPromptEnabled() { return BypassUserPrompt; }

		public static void RunMethodAndBypassUserPromptForGetMethods(Action method)
		{
			try
			{
				BypassUserPrompt = true;
				method();
			}
			finally
			{
				BypassUserPrompt = false;
			}
		}

		public static bool UnsetPropertiesContains(string propertyName)
		{
			foreach (UnsetPropertyDetail upd in UnsetProperties)
				if (upd.PropertyName == propertyName)
					return true;
			return false;
		}
		public static void UnsetPropertiesRemove(string propertyName)
		{
			for (int i = UnsetProperties.Count - 1; i >= 0; i--)
				if (UnsetProperties[i].PropertyName == propertyName)
					UnsetProperties.RemoveAt(i);
		}

		private static ObservableCollection<UnsetPropertyDetail> unsetProperties;
		public static ObservableCollection<UnsetPropertyDetail> UnsetProperties
		{
			get
			{
				if (unsetProperties == null)
					unsetProperties = new ObservableCollection<UnsetPropertyDetail>();
				return unsetProperties;
			}
		}
	}
}

public interface IInterceptorNotifiable
{
	void OnPropertySet(string propertyName);
	void OnPropertyGet(string propertyName);
}

/// <summary>
/// A simple RealProxy based property interceptor
/// Will call OnPropertyChanged whenever and property on the child object is changed
/// </summary>
public class Interceptor<T> where T : MarshalByRefObject, IInterceptorNotifiable, new()
{

	class InterceptorProxy : RealProxy
	{
		T proxy;
		T target;
		//EventHandler<PropertyChangedEventArgs> OnPropertyChanged;

		public InterceptorProxy(T target)
			: base(typeof(T))
		{
			this.target = target;
		}

		public override object GetTransparentProxy()
		{
			proxy = (T)base.GetTransparentProxy();
			return proxy;
		}

		public bool IsOfTypeOrNullableType(Type typeOfObject, Type typeToCheck)
		{
			return typeOfObject == typeToCheck || (Nullable.GetUnderlyingType(typeOfObject) != null && Nullable.GetUnderlyingType(typeOfObject) == typeToCheck);
		}

		public override IMessage Invoke(IMessage msg)
		{
			IMethodCallMessage call = msg as IMethodCallMessage;
			if (call != null)
			{
			gotoRetryAfterUserSet:
				var result = InvokeMethod(call);
				if (call.MethodName.StartsWith("set_"))
				{
					string propName = call.MethodName.Substring(4);
					target.OnPropertySet(propName);
				}
				else if (call.MethodName.StartsWith("get_"))
				{
					string propName = call.MethodName.Substring(4);
					target.OnPropertyGet(propName);

					if (result.ReturnValue == null)
					{
						PropertyInfo pi = target.GetType().GetProperty(propName);
						string propOwnLongName = pi.DeclaringType.Name + "." + propName;
						//if (pi.PropertyType.IsEnum || (Nullable.GetUnderlyingType(pi.PropertyType) != null && Nullable.GetUnderlyingType(pi.PropertyType).IsEnum))
						//	System.Windows.Forms.MessageBox.Show("Enum = " + propName);
						SettingAttribute att = pi.GetCustomAttribute(typeof(SettingAttribute)) as SettingAttribute;
						string UserPrompt = "Please enter value for " + propName;
						bool IsPasswordDoNotSave = false;
						if (att != null)
						{
							if (!string.IsNullOrWhiteSpace(att.UserPrompt))
								UserPrompt = att.UserPrompt;
							IsPasswordDoNotSave = att.PasswordPromptEveryTime;
						}

						object tmpUserAnswer = null;
						if (!StaticPropertyInterceptor.IsBypassingUserPromptEnabled())
						{
							if (pi.PropertyType.IsEnum || (Nullable.GetUnderlyingType(pi.PropertyType) != null && Nullable.GetUnderlyingType(pi.PropertyType).IsEnum))
							{
								if (att == null || string.IsNullOrWhiteSpace(att.UserPrompt))
									UserPrompt = "Please pick one of the following options for " + propName;
								tmpUserAnswer = UserMessages.PickItem(
									pi.PropertyType,
									Enum.GetValues(pi.PropertyType.IsEnum ? pi.PropertyType : Nullable.GetUnderlyingType(pi.PropertyType)),
									UserPrompt,
									null);
							}
							else if (IsOfTypeOrNullableType(pi.PropertyType, typeof(string)))
								tmpUserAnswer = UserMessages.Prompt(UserPrompt, propOwnLongName, IsPassword: IsPasswordDoNotSave);
							else if (IsOfTypeOrNullableType(pi.PropertyType, typeof(short)))
							{
								tmpUserAnswer = UserMessages.Prompt(UserPrompt, propOwnLongName, IsPassword: IsPasswordDoNotSave);
								short tmpshort;
								if (short.TryParse(tmpUserAnswer.ToString(), out tmpshort))
									tmpUserAnswer = tmpshort;
								else
								{
									UserMessages.ShowErrorMessage("Cannot convert value of " + propName + " to an integer: " + tmpUserAnswer);
									tmpUserAnswer = null;
								}
							}
							else if (IsOfTypeOrNullableType(pi.PropertyType, typeof(int)))
							{
								tmpUserAnswer = UserMessages.Prompt(UserPrompt, propOwnLongName, IsPassword: IsPasswordDoNotSave);
								int tmpint;
								if (int.TryParse(tmpUserAnswer.ToString(), out tmpint))
									tmpUserAnswer = tmpint;
								else
								{
									UserMessages.ShowErrorMessage("Cannot convert value of " + propName + " to an integer: " + tmpUserAnswer);
									tmpUserAnswer = null;
								}
							}
							else if (IsOfTypeOrNullableType(pi.PropertyType, typeof(double)))
							{
								tmpUserAnswer = UserMessages.Prompt(UserPrompt, propOwnLongName, IsPassword: IsPasswordDoNotSave);
								double tmpdouble;
								if (double.TryParse(tmpUserAnswer.ToString(), out tmpdouble))
									tmpUserAnswer = tmpdouble;
								else
								{
									UserMessages.ShowErrorMessage("Cannot convert value of " + propName + " to a double: " + tmpUserAnswer);
									tmpUserAnswer = null;
								}
							}
							else if (IsOfTypeOrNullableType(pi.PropertyType, typeof(bool)))
							{
								if (att == null || string.IsNullOrWhiteSpace(att.UserPrompt))
									UserPrompt = propName + "?";
								tmpUserAnswer = Nullable.GetUnderlyingType(pi.PropertyType) != null ? UserMessages.ConfirmNullable(UserPrompt) : UserMessages.Confirm(UserPrompt);
							}
							else
								UserMessages.ShowWarningMessage("No hook method is defined for a property of type = "
									+ (Nullable.GetUnderlyingType(pi.PropertyType) != null ? Nullable.GetUnderlyingType(pi.PropertyType).Name : pi.PropertyType.Name));
						}

						if (tmpUserAnswer != null && tmpUserAnswer.ToString() != "")
						{
							pi.SetValue(target, tmpUserAnswer);
							if (!IsPasswordDoNotSave)
								target.OnPropertySet(propName);
							goto gotoRetryAfterUserSet;
						}
						else
						{
							//User cancelled the request to enter the setting
							if (!StaticPropertyInterceptor.UnsetPropertiesContains(propName))
								StaticPropertyInterceptor.UnsetProperties.Add(new UnsetPropertyDetail(propName, pi, UserPrompt));
						}
					}
					else if (result.ReturnValue != null)
					{
						if (StaticPropertyInterceptor.UnsetPropertiesContains(propName))
							StaticPropertyInterceptor.UnsetPropertiesRemove(propName);
					}
				}
				return result;
			}
			else
			{
				throw new NotSupportedException();
			}
		}

		IMethodReturnMessage InvokeMethod(IMethodCallMessage callMsg)
		{
			return RemotingServices.ExecuteMessage(target, callMsg);
		}

	}

	public static T Create(T instance = null)
	{
		var interceptor = new InterceptorProxy(instance ?? new T());
		return (T)interceptor.GetTransparentProxy();
	}

	private Interceptor()
	{
	}
}