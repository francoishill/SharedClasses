using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace SharedClasses
{
	public partial class MiniDownloadBarForm : Form
	{
		public MiniDownloadBarForm()
		{
			InitializeComponent();
		}

		private static MiniDownloadBarForm miniForm;
		public static void ShowMiniDownloadBar()
		{
			if (miniForm == null)
				miniForm = new MiniDownloadBarForm();
			miniForm.Show();
		}
		public static void CloseDownloadBar()
		{
			miniForm.ForceClose = true;
			miniForm.Close();
			miniForm.Dispose();
			miniForm = null;
		}
		public static void UpdateProgress(int percentage)
		{
			if (miniForm == null)
				ShowMiniDownloadBar();
			if (miniForm.progressBar1.Value != percentage)
			{
				miniForm.progressBar1.Value = percentage;
				Application.DoEvents();
			}
		}
		public static void UpdateMessage(string messageHover)
		{
			if (miniForm == null)
				ShowMiniDownloadBar();
			miniForm.toolTip1.SetToolTip(miniForm.progressBar1, messageHover);
		}

		protected override bool ShowWithoutActivation { get { return true; } }

		private void MiniDownloadBarForm_Shown(object sender, EventArgs e)
		{
			RepositionForm();
		}

		private void MiniDownloadBarForm_SizeChanged(object sender, EventArgs e)
		{
			RepositionForm();
		}

		private void RepositionForm()
		{
			var workArea = Screen.PrimaryScreen.WorkingArea;
			this.Top = workArea.Bottom - this.Height;
			this.Left = workArea.Right - this.Width;
		}

		private bool ForceClose = false;
		private void MiniDownloadBarForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason == CloseReason.UserClosing && !ForceClose)
			{
				e.Cancel = true;
				this.Hide();
			}
		}

		private const int WM_GETMINMAXINFO = 0x0024;
		protected override void WndProc(ref Message m)
		{
			//if (m.Msg == WM_WINDOWPOSCHANGING)
			//{
			//	WindowPos windowPos = (WindowPos)m.GetLParam(typeof(WindowPos));
			//	// Make changes to windowPos
			//	// Then marshal the changes back to the message
			//	Marshal.StructureToPtr(windowPos, m.LParam, true);
			//}
			base.WndProc(ref m);
			// Make changes to WM_GETMINMAXINFO after it has been handled by the underlying
			// WndProc, so we only need to repopulate the minimum size constraints
			if (m.Msg == WM_GETMINMAXINFO)
			{
				//Need this to override the minimum heigth of the form enforced by the operating system:
				//http://stackoverflow.com/questions/992352/overcome-os-imposed-windows-form-minimum-size-limit
				MinMaxInfo minMaxInfo = (MinMaxInfo)m.GetLParam(typeof(MinMaxInfo));
				minMaxInfo.ptMinTrackSize.x = this.MinimumSize.Width;
				minMaxInfo.ptMinTrackSize.y = this.MinimumSize.Height;
				Marshal.StructureToPtr(minMaxInfo, m.LParam, true);
			}
		}

		struct POINT
		{
			public int x;
			public int y;
		}

#pragma warning disable
		struct MinMaxInfo
		{
			public POINT ptReserved;
			public POINT ptMaxSize;
			public POINT ptMaxPosition;
			public POINT ptMinTrackSize;
			public POINT ptMaxTrackSize;
		}
#pragma warning enable
	}
}
