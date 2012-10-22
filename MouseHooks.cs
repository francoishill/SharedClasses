using System.Runtime.InteropServices;
using System;
using System.Windows.Forms;
using System.Collections.Generic;

namespace SharedClasses
{
	public class MouseHooks
	{
		public enum MouseButton { Left, Right, Middle };

		public delegate void MouseUpEventHandler(object sender, MouseUpEventArgs e);
		public class MouseUpEventArgs : EventArgs
		{
			public MouseButton MouseButtonUp;
			public MouseUpEventArgs(MouseButton mouseButtonUp)
			{
				MouseButtonUp = mouseButtonUp;
			}
		}

		public delegate void MouseDownEventHandler(object sender, MouseDownEventArgs e);
		public class MouseDownEventArgs : EventArgs
		{
			public MouseButton MouseButtonDown;
			public MouseDownEventArgs(MouseButton mouseButtonDown)
			{
				MouseButtonDown = mouseButtonDown;
			}
		}

		public delegate void MouseGestureEventHandler(object sender, MouseGestureEventArgs e);
		public class MouseGestureEventArgs : EventArgs
		{
			public Win32Api.MouseGestures MouseGesture;
			public MouseGestureEventArgs(Win32Api.MouseGestures mouseGesture)
			{
				MouseGesture = mouseGesture;
			}
		}

		public delegate void MouseMoveEventHandler(object sender, EventArgs e);
		public class MouseMoveEventArgs : EventArgs
		{
			//public Win32Api.MouseGestures MouseGesture;
			public MouseMoveEventArgs()//Win32Api.MouseGestures mouseGesture)
			{
				//MouseGesture = mouseGesture;
			}
		}

		public class MouseHook
		{
			public event MouseUpEventHandler MouseUpEvent;
			public event MouseDownEventHandler MouseDownEvent;
			public event MouseGestureEventHandler MouseGestureEvent;
			public event MouseMoveEventHandler MouseMoveEvent;
			//protected virtual void OnMyEvent(MyEventArgs e)
			//{
			//    MyEvent(this, e);
			//}

			//List<String> tmpStringListMiddleClicks = new List<string>();
			public List<String> currentPasswordList = null;
			public MousePressStates mousePressStates = new MousePressStates();

			private Boolean Started = false;
			Win32Api.HookProc MouseHookProcedure; //Declare MouseHookProcedure as HookProc type.        
			static int hMouseHook = 0; //Declare mouse hook handle as int. 

			#region System functions and classes

			public MouseHook()
			{
			}
			~MouseHook()
			{
				Stop();
			}

			public void Start()
			{
				if (!Started)
				{
					Started = true;
					if (hMouseHook == 0)
					{
						MouseHookProcedure = new Win32Api.HookProc(MouseHookProc);
						hMouseHook = Win32Api.SetWindowsHookEx((int)Win32Api.HookTypes.WH_MOUSE_LL, MouseHookProcedure, System.Runtime.InteropServices.Marshal.GetHINSTANCE(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0]), 0);

						if (hMouseHook == 0)
						{
							UserMessages.ShowErrorMessage("SetWindowsHookEx Failed");
							Stop();
						}
					}
				}
			}

			public void Stop()
			{
				if (Started)
				{
					Started = false;
					bool retMouse = true;
					if (hMouseHook != 0)
					{
						retMouse = Win32Api.UnhookWindowsHookEx(hMouseHook);
						hMouseHook = 0;
					}
					if (!(retMouse)) throw new Exception("UnhookWindowsHookEx failed.");
				}
			}


