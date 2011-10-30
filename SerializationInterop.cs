using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
public class SerializationInterop
{
	//TODO: Check uit and use the code snippet indexer
	/// <summary>
	/// Xml or Binary format (xml generally uses 10x more bytes but it also stores the name of each field with the value, binary only stores the value).
	/// </summary>
	public enum SerializationFormat { Binary, Xml };

	//private enum ExistanceInClass { Field, Property, None }
	//private static ExistanceInClass DoesFieldOrPropertyExist(object obj, string fieldOrPropertyName)
	//{
	//	MemberInfo mi = obj.GetType().GetMembers();
	//	if (mi.MemberType == MemberTypes.
	//}

	//private static bool SetFieldOrPropertyOfObject(object obj, string fieldOrPropertyName, dynamic valueToConvertToType, Type typeToConvertTo)
	//{
	//	FieldInfo fieldInfo = obj.GetType().GetField(fieldOrPropertyName);
	//	PropertyInfo propertyInfo = obj.GetType().GetProperty(fieldOrPropertyName);
	//	if (fieldInfo == null && propertyInfo == null)
	//		return false;

	//	dynamic tmpValue = null;
	//	if (typeToConvertTo == typeof(string)) tmpValue = valueToConvertToType.ToString();
	//	else if (typeToConvertTo == typeof(int)) tmpValue = Convert.ToInt32(valueToConvertToType);
	//	else if (typeToConvertTo == typeof(long)) tmpValue = Convert.ToInt64(valueToConvertToType);
	//	else if (typeToConvertTo == typeof(bool)) tmpValue = Convert.ToBoolean(valueToConvertToType);
	//	else if (typeToConvertTo == typeof(double)) tmpValue = Convert.ToDouble(valueToConvertToType);
	//	else if (typeToConvertTo == typeof(List<string>))
	//	{
	//		List<string> tmpList = new List<string>();
	//		foreach (string s in valueToConvertToType.Split('|')) tmpList.Add(s);
	//		if (tmpList.Count == 0 || tmpList[0].Trim().Length == 0) tmpList = new List<string>();
	//		tmpValue = tmpList;
	//	}
	//	if (tmpValue != null)
	//	{
	//		if (fieldInfo != null) fieldInfo.SetValue(obj, tmpValue);
	//		else if (propertyInfo != null) propertyInfo.SetValue(obj, tmpValue, null);
	//		return true;
	//	}
	//	else return false;
	//}

	private static bool SetMemberValue(ref object obj, ref MemberInfo memberInfo, ref object val)
	{
		if (memberInfo.MemberType == MemberTypes.Field)
			((FieldInfo)memberInfo).SetValue(obj, val);
		else if (memberInfo.MemberType == MemberTypes.Property)
			((PropertyInfo)memberInfo).SetValue(obj, val, null);
		else return false;
		return true;//Only returns true if member was field/property
	}

	private static object ReadTypeFromBinaryReader(Type typeToRead, ref BinaryReader binaryReader)
	{
		if (typeToRead == typeof(string)) return binaryReader.ReadString();
		else if (typeToRead == typeof(int)) return binaryReader.ReadInt32();
		else if (typeToRead == typeof(long)) return binaryReader.ReadInt64();
		else if (typeToRead == typeof(bool)) return binaryReader.ReadBoolean();
		else if (typeToRead == typeof(double)) return binaryReader.ReadDouble();
		else return null;
	}

	private static object GetObjectValueFromMemberInfo(ref object obj, MemberInfo memberInfo)
	{
		if (memberInfo.MemberType == MemberTypes.Field)
			return ((FieldInfo)memberInfo).GetValue(obj);
		else if (memberInfo.MemberType == MemberTypes.Property)
			return ((PropertyInfo)memberInfo).GetValue(obj, null);
		else return null;
	}

