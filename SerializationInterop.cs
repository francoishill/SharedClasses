using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
public class SerializationInterop
{
	public enum SerializationFormat { Binary, Xml };
	
	public static void SerializeObject(object objToSerialize, Stream streamToSerializeTo, SerializationFormat serializationFormat, bool CloseStream = true)
	{
		if (serializationFormat == SerializationFormat.Binary) new BinaryFormatter().Serialize(streamToSerializeTo, objToSerialize);
		else if (serializationFormat == SerializationFormat.Xml) new XmlSerializer(objToSerialize.GetType()).Serialize(streamToSerializeTo, objToSerialize);
		if (CloseStream) streamToSerializeTo.Close();
	}

	public static void SerializeObject_Tofile(object objToSerialize, string filePath, SerializationFormat serializationFormat, bool CloseStream = true)
	{
		Stream fileWriteStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
		SerializeObject(objToSerialize, fileWriteStream, serializationFormat, CloseStream);
	}

	public static object DeserializeObject(Stream streamToDeserializeFrom, SerializationFormat serializationFormat, Type typeOfObject, bool CloseStream = true)
	{
		try
		{
			streamToDeserializeFrom.Position = 0;
			if (serializationFormat == SerializationFormat.Binary)
			{
				try
				{
					object returningObject = new BinaryFormatter().Deserialize(streamToDeserializeFrom);
					return returningObject;
				}
				catch (Exception exc)
				{
					UserMessages.ShowWarningMessage("Cannot deserialize object to type " + typeOfObject.GetType().ToString() + Environment.NewLine + exc.Message);
				}
			}
			else if (serializationFormat == SerializationFormat.Xml)
			{
				try
				{
					object returningObject = new XmlSerializer(typeOfObject).Deserialize(streamToDeserializeFrom);
					return returningObject;
				}
				catch (Exception exc)
				{
					UserMessages.ShowWarningMessage("Cannot deserialize object to type " + typeOfObject.GetType().ToString() + Environment.NewLine + exc.Message);
				}
			}
			return null;
		}
		finally { if (CloseStream) streamToDeserializeFrom.Close(); }
	}

	public static object DeserializeObject_Fromfile(string filePath, SerializationFormat serializationFormat, Type typeOfObject, bool CloseStream = true)
	{
		Stream fileReadStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		return DeserializeObject(fileReadStream, serializationFormat, typeOfObject, CloseStream);
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