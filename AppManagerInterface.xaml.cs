using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for AppManagerInterface.xaml
	/// </summary>
	public partial class AppManagerInterface : Window
	{
		//NamedPipesInterop.NamedPipeServer server;
		TempFormAppManager tempForm;

		public AppManagerInterface()
		{
			InitializeComponent();
		}

		private void Window_Loaded_1(object sender, RoutedEventArgs e)
		{
			foreach (string app in GlobalSettings.ApplicationManagerSettings.Instance.GetListedApplicationNames().OrderBy(a => a))
				WindowMessagesInterop.AddPredefinedApplication(app);

			listBoxRegisteredApplications.ItemsSource = WindowMessagesInterop.RegisteredApplications;//NamedPipesInterop.NamedPipeServer.GetConnectedClientApplications();

			//NamedPipesInterop.NamedPipeServer.GetConnectedClientApplications().CollectionChanged += delegate { };

			this.Hide();

			//TODO: This form is critically important, becuase this form has ShowInTaskbar=false, an additional form is required with ShowInTaskbar=true.
			tempForm = new TempFormAppManager();
			tempForm.Show();
			tempForm.Hide();

			//server = new NamedPipesInterop.NamedPipeServer(
			//	NamedPipesInterop.APPMANAGER_PIPE_NAME,
			//	ActionOnError: (e1) => { Console.WriteLine("Error: " + e1.GetException().Message); },
			//	ActionOnMessageReceived: (m, serv) => { Console.WriteLine("Message received, " + m.MessageType.ToString() + ": " + (m.AdditionalText ?? "")); }
			//	)
			//	.Start();
			//this.Closing += delegate { server.Stop(); };
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			SendMessageToFrameworkElementDataContext(sender as FrameworkElement, WindowMessagesInterop.MessageTypes.Show);//PipeMessageTypes.Show);
		}

		private void Button_Click_2(object sender, RoutedEventArgs e)
		{
			SendMessageToFrameworkElementDataContext(sender as FrameworkElement, WindowMessagesInterop.MessageTypes.Hide);//PipeMessageTypes.Hide);
		}

		private void Button_Click_3(object sender, RoutedEventArgs e)
		{
			SendMessageToFrameworkElementDataContext(sender as FrameworkElement, WindowMessagesInterop.MessageTypes.Close);//PipeMessageTypes.Close);
		}

		private void Button_Click_4(object sender, RoutedEventArgs e)
		{
			//NamedPipesInterop.NamedPipeServer.ClientApplication ra = (sender as FrameworkElement).DataContext as NamedPipesInterop.NamedPipeServer.ClientApplication;
			WindowMessagesInterop.RegisteredApp ra = (sender as FrameworkElement).DataContext as WindowMessagesInterop.RegisteredApp;
			if (ra == null)
				return;

			string errStarting;
			if (!ra.StartProcessWithName(out errStarting))
				UserMessages.ShowErrorMessage(errStarting);
		}

		private void Button_Click_5(object sender, RoutedEventArgs e)
		{
			//NamedPipesInterop.NamedPipeServer.ClientApplication ra = (sender as FrameworkElement).DataContext as NamedPipesInterop.NamedPipeServer.ClientApplication;
			WindowMessagesInterop.RegisteredApp ra = (sender as FrameworkElement).DataContext as WindowMessagesInterop.RegisteredApp;
			if (ra == null)
				return;

			ra.BroadcastStringMessage("Hallo there sexy");
		}

		private void Border_PreviewMouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)//Show textbox on double click
			{
				//NamedPipesInterop.NamedPipeServer.ClientApplication ra = GetClientApplicationFromFrameworkElementDataContext(sender as FrameworkElement);
				WindowMessagesInterop.RegisteredApp ra = GetClientApplicationFromFrameworkElementDataContext(sender as FrameworkElement);
				if (ra == null)
					return;
				ra.AppNameTextboxVisible = true;
			}
		}

		private void Border_LostKeyboardFocus_1(object sender, KeyboardFocusChangedEventArgs e)
		{
			//NamedPipesInterop.NamedPipeServer.ClientApplication ra = GetClientApplicationFromFrameworkElementDataContext(sender as FrameworkElement);
			WindowMessagesInterop.RegisteredApp ra = GetClientApplicationFromFrameworkElementDataContext(sender as FrameworkElement);
			if (ra == null)
				return;
			ra.AppNameTextboxVisible = false;
		}

		private void textboxappname_LostKeyboardFocus_1(object sender, KeyboardFocusChangedEventArgs e)
		{
			//NamedPipesInterop.NamedPipeServer.ClientApplication ra = GetClientApplicationFromFrameworkElementDataContext(sender as FrameworkElement);
			WindowMessagesInterop.RegisteredApp ra = GetClientApplicationFromFrameworkElementDataContext(sender as FrameworkElement);
			if (ra == null)
				return;
			ra.AppNameTextboxVisible = false;
		}

		//private NamedPipesInterop.NamedPipeServer.ClientApplication GetClientApplicationFromFrameworkElementDataContext(FrameworkElement frameworkElement)
		private WindowMessagesInterop.RegisteredApp GetClientApplicationFromFrameworkElementDataContext(FrameworkElement frameworkElement)
		{
			if (frameworkElement == null)
				return null;
			return frameworkElement.DataContext as WindowMessagesInterop.RegisteredApp;//NamedPipesInterop.NamedPipeServer.ClientApplication;
		}

		private void SendMessageToFrameworkElementDataContext(FrameworkElement frameworkElement, WindowMessagesInterop.MessageTypes messageType)
		//private void SendMessageToFrameworkElementDataContext(FrameworkElement frameworkElement, PipeMessageTypes messageType)
		{
			if (frameworkElement == null)
				return;
			WindowMessagesInterop.RegisteredApp ra = frameworkElement.DataContext as WindowMessagesInterop.RegisteredApp;
			if (ra == null)
				return;
			ra.AppNameTextboxVisible = false;
			ra.BroadCastMessage(messageType);

			//NamedPipesInterop.NamedPipeServer.ClientApplication ra = GetClientApplicationFromFrameworkElementDataContext(frameworkElement);
			//if (ra == null)
			//	return;
			//ra.SendMessage(messageType);
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
