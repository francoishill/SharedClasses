namespace SharedClasses
{
	partial class AutoUpdatingForm
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
			this.labelMessage = new System.Windows.Forms.Label();
			this.labelCurrentVersion = new System.Windows.Forms.Label();
			this.labelNewVersion = new System.Windows.Forms.Label();
			this.linkLabelClickHereLink = new System.Windows.Forms.LinkLabel();
			this.progressBar1 = new System.Windows.Forms.ProgressBar();
			this.labelStatus = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// labelMessage
			// 
			this.labelMessage.AutoSize = true;
			this.labelMessage.Location = new System.Drawing.Point(12, 8);
			this.labelMessage.Name = "labelMessage";
			this.labelMessage.Size = new System.Drawing.Size(35, 13);
			this.labelMessage.TabIndex = 1;
			this.labelMessage.Text = "label1";
			// 
			// labelCurrentVersion
			// 
			this.labelCurrentVersion.AutoSize = true;
			this.labelCurrentVersion.Location = new System.Drawing.Point(12, 24);
			this.labelCurrentVersion.Name = "labelCurrentVersion";
			this.labelCurrentVersion.Size = new System.Drawing.Size(35, 13);
			this.labelCurrentVersion.TabIndex = 2;
			this.labelCurrentVersion.Text = "label1";
			// 
			// labelNewVersion
			// 
			this.labelNewVersion.AutoSize = true;
			this.labelNewVersion.Location = new System.Drawing.Point(12, 40);
			this.labelNewVersion.Name = "labelNewVersion";
			this.labelNewVersion.Size = new System.Drawing.Size(35, 13);
			this.labelNewVersion.TabIndex = 3;
			this.labelNewVersion.Text = "label1";
			// 
			// linkLabelClickHereLink
			// 
			this.linkLabelClickHereLink.AutoSize = true;
			this.linkLabelClickHereLink.LinkArea = new System.Windows.Forms.LinkArea(0, 10);
			this.linkLabelClickHereLink.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(0)))));
			this.linkLabelClickHereLink.Location = new System.Drawing.Point(12, 68);
			this.linkLabelClickHereLink.Name = "linkLabelClickHereLink";
			this.linkLabelClickHereLink.Size = new System.Drawing.Size(179, 17);
			this.linkLabelClickHereLink.TabIndex = 4;
			this.linkLabelClickHereLink.TabStop = true;
			this.linkLabelClickHereLink.Text = "Click here to download this version";
			this.linkLabelClickHereLink.UseCompatibleTextRendering = true;
			this.linkLabelClickHereLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelClickHereLink_LinkClicked);
			// 
			// progressBar1
			// 
			this.progressBar1.Location = new System.Drawing.Point(12, 111);
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(470, 18);
			this.progressBar1.TabIndex = 5;
			this.progressBar1.Visible = false;
			// 
			// labelStatus
			// 
			this.labelStatus.AutoSize = true;
			this.labelStatus.Location = new System.Drawing.Point(12, 95);
			this.labelStatus.Name = "labelStatus";
			this.labelStatus.Size = new System.Drawing.Size(35, 13);
			this.labelStatus.TabIndex = 6;
			this.labelStatus.Text = "label1";
			this.labelStatus.Visible = false;
			// 
			// AutoUpdatingForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(494, 141);
			this.Controls.Add(this.labelStatus);
			this.Controls.Add(this.progressBar1);
			this.Controls.Add(this.linkLabelClickHereLink);
			this.Controls.Add(this.labelNewVersion);
			this.Controls.Add(this.labelCurrentVersion);
			this.Controls.Add(this.labelMessage);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AutoUpdatingForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "AutoUpdatingForm";
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label labelMessage;
		private System.Windows.Forms.Label labelCurrentVersion;
		private System.Windows.Forms.Label labelNewVersion;
		private System.Windows.Forms.LinkLabel linkLabelClickHereLink;
		private System.Windows.Forms.ProgressBar progressBar1;
		private System.Windows.Forms.Label labelStatus;
	}
}