using System;
using System.IO;
using System.Reflection;

namespace SharedClasses
{
	public static class Helpers
	{
		public static bool GetEmbeddedResource(Predicate<string> predicateToValidateOn, out byte[] FileContentsBytes)
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				//Assembly objAssembly = Assembly.GetExecutingAssembly();
				try
				{
					string[] myResources = assembly.GetManifestResourceNames();
					foreach (string reso in myResources)
					{
						Console.WriteLine(reso);
						if (predicateToValidateOn(reso))
						{
							Stream stream = assembly.GetManifestResourceStream(reso);
							int length = (int)stream.Length;
							byte[] bytesOfDotnetCheckerDLL = new byte[length];
							stream.Read(bytesOfDotnetCheckerDLL, 0, length);
							stream.Close();
							FileContentsBytes = bytesOfDotnetCheckerDLL;
							bytesOfDotnetCheckerDLL = null;
							return true;
						}
					}
				}
				catch { }
			}
			FileContentsBytes = null;
			return false;
		}

		public static bool GetEmbeddedResource_FirstOneEndingWith(string EndOfFilename, out byte[] FileContentsBytes)
		{
			return GetEmbeddedResource(reso => reso.ToLower().EndsWith(EndOfFilename.ToLower()), out FileContentsBytes);
		}
	}
}