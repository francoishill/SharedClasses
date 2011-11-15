namespace SharedClasses
{
	partial class InlineCommandsUserControl
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
			this.components = new System.ComponentModel.Container();
			this.richTextBox_CommandLine = new System.Windows.Forms.RichTextBox();
			this.treeView_CommandList = new System.Windows.Forms.TreeView();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.textBox_Messages = new System.Windows.Forms.TextBox();
			this.label_ArgumentsExample = new System.Windows.Forms.Label();
			this.toolTip_EntireUserControl = new System.Windows.Forms.ToolTip(this.components);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.SuspendLayout();
			// 
			// richTextBox_CommandLine
			// 
			this.richTextBox_CommandLine.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.richTextBox_CommandLine.AutoWordSelection = true;
			this.richTextBox_CommandLine.EnableAutoDragDrop = true;
			this.richTextBox_CommandLine.Location = new System.Drawing.Point(2, 6);
			this.richTextBox_CommandLine.Multiline = false;
			this.richTextBox_CommandLine.Name = "richTextBox_CommandLine";
			this.richTextBox_CommandLine.ShowSelectionMargin = true;
			this.richTextBox_CommandLine.Size = new System.Drawing.Size(400, 20);
			this.richTextBox_CommandLine.TabIndex = 0;
			this.richTextBox_CommandLine.Text = "";
			this.richTextBox_CommandLine.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.richTextBox1_KeyPress);
			// 
			// treeView_CommandList
			// 
			this.treeView_CommandList.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeView_CommandList.HideSelection = false;
			this.treeView_CommandList.Location = new System.Drawing.Point(0, 0);
			this.treeView_CommandList.Name = "treeView_CommandList";
			this.treeView_CommandList.ShowLines = false;
			this.treeView_CommandList.ShowPlusMinus = false;
			this.treeView_CommandList.ShowRootLines = false;
			this.treeView_CommandList.Size = new System.Drawing.Size(400, 86);
			this.treeView_CommandList.TabIndex = 1;
			this.treeView_CommandList.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView_CommandList_AfterSelect);
			// 
			// splitContainer1
			// 
			this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainer1.Location = new System.Drawing.Point(2, 45);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.treeView_CommandList);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.textBox_Messages);
			this.splitContainer1.Size = new System.Drawing.Size(400, 202);
			this.splitContainer1.SplitterDistance = 86;
			this.splitContainer1.TabIndex = 2;
			// 
			// textBox_Messages
			// 
			this.textBox_Messages.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
			this.textBox_Messages.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBox_Messages.Location = new System.Drawing.Point(0, 0);
			this.textBox_Messages.Multiline = true;
			this.textBox_Messages.Name = "textBox_Messages";
			this.textBox_Messages.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBox_Messages.Size = new System.Drawing.Size(400, 112);
			this.textBox_Messages.TabIndex = 0;
			// 
			// label_ArgumentsExample
			// 
			this.label_ArgumentsExample.AutoEllipsis = true;
			this.label_ArgumentsExample.AutoSize = true;
			this.label_ArgumentsExample.ForeColor = System.Drawing.Color.Gray;
			this.label_ArgumentsExample.Location = new System.Drawing.Point(-1, 29);
			this.label_ArgumentsExample.Name = "label_ArgumentsExample";
			this.label_ArgumentsExample.Size = new System.Drawing.Size(101, 13);
			this.label_ArgumentsExample.TabIndex = 3;
			this.label_ArgumentsExample.Text = " arguments example";
			// 
			// InlineCommandsUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label_ArgumentsExample);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.richTextBox_CommandLine);
			this.Name = "InlineCommandsUserControl";
			this.Padding = new System.Windows.Forms.Padding(2);
			this.Size = new System.Drawing.Size(404, 247);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.RichTextBox richTextBox_CommandLine;
		private System.Windows.Forms.TreeView treeView_CommandList;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.TextBox textBox_Messages;
		private System.Windows.Forms.Label label_ArgumentsExample;
		private System.Windows.Forms.ToolTip toolTip_EntireUserControl;
	}
}
