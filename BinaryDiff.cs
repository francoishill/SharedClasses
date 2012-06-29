using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.IO.Compression;

namespace SharedClasses
{
	public class BinaryDiff
	{
		private const string xdelta3Path = @"C:\Windows\xdelta3.exe";
		//private const string MetadataTablename = "metadata";

		public enum XDelta3Command { GetDiff, MakePatch };
		public static bool DoXDelta3Command(XDelta3Command command, string file1, string file2, string file3)
		{
			if (!File.Exists(xdelta3Path))
			{
				NetworkInterop.FtpDownloadFile(
					null,
					Path.GetDirectoryName(xdelta3Path),
					GlobalSettings.VisualStudioInteropSettings.Instance.FtpUsername,
					GlobalSettings.VisualStudioInteropSettings.Instance.FtpPassword,
					OnlineSettings.AutoSyncSettings.Instance.OnlineXDeltaExeFileUrl);
			}

			try
			{
				string commandStr = "";
				//For usage of xdelta3.exe, say "xdelta3.exe -h"
				switch (command)
				{
					case XDelta3Command.GetDiff:
						commandStr = "-e -s";
						break;
					case XDelta3Command.MakePatch:
						commandStr = "-d -s";
						break;
				}

				Process.Start(
					xdelta3Path,
					string.Format("{0} \"{1}\" \"{2}\" \"{3}\"", commandStr, file1, file2, file3));
				return true;
			}
			catch (Exception exc)
			{
				UserMessages.ShowWarningMessage("Cannot run xDelta3 command '" + command.ToString() + "': " + exc.Message);
				return false;
			}
		}

		public static bool MakePatch(string oldfile, string newfile, string deltafile)
		{
			return DoXDelta3Command(XDelta3Command.GetDiff, oldfile, newfile, deltafile);
		}

		public static bool ApplyPatch(string originalfile, string difffile, string patchedfile)
		{
			return DoXDelta3Command(XDelta3Command.MakePatch, originalfile, difffile, patchedfile);
		}

		public static int? GetServerCurrentVersion(string RootFolder, string UserFolderName)
		{
			int? max = null;

			string patchesDir = RootFolder.TrimEnd('\\') + "\\" + UserFolderName + "\\Patches";
			foreach (string dir in Directory.GetDirectories(patchesDir))
			{
				string versionFolderName = Path.GetFileName(dir);
				string versionStart = "Version";
				if (versionFolderName.StartsWith(versionStart, StringComparison.InvariantCultureIgnoreCase))
				{
					int tmpint;
					if (int.TryParse(versionFolderName.Substring(versionStart.Length), out tmpint))
						if (!max.HasValue || tmpint > max.Value)
							max = tmpint;
				}
			}
			return max;
		}

		public class FileMetaData
		{
			public string RelativePath { get; set; }
			public long Bytes { get; set; }
			public DateTime Modified { get; set; }
			public bool HasPatch;
			public FileMetaData() { }
			public FileMetaData(string RelativePath, long Bytes, DateTime Modified)
			{
				this.RelativePath = RelativePath;
				this.Bytes = Bytes;
				this.Modified = Modified;
				this.HasPatch = false;
			}
		}
		public class FolderData
		{
			private const string cMetaDataFilename = "autosync.meta";
			private const string cRemoteFilesMetadataFilename = "FilesMetadata.json";
			private const string cCachedSubfolderName = "_cached";
			private const string cPatchFileExtension = ".patch";

			private string _folderpath;
			public string FolderPath { get { return _folderpath; } set { _folderpath = value.TrimEnd('\\'); } }
			public bool IsLocal;
			public FileMetaData[] FilesData;
			public FolderData(string FolderPath, bool IsLocal, FileMetaData[] FilesData)
			{
				this.FolderPath = FolderPath.TrimEnd('\\');
				this.IsLocal = IsLocal;
				this.FilesData = FilesData;
			}

			private string GetRelativeVersionDir(int version)
			{
				return Path.GetDirectoryName(FolderPath).TrimEnd('\\')
					+ "\\Patches\\Version" + version.ToString() + "\\"
					+ cRemoteFilesMetadataFilename;
			}

			public string GetMetadataFullpathLocal()
			{
				return "{0}\\{1}\\{2}".Fmt(FolderPath.TrimEnd('\\'), cCachedSubfolderName, cMetaDataFilename);
			}

			public string GetMetadataFullpathServer(int version)
			{
				return GetRelativeVersionDir(version);
			}

			private string GetAbsolutePath(FileMetaData file)
			{
				return FolderPath + "\\" + file.RelativePath;
			}

