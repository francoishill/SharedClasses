using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SharedClasses;
using System.Net;

namespace SharedClasses
{
	public class FolderDetails
	{
		private const int cMaxFilesToShowInUsermessages = 20;

		public string LocalRootDirectory;
		/*public string FtpRootUrl;
		public string FtpUsername;
		public string FtpPassword;*/
		public FileDetails[] Files;

		//Add support for excluded files
		//public List<string> ExcludedFiles;
		public List<string> ExcludedRelativeFolders;
		private NetworkCredential CredentailsIfRequired;

		public FolderDetails() { }
		public FolderDetails(string LocalRootDirectory, List<string> ExcludedRelativeFolders = null, NetworkCredential CredentailsIfRequired = null)
		{
			LocalRootDirectory = LocalRootDirectory.TrimEnd('\\');
			this.LocalRootDirectory = LocalRootDirectory;
			//this.FtpRootUrl = FtpRootUrl.TrimEnd('/', '\\');
			//this.FtpUsername = FtpUsername;
			//this.FtpPassword = FtpPassword;
			this.ExcludedRelativeFolders = ExcludedRelativeFolders;
			this.CredentailsIfRequired = CredentailsIfRequired;
			MakeNetworkConnectionIfHaveCredentials();
			RegenerateFilesList();
		}
		~FolderDetails()
		{
			if (connection != null)
				connection.Dispose();
		}

		private NetworkConnection connection = null;
		private void MakeNetworkConnectionIfHaveCredentials()
		{
			if (CredentailsIfRequired != null)
				connection = new NetworkConnection(LocalRootDirectory, CredentailsIfRequired);
		}

		public void RegenerateFilesList()
		{
			var files = new List<FileDetails>();
			string pathNoSlash = LocalRootDirectory;
			foreach (var f in Directory.GetFiles(pathNoSlash, "*", SearchOption.AllDirectories))
			{
				//Must maybe give user option to ignore .svn or not
				if (f.IndexOf(".svn", StringComparison.InvariantCultureIgnoreCase) == -1
					&& !MustPathBeExcluded(FileDetails.GetRelativePath(pathNoSlash, f)))
					files.Add(new FileDetails(pathNoSlash, f));
			}
			this.Files = files.ToArray();
		}

		public void SaveDetails(string overridePath = null)
		{
			JSON.SetDefaultJsonInstanceSettings();
			var json = JSON.Instance.ToJSON(this, false);
			File.WriteAllText(
				overridePath ?? GetCachedJsonFilepath(),
				json);
		}
		public void RemoveDetails()
		{
			if (File.Exists(GetCachedJsonFilepath()))
				File.Delete(GetCachedJsonFilepath());
		}

		public static FolderDetails CreateFromJsonFile(string filePath)
		{
			JSON.SetDefaultJsonInstanceSettings();
			FolderDetails tmpObj = new FolderDetails();
			if (JSON.Instance.FillObject(tmpObj, File.ReadAllText(filePath)) != null)
				return tmpObj;
			return null;
		}

		public string GetAbsolutePath(FileDetails fd)
		{
			return LocalRootDirectory.TrimEnd('\\') + "\\" + fd.RelativePath.TrimStart('\\');
		}

		//Null returned if something besides the filedetails failed
		public bool? CompareToCached(out Dictionary<string, FileDetails> newFiles, out Dictionary<string, FileDetails> deletedFiles, out Dictionary<string, FileDetails> changedFiles, out FolderDetails cachedDetails)
		{
			newFiles = null;
			deletedFiles = null;
			changedFiles = null;
			cachedDetails = null;

			if (!File.Exists(GetCachedJsonFilepath()))
				return false;

			JSON.SetDefaultJsonInstanceSettings();
			cachedDetails = new FolderDetails();
			if (JSON.Instance.FillObject(cachedDetails, File.ReadAllText(GetCachedJsonFilepath())) != null)
				return cachedDetails.CompareDetails(this, out newFiles, out deletedFiles, out changedFiles);
			return false;
		}

