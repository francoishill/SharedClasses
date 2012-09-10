using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System;

namespace SharedClasses{
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