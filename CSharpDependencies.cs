using System;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Build.Evaluation;
using AddDependenciesCSharp;
using System.Collections.Generic;
//using Microsoft.Build.BuildEngine;

namespace SharedClasses
{
	public class CSharpDependencies
	{
		private static string[] IgnoredFiles = new string[]
		{
			"WPFcanvasArrows.xaml",
		};

		public static FullPathAndDisplayName[] GetSharedClasses()
		{
			//Function to determine whether a file must be included
			Func<string, bool> shouldFileBeIncludedCheck = new Func<string,bool>(
				filepath => IgnoredFiles.Count(ignfilename => ignfilename.Equals(Path.GetFileName(filepath), StringComparison.InvariantCultureIgnoreCase)) == 0);

			return Directory.GetFiles(@"C:\Francois\Dev\VSprojects\SharedClasses")
				.Where(shouldFileBeIncludedCheck)
				.Where(f =>
					(f.EndsWith(".cs", StringComparison.InvariantCultureIgnoreCase) || f.EndsWith(".xaml", StringComparison.InvariantCultureIgnoreCase))
					&& (!f.EndsWith(".xaml.cs", StringComparison.InvariantCultureIgnoreCase))
					&& (!f.EndsWith(".Designer.cs", StringComparison.InvariantCultureIgnoreCase)))
				.Select(f => new FullPathAndDisplayName(f)).ToArray();
		}

		//private static bool? IsWpfProject_FalseWinforms_NullConsole(ref Project csProject)
		public static bool IsWpfProject_FalseWinforms(ref Project csProject)
		{
			if (csProject == null)
				return false;
			return csProject.GetItems("ApplicationDefinition").ToArray().Length > 0;
		}

		public static string GetRelativePath(string startPath, string destinationFullPath)
		{
			Uri destUri = new Uri(destinationFullPath);
			Uri startUri = new Uri(startPath);
			return startUri.MakeRelativeUri(destUri).ToString().Replace('/', '\\');
		}

