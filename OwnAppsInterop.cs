using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using System.IO;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SharedClasses
{
	public static class OwnAppsInterop
	{
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
							//TODO: For now we use the DisplayIcon, is this the best way, what if DisplayIcon is different from EXE
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

		private static string GetAppMainEntryPointCodeBlock(string csprojFullPath, ApplicationTypes appType, out string errorIfFailed)
		{
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
						if (gotAppDefXmlNode == null) return null;
						string appXamlFileRelativePath = tmpAppDefxmlnode.Attributes["Include"].Value;
						string appXamlStartupBlock = GetOverrideOnStartupBlockInAppXamlOfWpfApplication_RemoveComments(Path.Combine(Path.GetDirectoryName(csprojFullPath), appXamlFileRelativePath), out errorIfFailed);
						return appXamlStartupBlock;
					case ApplicationTypes.Winforms://Winforms and Console almost the same, Console only has a parameter to the void main method
					case ApplicationTypes.Console:
						XmlNode programNode;
						bool? gotCompileNodes = GetCompileProgramCsNode(ref xmlDoc, ref nsmgr, nsmgrPrefix, csprojFullPath, out programNode, out errorIfFailed);
						if (gotCompileNodes == null) return null;
						string programCsFileRelativePath = programNode.Attributes["Include"].Value;
						string expectedMethodStartString =
							appType == ApplicationTypes.Winforms
							? "static void Main("
							: "static void Main(string[]";
						string programCsVoidMainBlock = ExtractMethodBlockFromSourcecodeFile(Path.Combine(Path.GetDirectoryName(csprojFullPath), programCsFileRelativePath), expectedMethodStartString, out errorIfFailed);
						return programCsVoidMainBlock;
					case ApplicationTypes.DLL:
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
					return StringIndexOfIgnoreInsideStringOrChar(ref mainEntryPointCodeBlock, expectedStringInCode) != -1;
				case ApplicationTypes.Winforms:
				case ApplicationTypes.Console:
					if (mainEntryPointCodeBlock == null) return null;
					if (string.IsNullOrWhiteSpace(mainEntryPointCodeBlock)) return false;
					return StringIndexOfIgnoreInsideStringOrChar(ref mainEntryPointCodeBlock, expectedStringInCode) != -1;
				case ApplicationTypes.DLL:
					return false;
				default:
					return null;//The errorIfFailed will be populated with an error message inside above called method GetAppMainEntryPointCodeBlock
			}
		}

		public static bool? IsAutoUpdatingImplemented(string csprojFullPath, ApplicationTypes appType, out string errorIfFailed)
		{
			string expectedStringInCode = "AutoUpdating.CheckForUpdates_ExceptionHandler(";
			string mainEntryPointCodeBlock = GetAppMainEntryPointCodeBlock(csprojFullPath, appType, out errorIfFailed);
			switch (appType)
			{
				case ApplicationTypes.WPF:
					if (mainEntryPointCodeBlock == null) return null;
					if (string.IsNullOrWhiteSpace(mainEntryPointCodeBlock)) return false;
					//Comments are already removed not check that we have
					return StringIndexOfIgnoreInsideStringOrChar(ref mainEntryPointCodeBlock, expectedStringInCode) != -1;
				case ApplicationTypes.Winforms:
				case ApplicationTypes.Console:
					if (mainEntryPointCodeBlock == null) return null;
					if (string.IsNullOrWhiteSpace(mainEntryPointCodeBlock)) return false;
					return StringIndexOfIgnoreInsideStringOrChar(ref mainEntryPointCodeBlock, expectedStringInCode) != -1;
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
					return StringIndexOfIgnoreInsideStringOrChar(ref mainEntryPointCodeBlock, expectedStringInCode) != -1;
				case ApplicationTypes.Winforms:
				case ApplicationTypes.Console:
					if (mainEntryPointCodeBlock == null) return null;
					if (string.IsNullOrWhiteSpace(mainEntryPointCodeBlock)) return false;
					return StringIndexOfIgnoreInsideStringOrChar(ref mainEntryPointCodeBlock, expectedStringInCode) != -1;
				case ApplicationTypes.DLL:
					return false;
				default:
					return null;//The errorIfFailed will be populated with an error message inside above called method GetAppMainEntryPointCodeBlock
			}
		}

		private static bool IsIndexInsideStringMultilineLiteral(ref string haystack, int needleIndex, int needleLength, int startIndex = 0)
		{
			int indexOfLiteralStart = haystack.IndexOf("@\"", startIndex);
			if (indexOfLiteralStart == -1)
				return false;
			int indexOfLiteralEnd = haystack.IndexOf("\"", indexOfLiteralStart + 1);
			if (indexOfLiteralEnd == -1) indexOfLiteralEnd = haystack.Length - 1;
			if (indexOfLiteralEnd > 0 && haystack[indexOfLiteralEnd - 1] == '"')//If we escaped the " by using two quotes like ""
				return IsIndexInsideNormalSingleLineString(ref haystack, needleIndex, needleLength, indexOfLiteralEnd + 1);
			if (indexOfLiteralEnd < needleIndex)
				return IsIndexInsideStringMultilineLiteral(ref haystack, needleIndex, needleLength, indexOfLiteralEnd + 1);
			return indexOfLiteralStart < needleIndex && indexOfLiteralEnd > needleIndex + needleLength;
		}

		private static bool IsIndexInsideNormalSingleLineString(ref string haystack, int needleIndex, int needleLength, int startIndex = 0)
		{
			int indexOfNormalStringStart = haystack.IndexOf("\"", startIndex);
			if (indexOfNormalStringStart == -1)
				return false;
			int indexOfStringEnd = haystack.IndexOf("\"", indexOfNormalStringStart + 1);
			if (indexOfStringEnd == -1) indexOfStringEnd = haystack.Length - 1;
			if (indexOfStringEnd > 0 && haystack[indexOfStringEnd - 1] == '\\')//If we escaped the " by using \"
				return IsIndexInsideNormalSingleLineString(ref haystack, needleIndex, needleLength, indexOfStringEnd + 1);
			if (indexOfStringEnd < needleIndex)
				return IsIndexInsideStringMultilineLiteral(ref haystack, needleIndex, needleLength, indexOfStringEnd + 1);
			return indexOfNormalStringStart < needleIndex && indexOfStringEnd > needleIndex + needleLength;
		}

		private static int StringIndexOfIgnoreInsideStringOrChar(ref string haystack, string needle, int startIndex = 0)
		{//Does not currently ignore char (altough method name states it)
			int currentIndexOf = haystack.IndexOf(needle, startIndex);

			while (IsIndexInsideStringMultilineLiteral(ref haystack, currentIndexOf, needle.Length, startIndex)
				|| IsIndexInsideNormalSingleLineString(ref haystack, currentIndexOf, needle.Length, startIndex))
			{
				startIndex += currentIndexOf + needle.Length;//Skip this char as it part of a string @"..." or "..."
				currentIndexOf = haystack.IndexOf(needle, startIndex);
			}
			return currentIndexOf;
		}

		private static int StringIndexOfIgnoreInsideStringOrChar(ref string haystack, char needle, int startIndex = 0)
		{
			return StringIndexOfIgnoreInsideStringOrChar(ref haystack, needle.ToString(), startIndex);
		}

		private static string ExtractMethodBlockFromSourcecodeFile(string sourceFilepath, string expectedMethodStartString, out string errorIfFailed)
		{
			try
			{
				string csContent = File.ReadAllText(sourceFilepath);
				RemoveCommentsInCsFile(ref csContent);

				int startOfOverrideOnStartup = StringIndexOfIgnoreInsideStringOrChar(ref csContent, expectedMethodStartString);
				if (startOfOverrideOnStartup == -1)//We did not find the override OnStartup method
				{
					errorIfFailed = null;
					return "";
				}
				else
				{
					int indexFirstOpenCurly = StringIndexOfIgnoreInsideStringOrChar(ref csContent, '{', startOfOverrideOnStartup + expectedMethodStartString.Length);

					int tmpindexNextOpenCurly = StringIndexOfIgnoreInsideStringOrChar(ref csContent, '{', indexFirstOpenCurly + 1);
					int tmpindexNextCloseCurly = StringIndexOfIgnoreInsideStringOrChar(ref csContent, '}', (tmpindexNextOpenCurly == -1 ? 0 : indexFirstOpenCurly + 1));
					if (tmpindexNextOpenCurly == -1)
						tmpindexNextOpenCurly = csContent.Length - 1;
					else
					{

						int openCount = 0;
						if (tmpindexNextCloseCurly < tmpindexNextOpenCurly)//We do not have sub sections (for loops, etc) inside the override OnStartup
						{
							errorIfFailed = null;
							return csContent.Substring(indexFirstOpenCurly, tmpindexNextCloseCurly - indexFirstOpenCurly);
						}
						else
							openCount = 1;

						while (openCount > 0)
						{
							if (tmpindexNextCloseCurly > tmpindexNextOpenCurly)
							{
								openCount++;
								tmpindexNextCloseCurly = StringIndexOfIgnoreInsideStringOrChar(ref csContent, '}', tmpindexNextOpenCurly + 1);//This must happen before next line (as we set tmpindexNextOpenCurly again)
								tmpindexNextOpenCurly = StringIndexOfIgnoreInsideStringOrChar(ref csContent, '{', tmpindexNextOpenCurly + 1);
							}
							else if (tmpindexNextCloseCurly < tmpindexNextOpenCurly)
							{
								openCount--;
								if (openCount == 0)
									break;
								tmpindexNextOpenCurly = StringIndexOfIgnoreInsideStringOrChar(ref csContent, '{', tmpindexNextCloseCurly + 1);
								tmpindexNextCloseCurly = StringIndexOfIgnoreInsideStringOrChar(ref csContent, '}', tmpindexNextCloseCurly + 1);
							}
							else
								break;

							if (tmpindexNextOpenCurly == -1)
								tmpindexNextOpenCurly = csContent.Length - 1;
							if (tmpindexNextCloseCurly == -1)
								tmpindexNextCloseCurly = csContent.Length - 1;
						}
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

		private static string GetOverrideOnStartupBlockInAppXamlOfWpfApplication_RemoveComments(string appXamlFullPath, out string errorIfFailed)
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

			return ExtractMethodBlockFromSourcecodeFile(xamlCsPath, "protected override void OnStartup", out errorIfFailed);

			//First parse the .xaml.cs file and remove comments
			//Just use code inside override OnStartup, if it sits inside the Eventhandler for Startup, we dont allow this,
			//We force user to place it inside override OnStartup

			/*//If we have override OnStartup() then we must check whether it contains base.OnStartup
			//If we have a Startup event handler, get code inside this eventhandler
			//
			//Also (can have an event handler and override OnStartup() which contains

			//string xamlCSfileContent = File.ReadAllText(csPath);*/
		}

		private const string cCsFileCommentsRegex = @"(/\*([^*]|[\r\n]|(\*+([^*/]|[\r\n])))*\*+/)|(//.*)";//Multiline
		private static void RemoveCommentsInCsFile(ref string csFileContent)
		{
			MatchCollection commentBlocks = Regex.Matches(csFileContent, cCsFileCommentsRegex, RegexOptions.Multiline);
			for (int i = commentBlocks.Count - 1; i >= 0; i--)
				csFileContent = csFileContent.Remove(commentBlocks[i].Index, commentBlocks[i].Length);
		}

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

				string cThisAppName = "AnalyseProjects";
				if (!origFileContent.Equals(newFileContents))
				{
					string backupFilePath = SettingsInterop.GetFullFilePathInLocalAppdata(
						DateTime.Now.ToString("yyyy_MM_dd__HH_MM_ss") + " " + Path.GetFileName(csprojFullpath),
						cThisAppName,
						"BackupsOfChangedCsProjFiles\\" + Path.GetFileNameWithoutExtension(csprojFullpath));
					File.WriteAllText(backupFilePath, origFileContent);

					File.WriteAllText(@"C:\Francois\Other\tmp\tmpRel\" + Path.GetFileName(csprojFullpath), newFileContents);
					//File.WriteAllText(csprojFullpath, newFileContents);

					string tmpwarnmsg = "Some relative paths were fixed in csproject (via method FixIncludeFilepathsInCsProjFile)"
						+ " '" + csprojFullpath + "' and a backup was made in file"
						+ " '" + backupFilePath + "'";

					if (warnings == null)
						warnings = new List<string>();
					warnings.Add(tmpwarnmsg);
					Logging.LogWarningToFile(tmpwarnmsg, Logging.ReportingFrequencies.Daily, cThisAppName, "Logs");
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
	}
}