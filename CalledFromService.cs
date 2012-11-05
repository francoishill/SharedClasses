using System;
using System.IO;

namespace SharedClasses
{
	public static class CalledFromService
	{
		private readonly static string ReasonForThisUsage =
			  @"When we call this EXE from a service, the local paths comes out weird"
			+ Environment.NewLine + @"For instance [Environment.SpecialFolder.LocalApplicationData] will be C:\Windows\System32\config\systemprofile\AppData\Local"
			+ Environment.NewLine + @"Therefore we dont use this, because for instance if we save a NSIS file (tmp.nsi) in a subfolder and we call makensis.exe"
			+ Environment.NewLine + @"It will fail to compile the exe file as makensis will say file not found"
			+ Environment.NewLine + @"This is weird but its how it is";

		//TODO: Weird behaviour of Environment.GetFolderPath when called from a service
		/// <summary>
		/// See constant above 'ReasonForThisUsage' 
		/// </summary>
		/// <param name="folder">The special folder chosen from the enum</param>
		public static string Environment_GetFolderPath(Environment.SpecialFolder folder)
		{
			if (folder == Environment.SpecialFolder.LocalApplicationData)
			{
				string returnDir = @"c:\francois\LocalAppData";
				if (!Directory.Exists(returnDir))
				{
					Directory.CreateDirectory(returnDir);
					File.WriteAllText(Path.Combine(returnDir, "Reason for this folder.txt"), ReasonForThisUsage);
				}
				return returnDir;
			}
			else
				return Environment.GetFolderPath(folder);
		}
	}
}