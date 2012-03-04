using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for AppManagerInterface.xaml
	/// </summary>
	public partial class AppManagerInterface : Window
	{
		public AppManagerInterface()
		{
			System.Windows.Forms.Application.EnableVisualStyles();
			InitializeComponent();
		}

		private void Window_Loaded_1(object sender, RoutedEventArgs e)
		{
			listBoxRegisteredApplications.ItemsSource = WindowMessagesInterop.RegisteredApplications;
			System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(this);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
			source.AddHook(WndProc);
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			string errMsg;
			if (!WindowMessagesInterop.ApplicationManagerHandleMessage(msg, wParam, lParam, out errMsg))
				MessageBox.Show(errMsg);
			return IntPtr.Zero;
		}

		private void Border_PreviewMouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
			{
				Border border = sender as Border;
				if (border == null)
					return;
				WindowMessagesInterop.RegisteredApp ra = border.DataContext as WindowMessagesInterop.RegisteredApp;
				if (ra == null)
					return;
				ra.AppNameTextboxVisible = true;
			}
		}

		private void Border_LostKeyboardFocus_1(object sender, KeyboardFocusChangedEventArgs e)
		{
			Border border = sender as Border;
			if (border == null)
				return;
			WindowMessagesInterop.RegisteredApp ra = border.DataContext as WindowMessagesInterop.RegisteredApp;
			if (ra == null)
				return;
			ra.AppNameTextboxVisible = false;
		}

		private void textboxappname_LostKeyboardFocus_1(object sender, KeyboardFocusChangedEventArgs e)
		{
			TextBox textbox = sender as TextBox;
			if (textbox == null)
				return;
			WindowMessagesInterop.RegisteredApp ra = textbox.DataContext as WindowMessagesInterop.RegisteredApp;
			if (ra == null)
				return;
			ra.AppNameTextboxVisible = false;
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			Button button = sender as Button;
			WindowMessagesInterop.RegisteredApp ra = button.DataContext as WindowMessagesInterop.RegisteredApp;
			if (ra == null)
				return;
			ra.BroadCastMessage(WindowMessagesInterop.MessageTypes.Show);
		}

		private void Button_Click_2(object sender, RoutedEventArgs e)
		{
			Button button = sender as Button;
			WindowMessagesInterop.RegisteredApp ra = button.DataContext as WindowMessagesInterop.RegisteredApp;
			if (ra == null)
				return;
			ra.BroadCastMessage(WindowMessagesInterop.MessageTypes.Hide);
		}

		private void Button_Click_3(object sender, RoutedEventArgs e)
		{
			Button button = sender as Button;
			WindowMessagesInterop.RegisteredApp ra = button.DataContext as WindowMessagesInterop.RegisteredApp;
			if (ra == null)
				return;
			ra.BroadCastMessage(WindowMessagesInterop.MessageTypes.Close);
		}
	}
}
