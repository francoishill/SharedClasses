namespace SharedClasses
{
	partial class OverlayGesturesForm
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
			this.mouseGestures1 = new MouseGestures.MouseGestures(this.components);
			this.SuspendLayout();
			// 
			// mouseGestures1
			// 
			this.mouseGestures1.BeginGestureEvent += new System.EventHandler(this.mouseGestures1_BeginGestureEvent);
			this.mouseGestures1.MouseMove += new System.EventHandler(this.mouseGestures1_MouseMove);
			this.mouseGestures1.EndGestureEvent += new System.EventHandler(this.mouseGestures1_EndGestureEvent);
			this.mouseGestures1.Gesture += new MouseGestures.MouseGestures.GestureHandler(this.mouseGestures1_Gesture);
			// 
			// OverlayGesturesForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "OverlayGesturesForm";
			this.Opacity = 0.3D;
			this.Text = "OverlayGesturesForm";
			this.TopMost = true;
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			this.ResumeLayout(false);

		}

		#endregion

		private MouseGestures.MouseGestures mouseGestures1;
	}
}