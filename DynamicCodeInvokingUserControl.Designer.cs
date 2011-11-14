namespace SharedClasses
{
	partial class DynamicCodeInvokingUserControl
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
			this.button_PopulateSimpleTypesList = new System.Windows.Forms.Button();
			this.textBox_FilterSimpleTypesList = new System.Windows.Forms.TextBox();
			this.checkBox_Instant = new System.Windows.Forms.CheckBox();
			this.label_BusyFiltering = new System.Windows.Forms.Label();
			this.textBox_TypeName = new System.Windows.Forms.TextBox();
			this.label_SelectedType = new System.Windows.Forms.Label();
			this.comboBox_SelectedMethodOverload = new System.Windows.Forms.ComboBox();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
			this.treeView_SimpleTypesList = new System.Windows.Forms.TreeView();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.SuspendLayout();
			// 
			// button_PopulateSimpleTypesList
			// 
			this.button_PopulateSimpleTypesList.Location = new System.Drawing.Point(7, 3);
			this.button_PopulateSimpleTypesList.Name = "button_PopulateSimpleTypesList";
			this.button_PopulateSimpleTypesList.Size = new System.Drawing.Size(134, 23);
			this.button_PopulateSimpleTypesList.TabIndex = 2;
			this.button_PopulateSimpleTypesList.Text = "Populate Simple Types";
			this.button_PopulateSimpleTypesList.UseVisualStyleBackColor = true;
			this.button_PopulateSimpleTypesList.Click += new System.EventHandler(this.button_PopulateSimpleTypesList_Click);
			// 
			// textBox_FilterSimpleTypesList
			// 
			this.textBox_FilterSimpleTypesList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBox_FilterSimpleTypesList.Enabled = false;
			this.textBox_FilterSimpleTypesList.Location = new System.Drawing.Point(147, 5);
			this.textBox_FilterSimpleTypesList.Name = "textBox_FilterSimpleTypesList";
			this.textBox_FilterSimpleTypesList.Size = new System.Drawing.Size(85, 20);
			this.textBox_FilterSimpleTypesList.TabIndex = 3;
			this.textBox_FilterSimpleTypesList.TextChanged += new System.EventHandler(this.textBox_FilterSimpleTypesList_TextChanged);
			this.textBox_FilterSimpleTypesList.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBox_FilterSimpleTypesList_KeyPress);
			// 
			// checkBox_Instant
			// 
			this.checkBox_Instant.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.checkBox_Instant.AutoSize = true;
			this.checkBox_Instant.Enabled = false;
			this.checkBox_Instant.Location = new System.Drawing.Point(238, 7);
			this.checkBox_Instant.Name = "checkBox_Instant";
			this.checkBox_Instant.Size = new System.Drawing.Size(58, 17);
			this.checkBox_Instant.TabIndex = 5;
			this.checkBox_Instant.Text = "Instant";
			this.checkBox_Instant.UseVisualStyleBackColor = true;
			// 
			// label_BusyFiltering
			// 
			this.label_BusyFiltering.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.label_BusyFiltering.AutoSize = true;
			this.label_BusyFiltering.ForeColor = System.Drawing.Color.Gray;
			this.label_BusyFiltering.Location = new System.Drawing.Point(183, 329);
			this.label_BusyFiltering.Name = "label_BusyFiltering";
			this.label_BusyFiltering.Size = new System.Drawing.Size(134, 13);
			this.label_BusyFiltering.TabIndex = 6;
			this.label_BusyFiltering.Text = "Busy filtering, please wait...";
			this.label_BusyFiltering.Visible = false;
			// 
			// textBox_TypeName
			// 
			this.textBox_TypeName.Location = new System.Drawing.Point(3, 5);
			this.textBox_TypeName.Name = "textBox_TypeName";
			this.textBox_TypeName.Size = new System.Drawing.Size(147, 20);
			this.textBox_TypeName.TabIndex = 7;
			this.textBox_TypeName.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBox_TypeName_KeyPress);
			// 
			// label_SelectedType
			// 
			this.label_SelectedType.AllowDrop = true;
			this.label_SelectedType.AutoSize = true;
			this.label_SelectedType.ForeColor = System.Drawing.Color.Green;
			this.label_SelectedType.Location = new System.Drawing.Point(0, 28);
			this.label_SelectedType.Name = "label_SelectedType";
			this.label_SelectedType.Size = new System.Drawing.Size(36, 13);
			this.label_SelectedType.TabIndex = 8;
			this.label_SelectedType.Text = "None.";
			this.label_SelectedType.DragDrop += new System.Windows.Forms.DragEventHandler(this.label_SelectedType_DragDrop);
			this.label_SelectedType.DragEnter += new System.Windows.Forms.DragEventHandler(this.label_SelectedType_DragEnter);
			// 
			// comboBox_SelectedMethodOverload
			// 
			this.comboBox_SelectedMethodOverload.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.comboBox_SelectedMethodOverload.FormattingEnabled = true;
			this.comboBox_SelectedMethodOverload.Location = new System.Drawing.Point(3, 44);
			this.comboBox_SelectedMethodOverload.Name = "comboBox_SelectedMethodOverload";
			this.comboBox_SelectedMethodOverload.Size = new System.Drawing.Size(424, 21);
			this.comboBox_SelectedMethodOverload.TabIndex = 10;
			this.comboBox_SelectedMethodOverload.SelectedIndexChanged += new System.EventHandler(this.comboBox_SelectedMethodOverload_SelectedIndexChanged);
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this.splitContainer1.Location = new System.Drawing.Point(3, 3);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.propertyGrid1);
			this.splitContainer1.Panel1.Controls.Add(this.textBox_TypeName);
			this.splitContainer1.Panel1.Controls.Add(this.label_SelectedType);
			this.splitContainer1.Panel1.Controls.Add(this.comboBox_SelectedMethodOverload);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.treeView_SimpleTypesList);
			this.splitContainer1.Panel2.Controls.Add(this.label_BusyFiltering);
			this.splitContainer1.Panel2.Controls.Add(this.checkBox_Instant);
			this.splitContainer1.Panel2.Controls.Add(this.textBox_FilterSimpleTypesList);
			this.splitContainer1.Panel2.Controls.Add(this.button_PopulateSimpleTypesList);
			this.splitContainer1.Size = new System.Drawing.Size(754, 345);
			this.splitContainer1.SplitterDistance = 430;
			this.splitContainer1.TabIndex = 11;
			// 
			// propertyGrid1
			// 
			this.propertyGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.propertyGrid1.Location = new System.Drawing.Point(3, 71);
			this.propertyGrid1.Name = "propertyGrid1";
			this.propertyGrid1.PropertySort = System.Windows.Forms.PropertySort.Alphabetical;
			this.propertyGrid1.Size = new System.Drawing.Size(424, 271);
			this.propertyGrid1.TabIndex = 11;
			this.propertyGrid1.ToolbarVisible = false;
			// 
			// treeView_SimpleTypesList
			// 
			this.treeView_SimpleTypesList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.treeView_SimpleTypesList.Enabled = false;
			this.treeView_SimpleTypesList.Location = new System.Drawing.Point(3, 32);
			this.treeView_SimpleTypesList.Name = "treeView_SimpleTypesList";
			this.treeView_SimpleTypesList.ShowLines = false;
			this.treeView_SimpleTypesList.ShowPlusMinus = false;
			this.treeView_SimpleTypesList.ShowRootLines = false;
			this.treeView_SimpleTypesList.Size = new System.Drawing.Size(314, 294);
			this.treeView_SimpleTypesList.TabIndex = 7;
			this.treeView_SimpleTypesList.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.treeView_SimpleTypesList_ItemDrag);
			// 
			// DynamicCodeInvokingUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.splitContainer1);
			this.DoubleBuffered = true;
			this.Name = "DynamicCodeInvokingUserControl";
			this.Padding = new System.Windows.Forms.Padding(3);
			this.Size = new System.Drawing.Size(760, 351);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel1.PerformLayout();
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button button_PopulateSimpleTypesList;
		private System.Windows.Forms.TextBox textBox_FilterSimpleTypesList;
		private System.Windows.Forms.CheckBox checkBox_Instant;
		private System.Windows.Forms.Label label_BusyFiltering;
		private System.Windows.Forms.TextBox textBox_TypeName;
		private System.Windows.Forms.Label label_SelectedType;
		private System.Windows.Forms.ComboBox comboBox_SelectedMethodOverload;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.TreeView treeView_SimpleTypesList;
		private System.Windows.Forms.PropertyGrid propertyGrid1;


	}
}
