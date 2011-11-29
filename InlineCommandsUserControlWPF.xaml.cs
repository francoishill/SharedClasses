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
using System.Windows.Threading;
using InlineCommands;
//using dragonz.actb.core;
//using dragonz.actb.provider;
using ICommandWithHandler = InlineCommands.TempNewCommandsManagerClass.ICommandWithHandler;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for InlineCommandsUserControlWPF.xaml
	/// </summary>
	public partial class InlineCommandsUserControlWPF : UserControl
	{
		public TextFeedbackEventHandler textFeedbackEvent;
		public ProgressChangedEventHandler progressChangedEvent;
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
					Dispatcher.BeginInvoke(DispatcherPriority.Background,
					(Action)delegate
					{
						textBox_Messages.Text += (textBox_Messages.Text.Length > 0 ? Environment.NewLine : "")
							+ evtargs.FeedbackText;
					});
				};
				progressChangedEvent += (snder, evtargs) =>
				{
					Dispatcher.BeginInvoke(DispatcherPriority.Background,
					(Action)delegate
					{
						textBox_Messages.Text += (textBox_Messages.Text.Length > 0 ? Environment.NewLine : "")
							+ evtargs.CurrentValue + "/" + evtargs.MaximumValue;
					});
				};
				textFeedbackEventInitialized = true;
			}

			label_ArgumentsExample.Content = "";
			treeView_CommandList.Items.Clear();
			List<ICommandWithHandler> tmplist = TempNewCommandsManagerClass.ListOfInitializedCommandInterfaces;
			textBox_CommandLine.ItemsSource = new ObservableCollection<string>();
			foreach (ICommandWithHandler comm in tmplist)
				treeView_CommandList.Items.Add(comm);

			ControlTemplate ct = this.FindResource("TextBoxBaseControlTemplate") as ControlTemplate;
			GetActualTextBoxOfAutocompleteControl().Template = ct;
			GetActualTextBoxOfAutocompleteControl().ApplyTemplate();
			GetActualTextBoxOfAutocompleteControl().HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
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

		private TextBlock GetEmbeddedButtonTextBlock()
		{
			TextBox actualTextBoxOfAutocompleteControl = GetActualTextBoxOfAutocompleteControl();
			if (actualTextBoxOfAutocompleteControl == null)
				return null;

			TextBlock returnTextBlock = (TextBlock)actualTextBoxOfAutocompleteControl.Template.FindName("EmbeddedButtonTextBlock", actualTextBoxOfAutocompleteControl);
			if (returnTextBlock == null)
				UserMessages.ShowWarningMessage("Could not find EmbeddedButtonTextBlock in template");
			return returnTextBlock;
		}

		private Border GetEmbeddedButton()
		{
			TextBox actualTextBoxOfAutocompleteControl = GetActualTextBoxOfAutocompleteControl();
			if (actualTextBoxOfAutocompleteControl == null)
				return null;

			Border returnBorder = (Border)actualTextBoxOfAutocompleteControl.Template.FindName("EmbeddedButton", actualTextBoxOfAutocompleteControl);
			if (returnBorder == null)
				UserMessages.ShowWarningMessage("Could not find EmbeddedButton in template");
			return returnBorder;
		}

		private ListBox GetEmbeddedListbox()
		{
			TextBox actualTextBoxOfAutocompleteControl = GetActualTextBoxOfAutocompleteControl();
			if (actualTextBoxOfAutocompleteControl == null)
				return null;

			ListBox returnListBox = (ListBox)actualTextBoxOfAutocompleteControl.Template.FindName("EmbeddedListbox", actualTextBoxOfAutocompleteControl);
			if (returnListBox == null)
				UserMessages.ShowWarningMessage("Could not find EmbeddedListbox in template");
			return returnListBox;
		}

		private void treeView_CommandList_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (e.NewValue != null && e.NewValue is ICommandWithHandler)
			{
				Dispatcher.BeginInvoke(DispatcherPriority.Background,
				(Action)delegate
				{
					e.Handled = true;
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

					textBox_CommandLine.ItemsSource = null;
					textBox_CommandLine.UpdateLayout();
					//autoCompleteTextbox.ItemsSource = command.GetPredefinedArgumentsList;
					//textBox_CommandLine.ItemsSource = command.GetPredefinedArgumentsList(0, true);
					//textBox_CommandLine.UpdateLayout();

					if (command.CurrentArgumentCount == 0)
						command.ClearAndAddAllBlankArguments();
					GetEmbeddedListbox().ItemsSource = command.CurrentArgumentsPair;
					this.UpdateLayout();
					if (command.CurrentArgumentCount > 0)
					{
						ListBox tmpListbox = GetEmbeddedListbox();
						tmpListbox.SelectedIndex = 0;
						GetActualTextboxOfArgument(tmpListbox.SelectedItem).Focus();
						textBox_CommandLine.IsDropDownOpen = false;
						//GetAutocompleteBoxOfArgument(tmpListbox.SelectedItem).IsDropDownOpen = true;
					}

					//textBox_CommandLine.ItemsSource = command.GetPredefinedArgumentsList(0, true);

					//textBox_CommandLine.Focus();
					ListBox tmpListbox2 = GetEmbeddedListbox();
					for (int i = 0; i < tmpListbox2.Items.Count; i++)
					{
						GetAutocompleteBoxOfArgument(tmpListbox2.Items[i]).ItemsSource = command.GetPredefinedArgumentsList(i, true);
						//GetAutocompleteBoxOfArgument(tmpListbox.Items[i]).IsDropDownOpen = true;
					}
					if (tmpListbox2.Items.Count > 0)
					{
						//GetAutocompleteBoxOfArgument(tmpListbox2.Items[0]).Focus();
						//textBox_CommandLine.Focus();
						//GetEmbeddedListbox().Focus();
						//GetActualTextBoxOfAutocompleteControl().Focus();
						GetActualTextboxOfArgument(tmpListbox2.Items[0]).Focus();
						//GetAutocompleteBoxOfArgument(tmpListbox2.Items[0]).Focus();
						GetAutocompleteBoxOfArgument(tmpListbox2.Items[0]).IsDropDownOpen = true;
					}

					//if (autcompleteManager == null)
					//{
					//	autcompleteManager = new AutoCompleteManager(textBox_CommandLine);
					//	autocompleteProvider = new AutocompleteProvider();
					//	autcompleteManager.DataProvider = autocompleteProvider;
					//}
					//autocompleteProvider.activeCommand = command;
				});
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
				//Border tmpBorder2 = GetEmbeddedButton2();
				//if (tmpBorder2.Visibility != System.Windows.Visibility.Collapsed)
				//	tmpBorder2.Visibility = System.Windows.Visibility.Collapsed;

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
			//Border tmpBorder2 = GetEmbeddedButton2();
			//tmpBorder2.Visibility = System.Windows.Visibility.Collapsed;
		}

		private bool CommandNameExistOfTextboxText(out string CommandName)
		{
			foreach (InlineCommands.TempNewCommandsManagerClass.ICommandWithHandler comm in treeView_CommandList.Items)
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

		private AutoCompleteBox GetAutocompleteBoxOfArgument(object listboxItem)
		{
			ListBox lb = GetEmbeddedListbox();
			ListBoxItem lbi = lb.ItemContainerGenerator.ContainerFromItem(listboxItem) as ListBoxItem;
			DataTemplate dataTemplate = lbi.ContentTemplate;
			Border dataTemplateBorder = VisualTreeHelper.GetChild(lbi, 0) as Border;
			ContentPresenter contentPresenter = dataTemplateBorder.Child as ContentPresenter;
			return dataTemplate.FindName("ArgumentText", contentPresenter) as AutoCompleteBox;
		}

		private TextBox GetActualTextboxOfArgument(object listboxItem)
		{
			AutoCompleteBox autoCompleteTextbox = GetAutocompleteBoxOfArgument(listboxItem);
			return autoCompleteTextbox.Template.FindName("Text", autoCompleteTextbox) as TextBox;
		}

		private void textBox_CommandLine_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && !textBox_CommandLine.IsDropDownOpen)// && textBox_CommandLine.Text.Length == 0)
			{
				e.Handled = true;
				InlineCommands.TempNewCommandsManagerClass.ICommandWithHandler activeCommand;
				if (GetActiveCommand(out activeCommand))
				{
					TextBox actualTextBox = GetActualTextBoxOfAutocompleteControl();
					actualTextBox.SelectionLength = 0;
					actualTextBox.SelectionStart = actualTextBox.Text.Length;

					ListBox lb = GetEmbeddedListbox();
					if (lb.Items.Count == 0) return;
					foreach (object lbo in lb.Items)
					{
						GetAutocompleteBoxOfArgument(lbo).IsDropDownOpen = false;
						TextBox t = GetActualTextboxOfArgument(lbo);
						t.SelectionLength = 0;
						t.SelectionStart = t.Text.Length;
						BindingExpression be = t.GetBindingExpression(TextBox.TextProperty);
						be.UpdateSource();
					}

					if (TempNewCommandsManagerClass.PerformCommandFromCurrentArguments(
							activeCommand,
							textFeedbackEvent,
							progressChangedEvent))
						textBox_CommandLine.Text = "";//.Clear();
				}
			}
			else if (e.Key == Key.Tab || (e.Key == Key.Enter && (textBox_CommandLine.IsDropDownOpen || textBox_CommandLine.Text.Length > 0)))
			{
				Border tmpBorder = GetEmbeddedButton();
				if (tmpBorder.Tag == null && textBox_CommandLine.Text.Trim().Length > 0)
				{
					if (InitiateCommandFromTextboxText())
						e.Handled = true;
				}
				else// if (textBox_CommandLine.Text.Trim().Length > 0)
				{
					e.Handled = true;

					ICommandWithHandler comm = tmpBorder.Tag as ICommandWithHandler;
					ListBox lb = GetEmbeddedListbox();
					if (lb.Items.Count == 0) return;
					int currentSelectedIndex = lb.SelectedIndex;
					int newSelectedIndex = currentSelectedIndex;
					if (Keyboard.Modifiers != ModifierKeys.Shift && Keyboard.Modifiers != ModifierKeys.None)
						return;
					if (Keyboard.Modifiers == ModifierKeys.None)
					{
						int nextSelectedIndex = currentSelectedIndex + 1;
						if (nextSelectedIndex >= lb.Items.Count)
							nextSelectedIndex = 0;
						newSelectedIndex = nextSelectedIndex;
					}
					else if (Keyboard.Modifiers == ModifierKeys.Shift)
					{
						int previousSelectedIndex = currentSelectedIndex - 1;
						if (previousSelectedIndex < 0)
							previousSelectedIndex = lb.Items.Count - 1;
						newSelectedIndex = previousSelectedIndex;
					}
					if (newSelectedIndex == -1) return;
					lb.SelectedIndex = newSelectedIndex;
					TextBox textboxOfArgument = GetActualTextboxOfArgument(lb.SelectedItem);
					GetAutocompleteBoxOfArgument(lb.SelectedItem).ItemsSource = comm.GetPredefinedArgumentsList(newSelectedIndex, true);
					textboxOfArgument.Focus();
					//GetAutocompleteBoxOfArgument(lb.SelectedItem).IsDropDownOpen = true;

					//return;
					////ICommandWithHandler comm = tmpBorder.Tag as ICommandWithHandler;
					//TempNewCommandsManagerClass.BoolResultWithErrorMessage boolResultWithErrorMessage =
					//	comm.AddCurrentArgument(textBox_CommandLine.Text.Trim());
					//if (boolResultWithErrorMessage.Success)
					//{
					//	GetActualTextBoxOfAutocompleteControl().Clear();
					//	textBox_CommandLine.ItemsSource = comm.GetPredefinedArgumentsList(comm.CurrentArgumentCount, true);

					//	if (e.Key == Key.Enter)
					//		textBox_CommandLine.RaiseEvent(
					//			new KeyEventArgs(
					//				Keyboard.PrimaryDevice,
					//				PresentationSource.FromVisual(textBox_CommandLine),
					//				0,
					//				Key.Enter)
					//			{
					//				RoutedEvent = TextBox.PreviewKeyDownEvent
					//			});
					//}
					//else
					//	TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, boolResultWithErrorMessage.ErrorMessage);
				}
			}
			else if (e.Key == Key.Escape)
			{
				TextBox actualTextBox = GetActualTextBoxOfAutocompleteControl();
				actualTextBox.SelectionLength = 0;
				actualTextBox.SelectionStart = actualTextBox.Text.Length;

				if (textBox_CommandLine.IsDropDownOpen)
				{
					e.Handled = true;
					textBox_CommandLine.IsDropDownOpen = false;
				}
				else if (actualTextBox.Text.Length > 0)
				{
					e.Handled = true;
					actualTextBox.Text = "";
				}
				//else if (GetEmbeddedButton().Tag != null && (GetEmbeddedButton().Tag as ICommandWithHandler).CurrentArgumentCount > 0)
				//{
				//	ICommandWithHandler tmpCommand = GetEmbeddedButton().Tag as ICommandWithHandler;
				//	tmpCommand.RemoveCurrentArgument(tmpCommand.CurrentArgumentCount - 1);
				//	textBox_CommandLine.ItemsSource = tmpCommand.GetPredefinedArgumentsList(tmpCommand.CurrentArgumentCount, true);
				//}
				else if (treeView_CommandList.SelectedItem != null)
				{
					e.Handled = true;
					ClearCommandSelection();
				}
				else
				{
					e.Handled = true;
					GetTopParent().Hide();
				}
			}
			else if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.Control)
			{
				e.Handled = true;
				textBox_CommandLine.IsDropDownOpen = true;
			}
			//else if (e.Key == Key.Down)
			//{
			//	ICommandWithHandler activeCommand;
			//	if (GetActiveCommand(out activeCommand))
			//	{
			//		ObservableCollection<string> predefinedList = activeCommand.GetPredefinedArgumentsList;
			//		if (predefinedList != null && predefinedList.Count > 0)
			//		{
			//			//TextBlock tmpTextBlock2 = (TextBlock)textBox_CommandLine.Template.FindName("EmbeddedButtonTextBlock2", textBox_CommandLine);
			//			//TextBlock tmpTextBlock2 = GetEmbeddedButtonTextBlock2();
			//			//int NextIndex = predefinedList.IndexOf(tmpTextBlock2.Text) + 1;
			//			//if (NextIndex >= predefinedList.Count)
			//			//	NextIndex = 0;
			//			//tmpTextBlock2.Text = predefinedList[NextIndex];

			//			//Border tmpBorder2 = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton2", textBox_CommandLine);
			//			//Border tmpBorder2 = GetEmbeddedButton2();
			//			//if (tmpBorder2.Visibility != System.Windows.Visibility.Visible)
			//			//	tmpBorder2.Visibility = System.Windows.Visibility.Visible;
			//		}
			//	}
			//}
			//else if (e.Key == Key.Up)
			//{
			//	ICommandWithHandler activeCommand;
			//	if (GetActiveCommand(out activeCommand))
			//	{
			//		ObservableCollection<string> predefinedList = activeCommand.GetPredefinedArgumentsList;
			//		if (predefinedList != null && predefinedList.Count > 0)
			//		{
			//			//TextBlock tmpTextBlock2 = (TextBlock)textBox_CommandLine.Template.FindName("EmbeddedButtonTextBlock2", textBox_CommandLine);
			//			//TextBlock tmpTextBlock2 = GetEmbeddedButtonTextBlock2();
			//			//int PreviousIndex = predefinedList.IndexOf(tmpTextBlock2.Text) - 1;
			//			//if (PreviousIndex < 0)
			//			//	PreviousIndex = predefinedList.Count - 1;
			//			//tmpTextBlock2.Text = predefinedList[PreviousIndex];

			//			//Border tmpBorder2 = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton2", textBox_CommandLine);
			//			//Border tmpBorder2 = GetEmbeddedButton2();
			//			//if (tmpBorder2.Visibility != System.Windows.Visibility.Visible)
			//			//	tmpBorder2.Visibility = System.Windows.Visibility.Visible;
			//		}
			//	}
			//}
		}

		private void ClearCommandSelection()
		{
			ClearSelection(treeView_CommandList);
			GetEmbeddedListbox().ItemsSource = null;
			ResetAutocompleteToCommandNamesList();
		}

		private Window GetTopParent()
		{
			DependencyObject dpParent = this.Parent;
			do
			{
				dpParent = LogicalTreeHelper.GetParent(dpParent);
			} while (dpParent.GetType().BaseType != typeof(Window));
			return dpParent as Window;
		}

		private bool InitiateCommandFromTextboxText()
		{
			foreach (ICommandWithHandler comm in treeView_CommandList.Items)//tmplist)
				if (textBox_CommandLine.Text.Equals(comm.CommandName, StringComparison.InvariantCultureIgnoreCase))
				{
					SetSelectedItem(treeView_CommandList, comm);
					//textBox_CommandLine.ItemsSource = comm.GetPredefinedArgumentsList(0, true);

					//ListBox tmpListbox = GetEmbeddedListbox();
					//for (int i = 0; i < tmpListbox.Items.Count; i++)
					//{
					//	GetAutocompleteBoxOfArgument(tmpListbox.Items[i]).ItemsSource = comm.GetPredefinedArgumentsList(i, true);
					//	//GetAutocompleteBoxOfArgument(tmpListbox.Items[i]).IsDropDownOpen = true;
					//}
					//if (tmpListbox.Items.Count > 0)
					//	GetAutocompleteBoxOfArgument(tmpListbox.Items[0]).IsDropDownOpen = true;
					//textBox_CommandLine.ItemsSource = null;
					return true;
					//break;
				}
			return false;
		}

		private bool GetActiveCommand(out InlineCommands.TempNewCommandsManagerClass.ICommandWithHandler activeCommand)
		{
			if (treeView_CommandList.SelectedItem != null && treeView_CommandList.SelectedItem is ICommandWithHandler)
			{
				activeCommand = treeView_CommandList.SelectedItem as InlineCommands.TempNewCommandsManagerClass.ICommandWithHandler; ;
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

		private void GoButton_Click(object sender, RoutedEventArgs e)
		{
			PerformCurrentCommand();
		}

		private void PerformCurrentCommand()
		{
			textBox_CommandLine.RaiseEvent(
					 new KeyEventArgs(
						 Keyboard.PrimaryDevice,
						 PresentationSource.FromVisual(textBox_CommandLine),
						 0,
						 Key.Enter)
					 {
						 RoutedEvent = TextBox.PreviewKeyDownEvent
					 });
		}

		private void ArgumentText_GotFocus(object sender, RoutedEventArgs e)
		{
			(sender as AutoCompleteBox).IsDropDownOpen = true;
		}

		private void ClearTextboxTextButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			DependencyObject dp = (sender as Border).Parent;
			if (dp != null)
			{
				TextBox actualtextbox = ((dp as DockPanel).Children[1] as TextBox);
				actualtextbox.Text = "";
				actualtextbox.Focus();
			}
		}

		private void EmbeddedButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			PerformCurrentCommand();
		}

		private void EmbeddedButton_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			ClearCommandSelection();
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