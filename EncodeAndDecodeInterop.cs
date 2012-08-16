using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
//using System.Windows.Forms;

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
			///// <summary>
			///// None is not a encoding type but just means no type chosen yet.
			///// </summary>
			//None
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
				//else { MessageBox.Show("No encoding type selected", "No type", MessageBoxButtons.OK, MessageBoxIcon.Error); }
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
			//else return EncodingType.None;
			else return EncodingType.ASCII;
		}

		private const string cDefaultHexChars = "0123456789ABCDEF";

		public static bool EncodeFileToHex(string originalFilePath, string outputFilePath, Action<string> actionOnError)
		{
			using (IndeterminateProgress min = new IndeterminateProgress("File2Hex: " + Path.GetFileName(originalFilePath), true))
			{
				bool cancelled = false;
				min.onCancel += delegate { cancelled = true; };

				int bufferlength = 1024 * 1024 * 10;//10MB
				byte[] buffer = new byte[bufferlength];
				using (FileStream fin = new FileStream(originalFilePath, FileMode.Open))
				using (StreamWriter sw = new StreamWriter(outputFilePath, false))
				{
					int actualread = fin.Read(buffer, 0, bufferlength);
					while (actualread > 0)
					{
						sw.Write(EncodeBytesToHex(buffer, 0, actualread, actionOnError));
						actualread = fin.Read(buffer, 0, bufferlength);

						if (cancelled)
						{
							return false;
						}
					}
					return true;
				}
			}
		}

		public static string EncodeBytesToHex(byte[] bytesToEncode, int offset, int count, Action<string> actionOnError, string Hex16CharactersToUse = null)
		{
			string _16charsToUse = Hex16CharactersToUse;
			if (_16charsToUse == null)
				_16charsToUse = cDefaultHexChars;

			StringBuilder tmpstr = new StringBuilder();
			//foreach (byte b in bytesToEncode)
			for (int i = offset; i < offset + count; i++)
			{
				if (i >= bytesToEncode.Length)
					continue;

				byte b = bytesToEncode[i];

				int remainder;
				int div;
				try
				{
					/*if (b == 8211) div = Math.DivRem((int)'-', 16, out remainder);
					else */
					div = Math.DivRem(b, 16, out remainder);
					tmpstr.Append(_16charsToUse[div].ToString() + _16charsToUse[remainder].ToString());
				}
				catch (Exception exc)
				{
					actionOnError("Error, could not encode hex, byte " + b.ToString() + ": " + Environment.NewLine + exc.Message);
					//UserMessages.ShowErrorMessage("Error, could not encode hex, byte " + b.ToString() + ": " + Environment.NewLine + exc.Message, "Exception error");
				}
			}
			return tmpstr.ToString();
		}

		public static string EncodeStringHex(string StringToEncode, Action<string> actionOnError, string Hex16CharactersToUse = null)
		{
			string _16charsToUse = Hex16CharactersToUse;
			if (_16charsToUse == null)
				_16charsToUse = cDefaultHexChars;

			string tmpstr = "";
			foreach (char c in StringToEncode.ToCharArray())
			{
				int remainder;
				int div;
				try
				{
					if ((int)c == 8211) div = Math.DivRem((int)'-', 16, out remainder);
					else div = Math.DivRem((int)c, 16, out remainder);
					tmpstr += _16charsToUse[div].ToString() + _16charsToUse[remainder].ToString();
				}
				catch (Exception exc)
				{
					actionOnError("Error, could not encode hex, char " + c.ToString() + ", (int)char = " + (int)c + ": " + Environment.NewLine + exc.Message);
					//UserMessages.ShowErrorMessage("Error, could not encode hex, char " + c.ToString() + ", (int)char = " + (int)c + ": " + Environment.NewLine + exc.Message, "Exception error");
				}
			}
			return tmpstr;
		}

		public static bool DecodeFileFromHex(string hexfilepath, string outputfilepath)
		{
			using (IndeterminateProgress min = new IndeterminateProgress("FileFromHex: " + Path.GetFileName(hexfilepath), true))
			{
				bool cancelled = false;
				min.onCancel += delegate { cancelled = true; };

				int bufferlength = 1024 * 1024 * 10;//10MB
				char[] buffer = new char[bufferlength];
				using (StreamReader sread = new StreamReader(hexfilepath))
				using (FileStream fwrite = new FileStream(outputfilepath, FileMode.Create))
				{
					int actualread = sread.ReadBlock(buffer, 0, bufferlength);
					while (actualread > 0)
					{
						byte[] bytes = DecodeBytesFromHex(new string(buffer), 0, actualread);
						fwrite.Write(bytes, 0, bytes.Length);
						//sw.Write(EncodeBytesToHex(buffer, 0, actualread));
						actualread = sread.ReadBlock(buffer, 0, bufferlength);

						if (cancelled)
						{
							return false;
						}
					}
					return true;
				}
			}
		}

		public static byte[] DecodeBytesFromHex(string StringToDecode, int offset, int count, string Hex16CharactersToUse = null)//, StringTypeEnum StringType)
		{
			if (StringToDecode == null)
				return new byte[0];

			byte[] result = new byte[(int)(count / 2)];//(int)(StringToDecode.Length / 2)];

			string _16charsToUse = Hex16CharactersToUse;
			if (_16charsToUse == null)
				_16charsToUse = cDefaultHexChars;

			//for (int i = 0; i <= StringToDecode.Length - 2; i = i + 2)
			for (int i = offset; i <= offset + count - 2; i = i + 2)
			{
				if (i >= StringToDecode.Length)
					continue;

				result[(i - offset) / 2] = (byte)(_16charsToUse.IndexOf(StringToDecode[i]) * 16 + _16charsToUse.IndexOf(StringToDecode[i + 1]));
			}
			return result;
		}
		

		//enum StringTypeEnum { Regex, Username, Password };
		public static string DecodeStringHex(string StringToDecode, string Hex16CharactersToUse = null)//, StringTypeEnum StringType)
		{
			string _16charsToUse = Hex16CharactersToUse;
			if (_16charsToUse == null)
				_16charsToUse = cDefaultHexChars;

			string tmpstr = "";
			for (int i = 0; i <= StringToDecode.Length - 2; i = i + 2)
			{
				tmpstr += (char)(_16charsToUse.IndexOf(StringToDecode[i]) * 16 + _16charsToUse.IndexOf(StringToDecode[i + 1]));
			}
			return tmpstr;
		}
	}
}