		public static string GetAbsolutePath(string basePath, string destinationRelativePath)
		{
			Uri baseUri = new Uri(Uri.EscapeUriString(basePath.Replace('\\', '/')));
			Uri relativeUri = new Uri(baseUri, Uri.EscapeUriString(destinationRelativePath));
			return relativeUri.AbsoluteUri.ToString().Replace('/', '\\').Replace(@"file:\\\", "");
		}

		public static void EnsureCsProjectHasCompileXamlCsFile(ref Project csProject, string includeXamlCsFileFullPath)
		{
			if (csProject == null)
				return;

			string relativePath = GetRelativePath(csProject.FullPath, includeXamlCsFileFullPath);
			var allcompileItems = csProject.GetItems("Compile").ToArray();
			var compileItems = allcompileItems.Where(ci => ci.UnevaluatedInclude.Equals(relativePath, StringComparison.InvariantCultureIgnoreCase)).ToArray();
			if (compileItems.Length > 1)
				UserMessages.ShowWarningMessage("Project file has more than one of the same Compile items: " + relativePath);
			else if (compileItems.Length == 0)
			{
				csProject.AddItem("Compile", relativePath);
				csProject.Save();
				allcompileItems = csProject.GetItems("Compile").ToArray();
				compileItems = allcompileItems.Where(ci => ci.UnevaluatedInclude.Equals(relativePath, StringComparison.InvariantCultureIgnoreCase)).ToArray();
			}

			string xamlcsname = Path.GetFileName(includeXamlCsFileFullPath);
			string xamlname = Path.GetFileNameWithoutExtension(includeXamlCsFileFullPath);
			var item = compileItems[0];
			if (!item.HasMetadata("Link") || !item.GetMetadata("Link").UnevaluatedValue.Equals(xamlcsname, StringComparison.InvariantCultureIgnoreCase))
			{
				item.SetMetadataValue("Link", xamlcsname);
				csProject.Save();
			}
			if (!item.HasMetadata("DependentUpon") || !item.GetMetadata("DependentUpon").UnevaluatedValue.Equals(xamlname, StringComparison.InvariantCultureIgnoreCase))
			{
				item.SetMetadataValue("DependentUpon", xamlname);
				csProject.Save();
			}
		}

		public static void EnsureCsProjectHasCompileWinformCsFile(ref Project csProject, string includewinformCsFileFullPath)
		{
			if (csProject == null)
				return;

			string relativePath = GetRelativePath(csProject.FullPath, includewinformCsFileFullPath);
			var allcompileItems = csProject.GetItems("Compile").ToArray();
			var compileItems = allcompileItems.Where(ci => ci.UnevaluatedInclude.Equals(relativePath, StringComparison.InvariantCultureIgnoreCase)).ToArray();
			if (compileItems.Length > 1)
				UserMessages.ShowWarningMessage("Project file has more than one of the same Compile items: " + relativePath);
			else if (compileItems.Length == 0)
			{
				csProject.AddItem("Compile", relativePath);
				csProject.Save();
				allcompileItems = csProject.GetItems("Compile").ToArray();
				compileItems = allcompileItems.Where(ci => ci.UnevaluatedInclude.Equals(relativePath, StringComparison.InvariantCultureIgnoreCase)).ToArray();
			}

			string csname = Path.GetFileName(includewinformCsFileFullPath);
			var item = compileItems[0];
			if (!item.HasMetadata("Link") || !item.GetMetadata("Link").UnevaluatedValue.Equals(csname, StringComparison.InvariantCultureIgnoreCase))
			{
				item.SetMetadataValue("Link", csname);
				csProject.Save();
			}
			if (!item.HasMetadata("SubType") || !item.GetMetadata("SubType").UnevaluatedValue.Equals("Form", StringComparison.InvariantCultureIgnoreCase))
			{
				item.SetMetadataValue("SubType", "Form");
				csProject.Save();
			}
		}

		public static void EnsureCsProjectHasCompileWinformDesignerCsFile(ref Project csProject, string includewinformDesignerCsFileFullPath)
		{
			if (csProject == null)
				return;

			string relativePath = GetRelativePath(csProject.FullPath, includewinformDesignerCsFileFullPath);
			var allcompileItems = csProject.GetItems("Compile").ToArray();
			var compileItems = allcompileItems.Where(ci => ci.UnevaluatedInclude.Equals(relativePath, StringComparison.InvariantCultureIgnoreCase)).ToArray();
			if (compileItems.Length > 1)
				UserMessages.ShowWarningMessage("Project file has more than one of the same Compile items: " + relativePath);
			else if (compileItems.Length == 0)
			{
				csProject.AddItem("Compile", relativePath);
				csProject.Save();
				allcompileItems = csProject.GetItems("Compile").ToArray();
				compileItems = allcompileItems.Where(ci => ci.UnevaluatedInclude.Equals(relativePath, StringComparison.InvariantCultureIgnoreCase)).ToArray();
			}

			string designercsname = Path.GetFileName(includewinformDesignerCsFileFullPath);
			string formcsname = Path.ChangeExtension(Path.GetFileNameWithoutExtension(includewinformDesignerCsFileFullPath), ".cs");
			var item = compileItems[0];
			if (!item.HasMetadata("Link") || !item.GetMetadata("Link").UnevaluatedValue.Equals(designercsname, StringComparison.InvariantCultureIgnoreCase))
			{
				item.SetMetadataValue("Link", designercsname);
				csProject.Save();
			}
			if (!item.HasMetadata("DependentUpon") || !item.GetMetadata("DependentUpon").UnevaluatedValue.Equals(formcsname, StringComparison.InvariantCultureIgnoreCase))
			{
				item.SetMetadataValue("DependentUpon", formcsname);
				csProject.Save();
			}
		}

		public static void EnsureCsProjectHasCompileIndependantCsFile(ref Project csProject, string includeCsFileFullPath)
		{
			if (csProject == null)
				return;

			string relativePath = GetRelativePath(csProject.FullPath, includeCsFileFullPath);
			var allcompileItems = csProject.GetItems("Compile").ToArray();
			var compileItems = allcompileItems.Where(ci => ci.UnevaluatedInclude.Equals(relativePath, StringComparison.InvariantCultureIgnoreCase)).ToArray();
			if (compileItems.Length > 1)
				UserMessages.ShowWarningMessage("Project file has more than one of the same Compile items: " + relativePath);
			else if (compileItems.Length == 0)
			{
				csProject.AddItem("Compile", relativePath);
				csProject.Save();
				allcompileItems = csProject.GetItems("Compile").ToArray();
				compileItems = allcompileItems.Where(ci => ci.UnevaluatedInclude.Equals(relativePath, StringComparison.InvariantCultureIgnoreCase)).ToArray();
			}

			string csname = Path.GetFileName(includeCsFileFullPath);
			var item = compileItems[0];
			if (!item.HasMetadata("Link") || !item.GetMetadata("Link").UnevaluatedValue.Equals(csname, StringComparison.InvariantCultureIgnoreCase))
			{
				item.SetMetadataValue("Link", csname);
				csProject.Save();
			}
		}

		public static void EnsureCsProjectHasPageXamlFile(ref Project csProject, string includeXamlFileFullPath)
		{
			if (csProject == null)
				return;

			string relativePath = GetRelativePath(csProject.FullPath, includeXamlFileFullPath);
			var allpageItems = csProject.GetItems("Page").ToArray();
			var pageItems = allpageItems.Where(ci => ci.UnevaluatedInclude.Equals(relativePath, StringComparison.InvariantCultureIgnoreCase)).ToArray();
			if (pageItems.Length > 1)
				UserMessages.ShowWarningMessage("Project file has more than one of the same Page items: " + relativePath);
			else if (pageItems.Length == 0)
			{
				csProject.AddItem("Page", relativePath);
				csProject.Save();
				allpageItems = csProject.GetItems("Page").ToArray();
				pageItems = allpageItems.Where(ci => ci.UnevaluatedInclude.Equals(relativePath, StringComparison.InvariantCultureIgnoreCase)).ToArray();
			}

			string xamlname = Path.GetFileName(includeXamlFileFullPath);
			var item = pageItems[0];
			if (!item.HasMetadata("Link") || !item.GetMetadata("Link").UnevaluatedValue.Equals(xamlname, StringComparison.InvariantCultureIgnoreCase))
			{
				item.SetMetadataValue("Link", xamlname);
				csProject.Save();
			}
			if (!item.HasMetadata("Generator") || !item.GetMetadata("Generator").UnevaluatedValue.Equals("MSBuild:Compile", StringComparison.InvariantCultureIgnoreCase))
			{
				item.SetMetadataValue("Generator", "MSBuild:Compile");
				csProject.Save();
			}
		}

		public static void EnsureCsProjectHasEmbeddedResourceWinformResxFile(ref Project csProject, string includeResxFileFullPath)
		{
			if (csProject == null)
				return;

			string relativePath = GetRelativePath(csProject.FullPath, includeResxFileFullPath);
			var allembeddedresourceItems = csProject.GetItems("EmbeddedResource").ToArray();
			var embeddedresourceItems = allembeddedresourceItems.Where(ci => ci.UnevaluatedInclude.Equals(relativePath, StringComparison.InvariantCultureIgnoreCase)).ToArray();
			if (embeddedresourceItems.Length > 1)
				UserMessages.ShowWarningMessage("Project file has more than one of the same EmbeddedResource items: " + relativePath);
			else if (embeddedresourceItems.Length == 0)
			{
				csProject.AddItem("EmbeddedResource", relativePath);
				csProject.Save();
				allembeddedresourceItems = csProject.GetItems("EmbeddedResource").ToArray();
				embeddedresourceItems = allembeddedresourceItems.Where(ci => ci.UnevaluatedInclude.Equals(relativePath, StringComparison.InvariantCultureIgnoreCase)).ToArray();
			}

			string resxname = Path.GetFileName(includeResxFileFullPath);
			string formcsname = Path.GetFileName(Path.ChangeExtension(includeResxFileFullPath, ".cs"));
			var item = embeddedresourceItems[0];
			if (!item.HasMetadata("Link") || !item.GetMetadata("Link").UnevaluatedValue.Equals(resxname, StringComparison.InvariantCultureIgnoreCase))
			{
				item.SetMetadataValue("Link", resxname);
				csProject.Save();
			}
			if (!item.HasMetadata("DependentUpon") || !item.GetMetadata("DependentUpon").UnevaluatedValue.Equals(formcsname, StringComparison.InvariantCultureIgnoreCase))
			{
				item.SetMetadataValue("DependentUpon", formcsname);
				csProject.Save();
			}
		}

		public static void EnsureCsProjectHasStandardReference(ref Project csProject, string referenceName)
		{
			if (csProject == null)
				return;

			var allreferences = csProject.GetItems("Reference").ToArray();

			if (allreferences.Where(ci => ci.UnevaluatedInclude.Equals(referenceName, StringComparison.InvariantCultureIgnoreCase)).ToArray().Length == 0)
			{
				csProject.AddItem("Reference", referenceName);
				csProject.Save();
			}
		}

		public static void EnsureCsProjectHasFrancoisOtherDllsReference(ref Project csProject, string referenceName)
		{
			if (csProject == null)
				return;

			List<string> matchingAssemblyPaths = new List<string>();
			foreach (string dir in Directory.GetDirectories(@"c:\francois\other\dlls"))
				foreach (string file in Directory.GetFiles(dir, "*.dll"))
					if (Path.GetFileNameWithoutExtension(file).Equals(referenceName, StringComparison.InvariantCultureIgnoreCase))
						matchingAssemblyPaths.Add(file);

			if (matchingAssemblyPaths.Count == 0)
			{
				UserMessages.ShowWarningMessage("Cannot find reference with name '" + referenceName + "'");
				return;
			}
			if (matchingAssemblyPaths.Count > 1)
				UserMessages.ShowWarningMessage("Multiple matches found for reference with name '" + referenceName + "', first one will be used of the following:"
					+ Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, matchingAssemblyPaths));

			var allreferences = csProject.GetItems("Reference").ToArray();
			var foundReferenceItem = allreferences.Where(ci => ci.UnevaluatedInclude.Equals(referenceName, StringComparison.InvariantCultureIgnoreCase)).ToArray();

			if (foundReferenceItem.Length == 0)
			{
				csProject.AddItem("Reference", referenceName);
				csProject.Save();
				allreferences = csProject.GetItems("Reference").ToArray();
				foundReferenceItem = allreferences.Where(ci => ci.UnevaluatedInclude.Equals(referenceName, StringComparison.InvariantCultureIgnoreCase)).ToArray();
			}

			string assemblyFullpath = matchingAssemblyPaths[0];
			string relativeAssemblyPath = GetRelativePath(csProject.FullPath, assemblyFullpath);
			if (!foundReferenceItem[0].HasMetadata("HintPath") || !foundReferenceItem[0].GetMetadata("HintPath").UnevaluatedValue.Equals(relativeAssemblyPath, StringComparison.InvariantCultureIgnoreCase))
			{
				foundReferenceItem[0].SetMetadataValue("HintPath", relativeAssemblyPath);
				csProject.Save();
			}
		}

