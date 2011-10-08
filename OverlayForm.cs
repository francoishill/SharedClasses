using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

public partial class OverlayForm : Form
{
	//public List<Form> ListOfChildForms = new List<Form>();
	public List<System.Windows.Window> ListOfChildWindows = new List<System.Windows.Window>();

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
		for (int i = 0; i < 60; i++)
		{
			ThreadingInterop.PerformVoidFunctionSeperateThread(() => { System.Threading.Thread.Sleep(3); });
			ThreadingInterop.UpdateGuiFromThread(this, () =>
			{
				this.Opacity += 0.01;
			});
		}
		//});

		int leftGap = 20;
		int topGap = 20;

		int NextLeftPos = leftGap;
		int MaxHeightInRow = 0;
		int NextTopPos = topGap;

		Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;

		foreach (System.Windows.Window window in ListOfChildWindows)
		{
			window.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
			if (NextLeftPos + window.Width + leftGap >= workingArea.Right)
			{
				NextTopPos += MaxHeightInRow + topGap;
				NextLeftPos = leftGap;
				MaxHeightInRow = 0;
			}

			window.Left = NextLeftPos;
			window.Top = NextTopPos;
			NextLeftPos += (int)window.Width + leftGap;
			if (window.Height > MaxHeightInRow) MaxHeightInRow = (int)window.Height;

			if (window.Tag == null)
				window.Tag = new OverlayChildManager(true, false);

			window.WindowStyle = System.Windows.WindowStyle.None;
			if (!window.AllowsTransparency) window.AllowsTransparency = true;
			//window.MaximizeBox = false;
			//window.MinimizeBox = false;
			window.ShowInTaskbar = false;
			//form.ShowIcon = false;

			if (!IsEventsAdded(window))//form))
			{
				//AddMouseDownEventToControlandSubcontrols(form);
				AddFormClosingEventToWindow(window);//form);
				//AddKeydownEventToControlandSubcontrols(form);
				AddKeydownEventToWindowAndChildren(window);

				MarkformEventsAdded(window);
			}
			//form.TopMost = true;
			window.Topmost = true;
			if (MayFormBeShow(window))
			{
				window.Opacity = 0.75F;
				PropertyInfo propertyFreezeEvent_Activated = window.GetType().GetProperty("FreezeEvent_Activated");
				if (propertyFreezeEvent_Activated != null) propertyFreezeEvent_Activated.SetValue(window, true, null);
				window.Show();
				//if (propertyFreezeEvent_Activated != null) propertyFreezeEvent_Activated.SetValue(form, false, null);
			}
		}

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

			form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
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

	private void AddKeydownEventToWindowAndChildren(System.Windows.Window window)
	{
		window.KeyDown += new System.Windows.Input.KeyEventHandler(control_KeyDown1);
		foreach (object o in System.Windows.LogicalTreeHelper.GetChildren(window))
			if (o is System.Windows.Controls.Control)
				(o as System.Windows.Controls.Control).KeyDown += new System.Windows.Input.KeyEventHandler(control_KeyDown1);
	}

	private void AddKeydownEventToControlandSubcontrols(Control control)
	{
		//if (control is TextBox) (control as TextBox).Multiline = true;
		control.KeyDown += new KeyEventHandler(control_KeyDown);
		//control.KeyDown += new System.Windows.Input.KeyEventHandler(control_KeyDown1);
		foreach (Control subcontrol in control.Controls)
			AddKeydownEventToControlandSubcontrols(subcontrol);
	}

	void control_KeyDown1(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == System.Windows.Input.Key.Tab)
			e.Handled = true;// == Keys.Tab) e.Handled = true;

		//if (e.KeyCode == Keys.Tab && ModifierKeys == Keys.Control)
		if (e.Key == System.Windows.Input.Key.Tab && ModifierKeys == Keys.Control)
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
		else if (e.Key == System.Windows.Input.Key.Tab && (ModifierKeys & (Keys.Control | Keys.Shift)) == (Keys.Control | Keys.Shift))
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

	private System.Windows.Window FindWindowOfControl(System.Windows.Controls.Control control)
	{
		System.Windows.Controls.Control tmpControl = control;
		while (!(tmpControl is System.Windows.Window))
			tmpControl = (System.Windows.Controls.Control)tmpControl.Parent;
		return tmpControl as System.Windows.Window;
	}

	private void control_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Tab) e.Handled = true;

		if (e.KeyCode == Keys.Tab && ModifierKeys == Keys.Control)
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
		else if (e.KeyCode == Keys.Tab && (ModifierKeys & (Keys.Control | Keys.Shift)) == (Keys.Control | Keys.Shift))
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

	private void AddFormClosingEventToWindow(System.Windows.Window window)//Form form)
	{
		//form.FormClosing += (s, closeargs) =>
		window.Closing += (s, closeargs) =>
		{
			Form thisForm = s as Form;
			//if (closeargs.CloseReason == CloseReason.UserClosing)
			{
				closeargs.Cancel = true;
				thisForm.Hide();
				SetFormAllowShow(thisForm, false);
				//UserMessages.ShowMessage("Userclose");
			}
		};
	}

	private void SetFormAllowShow(Form form, bool allowShowValue)
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

	private void AddMouseDownEventToControlandSubcontrols(Control control)
	{
		control.MouseDown += new MouseEventHandler(form_MouseDown);
		foreach (Control subcontrol in control.Controls)
			AddMouseDownEventToControlandSubcontrols(subcontrol);
	}

	private void form_MouseDown(object sender, MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			Win32Api.ReleaseCapture();
			if (!(sender is TextBox) && !(sender is RichTextBox))
				Win32Api.SendMessage(((Control)sender).FindForm().Handle, Win32Api.WM_NCLBUTTONDOWN, new IntPtr(Win32Api.HT_CAPTION), IntPtr.Zero);
		}
	}

	private void OverlayForm_FormClosing(object sender, FormClosingEventArgs e)
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

		public OverlayChildManager(bool AllowShowIn, bool EventAttachedIn)
		{
			AllowShow = AllowShowIn;
			EventAttached = EventAttachedIn;
		}
	}
}