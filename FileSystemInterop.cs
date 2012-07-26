using System.IO;
using System;
using System.Windows.Forms;
using SharedClasses;
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

	public static bool CanOpenFileForReading(string fullpath, out string errorIfCannotOpen)
	{
		if (!File.Exists(fullpath))
		{
			errorIfCannotOpen = "File deleted before processed: " + fullpath;
			return false;
		}
		else
		{
			try
			{
				File.OpenRead(fullpath).Close();
				errorIfCannotOpen = null;
				return true;
			}
			catch (Exception exc)
			{
				errorIfCannotOpen = "Cannot read from file: " + fullpath + ". " + exc.Message;
				return false;
			}
		}
	}

	//private const string EncodedCharStart = "[{_";
	//private const string EncodedCharEnd = "_}]";
	public static string FilenameEncodeToValid(string wantedFilename)
	{
		string result = wantedFilename;

		result = EncodeAndDecodeInterop.EncodeStringHex(result);

		//var invalidChars = Path.GetInvalidFileNameChars();
		//foreach (char c in invalidChars)
		//    result = result.Replace(c.ToString(), string.Format("{0}{1}{2}", EncodedCharStart, (int)c, EncodedCharEnd));
		
		return result;
	}

	public static string FilenameDecodeToValid(string encodedFilename)
	{
		string result = encodedFilename;

		result = EncodeAndDecodeInterop.DecodeStringHex(result);

		//if (encodedFilename.Split(new string[] { EncodedCharStart }, StringSplitOptions.None).Length != encodedFilename.Split(new string[] { EncodedCharEnd }, StringSplitOptions.None).Length)
		//	UserMessages.ShowWarningMessage("Cannot decode filename: " + encodedFilename);
		//else
		//{
		//	var invalidChars = Path.GetInvalidFileNameChars();
		//	foreach (char c in invalidChars)
		//		result = result.Replace(string.Format("{0}{1}{2}", EncodedCharStart, (int)c, EncodedCharEnd), c.ToString());
		//}
		
		return result;
	}

	public static string SelectFile(string title, string initialDir = null, IWin32Window owner = null)
	{
		OpenFileDialog ofd = new OpenFileDialog();
		ofd.Multiselect = false;
		ofd.CheckFileExists = true;

		ofd.Title = title;
		if (initialDir != null)
			ofd.InitialDirectory = initialDir;
		if (ofd.ShowDialog(owner) == DialogResult.OK)
			return ofd.FileName;
		else
			return null;
	}

	public static string SelectFolder(string title, string selectedDir = null, Environment.SpecialFolder? rootFolder = null, IWin32Window owner = null)
	{
		FolderBrowserDialog fbd = new FolderBrowserDialog();
		fbd.ShowNewFolderButton = true;

		fbd.Description = title;
		if (rootFolder.HasValue)
			fbd.RootFolder = rootFolder.Value;
		if (selectedDir != null)
			fbd.SelectedPath = selectedDir;
		if (fbd.ShowDialog(owner) == DialogResult.OK)
			return fbd.SelectedPath;
		else
			return null;
	}
}