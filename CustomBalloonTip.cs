﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace MonitorSystem
{
	public partial class CustomBalloonTip : Form
	{
		public enum IconTypes { Error, Information, Question, Shield, Warning, None };
		public CustomBalloonTip(string Title, string Message, int Duration, CustomBalloonTip.IconTypes iconType, Form1.SimpleDelegate VoidDelegateToRunOnClick)
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

			controls = new Component[] { this, label_Title, label_Message, pictureBox_Icon };

			AddDelgateToRelevantControls(VoidDelegateToRunOnClick);
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
		//      WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);

		//    return baseParams;
		//  }
		//}

		Component[] controls;
		private void AddDelgateToRelevantControls(Form1.SimpleDelegate VoidDelegateToRunOnClick)
		{
			foreach (object c in controls)
			{
				if (c is Control)
					((Control)c).Click += delegate
					{
						this.Close();
						VoidDelegateToRunOnClick.Invoke();
					};
				else if (c is ToolStripItem)
					((ToolStripItem)c).Click += delegate
					{
						this.Close();
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
			StartTimerForClosing();
		}

		public void StartTimerForClosing()
		{
			Form1.PerformVoidFunctionSeperateThread(() =>
			{
				int EndTop = this.Top + this.Height;
				while (this.Top < EndTop)
				{
					Thread.Sleep(10);
					Form1.UpdateGuiFromThread(this, () =>
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
					Form1.PerformVoidFunctionSeperateThread(() =>
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
	}
}
