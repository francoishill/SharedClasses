using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
//using System.Windows.Forms;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

public partial class OverlayForm : System.Windows.Forms.Form
{
	//public List<Form> ListOfChildForms = new List<Form>();
	public List<Window> ListOfChildWindows = new List<Window>();

	public OverlayForm()
	{
		InitializeComponent();
	}

	private void OverlayForm_Click(object sender, EventArgs e)
	{
		this.Close();//.Hide();
	}

	public static void tmpQuickCustomBalloonTip(string msg)
	{
		CustomBalloonTip.ShowCustomBalloonTip("Title", msg, 2000, CustomBalloonTip.IconTypes.Information, delegate { });
	}

	private void OverlayForm_Shown(object sender, EventArgs e)
	{
		this.Opacity = 0;
		this.Show();
		//button1.Location = new Point((this.Width - button1.Width) / 2, (this.Height - button1.Height) / 2);
		//ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
		//{
		for (int i = 0; i < 80; i++)
		{
			ThreadingInterop.PerformVoidFunctionSeperateThread(() => { System.Threading.Thread.Sleep(3); });
			ThreadingInterop.UpdateGuiFromThread(this, () =>
			{
				this.Opacity += 0.01;
			});
		}
		//});

		foreach (Window window in ListOfChildWindows)
		{
			if (window.Tag == null)
				window.Tag = new OverlayChildManager(true, false, false);

			window.WindowStyle = WindowStyle.None;
			if (!window.AllowsTransparency) window.AllowsTransparency = true;
			//window.MaximizeBox = false;
			//window.MinimizeBox = false;
			window.ShowInTaskbar = false;
			//form.ShowIcon = false;

			if (!IsEventsAdded(window))//form))
			{
				//AddMouseDownEventToControlandSubcontrols(form);
				AddClosingEventToWindow(window);//form);
				AddMouseLeftButtonDownEventToWindow(window);
				//AddKeydownEventToControlandSubcontrols(form);
				AddKeyupEventToWindowAndChildren(window);

				MarkformEventsAdded(window);
			}
			//form.TopMost = true;
			window.Topmost = true;
			if (MayFormBeShow(window))
			{
				//window.Opacity = 0.75F;
				window.Show();				
				//if (propertyFreezeEvent_Activated != null) propertyFreezeEvent_Activated.SetValue(form, false, null);
			}
		}

		AutoLayoutOfForms();

		/*foreach (Form form in ListOfChildForms)
		{
			form.StartPosition = FormStartPosition.Manual;
			if (NextLeftPos + form.Width + leftGap >= workingArea.Right)
			{
				NextTopPos += MaxHeightInRow + topGap;
				NextLeftPos = leftGap;
				MaxHeightInRow = 0;
			}

			form.Location = new Point(NextLeftPos, NextTopPos);
			NextLeftPos += form.Width + leftGap;
			if (form.Height > MaxHeightInRow) MaxHeightInRow = form.Height;

			if (form.Tag == null)
				form.Tag = new OverlayChildManager(true, false);

			form.FormBorderStyle = Forms.FormBorderStyle.SizableToolWindow;
			form.MaximizeBox = false;
			form.MinimizeBox = false;
			form.ShowInTaskbar = false;
			//form.ShowIcon = false;

			if (!IsEventsAdded(form))
			{
				//AddMouseDownEventToControlandSubcontrols(form);
				AddFormClosingEventToControl(form);
				AddKeydownEventToControlandSubcontrols(form);

				MarkformEventsAdded(form);
			}
			form.TopMost = true;
			if (MayFormBeShow(form))
			{
				form.Opacity = 0.75F;
				PropertyInfo propertyFreezeEvent_Activated = form.GetType().GetProperty("FreezeEvent_Activated");
				if (propertyFreezeEvent_Activated != null) propertyFreezeEvent_Activated.SetValue(form, true, null);
				form.Show();
				//if (propertyFreezeEvent_Activated != null) propertyFreezeEvent_Activated.SetValue(form, false, null);
			}
		}*/
	}

