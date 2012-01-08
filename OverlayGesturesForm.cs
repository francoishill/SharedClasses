using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DynamicDLLsInterop;
using InlineCommands;
using InterfaceForQuickAccessPlugin;

namespace SharedClasses
{
	public partial class OverlayGesturesForm : Form
	{
		private bool IsGestureBusy = false;

		public OverlayGesturesForm()
		{
			InitializeComponent();
		}

		public static void RunMethodAfterMilliseconds(Action method, int milliseconds)
		{
			System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
			timer.Interval = milliseconds;
			timer.Tick += delegate
			{
				timer.Dispose();
				timer = null;
				method();
			};
			timer.Start();
		}

		private void mouseGestures1_BeginGestureEvent(object sender, EventArgs e)
		{
			IsGestureBusy = true;
		}

		private void mouseGestures1_EndGestureEvent(object sender, EventArgs e)
		{
			IsGestureBusy = false;
			this.Refresh();
			this.Close();
		}

		private bool FindGestureMathcAndPerformIt(string gestureText)//like URD or LURD
		{
			foreach (IQuickAccessPluginInterface qai in DynamicDLLs.PluginList)
				if (qai.GetType().GetInterface(typeof(MouseGesturePlugins.IMouseGesture).Name) != null)
				{
					MouseGesturePlugins.IMouseGesture gesture = (MouseGesturePlugins.IMouseGesture)qai.GetType().GetConstructor(new Type[0]).Invoke(new object[0]);
					if (gesture.GestureString.ToLower() == gestureText.ToLower())
					{
						string tmpErrorMessage;
						if (UserMessages.Confirm("Gesture command found in plugins, name = " + gesture.ThisCommandName + ", perform command now?", DefaultYesButton: true))
						{
							if (!gesture.PerformCommandAfterGesture(out tmpErrorMessage))
								UserMessages.ShowWarningMessage("Gesture plugin match was found but to perform the gesture failed, the following error message was returned:" + Environment.NewLine + tmpErrorMessage);
							return true;
						}
						//else break;
					}
					//gesture.PerformCommandAfterGesture();
					//ICommandWithHandler comm = (ICommandWithHandler)qai.GetType().GetConstructor(new Type[0]).Invoke(new object[0]);
				}
			return false;
		}

		private void mouseGestures1_Gesture(object sender, MouseGestures.MouseGestureEventArgs e)
		{
			this.Refresh();
			//this.Close();

			if (e.Gesture.ToString() == "URDLU")
			{
				Clipboard.SetText("bokbokkie");
				RunMethodAfterMilliseconds(delegate { Clipboard.Clear(); }, 7000);
			}
			else if (GlobalSettings.MouseGesturesSettings.Instance.GetGesturesWithGesturePluginName().ContainsKey(e.Gesture.ToString()))
				UserMessages.ShowInfoMessage(GlobalSettings.MouseGesturesSettings.Instance.GetGesturesWithGesturePluginName()[e.Gesture.ToString()], "Gesture message");
			else if (FindGestureMathcAndPerformIt(e.Gesture.ToString()))
			{ }
			else
				System.Windows.Forms.MessageBox.Show("Unknown gesture: " + e.Gesture.ToString());
		}

		private void mouseGestures1_MouseMove(object sender, EventArgs e)
		{
			if (IsGestureBusy)
			{
				System.Drawing.Point pt = System.Windows.Forms.Cursor.Position; // Get the mouse cursor in screen coordinates 
				using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
				{
					g.FillEllipse(System.Drawing.Brushes.Black, pt.X, pt.Y, 5, 5);
				}
			}
		}
	}
}
