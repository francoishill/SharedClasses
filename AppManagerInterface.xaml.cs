using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using ApplicationManager;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for AppManagerInterface.xaml
	/// </summary>
	public partial class AppManagerInterface : Window
	{
		TempForm tempForm;

		public AppManagerInterface()
		{
			InitializeComponent();
		}

		private void Window_Loaded_1(object sender, RoutedEventArgs e)
		{
			//GenericSettings.EnsureAllSettingsAreInitialized();

			foreach (string app in GlobalSettings.ApplicationManagerSettings.Instance.GetListedApplicationNames())
				WindowMessagesInterop.RegisteredApplications.Add(app);//(IntPtr)Process.GetCurrentProcess().Id);
			listBoxRegisteredApplications.ItemsSource = WindowMessagesInterop.RegisteredApplications;
			//System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(this);
			this.Hide();

			//TODO: This form is critically important, becuase this form has ShowInTaskbar=false, an additional form is required with ShowInTaskbar=true.
			tempForm = new TempForm();
			tempForm.Show();
			tempForm.Hide();
		}

		//protected override void OnSourceInitialized(EventArgs e)
		//{
		//	base.OnSourceInitialized(e);
		//	HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
		//	source.AddHook(WndProc);
		//}

		//private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		//{
		//	string errMsg;
		//	if (!WindowMessagesInterop.ApplicationManagerHandleMessage(msg, wParam, lParam, out errMsg))
		//		MessageBox.Show(errMsg);
		//	return IntPtr.Zero;
		//}

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

		private void Button_Click_4(object sender, RoutedEventArgs e)
		{
			Button button = sender as Button;
			WindowMessagesInterop.RegisteredApp ra = button.DataContext as WindowMessagesInterop.RegisteredApp;
			if (ra == null)
				return;
			string errStarting;
			if (!ra.Start(out errStarting))
				UserMessages.ShowErrorMessage(errStarting);
		}

		private void Window_StateChanged_1(object sender, EventArgs e)
		{
			if (this.WindowState == System.Windows.WindowState.Normal)
				PositionWindowBottomRight();
		}

		private void Window_IsVisibleChanged_1(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (this.Visibility == System.Windows.Visibility.Visible)
				PositionWindowBottomRight();
		}

		private void Window_SizeChanged_1(object sender, SizeChangedEventArgs e)
		{
			PositionWindowBottomRight();
		}

		private void PositionWindowBottomRight()
		{
			if (this.WindowState != System.Windows.WindowState.Minimized)
			{
				this.Left = System.Windows.SystemParameters.WorkArea.Right - this.ActualWidth;
				this.Top = System.Windows.SystemParameters.WorkArea.Bottom - this.ActualHeight;
			}
		}

		private void ShowNow()
		{
			this.Show();
			this.BringIntoView();
			this.Activate();
		}

		private void OnMenuItemShowClick(object sender, EventArgs e)
		{
			this.ShowNow();
		}

		private void OnMenuItemExitClick(object sender, EventArgs e)
		{
			if (tempForm != null)
				tempForm.Close();
			this.Close();
		}

		private void OnMenuItem_MouseClick(object sender, MouseButtonEventArgs e)
		{
			if (this.IsVisible)
				this.Hide();
			else
				this.ShowNow();
		}
	}

	#region Converters
	public class BoolIsAliveToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (!(value is bool) || !(bool)value)
				return new SolidColorBrush(Colors.LightGray);
			else
				return new SolidColorBrush(Colors.Green);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
	#endregion Converters
}
