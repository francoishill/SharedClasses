﻿partial class OverlayForm
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
		this.SuspendLayout();
		// 
		// OverlayForm
		// 
		this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
		this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.Black;
		this.ClientSize = new System.Drawing.Size(292, 270);
		this.DoubleBuffered = true;
		this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
		this.Name = "OverlayForm";
		this.Opacity = 0D;
		this.ShowIcon = false;
		this.ShowInTaskbar = false;
		this.Text = "OverlayForm";
		this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
		this.Activated += new System.EventHandler(this.OverlayForm_Activated);
		this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OverlayForm_FormClosing);
		this.Shown += new System.EventHandler(this.OverlayForm_Shown);
		this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OverlayForm_KeyDown);
		this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OverlayForm_MouseClick);
		this.ResumeLayout(false);

	}

	#endregion


}