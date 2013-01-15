using System;
using System.IO;
using System.Xml.Serialization;
//using TestingSharedClasses.Properties;

namespace SharedClasses
{
	public class SettingsInterop
	{
		public const string SettingsFileExtension = ".fset";

		public static string GetComputerGuidAsString()
		{
			return GetComputerGuid().ToString();
		}

		public static Guid GetComputerGuid()
		{
			var guidpath = SettingsInterop.GetFullFilePathInLocalAppdata("_ComputerGuid", "SharedClasses");
			if (File.Exists(guidpath))
			{
				string guidfromfile = File.ReadAllText(guidpath).Trim();
				Guid tmpguid;
				if (Guid.TryParse(guidfromfile, out tmpguid))
					return tmpguid;
			}
			Guid newGuid = Guid.NewGuid();
			string newGuidStr = newGuid.ToString();
			File.WriteAllText(guidpath, newGuidStr);
			return newGuid;
		}

		public static string GetComputerGuidAsFileName()
		{
			return GetComputerGuid().ToString().Replace('-', '_');
		}

		/// <summary>
		/// Always returned without leading backslash.
		/// </summary>
		/// <param name="ApplicationName">The name of the application which will be appended at end of path (appdata\CompanyName\ApplicationName).</param>
		/// <param name="CompanyName">The name of the subfolder in appdata path (appdata\CompanyName\ApplicationName).</param>
		/// <returns></returns>
		public static string LocalAppdataPath(string ApplicationName, string CompanyName = "FJH", bool EnsurePathExists = true)
		{
			string LocalAppdataPath = CalledFromService.Environment_GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			LocalAppdataPath = (LocalAppdataPath ?? "").Trim('\\');
			ApplicationName = (ApplicationName ?? "").Trim('\\');
			CompanyName = CompanyName.Trim('\\');
			string returnPath = LocalAppdataPath + "\\" + CompanyName + "\\" + ApplicationName;
			if (EnsurePathExists && !Directory.Exists(returnPath)) Directory.CreateDirectory(returnPath);
			return returnPath;
		}

