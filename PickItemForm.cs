using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SharedClasses;

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
		return (T)PickItem(typeof(T), itemArray, Message, defaultItem, owner);
		//PickItemForm pickItemForm = new PickItemForm();
		//pickItemForm.label_Message.Text = Message;
		//pickItemForm.comboBox_ItemPicked.Items.Clear();
		//if (itemArray != null)
		//	foreach (T item in itemArray)
		//		pickItemForm.comboBox_ItemPicked.Items.Add(item);
		//if (pickItemForm.ShowDialog(owner) == DialogResult.OK)
		//	return (T)pickItemForm.comboBox_ItemPicked.SelectedItem;
		//else return defaultItem;
	}

	public static object PickItem(Type ObjectType, Array itemArray, string Message, object defaultItem, IWin32Window owner = null)
	{
		PickItemForm pickItemForm = new PickItemForm();
		if (owner == null) pickItemForm.StartPosition = FormStartPosition.CenterScreen;
		pickItemForm.label_Message.Text = Message;
		pickItemForm.comboBox_ItemPicked.Items.Clear();
		if (itemArray != null)
			foreach (var item in itemArray)
				pickItemForm.comboBox_ItemPicked.Items.Add(item);
		if (pickItemForm.ShowDialog(owner) == DialogResult.OK)
			return pickItemForm.comboBox_ItemPicked.SelectedItem;
		else return defaultItem;
	}

	public static T PickItem<T>(Array itemArray, string Message, T defaultItem)
	{
		return PickItem<T>(null, itemArray, Message, defaultItem);
	}
	public static object PickItem(Type ObjectType, Array itemArray, string Message, object defaultItem)
	{
		return PickItem(ObjectType, null, itemArray, Message, defaultItem);
	}
	public static T PickItem<T>(IWin32Window owner, Array itemArray, string Message, T defaultItem)
	{
		//return PickItemForm.PickItem<T>(itemArray, Message, defaultItem, owner);
		return (T)PickItem(typeof(T), owner, itemArray, Message, defaultItem);
	}
	public static object PickItem(Type ObjectType, IWin32Window owner, Array itemArray, string Message, object defaultItem)
	{
		return PickItemForm.PickItem(ObjectType, itemArray, Message, defaultItem, owner);
	}

	public static T PickItem<T>(List<T> itemList, string Message, T defaultItem)
	{
		return PickItem<T>(null, itemList, Message, defaultItem);
	}
	public static T PickItem<T>(IWin32Window owner, List<T> itemList, string Message, T defaultItem)
	{
		return PickItemForm.PickItem<T>(itemList.ToArray(), Message, defaultItem, owner);
	}
}