using System;

namespace SharedClasses
{
	public static class StringExtensions
	{
		public static bool Equals(this string str, StringComparison comparisonType, params string[] EitherOfTheseValues)
		{
			foreach (string s in EitherOfTheseValues)
				if (str.Equals(s, comparisonType))
					return true;
			return false;
		}

		public static string InsertSpacesBeforeCamelCase(this string str)
		{
			if (str == null) return str;
			for (int i = str.Length - 1; i >= 1; i--)
			{
				if (str[i].ToString().ToUpper() == str[i].ToString())
					str = str.Insert(i, " ");
			}
			return str;
		}
	}
}