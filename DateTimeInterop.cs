using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedClasses
{
	public static class DateTimeInterop
	{
		internal static String[] MonthNamesFull = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
		internal static String[] MonthNamesFirstTrheeLetters = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

		internal static String[] WeekdayNamesFull = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
		internal static String[] WeekdayNamesFirstThreeLetters = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
		internal static String[] WeekdayNamesFirstTwoLetters = { "Mo", "Tu", "We", "Th", "Fr", "Sa", "Su" };


		/// <summary>
		/// A string array of the full twelve month names.
		/// </summary>
		/// <returns></returns>
		public static String[] GetMonthNamesFull() { return MonthNamesFull; }
		/// <summary>
		/// A string array of the first three letters of the twelve month names.
		/// </summary>
		/// <returns></returns>
		public static String[] GetMonthNamesFirstTrheeLetters() { return MonthNamesFirstTrheeLetters; }

		/// <summary>
		/// A string array of the full seven weekday names.
		/// </summary>
		/// <returns></returns>
		public static String[] GetWeekdayNamesFull() { return WeekdayNamesFull; }
		/// <summary>
		/// A string array of the first three letters of the seven weekday names.
		/// </summary>
		/// <returns></returns>
		public static String[] GetWeekdayNamesFirstThreeLetters() { return WeekdayNamesFirstThreeLetters; }
		/// <summary>
		/// A string array of the first two letters of the seven weekday names.
		/// </summary>
		/// <returns></returns>
		public static String[] GetWeekdayNamesFirstTwoLetters() { return WeekdayNamesFirstTwoLetters; }

		internal static int ReturnOne(String s)
		{
			return 1;
		}

		/// <summary>
		/// Convert an integer to the number of digits (i.e. if the number is 12 and required digits is 4 then it will return a string = "0012").
		/// </summary>
		/// <param name="Number">The number.</param>
		/// <param name="NumDigitsRequired">Number of required digits of the number.</param>
		/// <returns>The number converted to required digits by adding zeros.</returns>
		public static String AddZeros(int Number, int NumDigitsRequired)
		{
			String tmpStr = "";

			if (Number.ToString().ToCharArray().Length > NumDigitsRequired)
			{
				System.Windows.Forms.MessageBox.Show("The entered number cannot have more digits than the Maximum", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				return null;
			}
			else
			{
				if (Number.ToString().ToCharArray().Length == NumDigitsRequired) return Number.ToString();
				else
				{
					tmpStr = Number.ToString();
					while (tmpStr.Length < NumDigitsRequired)
						tmpStr = "0" + tmpStr;
					return tmpStr;
				}
			}
			//return null;
		}
	}
}
