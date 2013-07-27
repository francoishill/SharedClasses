using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
//using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;

namespace SharedClasses
{
	public delegate void ShowErrorMessageDelegate(string errorMessage, string title);

	public class RegistryInterop
	{
		/* Additional dependencies for this file:
			Minimum winforms
			Class: fastJSON
			Class: FeedbackMessageTypes*/

		//Need to place this next couple of functions in their own class (and file)
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public extern static IntPtr GetProcAddress(IntPtr hModule, string methodName);
		[DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
		public extern static IntPtr GetModuleHandle(string moduleName);
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool IsWow64Process(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool isWow64);
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public extern static IntPtr GetCurrentProcess();
		public enum WindowsVersion { Win3_1, Win95, Win98SE, Win98, WinME, WinNT3_51, WinNT4_0, Win2000, WinXP, Win2003, WinVista, Win7, WinCE, Unix, Unknown };
		public static WindowsVersion GetMachineOS()
		{
			WindowsVersion WinType = WindowsVersion.Unknown;
			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32S:
					WinType = WindowsVersion.Win3_1;
					break;
				case PlatformID.Win32Windows:

					switch (Environment.OSVersion.Version.Minor)
					{
						case 0:
							WinType = WindowsVersion.Win95;
							break;
						case 10:
							if (Environment.OSVersion.Version.Revision.ToString() == "2222A")
								WinType = WindowsVersion.Win98SE;
							else
								WinType = WindowsVersion.Win98;
							break;
						case 90:
							WinType = WindowsVersion.WinME;
							break;
					}
					break;

				case PlatformID.Win32NT:
					switch (Environment.OSVersion.Version.Major)
					{
						case 3:
							WinType = WindowsVersion.WinNT3_51;
							break;
						case 4:
							WinType = WindowsVersion.WinNT4_0;
							break;
						case 5:
							switch (Environment.OSVersion.Version.Minor)
							{
								case 0:
									WinType = WindowsVersion.Win2000;
									break;
								case 1:
									WinType = WindowsVersion.WinXP;
									break;
								case 2:
									WinType = WindowsVersion.Win2003;
									break;
							}

							break;
						case 6:
							switch (Environment.OSVersion.Version.Minor)
							{
								case 0:
									WinType = WindowsVersion.WinVista;
									break;
								default:
									WinType = WindowsVersion.Win7;
									break;
								//case 1:
								//    strVersion = "Windows 2008";
								//    break;
								//case 2:
								//    strVersion = "Windows 7";
								//    break;
							}
							break;
					}
					break;

				case PlatformID.WinCE:
					WinType = WindowsVersion.WinCE;
					break;
				case PlatformID.Unix:
					WinType = WindowsVersion.Unix;
					break;
			}

			return WinType;
		}
		private static bool Is64BitProcess
		{
			get { return Environment.Is64BitOperatingSystem; } //return IntPtr.Size == 8; }
		}
		public static bool Is64BitOperatingSystem
		{
			get
			{
				// Clearly if this is a 64-bit process we must be on a 64-bit OS.
				if (Is64BitProcess)
					return true;
				// Ok, so we are a 32-bit process, but is the OS 64-bit?
				// If we are running under Wow64 than the OS is 64-bit.
				bool isWow64;
				return ModuleContainsFunction("kernel32.dll", "IsWow64Process") && IsWow64Process(GetCurrentProcess(), out isWow64) && isWow64;
			}
		}
		private static bool ModuleContainsFunction(string moduleName, string methodName)
		{
			IntPtr hModule = GetModuleHandle(moduleName);
			if (hModule != IntPtr.Zero)
				return GetProcAddress(hModule, methodName) != IntPtr.Zero;
			return false;
		}

		public class MainContextMenuItem
		{
			public string MainmenuItemRegistryName;
			//public string MainmenuItemDisplayName;
			public string MainmenuItemIconpath;
			public List<SubContextMenuItem> SubCommands;
			public MainContextMenuItem() { }
			public MainContextMenuItem(List<string> FileExtensionsInClassesRoot, string MainmenuItemRegistryName, /*string MainmenuItemDisplayName, */string MainmenuItemIconpath, List<SubContextMenuItem> SubCommands)
			{
				this.MainmenuItemRegistryName = MainmenuItemRegistryName;
				//this.MainmenuItemDisplayName = MainmenuItemDisplayName;
				this.MainmenuItemIconpath = MainmenuItemIconpath;
				this.SubCommands = SubCommands;
			}

