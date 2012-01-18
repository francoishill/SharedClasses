using System;
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