partial class UserMessages
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
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.panel2 = new System.Windows.Forms.Panel();
			this.labelCountRepeatedCount = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// labelMessage
			// 
			this.labelMessage.AutoSize = true;
			this.labelMessage.Location = new System.Drawing.Point(66, 36);
			this.labelMessage.Name = "labelMessage";
			this.labelMessage.Size = new System.Drawing.Size(13, 13);
			this.labelMessage.TabIndex = 0;
			this.labelMessage.Text = "a";
			// 
			// pictureBox1
			// 
			this.pictureBox1.Location = new System.Drawing.Point(29, 27);
			this.pictureBox1.MaximumSize = new System.Drawing.Size(32, 32);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(32, 32);
			this.pictureBox1.TabIndex = 1;
			this.pictureBox1.TabStop = false;
			// 
			// panel1
			// 
			this.panel1.AutoSize = true;
			this.panel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.panel1.BackColor = System.Drawing.Color.White;
			this.panel1.Controls.Add(this.pictureBox1);
			this.panel1.Controls.Add(this.labelMessage);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Padding = new System.Windows.Forms.Padding(40, 30, 40, 70);
			this.panel1.Size = new System.Drawing.Size(194, 131);
			this.panel1.TabIndex = 2;
			// 
			// panel2
			// 
			this.panel2.AutoSize = true;
			this.panel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.panel2.Controls.Add(this.labelCountRepeatedCount);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel2.Location = new System.Drawing.Point(0, 81);
			this.panel2.MinimumSize = new System.Drawing.Size(0, 50);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(194, 50);
			this.panel2.TabIndex = 3;
			// 
			// labelCountRepeatedCount
			// 
			this.labelCountRepeatedCount.AutoSize = true;
			this.labelCountRepeatedCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelCountRepeatedCount.ForeColor = System.Drawing.Color.Gray;
			this.labelCountRepeatedCount.Location = new System.Drawing.Point(12, 20);
			this.labelCountRepeatedCount.Name = "labelCountRepeatedCount";
			this.labelCountRepeatedCount.Size = new System.Drawing.Size(13, 13);
			this.labelCountRepeatedCount.TabIndex = 0;
			this.labelCountRepeatedCount.Text = "0";
			this.labelCountRepeatedCount.Visible = false;
			// 
			// UserMessages
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ClientSize = new System.Drawing.Size(194, 131);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.panel1);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.KeyPreview = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(200, 38);
			this.Name = "UserMessages";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "UserMessages";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.UserMessages_FormClosing);
			this.SizeChanged += new System.EventHandler(this.UserMessages_SizeChanged);
			this.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.GlobalKeyDown);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

	}

	#endregion

	private System.Windows.Forms.Label labelMessage;
	private System.Windows.Forms.PictureBox pictureBox1;
	private System.Windows.Forms.Panel panel1;
	private System.Windows.Forms.Panel panel2;
	private System.Windows.Forms.Label labelCountRepeatedCount;
}