			private string GetSubcommandNamesConcatenated(string currentPathInClassesRoot)
			{
				List<string> tmpSubcommandList = new List<string>();
				foreach (SubContextMenuItem sc in this.SubCommands)
					if (sc.FileExtensionsInClassesRoot.Contains(currentPathInClassesRoot, StringComparer.InvariantCultureIgnoreCase))
						if (sc.IsSeparator() || !tmpSubcommandList.Contains(sc.CommandName))
							tmpSubcommandList.Add(sc.CommandName);

				string str = "";
				foreach (string scn in tmpSubcommandList)
					str += (str.Length > 0 ? ";" : "") + scn;
				return str;
			}

			//[Obsolete("Please use GetNsisLines()", true)]
			//public bool WriteRegistryEntries()
			//{
			//    Boolean Is64Bit = Is64BitOperatingSystem;
			//    RegistryView registryViewToUse = Is64Bit ? RegistryView.Registry64 : RegistryView.Registry32;

			//    bool succeeded = false;

			//    try
			//    {
			//        /*foreach (string pathInClassesRoot in this.FileExtensionsInClassesRoot)
			//            using (RegistryKey subfolderInClassesRootShell =
			//                    RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, registryViewToUse)
			//                            .OpenOrCreateWriteableSubkey(pathInClassesRoot + "\\" + "shell" + "\\" + this.MainmenuItemRegistryName))
			//            {
			//                //subfolderInClassesRootShell.SetValue(null, this.MainmenuItemDisplayName, RegistryValueKind.String);
			//                subfolderInClassesRootShell.SetValue("Icon", this.MainmenuItemIconpath, RegistryValueKind.String);
			//                subfolderInClassesRootShell.SetValue("SubCommands", GetSubcommandNamesConcatenated());

			//                foreach (SubContextMenuItem subcommand in this.SubCommands)
			//                {
			//                    using (RegistryKey subfolderInCommandStoreShell =
			//                        RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryViewToUse)
			//                            .OpenOrCreateWriteableSubkey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell\" + subcommand.CommandName))
			//                    {
			//                        subfolderInCommandStoreShell.SetValue(null, subcommand.DisplayName);
			//                        subfolderInCommandStoreShell.SetValue("Icon", subcommand.CommandIconpath);

			//                        using (RegistryKey subcommandRegistryKey = subfolderInCommandStoreShell.OpenOrCreateWriteableSubkey("Command"))
			//                        {
			//                            subcommandRegistryKey.SetValue(null, subcommand.CommandlineExcludingArgumentString);

			//                            succeeded = true;
			//                        }
			//                    }
			//                }
			//            }*/
			//    }
			//    catch (Exception exc)
			//    {
			//        UserMessages.ShowErrorMessage("Error setting registry entries: " + exc.Message);
			//        return false;
			//    }

			//    return succeeded;
			//}

			private enum RegistryRootKeys { HKCR, HKLM, HKCU, HKU, HKCC, HKDD, HKPD, SHCTX };

			private string GetNsisWriteRegStrLine(RegistryRootKeys rootKey, string subKey, string valueName, string valueStringVal)
			{
				return string.Format(
					"WriteRegStr {0} \"{1}\" \"{2}\" \"{3}\"", rootKey.ToString(), subKey, valueName, valueStringVal);
			}

