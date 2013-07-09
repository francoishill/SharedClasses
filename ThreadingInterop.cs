﻿using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

public class ThreadingInterop
{
	public static bool ForceExitAllTreads = false;

	private static bool AlreadyAttachedToApplicationExitEvent = false;
	//TODO: Have a look at this function (automatically queues to a thread) - System.Threading.ThreadPool.QueueUserWorkItem()
	//PerformVoidFunctionSeperateThread(() => { MessageBox.Show("Test"); MessageBox.Show("Test1"); });
	public static Thread PerformOneArgFunctionSeperateThread(Action<object> action, object arg, bool WaitUntilFinish = true, string ThreadName = "UnknownName", bool CheckInvokeRequired = false, Control controlToCheckInvokeRequired = null, bool AttachForceExitToFormClose = true)
	{
		if (AttachForceExitToFormClose)
			if (!AlreadyAttachedToApplicationExitEvent)
			{
				if (Application.OpenForms.Count > 0)
					Application.OpenForms[0].FormClosing += delegate
					{
						ForceExitAllTreads = true;
					};
				//Application.ApplicationExit += delegate
				//{
				//  ForceExitAllTreads = true;
				//};
				AlreadyAttachedToApplicationExitEvent = true;
			}

		System.Threading.Thread th = new System.Threading.Thread(() =>
		{
			if (CheckInvokeRequired && controlToCheckInvokeRequired != null)
				controlToCheckInvokeRequired.Invoke((Action)delegate { action(arg); });
			else
				action(arg);
		});
		th.Name = ThreadName;
		th.Start();
		//th.Join();
		if (WaitUntilFinish)
		{
			while (th.IsAlive && !ForceExitAllTreads) { Application.DoEvents(); }
			//th.Join();Cannot use this, makes QuickAccess not work (window does not want to show when clicking on tray icon)
			th.Abort();
			th = null;
			return null;
		}
		else
		{
			Application.DoEvents();
			return th;
		}
	}
	public static Thread PerformVoidFunctionSeperateThread(MethodInvoker method, bool WaitUntilFinish = true, string ThreadName = "UnknownName", bool CheckInvokeRequired = false, Control controlToCheckInvokeRequired = null, bool AttachForceExitToFormClose = true)
	{
		return PerformOneArgFunctionSeperateThread(
			(Action<object>)delegate(object arg) { method(); },
			null,
			WaitUntilFinish,
			ThreadName,
			CheckInvokeRequired,
			controlToCheckInvokeRequired,
			AttachForceExitToFormClose);
	}

	public static void UpdateGuiFromThread(Control controlToUpdate, Action action)
	{
		if (controlToUpdate.InvokeRequired)
		{
			try
			{
				controlToUpdate.Invoke(action);//, new object[] { });
			}
			catch { }
		}
		else
		{
			try
			{
				action();
			}
			catch { }
		}
	}

	delegate void AutocompleteCallback(ComboBox txtBox, String text);
	delegate void ClearAutocompleteCallback(ComboBox txtBox);
	public static void ClearTextboxAutocompleteCustomSource(ComboBox txtBox)
	{
		if (txtBox.InvokeRequired)
		{
			ClearAutocompleteCallback d = new ClearAutocompleteCallback(ClearTextboxAutocompleteCustomSource);
			txtBox.Invoke(d, new object[] { txtBox });
		}
		else
		{
			txtBox.AutoCompleteCustomSource.Clear();
		}
	}

	public static void AddTextboxAutocompleteCustomSource(ComboBox txtBox, string textToAdd)
	{
		if (txtBox.InvokeRequired)
		{
			AutocompleteCallback d = new AutocompleteCallback(AddTextboxAutocompleteCustomSource);
			txtBox.Invoke(d, new object[] { txtBox, textToAdd });
		}
		else
		{
			txtBox.AutoCompleteCustomSource.Add(textToAdd);
		}
	}

	/// <summary>
	/// Usage: using (new WaitIndicator()) { Your code here... }
	/// </summary>
	public class WaitIndicator : IDisposable
	{
		public class ProgressForm : Form
		{
			public ProgressForm()
			{
				ControlBox = false;
				ShowInTaskbar = false;
				StartPosition = FormStartPosition.CenterScreen;
				TopMost = true;
				FormBorderStyle = FormBorderStyle.None;
				var progreassBar = new ProgressBar()
				{
					Style = ProgressBarStyle.Marquee,
					Size = new System.Drawing.Size(200, 20),
					Value = 40,
					ForeColor = Color.Orange,
					BackColor = Color.Purple,
					MarqueeAnimationSpeed = 40
				};
				AutoSize = true;
				AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
				//Size = progreassBar.Size;
				Controls.Add(progreassBar);

				progreassBar.MouseDown += (snder, evtargs) =>
				{
					firstPoint = evtargs.Location;
					IsMouseDown = true;
				};
				progreassBar.MouseUp += (snder, evtargs) =>
				{
					IsMouseDown = false;
				};
				progreassBar.MouseMove += (snder, evtargs) =>
				{
					this.OnMouseMove(evtargs);
				};
			}

			private bool IsMouseDown = false;
			private Point firstPoint;
			protected override void OnMouseDown(MouseEventArgs e)
			{
				firstPoint = e.Location;
				IsMouseDown = true;
				base.OnMouseDown(e);
			}

			protected override void OnMouseUp(MouseEventArgs e)
			{
				IsMouseDown = false;
				base.OnMouseUp(e);
			}

			protected override void OnMouseMove(MouseEventArgs e)
			{
				if (IsMouseDown)
				{
					// Get the difference between the two points
					int xDiff = firstPoint.X - e.Location.X;
					int yDiff = firstPoint.Y - e.Location.Y;

					// Set the new point
					int x = this.Location.X - xDiff;
					int y = this.Location.Y - yDiff;
					this.Location = new Point(x, y);
				} base.OnMouseMove(e);
			}
		}

		public ProgressForm progressForm;
		public Form ParentForm;
		Thread thread;
		bool disposed = false; //to avoid redundant call
		public WaitIndicator(Form parentForm_usedForPositioning)
		{
			progressForm = new ProgressForm();
			progressForm.Shown += delegate { UpdateOwnLocation(); };
			ParentForm = parentForm_usedForPositioning;
			thread = new Thread(_ => progressForm.ShowDialog());
			thread.Start();
		}

		private void UpdateOwnLocation()
		{
			if (ParentForm == null)
				return;
			try
			{
				this.progressForm.Location = new Point(
					ParentForm.Left + (ParentForm.Width / 2) - (this.progressForm.Width / 2),
					ParentForm.Top + (ParentForm.Height / 2) - (this.progressForm.Height / 2));
				Application.DoEvents();
			}
			catch { }
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				thread.Abort();
				progressForm = null;
			}
			disposed = true;
		}
	}
}