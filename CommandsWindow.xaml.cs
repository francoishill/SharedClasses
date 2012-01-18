using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for CommandsWindow.xaml
	/// </summary>
	public partial class CommandsWindow : Window
	{
		System.Windows.Forms.Form MainFormUsedForShuttingDownServers;
		public CommandsUsercontrol GetCommandsUsercontrol() { return commandsUsercontrol1; }

		public CommandsWindow(System.Windows.Forms.Form mainFormUsedForShuttingDownServers)
		{
			InitializeComponent();
			MainFormUsedForShuttingDownServers = mainFormUsedForShuttingDownServers;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct MARGINS
		{
			public int cxLeftWidth;      // width of left border that retains its size
			public int cxRightWidth;     // width of right border that retains its size
			public int cyTopHeight;      // height of top border that retains its size
			public int cyBottomHeight;   // height of bottom border that retains its size
		};

		[DllImport("DwmApi.dll")]
		private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

		private void CommandsWindow1_Loaded(object sender, RoutedEventArgs e)
		{
			WindowInteropHelper windowInteropHelper = new WindowInteropHelper(this);
			IntPtr myHwnd = windowInteropHelper.Handle;
			HwndSource mainWindowSrc = System.Windows.Interop.HwndSource.FromHwnd(myHwnd);

			mainWindowSrc.CompositionTarget.BackgroundColor = Color.FromArgb(0, 0, 0, 0);

			MARGINS margins = new MARGINS()
			{
				cxLeftWidth = -1,
				cxRightWidth = -1,
				cyBottomHeight = -1,
				cyTopHeight = -1
			};

			DwmExtendFrameIntoClientArea(myHwnd, ref margins);

			System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
			timer.Interval = 1;
			timer.Tick += delegate
			{
				timer.Stop();
				timer.Dispose(); timer = null;

				commandsUsercontrol1.InitializeTreeViewNodes(MainFormUsedForShuttingDownServers);
				//commandsUsercontrol1.InitializeTreeViewNodes(
				//	MainFormUsedForShuttingDownServers,
				//	true,
				//	delegate { this.Close(); },
				//	delegate { this.Hide(); });
			};
			timer.Start();

			commandsUsercontrol1.UpdateTaskbarOverlayIconForUnreadMessages();

			//TODO: Have a look at the commented out code for the WindowChrome
			//Style _style = null;
			//if (Microsoft.Windows.Shell.SystemParameters2.Current.IsGlassEnabled == true)
			//	_style = (Style)Resources["FractalStyle"];
			//this.Style = _style;
		}

		private void CommandsWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			//e.Cancel = true;
			//this.Hide();
			//Application.Current.MainWindow.Close();
		}

		private void CommandsWindow1_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
				DragMove();
		}

		private void CommandsWindow1_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			//this.Hide();
		}

		private void ThumbButtonInfo_Click(object sender, EventArgs e)
		{
			this.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
			this.TaskbarItemInfo.ProgressValue = 0.8;
		}

		//private void _OnShowSystemMenuCommand(object sender, ExecutedRoutedEventArgs e)
		//{
		//	Window _window = (Window)e.Parameter;
		//	Point _point = new Point(_window.Left + 24, _window.Top + 24);

		//	Microsoft.Windows.Shell.SystemCommands.ShowSystemMenu(_window, _point);
		//}

		//private void _OnSystemCommandCloseWindow(object sender, ExecutedRoutedEventArgs e)
		//{
		//	Microsoft.Windows.Shell.SystemCommands.CloseWindow((Window)e.Parameter);
		//}

		private void MinimizeToTrayUsercontrolButton_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
		}

		private void CloseUsercontrolButton_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
		}

		private void MinimizeToTrayUsercontrolButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			this.Hide();
		}

		private void CloseUsercontrolButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			this.Close();
		}

		private void CommandsWindow1_StateChanged(object sender, EventArgs e)
		{
			if (this.WindowState == System.Windows.WindowState.Minimized)
			{
				this.WindowState = System.Windows.WindowState.Normal;
				this.Hide();				
			}
		}
	}
}