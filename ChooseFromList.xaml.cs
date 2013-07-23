using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for ChooseFromList.xaml
	/// </summary>
	public partial class ChooseFromList : Window
	{
		string NameOfAnItem;

		public ChooseFromList(string nameOfAnItem, IEnumerable<object> listOfItems, bool allowManualTextEntry)
		{
			InitializeComponent();
			this.NameOfAnItem = nameOfAnItem;
			this.labelUserMessage.Content = "Please select (or type) a " + nameOfAnItem;
			this.comboBoxList.ItemsSource = listOfItems;
			this.comboBoxList.IsEditable = allowManualTextEntry;
		}

		private void buttonCancel_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.Close();
		}

		private void buttonOK_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(comboBoxList.Text))
			{
				UserMessages.ShowWarningMessage(string.Format("Please enter (or choose) a valid {0}.", this.NameOfAnItem));
				comboBoxList.Focus();
				return;
			}

			this.DialogResult = true;
			this.Close();
		}
	}
}
