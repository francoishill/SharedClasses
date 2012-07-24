using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace SharedClasses
{
	public partial class MiniProgressIndeterminateForm : Form
	{
		ContextMenu contextMenu;
		public EventHandler onCancelHandler = new EventHandler(delegate { });

		public MiniProgressIndeterminateForm(string message, bool canCancel)
		{
			InitializeComponent();

			if (canCancel)
			{
				contextMenu = new System.Windows.Forms.ContextMenu(new MenuItem[]
				{
					new MenuItem("Cancel", (s, e) => { onCancelHandler(s, e); })
				});
				this.ContextMenu = contextMenu;
			}

			label1.Text = message;

			progressBar1.MouseDown += (s, e) => this.OnMouseDown(e);
			progressBar1.MouseMove += (s, e) => this.OnMouseMove(e);
			progressBar1.MouseUp += (s, e) => this.OnMouseUp(e);

			label1.MouseDown += (s, e) => this.OnMouseDown(e);
			label1.MouseMove += (s, e) => this.OnMouseMove(e);
			label1.MouseUp += (s, e) => this.OnMouseUp(e);
		}

		private void MiniProgressIndeterminateForm_Shown(object sender, EventArgs e)
		{
			this.BringToFront();
			this.TopMost = !this.TopMost;
			this.TopMost = !this.TopMost;
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

		private void MiniProgressIndeterminateForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason == CloseReason.UserClosing)
				e.Cancel = true;
		}
	}

	/// <summary>
	/// Usage is using(var progbar = new IndeterminateProgress(message, canCancel)) { }
	/// </summary>
	public class IndeterminateProgress : IDisposable
	{
		private MiniProgressIndeterminateForm tmpForm;
		private Thread thread;
		private bool disposed = false; //to avoid redundant call
		public EventHandler onCancel = new EventHandler(delegate { });

		/// <summary>
		/// Usage is using(var progbar = new IndeterminateProgress(message, canCancel)) { }
		/// </summary>
		/// <param name="message">The string message to show with progress</param>
		/// <param name="canCancel">Should the user be able to cancel (via right-click menu)?</param>
		public IndeterminateProgress(string message, bool canCancel)
		{
			tmpForm = new MiniProgressIndeterminateForm(message, canCancel);
			if (canCancel)
				tmpForm.onCancelHandler += (s, e) => { onCancel(s, e); };
			tmpForm.Disposed += delegate
			{
				thread.Abort();
				tmpForm = null;
			};
			tmpForm.Location = new Point(Form.MousePosition.X, Form.MousePosition.Y);
			thread = new Thread(_ => tmpForm.ShowDialog());
			thread.Start();
			//tmpForm.ShowDialog();
			//tmpForm.Dispose();
			//tmpForm = null;
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
				tmpForm = null;
			}
			disposed = true;
		}
	}
}