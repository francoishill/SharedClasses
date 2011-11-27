using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//using dragonz.actb.core;
//using dragonz.actb.provider;
using ICommandWithHandler = TempNewCommandsManagerClass.ICommandWithHandler;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for InlineCommandsUserControlWPF.xaml
	/// </summary>
	public partial class InlineCommandsUserControlWPF : UserControl
	{
		public TextFeedbackEventHandler textFeedbackEvent;
		//AutoCompleteManager autcompleteManager;
		//AutocompleteProvider autocompleteProvider;

		public InlineCommandsUserControlWPF()
		{
			InitializeComponent();
		}

		private bool textFeedbackEventInitialized = false;
		public void InitializeTreeViewNodes()
		{
			if (!textFeedbackEventInitialized)
			{
				textFeedbackEvent += (snder, evtargs) =>
				{
					textBox_Messages.Text += (textBox_Messages.Text.Length > 0 ? Environment.NewLine : "")
						+ evtargs.FeedbackText;
				};
				textFeedbackEventInitialized = true;
			}

			label_ArgumentsExample.Content = "";
			treeView_CommandList.Items.Clear();
			List<ICommandWithHandler> tmplist = TempNewCommandsManagerClass.ListOfInitializedCommandInterfaces;
			textBox_CommandLine.ItemsSource = new ObservableCollection<string>();
			foreach (ICommandWithHandler comm in tmplist)
				treeView_CommandList.Items.Add(comm);
			ResetAutocompleteToCommandNamesList();
			HideEmbeddedButton();
			GetActualTextBoxOfAutocompleteControl().Focus();
		}

		private void ResetAutocompleteToCommandNamesList()
		{
			textBox_CommandLine.ItemsSource = new ObservableCollection<string>();
			List<ICommandWithHandler> tmplist = TempNewCommandsManagerClass.ListOfInitializedCommandInterfaces;
			foreach (ICommandWithHandler comm in tmplist)
				(textBox_CommandLine.ItemsSource as ObservableCollection<string>).Add(comm.CommandName);
		}

		private TextBox GetActualTextBoxOfAutocompleteControl()
		{
			TextBox actualTextBoxOfAutocompleteControl = (TextBox)textBox_CommandLine.Template.FindName("Text", textBox_CommandLine);
			if (actualTextBoxOfAutocompleteControl == null)
				UserMessages.ShowWarningMessage("Could not find Text in template");
			return actualTextBoxOfAutocompleteControl;
		}

		enum OneOrTwo { One, Two };
		private TextBlock _GetEmbeddedButtonTextblock(OneOrTwo oneOrTwo)
		{
			TextBox actualTextBoxOfAutocompleteControl = GetActualTextBoxOfAutocompleteControl();
			if (actualTextBoxOfAutocompleteControl == null)
				return null;

			TextBlock returnTextBlock = (TextBlock)actualTextBoxOfAutocompleteControl.Template.FindName(oneOrTwo == OneOrTwo.One ? "EmbeddedButtonTextBlock" : "EmbeddedButtonTextBlock2", actualTextBoxOfAutocompleteControl);
			if (returnTextBlock == null)
				UserMessages.ShowWarningMessage("Could not find " + (oneOrTwo == OneOrTwo.One ? "EmbeddedButtonTextBlock" : "EmbeddedButtonTextBlock2") + " in template");
			return returnTextBlock;
		}

		private TextBlock GetEmbeddedButtonTextBlock()
		{
			return _GetEmbeddedButtonTextblock(OneOrTwo.One);
		}

		private TextBlock GetEmbeddedButtonTextBlock2()
		{
			return _GetEmbeddedButtonTextblock(OneOrTwo.Two);
		}

		private Border _GetEmbeddedButton(OneOrTwo oneOrTwo)
		{
			TextBox actualTextBoxOfAutocompleteControl = GetActualTextBoxOfAutocompleteControl();
			if (actualTextBoxOfAutocompleteControl == null)
				return null;

			Border returnBorder = (Border)actualTextBoxOfAutocompleteControl.Template.FindName(oneOrTwo == OneOrTwo.One ? "EmbeddedButton" : "EmbeddedButton2", actualTextBoxOfAutocompleteControl);
			if (returnBorder == null)
				UserMessages.ShowWarningMessage("Could not find " + (oneOrTwo == OneOrTwo.One ? "EmbeddedButton" : "EmbeddedButton2") + " in template");
			return returnBorder;
		}

		private Border GetEmbeddedButton()
		{
			return _GetEmbeddedButton(OneOrTwo.One);
		}

		private Border GetEmbeddedButton2()
		{
			return _GetEmbeddedButton(OneOrTwo.Two);
		}

		private void treeView_CommandList_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (e.NewValue != null && e.NewValue is ICommandWithHandler)
			{
				ICommandWithHandler command = e.NewValue as ICommandWithHandler;
				label_ArgumentsExample.Content = label_ArgumentsExample.ToolTip = command.ArgumentsExample.Replace("\n", "  ");

				//TextBlock tmpTextBlock = (TextBlock)textBox_CommandLine.Template.FindName("EmbeddedButtonTextBlock", textBox_CommandLine);
				TextBlock tmpTextBlock = GetEmbeddedButtonTextBlock();
				tmpTextBlock.Text = command.DisplayName;
				tmpTextBlock.ToolTip = command.Description + Environment.NewLine + "For example:" + Environment.NewLine + command.ArgumentsExample;
				//Border tmpBorder = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton", textBox_CommandLine);
				Border tmpBorder = GetEmbeddedButton();
				tmpBorder.Tag = command;
				if (tmpBorder.Visibility != System.Windows.Visibility.Visible)
					tmpBorder.Visibility = System.Windows.Visibility.Visible;

				TextBox actualTextbox = GetActualTextBoxOfAutocompleteControl();
				actualTextbox.Focus();
				actualTextbox.Text = "";
				//textBox_CommandLine.Focus();
				//textBox_CommandLine.Text = "";

				//autoCompleteTextbox.ItemsSource = command.GetPredefinedArgumentsList;
				textBox_CommandLine.ItemsSource = command.GetPredefinedArgumentsList;

				//if (autcompleteManager == null)
				//{
				//	autcompleteManager = new AutoCompleteManager(textBox_CommandLine);
				//	autocompleteProvider = new AutocompleteProvider();
				//	autcompleteManager.DataProvider = autocompleteProvider;
				//}
				//autocompleteProvider.activeCommand = command;
			}
			else if (e.NewValue == null)
			{
				label_ArgumentsExample.Content = "";

				//TextBlock tmpTextBlock = (TextBlock)textBox_CommandLine.Template.FindName("EmbeddedButtonTextBlock", textBox_CommandLine);
				TextBlock tmpTextBlock = GetEmbeddedButtonTextBlock();
				tmpTextBlock.Text = "";

				//Border tmpBorder = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton", textBox_CommandLine);
				Border tmpBorder = GetEmbeddedButton();
				tmpBorder.Tag = null;
				if (tmpBorder.Visibility != System.Windows.Visibility.Collapsed)
					tmpBorder.Visibility = System.Windows.Visibility.Collapsed;

				//Border tmpBorder2 = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton2", textBox_CommandLine);
				Border tmpBorder2 = GetEmbeddedButton2();
				if (tmpBorder2.Visibility != System.Windows.Visibility.Collapsed)
					tmpBorder2.Visibility = System.Windows.Visibility.Collapsed;

				//textBox_CommandLine.Focus();
				GetActualTextBoxOfAutocompleteControl().Focus();
				//autocompleteProvider.activeCommand = null;
			}
		}

		private void HideEmbeddedButton()
		{
			//Border tmpBorder = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton", textBox_CommandLine);
			Border tmpBorder = GetEmbeddedButton();
			tmpBorder.Visibility = System.Windows.Visibility.Collapsed;

			//Border tmpBorder2 = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton2", textBox_CommandLine);
			Border tmpBorder2 = GetEmbeddedButton2();
			tmpBorder2.Visibility = System.Windows.Visibility.Collapsed;
		}

		private bool CommandNameExistOfTextboxText(out string CommandName)
		{
			foreach (ICommandWithHandler comm in treeView_CommandList.Items)
				if (textBox_CommandLine.Text.Equals(comm.CommandName, StringComparison.InvariantCultureIgnoreCase))
				{
					CommandName = comm.DisplayName;
					return true;
				}
			CommandName = "";
			return false;
		}

		static void ClearSelection(TreeView input)
		{
			var selected = input.SelectedItem;
			if (selected == null)
				return;

			TreeViewItem tvi = input.ItemContainerGenerator.ContainerFromItem(selected) as TreeViewItem;
			if (tvi != null)
				tvi.IsSelected = false;
		}

		static public bool SetSelectedItem(
		TreeView treeView, object item)
		{
			return SetSelected(treeView, item);
		}

		private static bool SetSelected(ItemsControl parent,
				object child)
		{
			if (parent == null || child == null)
				return false;

			TreeViewItem childNode = parent.ItemContainerGenerator
					.ContainerFromItem(child) as TreeViewItem;

			if (childNode != null)
			{
				childNode.Focus();
				return childNode.IsSelected = true;
			}

			if (parent.Items.Count > 0)
			{
				foreach (object childItem in parent.Items)
				{
					ItemsControl childControl = parent
							.ItemContainerGenerator
							.ContainerFromItem(childItem)
							as ItemsControl;

					if (SetSelected(childControl, child))
					{
						return true;
					}
				}
			}

			return false;
		}

		private void textBox_CommandLine_TextChanged(object sender, TextChangedEventArgs e)
		{
			string tmpCommandName;
			if (CommandNameExistOfTextboxText(out tmpCommandName))
				label_ArgumentsExample.Content = "Press TAB to initiate mode for " + tmpCommandName;
		}

		private void textBox_CommandLine_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				ICommandWithHandler activeCommand;
				if (GetActiveCommand(out activeCommand))
				{
					TextBox actualTextBox = GetActualTextBoxOfAutocompleteControl();
					actualTextBox.SelectionLength = 0;
					actualTextBox.SelectionStart = actualTextBox.Text.Length;
					//Border tmpBorder2 = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton2", textBox_CommandLine);
					Border tmpBorder2 = GetEmbeddedButton2();
					//TextBlock tmpTextBlock2 = (TextBlock)textBox_CommandLine.Template.FindName("EmbeddedButtonTextBlock2", textBox_CommandLine);
					TextBlock tmpTextBlock2 = GetEmbeddedButtonTextBlock2();
					if (TempNewCommandsManagerClass.PerformCommandFromString(
						activeCommand,
						textFeedbackEvent,
						tmpBorder2.Visibility == System.Windows.Visibility.Collapsed
						? textBox_CommandLine.Text
						: tmpTextBlock2.Text))
						textBox_CommandLine.Text = "";//.Clear();
				}
			}
			else if (e.Key == Key.Tab)
			{
				foreach (ICommandWithHandler comm in treeView_CommandList.Items)//tmplist)
					if (textBox_CommandLine.Text.Equals(comm.CommandName, StringComparison.InvariantCultureIgnoreCase))
					{
						e.Handled = true;
						SetSelectedItem(treeView_CommandList, comm);
						break;
					}
			}
			else if (e.Key == Key.Escape)
			{
				if (textBox_CommandLine.Text.Length > 0) textBox_CommandLine.Text = "";
				else
				{
					ClearSelection(treeView_CommandList);
					ResetAutocompleteToCommandNamesList();
				}
			}
			else if (e.Key == Key.Down)
			{
				ICommandWithHandler activeCommand;
				if (GetActiveCommand(out activeCommand))
				{
					ObservableCollection<string> predefinedList = activeCommand.GetPredefinedArgumentsList;
					if (predefinedList != null && predefinedList.Count > 0)
					{
						//TextBlock tmpTextBlock2 = (TextBlock)textBox_CommandLine.Template.FindName("EmbeddedButtonTextBlock2", textBox_CommandLine);
						TextBlock tmpTextBlock2 = GetEmbeddedButtonTextBlock2();
						int NextIndex = predefinedList.IndexOf(tmpTextBlock2.Text) + 1;
						if (NextIndex >= predefinedList.Count)
							NextIndex = 0;
						tmpTextBlock2.Text = predefinedList[NextIndex];

						//Border tmpBorder2 = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton2", textBox_CommandLine);
						Border tmpBorder2 = GetEmbeddedButton2();
						if (tmpBorder2.Visibility != System.Windows.Visibility.Visible)
							tmpBorder2.Visibility = System.Windows.Visibility.Visible;
					}
				}
			}
			else if (e.Key == Key.Up)
			{
				ICommandWithHandler activeCommand;
				if (GetActiveCommand(out activeCommand))
				{
					ObservableCollection<string> predefinedList = activeCommand.GetPredefinedArgumentsList;
					if (predefinedList != null && predefinedList.Count > 0)
					{
						//TextBlock tmpTextBlock2 = (TextBlock)textBox_CommandLine.Template.FindName("EmbeddedButtonTextBlock2", textBox_CommandLine);
						TextBlock tmpTextBlock2 = GetEmbeddedButtonTextBlock2();
						int PreviousIndex = predefinedList.IndexOf(tmpTextBlock2.Text) - 1;
						if (PreviousIndex < 0)
							PreviousIndex = predefinedList.Count - 1;
						tmpTextBlock2.Text = predefinedList[PreviousIndex];

						//Border tmpBorder2 = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton2", textBox_CommandLine);
						Border tmpBorder2 = GetEmbeddedButton2();
						if (tmpBorder2.Visibility != System.Windows.Visibility.Visible)
							tmpBorder2.Visibility = System.Windows.Visibility.Visible;
					}
				}
			}
		}

		private bool GetActiveCommand(out ICommandWithHandler activeCommand)
		{
			if (treeView_CommandList.SelectedItem != null && treeView_CommandList.SelectedItem is ICommandWithHandler)
			{
				activeCommand = treeView_CommandList.SelectedItem as ICommandWithHandler;;
				return true;
			}
			activeCommand = null;
			return false;
		}

		private void textBox_CommandLine_TextChanged_1(object sender, RoutedEventArgs e)
		{
			string tmpCommandName;
			if (CommandNameExistOfTextboxText(out tmpCommandName))
				label_ArgumentsExample.Content = "Press TAB to initiate mode for " + tmpCommandName;
		}

		//private class AutocompleteProvider : IAutoCompleteDataProvider
		//{
		//	public ICommandWithHandler activeCommand;
		//	public IEnumerable<string> GetItems(string textPattern)
		//	{
		//		if (activeCommand != null)
		//		return activeCommand.GetPredefinedArgumentsList;
		//		return new List<string>();
		//	}
		//}
	}
}