			private int MouseHookProc(int nCode, Int32 wParam, IntPtr lParam)
			{
				switch (wParam)
				{
					case Win32Api.WM_LBUTTONDOWN: mousePressStates.MouseLeftDown = true; break;
					case Win32Api.WM_LBUTTONUP: mousePressStates.MouseLeftDown = false; break;

					case Win32Api.WM_RBUTTONDOWN: mousePressStates.MouseRightDown = true; break;
					case Win32Api.WM_RBUTTONUP: mousePressStates.MouseRightDown = false; break;

					case Win32Api.WM_MBUTTONDOWN: mousePressStates.MouseMiddleDown = true; break;
					case Win32Api.WM_MBUTTONUP: mousePressStates.MouseMiddleDown = false; break;

					case Win32Api.WM_MOUSEMOVE: if (MouseMoveEvent != null) MouseMoveEvent(this, new EventArgs()); break;

					//case WM_MOUSEWHEEL: OnMouseWheel(); break;
				}

				if (wParam == Win32Api.WM_LBUTTONUP && MouseUpEvent != null) MouseUpEvent(this, new MouseUpEventArgs(MouseButton.Left));
				if (wParam == Win32Api.WM_RBUTTONUP && MouseUpEvent != null) MouseUpEvent(this, new MouseUpEventArgs(MouseButton.Right));
				if (wParam == Win32Api.WM_MBUTTONUP && MouseUpEvent != null) MouseUpEvent(this, new MouseUpEventArgs(MouseButton.Middle));

				if (wParam == Win32Api.WM_LBUTTONDOWN && MouseDownEvent != null) MouseDownEvent(this, new MouseDownEventArgs(MouseButton.Left));
				if (wParam == Win32Api.WM_RBUTTONDOWN && MouseDownEvent != null) MouseDownEvent(this, new MouseDownEventArgs(MouseButton.Right));
				if (wParam == Win32Api.WM_MBUTTONDOWN && MouseDownEvent != null) MouseDownEvent(this, new MouseDownEventArgs(MouseButton.Middle));

				if (mousePressStates.MouseLeftDown && !mousePressStates.MouseMiddleDown && wParam == Win32Api.WM_RBUTTONDOWN) { mousePressStates.HoldLeftPressRight = true; if (MouseGestureEvent != null) MouseGestureEvent(this, new MouseGestureEventArgs(Win32Api.MouseGestures.LR)); }
				if (mousePressStates.HoldLeftPressRight && (wParam == Win32Api.WM_LBUTTONUP || wParam == Win32Api.WM_RBUTTONUP)) mousePressStates.HoldLeftPressRight = false;
				if (mousePressStates.MouseLeftDown && !mousePressStates.MouseRightDown && wParam == Win32Api.WM_MBUTTONDOWN) { mousePressStates.HoldLeftPressMiddle = true; if (MouseGestureEvent != null) MouseGestureEvent(this, new MouseGestureEventArgs(Win32Api.MouseGestures.LM)); }
				if (mousePressStates.HoldLeftPressMiddle && (wParam == Win32Api.WM_LBUTTONUP || wParam == Win32Api.WM_MBUTTONUP)) mousePressStates.HoldLeftPressMiddle = false;
				if (mousePressStates.MouseLeftDown && mousePressStates.MouseRightDown && wParam == Win32Api.WM_MBUTTONDOWN) { mousePressStates.HoldLeftAndRightPressMiddle = true; if (MouseGestureEvent != null) MouseGestureEvent(this, new MouseGestureEventArgs(Win32Api.MouseGestures.LRM)); }
				if (mousePressStates.HoldLeftAndRightPressMiddle && (wParam == Win32Api.WM_LBUTTONUP || wParam == Win32Api.WM_MBUTTONUP || wParam == Win32Api.WM_RBUTTONUP)) mousePressStates.HoldLeftAndRightPressMiddle = false;
				if (mousePressStates.MouseLeftDown && mousePressStates.MouseMiddleDown && wParam == Win32Api.WM_RBUTTONDOWN) { mousePressStates.HoldLeftAndMiddlePressRight = true; if (MouseGestureEvent != null) MouseGestureEvent(this, new MouseGestureEventArgs(Win32Api.MouseGestures.LMR)); }
				if (mousePressStates.HoldLeftAndMiddlePressRight && (wParam == Win32Api.WM_LBUTTONUP || wParam == Win32Api.WM_MBUTTONUP || wParam == Win32Api.WM_RBUTTONUP)) mousePressStates.HoldLeftAndMiddlePressRight = false;

				if (mousePressStates.MouseRightDown && !mousePressStates.MouseMiddleDown && wParam == Win32Api.WM_LBUTTONDOWN) { mousePressStates.HoldRightPressLeft = true; if (MouseGestureEvent != null) MouseGestureEvent(this, new MouseGestureEventArgs(Win32Api.MouseGestures.RL)); }
				if (mousePressStates.HoldRightPressLeft && (wParam == Win32Api.WM_RBUTTONUP || wParam == Win32Api.WM_LBUTTONUP)) mousePressStates.HoldRightPressLeft = false;
				if (mousePressStates.MouseRightDown && !mousePressStates.MouseLeftDown && wParam == Win32Api.WM_MBUTTONDOWN) { mousePressStates.HoldRightPressMiddle = true; if (MouseGestureEvent != null) MouseGestureEvent(this, new MouseGestureEventArgs(Win32Api.MouseGestures.RM)); }
				if (mousePressStates.HoldRightPressMiddle && (wParam == Win32Api.WM_RBUTTONUP || wParam == Win32Api.WM_MBUTTONUP)) mousePressStates.HoldRightPressMiddle = false;
				if (mousePressStates.MouseRightDown && mousePressStates.MouseMiddleDown && wParam == Win32Api.WM_LBUTTONDOWN) { mousePressStates.HoldRightAndMiddlePressLeft = true; if (MouseGestureEvent != null) MouseGestureEvent(this, new MouseGestureEventArgs(Win32Api.MouseGestures.RML)); }
				if (mousePressStates.HoldRightAndMiddlePressLeft && (wParam == Win32Api.WM_LBUTTONUP || wParam == Win32Api.WM_MBUTTONUP || wParam == Win32Api.WM_RBUTTONUP)) mousePressStates.HoldRightAndMiddlePressLeft = false;

				if (mousePressStates.MouseMiddleDown && !mousePressStates.MouseRightDown && wParam == Win32Api.WM_LBUTTONDOWN) { mousePressStates.HoldMiddlePressLeft = true; if (MouseGestureEvent != null) MouseGestureEvent(this, new MouseGestureEventArgs(Win32Api.MouseGestures.ML)); }
				if (mousePressStates.HoldMiddlePressLeft && (wParam == Win32Api.WM_MBUTTONUP || wParam == Win32Api.WM_LBUTTONUP)) mousePressStates.HoldMiddlePressLeft = false;
				if (mousePressStates.MouseMiddleDown && !mousePressStates.MouseLeftDown && wParam == Win32Api.WM_RBUTTONDOWN) { mousePressStates.HoldMiddlePressRight = true; if (MouseGestureEvent != null) MouseGestureEvent(this, new MouseGestureEventArgs(Win32Api.MouseGestures.MR)); }
				if (mousePressStates.HoldMiddlePressRight && (wParam == Win32Api.WM_MBUTTONUP || wParam == Win32Api.WM_RBUTTONUP)) mousePressStates.HoldMiddlePressRight = false;

				return Win32Api.CallNextHookEx(hMouseHook, nCode, wParam, lParam);
			}

