using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.ComponentModel;
using System.Reflection;
//using System.Drawing;
using System.Threading;
using System.Drawing;

namespace SharedClasses
{
	/// <summary>
	/// This class allows you to tap keyboard and mouse and / or to detect their activity even when an 
	/// application runes in background or does not have any user interface at all. This class raises 
	/// common .NET events with KeyEventArgs and MouseEventArgs so you can easily retrive any information you need.
	/// MouseGestures code obtained from http://mousegestures.codeplex.com
	/// </summary>
	public class UserActivityHook
	{
		/// <summary>
		/// Represents the method that will handle MouseGesture events.
		/// </summary>
		/// <param name="sender">The source of event.</param>
		/// <param name="start">A MouseGestureEventArgs that contains event data.</param>
		public delegate void GestureHandler(object sender, MouseGestureEventArgs e);
		public event GestureHandler OnGesture;

		private MouseGesture gesture;
		private Point lastGesturePoint;
		private double distance;

		public enum MouseGestureDirection
		{
			Unknown,
			Up,
			Right,
			Down,
			Left
		}

		/// <summary>
		/// A MouseGesture is a sequence of movements
		/// </summary>
		public class MouseGesture
		{
			#region Constants
			//consider moving the hardcoded value to a property
			public const int minGestureSize = 30;
			/// <summary>
			/// Minimal length of MouseMoveSegment
			/// </summary>
			//consider moving the hardcoded value to a property
			public const uint mouseMoveSegmentLength = 8;
			/// <summary>
			/// Defines maximum angle error in degrees. If the angle error is greater then
			/// maxAngleError then the direction is not recognized.
			/// </summary>
			/// <remarks>
			/// It must be positive number lesser then 45
			/// </remarks>
			//consider moving the hardcoded value to a propery
			private const double maxAngleError = 30;
			#endregion Constants

			Point start;
			List<MouseGestureDirection> directions;

			/// <summary>
			/// Create a empty mouse gesture
			/// </summary>
			/// <param name="start">The point where the gesture started</param>
			public MouseGesture(Point point)
			{
				start = point;
				directions = new List<MouseGestureDirection>();
			}

			/// <summary>
			/// The point where the gesture started
			/// </summary>
			public Point Start
			{
				get
				{
					return start;
				}
			}

			/// <summary>
			/// Append a motion to the gesture
			/// </summary>
			/// <remarks>
			/// Duplicate and unknown motions will be filtered out.
			/// </remarks>
			public void AddSegment(MouseGestureDirection direction)
			{
				if (direction != MouseGestureDirection.Unknown &&
				  (directions.Count == 0 ||
					direction != directions[directions.Count - 1]))
					directions.Add(direction);
			}

			/// <summary>
			/// The number of motions in the gesture
			/// </summary>
			public int Count
			{
				get
				{
					return directions.Count;
				}
			}

			/// <summary>
			/// Gets the performed gesture
			/// </summary>
			public string Motions
			{
				get
				{
					return this.ToString();
				}
			}

			#region Helper Functions
			/// <summary>
			/// Calculates distance between 2 points
			/// </summary>
			/// <param name="p1">First point</param>
			/// <param name="p2">Second point</param>
			/// <returns>Distance between two points</returns>
			public static double GetDistance(Point p1, Point p2)
			{
				int dx = p1.X - p2.X;
				int dy = p1.Y - p2.Y;

				return Math.Sqrt(dx * dx + dy * dy);
			}

			/// <summary>
			/// Recognizes direction between two points
			/// </summary>
			/// <param name="deltaX">Lenght of movement in the horizontal direction</param>
			/// <param name="deltaY">Lenght of movement in the vertical direction</param>
			/// <returns>Gesture direction, if fails returns MouseGestureDirection.Unknown</returns>
			public static MouseGestureDirection GetDirection(Point start, Point end)
			{
				int deltaX = end.X - start.X;
				int deltaY = end.Y - start.Y;

				double length = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

				double sin = deltaX / length;
				double cos = deltaY / length;

				double angle = Math.Asin(Math.Abs(sin)) * 180 / Math.PI;

				if ((sin >= 0) && (cos < 0))
					angle = 180 - angle;
				else if ((sin < 0) && (cos < 0))
					angle = angle + 180;
				else if ((sin < 0) && (cos >= 0))
					angle = 360 - angle;

				//direction recognition
				if ((angle > 360 - maxAngleError) || (angle < 0 + maxAngleError))
					return MouseGestureDirection.Down;
				else if ((angle > 90 - maxAngleError) && (angle < 90 + maxAngleError))
					return MouseGestureDirection.Right;
				else if ((angle > 180 - maxAngleError) && (angle < 180 + maxAngleError))
					return MouseGestureDirection.Up;
				else if ((angle > 270 - maxAngleError) && (angle < 270 + maxAngleError))
					return MouseGestureDirection.Left;
				else return MouseGestureDirection.Unknown;
			}
			#endregion

			/// <summary>
			/// A string representation of the gesture
			/// </summary>
			public override string ToString()
			{
				string s = string.Empty;
				foreach (MouseGestureDirection d in directions)
				{
					switch (d)
					{
						case MouseGestureDirection.Left:
							s += 'L'; break;
						case MouseGestureDirection.Right:
							s += 'R'; break;
						case MouseGestureDirection.Up:
							s += 'U'; break;
						case MouseGestureDirection.Down:
							s += 'D'; break;
						case MouseGestureDirection.Unknown:
							break;
					}
				}
				return s;
			}
		}

		/// <summary>
		/// Provides data for MouseGesture events
		/// </summary>
		public class MouseGestureEventArgs : EventArgs
		{
			private MouseGesture gesture;

			/// <summary>
			/// Initializes new instance of MouseGestureEventArgs
			/// </summary>
			/// <param name="gesture">The gesture performed.</param>
			public MouseGestureEventArgs(MouseGesture mouseGesture)
				: base()
			{
				gesture = mouseGesture;
			}

