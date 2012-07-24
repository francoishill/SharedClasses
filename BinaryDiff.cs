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
		public static string ThisAppName = "AutoSync";

		private const string xdelta3Path = @"C:\Windows\xdelta3.exe";
		//private const string MetadataTablename = "metadata";

		public enum XDelta3Command { MakePatch, ApplyPatch };
		public static bool DoXDelta3Command(XDelta3Command command, string file1, string file2, string file3, TextFeedbackEventHandler textFeedbackHandler)
		{
			if (!File.Exists(xdelta3Path))
			{
				NetworkInterop.FtpDownloadFile(
					null,
					Path.GetDirectoryName(xdelta3Path),
					OnlineSettings.OnlineAppsSettings.Instance.AppsDownloadFtpUsername,//GlobalSettings.VisualStudioInteropSettings.Instance.FtpUsername,
					OnlineSettings.OnlineAppsSettings.Instance.AppsDownloadFtpPassword,//GlobalSettings.VisualStudioInteropSettings.Instance.FtpPassword,
					OnlineSettings.AutoSyncSettings.Instance.OnlineXDeltaExeFileUrl,
					textFeedbackHandler);
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

				List<string> outputs;
				List<string> errors;
				bool? result = SharedClasses.ProcessesInterop.StartProcessCatchOutput(
					new System.Diagnostics.ProcessStartInfo(
						xdelta3Path,
						string.Format("{0} \"{1}\" \"{2}\" \"{3}\"", commandStr, file1, file2, file3)),
					out outputs,
					out errors);

				if (result == true)//Successfully ran with no errors/output
					return true;
				else if (result == null)//Successfully ran, but had some errors/output
				{
					string errMsgesConcated = "";
					if (outputs.Count > 0)
						errMsgesConcated += string.Join("|", outputs);
					if (errors.Count > 0)
						errMsgesConcated += (errMsgesConcated.Length > 0 ? "|" : "") + string.Join("|", errors);
					TextFeedbackEventArgs.RaiseSimple(textFeedbackHandler, "There were errors when trying to " + command + " using xDelta3: " + errMsgesConcated, TextFeedbackType.Error);
					return false;
				}
				else// if (result == false)//Unable to run process
				{
					TextFeedbackEventArgs.RaiseSimple(textFeedbackHandler, "Unable to patch using xDelta3 (" + xdelta3Path + "), could not start process", TextFeedbackType.Error);
					return false;
				}
			}
			catch (Exception exc)
			{
				UserMessages.ShowWarningMessage("Cannot run xDelta3 command '" + command.ToString() + "': " + exc.Message);
				return false;
			}
		}

		public static bool MakePatch(string oldfile, string newfile, string deltafile, TextFeedbackEventHandler textFeedbackHandler)
		{
			return DoXDelta3Command(XDelta3Command.MakePatch, oldfile, newfile, deltafile, textFeedbackHandler);
		}

		public static bool ApplyPatch(string originalfile, string difffile, string patchedfile, TextFeedbackEventHandler textFeedbackHandler)
		{
			return DoXDelta3Command(XDelta3Command.ApplyPatch, originalfile, difffile, patchedfile, textFeedbackHandler);
		}

		private static int? _AutoSyncFtpPortToUse = null;
		public static int AutoSyncFtpPortToUse
		{
			get
			{
				if (_AutoSyncFtpPortToUse.HasValue)
					return _AutoSyncFtpPortToUse.Value;

				_AutoSyncFtpPortToUse = 21;
				if (File.Exists(GetFilePathToSettingOfAutoSyncFtpPort()))
				{
					int tmpint;
					string filecontent = File.ReadAllText(GetFilePathToSettingOfAutoSyncFtpPort());
					if (int.TryParse(filecontent, out tmpint))
						_AutoSyncFtpPortToUse = tmpint;
					else
						UserMessages.ShowWarningMessage("Setting for Port number in file is not a valid integer, will use default of " + _AutoSyncFtpPortToUse.Value
							+ Environment.NewLine + "File content: " + filecontent);
				}
				return _AutoSyncFtpPortToUse.Value;
			}
			set
			{
				_AutoSyncFtpPortToUse = value;
				File.WriteAllText(GetFilePathToSettingOfAutoSyncFtpPort(), value.ToString());
			}
		}
		public static string GetFilePathToSettingOfAutoSyncFtpPort()
		{
			return SettingsInterop.GetFullFilePathInLocalAppdata("AutoSyncFtpPortToUse.fjset", BinaryDiff.ThisAppName);
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

				int retrycount = 0;
				int retrymax = 5;
				retryhere:
				try
				{
					this.MD5Hash = localFileinfo.FullName.FileToMD5Hash();
				}
				catch (Exception exc)
				{
					Thread.Sleep(2000);
					if (retrycount++ < retrymax)
						goto retryhere;
					//TODO: Obviously this is super Unuserfriendly
					UserMessages.ShowErrorMessage("Cannot obtain file MD5Hash, please solve the problem and then click OK: " + exc.Message);
					retrycount = 0;
					goto retryhere;
				}
				
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
			private const string cNewServerHowToUseFileName = "How to use.txt";
			public const string cMonExtension = ".amon";

			private string _folderpath;
			public string LocalFolderPath { get { return _folderpath; } set { _folderpath = value.TrimEnd('\\'); } }
			private string _serverrooturi;
			public string ServerRootUri { get { return _serverrooturi; } set { _serverrooturi = value.TrimEnd('/'); } }
			//public string UserFolderName;
			public/*private*/ string FtpUsername;
			public/*private*/ string FtpPassword;
			private /*FileMetaData[]*/List<FileMetaData> cachedMetadata;
			//public bool IsLocal;
			public /*FileMetaData[]*/List<FileMetaData> FilesData;
			public List<FileMetaData> AddedFiles;
			public List<FileMetaData> RemovedFiles;
			public FolderData() { }
			public FolderData(string LocalFolderPath, string ServerRootUri, /*string UserFolderName, *//*bool IsLocal,*/ /*FileMetaData[]*/List<FileMetaData> FilesData, string FtpUsername, string FtpPassword)
			{
				this.LocalFolderPath = LocalFolderPath.TrimEnd('\\');
				this.ServerRootUri = ServerRootUri;
				//this.UserFolderName = UserFolderName;
				//this.IsLocal = IsLocal;
				this.FilesData = FilesData;
				this.FtpUsername = FtpUsername;
				this.FtpPassword = FtpPassword;

				if (FilesData == null)
					PopulateFilesData();
			}

			private FileSystemWatcher folderWatcher = null;
			private Action<FileSystemEventArgs> ActionOnFile_Changed_Created_Deleted = null;
			private Action<RenamedEventArgs> ActionOnFile_Renamed = null;
			public void StartFolderWatcher(Action<FileSystemEventArgs> ActionOnFile_Changed_Created_Deleted, Action<RenamedEventArgs> ActionOnFile_Renamed)
			{
				if (folderWatcher == null)
				{
					this.ActionOnFile_Changed_Created_Deleted = ActionOnFile_Changed_Created_Deleted;
					this.ActionOnFile_Renamed = ActionOnFile_Renamed;
					folderWatcher = new FileSystemWatcher(LocalFolderPath, "*");
					folderWatcher.IncludeSubdirectories = true;
					folderWatcher.Changed += folderWatcher_Changed_Created_Deleted;
					folderWatcher.Created += folderWatcher_Changed_Created_Deleted;
					folderWatcher.Deleted += folderWatcher_Changed_Created_Deleted;
					folderWatcher.Renamed += folderWatcher_Renamed;
					folderWatcher.EnableRaisingEvents = true;
				}
			}

			private bool MustPathBeIgnored(string path)
			{
				return path.StartsWith(GetCacheFolder(), StringComparison.InvariantCultureIgnoreCase);
			}

			private void folderWatcher_Changed_Created_Deleted(object sender, FileSystemEventArgs e)
			{
				//TODO: Be very careful here, this event gets fired 3 times on when a file is modified/changed
				if (MustPathBeIgnored(e.FullPath))
					return;
				if (Path.GetFileName(e.FullPath).StartsWith("~$w", StringComparison.InvariantCultureIgnoreCase))
					//TODO: Word temp files excluded starting with ~$w
					return;
				if (ActionOnFile_Changed_Created_Deleted != null)
					ActionOnFile_Changed_Created_Deleted(e);
			}

			private void folderWatcher_Renamed(object sender, RenamedEventArgs e)
			{
				if (MustPathBeIgnored(e.FullPath))
					return;
				if (ActionOnFile_Renamed != null)
					ActionOnFile_Renamed(e);
			}

			public void RemoveFolderWatcher()
			{
				if (folderWatcher != null)
				{
					folderWatcher.Changed -= folderWatcher_Changed_Created_Deleted;
					folderWatcher.Created -= folderWatcher_Changed_Created_Deleted;
					folderWatcher.Deleted -= folderWatcher_Changed_Created_Deleted;
					folderWatcher.Renamed -= folderWatcher_Renamed;
				}
			}

			public static string GetFolderForStoringMonitoredFolders()
			{
				string dir = SettingsInterop.LocalAppdataPath(ThisAppName + "\\MonitoredFolders").TrimEnd('\\');
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
				return dir;
			}
			public static string GetLocalMonitoredPathFromEncodedFilename(string encodedFilenameFullPath)
			{
				return FileSystemInterop.FilenameDecodeToValid(Path.GetFileNameWithoutExtension(encodedFilenameFullPath));
			}

			public string _GetFilePathForRegisteringAsMonitored()
			{
				return GetFolderForStoringMonitoredFolders() + "\\" + FileSystemInterop.FilenameEncodeToValid(GetZippedFilename(this.GetMetadataFullpathLocal())) + cMonExtension;
			}
			public void RegisterAsMonitoredPath()
			{
				File.Create(_GetFilePathForRegisteringAsMonitored()).Close();
			}
			public void UnregisterAsMonitoredPath()
			{
				File.Delete(_GetFilePathForRegisteringAsMonitored());
			}

			public void PopulateFilesData()
			{
				if (!Directory.Exists(LocalFolderPath))
					return;

				List<string> localfiles = Directory.GetFiles(LocalFolderPath, "*", SearchOption.AllDirectories)
					//.Select(s => s.ToLower())
					.Where(f => !f.StartsWith(LocalFolderPath + "\\" + cCachedSubfolderName, StringComparison.InvariantCultureIgnoreCase))
					.ToList();
				localfiles.Sort();

				this.FilesData = new List<FileMetaData>();//new FileMetaData[localfiles.Count];
				int substringStartPos = (LocalFolderPath.TrimEnd('\\') + "\\").Length;
				for (int i = 0; i < localfiles.Count; i++)
				{
					if (Path.GetFileName(localfiles[i]).StartsWith("~$w", StringComparison.InvariantCultureIgnoreCase))
						//TODO: Word temp files excluded starting with ~$w
						continue;

					var fileinfo = new FileInfo(localfiles[i]);
					//this.FilesData[i] =
					this.FilesData.Add(new FileMetaData(
						localfiles[i].Substring(substringStartPos),
						fileinfo));
				}
			}

			public static bool PopulateFolderDataFromZippedJson(FolderData folderData, string zippedJsonPath, TextFeedbackEventHandler textFeedbackHandler)
			{
				JSON.SetDefaultJsonInstanceSettings();
				string unzippedpath = DecompressFile(zippedJsonPath, textFeedbackHandler);
				if (unzippedpath == null)
					UserMessages.ShowWarningMessage("Cannot unzip file (cannot fill folderData): " + zippedJsonPath);
				else
				{
					int retrycount = 0;
					int retrymax = 5;
				retryhere:
					try
					{
						JSON.Instance.FillObject(folderData, File.ReadAllText(unzippedpath));
						if (!File.Exists(unzippedpath))
							File.Delete(unzippedpath);
						return true;
					}
					catch (Exception exc)
					{
						Thread.Sleep(2000);
						if (retrycount++ < retrymax)
							goto retryhere;
						TextFeedbackEventArgs.RaiseSimple(textFeedbackHandler, "Error occurred (retried " + retrymax + " times), cannot populate metadata from JSON: " + exc.Message, TextFeedbackType.Error);
					}
				}
				return false;
			}

			public List<FileMetaData>/*FileMetaData[]*/ GetLastCachedFolderData(TextFeedbackEventHandler textFeedbackHandler)
			{
				if (cachedMetadata != null)
					return cachedMetadata;

				//FileMetaData[] filesMetadata = new FileMetaData[0];
				List<FileMetaData> filesMetadata = new List<FileMetaData>();
				FolderData folderData = new FolderData(
					this.LocalFolderPath, this.ServerRootUri, /*this.UserFolderName,*/ filesMetadata, this.FtpUsername, this.FtpPassword);

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
					if (PopulateFolderDataFromZippedJson(folderData, zippedfilepath, textFeedbackHandler))
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
				return "{0}\\{1}\\{2}".Fmt(LocalFolderPath.TrimEnd('\\'), cCachedSubfolderName, cMetaDataFilename);
			}

			private string GetTempServerVersionFileFullpathLocal()
			{
				return GetTempServerRootDir() + "\\" + cServerVersionFilename;
			}

			private string GetLocalVersionFileFullpath()
			{
				return "{0}\\{1}\\{2}".Fmt(LocalFolderPath.TrimEnd('\\'), cCachedSubfolderName, cLocalVersionFilename);
			}

			/*private string GetRootUserfolderUri()
			{
				return this.ServerRootUri + "/" + this.UserFolderName;
			}*/

			private string GetOnlineNewestVersionFileUri()
			{
				//return GetRootUserfolderUri() + "/" + cServerVersionFilename;
				return ServerRootUri + "/" + cServerVersionFilename;
			}

			private string GetServerLockFileUri()
			{
				//return GetRootUserfolderUri() + "/" + cServerLockFilename;
				return ServerRootUri + "/" + cServerLockFilename;
			}

			private string GetServerVersionFolderUri(int newVersion)
			{
				//return GetRootUserfolderUri() + "/Patches/Version" + newVersion;
				return ServerRootUri + "/Patches/Version" + newVersion;
			}

			/*public string GetMetadataFullpathServer(int version)
			{
				return GetRelativeVersionDir(version);
			}*/

			private string GetAbsolutePath(FileMetaData file)
			{
				return LocalFolderPath + "\\" + file.RelativePath;
			}

			private string GetAboluteServerUri(FileMetaData file)
			{
				//return GetRootUserfolderUri() + "/Original/" + file.RelativePath.Replace("\\", "/");
				return ServerRootUri + "/Original/" + file.RelativePath.Replace("\\", "/");
			}

			private string GetCacheFolder()
			{
				return "{0}\\{1}".Fmt(this.LocalFolderPath, cCachedSubfolderName);
			}

			private string GetLocalOriginalFilePath(FileMetaData file)
			{
				string filepath = GetCacheFolder() + "\\Original\\{0}".Fmt(file.RelativePath);
				string dir = Path.GetDirectoryName(filepath);
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
				return filepath;
			}

			public int? GetServerCurrentVersion(TextFeedbackEventHandler textFeedbackHandler/*string RootFolder, */)
			{
				int retrycount = 0;
				int retrymax = 5;
			retryhere:
				try
				{
					//int? max = null;

					string tmpFile = GetTempServerVersionFileFullpathLocal();
					if (File.Exists(tmpFile))
						File.Delete(tmpFile);
					//while (File.Exists(tmpFile))
					//{
					//    Thread.Sleep(300);
					//    File.Delete(tmpFile);
					//}
					if (!DownloadFile(Path.GetDirectoryName(tmpFile).TrimEnd('\\'), GetOnlineNewestVersionFileUri(), textFeedbackHandler))
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
				catch (Exception exc)
				{
					Thread.Sleep(2000);
					if (retrycount++ < retrymax)
						goto retryhere;
					TextFeedbackEventArgs.RaiseSimple(textFeedbackHandler, "Error occurred (retried " + retrymax + " times), cannot get server version: " + exc.Message, TextFeedbackType.Error);
					return null;
				}
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

			private Dictionary<string, FileMetaData> GetTempCachedFileAndMetadataDictionary(TextFeedbackEventHandler textFeedbackHandler)
			{
				//FileMetaData[] cachedMetadata = GetLastCachedFolderData();
				List<FileMetaData> cachedMetadata = GetLastCachedFolderData(textFeedbackHandler);
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

			private bool HasLocalChanges(out List<string> changedRelativePaths, out List<string> addedRelativePaths, out List<string> removedRelativePaths, TextFeedbackEventHandler textFeedbackHandler)
			{
				changedRelativePaths = new List<string>();
				addedRelativePaths = new List<string>();
				removedRelativePaths = new List<string>();
				var tmpcacheddict = GetTempCachedFileAndMetadataDictionary(textFeedbackHandler);

				if (this.FilesData == null)
					return false;//TODO: No local changes if no local metadata file yet?

				if (tmpcacheddict == null)
				{
					var locver = GetLocalVersion();
					if (!locver.HasValue || locver.Value == 1)
						this.SaveJsonLocallyReturnZippedPath(textFeedbackHandler);
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
					NetworkInterop.InsertPortNumberIntoUrl(GetServerVersionFolderUri(newVersion) + "/" + Path.GetDirectoryName(file.RelativePath).Replace("\\", "/"), AutoSyncFtpPortToUse),
					FtpUsername,
					FtpPassword,
					new string[] { patchfiledir });
			}

			public bool UploadFilesWithPatches(int newVersion, TextFeedbackEventHandler textFeedbackHandler)
			{
				if (!Directory.Exists(GetPatchesRootDir()))
					Directory.CreateDirectory(GetPatchesRootDir());

				var localPatchesDir = GetCacheFolder() + "\\Patches";
				Directory.Delete(localPatchesDir, true);
				Directory.CreateDirectory(localPatchesDir);

				var tmpcacheddict = GetTempCachedFileAndMetadataDictionary(textFeedbackHandler);
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
						if (tmpcacheddict != null && tmpcacheddict.ContainsKey(f.RelativePath))
						{
							MakePatch(GetLocalOriginalFilePath(f), GetAbsolutePath(f), patchfile, textFeedbackHandler);
							if (!UploadPatch(f, patchfile, newVersion))
								return false;
						}
						//patchesMade.Add(patchfile);
					}
				}

				string zippedJsonpathLocal = SaveJsonLocallyReturnZippedPath(textFeedbackHandler);

				cachedMetadata = null;

				if (zippedJsonpathLocal == null)
					return false;

				//Upload the json file of metadata up to server
				return NetworkInterop.FtpUploadFiles(
					null,
					NetworkInterop.InsertPortNumberIntoUrl(GetServerVersionFolderUri(newVersion) + "/", AutoSyncFtpPortToUse),// + Path.GetDirectoryName(file.RelativePath).Replace("\\", "/"),
					FtpUsername,
					FtpPassword,
					new string[] { zippedJsonpathLocal });
			}

			private string _SaveJsonReturnZippedPath(string path, TextFeedbackEventHandler textFeedbackHandler)
			{
				JSON.SetDefaultJsonInstanceSettings();
				var json = WebInterop.GetJsonStringFromObject(this, false);
				File.WriteAllText(path, json);
				string zippedPath = CompressFile(path, textFeedbackHandler);
				File.Delete(path);
				return zippedPath;
			}

			public string SaveJsonLocallyReturnZippedPath(TextFeedbackEventHandler textFeedbackHandler)
			{
				return _SaveJsonReturnZippedPath(GetMetadataFullpathLocal(), textFeedbackHandler);
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
				return NetworkInterop.FtpFileExists(NetworkInterop.InsertPortNumberIntoUrl(this.GetServerLockFileUri(), AutoSyncFtpPortToUse), FtpUsername, FtpPassword) == true;
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

			private bool LockServer(TextFeedbackEventHandler textFeedbackHandler)
			{
				int retrycount = 0;
				int retrymax = 5;
			retryhere:
				try
				{
					string tmpFilePath = Path.GetTempPath().TrimEnd('\\') + "\\" + cServerLockFilename;
					byte[] guidBytes = Guid.NewGuid().ToByteArray();
					File.WriteAllBytes(tmpFilePath, guidBytes);
					//TODO: FtpUploadFiles returns boolean, is this returned value trustworthy? See steps in upload method itsself
					if (!NetworkInterop.FtpUploadFiles(null, NetworkInterop.InsertPortNumberIntoUrl(ServerRootUri/*GetRootUserfolderUri()*/, AutoSyncFtpPortToUse), FtpUsername, FtpPassword, new string[] { tmpFilePath }))
						return false;
					if (!DownloadFile(Path.GetDirectoryName(tmpFilePath), /*GetRootUserfolderUri()*/ServerRootUri + "//" + cServerLockFilename, textFeedbackHandler))
						return false;
					return BytesArraysEqual(File.ReadAllBytes(tmpFilePath), guidBytes);
				}
				catch (Exception exc)
				{
					Thread.Sleep(2000);
					if (retrycount++ < retrymax)
						goto retryhere;
					TextFeedbackEventArgs.RaiseSimple(textFeedbackHandler, "Error occurred (retried " + retrymax + " times), cannot lock server: " + exc.Message, TextFeedbackType.Error);
					return false;
				}
			}

			private bool UnlockServer()
			{
				return NetworkInterop.DeleteFTPfile(
					null,
					NetworkInterop.InsertPortNumberIntoUrl(GetServerLockFileUri(), AutoSyncFtpPortToUse),
					FtpUsername,
					FtpPassword);
			}

			private int? GetNewVersionFromserver(int currentVersion, TextFeedbackEventHandler textFeedbackHandler)
			{
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler,
					"{" + this.LocalFolderPath + "} Requesting new version folder from server (current version = " + currentVersion + ")", TextFeedbackType.Subtle);

				int newVersionNum = currentVersion + 1;
				string newversionUri = NetworkInterop.InsertPortNumberIntoUrl(GetServerVersionFolderUri(newVersionNum), AutoSyncFtpPortToUse);
				//if (NetworkInterop.FtpDirectoryExists(newversionUri, FtpUsername, FtpPassword))
				if (!NetworkInterop.RemoveFTPDirectory(newversionUri, FtpUsername, FtpPassword))
					//UserMessages.ShowErrorMessage("Could not remove FTP directory: " + newversionUri);
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "{" + this.LocalFolderPath + "} Could not remove FTP directory: " + newversionUri, TextFeedbackType.Error);

				if (!NetworkInterop.CreateFTPDirectory(newversionUri, FtpUsername, FtpPassword))
				{
					//UserMessages.ShowErrorMessage("Could not create new version dir: " + newversionUri);
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "{" + this.LocalFolderPath + "} Could not create new version dir: " + newversionUri, TextFeedbackType.Error);
					return null;
				}
				else
					return newVersionNum;
				//return null;
			}

			private bool UpateServerVersionFile(int newVersion)
			{
				string onlineVersionFileLocalTempPath = GetTempServerVersionFileFullpathLocal();
				string onlineVersionUri = NetworkInterop.InsertPortNumberIntoUrl(ServerRootUri, AutoSyncFtpPortToUse);//GetRootUserfolderUri();//Path.GetDirectoryName(GetOnlineNewestVersionFileUri().Replace("/", "\\")).Replace("\\", "/");
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

			private void PopulateAddedFilesLocally(TextFeedbackEventHandler textFeedbackHandler)
			{
				AddedFiles = new List<FileMetaData>();
				var tmpcacheddict = GetTempCachedFileAndMetadataDictionary(textFeedbackHandler);
				if (tmpcacheddict == null)
					return;
				var tmplocaldict = GetTempDictForLocalFilesMetadata();
				foreach (string f in tmplocaldict.Keys)
					if (!tmpcacheddict.ContainsKey(f)
						&& !Path.GetFileName(f).StartsWith("~$w", StringComparison.InvariantCultureIgnoreCase))
					{
						//TODO: Word temp files excluded starting with ~$w
						AddedFiles.Add(tmplocaldict[f]);

						bool alreadyInFilesData = false;
						for (int i = 0; i < FilesData.Count; i++)
							if (FilesData[i].RelativePath.Equals(tmplocaldict[f].RelativePath, StringComparison.InvariantCultureIgnoreCase))
								alreadyInFilesData = true;
						if (!alreadyInFilesData)
							FilesData.Add(tmplocaldict[f]);
					}
			}

			private void PopulateRemovedFilesLocally(TextFeedbackEventHandler textFeedbackHandler)
			{
				RemovedFiles = new List<FileMetaData>();
				var tmpcacheddict = GetTempCachedFileAndMetadataDictionary(textFeedbackHandler);
				if (tmpcacheddict == null)
					return;
				var tmplocaldict = GetTempDictForLocalFilesMetadata();
				foreach (string f in tmpcacheddict.Keys)
					if (!tmplocaldict.ContainsKey(f)
						&& !Path.GetFileName(f).StartsWith("~$w", StringComparison.InvariantCultureIgnoreCase))
					{
						//TODO: Word temp files excluded starting with ~$w
						RemovedFiles.Add(tmpcacheddict[f]);
						if (FilesData != null)
							for (int i = 0; i < FilesData.Count; i++)
								if (FilesData[i].RelativePath.Equals(tmpcacheddict[f].RelativePath, StringComparison.InvariantCultureIgnoreCase))
								{
									FilesData.RemoveAt(i);
									break;
								}
					}
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
						NetworkInterop.InsertPortNumberIntoUrl(/*GetRootUserfolderUri()*/ServerRootUri + "/Original/" + Path.GetDirectoryName(af.RelativePath).Replace("\\", "/"), AutoSyncFtpPortToUse),
						FtpUsername,
						FtpPassword,
						new string[] { GetAbsolutePath(af) }))
						return false;
				}

				foreach (var rf in RemovedFiles)
				{
					//TODO: It might be dangerous to delete a file from the server?
					//Rather move it to "deleted" folder on the server???
					File.Delete(GetLocalOriginalFilePath(rf));
					/*if (!NetworkInterop.DeleteFTPfile(
						null,
						NetworkInterop.InsertPortNumberIntoUrl(GetRootUserfolderUri() + "/Original/" + rf.RelativePath.Replace("\\", "/"), AutoSyncFtpPortToUse),
						FtpUsername,
						FtpPassword))
						return false;*/
				}

				return true;
			}

			public bool DownloadFile(string localRoot, string onlineFileFullUrl, TextFeedbackEventHandler textFeedbackHandler)
			{
				return NetworkInterop.FtpDownloadFile(null, localRoot, FtpUsername, FtpPassword, NetworkInterop.InsertPortNumberIntoUrl(onlineFileFullUrl, AutoSyncFtpPortToUse), textFeedbackHandler) != null;
			}

			public void InitialSetupLocally()
			{
				if (!Directory.Exists(LocalFolderPath))
					Directory.CreateDirectory(LocalFolderPath);
				if (!File.Exists(GetLocalVersionFileFullpath()))
				{
					if (!Directory.Exists(Path.GetDirectoryName(GetLocalVersionFileFullpath())))
						Directory.CreateDirectory(Path.GetDirectoryName(GetLocalVersionFileFullpath()));
					File.WriteAllText(GetLocalVersionFileFullpath(), "0".ToString());
				}
			}

			public bool InitiateSyncing(TextFeedbackEventHandler textFeedbackHandler)
			{
				bool? createDir_NullIfExisted = NetworkInterop.CreateFTPDirectory_NullIfExisted(NetworkInterop.InsertPortNumberIntoUrl(this.ServerRootUri, AutoSyncFtpPortToUse), this.FtpUsername, this.FtpPassword);
				if (!createDir_NullIfExisted.HasValue)
				{
					//Directory already existed
					bool? serverVersionFileExists = NetworkInterop.FtpFileExists(NetworkInterop.InsertPortNumberIntoUrl(ServerRootUri + "/" + cServerVersionFilename, AutoSyncFtpPortToUse), this.FtpUsername, this.FtpPassword);
					if (!serverVersionFileExists.HasValue)
					{
						//Could not determine if server folder was valid syncing folder
						UserMessages.ShowErrorMessage("Unable to initiate syncing, server directory ('" + ServerRootUri + "') existed but could not determine whether it's a valid syncing directory");
						return false;
					}
					else if (serverVersionFileExists.Value == true)
					{
						//This is a valid syncing folder
						this.InitialSetupLocally();
						UploadPatchesResult result = this.UploadChangesToServer(textFeedbackHandler);
						if (result == UploadPatchesResult.Success || result == UploadPatchesResult.NoLocalChanges)
							return true;
						else
						{
							UserMessages.ShowWarningMessage("Unable to initiate syncing, could not sync from server after setting up folders");
							return false;
						}
					}
					else
					{
						//The folder exists but is not valid syncing folder (did not contain serverversion file)
						UserMessages.ShowErrorMessage("Unable to initiate syncing, server directory ('" + ServerRootUri + "') existed but is NOT a valid syncing directory, please choose another online folder.");
						return false;
					}
				}
				else if (createDir_NullIfExisted.Value == true)
				{
					//Server folder newly created
					List<string> folderToCreate = new List<string>();
					folderToCreate.Add(ServerRootUri + "/Original");
					folderToCreate.Add(ServerRootUri + "/Patches");
					folderToCreate.Add(ServerRootUri + "/Patches/Version1");
					foreach (string path in folderToCreate.Select(s => NetworkInterop.InsertPortNumberIntoUrl(s, AutoSyncFtpPortToUse)))
						if (!NetworkInterop.CreateFTPDirectory(path, this.FtpUsername, this.FtpPassword))
						{
							UserMessages.ShowWarningMessage("Unable to initiate syncing, could not create server folder: " + path);
							return false;
						}

					string tempFolder = Path.GetTempPath().TrimEnd('\\');

					string tempVersionPath = tempFolder + "\\" + cServerVersionFilename;
					File.WriteAllText(tempVersionPath, "1");
					if (!NetworkInterop.FtpUploadFiles(null, NetworkInterop.InsertPortNumberIntoUrl(this.ServerRootUri, AutoSyncFtpPortToUse), this.FtpUsername, this.FtpPassword, new string[] { tempVersionPath }))
					{
						UserMessages.ShowWarningMessage("Unable to initiate syncing, could not upload server version file: " + cServerVersionFilename);
						return false;
					}
					string tempHowToUseFile = tempFolder + "\\" + cNewServerHowToUseFileName;
					File.WriteAllText(tempHowToUseFile, "This file will eventually contain information of how to use AutoSync");
					if (!NetworkInterop.FtpUploadFiles(null, NetworkInterop.InsertPortNumberIntoUrl(this.ServerRootUri + "/Original", AutoSyncFtpPortToUse), this.FtpUsername, this.FtpPassword, new string[] { tempHowToUseFile }))
					{
						UserMessages.ShowWarningMessage("Unable to initiate syncing, could not upload server 'How to use' file: " + cNewServerHowToUseFileName);
						return false;
					}

					FileMetaData tmpHowToUseFileData = new FileMetaData(cNewServerHowToUseFileName, new FileInfo(tempHowToUseFile));
					if (this.FilesData == null)
						this.FilesData = new List<FileMetaData>();
					this.FilesData.Add(tmpHowToUseFileData);
					if (this.AddedFiles == null)
						this.AddedFiles = new List<FileMetaData>();
					this.AddedFiles.Add(tmpHowToUseFileData);
					string tempZippedJsonPath = this._SaveJsonReturnZippedPath(tempFolder + "\\" + cMetaDataFilename, textFeedbackHandler);
					if (!NetworkInterop.FtpUploadFiles(null, NetworkInterop.InsertPortNumberIntoUrl(this.ServerRootUri + "/Patches/Version1", AutoSyncFtpPortToUse), this.FtpUsername, this.FtpPassword, new string[] { tempZippedJsonPath }))
					{
						UserMessages.ShowWarningMessage("Unable to initiate syncing, could not upload server metadata file: " + cMetaDataFilename);
						return false;
					}

					File.Delete(tempVersionPath);
					File.Delete(tempHowToUseFile);
					File.Delete(tempZippedJsonPath);

					this.FilesData.Clear();
					this.AddedFiles.Clear();

					this.InitialSetupLocally();
					UploadPatchesResult result = this.UploadChangesToServer(textFeedbackHandler);
					if (result == UploadPatchesResult.Success || result == UploadPatchesResult.NoLocalChanges)
						return true;
					else
					{
						UserMessages.ShowWarningMessage("Unable to initiate syncing, could not sync from server after setting up folders");
						return false;
					}
				}
				else
				{
					//Error occurred, could not create directory or obtain whether existed
					UserMessages.ShowErrorMessage("Unable to initiate syncing, could not create or determine whether directory '" + ServerRootUri + "' exists");
					return false;
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
				FailedToApplyPatchLocally,
				FailedToUpdateLocalMetadataAfterServerdownload,
				FailedDownloadFromServer,
				//UserCancelledDoNotWantToLoseLocalChanges,
				//FailedApplyingPatches
			}
			public UploadPatchesResult UploadChangesToServer(TextFeedbackEventHandler textFeedbackHandler)
			{
				List<string> localChangesRelativePaths;
				List<string> localAddedRelativePaths;
				List<string> localRemovedRelativePaths;

				bool hasLocalChanges = this.HasLocalChanges(out localChangesRelativePaths, out localAddedRelativePaths, out localRemovedRelativePaths, textFeedbackHandler);

				int? localVersion = null;
				int? serverVersion = null;

				if (!hasLocalChanges)
				{
					localVersion = GetLocalVersion();
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "{" + this.LocalFolderPath + "} Local version = " + (localVersion.HasValue ? localVersion.Value.ToString() : "[NULL]"), TextFeedbackType.Subtle);
					serverVersion = GetServerCurrentVersion(textFeedbackHandler);//this.UserFolderName);
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "{" + this.LocalFolderPath + "} Server version = " + (serverVersion.HasValue ? serverVersion.Value.ToString() : "[NULL]"), TextFeedbackType.Subtle);

					if (serverVersion == null)
						return UploadPatchesResult.FailedObtainingServerVersion;
					else if (localVersion == null)
						return UploadPatchesResult.FailedObtainingLocalVersion;
					else if (localVersion.Value > serverVersion.Value)
						return UploadPatchesResult.InvalidLocalVersionNewerThanServer;
					else if (localVersion.Value == serverVersion.Value)
						return UploadPatchesResult.NoLocalChanges;
				}

				if (IsServerLocked())
					return UploadPatchesResult.ServerAlreadyLocked;

				if (hasLocalChanges)
				{
					localVersion = GetLocalVersion();
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "{" + this.LocalFolderPath + "} Local version = " + (localVersion.HasValue ? localVersion.Value.ToString() : "[NULL]"), TextFeedbackType.Subtle);
					serverVersion = GetServerCurrentVersion(textFeedbackHandler);//this.UserFolderName);
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "{" + this.LocalFolderPath + "} Server version = " + (serverVersion.HasValue ? serverVersion.Value.ToString() : "[NULL]"), TextFeedbackType.Subtle);
				}

				if (serverVersion == null)
					return UploadPatchesResult.FailedObtainingServerVersion;
				else if (localVersion == null)
					return UploadPatchesResult.FailedObtainingLocalVersion;
				else if (localVersion.Value > serverVersion.Value)
					return UploadPatchesResult.InvalidLocalVersionNewerThanServer;
				else if (localVersion.Value < serverVersion.Value)
				{
					//TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "{" + this.LocalFolderPath + "} Server version newer = " + serverVersion.Value + ", local version = " + localVersion.Value, TextFeedbackType.Subtle);

					//DONE: Cannot only download newest version, what about patched files previous versions?
					for (int i = localVersion.Value + 1; i <= serverVersion.Value; i++)
					{
						TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "{" + this.LocalFolderPath + "} Updating local version to " + i, TextFeedbackType.Subtle);
						if (!DownloadFile(GetTempServerRootDir(), GetServerVersionFolderUri(i) + "/" + GetZippedFilename(cMetaDataFilename), textFeedbackHandler))
							return UploadPatchesResult.FailedToUpdateLocalVersion;
						string tmpVersionMetadataJsonPath = GetTempServerRootDir() + "\\" + GetZippedFilename(cMetaDataFilename);
						FolderData tempVersionMetadata = new FolderData();
						if (!PopulateFolderDataFromZippedJson(tempVersionMetadata, tmpVersionMetadataJsonPath, textFeedbackHandler))
							return UploadPatchesResult.FailedToUpdateLocalVersion;

						tempVersionMetadata.LocalFolderPath = this.LocalFolderPath;

						List<string> affectedUpdatedFiles = new List<string>();

						foreach (var af in tempVersionMetadata.AddedFiles)
							if (!localAddedRelativePaths.Contains(af.RelativePath)
								|| UserMessages.Confirm("Local file added and online file added with same name, replace local with online?" + Environment.NewLine + GetAbsolutePath(af)))
							{
								//TODO: Must log if unable to add(download) file, if user chose to not replace, what happens with online new file?
								if (!DownloadFile(Path.GetDirectoryName(GetAbsolutePath(af)), GetAboluteServerUri(af), textFeedbackHandler))
								{
									//UserMessages.ShowErrorMessage("Unable to download added file from server: " + GetAboluteServerUri(af));
									TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "{" + this.LocalFolderPath + "} Unable to download added file from server: " + GetAboluteServerUri(af), TextFeedbackType.Error);
									return UploadPatchesResult.FailedDownloadFromServer;
								}
								affectedUpdatedFiles.Add(af.RelativePath);
								//if (File.Exists(GetLocalOriginalFilePath(af)))
								//    File.Delete(GetLocalOriginalFilePath(af));Overwrite in next line true
								File.Copy(GetAbsolutePath(af), GetLocalOriginalFilePath(af), true);
								File.SetLastWriteTime(GetAbsolutePath(af), af.Modified);
								File.SetLastWriteTime(GetLocalOriginalFilePath(af), af.Modified);
							}
						foreach (var rf in tempVersionMetadata.RemovedFiles ?? new List<FileMetaData>())
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
								if (!DownloadFile(Path.GetDirectoryName(patchfilepath), GetServerVersionFolderUri(i) + "/" + f.RelativePath.Replace("\\", "/") + cPatchFileExtension, textFeedbackHandler))
									return UploadPatchesResult.FailedDownloadFromServer;//UploadPatchesResult.FailedToUpdateLocalVersion;
								if (!ApplyPatch(GetLocalOriginalFilePath(f), patchfilepath, destinationPath, textFeedbackHandler))
									//TODO: Must check feedback of xDelta3, it might have an error...
									return UploadPatchesResult.FailedToApplyPatchLocally;//FailedToUpdateLocalVersion;
								else if (!isconflict)//Conflicts are not updated??
								{
									//patchedIndexes.Add(j);
									affectedUpdatedFiles.Add(f.RelativePath);
									File.SetLastWriteTime(destinationPath, f.Modified);
								}
							}
						}

						if (affectedUpdatedFiles.Count > 0)
						{
							if (!File.Exists(GetZippedFilename(GetMetadataFullpathLocal()))
								|| this.FilesData == null)
							{
								if (this.FilesData == null)
								{
									this.FilesData = tempVersionMetadata.FilesData;
									this.AddedFiles = tempVersionMetadata.AddedFiles;
									this.RemovedFiles = tempVersionMetadata.RemovedFiles;
								}
								//this.FilesData = tempVersionMetadata.FilesData;
								//this.SaveJsonLocallyReturnZippedPath();
								tempVersionMetadata.SaveJsonLocallyReturnZippedPath(textFeedbackHandler);
								hasLocalChanges = false;
							}
							else
							{
								/*FolderData tmpSavedData = new FolderData();
								if (!PopulateFolderDataFromZippedJson(tmpSavedData, GetZippedFilename(GetMetadataFullpathLocal())))
									return UploadPatchesResult.FailedToUpdateLocalMetadataAfterServerdownload;

								if (tmpSavedData.FilesData != null)
								{
									foreach (var fm in tmpSavedData.FilesData)
										if (affectedUpdatedFiles.Contains(fm.RelativePath, StringComparer.InvariantCultureIgnoreCase))
											fm.RecalculateDetails(FolderPath);//.Modified = new FileInfo(GetAbsolutePath(fm)).LastWriteTime;
								}

								if (this.FilesData != null)
								{
									foreach (var fm in this.FilesData)
										if (affectedUpdatedFiles.Contains(fm.RelativePath, StringComparer.InvariantCultureIgnoreCase))
											fm.RecalculateDetails(FolderPath);//.Modified = new FileInfo(GetAbsolutePath(fm)).LastWriteTime;
								}

								tmpSavedData.AddedFiles = tempVersionMetadata.AddedFiles;
								tmpSavedData.RemovedFiles = tempVersionMetadata.RemovedFiles;

								this.AddedFiles = tempVersionMetadata.AddedFiles;
								this.RemovedFiles = tempVersionMetadata.RemovedFiles;

								tmpSavedData.SaveJsonLocallyReturnZippedPath();
								//this.SaveJsonLocallyReturnZippedPath();*/
								this.FilesData = tempVersionMetadata.FilesData;
								this.AddedFiles = tempVersionMetadata.AddedFiles;
								this.RemovedFiles = tempVersionMetadata.RemovedFiles;
								this.SaveJsonLocallyReturnZippedPath(textFeedbackHandler);

								hasLocalChanges = this.HasLocalChanges(out localChangesRelativePaths, out localAddedRelativePaths, out localRemovedRelativePaths, textFeedbackHandler);
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

				if (!LockServer(textFeedbackHandler))
					return UploadPatchesResult.UnableToLock;

				int? newVersion = GetNewVersionFromserver(localVersion.Value, textFeedbackHandler);
				if (newVersion == null)
					return UploadPatchesResult.FailedAddingNewVersionFolder;

				//string serverNewVersionFolder = GetServerVersionFolderUri(newVersion.Value);

				PopulateAddedFilesLocally(textFeedbackHandler);
				PopulateRemovedFilesLocally(textFeedbackHandler);
				if (hasLocalChanges)
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "{" + this.LocalFolderPath + "} Uploading local changes to server");
				if (!UploadFilesWithPatches(newVersion.Value, textFeedbackHandler))
					return UploadPatchesResult.FailedUploadingPatches;

				//First implement next line this before testing again
				if (!CheckForAddedOrRemovedFiles())
					return UploadPatchesResult.FailedToAddOrRemoveFiles;

				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "{" + this.LocalFolderPath + "} Updating server version file (new version = " + newVersion + ") and finishing off before unlocking");
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

		public static string CompressFile(string originalFilepath, TextFeedbackEventHandler textFeedbackHandler)
		{
			string returnString = null;

			int retrycount = 0;
			int retrymax = 5;
		retryhere:
			try
			{
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
			}
			catch (Exception exc)
			{
				Thread.Sleep(2000);
				if (retrycount++ < retrymax)
					goto retryhere;
				TextFeedbackEventArgs.RaiseSimple(textFeedbackHandler, "Error occurred (retried " + retrymax + " times), cannot compress file: " + exc.Message, TextFeedbackType.Error);
			}
			return returnString;
		}

		public static string DecompressFile(string zipFilepath, TextFeedbackEventHandler textFeedbackHandler)
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
				int retrycount = 0;
				int retryMax = 5;
			retryhere:
				try
				{
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
				catch (Exception exc)
				{
					Thread.Sleep(2000);
					if (retrycount++ < retryMax)
						goto retryhere;
					else
						TextFeedbackEventArgs.RaiseSimple(textFeedbackHandler, "Error occurred (retried " + retryMax + " times), cannot decompress file: " + exc.Message, TextFeedbackType.Error);
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