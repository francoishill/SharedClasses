using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for HighResourceUsageSettingsWindow.xaml
	/// </summary>
	public partial class HighResourceUsageSettingsWindow : Window
	{
		public HighResourceUsageSettingsWindow()
		{
			InitializeComponent();

			var settings = SettingsSimple.HighResourceUsageSettings.Instance;
			this.Title = "High resources usage settings for " + OwnAppsShared.GetApplicationName();

			this.DataContext = settings;
		}

		public static void ShowWindowDialog()
		{
			var tmpwin = new HighResourceUsageSettingsWindow();
			tmpwin.ShowDialog();
		}
	}
}