		public static void EnsureCsProjectHasMinimumWPFreferences(ref Project csProject)
		{
			if (csProject == null)
				return;

			var minimumWpfReferenceNames = new string[]
			{ 
				"PresentationCore",
				"PresentationFramework",
				"System.Xaml",
				"WindowsBase"
			};

			foreach (string name in minimumWpfReferenceNames)
				EnsureCsProjectHasStandardReference(ref csProject, name);
		}

		public static void EnsureCsProjectHasMinimumWinformsReferences(ref Project csProject)
		{
			if (csProject == null)
				return;

			var minimumWinformsReferenceNames = new string[]
			{ 
				"System.Drawing",
				"System.Windows.Forms",
			};

			foreach (string name in minimumWinformsReferenceNames)
				EnsureCsProjectHasStandardReference(ref csProject, name);
		}

		public static void EnsureCsProjectIsFullFramework(ref Project csProject)
		{
			if (csProject == null)
				return;

			csProject.SetProperty("TargetFrameworkProfile", "");
			csProject.Save();
		}

		public static void EnsureCsProjectHasAdditionalDependenciesAddedToCsProject(ref Project csProject, string csFile, bool addNestedDependencies)
		{
			int blockCommentNestedCount = 0;
			bool additionalLineFound = false;

			string[] filelines = File.ReadAllLines(csFile);

			bool isWpfProject_FalseWinforms = IsWpfProject_FalseWinforms(ref csProject);
			foreach (string preline in filelines)
			{
				string line = preline;

				if (line.Contains("/*"))
					blockCommentNestedCount++;
				else if (line.Contains("*/"))
					blockCommentNestedCount--;

				if (blockCommentNestedCount > 0 && line.IndexOf("Additional dependencies", StringComparison.InvariantCultureIgnoreCase) != -1)
					additionalLineFound = true;
				else if (additionalLineFound)//So that the same line is not used
				{
					while (line.EndsWith("*/"))
						line = line.Substring(0, line.Length - 2);

					//Why was the following required, means we do not use 'Winforms:' lines if it's a WPF app, and vica versa??
					/*if (isWpfProject_FalseWinforms && line.Trim().StartsWith("Winforms:", StringComparison.InvariantCultureIgnoreCase))
						continue;
					if (!isWpfProject_FalseWinforms && line.Trim().StartsWith("WPF:", StringComparison.InvariantCultureIgnoreCase))
						continue;*/

					if (line.Trim().StartsWith("Winforms:", StringComparison.InvariantCultureIgnoreCase))
						line = line.Replace("Winforms:", "");
					if (line.Trim().StartsWith("WPF:", StringComparison.InvariantCultureIgnoreCase))
						line = line.Replace("WPF:", "");

					if (line.Trim().Equals("Minimum winforms", StringComparison.InvariantCultureIgnoreCase))
					{
						EnsureCsProjectHasMinimumWinformsReferences(ref csProject);
					}
					else if (line.Trim().Equals("Full framework", StringComparison.InvariantCultureIgnoreCase))
					{
						EnsureCsProjectIsFullFramework(ref csProject);
					}
					else if (line.Trim().StartsWith("Class:", StringComparison.InvariantCultureIgnoreCase))
					{
						string relativePath = line.Trim().Replace("Class:", "").Trim() + ".cs";
						string absolutePath = GetAbsolutePath(csFile, relativePath);
						if (addNestedDependencies)
							CSharpDependencies.EnsureCsProjectHasAdditionalDependenciesAddedToCsProject(ref csProject, absolutePath, addNestedDependencies);
						EnsureCsProjectHasCompileIndependantCsFile(ref csProject, absolutePath);
					}
					else if (line.Trim().StartsWith("Form:", StringComparison.InvariantCultureIgnoreCase))
					{
						string relativePath = line.Trim().Replace("Form:", "").Trim();
						if (!relativePath.EndsWith(".cs", StringComparison.InvariantCultureIgnoreCase))
							relativePath += ".cs";
						string absolutePath = GetAbsolutePath(csFile, relativePath);
						if (addNestedDependencies)
							CSharpDependencies.EnsureCsProjectHasAdditionalDependenciesAddedToCsProject(ref csProject, absolutePath, addNestedDependencies);
						EnsureCsProjectHasCompileWinformCsFile(ref csProject, absolutePath);
						EnsureCsProjectHasCompileWinformDesignerCsFile(ref csProject, FullPathAndDisplayName.GetLinkedDesignerCsFileFromWinformCsFile(absolutePath));
						EnsureCsProjectHasEmbeddedResourceWinformResxFile(ref csProject, FullPathAndDisplayName.GetLinkedResxFileFromWinformCsFile(absolutePath));
					}
					else if (line.Trim().StartsWith("Window:", StringComparison.InvariantCultureIgnoreCase))
					{
						string relativePath = line.Trim().Replace("Window:", "").Trim();
						if (!relativePath.EndsWith(".xaml", StringComparison.InvariantCultureIgnoreCase))
							relativePath += ".xaml";
						string absolutePath = GetAbsolutePath(csFile, relativePath);
						if (addNestedDependencies)
							CSharpDependencies.EnsureCsProjectHasAdditionalDependenciesAddedToCsProject(ref csProject, absolutePath, addNestedDependencies);
						EnsureCsProjectHasPageXamlFile(ref csProject, absolutePath);
						EnsureCsProjectHasCompileXamlCsFile(ref csProject, FullPathAndDisplayName.GetLinkedXamlCsFileFromXamlFile(absolutePath));
					}
					//WinformOrWpf: Form: InputBox, Window: InputBoxWPF
					else if (line.Trim().StartsWith("File:", StringComparison.InvariantCultureIgnoreCase))
					{
						string relativePath = line.Trim().Replace("File:", "").Trim();
						string absolutePath = GetAbsolutePath(csFile, relativePath);
						if (addNestedDependencies)
							CSharpDependencies.EnsureCsProjectHasAdditionalDependenciesAddedToCsProject(ref csProject, absolutePath, addNestedDependencies);
						EnsureCsProjectHasCompileIndependantCsFile(ref csProject, absolutePath);
					}
					else if (line.Trim().StartsWith("Assembly:", StringComparison.InvariantCultureIgnoreCase))
					{
						string referenceName = line.Trim().Replace("Assembly:", "").Trim();
						EnsureCsProjectHasStandardReference(ref csProject, referenceName);
					}
					else if (line.Trim().StartsWith("Assembly own:", StringComparison.InvariantCultureIgnoreCase))
					{
						string ownReferenceName = line.Trim().Replace("Assembly own:", "").Trim();
						EnsureCsProjectHasFrancoisOtherDllsReference(ref csProject, ownReferenceName);
					}

					/*	Example of a cumbersome usage (note that some lines are just normal comments
						Additional dependencies and sample code:
						Minimum winforms
						Full framework
						Class: EncodeAndDecodeInterop
						Winforms: Form: InputBox
						WPF: Window: InputBoxWPF
						WPF: Class: WPFdraggableCanvas
						File: GoogleAPIs\Google.Apis.Tasks.v1.cs
						The following assemblies should be located in c:\francois\other\dlls\GoogleApis:
						Assembly: System.Security
						Assembly own: DotNetOpenAuth
						Assembly own: Google.Apis.Authentication.OAuth2
						Assembly own: Google.Apis
						Assembly own: Google.Apis.Samples.Helper
						Assembly own: Newtonsoft.JSon.Net35*/
				}

				if (additionalLineFound && blockCommentNestedCount == 0)
					return;
			}
		}

