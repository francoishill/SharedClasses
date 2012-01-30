namespace SharedClasses
{
	partial class ConfirmUsingFaceDetection
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.imageBoxFrameGrabber = new Emgu.CV.UI.ImageBox();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.labelSecondsRemaining = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.imageBoxFrameGrabber)).BeginInit();
			this.SuspendLayout();
			// 
			// imageBoxFrameGrabber
			// 
			this.imageBoxFrameGrabber.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.imageBoxFrameGrabber.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.imageBoxFrameGrabber.Location = new System.Drawing.Point(12, 12);
			this.imageBoxFrameGrabber.Name = "imageBoxFrameGrabber";
			this.imageBoxFrameGrabber.Size = new System.Drawing.Size(320, 240);
			this.imageBoxFrameGrabber.TabIndex = 5;
			this.imageBoxFrameGrabber.TabStop = false;
			// 
			// timer1
			// 
			this.timer1.Enabled = true;
			this.timer1.Interval = 1000;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// labelSecondsRemaining
			// 
			this.labelSecondsRemaining.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelSecondsRemaining.AutoSize = true;
			this.labelSecondsRemaining.Location = new System.Drawing.Point(12, 259);
			this.labelSecondsRemaining.Name = "labelSecondsRemaining";
			this.labelSecondsRemaining.Size = new System.Drawing.Size(35, 13);
			this.labelSecondsRemaining.TabIndex = 6;
			this.labelSecondsRemaining.Text = "label1";
			// 
			// ConfirmUsingFaceDetection
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(346, 281);
			this.Controls.Add(this.labelSecondsRemaining);
			this.Controls.Add(this.imageBoxFrameGrabber);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "ConfirmUsingFaceDetection";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "ConfirmUsingFaceDetection";
			this.Load += new System.EventHandler(this.ConfirmUsingFaceDetection_Load);
			((System.ComponentModel.ISupportInitialize)(this.imageBoxFrameGrabber)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private Emgu.CV.UI.ImageBox imageBoxFrameGrabber;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.Label labelSecondsRemaining;
	}
}