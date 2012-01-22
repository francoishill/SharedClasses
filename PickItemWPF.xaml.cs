using System;
using System.Windows;

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
	}
}