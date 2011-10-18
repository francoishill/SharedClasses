using System;
using System.Windows.Forms;
using System.IO;

public class SettingsInterop
{
	/// <summary>
	/// Always returned without leading backslash.
	/// </summary>
	/// <param name="ApplicationName">The name of the application which will be appended at end of path (appdata\CompanyName\ApplicationName).</param>
	/// <param name="CompanyName">The name of the subfolder in appdata path (appdata\CompanyName\ApplicationName).</param>
	/// <returns></returns>
	public static string LocalAppdataPath(string ApplicationName, string CompanyName = "FJH", bool EnsurePathExists = true)
	{
		string LocalAppdataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		FileSystemInterop.RemoveLeadingAndTrailingBackslashes(ref LocalAppdataPath);
		FileSystemInterop.RemoveLeadingAndTrailingBackslashes(ref ApplicationName);
		FileSystemInterop.RemoveLeadingAndTrailingBackslashes(ref CompanyName);
		string returnPath = LocalAppdataPath + "\\" + CompanyName + "\\" + ApplicationName;
		if (EnsurePathExists && !Directory.Exists(returnPath)) Directory.CreateDirectory(returnPath);
		return returnPath;
	}

	public static string GetFullFilePathInLocalAppdata(string ApplicationName, string fileName, string SubfolderNameInApplication = null, string CompanyName = "FJH", bool EnsurePathExists = true)
	{
		FileSystemInterop.RemoveLeadingAndTrailingBackslashes(ref ApplicationName);
		FileSystemInterop.RemoveLeadingAndTrailingBackslashes(ref SubfolderNameInApplication);
		FileSystemInterop.RemoveLeadingAndTrailingBackslashes(ref fileName);
		FileSystemInterop.RemoveLeadingAndTrailingBackslashes(ref CompanyName);
		bool isSubfolderDefined = SubfolderNameInApplication != null && SubfolderNameInApplication.Trim().Length > 0;
		string folderOfFile =
			LocalAppdataPath(ApplicationName, CompanyName, EnsurePathExists) +
			(isSubfolderDefined ? "\\" + SubfolderNameInApplication : "");
		if (EnsurePathExists && !Directory.Exists(folderOfFile)) Directory.CreateDirectory(folderOfFile);
		return folderOfFile + "\\" + fileName;
	}
}