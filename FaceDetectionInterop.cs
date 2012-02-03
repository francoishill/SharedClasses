using System;
using System.Collections.Generic;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;

namespace SharedClasses
{
	public class FaceDetectionInterop
	{
		public static readonly string FaceTrainingFolderName = "FaceTraining";

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
			return FaceTrainingFolderPath;
		}

		public static void InitializeFaceDetection()
		{
			if (Directory.GetDirectories(GetFaceDetectionFolderPath()).Length == 0)
			{
				UserMessages.ShowWarningMessage("No face training records found, training will now start");
				FaceTrainingForm.ShowFacetraining();
			}
		}

		private static readonly string SqliteDatabasePath = @"C:\Francois\Other\tmp\tmpfile";
		public static void AddFace(Image<Gray, byte> faceImage, string Passphrase, string Salt)
		{
			if (faceImage == null)
			{
				UserMessages.ShowInfoMessage("Face image object may not be NULL");
				return;
			}

			if (!File.Exists(SqliteDatabasePath))
			{
				using (FileStream fs = new FileStream(SqliteDatabasePath, FileMode.Create))
				{
					using (BinaryWriter bw = new BinaryWriter(fs))
					{
						byte[] imageBytes = new byte[0];
						using (MemoryStream ms = new MemoryStream())
						{
							faceImage.ToBitmap().Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
							imageBytes = EncryptionInterop.EncryptBytes(ms.ToArray(), Passphrase, Salt);
						}

						bw.Write(imageBytes.Length);
						bw.Write(imageBytes);
						imageBytes = null;
					}
				}
			}
			else
				UserMessages.ShowInfoMessage("Function not incorporated yet to append to if the file exists = " + SqliteDatabasePath);
		}
	}
}