			/// <summary>
			/// The gesture performed.
			/// </summary>
			public MouseGesture Gesture
			{
				get
				{
					return gesture;
				}
			}
		}

		public class MoreMouseButton
		{
			public enum MoreButtonStates { Up, Down, DoubleClicked, None };
			public MouseButtons Button;
			public MoreButtonStates ButtonState;
			public MoreMouseButton(MouseButtons Button, MoreButtonStates ButtonState)
			{
				this.Button = Button;
				this.ButtonState = ButtonState;
			}
		}
		public class MoreMouseEventArgs : EventArgs
		{
			public MoreMouseButton Button { get; private set; }
			public int Clicks { get; private set; }
			public int Delta { get; private set; }
			public Point Location { get { return new Point(X, Y); } }
			public int X { get; private set; }
			public int Y { get; private set; }
			public MoreMouseEventArgs(MoreMouseButton button, int clicks, int x, int y, int delta)
			{
				this.Button = button;
				this.Clicks = clicks;
				this.Delta = delta;
				this.X = x;
				this.Y = y;
			}
		}
		public delegate void MoreMouseEventHandler(object sender, MoreMouseEventArgs e);
		public delegate bool MoreMouseEventHandlerWithHandledState(object sender, MoreMouseEventArgs e);

		#region Windows structure definitions

		/// <summary>
		/// The MOUSEHOOKSTRUCT structure contains information about a mouse event passed to a WH_MOUSE hook procedure, MouseProc. 
		/// </summary>
		/// <remarks>
		/// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookstructures/cwpstruct.asp
		/// </remarks>
		[StructLayout(LayoutKind.Sequential)]
		private class MouseHookStruct
		{
			/// <summary>
			/// Specifies a POINT structure that contains the x- and y-coordinates of the cursor, in screen coordinates. 
			/// </summary>
			public Point pt;
			/// <summary>
			/// Handle to the window that will receive the mouse message corresponding to the mouse event. 
			/// </summary>
			public int hwnd;
			/// <summary>
			/// Specifies the hit-test value. For a list of hit-test values, see the description of the WM_NCHITTEST message. 
			/// </summary>
			public int wHitTestCode;
			/// <summary>
			/// Specifies extra information associated with the message. 
			/// </summary>
			public int dwExtraInfo;
		}

		/// <summary>
		/// The MSLLHOOKSTRUCT structure contains information about a low-level keyboard input event. 
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private class MouseLLHookStruct
		{
			/// <summary>
			/// Specifies a POINT structure that contains the x- and y-coordinates of the cursor, in screen coordinates. 
			/// </summary>
			public Point pt;
			/// <summary>
			/// If the message is WM_MOUSEWHEEL, the high-order word of this member is the wheel delta. 
			/// The low-order word is reserved. A positive value indicates that the wheel was rotated forward, 
			/// away from the user; a negative value indicates that the wheel was rotated backward, toward the user. 
			/// One wheel click is defined as WHEEL_DELTA, which is 120. 
			///If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP,
			/// or WM_NCXBUTTONDBLCLK, the high-order word specifies which X button was pressed or released, 
			/// and the low-order word is reserved. This value can be one or more of the following values. Otherwise, mouseData is not used. 
			///XBUTTON1
			///The first X button was pressed or released.
			///XBUTTON2
			///The second X button was pressed or released.
			/// </summary>
			public int mouseData;
			/// <summary>
			/// Specifies the event-injected flag. An application can use the following value to test the mouse flags. Value Purpose 
			///LLMHF_INJECTED Test the event-injected flag.  
			///0
			///Specifies whether the event was injected. The value is 1 if the event was injected; otherwise, it is 0.
			///1-15
			///Reserved.
			/// </summary>
			public int flags;
			/// <summary>
			/// Specifies the time stamp for this message.
			/// </summary>
			public int time;
			/// <summary>
			/// Specifies extra information associated with the message. 
			/// </summary>
			public int dwExtraInfo;
		}

		/// <summary>
		/// The KBDLLHOOKSTRUCT structure contains information about a low-level keyboard input event. 
		/// </summary>
		/// <remarks>
		/// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookstructures/cwpstruct.asp
		/// </remarks>
		[StructLayout(LayoutKind.Sequential)]
		private class KeyboardHookStruct
		{
			/// <summary>
			/// Specifies a virtual-key code. The code must be a value in the range 1 to 254. 
			/// </summary>
			public int vkCode;
			/// <summary>
			/// Specifies a hardware scan code for the key. 
			/// </summary>
			public int scanCode;
			/// <summary>
			/// Specifies the extended-key flag, event-injected flag, context code, and transition-state flag.
			/// </summary>
			public int flags;
			/// <summary>
			/// Specifies the time stamp for this message.
			/// </summary>
			public int time;
			/// <summary>
			/// Specifies extra information associated with the message. 
			/// </summary>
			public int dwExtraInfo;
		}
		#endregion

