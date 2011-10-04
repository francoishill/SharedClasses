using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

public static class Win32Api
{
	public static readonly int WM_NCLBUTTONDOWN = 0xA1;
	public static readonly int HT_CAPTION = 0x2;

	[DllImportAttribute("user32.dll")]
	public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
	[DllImportAttribute("user32.dll")]
	public static extern bool ReleaseCapture();

	/// <summary>The GetForegroundWindow function returns a handle to the foreground window.</summary>
	[DllImport("user32.dll")]
	public static extern IntPtr GetForegroundWindow();

	public static readonly int WM_HOTKEY = 786;
	[DllImport("user32.dll")]
	public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

	public static readonly uint MOD_ALT = 1;
	public static readonly uint MOD_CONTROL = 2;
	public static readonly uint MOD_SHIFT = 4;
	public static readonly uint MOD_WIN = 8;
	public static readonly int Hotkey1 = 500;
	public static readonly int Hotkey2 = 501;

	[DllImport("user32.dll")]
	public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

	// Activate or minimize a window
	[DllImportAttribute("User32.DLL")]
	public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
	public static readonly int SW_SHOW = 5;
	public static readonly int SW_MINIMIZE = 6;
	public static readonly int SW_RESTORE = 9;

	public static readonly int HWND_TOPMOST = -1;
	[DllImport("user32.dll", EntryPoint = "SetWindowPos")]
	public static extern bool SetWindowPos(
			 int hWnd,           // window handle
			 int hWndInsertAfter,    // placement-order handle
			 int X,          // horizontal position
			 int Y,          // vertical position
			 int cx,         // width
			 int cy,         // height
			 uint uFlags);

	// For Windows Mobile, replace user32.dll with coredll.dll
	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

	[DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
	public static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

	public static readonly int WM_SCROLL = 276; // Horizontal scroll
	public static readonly int WM_VSCROLL = 277; // Vertical scroll
	public static readonly int SB_LINEUP = 0; // Scrolls one line up
	public static readonly int SB_LINELEFT = 0;// Scrolls one cell left
	public static readonly int SB_LINEDOWN = 1; // Scrolls one line down
	public static readonly int SB_LINERIGHT = 1;// Scrolls one cell right
	public static readonly int SB_PAGEUP = 2; // Scrolls one page up
	public static readonly int SB_PAGELEFT = 2;// Scrolls one page left
	public static readonly int SB_PAGEDOWN = 3; // Scrolls one page down
	public static readonly int SB_PAGERIGTH = 3; // Scrolls one page right
	public static readonly int SB_PAGETOP = 6; // Scrolls to the upper left
	public static readonly int SB_LEFT = 6; // Scrolls to the left
	public static readonly int SB_PAGEBOTTOM = 7; // Scrolls to the upper right
	public static readonly int SB_RIGHT = 7; // Scrolls to the right
	public static readonly int SB_ENDSCROLL = 8; // Ends scroll

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

	public static readonly int SW_SHOWNOACTIVATE = 4;
	public static readonly uint SWP_NOACTIVATE = 0x0010;

	[DllImport("user32.dll")]
	public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
	[DllImport("msvcr70.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern int _fpreset();

	internal struct LASTINPUTINFO
	{
		public uint cbSize;

		public uint dwTime;
	}

	[DllImport("User32.dll")]
	public static extern bool LockWorkStation();

	[DllImport("User32.dll")]
	private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

	[DllImport("Kernel32.dll")]
	private static extern uint GetLastError();

	public static uint GetIdleTime()
	{
		LASTINPUTINFO lastInPut = new LASTINPUTINFO();
		lastInPut.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInPut);
		GetLastInputInfo(ref lastInPut);

		return ((uint)Environment.TickCount - lastInPut.dwTime);
	}

	public static long GetTickCount()
	{
		return Environment.TickCount;
	}

	public static long GetLastInputTime()
	{
		LASTINPUTINFO lastInPut = new LASTINPUTINFO();
		lastInPut.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInPut);
		if (!GetLastInputInfo(ref lastInPut))
		{
			throw new Exception(GetLastError().ToString());
		}

		return lastInPut.dwTime;
	}

	public const int TV_FIRST = 0x1100;
	public const int TVM_SETEXTENDEDSTYLE = TV_FIRST + 44;
	public const int TVM_GETEXTENDEDSTYLE = TV_FIRST + 45;
	public const int TVS_EX_DOUBLEBUFFER = 0x004;
	public const int TVS_EX_AUTOHSCROLL = 0x0020;
	public const int TVS_EX_FADEINOUTEXPANDOS = 0x0040;

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	internal static extern int SendMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

	[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
	public extern static int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);
}