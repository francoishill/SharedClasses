using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace SharedClasses
{
	public class CompressAndDecompressInterop
	{
		/// <summary>
		/// Use a custom command line for winrar (do not include the winrar.exe, only start with winrar commandline).
		/// </summary>
		/// <param name="CustomCommandLine">In the winRAR format: [command] -[switch1] -[switchN] [archive] [files...] [@listfiles...] [path_to_extract\].</param>
		/// <param name="DirectoryOnlyOfArchiveFile">The destination directory only of the archive to be created (exclude the filename).</param>
		/// <returns>Returns true if winRAR command was successfully excecuted.</returns>
		public static bool RarCustomCommandline(string CustomCommandLine, string DirectoryOnlyOfArchiveFile)
		{
			string the_rar;
			Microsoft.Win32.RegistryKey the_Reg;
			object the_Obj;
			string the_Info;
			System.Diagnostics.ProcessStartInfo the_StartInfo;
			System.Diagnostics.Process the_Process;
			try
			{
				the_Reg = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"Applications\WinRAR.exe\Shell\Open\Command");
				the_Obj = the_Reg.GetValue("");
				the_rar = the_Obj.ToString();
				the_Reg.Close();
				the_rar = the_rar.Substring(1, the_rar.Length - 7);
				//the_Info = " X " + " " + RarName + " " + Path;
				the_Info = CustomCommandLine;

				//WinRAR  <command> -<switch1> -<switchN> <archive> <files...> <@listfiles...> <path_to_extract\>
				the_StartInfo = new System.Diagnostics.ProcessStartInfo();
				//MessageBox.Show(the_rar);

				the_StartInfo.FileName = the_rar;
				the_StartInfo.Arguments = the_Info;
				the_StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
				the_StartInfo.WorkingDirectory = DirectoryOnlyOfArchiveFile;
				the_Process = new System.Diagnostics.Process();
				the_Process.StartInfo = the_StartInfo;
				the_Process.Start();
				the_Process.WaitForExit();
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Unrar a compressed file to a specific location using WinRAR.exe, password is optional.
		/// </summary>
		/// <param name="PathToExtract">Directory to where the files must be extracted.</param>
		/// <param name="RarfileFullPath">The full path of the rar or zip file.</param>
		/// <param name="Password">The password if the rar file requires a password.</param>
		/// <returns>Returns true if successfully extracted, else returns false.</returns>
		public static bool UnRarWithWinrar(string PathToExtract, string RarfileFullPath, string Password = null)
		{
			string the_rar;
			Microsoft.Win32.RegistryKey the_Reg;
			object the_Obj;
			string the_Info;
			System.Diagnostics.ProcessStartInfo the_StartInfo;
			System.Diagnostics.Process the_Process;
			try
			{
				if (Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"Applications\WinRAR.exe\Shell\Open\Command") != null) the_Reg = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"Applications\WinRAR.exe\Shell\Open\Command");
				else if (Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"Applications\WinRAR\Shell\Open\Command") != null) the_Reg = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"Applications\WinRAR\Shell\Open\Command");
				else if (Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"WinRAR.exe\Shell\Open\Command") != null) the_Reg = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"WinRAR.exe\Shell\Open\Command");
				else if (Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"WinRAR\Shell\Open\Command") != null) the_Reg = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"WinRAR\Shell\Open\Command");
				else
				{
					System.Windows.Forms.MessageBox.Show("None of the valid registry paths of winrar found", "WinRAR not found", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
					return false;
				}
				the_Obj = the_Reg.GetValue("");
				the_rar = the_Obj.ToString();
				the_Reg.Close();
				the_rar = the_rar.Substring(1, the_rar.Length - 7);

				if (!PathToExtract.StartsWith("\"")) PathToExtract = "\"" + PathToExtract;
				if (!PathToExtract.EndsWith("\"")) PathToExtract = PathToExtract + "\"";
				if (!RarfileFullPath.StartsWith("\"")) RarfileFullPath = "\"" + RarfileFullPath;
				if (!RarfileFullPath.EndsWith("\"")) RarfileFullPath = RarfileFullPath + "\"";

				//the_Info = " X " + " " + RarName + " " + Path;
				if (Password != null && Password.Length > 0) the_Info = @"x -p" + Password + " -r " + RarfileFullPath + " " + PathToExtract;
				else the_Info = @"x " + RarfileFullPath + " " + PathToExtract;

				//WinRAR  <command> -<switch1> -<switchN> <archive> <files...> <@listfiles...> <path_to_extract\>
				the_StartInfo = new System.Diagnostics.ProcessStartInfo();
				//MessageBox.Show(the_rar);

				the_StartInfo.FileName = the_rar;
				the_StartInfo.Arguments = the_Info;
				the_StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
				the_StartInfo.WorkingDirectory = PathToExtract;
				the_Process = new System.Diagnostics.Process();
				the_Process.StartInfo = the_StartInfo;
				the_Process.Start();
				the_Process.WaitForExit();
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Archive files into a zip or rar archive using WinRAR.exe, password is optional.
		/// </summary>
		/// <param name="FilesToInclude">The list of files to archive (may also use c:\ldkj\*.txt).</param>
		/// <param name="RarfileFullPath">The full file name of the new compressed file.</param>
		/// <param name="Password">Password if the user wants the archive to be password protected.</param>
		/// <param name="OnlyCSharpProjectUpdate">If this is true, all 'Debug' and 'Release' folders will be excluded</param>
		/// <returns>Returns true if successfully compressed, else returns false.</returns>
		public static bool RarWithWinrar(System.Collections.Generic.List<string> FilesToInclude, string RarfileFullPath, string Password = null, Boolean OnlyCSharpProjectUpdate = false)
		{
			string the_rar;
			Microsoft.Win32.RegistryKey the_Reg;
			object the_Obj;
			string the_Info;
			System.Diagnostics.ProcessStartInfo the_StartInfo;
			System.Diagnostics.Process the_Process;
			try
			{
				if (Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"Applications\WinRAR.exe\Shell\Open\Command") != null) the_Reg = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"Applications\WinRAR.exe\Shell\Open\Command");
				else if (Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"Applications\WinRAR\Shell\Open\Command") != null) the_Reg = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"Applications\WinRAR\Shell\Open\Command");
				else if (Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"WinRAR.exe\Shell\Open\Command") != null) the_Reg = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"WinRAR.exe\Shell\Open\Command");
				else if (Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"WinRAR\Shell\Open\Command") != null) the_Reg = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"WinRAR\Shell\Open\Command");
				else
				{
					System.Windows.Forms.MessageBox.Show("None of the valid registry paths of winrar found", "WinRAR not found", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
					return false;
				}
				the_Obj = the_Reg.GetValue("");
				the_rar = the_Obj.ToString();
				the_Reg.Close();
				the_rar = the_rar.Substring(1, the_rar.Length - 7);

				if (!RarfileFullPath.StartsWith("\"")) RarfileFullPath = "\"" + RarfileFullPath;
				if (!RarfileFullPath.EndsWith("\"")) RarfileFullPath = RarfileFullPath + "\"";
				String fileListTotalString = "";
				foreach (String file in FilesToInclude) fileListTotalString
						+= " "
						+ (file.StartsWith("\"") ? "" : "\"")
						+ file
						+ (file.EndsWith("\"") ? "" : "\"");

				string ExclusionString = "";
				//if (OnlyCSharpProjectUpdate) ExclusionString = "-xDebug -x*/Debug -x*/Debug/* -xRelease -x*/Release -x*/Release/* ";
				if (OnlyCSharpProjectUpdate) ExclusionString = @"-x*\Debug\* -x*\Release\* ";

				if (Password != null && Password.Length > 0) the_Info = "A -p" + Password + " -ep1 -r " + ExclusionString + RarfileFullPath + fileListTotalString;
				else the_Info = "A -ep1 -r " + ExclusionString + RarfileFullPath + fileListTotalString;
				//WinRAR  <command> -<switch1> -<switchN> <archive> <files...> <@listfiles...> <path_to_extract\>
				//rar a -r  -x.svn -x*/.svn -x*/.svn/* -x*/anotherSubFolder -x*/anotherSubFolder/* myarchive
				//MessageBox.Show(the_Info);
				the_StartInfo = new System.Diagnostics.ProcessStartInfo();
				the_StartInfo.FileName = the_rar;
				the_StartInfo.Arguments = the_Info;
				the_StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
				the_StartInfo.WorkingDirectory = Path.GetDirectoryName(RarfileFullPath);
				the_Process = new System.Diagnostics.Process();
				the_Process.StartInfo = the_StartInfo;
				the_Process.Start();
				the_Process.WaitForExit();
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Zip one or multiple files into a single zip archive.
		/// </summary>
		/// <param name="FileList">List of files (full paths of files).</param>
		/// <param name="outputPathAndFile">The output zip file (full path).</param>
		/// <param name="password">Password is optional to add a password to zip archive.</param>
		public static void ZipFiles(System.Collections.Generic.List<String> FileList, string outputPathAndFile, string password = null)
		{
			//ArrayList ar = GenerateFileList(inputFolderPath); // generate file list
			//int TrimLength = (Directory.GetParent(inputFolderPath)).ToString().Length;
			// find number of chars to remove     // from orginal file path
			//TrimLength += 1; //remove '\'
			FileStream ostream;
			byte[] obuffer;
			//string outPath = inputFolderPath + @"\" + outputPathAndFile;
			ZipOutputStream oZipStream = new ZipOutputStream(File.Create(outputPathAndFile)); // create zip stream
			if (password != null && password != String.Empty)
				oZipStream.Password = password;
			oZipStream.SetLevel(9); // maximum compression
			ZipEntry oZipEntry;
			foreach (string Fil in FileList) // for each file, generate a zipentry
			{
				//oZipEntry = new ZipEntry(Fil.Remove(0, TrimLength));
				oZipEntry = new ZipEntry(Path.GetFileName(Fil));
				oZipStream.PutNextEntry(oZipEntry);

				if (!Fil.EndsWith(@"/")) // if a file ends with '/' its a directory
				{
					ostream = File.OpenRead(Fil);
					obuffer = new byte[ostream.Length];
					ostream.Read(obuffer, 0, obuffer.Length);
					oZipStream.Write(obuffer, 0, obuffer.Length);
				}
			}
			oZipStream.Finish();
			oZipStream.Close();
		}

		/// <summary>
		/// Unzip a zip archive to a specific directory.
		/// </summary>
		/// <param name="zipPathAndFile">The path of the zipped archive (full path).</param>
		/// <param name="outputFolder">The folder to unzip the files.</param>
		/// <param name="password">If the file requires a password.</param>
		/// <param name="deleteZipFile">Delete the zipped archive.</param>
		public static void UnZipFiles(string zipPathAndFile, string outputFolder, string password, bool deleteZipFile)
		{
			ZipInputStream s = new ZipInputStream(File.OpenRead(zipPathAndFile));
			if (password != null && password != String.Empty)
				s.Password = password;
			ZipEntry theEntry;
			string tmpEntry = String.Empty;
			while ((theEntry = s.GetNextEntry()) != null)
			{
				string directoryName = outputFolder;
				string fileName = Path.GetFileName(theEntry.Name);
				// create directory 
				if (directoryName != "")
				{
					Directory.CreateDirectory(directoryName);
				}
				if (fileName != String.Empty)
				{
					if (theEntry.Name.IndexOf(".ini") < 0)
					{
						string fullPath = directoryName + "\\" + theEntry.Name;
						fullPath = fullPath.Replace("\\ ", "\\");
						string fullDirPath = Path.GetDirectoryName(fullPath);
						if (!Directory.Exists(fullDirPath)) Directory.CreateDirectory(fullDirPath);
						FileStream streamWriter = File.Create(fullPath);
						int size = 2048;
						byte[] data = new byte[2048];
						while (true)
						{
							size = s.Read(data, 0, data.Length);
							if (size > 0)
							{
								streamWriter.Write(data, 0, size);
							}
							else
							{
								break;
							}
						}
						streamWriter.Close();
					}
				}
			}
			s.Close();
			if (deleteZipFile)
				File.Delete(zipPathAndFile);
		}
	}
}