		public static void EnsureCorrectFileDependancies(string csprojFile, FullPathAndDisplayName[] files, bool addNestedDependencies)
		{
			Project project = new Project(csprojFile);

			foreach (FullPathAndDisplayName file in files)
				file.EnsureExistanceInCsProject_IncludeAllLinks(ref project, addNestedDependencies);

			project = null;

			//.xaml (also .xaml.cs)
			//.cs   (also .Designer.cs AND .resx)
			//.cs   (nothing)

			//ProjectItem[] compileProjectItems = project.GetItems("Compile").ToArray();
			//foreach (ProjectItem item in compileProjectItems)
			//    Console.WriteLine(item.EvaluatedInclude);//.UnevaluatedInclude);

			//project.AddItem(
			//    "Compile",
			//    @"..\..\SharedClasses\InputBoxWPF.xaml.cs",
			//    new Dictionary<string, string>()
			//    {
			//        { "Link", "InputBoxWPF.xaml.cs" },
			//        { "DependentUpon", "InputBoxWPF.xaml" },
			//    });
			//project.Save(Path.ChangeExtension(csprojFile, "2.csproj"));
			//<Compile Include="..\..\SharedClasses\InputBoxWPF.xaml.cs">
			//  <Link>InputBoxWPF.xaml.cs</Link>
			//  <DependentUpon>InputBoxWPF.xaml</DependentUpon>
			//</Compile>
			//<Page Include="..\..\SharedClasses\InputBoxWPF.xaml">
			//  <Link>InputBoxWPF.xaml</Link>
			//  <Generator>MSBuild:Compile</Generator>
			//</Page>

			//XmlDocument xmldoc = new XmlDocument();
			//xmldoc.Load(csprojFile);

			//XmlNamespaceManager mgr = new XmlNamespaceManager(xmldoc.NameTable);
			//mgr.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");

			//foreach (XmlNode item in xmldoc.SelectNodes("//x:ProjectGuid", mgr))
			//{
			//    string test = item.InnerText.ToString();
			//}

			//XmlNodeList compileNodes = xmldoc.SelectNodes("/Project/ItemGroup", mgr);//ItemGroup/Compile");
			//XmlNodeList pageNodes = doc.SelectNodes("/Project/ItemGroup/Page");
		}
	}

