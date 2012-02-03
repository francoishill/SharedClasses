using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SharedClasses
{
	public class RegistryInterop
	{
		//TODO: Need to place this next couple of functions in their own class (and file)
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
			get { return IntPtr.Size == 8; }
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

		private static void DoNotUseOnOwn_EnsureClassesRootSubpathExists(String ClassesRootSub)
		{
			Microsoft.Win32.RegistryKey regkeyClassesRoot = Microsoft.Win32.Registry.ClassesRoot;
			Boolean ExtensionKeyFound = false;
			foreach (String s in regkeyClassesRoot.GetSubKeyNames()) if (s.ToUpper() == ClassesRootSub.ToUpper()) ExtensionKeyFound = true;
			if (!ExtensionKeyFound) regkeyClassesRoot.CreateSubKey(ClassesRootSub);
		}

		private static void DoNotUseOnOwn_EnsureClassesRootSubShellpathExists(String ClassesRootSub, String DefaultCommandName = null)
		{
			DoNotUseOnOwn_EnsureClassesRootSubpathExists(ClassesRootSub);

			Microsoft.Win32.RegistryKey tempregkeyFileExtension = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ClassesRootSub, true);
			Boolean ShellKeyFound = false;
			foreach (String s in tempregkeyFileExtension.GetSubKeyNames()) if (s.ToUpper() == "Shell".ToUpper()) ShellKeyFound = true;
			if (!ShellKeyFound) tempregkeyFileExtension.CreateSubKey("Shell");
			if (DefaultCommandName != null) tempregkeyFileExtension.OpenSubKey("Shell", true).SetValue("", DefaultCommandName);
		}

		private static void AddCommandToFolder(String CommandName, String CommandLine, String CommandDisplayName, String CommandIcon, String DefaultCommandName = null, string ThisAppKeyName = null)//"%1" is the file full path when right clicking on it
		{
			//Check if it is windows 7
			Boolean Windows7 = GetMachineOS() == WindowsVersion.Win7;
			if (Windows7) AddCommandToFolder_Windows7(ThisAppKeyName, CommandName, CommandLine, CommandDisplayName, CommandIcon, DefaultCommandName);
			else AddCommandToFolder_Normal(ThisAppKeyName, CommandName, CommandLine, CommandDisplayName, CommandIcon, DefaultCommandName);
		}

		private static void AddCommandToFolder_Normal(string ThisAppKeyName, String CommandName, String CommandLine, String CommandDisplayName, String CommandIcon, String DefaultCommandName = null)
		{
			//Check if system is 64bit
			Boolean Is64Bit = Is64BitOperatingSystem;

			DoNotUseOnOwn_EnsureClassesRootSubShellpathExists("Folder", DefaultCommandName);

			//Microsoft.Win32.RegistryKey tempregkeyShellFolder = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey("Folder", true).OpenSubKey("Shell", true);
			Microsoft.Win32.RegistryKey tempregkeyShellFolder = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.ClassesRoot, Is64Bit ? Microsoft.Win32.RegistryView.Registry64 : Microsoft.Win32.RegistryView.Registry32).OpenSubKey("Folder", true).OpenSubKey("Shell", true);
			Boolean CommandNameKeyFound = false;
			foreach (String s in tempregkeyShellFolder.GetSubKeyNames()) if (s.ToUpper() == CommandName.ToUpper()) CommandNameKeyFound = true;
			if (!CommandNameKeyFound) tempregkeyShellFolder.CreateSubKey(CommandName);

			tempregkeyShellFolder.OpenSubKey(CommandName, true).SetValue("", CommandDisplayName == null ? CommandName : CommandDisplayName);
			if (CommandIcon != null) tempregkeyShellFolder.OpenSubKey(CommandName, true).SetValue("Icon", CommandIcon);

			Microsoft.Win32.RegistryKey tempregkeyCommandName = tempregkeyShellFolder.OpenSubKey(CommandName, true);
			Boolean CommandKeyFound = false;
			foreach (String s in tempregkeyCommandName.GetSubKeyNames()) if (s.ToUpper() == "Command".ToUpper()) CommandKeyFound = true;
			if (!CommandKeyFound) tempregkeyCommandName.CreateSubKey("Command");

			tempregkeyCommandName.OpenSubKey("Command", true).SetValue("", CommandLine);
		}

		private static void AddCommandToFolder_Windows7(string ThisAppKeyName, String CommandName, String CommandLine, String CommandDisplayName, String CommandIcon, String DefaultCommandName = null)
		{
			//Check if system is 64bit
			Boolean Is64Bit = Is64BitOperatingSystem;

			DoNotUseOnOwn_EnsureClassesRootSubShellpathExists("Folder", DefaultCommandName);

			string tmpFullCommandName = "_" + ThisAppKeyName + "." + CommandName;

			string CommandStoreRelativePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell";
			string FullCommandPath = @"HKEY_LOCAL_MACHINE\" + CommandStoreRelativePath + "\\" + tmpFullCommandName;
			string ThisAppFullFolderShellPath = @"HKEY_CLASSES_ROOT\Folder\shell\" + ThisAppKeyName;

			//Sets the command and icon of it
			RegistryKey LocalMachineKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Is64Bit ? Microsoft.Win32.RegistryView.Registry64 : Microsoft.Win32.RegistryView.Registry32);
			LocalMachineKey.CreateSubKey(CommandStoreRelativePath + "\\" + tmpFullCommandName + @"\Command");
			LocalMachineKey.OpenSubKey(CommandStoreRelativePath + "\\" + tmpFullCommandName, true).SetValue("", CommandDisplayName == null ? CommandName : CommandDisplayName);
			LocalMachineKey.OpenSubKey(CommandStoreRelativePath + "\\" + tmpFullCommandName + @"\Command", true).SetValue("", CommandLine);
			LocalMachineKey.OpenSubKey(CommandStoreRelativePath + "\\" + tmpFullCommandName, true).SetValue("Icon", CommandIcon);

			//Enusre default value is "null"
			Boolean DefaultValueFound = false;
			foreach (string val in Registry.ClassesRoot.OpenSubKey(@"Folder\shell\" + ThisAppKeyName, false).GetValueNames()) if (val == "") DefaultValueFound = true;
			if (DefaultValueFound) Registry.ClassesRoot.OpenSubKey(@"Folder\shell\" + ThisAppKeyName, true).DeleteValue("");

			//Set the icon
			Registry.ClassesRoot.OpenSubKey(@"Folder\shell\" + ThisAppKeyName, true).SetValue("Icon", CommandIcon);

			//Set the subommands (add if currently existing)
			string CurrentSubCommands = Registry.GetValue(ThisAppFullFolderShellPath, "SubCommands", "").ToString();
			//if (!CurrentSubCommands.ToUpper().Contains(tmpFullCommandName.ToUpper()))
			//{
			if (CurrentSubCommands.Length > 0) CurrentSubCommands += ";";
			CurrentSubCommands += tmpFullCommandName;
			//}
			Registry.SetValue(ThisAppFullFolderShellPath, "SubCommands", CurrentSubCommands);
			LocalMachineKey.Close();
		}

		private static void AddSeperatorToFolder_Windows7(string ThisAppKeyName)
		{
			string ThisAppFullFolderShellPath = @"HKEY_CLASSES_ROOT\Folder\shell\" + ThisAppKeyName;
			string CurrentSubCommands = Registry.GetValue(ThisAppFullFolderShellPath, "SubCommands", "").ToString();
			if (CurrentSubCommands.Length > 0) CurrentSubCommands += ";";
			CurrentSubCommands += "Windows.separator";
			Registry.SetValue(ThisAppFullFolderShellPath, "SubCommands", CurrentSubCommands);
		}

		private static void DoNotUseOnOwn_AddFileExtensionToFileTypeHandlerRemoveCommands(String FileExtension, String FileTypeName)
		{
			DoNotUseOnOwn_EnsureClassesRootSubpathExists(FileExtension);
			Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(FileExtension, true).SetValue("", FileTypeName);
			Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(FileExtension, true).DeleteSubKeyTree("Shell", false);
		}

		public static void AddCommandToFileTypeHandlerAndAddExstensionListToHandler(List<string> ExtensionList, String FileTypeName, String FileTypeDescription, String FileTypeDefaultIcon, String CommandName, String CommandLine, String CommandDisplayName, String CommandIcon, String DefaultCommandName = null)
		{
			Boolean Continue = true;
			foreach (String extension in ExtensionList)
			{
				if (extension.Length > 1)
				{
					if (!extension.StartsWith("."))
					{
						Continue = false;
						MessageBox.Show("Each extension must have at least two characters", "Extensions error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
				else
				{
					Continue = false;
					MessageBox.Show("Extensions must start with a dot: \".\"", "Extensions error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}

			if (Continue)
			{
				DoNotUseOnOwn_EnsureClassesRootSubShellpathExists(FileTypeName, DefaultCommandName);

				if (FileTypeDescription != null) Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(FileTypeName, true).SetValue("", FileTypeDescription);
				else Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(FileTypeName, true).DeleteValue("", false);

				Microsoft.Win32.RegistryKey tempregkeyShellFileTypeName = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(FileTypeName, true).OpenSubKey("Shell", true);
				Boolean CommandNameKeyFound = false;
				foreach (String s in tempregkeyShellFileTypeName.GetSubKeyNames()) if (s.ToUpper() == CommandName.ToUpper()) CommandNameKeyFound = true;
				if (!CommandNameKeyFound) tempregkeyShellFileTypeName.CreateSubKey(CommandName);

				tempregkeyShellFileTypeName.OpenSubKey(CommandName, true).SetValue("", CommandDisplayName == null ? CommandName : CommandDisplayName);
				if (CommandIcon != null) tempregkeyShellFileTypeName.OpenSubKey(CommandName, true).SetValue("Icon", CommandIcon);

				Microsoft.Win32.RegistryKey tempregkeyCommandName = tempregkeyShellFileTypeName.OpenSubKey(CommandName, true);
				Boolean CommandKeyFound = false;
				foreach (String s in tempregkeyCommandName.GetSubKeyNames()) if (s.ToUpper() == "Command".ToUpper()) CommandKeyFound = true;
				if (!CommandKeyFound) tempregkeyCommandName.CreateSubKey("Command");

				tempregkeyCommandName.OpenSubKey("Command", true).SetValue("", CommandLine);

				if (FileTypeDefaultIcon != null)
				{
					Microsoft.Win32.RegistryKey tempregkeyFileTypeName = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(FileTypeName, true);
					Boolean DefaultIconKeyFound = false;
					foreach (String s in tempregkeyFileTypeName.GetSubKeyNames()) if (s.ToUpper() == "DefaultIcon".ToUpper()) DefaultIconKeyFound = true;
					if (!DefaultIconKeyFound) tempregkeyFileTypeName.CreateSubKey("DefaultIcon");

					tempregkeyFileTypeName.OpenSubKey("DefaultIcon", true).SetValue("", FileTypeDefaultIcon);
				}

				foreach (String extension in ExtensionList)
					DoNotUseOnOwn_AddFileExtensionToFileTypeHandlerRemoveCommands(extension, FileTypeName);
			}
		}
	}
}