	private void AddMouseLeftButtonDownEventToWindow(Window window)
	{
		window.MouseLeftButtonDown += (s, closeargs) =>
		{
			(s as Window).DragMove();
		};
	}

	private bool IsWindowAlreadyPositioned(Window window)//Form form)
	{
		if (window == null) return false;
		if (!(window.Tag is OverlayChildManager)) return false;
		return (window.Tag as OverlayChildManager).WindowAlreadyPositioned;
	}

	private void MarkfWindowAsAlreadyPositioned(Window window)// Form form)
	{
		if (window == null) return;
		if (!(window.Tag is OverlayChildManager)) return;
		(window.Tag as OverlayChildManager).WindowAlreadyPositioned = true;
	}

	private void AutoLayoutOfForms()
	{
		int leftGap = 20;
		int topGap = 20;

		int NextLeftPos = leftGap;
		int MaxHeightInRow = 0;
		int NextTopPos = topGap;

		Rectangle workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;

		foreach (Window window in ListOfChildWindows)
		{
			window.WindowStartupLocation = WindowStartupLocation.Manual;
			if (NextLeftPos + window.Width + leftGap >= workingArea.Right)
			{
				NextTopPos += MaxHeightInRow + topGap;
				NextLeftPos = leftGap;
				MaxHeightInRow = 0;
			}

			if (!IsWindowAlreadyPositioned(window))
			{
				window.Left = NextLeftPos;
				window.Top = NextTopPos;
				MarkfWindowAsAlreadyPositioned(window);
			}
			NextLeftPos += (int)window.Width + leftGap;
			if (window.Height > MaxHeightInRow) MaxHeightInRow = (int)window.Height;
			
			//PositionBeforeActivated = new Point(this.Left, this.Top);
			PropertyInfo propertyPositionBeforeActivated = window.GetType().GetProperty("PositionBeforeActivated");
			if (propertyPositionBeforeActivated != null) propertyPositionBeforeActivated.SetValue(window, new System.Windows.Point(window.Left, window.Top), null);
			
			PropertyInfo propertyFreezeEvent_Activated = window.GetType().GetProperty("AllowedToAnimationLocation");
			if (propertyFreezeEvent_Activated != null) propertyFreezeEvent_Activated.SetValue(window, true, null);
		}
	}

	private void AddKeyupEventToWindowAndChildren(Window window)
	{
		window.KeyUp += new System.Windows.Input.KeyEventHandler(control_KeyUp1);
		foreach (object o in LogicalTreeHelper.GetChildren(window))
			if (o is System.Windows.Controls.Control)
			{
				(o as System.Windows.Controls.Control).KeyUp += new System.Windows.Input.KeyEventHandler(control_KeyUp1);
			}
	}

	private void AddKeydownEventToControlandSubcontrols(System.Windows.Forms.Control control)
	{
		//if (control is TextBox) (control as TextBox).Multiline = true;
		control.KeyDown += new System.Windows.Forms.KeyEventHandler(control_KeyDown);
		//control.KeyDown += new System.Windows.Input.KeyEventHandler(control_KeyDown1);
		foreach (System.Windows.Forms.Control subcontrol in control.Controls)
			AddKeydownEventToControlandSubcontrols(subcontrol);
	}

