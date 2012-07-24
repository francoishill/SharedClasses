namespace SharedClasses
{
	partial class LinkedFolderUserControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.buttonRemoveSelf = new System.Windows.Forms.Button();
			this.textBoxFtpPassword = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.textBoxFtpUsername = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.textBoxFtpRootUrl = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.buttonBrowseForLocalFolder = new System.Windows.Forms.Button();
			this.textBoxLocalRootDirectory = new System.Windows.Forms.TextBox();
			this.textBoxExcludedFolders = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.textBoxExcludedFolders);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.buttonRemoveSelf);
			this.groupBox1.Controls.Add(this.textBoxFtpPassword);
			this.groupBox1.Controls.Add(this.label5);
			this.groupBox1.Controls.Add(this.textBoxFtpUsername);
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.textBoxFtpRootUrl);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.buttonBrowseForLocalFolder);
			this.groupBox1.Controls.Add(this.textBoxLocalRootDirectory);
			this.groupBox1.Location = new System.Drawing.Point(3, 3);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(620, 277);
			this.groupBox1.TabIndex = 20;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "groupBox1";
			// 
			// buttonRemoveSelf
			// 
			this.buttonRemoveSelf.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonRemoveSelf.Location = new System.Drawing.Point(551, 228);
			this.buttonRemoveSelf.Name = "buttonRemoveSelf";
			this.buttonRemoveSelf.Size = new System.Drawing.Size(55, 23);
			this.buttonRemoveSelf.TabIndex = 28;
			this.buttonRemoveSelf.Text = "Remove";
			this.buttonRemoveSelf.UseVisualStyleBackColor = true;
			this.buttonRemoveSelf.Visible = false;
			// 
			// textBoxFtpPassword
			// 
			this.textBoxFtpPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxFtpPassword.Location = new System.Drawing.Point(123, 108);
			this.textBoxFtpPassword.Name = "textBoxFtpPassword";
			this.textBoxFtpPassword.PasswordChar = '*';
			this.textBoxFtpPassword.Size = new System.Drawing.Size(254, 20);
			this.textBoxFtpPassword.TabIndex = 27;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(36, 111);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(79, 13);
			this.label5.TabIndex = 26;
			this.label5.Text = "FTP Password:";
			// 
			// textBoxFtpUsername
			// 
			this.textBoxFtpUsername.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxFtpUsername.Location = new System.Drawing.Point(123, 82);
			this.textBoxFtpUsername.Name = "textBoxFtpUsername";
			this.textBoxFtpUsername.Size = new System.Drawing.Size(254, 20);
			this.textBoxFtpUsername.TabIndex = 25;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(36, 85);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(81, 13);
			this.label4.TabIndex = 24;
			this.label4.Text = "FTP Username:";
			// 
			// textBoxFtpRootUrl
			// 
			this.textBoxFtpRootUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxFtpRootUrl.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			this.textBoxFtpRootUrl.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.AllUrl;
			this.textBoxFtpRootUrl.Location = new System.Drawing.Point(123, 56);
			this.textBoxFtpRootUrl.Name = "textBoxFtpRootUrl";
			this.textBoxFtpRootUrl.Size = new System.Drawing.Size(452, 20);
			this.textBoxFtpRootUrl.TabIndex = 23;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(50, 59);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(67, 13);
			this.label3.TabIndex = 22;
			this.label3.Text = "FTP root Url:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(17, 33);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(100, 13);
			this.label2.TabIndex = 21;
			this.label2.Text = "Local root directory:";
			// 
			// buttonBrowseForLocalFolder
			// 
			this.buttonBrowseForLocalFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonBrowseForLocalFolder.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.buttonBrowseForLocalFolder.Location = new System.Drawing.Point(581, 28);
			this.buttonBrowseForLocalFolder.Name = "buttonBrowseForLocalFolder";
			this.buttonBrowseForLocalFolder.Size = new System.Drawing.Size(25, 23);
			this.buttonBrowseForLocalFolder.TabIndex = 20;
			this.buttonBrowseForLocalFolder.Text = "...";
			this.buttonBrowseForLocalFolder.UseVisualStyleBackColor = true;
			this.buttonBrowseForLocalFolder.Click += new System.EventHandler(this.buttonBrowseForLocalFolder_Click_1);
			// 
			// textBoxLocalRootDirectory
			// 
			this.textBoxLocalRootDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxLocalRootDirectory.Location = new System.Drawing.Point(123, 30);
			this.textBoxLocalRootDirectory.Name = "textBoxLocalRootDirectory";
			this.textBoxLocalRootDirectory.Size = new System.Drawing.Size(452, 20);
			this.textBoxLocalRootDirectory.TabIndex = 19;
			// 
			// textBoxExcludedFolders
			// 
			this.textBoxExcludedFolders.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxExcludedFolders.Location = new System.Drawing.Point(34, 160);
			this.textBoxExcludedFolders.Multiline = true;
			this.textBoxExcludedFolders.Name = "textBoxExcludedFolders";
			this.textBoxExcludedFolders.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBoxExcludedFolders.Size = new System.Drawing.Size(288, 91);
			this.textBoxExcludedFolders.TabIndex = 30;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(31, 144);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(160, 13);
			this.label1.TabIndex = 29;
			this.label1.Text = "Excluded folders (relative paths):";
			// 
			// LinkedFolderUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.groupBox1);
			this.Name = "LinkedFolderUserControl";
			this.Size = new System.Drawing.Size(626, 309);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button buttonBrowseForLocalFolder;
		public System.Windows.Forms.GroupBox groupBox1;
		public System.Windows.Forms.TextBox textBoxFtpPassword;
		public System.Windows.Forms.TextBox textBoxFtpUsername;
		public System.Windows.Forms.TextBox textBoxFtpRootUrl;
		public System.Windows.Forms.TextBox textBoxLocalRootDirectory;
		public System.Windows.Forms.Button buttonRemoveSelf;
		public System.Windows.Forms.TextBox textBoxExcludedFolders;
		private System.Windows.Forms.Label label1;

	}
}
