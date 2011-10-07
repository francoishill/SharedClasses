using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

public partial class OverlayForm : Form
{
	public List<Form> ListOfChildForms = new List<Form>();

	public OverlayForm()
	{
		InitializeComponent();
	}

	private void OverlayForm_Click(object sender, EventArgs e)
	{
		this.Close();//.Hide();
	}

	private void tmpQuickCustomBalloonTip(string msg)
	{
		CustomBalloonTip.ShowCustomBalloonTip("Title", msg, 2000, CustomBalloonTip.IconTypes.Information, delegate { });
	}

	private void OverlayForm_Shown(object sender, EventArgs e)
	{
		this.Opacity = 0;
		this.Show();
		//button1.Location = new Point((this.Width - button1.Width) / 2, (this.Height - button1.Height) / 2);
		ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
		{
			for (int i = 0; i < 60; i++)
			{
				System.Threading.Thread.Sleep(4);
				ThreadingInterop.UpdateGuiFromThread(this, () =>
				{
					this.Opacity += 0.01;
				});
			}
		});

		int leftGap = 20;
		int topGap = 20;

		int NextLeftPos = leftGap;
		int MaxHeightInRow = 0;
		int NextTopPos = topGap;

		Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;

		foreach (Form form in ListOfChildForms)
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
				form.Tag = new OverlayChildManager(true, false, false);

			form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
			form.MaximizeBox = false;
			form.MinimizeBox = false;
			form.ShowInTaskbar = false;
			//form.ShowIcon = false;

			if (!IsMouseDownEventAdded(form))
			{
				AddEventToControlSubcontrols(form);
				MarkformMouseDowneventAdded(form);
			}

			if (!IsClosingEventAdded(form))
			{
				form.FormClosing += (s, closeargs) =>
				{
					Form thisForm = s as Form;
					if (closeargs.CloseReason == CloseReason.UserClosing)
					{
						closeargs.Cancel = true;
						thisForm.Hide();
						SetFormAllowShow(thisForm, false);
						//UserMessages.ShowMessage("Userclose");
					}
				};
				MarkformClosingeventAdded(form);
			}
			form.TopMost = true;
			if (MayFormBeShow(form))
				form.Show();
		}
	}

	private void SetFormAllowShow(Form form, bool allowShowValue)
	{
		if (form == null) return;
		if (!(form.Tag is OverlayChildManager)) return;
		(form.Tag as OverlayChildManager).AllowShow = allowShowValue;
	}

	private bool MayFormBeShow(Form form)
	{
		if (form == null) return true;
		if (!(form.Tag is OverlayChildManager)) return true;
		return (form.Tag as OverlayChildManager).AllowShow;
	}

	private bool IsClosingEventAdded(Form form)
	{
		if (form == null) return false;
		if (!(form.Tag is OverlayChildManager)) return false;
		return (form.Tag as OverlayChildManager).ClosingEventAttached;
	}

	private bool IsMouseDownEventAdded(Form form)
	{
		if (form == null) return false;
		if (!(form.Tag is OverlayChildManager)) return false;
		return (form.Tag as OverlayChildManager).MouseDownEventAttached;
	}

	private void MarkformClosingeventAdded(Form form)
	{
		if (form == null) return;
		if (!(form.Tag is OverlayChildManager)) return;
		(form.Tag as OverlayChildManager).ClosingEventAttached = true;
	}

	private void MarkformMouseDowneventAdded(Form form)
	{
		if (form == null) return;
		if (!(form.Tag is OverlayChildManager)) return;
		(form.Tag as OverlayChildManager).MouseDownEventAttached = true;
	}

	private void AddEventToControlSubcontrols(Control control)
	{
		control.MouseDown += new MouseEventHandler(form_MouseDown);
		foreach (Control subcontrol in control.Controls)
			AddEventToControlSubcontrols(subcontrol);
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
		foreach (Form form in ListOfChildForms)
			if (form != null && !form.IsDisposed)
			{
				form.Owner = null;
				form.Hide();
			}
		//ListOfChildForms = null;
	}

	public class OverlayChildManager
	{
		public bool AllowShow;
		public bool ClosingEventAttached;
		public bool MouseDownEventAttached;

		public OverlayChildManager(bool AllowShowIn, bool ClosingEventAttachedIn, bool MouseDownEventAttachedIn)
		{
			AllowShow = AllowShowIn;
			ClosingEventAttached = ClosingEventAttachedIn;
			MouseDownEventAttached = MouseDownEventAttachedIn;
		}
	}
}