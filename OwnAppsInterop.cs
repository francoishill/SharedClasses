using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using System.IO;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
//using System.Xml.Linq;

namespace SharedClasses
{
	public static class OwnAppsInterop
	{
		/* Additional dependencies for this file:
			Class: EncryptionInterop
			Class: Helpers
			Class: LicensingInterop_Shared
			Class: Logging
			Class: NetworkInteropSimple
			Class: NsisInterop
			Class: PhpInterop
			Class: PublishInterop
			Class: RegexInterop
			Class: StandaloneUploaderInterop
			Class: UploadingProtocolTypesEnum
			Assembly: WindowsBase
			Assembly: PresentationCore
			Assembly: PresentationFramework
			Assembly: System.Xaml*/

		private const string cAppNameForStoringSettings = "AnalyseProjects";//Constant for now
		private const string cRegistryEntriesFilename = "RegistryEntries.json";
		//private const string cPolicies  ||||   Implement this
		public static string[] GetPicturesInDirectory(string dirPath)
		{
			if (!Directory.Exists(dirPath))
				return new string[0];
			return Directory.GetFiles(dirPath, "*.*")//Not searching sub directories too
				.Where(
					s => s.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase)
					|| s.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase)
					|| s.EndsWith(".bmp", StringComparison.InvariantCultureIgnoreCase))
				.ToArray();
		}


		public static string GetRegistryEntriesFullfilepath(string applicationName, out string errorIfFailed)
		{
			//int implementthisAbove;
			KeyValuePair<string, ApplicationTypes>? csprojFullpathAndApptype = GetCsprojFullpathFromApplicationName(applicationName, out errorIfFailed);
			if (!csprojFullpathAndApptype.HasValue) return null;
			return Path.Combine(Path.GetDirectoryName(csprojFullpathAndApptype.Value.Key), "Properties", cRegistryEntriesFilename);
		}

		public static int StringIndexOfIgnoreInsideStringOrChar(this string haystack, string needle, int startIndex = 0, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
		{//Does not currently ignore char (altough method name states it)
			int currentIndexOf = haystack.IndexOf(needle, startIndex, comparisonType);

			while (IsIndexInsideString(StringTypes.Both, ref haystack, currentIndexOf, needle.Length))
			{
				startIndex = currentIndexOf + needle.Length;//Skip this char as it part of a string @"..." or "..."
				//startIndex += currentIndexOf + needle.Length;//Skip this char as it part of a string @"..." or "..."
				currentIndexOf = haystack.IndexOf(needle, startIndex);
			}
			return currentIndexOf;
		}

		public static int StringIndexOfIgnoreInsideStringOrChar(this string haystack, char needle, int startIndex = 0, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
		{
			return StringIndexOfIgnoreInsideStringOrChar(haystack, needle.ToString(), startIndex, comparisonType);
		}

		public enum ApplicationTypes { WPF, Winforms, Console, DLL }

		public static Dictionary<string, string> GetListOfInstalledApplications()
		{
			Dictionary<string, string> tmpdict = new Dictionary<string, string>();
			using (var uninstallRootKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryInterop.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32)
				.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
			{
				if (null == uninstallRootKey)
					return null;
				var appKeys = uninstallRootKey.GetSubKeyNames().ToArray();
				foreach (string appkeyname in appKeys)
				{
					try
					{
						using (RegistryKey appkey = uninstallRootKey.OpenSubKey(appkeyname))
						{
							object publisherValue = appkey.GetValue("Publisher");
							if (publisherValue == null)
								continue;
							if (!publisherValue.ToString().Trim().Equals(NsisInterop.cDefaultPublisherName))
								continue;
							/*object urlInfoValue = appkey.GetValue("URLInfoAbout");
							if (urlInfoValue == null)
								continue;//The value must exist for URLInfoAbout
							if (!urlInfoValue.ToString().StartsWith(SettingsSimple.HomePcUrls.Instance.AppsPublishingRoot, StringComparison.InvariantCultureIgnoreCase))
								continue;//The URLInfoAbout value must start with our AppsPublishingRoot*/

							//If we reached this point in the foreach loop, this application is one of our own, now make sure the EXE also exists
							object displayIcon = appkey.GetValue("DisplayIcon");
							//For now we use the DisplayIcon, is this the best way, what if DisplayIcon is different from EXE
							if (displayIcon == null)
								continue;//We need the DisplayIcon value, it contains the full path of the EXE
							if (!File.Exists(displayIcon.ToString()))
								continue;//The application is probably not installed
							//At this point we know the registry entry is our own application and it is actaully installed (file exists)
							string exePath = displayIcon.ToString();
							string appname = Path.GetFileNameWithoutExtension(exePath);
							if (!tmpdict.ContainsKey(appname))
								tmpdict.Add(appname, exePath);
						}
					}
					catch { }
				}
			}
			return tmpdict;
		}

		public static string GetSolutionPathFromApplicationName(string ApplicationName, out string errorIfFailed)
		{
			string solutionDir = Path.Combine(RootVSprojectsDir, ApplicationName);
			if (!Directory.Exists(solutionDir))
			{
				errorIfFailed = "Directory does not exist: " + solutionDir;
				return null;
			}
			var solutionFiles = Directory.GetFiles(solutionDir, "*.sln");//Full paths
			if (solutionFiles.Length == 0)
			{
				errorIfFailed = "Cannot find solution file in dir: " + solutionDir;
				return null;
			}
			else
			{
				var solutionFilesSameNameAsApplicationName =
						solutionFiles.Where(f => Path.GetFileNameWithoutExtension(f).Equals(ApplicationName)).ToArray();
				if (solutionFiles.Length > 1 && solutionFilesSameNameAsApplicationName.Length > 1)
				{
					errorIfFailed = "Multiple solution files found for application "
						+ ApplicationName + ", none of them having same name as application:"
						+ Environment.NewLine
						+ string.Join(Environment.NewLine, solutionFiles);
					return null;

				}
				//else if (solutionFiles.Length == 1 || solutionFilesSameNameAsApplicationName.Length == 1)
				//{
				errorIfFailed = null;
				return solutionFiles.Length == 1 ? solutionFiles.First() : solutionFilesSameNameAsApplicationName.First();
			}
		}

		public static string GetPathRelativeToVsRootFolder(string path, out string errorIfFailed)
		{
			if (!path.StartsWith(RootVSprojectsDir, StringComparison.InvariantCultureIgnoreCase))
			{
				errorIfFailed = string.Format("Cannot GetPathRelativeToVsRootFolder, the path = '{0}' must be inside the root folder = '{1}'.",
					path, RootVSprojectsDir);
				return null;
			}
			errorIfFailed = null;
			return path.Substring(RootVSprojectsDir.TrimEnd('\\').Length + 1);//We do not want to include the first \\
		}

		public static string GetAppIconPath(string ApplicationName, out string errorIfFailed)
		{
			string tmpSolutionPath = GetSolutionPathFromApplicationName(ApplicationName, out errorIfFailed);
			if (tmpSolutionPath == null)
				return null;

			string expectedNormalPath = Path.Combine(Path.GetDirectoryName(tmpSolutionPath), ApplicationName, "app.ico");
			if (File.Exists(expectedNormalPath))
			{
				errorIfFailed = null;
				return expectedNormalPath;
			}

			string expectedOtherPath = Path.Combine(Path.GetDirectoryName(tmpSolutionPath), "app.ico");
			if (File.Exists(expectedOtherPath))
			{
				errorIfFailed = null;
				return expectedOtherPath;
			}

			errorIfFailed = "Unable to find 'app.ico' in the normal locations of the VSproject directory for the application '" + ApplicationName + "'";
			return null;
		}

		public static bool DirIsValidSvnPath(string dir)
		{
			if (!Directory.Exists(dir))
				return false;
			return Directory.Exists(System.IO.Path.Combine(dir, ".svn"));
		}

		public static bool DirIsValidGitPath(string dir)
		{
			if (!Directory.Exists(dir))
				return false;
			return Directory.Exists(System.IO.Path.Combine(dir, ".git"));
		}

		private static XmlDocument OpenCsprojAsXmlDocument(string csprojFullPath, ref string namespacePrefix, out XmlNamespaceManager xmlNSmanager, out string errorIfFailed)
		{
			if (!namespacePrefix.EndsWith(":"))
			{
				errorIfFailed = "Error Opening Csproj as XmlDocument, namespacePrefix must end with ':'";
				xmlNSmanager = null;
				return null;
			}

			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(csprojFullPath);

			XmlNamespaceManager nsmgr = null;
			string nsmgrPrefix = namespacePrefix;
			if (xmlDoc.DocumentElement.Attributes["xmlns"] == null)
			{
				errorIfFailed = "Cannot find namespace in Xml Text: " + csprojFullPath;
				xmlNSmanager = null;
				return null;
			}
			else
			{
				string xmlns = xmlDoc.DocumentElement.Attributes["xmlns"].Value;
				nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);

				nsmgr.AddNamespace(nsmgrPrefix.TrimEnd(':'), xmlns);
			}

			errorIfFailed = null;
			xmlNSmanager = nsmgr;
			return xmlDoc;
		}

		private static string GetOutputTypeNodeInnerText(ref XmlDocument xmlDoc, ref XmlNamespaceManager nsmgr, string nsmgrPrefix, string csprojFullPath_JustForMessages, out string errorIfFailed)
		{
			XmlNodeList OutputTypeNodes = xmlDoc.SelectNodes(string.Format("/{0}Project/{0}PropertyGroup/{0}OutputType", nsmgrPrefix), nsmgr);
			if (OutputTypeNodes.Count == 0)
			{
				errorIfFailed = "Unable to find Project/PropertyGroup/OutputType node in .csproj file: " + csprojFullPath_JustForMessages;
				return null;
			}
			else if (OutputTypeNodes.Count > 1)
			{
				errorIfFailed = "Found multiple Project/PropertyGroup/OutputType nodes in .csproj file: " + csprojFullPath_JustForMessages;
				return null;
			}
			else
			{
				errorIfFailed = null;
				return OutputTypeNodes[0].InnerText.Trim();
			}
		}

		/// <summary>
		/// True means we got a single Xml node (WPF app), False means no Xml node (not WPF app), Null means an unexpected error
		/// </summary>
		/// <param name="xmlDoc"></param>
		/// <param name="nsmgr"></param>
		/// <param name="nsmgrPrefix"></param>
		/// <param name="csprojFullPath_JustForMessages"></param>
		/// <param name="errorIfFailed"></param>
		/// <returns></returns>
		private static bool? GetApplicationDefinitionXmlNode(ref XmlDocument xmlDoc, ref XmlNamespaceManager nsmgr, string nsmgrPrefix, string csprojFullPath_JustForMessages, out XmlNode xmlNode, out string errorIfFailed)
		{
			XmlNodeList ApplicationDefinitionNodes = xmlDoc.SelectNodes(string.Format("/{0}Project/{0}ItemGroup/{0}ApplicationDefinition", nsmgrPrefix), nsmgr);
			if (ApplicationDefinitionNodes.Count == 0)//We assume its Winforms
			{
				xmlNode = null;
				errorIfFailed = null;
				return false;//We dont have this node
			}
			else if (ApplicationDefinitionNodes.Count > 1)
			{
				xmlNode = null;
				errorIfFailed = "Found multiple Project/ItemGroup/ApplicationDefinition nodes in .csproj file: " + csprojFullPath_JustForMessages;
				return null;//We have multiple nodes
			}
			else
			{
				xmlNode = ApplicationDefinitionNodes[0];
				errorIfFailed = null;
				return true;
			}
		}

		private static bool? GetCompileProgramCsNode(ref XmlDocument xmlDoc, ref XmlNamespaceManager nsmgr, string nsmgrPrefix, string csprojFullPath_JustForMessages, out XmlNode xmlNode, out string errorIfFailed)
		{
			XmlNodeList CompileNodes = xmlDoc.SelectNodes(string.Format("/{0}Project/{0}ItemGroup/{0}Compile", nsmgrPrefix), nsmgr);
			if (CompileNodes.Count == 0)
			{
				xmlNode = null;
				errorIfFailed = null;
				return false;//We do not have any <Compile> nodes
			}
			else// if (CompileNodes.Count > 0)
			{
				List<XmlNode> allNodes = new List<XmlNode>();
				foreach (XmlNode node in CompileNodes)
					allNodes.Add(node);
				var programCsNodesOnly = allNodes
					.Where(node => node.Attributes["Include"] != null
						&& node.Attributes["Include"].Value != null
						&& node.Attributes["Include"].Value.Equals("Program.cs", StringComparison.InvariantCultureIgnoreCase))
					.ToList();
				if (programCsNodesOnly.Count == 0)
				{
					xmlNode = null;
					errorIfFailed = null;
					return false;//We do not have any <Compile Include="Program.cs" > nodes
				}
				else if (programCsNodesOnly.Count > 1)
				{
					xmlNode = null;
					errorIfFailed = "Found multiple Project/ItemGroup/Compile nodes (with attribute Include='Program.cs)' in .csproj file: " + csprojFullPath_JustForMessages;
					return null;//We have multiple nodes
				}
				else
				{
					xmlNode = programCsNodesOnly[0];
					errorIfFailed = null;
					return true;
				}
			}
		}

		public static ApplicationTypes? GetApplicationTypeOfCsproj(string csprojFullPath, out string errorIfFailed)
		{
			if (csprojFullPath == null)
			{
				errorIfFailed = "Path to .csproj file may not be NULL";
				return null;
			}
			if (!File.Exists(csprojFullPath))
			{
				errorIfFailed = "Csproj file does not exist: " + csprojFullPath;
				return null;
			}
			try
			{
				string nsmgrPrefix = "NS:";
				XmlNamespaceManager nsmgr;
				XmlDocument xmlDoc = OpenCsprojAsXmlDocument(csprojFullPath, ref nsmgrPrefix, out nsmgr, out errorIfFailed);
				if (xmlDoc == null)
					return null;

				string outputType = GetOutputTypeNodeInnerText(ref xmlDoc, ref nsmgr, nsmgrPrefix, csprojFullPath, out errorIfFailed);
				if (outputType == null) return null;

				if (outputType.Equals("Library", StringComparison.InvariantCultureIgnoreCase))
					return ApplicationTypes.DLL;
				else if (outputType.Equals("Exe", StringComparison.InvariantCultureIgnoreCase))
					return ApplicationTypes.Console;
				else//It is WPF or Winforms, only WPF apps have an XML node (in csproj file) in this path: Project/ItemGroup/ApplicationDefinition
				{
					XmlNode outXmlNode;
					bool? gotApplicationDefinitionInnerText = GetApplicationDefinitionXmlNode(ref xmlDoc, ref nsmgr, nsmgrPrefix, csprojFullPath,
						out outXmlNode, out errorIfFailed);
					if (gotApplicationDefinitionInnerText == null) return null;

					if (gotApplicationDefinitionInnerText == false)//Winforms
						return ApplicationTypes.Winforms;
					else//Wpf
						return ApplicationTypes.WPF;
				}
			}
			catch (Exception exc)
			{
				errorIfFailed = "Error reading Csproj file: " + exc.Message;
				return null;
			}
		}

		public static Dictionary<string, ApplicationTypes> GetRelativePathsToCsProjsFromSolutionFile(string solutionFullPath, out string errIfFailed)
		{
			if (solutionFullPath == null)
			{
				errIfFailed = "Solution Fullpath may not be NULL";
				return null;
			}

			Dictionary<string, ApplicationTypes> tmpdict = new Dictionary<string, ApplicationTypes>();

			var fileLines = File.ReadAllLines(solutionFullPath);
			for (int i = 0; i < fileLines.Length; i++)
			{
				if (i == fileLines.Length - 1)//We cant use last line, the next line should be EndProject
					continue;
				if (fileLines[i].StartsWith("Project(\"{", StringComparison.InvariantCultureIgnoreCase)
					&& fileLines[i + 1].Trim().Equals("EndProject", StringComparison.InvariantCultureIgnoreCase))
				{
					string[] commaSplit = fileLines[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					if (commaSplit.Length < 2)//We need the second element [1]
						continue;
					string csprojRelPath = commaSplit[1]
						.Trim(' ', '"');

					OwnAppsInterop.ApplicationTypes? getApplicationType = GetApplicationTypeOfCsproj(Path.Combine(Path.GetDirectoryName(solutionFullPath), csprojRelPath), out errIfFailed);
					if (!getApplicationType.HasValue)
						return null;

					tmpdict.Add(csprojRelPath, getApplicationType.Value);
				}
			}

			if (tmpdict.Count == 0)
			{
				errIfFailed = "The solution file does not reference any .csproj files: " + solutionFullPath;
				return null;
			}

			errIfFailed = null;
			return tmpdict;
		}

		public static KeyValuePair<string, ApplicationTypes>? GetRelativePathToCsProjWithSameFilenameAsSolution(string solutionFullPath, out string errIfFailed)
		{
			var allcsProjsInSolution = GetRelativePathsToCsProjsFromSolutionFile(solutionFullPath, out errIfFailed);
			if (allcsProjsInSolution == null) return null;
			var possibleCsProjsWithSameName = allcsProjsInSolution
				.Where(kv => Path.GetFileNameWithoutExtension(kv.Key)
					.Equals(Path.GetFileNameWithoutExtension(solutionFullPath), StringComparison.InvariantCultureIgnoreCase)).ToList();
			if (possibleCsProjsWithSameName.Count == 0)
			{
				errIfFailed = "Could not find any .csproj file with same filename (without extension) as solution file: " + solutionFullPath;
				return null;
			}
			else if (possibleCsProjsWithSameName.Count > 1)
			{
				errIfFailed = "Multiple .csproj files found with same filename (without extension) as solution file: " + solutionFullPath;
				return null;
			}
			else
			{
				errIfFailed = null;
				return possibleCsProjsWithSameName[0];
			}
		}

		public static KeyValuePair<string, ApplicationTypes>? GetCsprojFullpathFromApplicationName(string applicationName, out string errorIfFailed)
		{
			string solutionPath = GetSolutionPathFromApplicationName(applicationName, out errorIfFailed);
			if (solutionPath == null) return null;
			KeyValuePair<string, ApplicationTypes>? csprojRelativePathAndApptype = GetRelativePathToCsProjWithSameFilenameAsSolution(solutionPath, out errorIfFailed);
			if (!csprojRelativePathAndApptype.HasValue) return null;
			string csprojFullpath = Path.Combine(Path.GetDirectoryName(solutionPath), csprojRelativePathAndApptype.Value.Key);
			return new KeyValuePair<string, ApplicationTypes>(csprojFullpath, csprojRelativePathAndApptype.Value.Value);
		}

		public static bool? IsAppIconImplemented(string csprojFullPath, out string AppIconRelativePath, out string errorIfFailed)
		{
			try
			{
				string nsmgrPrefix = "NS:";
				XmlNamespaceManager nsmgr;
				XmlDocument xmlDoc = OpenCsprojAsXmlDocument(csprojFullPath, ref nsmgrPrefix, out nsmgr, out errorIfFailed);
				if (xmlDoc == null)
				{
					AppIconRelativePath = null;
					return null;
				}

				XmlNodeList ApplicationIconNodes = xmlDoc.SelectNodes(string.Format("/{0}Project/{0}PropertyGroup/{0}ApplicationIcon", nsmgrPrefix), nsmgr);
				if (ApplicationIconNodes.Count == 0)
				{
					AppIconRelativePath = null;
					errorIfFailed = null;
					return false;//App Icon not implemented
				}
				else if (ApplicationIconNodes.Count > 1)
				{
					AppIconRelativePath = null;
					errorIfFailed = "Found multiple Project/PropertyGroup/ApplicationIcon nodes in .csproj file: " + csprojFullPath;
					return null;
				}
				else
				{
					AppIconRelativePath = ApplicationIconNodes[0].InnerText.Trim();
					errorIfFailed = null;
					if (string.IsNullOrWhiteSpace(AppIconRelativePath))
						return false;//If the <ApplicationIcon> node is empty
					else
						return true;
				}
			}
			catch (Exception exc)
			{
				AppIconRelativePath = null;
				errorIfFailed = "Error determining IsAppIconImplemented: " + exc.Message;
				return null;
			}
		}

		/// <summary>
		/// Get the application entry point, if NULL returned, check the 'errorIfFailed' out parameter.
		/// </summary>
		/// <param name="csprojFullPath">The full path to the .csproj file.</param>
		/// <param name="appType">The application type of the .csproj file.</param>
		/// <param name="additionalChecksIfFoundEntryPoint">Additional checks that can be done, a string is returned, if NULL the the check passed, else this returned string will be used as the 'errorIfFailed'.</param>
		/// <param name="errorIfFailed">The error if this method failed.</param>
		/// <returns>Returns the application Main entry point (if found).</returns>
		private static string GetAppMainEntryPointCodeBlock(string csprojFullPath, ApplicationTypes appType, out string errorIfFailed)
		{
			Func<string, int, string> additionalChecksIfFoundEntryPoint = delegate { return null; };//Just return "success" by default (NULL means no error)

			try
			{
				string nsmgrPrefix = "NS:";
				XmlNamespaceManager nsmgr;
				XmlDocument xmlDoc = OpenCsprojAsXmlDocument(csprojFullPath, ref nsmgrPrefix, out nsmgr, out errorIfFailed);
				if (xmlDoc == null) return null;

				switch (appType)
				{
					case ApplicationTypes.WPF:
						XmlNode tmpAppDefxmlnode;
						bool? gotAppDefXmlNode = GetApplicationDefinitionXmlNode(ref xmlDoc, ref nsmgr, nsmgrPrefix, csprojFullPath, out tmpAppDefxmlnode, out errorIfFailed);
						if (gotAppDefXmlNode != true) return null;
						string appXamlFileRelativePath = tmpAppDefxmlnode.Attributes["Include"].Value;
						string appXamlStartupBlock = GetOverrideOnStartupBlockInAppXamlOfWpfApplication_RemoveComments(Path.Combine(Path.GetDirectoryName(csprojFullPath), appXamlFileRelativePath), additionalChecksIfFoundEntryPoint, out errorIfFailed);
						if (appXamlStartupBlock == null)
							return null;//errorIfFailed already set
						return appXamlStartupBlock;
					case ApplicationTypes.Winforms://Winforms and Console almost the same, Console only has a parameter to the void main method
					case ApplicationTypes.Console:
						XmlNode programNode;
						bool? gotCompileNodes = GetCompileProgramCsNode(ref xmlDoc, ref nsmgr, nsmgrPrefix, csprojFullPath, out programNode, out errorIfFailed);
						if (gotCompileNodes == null) return null;
						string programCsFileRelativePath = programNode.Attributes["Include"].Value;

						string[] expectedOneOfMethodStartStrings =
							appType == ApplicationTypes.Winforms
							? new string[] { "static void Main(" }
							: new string[] { "static void Main(string[]", "static int Main(string[]" };

						string programCsVoidMainBlock = null;
						List<string> tmpErrors = new List<string>();
						foreach (var expBlock in expectedOneOfMethodStartStrings)
						{
							additionalChecksIfFoundEntryPoint = (contentOfFileContainingEntryPoint, indexOfEntryPointInFile) =>
							{
								int currentChar = indexOfEntryPointInFile;
								while (--currentChar >= 0//We -- first so we do not include the start of 'static void Main...'
									&& (char.IsWhiteSpace(contentOfFileContainingEntryPoint, currentChar)
										|| contentOfFileContainingEntryPoint[currentChar] == '\r'))
								{ }
								const string cTextOfStaThread = "[STAThread]";
								if (currentChar >= 0)//We are at a non-whitespace char now
									if (contentOfFileContainingEntryPoint[currentChar] == ']')//Now try to find [STAThread]
									{
										int indexOfSTAThreadStart = currentChar - (cTextOfStaThread.Length - 1);//-1 because already at last char
										if (indexOfSTAThreadStart >= 0//Just make sure not out of bounds
											&& contentOfFileContainingEntryPoint.Substring(indexOfSTAThreadStart, cTextOfStaThread.Length)
											.Equals(cTextOfStaThread))
											return null;
									}
								return "Could not find " + cTextOfStaThread + " before the application Main entry point";
							};
							string tmpCodeBlock = ExtractMethodBlockFromSourcecodeFile(Path.Combine(Path.GetDirectoryName(csprojFullPath), programCsFileRelativePath), expBlock, additionalChecksIfFoundEntryPoint, out errorIfFailed);
							if (tmpCodeBlock == null)
								tmpErrors.Add(errorIfFailed);
							if (!string.IsNullOrEmpty(tmpCodeBlock))
							{
								programCsVoidMainBlock = tmpCodeBlock;
								break;
							}
						}
						if (programCsVoidMainBlock == null)
						{
							errorIfFailed = "Unable to 'GetAppMainEntryPointCodeBlock'"
								+ (tmpErrors.Count > 0
									? ", the following errors were recorded: " + string.Join("|", tmpErrors)
									: "");
							return null;
						}
						return programCsVoidMainBlock;
					case ApplicationTypes.DLL:
						errorIfFailed = "Application type DLL does not support GetAppMainEntryPointCodeBlock.";
						return null;
					default:
						errorIfFailed = "GetAppMainEntryPointCodeBlock is not implemented for enum ApplicationTypes = " + appType.ToString();
						return null;
				}
			}
			catch (Exception exc)
			{
				errorIfFailed = "Exception occurred in IsAutoUpdatingImplemented (for csproj '" + csprojFullPath + "'): " + exc.Message;
				return null;
			}
		}

		public static bool? IsUnhandledExceptionHandlingImplemented(string csprojFullPath, ApplicationTypes appType, out string errorIfFailed)
		{
			string expectedStringInCode = "AppDomain.CurrentDomain.UnhandledException +=";
			string mainEntryPointCodeBlock = GetAppMainEntryPointCodeBlock(csprojFullPath, appType, out errorIfFailed);
			switch (appType)
			{
				case ApplicationTypes.WPF:
					if (mainEntryPointCodeBlock == null) return null;
					if (string.IsNullOrWhiteSpace(mainEntryPointCodeBlock)) return false;
					//Comments are already removed not check that we have
					return mainEntryPointCodeBlock.StringIndexOfIgnoreInsideStringOrChar(expectedStringInCode) != -1;
				case ApplicationTypes.Winforms:
				case ApplicationTypes.Console:
					if (mainEntryPointCodeBlock == null) return null;
					if (string.IsNullOrWhiteSpace(mainEntryPointCodeBlock)) return false;
					return mainEntryPointCodeBlock.StringIndexOfIgnoreInsideStringOrChar(expectedStringInCode) != -1;
				case ApplicationTypes.DLL:
					return false;
				default:
					return null;//The errorIfFailed will be populated with an error message inside above called method GetAppMainEntryPointCodeBlock
			}
		}

		public static bool? IsAutoUpdatingImplemented_AndNotUsingOwnUnhandledExceptionHandler(string csprojFullPath, ApplicationTypes appType, out string errorIfFailed)
		{
			string expectedStringInCode = "AutoUpdating.CheckForUpdates_ExceptionHandler(";
			string notAllowedStringInCode = "AppDomain.CurrentDomain.UnhandledException";
			string mainEntryPointCodeBlock = GetAppMainEntryPointCodeBlock(csprojFullPath, appType, out errorIfFailed);
			switch (appType)
			{
				case ApplicationTypes.WPF:
					if (mainEntryPointCodeBlock == null) return null;
					if (string.IsNullOrWhiteSpace(mainEntryPointCodeBlock)) return false;//Main entry point found but no code inside it
					if (mainEntryPointCodeBlock.StringIndexOfIgnoreInsideStringOrChar(notAllowedStringInCode) != -1)
					{
						errorIfFailed = "Not allowed to handle UnhandledExceptions by own handler, supposed to use CheckForUpdates_ExceptionHandler. This code may not be inside main entry point: '" + notAllowedStringInCode + "'";
						return null;
					}
					//Comments are already removed not check that we have
					return mainEntryPointCodeBlock.StringIndexOfIgnoreInsideStringOrChar(expectedStringInCode) != -1;
				case ApplicationTypes.Winforms:
				case ApplicationTypes.Console:
					if (mainEntryPointCodeBlock == null) return null;
					if (string.IsNullOrWhiteSpace(mainEntryPointCodeBlock)) return false;//Main entry point found but no code inside it
					if (mainEntryPointCodeBlock.StringIndexOfIgnoreInsideStringOrChar(notAllowedStringInCode) != -1)
					{
						errorIfFailed = "Not allowed to handle UnhandledExceptions by own handler, supposed to use CheckForUpdates_ExceptionHandler. This code may not be inside main entry point: '" + notAllowedStringInCode + "'";
						return null;
					}
					return mainEntryPointCodeBlock.StringIndexOfIgnoreInsideStringOrChar(expectedStringInCode) != -1;
				case ApplicationTypes.DLL:
					return false;
				default:
					return null;//The errorIfFailed will be populated with an error message inside above called method GetAppMainEntryPointCodeBlock
			}
		}

		public static bool? IsLicensingImplemented(string csprojFullPath, ApplicationTypes appType, out string errorIfFailed)
		{
			string expectedStringInCode = "LicensingInterop_Client.Client_ValidateLicense(";
			string mainEntryPointCodeBlock = GetAppMainEntryPointCodeBlock(csprojFullPath, appType, out errorIfFailed);
			switch (appType)
			{
				case ApplicationTypes.WPF:
					if (mainEntryPointCodeBlock == null) return null;
					if (string.IsNullOrWhiteSpace(mainEntryPointCodeBlock)) return false;
					//Comments are already removed not check that we have
					return mainEntryPointCodeBlock.StringIndexOfIgnoreInsideStringOrChar(expectedStringInCode) != -1;
				case ApplicationTypes.Winforms:
				case ApplicationTypes.Console:
					if (mainEntryPointCodeBlock == null) return null;
					if (string.IsNullOrWhiteSpace(mainEntryPointCodeBlock)) return false;
					return mainEntryPointCodeBlock.StringIndexOfIgnoreInsideStringOrChar(expectedStringInCode) != -1;
				case ApplicationTypes.DLL:
					return false;
				default:
					return null;//The errorIfFailed will be populated with an error message inside above called method GetAppMainEntryPointCodeBlock
			}
		}

		public static bool? ArePoliciesImplemented(string csprojFullPath, ApplicationTypes appType, out string errorIfFailed)
		{
			//int implementthis;
			errorIfFailed = null; return null;
		}

		private static bool _isIndexInsideNormalSingleLineString(ref string haystack, int needleIndex, int needleLength)
		{
			var singlelineStringMatches = Regex.Matches(haystack, "([\"'])(?<q>.+?)\\1");
			for (int i = 0; i < singlelineStringMatches.Count; i++)
				if (singlelineStringMatches[i].Index < needleIndex
					&& singlelineStringMatches[i].Index + singlelineStringMatches[i].Length > needleIndex + needleLength)
					return true;

			return false;

			/*int indexOfNormalStringStart = haystack.IndexOf("\"", startIndex);
			if (indexOfNormalStringStart == -1)
				return false;
			int indexOfStringEnd = haystack.IndexOf("\"", indexOfNormalStringStart + 1);
			if (indexOfStringEnd == -1) return true;//indexOfStringEnd = haystack.Length - 1;
			if (indexOfStringEnd > 0 && haystack[indexOfStringEnd - 1] == '\\')//If we escaped the " by using \"
				return IsIndexInsideNormalSingleLineString(ref haystack, needleIndex, needleLength, indexOfStringEnd + 1);
			if (indexOfStringEnd < needleIndex)
				return IsIndexInsideNormalSingleLineString(ref haystack, needleIndex, needleLength, indexOfStringEnd + 1);
			return indexOfNormalStringStart < needleIndex && indexOfStringEnd > needleIndex + needleLength;*/
		}

		private static bool _isIndexInsideStringMultilineLiteral(ref string haystack, int needleIndex, int needleLength)
		{
			//Original regex for multiline strings, obtained from http://go4answers.webhost4life.com/Example/find-multiline-string-literals-bla-99482.aspx
			//var matches = Regex.Matches(fileContent, @"(@""(?:[^""]+|"""")*""(?!""))|(@""(?:[^""]+|"""")*)");
			var multilineLiteralMatches = Regex.Matches(haystack, @"(@""(?:[^""]*|"""")*""(?!""))|(@""(?:[^""]*|"""")*)");
			for (int i = 0; i < multilineLiteralMatches.Count; i++)
				if (multilineLiteralMatches[i].Index < needleIndex
					&& multilineLiteralMatches[i].Index + multilineLiteralMatches[i].Length > needleIndex + needleLength)
					return true;

			return false;

			/*int indexOfLiteralStart = haystack.IndexOf("@\"", startIndex);
			if (indexOfLiteralStart == -1)
				return false;
			int indexOfLiteralEnd = haystack.IndexOf("\"", indexOfLiteralStart + 1);
			if (indexOfLiteralEnd == -1) return true;//indexOfLiteralEnd = haystack.Length - 1;
			if (indexOfLiteralEnd > 0 && haystack[indexOfLiteralEnd - 1] == '"')//If we escaped the " by using two quotes like ""
				return IsIndexInsideStringMultilineLiteral(ref haystack, needleIndex, needleLength, indexOfLiteralEnd + 1);
			if (indexOfLiteralEnd < needleIndex)
				return IsIndexInsideStringMultilineLiteral(ref haystack, needleIndex, needleLength, indexOfLiteralEnd + 1);
			return indexOfLiteralStart < needleIndex && indexOfLiteralEnd > needleIndex + needleLength;*/
		}

		public enum StringTypes { SinglelineString, MultilineLiteral, Both };
		public static bool IsIndexInsideString(StringTypes stringTypes, ref string haystack, int needleIndex, int needleLength)
		{
			if (stringTypes == StringTypes.SinglelineString
				|| stringTypes == StringTypes.Both)
				if (_isIndexInsideNormalSingleLineString(ref haystack, needleIndex, needleLength))
					return true;
			if (stringTypes == StringTypes.MultilineLiteral
				|| stringTypes == StringTypes.Both)
				if (_isIndexInsideStringMultilineLiteral(ref haystack, needleIndex, needleLength))
					return true;
			return false;
		}

		public static string ExtractMethodBlockFromSourcecodeFile(string sourceFilepath, string expectedMethodStartString, Func<string, int, string> additionalChecksIfFoundEntryPoint, out string errorIfFailed)
		{
			try
			{
				string csContent = File.ReadAllText(sourceFilepath);
				RemoveCommentsInCsFile(ref csContent);

				int startOfExpectedString = csContent.StringIndexOfIgnoreInsideStringOrChar(expectedMethodStartString);
				if (startOfExpectedString == -1)//We did not find the expected string
				{
					errorIfFailed = null;
					return "";
				}
				else
				{
					int indexFirstOpenCurly = csContent.StringIndexOfIgnoreInsideStringOrChar('{', startOfExpectedString + expectedMethodStartString.Length);

					int tmpindexNextOpenCurly = csContent.StringIndexOfIgnoreInsideStringOrChar('{', indexFirstOpenCurly + 1);
					int tmpindexNextCloseCurly = csContent.StringIndexOfIgnoreInsideStringOrChar('}', (tmpindexNextOpenCurly == -1 ? indexFirstOpenCurly + 1 : indexFirstOpenCurly + 1));
					if (tmpindexNextOpenCurly == -1)
						tmpindexNextOpenCurly = csContent.Length - 1;
					else
					{

						int openCount = 0;
						if (tmpindexNextCloseCurly < tmpindexNextOpenCurly)//We do not have sub sections (for loops, etc) inside the override OnStartup
						{
							errorIfFailed = null;
							string additionalChecksPossibleErrorString = additionalChecksIfFoundEntryPoint(csContent, startOfExpectedString);
							if (additionalChecksPossibleErrorString != null)
							{
								errorIfFailed = additionalChecksPossibleErrorString;
								return null;
							}
							return csContent.Substring(indexFirstOpenCurly, tmpindexNextCloseCurly - indexFirstOpenCurly);
						}
						else
							openCount = 1;

						while (openCount > 0)
						{
							if (tmpindexNextCloseCurly > tmpindexNextOpenCurly)
							{
								openCount++;
								tmpindexNextCloseCurly = csContent.StringIndexOfIgnoreInsideStringOrChar('}', tmpindexNextOpenCurly + 1);//This must happen before next line (as we set tmpindexNextOpenCurly again)
								tmpindexNextOpenCurly = csContent.StringIndexOfIgnoreInsideStringOrChar('{', tmpindexNextOpenCurly + 1);
							}
							else if (tmpindexNextCloseCurly < tmpindexNextOpenCurly)
							{
								openCount--;
								if (openCount == 0)
									break;
								tmpindexNextOpenCurly = csContent.StringIndexOfIgnoreInsideStringOrChar('{', tmpindexNextCloseCurly + 1);
								tmpindexNextCloseCurly = csContent.StringIndexOfIgnoreInsideStringOrChar('}', tmpindexNextCloseCurly + 1);
							}
							else
								break;

							if (tmpindexNextOpenCurly == -1)
								tmpindexNextOpenCurly = csContent.Length - 1;
							if (tmpindexNextCloseCurly == -1)
								tmpindexNextCloseCurly = csContent.Length - 1;
						}
					}

					string tmpAdditionalChecksPossibleErrorString = additionalChecksIfFoundEntryPoint(csContent, startOfExpectedString);
					if (tmpAdditionalChecksPossibleErrorString != null)
					{
						errorIfFailed = tmpAdditionalChecksPossibleErrorString;
						return null;
					}
					errorIfFailed = null;
					return csContent.Substring(indexFirstOpenCurly, tmpindexNextCloseCurly - indexFirstOpenCurly + 1);
				}
			}
			catch (Exception exc)
			{
				errorIfFailed = "Error getting '" + expectedMethodStartString + "' in file '" + sourceFilepath + "': " + exc.Message;
				return null;
			}
		}

		private static string GetOverrideOnStartupBlockInAppXamlOfWpfApplication_RemoveComments(string appXamlFullPath, Func<string, int, string> additionalChecksIfFoundEntryPoint, out string errorIfFailed)
		{
			string xamlCsPath = appXamlFullPath + ".cs";
			if (!File.Exists(xamlCsPath))
			{
				errorIfFailed = null;
				return "";//We do not have a .cs file as the code-behind for the .xaml file
			}

			//We are only obtaining code inside 'override OnStartup' and not in event handler of Application.Startup, otherwise gets too complex
			/*
			//Check first to see if we have an event handler for Startup in the .xaml file
			string startupEventHandler = null;
			var xamlDoc = XDocument.Parse(File.ReadAllText(appXamlFullPath));
			var applicationNode = xamlDoc.Document.Elements().ToArray()[0];
			foreach (var attrib in applicationNode.Attributes())
				if (attrib.Name == "Startup")
				{
					startupEventHandler = attrib.Value;
					break;
				}*/

			return ExtractMethodBlockFromSourcecodeFile(xamlCsPath, "protected override void OnStartup", additionalChecksIfFoundEntryPoint, out errorIfFailed);

			//First parse the .xaml.cs file and remove comments
			//Just use code inside override OnStartup, if it sits inside the Eventhandler for Startup, we dont allow this,
			//We force user to place it inside override OnStartup

			/*//If we have override OnStartup() then we must check whether it contains base.OnStartup
			//If we have a Startup event handler, get code inside this eventhandler
			//
			//Also (can have an event handler and override OnStartup() which contains

			//string xamlCSfileContent = File.ReadAllText(csPath);*/
		}

		public static void RemoveCommentsInCsFile(ref string code)
		{
			//Obtained from http://stackoverflow.com/questions/3524317/regex-to-strip-line-comments-from-c-sharp/3524689#3524689
			var re = @"(@(?:""[^""]*"")+|""(?:[^""\n\\]+|\\.)*""|'(?:[^'\n\\]+|\\.)*')|//.*|/\*(?s:.*?)\*/";
			code = Regex.Replace(code, re, "$1");
		}
		//private const string cCsFileCommentsRegex = @"(/\*([^*]|[\r\n]|(\*+([^*/]|[\r\n])))*\*+/)|(//.*)";//Multiline
		/*private static void RemoveCommentsInCsFile(ref string csFileContent)
		{
			StripComments(ref csFileContent);
			MatchCollection commentBlocks = Regex.Matches(csFileContent, cCsFileCommentsRegex, RegexOptions.Multiline);
			for (int i = commentBlocks.Count - 1; i >= 0; i--)
				if (!IsIndexInsideNormalSingleLineString(ref csFileContent, commentBlocks[i].Index, 2)//we use length=2 as it might be // or /*
					&& !IsIndexInsideStringMultilineLiteral(ref csFileContent, commentBlocks[i].Index, 2))
					csFileContent = csFileContent.Remove(commentBlocks[i].Index, commentBlocks[i].Length);
		}*/

		private static string FixRelativePath(string originalStringToBeReplace, string csprojFullpath_JustUsedInErrorMessages, out string errorIfFailed)
		{
			try
			{
				if (originalStringToBeReplace.ToLower().Contains(@"\Francois\Dev\VSprojects\".ToLower()))
				{
					int tmpindex = originalStringToBeReplace.IndexOf(@"\Francois\Dev\VSprojects\", StringComparison.InvariantCultureIgnoreCase);
					tmpindex += @"\Francois\Dev\VSprojects\".Length;//We want to get just after Dev\
					errorIfFailed = null;
					return @"..\..\" + originalStringToBeReplace.Substring(tmpindex);
				}
				else if (originalStringToBeReplace.ToLower().Contains(@"\Francois\Dev\".ToLower()))
				{
					int tmpindex = originalStringToBeReplace.IndexOf(@"\Francois\Dev\", StringComparison.InvariantCultureIgnoreCase);
					tmpindex += @"\Francois\Dev\".Length;//We want to get just after Dev\
					errorIfFailed = null;
					return @"..\..\..\" + originalStringToBeReplace.Substring(tmpindex);
				}
				else if (Regex.IsMatch(originalStringToBeReplace, @"^[a-zA-Z]:\\"))//Is an absolute path
				{
					//Already handled the next commented block by the previous if()
					/*if (originalStringToBeReplace.ToLower().Contains(@"\Dev\DLLs\".ToLower()))
					{
						int tmpindex = originalStringToBeReplace.IndexOf(@"\Dev\DLLs\", StringComparison.InvariantCultureIgnoreCase);
						tmpindex += @"\Dev\".Length;//We want to get where DLLs starts
						errorIfFailed = null;
						return @"..\..\..\" + originalStringToBeReplace.Substring(tmpindex);
					}
					else
					{*/
					errorIfFailed = "Cannot fix absolute path '" + originalStringToBeReplace + "' inside project '"
						+ csprojFullpath_JustUsedInErrorMessages + "'";
					return null;
					/*}*/
				}

				errorIfFailed = null;
				return originalStringToBeReplace;
			}
			catch (Exception exc)
			{
				errorIfFailed = "Exception in FixRelativePath: " + exc.Message;
				return originalStringToBeReplace;
			}
		}

		public static bool FixIncludeFilepathsInCsProjFile(string csprojFullpath, out string errorIfFailed, out List<string> warnings)
		{
			try
			{
				//int tmpfixcount = 0;
				string origFileContent = File.ReadAllText(csprojFullpath);
				List<string> warningsInRegexReplace = new List<string>();
				string newFileContents = RegexInterop.RegexReplaceMatches(
					ref origFileContent,
					@"(?<=Include="")[^""]+(?="")",
					(origString) =>
					{
						string tmperr;
						string fixedRelPath = FixRelativePath(origString, csprojFullpath, out tmperr);

						//if (!origString.Equals(fixedRelPath))
						//    tmpfixcount++;

						if (fixedRelPath != null && tmperr == null)
							return fixedRelPath;
						else
						{
							warningsInRegexReplace.Add(tmperr);
							return origString;
						}
					},
					out errorIfFailed);

				if (warningsInRegexReplace.Count > 0)
					warnings = warningsInRegexReplace;
				else
					warnings = null;

				if (newFileContents == null)
					return false;//errorIfFailed already set

				if (!origFileContent.Equals(newFileContents))
				{
					string backupFilePath = SettingsInterop.GetFullFilePathInLocalAppdata(
						DateTime.Now.ToString("yyyy_MM_dd__HH_mm_ss") + " " + Path.GetFileName(csprojFullpath),
						cAppNameForStoringSettings,
						"BackupsOfChangedCsProjFiles\\" + Path.GetFileNameWithoutExtension(csprojFullpath));
					File.WriteAllText(backupFilePath, origFileContent);
					File.WriteAllText(csprojFullpath, newFileContents);

					string tmpwarnmsg = "Some relative paths were fixed in csproject (via method FixIncludeFilepathsInCsProjFile)"
						+ " '" + csprojFullpath + "' and a backup was made in file"
						+ " '" + backupFilePath + "'";

					if (warnings == null)
						warnings = new List<string>();
					warnings.Add(tmpwarnmsg);
					Logging.LogWarningToFile(tmpwarnmsg, Logging.ReportingFrequencies.Daily, cAppNameForStoringSettings, "Logs");
				}

				return true;
			}
			catch (Exception exc)
			{
				warnings = null;
				errorIfFailed = "Exception in FixIncludeFilepathsInCsProjFile: " + exc.Message;
				return false;
			}
		}

		private static bool WpfEnsureAppXamlDoesNotHaveStartupUri(string csprojFullpath, out string errorIfFailed)
		{
			try
			{
				string nsPrefix = "NS:";
				XmlNamespaceManager nsmgr;
				XmlDocument xmlDoc =
				OpenCsprojAsXmlDocument(csprojFullpath, ref nsPrefix, out nsmgr, out errorIfFailed);
				if (xmlDoc == null)
					return false;//errorIfFailed already set
				XmlNode appdefXmlNode;
				bool? getAppDefXmlNodeSuccess = GetApplicationDefinitionXmlNode(ref xmlDoc, ref nsmgr, nsPrefix,
					csprojFullpath, out appdefXmlNode, out errorIfFailed);
				if (getAppDefXmlNodeSuccess != true)//We do not have ApplicationDefinition node or another error occurred
					return false;

				string appXamlRelativePath = appdefXmlNode.Attributes["Include"].Value;
				string appXamlFullpath = Path.Combine(Path.GetDirectoryName(csprojFullpath), appXamlRelativePath);
				string contentsOfAppXaml = File.ReadAllText(appXamlFullpath);
				if (contentsOfAppXaml.Contains("StartupUri="))
				{
					errorIfFailed = "String 'StartupUri=' was found inside '" + appXamlRelativePath + "' of csproj: " + csprojFullpath;
					return false;
				}
				else
				{
					errorIfFailed = null;
					return true;
				}
			}
			catch (Exception exc)
			{
				errorIfFailed = "Exception in : WpfEnsureAppXamlDoesNotHaveStartupUri" + exc.Message;
				return false;
			}
		}

		public static bool? IsMainWindowOrFormImplementedInAppMainEntryPoint(string csprojFullpath, ApplicationTypes appType, out string errorIfFailed)
		{
			string[] expectedOneOfStringsInCode = null;

			if (appType == ApplicationTypes.WPF)
			{
				if (!WpfEnsureAppXamlDoesNotHaveStartupUri(csprojFullpath, out errorIfFailed))
					return null;//Make sure the App.xaml file does not have StartupUri= inside it (we want it to be implemented in the override OnStartup block)
				expectedOneOfStringsInCode = new string[]
				{
					"SingleInstanceApplicationManager<MainWindow>.CheckIfAlreadyRunningElseCreateNew",//Because this will automatically create and show MainWindow
					"new MainWindow("
				};
			}
			else if (appType == ApplicationTypes.Winforms)
			{
				expectedOneOfStringsInCode = new string[]
				{
					"SingleInstanceApplicationManager<MainForm>.CheckIfAlreadyRunningElseCreateNew",
					"Application.Run(new MainForm(",
					"new MainForm("
				};
			}
			else if (appType != ApplicationTypes.WPF && appType != ApplicationTypes.Winforms)
			{
				errorIfFailed = "Method IsMainWindowOrFormImplementedInAppMainEntryPoint only currently supports WPF and Winforms application types";
				return null;
			}

			string mainEntryPointCodeBlock = GetAppMainEntryPointCodeBlock(csprojFullpath, appType, out errorIfFailed);
			if (mainEntryPointCodeBlock == null) return null;
			if (string.IsNullOrWhiteSpace(mainEntryPointCodeBlock)) return false;
			foreach (var expec in expectedOneOfStringsInCode)
				if (mainEntryPointCodeBlock.StringIndexOfIgnoreInsideStringOrChar(expec) != -1)
					return true;
			//We did not find any of the expected Strings in Main Entrypoint of app
			errorIfFailed = "We could not find expected Strings in Main Entry point of application '" +
				csprojFullpath + "', expected one of the following: " + string.Join("|", expectedOneOfStringsInCode);
			return false;
		}

		public static bool? CheckIfCsprojHasMainWinOrFormAndIfItsImplemented(string csprojFullpath, ApplicationTypes projApplicationType, out string mainWinOrFormCodebehindRelativePathToCsproj, out string errorIfFailed)
		{
			string fileContent = File.ReadAllText(csprojFullpath);

			switch (projApplicationType)
			{
				case OwnAppsInterop.ApplicationTypes.WPF:
					bool hasPageIncludeMainWindowXaml
						= Regex.IsMatch(fileContent, @"<Page[^>]+Include=""MainWindow.xaml""[^>]*>");//<Page Include="MainWindow.xaml">
					if (hasPageIncludeMainWindowXaml == false)
					{
						errorIfFailed = null;
						mainWinOrFormCodebehindRelativePathToCsproj = null;
						return false;
					}
					bool hasCompileIncludeMainWindowXamlCs = 
						Regex.IsMatch(fileContent, @"<Compile[^>]+Include=""MainWindow.xaml.cs""[^>]*>");//<Compile Include="MainWindow.xaml.cs">
					if (hasCompileIncludeMainWindowXamlCs == false)
					{
						errorIfFailed = null;
						mainWinOrFormCodebehindRelativePathToCsproj = null;
						return false;
					}

					string mainWindowXamlFullpath = Path.Combine(Path.GetDirectoryName(csprojFullpath), "MainWindow.xaml");
					string contentOfMainWindowXaml = File.ReadAllText(mainWindowXamlFullpath);
					string regexPatternAppIconImplementedInMainWindow = "(?<=<Window[^>]+)Icon='app.ico'(?=[^>]*>)";
					string regexPatternAppIconImplementedInMainWindow_Alternative = "(?<=<Window[^>]+)Icon=\"app.ico\"(?=[^>]*>)";
					if (!Regex.IsMatch(contentOfMainWindowXaml, regexPatternAppIconImplementedInMainWindow)
						&& !Regex.IsMatch(contentOfMainWindowXaml, regexPatternAppIconImplementedInMainWindow_Alternative))
					{
						errorIfFailed = "app.ico not implemented in MainWindow.xaml.";
						mainWinOrFormCodebehindRelativePathToCsproj = null;
						return null;
					}

					mainWinOrFormCodebehindRelativePathToCsproj = "MainWindow.xaml.cs";
					return IsMainWindowOrFormImplementedInAppMainEntryPoint(csprojFullpath, projApplicationType, out errorIfFailed);
				case OwnAppsInterop.ApplicationTypes.Winforms:
					bool hasCompileIncludeMainFormCs
						= Regex.IsMatch(fileContent, @"<Compile[^>]+Include=""MainForm.cs""[^>]*>");//<Compile Include="MainForm.cs">
					if (hasCompileIncludeMainFormCs == false)
					{
						errorIfFailed = null;
						mainWinOrFormCodebehindRelativePathToCsproj = null;
						return false;
					}
					bool hasCompileIncludeMainFormDesignerCs = 
						Regex.IsMatch(fileContent, @"<Compile[^>]+Include=""MainForm.Designer.cs""[^>]*>");//<Compile Include="MainForm.Designer.cs">
					if (hasCompileIncludeMainFormDesignerCs == false)
					{
						errorIfFailed = null;
						mainWinOrFormCodebehindRelativePathToCsproj = null;
						return false;
					}

					mainWinOrFormCodebehindRelativePathToCsproj = "MainForm.cs";
					return IsMainWindowOrFormImplementedInAppMainEntryPoint(csprojFullpath, projApplicationType, out errorIfFailed);
				default:
					errorIfFailed = "Method 'CheckIfCsprojHasMainWinOrFormAndIfItsImplemented' does not currently support application of type: " + projApplicationType;
					mainWinOrFormCodebehindRelativePathToCsproj = null;
					return null;
				/*case OwnAppsInterop.ApplicationTypes.Console:
				case OwnAppsInterop.ApplicationTypes.DLL:
				default:*/
			}

			//errorIfFailed = "Unknown error in CheckIfCsprojHasMainWinOrFormAndIfItsImplemented";
			//return null;
		}

		public static bool GetAllIncludedSourceFilesInCsproj(string csprojFullpath, out Dictionary<string, string> includeRelativePathsAndXmlTagnames, out string errorIfFailed)
		{
			try
			{
				string fileContent = File.ReadAllText(csprojFullpath);
				MatchCollection includeTagMatches = Regex.Matches(fileContent, @"<[^>]+Include=""[^""]+""[^>]*>");
				includeRelativePathsAndXmlTagnames = new Dictionary<string, string>();
				foreach (Match match in includeTagMatches)
				{
					string xmltagstring = fileContent.Substring(match.Index, match.Length);
					int firstSpacePos = xmltagstring.IndexOf(' ');
					string tagname = xmltagstring.Substring(1, firstSpacePos - 1);//We also skip first character '<'
					int posOfPathStart = xmltagstring.IndexOf("Include=\"") + "Include=\"".Length;
					int posOfPathEnd = xmltagstring.IndexOf("\"", posOfPathStart) - 1;
					string relpathIncluded = xmltagstring.Substring(posOfPathStart, posOfPathEnd - posOfPathStart + 1);
					includeRelativePathsAndXmlTagnames.Add(relpathIncluded, tagname);
				}
				errorIfFailed = null;
				return true;
			}
			catch (Exception exc)
			{
				includeRelativePathsAndXmlTagnames = null;
				errorIfFailed = "Exception in GetAllIncludedSourceFilesInCsproj: " + exc.Message;
				return false;
			}
		}

		private static string rootVSprojectsDir;
		public static string RootVSprojectsDir
		{
			get
			{
				if (rootVSprojectsDir == null)//Checks was not done yet
				{
					rootVSprojectsDir = @"C:\Francois\Dev\VSprojects";
					if (!Directory.Exists(rootVSprojectsDir))
					{
						UserMessages.ShowErrorMessage("Please ensure projects are in directory (dir not found): " + rootVSprojectsDir);
						rootVSprojectsDir = null;
					}
				}
				return rootVSprojectsDir;
			}
		}

		private static bool? _checkTracUrlExists(string tracUrl, out string errorIfFailed)
		{
			try
			{
				string tracWebpageContent = new WebClient().DownloadString(tracUrl);
				Console.WriteLine("Trac url: " + tracUrl);
				if (tracWebpageContent.IndexOf("Environment not found", StringComparison.InvariantCultureIgnoreCase) != -1)
				{
					errorIfFailed = null;
					return false;//The environment does not exist
				}
				else
				{
					errorIfFailed = null;
					return true;
				}
			}
			catch (WebException webexc)
			{
				HttpWebResponse webResponse = webexc.Response as HttpWebResponse;
				if (webResponse != null)
				{
					if (webResponse.StatusCode == HttpStatusCode.NotFound)
					{
						errorIfFailed = null;
						return false;
					}
					else
					{
						errorIfFailed = string.Format("Unknown Http status code ({0}: {1}), message: {2}",
							(int)webResponse.StatusCode, webResponse.StatusDescription, webexc.Message);
						return null;
					}
				}
				else if (webexc.Status == WebExceptionStatus.NameResolutionFailure)
				{
					errorIfFailed = "Please check the internet connectivity, could not determine whether Trac url exists.";
					return null;
				}
				errorIfFailed = "Error determining whether Trac url exists: " + webexc.Message;
				return null;
			}
			catch (Exception exc)
			{
				errorIfFailed = "Error determining whether Trac url exists: " + exc.Message;
				return null;
			}
		}

		public static bool? DoesTracEnvironmentExistForProject(string applicationName, out string errorIfFailed, Action<bool?, string> callbackIfSeparateThreadTracUrlNotExistAnymore)
		{
			if (callbackIfSeparateThreadTracUrlNotExistAnymore == null) callbackIfSeparateThreadTracUrlNotExistAnymore = delegate { };

			string tracRootUrl = TracXmlRpcInterop.ChangeLogs.GetTracBaseUrlForApplication(applicationName);

			string filenameIfCached = SettingsInterop.GetFullFilePathInLocalAppdata(
				applicationName, cAppNameForStoringSettings, "CachedTracEnvironments");

			if (File.Exists(filenameIfCached))//The trac environment exists, at least according to a previous check
			{
				//Check if it actually still exists online, on a separate thread
				ThreadingInterop.PerformOneArgFunctionSeperateThread<KeyValuePair<string, string>>(
					(tracUrlOfApp_AndCacheFilename) =>
					{
						string tmperr;
						bool? tracUrlExistsResult = _checkTracUrlExists(tracUrlOfApp_AndCacheFilename.Key, out tmperr);
						if (tracUrlExistsResult.HasValue 
							&& tracUrlExistsResult.Value == false)//Does not exist
						{
							try
							{
								File.Delete(tracUrlOfApp_AndCacheFilename.Value);
								callbackIfSeparateThreadTracUrlNotExistAnymore(false, tracUrlOfApp_AndCacheFilename.Key);
							}
							catch (Exception exc)
							{
								callbackIfSeparateThreadTracUrlNotExistAnymore(null, "Cannot delete cached file (for trac url check): " + exc.Message);
								return;
							}
						}
						callbackIfSeparateThreadTracUrlNotExistAnymore(tracUrlExistsResult, tmperr);
					},
					new KeyValuePair<string, string>(tracRootUrl, filenameIfCached),
					false);

				errorIfFailed = null;
				return true;
			}

			//int todoItem;
			//Not tested with no internent yet

			bool? doesTracUrlExistResult = _checkTracUrlExists(tracRootUrl, out errorIfFailed);
			if (doesTracUrlExistResult.HasValue
				&& doesTracUrlExistResult.Value == true)
			{
				if (!File.Exists(filenameIfCached))
					File.Create(filenameIfCached).Close();//This file means that the trac url does exist
			}
			return doesTracUrlExistResult;
		}

		public static string GetApplicationName()
		{
			string applicationName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
			if (applicationName.EndsWith(".vshost", StringComparison.InvariantCultureIgnoreCase))
				applicationName = applicationName.Substring(0, applicationName.Length - ".vshost".Length);
			return applicationName;
		}
	}
}