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
		int Counter = 11;

		Capture grabber;
		List<string> NamePersons = new List<string>();
		Image<Bgr, Byte> currentFrame;
		Image<Gray, byte> gray = null;
		HaarCascade face;
		int ContTrain, t;
		Image<Gray, byte> result = null;
		List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
		List<string> labels= new List<string>();
		string name, names = null;
		MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);

		public ConfirmUsingFaceDetection()
		{
			InitializeComponent();
			labelSecondsRemaining.Text = "";

			face = new HaarCascade("haarcascade_frontalface_default.xml");

			try
			{
				//Load of previus trainned faces and labels for each image
				string Labelsinfo = File.ReadAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt");
				string[] Labels = Labelsinfo.Split('%');
				int NumLabels = Convert.ToInt16(Labels[0]);
				ContTrain = NumLabels;
				string LoadFaces;

				for (int tf = 1; tf < NumLabels + 1; tf++)
				{
					LoadFaces = "face" + tf + ".bmp";
					trainingImages.Add(new Image<Gray, byte>(Application.StartupPath + "/TrainedFaces/" + LoadFaces));
					labels.Add(Labels[tf]);
				}

			}
			catch (Exception e)
			{
				//MessageBox.Show(e.ToString());
				MessageBox.Show("Nothing in binary database, please add at least a face", "Triained faces load", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void ConfirmUsingFaceDetection_Load(object sender, EventArgs e)
		{
			//Initialize the capture device
			grabber = new Capture();
			grabber.QueryFrame();
			//Initialize the FrameGraber event
			Application.Idle += new EventHandler(FrameGrabber);
		}

		void FrameGrabber(object sender, EventArgs e)
		{
			//label3.Text = "0";
			//label4.Text = "";
			NamePersons.Add("");


			//Get the current frame form capture device
			currentFrame = grabber.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

			//Convert it to Grayscale
			gray = currentFrame.Convert<Gray, Byte>();

			//Face Detector
			MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
				face,
				1.2,
				10,
				Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
				new Size(20, 20));

			//Action for each element detected
			foreach (MCvAvgComp f in facesDetected[0])
			{
				t = t + 1;
				result = currentFrame.Copy(f.rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
				//draw the face detected in the 0th (gray) channel with blue color
				currentFrame.Draw(f.rect, new Bgr(Color.Red), 2);


				if (trainingImages.ToArray().Length != 0)
				{
					//TermCriteria for face recognition with numbers of trained images like maxIteration
					MCvTermCriteria termCrit = new MCvTermCriteria(ContTrain, 0.001);

					//Eigen face recognizer
					EigenObjectRecognizer recognizer = new EigenObjectRecognizer(
						 trainingImages.ToArray(),
						 labels.ToArray(),
						 3000,
						 ref termCrit);

					if (name != null && name.ToLower() == "Francois".ToLower())
						this.DialogResult = System.Windows.Forms.DialogResult.OK;

					name = recognizer.Recognize(result);

					//Draw the label for each face detected and recognized
					currentFrame.Draw(name, ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.LightGreen));

				}

				NamePersons[t - 1] = name;
				NamePersons.Add("");


				//Set the number of faces detected on the scene
				//label3.Text = facesDetected[0].Length.ToString();
			}
			t = 0;

			//Names concatenation of persons recognized
			for (int nnn = 0; nnn < facesDetected[0].Length; nnn++)
			{
				names = names + NamePersons[nnn] + ", ";
			}
			//Show the faces procesed and recognized
			imageBoxFrameGrabber.Image = currentFrame;
			//label4.Text = names;
			names = "";
			//Clear the list(vector) of names
			NamePersons.Clear();
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			//this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			if (Counter > 0)
			{
				Counter--;
				labelSecondsRemaining.Text = "Seconds remaining = " + Counter;
			}
			else
				this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		}
	}
}
