using System;
using System.IO;
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
			if (serializationFormat == SerializationFormat.Binary) return new BinaryFormatter().Deserialize(streamToDeserializeFrom);
			else if (serializationFormat == SerializationFormat.Xml) return new XmlSerializer(typeOfObject).Deserialize(streamToDeserializeFrom);
			return null;
		}
		finally { if (CloseStream) streamToDeserializeFrom.Close(); }
	}

	public static object DeserializeObject_Fromfile(string filePath, SerializationFormat serializationFormat, Type typeOfObject, bool CloseStream = true)
	{
		Stream fileReadStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		return DeserializeObject(fileReadStream, serializationFormat, typeOfObject, CloseStream);
	}
}