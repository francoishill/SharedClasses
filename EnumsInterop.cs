﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedClasses
{
	public static class EnumsInterop
	{
		/// <summary>
		/// Use this function if a enum has [Description] values to get the description.
		/// public enum TextureTileTypes
		/// {
		///     [System.ComponentModel.Description("Tile")]
		///     Tile,
		///     [Description("Clamp")]
		///     Clmp
		/// }
		/// </summary>
		/// <param name="value">The enum.whatever to get the description of.</param>
		/// <returns>The description of the enum.whatever.</returns>
		public static string GetEnumDescription<T>(this T value) where T : struct
		{
			System.Reflection.FieldInfo fi = value.GetType().GetField(value.ToString());
			System.ComponentModel.DescriptionAttribute[] attributes =
              (System.ComponentModel.DescriptionAttribute[])fi.GetCustomAttributes
				(typeof(System.ComponentModel.DescriptionAttribute), false);
			return (attributes.Length > 0) ? attributes[0].Description : value.ToString();
		}

		public static List<string> GetStringListOfEnumNames(this Type EnumType)
		{
			List<string> tmplist = new List<string>();
			foreach (string s in Enum.GetNames(EnumType))
				tmplist.Add(s);
			return tmplist;
		}

		public static Dictionary<T, string> GetDictionaryWithDescriptions<T>(this Type enumType) where T : struct
		{
			Dictionary<T, string> tmpdict = new Dictionary<T, string>();
			foreach (T en in Enum.GetValues(enumType))
				tmpdict.Add(en, en.GetEnumDescription());
			return tmpdict;
		}
	}
}
