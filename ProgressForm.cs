using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SharedClasses
{
	public partial class ProgressForm : Form
	{
		private bool UserCancelled = false;

		public ProgressForm()
		{
			InitializeComponent();
		}

		private static ProgressForm form = null;
		public static bool RunProgressLoop(Action<int> actionEachLoopIteration, int loopStart, int loopEnd, string userMessage)
		{
			try
			{
				DateTime startTime;
				double stepDuration;
				double totalDuration = 0;
				double averageDuration = 0;
				double estimatedSecondsRemaining = 0;

				int iStart = loopStart;
				int iEnd = loopEnd;
				if (loopEnd < loopStart)
				{
					iStart = loopEnd;
					iEnd = loopStart;
				}
				int totalSteps = iEnd - iStart + 1;

				if (form == null)
					form = new ProgressForm();
				form.labelUsermessage.Text = userMessage;
				form.progressBar1.Minimum = 0;
				form.progressBar1.Maximum = 100;
				form.progressBar1.Value = 0;
				form.progressBar1.Style = ProgressBarStyle.Marquee;
				form.Show();

				bool loopExited = false;
				ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				{
					int iterCount = 0;
					for (int i = iStart; i <= iEnd; i++)
					{
						startTime = DateTime.Now;
						actionEachLoopIteration(i);
						stepDuration = DateTime.Now.Subtract(startTime).TotalSeconds;

						if (iterCount > 0)
						{
							totalDuration += stepDuration;
							if (Math.Round((double)((i - iStart) * 100 / totalSteps)) != form.progressBar1.Value)
							{
								averageDuration = totalDuration / iterCount;
								estimatedSecondsRemaining = averageDuration * (totalSteps - iterCount);

								Action updateProgressAction = (Action)delegate
								{
									if (form.progressBar1.Style != ProgressBarStyle.Continuous)
										form.progressBar1.Style = ProgressBarStyle.Continuous;
									form.progressBar1.Value = (int)Math.Round((double)((i - iStart) * 100 / totalSteps));
									form.labelTimeleft.Visible = true;
									form.labelTimeleft.Text = string.Format(
										"Approximately {0} seconds remaining ({1}/{2})...",
										Math.Round(estimatedSecondsRemaining),
										iterCount,
										totalSteps);
									form.labelElapsedtime.Visible = true;
									form.labelElapsedtime.Text = "Elapsed time: " + Math.Round(totalDuration) + "s";
								};
								if (form.InvokeRequired)
									form.Invoke(updateProgressAction);
								else
									updateProgressAction();
							}
						}

						iterCount++;

						if (form.UserCancelled)
						{
							loopExited = true;
							break;
						}
					}
				},
				true,
				AttachForceExitToFormClose: false);

				return !loopExited;
			}
			finally
			{
				if (form != null)
				{
					form.Close();
					form.Dispose();
					form = null;
				}
			}
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			UserCancelled = true;
		}
	}
}
