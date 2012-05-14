namespace SharedClasses
{
	partial class ProgressForm
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
			this.progressBar1 = new System.Windows.Forms.ProgressBar();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.labelUsermessage = new System.Windows.Forms.Label();
			this.labelTimeleft = new System.Windows.Forms.Label();
			this.labelElapsedtime = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// progressBar1
			// 
			this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.progressBar1.Location = new System.Drawing.Point(12, 31);
			this.progressBar1.MarqueeAnimationSpeed = 10;
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(405, 15);
			this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.progressBar1.TabIndex = 0;
			// 
			// buttonCancel
			// 
			this.buttonCancel.Location = new System.Drawing.Point(342, 66);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "C&ancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// labelUsermessage
			// 
			this.labelUsermessage.AutoSize = true;
			this.labelUsermessage.Location = new System.Drawing.Point(9, 9);
			this.labelUsermessage.Name = "labelUsermessage";
			this.labelUsermessage.Size = new System.Drawing.Size(74, 13);
			this.labelUsermessage.TabIndex = 2;
			this.labelUsermessage.Text = "User message";
			// 
			// labelTimeleft
			// 
			this.labelTimeleft.AutoSize = true;
			this.labelTimeleft.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
			this.labelTimeleft.Location = new System.Drawing.Point(12, 53);
			this.labelTimeleft.Name = "labelTimeleft";
			this.labelTimeleft.Size = new System.Drawing.Size(47, 13);
			this.labelTimeleft.TabIndex = 3;
			this.labelTimeleft.Text = "Time left";
			this.labelTimeleft.Visible = false;
			// 
			// labelElapsedtime
			// 
			this.labelElapsedtime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelElapsedtime.AutoSize = true;
			this.labelElapsedtime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(170)))), ((int)(((byte)(170)))));
			this.labelElapsedtime.Location = new System.Drawing.Point(12, 79);
			this.labelElapsedtime.Name = "labelElapsedtime";
			this.labelElapsedtime.Size = new System.Drawing.Size(67, 13);
			this.labelElapsedtime.TabIndex = 4;
			this.labelElapsedtime.Text = "Elapsed time";
			this.labelElapsedtime.Visible = false;
			// 
			// ProgressForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(429, 101);
			this.ControlBox = false;
			this.Controls.Add(this.labelElapsedtime);
			this.Controls.Add(this.labelTimeleft);
			this.Controls.Add(this.labelUsermessage);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.progressBar1);
			this.DoubleBuffered = true;
			this.Name = "ProgressForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonCancel;
		public System.Windows.Forms.Label labelUsermessage;
		private System.Windows.Forms.Label labelTimeleft;
		private System.Windows.Forms.Label labelElapsedtime;
		public System.Windows.Forms.ProgressBar progressBar1;

	}
}