		#region Windows function imports
		/// <summary>
		/// The SetWindowsHookEx function installs an application-defined hook procedure into a hook chain. 
		/// You would install a hook procedure to monitor the system for certain types of events. These events 
		/// are associated either with a specific thread or with all threads in the same desktop as the calling thread. 
		/// </summary>
		/// <param name="idHook">
		/// [in] Specifies the type of hook procedure to be installed. This parameter can be one of the following values.
		/// </param>
		/// <param name="lpfn">
		/// [in] Pointer to the hook procedure. If the dwThreadId parameter is zero or specifies the identifier of a 
		/// thread created by a different process, the lpfn parameter must point to a hook procedure in a dynamic-link 
		/// library (DLL). Otherwise, lpfn can point to a hook procedure in the code associated with the current process.
		/// </param>
		/// <param name="hMod">
		/// [in] Handle to the DLL containing the hook procedure pointed to by the lpfn parameter. 
		/// The hMod parameter must be set to NULL if the dwThreadId parameter specifies a thread created by 
		/// the current process and if the hook procedure is within the code associated with the current process. 
		/// </param>
		/// <param name="dwThreadId">
		/// [in] Specifies the identifier of the thread with which the hook procedure is to be associated. 
		/// If this parameter is zero, the hook procedure is associated with all existing threads running in the 
		/// same desktop as the calling thread. 
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is the handle to the hook procedure.
		/// If the function fails, the return value is NULL. To get extended error information, call GetLastError.
		/// </returns>
		/// <remarks>
		/// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookfunctions/setwindowshookex.asp
		/// </remarks>
		[DllImport("user32.dll", CharSet = CharSet.Auto,
			 CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		private static extern int SetWindowsHookEx(
				int idHook,
				HookProc lpfn,
				IntPtr hMod,
				int dwThreadId);

		/// <summary>
		/// The UnhookWindowsHookEx function removes a hook procedure installed in a hook chain by the SetWindowsHookEx function. 
		/// </summary>
		/// <param name="idHook">
		/// [in] Handle to the hook to be removed. This parameter is a hook handle obtained by a previous call to SetWindowsHookEx. 
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is nonzero.
		/// If the function fails, the return value is zero. To get extended error information, call GetLastError.
		/// </returns>
		/// <remarks>
		/// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookfunctions/setwindowshookex.asp
		/// </remarks>
		[DllImport("user32.dll", CharSet = CharSet.Auto,
				CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		private static extern int UnhookWindowsHookEx(int idHook);

		/// <summary>
		/// The CallNextHookEx function passes the hook information to the next hook procedure in the current hook chain. 
		/// A hook procedure can call this function either before or after processing the hook information. 
		/// </summary>
		/// <param name="idHook">Ignored.</param>
		/// <param name="nCode">
		/// [in] Specifies the hook code passed to the current hook procedure. 
		/// The next hook procedure uses this code to determine how to process the hook information.
		/// </param>
		/// <param name="wParam">
		/// [in] Specifies the wParam value passed to the current hook procedure. 
		/// The meaning of this parameter depends on the type of hook associated with the current hook chain. 
		/// </param>
		/// <param name="lParam">
		/// [in] Specifies the lParam value passed to the current hook procedure. 
		/// The meaning of this parameter depends on the type of hook associated with the current hook chain. 
		/// </param>
		/// <returns>
		/// This value is returned by the next hook procedure in the chain. 
		/// The current hook procedure must also return this value. The meaning of the return value depends on the hook type. 
		/// For more information, see the descriptions of the individual hook procedures.
		/// </returns>
		/// <remarks>
		/// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookfunctions/setwindowshookex.asp
		/// </remarks>
		[DllImport("user32.dll", CharSet = CharSet.Auto,
				 CallingConvention = CallingConvention.StdCall)]
		private static extern int CallNextHookEx(
				int idHook,
				int nCode,
				int wParam,
				IntPtr lParam);

		/// <summary>
		/// The CallWndProc hook procedure is an application-defined or library-defined callback 
		/// function used with the SetWindowsHookEx function. The HOOKPROC type defines a pointer 
		/// to this callback function. CallWndProc is a placeholder for the application-defined 
		/// or library-defined function name.
		/// </summary>
		/// <param name="nCode">
		/// [in] Specifies whether the hook procedure must process the message. 
		/// If nCode is HC_ACTION, the hook procedure must process the message. 
		/// If nCode is less than zero, the hook procedure must pass the message to the 
		/// CallNextHookEx function without further processing and must return the 
		/// value returned by CallNextHookEx.
		/// </param>
		/// <param name="wParam">
		/// [in] Specifies whether the message was sent by the current thread. 
		/// If the message was sent by the current thread, it is nonzero; otherwise, it is zero. 
		/// </param>
		/// <param name="lParam">
		/// [in] Pointer to a CWPSTRUCT structure that contains details about the message. 
		/// </param>
		/// <returns>
		/// If nCode is less than zero, the hook procedure must return the value returned by CallNextHookEx. 
		/// If nCode is greater than or equal to zero, it is highly recommended that you call CallNextHookEx 
		/// and return the value it returns; otherwise, other applications that have installed WH_CALLWNDPROC 
		/// hooks will not receive hook notifications and may behave incorrectly as a result. If the hook 
		/// procedure does not call CallNextHookEx, the return value should be zero. 
		/// </returns>
		/// <remarks>
		/// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookfunctions/callwndproc.asp
		/// </remarks>
		private delegate int HookProc(int nCode, int wParam, IntPtr lParam);

		/// <summary>
		/// The ToAscii function translates the specified virtual-key code and keyboard 
		/// state to the corresponding character or characters. The function translates the code 
		/// using the input language and physical keyboard layout identified by the keyboard layout handle.
		/// </summary>
		/// <param name="uVirtKey">
		/// [in] Specifies the virtual-key code to be translated. 
		/// </param>
		/// <param name="uScanCode">
		/// [in] Specifies the hardware scan code of the key to be translated. 
		/// The high-order bit of this value is set if the key is up (not pressed). 
		/// </param>
		/// <param name="lpbKeyState">
		/// [in] Pointer to a 256-byte array that contains the current keyboard state. 
		/// Each element (byte) in the array contains the state of one key. 
		/// If the high-order bit of a byte is set, the key is down (pressed). 
		/// The low bit, if set, indicates that the key is toggled on. In this function, 
		/// only the toggle bit of the CAPS LOCK key is relevant. The toggle state 
		/// of the NUM LOCK and SCROLL LOCK keys is ignored.
		/// </param>
		/// <param name="lpwTransKey">
		/// [out] Pointer to the buffer that receives the translated character or characters. 
		/// </param>
		/// <param name="fuState">
		/// [in] Specifies whether a menu is active. This parameter must be 1 if a menu is active, or 0 otherwise. 
		/// </param>
		/// <returns>
		/// If the specified key is a dead key, the return value is negative. Otherwise, it is one of the following values. 
		/// Value Meaning 
		/// 0 The specified virtual key has no translation for the current state of the keyboard. 
		/// 1 One character was copied to the buffer. 
		/// 2 Two characters were copied to the buffer. This usually happens when a dead-key character 
		/// (accent or diacritic) stored in the keyboard layout cannot be composed with the specified 
		/// virtual key to form a single character. 
		/// </returns>
		/// <remarks>
		/// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/userinput/keyboardinput/keyboardinputreference/keyboardinputfunctions/toascii.asp
		/// </remarks>
		[DllImport("user32")]
		private static extern int ToAscii(
				int uVirtKey,
				int uScanCode,
				byte[] lpbKeyState,
				byte[] lpwTransKey,
				int fuState);

		/// <summary>
		/// The GetKeyboardState function copies the status of the 256 virtual keys to the 
		/// specified buffer. 
		/// </summary>
		/// <param name="pbKeyState">
		/// [in] Pointer to a 256-byte array that contains keyboard key states. 
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is nonzero.
		/// If the function fails, the return value is zero. To get extended error information, call GetLastError. 
		/// </returns>
		/// <remarks>
		/// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/userinput/keyboardinput/keyboardinputreference/keyboardinputfunctions/toascii.asp
		/// </remarks>
		[DllImport("user32")]
		private static extern int GetKeyboardState(byte[] pbKeyState);

		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		private static extern short GetKeyState(int vKey);

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern int GetDoubleClickTime();//In milliseconds

		#endregion

		#region Windows constants

		//values from Winuser.h in Microsoft SDK.
		/// <summary>
		/// Windows NT/2000/XP: Installs a hook procedure that monitors low-level mouse input events.
		/// </summary>
		private const int WH_MOUSE_LL       = 14;
		/// <summary>
		/// Windows NT/2000/XP: Installs a hook procedure that monitors low-level keyboard  input events.
		/// </summary>
		private const int WH_KEYBOARD_LL    = 13;

		/// <summary>
		/// Installs a hook procedure that monitors mouse messages. For more information, see the MouseProc hook procedure. 
		/// </summary>
		private const int WH_MOUSE          = 7;
		/// <summary>
		/// Installs a hook procedure that monitors keystroke messages. For more information, see the KeyboardProc hook procedure. 
		/// </summary>
		private const int WH_KEYBOARD       = 2;

		/// <summary>
		/// The WM_MOUSEMOVE message is posted to a window when the cursor moves. 
		/// </summary>
		private const int WM_MOUSEMOVE      = 0x200;
		/// <summary>
		/// The WM_LBUTTONDOWN message is posted when the user presses the left mouse button 
		/// </summary>
		private const int WM_LBUTTONDOWN    = 0x201;
		/// <summary>
		/// The WM_RBUTTONDOWN message is posted when the user presses the right mouse button
		/// </summary>
		private const int WM_RBUTTONDOWN    = 0x204;
		/// <summary>
		/// The WM_MBUTTONDOWN message is posted when the user presses the middle mouse button 
		/// </summary>
		private const int WM_MBUTTONDOWN    = 0x207;
		/// <summary>
		/// The WM_LBUTTONUP message is posted when the user releases the left mouse button 
		/// </summary>
		private const int WM_LBUTTONUP      = 0x202;
		/// <summary>
		/// The WM_RBUTTONUP message is posted when the user releases the right mouse button 
		/// </summary>
		private const int WM_RBUTTONUP      = 0x205;
		/// <summary>
		/// The WM_MBUTTONUP message is posted when the user releases the middle mouse button 
		/// </summary>
		private const int WM_MBUTTONUP      = 0x208;
		/// <summary>
		/// The WM_LBUTTONDBLCLK message is posted when the user double-clicks the left mouse button 
		/// </summary>
		private const int WM_LBUTTONDBLCLK  = 0x203;
		/// <summary>
		/// The WM_RBUTTONDBLCLK message is posted when the user double-clicks the right mouse button 
		/// </summary>
		private const int WM_RBUTTONDBLCLK  = 0x206;
		/// <summary>
		/// The WM_RBUTTONDOWN message is posted when the user presses the right mouse button 
		/// </summary>
		private const int WM_MBUTTONDBLCLK  = 0x209;
		/// <summary>
		/// The WM_MOUSEWHEEL message is posted when the user presses the mouse wheel. 
		/// </summary>
		private const int WM_MOUSEWHEEL     = 0x020A;

		/// <summary>
		/// The WM_KEYDOWN message is posted to the window with the keyboard focus when a nonsystem 
		/// key is pressed. A nonsystem key is a key that is pressed when the ALT key is not pressed.
		/// </summary>
		private const int WM_KEYDOWN = 0x100;
		/// <summary>
		/// The WM_KEYUP message is posted to the window with the keyboard focus when a nonsystem 
		/// key is released. A nonsystem key is a key that is pressed when the ALT key is not pressed, 
		/// or a keyboard key that is pressed when a window has the keyboard focus.
		/// </summary>
		private const int WM_KEYUP = 0x101;
		/// <summary>
		/// The WM_SYSKEYDOWN message is posted to the window with the keyboard focus when the user 
		/// presses the F10 key (which activates the menu bar) or holds down the ALT key and then 
		/// presses another key. It also occurs when no window currently has the keyboard focus; 
		/// in this case, the WM_SYSKEYDOWN message is sent to the active window. The window that 
		/// receives the message can distinguish between these two contexts by checking the context 
		/// code in the lParam parameter. 
		/// </summary>
		private const int WM_SYSKEYDOWN = 0x104;
		/// <summary>
		/// The WM_SYSKEYUP message is posted to the window with the keyboard focus when the user 
		/// releases a key that was pressed while the ALT key was held down. It also occurs when no 
		/// window currently has the keyboard focus; in this case, the WM_SYSKEYUP message is sent 
		/// to the active window. The window that receives the message can distinguish between 
		/// these two contexts by checking the context code in the lParam parameter. 
		/// </summary>
		private const int WM_SYSKEYUP = 0x105;

		private const byte VK_SHIFT     = 0x10;
		private const byte VK_CAPITAL   = 0x14;
		private const byte VK_NUMLOCK   = 0x90;

		#endregion

		/// <summary>
		/// Creates an instance of UserActivityHook object and sets mouse and keyboard hooks.
		/// </summary>
		/// <exception cref="Win32Exception">Any windows problem.</exception>
		public UserActivityHook()
		{
			Start();
		}

		/// <summary>
		/// Creates an instance of UserActivityHook object and installs both or one of mouse and/or keyboard hooks and starts rasing events
		/// </summary>
		/// <param name="InstallMouseHook"><b>true</b> if mouse events must be monitored</param>
		/// <param name="InstallKeyboardHook"><b>true</b> if keyboard events must be monitored</param>
		/// <exception cref="Win32Exception">Any windows problem.</exception>
		/// <remarks>
		/// To create an instance without installing hooks call new UserActivityHook(false, false)
		/// </remarks>
		public UserActivityHook(bool InstallMouseHook, bool InstallKeyboardHook, bool GestureOnLeft = false, bool GestureOnMiddle = false, bool GestureOnRight = false, bool GesturesRequireAltKeyDown = false)
		{
			this.GestureOnLeft = GestureOnLeft;
			this.GestureOnMiddle = GestureOnMiddle;
			this.GestureOnRight = GestureOnRight;
			this.GesturesRequireAltKeyDown = GesturesRequireAltKeyDown;
			Start(InstallMouseHook, InstallKeyboardHook);
		}

		/// <summary>
		/// Destruction.
		/// </summary>
		~UserActivityHook()
		{
			//uninstall hooks and do not throw exceptions
			Stop(true, true, false);
		}

		/// <summary>
		/// Occurs when the user moves the mouse, presses any mouse button or scrolls the wheel
		/// </summary>
		public event MoreMouseEventHandler OnMouseActivity;
		public event MoreMouseEventHandlerWithHandledState OnMouseActivityWithReturnHandledState;
		/// <summary>
		/// Occurs when the user presses a key
		/// </summary>
		public event KeyEventHandler KeyDown;
		/// <summary>
		/// Occurs when the user presses and releases 
		/// </summary>
		public event KeyPressEventHandler KeyPress;
		/// <summary>
		/// Occurs when the user releases a key
		/// </summary>
		public event KeyEventHandler KeyUp;


		/// <summary>
		/// Stores the handle to the mouse hook procedure.
		/// </summary>
		private int hMouseHook = 0;
		/// <summary>
		/// Stores the handle to the keyboard hook procedure.
		/// </summary>
		private int hKeyboardHook = 0;


		/// <summary>
		/// Declare MouseHookProcedure as HookProc type.
		/// </summary>
		private static HookProc MouseHookProcedure;
		/// <summary>
		/// Declare KeyboardHookProcedure as HookProc type.
		/// </summary>
		private static HookProc KeyboardHookProcedure;

		/// <summary>
		/// Installs both mouse and keyboard hooks and starts rasing events
		/// </summary>
		/// <exception cref="Win32Exception">Any windows problem.</exception>
		public void Start()
		{
			this.Start(true, true);
		}

		/// <summary>
		/// Installs both or one of mouse and/or keyboard hooks and starts rasing events
		/// </summary>
		/// <param name="InstallMouseHook"><b>true</b> if mouse events must be monitored</param>
		/// <param name="InstallKeyboardHook"><b>true</b> if keyboard events must be monitored</param>
		/// <exception cref="Win32Exception">Any windows problem.</exception>
		public void Start(bool InstallMouseHook, bool InstallKeyboardHook)
		{
			// install Mouse hook only if it is not installed and must be installed
			if (hMouseHook == 0 && InstallMouseHook)
			{
				//Create an instance of HookProc.
				MouseHookProcedure = new HookProc(MouseHookProc);
				//install hook
				hMouseHook = SetWindowsHookEx(
						WH_MOUSE_LL,
						MouseHookProcedure,
						Win32Api.GetModuleHandle("user32"),//Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]),
						0);
				//If SetWindowsHookEx fails.
				if (hMouseHook == 0)
				{
					//Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
					int errorCode = Marshal.GetLastWin32Error();
					//do cleanup
					Stop(true, false, false);
					//Initializes and throws a new instance of the Win32Exception class with the specified error. 
					throw new Win32Exception(errorCode);
				}
			}

			// install Keyboard hook only if it is not installed and must be installed
			if (hKeyboardHook == 0 && InstallKeyboardHook)
			{
				// Create an instance of HookProc.
				KeyboardHookProcedure = new HookProc(KeyboardHookProc);
				//install hook
				hKeyboardHook = SetWindowsHookEx(
						WH_KEYBOARD_LL,
						KeyboardHookProcedure,
						Win32Api.GetModuleHandle("user32"),//Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]),
						0);
				//If SetWindowsHookEx fails.
				if (hKeyboardHook == 0)
				{
					//Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
					int errorCode = Marshal.GetLastWin32Error();
					//do cleanup
					Stop(false, true, false);
					//Initializes and throws a new instance of the Win32Exception class with the specified error. 
					throw new Win32Exception(errorCode);
				}
			}
		}

		/// <summary>
		/// Stops monitoring both mouse and keyboard events and rasing events.
		/// </summary>
		/// <exception cref="Win32Exception">Any windows problem.</exception>
		public void Stop()
		{
			this.Stop(true, true, true);
		}

		/// <summary>
		/// Stops monitoring both or one of mouse and/or keyboard events and rasing events.
		/// </summary>
		/// <param name="UninstallMouseHook"><b>true</b> if mouse hook must be uninstalled</param>
		/// <param name="UninstallKeyboardHook"><b>true</b> if keyboard hook must be uninstalled</param>
		/// <param name="ThrowExceptions"><b>true</b> if exceptions which occured during uninstalling must be thrown</param>
		/// <exception cref="Win32Exception">Any windows problem.</exception>
		public void Stop(bool UninstallMouseHook, bool UninstallKeyboardHook, bool ThrowExceptions)
		{
			//if mouse hook set and must be uninstalled
			if (hMouseHook != 0 && UninstallMouseHook)
			{
				//uninstall hook
				int retMouse = UnhookWindowsHookEx(hMouseHook);
				//reset invalid handle
				hMouseHook = 0;
				//if failed and exception must be thrown
				if (retMouse == 0 && ThrowExceptions)
				{
					//Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
					int errorCode = Marshal.GetLastWin32Error();
					//Initializes and throws a new instance of the Win32Exception class with the specified error. 
					throw new Win32Exception(errorCode);
				}
			}

			//if keyboard hook set and must be uninstalled
			if (hKeyboardHook != 0 && UninstallKeyboardHook)
			{
				//uninstall hook
				int retKeyboard = UnhookWindowsHookEx(hKeyboardHook);
				//reset invalid handle
				hKeyboardHook = 0;
				//if failed and exception must be thrown
				if (retKeyboard == 0 && ThrowExceptions)
				{
					//Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
					int errorCode = Marshal.GetLastWin32Error();
					//Initializes and throws a new instance of the Win32Exception class with the specified error. 
					throw new Win32Exception(errorCode);
				}
			}
		}

		private bool GestureOnLeft = false;
		private bool GestureOnMiddle = false;
		private bool GestureOnRight = true;
		private bool GesturesRequireAltKeyDown = false;

		DateTime lastLeftDown = DateTime.MinValue;
		DateTime lastMiddleDown = DateTime.MinValue;
		DateTime lastRightDown = DateTime.MinValue;
		/// <summary>
		/// A callback function which will be called every time a mouse activity detected.
		/// </summary>
		/// <param name="nCode">
		/// [in] Specifies whether the hook procedure must process the message. 
		/// If nCode is HC_ACTION, the hook procedure must process the message. 
		/// If nCode is less than zero, the hook procedure must pass the message to the 
		/// CallNextHookEx function without further processing and must return the 
		/// value returned by CallNextHookEx.
		/// </param>
		/// <param name="wParam">
		/// [in] Specifies whether the message was sent by the current thread. 
		/// If the message was sent by the current thread, it is nonzero; otherwise, it is zero. 
		/// </param>
		/// <param name="lParam">
		/// [in] Pointer to a CWPSTRUCT structure that contains details about the message. 
		/// </param>
		/// <returns>
		/// If nCode is less than zero, the hook procedure must return the value returned by CallNextHookEx. 
		/// If nCode is greater than or equal to zero, it is highly recommended that you call CallNextHookEx 
		/// and return the value it returns; otherwise, other applications that have installed WH_CALLWNDPROC 
		/// hooks will not receive hook notifications and may behave incorrectly as a result. If the hook 
		/// procedure does not call CallNextHookEx, the return value should be zero. 
		/// </returns>
		private int MouseHookProc(int nCode, int wParam, IntPtr lParam)
		{
			// if ok and someone listens to our events
			if ((nCode >= 0) && (OnMouseActivity != null || OnMouseActivityWithReturnHandledState != null || OnGesture != null))
			{
				//Marshall the data from callback.
				MouseLLHookStruct mouseHookStruct = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct));

				//detect button clicked
				MoreMouseButton button = null;
				short mouseDelta = 0;
				DateTime now = DateTime.Now;
				switch (wParam)
				{
					case WM_LBUTTONDOWN:
						if (GestureOnLeft && OnGesture != null)
							BeginGesture();
						button = new MoreMouseButton(MouseButtons.Left, MoreMouseButton.MoreButtonStates.Down);
						if (now.Subtract(lastLeftDown).TotalMilliseconds <= GetDoubleClickTime())
						{
							button.ButtonState = MoreMouseButton.MoreButtonStates.DoubleClicked;
							lastLeftDown = DateTime.MinValue;//So we dont receive another double-click on the third fast click
						}
						else
							lastLeftDown = now;
						break;
					case WM_LBUTTONUP:
						if (GestureOnLeft && OnGesture != null)
							EndGesture();
						button = new MoreMouseButton(MouseButtons.Left, MoreMouseButton.MoreButtonStates.Up);
						break;
					case WM_LBUTTONDBLCLK:
						//Never occurs see inside the left button down case
						//button = new MoreMouseButton(MouseButtons.Left, MoreMouseButton.MoreButtonStates.DoubleClicked);
						break;
					case WM_MBUTTONDOWN:
						if (GestureOnMiddle && OnGesture != null)
							BeginGesture();
						button = new MoreMouseButton(MouseButtons.Middle, MoreMouseButton.MoreButtonStates.Down);
						if (now.Subtract(lastMiddleDown).TotalMilliseconds <= GetDoubleClickTime())
						{
							button.ButtonState = MoreMouseButton.MoreButtonStates.DoubleClicked;
							lastMiddleDown = DateTime.MinValue;//So we dont receive another double-click on the third fast click
						}
						else
							lastMiddleDown = now;
						break;
					case WM_MBUTTONUP:
						if (GestureOnMiddle && OnGesture != null)
							EndGesture();
						button = new MoreMouseButton(MouseButtons.Middle, MoreMouseButton.MoreButtonStates.Up);
						break;
					case WM_MBUTTONDBLCLK:
						//Never occurs see inside the middle button down case
						//button = new MoreMouseButton(MouseButtons.Middle, MoreMouseButton.MoreButtonStates.DoubleClicked);
						break;
					case WM_RBUTTONDOWN:
						if (GestureOnRight && OnGesture != null)
							BeginGesture();
						button = new MoreMouseButton(MouseButtons.Right, MoreMouseButton.MoreButtonStates.Down);
						if (now.Subtract(lastRightDown).TotalMilliseconds <= GetDoubleClickTime())
						{
							button.ButtonState = MoreMouseButton.MoreButtonStates.DoubleClicked;
							lastRightDown = DateTime.MinValue;//So we dont receive another double-click on the third fast click
						}
						else
							lastRightDown = now;
						break;
					case WM_RBUTTONUP:
						if (GestureOnRight && OnGesture != null)
							EndGesture();
						button = new MoreMouseButton(MouseButtons.Right, MoreMouseButton.MoreButtonStates.Up);
						break;
					case WM_RBUTTONDBLCLK:
						//Never occurs see inside the right button down case
						//button = new MoreMouseButton(MouseButtons.Right, MoreMouseButton.MoreButtonStates.DoubleClicked);
						break;
					case WM_MOUSEWHEEL:
						//If the message is WM_MOUSEWHEEL, the high-order word of mouseData member is the wheel delta. 
						//One wheel click is defined as WHEEL_DELTA, which is 120. 
						//(value >> 16) & 0xffff; retrieves the high-order word from the given 32-bit value
						mouseDelta = (short)((mouseHookStruct.mouseData >> 16) & 0xffff);
						//X BUTTONS (I havent them so was unable to test)
						//If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP, 
						//or WM_NCXBUTTONDBLCLK, the high-order word specifies which X button was pressed or released, 
						//and the low-order word is reserved. This value can be one or more of the following values. 
						//Otherwise, mouseData is not used. 
						break;
					case WM_MOUSEMOVE:
						if (OnGesture != null)
							AddToGesture();
						break;
				}

				//double clicks
				int clickCount = 0;
				if (button != null)//MouseButtons.None)
					if (wParam == WM_LBUTTONDBLCLK || wParam == WM_RBUTTONDBLCLK) clickCount = 2;
					else clickCount = 1;

				//generate event 
				MoreMouseEventArgs e = new MoreMouseEventArgs(
					button,
					clickCount,
					mouseHookStruct.pt.X,
					mouseHookStruct.pt.Y,
					mouseDelta);

				//raise it
				if (OnMouseActivity != null)
					OnMouseActivity(this, e);
				if (OnMouseActivityWithReturnHandledState != null)
				{
					if (OnMouseActivityWithReturnHandledState(this, e))
						return 1;//If we return non-zero value, it marks the event as "handled"??
				}
			}
			//call next hook
			return CallNextHookEx(hMouseHook, nCode, wParam, lParam);
		}

		Form tmpOverlayForm;
		private List<KeyValuePair<Point, Point>> linesDrawn = new List<KeyValuePair<Point, Point>>();//Lines drawn to screen
		//private bool hasLinesDrawn = false;
		private Pen GestureLinePen = new Pen(Color.FromArgb(255, 0, 120, 0), 2);//Pens.Green;
		//private int gestureSegmentCount = 0;
		private void BeginGesture()
		{
			if (this.GesturesRequireAltKeyDown
				&& System.Windows.Input.Keyboard.Modifiers != System.Windows.Input.ModifierKeys.Alt)
				return;

			lastGesturePoint = Cursor.Position;
			gesture = new MouseGesture(lastGesturePoint);
			distance = 0;
			//hasLinesDrawn = false;
			linesDrawn.Clear();
			//gestureSegmentCount = 0;
		}

		private void AddToGesture()
		{
			if (gesture != null)
			{
				Point point = Cursor.Position;
				double segmentDistance = MouseGesture.GetDistance(lastGesturePoint, point);
				if (distance > 0 || segmentDistance > MouseGesture.mouseMoveSegmentLength)
				{
					//int rem;
					//Math.DivRem(gestureSegmentCount, 3, out rem);
					//if (rem == 0)
					//{
					if (tmpOverlayForm == null)
					{
						tmpOverlayForm = new Form();
						tmpOverlayForm.FormClosing += (snder, evt) => { evt.Cancel = true; ((Form)snder).Hide(); };
						tmpOverlayForm.FormBorderStyle = FormBorderStyle.None;
						tmpOverlayForm.Opacity = 0.5;// 05;
						tmpOverlayForm.TopMost = true;
						tmpOverlayForm.StartPosition = FormStartPosition.Manual;
						tmpOverlayForm.WindowState = FormWindowState.Maximized;
						tmpOverlayForm.Paint += (snder, evt) =>
						{
							//SendRightMouseClick();
							foreach (var lineEndpoints in linesDrawn)
								evt.Graphics.DrawLine(GestureLinePen, lineEndpoints.Key, lineEndpoints.Value);
						};
					}
					if (!tmpOverlayForm.Visible)
					{
						tmpOverlayForm.BringToFront();
						tmpOverlayForm.Show();
						Application.DoEvents();
						tmpOverlayForm.Activate();
					}
					//while (tmpOverlayForm.Handle == IntPtr.Zero)
					//    Application.DoEvents();

					linesDrawn.Add(new KeyValuePair<Point, Point>(lastGesturePoint, Cursor.Position));
					tmpOverlayForm.Invalidate();

					////IntPtr desktop = Win32Api.GetDC(IntPtr.Zero);
					//using (Graphics g = Graphics.FromHdc(tmpOverlayForm.Handle))//desktop))
					//{
					//    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
					//    g.DrawLine(GestureLinePen, lastGesturePoint, Cursor.Position);
					//    //linesDrawn.Add(new KeyValuePair<Point,Point>(lastGesturePoint, Cursor.Position));
					//    hasLinesDrawn = true;
					//}
					//Win32Api.ReleaseDC(tmpOverlayForm.Handle);//desktop);
					//}
				}
				if (segmentDistance >= MouseGesture.mouseMoveSegmentLength)
				{
					gesture.AddSegment(MouseGesture.GetDirection(lastGesturePoint, point));
					distance += segmentDistance;
					lastGesturePoint = point;
					//gestureSegmentCount++;
				}
			}
		}

		private void EndGesture()
		{
			//if (hasLinesDrawn)
			if (linesDrawn.Count > 0)//Lines where drawn
			{
				//foreach (var lineDrawn in linesDrawn)
				//{
				//}
				tmpOverlayForm.Hide();
				//hasLinesDrawn = false;
				linesDrawn.Clear();
				//Win32Api.InvalidateRect(IntPtr.Zero, IntPtr.Zero, true);
			}

			//check minimal length
			//change minimal length checking  - does not work for gesture LeftRight, etc...
			if (distance < MouseGesture.minGestureSize || gesture.Count == 0)
			{
				//too short for mouse gesture - send regular right mouse click
				//mf.Enabled = false;
				//tmpTextbox.Text += "Disabled" + Environment.NewLine;
				//SendRightMouseClick();
				//Application.DoEvents();
				//mf.Enabled = true;
				//tmpTextbox.Text += "Enabled" + Environment.NewLine;
			}
			else
			{
				GestureHandler temp = OnGesture;
				if (temp != null)
				{
					temp(this, new MouseGestureEventArgs(gesture));
					//MouseGestureEventArgs args = new MouseGestureEventArgs(gesture);
					//tmpTextbox.Text += "Before temp" + Environment.NewLine;
					//temp(this, args);
					//tmpTextbox.Text += "After temp" + Environment.NewLine;
				}
			}
			gesture = null;
		}

		private static void SendRightMouseClick()
		{
			Win32Api.INPUTMOUSE im = new Win32Api.INPUTMOUSE();
			im.type = Win32Api.INPUTTYPE.INPUT_MOUSE;

			//Sends MOUSEEVENTF_RIGHTDOWN
			im.mi.dwFlags = Win32Api.MOUSEEVENTFLAGS.MOUSEEVENTF_RIGHTDOWN;
			Win32Api.SendInput((uint)1, ref im, Marshal.SizeOf(im));

			//Sends MOUSEEVENTF_RIGHTUP
			im.mi.dwFlags = Win32Api.MOUSEEVENTFLAGS.MOUSEEVENTF_RIGHTUP;
			Win32Api.SendInput((uint)1, ref im, Marshal.SizeOf(im));
		}

		/// <summary>
		/// A callback function which will be called every time a keyboard activity detected.
		/// </summary>
		/// <param name="nCode">
		/// [in] Specifies whether the hook procedure must process the message. 
		/// If nCode is HC_ACTION, the hook procedure must process the message. 
		/// If nCode is less than zero, the hook procedure must pass the message to the 
		/// CallNextHookEx function without further processing and must return the 
		/// value returned by CallNextHookEx.
		/// </param>
		/// <param name="wParam">
		/// [in] Specifies whether the message was sent by the current thread. 
		/// If the message was sent by the current thread, it is nonzero; otherwise, it is zero. 
		/// </param>
		/// <param name="lParam">
		/// [in] Pointer to a CWPSTRUCT structure that contains details about the message. 
		/// </param>
		/// <returns>
		/// If nCode is less than zero, the hook procedure must return the value returned by CallNextHookEx. 
		/// If nCode is greater than or equal to zero, it is highly recommended that you call CallNextHookEx 
		/// and return the value it returns; otherwise, other applications that have installed WH_CALLWNDPROC 
		/// hooks will not receive hook notifications and may behave incorrectly as a result. If the hook 
		/// procedure does not call CallNextHookEx, the return value should be zero. 
		/// </returns>
		private int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
		{
			//indicates if any of underlaing events set e.Handled flag
			bool handled = false;
			//it was ok and someone listens to events
			if ((nCode >= 0) && (KeyDown != null || KeyUp != null || KeyPress != null))
			{
				//read structure KeyboardHookStruct at lParam
				KeyboardHookStruct MyKeyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
				//raise KeyDown
				if (KeyDown != null && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
				{
					Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
					KeyEventArgs e = new KeyEventArgs(keyData);
					KeyDown(this, e);
					handled = handled || e.Handled;
				}

				// raise KeyPress
				if (KeyPress != null && wParam == WM_KEYDOWN)
				{
					bool isDownShift = ((GetKeyState(VK_SHIFT) & 0x80) == 0x80 ? true : false);
					bool isDownCapslock = (GetKeyState(VK_CAPITAL) != 0 ? true : false);

					byte[] keyState = new byte[256];
					GetKeyboardState(keyState);
					byte[] inBuffer = new byte[2];
					if (ToAscii(MyKeyboardHookStruct.vkCode,
										MyKeyboardHookStruct.scanCode,
										keyState,
										inBuffer,
										MyKeyboardHookStruct.flags) == 1)
					{
						char key = (char)inBuffer[0];
						if ((isDownCapslock ^ isDownShift) && Char.IsLetter(key)) key = Char.ToUpper(key);
						KeyPressEventArgs e = new KeyPressEventArgs(key);
						KeyPress(this, e);
						handled = handled || e.Handled;
					}
				}

				// raise KeyUp
				if (KeyUp != null && (wParam == WM_KEYUP || wParam == WM_SYSKEYUP))
				{
					Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
					KeyEventArgs e = new KeyEventArgs(keyData);
					KeyUp(this, e);
					handled = handled || e.Handled;
				}

			}

			//if event handled in application do not handoff to other listeners
			if (handled)
				return 1;
			else
				return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam);
		}
	}
}