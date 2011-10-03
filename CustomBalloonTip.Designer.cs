namespace MonitorSystem
{
	partial class CustomBalloonTip
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CustomBalloonTip));
			this.button_Close = new System.Windows.Forms.Button();
			this.timer_ShowDuration = new System.Windows.Forms.Timer(this.components);
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.pictureBox_Icon = new System.Windows.Forms.ToolStripDropDownButton();
			this.label_Title = new System.Windows.Forms.ToolStripStatusLabel();
			this.label_Message = new System.Windows.Forms.ToolStripStatusLabel();
			this.statusStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// button_Close
			// 
			this.button_Close.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.button_Close.BackColor = System.Drawing.Color.Transparent;
			this.button_Close.Cursor = System.Windows.Forms.Cursors.Hand;
			this.button_Close.FlatAppearance.BorderSize = 0;
			this.button_Close.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
			this.button_Close.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
			this.button_Close.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.button_Close.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.button_Close.ForeColor = System.Drawing.Color.Red;
			this.button_Close.Location = new System.Drawing.Point(635, -1);
			this.button_Close.Name = "button_Close";
			this.button_Close.Size = new System.Drawing.Size(20, 17);
			this.button_Close.TabIndex = 0;
			this.button_Close.Text = "x";
			this.button_Close.UseVisualStyleBackColor = false;
			this.button_Close.Click += new System.EventHandler(this.button1_Click);
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pictureBox_Icon,
            this.label_Title,
            this.label_Message});
			this.statusStrip1.Location = new System.Drawing.Point(0, -2);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.ManagerRenderMode;
			this.statusStrip1.Size = new System.Drawing.Size(654, 22);
			this.statusStrip1.SizingGrip = false;
			this.statusStrip1.TabIndex = 4;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// pictureBox_Icon
			// 
			this.pictureBox_Icon.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.pictureBox_Icon.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox_Icon.Image")));
			this.pictureBox_Icon.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.pictureBox_Icon.Name = "pictureBox_Icon";
			this.pictureBox_Icon.ShowDropDownArrow = false;
			this.pictureBox_Icon.Size = new System.Drawing.Size(20, 20);
			this.pictureBox_Icon.Text = "toolStripDropDownButton1";
			// 
			// label_Title
			// 
			this.label_Title.Font = new System.Drawing.Font("Verdana", 7F);
			this.label_Title.ForeColor = System.Drawing.Color.Green;
			this.label_Title.Margin = new System.Windows.Forms.Padding(0);
			this.label_Title.Name = "label_Title";
			this.label_Title.Size = new System.Drawing.Size(123, 22);
			this.label_Title.Text = "toolStripStatusLabel1";
			// 
			// label_Message
			// 
			this.label_Message.Font = new System.Drawing.Font("Verdana", 7F);
			this.label_Message.Margin = new System.Windows.Forms.Padding(20, 0, 0, 0);
			this.label_Message.Name = "label_Message";
			this.label_Message.Size = new System.Drawing.Size(476, 22);
			this.label_Message.Spring = true;
			this.label_Message.Text = "toolStripStatusLabel2";
			this.label_Message.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// CustomBalloonTip
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(230)))), ((int)(((byte)(240)))));
			this.ClientSize = new System.Drawing.Size(654, 20);
			this.ControlBox = false;
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.button_Close);
			this.Cursor = System.Windows.Forms.Cursors.Default;
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "CustomBalloonTip";
			this.Opacity = 0.98D;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.TopMost = true;
			this.Shown += new System.EventHandler(this.CustomBalloonTip_Shown);
			this.Resize += new System.EventHandler(this.CustomBalloonTip_Resize);
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button button_Close;
		private System.Windows.Forms.Timer timer_ShowDuration;
		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.ToolStripStatusLabel label_Title;
		private System.Windows.Forms.ToolStripStatusLabel label_Message;
		private System.Windows.Forms.ToolStripDropDownButton pictureBox_Icon;
	}
}