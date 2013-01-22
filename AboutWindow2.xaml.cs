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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Interop;
using System.ComponentModel;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for About.xaml
	/// </summary>
	public partial class AboutWindow2 : Window
	{
		ObservableCollection<DisplayItem> ItemsToDisplay;

		public AboutWindow2(ObservableCollection<DisplayItem> ItemsToDisplay)
		{
			InitializeComponent();

			this.ItemsToDisplay = ItemsToDisplay;

			try
			{
				IconBitmapDecoder ibd = new IconBitmapDecoder(
					new Uri(@"pack://application:,,/app.ico", UriKind.RelativeOrAbsolute),
					BitmapCreateOptions.None,
					BitmapCacheOption.Default);
				this.Icon = ibd.Frames[0];
			}
			catch { }
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			listboxItemsToDisplay.ItemsSource = ItemsToDisplay;
			listboxItemsToDisplay.UpdateLayout();
			imageIcon.Source = this.Icon;
		}

		private void borderItemPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			FrameworkElement fwe = sender as FrameworkElement;
			if (fwe == null) return;
			DisplayItem di = fwe.DataContext as DisplayItem;
			if (di == null) return;
			di.GotoLink();
		}

		public static void ShowAboutWindow(ObservableCollection<DisplayItem> ItemsToDisplay, bool showmodal = true, Window owner = null)
		{
			int aboutWindowAddApplicationUrl;
			//Add the application's url to the AboutWindow2, like http://firepuma.com/ownapplication/quickaccess

			List<DisplayItem> forcedItemsToDisplay = new List<DisplayItem>();
			string thisAppname = System.IO.Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
			//If AutoUpdater is not installed, mention this in the AboutWindow2 and give user link to download it
			if (!AutoUpdating.IsAutoUpdaterInstalled())
				forcedItemsToDisplay.Add(new DisplayItem("Not keeping up to date", "Click to download AutoUpdater", AutoUpdating.GetDownloadlinkForLatestAutoUpdater()));
			forcedItemsToDisplay.Add(new DisplayItem("Application name", thisAppname));
			forcedItemsToDisplay.Add(new DisplayItem("Check out the website", "Click to open website", AutoUpdating.GetApplicationOnlineUrl(thisAppname)));

			for (int i = forcedItemsToDisplay.Count - 1; i >= 0; i--)
				ItemsToDisplay.Insert(0, forcedItemsToDisplay[i]);

			var win = new AboutWindow2(ItemsToDisplay)
			{
				Owner = owner
			};
			if (showmodal)
				win.ShowDialog();
			else
				win.Show();
		}

		private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				this.Close();
		}
	}

	public class DisplayItem : INotifyPropertyChanged
	{
		private string _name;
		public string Name { get { return _name; } set { _name = value; OnPropertyChanged("Name"); } }

		private string _displaytext;
		public string DisplayText { get { return _displaytext; } set { _displaytext = value; OnPropertyChanged("DisplayText"); } }

		private string _linkonclick;
		public string LinkOnClick { get { return _linkonclick; } set { _linkonclick = value; OnPropertyChanged("LinkOnClick"); } }

		private Cursor _currentcursor;
		public Cursor CurrentCursor { get { return _currentcursor; } set { _currentcursor = value; OnPropertyChanged("CurrentCursor"); } }


		public DisplayItem(string Name, string DisplayText, string LinkOnClick = null)
		{
			this.Name = Name;
			this.DisplayText = DisplayText;
			this.LinkOnClick = LinkOnClick;
			if (LinkOnClick != null) CurrentCursor = Cursors.Hand;
		}
		public void GotoLink()
		{
			if (LinkOnClick == null)
				return;
			Process.Start(LinkOnClick);
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		public void OnPropertyChanged(string propertyName) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
	}
}
