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
	}
}