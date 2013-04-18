using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SharedClasses
{
	public static class EncryptionInterop
	{
		public static byte[] EncryptBytes(byte[] inputBytes, string passPhrase, string saltValue)
		{
			RijndaelManaged RijndaelCipher = new RijndaelManaged();

			RijndaelCipher.Mode = CipherMode.CBC;
			byte[] salt = Encoding.ASCII.GetBytes(saltValue);
			PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, salt, "SHA1", 2);

			ICryptoTransform Encryptor = RijndaelCipher.CreateEncryptor(password.GetBytes(32), password.GetBytes(16));

			MemoryStream memoryStream = new MemoryStream();
			CryptoStream cryptoStream = new CryptoStream(memoryStream, Encryptor, CryptoStreamMode.Write);
			cryptoStream.Write(inputBytes, 0, inputBytes.Length);
			cryptoStream.FlushFinalBlock();
			byte[] CipherBytes = memoryStream.ToArray();

			memoryStream.Close();
			cryptoStream.Close();

			return CipherBytes;
		}

		// Example usage: DecryptBytes(encryptedBytes, "SensitivePhrase", "SodiumChloride");
		public static byte[] DecryptBytes(byte[] inputBytes, string passPhrase, string saltValue)
		{
			RijndaelManaged RijndaelCipher = new RijndaelManaged();

			RijndaelCipher.Mode = CipherMode.CBC;
			byte[] salt = Encoding.ASCII.GetBytes(saltValue);
			PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, salt, "SHA1", 2);

			ICryptoTransform Decryptor = RijndaelCipher.CreateDecryptor(password.GetBytes(32), password.GetBytes(16));

			MemoryStream memoryStream = new MemoryStream(inputBytes);
			CryptoStream cryptoStream = new CryptoStream(memoryStream, Decryptor, CryptoStreamMode.Read);
			byte[] plainBytes = new byte[inputBytes.Length];

			int DecryptedCount = cryptoStream.Read(plainBytes, 0, plainBytes.Length);
			byte[] paddedPlainBytes = new byte[DecryptedCount];
			for (int i = 0; i < DecryptedCount; i++)
				paddedPlainBytes[i] = plainBytes[i];

			memoryStream.Close();
			cryptoStream.Close();

			return paddedPlainBytes;
		}

		public static string SimpleTripleDesEncrypt(string Data, string keystring)
		{
			byte[] key = Encoding.ASCII.GetBytes(keystring);
			byte[] iv = Encoding.ASCII.GetBytes("password");
			byte[] data = Encoding.ASCII.GetBytes(Data);
			byte[] enc = new byte[0];
			TripleDES tdes = TripleDES.Create();
			tdes.IV = iv;
			tdes.Key = key;
			tdes.Mode = CipherMode.CBC;
			tdes.Padding = PaddingMode.Zeros;//This causes \0 bytes to be padded to end, to make up intervals of 8 characters?
			ICryptoTransform ict = tdes.CreateEncryptor();
			enc = ict.TransformFinalBlock(data, 0, data.Length);
			return ByteArrayToString(enc);
		}

		public static string SimpleTripleDesDecrypt(string Data, string keystring)
		{
			byte[] key = Encoding.ASCII.GetBytes(keystring);
			byte[] iv = Encoding.ASCII.GetBytes("password");
			byte[] data = StringToByteArray(Data);
			byte[] enc = new byte[0];
			TripleDES tdes = TripleDES.Create();
			tdes.IV = iv;
			tdes.Key = key;
			tdes.Mode = CipherMode.CBC;
			tdes.Padding = PaddingMode.Zeros;//This causes \0 bytes to be padded to end, to make up intervals of 8 characters?
			ICryptoTransform ict = tdes.CreateDecryptor();
			enc = ict.TransformFinalBlock(data, 0, data.Length);
			//return Encoding.ASCII.GetString(enc);
			return Encoding.ASCII.GetString(enc).TrimEnd('\0');
		}

		public static string ByteArrayToString(byte[] ba)
		{
			string hex = BitConverter.ToString(ba);
			return hex.Replace("-", "");
		}

		public static string StringToHex(string stringIn)
		{
			return ByteArrayToString(Encoding.Default.GetBytes(stringIn));
		}

		public static byte[] StringToByteArray(String hex)
		{
			int NumberChars = hex.Length;
			byte[] bytes = new byte[NumberChars / 2];
			for (int i = 0; i < NumberChars; i += 2)
				bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			return bytes;
		}

		public static string HexToString(string hexIn)
		{
			return Encoding.Default.GetString(StringToByteArray(hexIn));
		}
	}
}