	void control_KeyUp1(object sender, System.Windows.Input.KeyEventArgs e)
	{
		//if (e.Key == System.Windows.Input.Key.Tab)
		//  e.Handled = true;// == Keys.Tab) e.Handled = true;

		//if (e.KeyCode == Keys.Tab && ModifierKeys == Keys.Control)
		//if (e.Key == System.Windows.Input.Key.Tab) UserMessages.ShowMessage("Tab");
		if (e.Key == System.Windows.Input.Key.Tab && ModifierKeys == System.Windows.Forms.Keys.None)
		{
			TraversalRequest tRequest = new TraversalRequest(FocusNavigationDirection.Next);
			UIElement keyboardFocus = Keyboard.FocusedElement as UIElement;
			if (keyboardFocus != null)
				keyboardFocus.MoveFocus(tRequest);
		}
		if (e.Key == System.Windows.Input.Key.Tab && ModifierKeys == System.Windows.Forms.Keys.Control)
		{
			//if (ListOfChildForms != null && ListOfChildForms.IndexOf((sender as Control).FindForm()) != -1)
			//{
			//  int currentActiveFormIndex = ListOfChildForms.IndexOf((sender as Control).FindForm());
			//  int newIndexToActivate = currentActiveFormIndex == ListOfChildForms.Count - 1 ? 0 : currentActiveFormIndex + 1;
			//  ListOfChildForms[newIndexToActivate].Activate();
			//}
			if (ListOfChildWindows != null && ListOfChildWindows.IndexOf(FindWindowOfControl(sender as System.Windows.Controls.Control)) != -1)
			{
				int currentActiveFormIndex = ListOfChildWindows.IndexOf(FindWindowOfControl(sender as System.Windows.Controls.Control));
				int newIndexToActivate = currentActiveFormIndex == ListOfChildWindows.Count - 1 ? 0 : currentActiveFormIndex + 1;
				ListOfChildWindows[newIndexToActivate].Activate();
			}
		}
		//else if (e.KeyCode == Keys.Tab && (ModifierKeys & (Keys.Control | Keys.Shift)) == (Keys.Control | Keys.Shift))
		else if (e.Key == System.Windows.Input.Key.Tab && (ModifierKeys & (System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)) == (System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift))
		{
			//if (ListOfChildForms != null && ListOfChildForms.IndexOf((sender as Control).FindForm()) != -1)
			//{
			//  int currentActiveFormIndex = ListOfChildForms.IndexOf((sender as Control).FindForm());
			//  int newIndexToActivate = currentActiveFormIndex == 0 ? ListOfChildForms.Count - 1 : currentActiveFormIndex - 1;
			//  ListOfChildForms[newIndexToActivate].Activate();
			//}
			if (ListOfChildWindows != null && ListOfChildWindows.IndexOf(FindWindowOfControl(sender as System.Windows.Controls.Control)) != -1)
			{
				int currentActiveFormIndex = ListOfChildWindows.IndexOf(FindWindowOfControl(sender as System.Windows.Controls.Control));
				int newIndexToActivate = currentActiveFormIndex == 0 ? ListOfChildWindows.Count - 1 : currentActiveFormIndex - 1;
				ListOfChildWindows[newIndexToActivate].Activate();
			}
		}
	}

	private Window FindWindowOfControl(System.Windows.Controls.Control control)
	{
		System.Windows.Controls.Control tmpControl = control;
		while (!(tmpControl is Window))
			tmpControl = (System.Windows.Controls.Control)tmpControl.Parent;
		return tmpControl as Window;
	}

	private void control_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
	{
		if (e.KeyCode == System.Windows.Forms.Keys.Tab) e.Handled = true;

		if (e.KeyCode == System.Windows.Forms.Keys.Tab && ModifierKeys == System.Windows.Forms.Keys.Control)
		{
			//if (ListOfChildForms != null && ListOfChildForms.IndexOf((sender as Control).FindForm()) != -1)
			//{
			//  int currentActiveFormIndex = ListOfChildForms.IndexOf((sender as Control).FindForm());
			//  int newIndexToActivate = currentActiveFormIndex == ListOfChildForms.Count - 1 ? 0 : currentActiveFormIndex + 1;
			//  ListOfChildForms[newIndexToActivate].Activate();
			//}
			if (ListOfChildWindows != null && ListOfChildWindows.IndexOf(FindWindowOfControl(sender as System.Windows.Controls.Control)) != -1)
			{
				int currentActiveFormIndex = ListOfChildWindows.IndexOf(FindWindowOfControl(sender as System.Windows.Controls.Control));
				int newIndexToActivate = currentActiveFormIndex == ListOfChildWindows.Count - 1 ? 0 : currentActiveFormIndex + 1;
				ListOfChildWindows[newIndexToActivate].Activate();
			}
		}
		else if (e.KeyCode == System.Windows.Forms.Keys.Tab && (ModifierKeys & (System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)) == (System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift))
		{
			//if (ListOfChildForms != null && ListOfChildForms.IndexOf((sender as Control).FindForm()) != -1)
			//{
			//  int currentActiveFormIndex = ListOfChildForms.IndexOf((sender as Control).FindForm());
			//  int newIndexToActivate = currentActiveFormIndex == 0 ? ListOfChildForms.Count - 1 : currentActiveFormIndex - 1;
			//  ListOfChildForms[newIndexToActivate].Activate();
			//}
			if (ListOfChildWindows != null && ListOfChildWindows.IndexOf(FindWindowOfControl(sender as System.Windows.Controls.Control)) != -1)
			{
				int currentActiveFormIndex = ListOfChildWindows.IndexOf(FindWindowOfControl(sender as System.Windows.Controls.Control));
				int newIndexToActivate = currentActiveFormIndex == 0 ? ListOfChildWindows.Count - 1 : currentActiveFormIndex - 1;
				ListOfChildWindows[newIndexToActivate].Activate();
			}
		}
	}