			private string GetCahceFolder()
			{
				return "{0}\\{1}".Fmt(this.FolderPath, cCachedSubfolderName);
			}

			private string GetCachedFilePath(FileMetaData file)
			{
				string filepath = GetCahceFolder() + "\\Original\\{0}".Fmt(file.RelativePath);
				string dir = Path.GetDirectoryName(filepath);
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
				return filepath;
			}

			public void CopyLocalFilesToCache()
			{
				foreach (var f in FilesData)
					File.Copy(GetAbsolutePath(f), GetCachedFilePath(f));
			}

			private bool HasFileChanged(FileMetaData file)
			{
				FileInfo fiOrig = new FileInfo(GetCachedFilePath(file));
				FileInfo fiCurr = new FileInfo(GetAbsolutePath(file));
				return
					fiOrig.Length != fiCurr.Length
					|| fiOrig.LastWriteTime != fiCurr.LastWriteTime
					|| fiOrig.FullName.FileToMD5Hash() != fiCurr.FullName.FileToMD5Hash();

			}

			public List<string> GeneratePatches()
			{				
				List<string> patchesMade = new List<string>();

				if (IsLocal)
				{
					var patchesdir = "{0}\\Patches".Fmt(GetCahceFolder());
					foreach (var f in this.FilesData)
					{
						f.HasPatch = false;
						var patchfile = patchesdir + "\\" + f.RelativePath;
						var patchfileDir = Path.GetDirectoryName(patchfile);
						patchfile += cPatchFileExtension;

						if (HasFileChanged(f))
						{
							if (!Directory.Exists(patchfileDir))
								Directory.CreateDirectory(patchfileDir);
							MakePatch(GetAbsolutePath(f), GetCachedFilePath(f), patchfile);
							patchesMade.Add(patchfile);
						}
					}
					if (!Directory.Exists(patchesdir))
						Directory.CreateDirectory(patchesdir);
				}
				else
				{
				}

				return patchesMade;
			}

			private int GetLocalVersion()
			{
				UserMessages.ShowInfoMessage("Must still incorporate this function");
				//Obtain from metadata
				return 0;
			}

			public enum UploadPatchesResult { Success, UnableToLock, ServerVersionNewer, FailedUploadingPatches, FailedApplyingPatches }
			public UploadPatchesResult UploadPatches(string serverRootFolder, string UserFolderName)
			{
				//Check server for matching version, if not match download changes first
				DynamicCodeInvoking.RunCodeReturnStruct versionResult =
					DynamicCodeInvoking.ClientGetAutoSyncVersion(UserFolderName);
				if (!versionResult.Success)
					UserMessages.ShowErrorMessage("Error obtaining version: " + versionResult.ErrorMessage);
				else
				{
					int serverVersion = (int)versionResult.MethodInvokeResultingObject;
					//UserMessages.ShowInfoMessage("Successful version = " + version);
					if (GetLocalVersion() < serverVersion)
						return UploadPatchesResult.ServerVersionNewer;
				}

				return UploadPatchesResult.Success;
				//Lock server (only requested folder)
				//Get new version
				//Create new version folder, write file this folder to say "Not applied yet"
				//Upload patches into new version folder (use http uploading, or maybe allow for FTP??)
				//Apply patches on server
				//Unlock server
			}
		}