	public enum FileTypes { Cs, Xaml, WinFormCs, Undefined };
	public class FullPathAndDisplayName
	{
		public FileTypes FileType { get; private set; }
		public string FullPath { get; private set; }
		public string DisplayName { get; private set; }
		public string LinkedXamlCsFile { get; private set; }
		public string LinkedDesignerCsFile { get; private set; }
		public string LinkedResxFile { get; private set; }
		public FullPathAndDisplayName(string FullPath)
		{
			this.FullPath = FullPath;
			this.DisplayName = Path.GetFileName(FullPath);
			this.FileType =
				FullPath.EndsWith(".cs", StringComparison.InvariantCultureIgnoreCase) && (File.Exists(Path.ChangeExtension(FullPath, ".Designer.cs")) || File.Exists(Path.ChangeExtension(FullPath, ".resx"))) ? FileTypes.WinFormCs :
				FullPath.EndsWith(".cs", StringComparison.InvariantCultureIgnoreCase) ? FileTypes.Cs :
				FullPath.EndsWith(".xaml", StringComparison.InvariantCultureIgnoreCase) ? FileTypes.Xaml :
				FileTypes.Undefined;

			if (FileType == FileTypes.Undefined)
				UserMessages.ShowWarningMessage("Cannot get filtype of file " + FullPath);
			else if (FileType == FileTypes.Xaml)
			{
				string linkedXamlCsFile = GetLinkedXamlCsFileFromXamlFile(FullPath);
				if (!File.Exists(linkedXamlCsFile))
					UserMessages.ShowWarningMessage("Cannot find linked .xaml.cs file for xaml file: " + FullPath);
				else
					this.LinkedXamlCsFile = linkedXamlCsFile;
			}
			else if (FileType == FileTypes.WinFormCs)
			{
				//.Designer.cs
				string linkedDesignerCsFile = GetLinkedDesignerCsFileFromWinformCsFile(FullPath);
				if (!File.Exists(linkedDesignerCsFile))
					UserMessages.ShowWarningMessage("Cannot find linked .Designer.cs file for Winform cs file: " + FullPath);
				else
					this.LinkedDesignerCsFile = linkedDesignerCsFile;
				//.resx
				string linkedResxFile = GetLinkedResxFileFromWinformCsFile(FullPath);
				if (!File.Exists(linkedResxFile))
					UserMessages.ShowWarningMessage("Cannot find linked .resx file for Winform cs file: " + FullPath);
				else
					this.LinkedResxFile = linkedResxFile;
			}
		}

