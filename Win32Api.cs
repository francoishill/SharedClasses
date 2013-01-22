using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SharedClasses
{
	public static class Win32Api
	{
		public const int WM_USER = 0x0400;

		public static readonly int WM_NCLBUTTONDOWN = 0xA1;
		public static readonly int HT_CAPTION = 0x2;

		#region Using taskbar notification area
		public const int TB_HIDEBUTTON = WM_USER + 4;

		private const int TB_DELETEBUTTON = WM_USER + 22;
		public const int TB_BUTTONCOUNT = WM_USER + 24;
		public const int TB_GETBUTTONINFO = 0x043F;//WM_USER + 63;

		public const int TBIF_IMAGE = 0x0001;
		public const int TBIF_TEXT = 0x0002;
		public const int TBIF_STATE = 0x0004;
		public const int TBIF_STYLE = 0x0008;
		public const int TBIF_LPARAM = 0x0010;
		public const int TBIF_COMMAND = 0x0020;
		public const int TBIF_SIZE = 0x0040;
		public const uint TBIF_BYINDEX = 0x80000000;



		[StructLayout(LayoutKind.Sequential)]
		public struct TBBUTTONINFO
		{
			public uint cbSize;
			public uint dwMask;
			public int idCommand;
			public int iImage;
			public byte fsState;
			public byte fsStyle;
			public short cx;
			public IntPtr lParam;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string lpszText;
			public int cchText;
		}
		#endregion Using taskbar notification area

		public const int CB_SHOWDROPDOWN = 0x014F;
		[DllImportAttribute("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
		[DllImportAttribute("user32.dll")]
		public static extern bool ReleaseCapture();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public extern static IntPtr GetModuleHandle(string moduleName);
		[DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
		public extern static IntPtr GetProcAddress(IntPtr hModule, string methodName);

		/// <summary>The GetForegroundWindow function returns a handle to the foreground window.</summary>
		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		// For Windows Mobile, replace user32.dll with coredll.dll 
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		public static readonly int WM_HOTKEY = 786;
		[DllImport("user32.dll")]
		public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

		public static readonly uint MOD_ALT = 1;
		public static readonly uint MOD_CONTROL = 2;
		public static readonly uint MOD_SHIFT = 4;
		public static readonly uint MOD_WIN = 8;
		public static readonly int Hotkey1 = 500;
		public static readonly int Hotkey2 = 501;
		public static readonly int MultipleHotkeyStart = 550;

		[DllImport("user32.dll")]
		public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		public const int SW_HIDE = 0;
		public const int SW_SHOWNORMAL = 1;
		public const int SW_SHOWMINIMIZED = 2;
		public const int SW_SHOWMAXIMIZED = 3;
		public const int SW_SHOWNOACTIVATE = 4;
		public static readonly int SW_SHOW = 5;
		public static readonly int SW_MINIMIZE = 6;
		public const int SW_RESTORE = 9;
		public const int SW_SHOWDEFAULT = 10;

		// Activate or minimize a window
		[DllImportAttribute("User32.DLL")]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll")]
		public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

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

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

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

		[DllImport("user32.dll")]
		public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

		public const int KEY_PRESSED = 0x8000;
		[DllImport("user32.dll")]
		public static extern short GetKeyState(VirtualKeyStates nVirtKey);
		public enum VirtualKeyStates : int
		{
			VK_LBUTTON = 0x01,
			VK_RBUTTON = 0x02,
			VK_CANCEL = 0x03,
			VK_MBUTTON = 0x04,
			//
			VK_XBUTTON1 = 0x05,
			VK_XBUTTON2 = 0x06,
			//
			VK_BACK = 0x08,
			VK_TAB = 0x09,
			//
			VK_CLEAR = 0x0C,
			VK_RETURN = 0x0D,
			//
			VK_SHIFT = 0x10,
			VK_CONTROL = 0x11,
			VK_MENU = 0x12,
			VK_PAUSE = 0x13,
			VK_CAPITAL = 0x14,
			//
			VK_KANA = 0x15,
			VK_HANGEUL = 0x15,  /* old name - should be here for compatibility */
			VK_HANGUL = 0x15,
			VK_JUNJA = 0x17,
			VK_FINAL = 0x18,
			VK_HANJA = 0x19,
			VK_KANJI = 0x19,
			//
			VK_ESCAPE = 0x1B,
			//
			VK_CONVERT = 0x1C,
			VK_NONCONVERT = 0x1D,
			VK_ACCEPT = 0x1E,
			VK_MODECHANGE = 0x1F,
			//
			VK_SPACE = 0x20,
			VK_PRIOR = 0x21,
			VK_NEXT = 0x22,
			VK_END = 0x23,
			VK_HOME = 0x24,
			VK_LEFT = 0x25,
			VK_UP = 0x26,
			VK_RIGHT = 0x27,
			VK_DOWN = 0x28,
			VK_SELECT = 0x29,
			VK_PRINT = 0x2A,
			VK_EXECUTE = 0x2B,
			VK_SNAPSHOT = 0x2C,
			VK_INSERT = 0x2D,
			VK_DELETE = 0x2E,
			VK_HELP = 0x2F,
			//
			VK_LWIN = 0x5B,
			VK_RWIN = 0x5C,
			VK_APPS = 0x5D,
			//
			VK_SLEEP = 0x5F,
			//
			VK_NUMPAD0 = 0x60,
			VK_NUMPAD1 = 0x61,
			VK_NUMPAD2 = 0x62,
			VK_NUMPAD3 = 0x63,
			VK_NUMPAD4 = 0x64,
			VK_NUMPAD5 = 0x65,
			VK_NUMPAD6 = 0x66,
			VK_NUMPAD7 = 0x67,
			VK_NUMPAD8 = 0x68,
			VK_NUMPAD9 = 0x69,
			VK_MULTIPLY = 0x6A,
			VK_ADD = 0x6B,
			VK_SEPARATOR = 0x6C,
			VK_SUBTRACT = 0x6D,
			VK_DECIMAL = 0x6E,
			VK_DIVIDE = 0x6F,
			VK_F1 = 0x70,
			VK_F2 = 0x71,
			VK_F3 = 0x72,
			VK_F4 = 0x73,
			VK_F5 = 0x74,
			VK_F6 = 0x75,
			VK_F7 = 0x76,
			VK_F8 = 0x77,
			VK_F9 = 0x78,
			VK_F10 = 0x79,
			VK_F11 = 0x7A,
			VK_F12 = 0x7B,
			VK_F13 = 0x7C,
			VK_F14 = 0x7D,
			VK_F15 = 0x7E,
			VK_F16 = 0x7F,
			VK_F17 = 0x80,
			VK_F18 = 0x81,
			VK_F19 = 0x82,
			VK_F20 = 0x83,
			VK_F21 = 0x84,
			VK_F22 = 0x85,
			VK_F23 = 0x86,
			VK_F24 = 0x87,
			//
			VK_NUMLOCK = 0x90,
			VK_SCROLL = 0x91,
			//
			VK_OEM_NEC_EQUAL = 0x92,   // '=' key on numpad
			//
			VK_OEM_FJ_JISHO = 0x92,   // 'Dictionary' key
			VK_OEM_FJ_MASSHOU = 0x93,   // 'Unregister word' key
			VK_OEM_FJ_TOUROKU = 0x94,   // 'Register word' key
			VK_OEM_FJ_LOYA = 0x95,   // 'Left OYAYUBI' key
			VK_OEM_FJ_ROYA = 0x96,   // 'Right OYAYUBI' key
			//
			VK_LSHIFT = 0xA0,
			VK_RSHIFT = 0xA1,
			VK_LCONTROL = 0xA2,
			VK_RCONTROL = 0xA3,
			VK_LMENU = 0xA4,
			VK_RMENU = 0xA5,
			//
			VK_BROWSER_BACK = 0xA6,
			VK_BROWSER_FORWARD = 0xA7,
			VK_BROWSER_REFRESH = 0xA8,
			VK_BROWSER_STOP = 0xA9,
			VK_BROWSER_SEARCH = 0xAA,
			VK_BROWSER_FAVORITES = 0xAB,
			VK_BROWSER_HOME = 0xAC,
			//
			VK_VOLUME_MUTE = 0xAD,
			VK_VOLUME_DOWN = 0xAE,
			VK_VOLUME_UP = 0xAF,
			VK_MEDIA_NEXT_TRACK = 0xB0,
			VK_MEDIA_PREV_TRACK = 0xB1,
			VK_MEDIA_STOP = 0xB2,
			VK_MEDIA_PLAY_PAUSE = 0xB3,
			VK_LAUNCH_MAIL = 0xB4,
			VK_LAUNCH_MEDIA_SELECT = 0xB5,
			VK_LAUNCH_APP1 = 0xB6,
			VK_LAUNCH_APP2 = 0xB7,
			//
			VK_OEM_1 = 0xBA,   // ';:' for US
			VK_OEM_PLUS = 0xBB,   // '+' any country
			VK_OEM_COMMA = 0xBC,   // ',' any country
			VK_OEM_MINUS = 0xBD,   // '-' any country
			VK_OEM_PERIOD = 0xBE,   // '.' any country
			VK_OEM_2 = 0xBF,   // '/?' for US
			VK_OEM_3 = 0xC0,   // '`~' for US
			//
			VK_OEM_4 = 0xDB,  //  '[{' for US
			VK_OEM_5 = 0xDC,  //  '\|' for US
			VK_OEM_6 = 0xDD,  //  ']}' for US
			VK_OEM_7 = 0xDE,  //  ''"' for US
			VK_OEM_8 = 0xDF,
			//
			VK_OEM_AX = 0xE1,  //  'AX' key on Japanese AX kbd
			VK_OEM_102 = 0xE2,  //  "<>" or "\|" on RT 102-key kbd.
			VK_ICO_HELP = 0xE3,  //  Help key on ICO
			VK_ICO_00 = 0xE4,  //  00 key on ICO
			//
			VK_PROCESSKEY = 0xE5,
			//
			VK_ICO_CLEAR = 0xE6,
			//
			VK_PACKET = 0xE7,
			//
			VK_OEM_RESET = 0xE9,
			VK_OEM_JUMP = 0xEA,
			VK_OEM_PA1 = 0xEB,
			VK_OEM_PA2 = 0xEC,
			VK_OEM_PA3 = 0xED,
			VK_OEM_WSCTRL = 0xEE,
			VK_OEM_CUSEL = 0xEF,
			VK_OEM_ATTN = 0xF0,
			VK_OEM_FINISH = 0xF1,
			VK_OEM_COPY = 0xF2,
			VK_OEM_AUTO = 0xF3,
			VK_OEM_ENLW = 0xF4,
			VK_OEM_BACKTAB = 0xF5,
			//
			VK_ATTN = 0xF6,
			VK_CRSEL = 0xF7,
			VK_EXSEL = 0xF8,
			VK_EREOF = 0xF9,
			VK_PLAY = 0xFA,
			VK_ZOOM = 0xFB,
			VK_NONAME = 0xFC,
			VK_PA1 = 0xFD,
			VK_OEM_CLEAR = 0xFE
		}

		public const byte VK_ALT = 12;
		public const byte VK_MENU = VK_ALT;
		public const byte VK_LCONTROL=	0xA2;

		public const byte VK_V = 0x56;
		public const byte VK_OEM_5 = 0xDC;

		//public const byte VK_RCONTROL=	0xA3;

		//public const byte VK_LSHIFT= 0xA0; // left shift key
		//public const byte VK_TAB = 0x09;
		//public const int KEYEVENTF_EXTENDEDKEY = 0x01;
		public const int KEYEVENTF_KEYUP = 0x02;

		public static readonly uint SWP_NOACTIVATE = 0x0010;

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern int GetWindowTextLength(IntPtr hWnd);
		[DllImport("user32.dll")]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
		[DllImport("msvcr70.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int _fpreset();

		public static string GetWindowText(IntPtr hWnd)
		{
			// Allocate correct string length first
			int length       = GetWindowTextLength(hWnd);
			StringBuilder sb = new StringBuilder(length + 1);
			GetWindowText(hWnd, sb, sb.Capacity);
			return sb.ToString();
		}

		[DllImport("User32.dll")]
		public static extern bool LockWorkStation();

		[DllImport("User32.dll")]
		private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
		internal struct LASTINPUTINFO
		{
			public uint cbSize;
			public uint dwTime;
		}

		public static bool GetLastInputInfo(out DateTime systemStartupTime, out TimeSpan idleTime)
		{
			systemStartupTime = DateTime.MinValue;
			idleTime = TimeSpan.Zero;

			// Get the system uptime
			int ticksSinceSystemStarted = Environment.TickCount;
			// The tick at which the last input was recorded
			int LastInputTicks = 0;
			// The number of ticks that passed since last input
			int IdleTicks = 0;

			// Set the struct
			Win32Api.LASTINPUTINFO LastInputInfo = new Win32Api.LASTINPUTINFO();
			LastInputInfo.cbSize = (uint)Marshal.SizeOf(LastInputInfo);
			LastInputInfo.dwTime = 0;

			// If we have a value from the function
			if (Win32Api.GetLastInputInfo(ref LastInputInfo))
			{
				// Get the number of ticks at the point when the last activity was seen
				LastInputTicks = (int)LastInputInfo.dwTime;
				// Number of idle ticks = system uptime ticks - number of ticks at last input
				IdleTicks = ticksSinceSystemStarted - LastInputTicks;

				systemStartupTime = DateTime.Now.Subtract(TimeSpan.FromMilliseconds(ticksSinceSystemStarted));
				idleTime = TimeSpan.FromMilliseconds(IdleTicks);
				return true;
			}
			return false;
		}

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
		public const int TVS_EX_MULTISELECT =0x0002;
		public const int TVS_EX_NOINDENTSTATE= 0x0008;
		public const int TVS_EX_RICHTOOLTIP =0x0010;
		public const int TVS_EX_PARTIALCHECKBOXES =0x0080;
		public const int TVS_EX_EXCLUSIONCHECKBOXES= 0x0100;
		public const int TVS_EX_DIMMEDCHECKBOXES =0x0200;
		public const int TVS_EX_DRAWIMAGEASYNC =0x0400;

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		internal static extern int SendMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, ref TBBUTTONINFO lParam);

		[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
		public extern static int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

		public enum MouseGestures
		{
			LR = 0,
			LM = 1,
			LRM = 2,
			LMR = 3,
			RL = 4,
			RM = 5,
			RML = 6,
			ML = 7,
			MR = 8,
		}

		/// <summary>
		/// Defines the various types of hooks that are available in Windows
		/// </summary>
		public enum HookTypes : int
		{
			WH_JOURNALRECORD = 0,
			WH_JOURNALPLAYBACK = 1,
			WH_KEYBOARD = 2,
			WH_GETMESSAGE = 3,
			WH_CALLWNDPROC = 4,
			WH_CBT = 5,
			WH_SYSMSGFILTER = 6,
			WH_MOUSE = 7,
			WH_HARDWARE = 8,
			WH_DEBUG = 9,
			WH_SHELL = 10,
			WH_FOREGROUNDIDLE = 11,
			WH_CALLWNDPROCRET = 12,
			WH_KEYBOARD_LL = 13,
			WH_MOUSE_LL = 14
		}

		public delegate int HookProc(int nCode, Int32 wParam, IntPtr lParam);
		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		public static extern int SetWindowsHookEx
		(
				int idHook,
				HookProc lpfn,
				IntPtr hInstance,
				int threadId
		);

		public const int WM_SETCURSOR = 0x20;
		public const int WM_TIMER = 0x113;

		public const int WM_MOUSEMOVE = 0x200;
		public const int WM_MBUTTONDOWN = 0x0207;
		public const int WM_MBUTTONUP = 0x0208;
		public const int WM_MBUTTONDBLCLK = 0x0209;

		public const int WM_LBUTTONDOWN = 0x0201;
		public const int WM_LBUTTONUP = 0x0202;
		public const int WM_LBUTTONDBLCLK = 0x0203;

		public const int WM_RBUTTONDOWN = 0x0204;
		public const int WM_RBUTTONUP = 0x0205;
		public const int WM_RBUTTONDBLCLK = 0x0206;

		public const int WM_ACTIVATE = 0x006;
		public const int WM_ACTIVATEAPP = 0x01C;
		public const int WM_NCACTIVATE = 0x086;
		public const int WM_CLOSE = 0x010;

		public const int WM_PAINT = 0x000F;
		public const int WM_ERASEBKGND = 0x0014;

		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		public static extern bool UnhookWindowsHookEx(int idHook);

		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		public static extern int CallNextHookEx
		(
				int idHook,
				int nCode,
				Int32 wParam,
				IntPtr lParam
		);

		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		public static extern void mouse_event(long dwFlags, long dx, long dy, long cButtons, long dwExtraInfo);

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X;
			public int Y;
			public POINT(int X, int Y)
			{
				this.X = X;
				this.Y = Y;
			}
		}

		/// <summary>
		/// Animates the window from left to right. This flag can be used with roll or slide animation.
		/// </summary>
		public const int AW_HOR_POSITIVE = 0X1;
		/// <summary>
		/// Animates the window from right to left. This flag can be used with roll or slide animation.
		/// </summary>
		public const int AW_HOR_NEGATIVE = 0X2;
		/// <summary>
		/// Animates the window from top to bottom. This flag can be used with roll or slide animation.
		/// </summary>
		public const int AW_VER_POSITIVE = 0X4;
		/// <summary>
		/// Animates the window from bottom to top. This flag can be used with roll or slide animation.
		/// </summary>
		public const int AW_VER_NEGATIVE = 0X8;
		/// <summary>
		/// Makes the window appear to collapse inward if AW_HIDE is used or expand outward if the AW_HIDE is not used.
		/// </summary>
		public const int AW_CENTER = 0X10;
		/// <summary>
		/// Hides the window. By default, the window is shown.
		/// </summary>
		public const int AW_HIDE = 0X10000;
		/// <summary>
		/// Activates the window.
		/// </summary>
		public const int AW_ACTIVATE = 0X20000;
		/// <summary>
		/// Uses slide animation. By default, roll animation is used.
		/// </summary>
		public const int AW_SLIDE = 0X40000;
		/// <summary>
		/// Uses a fade effect. This flag can be used only if hwnd is a top-level window.
		/// </summary>
		public const int AW_BLEND = 0X80000;

		/// <summary>
		/// Animates a window.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int AnimateWindow(IntPtr hwand, int dwTime, int dwFlags);

		public const int GWL_EXSTYLE = -20;
		public const int WS_EX_LAYERED = 0x80000;
		public const int WS_EX_TRANSPARENT = 0x20;
		public const int WS_EX_NOACTIVATE = 0x08000000;
		[DllImport("user32.dll")]
		public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
		[DllImport("user32.dll")]
		public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		//public static extern bool GetCursorPos(ref Win32Point pt);
		public static extern bool GetCursorPos(out POINT lpPoint);

		[DllImport("user32.dll")]
		public static extern IntPtr WindowFromPoint(POINT Point);

		[DllImport("user32.dll")]
		public static extern IntPtr WindowFromPoint(int xPoint, int yPoint);

		[DllImport("user32.dll")]
		public static extern bool SetWindowText(IntPtr hWnd, string lpString);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

		[StructLayout(LayoutKind.Sequential)]
		public struct FLASHWINFO
		{
			public UInt32 cbSize;
			public IntPtr hwnd;
			public UInt32 dwFlags;
			public UInt32 uCount;//Number of times to flash
			public UInt32 dwTimeout;//Interval/speed of flashing (in milliseconds)
		}

		public enum FLASHWINFOFLAGS
		{
			FLASHW_STOP = 0,
			FLASHW_CAPTION = 0x00000001,
			FLASHW_TRAY = 0x00000002,
			FLASHW_ALL = (FLASHW_CAPTION | FLASHW_TRAY),
			FLASHW_TIMER = 0x00000004,
			FLASHW_TIMERNOFG = 0x0000000C
		}
		public delegate bool CallBackPtr(int hwnd, int lParam);
		[DllImport("user32.dll")]
		public static extern int EnumWindows(CallBackPtr callPtr, int lPar);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);


		/* Short version
		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left, top, right, bottom;
		}*/

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			private int _Left;
			private int _Top;
			private int _Right;
			private int _Bottom;

			public RECT(RECT Rectangle)
				: this(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom)
			{
			}
			public RECT(int Left, int Top, int Right, int Bottom)
			{
				_Left = Left;
				_Top = Top;
				_Right = Right;
				_Bottom = Bottom;
			}

			public int X
			{
				get { return _Left; }
				set { _Left = value; }
			}
			public int Y
			{
				get { return _Top; }
				set { _Top = value; }
			}
			public int Left
			{
				get { return _Left; }
				set { _Left = value; }
			}
			public int Top
			{
				get { return _Top; }
				set { _Top = value; }
			}
			public int Right
			{
				get { return _Right; }
				set { _Right = value; }
			}
			public int Bottom
			{
				get { return _Bottom; }
				set { _Bottom = value; }
			}
			public int Height
			{
				get { return _Bottom - _Top; }
				set { _Bottom = value + _Top; }
			}
			public int Width
			{
				get { return _Right - _Left; }
				set { _Right = value + _Left; }
			}
			/*public Point Location
			{
				get { return new Point(Left, Top); }
				set
				{
					_Left = value.X;
					_Top = value.Y;
				}
			}
			public Size Size
			{
				get { return new Size(Width, Height); }
				set
				{
					_Right = value.Width + _Left;
					_Bottom = value.Height + _Top;
				}
			}

			public static implicit operator Rectangle(RECT Rectangle)
			{
				return new Rectangle(Rectangle.Left, Rectangle.Top, Rectangle.Width, Rectangle.Height);
			}
			public static implicit operator RECT(Rectangle Rectangle)
			{
				return new RECT(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom);
			}*/
			public static bool operator ==(RECT Rectangle1, RECT Rectangle2)
			{
				return Rectangle1.Equals(Rectangle2);
			}
			public static bool operator !=(RECT Rectangle1, RECT Rectangle2)
			{
				return !Rectangle1.Equals(Rectangle2);
			}

			public override string ToString()
			{
				return "{Left: " + _Left + "; " + "Top: " + _Top + "; Right: " + _Right + "; Bottom: " + _Bottom + "}";
			}

			public override int GetHashCode()
			{
				return ToString().GetHashCode();
			}

			public bool Equals(RECT Rectangle)
			{
				return Rectangle.Left == _Left && Rectangle.Top == _Top && Rectangle.Right == _Right && Rectangle.Bottom == _Bottom;
			}

			public override bool Equals(object Object)
			{
				if (Object is RECT)
				{
					return Equals((RECT)Object);
				}
				/*else if (Object is Rectangle)
				{
					return Equals(new RECT((Rectangle)Object));
				}*/

				return false;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct WINDOWINFO
		{
			public uint cbSize;
			public RECT rcWindow;
			public RECT rcClient;
			public uint dwStyle;
			public uint dwExStyle;
			public uint dwWindowStatus;
			public uint cxWindowBorders;
			public uint cyWindowBorders;
			public ushort atomWindowType;
			public ushort wCreatorVersion;

			public WINDOWINFO(Boolean? filler)
				: this()   // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
			{
				cbSize = (UInt32)(Marshal.SizeOf(typeof(WINDOWINFO)));
			}

		}

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

		[DllImport("user32.dll")]
		public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

		public delegate int RecoveryDelegate(object parameterData);
		[DllImport("kernel32.dll")]
		public static extern int RegisterApplicationRecoveryCallback(
			RecoveryDelegate recoveryCallback,
			string parameterData,
			uint pingInterval,
			uint flags);

		[DllImport("kernel32.dll")]
		public static extern void ApplicationRecoveryFinished(bool success);

		[DllImport("kernel32.dll")]
		public static extern int ApplicationRecoveryInProgress(out bool cancelled);

		[Flags]
		public enum RestartRestrictions
		{
			None = 0,
			NotOnCrash = 1,
			NotOnHang = 2,
			NotOnPatch = 4,
			NotOnReboot = 8
		}
		[DllImport("kernel32.dll")]
		public static extern int RegisterApplicationRestart([MarshalAs(UnmanagedType.BStr)] string commandLineArgs, int flags);

		[DllImport("kernel32.dll")]
		public static extern int UnregisterApplicationRecoveryCallback();

		[DllImport("kernel32.dll")]
		public static extern int UnregisterApplicationRestart();

		/// <summary>
		/// Imported unmanaged function. The SendInput function synthesizes
		/// keystrokes, mouse motions, and button clicks.
		/// </summary>
		/// <param name="nInputs">Specifies the number of structures in the pInputs array</param>
		/// <param name="pInputs">Pointer to an array of INPUTMOUSE structures.</param>
		/// <param name="cbSize">Specifies the size, in bytes, of an INPUT structure</param>
		/// <returns>The function returns the number of events that it successfully
		/// inserted into the keyboard or mouse input stream. If the function
		/// returns zero, the input was already blocked by another thread.
		///</returns>
		///<remarks>
		///Type of pInputs parameter - the INPUT structure is defined
		///as union (MOUSEINPUT, KEYBDINPUT, HARDWAREINPUT) in the Platform SDK.
		/// We use overloaded methods with pInputs types MOUSEINPUT, KEYBDINPUT instead
		///</remarks>
		[DllImport("user32.dll")]
		public static extern uint SendInput(uint nInputs, ref INPUTMOUSE pInputs, int cbSize);

		/// <summary>
		/// Structure used to store information for synthesizing mouse input events
		/// </summary>
		[StructLayout(LayoutKind.Explicit, Size = 28)]
		public struct INPUTMOUSE
		{
			/// <summary>
			/// Type of INPUT structure
			/// </summary>
			/// <remarks>
			/// must be INPUT_MOUSE for INPUTMOUSE structure
			/// </remarks>
			[FieldOffset(0)]
			public INPUTTYPE type;

			/// <summary>
			/// The structure that contains information about a simulated mouse event
			/// </summary>
			[FieldOffset(4)]
			public tagMOUSEINPUT mi;
		}

		/// <summary>
		/// Defines type of INPUT structure
		/// </summary>
		public enum INPUTTYPE : uint
		{
			INPUT_MOUSE = 0,
			INPUT_KEYBOARD = 1,
			INPUT_HARDWARE = 2,
		}

		/// <summary>
		/// The structure contains information about a simulated mouse event
		/// </summary>
		public struct tagMOUSEINPUT
		{
			/// <summary>
			/// Specifies the absolute position of the mouse,
			/// or the amount of motion since the last mouse event was generated.
			/// </summary>
			public int dx;

			/// <summary>
			/// Specifies the absolute position of the mouse,
			/// or the amount of motion since the last mouse event was generated.
			/// </summary>
			public int dy;

			/// <summary>
			/// If dwFlags contains MOUSEEVENTF_WHEEL,
			/// then mouseData specifies the amount of wheel movement.
			/// </summary>
			public uint mouseData;

			/// <summary>
			/// A set of bit flags that specify various aspects of mouse motion and button clicks.
			/// The bits in this member can be any reasonable combination of the following values. 
			/// </summary>
			public MOUSEEVENTFLAGS dwFlags;

			/// <summary>
			/// Time stamp for the event, in milliseconds.
			/// If this parameter is 0, the system will provide its own time stamp. 
			/// </summary>
			public uint time;

			/// <summary>
			/// Specifies an additional value associated with the mouse event.
			/// </summary>
			public uint dwExtraInfo;
		}

		/// <summary>
		/// Enum defines MOUSEEVENTFLAGS bitfield
		/// </summary>
		[System.Flags]
		public enum MOUSEEVENTFLAGS : uint
		{
			MOUSEEVENTF_MOVE = 0x0001,        /* mouse move */
			MOUSEEVENTF_LEFTDOWN = 0x0002,    /* left button down */
			MOUSEEVENTF_LEFTUP = 0x0004,      /* left button up */
			MOUSEEVENTF_RIGHTDOWN = 0x0008,   /* right button down */
			MOUSEEVENTF_RIGHTUP = 0x0010,     /* right button up */
			MOUSEEVENTF_MIDDLEDOWN = 0x0020,  /* middle button down */
			MOUSEEVENTF_MIDDLEUP = 0x0040,    /* middle button up */
			MOUSEEVENTF_XDOWN = 0x0080,       /* x button down */
			MOUSEEVENTF_XUP = 0x0100,         /* x button down */
			MOUSEEVENTF_WHEEL = 0x0800,       /* wheel button rolled */
			MOUSEEVENTF_VIRTUALDESK = 0x4000, /* map to entire virtual desktop */
			MOUSEEVENTF_ABSOLUTE = 0x8000     /* absolute move */
		}

		/// <summary>
		/// Use with care, blocks all keyboard/mouse input. Use Ctrl+Alt+Delete to disable.
		/// </summary>
		/// <param name="fBlockIt">True to block input, false to unblock. Both must be called from same thread.</param>
		/// <returns>Returns if it suceeded.</returns>
		[DllImport("user32.dll")]
		public static extern bool BlockInput(bool fBlockIt);

		[DllImport("User32.dll")]
		public static extern IntPtr GetDC(IntPtr hwnd);

		[DllImport("User32.dll")]
		public static extern void ReleaseDC(IntPtr dc);

		[DllImport("user32.dll")]
		public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr BeginUpdateResource(string pFileName,
		   [MarshalAs(UnmanagedType.Bool)]bool bDeleteExistingResources);
	}
}