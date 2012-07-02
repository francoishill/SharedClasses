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

		public class FileMetaData
		{
			private string _relativePath;
			public string RelativePath { get { return _relativePath; } set { _relativePath = value; /*RecalculateDetails(localFileinfo);*/ } }
			private FileInfo localFileinfo { get; set; }
			public long Bytes { get; set; }
			public DateTime Modified { get; set; }
			public string MD5Hash { get; set; }
			public bool HasPatch;
			public FileMetaData() { }
			public FileMetaData(string RelativePath, FileInfo localFileinfo)//long Bytes, DateTime Modified)
			{
				this.localFileinfo = localFileinfo;
				this.RelativePath = RelativePath;
				RecalculateDetails(localFileinfo);
				this.HasPatch = false;
			}

			private void RecalculateDetails(FileInfo localFileinfo)
			{
				this.localFileinfo = localFileinfo;
				this.Bytes = localFileinfo.Length;
				this.Modified = localFileinfo.LastWriteTime;
				this.Modified = this.Modified.AddMilliseconds(-this.Modified.Millisecond);
				this.MD5Hash = localFileinfo.FullName.FileToMD5Hash();
			}
		}
		public class FolderData
		{
			private const string cMetaDataFilename = "autosync.meta";
			private const string cServerVersionFilename = "serverversion.txt";
			private const string cLocalVersionFilename = "localversion.txt";
			private const string cServerLockFilename = "lock.file";
			private const string cRemoteFilesMetadataFilename = "FilesMetadata.json";
			private const string cCachedSubfolderName = "_cached";
			private const string cPatchFileExtension = ".patch";

			private string _folderpath;
			public string FolderPath { get { return _folderpath; } set { _folderpath = value.TrimEnd('\\'); } }
			private string _serverrooturi;
			public string ServerRootUri { get { return _serverrooturi; } set { _serverrooturi = value.TrimEnd('/'); } }
			public string UserFolderName;
			private string FtpUsername;
			private string FtpPassword;
			private FileMetaData[] cachedMetadata;
			//public bool IsLocal;
			public FileMetaData[] FilesData;
			public FolderData(string FolderPath, string ServerRootUri, string UserFolderName, /*bool IsLocal,*/ FileMetaData[] FilesData, string FtpUsername, string FtpPassword)
			{
				this.FolderPath = FolderPath.TrimEnd('\\');
				this.ServerRootUri = ServerRootUri;
				this.UserFolderName = UserFolderName;
				//this.IsLocal = IsLocal;
				this.FilesData = FilesData;
				this.FtpUsername = FtpUsername;
				this.FtpPassword = FtpPassword;

				if (FilesData == null)
					PopulateFilesData();
			}

			private void PopulateFilesData()
			{
				List<string> localfiles = Directory.GetFiles(FolderPath, "*", SearchOption.AllDirectories)
					//.Select(s => s.ToLower())
					.Where(f => !f.StartsWith(FolderPath + "\\" + cCachedSubfolderName, StringComparison.InvariantCultureIgnoreCase))
					.ToList();
				localfiles.Sort();

				this.FilesData = new FileMetaData[localfiles.Count];
				int substringStartPos = (FolderPath.TrimEnd('\\') + "\\").Length;
				for (int i = 0; i < localfiles.Count; i++)
				{
					var fileinfo = new FileInfo(localfiles[i]);
					this.FilesData[i] = new FileMetaData(
						localfiles[i].Substring(substringStartPos),
						fileinfo);
				}
			}

			public FileMetaData[] GetLastCachedFolderData()
			{
				if (cachedMetadata != null)
					return cachedMetadata;

				FileMetaData[] filesMetadata = new FileMetaData[0];
				FolderData folderData = new FolderData(
					this.FolderPath, this.ServerRootUri, this.UserFolderName, filesMetadata, this.FtpUsername, this.FtpPassword);

				var datafullpath = this.GetMetadataFullpathLocal();
				var zippedfilepath = GetZippedFilename(datafullpath);
				bool datazippedexist = File.Exists(zippedfilepath);

				if (datazippedexist)
				{
					WebInterop.SetDefaultJsonInstanceSettings();
					string unzippedpath = DecompressFile(zippedfilepath);
					if (unzippedpath == null)
						UserMessages.ShowWarningMessage("Cannot unzip file (cannot fill folderData): " + datafullpath);
					else
					{
						JSON.Instance.FillObject(folderData, File.ReadAllText(unzippedpath));
						File.Delete(unzippedpath);
					}
					return folderData.FilesData;
				}
				return null;
			}

			/*private string GetRelativeVersionDir(int version)
			{
				return Path.GetDirectoryName(FolderPath).TrimEnd('\\')
					+ "\\Patches\\Version" + version.ToString() + "\\"
					+ cRemoteFilesMetadataFilename;
			}*/

			public string GetMetadataFullpathLocal()
			{
				return "{0}\\{1}\\{2}".Fmt(FolderPath.TrimEnd('\\'), cCachedSubfolderName, cMetaDataFilename);
			}

			private string GetTempServerVersionFileFullpathLocal()
			{
				return "{0}\\{1}\\{2}".Fmt(FolderPath.TrimEnd('\\'), cCachedSubfolderName, cServerVersionFilename);
			}

			private string GetLocalVersionFileFullpath()
			{
				return "{0}\\{1}\\{2}".Fmt(FolderPath.TrimEnd('\\'), cCachedSubfolderName, cLocalVersionFilename);
			}

			private string GetRootUserfolderUri()
			{
				return this.ServerRootUri + "/" + this.UserFolderName;
			}

			private string GetOnlineNewestVersionFileUri()
			{
				return GetRootUserfolderUri() + "/" + cServerVersionFilename;
			}

			private string GetServerLockFileUri()
			{
				return GetRootUserfolderUri() + "/" + cServerLockFilename;
			}

			private string GetServerVersionFolderUri(int newVersion)
			{
				return GetRootUserfolderUri() + "/Patches/Version" + newVersion;
			}

			/*public string GetMetadataFullpathServer(int version)
			{
				return GetRelativeVersionDir(version);
			}*/

			private string GetAbsolutePath(FileMetaData file)
			{
				return FolderPath + "\\" + file.RelativePath;
			}

			private string GetCacheFolder()
			{
				return "{0}\\{1}".Fmt(this.FolderPath, cCachedSubfolderName);
			}

			private string GetCachedFilePath(FileMetaData file)
			{
				string filepath = GetCacheFolder() + "\\Original\\{0}".Fmt(file.RelativePath);
				string dir = Path.GetDirectoryName(filepath);
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
				return filepath;
			}

			public int? GetServerCurrentVersion(/*string RootFolder, */string UserFolderName)
			{
				//int? max = null;

				File.Delete(GetTempServerVersionFileFullpathLocal());
				var localVersionFile = NetworkInterop.FtpDownloadFile(
					null,
					Path.GetDirectoryName(GetTempServerVersionFileFullpathLocal()).TrimEnd('\\'),
					FtpUsername,
					FtpPassword,
					GetOnlineNewestVersionFileUri());

				if (localVersionFile == null)
					return null;

				string filetext = File.ReadAllText(localVersionFile).Trim();
				int tmpint;
				if (int.TryParse(filetext, out tmpint))
					return tmpint;
				else
					return null;

				/*string patchesDir = RootFolder.TrimEnd('\\') + "\\" + UserFolderName + "\\Patches";
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
				return max;*/
			}

			public void CopyLocalFilesToCache()
			{
				foreach (var f in FilesData)
					File.Copy(GetAbsolutePath(f), GetCachedFilePath(f));
			}

			private bool HasFileChanged(FileMetaData file, FileMetaData cachedFileMetadata)
			{
				return file.MD5Hash != cachedFileMetadata.MD5Hash ||
					//file.Modified != cachedFileMetadata.Modified ||
					file.Modified.Year != cachedFileMetadata.Modified.Year ||
					file.Modified.Month != cachedFileMetadata.Modified.Month ||
					file.Modified.Day != cachedFileMetadata.Modified.Day ||
					file.Modified.Hour != cachedFileMetadata.Modified.Hour ||
					file.Modified.Minute != cachedFileMetadata.Modified.Minute ||
					file.Modified.Second != cachedFileMetadata.Modified.Second ||
					file.Bytes != cachedFileMetadata.Bytes;

				//FileInfo fiOrig = new FileInfo(GetCachedFilePath(file));
				//FileInfo fiCurr = new FileInfo(GetAbsolutePath(file));
				//return
				//    fiOrig.Length != fiCurr.Length
				//    || fiOrig.LastWriteTime != fiCurr.LastWriteTime
				//    || fiOrig.FullName.FileToMD5Hash() != fiCurr.FullName.FileToMD5Hash();
			}

			private Dictionary<string, FileMetaData> GetTempCachedFileAndMetadataDictionary()
			{
				FileMetaData[] cachedMetadata = GetLastCachedFolderData();
				var tmpdict = new Dictionary<string, FileMetaData>();
				if (cachedMetadata != null)
				{
					foreach (var cf in cachedMetadata)
						tmpdict.Add(cf.RelativePath, cf);
					return tmpdict;
				}
				else return null;
			}

			private Dictionary<string, FileMetaData> GetTempDictForLocalFilesMetadata()
			{
				var tmpdict = new Dictionary<string, FileMetaData>();
				if (FilesData != null)
				{
					foreach (var lf in FilesData)
						tmpdict.Add(lf.RelativePath, lf);
					return tmpdict;
				}
				else return null;
			}

			private bool HasLocalChanges()
			{
				var tmpcacheddict = GetTempCachedFileAndMetadataDictionary();
				if (tmpcacheddict != null && this.FilesData.Length != tmpcacheddict.Count)
					return true;
				foreach (var f in this.FilesData)
					if (
						tmpcacheddict == null
						|| !tmpcacheddict.ContainsKey(f.RelativePath)
						|| HasFileChanged(f, tmpcacheddict[f.RelativePath]))
						return true;
				return false;
			}

			private string GetPatchesRootDir()
			{
				return "{0}\\Patches".Fmt(GetCacheFolder());
			}
			private string GetPatchFilepath(FileMetaData file)
			{
				return GetPatchesRootDir() + "\\" + file.RelativePath;
			}

			private bool UploadPatch(FileMetaData file, string patchfiledir, int newVersion)
			{
				return NetworkInterop.FtpUploadFiles(
					null,
					GetServerVersionFolderUri(newVersion) + "/" + Path.GetDirectoryName(file.RelativePath).Replace("\\", "/"),
					FtpUsername,
					FtpPassword,
					new string[] { patchfiledir });
			}

			public bool UploadFilesWithPatches(int newVersion)
			{
				//List<string> patchesMade = new List<string>();

				//if (IsLocal)
				//{

				if (!Directory.Exists(GetPatchesRootDir()))
					Directory.CreateDirectory(GetPatchesRootDir());

				var localPatchesDir = GetCacheFolder() + "\\Patches";
				Directory.Delete(localPatchesDir, true);
				Directory.CreateDirectory(localPatchesDir);

				var tmpcacheddict = GetTempCachedFileAndMetadataDictionary();
				foreach (var f in this.FilesData)
				{
					f.HasPatch = false;
					var patchfile = GetPatchFilepath(f);//patchesdir + "\\" + f.RelativePath;
					var patchfileDir = Path.GetDirectoryName(patchfile);
					patchfile += cPatchFileExtension;

					if (
						tmpcacheddict == null
						|| !tmpcacheddict.ContainsKey(f.RelativePath)
						|| HasFileChanged(f, tmpcacheddict[f.RelativePath]))
					{
						if (!Directory.Exists(patchfileDir))
							Directory.CreateDirectory(patchfileDir);
						MakePatch(GetAbsolutePath(f), GetCachedFilePath(f), patchfile);
						if (!UploadPatch(f, patchfile, newVersion))
							return false;
						//patchesMade.Add(patchfile);
					}
				}

				WebInterop.SetDefaultJsonInstanceSettings();
				var json = WebInterop.GetJsonStringFromObject(this, false);
				File.WriteAllText(GetMetadataFullpathLocal(), json);
				string zippedPath = CompressFile(GetMetadataFullpathLocal());
				File.Delete(GetMetadataFullpathLocal());
				if (zippedPath == null)
					return false;
				cachedMetadata = null;
				//}
				//else
				//{
				//}
				return true;
				//return patchesMade;
			}

			private int? GetLocalVersion()
			{
				string localversionfile = GetLocalVersionFileFullpath();

				if (!File.Exists(localversionfile))
					return null;

				string filetext = File.ReadAllText(localversionfile).Trim();
				int tmpint;
				if (int.TryParse(filetext, out tmpint))
					return tmpint;
				else
					return null;

				//UserMessages.ShowInfoMessage("Must still incorporate this function");
				//Obtain from metadata
				//return 0;
			}

			private string CreateEmptyLockFileLocally()
			{
				string tmpFilePath = Path.GetTempPath().TrimEnd('\\') + "\\" + cServerLockFilename;
				if (File.Exists(tmpFilePath) && new FileInfo(tmpFilePath).Length > 0)
					File.Delete(tmpFilePath);
				if (!File.Exists(tmpFilePath))
					File.Create(tmpFilePath);
				return tmpFilePath;
			}

			private bool IsServerLocked()
			{
				return NetworkInterop.FtpFileExists(this.GetServerLockFileUri(), FtpUsername, FtpPassword);
			}

			private bool LockServer()
			{
				string tmpLocalLockFile = CreateEmptyLockFileLocally();
				//TODO: FtpUploadFiles returns boolean, is this returned value trustworthy? See steps in upload method itsself
				return NetworkInterop.FtpUploadFiles(
					null,
					GetRootUserfolderUri(),
					FtpUsername,
					FtpPassword,
					new string[] { tmpLocalLockFile });
			}

			private bool UnlockServer()
			{
				return NetworkInterop.DeleteFTPfile(GetServerLockFileUri(), FtpUsername, FtpPassword);
			}

			private int? GetNewVersionFromserver(int currentVersion)
			{
				int newVersionNum = currentVersion + 1;
				string newversionUri = GetServerVersionFolderUri(newVersionNum);
				if (NetworkInterop.FtpDirectoryExists(newversionUri, FtpUsername, FtpPassword))
					NetworkInterop.RemoveFTPDirectory(newversionUri, FtpUsername, FtpPassword);

				if (!NetworkInterop.CreateFTPDirectory(newversionUri, FtpUsername, FtpPassword))
				{
					UserMessages.ShowErrorMessage("Could not create new version dir: " + newversionUri);
					return null;
				}
				else
					return newVersionNum;
				//return null;
			}

			private bool UpateServerVersionFile(int newVersion)
			{
				string onlineVersionFileLocalTempPath = GetTempServerVersionFileFullpathLocal();
				string onlineVersionUri = GetRootUserfolderUri();//Path.GetDirectoryName(GetOnlineNewestVersionFileUri().Replace("/", "\\")).Replace("\\", "/");
				string localVersionFile = GetLocalVersionFileFullpath();
				File.Delete(onlineVersionFileLocalTempPath);
				File.WriteAllText(onlineVersionFileLocalTempPath, newVersion.ToString());
				if (!File.Exists(onlineVersionFileLocalTempPath))
					return false;
				if (!NetworkInterop.FtpUploadFiles(null, onlineVersionUri, FtpUsername, FtpPassword,
					new string[] { onlineVersionFileLocalTempPath }))
					return false;
				File.WriteAllText(localVersionFile, newVersion.ToString());
				return true;
			}

			private List<FileMetaData> GetAddedFilesLocally()
			{
				var tmplist = new List<FileMetaData>();
				var tmpcacheddict = GetTempCachedFileAndMetadataDictionary();
				var tmplocaldict = GetTempDictForLocalFilesMetadata();
				foreach (string f in tmplocaldict.Keys)
					if (!tmpcacheddict.ContainsKey(f))
						tmplist.Add(tmplocaldict[f]);
				return tmplist;
			}

			private List<FileMetaData> GetRemovedFilesLocally()
			{
				var tmplist = new List<FileMetaData>();
				var tmpcacheddict = GetTempCachedFileAndMetadataDictionary();
				var tmplocaldict = GetTempDictForLocalFilesMetadata();
				foreach (string f in tmpcacheddict.Keys)
					if (!tmplocaldict.ContainsKey(f))
						tmplist.Add(tmpcacheddict[f]);
				return tmplist;
			}

			private bool CheckForAddedOrRemovedFiles(List<FileMetaData> addedFiles, List<FileMetaData> removedFiles)
			{
				//var addedFiles = GetAddedFilesLocally();
				//var removedFiles = GetRemovedFilesLocally();

				if (addedFiles.Count == 0 && removedFiles.Count == 0)
					return true;

				foreach (var af in addedFiles)
				{
					File.Copy(GetAbsolutePath(af), GetCachedFilePath(af));
					if (!NetworkInterop.FtpUploadFiles(
						null,
						GetRootUserfolderUri() + "/Original/" + Path.GetDirectoryName(af.RelativePath).Replace("\\", "/"),
						FtpUsername,
						FtpPassword,
						new string[] { GetAbsolutePath(af) }))
						return false;
				}

				foreach (var rf in removedFiles)
				{
					File.Delete(GetCachedFilePath(rf));
					int CheckNextTodoItem;
					//TODO: This might be dangerous? Rather moved it to "deleted" folder on the server???
					if (!NetworkInterop.DeleteFTPfile(GetRootUserfolderUri() + "/Original/" + rf.RelativePath.Replace("\\", "/"), FtpUsername, FtpPassword))
						return false;
				}

				return true;
			}

			public enum UploadPatchesResult
			{
				Success,
				NoLocalChanges,
				ServerAlreadyLocked,
				UnableToLock,
				ServerVersionNewer,
				FailedObtainingServerVersion,
				FailedObtainingLocalVersion,
				FailedAddingNewVersionFolder,
				FailedUploadingPatches,
				FailedIncreasingServerVersion,
				FailedToAddOrRemoveFiles,
				//FailedApplyingPatches
			}
			public UploadPatchesResult UploadChangesToServer()
			{
				if (!this.HasLocalChanges())
					return UploadPatchesResult.NoLocalChanges;

				if (IsServerLocked())
					return UploadPatchesResult.ServerAlreadyLocked;

				int? serverVersion = GetServerCurrentVersion(this.UserFolderName);
				int? localVersion = GetLocalVersion();
				if (serverVersion == null)
					return UploadPatchesResult.FailedObtainingServerVersion;
				else if (localVersion == null)
					return UploadPatchesResult.FailedObtainingLocalVersion;
				else if (localVersion.Value < serverVersion.Value)
					//Must download newer version first
					return UploadPatchesResult.ServerVersionNewer;

				if (!LockServer())
					return UploadPatchesResult.UnableToLock;

				int? newVersion = GetNewVersionFromserver(localVersion.Value);
				if (newVersion == null)
					return UploadPatchesResult.FailedAddingNewVersionFolder;

				//string serverNewVersionFolder = GetServerVersionFolderUri(newVersion.Value);

				var addedFiles = GetAddedFilesLocally();
				var removedFiles = GetRemovedFilesLocally();
				if (!UploadFilesWithPatches(newVersion.Value))
					return UploadPatchesResult.FailedUploadingPatches;

				//First implement next line this before testing again
				if (!CheckForAddedOrRemovedFiles(addedFiles, removedFiles))
					return UploadPatchesResult.FailedToAddOrRemoveFiles;

				if (!UpateServerVersionFile(newVersion.Value))
					return UploadPatchesResult.FailedIncreasingServerVersion;

				while (!UnlockServer()) { }

				return UploadPatchesResult.Success;

				//Check server for matching version, if not match download changes first
				/*DynamicCodeInvoking.RunCodeReturnStruct versionResult =
					DynamicCodeInvoking.ClientGetAutoSyncVersion(UserFolderName);
				if (!versionResult.Success)
					UserMessages.ShowErrorMessage("Error obtaining version: " + versionResult.ErrorMessage);
				else
				{
					int serverVersion = (int)versionResult.MethodInvokeResultingObject;
					if (GetLocalVersion() < serverVersion)
						//Must download rewer version first
						return UploadPatchesResult.ServerVersionNewer;
				}*/

				//Upload patches into new version folder (use http uploading, or maybe allow for FTP??)
				//Apply patches on server
				//Unlock server
			}
		}

		//public const string ServerRootFolder = @"C:\Francois\AutoSyncServer";
		//public const string ServerRootUri = "ftp://fjh.dyndns.org/francois/AutoSyncServer";
		[Obsolete("Not used anymore", true)]
		public static FolderData GetFolderMetaData(string fullPathOrUserFolder, string serverRootUri, string UserFolderName)
		{
			return null;
			//Stopwatch sw = Stopwatch.StartNew();
			//if (!isLocal)
			//    fullPathOrUserFolder = ServerRootFolder.TrimEnd('\\') + "\\" + fullPathOrUserFolder + "\\RootFolder";

			/*FolderData folderData = new FolderData(fullPathOrUserFolder, serverRootUri, UserFolderName, null);

			if (!Directory.Exists(fullPathOrUserFolder))
			{
				Directory.CreateDirectory(fullPathOrUserFolder);
				//UserMessages.ShowWarningMessage("Sync folder does not exist: " + localRootFolder);
				//return null;
			}
			//var datafilename = localRootFolder.Replace('\\', '_').Replace(':', '_') + ".data";
			//var datafullpath = SettingsInterop.GetFullFilePathInLocalAppdata(datafilename, "AutoSync", "MetaData");
			var datafullpath = folderData.GetMetadataFullpathLocal();
			var zippedfilepath = GetZippedFilename(datafullpath);
			bool datazippedexist = File.Exists(zippedfilepath);

			if (datazippedexist)
			{
				WebInterop.SetDefaultJsonInstanceSettings();
				//if (isLocal)
				//{
				string unzippedpath = DecompressFile(zippedfilepath);
				if (unzippedpath == null)
					UserMessages.ShowWarningMessage("Cannot unzip file (cannot fill folderData): " + datafullpath);
				else
				{
					JSON.Instance.FillObject(folderData, File.ReadAllText(unzippedpath));
					File.Delete(unzippedpath);
				}
				//}
				//else
				//{
				//    JSON.Instance.FillObject(folderData, File.ReadAllText(zippedfilepath));
				//}
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
			//if (isLocal)
			folderData.CopyLocalFilesToCache();

			var json = WebInterop.GetJsonStringFromObject(folderData, false);
			File.WriteAllText(datafullpath, json);

			//if (isLocal)//Only compress if locally, not server json data
			//{
			CompressFile(datafullpath);
			File.Delete(datafullpath);
			//}

			//sw.Stop();
			//UserMessages.ShowInfoMessage("Total time: " + sw.Elapsed.TotalSeconds);
			return folderData;*/
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