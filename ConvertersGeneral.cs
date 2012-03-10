using System;
using System.Windows;
using System.Windows.Data;

namespace SharedClasses
{
	public class BooleanToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (parameter is string && parameter.ToString() == "opposite")
				return !(value is bool) || ((bool)value) == false ? Visibility.Visible : Visibility.Collapsed;
			else
				return !(value is bool) || ((bool)value) == false ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class BooleanToOpacityConverter : IValueConverter
	{
		private const double DefaultOpacityIfFail = 0.4;
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (!(value is bool))
				return DefaultOpacityIfFail;
			if ((bool)value)
				return 1;

			double tmpDouble;
			if (parameter == null || !double.TryParse(parameter.ToString(), out tmpDouble))
				return DefaultOpacityIfFail;

			return tmpDouble;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class BooleanToTextWrappingConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (parameter is string && parameter.ToString() == "opposite")
				return !(value is bool) || ((bool)value) == false ? TextWrapping.WrapWithOverflow : TextWrapping.NoWrap;
			else
				return !(value is bool) || ((bool)value) == false ? TextWrapping.NoWrap : TextWrapping.WrapWithOverflow;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class OppositeBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (!(value is bool))
				return false;
			else
				return !(bool)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}