		public static string GetLinkedXamlCsFileFromXamlFile(string xamlFile)
		{
			return Path.ChangeExtension(xamlFile, ".xaml.cs");
		}

		public static string GetLinkedDesignerCsFileFromWinformCsFile(string winformCsFile)
		{
			return Path.ChangeExtension(winformCsFile, ".Designer.cs");
		}

		public static string GetLinkedResxFileFromWinformCsFile(string winformCsFile)
		{
			return Path.ChangeExtension(winformCsFile, ".resx");
		}

		public void EnsureExistanceInCsProject_IncludeAllLinks(ref Project csProject, bool addNestedDependencies)
		{
			if (this.FileType == FileTypes.Xaml)
			{
				if (!CSharpDependencies.IsWpfProject_FalseWinforms(ref csProject))
					CSharpDependencies.EnsureCsProjectHasMinimumWPFreferences(ref csProject);
				CSharpDependencies.EnsureCsProjectHasPageXamlFile(ref csProject, this.FullPath);
				CSharpDependencies.EnsureCsProjectHasCompileXamlCsFile(ref csProject, this.LinkedXamlCsFile);
			}
			else if (this.FileType == FileTypes.WinFormCs)
			{
				if (CSharpDependencies.IsWpfProject_FalseWinforms(ref csProject))
					CSharpDependencies.EnsureCsProjectHasMinimumWinformsReferences(ref csProject);
				CSharpDependencies.EnsureCsProjectHasCompileWinformCsFile(ref csProject, this.FullPath);
				CSharpDependencies.EnsureCsProjectHasCompileWinformDesignerCsFile(ref csProject, this.LinkedDesignerCsFile);
				CSharpDependencies.EnsureCsProjectHasEmbeddedResourceWinformResxFile(ref csProject, this.LinkedResxFile);
			}
			else if (this.FileType == FileTypes.Cs)
			{
				CSharpDependencies.EnsureCsProjectHasCompileIndependantCsFile(ref csProject, this.FullPath);
			}

			if (this.FileType == FileTypes.Cs || this.FileType == FileTypes.WinFormCs)
				CSharpDependencies.EnsureCsProjectHasAdditionalDependenciesAddedToCsProject(ref csProject, this.FullPath, addNestedDependencies);
		}

		public override string ToString()
		{
			return DisplayName + (
				FileType == FileTypes.WinFormCs ? " (winform)" :
				FileType == FileTypes.Xaml ? " (WPF window)" : "");
		}
	}
}