	private void AddClosingEventToWindow(System.Windows.Window window)//Form form)
	{
		//form.FormClosing += (s, closeargs) =>
		window.Closing += (s, closeargs) =>
		{
			System.Windows.Forms.Form thisForm = s as System.Windows.Forms.Form;
			//if (closeargs.CloseReason == CloseReason.UserClosing)
			{
				closeargs.Cancel = true;
				thisForm.Hide();
				SetFormAllowShow(thisForm, false);
				//UserMessages.ShowMessage("Userclose");
			}
		};
	}

	private void SetFormAllowShow(System.Windows.Forms.Form form, bool allowShowValue)
	{
		if (form == null) return;
		if (!(form.Tag is OverlayChildManager)) return;
		(form.Tag as OverlayChildManager).AllowShow = allowShowValue;
	}

	private bool MayFormBeShow(System.Windows.Window window)// Form form)
	{
		if (window == null) return true;
		if (!(window.Tag is OverlayChildManager)) return true;
		return (window.Tag as OverlayChildManager).AllowShow;
	}

	private bool IsEventsAdded(System.Windows.Window window)//Form form)
	{
		if (window == null) return false;
		if (!(window.Tag is OverlayChildManager)) return false;
		return (window.Tag as OverlayChildManager).EventAttached;
	}

	private void MarkformEventsAdded(System.Windows.Window window)// Form form)
	{
		if (window == null) return;
		if (!(window.Tag is OverlayChildManager)) return;
		(window.Tag as OverlayChildManager).EventAttached = true;
	}

	private void AddMouseDownEventToControlandSubcontrols(System.Windows.Forms.Control control)
	{
		control.MouseDown += new System.Windows.Forms.MouseEventHandler(form_MouseDown);
		foreach (System.Windows.Forms.Control subcontrol in control.Controls)
			AddMouseDownEventToControlandSubcontrols(subcontrol);
	}

	private void form_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
	{
		if (e.Button == System.Windows.Forms.MouseButtons.Left)
		{
			Win32Api.ReleaseCapture();
			if (!(sender is System.Windows.Forms.TextBox) && !(sender is System.Windows.Forms.RichTextBox))
				Win32Api.SendMessage(((System.Windows.Forms.Control)sender).FindForm().Handle, Win32Api.WM_NCLBUTTONDOWN, new IntPtr(Win32Api.HT_CAPTION), IntPtr.Zero);
		}
	}

	private void OverlayForm_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
	{
		foreach (System.Windows.Window window in ListOfChildWindows)
			if (window != null)
			{
				window.Owner = null;
				window.Hide();
			}
		//foreach (Form form in ListOfChildForms)
		//  if (form != null && !form.IsDisposed)
		//  {
		//    form.Owner = null;
		//    form.Hide();
		//  }
		//ListOfChildForms = null;
	}

	public class OverlayChildManager
	{
		public bool AllowShow;
		public bool EventAttached;
		public bool WindowAlreadyPositioned;

		public OverlayChildManager(bool AllowShowIn, bool EventAttachedIn, bool WindowAlreadyPositionedIn)
		{
			AllowShow = AllowShowIn;
			EventAttached = EventAttachedIn;
			WindowAlreadyPositioned = WindowAlreadyPositionedIn;
		}
	}
}