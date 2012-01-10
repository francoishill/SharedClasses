using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
	/// Interaction logic for PropertiesEditor.xaml
	/// </summary>
	public partial class PropertiesEditor : Window
	{
		public PropertiesEditor(object[] objectsToView)
		{
			InitializeComponent();

			//tabControl1.ItemsSource = new ObservableCollection<object>() { selectedObject };
			propertyGrid1.SelectedObject = null;
			listBox1.Items.Clear();
			foreach (object obj in objectsToView)
				listBox1.Items.Add(obj);

			//propertyGrid1.SelectedObject = selectedObject;
		}

		private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count != 1)
				return;
			propertyGrid1.SelectedObject = e.AddedItems[0];
		}

	}

	public class TmpClass// : DependencyObject
	{
		//public static readonly DependencyProperty BaseUriProperty =
		//	DependencyProperty.Register("BaseUri", typeof(string), typeof(TmpClass));

		[Category("Test category")]
		[SettingAttribute("Please enter the base Uri for Visual Studio publishing, ie. code.google.com")]
		public String BaseUri { get; set; }
		public TmpClass()
		{
			BaseUri = "fjh.dyndns";
		}
	}


	public class FontStyleConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      FontStyle fs = (FontStyle)value;
      return fs == FontStyles.Italic;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value != null)
      {
        bool isSet = (bool)value;

        if (isSet)
        {
          return FontStyles.Italic;
        }
      }

      return FontStyles.Normal;
    }
  }

	public class FontWeightConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var fs = (FontWeight)value;
			return fs == FontWeights.Bold;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value != null)
			{
				bool isSet = (bool)value;

				if (isSet)
				{
					return FontWeights.Bold;
				}
			}

			return FontWeights.Normal;
		}
	}

	public class FontList : ObservableCollection<FontFamily>
	{
		public FontList()
		{
			foreach (var ff in Fonts.SystemFontFamilies)
			{
				Add(ff);
			}
		}
	}

	public class FontSizeList : ObservableCollection<double>
	{
		public FontSizeList()
		{
			Add(8);
			Add(9);
			Add(10);
			Add(11);
			Add(12);
			Add(14);
			Add(16);
			Add(18);
			Add(20);
		}
	}
}
