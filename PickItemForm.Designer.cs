partial class PickItemForm
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
		this.comboBox_ItemPicked = new System.Windows.Forms.ComboBox();
		this.label_Message = new System.Windows.Forms.Label();
		this.button_OK = new System.Windows.Forms.Button();
		this.SuspendLayout();
		// 
		// comboBox_ItemPicked
		// 
		this.comboBox_ItemPicked.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
					| System.Windows.Forms.AnchorStyles.Right)));
		this.comboBox_ItemPicked.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.comboBox_ItemPicked.FormattingEnabled = true;
		this.comboBox_ItemPicked.Location = new System.Drawing.Point(12, 37);
		this.comboBox_ItemPicked.Name = "comboBox_ItemPicked";
		this.comboBox_ItemPicked.Size = new System.Drawing.Size(178, 21);
		this.comboBox_ItemPicked.TabIndex = 0;
		// 
		// label_Message
		// 
		this.label_Message.AutoSize = true;
		this.label_Message.ForeColor = System.Drawing.Color.Green;
		this.label_Message.Location = new System.Drawing.Point(13, 13);
		this.label_Message.Name = "label_Message";
		this.label_Message.Size = new System.Drawing.Size(102, 13);
		this.label_Message.TabIndex = 1;
		this.label_Message.Text = "Please pick an item:";
		// 
		// button_OK
		// 
		this.button_OK.Location = new System.Drawing.Point(274, 35);
		this.button_OK.Name = "button_OK";
		this.button_OK.Size = new System.Drawing.Size(75, 23);
		this.button_OK.TabIndex = 2;
		this.button_OK.Text = "&Ok";
		this.button_OK.UseVisualStyleBackColor = true;
		this.button_OK.Click += new System.EventHandler(this.button_OK_Click);
		// 
		// PickItemForm
		// 
		this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
		this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.ClientSize = new System.Drawing.Size(361, 70);
		this.Controls.Add(this.button_OK);
		this.Controls.Add(this.label_Message);
		this.Controls.Add(this.comboBox_ItemPicked);
		this.DoubleBuffered = true;
		this.MaximizeBox = false;
		this.MinimizeBox = false;
		this.Name = "PickItemForm";
		this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "Pick an item";
		this.TopMost = true;
		this.ResumeLayout(false);
		this.PerformLayout();

	}

	#endregion

	public System.Windows.Forms.ComboBox comboBox_ItemPicked;
	public System.Windows.Forms.Label label_Message;
	private System.Windows.Forms.Button button_OK;

}