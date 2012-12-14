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
				return !(value is bool) || ((bool)value) == false
					? (parameter is string && parameter.ToString().Equals("HideInsteadOfCollapse", StringComparison.InvariantCultureIgnoreCase)
						? Visibility.Hidden
						: Visibility.Collapsed)
					: Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class BooleanToOpacityConverter : IValueConverter
	{
		private const double cDefaultOpacityIfFail = 0.4;
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (parameter != null && parameter.ToString().StartsWith("opposite", StringComparison.InvariantCultureIgnoreCase))
			{
				//The parameter starts with 'opposite', must check if it has a double value after it, like 'opposite0.2'
				double notOpacitValue = cDefaultOpacityIfFail;
				if (parameter.ToString().Length > "opposite".Length)
				{
					string possibleDoubleVal = parameter.ToString().Substring("opposite".Length);
					double tmpdouble;
					if (double.TryParse(possibleDoubleVal, out tmpdouble))
						notOpacitValue = tmpdouble;
				}

				if (!(value is bool))
					return 1;
				if ((bool)value)
					return notOpacitValue;
				else
					return 1;
			}

			if (!(value is bool))
				return cDefaultOpacityIfFail;
			if ((bool)value)
				return 1;

			double tmpDouble;
			if (parameter == null || !double.TryParse(parameter.ToString(), out tmpDouble))
			{
				return cDefaultOpacityIfFail;
			}

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

	public class DateTimeToHumanfriendlyStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (!(value is DateTime))
				return value;

			const int SECOND = 1;
			const int MINUTE = 60 * SECOND;
			const int HOUR = 60 * MINUTE;
			const int DAY = 24 * HOUR;
			const int MONTH = 30 * DAY;

			TimeSpan ts = DateTime.Now.Subtract((DateTime)value);
			double delta = Math.Abs(ts.TotalSeconds);
			if (delta < 0)
			{
				return "not yet";
			}
			if (delta < 1 * MINUTE)
			{
				return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";
			}
			if (delta < 2 * MINUTE)
			{
				return "a minute ago";
			}
			if (delta < 45 * MINUTE)
			{
				return ts.Minutes + " minutes ago";
			}
			if (delta < 90 * MINUTE)
			{
				return "an hour ago";
			}
			if (delta < 24 * HOUR)
			{
				return ts.Hours + " hours ago";
			}
			if (delta < 48 * HOUR)
			{
				return "yesterday";
			}
			if (delta < 30 * DAY)
			{
				return ts.Days + " days ago";
			}
			if (delta < 12 * MONTH)
			{
				int months = System.Convert.ToInt32(Math.Floor((double)ts.Days / 30));
				return months <= 1 ? "one month ago" : months + " months ago";
			}
			else
			{
				int years = System.Convert.ToInt32(Math.Floor((double)ts.Days / 365));
				return years <= 1 ? "one year ago" : years + " years ago";
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class AddToDoubleValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null || parameter == null)
				return value;

			double dblVal;
			double parVal;

			if (!double.TryParse(value.ToString(), out dblVal))
				return value;
			if (!double.TryParse(parameter.ToString(), out parVal))
				return value;

			return dblVal + parVal;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}