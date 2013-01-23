
//Multiple face detection and recognition in real time
//Using EmguCV cross platform .Net wrapper to the Intel OpenCV image processing library for C#.Net
//Writed by Sergio Andrés Guitérrez Rojas
//"Serg3ant" for the delveloper comunity
// Sergiogut1805@hotmail.com
//Regards from Bucaramanga-Colombia ;)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.IO;
using SharedClasses;

namespace SharedClasses
{
	public partial class FaceTrainingForm : Form
	{
		//Declararation of all variables, vectors and haarcascades
		Image<Bgr, Byte> currentFrame;
		Capture grabber;

		//HaarCascade haarCascades = 
		HaarCascade face;
		HaarCascade eye;
		HaarCascade nose;
		HaarCascade righteye;//Note that it uses left eye xml file because the image is forcebly flipped horizontally

		//Think about having separate installation for Face Recognition (EmguCV) Dlls: cv110.dll, cvaux110.dll, cvextern.dll, cxcore110.dll, opencv_calib3d220.dll, opencv_contrib220.dll, opencv_core220.dll, opencv_features2d220.dll, opencv_ffmpeg220.dll, opencv_flann220.dll, opencv_gpu220.dll, opencv_highgui220.dll, opencv_imgproc220.dll, opencv_legacy220.dll, opencv_ml220.dll, opencv_objdetect220.dll, opencv_video220.dll (maybe also Emgu.CV.dll, Emgu.CV.UI.dll, Emgu.Util.dll which are included in the VS Project references).
		MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);//If it fails here, DLLs are missing, all the "cv..." and "opencv_..." Dlls mentioned above
		Image<Gray, byte> result, TrainedFace = null;
		Image<Gray, byte> gray = null;
		List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
		Image<Gray, byte> trainingImage;
		List<string> labels = new List<string>();
		List<string> NamePersons = new List<string>();
		//int ContTrain, NumLabels, buildTask;
		//int MaximumIterations;
		string name, names = null;

