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

	//public static void SerializeObject(object objToSerialize, Stream streamToSerializeTo, SerializationFormat serializationFormat, bool CloseStream = true)
	//{
	//	if (serializationFormat == SerializationFormat.Binary) new BinaryFormatter().Serialize(streamToSerializeTo, objToSerialize);
	//	else if (serializationFormat == SerializationFormat.Xml) new XmlSerializer(objToSerialize.GetType()).Serialize(streamToSerializeTo, objToSerialize);
	//	if (CloseStream) streamToSerializeTo.Close();
	//}

	//public static void SerializeObject_Tofile(object objToSerialize, string filePath, SerializationFormat serializationFormat, bool CloseStream = true)
	//{
	//	Stream fileWriteStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
	//	SerializeObject(objToSerialize, fileWriteStream, serializationFormat, CloseStream);
	//}

	//public static object DeserializeObject(Stream streamToDeserializeFrom, SerializationFormat serializationFormat, Type typeOfObject, bool CloseStream = true)
	//{
	//	try
	//	{
	//		streamToDeserializeFrom.Position = 0;
	//		if (serializationFormat == SerializationFormat.Binary)
	//		{
	//			try
	//			{
	//				object returningObject = new BinaryFormatter().Deserialize(streamToDeserializeFrom);
	//				return returningObject;
	//			}
	//			catch (Exception exc)
	//			{
	//				UserMessages.ShowWarningMessage("Cannot deserialize object to type " + typeOfObject.GetType().ToString() + Environment.NewLine + exc.Message);
	//			}
	//		}
	//		else if (serializationFormat == SerializationFormat.Xml)
	//		{
	//			try
	//			{
	//				object returningObject = new XmlSerializer(typeOfObject).Deserialize(streamToDeserializeFrom);
	//				return returningObject;
	//			}
	//			catch (Exception exc)
	//			{
	//				UserMessages.ShowWarningMessage("Cannot deserialize object to type " + typeOfObject.GetType().ToString() + Environment.NewLine + exc.Message);
	//			}
	//		}
	//		return null;
	//	}
	//	finally { if (CloseStream) streamToDeserializeFrom.Close(); }
	//}

	//public static object DeserializeObject_Fromfile(string filePath, SerializationFormat serializationFormat, Type typeOfObject, bool CloseStream = true)
	//{
	//	Stream fileReadStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
	//	return DeserializeObject(fileReadStream, serializationFormat, typeOfObject, CloseStream);
	//}

	/// <summary>
	/// Deserialize an object from a stream.
	/// </summary>
	/// <param name="streamToDeserialize">The stream to read from.</param>
	/// <param name="emptyObject">The empty object to populate the fields for.</param>
	/// <param name="CloseStream">Whether to close the stream when finished reading.</param>
	/// <param name="serializationFormat">Xml or Binary format (xml generally uses 10x more bytes but it also stores the name of each field with the value, binary only stores the value).</param>
	/// <returns>Returns the populated object.</returns>
	public static object DeserializeCustom(Stream streamToDeserialize, object emptyObject, bool CloseStream = true, SerializationFormat serializationFormat = SerializationFormat.Binary)
	{
		try
		{
			Dictionary<string, object> tmpHashTable = new Dictionary<string, object>();
			//bool isSelected = false;
			//TreeNode tmpNode = new TreeNode("tmp");
			streamToDeserialize.Position = 0;
			XmlTextReader textReader = serializationFormat == SerializationFormat.Xml ? new XmlTextReader(streamToDeserialize) : null;
			BinaryReader binaryReader = serializationFormat == SerializationFormat.Binary ? new BinaryReader(streamToDeserialize) : null;

			if (serializationFormat == SerializationFormat.Xml)
			{
				textReader.Read();
				while (textReader.NodeType != XmlNodeType.EndElement)// || textReader.Name != "SubNode")
				{
					if (textReader.Name == emptyObject.GetType().Name)
					{
						textReader.Read();
						continue;
					}
					//if (textReader.Name == "SubNode" && textReader.NodeType == XmlNodeType.Element)tmpNode.Nodes.Add(ReadSubNode(textReader));//, NSISclassMenu, SectionGroupMenu, SectionMenu, ShortcutMenu, FileTextblockMenu, FileLineMenu));
					//else if (textReader.NodeType == XmlNodeType.Element && textReader.Name != "SubNode")
					//{
					string name = textReader.Name;
					if (!textReader.IsEmptyElement) textReader.Read();
					string value = textReader.Value;

					tmpHashTable.Add(name.Trim(), value.Trim());
					//}
					textReader.Read();
					if (textReader.NodeType == XmlNodeType.EndElement) textReader.Read();
				}
			}
			foreach (FieldInfo info in emptyObject.GetType().GetFields())
				if (info.IsPublic)
				{
					Console.WriteLine("Reading " + info.Name + ", type = " + info.FieldType);
					if (info.FieldType == typeof(string))
						info.SetValue(emptyObject, serializationFormat == SerializationFormat.Xml ? tmpHashTable[info.Name] : binaryReader.ReadString());
					else if (info.FieldType == typeof(int))
						info.SetValue(emptyObject, Convert.ToInt32(serializationFormat == SerializationFormat.Xml ? tmpHashTable[info.Name] : binaryReader.ReadInt32()));
					else if (info.FieldType == typeof(long))
						info.SetValue(emptyObject, Convert.ToInt64(serializationFormat == SerializationFormat.Xml ? tmpHashTable[info.Name] : binaryReader.ReadInt64()));
					else if (info.FieldType == typeof(bool))
						info.SetValue(emptyObject, Convert.ToBoolean(serializationFormat == SerializationFormat.Xml ? tmpHashTable[info.Name] : binaryReader.ReadBoolean()));
					else if (info.FieldType == typeof(double))
						info.SetValue(emptyObject, Convert.ToDouble(serializationFormat == SerializationFormat.Xml ? tmpHashTable[info.Name] : binaryReader.ReadDouble()));
					else if (info.FieldType == typeof(List<string>))
					{
						List<string> tmpList = new List<string>();
						string pipeConcatenatedList = serializationFormat == SerializationFormat.Xml ? tmpHashTable[info.Name].ToString() : binaryReader.ReadString();
						foreach (string s in pipeConcatenatedList.Split('|'))
							tmpList.Add(s);
						if (tmpList.Count == 0 || tmpList[0].Trim().Length == 0)
							tmpList = new List<string>();
						info.SetValue(emptyObject, tmpList);
					}
					//else if (info.FieldType == typeof(NSISclass.Compressor))
					//{
					//	string tmpCompressionmodeString = tmpHashTable[info.Name].ToString().Split(',')[0];
					//	Boolean EnumFound = false;
					//	foreach (NSISclass.Compressor.CompressionModeEnum compressionmode in Enum.GetValues(typeof(NSISclass.Compressor.CompressionModeEnum)))
					//		if (tmpCompressionmodeString == compressionmode.ToString())
					//		{
					//			EnumFound = true;
					//			info.SetValue(tmpNode.Tag, new NSISclass.Compressor(compressionmode, Convert.ToBoolean(tmpHashTable[info.Name].ToString().Split(',')[1]), Convert.ToBoolean(tmpHashTable[info.Name].ToString().Split(',')[2])));
					//		}
					//	if (!EnumFound) MessageBox.Show("Could not obtain Compressionmode from string = '" + tmpCompressionmodeString + "'", "Unknown Compressionmode enum", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					//}
					//else if (info.FieldType == typeof(NSISclass.LicensePageDetails))
					//{
					//	string tmpAcceptwithString = tmpHashTable[info.Name].ToString().Split(',')[2];
					//	Boolean EnumFound = false;
					//	foreach (NSISclass.LicensePageDetails.AcceptWith acceptwith in Enum.GetValues(typeof(NSISclass.LicensePageDetails.AcceptWith)))
					//		if (tmpAcceptwithString == acceptwith.ToString())
					//		{
					//			EnumFound = true;
					//			info.SetValue(tmpNode.Tag, new NSISclass.LicensePageDetails(Convert.ToBoolean(tmpHashTable[info.Name].ToString().Split(',')[0]), tmpHashTable[info.Name].ToString().Split(',')[1], acceptwith));
					//		}
					//	if (!EnumFound) MessageBox.Show("Could not obtain Acceptwith from string = '" + tmpAcceptwithString + "'", "Unknown Acceptwith enum", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					//}
					else
						UserMessages.ShowWarningMessage("The following type cannot be converted to a string (no function yet for it): " + info.FieldType.ToString(), "Invalid type");
				}

			//return returnObject;
			return emptyObject;
		}
		finally
		{
			if (CloseStream) streamToDeserialize.Close();
		}
	}

	//private static void WriteStringToStream(ref Stream stream, string str)
	//{
	//	byte[] bytesToWrite = Encoding.ASCII.GetBytes(str);
	//	stream.Write(bytesToWrite, 0, bytesToWrite.Length);
	//}

	//private static string GetXmlValuePairString(string key, string value)
	//{
	//	return "<" + key + ">" + 
	//}

	/// <summary>
	/// Serialize a object into a stream.
	/// </summary>
	/// <param name="objectToSerialize">The object to serialize.</param>
	/// <param name="streamToSerializeTo">The stream to write to.</param>
	/// <param name="CloseStream">Whether to close the stream at end of writing.</param>
	/// <param name="serializationFormat">Xml or Binary format (xml generally uses 10x more bytes but it also stores the name of each field with the value, binary only stores the value).</param>
	public static void SerializeCustom(Object objectToSerialize, Stream streamToSerializeTo, bool CloseStream = true, SerializationFormat serializationFormat = SerializationFormat.Binary)//, Hashtable HashTableIn)
	{
		//TODO: Maybe later look at also serializing/deserializing public properties, and not the fields only
		try
		{
			BinaryWriter binaryWriter = serializationFormat == SerializationFormat.Binary ? new BinaryWriter(streamToSerializeTo) : null;
			XmlTextWriter xmlTextWriter = serializationFormat == SerializationFormat.Xml ? new XmlTextWriter(streamToSerializeTo, Encoding.ASCII) : null;// streamToSerializeTo, Encoding.ASCII);

			//Dictionary<string, object> tmpHashTable = new Dictionary<string, object>();
			string tmpClassName = objectToSerialize.GetType().ToString();
			tmpClassName = tmpClassName.Split('+')[tmpClassName.Split('+').Length - 1];
			//tmpClassName = tmpClassName.Split('.')[tmpClassName.Split('.').Length - 1];
			//tmpClassName = tmpClassName.Split('+')[tmpClassName.Split('+').Length - 1];
			//tmpHashTable.Add("ClassName", tmpClassName);
			//xmlTextWriter.WriteStartDocument();
			//xmlTextWriter.WriteStartElement("Root");
			//xmlTextWriter.WriteElementString("ClassName", tmpClassName);
			if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteStartElement(tmpClassName);
			//binaryWriter.Write(tmpClassName);
			//tmpHashTable.Add("Text", node.Text);
			foreach (FieldInfo info in objectToSerialize.GetType().GetFields())
				if (info.IsPublic)
				{
					Console.WriteLine("Writing " + info.Name + ", type = " + info.FieldType);
					if (
							info.FieldType == typeof(string) ||
							info.FieldType == typeof(bool) ||
							info.FieldType == typeof(int) ||
							info.FieldType == typeof(double) ||
							info.FieldType == typeof(long)
						)
					if (info.FieldType == typeof(string))
					{
						if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(info.Name, info.GetValue(objectToSerialize).ToString());
						else binaryWriter.Write((string)info.GetValue(objectToSerialize));
					}
					else if (info.FieldType == typeof(bool))
					{
						if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(info.Name, info.GetValue(objectToSerialize).ToString());
						else binaryWriter.Write((bool)info.GetValue(objectToSerialize));
					}
					else if (info.FieldType == typeof(int))
					{
						if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(info.Name, info.GetValue(objectToSerialize).ToString());
						else binaryWriter.Write((int)info.GetValue(objectToSerialize));
					}
					else if (info.FieldType == typeof(long))
					{
						if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(info.Name, info.GetValue(objectToSerialize).ToString());
						else binaryWriter.Write((long)info.GetValue(objectToSerialize));
					}
					else if (info.FieldType == typeof(double))
					{
						if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(info.Name, info.GetValue(objectToSerialize).ToString());
						else binaryWriter.Write((double)info.GetValue(objectToSerialize));
					}
					else if (info.FieldType == typeof(List<string>))
					{
						string tmpStr = "";
						foreach (string s in (List<string>)info.GetValue(objectToSerialize))
							tmpStr += (tmpStr.Length > 0 ? "|" : "") + s;
						if (serializationFormat == SerializationFormat.Xml) xmlTextWriter.WriteElementString(info.Name, tmpStr);
						else binaryWriter.Write(tmpStr);
						//tmpHashTable.Add(info.Name, tmpStr);
					}
					//else if (info.FieldType == typeof(NSISclass.Compressor))
					//	tmpHashTable.Add(info.Name, ((NSISclass.Compressor)info.GetValue(node.Tag)).CompressionMode + "," + ((NSISclass.Compressor)info.GetValue(node.Tag)).Final + "," + ((NSISclass.Compressor)info.GetValue(node.Tag)).Solid);
					//else if (info.FieldType == typeof(NSISclass.LicensePageDetails))
					//	tmpHashTable.Add(info.Name, ((NSISclass.LicensePageDetails)info.GetValue(node.Tag)).ShowLicensePage + "," + ((NSISclass.LicensePageDetails)info.GetValue(node.Tag)).LicenseFilePath + "," + ((NSISclass.LicensePageDetails)info.GetValue(node.Tag)).acceptWith);
					else
						UserMessages.ShowWarningMessage("The following type cannot be converted to a string (no function yet for it): " + info.FieldType.ToString(), "Invalid type");
				}

			if (serializationFormat == SerializationFormat.Xml)
			{
				xmlTextWriter.WriteEndElement();
				//xmlTextWriter.WriteEndDocument();		
				xmlTextWriter.Flush();
			}
			else
			{
				binaryWriter.Flush();
			}			
			//xmlTextWriter.Close();
			//return tmpHashTable;
		}
		finally
		{
			if (CloseStream) streamToSerializeTo.Close();
		}
	}

	//sealed class AllowAllAssemblyVersionsDeserializationBinder : System.Runtime.Serialization.SerializationBinder
	//{
	//	public override Type BindToType(string assemblyName, string typeName)
	//	{
	//		Type typeToDeserialize = null;

	//		String currentAssembly = Assembly.GetExecutingAssembly().FullName;

	//		// In this case we are always using the current assembly
	//		assemblyName = currentAssembly;

	//		// Get the type using the typeName and assemblyName
	//		typeToDeserialize = Type.GetType(String.Format("{0}, {1}",
	//				typeName, assemblyName));

	//		return typeToDeserialize;
	//	}
	//}

	//public static MyRequestObject Deserialize(byte[] b)
	//{
	//	MyRequestObject mro = null;
	//	System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
	//	System.IO.MemoryStream ms = new System.IO.MemoryStream(b);

	//	// To prevent errors serializing between version number differences (e.g. Version 1 serializes, and Version 2 deserializes)
	//	formatter.Binder = new AllowAllVersionsDeserializationBinder();

	//	// Allow the exceptions to bubble up
	//	// System.ArgumentNullException
	//	// System.Runtime.Serialization.SerializationException
	//	// System.Security.SecurityException
	//	mro = (MyRequestObject)formatter.Deserialize(ms);
	//	ms.Close();
	//	return mro;
	//}

}