		public static string GetJsonFolderPath()
		{
			string dir = SettingsInterop.LocalAppdataPath("AutoUploadChangesToFtp").TrimEnd('\\');
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			return dir;
		}
		private string GetCachedJsonFilepath()
		{
			return GetJsonFolderPath() + "\\" + FileSystemInterop.FilenameEncodeToValid(LocalRootDirectory, err => UserMessages.ShowErrorMessage(err)) + ".json";
		}

		private bool MustPathBeExcluded(string relativePath)
		{
			if (ExcludedRelativeFolders == null)
				return false;
			foreach (var exclPath in ExcludedRelativeFolders)
				if (relativePath.StartsWith(exclPath.TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase))
					return true;
			return false;
		}

		/// <summary>
		/// //true=exact match,false=not match,null=something besides the filedetails did not match
		/// </summary>
		/// <param name="otherFolderDet"></param>
		/// <param name="newIn2"></param>
		/// <param name="missingIn2"></param>
		/// <param name="changedItems"></param>
		/// <returns></returns>
		public bool? CompareDetails(FolderDetails otherFolderDet, out Dictionary<string, FileDetails> newIn2, out Dictionary<string, FileDetails> missingIn2, out Dictionary<string, FileDetails> changedItems)
		{
			newIn2 = null;
			missingIn2 = null;
			changedItems = null;

			//Does not compare paths
			/*if (this.FtpRootUrl != otherFolderDet.FtpRootUrl)
				return null;*/
			if (!AreStringListsEqual(this.ExcludedRelativeFolders, otherFolderDet.ExcludedRelativeFolders))
				return null;

			if (this.Files == null)
			{
				newIn2 = new Dictionary<string, FileDetails>();
				foreach (var f in otherFolderDet.Files)
					newIn2.Add(f.RelativePath, f);

				changedItems = new Dictionary<string, FileDetails>();
				missingIn2 = new Dictionary<string, FileDetails>();
				return false;
			}

			var tmpDict1= new Dictionary<string, FileDetails>();
			foreach (var f in this.Files)
				tmpDict1.Add(f.RelativePath, f);

			var tmpDict2 = new Dictionary<string, FileDetails>();
			foreach (var f in otherFolderDet.Files)
				tmpDict2.Add(f.RelativePath, f);

			newIn2 = new Dictionary<string, FileDetails>();
			foreach (var fpath in tmpDict2.Keys)
				if (!tmpDict1.ContainsKey(fpath))
					if (!MustPathBeExcluded(fpath))
						newIn2.Add(fpath, tmpDict2[fpath]);

			missingIn2 = new Dictionary<string, FileDetails>();
			foreach (var fpath in tmpDict1.Keys)
				if (!tmpDict2.ContainsKey(fpath))
					if (!MustPathBeExcluded(fpath))
						missingIn2.Add(fpath, tmpDict1[fpath]);

			changedItems = new Dictionary<string, FileDetails>();
			foreach (var fpath in tmpDict1.Keys)
				if (tmpDict2.ContainsKey(fpath))
				{
					var file1 = tmpDict1[fpath];
					var file2 = tmpDict2[fpath];

					if (!file1.Equals(file2))
						if (!MustPathBeExcluded(fpath))
							changedItems.Add(fpath, tmpDict1[fpath]);
				}

			return
				newIn2.Count == 0
				&& missingIn2.Count == 0
				&& changedItems.Count == 0;
		}

		public override bool Equals(object obj)
		{
			FolderDetails otherFolderDet = obj as FolderDetails;
			if (otherFolderDet == null)
				return false;

			//Does not compare paths
			Dictionary<string, FileDetails> newIn2;
			Dictionary<string, FileDetails> missingIn2;
			Dictionary<string, FileDetails> changedItems;
			return CompareDetails(otherFolderDet, out newIn2, out missingIn2, out changedItems) == true;
		}
		public override int GetHashCode() { return 0; }

