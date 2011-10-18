using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

public partial class CustomBalloonTip : Form
{
	public delegate void SimpleDelegate();

	public enum IconTypes { Error, Information, Question, Shield, Warning, None };
	public CustomBalloonTip(string Title, string Message, int Duration, CustomBalloonTip.IconTypes iconType, SimpleDelegate OnClickCallback, bool OnClickCallbackOnSeparateThread = true)
	{
		InitializeComponent();

		this.label_Title.Text = Title;
		this.label_Message.Text = Message;
		if (Duration > 0) this.timer_ShowDuration.Interval = Duration;
		else this.timer_ShowDuration.Tag = -1;
		switch (iconType)
		{
			case IconTypes.Error: this.pictureBox_Icon.Image = SystemIcons.Error.ToBitmap(); break;
			case IconTypes.Information: this.pictureBox_Icon.Image = SystemIcons.Information.ToBitmap(); break;
			case IconTypes.Question: this.pictureBox_Icon.Image = SystemIcons.Question.ToBitmap(); break;
			case IconTypes.Shield: this.pictureBox_Icon.Image = SystemIcons.Shield.ToBitmap(); break;
			case IconTypes.Warning: this.pictureBox_Icon.Image = SystemIcons.Warning.ToBitmap(); break;
			default: break;
		}

		controls = new Component[] { this, label_Title, label_Message, pictureBox_Icon, statusStrip1 };

		AddDelgateToRelevantControls_Click(OnClickCallback, OnClickCallbackOnSeparateThread);
	}

	protected override bool ShowWithoutActivation
	{
		get
		{
			return true;
			//return base.ShowWithoutActivation;
		}
	}

	//const int WS_EX_NOACTIVATE = 0x08000000;
	//const int WS_EX_TOOLWINDOW = 0x00000080;
	//protected override CreateParams CreateParams
	//{
	//  get
	//  {
	//    //return base.CreateParams;
	//    CreateParams baseParams = base.CreateParams;

	//    baseParams.ExStyle |= (int)(
	//      WS_EX_NOACTIVATE
	//      | WS_EX_TOOLWINDOW
	//      );

	//    return baseParams;
	//  }
	//}

	IntPtr LastActiveWindow;
	private void CustomBalloonTip_MouseEnter(object sender, EventArgs e)
	{
		if (this != null && !this.IsDisposed)
		{
			IntPtr last = Win32Api.GetForegroundWindow();
			if (last != this.Handle)
			{
				LastActiveWindow = Win32Api.GetForegroundWindow();
			}
			Win32Api.SetForegroundWindow(this.Handle);
		}
	}

	private void CustomBalloonTip_MouseLeave(object sender, EventArgs e)
	{
		Win32Api.SetForegroundWindow(LastActiveWindow);
	}

	Component[] controls;
	private void AddDelgateToRelevantControls_Click(SimpleDelegate VoidDelegateToRunOnClick, bool OnClickCallbackOnSeparateThread)
	{
		foreach (object c in controls)
		{
			if (c is Control)
				((Control)c).Click += delegate
				{
					this.Close();
					if (OnClickCallbackOnSeparateThread)
						ThreadingInterop.PerformVoidFunctionSeperateThread(() => { VoidDelegateToRunOnClick.Invoke(); });
					else
						VoidDelegateToRunOnClick.Invoke();
				};
			else if (c is ToolStripItem)
				((ToolStripItem)c).Click += delegate
				{
					this.Close();
					if (OnClickCallbackOnSeparateThread)
						ThreadingInterop.PerformVoidFunctionSeperateThread(() => { VoidDelegateToRunOnClick.Invoke(); });
					else
						VoidDelegateToRunOnClick.Invoke();
				};
			else if (c is StatusStrip)
				((StatusStrip)c).Click += delegate
				{
					this.Close();
					if (OnClickCallbackOnSeparateThread)
						ThreadingInterop.PerformVoidFunctionSeperateThread(() => { VoidDelegateToRunOnClick.Invoke(); });
					else
						VoidDelegateToRunOnClick.Invoke();
				};
		}
	}

	private void button1_Click(object sender, EventArgs e)
	{
		this.Close();
	}

	delegate void DecreaseOpacityCallback();
	private void CustomBalloonTip_Shown(object sender, EventArgs e)
	{
		VisibleBalloonTipForms.Add(this);
		StartTimerForClosing();
	}

	public void StartTimerForClosing()
	{
		ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
		{
			int EndTop = this.Top + this.Height;
			while (this.Top < EndTop)
			{
				Thread.Sleep(10);
				ThreadingInterop.UpdateGuiFromThread(this, () =>
					{
						if (this.Top + 5 > EndTop) this.Top = EndTop;
						else this.Top += 5;
					});
				//Action SlideDown = (Action)(() =>
				//{
				//  if (this.Top + 5 > EndTop) this.Top = EndTop;
				//  else this.Top += 5;
				//});
				//if (this.InvokeRequired)
				//  this.Invoke(SlideDown, new object[] { });
				//else
				//  SlideDown.Invoke();
			}
		});

