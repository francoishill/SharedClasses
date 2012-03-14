using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for AppManagerInterface.xaml
	/// </summary>
	public partial class AppManagerInterface : Window
	{
		NamedPipesInterop.NamedPipeServer server;

		public AppManagerInterface()
		{
			InitializeComponent();
		}

		private void Window_Loaded_1(object sender, RoutedEventArgs e)
		{
			//foreach (string app in GlobalSettings.ApplicationManagerSettings.Instance.GetListedApplicationNames())
			//	NamedPipesInterop.NamedPipeServer.AddtoPredefinedAvailableClientNames(app);
				//NamedPipesInterop.NamedPipeServer.ConnectedClientApplications.Add(new NamedPipesInterop.NamedPipeServer.ClientApplication(app, null));
			listBoxRegisteredApplications.ItemsSource = NamedPipesInterop.NamedPipeServer.GetConnectedClientApplications();

			//NamedPipesInterop.NamedPipeServer.GetConnectedClientApplications().CollectionChanged += delegate { };

			this.Hide();

			server = new NamedPipesInterop.NamedPipeServer(
			NamedPipesInterop.APPMANAGER_PIPE_NAME,
			ActionOnError: (e1) => { Console.WriteLine("Error: " + e1.GetException().Message); },
			ActionOnMessageReceived: (m, serv) => { Console.WriteLine("Message received, " + m.MessageType.ToString() + ": " + (m.AdditionalText ?? "")); }
			).Start();
			this.Closing += delegate { server.Stop(); };
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			SendMessageToFrameworkElementDataContext(sender as FrameworkElement, PipeMessageTypes.Show);
		}

		private void Button_Click_2(object sender, RoutedEventArgs e)
		{
			SendMessageToFrameworkElementDataContext(sender as FrameworkElement, PipeMessageTypes.Hide);
		}

		private void Button_Click_3(object sender, RoutedEventArgs e)
		{
			SendMessageToFrameworkElementDataContext(sender as FrameworkElement, PipeMessageTypes.Close);
		}

		private void Button_Click_4(object sender, RoutedEventArgs e)
		{
			NamedPipesInterop.NamedPipeServer.ClientApplication ca = (sender as FrameworkElement).DataContext as NamedPipesInterop.NamedPipeServer.ClientApplication;
			if (ca == null)
				return;

			string errStarting;
			if (!ca.StartProcessWithName(out errStarting))
				UserMessages.ShowErrorMessage(errStarting);
		}

		private void Border_PreviewMouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)//Show textbox on double click
			{
				NamedPipesInterop.NamedPipeServer.ClientApplication ca = GetClientApplicationFromFrameworkElementDataContext(sender as FrameworkElement);
				if (ca == null)
					return;
				ca.AppNameTextboxVisible = true;
			}
		}

		private void Border_LostKeyboardFocus_1(object sender, KeyboardFocusChangedEventArgs e)
		{
			NamedPipesInterop.NamedPipeServer.ClientApplication ca = GetClientApplicationFromFrameworkElementDataContext(sender as FrameworkElement);
			if (ca == null)
				return;
			ca.AppNameTextboxVisible = false;
		}

		private void textboxappname_LostKeyboardFocus_1(object sender, KeyboardFocusChangedEventArgs e)
		{
			NamedPipesInterop.NamedPipeServer.ClientApplication ca = GetClientApplicationFromFrameworkElementDataContext(sender as FrameworkElement);
			if (ca == null)
				return;
			ca.AppNameTextboxVisible = false;
		}

		private NamedPipesInterop.NamedPipeServer.ClientApplication GetClientApplicationFromFrameworkElementDataContext(FrameworkElement frameworkElement)
		{
			if (frameworkElement == null)
				return null;
			return frameworkElement.DataContext as NamedPipesInterop.NamedPipeServer.ClientApplication;
		}

		private void SendMessageToFrameworkElementDataContext(FrameworkElement frameworkElement, PipeMessageTypes messageType)
		{
			//Button button = sender as Button;
			//WindowMessagesInterop.RegisteredApp ra = button.DataContext as WindowMessagesInterop.RegisteredApp;
			NamedPipesInterop.NamedPipeServer.ClientApplication ca = GetClientApplicationFromFrameworkElementDataContext(frameworkElement);
			if (ca == null)
				return;
			ca.SendMessage(messageType);
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
			//if (tempForm != null)
			//	tempForm.Close();
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
