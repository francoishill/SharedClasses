using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Threading;
using System.Threading;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace SharedClasses
{
	public static class WPFHelper
	{
		public static void DoEvents()
		{
			DispatcherFrame frame = new DispatcherFrame(true);
			Dispatcher.CurrentDispatcher.BeginInvoke
			(
			DispatcherPriority.Background,
			(SendOrPostCallback)delegate(object arg)
			{
				var f = arg as DispatcherFrame;
				f.Continue = false;
			},
			frame
			);
			Dispatcher.PushFrame(frame);
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

		public static void MakeWindowClickThrough(this Window window)
		{
			int initialStyle = Win32Api.GetWindowLong(window.GetWindowHandle(), Win32Api.GWL_EXSTYLE);
			Win32Api.SetWindowLong(window.GetWindowHandle(), Win32Api.GWL_EXSTYLE, initialStyle | Win32Api.WS_EX_LAYERED | Win32Api.WS_EX_TRANSPARENT);
		}

		public static void OnPropertyChanged<T>(T item) where T : INotifyPropertyChanged
		{
		}
		/*Example of implementation of OnPropertyChanged
		private void OnPropertyChanged(params Expression<Func<CloudNoteItem, object>>[] propertiesOrFieldsAsExpressions)
		{
			ReflectionInterop.DoForeachPropertOrField<CloudNoteItem>(
				this,
				propertiesOrFieldsAsExpressions,
				(instanceObj, memberInfo, memberValue) =>
				{
					PropertyChanged(instanceObj, new PropertyChangedEventArgs(memberInfo.Name));
				});
		}*/

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

		public static IntPtr GetWindowHandle(this Window window) { return new WindowInteropHelper(window).Handle; }

		private static string GetLastWindowPositionFilename(string applicationName, string subfolderName = null)
		{
			return SettingsInterop.GetFullFilePathInLocalAppdata("LastWindowPos.fjset", applicationName, subfolderName);
		}
		public static bool LoadLastWindowPosition(this Window window, string applicationName, string subfolderName = null)
		{
			try
			{
				string LastWindowPositionFilename = GetLastWindowPositionFilename(applicationName, subfolderName);
				if (!File.Exists(LastWindowPositionFilename))
					return false;

				string[] fileLines = File.ReadAllLines(LastWindowPositionFilename)
					.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

				//We expect double values in this order on separate lines: Left, Top, ActualWidth, ActualHeight
				if (fileLines.Length != 4)
					return false;
				double tmpdouble;
				if (fileLines.Count(l => !double.TryParse(l, out tmpdouble)) > 0)
					return false;//We failed to cast one of the items to a double

				window.Left = double.Parse(fileLines[0]);
				window.Top = double.Parse(fileLines[1]);
				window.Width = double.Parse(fileLines[2]);
				window.Height = double.Parse(fileLines[3]);
				return true;
			}
			catch (Exception exc)
			{
				UserMessages.ShowWarningMessage("Failed loading last window position from file: " + exc.Message);
				return false;
			}
		}

		public static bool SaveLastWindowPosition(this Window window, string applicationName, string subfolderName = null)
		{
			try
			{
				string LastWindowPositionFilename = GetLastWindowPositionFilename(applicationName, subfolderName);
				File.WriteAllLines(LastWindowPositionFilename, new string[]
				{ 
					window.Left.ToString(),
					window.Top.ToString(),
					window.ActualWidth.ToString(),
					window.ActualHeight.ToString()
				});
				return true;
			}
			catch (Exception exc)
			{
				UserMessages.ShowWarningMessage("Failed saving last window position from file: " + exc.Message);
				return false;
			}
		}

		public static void ZoomControlBasedOnMouseWheelEvent(
			this Control control, ref MouseWheelEventArgs evt, int zoomInterval = 2)
		{
			if (evt.Delta < 0)//Rolled down
			{
				if (control.FontSize - zoomInterval >= 8)
					control.FontSize -= zoomInterval;
			}
			else if (evt.Delta > 0)//Rolled up
			{
				if (control.FontSize + zoomInterval <= 50)
					control.FontSize += zoomInterval;
			}
		}

		public static void ScrollTextboxHorizontallyFromMouseWheelEvent(
			this TextBoxBase textboxbaseControl, ref MouseWheelEventArgs evt, int horizontalScrollInterval = 25)
		{
			evt.Handled = true;
			if (evt.Delta < 0)//Rolled down
			{
				textboxbaseControl.ScrollToHorizontalOffset(textboxbaseControl.HorizontalOffset + horizontalScrollInterval);
			}
			else if (evt.Delta > 0)//Rolled up
			{
				textboxbaseControl.ScrollToHorizontalOffset(textboxbaseControl.HorizontalOffset - horizontalScrollInterval);
			}
		}

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

		#region GetVisualChild

		public static WindowInteropHelper GetInteropHelper(this Window window)
		{
			return new WindowInteropHelper(window);
		}

		public static IntPtr GetHandle(this Window window)
		{
			var windowInteropHelper = window.GetInteropHelper();
			windowInteropHelper.EnsureHandle();
			return windowInteropHelper.Handle;
		}

		public static T FindVisualChild<T>(this Visual parent) where T : Visual
		{
			T child = default(T);

			int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < numVisuals; i++)
			{
				Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
				child = v as T;
				if (child == null)
				{
					child = FindVisualChild<T>(v);
				}
				if (child != null)
				{
					break;
				}
			}

			return child;
		}

		#endregion GetVisualChild

		#region FindVisualParent

		public static T FindVisualParent<T>(UIElement element) where T : UIElement
		{
			UIElement parent = element;
			while (parent != null)
			{
				T correctlyTyped = parent as T;
				if (correctlyTyped != null)
				{
					return correctlyTyped;
				}

				parent = VisualTreeHelper.GetParent(parent) as UIElement;
			}

			return null;
		}

		#endregion FindVisualParent

		#region FindPartByName

		public static DependencyObject FindPartByName(DependencyObject ele, string name)
		{
			DependencyObject result;
			if (ele == null)
			{
				return null;
			}
			if (name.Equals(ele.GetValue(FrameworkElement.NameProperty)))
			{
				return ele;
			}

			int numVisuals = VisualTreeHelper.GetChildrenCount(ele);
			for (int i = 0; i < numVisuals; i++)
			{
				DependencyObject vis = VisualTreeHelper.GetChild(ele, i);
				if ((result = FindPartByName(vis, name)) != null)
				{
					return result;
				}
			}
			return null;
		}

		#endregion FindPartByName

		public static bool DoesFrameworkElementContainMouse(FrameworkElement frameworkElement, int ignoreBorderWidth = 0)
		{
			frameworkElement.UpdateLayout();
			var elementRect = new Rect(frameworkElement.PointToScreen(
				new Point(0, 0)),
				new Size(frameworkElement.ActualWidth, frameworkElement.ActualHeight));
			elementRect = Rect.Inflate(elementRect, -ignoreBorderWidth, -ignoreBorderWidth);
			var mousePos = MouseLocation.GetMousePosition();

			Win32Api.POINT p;
			if (Win32Api.GetCursorPos(out p))
			{
				IntPtr handleOfWindowBelowMouse = Win32Api.WindowFromPoint(p);
				if (handleOfWindowBelowMouse != IntPtr.Zero)
				{
					//var window = WPFHelper.FindVisualParent<Window>(frameworkElement);
					//if (window != null)
					//{
					//IntPtr windowHandle = window.GetHandle();
					if (elementRect.Contains(mousePos))
						Console.WriteLine("TRUE (type=" + frameworkElement.GetType().ToString() + "): Mousepos: " + mousePos.ToString() + ", elementRect: " + elementRect.ToString());
					else
						Console.WriteLine("FALSE (type=" + frameworkElement.GetType().ToString() + "): Mousepos: " + mousePos.ToString() + ", elementRect: " + elementRect.ToString());
					return /*windowHandle == handleOfWindowBelowMouse || */elementRect.Contains(mousePos);
					//}
				}
			}

			return elementRect.Contains(mousePos);
		}

		public static FrameworkElement GetFrameworkElementFromObjectSender(object sender)
		{
			return sender as FrameworkElement;
		}

		public static T GetFromObjectSender<T>(object sender) where T : class
		{
			FrameworkElement fe = GetFrameworkElementFromObjectSender(sender);
			if (fe == null) return null;
			return fe.DataContext as T;
		}
		public static void DoActionIfObtainedItemFromObjectSender<T>(object sender, Action<T> action,
			bool separateThread = false, bool waitUntilFinishIfSeparateThread = false) where T : class
		{
			T item = GetFromObjectSender<T>(sender);
			if (item == null) return;

			if (separateThread)
				ThreadingInterop.PerformOneArgFunctionSeperateThread<T>(
					action,
					item,
					waitUntilFinishIfSeparateThread);
			else
				action(item);
		}

		private static System.Windows.Forms.Timer _tempTimer;
		private static Action _actionToDelayLoad;
		public static void DelayLoadProcedure_CalledWithinWindowLoadedEvent(Action actionToBeDelayLoaded, int delayMilliseconds = 200)
		{
			if (actionToBeDelayLoaded == null) return;
			_actionToDelayLoad = actionToBeDelayLoaded;
			_tempTimer =
				new System.Windows.Forms.Timer();
			_tempTimer.Interval = delayMilliseconds;
			_tempTimer.Tick +=
				delegate
				{
					_tempTimer.Stop();
					_tempTimer.Dispose(); _tempTimer = null;
					_actionToDelayLoad();
				};
			_tempTimer.Start();
		}

		public static class MouseLocation
		{
			public static Point GetMousePosition()
			{
				Win32Api.POINT w32Mouse = new Win32Api.POINT();
				Win32Api.GetCursorPos(out w32Mouse);
				return new Point(w32Mouse.X, w32Mouse.Y);
			}
		}

		/// <summary>
		/// Copies a UI element to the clipboard as an image.
		/// </summary>
		/// <param name="element">The element to copy.</param>
		public static BitmapSource GetImageFromUIElement(FrameworkElement element, Size? fitToSize = null)
		{
			//http://elegantcode.com/2010/12/09/wpf-copy-uielement-as-image-to-clipboard/

			double width = element.ActualWidth;
			double height = element.ActualHeight;
			if (fitToSize.HasValue)
			{
				width = fitToSize.Value.Width;
				height = fitToSize.Value.Height;
			}

			if (width == 0 || height == 0)
				return null;

			if (width < 30)
			{
				double factor = 30D / width;
				width *= factor;
				height *= factor;
			}

			//double minratio = 0.1D;
			//if (width/height < minratio)
			//    width = height * minratio;
			//if (width < 30)
			//    width = 30;

			RenderTargetBitmap bmpCopied = new RenderTargetBitmap(
				(int)Math.Round(width),
				(int)Math.Round(height),
				96, 96, PixelFormats.Default);
			DrawingVisual dv = new DrawingVisual();
			using (DrawingContext dc = dv.RenderOpen())
			{
				VisualBrush vb = new VisualBrush(element);
				vb.Stretch = Stretch.UniformToFill;
				dc.DrawRectangle(vb, null, new Rect(new Point(), new Size(width, height)));
			}
			bmpCopied.Render(dv);
			//Clipboard.SetImage(bmpCopied);
			return bmpCopied;
		}

		public static BitmapImage BitmapSourceToBitmapImage(BitmapSource bitmapSource)
		{
			if (bitmapSource == null)
				return null;
			JpegBitmapEncoder encoder = new JpegBitmapEncoder();
			MemoryStream memoryStream = new MemoryStream();
			BitmapImage bImg = new BitmapImage();

			encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
			encoder.Save(memoryStream);

			bImg.BeginInit();
			bImg.StreamSource = new MemoryStream(memoryStream.ToArray());
			bImg.EndInit();

			memoryStream.Close();

			return bImg;
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
}