		if (timer_ShowDuration.Interval != 0 && (timer_ShowDuration.Tag == null || timer_ShowDuration.Tag.ToString() != "-1"))
		{
			timer_ShowDuration.Tick += delegate
			{
				timer_ShowDuration.Stop();
				Rectangle bounds = new Rectangle(this.Location, this.Size);
				ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				{
					for (int i = 0; i <= 10; i++)
					{
						Thread.Sleep(20);
						Rectangle checkRectangle = new Rectangle(bounds.X - 15, bounds.Y - 15, bounds.Width + 30, bounds.Height + 30);
						while (checkRectangle.Contains(MousePosition))
							Thread.Sleep(1);
						if (this.InvokeRequired)
						{
							DecreaseOpacityCallback d = new DecreaseOpacityCallback(delegate
							{
								this.Opacity -= 0.1;
							});

							this.Invoke(d, new object[] { });
						}
						else
						{
							this.Opacity -= 0.1;
						}
					}
				});

				//DONE TODO: Should implement to NOT close if mouse is inside form
				this.Close();
			};
			timer_ShowDuration.Start();
		}
	}

	private void CustomBalloonTip_Resize(object sender, EventArgs e)
	{
		//label_Message.MaximumSize = new System.Drawing.Size(this.Width - label_Message.Location.X - button_Close.Width, label_Message.MaximumSize.Height);
		//label_Message.Width = this.Width - label_Message.Location.X - button_Close.Width;
	}


	//delegate void MoveWindowUpCallback();
	public static List<CustomBalloonTip> VisibleBalloonTipForms = new List<CustomBalloonTip>();
	public static void ShowCustomBalloonTip(string Title, string Message, int Duration, CustomBalloonTip.IconTypes iconType, SimpleDelegate OnClickCallback)
	{
		CustomBalloonTip cbt = new CustomBalloonTip(Title, Message, Duration, iconType, delegate { OnClickCallback.Invoke(); });
		//this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Right - this.Width, Screen.PrimaryScreen.WorkingArea.Bottom - this.Height);
		int TopStart = 0;
		foreach (CustomBalloonTip tmpVisibleFrms in VisibleBalloonTipForms)
			if (tmpVisibleFrms != null && tmpVisibleFrms.Visible)
				TopStart += tmpVisibleFrms.Height;
		int gapFromSide = 100;
		cbt.Location = new Point(Screen.PrimaryScreen.WorkingArea.Left + gapFromSide, Screen.PrimaryScreen.WorkingArea.Top + TopStart - cbt.Height);
		cbt.Width = Screen.PrimaryScreen.WorkingArea.Width - gapFromSide * 2;
		cbt.FormClosed += (snder, evtargs) =>
		{
			CustomBalloonTip thisCustomTip = snder as CustomBalloonTip;
			if (VisibleBalloonTipForms.Contains(thisCustomTip))
			{
				int indexOfRemoved = VisibleBalloonTipForms.IndexOf(thisCustomTip);
				int cbtHeight = thisCustomTip.Height;
				for (int i = indexOfRemoved + 1; i < VisibleBalloonTipForms.Count; i++)
				{
					CustomBalloonTip tmpForm = VisibleBalloonTipForms[i];
					ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
					{
						//int StartPoint = VisibleBalloonTipForms[i].Top;
						int EndPoint = tmpForm.Top - cbtHeight;
						while (tmpForm.Top > EndPoint)
						{
							System.Threading.Thread.Sleep(10);
							ThreadingInterop.UpdateGuiFromThread(tmpForm, () =>
							{
								if (tmpForm.Top - 5 > EndPoint) tmpForm.Top -= 5;
								else tmpForm.Top = EndPoint;
							});
							//Action SlideUpAction = (Action)(() =>
							//{
							//  if (tmpForm.Top - 5 > EndPoint) tmpForm.Top -= 5;
							//  else tmpForm.Top = EndPoint;
							//});
							//if (tmpForm.InvokeRequired)
							//  tmpForm.Invoke(SlideUpAction, new object[] { });
							//else SlideUpAction.Invoke();
							//VisibleBalloonTipForms[i].Top -= 5;
						}
					}, false);
					//VisibleBalloonTipForms[i].Top -= cbt.Height;
				}
				VisibleBalloonTipForms.Remove(thisCustomTip);
				//foreach (
			}
		};

		IntPtr currentActiveWindow = Win32Api.GetForegroundWindow();
		VisibleBalloonTipForms.Add(cbt);
		cbt.Show();
		if (Win32Api.GetForegroundWindow() != currentActiveWindow)
			Win32Api.SetForegroundWindow(currentActiveWindow);

		//ShowInactiveTopmost(cbt);
		//cbt.StartTimerForClosing();
		//cbt.Show();
	}
}