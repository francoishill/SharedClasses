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
using ICommandWithHandler = TempNewCommandsManagerClass.ICommandWithHandler;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for InlineCommandsUserControlWPF.xaml
	/// </summary>
	public partial class InlineCommandsUserControlWPF : UserControl
	{
		public TextFeedbackEventHandler textFeedbackEvent;

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
					//ThreadingInterop.UpdateGuiFromThread(this, () =>
					//{
					textBox_Messages.Text += (textBox_Messages.Text.Length > 0 ? Environment.NewLine : "")
						+ evtargs.FeedbackText;
					//}
					//);
				};
				textFeedbackEventInitialized = true;
			}

			label_ArgumentsExample.Content = "";
			treeView_CommandList.Items.Clear();
			List<ICommandWithHandler> tmplist = TempNewCommandsManagerClass.ListOfInitializedCommandInterfaces;
			foreach (ICommandWithHandler comm in tmplist)
			{
				treeView_CommandList.Items.Add(comm);
				//listView1.Items.Add(comm);
			}
			HideEmbeddedButton();
			textBox_CommandLine.Focus();
		}

		//private void textBox_CommandLine_KeyUp(object sender, KeyEventArgs e)
		//{
		//	if (e.Key == Key.Enter)
		//	{
		//		e.Handled = true;
		//		if (treeView_CommandList.SelectedItem != null && treeView_CommandList.SelectedItem is ICommandWithHandler)
		//		{
		//			ICommandWithHandler comm = treeView_CommandList.SelectedItem as ICommandWithHandler;
		//			if (TempNewCommandsManagerClass.PerformCommandFromString(
		//				comm,
		//				textFeedbackEvent,
		//				textBox_CommandLine.Text))
		//				textBox_CommandLine.Clear();
		//		}
		//	}
		//}

		private void treeView_CommandList_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (e.NewValue != null && e.NewValue is ICommandWithHandler)
			{
				ICommandWithHandler command = e.NewValue as ICommandWithHandler;
				label_ArgumentsExample.Content = label_ArgumentsExample.ToolTip = command.ArgumentsExample.Replace("\n", "  ");

				TextBlock tmpTextBlock = (TextBlock)textBox_CommandLine.Template.FindName("EmbeddedButtonTextBlock", textBox_CommandLine);
				tmpTextBlock.Text = command.DisplayName;
				tmpTextBlock.ToolTip = command.Description + Environment.NewLine + "For example:" + Environment.NewLine + command.ArgumentsExample;
				Border tmpBorder = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton", textBox_CommandLine);
				tmpBorder.Tag = command;
				if (tmpBorder.Visibility != System.Windows.Visibility.Visible)
					tmpBorder.Visibility = System.Windows.Visibility.Visible;

				//ObservableCollection<string> predefinedList = command.GetPredefinedArgumentsList;
				//if (predefinedList != null && predefinedList.Count > 0)
				//{
				//	TextBlock tmpTextBlock2 = (TextBlock)textBox_CommandLine.Template.FindName("EmbeddedButtonTextBlock2", textBox_CommandLine);
				//	tmpTextBlock2.Text = predefinedList[0];
				//	tmpTextBlock2.ToolTip = string.Join(Environment.NewLine, command.GetPredefinedArgumentsList);
				//	Border tmpBorder2 = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton2", textBox_CommandLine);
				//	if (tmpBorder2.Visibility != System.Windows.Visibility.Visible)
				//		tmpBorder2.Visibility = System.Windows.Visibility.Visible;
				//}
				//else
				//{
				//	Border tmpBorder2 = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton2", textBox_CommandLine);
				//	if (tmpBorder2.Visibility != System.Windows.Visibility.Collapsed)
				//		tmpBorder2.Visibility = System.Windows.Visibility.Collapsed;
				//}

				textBox_CommandLine.Focus();
				textBox_CommandLine.Text = "";

				//listBox_AutoCompleteArguments.ItemsSource = command.GetPredefinedArgumentsList;
			}
			else if (e.NewValue == null)
			{
				label_ArgumentsExample.Content = "";

				TextBlock tmpTextBlock = (TextBlock)textBox_CommandLine.Template.FindName("EmbeddedButtonTextBlock", textBox_CommandLine);
				tmpTextBlock.Text = "";

				Border tmpBorder = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton", textBox_CommandLine);
				tmpBorder.Tag = null;
				if (tmpBorder.Visibility != System.Windows.Visibility.Collapsed)
					tmpBorder.Visibility = System.Windows.Visibility.Collapsed;

				Border tmpBorder2 = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton2", textBox_CommandLine);
				if (tmpBorder2.Visibility != System.Windows.Visibility.Collapsed)
					tmpBorder2.Visibility = System.Windows.Visibility.Collapsed;

				textBox_CommandLine.Focus();
			}
		}

		//private void textBox_CommandLine_KeyDown(object sender, KeyEventArgs e)
		//{
		//	if (e.Key == Key.Enter)
		//	{
		//		e.Handled = true;
		//		if (treeView_CommandList.SelectedItem != null && treeView_CommandList.SelectedItem is ICommandWithHandler)
		//		{
		//			ICommandWithHandler comm = treeView_CommandList.SelectedItem as ICommandWithHandler;
		//			if (TempNewCommandsManagerClass.PerformCommandFromString(
		//				comm,
		//				textFeedbackEvent,
		//				textBox_CommandLine.Text))
		//				textBox_CommandLine.Clear();
		//		}
		//	}
		//	if (e.Key == Key.Tab)
		//	{
		//		//List<ICommandWithHandler> tmplist = TempNewCommandsManagerClass.ListOfInitializedCommandInterfaces;
		//		foreach (ICommandWithHandler comm in treeView_CommandList.Items)//tmplist)
		//			if (textBox_CommandLine.Text.Equals(comm.CommandName, StringComparison.InvariantCultureIgnoreCase))
		//			{
		//				e.Handled = true;
		//				//(treeView_CommandList.Items[treeView_CommandList.Items.IndexOf(comm)] as TreeViewItem).IsSelected = true;
		//				//SetSelectedItem(treeView_CommandList, comm);
		//				////treeView_CommandList.sel.SelectedItem = comm;
		//				//textBox_CommandLine.Focus();
		//				//textBox_CommandLine.Text = "";
		//				//SetActiveCommand_NullForNone(comm);
		//				SetSelectedItem(treeView_CommandList, comm);
		//				break;
		//			}
		//	}
		//	if (e.Key == Key.Escape)
		//	{
		//		//SetActiveCommand_NullForNone(null);
		//		if (textBox_CommandLine.Text.Length > 0) textBox_CommandLine.Text = "";
		//		else ClearSelection(treeView_CommandList);
		//	}
		//	if (e.Key == Key.Down)
		//	{
		//		if (treeView_CommandList.SelectedItem != null && treeView_CommandList.SelectedItem is ICommandWithHandler)
		//		{
		//			ICommandWithHandler comm = treeView_CommandList.SelectedItem as ICommandWithHandler;
		//			ObservableCollection<string> predefinedList = comm.GetPredefinedArgumentsList;
		//			TextBlock tmpTextBlock2 = (TextBlock)textBox_CommandLine.Template.FindName("EmbeddedButtonTextBlock2", textBox_CommandLine);
		//			int NextIndex = predefinedList.IndexOf(tmpTextBlock2.Text) + 1;
		//			if (NextIndex >= predefinedList.Count)
		//				NextIndex = 0;
		//			tmpTextBlock2.Text = predefinedList[NextIndex];
		//		}
		//	}
		//}

		//private void SetActiveCommand_NullForNone(ICommandWithHandler command)
		//{
		//	if (command != null) SetSelectedItem(treeView_CommandList, command);
		//	else ClearSelection(treeView_CommandList);
		//	//treeView_CommandList.sel.SelectedItem = comm;
		//	//textBox_CommandLine.Focus();
		//	//if (command != null) textBox_CommandLine.Text = "";

		//	//TextBlock tmpTextBlock = (TextBlock)textBox_CommandLine.Template.FindName("EmbeddedButtonTextBlock", textBox_CommandLine);
		//	//tmpTextBlock.Text = command != null ? command.CommandName : "";

		//	//Border tmpBorder = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton", textBox_CommandLine);
		//	//tmpBorder.Tag = command;
		//	//if (command != null && tmpBorder.Visibility != System.Windows.Visibility.Visible)
		//	//	tmpBorder.Visibility = System.Windows.Visibility.Visible;
		//	//else if (command == null && tmpBorder.Visibility != System.Windows.Visibility.Collapsed)
		//	//	tmpBorder.Visibility = System.Windows.Visibility.Collapsed;
		//}

		private void HideEmbeddedButton()
		{
			Border tmpBorder = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton", textBox_CommandLine);
			tmpBorder.Visibility = System.Windows.Visibility.Collapsed;

			Border tmpBorder2 = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton2", textBox_CommandLine);
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
			// this should be some container that you put in
			// possibly the actual treeviewitem, not sure on that though
			var selected = input.SelectedItem;
			if (selected == null)
				return;

			// in my case this works perfectly
			TreeViewItem tvi = input.ItemContainerGenerator.ContainerFromItem(selected) as TreeViewItem;
			if (tvi == null)
			{
				// it must be a child, heres a hack fix
				// my nodes are inherited from TreeViewItemViewModel by Josh Smith
				//var child = selected as ICommandWithHandler;
				//if (child == null)
				//	return;
				//child.IsSelected = false;

			}
			else
				tvi.IsSelected = false;
		}

		static public bool SetSelectedItem(
		TreeView treeView, object item)
		{
			return SetSelected(treeView, item);
		}

		static private bool SetSelected(ItemsControl parent,
				object child)
		{

			if (parent == null || child == null)
			{
				return false;
			}

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
				if (treeView_CommandList.SelectedItem != null && treeView_CommandList.SelectedItem is ICommandWithHandler)
				{
					ICommandWithHandler comm = treeView_CommandList.SelectedItem as ICommandWithHandler;
					Border tmpBorder2 = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton2", textBox_CommandLine);
					TextBlock tmpTextBlock2 = (TextBlock)textBox_CommandLine.Template.FindName("EmbeddedButtonTextBlock2", textBox_CommandLine);
					//if (tmpBorder2.Visibility == System.Windows.Visibility.Collapsed)
					//{
						if (TempNewCommandsManagerClass.PerformCommandFromString(
							comm,
							textFeedbackEvent,
							tmpBorder2.Visibility == System.Windows.Visibility.Collapsed
							? textBox_CommandLine.Text
							: tmpTextBlock2.Text))
							textBox_CommandLine.Clear();
					//}
					//else
					//{
					//	//System.Windows.Forms.MessageBox.Show(tmpTextBlock2.Text);
					//	TempNewCommandsManagerClass.PerformCommandFromString(
					//		comm,
					//		textFeedbackEvent,
					//		tmpTextBlock2.Text);
					//}
				}
			}
			else if (e.Key == Key.Tab)
			{
				//List<ICommandWithHandler> tmplist = TempNewCommandsManagerClass.ListOfInitializedCommandInterfaces;
				foreach (ICommandWithHandler comm in treeView_CommandList.Items)//tmplist)
					if (textBox_CommandLine.Text.Equals(comm.CommandName, StringComparison.InvariantCultureIgnoreCase))
					{
						e.Handled = true;
						//(treeView_CommandList.Items[treeView_CommandList.Items.IndexOf(comm)] as TreeViewItem).IsSelected = true;
						//SetSelectedItem(treeView_CommandList, comm);
						////treeView_CommandList.sel.SelectedItem = comm;
						//textBox_CommandLine.Focus();
						//textBox_CommandLine.Text = "";
						//SetActiveCommand_NullForNone(comm);
						SetSelectedItem(treeView_CommandList, comm);
						break;
					}
			}
			else if (e.Key == Key.Escape)
			{
				//SetActiveCommand_NullForNone(null);
				if (textBox_CommandLine.Text.Length > 0) textBox_CommandLine.Text = "";
				else ClearSelection(treeView_CommandList);
			}
			else if (e.Key == Key.Down)
			{
				if (treeView_CommandList.SelectedItem != null && treeView_CommandList.SelectedItem is ICommandWithHandler)
				{
					ICommandWithHandler comm = treeView_CommandList.SelectedItem as ICommandWithHandler;
					ObservableCollection<string> predefinedList = comm.GetPredefinedArgumentsList;
					if (predefinedList != null && predefinedList.Count > 0)
					{
						TextBlock tmpTextBlock2 = (TextBlock)textBox_CommandLine.Template.FindName("EmbeddedButtonTextBlock2", textBox_CommandLine);
						int NextIndex = predefinedList.IndexOf(tmpTextBlock2.Text) + 1;
						if (NextIndex >= predefinedList.Count)
							NextIndex = 0;
						tmpTextBlock2.Text = predefinedList[NextIndex];

						Border tmpBorder2 = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton2", textBox_CommandLine);
						if (tmpBorder2.Visibility != System.Windows.Visibility.Visible)
							tmpBorder2.Visibility = System.Windows.Visibility.Visible;
					}
				}
			}
			else if (e.Key == Key.Up)
			{
				if (treeView_CommandList.SelectedItem != null && treeView_CommandList.SelectedItem is ICommandWithHandler)
				{
					ICommandWithHandler comm = treeView_CommandList.SelectedItem as ICommandWithHandler;
					ObservableCollection<string> predefinedList = comm.GetPredefinedArgumentsList;
					if (predefinedList != null && predefinedList.Count > 0)
					{
						TextBlock tmpTextBlock2 = (TextBlock)textBox_CommandLine.Template.FindName("EmbeddedButtonTextBlock2", textBox_CommandLine);
						int PreviousIndex = predefinedList.IndexOf(tmpTextBlock2.Text) - 1;
						if (PreviousIndex < 0)
							PreviousIndex = predefinedList.Count - 1;
						tmpTextBlock2.Text = predefinedList[PreviousIndex];

						Border tmpBorder2 = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton2", textBox_CommandLine);
						if (tmpBorder2.Visibility != System.Windows.Visibility.Visible)
							tmpBorder2.Visibility = System.Windows.Visibility.Visible;
					}
				}
			}
		}
	}
}