		/*public bool CompareToCachedAndUploadChanges(TextFeedbackEventHandler textFeedbackHandler = null, ProgressChangedEventHandler progressChangedHandler = null, Form formToCheckIfVisible = null)
		{
			bool hasChanges = false;

			FolderDetails cachedDetails;// = new FolderDetails();

			Dictionary<string, FileDetails> newFiles;
			Dictionary<string, FileDetails> deletedFiles;
			Dictionary<string, FileDetails> changedFiles;

			if (this.ExcludedRelativeFolders != null && this.ExcludedRelativeFolders.Count > 0)
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "There are excluded folders.", TextFeedbackType.Subtle);
			bool? compareResult = this.CompareToCached(out newFiles, out deletedFiles, out changedFiles, out cachedDetails);
			if (compareResult != true)
			{
				if (compareResult == null)
				{
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "Cannot compare current and cached files, different FtpUrls.", TextFeedbackType.Error);
					//UserMessages.ShowWarningMessage("Cannot compare current and cached files, different FtpUrls.");
					return true;//Saying comparison true, because cannot really compare if FTP URLs differ
				}
				if (newFiles == null)//&& deletedFiles == null && changedFiles == null)
				{
					this.SaveDetails();
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "Cannot compare current and cached files, cached file was missing and got regenerated.", TextFeedbackType.Error);
					//UserMessages.ShowWarningMessage("Cannot compare current and cached files, cached file was missing and got regenerated.");
					return true;//Cached files was missing and got regenerated, therefore no changes at this point
				}


				hasChanges = changedFiles.Count > 0 || newFiles.Count > 0 || deletedFiles.Count > 0;

				if (changedFiles.Count > 0 && (formToCheckIfVisible == null || formToCheckIfVisible.Visible) && UserMessages.Confirm(string.Format("There are {0} changed files, continue to upload them to the ftp site?{1}{2}", changedFiles.Count, Environment.NewLine,
					string.Join(Environment.NewLine, changedFiles.Keys.Count <= cMaxFilesToShowInUsermessages ? changedFiles.Keys : changedFiles.Keys.Take(cMaxFilesToShowInUsermessages))
					+ (changedFiles.Keys.Count <= cMaxFilesToShowInUsermessages ? "" : Environment.NewLine + "..." + Environment.NewLine + "and another " + (changedFiles.Keys.Count - cMaxFilesToShowInUsermessages) + " files")
					)))
				{
					foreach (var cf in changedFiles.Keys)
					{
						if (!NetworkInterop.FtpUploadFiles(
							null,
							changedFiles[cf].GetUrl(this.FtpRootUrl, true),
							this.FtpUsername,
							this.FtpPassword,
							new string[] { this.GetAbsolutePath(changedFiles[cf]) },
							null,
							textFeedbackHandler,
							progressChangedHandler))
						{
							TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "Uploading other changed files cancelled, unable to upload changed file: " + changedFiles[cf].GetUrl(this.FtpRootUrl, false), TextFeedbackType.Error);
							return false;
						}
					}
				}
				else if (changedFiles.Count > 0)
				{
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "There were changed files which were not uploaded now", TextFeedbackType.Subtle);
					return true;
				}

				if (newFiles.Count > 0 && (formToCheckIfVisible == null || formToCheckIfVisible.Visible) && UserMessages.Confirm(string.Format("There are {0} added files, continue to upload them to the ftp site?{1}{2}", newFiles.Count, Environment.NewLine,
					string.Join(Environment.NewLine, newFiles.Keys.Count <= cMaxFilesToShowInUsermessages ? newFiles.Keys : newFiles.Keys.Take(cMaxFilesToShowInUsermessages))
					+ (newFiles.Keys.Count <= cMaxFilesToShowInUsermessages ? "" : Environment.NewLine + "..." + Environment.NewLine + "and another " + (newFiles.Keys.Count - cMaxFilesToShowInUsermessages) + " files")
					)))
				{
					foreach (var nf in newFiles.Keys)
					{
						if (!NetworkInterop.FtpUploadFiles(
							null,
							newFiles[nf].GetUrl(this.FtpRootUrl, true),
							this.FtpUsername,
							this.FtpPassword,
							new string[] { this.GetAbsolutePath(newFiles[nf]) },
							null,
							textFeedbackHandler,
							progressChangedHandler))
						{
							TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "Uploading other new files cancelled, unable to upload new file: " + newFiles[nf].GetUrl(this.FtpRootUrl, false), TextFeedbackType.Error);
							return false;
						}
					}
				}
				else if (newFiles.Count > 0)
				{
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "There were new files which were not uploaded now", TextFeedbackType.Subtle);
					return true;
				}

				if (deletedFiles.Count > 0 && (formToCheckIfVisible == null || formToCheckIfVisible.Visible) && UserMessages.Confirm(string.Format("There are {0} deleted files, continue to delete them from the ftp site?{1}{2}", deletedFiles.Count, Environment.NewLine,
					string.Join(Environment.NewLine, deletedFiles.Keys.Count <= cMaxFilesToShowInUsermessages ? deletedFiles.Keys : deletedFiles.Keys.Take(cMaxFilesToShowInUsermessages))
					+ (deletedFiles.Keys.Count <= cMaxFilesToShowInUsermessages ? "" : Environment.NewLine + "..." + Environment.NewLine + "and another " + (deletedFiles.Keys.Count - cMaxFilesToShowInUsermessages) + " files")
					)))
				{
					foreach (var df in deletedFiles.Keys)
					{
						if (!NetworkInterop.DeleteFTPfile(
							null,
							deletedFiles[df].GetUrl(cachedDetails.FtpRootUrl, false),
							cachedDetails.FtpUsername,
							cachedDetails.FtpPassword,
							textFeedbackHandler,
							progressChangedHandler))
						{
							TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "Deleting other files cancelled, unable to delete file online: " + deletedFiles[df].GetUrl(cachedDetails.FtpRootUrl, false), TextFeedbackType.Error);
							return false;
						}
					}
				}
				else if (deletedFiles.Count > 0)
				{
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "There were deleted files which were not uploaded now", TextFeedbackType.Subtle);
					return true;
				}

				this.SaveDetails();

			}
			else
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "No changes for " + this.LocalRootDirectory + ".", TextFeedbackType.Subtle);

			return hasChanges;
		}*/

