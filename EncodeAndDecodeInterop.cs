using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharedClasses
{
	public class EncodeAndDecodeInterop
	{
		/// <summary>
		/// The encoding or decoding type.
		/// </summary>
		public enum EncodingType
		{
			/// <summary>
			/// ASCII encoding or decoding.
			/// </summary>
			ASCII,
			/// <summary>
			/// Unicode encoding or decoding.
			/// </summary>
			Unicode,
			/// <summary>
			/// UTF32 encoding or decoding.
			/// </summary>
			UTF32,
			/// <summary>
			/// UTF7 encoding or decoding.
			/// </summary>
			UTF7,
			/// <summary>
			/// UTF8 encoding or decoding.
			/// </summary>
			UTF8,
			/// <summary>
			/// None is not a encoding type but just means no type chosen yet.
			/// </summary>
			None
		}
		//public enum EncodingType { UTF8, None }

		/// <summary>
		/// Encode a string using the specified encoding type.
		/// </summary>
		/// <param name="data">The original string which must be encoded.</param>
		/// <param name="encodingType">The encoding type to use.</param>
		/// <returns>The encoded string.</returns>
		public static string EncodeString(string data, EncodingType encodingType)
		{
			if (data == null)
				return null;

			try
			{
				byte[] encData_byte = new byte[data.Length];
				if (encodingType == EncodingType.ASCII) encData_byte = System.Text.Encoding.ASCII.GetBytes(data);
				else if (encodingType == EncodingType.Unicode) encData_byte = System.Text.Encoding.Unicode.GetBytes(data);
				else if (encodingType == EncodingType.UTF32) encData_byte = System.Text.Encoding.UTF32.GetBytes(data);
				else if (encodingType == EncodingType.UTF7) encData_byte = System.Text.Encoding.UTF7.GetBytes(data);
				else if (encodingType == EncodingType.UTF8) encData_byte = System.Text.Encoding.UTF8.GetBytes(data);
				else { MessageBox.Show("No encoding type selected", "No type", MessageBoxButtons.OK, MessageBoxIcon.Error); }
				string encodedData = Convert.ToBase64String(encData_byte);
				return encodedData;
			}
			catch (Exception e)
			{
				throw new Exception("Error in base64Encode" + e.Message);
			}
		}

		/// <summary>
		/// Decode a string using the specified decoding type.
		/// </summary>
		/// <param name="data">The original string which must be decoded.</param>
		/// <param name="decodingType">The decoding type to use.</param>
		/// <returns>The decoded string.</returns>
		public static string DecodeString(string data, EncodingType decodingType)
		{
			if (data == null)
				return null;

			try
			{
				if (decodingType == EncodingType.ASCII)
				{
					System.Text.ASCIIEncoding ASCIIEncoder = new ASCIIEncoding();
					System.Text.Decoder ASCIIDecoder = ASCIIEncoder.GetDecoder();

					byte[] todecode_byte = Convert.FromBase64String(data);
					int charCount = ASCIIDecoder.GetCharCount(todecode_byte, 0, todecode_byte.Length);
					char[] decoded_char = new char[charCount];
					ASCIIDecoder.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
					string result = new String(decoded_char);
					return result;
				}
				else if (decodingType == EncodingType.Unicode)
				{
					System.Text.UnicodeEncoding UnicodeEncoder = new UnicodeEncoding();
					System.Text.Decoder UnicodeDecoder = UnicodeEncoder.GetDecoder();

					byte[] todecode_byte = Convert.FromBase64String(data);
					int charCount = UnicodeDecoder.GetCharCount(todecode_byte, 0, todecode_byte.Length);
					char[] decoded_char = new char[charCount];
					UnicodeDecoder.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
					string result = new String(decoded_char);
					return result;
				}
				else if (decodingType == EncodingType.UTF32)
				{
					System.Text.UTF32Encoding UTF32Encoder = new UTF32Encoding();
					System.Text.Decoder UTF32Decoder = UTF32Encoder.GetDecoder();

					byte[] todecode_byte = Convert.FromBase64String(data);
					int charCount = UTF32Decoder.GetCharCount(todecode_byte, 0, todecode_byte.Length);
					char[] decoded_char = new char[charCount];
					UTF32Decoder.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
					string result = new String(decoded_char);
					return result;
				}
				else if (decodingType == EncodingType.UTF7)
				{
					System.Text.UTF7Encoding UTF7Encoder = new UTF7Encoding();
					System.Text.Decoder UTF7Decoder = UTF7Encoder.GetDecoder();

					byte[] todecode_byte = Convert.FromBase64String(data);
					int charCount = UTF7Decoder.GetCharCount(todecode_byte, 0, todecode_byte.Length);
					char[] decoded_char = new char[charCount];
					UTF7Decoder.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
					string result = new String(decoded_char);
					return result;
				}
				if (decodingType == EncodingType.UTF8)
				{
					System.Text.UTF8Encoding UTF8Encoder = new System.Text.UTF8Encoding();
					System.Text.Decoder UTF8Decoder = UTF8Encoder.GetDecoder();

					byte[] todecode_byte = Convert.FromBase64String(data);
					int charCount = UTF8Decoder.GetCharCount(todecode_byte, 0, todecode_byte.Length);
					char[] decoded_char = new char[charCount];
					UTF8Decoder.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
					string result = new String(decoded_char);
					return result;
				}
				return null;
			}
			catch (Exception e)
			{
				throw new Exception("Error in base64Decode" + e.Message);
			}
		}

		private static EncodingType GetEncodingType(String TypeName)
		{
			if (TypeName.ToUpper() == EncodingType.ASCII.ToString().ToUpper()) return EncodingType.ASCII;
			else if (TypeName.ToUpper() == EncodingType.Unicode.ToString().ToUpper()) return EncodingType.Unicode;
			else if (TypeName.ToUpper() == EncodingType.UTF32.ToString().ToUpper()) return EncodingType.UTF32;
			else if (TypeName.ToUpper() == EncodingType.UTF7.ToString().ToUpper()) return EncodingType.UTF7;
			if (TypeName.ToUpper() == EncodingType.UTF8.ToString().ToUpper()) return EncodingType.UTF8;
			else return EncodingType.None;
		}
	}
}
