using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace SharedClasses
{
	public class WindowsInterop
	{
		public static readonly string LocalAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		public static readonly string MydocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

		public static void StartCommandPromptOrVScommandPrompt(Object textfeedbackSenderObject, string cmdpath, bool VisualStudioMode, TextFeedbackEventHandler textFeedbackEvent = null)
		{
			const string vsbatfile = @"c:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\vcvarsall.bat";

			string processArgs = 
								VisualStudioMode ? @"/k """ + vsbatfile + @""" x86"
				: "";

			if (Directory.Exists(cmdpath))
			{
				if (!VisualStudioMode || (VisualStudioMode && File.Exists(vsbatfile)))
				{
					Process proc = new Process();
					proc.StartInfo = new ProcessStartInfo("cmd", processArgs);
					proc.StartInfo.WorkingDirectory = cmdpath;
					proc.Start();
				}
				else TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, @"Unable to start Visual Studio Command Prompt, cannot find file: """ + vsbatfile + @"""" + cmdpath);
			}
			else TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Folder does not exist, cannot start cmd: " + cmdpath);
		}

		public static void ShowAndActivateForm(Form form)
		{
			form.Visible = true;
			form.Activate();
			form.WindowState = FormWindowState.Normal;
		}

		public static void ShowAndActivateWindow(Window window)
		{
			System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(window);
			//window.Visibility = Visibility.Visible;
			window.Show();
			window.UpdateLayout();
			if (window.WindowState != WindowState.Normal) window.WindowState = WindowState.Normal;
			window.Activate();
		}

		// Define the Win32 API methods we are going to use
		[DllImport("user32.dll")]
		private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

		[DllImport("user32.dll")]
		private static extern bool InsertMenu(IntPtr hMenu, Int32 wPosition, Int32 wFlags, Int32 wIDNewItem, string lpNewItem);

		/// Define our Constants we will use
		public const Int32 WM_SYSCOMMAND = 0x112;
		public const Int32 MF_SEPARATOR = 0x800;
		public const Int32 MF_BYPOSITION = 0x400;
		public const Int32 MF_STRING = 0x0;

		public static IntPtr GetWindowHandle(Window window) { return new WindowInteropHelper(window).Handle; }

		public static void SetHookForSystemMenu(Window window, HwndSourceHook wndProc, List<SystemMenuItem> MenuItemList)
		{
			if (
				window == null || GetWindowHandle(window) == IntPtr.Zero
				|| wndProc == null
				|| MenuItemList == null || MenuItemList.Count == 0)
				return;

			/// Get the Handle for the Forms System Menu
			IntPtr systemMenuHandle = GetSystemMenu(GetWindowHandle(window), false);

			int counter = 5;
			foreach (SystemMenuItem menuitem in MenuItemList)
			{
				switch (menuitem.SystemMenuItemType)
				{
					case SystemMenuItemTypeEnum.Separator:
						InsertMenu(systemMenuHandle, counter++, MF_BYPOSITION | MF_SEPARATOR, 0, string.Empty);
						break;
					case SystemMenuItemTypeEnum.String:
						InsertMenu(systemMenuHandle, counter++, MF_BYPOSITION, menuitem.wParamForItem, menuitem.DisplayText);
						break;
					//default:
					//	break;
				}
			}

			// Attach our WndProc handler to this Window
			HwndSource source = HwndSource.FromHwnd(GetWindowHandle(window));
			source.AddHook(wndProc);
		}
	}

	public enum SystemMenuItemTypeEnum { Separator, String }
	public class SystemMenuItem
	{
		public SystemMenuItemTypeEnum SystemMenuItemType;
		public int wParamForItem;
		public string DisplayText;
		public SystemMenuItem(SystemMenuItemTypeEnum SystemMenuItemType = SystemMenuItemTypeEnum.Separator, int wParamForItem = 0, string DisplayText = null)
		{
			if (SystemMenuItemType == SystemMenuItemTypeEnum.String && (wParamForItem == 0 || DisplayText == null))
			{
				if (wParamForItem == 0)
					UserMessages.ShowWarningMessage("Cannot add String SystemMenuItem with wParamForItem = " + 0);
				else//if DisplayText == null
					UserMessages.ShowWarningMessage("Cannot add String SystemMenuItem with no DisplayText");
			}
			this.SystemMenuItemType = SystemMenuItemType;
			this.wParamForItem = wParamForItem;
			this.DisplayText = DisplayText;
		}
	}

	public class WindowBehavior
	{
		private static readonly Type OwnerType = typeof(WindowBehavior);

		#region HideCloseButton (attached property)

		public static readonly DependencyProperty HideCloseButtonProperty =
        DependencyProperty.RegisterAttached(
						"HideCloseButton",
						typeof(bool),
						OwnerType,
						new FrameworkPropertyMetadata(false, new PropertyChangedCallback(HideCloseButtonChangedCallback)));

		[AttachedPropertyBrowsableForType(typeof(Window))]
		public static bool GetHideCloseButton(Window obj)
		{
			return (bool)obj.GetValue(HideCloseButtonProperty);
		}

		[AttachedPropertyBrowsableForType(typeof(Window))]
		public static void SetHideCloseButton(Window obj, bool value)
		{
			obj.SetValue(HideCloseButtonProperty, value);
		}

		private static void HideCloseButtonChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var window = d as Window;
			if (window == null) return;

			var hideCloseButton = (bool)e.NewValue;
			if (hideCloseButton && !GetIsHiddenCloseButton(window))
			{
				if (!window.IsLoaded)
				{
					window.Loaded += LoadedDelegate;
				}
				else
				{
					HideCloseButton(window);
				}
				SetIsHiddenCloseButton(window, true);
			}
			else if (!hideCloseButton && GetIsHiddenCloseButton(window))
			{
				if (!window.IsLoaded)
				{
					window.Loaded -= LoadedDelegate;
				}
				else
				{
					ShowCloseButton(window);
				}
				SetIsHiddenCloseButton(window, false);
			}
		}

		#region Win32 imports

		private const int GWL_STYLE = -16;
		private const int WS_SYSMENU = 0x80000;
		[DllImport("user32.dll", SetLastError = true)]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		#endregion

		private static readonly RoutedEventHandler LoadedDelegate = (sender, args) =>
		{
			if (sender is Window == false) return;
			var w = (Window)sender;
			HideCloseButton(w);
			w.Loaded -= LoadedDelegate;
		};

		private static void HideCloseButton(Window w)
		{
			var hwnd = new WindowInteropHelper(w).Handle;
			SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
		}

		private static void ShowCloseButton(Window w)
		{
			var hwnd = new WindowInteropHelper(w).Handle;
			SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) | WS_SYSMENU);
		}

		#endregion

		#region IsHiddenCloseButton (readonly attached property)

		private static readonly DependencyPropertyKey IsHiddenCloseButtonKey =
        DependencyProperty.RegisterAttachedReadOnly(
						"IsHiddenCloseButton",
						typeof(bool),
						OwnerType,
						new FrameworkPropertyMetadata(false));

		public static readonly DependencyProperty IsHiddenCloseButtonProperty =
        IsHiddenCloseButtonKey.DependencyProperty;

		[AttachedPropertyBrowsableForType(typeof(Window))]
		public static bool GetIsHiddenCloseButton(Window obj)
		{
			return (bool)obj.GetValue(IsHiddenCloseButtonProperty);
		}

		private static void SetIsHiddenCloseButton(Window obj, bool value)
		{
			obj.SetValue(IsHiddenCloseButtonKey, value);
		}

		#endregion
	}
}