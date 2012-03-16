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
	}
}