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
using System.Windows.Shapes;
using System.Diagnostics;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for AboutWindow.xaml
	/// </summary>
	public partial class AboutWindow : Window
	{
		public string applicationFullPath = null;

		public AboutWindow(string applicationFullPath)
		{
			InitializeComponent();
			stackpanelKeyAndValuePairs.Children.Clear();
			this.applicationFullPath = applicationFullPath;
		}

		public void AddKeyValue(string key, string value)
		{
			stackpanelKeyAndValuePairs.Children.Add(new Label()
			{
				Content = key,
				Tag = value,
				Template = this.Resources["tempOutlinedLabel"] as ControlTemplate
			});
		}

		public void AddAppName()
		{
			AddKeyValue("Application",  System.IO.Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().ProcessName));
		}

		public void AddVersion()
		{
			string versionfilePath = applicationFullPath + ".version";
			AddKeyValue("Version",
				System.IO.File.Exists(versionfilePath) ? System.IO.File.ReadAllText(versionfilePath)
				: "No version file found");
		}

		private static AboutWindow aboutWindow = null;
		public static void ShowAboutWindow()
		{
			if (aboutWindow == null)
			{
				Process currProc = Process.GetCurrentProcess();
				if (currProc == null)
					UserMessages.ShowWarningMessage("Cannot find current process");
				else
				{
					aboutWindow = new AboutWindow(currProc.MainModule.FileName);
					aboutWindow.applicationFullPath = currProc.MainModule.FileName;
					aboutWindow.Title = "About " + System.IO.Path.GetFileNameWithoutExtension(currProc.ProcessName);
					aboutWindow.AddAppName();
					aboutWindow.AddVersion();
					aboutWindow.ShowDialog();
				}
			}
			else
			{
				aboutWindow.BringIntoView();
				aboutWindow.Activate();
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