			const string commandStore_Shell_Subpath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell";
			public List<string> GetRegistryAssociationNsisLines(Action<string> actionOnError)
			{
				List<string> nsisLines = new List<string>();
				const string labelStartUseCurrentClassesRootVariable = "defaultkeyIsBlank";
				int tmpcounter = 0;

				foreach (SubContextMenuItem subcommand in this.SubCommands)
					foreach (string pathInClassesRoot in subcommand.FileExtensionsInClassesRoot)
					{
						tmpcounter++;
						nsisLines.Add("");
						
						string gotolabelClassesRootIsBlank = labelStartUseCurrentClassesRootVariable + tmpcounter;
						
						nsisLines.Add(string.Format("StrCpy $classesRootMainKey \"{0}\" \"\" 0", pathInClassesRoot));
						nsisLines.Add("ReadRegStr $0 HKCR $classesRootMainKey \"\"");
						nsisLines.Add(string.Format("StrCmp $0 \"\" {0} 0", gotolabelClassesRootIsBlank));
						nsisLines.Add(string.Format("StrCpy $classesRootMainKey $0 \"\" 0 ;the (default) value of CLASSES_ROOT\\{0} is not empty, using its value to get new path in CLASSES_ROOT", pathInClassesRoot));

						//nsisLines.Add(string.Format("StrCpy $classesRootMainKey \"$classesRootMainKey\\CurVer\" \"\" 0", pathInClassesRoot));
						nsisLines.Add("ReadRegStr $1 HKCR \"$classesRootMainKey\\CurVer\" \"\"");
						nsisLines.Add(string.Format("StrCmp $1 \"\" {0} 0", gotolabelClassesRootIsBlank) + " ; The CurVer value was empty, jump to label");
						nsisLines.Add(string.Format("StrCpy $classesRootMainKey $1 \"\" 0 ;the (CurVer) value of CLASSES_ROOT\\{0} is not empty, using its value to get new path in CLASSES_ROOT", pathInClassesRoot));
						
						nsisLines.Add(string.Format("{0}:", gotolabelClassesRootIsBlank) + " ; jump to here when we are happy with the current classesRootMainKey variable");

						//string pathInShell = pathInClassesRoot + @"\shell\" + this.MainmenuItemRegistryName;
						string pathInShell = @"$classesRootMainKey\shell\" + this.MainmenuItemRegistryName;

						string tmpline = GetNsisWriteRegStrLine(RegistryRootKeys.HKCR, pathInShell, "Icon", "$\\\"" + this.MainmenuItemIconpath.Trim('\\') + "$\\\"");
						//if (!nsisLines.Contains(tmpline))
						nsisLines.Add(tmpline);
						tmpline = GetNsisWriteRegStrLine(RegistryRootKeys.HKCR, pathInShell, "SubCommands", GetSubcommandNamesConcatenated(pathInClassesRoot));
						//if (!nsisLines.Contains(tmpline))
						nsisLines.Add(tmpline);
					}

				foreach (SubContextMenuItem subcommand in this.SubCommands)
				{
					if (subcommand.IsSeparator())
						continue;
					string commandStoreShell_CommandSubpath = commandStore_Shell_Subpath + "\\" + subcommand.CommandName;
					nsisLines.Add(GetNsisWriteRegStrLine(RegistryRootKeys.HKLM, commandStoreShell_CommandSubpath, "", subcommand.DisplayName));
					nsisLines.Add(GetNsisWriteRegStrLine(RegistryRootKeys.HKLM, commandStoreShell_CommandSubpath, "Icon", "$\\\"" + subcommand.CommandIconpath.Trim('\\') + "$\\\""));
					nsisLines.Add(GetNsisWriteRegStrLine(RegistryRootKeys.HKLM, commandStoreShell_CommandSubpath + "\\Command", "", "$\\\"" + subcommand.CommandlineExcludingArgumentString.Trim('\\') + "$\\\"" + subcommand.GetNsisArgumentsPostfixToCommandline(actionOnError)));
				}
				return nsisLines;
			}

