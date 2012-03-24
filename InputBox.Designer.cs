namespace SharedClasses
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
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		public System.Windows.Forms.TextBox textBox_INPUT;
		public System.Windows.Forms.Label label_MESSAGE;
		private System.Windows.Forms.Button button_OK;

	}
}