		public static string GetFullFilePathInLocalAppdata(string fileName, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH", bool EnsurePathExists = true)
		{
			ApplicationName = (ApplicationName ?? "").Trim('\\');
			SubfolderNameInApplication = (SubfolderNameInApplication ?? "").Trim('\\');
			fileName = (fileName ?? "").Trim('\\');
			CompanyName = (CompanyName ?? "").Trim('\\');
			bool isSubfolderDefined = !string.IsNullOrWhiteSpace(SubfolderNameInApplication);
			string fileParentFolderPath =
				LocalAppdataPath(ApplicationName, CompanyName, EnsurePathExists)
					+ (isSubfolderDefined ? "\\" + SubfolderNameInApplication : "");
			if (EnsurePathExists && !Directory.Exists(fileParentFolderPath)) Directory.CreateDirectory(fileParentFolderPath);
			return fileParentFolderPath + "\\" + fileName;
		}

		public static string GetFullFolderPathInLocalAppdata(string folderName, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH", bool EnsurePathExists = true)
		{
			ApplicationName = (ApplicationName ?? "").Trim('\\');
			SubfolderNameInApplication = (SubfolderNameInApplication ?? "").Trim('\\');
			folderName = (folderName ?? "").Trim('\\');
			CompanyName = (CompanyName ?? "").Trim('\\');
			bool isSubfolderDefined = !string.IsNullOrWhiteSpace(SubfolderNameInApplication);
			string folderPath =
				LocalAppdataPath(ApplicationName, CompanyName, EnsurePathExists)
					+ (isSubfolderDefined ? "\\" + SubfolderNameInApplication : "")
					+ "\\" + folderName;
			if (EnsurePathExists && !Directory.Exists(folderPath))
				Directory.CreateDirectory(folderPath);
			return folderPath;
		}

		public static void FlushSettings<T>(T settingsObject, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
		{
			//string fileName = typeof(T).Name.Split('+')[typeof(T).Name.Split('+').Length - 1];
			//fileName += SettingsFileExtension;
			//SerializeToFile<T>(
			//	GetFullFilePathInLocalAppdata(fileName, ApplicationName, SubfolderNameInApplication, CompanyName, true),
			//	settingsObject);
			FlushSettings(typeof(T), settingsObject, ApplicationName, SubfolderNameInApplication, CompanyName);
		}
		public static void FlushSettings(Type ObjectType, object settingsObject, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
		{
			string fileName = ObjectType.Name.Split('+')[ObjectType.Name.Split('+').Length - 1];
			fileName += SettingsFileExtension;
			SerializeToFile(
				GetFullFilePathInLocalAppdata(fileName, ApplicationName, SubfolderNameInApplication, CompanyName, true),
				settingsObject);
		}

		public static T GetSettings<T>(string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
		{
			return (T)GetSettings(typeof(T), ApplicationName, SubfolderNameInApplication, CompanyName);
		}

		public static object GetSettings(Type ObjectType, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
		{
			string fileName = ObjectType.Name.Split('+')[ObjectType.Name.Split('+').Length - 1];
			fileName += SettingsFileExtension;
			return DeserializeFromFile(
				ObjectType,
				GetFullFilePathInLocalAppdata(fileName, ApplicationName, SubfolderNameInApplication, CompanyName));
		}

		//public static bool SetSetting(string settingGroup, string SettingName, string Value, string ApplicationName, string fileName, string SubfolderNameInApplication = null, string CompanyName = "FJH", bool EnsurePathExists = true)
		//{
		//	string settingsFilename = GetFullFilePathInLocalAppdata(ApplicationName, fileName, SubfolderNameInApplication, CompanyName, true);

		//	//XPathDocument document = new XPathDocument(@"c:\testfile3.xml");//;settingsFilename);
		//	//XPathNavigator navigator = document.CreateNavigator();

		//	//XPathNavigator rootNode = navigator.SelectSingleNode("/settings");
		//	//XPathNavigator settingsGroupNode = navigator.SelectSingleNode("/settings/" + settingGroup);
		//	//XPathNavigator settingNode = navigator.SelectSingleNode("/settings/" + settingGroup + "/" + SettingName);
		//	////XPathNavigator nodes = navigator.SelectSingleNode("/settings/networkinterop/RootUriForVsPublishing1");
		//	////settingNode.SetValue(Value);

		//	//Settings.Default.Properties.Add(new System.Configuration.SettingsProperty("MyProp1") { DefaultValue = "MyVal1" });
		//	//Settings.Default.Save();
		//	//Settings.Default["MySetting1"] = "Test value";
		//	//Settings.Default.Save();

		//	Serialize<NetworkInteropSettings>(@"c:\testfile2.xml",
		//		new NetworkInteropSettings() { RootUriForVsPublishing = "http://" + "fjh.dyndns.org" + "/ownapplications" });

		//	NetworkInteropSettings ni = Deserialize<NetworkInteropSettings>(@"c:\testfile2.xml");
		//	MessageBox.Show(ni.RootUriForVsPublishing);

		//	//nodes.MoveNext();
		//	//XPathNavigator nodesNavigator = nodes.Current;

		//	//XPathNodeIterator nodesText = nodesNavigator.SelectDescendants(XPathNodeType.Text, false);

		//	//while (nodesText.MoveNext())
		//	//	Console.WriteLine(nodesText.Current.Value);

		//	return true;
		//}

		[Serializable]
		public class NetworkInteropSettings
		{
			public string RootUriForVsPublishing { get; set; }
		}

		private static void SerializeToFile<T>(string file, T settingsObject)
		{
			//XmlSerializer xs = new XmlSerializer(settingsObject.GetType());
			//StreamWriter writer = File.CreateText(file);
			//xs.Serialize(writer, settingsObject);
			//writer.Flush();
			//writer.Close();
			SerializeToFile(file, settingsObject);
		}

		private static object LockObject = new object();
		private static void SerializeToFile(string file, object settingsObject)
		{
			lock (LockObject)
			{
				//TODO: Never change this serializer to binary for instance, otherwise properties with XmlIgnore attribute will not work as they should
				XmlSerializer xs = new XmlSerializer(settingsObject.GetType());
				StreamWriter writer = File.CreateText(file);
				xs.Serialize(writer, settingsObject);
				writer.Flush();
				writer.Close();
			}
		}

		private static T DeserializeFromFile<T>(string file)
		{
			return (T)DeserializeFromFile(typeof(T), file);
		}

		private static object DeserializeFromFile(Type ObjectType, string file)
		{
			lock (LockObject)
			{
				if (!File.Exists(file)) return ObjectType.GetConstructor(new Type[] { }).Invoke(new object[] { });
				XmlSerializer xs 
            = new XmlSerializer(ObjectType);
				StreamReader reader = File.OpenText(file);
				object c = xs.Deserialize(reader);
				reader.Close();
				return c;
			}
		}

		public static Environment.SpecialFolder? GetLongestSpecialFolderMatch(string fullpath, out string RelativeToSpecialFolder_NoSlash)
		{
			Environment.SpecialFolder? currSf = null;
			foreach (Environment.SpecialFolder sf in Enum.GetValues(typeof(Environment.SpecialFolder)))
			{
				string sfpath = Environment.GetFolderPath(sf).TrimEnd('\\');
				if (fullpath.StartsWith(sfpath, StringComparison.InvariantCultureIgnoreCase))
				{
					if (!currSf.HasValue)
						currSf = sf;
					else
					{
						//if (sfpath.Split(new char[]{'\\'}, StringSplitOptions.RemoveEmptyEntries).Length
						//    > Environment.GetFolderPath(currSf.Value).TrimEnd('\\').Split(new char[]{'\\'}, StringSplitOptions.RemoveEmptyEntries).Length)
						if (sfpath.Length > Environment.GetFolderPath(currSf.Value).TrimEnd('\\').Length)
							currSf = sf;
					}
				}
			}
			if (currSf.HasValue)
			{
				int startIndex = Environment.GetFolderPath(currSf.Value).TrimEnd('\\').Length + 1;
				if (startIndex < fullpath.Length)
					RelativeToSpecialFolder_NoSlash = fullpath.Substring(Environment.GetFolderPath(currSf.Value).TrimEnd('\\').Length + 1);
				else
					RelativeToSpecialFolder_NoSlash = "";
			}
			else
				RelativeToSpecialFolder_NoSlash = fullpath;
			return currSf;
		}
	}
}