			private string GetNsisDeleteRegKeyLine(RegistryRootKeys rootKey, string subkey)
			{
				return string.Format("DeleteRegKey {0} \"{1}\"", rootKey.ToString(), subkey);
			}
			public List<string> GetRegistryUnassociationNsisLines()
			{
				List<string> nsisLines = new List<string>();
				const string labelStartUseCurrentClassesRootVariable = "defaultkeyIsBlank";
				int tmpcounter = 0;

				foreach (SubContextMenuItem subcommand in this.SubCommands)
					foreach (string pathInClassesRoot in subcommand.FileExtensionsInClassesRoot)
					{
						tmpcounter++;
						nsisLines.Add("");
						string gotolabelClassesRootIsBlank = labelStartUseCurrentClassesRootVariable + tmpcounter;
						
						nsisLines.Add(string.Format("StrCpy $classesRootMainKey \"{0}\" \"\" 0", pathInClassesRoot));
						nsisLines.Add("ReadRegStr $0 HKCR $classesRootMainKey \"\"");
						nsisLines.Add(string.Format("StrCmp $0 \"\" {0} 0", gotolabelClassesRootIsBlank));
						nsisLines.Add(string.Format("StrCpy $classesRootMainKey $0 \"\" 0 ;the (default) value of CLASSES_ROOT\\{0} is not empty, using its value to get new path in CLASSES_ROOT", pathInClassesRoot));

						//nsisLines.Add(string.Format("StrCpy $classesRootMainKey \"$classesRootMainKey\\CurVer\" \"\" 0", pathInClassesRoot));
						nsisLines.Add("ReadRegStr $1 HKCR \"$classesRootMainKey\\CurVer\" \"\"");
						nsisLines.Add(string.Format("StrCmp $1 \"\" {0} 0", gotolabelClassesRootIsBlank) + " ; The CurVer value was empty, jump to label");
						nsisLines.Add(string.Format("StrCpy $classesRootMainKey $1 \"\" 0 ;the (CurVer) value of CLASSES_ROOT\\{0} is not empty, using its value to get new path in CLASSES_ROOT", pathInClassesRoot));
						
						nsisLines.Add(string.Format("{0}:", gotolabelClassesRootIsBlank));

						string tmpline = GetNsisDeleteRegKeyLine(RegistryRootKeys.HKCR, @"$classesRootMainKey\shell\" + this.MainmenuItemRegistryName);
						//if (!nsisLines.Contains(tmpline))
						nsisLines.Add(tmpline);
					}

				foreach (SubContextMenuItem subcommand in this.SubCommands)
				{
					if (subcommand.IsSeparator())
						continue;
					nsisLines.Add(GetNsisDeleteRegKeyLine(RegistryRootKeys.HKLM, commandStore_Shell_Subpath + "\\" + subcommand.CommandName + "\\Command"));
					nsisLines.Add(GetNsisDeleteRegKeyLine(RegistryRootKeys.HKLM, commandStore_Shell_Subpath + "\\" + subcommand.CommandName));
				}
				return nsisLines;
			}
		}

		public class SubContextMenuItem
		{
			public List<string> FileExtensionsInClassesRoot;
			public string CommandName;
			public string DisplayName;
			public string CommandIconpath;
			public string CommandlineExcludingArgumentString;
			public string CommandlinePassedConstantArgument;//For instance "zipfile" or "unzipfile"
			public int ArgumentCount;
			public SubContextMenuItem() { }
			public SubContextMenuItem(List<string> FileExtensionsInClassesRoot, string CommandName, string DisplayName, string CommandIconpath, string CommandlineExcludingArgumentString, string CommandlinePassedConstantArgument, int ArgumentCount)
			{
				this.FileExtensionsInClassesRoot = FileExtensionsInClassesRoot;
				this.CommandName = CommandName;
				this.DisplayName = DisplayName;
				this.CommandIconpath = CommandIconpath;
				this.CommandlineExcludingArgumentString = CommandlineExcludingArgumentString;
				this.CommandlinePassedConstantArgument = CommandlinePassedConstantArgument;
				this.ArgumentCount = ArgumentCount;
			}
			public string GetNsisArgumentsPostfixToCommandline(Action<string> actionOnError)
			{
				if (this.ArgumentCount > 1)
				{
					//UserMessages.ShowWarningMessage("Currently more than 1 argument for registry association is unsupported");
					if (actionOnError != null)
						actionOnError("Currently more than 1 argument for registry association is unsupported");
					return "";
				}
				return
					(!string.IsNullOrWhiteSpace(CommandlinePassedConstantArgument) ? " $\\\"" + CommandlinePassedConstantArgument + "$\\\"" : "")
					+ " $\\\"%V$\\\"";
			}
			public bool IsSeparator() { return this.CommandName == "|"; }
		}

