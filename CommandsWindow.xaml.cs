using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Reflection;
using System.IO;
using System.Diagnostics;

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

		private void CommandsWindow1_Loaded(object sender, RoutedEventArgs e)
		{
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
			Style _style = null;
			if (Microsoft.Windows.Shell.SystemParameters2.Current.IsGlassEnabled == true)
				_style = (Style)Resources["FractalStyle"];
			this.Style = _style;
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

		private void _OnShowSystemMenuCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Window _window = (Window)e.Parameter;
			Point _point = new Point(_window.Left + 24, _window.Top + 24);

			Microsoft.Windows.Shell.SystemCommands.ShowSystemMenu(_window, _point);
		}

		private void _OnSystemCommandCloseWindow(object sender, ExecutedRoutedEventArgs e)
		{
			Microsoft.Windows.Shell.SystemCommands.CloseWindow((Window)e.Parameter);
		}

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
	}
}