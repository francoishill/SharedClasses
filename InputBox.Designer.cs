﻿namespace SharedClasses
{
	partial class InputBox
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
			this.textBox_INPUT = new System.Windows.Forms.TextBox();
			this.label_MESSAGE = new System.Windows.Forms.Label();
			this.button_OK = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// textBox_INPUT
			// 
			this.textBox_INPUT.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBox_INPUT.Location = new System.Drawing.Point(12, 96);
			this.textBox_INPUT.Name = "textBox_INPUT";
			this.textBox_INPUT.Size = new System.Drawing.Size(308, 20);
			this.textBox_INPUT.TabIndex = 0;
			this.textBox_INPUT.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBox_INPUT_KeyPress);
			// 
			// label_MESSAGE
			// 
			this.label_MESSAGE.AutoSize = true;
			this.label_MESSAGE.Location = new System.Drawing.Point(12, 9);
			this.label_MESSAGE.Name = "label_MESSAGE";
			this.label_MESSAGE.Size = new System.Drawing.Size(35, 13);
			this.label_MESSAGE.TabIndex = 1;
			this.label_MESSAGE.Text = "label1";
			// 
			// button_OK
			// 
			this.button_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button_OK.Location = new System.Drawing.Point(355, 93);
			this.button_OK.Name = "button_OK";
			this.button_OK.Size = new System.Drawing.Size(75, 23);
			this.button_OK.TabIndex = 2;
			this.button_OK.Text = "Ok";
			this.button_OK.UseVisualStyleBackColor = true;
			this.button_OK.Click += new System.EventHandler(this.button_OK_Click);
			// 
			// InputBox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.ClientSize = new System.Drawing.Size(442, 128);
			this.Controls.Add(this.button_OK);
			this.Controls.Add(this.label_MESSAGE);
			this.Controls.Add(this.textBox_INPUT);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "InputBox";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "InputBox1";
			this.Shown += new System.EventHandler(this.InputBox_Shown);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		public System.Windows.Forms.TextBox textBox_INPUT;
		private System.Windows.Forms.Label label_MESSAGE;
		private System.Windows.Forms.Button button_OK;

	}
}