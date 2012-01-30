using System;
using System.IO;

namespace SharedClasses
{
	public class FaceDetectionInterop
	{
		public static readonly string FaceTrainingFolderName = "FaceTraining";

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
	}
}