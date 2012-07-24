using System;
using System.IO;
using System.Security.Cryptography;

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

		/// <summary>
		/// Formats a string
		/// </summary>
		/// <param name="str">Itsself</param>
		/// <param name="args">The arguments replacing {0}, {1}, etc</param>
		/// <returns>The formatted string</returns>
		public static string Fmt(this string str, params object[] args)
		{
			return string.Format(str, args);
		}

		/// <summary>
		/// Function to get file impression in form of string from a file location.
		/// </summary>
		/// <param name="_fileName">File Path to get file impression.</param>
		/// <returns>Byte Array</returns>
		public static string FileToMD5Hash(this string _fileName)
		{
			if (!File.Exists(_fileName))
				return "[InvalidFilePath:" + _fileName + "]";

			using (var stream = new BufferedStream(File.OpenRead(_fileName), 1200000))
			{
				SHA256Managed sha = new SHA256Managed();
				byte[] checksum = sha.ComputeHash(stream);
				return BitConverter.ToString(checksum).Replace("-", string.Empty);
			}
		}
	}
}