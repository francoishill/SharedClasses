using System;
using System.Windows.Forms;
using System.IO;
using System.Xml.XPath;
//using TestingSharedClasses.Properties;

public class SettingsInterop
{
	private const string SettingsFileExtension = ".fset";

	/// <summary>
	/// Always returned without leading backslash.
	/// </summary>
	/// <param name="ApplicationName">The name of the application which will be appended at end of path (appdata\CompanyName\ApplicationName).</param>
	/// <param name="CompanyName">The name of the subfolder in appdata path (appdata\CompanyName\ApplicationName).</param>
	/// <returns></returns>
	public static string LocalAppdataPath(string ApplicationName, string CompanyName = "FJH", bool EnsurePathExists = true)
	{
		string LocalAppdataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		FileSystemInterop.RemoveLeadingAndTrailingBackslashes(ref LocalAppdataPath);
		FileSystemInterop.RemoveLeadingAndTrailingBackslashes(ref ApplicationName);
		FileSystemInterop.RemoveLeadingAndTrailingBackslashes(ref CompanyName);
		string returnPath = LocalAppdataPath + "\\" + CompanyName + "\\" + ApplicationName;
		if (EnsurePathExists && !Directory.Exists(returnPath)) Directory.CreateDirectory(returnPath);
		return returnPath;
	}

	public static string GetFullFilePathInLocalAppdata(string fileName, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH", bool EnsurePathExists = true)
	{
		FileSystemInterop.RemoveLeadingAndTrailingBackslashes(ref ApplicationName);
		FileSystemInterop.RemoveLeadingAndTrailingBackslashes(ref SubfolderNameInApplication);
		FileSystemInterop.RemoveLeadingAndTrailingBackslashes(ref fileName);
		FileSystemInterop.RemoveLeadingAndTrailingBackslashes(ref CompanyName);
		bool isSubfolderDefined = SubfolderNameInApplication != null && SubfolderNameInApplication.Trim().Length > 0;
		string folderOfFile =
			LocalAppdataPath(ApplicationName, CompanyName, EnsurePathExists) +
			(isSubfolderDefined ? "\\" + SubfolderNameInApplication : "");
		if (EnsurePathExists && !Directory.Exists(folderOfFile)) Directory.CreateDirectory(folderOfFile);
		return folderOfFile + "\\" + fileName;
	}
	
	public static void FlushSettings<T>(T settingsObject, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{
		string fileName = typeof(T).Name.Split('+')[typeof(T).Name.Split('+').Length - 1];
		fileName += SettingsFileExtension;
		SerializeToFile<T>(
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
		System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(settingsObject.GetType());
		StreamWriter writer = File.CreateText(file);
		xs.Serialize(writer, settingsObject);
		writer.Flush();
		writer.Close();
	}

	private static T DeserializeFromFile<T>(string file)
	{
		return (T)DeserializeFromFile(typeof(T), file);
	}

	private static object DeserializeFromFile(Type ObjectType, string file)
	{
		if (!File.Exists(file)) return ObjectType.GetConstructor(new Type[] { }).Invoke(new object[] { });
		System.Xml.Serialization.XmlSerializer xs 
            = new System.Xml.Serialization.XmlSerializer(
					ObjectType);
		StreamReader reader = File.OpenText(file);
		object c = xs.Deserialize(reader);
		reader.Close();
		return c;
	}
}