			public class MousePressStates
			{
				//public enum MouseRockerGestures { HoldLeftPressRight, HoldRightPressLeft };
				//public enum MouseGestures { HoldLeftPressRight, HoldLeftPressMiddle, HoldLeftAndRightPressMiddle, HoldLeftAndMiddlePressRight, HoldRightPressLeft, HoldRightPressMiddle, HoldRightAndMiddlePressLeft, HoldMiddlePressLeft, HoldMiddlePressRight };
				public Boolean MouseLeftDown;
				public Boolean MouseRightDown;
				public Boolean MouseMiddleDown;

				public Boolean HoldLeftPressRight;
				public Boolean HoldLeftPressMiddle;
				public Boolean HoldLeftAndRightPressMiddle;
				public Boolean HoldLeftAndMiddlePressRight;

				public Boolean HoldRightPressLeft;
				public Boolean HoldRightPressMiddle;
				//public Boolean HoldRightAndLeftPressMiddle;
				public Boolean HoldRightAndMiddlePressLeft;

				public Boolean HoldMiddlePressLeft;
				public Boolean HoldMiddlePressRight;
				//public Boolean HoldMiddleAndLeftPressRight;
				//public Boolean HoldMiddleAndRightPressLeft;

				public MousePressStates()
				{
					HoldLeftPressRight = HoldLeftPressMiddle =
					HoldLeftAndRightPressMiddle = HoldLeftAndMiddlePressRight =
					HoldRightPressLeft = HoldRightPressMiddle =
						//HoldRightAndLeftPressMiddle = HoldRightAndMiddlePressLeft =
					HoldRightAndMiddlePressLeft =
					HoldMiddlePressLeft = HoldMiddlePressRight =
						//HoldMiddleAndLeftPressRight = HoldMiddleAndRightPressLeft =
					false;
				}
			}

			#endregion System functions and classes
		}
	}
}