using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
//using System.Drawing;
using SystemIcons = System.Drawing.SystemIcons;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for tmpUserControl.xaml
	/// </summary>
	public partial class CustomBalloonTipwpf : Window
	{
		//private System.Windows.Forms.Timer timer_ShowDuration = new System.Windows.Forms.Timer();
		//public string KeyForForm;

		public delegate void SimpleDelegateWithSender(object returnObject);
		public enum IconTypes { Error, Information, Question, Shield, Warning, None };
		public CustomBalloonTipwpf()//string Title, string Message, int Duration, IconTypes iconType, SimpleDelegateWithSender OnClickCallback, bool OnClickCallbackOnSeparateThread = true)
		{
			//TODO: Start moving notifications over from separate windows to one window and separate user controls.
			InitializeComponent();

			listBox1.ItemsSource = VisibleBalloonTipForms;

			//this.label_Title.Content = Title;
			//this.label_Message.Content = Message;
			//if (Duration > 0) this.timer_ShowDuration.Interval = Duration;
			//else this.timer_ShowDuration.Tag = -1;

			//Imaging.CreateBitmapSourceFromHIcon(System.Drawing.SystemIcons.Information.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

			//switch (iconType)
			//{
			//	case IconTypes.Error: this.pictureBox_Icon.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Error.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()); break;
			//	case IconTypes.Information: this.pictureBox_Icon.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Information.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()); break;
			//	case IconTypes.Question: this.pictureBox_Icon.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Question.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()); break;
			//	case IconTypes.Shield: this.pictureBox_Icon.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Shield.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()); break;
			//	case IconTypes.Warning: this.pictureBox_Icon.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Warning.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()); break;
			//	default: this.pictureBox_Icon.Visibility = System.Windows.Visibility.Collapsed; break;
			//}

			//this.MouseLeftButtonUp += (snder, evtargs) =>
			//{
			//	(snder as CustomBalloonTipwpf).Close();

			//	if (OnClickCallback != null)
			//	{
			//		if (OnClickCallbackOnSeparateThread)
			//			ThreadingInterop.PerformVoidFunctionSeperateThread(() => { OnClickCallback.Invoke((snder as CustomBalloonTipwpf).KeyForForm); });
			//		else
			//			OnClickCallback.Invoke((snder as CustomBalloonTipwpf).KeyForForm);
			//	}
			//};
		}

		private static ImageSource GetImageSourceFromIconType(IconTypes IconType)
		{
			switch (IconType)
			{
				case IconTypes.Error: return Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Error.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
				case IconTypes.Information: return Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Information.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
				case IconTypes.Question: return Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Question.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
				case IconTypes.Shield: return Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Shield.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
				case IconTypes.Warning: return Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Warning.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
				default: return null;
			}
		}

		public class CustomBalloonTipClass
		{
			public string Title { get; set; }
			public string Message { get; set; }
			public ImageSource Icon { get; set; }
			public int Duration { get; set; }
			public SimpleDelegateWithSender OnClickCallback { get; set; }
			public bool OnClickCallbackOnSeparateThread { get; set; }
			public Transform LayoutTransformation { get; set; }
			//public bool IsMouseInside { get; set; }
			public CustomBalloonTipClass(string Title, string Message, ImageSource Icon, int Duration, SimpleDelegateWithSender OnClickCallback, bool OnClickCallbackOnSeparateThread, Transform LayoutTransformation = null)
			{
				this.Title = Title;
				this.Message = Message;
				this.Icon = Icon;
				this.Duration = Duration;
				this.OnClickCallback = OnClickCallback;
				this.OnClickCallbackOnSeparateThread = OnClickCallbackOnSeparateThread;
				this.LayoutTransformation = LayoutTransformation;

				//this.IsMouseInside = false;
			}
		}

		public void StartTimerToRemoveItem(CustomBalloonTipClass item)
		{
			System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
			timer.Interval = item.Duration;
			timer.Tick += (snder, evtargs) =>
			{
				(snder as System.Windows.Forms.Timer).Stop();
				(snder as System.Windows.Forms.Timer).Dispose();

				//ListBoxItem lbi = listBox1.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
				//while (lbi != null && lbi.IsMouseOver)
				//while (item != null && item.IsMouseInside)
				//Will loop (and process messages) while the mouse is over one of the items (which are inside the wrappanel)
				ItemsPresenter itemsPresenter = FindVisualChild<ItemsPresenter>(listBox1);
				WrapPanel itemsPanelWrapPanel = FindVisualChild<WrapPanel>(itemsPresenter);
				while (itemsPanelWrapPanel.IsMouseOver)
					System.Windows.Forms.Application.DoEvents();

				ListBoxItem lbi = listBox1.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
				ContentPresenter myContentPresenter = FindVisualChild<ContentPresenter>(lbi);

				// Finding textBlock from the DataTemplate that is set on that ContentPresenter
				if (myContentPresenter == null)
					VisibleBalloonTipForms.Remove(item);
				else
				{
					DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
					Border myBorder = (Border)myDataTemplate.FindName("ItemMainBorder", myContentPresenter);

					FadeItemStoryboadAndPerformOnComplete(myBorder, delegate { VisibleBalloonTipForms.Remove(item); });
				}

				//VisibleBalloonTipForms.Remove(item);
			};
			timer.Start();
		}

		public void FadeItemStoryboadAndPerformOnComplete(FrameworkElement frameworkElement, SimpleDelegateWithSender actionWithSender)
		{
			if (!(frameworkElement.LayoutTransform is ScaleTransform))
				frameworkElement.LayoutTransform = new ScaleTransform(1, 1);

			Storyboard storyboard = new Storyboard();
			DoubleAnimation growAnimationX = new DoubleAnimation(1, 0.1, TimeSpan.FromMilliseconds(100));
			DoubleAnimation growAnimationY = new DoubleAnimation(1, 0.1, TimeSpan.FromMilliseconds(100));
			storyboard.Children.Add(growAnimationX);
			storyboard.Children.Add(growAnimationY);

			Storyboard.SetTargetProperty(growAnimationX, new PropertyPath("LayoutTransform.ScaleX"));
			Storyboard.SetTargetProperty(growAnimationY, new PropertyPath("LayoutTransform.ScaleY"));
			Storyboard.SetTarget(growAnimationX, frameworkElement);
			Storyboard.SetTarget(growAnimationY, frameworkElement);

			storyboard.Completed += (snder, evtargs) =>
			{
				actionWithSender(snder);
			};

			storyboard.Begin();
		}

		private static T FindVisualChild<T>(DependencyObject parent) where T : Visual
		{
			T child = default(T);

			if (parent == null)
				return null;
			int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < numVisuals; i++)
			{
				Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
				child = v as T;
				if (child == null)
				{
					child = FindVisualChild<T>(v);
				}
				if (child != null)
				{
					break;
				}
			}
			return child;
		}

		public static CustomBalloonTipwpf StaticInstance;
		private ObservableCollection<CustomBalloonTipClass> visibleBalloonTipForms;
		public ObservableCollection<CustomBalloonTipClass> VisibleBalloonTipForms { get { if (visibleBalloonTipForms == null) visibleBalloonTipForms = new ObservableCollection<CustomBalloonTipClass>(); return visibleBalloonTipForms; } set { visibleBalloonTipForms = value; } }
		public static void ShowCustomBalloonTip(string Title, string Message, int Duration, IconTypes iconType, SimpleDelegateWithSender OnClickCallback = null, string keyForForm = null, bool CallbackOnSeparateThread = false, double Scaling = 1)
		{
			if (StaticInstance == null)
				StaticInstance = new CustomBalloonTipwpf();//"", "", 0, IconTypes.Warning, null);

			CustomBalloonTipClass cbt = new CustomBalloonTipClass(
				Title,
				Message,
				GetImageSourceFromIconType(iconType),
				Duration,
				OnClickCallback,
				CallbackOnSeparateThread,
				new ScaleTransform(Scaling, Scaling)//1,1)
				//new ScaleTransform(StaticInstance.VisibleBalloonTipForms.Count+1, StaticInstance.VisibleBalloonTipForms.Count+1)//Scaling, Scaling)
				);
			StaticInstance.VisibleBalloonTipForms.Add(cbt);
			StaticInstance.Show();
			StaticInstance.Activate();
			StaticInstance.BringIntoView();

			StaticInstance.StartTimerToRemoveItem(cbt);

			////DONE: Also think about moving all these notifications into usercontrols in one MAIN window
			//CustomBalloonTipwpf cbt = new CustomBalloonTipwpf(Title, Message, Duration, iconType, OnClickCallback, CallbackOnSeparateThread);
			//if (LayoutTransformation != 1) cbt.mainBorder.LayoutTransform = new ScaleTransform(LayoutTransformation, LayoutTransformation);
			//cbt.KeyForForm = keyForForm;
			////this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Right - this.Width, Screen.PrimaryScreen.WorkingArea.Bottom - this.Height);
			//double TopStart = 0;
			//Console.WriteLine("Count = " + VisibleBalloonTipForms.Count);
			//foreach (CustomBalloonTipwpf tmpVisibleFrms in VisibleBalloonTipForms)
			//	if (tmpVisibleFrms != null && tmpVisibleFrms.Visibility == Visibility.Visible)
			//		TopStart += tmpVisibleFrms.ActualHeight;
			////int gapFromSide = 100;
			////cbt.Location = new Point(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Left + gapFromSide, Screen.PrimaryScreen.WorkingArea.Top + TopStart - cbt.Height);
			////cbt.ActualWidth = Screen.PrimaryScreen.WorkingArea.Width - gapFromSide * 2;

			//cbt.Closed += new EventHandler(cbt_Closed);

			////cbt.Closed += (snder, evtargs) =>
			////{

			////};

			//IntPtr currentActiveWindow = Win32Api.GetForegroundWindow();
			//VisibleBalloonTipForms.Add(cbt);
			//cbt.Show();
			//if (Win32Api.GetForegroundWindow() != currentActiveWindow)
			//	Win32Api.SetForegroundWindow(currentActiveWindow);

			////cbt.Width = Screen.PrimaryScreen.WorkingArea.Width - gapFromSide * 2;
			//cbt.Top = Screen.PrimaryScreen.WorkingArea.Top + TopStart - cbt.ActualHeight;
			//cbt.Left = Screen.PrimaryScreen.WorkingArea.Left + (Screen.PrimaryScreen.WorkingArea.Width - cbt.Width) / 2;// - 2*gapFromSide)/2;

			//cbt.TranslateAnimateWindowRelatively(0.1, cbt.ActualHeight, 200);

			////ShowInactiveTopmost(cbt);
			////cbt.StartTimerForClosing();
			////cbt.Show();
		}

		public static bool AllowToClose = false;
		private void customBalloonTipwpf_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!AllowToClose)
			{
				e.Cancel = true;
				this.Hide();
			}
		}

		private void Border_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if ((sender is Border) && (sender as Border).DataContext is CustomBalloonTipClass)
			{
				CustomBalloonTipClass bt = (sender as Border).DataContext as CustomBalloonTipClass;
				if (bt.OnClickCallback != null)
				{
					VisibleBalloonTipForms.Remove(bt);
					if (bt.OnClickCallbackOnSeparateThread)
						ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
						{
							bt.OnClickCallback(bt);
						});
					else
						bt.OnClickCallback(bt);					
				}
			}
		}
	}

	public class WrapPanelParentHeightToHeightConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (!(value is double))
				return 100;
			return (double)value - 10;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}