using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Emgu.CV;
using Emgu.CV.Structure;

namespace SharedClasses
{
	public class FaceDetectionInterop
	{
		public static readonly string FaceTrainingFolderName = "FaceTraining";

		public const string Passphrase = "mysecretpassphrase";
		public const string Salt = "alluminiumcopper";

		private static Dictionary<string, bool> ListOfRequiredDllsInExeDir = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase)
		{
			{ "cvextern.dll", false },
			{ "cvextern_gpu.dll", false },
			{ "opencv_calib3d231.dll", false },
			{ "opencv_contrib231.dll", false },
			{ "opencv_core231.dll", false },
			{ "opencv_features2d231.dll", false },
			{ "opencv_ffmpeg.dll", false },
			{ "opencv_flann231.dll", false },
			{ "opencv_gpu231.dll", false },
			{ "opencv_highgui231.dll", false },
			{ "opencv_imgproc231.dll", false },
			{ "opencv_legacy231.dll", false },
			{ "opencv_ml231.dll", false },
			{ "opencv_objdetect231.dll", false },
			{ "opencv_video231.dll", false }
		};

		public static bool CheckFaceDetectionDllsExistInCurrentExeDir(out List<string> MissingFiles)
		{
			MissingFiles = new List<string>();
			foreach (string fullfilepath in Directory.GetFiles(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0])))
				if (ListOfRequiredDllsInExeDir.ContainsKey(Path.GetFileName(fullfilepath).ToLower()))
					ListOfRequiredDllsInExeDir[Path.GetFileName(fullfilepath).ToLower()] = true;
			//ListOfFilenamesInExeDir.Add(Path.GetFileName(fullfilepath));
			if (ListOfRequiredDllsInExeDir.ContainsValue(false))
				foreach (string file in ListOfRequiredDllsInExeDir.Keys)
					if (!ListOfRequiredDllsInExeDir[file])
						MissingFiles.Add(file);
			//if (UserMessages.Confirm("Could not find required DLL for FaceDetection, the program will Crash when attempting anything involving face detection: " + file))
			//	Application.Current.Shutdown(0);
			return MissingFiles.Count == 0;
		}

		public static bool CheckFaceDetectionDllsExistInCurrentExeDir(bool ShowMissingList = false)
		{
			List<string> tmpMissingFiles = new List<string>();
			bool AllDllsExist = CheckFaceDetectionDllsExistInCurrentExeDir(out tmpMissingFiles);
			if (ShowMissingList)
				foreach (string missingfile in tmpMissingFiles)
					UserMessages.ShowErrorMessage("Missing file required for face detection: " + missingfile);
			return AllDllsExist;
		}

		public static string GetFaceDetectionFolderPath()
		{
			string FaceTrainingFolderPath = SettingsInterop.LocalAppdataPath(GenericSettings.RootApplicationNameForSharedClasses) + "\\" + FaceTrainingFolderName;
			if (!Directory.Exists(FaceTrainingFolderPath))
				Directory.CreateDirectory(FaceTrainingFolderPath);
			return FaceTrainingFolderPath + "\\";
		}

		public static void InitializeFaceDetection()
		{
			//if (Directory.GetDirectories(GetFaceDetectionFolderPath()).Length == 0)
			if (Directory.GetFiles(SqliteDatabaseDir, "*" + FileExtensionForFaces).Length == 0)
			{
				UserMessages.ShowWarningMessage("No face training records found, training will now start");
				FaceTrainingForm.ShowFacetraining();
			}
		}

		public static string ReverseString(string s)
		{
			char[] arr = s.ToCharArray();
			Array.Reverse(arr);
			return new string(arr);
		}

		private static readonly string FileExtensionForFaces =  ".faces";
		private static string SqliteDatabaseDir { get { return GetFaceDetectionFolderPath(); } }//= @"C:\Francois\Other\tmp\";
		public static void AddFace(string PersonName, Image<Gray, byte> faceImage, string Passphrase, string Salt)
		{
			if (faceImage == null)
			{
				UserMessages.ShowInfoMessage("Face image object may not be NULL");
				return;
			}

			string FacesFilePath = SqliteDatabaseDir + PersonName + FileExtensionForFaces;
			bool FileDidExists = File.Exists(FacesFilePath);

			using (FileStream fs = new FileStream(FacesFilePath, FileDidExists ? FileMode.Append : FileMode.Create))
			{
				using (BinaryWriter bw = new BinaryWriter(fs))
				{
					if (!FileDidExists)
					{
						byte[] encryptedPersonNameBytes = EncryptionInterop.EncryptBytes(Encoding.ASCII.GetBytes(ReverseString(PersonName)), ReverseString(Passphrase), ReverseString(Salt));
						bw.Write(encryptedPersonNameBytes.Length);
						bw.Write(encryptedPersonNameBytes);
						encryptedPersonNameBytes = null;
					}

					byte[] encryptedImageBytes = new byte[0];
					using (MemoryStream ms = new MemoryStream())
					{
						faceImage.ToBitmap().Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
						encryptedImageBytes = EncryptionInterop.EncryptBytes(ms.ToArray(), Passphrase, Salt);
					}

					bw.Write(encryptedImageBytes.Length);
					bw.Write(encryptedImageBytes);//Encrypted above
					encryptedImageBytes = null;
				}
			}
		}

		public static Dictionary<string, List<Image<Gray, byte>>> GetListOfTrainedFaces(string Passphrase, string Salt)
		{
			Dictionary<string, List<Image<Gray, byte>>> tmpList = new Dictionary<string, List<Image<Gray, byte>>>(StringComparer.InvariantCultureIgnoreCase);

			//string FacesFilePath = Directory + FileExtensionForFaces;
			//if (!File.Exists(FacesFilePath))
			//{
			//	UserMessages.ShowWarningMessage("There are no trained faces for " + PersonName);
			//	return tmpList;
			//}

			if (!Directory.Exists(SqliteDatabaseDir))
			{
				UserMessages.ShowWarningMessage("Directory for face training is empty (or does not exist): " + SqliteDatabaseDir);
				return tmpList;
			}

			foreach (string FacesFilePath in Directory.GetFiles(SqliteDatabaseDir, "*" + FileExtensionForFaces))
			{
				ExtractFacesFromFile(Passphrase, Salt, ref tmpList, FacesFilePath);
			}

			return tmpList;
		}

		public static void ExtractFacesFromFile(string Passphrase, string Salt, ref Dictionary<string, List<Image<Gray, byte>>> tmpList, string FacesFilePath)
		{
			if (tmpList == null)
				tmpList = new Dictionary<string, List<Image<Gray, byte>>>();

			string FaceFilenameOnly = Path.GetFileName(FacesFilePath);
			string PersonName = FaceFilenameOnly.Substring(0, FaceFilenameOnly.Length - FileExtensionForFaces.Length);

			using (FileStream fs = new FileStream(FacesFilePath, FileMode.Open))
			{
				using (BinaryReader br = new BinaryReader(fs))
				{
					Int32 personnamebytesLength = 0;
					try
					{
						personnamebytesLength = br.ReadInt32();
						string personnameString = ReverseString(Encoding.ASCII.GetString(
							EncryptionInterop.DecryptBytes(br.ReadBytes(personnamebytesLength), ReverseString(Passphrase), ReverseString(Salt))));
						if (personnameString.ToLower() != PersonName.ToLower())
						{
							UserMessages.ShowErrorMessage("Cannot read from file as the filename and face name does not correspond");
							return;
						}
					}
					catch (EndOfStreamException)
					{
						UserMessages.ShowErrorMessage("Could not read person name from file: end of file reached, unable to get faces trained from file.");
						return;
					}
					catch (Exception exc)
					{
						UserMessages.ShowErrorMessage("Could not read person name and obtain faces trained from file: " + exc.Message);
						return;
					}

					Int32 imagebyteLength = 0;
					bool EOF = false;
					while (!EOF)
					{
						try
						{
							imagebyteLength = br.ReadInt32();
							byte[] imageBytes = EncryptionInterop.DecryptBytes(br.ReadBytes(imagebyteLength), Passphrase, Salt);
							using (MemoryStream ms = new MemoryStream(imageBytes))
							{
								if (!tmpList.ContainsKey(PersonName))
									tmpList.Add(PersonName, new List<Image<Gray, byte>>());
								if (tmpList[PersonName] == null)
									tmpList[PersonName] = new List<Image<Gray, byte>>();
								tmpList[PersonName].Add(new Image<Gray, byte>(new System.Drawing.Bitmap(ms)));
							}
							imageBytes = null;
						}
						catch (EndOfStreamException)
						{
							EOF = true;
						}
						catch (Exception exc)
						{
							UserMessages.ShowErrorMessage("Error reading bytes: " + exc.Message);
						}
					}
				}
			}
		}
	}
}