	/// <summary>
	/// Deserialize an object from a stream.
	/// </summary>
	/// <param name="streamToDeserialize">The stream to read from.</param>
	/// <param name="emptyObject">The empty object to populate the fields for.</param>
	/// <param name="CloseStream">Whether to close the stream when finished reading.</param>
	/// <param name="serializationFormat">Xml or Binary format (xml generally uses 10x more bytes but it also stores the name of each field with the value, binary only stores the value).</param>
	/// <returns>Returns the populated object.</returns>
	public static object DeserializeCustomObjectFromStream(Stream streamToDeserialize, object emptyObject, bool CloseStream = true, SerializationFormat serializationFormat = SerializationFormat.Binary)
	{
		try
		{
			Dictionary<string, object> tmpHashTable = new Dictionary<string, object>();
			streamToDeserialize.Position = 0;
			XmlTextReader textReader = serializationFormat == SerializationFormat.Xml ? new XmlTextReader(streamToDeserialize) : null;
			BinaryReader binaryReader = serializationFormat == SerializationFormat.Binary ? new BinaryReader(streamToDeserialize) : null;

			List<string> tmpHashTableKeys = new List<string>();
			if (serializationFormat == SerializationFormat.Xml)
			{
				textReader.Read();
				while (textReader.NodeType != XmlNodeType.EndElement)
				{
					if (textReader.Name == emptyObject.GetType().Name)
					{
						textReader.Read();
						continue;
					}
					string name = textReader.Name;
					if (!textReader.IsEmptyElement) textReader.Read();
					string value = textReader.Value;

					tmpHashTable.Add(name.Trim(), value.Trim());
					textReader.Read();
					if (textReader.NodeType == XmlNodeType.EndElement) textReader.Read();
				}
				foreach (string key in tmpHashTable.Keys)
					tmpHashTableKeys.Add(key);
			}

			//if (binaryReader.PeekChar() == -1) return emptyObject;
			//string fieldOrPropertyName = binaryReader.ReadString();			
			//SetFieldOrPropertyOfObject(emptyObject, fieldOrPropertyName, 

			int tmpCounterForXmlReader = 0;
			while (true)
			{
				if (serializationFormat == SerializationFormat.Binary && (binaryReader.PeekChar() == -1 || binaryReader.BaseStream.Length == binaryReader.BaseStream.Position))
					break;
				if (serializationFormat == SerializationFormat.Binary && binaryReader.BaseStream.Length - binaryReader.BaseStream.Position - 1 < binaryReader.PeekChar())
					continue;

				if (serializationFormat == SerializationFormat.Xml && tmpCounterForXmlReader >= tmpHashTable.Count)
					break;
				//MemberInfo[] memberInfos = emptyObject.GetType().GetMembers();
				
				string memberNameToSearch =
					serializationFormat == SerializationFormat.Xml
					? tmpHashTableKeys[tmpCounterForXmlReader]
					: binaryReader.ReadString();
				MemberInfo[] possibleMemberInfos = emptyObject.GetType().GetMember(memberNameToSearch);
				if (possibleMemberInfos == null || possibleMemberInfos.Length == 0) return emptyObject;
				if (possibleMemberInfos.Length > 1)
					UserMessages.ShowWarningMessage("Ambiguity found, multiple members found with name " + memberNameToSearch);
				MemberInfo memberInfo = possibleMemberInfos[0];
				//foreach (MemberInfo memberInfo in memberInfos)
				if (memberInfo.MemberType == MemberTypes.Field || memberInfo.MemberType == MemberTypes.Property)
				{
					Type memberType = memberInfo.MemberType == MemberTypes.Field ? ((FieldInfo)memberInfo).FieldType : ((PropertyInfo)memberInfo).PropertyType;
					Console.WriteLine("Reading member " + memberInfo.Name + ", type = " + memberType);
					object valueToSet = 
						serializationFormat == SerializationFormat.Xml
							? tmpHashTable[memberInfo.Name]
							: ReadTypeFromBinaryReader(memberType, ref binaryReader);
					if (valueToSet == null)
						continue;
					SetMemberValue(ref emptyObject, ref memberInfo, ref valueToSet);
					//SetMemberValue(emptyObject, memberInfo, 
					//if (memberType == typeof(string))
					//	memberInfo.se
				}
				tmpCounterForXmlReader++;
			}

			//foreach (FieldInfo fieldInfo in emptyObject.GetType().GetFields())
			//{
			//	if (fieldInfo.IsPublic)
			//	{
			//		Console.WriteLine("Reading field " + fieldInfo.Name + ", type = " + fieldInfo.FieldType);
			//		if (fieldInfo.FieldType == typeof(string))
			//			fieldInfo.SetValue(emptyObject, serializationFormat == SerializationFormat.Xml ? tmpHashTable[fieldInfo.Name] : binaryReader.ReadString());
			//		else if (fieldInfo.FieldType == typeof(int))
			//			fieldInfo.SetValue(emptyObject, Convert.ToInt32(serializationFormat == SerializationFormat.Xml ? tmpHashTable[fieldInfo.Name] : binaryReader.ReadInt32()));
			//		else if (fieldInfo.FieldType == typeof(long))
			//			fieldInfo.SetValue(emptyObject, Convert.ToInt64(serializationFormat == SerializationFormat.Xml ? tmpHashTable[fieldInfo.Name] : binaryReader.ReadInt64()));
			//		else if (fieldInfo.FieldType == typeof(bool))
			//			fieldInfo.SetValue(emptyObject, Convert.ToBoolean(serializationFormat == SerializationFormat.Xml ? tmpHashTable[fieldInfo.Name] : binaryReader.ReadBoolean()));
			//		else if (fieldInfo.FieldType == typeof(double))
			//			fieldInfo.SetValue(emptyObject, Convert.ToDouble(serializationFormat == SerializationFormat.Xml ? tmpHashTable[fieldInfo.Name] : binaryReader.ReadDouble()));
			//		else
			//			UserMessages.ShowWarningMessage("The following FIELD type cannot be converted to a string (no function yet for it): " + fieldInfo.FieldType.ToString(), "Invalid type");
			//	}
			//}

			//foreach (PropertyInfo propertyInfo in emptyObject.GetType().GetProperties())
			//{
			//	if (propertyInfo.CanWrite)
			//	{
			//		Console.WriteLine("Reading property " + propertyInfo.Name + ", type = " + propertyInfo.PropertyType);
			//		if (propertyInfo.PropertyType == typeof(string))
			//			propertyInfo.SetValue(emptyObject, serializationFormat == SerializationFormat.Xml ? tmpHashTable[propertyInfo.Name] : binaryReader.ReadString(), null);
			//		else if (propertyInfo.PropertyType == typeof(int))
			//			propertyInfo.SetValue(emptyObject, Convert.ToInt32(serializationFormat == SerializationFormat.Xml ? tmpHashTable[propertyInfo.Name] : binaryReader.ReadInt32()), null);
			//		else if (propertyInfo.PropertyType == typeof(long))
			//			propertyInfo.SetValue(emptyObject, Convert.ToInt64(serializationFormat == SerializationFormat.Xml ? tmpHashTable[propertyInfo.Name] : binaryReader.ReadInt64()), null);
			//		else if (propertyInfo.PropertyType == typeof(bool))
			//			propertyInfo.SetValue(emptyObject, Convert.ToBoolean(serializationFormat == SerializationFormat.Xml ? tmpHashTable[propertyInfo.Name] : binaryReader.ReadBoolean()), null);
			//		else if (propertyInfo.PropertyType == typeof(double))
			//			propertyInfo.SetValue(emptyObject, Convert.ToDouble(serializationFormat == SerializationFormat.Xml ? tmpHashTable[propertyInfo.Name] : binaryReader.ReadDouble()), null);
			//		else
			//			UserMessages.ShowWarningMessage("The PROPERTY following type cannot be converted to a string (no function yet for it): " + propertyInfo.PropertyType.ToString(), "Invalid type");
			//	}
			//}

			return emptyObject;
		}
		finally
		{
			if (CloseStream) streamToDeserialize.Close();
		}
	}

