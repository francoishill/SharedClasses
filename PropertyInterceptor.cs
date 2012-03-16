using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using PropertyInterceptor;
using SharedClasses;

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

					PropertyInfo pi = target.GetType().GetProperty(propName);
					string propOwnLongName = pi.DeclaringType.Name + "." + propName;
					//if (pi.PropertyType.IsEnum || (Nullable.GetUnderlyingType(pi.PropertyType) != null && Nullable.GetUnderlyingType(pi.PropertyType).IsEnum))
					//	System.Windows.Forms.MessageBox.Show("Enum = " + propName);
					SettingAttribute att = pi.GetCustomAttribute(typeof(SettingAttribute)) as SettingAttribute;

					//if (att != null && att.IgnoredByPropertyInterceptor_EncryptingAnother &&
					//	!ConfirmUsingFaceDetection.ConfirmUsingFacedetection(
					//			GlobalSettings.FaceDetectionInteropSettings.Instance.FaceName,
					//			10))
					//{
					//	pi.SetValue(target, null);
					//	target.OnPropertySet(propName);
					//	goto gotoRetryAfterUserSet;
					//}

					if (result.ReturnValue == null)
					{
						if (att != null && att.IsEncrypted)
						{
							PropertyInfo encryptedPi = target.GetType().GetProperty(att.EncryptedPropertyName);
							object encryptedVal = encryptedPi.GetValue(target);
							if (encryptedVal != null)
							{
								string decryptedVal = GenericSettings.Decrypt(encryptedVal.ToString(), att.EncryptedPropertyName, att.RequireFacialAutorisationEverytime);
								if (decryptedVal != null)
								{
									pi.SetValue(target, decryptedVal);
									//target.OnPropertySet(pi.Name);
									goto gotoRetryAfterUserSet;
								}
							}
							else
							{
								string UserPrompt = "Please enter value for " + propName;
								if (att != null && !string.IsNullOrWhiteSpace(att.UserPrompt))
									UserPrompt = att.UserPrompt;
								object tmpUnEncryptedAnswer = InputBoxWPF.Prompt(UserPrompt, propOwnLongName, IsPassword: true);
								if (tmpUnEncryptedAnswer != null)
								{
									encryptedPi.SetValue(target, GenericSettings.Encrypt(tmpUnEncryptedAnswer.ToString(), att.EncryptedPropertyName));
									pi.SetValue(target, tmpUnEncryptedAnswer.ToString());
									target.OnPropertySet(propName);
									goto gotoRetryAfterUserSet;
								}
							}
						}
						else
						{
							//It does not ask for userinput if the current value is null
							if (att != null && att.IgnoredByPropertyInterceptor_EncryptingAnother)
								return result;

							string UserPrompt = "Please enter value for " + propName;
							bool IsPasswordDoNotSave = false;
							if (att != null)
							{
								if (!string.IsNullOrWhiteSpace(att.UserPrompt))
									UserPrompt = att.UserPrompt;
								IsPasswordDoNotSave = att.DoNoSaveToFile;
							}

							object tmpUserAnswer = null;
							if (!StaticPropertyInterceptor.IsBypassingUserPromptEnabled())
							{
								if (pi.PropertyType.IsEnum || (Nullable.GetUnderlyingType(pi.PropertyType) != null && Nullable.GetUnderlyingType(pi.PropertyType).IsEnum))
								{
									if (att == null || string.IsNullOrWhiteSpace(att.UserPrompt))
										UserPrompt = "Please pick one of the following options for " + propName;
									tmpUserAnswer = PickItemWPF.PickItem(
										pi.PropertyType,
										Enum.GetValues(pi.PropertyType.IsEnum ? pi.PropertyType : Nullable.GetUnderlyingType(pi.PropertyType)),
										UserPrompt,
										null);
								}
								else if (IsOfTypeOrNullableType(pi.PropertyType, typeof(string)))
									tmpUserAnswer = InputBoxWPF.Prompt(UserPrompt, propOwnLongName, IsPassword: IsPasswordDoNotSave);
								else if (IsOfTypeOrNullableType(pi.PropertyType, typeof(short)))
								{
									tmpUserAnswer = InputBoxWPF.Prompt(UserPrompt, propOwnLongName, IsPassword: IsPasswordDoNotSave);
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
									tmpUserAnswer = InputBoxWPF.Prompt(UserPrompt, propOwnLongName, IsPassword: IsPasswordDoNotSave);
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
									tmpUserAnswer = InputBoxWPF.Prompt(UserPrompt, propOwnLongName, IsPassword: IsPasswordDoNotSave);
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
								else if (IsOfTypeOrNullableType(pi.PropertyType, typeof(string[])))
								{
									List<string> tmpList = null;
									string tmpstr = null;
									do
									{
										tmpstr = InputBoxWPF.Prompt(UserPrompt);
										if (tmpstr == null) continue;
										else
										{
											if (tmpList == null)
												tmpList = new List<string>();
											tmpList.Add(tmpstr);
										}
									}
									while (tmpstr != null);
									if (tmpList != null)
									{
										tmpUserAnswer = tmpList.ToArray();
										tmpList.Clear();
										tmpList = null;
									}
								}
								/*
								//TODO: This works but when flushing to file (serializing), it fails as it is a custom dictionary
								else if (IsOfTypeOrNullableType(pi.PropertyType, typeof(Dictionary<string, List<GlobalSettings.MouseGesturesSettings.GestureDirection>>)))
								{
									string tmpString;
									List<GlobalSettings.MouseGesturesSettings.GestureDirection> tmpList;
									while (UserMessages.EnterStringAndListOfEnums<GlobalSettings.MouseGesturesSettings.GestureDirection>("Enter message which will be shown when the MouseGesture is performed.", out tmpString, out tmpList))
									{
										if (tmpUserAnswer == null)
											tmpUserAnswer = new Dictionary<string, List<GlobalSettings.MouseGesturesSettings.GestureDirection>>();
										(tmpUserAnswer as Dictionary<string, List<GlobalSettings.MouseGesturesSettings.GestureDirection>>)
											.Add(tmpString, tmpList);
									}
									//UserMessages.ShowMessage("Dictionary hook type");
								}*/
								else
									UserMessages.ShowWarningMessage("No hook method is defined for a property of type = "
										+ (Nullable.GetUnderlyingType(pi.PropertyType) != null ? Nullable.GetUnderlyingType(pi.PropertyType).Name : pi.PropertyType.Name));
							}

							if (tmpUserAnswer != null && tmpUserAnswer.ToString() != "")
							{
								//if (att.IsEncrypted)
								//{
								//	tmpUserAnswer = GenericSettings.Encrypt(tmpUserAnswer.ToString(), propName);
								//}

								pi.SetValue(target, tmpUserAnswer);
								target.OnPropertySet(propName);
								goto gotoRetryAfterUserSet;
								//Removed this as the passwords should be XmlIgnore and have another public property called (for instance) PasswordEncrypted and should have attribute XmlElement and also in its get/set use encoding/encryption
								//if (!IsPasswordDoNotSave)//This occurs the 
								//	target.OnPropertySet(propName);
								//goto gotoRetryAfterUserSet;
							}
							else
							{
								//User cancelled the request to enter the setting
								if (!StaticPropertyInterceptor.UnsetPropertiesContains(propName))
									StaticPropertyInterceptor.UnsetProperties.Add(new UnsetPropertyDetail(propName, pi, UserPrompt));
							}
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