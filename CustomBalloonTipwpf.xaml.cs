using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;
//using System.Drawing;
using System.Collections.Generic;
using SystemIcons = System.Drawing.SystemIcons;
using Screen = System.Windows.Forms.Screen;
using Rectangle = System.Drawing.Rectangle;
using System.Threading;
using System.Windows.Media.Animation;

/// <summary>
/// Interaction logic for tmpUserControl.xaml
/// </summary>
public partial class CustomBalloonTipwpf : Window
{
	private System.Windows.Forms.Timer timer_ShowDuration = new System.Windows.Forms.Timer();
	public string KeyForForm;

	public delegate void SimpleDelegateWithSender(object returnObject);
	public enum IconTypes { Error, Information, Question, Shield, Warning, None };
	public CustomBalloonTipwpf(string Title, string Message, int Duration, IconTypes iconType, SimpleDelegateWithSender OnClickCallback, bool OnClickCallbackOnSeparateThread = true)
	{
		InitializeComponent();

		this.label_Title.Content = Title;
		this.label_Message.Content = Message;
		if (Duration > 0) this.timer_ShowDuration.Interval = Duration;
		else this.timer_ShowDuration.Tag = -1;

		Imaging.CreateBitmapSourceFromHIcon(System.Drawing.SystemIcons.Information.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

		switch (iconType)
		{

			case IconTypes.Error: this.pictureBox_Icon.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Error.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()); break;
			case IconTypes.Information: this.pictureBox_Icon.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Information.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()); break;
			case IconTypes.Question: this.pictureBox_Icon.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Question.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()); break;
			case IconTypes.Shield: this.pictureBox_Icon.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Shield.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()); break;
			case IconTypes.Warning: this.pictureBox_Icon.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Warning.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()); break;
			default: this.pictureBox_Icon.Visibility = System.Windows.Visibility.Collapsed; break;
		}

		this.MouseLeftButtonUp += (snder, evtargs) =>
		{
			(snder as CustomBalloonTipwpf).Close();

			if (OnClickCallbackOnSeparateThread)
				ThreadingInterop.PerformVoidFunctionSeperateThread(() => { OnClickCallback.Invoke((snder as CustomBalloonTipwpf).KeyForForm); });
			else
				OnClickCallback.Invoke((snder as CustomBalloonTipwpf).KeyForForm);
		};
	}

	public static List<CustomBalloonTipwpf> VisibleBalloonTipForms = new List<CustomBalloonTipwpf>();
	public static void ShowCustomBalloonTip(string Title, string Message, int Duration, IconTypes iconType, SimpleDelegateWithSender OnClickCallback, string keyForForm = null, bool CallbackOnSeparateThread = false, double Scaling = 1)
	{
		//TODO: Also think about moving all these notifications into usercontrols in one MAIN window
		CustomBalloonTipwpf cbt = new CustomBalloonTipwpf(Title, Message, Duration, iconType, OnClickCallback, CallbackOnSeparateThread);
		if (Scaling != 1) cbt.mainBorder.LayoutTransform = new ScaleTransform(Scaling, Scaling);
		cbt.KeyForForm = keyForForm;
		//this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Right - this.Width, Screen.PrimaryScreen.WorkingArea.Bottom - this.Height);
		double TopStart = 0;
		foreach (CustomBalloonTipwpf tmpVisibleFrms in VisibleBalloonTipForms)
			if (tmpVisibleFrms != null && tmpVisibleFrms.Visibility == Visibility.Visible)
				TopStart += tmpVisibleFrms.ActualHeight;
		//int gapFromSide = 100;
		//cbt.Location = new Point(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Left + gapFromSide, Screen.PrimaryScreen.WorkingArea.Top + TopStart - cbt.Height);
		//cbt.ActualWidth = Screen.PrimaryScreen.WorkingArea.Width - gapFromSide * 2;
		cbt.Closed += (snder, evtargs) =>
		{
			CustomBalloonTipwpf thisCustomTip = snder as CustomBalloonTipwpf;
			if (VisibleBalloonTipForms.Contains(thisCustomTip))
			{
				int indexOfRemoved = VisibleBalloonTipForms.IndexOf(thisCustomTip);
				double cbtHeight = thisCustomTip.ActualHeight;
				for (int i = indexOfRemoved + 1; i < VisibleBalloonTipForms.Count; i++)
				{
					CustomBalloonTipwpf tmpForm = VisibleBalloonTipForms[i];
					//ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
					//{
						//int StartPoint = VisibleBalloonTipForms[i].Top;
						double EndPoint = tmpForm.Top - cbtHeight;
						while (tmpForm.Top > EndPoint)
						{
							//System.Threading.Thread.Sleep(10);
							//ThreadingInterop.UpdateGuiFromThread(tmpForm, () =>
							//{
								if (tmpForm.Top - 5 > EndPoint) tmpForm.Top -= 5;
								else tmpForm.Top = EndPoint;
							//});

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
					//}, false);
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

		//cbt.Width = Screen.PrimaryScreen.WorkingArea.Width - gapFromSide * 2;
		cbt.Top = Screen.PrimaryScreen.WorkingArea.Top + TopStart - cbt.ActualHeight;
		cbt.Left = Screen.PrimaryScreen.WorkingArea.Left + (Screen.PrimaryScreen.WorkingArea.Width - cbt.Width) / 2;// - 2*gapFromSide)/2;

		cbt.TranslateAnimateWindowRelatively(0.1, cbt.ActualHeight, 0);//200);

		//ShowInactiveTopmost(cbt);
		//cbt.StartTimerForClosing();
		//cbt.Show();
	}

	public void StartTimerForClosing()
	{
		if (timer_ShowDuration.Interval != 0 && (timer_ShowDuration.Tag == null || timer_ShowDuration.Tag.ToString() != "-1"))
		{
			timer_ShowDuration.Tick += delegate
			{
				timer_ShowDuration.Stop();
				Rectangle bounds = new Rectangle(new System.Drawing.Point((int)this.Left, (int)this.Top), new System.Drawing.Size((int)this.ActualWidth, (int)this.ActualHeight));
				//ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				//{
					for (int i = 0; i <= 10; i++)
					{
						Thread.Sleep(20);
						Rectangle checkRectangle = new Rectangle(bounds.X - 15, bounds.Y - 15, bounds.Width + 30, bounds.Height + 30);
						while (checkRectangle.Contains(System.Windows.Forms.Control.MousePosition))
						{ }//Thread.Sleep(1);
						//if (this.InvokeRequired)
						//{
						//  DecreaseOpacityCallback d = new DecreaseOpacityCallback(delegate
						//  {
						//    this.Opacity -= 0.1;
						//  });

						//  this.Invoke(d, new object[] { });
						//}
						//else
						//{
							this.Opacity -= 0.1;
						//}
					}
				//});

				//DONE TODO: Should implement to NOT close if mouse is inside form
				this.Close();
			};
			timer_ShowDuration.Start();
		}
	}

	public void TranslateAnimateWindowRelatively(double animationDurationInSeconds, double topRelativeToCurrent, double leftRelativeToCurrent)
	{
		Duration animationDuration = new Duration(TimeSpan.FromSeconds(animationDurationInSeconds));

		//Rect workingArea = SystemParameters.WorkArea;
		double newTopPosition = this.Top + topRelativeToCurrent;//this.ActualHeight;//workingArea.Top + (workingArea.Height - this.ActualHeight * ActivatedScaleY) / 2;
		double newLeftPosition = this.Left + leftRelativeToCurrent;

		DoubleAnimation topAnimation = new DoubleAnimation()
		{
			From = this.Top,
			To = newTopPosition,
			Duration = animationDuration
		};

		DoubleAnimation leftAnimation = new DoubleAnimation()
		{
			From = this.Left,
			To = newLeftPosition,
			Duration = animationDuration
		};

		Storyboard storyboard = new Storyboard()
		{
			Name = "storyboardFadeout",
			AutoReverse = false,
			RepeatBehavior = new RepeatBehavior(1),
			FillBehavior = FillBehavior.Stop
		};
		Storyboard.SetTargetName(storyboard, this.Name);
		Storyboard.SetTargetProperty(topAnimation, new PropertyPath(Window.TopProperty));
		Storyboard.SetTargetProperty(leftAnimation, new PropertyPath(Window.LeftProperty));
		storyboard.Children.Add(topAnimation);
		storyboard.Children.Add(leftAnimation);

		storyboard.Completed += delegate
		{
			this.Top = newTopPosition;
			this.Left = newLeftPosition;
		};
		storyboard.Begin(this);
	}

	private void customBalloonTipwpf_Loaded(object sender, RoutedEventArgs e)
	{
		StartTimerForClosing();
		//TranslateWindowToCorrectPosition();
	}
}