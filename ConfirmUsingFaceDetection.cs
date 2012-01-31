using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.IO;


namespace SharedClasses
{
	public partial class ConfirmUsingFaceDetection : Form
	{
		int Counter = 0;
		Capture grabber;
		//List<string> NamePersons = new List<string>();
		//Image<Bgr, Byte> currentFrame;
		//Image<Gray, byte> gray = null;
		HaarCascade face;
		HaarCascade eye;
		//int ContTrain, t;
		int MaximumIterations;
		//Image<Gray, byte> result = null;
		List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
		List<string> labels = new List<string>();
		//string name, names = null;
		MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);//If it fails here, DLLs are missing, all the "cv..." and "opencv_..." Dlls

		string RequiredFaceName;
		bool IsTimerRequired = false;

		/// <summary>
		/// The constructor.
		/// </summary>
		/// <param name="DirectoryWithTrainedFaces">The directory must have a folder for each "person" and all .bmp files inside will be used for recognizing.</param>
		/// <param name="RequiredFaceName">The name of the "person" which will result in a DialogResult of OK.</param>
		/// <param name="TimeOutSeconds_nullIfNever">The duration (in milliseconds) after which it must result in a DialogResult of Cancel.</param>
		public ConfirmUsingFaceDetection(string RequiredFaceName, string UserMessage, int? TimeOutSeconds_nullIfNever)
		{
			InitializeComponent();

			labelSecondsRemaining.Text = "";

			VisualStudioInterop.GetEmbeddedResource("haarcascade_frontalface_default.xml", SettingsInterop.LocalAppdataPath("SharedClasses") + "\\haarcascade_frontalface_default.xml");
			VisualStudioInterop.GetEmbeddedResource("haarcascade_eye.xml", SettingsInterop.LocalAppdataPath("SharedClasses") + "\\haarcascade_eye.xml");
			face = new HaarCascade(SettingsInterop.LocalAppdataPath("SharedClasses") + @"\haarcascade_frontalface_default.xml");
			eye = new HaarCascade(SettingsInterop.LocalAppdataPath("SharedClasses") + @"\haarcascade_eye.xml");

			try
			{
				//Load of previus trainned faces and labels for each image
				//string Labelsinfo = File.ReadAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt");
				//string[] Labels = Labelsinfo.Split('%');
				//int NumLabels = Convert.ToInt16(Labels[0]);
				//string LoadFaces;
				//string DirectoryWithTrainedFaces = FaceDetectionInterop.GetFaceDetectionFolderPath();
				MaximumIterations = 0;
				//if (!Directory.Exists(DirectoryWithTrainedFaces))
				//	UserMessages.ShowWarningMessage("Cannot find trained faces, directory does not exist: " + DirectoryWithTrainedFaces);
				//else
				//{
				foreach (string file in Directory.GetFiles(FaceDetectionInterop.GetFaceDetectionFolderPath(), "*.bmp", SearchOption.AllDirectories))
				{
					MaximumIterations++;
					trainingImages.Add(new Image<Gray, byte>(file));
					labels.Add(Path.GetDirectoryName(file).Split('\\')[Path.GetDirectoryName(file).Split('\\').Length - 1]);
				}
				//}

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

			this.RequiredFaceName = RequiredFaceName;
			if (TimeOutSeconds_nullIfNever != null && (int)TimeOutSeconds_nullIfNever > 0)
			{
				Counter = (int)TimeOutSeconds_nullIfNever;
				//timer1.Enabled = true;
				IsTimerRequired = true;
			}
			labelUserPrompt.Text = UserMessage;
		}

		private void ConfirmUsingFaceDetection_Load(object sender, EventArgs e)
		{
			//Initialize the capture device
			grabber = new Capture();
			grabber.QueryFrame();
			//Initialize the FrameGraber event
			Application.Idle += new EventHandler(FrameGrabber);

			if (IsTimerRequired)
				timer1.Enabled = true;
		}

		void FrameGrabber(object sender, EventArgs e)
		{
			//label3.Text = "0";
			//label4.Text = "";
			//NamePersons.Add("");


			//Get the current frame form capture device
			Image<Bgr, Byte> currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC).Flip(FLIP.HORIZONTAL);

			//Convert it to Grayscale
			Image<Gray, Byte> gray = currentFrame.Convert<Gray, Byte>();

			//Face Detector
			MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
				face,
				1.2,
				10,
				Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
				new Size(20, 20));

			int t = 0;
			//Action for each element detected
			foreach (MCvAvgComp f in facesDetected[0])
			{
				t = t + 1;
				Image<Gray, byte> result = currentFrame.Copy(f.rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
				//draw the face detected in the 0th (gray) channel with blue color
				currentFrame.Draw(f.rect, new Bgr(Color.Red), 2);


				if (trainingImages.ToArray().Length != 0)
				{
					//TermCriteria for face recognition with numbers of trained images like maxIteration
					MCvTermCriteria termCrit = new MCvTermCriteria(MaximumIterations, 0.001);

					//Eigen face recognizer
					EigenObjectRecognizer recognizer = new EigenObjectRecognizer(
						 trainingImages.ToArray(),
						 labels.ToArray(),
						 GlobalSettings.FaceDetectionInteropSettings.Instance.RecognitionTolerance ?? 0,
						 ref termCrit);

					string name = recognizer.Recognize(result);

					if (name != null && name.Trim().ToLower() == RequiredFaceName.Trim().ToLower())
						this.DialogResult = System.Windows.Forms.DialogResult.OK;
					//Draw the label for each face detected and recognized
					currentFrame.Draw(name, ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.LightGreen));
				}

				//NamePersons[t - 1] = name;
				//NamePersons.Add("");


				//Set the number of faces detected on the scene
				//label3.Text = facesDetected[0].Length.ToString();

				gray.ROI = f.rect;
				MCvAvgComp[][] eyesDetected = gray.DetectHaarCascade(
					 eye,
					 1.1,
					 10,
					 Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
					 new Size(20, 20));
				gray.ROI = Rectangle.Empty;

				foreach (MCvAvgComp ey in eyesDetected[0])
				{
					Rectangle eyeRect = ey.rect;
					eyeRect.Offset(f.rect.X, f.rect.Y);
					currentFrame.Draw(eyeRect, new Bgr(Color.Blue), 2);
				}
			}
			t = 0;

			//Names concatenation of persons recognized
			//for (int nnn = 0; nnn < facesDetected[0].Length; nnn++)
			//{
			//	names = names + NamePersons[nnn] + ", ";
			//}
			//Show the faces procesed and recognized
			imageBoxFrameGrabber.Image = currentFrame;
			//label4.Text = names;
			//names = "";
			//Clear the list(vector) of names
			//NamePersons.Clear();
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			if (Counter > 0)
			{
				Counter--;
				labelSecondsRemaining.Text = "Seconds remaining = " + Counter;
			}
			else
				this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		}

		public static bool ConfirmUsingFacedetection(string RequiredFaceName, string UserMessage, int? TimeOutSeconds_nullIfNever, IWin32Window owner = null)
		{
			ConfirmUsingFaceDetection frm = new ConfirmUsingFaceDetection(RequiredFaceName, UserMessage, TimeOutSeconds_nullIfNever);
			return frm.ShowDialog(owner) == System.Windows.Forms.DialogResult.OK;
		}

		private void ConfirmUsingFaceDetection_FormClosing(object sender, FormClosingEventArgs e)
		{
			grabber.Dispose();
			grabber = null;
			Application.Idle -= new EventHandler(FrameGrabber);
		}
	}
}
