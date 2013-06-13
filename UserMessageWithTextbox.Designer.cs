namespace SharedClasses
{
	partial class UserMessageWithTextbox
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
			this.buttonOK = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.buttonGotoSelectedPath = new System.Windows.Forms.Button();
			this.buttonOpenSelectedPath = new System.Windows.Forms.Button();
			this.labelIfTheRootDirsAreSet = new System.Windows.Forms.Label();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.SuspendLayout();
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.Location = new System.Drawing.Point(573, 253);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 0;
			this.buttonOK.Text = "&Ok";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(10, 18);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(35, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "label1";
			// 
			// textBox1
			// 
			this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBox1.HideSelection = false;
			this.textBox1.Location = new System.Drawing.Point(13, 67);
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBox1.Size = new System.Drawing.Size(635, 180);
			this.textBox1.TabIndex = 2;
			this.textBox1.KeyUp += new System.Windows.Forms.KeyEventHandler(this.textBox1_KeyUp);
			this.textBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.textBox1_MouseUp);
			// 
			// buttonGotoSelectedPath
			// 
			this.buttonGotoSelectedPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonGotoSelectedPath.Enabled = false;
			this.buttonGotoSelectedPath.Location = new System.Drawing.Point(453, 8);
			this.buttonGotoSelectedPath.Name = "buttonGotoSelectedPath";
			this.buttonGotoSelectedPath.Size = new System.Drawing.Size(195, 23);
			this.buttonGotoSelectedPath.TabIndex = 3;
			this.buttonGotoSelectedPath.Text = "&Goto selected file/folder";
			this.buttonGotoSelectedPath.UseVisualStyleBackColor = true;
			this.buttonGotoSelectedPath.Click += new System.EventHandler(this.buttonGotoSelectedPath_Click);
			// 
			// buttonOpenSelectedPath
			// 
			this.buttonOpenSelectedPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOpenSelectedPath.Enabled = false;
			this.buttonOpenSelectedPath.Location = new System.Drawing.Point(453, 37);
			this.buttonOpenSelectedPath.Name = "buttonOpenSelectedPath";
			this.buttonOpenSelectedPath.Size = new System.Drawing.Size(195, 23);
			this.buttonOpenSelectedPath.TabIndex = 4;
			this.buttonOpenSelectedPath.Text = "Open selected file/folder";
			this.buttonOpenSelectedPath.UseVisualStyleBackColor = true;
			this.buttonOpenSelectedPath.Click += new System.EventHandler(this.buttonOpenSelectedPath_Click);
			// 
			// labelIfTheRootDirsAreSet
			// 
			this.labelIfTheRootDirsAreSet.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelIfTheRootDirsAreSet.AutoSize = true;
			this.labelIfTheRootDirsAreSet.Location = new System.Drawing.Point(9, 258);
			this.labelIfTheRootDirsAreSet.Name = "labelIfTheRootDirsAreSet";
			this.labelIfTheRootDirsAreSet.Size = new System.Drawing.Size(483, 13);
			this.labelIfTheRootDirsAreSet.TabIndex = 5;
			this.labelIfTheRootDirsAreSet.Text = "Directories in which selected files might be which will all be used in opening fi" +
    "les/folders (hover for list)";
			this.labelIfTheRootDirsAreSet.Visible = false;
			// 
			// UserMessageWithTextbox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(660, 288);
			this.Controls.Add(this.labelIfTheRootDirsAreSet);
			this.Controls.Add(this.buttonOpenSelectedPath);
			this.Controls.Add(this.buttonGotoSelectedPath);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.buttonOK);
			this.DoubleBuffered = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "UserMessageWithTextbox";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Form2";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Button buttonGotoSelectedPath;
		private System.Windows.Forms.Button buttonOpenSelectedPath;
		private System.Windows.Forms.Label labelIfTheRootDirsAreSet;
		private System.Windows.Forms.ToolTip toolTip1;
	}
}