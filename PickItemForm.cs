using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

public partial class PickItemForm : Form
{
	private PickItemForm()
	{
		InitializeComponent();
	}

	private void button_OK_Click(object sender, EventArgs e)
	{
		if (comboBox_ItemPicked.SelectedIndex == -1)
			UserMessages.ShowWarningMessage("Please select an item.");
		else this.DialogResult = System.Windows.Forms.DialogResult.OK;
	}

	public static T PickItem<T>(Array itemArray, string Message, T defaultItem, IWin32Window owner = null)
	{
		PickItemForm pickItemForm = new PickItemForm();
		pickItemForm.label_Message.Text = Message;
		pickItemForm.comboBox_ItemPicked.Items.Clear();
		if (itemArray != null)
			foreach (T item in itemArray)
				pickItemForm.comboBox_ItemPicked.Items.Add(item);
		if (pickItemForm.ShowDialog(owner) == DialogResult.OK)
			return (T)pickItemForm.comboBox_ItemPicked.SelectedItem;
		else return defaultItem;
	}
}