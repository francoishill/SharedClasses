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

		foreach (Form form in ListOfChildForms)
		{
			form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
			form.MaximizeBox = false;
			form.MinimizeBox = false;
			form.ShowInTaskbar = false;
			//form.ShowIcon = false;
			AddEventToControlSubcontrols(form);
			//form.MouseDown += new MouseEventHandler(form_MouseDown);
			//foreach (Control c in form.Controls)
			//  c.MouseDown += new MouseEventHandler(form_MouseDown);
			form.FormClosing += (s, closeargs) =>
			{
				if (closeargs.CloseReason == CloseReason.UserClosing)
				{
					closeargs.Cancel = true;
					((Form)s).Hide();
					((Form)s).Tag = true;
				}
			};
			form.TopMost = true;
			if (form.Tag == null || form.Tag.ToString().ToLower() != "true")
				form.Show();
		}

		//CustomBalloonTip.ShowCustomBalloonTip("Quick", "Quicker", 1000, CustomBalloonTip.IconTypes.Information, () => { });
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
}