using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SharedClasses;
using System.Diagnostics;
using System.Threading;

namespace SharedClasses
{
	internal class ListOfLockfiles
	{
		public List<string> files = new List<string>();
		public ListOfLockfiles()
		{
		}
		~ListOfLockfiles()
		{
			foreach (var file in files)
				if (File.Exists(file))
				{
					try { File.Delete(file); }
					catch { }
				}
		}
	}

	public static class MultiUserInterop
	{
		private static string defaultFile = @"C:\Francois\tmp\MultiUserApp\ABC.txt";
		private static ListOfLockfiles listofLockFiles = new ListOfLockfiles();

		public static bool GetLockOnFile()
		{
			try
			{
				string filePath = defaultFile;
				string lockfilePath = filePath + ".lock";

				string textToWrite = SettingsInterop.GetComputerGuidAsString();
				File.WriteAllText(lockfilePath, textToWrite);
				var textInFile = File.ReadAllText(lockfilePath).Trim();
				bool obtainedLock = textInFile.Equals(textToWrite, StringComparison.InvariantCultureIgnoreCase);
				if (obtainedLock)
					listofLockFiles.files.Add(lockfilePath);
				return obtainedLock;
			}
			catch (Exception exc)
			{
				UserMessages.ShowErrorMessage(exc.Message);
				return false;
			}
		}

		public class LockSingleProcessAction
		{
			private const string cFileExtensionLockFile = ".fjset";

			public string DirectoryForLockFiles;
			public string ActionNameFilePrefix;

			public LockSingleProcessAction(string directoryForLockFiles, string actionName)
			{
				this.ActionNameFilePrefix = actionName + "_";
				this.DirectoryForLockFiles = directoryForLockFiles;
			}

			public string GetLockFilePathOfTheOtherProcessLock(Predicate<string> shouldLockFileBeDeletedForApplicationNotRunning = null)
			{
				if (shouldLockFileBeDeletedForApplicationNotRunning == null) shouldLockFileBeDeletedForApplicationNotRunning = delegate { return true; };

				string lockFilePath = null;

				foreach (var fjsetFile in Directory.GetFiles(this.DirectoryForLockFiles, "*" + cFileExtensionLockFile, SearchOption.TopDirectoryOnly))
				{
					string filenameOnlyWithoutExtension = Path.GetFileNameWithoutExtension(fjsetFile);
					if (!filenameOnlyWithoutExtension.StartsWith(this.ActionNameFilePrefix, StringComparison.InvariantCultureIgnoreCase))
						continue;
					string procIdStr = filenameOnlyWithoutExtension.Substring(this.ActionNameFilePrefix.Length);
					int processID;
					if (!int.TryParse(procIdStr, out processID))
						continue;
					if (processID == Process.GetCurrentProcess().Id)
						continue;
					try
					{
						var proc = Process.GetProcessById(processID);
						if (proc != null)
							lockFilePath = fjsetFile;
						else
						{
							if (shouldLockFileBeDeletedForApplicationNotRunning(fjsetFile))
								File.Delete(fjsetFile);
						}
					}
					catch //If process not running it will throw exception
					{
						try
						{
							if (shouldLockFileBeDeletedForApplicationNotRunning(fjsetFile))
								File.Delete(fjsetFile);
						}
						catch//Just incase another process deleted this file just now
						{
						}
					}
				}

				return lockFilePath;
			}

			public bool DoesAnotherProcessHaveTheLockAndIsRunning(Predicate<string> shouldLockFileBeDeletedForApplicationNotRunning = null)
			{
				return GetLockFilePathOfTheOtherProcessLock(shouldLockFileBeDeletedForApplicationNotRunning) != null;
			}

			private string GetTempLockFilePath()
			{
				return Path.Combine(DirectoryForLockFiles, "templock.lock");
			}

			private bool AnotherAppHasTheTempLock()
			{
				string tempLockFilePath = GetTempLockFilePath();
				bool anotherAppJustLockedIt = false;
				if (File.Exists(tempLockFilePath))
				{
					//Just double-check the process which wrote this templock file did not crash/close before deleting it
					try
					{
						string lockContentsExpectsProcessID = File.ReadAllText(tempLockFilePath).Trim();
						int tmpProcId;
						if (!int.TryParse(lockContentsExpectsProcessID, out tmpProcId))
						{
							Thread.Sleep(500);//Maybe the other app was busy writing it
							if (!int.TryParse(lockContentsExpectsProcessID, out tmpProcId))
								File.Delete(tempLockFilePath);
							else
							{
								try
								{
									var proc = Process.GetProcessById(tmpProcId);
									if (proc != null)
										anotherAppJustLockedIt = true;
									else
										File.Delete(tempLockFilePath);
								}
								catch
								{
									File.Delete(tempLockFilePath);
								}
							}
						}
					}
					catch
					{
						try
						{
							File.Delete(tempLockFilePath);
						}
						catch
						{
						}
					}
				}
				return anotherAppJustLockedIt;
			}

			private string GetApplicationLockFilePath()
			{
				string appLockFile = Path.Combine(
					this.DirectoryForLockFiles,
					this.ActionNameFilePrefix + Process.GetCurrentProcess().Id.ToString() + cFileExtensionLockFile);
				return appLockFile;
			}

			public bool ObtainApplicationLock(string specificContentOfFile = null)
			{
				if (AnotherAppHasTheTempLock())
					return false;

				bool succeededToLock = false;
				try
				{
					string tempLockFilePath = GetTempLockFilePath();
					int currentProcID = Process.GetCurrentProcess().Id;
					using (var tempLockFile = File.Open(tempLockFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
					using (var sw = new StreamWriter(tempLockFile))
					{
						sw.Write(currentProcID);
					}

					if (File.Exists(tempLockFilePath)
						&& File.ReadAllText(tempLockFilePath).Trim().Equals(currentProcID.ToString()))
					{//Now have the temp lock, just so another app does not try to do what we do
						try
						{
							string appLockFile = GetApplicationLockFilePath();
							if (!string.IsNullOrEmpty(specificContentOfFile))
								File.WriteAllText(appLockFile, specificContentOfFile);
							else
								File.Create(appLockFile).Close();

							succeededToLock = true;
						}
						finally
						{
							File.Delete(tempLockFilePath);//We are done delete the temp lock
						}
					}
				}
				catch
				{
				}

				return succeededToLock;
			}

			public void RemoveApplicationLock()
			{
				string appLockFile = GetApplicationLockFilePath();
				try
				{
					if (File.Exists(appLockFile))
						File.Delete(appLockFile);
				}
				catch { }
			}

			public void DoActionIfLockCouldBeObtainedAndThenUnlock(Action action, string specificContentOfLockFile = null)
			{
				if (!this.ObtainApplicationLock(specificContentOfLockFile))
					return;

				if (action != null)
					action();

				RemoveApplicationLock();
			}
		}
	}
}
