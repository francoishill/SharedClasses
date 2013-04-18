using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for DatePickerWpf.xaml
	/// </summary>
	public partial class DatePickerWpf : Window
	{
		public DatePickerWpf()
		{
			InitializeComponent();
		}

		public static List<DateTime> ChooseDate()
		{
			var win = new DatePickerWpf();
			if (win.ShowDialog() == true)
				return win.calendar1.SelectedDates.ToList();
			else
				return null;
		}

		private void calendar1_SelectedDatesChanged_1(object sender, SelectionChangedEventArgs e)
		{
			if (calendar1.SelectedDates == null) return;
			labelStatus.Text = string.Format("There {0} {1} {2} selected",
				calendar1.SelectedDates.Count > 1 ? "are" : "is",
				calendar1.SelectedDates.Count,
				calendar1.SelectedDates.Count > 1 ? "dates" : "date");
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
			this.Close();
		}
	}

	public class WeekendDaysConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string text;
			var date = (DateTime)value;

			//the rule for coloring week days
			if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
			{
				text = "Weekend!";
			}
			else
			{
				text = null;
			}

			return text;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