		public static List<string> GetNsisLines(MainContextMenuItem mainItem, Action<string> actionOnError)
		{
			return mainItem.GetRegistryAssociationNsisLines(actionOnError);
		}

		public static string GetAppPathFromRegistry(string exeKeyName)
		{
			using (var appPathRootkey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32)
				.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths"))
			{
				if (null == appPathRootkey)
					return null;
				var appPathKeys = appPathRootkey.GetSubKeyNames().ToArray();
				for (int i = 0; i < appPathKeys.Length; i++)
				{
					if (appPathKeys[i].Equals(exeKeyName, StringComparison.InvariantCultureIgnoreCase))
					{
						using (var foundKey = appPathRootkey.OpenSubKey(appPathKeys[i]))
							return foundKey.GetValue(null).ToString();
					}
					else if (!exeKeyName.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase)
						&& appPathKeys[i].Equals(exeKeyName + ".exe", StringComparison.InvariantCultureIgnoreCase))
					{
						using (var foundKey = appPathRootkey.OpenSubKey(appPathKeys[i]))
							return foundKey.GetValue(null).ToString();
					}
				}
			}
			return null;
		}

		public static RegistryInterop.MainContextMenuItem GetRegistryAssociationItemFromJsonFile(string registryEntriesJsonFilepath, Action<string, FeedbackMessageTypes> actionOnMessage)
		{
			if (!File.Exists(registryEntriesJsonFilepath))
			{
				actionOnMessage("No file for project to define registry entries, file not found: " + registryEntriesJsonFilepath, FeedbackMessageTypes.Status);
				return null;
			}
			RegistryInterop.MainContextMenuItem mainRegistryItem = new RegistryInterop.MainContextMenuItem();
			try
			{
				JSON.Instance.FillObject(mainRegistryItem, File.ReadAllText(registryEntriesJsonFilepath));
				return mainRegistryItem;
			}
			catch (Exception exc)
			{
				actionOnMessage("Could not fill json object from file contents: " + registryEntriesJsonFilepath
						+ Environment.NewLine + "Error:"
						+ Environment.NewLine + exc.Message,
						FeedbackMessageTypes.Error);
				return null;
			}
		}

		[Obsolete("Cannot use this anymore. Windows 8 UAC on (even Win 7 with UAC on).", true)]
		public static void AssociateUrlProtocolHandler(string urlStartString, string protocolName, string fullCommandline) { }
		/*public static void AssociateUrlProtocolHandler(string urlStartString, string protocolName, string fullCommandline)
		{
			//var classesRootKey = RegistryKey
			//	.OpenBaseKey(RegistryHive.LocalMachine, RegistryInterop.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32)
			//	.OpenSubKey(@"SOFTWARE\Classes");
			//var quickaccessKey = classesRootKey.CreateSubKey(urlStartString);
			//quickaccessKey.SetValue(null, "URL:" + protocolName);
			//quickaccessKey.SetValue("URL Protocol", "");
			//var shellSubkey = quickaccessKey.CreateSubKey("shell");
			//var openSubkey = shellSubkey.CreateSubKey("open");
			//var commandSubkey = openSubkey.CreateSubKey("command");
			//commandSubkey.SetValue(null, fullCommandline);

			var classesRootKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryInterop.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
			var quickaccessKey = classesRootKey.CreateSubKey(urlStartString);
			quickaccessKey.SetValue(null, "URL:" + protocolName);
			quickaccessKey.SetValue("URL Protocol", "");
			var shellSubkey = quickaccessKey.CreateSubKey("shell");
			var openSubkey = shellSubkey.CreateSubKey("open");
			var commandSubkey = openSubkey.CreateSubKey("command");
			commandSubkey.SetValue(null, fullCommandline);
		}*/
	}

	public static class RegistryExtensions
	{
		public static RegistryKey OpenOrCreateWriteableSubkey(this RegistryKey thiskey, string SubNameOrPath)
		{
			RegistryKey rk = thiskey.OpenSubKey(SubNameOrPath, true);
			if (rk == null)
				rk = thiskey.CreateSubKey(SubNameOrPath);
			return rk;
		}
	}
}