		public FaceTrainingForm()
		{
			InitializeComponent();
			//Load haarcascades for face detection
			VisualStudioInterop.GetEmbeddedResource_FirstOneEndingWith("haarcascade_frontalface_default.xml", SettingsInterop.LocalAppdataPath("SharedClasses") + "\\haarcascade_frontalface_default.xml");
			face = new HaarCascade(SettingsInterop.LocalAppdataPath("SharedClasses") + @"\haarcascade_frontalface_default.xml");
			
			VisualStudioInterop.GetEmbeddedResource_FirstOneEndingWith("haarcascade_eye.xml", SettingsInterop.LocalAppdataPath("SharedClasses") + "\\haarcascade_eye.xml");
			eye = new HaarCascade(SettingsInterop.LocalAppdataPath("SharedClasses") + @"\haarcascade_eye.xml");

			VisualStudioInterop.GetEmbeddedResource_FirstOneEndingWith("haarcascade_mcs_nose.xml", SettingsInterop.LocalAppdataPath("SharedClasses") + "\\haarcascade_mcs_nose.xml");
			nose = new HaarCascade(SettingsInterop.LocalAppdataPath("SharedClasses") + @"\haarcascade_mcs_nose.xml");

			//Note that it uses left eye xml file because the image is forcebly flipped horizontally
			VisualStudioInterop.GetEmbeddedResource_FirstOneEndingWith("haarcascade_lefteye_2splits.xml", SettingsInterop.LocalAppdataPath("SharedClasses") + "\\haarcascade_lefteye_2splits.xml");
			righteye = new HaarCascade(SettingsInterop.LocalAppdataPath("SharedClasses") + @"\haarcascade_lefteye_2splits.xml");

			try
			{
				//MaximumIterations = 0;
				//foreach (string file in Directory.GetFiles(FaceDetectionInterop.GetFaceDetectionFolderPath(), "*.bmp", SearchOption.AllDirectories))
				//{
				//	MaximumIterations++;
				//	trainingImages.Add(new Image<Gray, byte>(file));
				//	labels.Add(Path.GetDirectoryName(file).Split('\\')[Path.GetDirectoryName(file).Split('\\').Length - 1]);
				//}

				//string PersonName = "Francois Hill";
				trainingImages.Clear();
				labels.Clear();

				Dictionary<string, List<Image<Gray, byte>>> facesList = FaceDetectionInterop.GetListOfTrainedFaces(FaceDetectionInterop.Passphrase, FaceDetectionInterop.Salt);
				foreach (string personname in facesList.Keys)
					foreach (Image<Gray, byte> faceimage in facesList[personname])
					{
						trainingImages.Add(faceimage);
						labels.Add(personname);
					}

				////Load of previus trainned faces and labels for each image
				//string Labelsinfo = File.ReadAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt");
				//string[] Labels = Labelsinfo.Split('%');
				//NumLabels = Convert.ToInt16(Labels[0]);
				//ContTrain = NumLabels;
				//string LoadFaces;

				//for (int tf = 1; tf < NumLabels + 1; tf++)
				//{
				//	LoadFaces = "face" + tf + ".bmp";
				//	trainingImages.Add(new Image<Gray, byte>(Application.StartupPath + "/TrainedFaces/" + LoadFaces));
				//	labels.Add(Labels[tf]);
				//}

			}
			catch// (Exception e)
			{
				//MessageBox.Show(e.ToString());
				MessageBox.Show("Nothing in binary database, please add at least a face", "Triained faces load", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}

			//GenericSettings.EnsureAllSettingsAreInitialized();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			//Initialize the capture device
			grabber = new Capture();
			grabber.QueryFrame();
			//Initialize the FrameGraber event
			Application.Idle += new EventHandler(FrameGrabber);
			button1.Enabled = false;
		}

		private void button2_Click(object sender, System.EventArgs e)
		{
			try
			{
				//Trained face counter
				//ContTrain = ContTrain + 1;

				//Get a gray frame from capture device
				gray = grabber.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

				//Face Detector
				MCvAvgComp[] facesDetected = face.Detect(
				gray,
				1.2,
				10,
				Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
				new Size(20, 20));

				//Action for each element detected
				foreach (MCvAvgComp f in facesDetected)
				{
					TrainedFace = currentFrame.Copy(f.rect).Convert<Gray, byte>();
					break;
				}

				//resize face detected image for force to compare the same size with the 
				//test image with cubic interpolation type method
				TrainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
				trainingImages.Add(TrainedFace);
				trainingImage = TrainedFace;
				labels.Add(textBox1.Text);

				//Show face added in gray scale
				imageBox1.Image = TrainedFace;

				//Write the number of triained faces in a file text for further load
				//File.WriteAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt", trainingImages.ToArray().Length.ToString() + "%");

				FaceDetectionInterop.AddFace(textBox1.Text, trainingImage, FaceDetectionInterop.Passphrase, FaceDetectionInterop.Salt);

				////Write the labels of triained faces in a file text for further load
				//for (int i = 1; i < trainingImages.ToArray().Length + 1; i++)
				//{
				//	string dir = FaceDetectionInterop.GetFaceDetectionFolderPath() + "\\" + labels[i - 1];
				//	if (!Directory.Exists(dir))
				//		Directory.CreateDirectory(dir);
				//	string filepath = dir + "\\face" + i + ".bmp";

				//	//CvArray<byte> bytes;
				//	//MemoryStream ms = new MemoryStream();
				//	//trainingImages.ToArray()[i - 1].ToBitmap().Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
				//	//byte[] bs = ms.ToArray();
				//	//foreach (byte b in bs) { }

				//	//trainingImages.ToArray()[i - 1].Save(
				//	//	//Application.StartupPath + "/TrainedFaces/face" + i + ".bmp"
				//	//	filepath
				//	//	);
				//	//File.AppendAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt", labels.ToArray()[i - 1] + "%");
				//}
				CustomBalloonTipwpf.ShowCustomBalloonTip(
					"Face added",
					"Detected and added face for " + textBox1.Text,
					1000,
					CustomBalloonTipwpf.IconTypes.Information);
				//MessageBox.Show(textBox1.Text + "´s face detected and added :)", "Training OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception exc)
			{
				UserMessages.ShowErrorMessage("Error while saving file: " + exc.Message);
				//MessageBox.Show("Enable the face detection first", "Training Fail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		void FrameGrabber(object sender, EventArgs e)
		{
			label3.Text = "0";
			//label4.Text = "";
			NamePersons.Add("");

			//Get the current frame form capture device
			currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC).Flip(FLIP.HORIZONTAL);

			//Convert it to Grayscale
			gray = currentFrame.Convert<Gray, Byte>();

			//Face Detector
			MCvAvgComp[] facesDetected = face.Detect(
				gray,
				1.2,
				10,
				Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
				new Size(20, 20));

			int t = 0;
			//Action for each element detected
			foreach (MCvAvgComp f in facesDetected)
			{
				t = t + 1;
				result = currentFrame.Copy(f.rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
				//draw the face detected in the 0th (gray) channel with blue color
				currentFrame.Draw(f.rect, new Bgr(Color.Red), 2);


				if (trainingImages.ToArray().Length != 0)
				{
					//TermCriteria for face recognition with numbers of trained images like maxIteration
					MCvTermCriteria termCrit = new MCvTermCriteria(trainingImages.Count, 0.001);

					//Eigen face recognizer
					EigenObjectRecognizer recognizer = new EigenObjectRecognizer(
						 trainingImages.ToArray(),
						 labels.ToArray(),
						 GlobalSettings.FaceDetectionInteropSettings.Instance.RecognitionTolerance ?? 0,
						 ref termCrit);

					name = recognizer.Recognize(result);

					//Draw the label for each face detected and recognized
					currentFrame.Draw(name, ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.LightGreen));

				}

				NamePersons[t - 1] = name;
				NamePersons.Add("");


				//Set the number of faces detected on the scene
				label3.Text = facesDetected.Length.ToString();


				//Set the region of interest on the faces                        
				gray.ROI = f.rect;
				MCvAvgComp[] eyesDetected = eye.Detect(
					 gray,
					 1.1,
					 10,
					 Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
					 new Size(20, 20));
				gray.ROI = Rectangle.Empty;

				foreach (MCvAvgComp ey in eyesDetected)
				{
					Rectangle eyeRect = ey.rect;
					eyeRect.Offset(f.rect.X, f.rect.Y);
					currentFrame.Draw(eyeRect, new Bgr(Color.Blue), 2);
				}

				gray.ROI = f.rect;
				MCvAvgComp[] nosesDetected = nose.Detect(
					 gray,
					 1.1,
					 10,
					 Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
					 new Size(20, 20));
				gray.ROI = Rectangle.Empty;

				foreach (MCvAvgComp nos in nosesDetected)
				{
					Rectangle eyeRect = nos.rect;
					eyeRect.Offset(f.rect.X, f.rect.Y);
					currentFrame.Draw(eyeRect, new Bgr(Color.Green), 2);
				}

				gray.ROI = f.rect;
				MCvAvgComp[] righteyesDetected = righteye.Detect(
					 gray,
					 1.1,
					 10,
					 Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
					 new Size(20, 20));
				gray.ROI = Rectangle.Empty;

				foreach (MCvAvgComp re in righteyesDetected)
				{
					Rectangle eyeRect = re.rect;
					eyeRect.Offset(f.rect.X, f.rect.Y);
					currentFrame.Draw(eyeRect, new Bgr(Color.Orange), 2);
				}
			}
			t = 0;

			//Names concatenation of persons recognized
			for (int nnn = 0; nnn < facesDetected.Length; nnn++)
			{
				names = names + NamePersons[nnn] + ", ";
			}
			//Show the faces procesed and recognized
			imageBoxFrameGrabber.Image = currentFrame;
			label4.Text = names;
			names = "";
			//Clear the list(vector) of names
			NamePersons.Clear();

		}

		//private void button3_Click(object sender, EventArgs e)
		//{
		//	if (ConfirmUsingFaceDetection.ConfirmUsingFacedetection(
		//		@"C:\Users\francois\Downloads\FaceRecProOV\FaceRecProOV\bin\Debug\OwnFormatTrainedFaces",
		//		"Francois Hill",
		//		0,
		//		this))
		//		MessageBox.Show("Success");
		//	else
		//		MessageBox.Show("Failure!");
		//}

		public static void ShowFacetraining()
		{
			if (FaceDetectionInterop.CheckFaceDetectionDllsExistInCurrentExeDir(true))
			{
				FaceTrainingForm frm = new FaceTrainingForm();
				frm.ShowDialog();
			}
		}

		private void FaceTrainingForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (grabber != null)
			{
				grabber.Dispose();
				grabber = null;
			}
			Application.Idle -= new EventHandler(FrameGrabber);
		}

		private void FaceTrainingForm_Load(object sender, EventArgs e)
		{
			button1.PerformClick();
		}

		//private void button3_Click(object sender, EventArgs e)
		//{
		//	UserMessages.ShowInfoMessage("FtpPassword = " + GlobalSettings.VisualStudioInteropSettings.Instance.FtpPassword);
		//}
	}
}
