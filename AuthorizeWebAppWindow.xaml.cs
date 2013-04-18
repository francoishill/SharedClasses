using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for AuthorizeWebAppWindow.xaml
	/// </summary>
	public partial class AuthorizeWebAppWindow : Window
	{
		string url;
		string expectedCallbackUrl;

		public AuthorizeWebAppWindow(string url, string expectedCallbackUrl)
		{
			InitializeComponent();

			this.url = url;
			this.expectedCallbackUrl = expectedCallbackUrl;
		}

		private void Window_Loaded_1(object sender, RoutedEventArgs e)
		{
			HideScriptErrors(webbrowser1, true);
			webbrowser1.Navigate(url);
		}

		public void HideScriptErrors(WebBrowser wb, bool Hide)
		{
			FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
			if (fiComWebBrowser == null) return;
			object objComWebBrowser = fiComWebBrowser.GetValue(wb);
			if (objComWebBrowser == null) return;
			objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { Hide });
		}

		private void webbrowser1_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
		{
			string url = e.Uri.ToString().Trim('/');
			if (url.Equals(expectedCallbackUrl, StringComparison.InvariantCultureIgnoreCase))
				this.DialogResult = true;
		}

		private void webbrowser1_Navigating_1(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
		{
			var url = e.Uri.ToString();
			if (url.Equals(expectedCallbackUrl, StringComparison.InvariantCultureIgnoreCase))
				this.DialogResult = true;
		}
	}
}
