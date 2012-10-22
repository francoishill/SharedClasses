using System;
using System.Runtime.InteropServices;
using System.Text;

public static class Win32Api
{
	public static readonly int WM_NCLBUTTONDOWN = 0xA1;
	public static readonly int HT_CAPTION = 0x2;

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

	[DllImport("user32.dll")]
	public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

	public static readonly int SW_SHOWNOACTIVATE = 4;
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
	public const int WS_EX_NOACTIVATE = 0x08000000;
	[DllImport("user32.dll")]
	public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
	[DllImport("user32.dll")]
	public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	//public static extern bool GetCursorPos(ref Win32Point pt);
	public static extern bool GetCursorPos(out Win32Point lpPoint);

	[StructLayout(LayoutKind.Sequential)]
	public struct Win32Point
	{
		public Int32 X;
		public Int32 Y;
	};

	[DllImport("user32.dll")]
	public static extern IntPtr WindowFromPoint(Win32Point Point);

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
}