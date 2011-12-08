using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharedClasses
{
	public class SystemInfoInterop
	{
		internal static Boolean _timerElapsed;
		/// <summary>
		/// Pause for amount of milliseconds without freezing the computer.
		/// </summary>
		/// <param name="timervalue"></param>
		public static void Wait(int timervalue)
		{
			timerSleep.Tick += new EventHandler(timerSleep_Tick);
			_Wait(timervalue);
			timerSleep.Tick -= new EventHandler(timerSleep_Tick);
		}
		internal static void _Wait(int timervalue)
		{
			timerSleep.Interval = timervalue;
			timerSleep.Enabled = true;
			_timerElapsed = false;
			while (!_timerElapsed) Application.DoEvents();
		}
		internal static System.Windows.Forms.Timer timerSleep = new System.Windows.Forms.Timer();
		internal static void timerSleep_Tick(object sender, EventArgs e)
		{
			_timerElapsed = true;
			timerSleep.Enabled = false;
		}

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

		//private static bool Is64BitProcess
		//{
		//	get { return IntPtr.Size == 8; }
		//}

		public static bool Is64BitOperatingSystem
		{
			get
			{
				// Clearly if this is a 64-bit process we must be on a 64-bit OS.
				if (Environment.Is64BitProcess)//Is64BitProcess)
					return true;
				// Ok, so we are a 32-bit process, but is the OS 64-bit?
				// If we are running under Wow64 than the OS is 64-bit.
				return Environment.Is64BitOperatingSystem;
				//bool isWow64;
				//return ModuleContainsFunction("kernel32.dll", "IsWow64Process") && User32stuff.Win32.IsWow64Process(User32stuff.Win32.GetCurrentProcess(), out isWow64) && isWow64;
			}
		}

		private static bool ModuleContainsFunction(string moduleName, string methodName)
		{
			
			IntPtr hModule = Win32Api.GetModuleHandle(moduleName);
			if (hModule != IntPtr.Zero)
				return Win32Api.GetProcAddress(hModule, methodName) != IntPtr.Zero;
			return false;
		}
	}
}
