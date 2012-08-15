using System;
using System.Windows;
using System.Collections.Generic;

namespace SharedClasses
{
	public partial class PickItemWPF : Window
	{
		public PickItemWPF()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (comboBox_ItemPicked.SelectedIndex == -1)
				UserMessages.ShowWarningMessage("Please select an item.");
			else this.DialogResult = true;
		}

		public static object PickItem(Type ObjectType, Array itemArray, string Message, object defaultItem, System.Windows.Window owner = null)
		{
			PickItemWPF pickItemWindow = new PickItemWPF();
			pickItemWindow.label_Message.Content = Message;
			pickItemWindow.comboBox_ItemPicked.Items.Clear();
			if (itemArray != null)
				foreach (var item in itemArray)
					pickItemWindow.comboBox_ItemPicked.Items.Add(item);
			if (pickItemWindow.ShowDialog() == true)
				return pickItemWindow.comboBox_ItemPicked.SelectedItem;
			else return defaultItem;
		}

		public static object PickItem(Type ObjectType, Array itemArray, string Message, object defaultItem)
		{
			return SharedClasses.PickItemWPF.PickItem(ObjectType, itemArray, Message, defaultItem, null);
		}

		public static T PickItem<T>(List<T> items, string Message, T defaultItem)
		{
			return (T)PickItem(typeof(T), items.ToArray(), Message, defaultItem);
		}
	}
}