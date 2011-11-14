using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.CodeDom;
using System.Collections;
using MethodDetailsClass = DynamicCodeInvoking.MethodDetailsClass;

namespace SharedClasses
{
	public partial class DynamicCodeInvokingUserControl : UserControl
	{
		string[] OriginalSimpleTypesList = new string[0];
		bool isBusyFiltering = false;

		public DynamicCodeInvokingUserControl()
		{
			Constructor_DynamicCodeInvokingUserControl();
		}

		public DynamicCodeInvokingUserControl(bool PrepopulateSimpleTypesList = false)
		{
			Constructor_DynamicCodeInvokingUserControl();
		}

		public void Constructor_DynamicCodeInvokingUserControl(bool PrepopulateSimpleTypesList = false)
		{
			InitializeComponent();
			this.ParentChanged += delegate
			{
				if (this.ParentForm != null)
				{
					this.ParentForm.HandleCreated += delegate
					{
						StylingInterop.SetTreeviewVistaStyle(treeView_SimpleTypesList);
					};
				}
			};
			if (PrepopulateSimpleTypesList)
				PopulateSimpleTypesList();
		}

		private void button_PopulateSimpleTypesList_Click(object sender, EventArgs e)
		{
			PopulateSimpleTypesList();
		}

		private void PopulateSimpleTypesList()
		{
			if (!treeView_SimpleTypesList.Enabled)
			{
				if (treeView_SimpleTypesList.Nodes.Count == 0)
				{
					this.Cursor = Cursors.WaitCursor;
					treeView_SimpleTypesList.BeginUpdate();
					List<string> alltypesList = DynamicCodeInvoking.GetAllUniqueSimpleTypeStringsInCurrentAssembly;
					OriginalSimpleTypesList = alltypesList.ToArray();
					foreach (string type in alltypesList)
						treeView_SimpleTypesList.Nodes.Add(type, type);
					treeView_SimpleTypesList.EndUpdate();
					this.Cursor = Cursors.Default;
				}
				treeView_SimpleTypesList.Enabled = true;
				textBox_FilterSimpleTypesList.Enabled = true;
				checkBox_Instant.Enabled = true;
				button_PopulateSimpleTypesList.Enabled = false;
			}
		}

		private void textBox_FilterSimpleTypesList_TextChanged(object sender, EventArgs e)
		{
			if (checkBox_Instant.Checked) FilterSimpleTypesList(textBox_FilterSimpleTypesList.Text);
		}

		private void FilterSimpleTypesList(string filterText)
		{
			isBusyFiltering = true;
			label_BusyFiltering.Visible = true;
			Application.DoEvents();

			this.Cursor = Cursors.WaitCursor;
			treeView_SimpleTypesList.BeginUpdate();
			treeView_SimpleTypesList.Nodes.Clear();
			foreach (string s in OriginalSimpleTypesList)
				if (s.ToLower().Contains(filterText.ToLower()))
					treeView_SimpleTypesList.Nodes.Add(s);
			treeView_SimpleTypesList.EndUpdate();
			this.Cursor = Cursors.Default;

			label_BusyFiltering.Visible = false;
			isBusyFiltering = false;
			Application.DoEvents();
		}

		private void textBox_FilterSimpleTypesList_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (isBusyFiltering)
				e.Handled = true;
			if (!checkBox_Instant.Checked && ((int)e.KeyChar == 13))
				FilterSimpleTypesList(textBox_FilterSimpleTypesList.Text);
		}

		private void label_SelectedType_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
				e.Effect = DragDropEffects.Move;
		}

		private void label_SelectedType_DragDrop(object sender, DragEventArgs e)
		{
			TreeNode node = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");
			PopulateControlsUsingType(node.Text);
		}

		private void treeView_SimpleTypesList_ItemDrag(object sender, ItemDragEventArgs e)
		{
			DoDragDrop(e.Item, DragDropEffects.All);
		}

		private void textBox_TypeName_KeyPress(object sender, KeyPressEventArgs e)
		{
			if ((int)e.KeyChar == 13)
			{
				PopulateControlsUsingType(textBox_TypeName.Text, true);
			}
		}

		private void PopulateControlsUsingType(string SimpleNameString, bool ignoreCase = false)
		{
			Type type = DynamicCodeInvoking.GetTypeFromSimpleString(SimpleNameString, ignoreCase);
			comboBox_SelectedMethodOverload.Items.Clear();
			if (type == null)
			{
				label_SelectedType.Text = "None.";
				return;
			}
			label_SelectedType.Text = type.Name;
			label_SelectedType.Tag = type;
			MethodInfo[] methodInfos = type.GetMethods(
				System.Reflection.BindingFlags.Public | BindingFlags.Static);
			foreach (MethodInfo mi in methodInfos)
				comboBox_SelectedMethodOverload.Items.Add(new DynamicCodeInvoking.MethodDetailsClass(mi));
			if (comboBox_SelectedMethodOverload.Items.Count == 1)
				comboBox_SelectedMethodOverload.SelectedIndex = 0;
		}

		private void comboBox_SelectedMethodOverload_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (comboBox_SelectedMethodOverload.SelectedIndex == -1) return;

			if (!(comboBox_SelectedMethodOverload.SelectedItem is DynamicCodeInvoking.MethodDetailsClass)
				&& UserMessages.ShowWarningMessage("Cannot cast comboboxItem to DynamicCodeInvoking.MethodDetailsClass: " + comboBox_SelectedMethodOverload.SelectedItem.ToString()))
				return;

			MethodDetailsClass methodDetails = comboBox_SelectedMethodOverload.SelectedItem as MethodDetailsClass;
			AssignObjectToPropertyGrid(methodDetails);
		}

		public void AssignObjectToPropertyGrid(MethodDetailsClass methodDetails)
		{
			//IDictionary d = new Hashtable();
			//d["Hello"] = "World";
			//d["Meaning"] = 42;
			//d["Shade"] = Color.ForestGreen;
			//propertyGrid1.SelectedObject = new DictionaryPropertyGridAdapter(d);
			//CodeTypeDeclaration dynamicClass = new CodeTypeDeclaration("TestClass");
			//dynamicClass.IsClass = true;

			//CodeMemberProperty dynamicProperty = new CodeMemberProperty();
			//dynamicProperty.Type = new CodeTypeReference(typeof(string));
			//dynamicProperty.Name = "MyFirstProperty";

			//dynamicClass.Members.Add(dynamicProperty);
			//propertyGrid1.SelectedObject = dynamicClass;
			//return;
			propertyGrid1.SelectedObject = new DictionaryPropertyGridAdapter(methodDetails.HashTableOfParameters);
		}

		public Dictionary<string, ParameterNameAndType> GetSelectedDictionaryWithParameterNamesAndValues()
		{
			return (propertyGrid1.SelectedObject as DictionaryPropertyGridAdapter)._dictionary;
		}

		public Type GetSelectedMethodClassType()
		{
			if (label_SelectedType.Tag == null && UserMessages.ShowWarningMessage("No type dropped on label"))
				return null;
			return label_SelectedType.Tag as Type;
		}

		public string GetSelectedMethodName()
		{
			return (comboBox_SelectedMethodOverload.SelectedItem as MethodDetailsClass).MethodName;
		}
	}
}
