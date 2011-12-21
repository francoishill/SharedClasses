using System;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

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
							tmpUserAnswer = UserMessages.Prompt(UserPrompt);
						else if (IsOfTypeOrNullableType(pi.PropertyType, typeof(short)))
						{
							tmpUserAnswer = UserMessages.Prompt(UserPrompt);
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
							tmpUserAnswer = UserMessages.Prompt(UserPrompt);
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
							tmpUserAnswer = UserMessages.Prompt(UserPrompt);
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
						
						if (tmpUserAnswer != null && tmpUserAnswer.ToString() != "")
						{
							pi.SetValue(target, tmpUserAnswer);
							if (!IsPasswordDoNotSave)
								target.OnPropertySet(propName);
							goto gotoRetryAfterUserSet;
						}
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