using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DynamicDLLsInterop;
using InlineCommandToolkit;//InlineCommands.CommandsManagerClass.OverrideToStringClass;
//using InterfaceForQuickAccessPlugin;
using PropertyInterceptor;
//using dragonz.actb.core;
//using dragonz.actb.provider;
using ICommandWithHandler = InlineCommandToolkit.InlineCommands.ICommandWithHandler;//InlineCommands.CommandsManagerClass.ICommandWithHandler;
using MessagesParagraph = InlineCommandToolkit.MessagesParagraph;
using OverrideToStringClass = InlineCommandToolkit.InlineCommands.OverrideToStringClass;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for InlineCommandsUserControlWPF.xaml
	/// </summary>
	public partial class CommandsUsercontrol : UserControl
	{
		public TextFeedbackEventHandler textFeedbackEvent;
		public ProgressChangedEventHandler progressChangedEvent;
		public System.Windows.Forms.Form MainFormUsedForShuttingDownServers;
		Socket listeningSocket;
		public event System.ComponentModel.PropertyChangedEventHandler CommandPropertyChangedEvent;

		//AnimationTimeline firstHalve;
		//AnimationTimeline secondHalve;

		Storyboard rotateMessagesFirstHalve { get { return mainGrid.Resources["rotateMessagesFirstHalve"] as Storyboard; } }
		Storyboard rotateMessagesSecondHalve { get { return mainGrid.Resources["rotateMessagesSecondHalve"] as Storyboard; } }

		//AutoCompleteManager autcompleteManager;
		//AutocompleteProvider autocompleteProvider;

		private void HideParentWindow()
		{
			if (!(this.Parent is Grid) || !((this.Parent as Grid).Parent is Window))
				return;
			((this.Parent as Grid).Parent as Window).Hide();
		}

		public void LoadAllPlugins()
		{
			System.Windows.Forms.Application.DoEvents();
			if (!AppDomain.CurrentDomain.BaseDirectory.ToLower().Contains(@"QuickAccess\QuickAccess\bin".ToLower()))
				DynamicDLLs.LoadPluginsInDirectory(System.AppDomain.CurrentDomain.BaseDirectory + @"Plugins");
			else
				foreach (string pluginProjectBaseDir in Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\\Visual Studio 2010\Projects\QuickAccess", "*Plugin"))
					DynamicDLLs.LoadPluginsInDirectory(pluginProjectBaseDir + @"\bin\Release");
		}

		public CommandsUsercontrol()
		{
			InitializeComponent();
			//LoadPlugins();
		}

		//public CommandsUsercontrol(System.Windows.Forms.Form mainFormUsedForShuttingDownServers)
		//{
		//	InitializeComponent();
		//	MainFormUsedForShuttingDownServers = mainFormUsedForShuttingDownServers;
		//}

		//private FlowDocument messagesFlowDocument = new FlowDocument();
		private bool textFeedbackEventInitialized = false;
		public void InitializeTreeViewNodes(System.Windows.Forms.Form mainFormUsedForShuttingDownServers)//, bool ShowExitAndMinimizeToTrayButtons = false, Action CloseButtonPressed = null, Action MinimizeToTrayButtonPressed = null)
		{
			MainFormUsedForShuttingDownServers = mainFormUsedForShuttingDownServers;
			if (!textFeedbackEventInitialized)
			{
				//if (ShowExitAndMinimizeToTrayButtons)
				//{
				//	stackPanelWithCloseAndMinimizeToTrayButton.Visibility = System.Windows.Visibility.Visible;
					
				//	CloseUsercontrolButton.PreviewMouseLeftButtonDown += delegate { CloseButtonPressed(); };
				//	MinimizeToTrayUsercontrolButton.PreviewMouseLeftButtonDown += delegate { MinimizeToTrayButtonPressed(); };
				//}

				this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(CommandsUsercontrol_IsVisibleChanged);

				//DONE: Should eventually keep track of which messages goes with which command (keep track of Paragraphs).
				textFeedbackEvent += (snder, evtargs) =>
				{
					Dispatcher.BeginInvoke(DispatcherPriority.Background,
					(Action)delegate
					{
						MessagesParagraph tmpParagraph = new MessagesParagraph(evtargs.FeedbackType);
						//tmpParagraph.LineStackingStrategy = LineStackingStrategy.MaxHeight;
						if (evtargs is TextFeedbackEventArgs_MultiObjects)
						{
							TextFeedbackEventArgs_MultiObjects args = (evtargs as TextFeedbackEventArgs_MultiObjects);
							List<TextFeedbackSection> list = args.FeedbackStringList;
							int cnt = 0;
							foreach (TextFeedbackSection sec in list)
							{
								if (args.AutoSeparateWithSpaces && cnt++ > 0)
									tmpParagraph.Inlines.Add(new Run(" "));

								if (sec.DisplayType == TextFeedbackSection.DisplayTypeEnum.MakeButton)
								{
									Button buttonToAdd = new Button() { Content = sec.Text, Tag = sec };
									if (sec.ActionOnDoubleClick != null)
										buttonToAdd.MouseDoubleClick += (dblclicksnder, dblclickevtargs) =>
										{
											if (!(dblclicksnder is Button) || (!((dblclicksnder as Button).Tag is TextFeedbackSection)))
												return;
											TextFeedbackSection snderSec = (dblclicksnder as Button).Tag as TextFeedbackSection;
											if (snderSec.ActionOnDoubleClick != null)
												snderSec.ActionOnDoubleClick(snderSec.ActionTag);
										};
									if (this.Resources["paragraphStandOut"] is ControlTemplate)
										buttonToAdd.Template = (ControlTemplate)this.Resources["paragraphStandOut"];
									InlineUIContainer inlineUiContainer = new InlineUIContainer(buttonToAdd) { BaselineAlignment = BaselineAlignment.Center };
									tmpParagraph.Inlines.Add(inlineUiContainer);
								}
								else
								{
									Run runToAdd = new Run(sec.Text);
									//if (sec.DisplayType == TextFeedbackSection.DisplayTypeEnum.LargerSize)
									//	runToAdd.FontSize = 18;
									//if (sec.DisplayType == TextFeedbackSection.DisplayTypeEnum.Bold)
									//	runToAdd.FontWeight = FontWeights.Bold;
									tmpParagraph.Inlines.Add(runToAdd);
								}
							}
						}
						else
							tmpParagraph.Inlines.Add(new Run(evtargs.FeedbackText));

						tmpParagraph.Foreground =
							evtargs.FeedbackType == TextFeedbackType.Error ? Brushes.Red
							: evtargs.FeedbackType == TextFeedbackType.Noteworthy ? Brushes.Purple
							: evtargs.FeedbackType == TextFeedbackType.Success ? Brushes.Green
							: evtargs.FeedbackType == TextFeedbackType.Subtle ? Brushes.LightGray
							: Brushes.Gold;
						tmpParagraph.TextIndent = 0;// -25;
						tmpParagraph.Margin = new Thickness(0);//25, 0, 0, 0);
						tmpParagraph.ToolTip = DateTime.Now.ToString(@"HH\hmm:ss \o\n ddd, dd MM yyyy");

						if (snder is ICommandWithHandler)
						{
							ICommandWithHandler tmpCommand = snder as ICommandWithHandler;
							tmpCommand.MessagesList.Add(tmpParagraph);

							//DONE: Handle the messages better
							//Like if a message is new and the relevant command is not selected, add a "star" to the command
							//if (tmpCommand != activeCommand) SetDataContext(tmpCommand);
							//SelectTreeViewItemBasedOnCommand(tmpCommand);
							if (tmpCommand == activeCommand)
							{
								if (this.IsVisible)
									tmpParagraph.Unread = false;
								textBox_Messages.Document.Blocks.Add(tmpParagraph);
								textBox_Messages.ScrollToEnd();
							}

							tmpCommand.NotifyPropertyChanged("NumberUnreadMessages");
						}
						else
						{
							//UserMessages.ShowWarningMessage("Text feedback did not have a command linked to it, not written into messages list: " + evtargs.FeedbackText);
							textBox1.Text +=
								(textBox1.Text.Length > 0 ? Environment.NewLine : "")
								+ "(" + DateTime.Now.ToString("ddd, dd MM yyyy HH:mm:ss") + ")  " + evtargs.FeedbackText;
							textBox1.ScrollToEnd();
						}

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
						//textBox_Messages.Document.Blocks.Add(new Paragraph(new Run(evtargs.CurrentValue + "/" + evtargs.MaximumValue)) { Foreground = Brushes.Blue });
						progressBar1.Maximum = evtargs.MaximumValue;
						progressBar1.Value = evtargs.CurrentValue;
						if (evtargs.CurrentValue == evtargs.MaximumValue)
							progressBar1.Value = 0;
						//textBox_Messages.Text += (textBox_Messages.Text.Length > 0 ? Environment.NewLine : "")
						////textBox_Messages.Content += (textBox_Messages.Content.ToString().Length > 0 ? Environment.NewLine : "")
						//	+ evtargs.CurrentValue + "/" + evtargs.MaximumValue;
						//textBox_Messages.ScrollToEnd();//.ScrollToBottom();
					});
				};

				//label_ArgumentsExample.Content = "";

				//System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
				//timer.Interval = 5000;
				//timer.Tick += delegate
				//{
				//	timer.Stop();
				//DynamicDLLsInterop.DynamicDLLs.LoadPluginsInDirectory(@"D:\Francois\Dev\VSprojects\QuickAccess\QuickAccess\bin\Release\Plugins");
				//DynamicDLLsInterop.DynamicDLLs.LoadPluginsInDirectory(@"D:\Francois\Dev\VSprojects\QuickAccess\QuickAccess\bin\Release\Plugins");


				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackEvent, "Loading plugins...", TextFeedbackType.Subtle);
				LoadAllPlugins();
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackEvent, "Done loading plugins.", TextFeedbackType.Subtle);

				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackEvent, "Refreshing Command plugins...", TextFeedbackType.Subtle);
				treeView_CommandList.Items.Clear();
				//List<OverrideToStringClass> tmplist = new List<OverrideToStringClass>();//CommandsManagerClass.ListOfInitializedCommandInterfaces;
				foreach (IQuickAccessPluginInterface qai in DynamicDLLs.PluginList)
					if (qai.GetType().GetInterface(typeof(ICommandWithHandler).Name) != null)
					{
						ICommandWithHandler comm = qai as ICommandWithHandler;
						if (qai is OverrideToStringClass)
							(qai as OverrideToStringClass).PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Command_PropertyChanged);
						//ICommandWithHandler comm = (ICommandWithHandler)qai.GetType().GetConstructor(new Type[0]).Invoke(new object[0]);
						//OverrideToStringClass comm =
						//		(OverrideToStringClass)qai.GetType().GetConstructor(new Type[0]).Invoke(new object[0]);
						//MessageBox.Show(comm.DisplayName);
						//tmplist.Add(comm);
						CommandsManagerClass.ListOfInitializedCommandInterfaces.Add(comm);
					}
				textBox_CommandLine.ItemsSource = new ObservableCollection<string>();
				//foreach (ICommandWithHandler comm in tmplist)
				//foreach (OverrideToStringClass comm in tmplist)
				foreach (InlineCommandToolkit.InlineCommands.OverrideToStringClass comm in CommandsManagerClass.ListOfInitializedCommandInterfaces)
					treeView_CommandList.Items.Add(comm);
				//	timer.Dispose(); timer = null;
				//};
				//timer.Start();
				treeView_CommandList.UpdateLayout();
				this.UpdateLayout();
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackEvent, "Done refreshing Command plugins.", TextFeedbackType.Noteworthy);

				//ControlTemplate ct = this.FindResource("TextBoxBaseControlTemplate") as ControlTemplate;
				//GetActualTextBoxOfAutocompleteControl().Template = ct;
				//GetActualTextBoxOfAutocompleteControl().ApplyTemplate();
				TextBox actualTextbox = GetActualTextBoxOfAutocompleteControl();
				if (actualTextbox != null)
					actualTextbox.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
				ResetAutocompleteToCommandNamesList();
				//HideEmbeddedButton();
				SetVisibilityOfExtraControls();

				//if (!GetActualTextBoxOfAutocompleteControl().IsFocused) GetActualTextBoxOfAutocompleteControl().Focus();
				FocusActualTextboxOfAutocompleteControl();

				ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				{
					//ICommandWithHandler Command = null;
					//foreach (IQuickAccessPluginInterface qai in DynamicDLLs.PluginList)
					//	if (qai.GetType().GetInterface(typeof(ICommandWithHandler).Name) != null)
					//		tmpCommand = (ICommandWithHandler)qai.GetType().GetConstructor(new Type[0]).Invoke(new object[0]);
					NetworkInterop.StartServer_FileStream(
						null,//tmpCommand,
						out listeningSocket,
						(err) => UserMessages.ShowErrorMessage(err),
						MainFormUsedForShuttingDownServers,
						TextFeedbackEvent: textFeedbackEvent,
						ProgressChangedEvent: progressChangedEvent);
				}, false);

				//XmlRpcInterop.SampleServer();
				XmlRpcInterop.StartDynamicCodeInvokingServer_XmlRpc();

				//Taken out the monitoring of subversion, it's redundant because TestingMonitorSubversion application already does that
				/*foreach (IQuickAccessPluginInterface iqa in DynamicDLLsInterop.DynamicDLLs.PluginList)
					if (iqa is ICommandWithHandler)
					{
						ICommandWithHandler comm = iqa as ICommandWithHandler;
						if (comm.CommandName.ToLower() == "subversion")
						{
							SubversionInterop.StartMonitoringSubversionDirectories(textFeedbackEvent, comm);
							break;
						}
					}*/

				//SubversionInterop.StartMonitoringSubversionDirectories(textFeedbackEvent);

				//firstHalve = MyDoubleAnim(0, -90, 0.2);
				//secondHalve = MyDoubleAnim(90, 0, 0.2);

				//firstHalve.Completed += new EventHandler(firstHalve_Completed);
				rotateMessagesFirstHalve.Completed += new EventHandler(rotateMessagesFirstHalve_Completed);

				GeneralResourceDictionary.CommandPerformedEvent += new CommandPerformedEventHandler(GeneralResourceDictionary_CommandPerformedEvent);
				//if (this.Resources.MergedDictionaries[0] is GeneralResourceDictionary)
					//System.Windows.Forms.MessageBox.Show(this.Resources.MergedDictionaries[0].GetType().Name);

				//Facedetection disabled for now
				//FaceDetectionInterop.InitializeFaceDetection();
				GenericSettings.EnsureAllSettingsAreInitialized();

				textFeedbackEventInitialized = true;
			}
		}

		private void rotateMessagesFirstHalve_Completed(object sender, EventArgs e)
		{
			textBox_Messages.Document.Blocks.Clear();
			if (activeCommand != null)
			{
				textBox_Messages.Document.Blocks.AddRange(activeCommand.MessagesList);
				textBox_Messages.ScrollToEnd();
			}
			rotateMessagesSecondHalve.Begin();
			//planerator1.BeginAnimation(Planerator.Planerator.RotationYProperty, secondHalve);
		}

		private void GeneralResourceDictionary_CommandPerformedEvent(object sender, CommandPerformedEventArgs e)
		{
			switch (e.CommandPerformed)
			{
				case CommandPerformedType.EmbeddedButton_MouseLeftButtonDown:
					EmbeddedButton_MouseLeftButtonDown(sender, e.eventArgs as MouseButtonEventArgs);
					break;
				case CommandPerformedType.EmbeddedButton_MouseRightButtonDown:
					EmbeddedButton_MouseRightButtonDown(sender, e.eventArgs as MouseButtonEventArgs);
					break;
				case CommandPerformedType.ClearTextboxTextButton_MouseLeftButtonDown:
					ClearTextboxTextButton_MouseLeftButtonDown(sender, e.eventArgs as MouseButtonEventArgs);
					break;
				case CommandPerformedType.AutoCompleteActualTextBox_PreviewKeyDown:
					AutoCompleteActualTextBox_PreviewKeyDown(sender, e.eventArgs as KeyEventArgs);
					break;
				case CommandPerformedType.ArgumentText_DragOver:
					ArgumentText_DragOver(sender, e.eventArgs as DragEventArgs);
					break;
				case CommandPerformedType.ArgumentText_Drop:
					ArgumentText_Drop(sender, e.eventArgs as DragEventArgs);
					break;
				case CommandPerformedType.ArgumentText_GotFocus:
					ArgumentText_GotFocus(sender, e.eventArgs as RoutedEventArgs);
					break;
				case CommandPerformedType.ArgumentText_SelectionChanged:
					ArgumentText_SelectionChanged(sender, e.eventArgs as SelectionChangedEventArgs);
					break;
				default:
					break;
			}
		}

		private ICommandWithHandler activeCommand = null;
		private void SetDataContext(ICommandWithHandler command = null)
		{
			if (activeCommand == command)
				return;

			activeCommand = command;
			textBox_CommandLine.DataContext = activeCommand;
			textBox_CommandLine.UpdateLayout();
			textBoxWithButtons.DataContext = activeCommand;
			textBoxWithButtons.UpdateLayout();
			textBox_Messages.DataContext = activeCommand;
			textBox_Messages.UpdateLayout();

			rotateMessagesFirstHalve.Begin();
			//planerator1.FieldOfView = 1;
			//planerator1.BeginAnimation(Planerator.Planerator.RotationYProperty, firstHalve);
			//textBox_Messages.Document.Blocks.Clear();
			//if (command != null)
			//{
			//	textBox_Messages.Document.Blocks.AddRange(command.MessagesList);
			//	textBox_Messages.ScrollToEnd();
			//}

			SetVisibilityOfExtraControls();
		}

		//private void firstHalve_Completed(object sender, EventArgs e)
		//{
		//	textBox_Messages.Document.Blocks.Clear();
		//	if (activeCommand != null)
		//	{
		//		textBox_Messages.Document.Blocks.AddRange(activeCommand.MessagesList);
		//		textBox_Messages.ScrollToEnd();
		//	}
		//	planerator1.BeginAnimation(Planerator.Planerator.RotationYProperty, secondHalve);
		//}

		private AnimationTimeline MyDoubleAnim(double from, double to, double sec)
		{
			int msec = (int)(sec * 1000);
			DoubleAnimation da = new DoubleAnimation(from, to, new Duration(new TimeSpan(msec * 1000 * 10)));
			da.DecelerationRatio = 0.2;
			//da.RepeatBehavior = RepeatBehavior.Forever;
			da.FillBehavior = FillBehavior.Stop;

			return da;
		}

		private void CommandsUsercontrol_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (activeCommand == null)
				return;

			if (e.NewValue is bool && (bool)e.NewValue == true)
				MarkAllCommandMessagesAsRead(activeCommand);
		}

		private void Command_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (string.Compare(e.PropertyName, "MessagesList", true) == 0)
				SetDataContext(activeCommand);
			if (string.Compare(e.PropertyName, "NumberUnreadMessages", true) == 0)
			{
				//BindingExpression be = BindingOperations.GetBindingExpression((((this.Parent as Grid).Parent as Window).TaskbarItemInfo.Overlay as DrawingImage).Drawing as GeometryDrawing, GeometryDrawing.BrushProperty);
				//BindingExpression be = BindingOperations.GetBindingExpression(((((this.Parent as Grid).Parent as Window).TaskbarItemInfo.Overlay as DrawingImage).Drawing as DrawingGroup).Children[0] as GeometryDrawing, GeometryDrawing.BrushProperty);
				//be.UpdateSource();
				UpdateTaskbarOverlayIconForUnreadMessages();
			}
			//System.Windows.Forms.MessageBox.Show("Prop changed: " + e.PropertyName);

			RaiseCommandPropertyChangedEvent(sender, e);
		}

		private void RaiseCommandPropertyChangedEvent(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (CommandPropertyChangedEvent != null)
				CommandPropertyChangedEvent(sender, e);
		}

		public void UpdateTaskbarOverlayIconForUnreadMessages()
		{
			(((((this.Parent as Grid)
						.Parent as Window)
						.TaskbarItemInfo.Overlay as DrawingImage)
						.Drawing as DrawingGroup)
						.Children[0] as GeometryDrawing)
						.Brush = HasUnreadMessagesBrush;
		}

		public static Brush HasUnreadMessagesBrush
		{
			get
			{
				if (DynamicDLLs.HasUnreadMessages)
					return Brushes.Red;
				else
					return Brushes.Transparent;//Green;
			}
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
			List<ICommandWithHandler> tmplist = CommandsManagerClass.ListOfInitializedCommandInterfaces;
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
			if (textBox_CommandLine.Template == null)
				return null;
			TextBox actualTextBoxOfAutocompleteControl = (TextBox)textBox_CommandLine.Template.FindName("Text", textBox_CommandLine);
			if (actualTextBoxOfAutocompleteControl == null)
				UserMessages.ShowWarningMessage("Could not find Text in template");
			return actualTextBoxOfAutocompleteControl;
		}

		private void FocusActualTextboxOfAutocompleteControl()
		{
			TextBox tmptxtbox = GetActualTextBoxOfAutocompleteControl();
			if (tmptxtbox != null && !tmptxtbox.IsFocused)
				tmptxtbox.Focus();
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
					//label_ArgumentsExample.Content = label_ArgumentsExample.ToolTip = command.ArgumentsExample.Replace("\n", "  ");

					MarkAllCommandMessagesAsRead(command);

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
					if (actualTextbox != null)
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
				//label_ArgumentsExample.Content = "";

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
				//if (!GetActualTextBoxOfAutocompleteControl().IsFocused) GetActualTextBoxOfAutocompleteControl().Focus();
				FocusActualTextboxOfAutocompleteControl();
				//autocompleteProvider.activeCommand = null;
			}
		}

		private static void MarkAllCommandMessagesAsRead(ICommandWithHandler command)
		{
			foreach (MessagesParagraph mp in command.MessagesList)
			{
				if (mp.Unread)
					mp.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
				else
					mp.Background = null;
				mp.Unread = false;
			}
			command.NotifyPropertyChanged("NumberUnreadMessages");
		}

		private void SetVisibilityOfExtraControls()
		{
			ControlTemplate controlTemplate = MainAutoCompleteTextbox.Template;
			Border controlTemplateBorder = VisualTreeHelper.GetChild(MainAutoCompleteTextbox, 0) as Border;
			DockPanel dp = controlTemplateBorder.Child as DockPanel;

			TextBox tb = dp.Children[0] as TextBox;//controlTemplate.FindName("TextBoxWithButtons", dp) as TextBox;
			tb.Visibility = textBox_CommandLine.DataContext != null ? Visibility.Visible : Visibility.Collapsed; ;

			AutoCompleteBox acb = dp.Children[1] as AutoCompleteBox;
			acb.Visibility = textBox_CommandLine.DataContext == null ? Visibility.Visible : Visibility.Collapsed; ;
			MainAutoCompleteTextbox.UpdateLayout();
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
			foreach (ICommandWithHandler comm in treeView_CommandList.Items)
				if (textBox_CommandLine.Text.Equals(comm.CommandName, StringComparison.InvariantCultureIgnoreCase))
				{
					CommandName = comm.DisplayName;
					return true;
				}
			CommandName = "";
			return false;
		}

		private void SelectTreeViewItemBasedOnCommand(ICommandWithHandler command)
		{
			int tmpIndex = treeView_CommandList.Items.IndexOf(command);
			if (tmpIndex == -1) return;
			TreeViewItem tvi = treeView_CommandList.ItemContainerGenerator.ContainerFromIndex(tmpIndex) as TreeViewItem;
			tvi.IsSelected = true;
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
			//string tmpCommandName;
			//if (CommandNameExistOfTextboxText(out tmpCommandName))
			//	label_ArgumentsExample.Content = "Press TAB to initiate mode for " + tmpCommandName;
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
			//			TextBox buildTask = GetActualTextboxOfArgument(lbo);
			//			buildTask.SelectionLength = 0;
			//			buildTask.SelectionStart = buildTask.Text.Length;
			//			BindingExpression be = buildTask.GetBindingExpression(TextBox.TextProperty);
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

		private bool GetActiveCommand(out ICommandWithHandler activeCommand)
		{
			if (treeView_CommandList.SelectedItem != null && treeView_CommandList.SelectedItem is ICommandWithHandler)
			{
				activeCommand = treeView_CommandList.SelectedItem as ICommandWithHandler; ;
				return true;
			}
			activeCommand = null;
			return false;
		}

		private void textBox_CommandLine_TextChanged_1(object sender, RoutedEventArgs e)
		{
			//string tmpCommandName;
			//if (CommandNameExistOfTextboxText(out tmpCommandName))
			//	label_ArgumentsExample.Content = "Press TAB to initiate mode for " + tmpCommandName;
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
			DependencyObject dp = (sender as Button).Parent;//Border).Parent;
			if (dp != null)
			{
				//TextBox actualtextbox = ((dp as DockPanel).Children[1] as TextBox);
				AutoCompleteBox autocompleteBox = (dp as DockPanel).Children[1] as AutoCompleteBox;
				autocompleteBox.Text = "";
				autocompleteBox.SelectedItem = null;
				autocompleteBox.UpdateLayout();
				autocompleteBox.IsDropDownOpen = true;
				if (!autocompleteBox.IsFocused) autocompleteBox.Focus();
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
			if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
			{
				((System.Windows.Controls.AutoCompleteBox)sender).Text += Environment.NewLine;
				//e.Handled = true;//Just handle it so we get the newline character
				//PressEnterKeyInsideArgumentTextbox();
			}
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
			//InlineCommands.CommandsManagerClass.ICommandWithHandler activeCommand;
			ICommandWithHandler activeCommand;
			if (GetActiveCommand(out activeCommand))
			{
				TextBox actualTextBox = GetActualTextBoxOfAutocompleteControl();
				if (activeCommand != null)
				{
					actualTextBox.SelectionLength = 0;
					actualTextBox.SelectionStart = actualTextBox.Text.Length;
				}

				ListBox lb = GetEmbeddedListbox();
				if (lb.Items.Count == 0) return;
				foreach (object lbo in lb.Items)
				{
					ListBoxItem lbi = (ListBoxItem)lb.ItemContainerGenerator.ContainerFromItem(lbo);
					DataTemplate dataTemplate = lbi.ContentTemplate;
					Border dataTemplateBorder = VisualTreeHelper.GetChild(lbi, 0) as Border;
					ContentPresenter contentPresenter = dataTemplateBorder.Child as ContentPresenter;
					AutoCompleteBox acb = dataTemplate.FindName("ArgumentText", contentPresenter) as AutoCompleteBox;
					acb.IsDropDownOpen = false;
					//GetAutocompleteBoxOfArgument(lbo).IsDropDownOpen = false;
					//TextBox buildTask = GetActualTextboxOfArgument(lbo);
					//buildTask.SelectionLength = 0;
					//buildTask.SelectionStart = buildTask.Text.Length;

					//AutoCompleteBox buildTask = GetAutocompleteBoxOfArgument(lbo);
					//BindingExpression be = buildTask.GetBindingExpression(AutoCompleteBox.TextProperty);//TextBox.TextProperty);
					//be.UpdateSource();
				}

				if (CommandsManagerClass.PerformCommandFromCurrentArguments(
						activeCommand,
						textFeedbackEvent,
						progressChangedEvent))
					textBox_CommandLine.Text = "";//.Clear();
			}
		}

		private void MainAutoCompleteTextbox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Control)
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
				if (actualTextBox != null)
				{
					actualTextBox.SelectionLength = 0;
					actualTextBox.SelectionStart = actualTextBox.Text.Length;
				}

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
					//if (!GetActualTextBoxOfAutocompleteControl().IsFocused) GetActualTextBoxOfAutocompleteControl().Focus();
					FocusActualTextboxOfAutocompleteControl();
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
			else if (e.Key == Key.Down || e.SystemKey == Key.Down)
			{
				if (Keyboard.Modifiers == ModifierKeys.Alt)
				{
					e.Handled = true;
					MoveWindowVertical(PixelsToMove);
				}
			}
			else if (e.Key == Key.Up || e.SystemKey == Key.Up)
			{
				if (Keyboard.Modifiers == ModifierKeys.Alt)
				{
					e.Handled = true;
					MoveWindowVertical(-PixelsToMove);
				}
			}
			else if (e.Key == Key.Left || e.SystemKey == Key.Left)
			{
				if (Keyboard.Modifiers == ModifierKeys.Alt)
				{
					e.Handled = true;
					MoveWindowHorizontal(-PixelsToMove);
				}
			}
			else if (e.Key == Key.Right || e.SystemKey == Key.Right)
			{
				if (Keyboard.Modifiers == ModifierKeys.Alt)
				{
					e.Handled = true;
					MoveWindowHorizontal(PixelsToMove);
				}
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

		const int PixelsToMove = 20;
		private void MoveWindowHorizontal(int pixels)
		{
			Window parentWindow = Window.GetWindow(this);
			if (pixels < 0)
			{
				double workingAreaLeftBound = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)parentWindow.Left, (int)parentWindow.Top)).WorkingArea.Left;
				if (parentWindow.Left + pixels >= workingAreaLeftBound)
					parentWindow.Left += pixels;
				else
					parentWindow.Left = workingAreaLeftBound;
			}
			else
			{
				double workingAreaRightBound = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)parentWindow.Left, (int)parentWindow.Top)).WorkingArea.Right;
				if (parentWindow.Left + parentWindow.ActualWidth + pixels <= workingAreaRightBound)
					parentWindow.Left += pixels;
				else
					parentWindow.Left = workingAreaRightBound - parentWindow.ActualWidth;
			}
		}

		private void MoveWindowVertical(int pixels)
		{
			Window parentWindow = Window.GetWindow(this);

			if (pixels < 0)
			{
				double workingAreaTopBound = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)parentWindow.Left, (int)parentWindow.Top)).WorkingArea.Top;
				if (parentWindow.Top + pixels >= workingAreaTopBound)
					parentWindow.Top += pixels;
				else
					parentWindow.Top = workingAreaTopBound;
			}
			else
			{
				double workingAreaBottomBound = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)parentWindow.Left, (int)parentWindow.Top)).WorkingArea.Bottom;
				if (parentWindow.Top + parentWindow.ActualHeight + pixels <= workingAreaBottomBound)
					parentWindow.Top += pixels;
				else
					parentWindow.Top = workingAreaBottomBound - parentWindow.ActualHeight;
			}
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


				try { ScrollIfNeeded(e.GetPosition(treeView_CommandList)); }
				catch { }
			}
		}

		ScrollViewer t_scrollViewer;
		private ScrollViewer ScrollViewer
		{
			get
			{
				if (t_scrollViewer == null)
				{
					DependencyObject border = VisualTreeHelper.GetChild(treeView_CommandList, 0);
					if (border != null)
					{
						t_scrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
					}
				}

				return t_scrollViewer;
			}
		}

		internal int scrollOffsetIncrement = 7;
		internal void ScrollIfNeeded(Point mouseLocation)
		{
			if (this.ScrollViewer != null)
			{
				double scrollOffset = 0.0;

				// See if we need to scroll down 
				if (this.ScrollViewer.ViewportHeight - mouseLocation.Y < 20.0)
					scrollOffset = scrollOffsetIncrement;
				else if (mouseLocation.Y < 20.0)
					scrollOffset = -scrollOffsetIncrement;

				// Scroll the tree down or up 
				if (scrollOffset != 0.0)
				{
					scrollOffset += this.ScrollViewer.VerticalOffset;
					if (scrollOffset < 0.0)
						scrollOffset = 0.0;
					else if (scrollOffset > this.ScrollViewer.ScrollableHeight)
						scrollOffset = this.ScrollViewer.ScrollableHeight;
					this.ScrollViewer.ScrollToVerticalOffset(scrollOffset);
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

		private void listBoxUnsetProperties_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 1)
			{
				if (e.AddedItems[0] is UnsetPropertyDetail)
				{
					UnsetPropertyDetail uspd = e.AddedItems[0] as PropertyInterceptor.UnsetPropertyDetail;

					PropertyInfo[] declaredClassProperties = uspd.PropertyInfo.DeclaringType.GetProperties(BindingFlags.Static | BindingFlags.Public);
					foreach (PropertyInfo dcp in declaredClassProperties)
					{
						if (uspd.PropertyInfo.DeclaringType == dcp.PropertyType)
							uspd.PropertyInfo.GetValue(dcp.GetValue(null, new object[0]), new object[0]);
					}

					ListBoxItem lbi = listBoxUnsetProperties.ItemContainerGenerator.ContainerFromItem(e.AddedItems[0]) as ListBoxItem;
					if (lbi != null)
						lbi.IsSelected = false;
				}
				//ListBoxItem lbi = listBoxUnsetProperties.ItemContainerGenerator.ContainerFromItem(e.AddedItems[0]) as ListBoxItem;
				//if (lbi is 
			}
		}

		private void ArgumentText_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Mouse.LeftButton == MouseButtonState.Pressed)
			{
				(sender as AutoCompleteBox).IsDropDownOpen = false;
				//Figure out why this next commented line does not focus next argument when selecting a dropdownitem with the mouse
				//FocusNextCommandArgument();
			}
			//BindingExpression be = (sender as AutoCompleteBox).GetBindingExpression(AutoCompleteBox.TextProperty);
			//be.UpdateSource();
			//be.UpdateTarget();
		}

		private void treeView_CommandList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
		}		

		private void DockPanel_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			(sender as DockPanel).ContextMenu.IsOpen = true;
		}

		public static void ShowAndEditAllSettings()
		{
			List<object> objList = new List<object>();
			foreach (Type type in typeof(GlobalSettings).GetNestedTypes(BindingFlags.Public))
				if (!type.IsAbstract && type.BaseType == typeof(GenericSettings))//Get all settings classes
				{
					PropertyInfo[] staticProperties = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
					foreach (PropertyInfo spi in staticProperties)
						if (type == spi.PropertyType)//Check to find the static "Instance" of the class
							objList.Add(spi.GetValue(null, new object[0]));
				}

			//objList.Add(TempClass.Instance);
			PropertiesEditor pe = new PropertiesEditor(objList);
			pe.ShowDialog();
			pe = null;
			objList.Clear();
			objList = null;
		}
		public static void ShowAndEditAllOnlineSettings()
		{
			List<object> objList = new List<object>();

			Dictionary<IInterceptorNotifiable, Dictionary<PropertyInfo, object>> objectsAndPropertyValues = new Dictionary<IInterceptorNotifiable, Dictionary<PropertyInfo, object>>();

			foreach (Type type in typeof(SettingsSimple).GetNestedTypes(BindingFlags.Public))
				if (!type.IsAbstract && type.BaseType.Name == typeof(SettingsSimple.BaseOnlineClass<>).Name)//Get all settings classes
				{
					PropertyInfo[] staticProperties = type.BaseType.GetProperties(BindingFlags.Static | BindingFlags.Public);
					foreach (PropertyInfo spi in staticProperties)
						if (type == spi.PropertyType)//Check to find the static "Instance" of the class
						{
							var tmpobj = (IInterceptorNotifiable)spi.GetValue(null, new object[0]);
							objList.Add(tmpobj);

							var tmpPropertyValues = new Dictionary<PropertyInfo, object>();
							foreach (var prop in tmpobj.GetType().GetProperties())
							{
								tmpPropertyValues.Add(prop, prop.GetValue(tmpobj, new object[0]).Clone());
							}
							objectsAndPropertyValues.Add(tmpobj, tmpPropertyValues);
						}
				}
			PropertiesEditor pe = new PropertiesEditor(objList);
			pe.ShowDialog();
			pe = null;
			objList.Clear();
			objList = null;

			//Check if any of the properties changed.
			var keys = objectsAndPropertyValues.Keys.ToArray();
			var values = objectsAndPropertyValues.Values.ToArray();
			for (int i = 0; i < keys.Length; i++)
			{
				foreach (var prop in values[i].Keys)
				{
					object tmpobj = values[i][prop];
					object tmpobj2 = prop.GetValue(keys[i], new object[0]);

					string err;
					ComparisonResult compareResult = CompareObjects.CompareObjectsByValue(tmpobj, tmpobj2, out err);
					switch (compareResult)
					{
						case ComparisonResult.NullValue:
							UserMessages.ShowWarningMessage(string.Format("Cannot compare values, one/both null values. Obj1 = '{0}', Obj2 = '{1}'", (tmpobj == null ? "[NULL]" : tmpobj.ToString()), (tmpobj2 == null ? "[NULL]" : tmpobj2.ToString())));
							break;
						case ComparisonResult.DifferentTypes:
							UserMessages.ShowWarningMessage(string.Format("Cannot compare different types of '{0}' and '{1}'", tmpobj.GetType().ToString(), tmpobj2.GetType().ToString()));
							break;
						case ComparisonResult.Equal:
							//UserMessages.ShowInfoMessage("Equal");
							break;
						case ComparisonResult.NotEqual:
							//UserMessages.ShowWarningMessage("Changed: " + prop.Name);
							keys[i].OnPropertySet(prop.Name);
							break;
						case ComparisonResult.UnsupportedType:
							UserMessages.ShowWarningMessage(
								string.Format("Type unsupported for comparison, either Obj1 = '{0}' or Obj2 = '{1}', error message:{2}{3}",
									tmpobj.GetType().ToString(),
									tmpobj2.GetType().ToString(),
									Environment.NewLine,
									err));
							break;
					}
				}
			}
		}

		private void labelEditSettings_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			ShowAndEditAllSettings();
		}

		private void Grid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			HideParentWindow();
		}

		private void MinimizeToTrayUsercontrolButton_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
		}

		private void CloseUsercontrolButton_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
		}

		private void labelEditOnlineSettings_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			ShowAndEditAllOnlineSettings();
		}

		//private void Button_Click(object sender, RoutedEventArgs e)
		//{
		//	CustomBalloonTipwpf.ShowCustomBalloonTip(
		//		"This is my title",
		//		"This is the body of the message",
		//		3000,
		//		CustomBalloonTipwpf.IconTypes.Error);
		//}

		//private bool IsTreeViewDragBusy = false;
	}

	public class tmp : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (!(value is bool) || (bool)value == false)
				return Visibility.Collapsed;
			return true;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class ZeroCollapsedNonZeroVisibleConverter : IValueConverter
	{

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var rv = Visibility.Visible;
			var val = 0;
			int.TryParse(value.ToString(), out val);
			if (val == 0)
			{
				rv = Visibility.Collapsed;
			}
			return rv;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	/*public class TrueVisibleFalseCollapsedConverter : IValueConverter
	{

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return (bool)value ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}*/

	public class NumberUnreadItemsToTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null || !(value is Dictionary<TextFeedbackType, int>))
				return null;
			Dictionary<TextFeedbackType, int> tmpdict = value as Dictionary<TextFeedbackType, int>;
			bool NonZeroFound = false;
			foreach (TextFeedbackType fbt in tmpdict.Keys)
				if (tmpdict[fbt] > 0)
				{
					NonZeroFound = true;
					break;
				}
			if (!NonZeroFound)
				return null;

			//return "*";
			//DONE: The following text will only contain errors, noteworthy, success, subtle. Ensure this always adds other values as well

			//List<string> tmplist = new List<string>();
			//foreach (TextFeedbackType ft in Enum.GetValues(typeof(TextFeedbackType)))
			//	tmplist.Add(ft.ToString() + " = " + tmpdict[ft].ToString());
			//return tmplist;

			string tmpstr = "Unread messages count";
			foreach (TextFeedbackType ft in Enum.GetValues(typeof(TextFeedbackType)))
				tmpstr += Environment.NewLine + "  " + ft.ToString() + " = " + tmpdict[ft].ToString();
			return tmpstr;//string.Format("  {0},{1},{2},{3}", tmpdict[TextFeedbackType.Error], tmpdict[TextFeedbackType.Noteworthy], tmpdict[TextFeedbackType.Success], tmpdict[TextFeedbackType.Subtle]);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class UnreadItemsStartVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null || !(value is Dictionary<TextFeedbackType, int>))
				return Visibility.Collapsed;
			Dictionary<TextFeedbackType, int> tmpdict = value as Dictionary<TextFeedbackType, int>;
			foreach (TextFeedbackType fbt in tmpdict.Keys)
				if (tmpdict[fbt] > 0)
					return Visibility.Visible;
			return Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	//public class MessagesParagraphListToText : System.Windows.Data.IValueConverter
	//{

	//	public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	//	{
	//		if (!(value is ObservableCollection<MessagesParagraph>))
	//			return "Cannot convert paragraph list to string";

	//		string tmpstr = "";
	//		ObservableCollection<MessagesParagraph> tmplist = value as ObservableCollection<MessagesParagraph>;
	//		foreach (MessagesParagraph s in tmplist)
	//			foreach (Inline inline in s.Inlines)
	//				if (inline is Run)
	//					tmpstr += (tmpstr.Length > 0 ? ";" : "") + (inline as Run).Text;
	//		return tmpstr;
	//		//var rv = Visibility.Visible;
	//		//var val = 0;
	//		//int.TryParse(value.ToString(), out val);
	//		//if (val == 0)
	//		//{
	//		//	rv = Visibility.Collapsed;
	//		//}
	//		//return rv;
	//	}

	//	public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	//	{
	//		throw new NotImplementedException();
	//	}
	//}

	//public class RichTextBoxHelper : DependencyObject
	//{
	//	public static string GetDocumentXaml(DependencyObject obj)
	//	{
	//		return (string)obj.GetValue(DocumentXamlProperty);
	//	}
	//	public static void SetDocumentXaml(DependencyObject obj, string value)
	//	{
	//		obj.SetValue(DocumentXamlProperty, value);
	//	}
	//	public static readonly DependencyProperty DocumentXamlProperty = 
	//	DependencyProperty.RegisterAttached(
	//			"DocumentXaml",
	//			typeof(string),
	//			typeof(RichTextBoxHelper),
	//			new FrameworkPropertyMetadata
	//			{
	//				BindsTwoWayByDefault = true,
	//				PropertyChangedCallback = (obj, e) =>
	//				{
	//					var richTextBox = (RichTextBox)obj;

	//					// Parse the XAML to a document (or use XamlReader.Parse())
	//					var xaml = GetDocumentXaml(richTextBox);
	//					var doc = new FlowDocument();
	//					var range = new TextRange(doc.ContentStart, doc.ContentEnd);

	//					range.Load(new MemoryStream(Encoding.UTF8.GetBytes(xaml)),
	//						DataFormats.Xaml);

	//					// Set the document
	//					richTextBox.Document = doc;

	//					// When the document changes update the source
	//					range.Changed += (obj2, e2) =>
	//					{
	//						if (richTextBox.Document == doc)
	//						{
	//							MemoryStream buffer = new MemoryStream();
	//							range.Save(buffer, DataFormats.Xaml);
	//							SetDocumentXaml(richTextBox,
	//								Encoding.UTF8.GetString(buffer.ToArray()));
	//						}
	//					};
	//				}
	//			});
	//}
}
