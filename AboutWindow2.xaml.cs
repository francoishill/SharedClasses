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
	}

	public class DisplayItem
	{
		public string Name { get; set; }
		public string Text { get; set; }
		private string LinkOnClick { get; set; }
		public Cursor CurrentCursor { get; private set; }

		public DisplayItem(string Name, string Text, string LinkOnClick = null)
		{
			this.Name = Name;
			this.Text = Text;
			this.LinkOnClick = LinkOnClick;
			if (LinkOnClick != null) CurrentCursor = Cursors.Hand;
		}
		public void GotoLink()
		{
			if (LinkOnClick == null)
				return;
			Process.Start(LinkOnClick);
		}
	}
}
