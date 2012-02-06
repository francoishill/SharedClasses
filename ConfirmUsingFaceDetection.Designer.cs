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
			this.labelUserPrompt = new System.Windows.Forms.Label();
			this.textBoxManuallyEnteredPassword = new System.Windows.Forms.TextBox();
			this.buttonUseThisPassword = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.imageBoxFrameGrabber)).BeginInit();
			this.SuspendLayout();
			// 
			// imageBoxFrameGrabber
			// 
			this.imageBoxFrameGrabber.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.imageBoxFrameGrabber.Location = new System.Drawing.Point(11, 42);
			this.imageBoxFrameGrabber.Name = "imageBoxFrameGrabber";
			this.imageBoxFrameGrabber.Size = new System.Drawing.Size(320, 240);
			this.imageBoxFrameGrabber.TabIndex = 5;
			this.imageBoxFrameGrabber.TabStop = false;
			// 
			// timer1
			// 
			this.timer1.Interval = 1000;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// labelSecondsRemaining
			// 
			this.labelSecondsRemaining.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelSecondsRemaining.AutoSize = true;
			this.labelSecondsRemaining.ForeColor = System.Drawing.Color.Gray;
			this.labelSecondsRemaining.Location = new System.Drawing.Point(8, 316);
			this.labelSecondsRemaining.Name = "labelSecondsRemaining";
			this.labelSecondsRemaining.Size = new System.Drawing.Size(35, 13);
			this.labelSecondsRemaining.TabIndex = 6;
			this.labelSecondsRemaining.Text = "label1";
			// 
			// labelUserPrompt
			// 
			this.labelUserPrompt.AutoEllipsis = true;
			this.labelUserPrompt.AutoSize = true;
			this.labelUserPrompt.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelUserPrompt.ForeColor = System.Drawing.Color.Green;
			this.labelUserPrompt.Location = new System.Drawing.Point(12, 9);
			this.labelUserPrompt.Name = "labelUserPrompt";
			this.labelUserPrompt.Size = new System.Drawing.Size(94, 13);
			this.labelUserPrompt.TabIndex = 7;
			this.labelUserPrompt.Text = "Face detection for:";
			this.labelUserPrompt.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// textBoxManuallyEnteredPassword
			// 
			this.textBoxManuallyEnteredPassword.Location = new System.Drawing.Point(11, 289);
			this.textBoxManuallyEnteredPassword.Name = "textBoxManuallyEnteredPassword";
			this.textBoxManuallyEnteredPassword.Size = new System.Drawing.Size(172, 20);
			this.textBoxManuallyEnteredPassword.TabIndex = 8;
			this.textBoxManuallyEnteredPassword.UseSystemPasswordChar = true;
			this.textBoxManuallyEnteredPassword.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxManuallyEnteredPassword_KeyPress);
			this.textBoxManuallyEnteredPassword.Leave += new System.EventHandler(this.textBoxManuallyEnteredPassword_Leave);
			// 
			// buttonUseThisPassword
			// 
			this.buttonUseThisPassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonUseThisPassword.Location = new System.Drawing.Point(217, 287);
			this.buttonUseThisPassword.Name = "buttonUseThisPassword";
			this.buttonUseThisPassword.Size = new System.Drawing.Size(114, 23);
			this.buttonUseThisPassword.TabIndex = 9;
			this.buttonUseThisPassword.Text = "Use &this password";
			this.buttonUseThisPassword.UseVisualStyleBackColor = true;
			this.buttonUseThisPassword.Click += new System.EventHandler(this.buttonUseThisPassword_Click);
			// 
			// ConfirmUsingFaceDetection
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(343, 338);
			this.Controls.Add(this.buttonUseThisPassword);
			this.Controls.Add(this.textBoxManuallyEnteredPassword);
			this.Controls.Add(this.labelUserPrompt);
			this.Controls.Add(this.labelSecondsRemaining);
			this.Controls.Add(this.imageBoxFrameGrabber);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "ConfirmUsingFaceDetection";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "ConfirmUsingFaceDetection";
			this.TopMost = true;
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ConfirmUsingFaceDetection_FormClosing);
			this.Load += new System.EventHandler(this.ConfirmUsingFaceDetection_Load);
			((System.ComponentModel.ISupportInitialize)(this.imageBoxFrameGrabber)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private Emgu.CV.UI.ImageBox imageBoxFrameGrabber;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.Label labelSecondsRemaining;
		private System.Windows.Forms.Label labelUserPrompt;
		private System.Windows.Forms.Button buttonUseThisPassword;
		public System.Windows.Forms.TextBox textBoxManuallyEnteredPassword;
	}
}