		public const string ServerRootFolder = @"C:\Francois\AutoSyncServer";
		public static FolderData GetFolderMetaData(string fullPathOrUserFolder, bool isLocal, int? versionIfServer = null)
		{
			//Stopwatch sw = Stopwatch.StartNew();
			if (!isLocal)
				fullPathOrUserFolder = ServerRootFolder.TrimEnd('\\') + "\\" + fullPathOrUserFolder + "\\RootFolder";
			FolderData folderData = new FolderData(fullPathOrUserFolder, isLocal, null);

			if (!Directory.Exists(fullPathOrUserFolder))
			{
				Directory.CreateDirectory(fullPathOrUserFolder);
				//UserMessages.ShowWarningMessage("Sync folder does not exist: " + localRootFolder);
				//return null;
			}
			//var datafilename = localRootFolder.Replace('\\', '_').Replace(':', '_') + ".data";
			//var datafullpath = SettingsInterop.GetFullFilePathInLocalAppdata(datafilename, "AutoSync", "MetaData");
			var datafullpath =
				isLocal
				? folderData.GetMetadataFullpathLocal()
				: folderData.GetMetadataFullpathServer(versionIfServer.Value);
			var zippedfilepath = isLocal ? GetZippedFilename(datafullpath) : datafullpath;
			bool datazippedexist = File.Exists(zippedfilepath);

			if (datazippedexist)
			{
				WebInterop.SetDefaultJsonInstanceSettings();
				if (isLocal)
				{
					string unzippedpath = DecompressFile(zippedfilepath);
					if (unzippedpath == null)
						UserMessages.ShowWarningMessage("Cannot unzip file (cannot fill folderData): " + datafullpath);
					else
					{
						JSON.Instance.FillObject(folderData, File.ReadAllText(unzippedpath));
						File.Delete(unzippedpath);
					}
				}
				else
				{
					JSON.Instance.FillObject(folderData, File.ReadAllText(zippedfilepath));
				}
				return folderData;
			}

			//var remoteFiles = NetworkInterop.GetFileList(
			//    remoteFtpRootUrl,
			//    GlobalSettings.VisualStudioInteropSettings.Instance.FtpUsername,
			//    GlobalSettings.VisualStudioInteropSettings.Instance.FtpPassword);

			List<string> localfiles = Directory.GetFiles(fullPathOrUserFolder, "*", SearchOption.AllDirectories)
				//.Select(s => s.ToLower())
				.ToList();
			localfiles.Sort();

			folderData.FilesData = new FileMetaData[localfiles.Count];
			int substringStart = (fullPathOrUserFolder.TrimEnd('\\') + "\\").Length;
			for (int i = 0; i < localfiles.Count; i++)
			{


				var fileinfo = new FileInfo(localfiles[i]);
				folderData.FilesData[i] = new FileMetaData(
					localfiles[i].Substring(substringStart),
					fileinfo.Length,
					fileinfo.LastWriteTimeUtc);
			}
			if (isLocal)
				folderData.CopyLocalFilesToCache();

			var json = WebInterop.GetJsonStringFromObject(folderData, false);
			File.WriteAllText(datafullpath, json);

			if (isLocal)//Only compress if locally, not server json data
			{
				CompressFile(datafullpath);
				File.Delete(datafullpath);
			}

			//sw.Stop();
			//UserMessages.ShowInfoMessage("Total time: " + sw.Elapsed.TotalSeconds);
			return folderData;
		}

		public static string GetZippedFilename(string originalFilepath) { return originalFilepath + ".gz"; }
		public static string GetUnzippedFilename(string zipFilepath) { return zipFilepath.Substring(0, zipFilepath.Length - 3); }

		public static string CompressFile(string originalFilepath)
		{
			string returnString = null;

			// Get the stream of the source file.
			using (FileStream inFile = new FileInfo(originalFilepath).OpenRead())
			{
				// Prevent compressing hidden and 
				// already compressed files.
				if ((File.GetAttributes(originalFilepath)
					& FileAttributes.Hidden)
					!= FileAttributes.Hidden & Path.GetExtension(originalFilepath) != ".gz")
				{
					// Create the compressed file.
					string outpath = GetZippedFilename(originalFilepath);
					using (FileStream outFile = 
                    			File.Create(outpath))
					{
						using (GZipStream Compress = 
                        	new GZipStream(outFile,
							CompressionMode.Compress))
						{
							// Copy the source file into 
							// the compression stream.
							inFile.CopyTo(Compress);
							returnString = outpath;
							//Console.WriteLine("Compressed {0} from {1} to {2} bytes.",
							//    Path.GetFileName(originalFile),
							//    fi.Length.ToString(), outFile.Length.ToString());
						}
					}
				}
			}
			return returnString;
		}

		public static string DecompressFile(string zipFilepath)
		{
			string returnString = null;

			// Get the stream of the source file.
			using (FileStream inFile = new FileInfo(zipFilepath).OpenRead())
			{
				// Get original file extension, for example
				// "doc" from report.doc.gz.
				string curFile = zipFilepath;

				//Create the decompressed file.
				string outfile = GetUnzippedFilename(zipFilepath);
				using (FileStream outFile = File.Create(outfile))
				{
					using (GZipStream Decompress = new GZipStream(inFile,
							CompressionMode.Decompress))
					{
						// Copy the decompression stream 
						// into the output file.
						Decompress.CopyTo(outFile);
						returnString = outfile;
						//Console.WriteLine("Decompressed: {0}", fi.Name);

					}
				}
			}
			return returnString;
		}

		//public string ConvertStringToHex(string asciiString)
		//{
		//    string hex = "";
		//    foreach (char c in asciiString)
		//    {
		//        int tmp = c;
		//        hex += String.Format("{0:x2}", (uint)System.Convert.ToUInt32(tmp.ToString()));
		//    }
		//    return hex;
		//}
	}
}