	/// <summary>
	/// Serialize a object into a stream.
	/// </summary>
	/// <param name="objectToSerialize">The object to serialize.</param>
	/// <param name="streamToSerializeTo">The stream to write to.</param>
	/// <param name="CloseStream">Whether to close the stream at end of writing.</param>
	/// <param name="serializationFormat">Xml or Binary format (xml generally uses 10x more bytes but it also stores the name of each field with the value, binary only stores the value).</param>
	public static void SerializeCustomObjectToStream(Object objectToSerialize, Stream streamToSerializeTo, bool CloseStream = true, SerializationFormat serializationFormat = SerializationFormat.Binary)//, Hashtable HashTableIn)
	{
		//DONE TODO: Maybe later look at also serializing/deserializing public properties, and not the fields only
		try
		{
			BinaryWriter binaryWriter = serializationFormat == SerializationFormat.Binary ? new BinaryWriter(streamToSerializeTo) : null;
			XmlTextWriter xmlTextWriter = serializationFormat == SerializationFormat.Xml ? new XmlTextWriter(streamToSerializeTo, Encoding.ASCII) : null;// streamToSerializeTo, Encoding.ASCII);

			string tmpClassName = objectToSerialize.GetType().ToString();
			tmpClassName = tmpClassName.Split('+')[tmpClassName.Split('+').Length - 1];
			if (serializationFormat == SerializationFormat.Xml)
				xmlTextWriter.WriteStartElement(tmpClassName);

			foreach (MemberInfo memberInfo in objectToSerialize.GetType().GetMembers())
			{
				if (memberInfo.MemberType == MemberTypes.Field || memberInfo.MemberType == MemberTypes.Property)
				{
					Type memberType = memberInfo.MemberType == MemberTypes.Field ? ((FieldInfo)memberInfo).FieldType : ((PropertyInfo)memberInfo).PropertyType;
					Console.WriteLine("Writing member " + memberInfo.Name + ", type = " + memberType);					
					if (serializationFormat == SerializationFormat.Xml)
						xmlTextWriter.WriteElementString(memberInfo.Name, GetObjectValueFromMemberInfo(ref objectToSerialize, memberInfo).ToString());
					else if (serializationFormat == SerializationFormat.Binary)
					{
						binaryWriter.Write((string)memberInfo.Name);
						binaryWriter.Write((dynamic)GetObjectValueFromMemberInfo(ref objectToSerialize, memberInfo));
					}
				}
			}

			//foreach (FieldInfo fieldInfo in objectToSerialize.GetType().GetFields())
			//{
			//	if (fieldInfo.IsPublic)
			//	{
			//		Console.WriteLine("Writing field " + fieldInfo.Name + ", type = " + fieldInfo.FieldType);
			//		if (serializationFormat == SerializationFormat.Binary)
			//			binaryWriter.Write((string)fieldInfo.Name);
			//		//if (
			//		//		fieldInfo.FieldType == typeof(string) ||
			//		//		fieldInfo.FieldType == typeof(bool) ||
			//		//		fieldInfo.FieldType == typeof(int) ||
			//		//		fieldInfo.FieldType == typeof(double) ||
			//		//		fieldInfo.FieldType == typeof(long)
			//		//	)
			//		if (fieldInfo.FieldType == typeof(string))
			//		{
			//			if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(fieldInfo.Name, fieldInfo.GetValue(objectToSerialize).ToString());
			//			else binaryWriter.Write((string)fieldInfo.GetValue(objectToSerialize));
			//		}
			//		else if (fieldInfo.FieldType == typeof(bool))
			//		{
			//			if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(fieldInfo.Name, fieldInfo.GetValue(objectToSerialize).ToString());
			//			else binaryWriter.Write((bool)fieldInfo.GetValue(objectToSerialize));
			//		}
			//		else if (fieldInfo.FieldType == typeof(int))
			//		{
			//			if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(fieldInfo.Name, fieldInfo.GetValue(objectToSerialize).ToString());
			//			else binaryWriter.Write((int)fieldInfo.GetValue(objectToSerialize));
			//		}
			//		else if (fieldInfo.FieldType == typeof(long))
			//		{
			//			if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(fieldInfo.Name, fieldInfo.GetValue(objectToSerialize).ToString());
			//			else binaryWriter.Write((long)fieldInfo.GetValue(objectToSerialize));
			//		}
			//		else if (fieldInfo.FieldType == typeof(double))
			//		{
			//			if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(fieldInfo.Name, fieldInfo.GetValue(objectToSerialize).ToString());
			//			else binaryWriter.Write((double)fieldInfo.GetValue(objectToSerialize));
			//		}
			//		else if (fieldInfo.FieldType == typeof(List<string>))
			//		{
			//			string tmpStr = "";
			//			foreach (string s in (List<string>)fieldInfo.GetValue(objectToSerialize))
			//				tmpStr += (tmpStr.Length > 0 ? "|" : "") + s;
			//			if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(fieldInfo.Name, tmpStr);
			//			else binaryWriter.Write(tmpStr);
			//		}
			//		else
			//			UserMessages.ShowWarningMessage("The following FIELD type cannot be converted to a string (no function yet for it): " + fieldInfo.FieldType.ToString(), "Invalid type");
			//	}
			//}

			//foreach (PropertyInfo propertyInfo in objectToSerialize.GetType().GetProperties())
			//{
			//	if (propertyInfo.CanWrite)
			//	{
			//		Console.WriteLine("Writing property " + propertyInfo.Name + ", type = " + propertyInfo.PropertyType);
			//		//if (
			//		//		propertyInfo.PropertyType == typeof(string) ||
			//		//		propertyInfo.PropertyType == typeof(bool) ||
			//		//		propertyInfo.PropertyType == typeof(int) ||
			//		//		propertyInfo.PropertyType == typeof(double) ||
			//		//		propertyInfo.PropertyType == typeof(long)
			//		//	)
			//		if (propertyInfo.PropertyType == typeof(string))
			//		{
			//			if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(propertyInfo.Name, propertyInfo.GetValue(objectToSerialize, null).ToString());
			//			else binaryWriter.Write((string)propertyInfo.GetValue(objectToSerialize, null));
			//		}
			//		else if (propertyInfo.PropertyType == typeof(bool))
			//		{
			//			if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(propertyInfo.Name, propertyInfo.GetValue(objectToSerialize, null).ToString());
			//			else binaryWriter.Write((bool)propertyInfo.GetValue(objectToSerialize, null));
			//		}
			//		else if (propertyInfo.PropertyType == typeof(int))
			//		{
			//			if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(propertyInfo.Name, propertyInfo.GetValue(objectToSerialize, null).ToString());
			//			else binaryWriter.Write((int)propertyInfo.GetValue(objectToSerialize, null));
			//		}
			//		else if (propertyInfo.PropertyType == typeof(long))
			//		{
			//			if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(propertyInfo.Name, propertyInfo.GetValue(objectToSerialize, null).ToString());
			//			else binaryWriter.Write((long)propertyInfo.GetValue(objectToSerialize, null));
			//		}
			//		else if (propertyInfo.PropertyType == typeof(double))
			//		{
			//			if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(propertyInfo.Name, propertyInfo.GetValue(objectToSerialize, null).ToString());
			//			else binaryWriter.Write((double)propertyInfo.GetValue(objectToSerialize, null));
			//		}
			//		else if (propertyInfo.PropertyType == typeof(List<string>))
			//		{
			//			string tmpStr = "";
			//			foreach (string s in (List<string>)propertyInfo.GetValue(objectToSerialize, null))
			//				tmpStr += (tmpStr.Length > 0 ? "|" : "") + s;
			//			if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(propertyInfo.Name, tmpStr);
			//			else binaryWriter.Write(tmpStr);
			//		}
			//		else
			//			UserMessages.ShowWarningMessage("The following FIELD type cannot be converted to a string (no function yet for it): " + propertyInfo.PropertyType.ToString(), "Invalid type");
			//	}
			//}

			if (serializationFormat == SerializationFormat.Xml)
			{
				xmlTextWriter.WriteEndElement();
				xmlTextWriter.Flush();
			}
			else
			{
				binaryWriter.Flush();
			}			
		}
		finally
		{
			if (CloseStream) streamToSerializeTo.Close();
		}
	}
}