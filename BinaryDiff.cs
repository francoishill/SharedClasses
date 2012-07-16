using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading;

namespace SharedClasses
{
	public class BinaryDiff
	{
		private const string xdelta3Path = @"C:\Windows\xdelta3.exe";
		//private const string MetadataTablename = "metadata";

		public enum XDelta3Command { MakePatch, ApplyPatch };
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
					case XDelta3Command.MakePatch:
						commandStr = "-e -f -s";
						break;
					case XDelta3Command.ApplyPatch:
						commandStr = "-d -f -s";
						break;
				}

				Process.Start(
					xdelta3Path,
					string.Format("{0} \"{1}\" \"{2}\" \"{3}\"", commandStr, file1, file2, file3)).WaitForExit();
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
			return DoXDelta3Command(XDelta3Command.MakePatch, oldfile, newfile, deltafile);
		}

		public static bool ApplyPatch(string originalfile, string difffile, string patchedfile)
		{
			return DoXDelta3Command(XDelta3Command.ApplyPatch, originalfile, difffile, patchedfile);
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
				//if (this.Bytes < 1024 * 100)//Only MD5Hash for files < 100kB
				this.MD5Hash = localFileinfo.FullName.FileToMD5Hash();
				//else
				//    this.MD5Hash = "MD5HashNotCalculatedFileLargerThan100kB";
			}

			public void RecalculateDetails(string basePath)
			{
				RecalculateDetails(new FileInfo(basePath.TrimEnd('\\') + "\\" + this.RelativePath));
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
			public List<FileMetaData> AddedFiles;
			public List<FileMetaData> RemovedFiles;
			public FolderData() { }
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
				if (!Directory.Exists(FolderPath))
					return;

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

			public static bool PopulateFolderDataFromZippedJson(FolderData folderData, string zippedJsonPath)
			{
				JSON.SetDefaultJsonInstanceSettings();
				string unzippedpath = DecompressFile(zippedJsonPath);
				if (unzippedpath == null)
					UserMessages.ShowWarningMessage("Cannot unzip file (cannot fill folderData): " + zippedJsonPath);
				else
				{
					JSON.Instance.FillObject(folderData, File.ReadAllText(unzippedpath));
					File.Delete(unzippedpath);
					return true;
				}
				return false;
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
					//WebInterop.SetDefaultJsonInstanceSettings();
					//string unzippedpath = DecompressFile(zippedfilepath);
					//if (unzippedpath == null)
					//    UserMessages.ShowWarningMessage("Cannot unzip file (cannot fill folderData): " + datafullpath);
					//else
					//{
					//    JSON.Instance.FillObject(folderData, File.ReadAllText(unzippedpath));
					//    File.Delete(unzippedpath);
					//}
					if (PopulateFolderDataFromZippedJson(folderData, zippedfilepath))
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
				return GetTempServerRootDir() + "\\" + cServerVersionFilename;
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

			private string GetAboluteServerUri(FileMetaData file)
			{
				return GetRootUserfolderUri() + "/Original/" + file.RelativePath.Replace("\\", "/");
			}

			private string GetCacheFolder()
			{
				return "{0}\\{1}".Fmt(this.FolderPath, cCachedSubfolderName);
			}

			private string GetLocalOriginalFilePath(FileMetaData file)
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

				string tmpFile = GetTempServerVersionFileFullpathLocal();
				File.Delete(tmpFile);
				while (File.Exists(tmpFile))
				{
					Thread.Sleep(300);
					File.Delete(tmpFile);
				}
				if (!DownloadFile(Path.GetDirectoryName(tmpFile).TrimEnd('\\'), GetOnlineNewestVersionFileUri()))
					return null;
				//var localVersionFile = NetworkInterop.FtpDownloadFile(
				//    null,
				//    ,
				//    FtpUsername,
				//    FtpPassword,
				//    GetOnlineNewestVersionFileUri());

				//if (localVersionFile == null)
				//    return null;

				string filetext = File.ReadAllText(tmpFile).Trim();
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
					File.Copy(GetAbsolutePath(f), GetLocalOriginalFilePath(f));
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

			private bool HasLocalChanges(out List<string> changedRelativePaths, out List<string> addedRelativePaths, out List<string> removedRelativePaths)
			{
				changedRelativePaths = new List<string>();
				addedRelativePaths = new List<string>();
				removedRelativePaths = new List<string>();
				var tmpcacheddict = GetTempCachedFileAndMetadataDictionary();

				if (this.FilesData == null)
					return false;//TODO: No local changes if no local metadata file yet?

				if (tmpcacheddict == null)
				{
					var locver = GetLocalVersion();
					if (!locver.HasValue || locver.Value == 1)
						this.SaveJsonLocallyReturnZippedPath();
					else
						UserMessages.ShowWarningMessage("Warning: local metadata file was missing and got regenerated, but version is " + locver.Value);
				}

				if (tmpcacheddict == null)
				{
					foreach (var af in this.FilesData)
						addedRelativePaths.Add(af.RelativePath);
					return true;
				}
				//if (tmpcacheddict.Count > this.FilesData.Length)
				//{
				foreach (var f in this.FilesData)
					if (!tmpcacheddict.ContainsKey(f.RelativePath))//Added file
						addedRelativePaths.Add(f.RelativePath);
					else
					{
						if (HasFileChanged(f, tmpcacheddict[f.RelativePath]))//If changed add to list
							changedRelativePaths.Add(f.RelativePath);
						tmpcacheddict.Remove(f.RelativePath);//If contained in cache remove from cache list, so all remaining items are removed items
					}
				foreach (var key in tmpcacheddict.Keys)
					removedRelativePaths.Add(key);
				//}
				/*if (tmpcacheddict != null && this.FilesData.Length != tmpcacheddict.Count)
					return true;

				foreach (var f in this.FilesData)
					if (
						tmpcacheddict == null
						|| !tmpcacheddict.ContainsKey(f.RelativePath)
						|| HasFileChanged(f, tmpcacheddict[f.RelativePath]))
						//return true;
						changedRelativePaths.Add(f.RelativePath);*/
				return changedRelativePaths.Count > 0 || addedRelativePaths.Count > 0 || removedRelativePaths.Count > 0;
				//return false;
			}

			private string GetPatchesRootDir()
			{
				return "{0}\\Patches".Fmt(GetCacheFolder());
			}
			private string GetPatchFilepath(FileMetaData file)
			{
				return GetPatchesRootDir() + "\\" + file.RelativePath;
			}

			private string GetTempServerRootDir()
			{
				string tmpdir = "{0}\\TempServer".Fmt(GetCacheFolder());
				if (!Directory.Exists(tmpdir))
					Directory.CreateDirectory(tmpdir);
				return tmpdir;
			}
			private string GetTempServerRootPathcesDir()
			{
				string tmpresult = GetTempServerRootDir() + "\\Patches";
				if (!Directory.Exists(tmpresult))
					Directory.CreateDirectory(tmpresult);
				return tmpresult;
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
						f.HasPatch = true;
						if (!Directory.Exists(patchfileDir))
							Directory.CreateDirectory(patchfileDir);
						MakePatch(GetLocalOriginalFilePath(f), GetAbsolutePath(f), patchfile);
						if (!UploadPatch(f, patchfile, newVersion))
							return false;
						//patchesMade.Add(patchfile);
					}
				}

				string zippedJsonpathLocal = SaveJsonLocallyReturnZippedPath();

				cachedMetadata = null;

				if (zippedJsonpathLocal == null)
					return false;

				//Upload the json file of metadata up to server
				return NetworkInterop.FtpUploadFiles(
					null,
					GetServerVersionFolderUri(newVersion) + "/",// + Path.GetDirectoryName(file.RelativePath).Replace("\\", "/"),
					FtpUsername,
					FtpPassword,
					new string[] { zippedJsonpathLocal });
			}

			public string SaveJsonLocallyReturnZippedPath()
			{
				JSON.SetDefaultJsonInstanceSettings();
				var json = WebInterop.GetJsonStringFromObject(this, false);
				File.WriteAllText(GetMetadataFullpathLocal(), json);
				string zippedPath = CompressFile(GetMetadataFullpathLocal());
				File.Delete(GetMetadataFullpathLocal());
				return zippedPath;
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

			/*private string CreateLockFileLocallyReturnPath()
			{
				string tmpFilePath = Path.GetTempPath().TrimEnd('\\') + "\\" + cServerLockFilename;
				//if (File.Exists(tmpFilePath) && new FileInfo(tmpFilePath).Length > 0)
				//    File.Delete(tmpFilePath);
				//if (!File.Exists(tmpFilePath))
				//    File.Create(tmpFilePath);
				return tmpFilePath;
			}*/

			private bool IsServerLocked()
			{
				return NetworkInterop.FtpFileExists(this.GetServerLockFileUri(), FtpUsername, FtpPassword);
			}

			private bool BytesArraysEqual(byte[] arr1, byte[] arr2)
			{
				if (arr1 == null && arr2 == null)
					return true;
				if (arr1 == null || arr2 == null)//Only one is null, previous statement check if both null
					return false;
				if (arr1.Length != arr2.Length)
					return false;
				for (int b = 0; b < arr1.Length; b++)
					if (arr1[b] != arr2[b])
						return false;
				return true;
			}

			private bool LockServer()
			{
				string tmpFilePath = Path.GetTempPath().TrimEnd('\\') + "\\" + cServerLockFilename;
				byte[] guidBytes = Guid.NewGuid().ToByteArray();
				File.WriteAllBytes(tmpFilePath, guidBytes);
				//TODO: FtpUploadFiles returns boolean, is this returned value trustworthy? See steps in upload method itsself
				if (!NetworkInterop.FtpUploadFiles(null, GetRootUserfolderUri(), FtpUsername, FtpPassword, new string[] { tmpFilePath }))
					return false;
				if (!DownloadFile(Path.GetDirectoryName(tmpFilePath), GetRootUserfolderUri() + "//" + cServerLockFilename))
					return false;
				return BytesArraysEqual(File.ReadAllBytes(tmpFilePath), guidBytes);
			}

			private bool UnlockServer()
			{
				return NetworkInterop.DeleteFTPfile(
					null,
					GetServerLockFileUri(),
					FtpUsername,
					FtpPassword);
			}

			private int? GetNewVersionFromserver(int currentVersion)
			{
				int newVersionNum = currentVersion + 1;
				string newversionUri = GetServerVersionFolderUri(newVersionNum);
				//if (NetworkInterop.FtpDirectoryExists(newversionUri, FtpUsername, FtpPassword))
				if (!NetworkInterop.RemoveFTPDirectory(newversionUri, FtpUsername, FtpPassword))
					UserMessages.ShowErrorMessage("Could not remove FTP directory: " + newversionUri);

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

			private void PopulateAddedFilesLocally()
			{
				AddedFiles = new List<FileMetaData>();
				var tmpcacheddict = GetTempCachedFileAndMetadataDictionary();
				if (tmpcacheddict == null)
					return;
				var tmplocaldict = GetTempDictForLocalFilesMetadata();
				foreach (string f in tmplocaldict.Keys)
					if (!tmpcacheddict.ContainsKey(f))
						AddedFiles.Add(tmplocaldict[f]);
			}

			private void PopulateRemovedFilesLocally()
			{
				RemovedFiles = new List<FileMetaData>();
				var tmpcacheddict = GetTempCachedFileAndMetadataDictionary();
				if (tmpcacheddict == null)
					return;
				var tmplocaldict = GetTempDictForLocalFilesMetadata();
				foreach (string f in tmpcacheddict.Keys)
					if (!tmplocaldict.ContainsKey(f))
						RemovedFiles.Add(tmpcacheddict[f]);
			}

			private bool CheckForAddedOrRemovedFiles()
			{
				//var addedFiles = GetAddedFilesLocally();
				//var removedFiles = GetRemovedFilesLocally();

				if (AddedFiles.Count == 0 && RemovedFiles.Count == 0)
					return true;

				foreach (var af in AddedFiles)
				{
					File.Copy(GetAbsolutePath(af), GetLocalOriginalFilePath(af));
					if (!NetworkInterop.FtpUploadFiles(
						null,
						GetRootUserfolderUri() + "/Original/" + Path.GetDirectoryName(af.RelativePath).Replace("\\", "/"),
						FtpUsername,
						FtpPassword,
						new string[] { GetAbsolutePath(af) }))
						return false;
				}

				foreach (var rf in RemovedFiles)
				{
					File.Delete(GetLocalOriginalFilePath(rf));
					int CheckNextTodoItem;
					//TODO: This might be dangerous? Rather move it to "deleted" folder on the server???
					if (!NetworkInterop.DeleteFTPfile(
						null,
						GetRootUserfolderUri() + "/Original/" + rf.RelativePath.Replace("\\", "/"),
						FtpUsername,
						FtpPassword))
						return false;
				}

				return true;
			}

			public bool DownloadFile(string localRoot, string onlineFileFullUrl)
			{
				return NetworkInterop.FtpDownloadFile(null, localRoot, FtpUsername, FtpPassword, onlineFileFullUrl) != null;
			}

			public void InitialSetupLocally()
			{
				if (!Directory.Exists(FolderPath))
					Directory.CreateDirectory(FolderPath);
				if (!File.Exists(GetLocalVersionFileFullpath()))
				{
					if (!Directory.Exists(Path.GetDirectoryName(GetLocalVersionFileFullpath())))
						Directory.CreateDirectory(Path.GetDirectoryName(GetLocalVersionFileFullpath()));
					File.WriteAllText(GetLocalVersionFileFullpath(), "0".ToString());
				}
			}

			public enum UploadPatchesResult
			{
				Success,
				NoLocalChanges,
				ServerAlreadyLocked,
				UnableToLock,
				ServerVersionNewer,
				InvalidLocalVersionNewerThanServer,
				FailedObtainingServerVersion,
				FailedObtainingLocalVersion,
				FailedAddingNewVersionFolder,
				FailedUploadingPatches,
				FailedIncreasingServerVersion,
				FailedToAddOrRemoveFiles,
				FailedToUpdateLocalVersion,
				FailedToUpdateLocalMetadataAfterServerdownload,
				//UserCancelledDoNotWantToLoseLocalChanges,
				//FailedApplyingPatches
			}
			public UploadPatchesResult UploadChangesToServer()
			{
				List<string> localChangesRelativePaths;
				List<string> localAddedRelativePaths;
				List<string> localRemovedRelativePaths;

				bool hasLocalChanges = this.HasLocalChanges(out localChangesRelativePaths, out localAddedRelativePaths, out localRemovedRelativePaths);

				if (IsServerLocked())
					return UploadPatchesResult.ServerAlreadyLocked;

				int? localVersion = GetLocalVersion();
				int? serverVersion = GetServerCurrentVersion(this.UserFolderName);
				if (serverVersion == null)
					return UploadPatchesResult.FailedObtainingServerVersion;
				else if (localVersion == null)
					return UploadPatchesResult.FailedObtainingLocalVersion;
				else if (localVersion.Value > serverVersion.Value)
					return UploadPatchesResult.InvalidLocalVersionNewerThanServer;
				else if (localVersion.Value < serverVersion.Value)
				{
					for (int i = localVersion.Value + 1; i <= serverVersion.Value; i++)
					{
						if (!DownloadFile(GetTempServerRootDir(), GetServerVersionFolderUri(i) + "/" + GetZippedFilename(cMetaDataFilename)))
							return UploadPatchesResult.FailedToUpdateLocalVersion;
						string tmpVersionMetadataJsonPath = GetTempServerRootDir() + "\\" + GetZippedFilename(cMetaDataFilename);
						FolderData tempVersionMetadata = new FolderData();
						if (!PopulateFolderDataFromZippedJson(tempVersionMetadata, tmpVersionMetadataJsonPath))
							return UploadPatchesResult.FailedToUpdateLocalVersion;

						List<string> affectedUpdatedFiles = new List<string>();

						foreach (var af in tempVersionMetadata.AddedFiles)
							if (!localAddedRelativePaths.Contains(af.RelativePath)
								|| UserMessages.Confirm("Local file added and online file added with same name, replace local with online?" + Environment.NewLine + GetAbsolutePath(af)))
							{
								//TODO: Must log if unable to add(download) file, if user chose to not replace, what happens with online new file?
								if (!DownloadFile(Path.GetDirectoryName(GetAbsolutePath(af)), GetAboluteServerUri(af)))
									UserMessages.ShowErrorMessage("Unable to download added file from server: " + GetAboluteServerUri(af));
								affectedUpdatedFiles.Add(af.RelativePath);
								File.Copy(GetAbsolutePath(af), GetLocalOriginalFilePath(af));
							}
						foreach (var rf in tempVersionMetadata.RemovedFiles)
						{
							//TODO: Must log if unable to remove file
							File.Delete(GetAbsolutePath(rf));
							affectedUpdatedFiles.Add(rf.RelativePath);
							//TODO: Is this really the safest way, should it not be moved to "deleted" folder?
							File.Delete(GetLocalOriginalFilePath(rf));
						}

						//Must look in each version for patches, as file1 might have patch in Version1 but file2&3 have patches in version2
						//if (i == serverVersion.Value)
						//{
						bool? userAgreedToDiscardLocalChanges = null;
						List<string> addedFilesInOnlineVersionRelativeList = new List<string>();
						foreach (var af in tempVersionMetadata.AddedFiles)
							addedFilesInOnlineVersionRelativeList.Add(af.RelativePath);

						//List<int> patchedIndexes = new List<int>();
						foreach (var f in tempVersionMetadata.FilesData)
						//for (int j = 0; j < tempVersionMetadata.FilesData.Length; j++)
						{
							//var f = tempVersionMetadata.FilesData[j];
							if (f.HasPatch && !addedFilesInOnlineVersionRelativeList.Contains(f.RelativePath, StringComparer.InvariantCultureIgnoreCase))
							{
								if (userAgreedToDiscardLocalChanges == null && localChangesRelativePaths.Contains(GetAbsolutePath(f)))
									userAgreedToDiscardLocalChanges = UserMessages.Confirm("There are local changes and also outstanding changes from server, discard local changes?");
								//return UploadPatchesResult.UserCancelledDoNotWantToLoseLocalChanges;

								string destinationPath = GetAbsolutePath(f);
								bool isconflict = false;
								if (userAgreedToDiscardLocalChanges != false && localChangesRelativePaths.Contains(GetAbsolutePath(f)))
									isconflict = true;
								if (isconflict)
									destinationPath =
										Path.GetDirectoryName(destinationPath).TrimEnd('\\') + "\\"
										+ Path.GetFileNameWithoutExtension(destinationPath)
										+ "Conflict (version {0})".Fmt(i)//"Conflict (version {0} on {1})".Fmt(i, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
										+ Path.GetExtension(destinationPath);
								string patchfilepath = GetTempServerRootPathcesDir() + "\\" + f.RelativePath + cPatchFileExtension;
								if (!DownloadFile(Path.GetDirectoryName(patchfilepath), GetServerVersionFolderUri(i) + "/" + f.RelativePath.Replace("\\", "/") + cPatchFileExtension))
									return UploadPatchesResult.FailedToUpdateLocalVersion;
								if (!ApplyPatch(GetLocalOriginalFilePath(f), patchfilepath, destinationPath))
									//TODO: Must check feedback of xDelta3, it might have an error...
									return UploadPatchesResult.FailedToUpdateLocalVersion;
								else if (!isconflict)//Conflicts are not updated??
									//patchedIndexes.Add(j);
									affectedUpdatedFiles.Add(f.RelativePath);
							}
						}

						if (affectedUpdatedFiles.Count > 0)
						{
							if (!File.Exists(GetZippedFilename(GetMetadataFullpathLocal()))
								|| this.FilesData == null)
							{
								this.SaveJsonLocallyReturnZippedPath();
								hasLocalChanges = false;
							}
							else
							{
								FolderData tmpSavedData = new FolderData();
								if (!PopulateFolderDataFromZippedJson(tmpSavedData, GetZippedFilename(GetMetadataFullpathLocal())))
									return UploadPatchesResult.FailedToUpdateLocalMetadataAfterServerdownload;

								foreach (var fm in tmpSavedData.FilesData)
									if (affectedUpdatedFiles.Contains(fm.RelativePath, StringComparer.InvariantCultureIgnoreCase))
										fm.RecalculateDetails(FolderPath);//.Modified = new FileInfo(GetAbsolutePath(fm)).LastWriteTime;

								foreach (var fm in this.FilesData)
									if (affectedUpdatedFiles.Contains(fm.RelativePath, StringComparer.InvariantCultureIgnoreCase))
										fm.RecalculateDetails(FolderPath);//.Modified = new FileInfo(GetAbsolutePath(fm)).LastWriteTime;

								tmpSavedData.SaveJsonLocallyReturnZippedPath();
								//this.SaveJsonLocallyReturnZippedPath();
								hasLocalChanges = this.HasLocalChanges(out localChangesRelativePaths, out localAddedRelativePaths, out localRemovedRelativePaths);
							}
						}
						//foreach (var pf in affectedUpdatedFiles)

						//Must;//If a patch is applied, update _cached metadata with this version
						//}

						File.WriteAllText(GetLocalVersionFileFullpath(), i.ToString());
						localVersion = GetLocalVersion();
					}

					//Download patches from server, only version serverVersion.Value is needed as its the patch compared to original
					//Apply these patches, but if user hasLocalChanges (see this variable above) then prompt to discard these local changes as newer version on server

					//Now the local machine is up to the server's date
					//return UploadPatchesResult.ServerVersionNewer;
				}

				if (!hasLocalChanges)//Also already checked server for newer version
					return UploadPatchesResult.NoLocalChanges;

				if (!LockServer())
					return UploadPatchesResult.UnableToLock;

				int? newVersion = GetNewVersionFromserver(localVersion.Value);
				if (newVersion == null)
					return UploadPatchesResult.FailedAddingNewVersionFolder;

				//string serverNewVersionFolder = GetServerVersionFolderUri(newVersion.Value);

				PopulateAddedFilesLocally();
				PopulateRemovedFilesLocally();
				if (!UploadFilesWithPatches(newVersion.Value))
					return UploadPatchesResult.FailedUploadingPatches;

				//First implement next line this before testing again
				if (!CheckForAddedOrRemovedFiles())
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