		public static bool AreStringListsEqual(List<string> list1, List<string> list2)
		{
			if (list1 == null && list2 == null)
				return true;
			if (list1 == null || list2 == null)
				return false;

			if (list1.Count != list2.Count)
				return false;

			foreach (string i1 in list1)
				if (!list2.Contains(i1, StringComparer.InvariantCultureIgnoreCase))
					return false;
			return true;
		}
	}

	public class FileDetails
	{
		public string RelativePath;
		public long FileSize;
		public DateTime LastWriteLocal;
		public FileDetails() { }
		public FileDetails(string basePathNoSlashAtEnd, string fullFilePath)
		{
			this.RelativePath = GetRelativePath(basePathNoSlashAtEnd, fullFilePath);//fullFilePath.Substring(basePathNoSlashAtEnd.Length + 1);
			var tmpFI = new FileInfo(fullFilePath);
			this.FileSize = tmpFI.Length;
			this.LastWriteLocal = tmpFI.LastWriteTime;
			this.LastWriteLocal.AddMilliseconds(-this.LastWriteLocal.Millisecond);
		}

		public static string GetRelativePath(string basePath, string fullFilePath)
		{
			return fullFilePath.Substring(basePath.TrimEnd('\\').Length + 1).TrimEnd('\\');
		}

		public string GetUrl(string baseUrl, bool returnOnlyDirAndExcludeFilename = false)
		{
			string tmpstr = baseUrl.TrimEnd('\\', '/') + '/' + this.RelativePath.Replace("\\", "/");
			if (returnOnlyDirAndExcludeFilename)
				return tmpstr.Substring(0, tmpstr.LastIndexOf('/'));
			else
				return tmpstr;
		}
		public override bool Equals(object obj)
		{
			FileDetails otherFileDet = obj as FileDetails;
			if (otherFileDet == null)
				return false;

			return
				this.RelativePath == otherFileDet.RelativePath &&
				this.FileSize == otherFileDet.FileSize &&
				this.LastWriteLocal.Year == otherFileDet.LastWriteLocal.Year &&
				this.LastWriteLocal.Month == otherFileDet.LastWriteLocal.Month &&
				this.LastWriteLocal.Day == otherFileDet.LastWriteLocal.Day &&
				this.LastWriteLocal.Hour == otherFileDet.LastWriteLocal.Hour &&
				this.LastWriteLocal.Minute == otherFileDet.LastWriteLocal.Minute &&
				this.LastWriteLocal.Second == otherFileDet.LastWriteLocal.Second;
		}
		public override int GetHashCode() { return 0; }
	}
}