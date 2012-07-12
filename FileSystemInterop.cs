using System.IO;
using System;
public class FileSystemInterop
{
	/// <summary>
	/// Because it uses a ref keyword it will convert the input argument itsself, but it also returns the result for more flexible use.
	/// </summary>
	/// <param name="FolderOrFileName">The reference of the string to remove the backslashes from.</param>
	/// <returns></returns>
	public static string RemoveLeadingAndTrailingBackslashes(ref string FolderOrFileName)
	{
		if (FolderOrFileName == null) return "";
		while (FolderOrFileName.StartsWith("\\") && FolderOrFileName.Length > 0) FolderOrFileName = FolderOrFileName.Substring(1);
		while (FolderOrFileName.EndsWith("\\")) FolderOrFileName = FolderOrFileName.Substring(0, FolderOrFileName.Length - 1);
		return FolderOrFileName;
	}

	private const string EncodedCharStart = "[{_";
	private const string EncodedCharEnd = "_}]";
	public static string FilenameEncodeToValid(string wantedFilename)
	{
		string result = wantedFilename;
		var invalidChars = Path.GetInvalidFileNameChars();
		foreach (char c in invalidChars)
			result = result.Replace(c.ToString(), string.Format("{0}{1}{2}", EncodedCharStart, (int)c, EncodedCharEnd));
		return result;
	}

	public static string FilenameDecodeToValid(string encodedFilename)
	{
		string result = encodedFilename;
		if (encodedFilename.Split(new string[] { EncodedCharStart }, StringSplitOptions.None).Length != encodedFilename.Split(new string[] { EncodedCharEnd }, StringSplitOptions.None).Length)
			UserMessages.ShowWarningMessage("Cannot decode filename: " + encodedFilename);
		else
		{
			var invalidChars = Path.GetInvalidFileNameChars();
			foreach (char c in invalidChars)
				result = result.Replace(string.Format("{0}{1}{2}", EncodedCharStart, (int)c, EncodedCharEnd), c.ToString());
		}
		return result;
	}
}