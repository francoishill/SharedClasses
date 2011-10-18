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
}