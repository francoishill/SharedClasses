using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

		//private FlowDocument messagesFlowDocument = new FlowDocument();
		private bool textFeedbackEventInitialized = false;
		public void InitializeTreeViewNodes()
		{
			if (!textFeedbackEventInitialized)
			{
				//TODO: Should eventually keep track of which messages goes with which command (keep track of Paragraphs).
				textFeedbackEvent += (snder, evtargs) =>
				{
					Dispatcher.BeginInvoke(DispatcherPriority.Background,
					(Action)delegate
					{
						textBox_Messages.Document.Blocks.Add(new Paragraph(new Run(evtargs.FeedbackText))
						{
							Foreground = evtargs.FeedbackType == TextFeedbackType.Error ? Brushes.Red
								: evtargs.FeedbackType == TextFeedbackType.Noteworthy ? Brushes.Purple
								: evtargs.FeedbackType == TextFeedbackType.Success ? Brushes.Green
								: evtargs.FeedbackType == TextFeedbackType.Subtle ? Brushes.LightGray
								: Brushes.Gold,
							TextIndent = -25,
							Margin = new Thickness(25, 0, 0, 0)
						});
						//textBox_Messages.Text += (textBox_Messages.Text.Length > 0 ? Environment.NewLine : "")
						////textBox_Messages.Content += (textBox_Messages.Content.ToString().Length > 0 ? Environment.NewLine : "")
						//	+ evtargs.FeedbackText;
						//textBox_Messages.ScrollToEnd();//.ScrollToBottom();
					});
				};
				progressChangedEvent += (snder, evtargs) =>
				{
					Dispatcher.BeginInvoke(DispatcherPriority.Background,
					(Action)delegate
					{
						textBox_Messages.Document.Blocks.Add(new Paragraph(new Run(evtargs.CurrentValue + "/" + evtargs.MaximumValue)) { Foreground = Brushes.Blue });
						//textBox_Messages.Text += (textBox_Messages.Text.Length > 0 ? Environment.NewLine : "")
						////textBox_Messages.Content += (textBox_Messages.Content.ToString().Length > 0 ? Environment.NewLine : "")
						//	+ evtargs.CurrentValue + "/" + evtargs.MaximumValue;
						//textBox_Messages.ScrollToEnd();//.ScrollToBottom();
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

			//ControlTemplate ct = this.FindResource("TextBoxBaseControlTemplate") as ControlTemplate;
			//GetActualTextBoxOfAutocompleteControl().Template = ct;
			//GetActualTextBoxOfAutocompleteControl().ApplyTemplate();
			GetActualTextBoxOfAutocompleteControl().HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
			ResetAutocompleteToCommandNamesList();
			HideEmbeddedButton();

			if (!GetActualTextBoxOfAutocompleteControl().IsFocused) GetActualTextBoxOfAutocompleteControl().Focus();
		}

		private AutoCompleteBox textBox_CommandLine
		{
			get
			{
				return MainAutoCompleteTextbox.Template.FindName("textBox_CommandLine", MainAutoCompleteTextbox) as AutoCompleteBox;
			}
		}

		private void ResetAutocompleteToCommandNamesList()
		{
			textBox_CommandLine.ItemsSource = new ObservableCollection<string>();
			List<ICommandWithHandler> tmplist = TempNewCommandsManagerClass.ListOfInitializedCommandInterfaces;
			foreach (ICommandWithHandler comm in tmplist)
				(textBox_CommandLine.ItemsSource as ObservableCollection<string>).Add(comm.CommandName);
		}

		private TextBox textBoxWithButtons
		{
			get
			{
				return MainAutoCompleteTextbox.Template.FindName("TextBoxWithButtons", MainAutoCompleteTextbox) as TextBox;
			}
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
			//TextBox actualTextBoxOfAutocompleteControl = GetActualTextBoxOfAutocompleteControl();
			//if (actualTextBoxOfAutocompleteControl == null)
			//	return null;

			//TextBlock returnTextBlock = (TextBlock)actualTextBoxOfAutocompleteControl.Template.FindName("EmbeddedButtonTextBlock", actualTextBoxOfAutocompleteControl);
			TextBlock returnTextBlock = (TextBlock)textBoxWithButtons.Template.FindName("EmbeddedButtonTextBlock", textBoxWithButtons);
			if (returnTextBlock == null)
				UserMessages.ShowWarningMessage("Could not find EmbeddedButtonTextBlock in template");
			return returnTextBlock;
		}

		private Border GetEmbeddedButton()
		{
			//TextBox actualTextBoxOfAutocompleteControl = GetActualTextBoxOfAutocompleteControl();
			//if (actualTextBoxOfAutocompleteControl == null)
			//	return null;

			//Border returnBorder = (Border)actualTextBoxOfAutocompleteControl.Template.FindName("EmbeddedButton", actualTextBoxOfAutocompleteControl);
			Border returnBorder = (Border)textBoxWithButtons.Template.FindName("EmbeddedButton", textBoxWithButtons);
			if (returnBorder == null)
				UserMessages.ShowWarningMessage("Could not find EmbeddedButton in template");
			return returnBorder;
		}

		private ListBox GetEmbeddedListbox()
		{
			//TextBox actualTextBoxOfAutocompleteControl = GetActualTextBoxOfAutocompleteControl();
			//if (actualTextBoxOfAutocompleteControl == null)
			//	return null;

			//ListBox returnListBox = (ListBox)actualTextBoxOfAutocompleteControl.Template.FindName("EmbeddedListbox", actualTextBoxOfAutocompleteControl);
			ListBox returnListBox = (ListBox)textBoxWithButtons.Template.FindName("EmbeddedListbox", textBoxWithButtons);
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
					SetDataContext(command);
					label_ArgumentsExample.Content = label_ArgumentsExample.ToolTip = command.ArgumentsExample.Replace("\n", "  ");

					//TextBlock tmpTextBlock = (TextBlock)textBox_CommandLine.Template.FindName("EmbeddedButtonTextBlock", textBox_CommandLine);
					//TextBlock tmpTextBlock = GetEmbeddedButtonTextBlock();
					//tmpTextBlock.Text = command.DisplayName;
					//tmpTextBlock.ToolTip = command.Description + Environment.NewLine + "For example:" + Environment.NewLine + command.ArgumentsExample;
					//Border tmpBorder = (Border)textBox_CommandLine.Template.FindName("EmbeddedButton", textBox_CommandLine);
					Border tmpBorder = GetEmbeddedButton();
					tmpBorder.Tag = command;
					if (tmpBorder.Visibility != System.Windows.Visibility.Visible)
						tmpBorder.Visibility = System.Windows.Visibility.Visible;

					TextBox actualTextbox = GetActualTextBoxOfAutocompleteControl();
					//					if (!actualTextbox.IsFocused) actualTextbox.Focus();
					actualTextbox.Text = "";
					//textBox_CommandLine.Focus();
					//textBox_CommandLine.Text = "";

					textBox_CommandLine.ItemsSource = null;
					textBox_CommandLine.UpdateLayout();
					//autoCompleteTextbox.ItemsSource = command.GetPredefinedArgumentsList;
					//textBox_CommandLine.ItemsSource = command.GetPredefinedArgumentsList(0, true);
					//textBox_CommandLine.UpdateLayout();

					//if (command.CurrentArgumentCount == 0)
					//	command.Add_AfterClearing_AllBlankArguments();
					//GetEmbeddedListbox().ItemsSource = command.CurrentArgumentsPair;
					this.UpdateLayout();
					if (command.CurrentArgumentCount > 0)
					{
						ListBox tmpListbox = GetEmbeddedListbox();

						//for (int i = 1; i < tmpListbox.Items.Count; i++)
						//	GetAutocompleteBoxOfArgument(tmpListbox.Items[i]).IsDropDownOpen = false;

						tmpListbox.SelectedIndex = 0;
						if (!SelectedTreeViewItemChangedFromDragOver
							&& !GetActualTextboxOfArgument(tmpListbox.SelectedItem).IsFocused)
							GetActualTextboxOfArgument(tmpListbox.SelectedItem).Focus();
						//else
						//	treeView_CommandList.Focus();
						SelectedTreeViewItemChangedFromDragOver = false;
						textBox_CommandLine.IsDropDownOpen = false;
						//GetAutocompleteBoxOfArgument(tmpListbox.SelectedItem).IsDropDownOpen = true;
					}

					//textBox_CommandLine.ItemsSource = command.GetPredefinedArgumentsList(0, true);

					//textBox_CommandLine.Focus();
					ListBox tmpListbox2 = GetEmbeddedListbox();
					for (int i = 0; i < tmpListbox2.Items.Count; i++)
					{
						//GetAutocompleteBoxOfArgument(tmpListbox2.Items[i]).ItemsSource = command.GetPredefinedArgumentsList(i, true);
						//GetAutocompleteBoxOfArgument(tmpListbox.Items[i]).IsDropDownOpen = true;
					}
					if (tmpListbox2.Items.Count > 0)
					{
						//GetAutocompleteBoxOfArgument(tmpListbox2.Items[0]).Focus();
						//textBox_CommandLine.Focus();
						//GetEmbeddedListbox().Focus();
						//GetActualTextBoxOfAutocompleteControl().Focus();
						//						if (!GetActualTextboxOfArgument(tmpListbox2.Items[0]).IsFocused) GetActualTextboxOfArgument(tmpListbox2.Items[0]).Focus();
						//GetAutocompleteBoxOfArgument(tmpListbox2.Items[0]).Focus();
						//GetAutocompleteBoxOfArgument(tmpListbox2.Items[0]).IsDropDownOpen = true;
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
				//tmpTextBlock.Text = "";

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
				if (!GetActualTextBoxOfAutocompleteControl().IsFocused) GetActualTextBoxOfAutocompleteControl().Focus();
				//autocompleteProvider.activeCommand = null;
			}
		}

		private ICommandWithHandler activeCommand = null;
		private void SetDataContext(ICommandWithHandler command = null)
		{
			activeCommand = command;
			textBox_CommandLine.DataContext = activeCommand;
			textBoxWithButtons.DataContext = activeCommand;
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
				if (!childNode.IsFocused) childNode.Focus();
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
			//if (e.Key == Key.Enter && !textBox_CommandLine.IsDropDownOpen)// && textBox_CommandLine.Text.Length == 0)
			//{
			//	e.Handled = true;
			//	InlineCommands.TempNewCommandsManagerClass.ICommandWithHandler activeCommand;
			//	if (GetActiveCommand(out activeCommand))
			//	{
			//		TextBox actualTextBox = GetActualTextBoxOfAutocompleteControl();
			//		actualTextBox.SelectionLength = 0;
			//		actualTextBox.SelectionStart = actualTextBox.Text.Length;

			//		ListBox lb = GetEmbeddedListbox();
			//		if (lb.Items.Count == 0) return;
			//		foreach (object lbo in lb.Items)
			//		{
			//			//GetAutocompleteBoxOfArgument(lbo).IsDropDownOpen = false;
			//			TextBox t = GetActualTextboxOfArgument(lbo);
			//			t.SelectionLength = 0;
			//			t.SelectionStart = t.Text.Length;
			//			BindingExpression be = t.GetBindingExpression(TextBox.TextProperty);
			//			be.UpdateSource();
			//		}

			//		if (TempNewCommandsManagerClass.PerformCommandFromCurrentArguments(
			//				activeCommand,
			//				textFeedbackEvent,
			//				progressChangedEvent))
			//			textBox_CommandLine.Text = "";//.Clear();
			//	}
			//}
			//else if (e.Key == Key.Tab || (e.Key == Key.Enter && (textBox_CommandLine.IsDropDownOpen || textBox_CommandLine.Text.Length > 0)))
			//{
			//	//Border tmpBorder = GetEmbeddedButton();
			//	//ICommandWithHandler comm = GetEmbeddedButton().Tag as ICommandWithHandler;
			//	if (GetEmbeddedButton().Tag == null && textBox_CommandLine.Text.Trim().Length > 0)
			//	{
			//		if (InitiateCommandFromTextboxText())
			//			e.Handled = true;
			//	}
			//	else// if (textBox_CommandLine.Text.Trim().Length > 0)
			//	{
			//		if (Keyboard.Modifiers != ModifierKeys.Shift && Keyboard.Modifiers != ModifierKeys.None)
			//			return;
			//		e.Handled = true;

			//		FocusNextCommandArgument();
			//		//GetAutocompleteBoxOfArgument(lb.SelectedItem).IsDropDownOpen = true;

			//		//return;
			//		////ICommandWithHandler comm = tmpBorder.Tag as ICommandWithHandler;
			//		//TempNewCommandsManagerClass.BoolResultWithErrorMessage boolResultWithErrorMessage =
			//		//	comm.AddCurrentArgument(textBox_CommandLine.Text.Trim());
			//		//if (boolResultWithErrorMessage.Success)
			//		//{
			//		//	GetActualTextBoxOfAutocompleteControl().Clear();
			//		//	textBox_CommandLine.ItemsSource = comm.GetPredefinedArgumentsList(comm.CurrentArgumentCount, true);

			//		//	if (e.Key == Key.Enter)
			//		//		textBox_CommandLine.RaiseEvent(
			//		//			new KeyEventArgs(
			//		//				Keyboard.PrimaryDevice,
			//		//				PresentationSource.FromVisual(textBox_CommandLine),
			//		//				0,
			//		//				Key.Enter)
			//		//			{
			//		//				RoutedEvent = TextBox.PreviewKeyDownEvent
			//		//			});
			//		//}
			//		//else
			//		//	TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, boolResultWithErrorMessage.ErrorMessage);
			//	}
			//}
			//else if (e.Key == Key.Escape)
			//{
			//	TextBox actualTextBox = GetActualTextBoxOfAutocompleteControl();
			//	actualTextBox.SelectionLength = 0;
			//	actualTextBox.SelectionStart = actualTextBox.Text.Length;

			//	if (textBox_CommandLine.IsDropDownOpen)
			//	{
			//		e.Handled = true;
			//		textBox_CommandLine.IsDropDownOpen = false;
			//	}
			//	else if (actualTextBox.Text.Length > 0)
			//	{
			//		e.Handled = true;
			//		actualTextBox.Text = "";
			//	}
			//	//else if (GetEmbeddedButton().Tag != null && (GetEmbeddedButton().Tag as ICommandWithHandler).CurrentArgumentCount > 0)
			//	//{
			//	//	ICommandWithHandler tmpCommand = GetEmbeddedButton().Tag as ICommandWithHandler;
			//	//	tmpCommand.RemoveCurrentArgument(tmpCommand.CurrentArgumentCount - 1);
			//	//	textBox_CommandLine.ItemsSource = tmpCommand.GetPredefinedArgumentsList(tmpCommand.CurrentArgumentCount, true);
			//	//}
			//	else if (treeView_CommandList.SelectedItem != null)
			//	{
			//		e.Handled = true;
			//		ClearCommandSelection();
			//	}
			//	else
			//	{
			//		e.Handled = true;
			//		GetTopParent().Hide();
			//	}
			//}
			//else if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.Control)
			//{
			//	e.Handled = true;
			//	textBox_CommandLine.IsDropDownOpen = true;
			//}
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

		private void FocusNextCommandArgument(int overwriteNewSelectedIndex = -2)
		{
			ICommandWithHandler comm = GetEmbeddedButton().Tag as ICommandWithHandler;
			ListBox lb = GetEmbeddedListbox();
			if (lb.Items.Count == 0) return;
			int currentSelectedIndex = lb.SelectedIndex;
			int newSelectedIndex = currentSelectedIndex;
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
			if (overwriteNewSelectedIndex != -2)
				newSelectedIndex = overwriteNewSelectedIndex;
			if (newSelectedIndex == -1 || newSelectedIndex == currentSelectedIndex) return;

			//bool OpenDropDownFound = false;
			//ListBox tmpListbox = GetEmbeddedListbox();
			//for (int i = 1; i < tmpListbox.Items.Count; i++)
			//	if (GetAutocompleteBoxOfArgument(tmpListbox.Items[i]).IsDropDownOpen)
			//		OpenDropDownFound = true;

			//if (OpenDropDownFound)
			//	return;

			lb.SelectedIndex = newSelectedIndex;
			TextBox textboxOfArgument = GetActualTextboxOfArgument(lb.SelectedItem);
			//GetAutocompleteBoxOfArgument(lb.SelectedItem).ItemsSource = comm.GetPredefinedArgumentsList(newSelectedIndex, true);
			if (!GetAutocompleteBoxOfArgument(lb.SelectedItem).IsFocused) GetAutocompleteBoxOfArgument(lb.SelectedItem).Focus();
			if (!textboxOfArgument.IsFocused) textboxOfArgument.Focus();
			GetAutocompleteBoxOfArgument(lb.Items[currentSelectedIndex]).IsDropDownOpen = false;
		}

		private void ClearCommandSelection()
		{
			ClearSelection(treeView_CommandList);
			SetDataContext(null);
			//GetEmbeddedListbox().ItemsSource = null;
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
			PressEnterKeyInsideArgumentTextbox();
			//textBox_CommandLine.RaiseEvent(
			//		 new KeyEventArgs(
			//			 Keyboard.PrimaryDevice,
			//			 PresentationSource.FromVisual(textBox_CommandLine),
			//			 0,
			//			 Key.Enter)
			//		 {
			//			 RoutedEvent = TextBox.PreviewKeyDownEvent
			//		 });
		}

		//private AutoCompleteBox LastFocusedArgumentAutocompleteTextbox;
		private void ArgumentText_GotFocus(object sender, RoutedEventArgs e)
		{
			//try
			//{
			//AutoCompleteBox acb = sender as AutoCompleteBox;
			//DockPanel dp = acb.Parent as DockPanel;
			//TextBlock tb = dp.Children[1] as TextBlock;
			//TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, tb.Text);
			//if (acb.ItemsSource is ObservableCollection<string>)
			//{
			//	ObservableCollection<string> tmpFilteredAutocompleteList = acb.ItemsSource as ObservableCollection<string>;
			//	if (tmpFilteredAutocompleteList.Count != null || tmpFilteredAutocompleteList[0] != acb.Text)
			//		acb.IsDropDownOpen = true;
			//}
			//if (LastFocusedArgumentAutocompleteTextbox != sender)
			//{
			//	LastFocusedArgumentAutocompleteTextbox = sender as AutoCompleteBox;
			//TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, (sender as AutoCompleteBox).Tag.ToString());
			(sender as AutoCompleteBox).IsDropDownOpen = true;
			//}
			//(sender as AutoCompleteBox).IsDropDownOpen = true;
			//}
			//catch { }
		}

		private void ClearTextboxTextButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			DependencyObject dp = (sender as Border).Parent;
			if (dp != null)
			{
				TextBox actualtextbox = ((dp as DockPanel).Children[1] as TextBox);
				actualtextbox.Text = "";
				if (!actualtextbox.IsFocused) actualtextbox.Focus();
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

		ContextMenu cm = new ContextMenu() { Visibility = Visibility.Collapsed };
		private void AutoCompleteActualTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			//(sender as TextBox).ContextMenu = GetActualTextBoxOfAutocompleteControl().Text == "" ? cm : null;
			//if (GetActualTextBoxOfAutocompleteControl().Text == "") e.Handled = true;
			if (textBox_CommandLine.ItemsSource == null) e.Handled = true;
		}

		private void TextBoxWithText_LostFocus(object sender, RoutedEventArgs e)
		{

		}

		//private TextBox LastFocusedArgumentTextbox;
		private void TextBoxWithText_GotFocus(object sender, RoutedEventArgs e)
		{
			//if (LastFocusedArgumentTextbox != sender)
			//	TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, (sender as TextBox).Tag.ToString());
			//LastFocusedArgumentTextbox = sender as TextBox;
			//(sender as TextBox).SelectionStart = (sender as TextBox).Text.Length;
		}

		private void textBox_Messages_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			//Window w = this.GetTopParent();
			//if (w != null)
			//{
			//	e.Handled = true;
			//	w.DragMove();
			//}
		}

		//private ListBox FindListboxInsidePopup(Popup popup)
		//{
		//	return popup.FindName("Selector") as ListBox;
		//}

		private TextBox FindTextboxPopupSibling(Popup popup)
		{
			return (popup.Parent as Grid).Children[0] as TextBox;
		}

		//private string lastValue;
		private void Popup_Opened(object sender, EventArgs e)
		{
			////LastSelectedIndex = FindListboxInsidePopup(sender as Popup).SelectedIndex;
			//lastValue = FindTextboxPopupSibling(sender as Popup).Text;
		}

		private void Popup_Closed(object sender, EventArgs e)
		{
			//if (FindTextboxPopupSibling(sender as Popup).Text != lastValue)
			//	if (!Keyboard.IsKeyDown(Key.Tab)) FocusNextCommandArgument();
		}

		private void AutoCompleteActualTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			//if (e.Key == Key.Enter)
			//{
			//	e.Handled = true;
			//	PressEnterKeyInsideArgumentTextbox();
			//}
			//else if (e.Key == Key.Tab)
			//{
			//	//Border tmpBorder = GetEmbeddedButton();
			//	//ICommandWithHandler comm = GetEmbeddedButton().Tag as ICommandWithHandler;
			//	if (GetEmbeddedButton().Tag == null && textBox_CommandLine.Text.Trim().Length > 0)
			//	{
			//		if (InitiateCommandFromTextboxText())
			//			e.Handled = true;
			//	}
			//	else// if (textBox_CommandLine.Text.Trim().Length > 0)
			//	{
			//		if (Keyboard.Modifiers != ModifierKeys.Shift && Keyboard.Modifiers != ModifierKeys.None)
			//			return;
			//		e.Handled = true;

			//		FocusNextCommandArgument();
			//	}
			//}
			//else if (e.Key == Key.Escape)
			//{
			//	TextBox actualTextBox = GetActualTextBoxOfAutocompleteControl();
			//	actualTextBox.SelectionLength = 0;
			//	actualTextBox.SelectionStart = actualTextBox.Text.Length;

			//	if (textBox_CommandLine.IsDropDownOpen)
			//	{
			//		e.Handled = true;
			//		textBox_CommandLine.IsDropDownOpen = false;
			//	}
			//	else if (actualTextBox.Text.Length > 0)
			//	{
			//		e.Handled = true;
			//		actualTextBox.Text = "";
			//	}
			//	//else if (GetEmbeddedButton().Tag != null && (GetEmbeddedButton().Tag as ICommandWithHandler).CurrentArgumentCount > 0)
			//	//{
			//	//	ICommandWithHandler tmpCommand = GetEmbeddedButton().Tag as ICommandWithHandler;
			//	//	tmpCommand.RemoveCurrentArgument(tmpCommand.CurrentArgumentCount - 1);
			//	//	textBox_CommandLine.ItemsSource = tmpCommand.GetPredefinedArgumentsList(tmpCommand.CurrentArgumentCount, true);
			//	//}
			//	else if (treeView_CommandList.SelectedItem != null)
			//	{
			//		e.Handled = true;
			//		ClearCommandSelection();
			//	}
			//	else
			//	{
			//		e.Handled = true;
			//		GetTopParent().Hide();
			//	}
			//}
			//else if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.Control)
			//{
			//	e.Handled = true;
			//	textBox_CommandLine.IsDropDownOpen = true;
			//}
			////else if (e.Key == Key.Down)
			////{
			////	//e.Handled = true;
			////	//Grid parentGrid = (sender as TextBox).Parent as Grid;
			////	//Popup popupAutocomplete = parentGrid.Children[2] as Popup;
			////	//ListBox listboxAutocomplete =	(popupAutocomplete.Child as Border).Child as ListBox;
			////	//TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, listboxAutocomplete.SelectedIndex.ToString());
			////}
		}

		private void PressEnterKeyInsideArgumentTextbox()
		{
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
					//GetAutocompleteBoxOfArgument(lbo).IsDropDownOpen = false;
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

		private void MainAutoCompleteTextbox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				PressEnterKeyInsideArgumentTextbox();
			}
			else if (e.Key == Key.Tab)
			{
				//Border tmpBorder = GetEmbeddedButton();
				//ICommandWithHandler comm = GetEmbeddedButton().Tag as ICommandWithHandler;
				if (GetEmbeddedButton().Tag == null && textBox_CommandLine.Text.Trim().Length > 0)
				{
					if (InitiateCommandFromTextboxText())
						e.Handled = true;
				}
				else// if (textBox_CommandLine.Text.Trim().Length > 0)
				{
					if (Keyboard.Modifiers != ModifierKeys.Shift && Keyboard.Modifiers != ModifierKeys.None)
						return;
					e.Handled = true;

					FocusNextCommandArgument();
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
			//	//e.Handled = true;
			//	//Grid parentGrid = (sender as TextBox).Parent as Grid;
			//	//Popup popupAutocomplete = parentGrid.Children[2] as Popup;
			//	//ListBox listboxAutocomplete =	(popupAutocomplete.Child as Border).Child as ListBox;
			//	//TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, listboxAutocomplete.SelectedIndex.ToString());
			//}
		}

		private void ArgumentText_DragOver(object sender, DragEventArgs e)
		{
			if (IsDragDropFormatSupported(e))
			{
				if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
				{
					e.Effects = DragDropEffects.Copy;
					e.Handled = true;
				}
			}
		}

		private void ArgumentText_Drop(object sender, DragEventArgs e)
		{
			e.Handled = true;
			int tmpIndex = GetEmbeddedListbox().Items.IndexOf((sender as AutoCompleteBox).DataContext);
			if (tmpIndex == -1)
				return;
			//GetEmbeddedListbox().SelectedItem = GetEmbeddedListbox().ItemContainerGenerator.ContainerFromIndex(tmpIndex) as ListBoxItem;
			(GetEmbeddedListbox().ItemContainerGenerator.ContainerFromIndex(tmpIndex) as ListBoxItem).IsSelected = true;
			DoDropOfActiveCommand(e);
		}

		private bool IsDragDropFormatSupported(DragEventArgs evtArgs)
		{
			return
				evtArgs.Data.GetDataPresent(DataFormats.FileDrop)
				|| evtArgs.Data.GetDataPresent(DataFormats.Text);
		}

		private void DoDropOfActiveCommand(DragEventArgs e, bool PrompBeforePerformingCommand = true)
		{
			string NewText = null;
			if (
				IsDragDropFormatSupported(e)
				 && activeCommand != null)
			{
				if (e.Data.GetDataPresent(DataFormats.FileDrop))
				{
					string[] filesDropped = e.Data.GetData(DataFormats.FileDrop) as string[];
					if (filesDropped.Length > 1 &&
						UserMessages.ShowWarningMessage("Only one file dropped is allowed"))
						return;
					NewText = filesDropped[0];
				}
				else if (e.Data.GetDataPresent(DataFormats.Text))
				{
					NewText = e.Data.GetData(DataFormats.Text) as string;
				}

				if (NewText != null)
				{
					ListBox embeddedListbox = GetEmbeddedListbox();
					CommandArgument arg = embeddedListbox.SelectedItem as CommandArgument;
					arg.CurrentValue = NewText;
					if (activeCommand.CurrentArguments.Count == 1)
						if (!PrompBeforePerformingCommand
							|| UserMessages.Confirm("Perform command now?", DefaultYesButton: true))
							PerformCurrentCommand();
				}
			}
		}

		private bool SelectedTreeViewItemChangedFromDragOver = false;
		private void treeView_CommandList_DragOver(object sender, DragEventArgs e)
		{
			if (IsDragDropFormatSupported(e))//(e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
			{
				e.Effects = DragDropEffects.Copy;
				e.Handled = true;
				DependencyObject k = VisualTreeHelper.HitTest(treeView_CommandList, e.GetPosition(treeView_CommandList)).VisualHit;
				if (k != null)
				{
					DependencyObject dp = k;
					do
					{
						if (dp is TreeViewItem) break;
						if (dp is TreeView) break;//Busy dragging inside treeview but not inside an treeviewitem
						dp = VisualTreeHelper.GetParent(dp);
					}
					while (dp != null);
					if (dp is TreeViewItem)
					{
						TreeViewItem tvi = dp as TreeViewItem;
						SelectedTreeViewItemChangedFromDragOver = true;
						tvi.IsSelected = true;
						treeView_CommandList.Focus();
						tvi.Focus();
					}
				}
			}
		}

		private void treeView_CommandList_PreviewDrop(object sender, DragEventArgs e)
		{
			e.Handled = true;

			DoDropOfActiveCommand(e, false);

			//if (activeCommand.CurrentArguments.Count == 1)
			//	activeCommand.CurrentArguments[0].CurrentValue = 
		}

		//private bool IsTreeViewDragBusy = false;
	}
}