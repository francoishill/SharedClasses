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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Reflection;
using System.IO;
using System.Diagnostics;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class OverlayWindow : Window
{
	public class OverlayChildManager
	{
		public bool AllowShow;
		public bool EventAttached;
		public bool WindowAlreadyPositioned;
		public UIElement FirstUIelementToFocus;

		public OverlayChildManager(bool AllowShowIn, bool EventAttachedIn, bool WindowAlreadyPositionedIn, UIElement FirstUIelementToFocusIn)
		{
			AllowShow = AllowShowIn;
			EventAttached = EventAttachedIn;
			WindowAlreadyPositioned = WindowAlreadyPositionedIn;
			FirstUIelementToFocus = FirstUIelementToFocusIn;
		}
	}

	public List<CommandUserControl> ListOfCommandUsercontrols = new List<CommandUserControl>();

	public OverlayWindow()
	{
		InitializeComponent();
		System.Windows.Forms.Application.EnableVisualStyles();
		System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(this);
	}

	private Element currentElement = new Element();
	private void overlayWindow_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left && currentElement.InputElement == null)
			this.Close();

		currentElement.InputElement = null;
	}

	public void SetupAllChildWindows()
	{
		foreach (CommandUserControl usercontrol in ListOfCommandUsercontrols)
		{
			if (usercontrol.Tag == null)
				usercontrol.Tag = new OverlayChildManager(true, false, false, null);

			if (this.wpfDraggableCanvas1.Children.IndexOf(usercontrol) == -1)
				this.wpfDraggableCanvas1.Children.Add(usercontrol);

			if (MayUsercontrolBeShown(usercontrol))
			{
				usercontrol.Visibility = Visibility.Visible;
				//usercontrol.currentFocusedElement.Focus();
			}
			else usercontrol.Visibility = Visibility.Hidden;
		}

		System.Windows.Forms.Application.DoEvents();
		AutoLayoutOfForms();
		if (ListOfCommandUsercontrols.Count > 0)
			ListOfCommandUsercontrols[0].currentFocusedElement.Focus();
	}

	public void AddEventsToAllChildUsercontrols()
	{
		foreach (CommandUserControl usercontrol in ListOfCommandUsercontrols)
		{
			if (!IsEventsAdded(usercontrol))
			{
				AddClosingEventToUsercontrol(usercontrol);
				AddMouseLeftButtonDownEventToCommandUsercontrol(usercontrol);
				AddMouseLeftButtonDownEventToChildren(usercontrol);
				AddMouseWheelEventToWindow(usercontrol);
				AddKeydownEventToWindowAndChildren(usercontrol);
				AddDropEventToUsercontrol(usercontrol);

				MarkformEventsAdded(usercontrol);
			}
		}
	}

	private void AddMouseLeftButtonDownEventToCommandUsercontrol(CommandUserControl usercontrol)
	{
		usercontrol.MouseLeftButtonDown += (s, closeargs) =>
		{
			this.currentElement.InputElement = (IInputElement)s;
			SetFocusToNewUsercontrol(usercontrol);
			Console.WriteLine("CommandUsercontrol MouseLeftButtonDown");
			//DONE TODO: DragMove to be implemented for usercontrol because its not window anymore
		};
	}

	public void SetFocusToNewUsercontrol(CommandUserControl usercontrolToFocus, bool forceSetFocus = false)
	{
		//Console.WriteLine("currentActiveUsercontrol == usercontrolToFocus: " + (currentActiveUsercontrol == usercontrolToFocus));
		if (currentActiveUsercontrol != usercontrolToFocus || forceSetFocus)
		{
			if (currentActiveUsercontrol != usercontrolToFocus && currentActiveUsercontrol != null)
				currentActiveUsercontrol.DeactivateControl();
			currentActiveUsercontrol = usercontrolToFocus;
			usercontrolToFocus.currentFocusedElement.Focus();
			usercontrolToFocus.ActivateControl();			
		}
	}

	public CommandUserControl currentActiveUsercontrol = null;
	private void AddMouseLeftButtonDownEventToChildren(CommandUserControl usercontrol)
	{
		List<object> totalChildList = new List<object>();
		System.Collections.IEnumerable gridCustomArguments_ChildList = LogicalTreeHelper.GetChildren(usercontrol.gridCustomArguments);
		System.Collections.IEnumerable treeViewPredefinedArguments_ChildList = LogicalTreeHelper.GetChildren(usercontrol.expanderContainingTreeview);
		foreach (object o in gridCustomArguments_ChildList) totalChildList.Add(o);
		foreach (object o in treeViewPredefinedArguments_ChildList) totalChildList.Add(o);

		foreach (object o in totalChildList)
			if (o is UIElement && (o as UIElement).Focusable)
			{
				(o as UIElement).MouseLeftButtonDown += (sendr, evtargs) =>
				{
					usercontrol.currentFocusedElement = sendr as UIElement;
					SetFocusToNewUsercontrol(usercontrol);
					//evtargs.Handled = true;
				};
				if (o is TreeView)
				{
					foreach (TreeViewItem item in (o as TreeView).Items)
					{
						Console.WriteLine(item.Header.ToString());
						item.PreviewMouseLeftButtonDown += (sendr, evtargs) =>
						{
							usercontrol.currentFocusedElement = o as UIElement;
							SetFocusToNewUsercontrol(usercontrol);
						};
					}
				}
			}
	}

	private void AddMouseWheelEventToWindow(Control usercontrol)
	{
		usercontrol.MouseWheel += (s, evtargs) =>
		{
			if (evtargs.Delta > 0) ActivatePreviousWindowInChildList(FindCommandUsercontrolOfControl(s as System.Windows.Controls.Control));
			else ActivateNextWindowInChildList(FindCommandUsercontrolOfControl(s as System.Windows.Controls.Control));
		};
	}

	private bool IsWindowAlreadyPositioned(CommandUserControl usercontrol)//Form form)
	{
		if (usercontrol == null) return false;
		if (!(usercontrol.Tag is OverlayChildManager)) return false;
		return (usercontrol.Tag as OverlayChildManager).WindowAlreadyPositioned;
	}

	private void MarkfWindowAsAlreadyPositioned(CommandUserControl usercontrol)// Form form)
	{
		if (usercontrol == null) return;
		if (!(usercontrol.Tag is OverlayChildManager)) return;
		(usercontrol.Tag as OverlayChildManager).WindowAlreadyPositioned = true;
	}

	private void MarkfWindowNOTAlreadyPositioned(CommandUserControl usercontrol)// Form form)
	{
		if (usercontrol == null) return;
		if (!(usercontrol.Tag is OverlayChildManager)) return;
		(usercontrol.Tag as OverlayChildManager).WindowAlreadyPositioned = false;
	}

	private double GetActualWidthConsiderScaling(Control control)
	{
		if (control.LayoutTransform == null || (control.LayoutTransform is MatrixTransform && control.RenderTransform is MatrixTransform)) return control.ActualWidth;
		else if (control.LayoutTransform is ScaleTransform && control.RenderTransform is ScaleTransform) return control.ActualWidth * (control.LayoutTransform as ScaleTransform).ScaleX * (control.RenderTransform as ScaleTransform).ScaleX;
		else if (control.LayoutTransform is ScaleTransform) return control.ActualWidth * (control.LayoutTransform as ScaleTransform).ScaleX;
		else if (control.RenderTransform is ScaleTransform) return control.ActualWidth * (control.RenderTransform as ScaleTransform).ScaleX;
		else return control.ActualWidth;
	}

	private double GetActualHeightConsiderScaling(Control control)
	{
		if (control.LayoutTransform == null || (control.LayoutTransform is MatrixTransform && control.RenderTransform is MatrixTransform)) return control.ActualHeight;
		else if (control.LayoutTransform is ScaleTransform && control.RenderTransform is ScaleTransform) return control.ActualHeight * (control.LayoutTransform as ScaleTransform).ScaleY * (control.RenderTransform as ScaleTransform).ScaleY;
		else if (control.LayoutTransform is ScaleTransform) return control.ActualHeight * (control.LayoutTransform as ScaleTransform).ScaleY;
		else if (control.RenderTransform is ScaleTransform) return control.ActualHeight * (control.RenderTransform as ScaleTransform).ScaleY;
		else return control.ActualHeight;
	}

	private void AutoLayoutOfForms(int startIndex = 0, bool ForceDoLayoutForFollowingControls = false)
	{
		int leftGap = 20;
		int topGap = 10;

		int NextLeftPos = leftGap;
		int MaxHeightInRow = 0;
		int NextTopPos = topGap;

		System.Drawing.Rectangle workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;

		for (int i = 0; i < startIndex; i++)
		{
			CommandUserControl usercontrol = ListOfCommandUsercontrols[i];
			if (NextLeftPos + GetActualWidthConsiderScaling(usercontrol) + leftGap >= workingArea.Right)
			{
				NextTopPos += MaxHeightInRow + topGap;
				NextLeftPos = leftGap;
				MaxHeightInRow = 0;
			}
			NextLeftPos += (int)GetActualWidthConsiderScaling(usercontrol) + leftGap;
			if (GetActualHeightConsiderScaling(usercontrol) > MaxHeightInRow) MaxHeightInRow = (int)GetActualHeightConsiderScaling(usercontrol);
		}

		for (int i = startIndex; i < ListOfCommandUsercontrols.Count; i++)
		{
			CommandUserControl usercontrol = ListOfCommandUsercontrols[i];
			if (NextLeftPos + GetActualWidthConsiderScaling(usercontrol) + leftGap >= workingArea.Right)
			{
				NextTopPos += MaxHeightInRow + topGap;
				NextLeftPos = leftGap;
				MaxHeightInRow = 0;
			}

			if (ForceDoLayoutForFollowingControls || !IsWindowAlreadyPositioned(usercontrol))
			{
				usercontrol.Margin = new Thickness(NextLeftPos, NextTopPos, 0, 0);
				usercontrol.autoLayoutMaginBeforeAnimation = usercontrol.Margin;
				MarkfWindowAsAlreadyPositioned(usercontrol);
			}
			NextLeftPos += (int)GetActualWidthConsiderScaling(usercontrol) + leftGap;
			if (GetActualHeightConsiderScaling(usercontrol) > MaxHeightInRow) MaxHeightInRow = (int)GetActualHeightConsiderScaling(usercontrol);
		}
	}

	private void AddKeydownEventToWindowAndChildren(Control usercontrol)
	{
		usercontrol.KeyDown += new System.Windows.Input.KeyEventHandler(control_KeyDown1);
		//foreach (object o in LogicalTreeHelper.GetChildren(usercontrol))
		//  if (o is System.Windows.Controls.Control)
		//  {
		//    (o as System.Windows.Controls.Control).KeyUp += new System.Windows.Input.KeyEventHandler(control_KeyDown1);
		//  }
	}

	private void AddDropEventToUsercontrol(CommandUserControl usercontrol)
	{
		//usercontrol.Drop += new DragEventHandler(usercontrol_Drop);
		foreach (object o in LogicalTreeHelper.GetChildren(usercontrol.mainGrid))
			if (o is System.Windows.Controls.Control)
			{
				//if (o is TextBox) System.Windows.Forms.MessageBox.Show("Test");
				//else System.Windows.Forms.MessageBox.Show(o.GetType().ToString());
				(o as System.Windows.Controls.Control).AllowDrop = true;
				(o as System.Windows.Controls.Control).Drop += new DragEventHandler(usercontrol_Drop);
			}
	}

	void usercontrol_Drop(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
		{
			string commandName = (sender as CommandUserControl).labelTitle.Content.ToString().ToLower();
			e.Handled = true;
			string[] filesDropped = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
			if ((new string[] { "cmd", "vscmd" }).Any(s => commandName == s) && InlineCommands.CommandList.ContainsKey(commandName)
				&& Directory.Exists(filesDropped[0]))
			{
				System.Windows.Forms.ComboBox tmpCombobox = new System.Windows.Forms.ComboBox();
				System.Windows.Forms.TextBox tmpTextbox = new System.Windows.Forms.TextBox();
				InlineCommands.CommandList[commandName].PerformCommand(commandName + " " + filesDropped[0], tmpCombobox, tmpTextbox);
				tmpCombobox.Dispose();
				tmpTextbox.Dispose();
				tmpCombobox = null;
				tmpTextbox = null;
				this.Close();
			}
			else
			{
				foreach (string filedropped in filesDropped)
					if (File.Exists(filedropped))
						UserMessages.ShowInfoMessage("File " + filedropped + " was dropped onto " + (sender as CommandUserControl).labelTitle.Content.ToString());
					else if (Directory.Exists(filedropped))
						UserMessages.ShowInfoMessage("Folder " + filedropped + " was dropped onto " + (sender as CommandUserControl).labelTitle.Content.ToString());
					else UserMessages.ShowWarningMessage("File/folder not found: " + filedropped);
			}
		}
	}

	void control_KeyDown1(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			this.Activate();
		}

		if (e.Key == System.Windows.Input.Key.Tab && System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Control)
		{
			e.Handled = true;
			ActivateNextWindowInChildList(FindCommandUsercontrolOfControl(sender as CommandUserControl));
		}
		else if (e.Key == System.Windows.Input.Key.Tab && (System.Windows.Forms.Control.ModifierKeys & (System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)) == (System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift))
		{
			e.Handled = true;
			ActivatePreviousWindowInChildList(FindCommandUsercontrolOfControl(sender as CommandUserControl));
		}
	}

	private void ActivatePreviousWindowInChildList(CommandUserControl usercontrol)
	{
		if (ListOfCommandUsercontrols != null && ListOfCommandUsercontrols.IndexOf(usercontrol) != -1)
		{
			int currentActiveFormIndex = ListOfCommandUsercontrols.IndexOf(usercontrol);
			int newIndexToActivate = currentActiveFormIndex == 0 ? ListOfCommandUsercontrols.Count - 1 : currentActiveFormIndex - 1;
			SetFocusToNewUsercontrol(ListOfCommandUsercontrols[newIndexToActivate]);
			//ListOfCommandUsercontrols[newIndexToActivate].Focus();
			//ListOfCommandUsercontrols[newIndexToActivate].currentFocusedElement.Focus();
		}
		else if (ListOfCommandUsercontrols != null)
			SetFocusToNewUsercontrol(ListOfCommandUsercontrols[0]);
		//ListOfCommandUsercontrols[0].Focus();
	}

	private void ActivateNextWindowInChildList(CommandUserControl usercontrol)
	{
		if (ListOfCommandUsercontrols != null && ListOfCommandUsercontrols.IndexOf(usercontrol) != -1)
		{
			int currentActiveFormIndex = ListOfCommandUsercontrols.IndexOf(usercontrol);
			int newIndexToActivate = currentActiveFormIndex == ListOfCommandUsercontrols.Count - 1 ? 0 : currentActiveFormIndex + 1;
			SetFocusToNewUsercontrol(ListOfCommandUsercontrols[newIndexToActivate]);
			//ListOfCommandUsercontrols[newIndexToActivate].Focus();
			//ListOfCommandUsercontrols[newIndexToActivate].currentFocusedElement.Focus();
		}
		else if (ListOfCommandUsercontrols != null)
			//ListOfCommandUsercontrols[0].Focus();
			SetFocusToNewUsercontrol(ListOfCommandUsercontrols[0]);
	}

	private CommandUserControl FindCommandUsercontrolOfControl(System.Windows.Controls.Control control)
	{
		System.Windows.Controls.Control tmpControl = control;
		while (!(tmpControl is CommandUserControl))
			tmpControl = (System.Windows.Controls.Control)tmpControl.Parent;
		return tmpControl as CommandUserControl;
	}

	private void AddClosingEventToUsercontrol(CommandUserControl usercontrol)//Form form)
	{
		usercontrol.AnimationComplete_Closing += (snder, evtargs) =>
		{
			//AutoLayoutOfForms(ListOfCommandUsercontrols.IndexOf(snder as CommandUserControl) + 1, true);
		};
		usercontrol.AnimationComplete_Activating += (snder, evtargs) =>
		{
			//AutoLayoutOfForms(ListOfCommandUsercontrols.IndexOf(snder as CommandUserControl) + 1, true);
		};
	}

	private void SetFormAllowShow(System.Windows.Forms.Form form, bool allowShowValue)
	{
		if (form == null) return;
		if (!(form.Tag is OverlayChildManager)) return;
		(form.Tag as OverlayChildManager).AllowShow = allowShowValue;
	}

	private bool MayUsercontrolBeShown(CommandUserControl usercontrol)// Form form)
	{
		if (usercontrol == null) return true;
		if (!(usercontrol.Tag is OverlayChildManager)) return true;
		return (usercontrol.Tag as OverlayChildManager).AllowShow;
	}

	private bool IsEventsAdded(Control usercontrol)//Form form)
	{
		if (usercontrol == null) return false;
		if (!(usercontrol.Tag is OverlayChildManager)) return false;
		return (usercontrol.Tag as OverlayChildManager).EventAttached;
	}

	private void MarkformEventsAdded(Control usercontrol)// Form form)
	{
		if (usercontrol == null) return;
		if (!(usercontrol.Tag is OverlayChildManager)) return;
		(usercontrol.Tag as OverlayChildManager).EventAttached = true;
	}

	public bool PreventClosing = true;
	private void overlayWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
	{
		if (PreventClosing) e.Cancel = true;
		this.Hide();
		//foreach (CommandUserControl usercontrol in ListOfCommandUsercontrols)
		//  if (usercontrol != null)
		//  {
		//    usercontrol.Owner = null;
		//    usercontrol.Hide();
		//  }
	}

	private void overlayWindow_Activated(object sender, EventArgs e)
	{
		this.Topmost = true;
		this.Topmost = false;
	}

	private bool IsOnlyControlModifierDown()
	{
		return System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Control;
	}

	private bool IsOnlyControlShiftModifiersDown()
	{
		return (System.Windows.Forms.Control.ModifierKeys & (System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)) == (System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift);
	}

	List<Key> NumberKeys = new List<Key>()
	{
		Key.D1,
		Key.D2,
		Key.D3,
		Key.D4,
		Key.D5,
		Key.D6,
		Key.D7,
		Key.D8,
		Key.D9,
		Key.D0
	};

	private bool IsAnumberKey(Key key)
	{
		return NumberKeys.Contains(key);
	}

	private void overlayWindow_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
			this.Close();
		else if (e.Key == Key.Tab && IsOnlyControlModifierDown())
		{
			if (ListOfCommandUsercontrols != null && ListOfCommandUsercontrols.Count > 0)
				ListOfCommandUsercontrols[0].Focus();
		}
		else if (e.Key == Key.Tab && IsOnlyControlShiftModifiersDown())
		{
			if (ListOfCommandUsercontrols != null && ListOfCommandUsercontrols.Count > 0)
				ListOfCommandUsercontrols[ListOfCommandUsercontrols.Count - 1].Focus();
		}
		else if (IsAnumberKey(e.Key) && IsOnlyControlModifierDown())
		{
			if (ListOfCommandUsercontrols.Count > NumberKeys.IndexOf(e.Key))
				SetFocusToNewUsercontrol(ListOfCommandUsercontrols[NumberKeys.IndexOf(e.Key)]);//.currentFocusedElement.Focus();
		}
	}

	public class Element
	{
		#region Fields
		bool isDragging = false;
		IInputElement inputElement = null;
		double x, y = 0;
		#endregion

		#region Constructor
		public Element() { }
		#endregion

		#region Properties
		public IInputElement InputElement
		{
			get { return this.inputElement; }
			set
			{
				this.inputElement = value;
				/* every time inputElement resets, the draggin stops (you actually don't even need to track it, but it made things easier in the begining, I'll change it next time I get to play with it. */
				this.isDragging = false;
			}
		}

		public double X
		{
			get { return this.x; }
			set { this.x = value; }
		}

		public double Y
		{
			get { return this.y; }
			set { this.y = value; }
		}

		public bool IsDragging
		{
			get { return this.isDragging; }
			set { this.isDragging = value; }
		}
		#endregion
	}

	private void overlayWindow_Drop(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
		{
			e.Handled = true;
			string[] filesDropped = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
			foreach (string filedropped in filesDropped)
				UserMessages.ShowInfoMessage("File " + filedropped + " was dropped onto OverlayWindow");
		}
	}
}