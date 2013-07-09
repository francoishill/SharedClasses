﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Windows.Media.Animation;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;
using Microsoft.Win32;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class WpfNotificationWindow : Window
	{
		private static ObservableCollection<NotificationClass> _notifications;// = new ObservableCollection<NotificationClass>();
		private static ObservableCollection<NotificationClass> notifications
		{
			get
			{
				if (_notifications == null)
				{
					_notifications = new ObservableCollection<NotificationClass>();
					//_notificationCreatedThread = Thread.CurrentThread;
				}
				return _notifications;
			}
		}
		//private static Thread _notificationCreatedThread;
		//private static Thread notificationCreatedThread
		//{
		//    get
		//    {
		//        var not = notifications;//Initializes the _notificationCreatedThread
		//        return _notificationCreatedThread;
		//    }
		//}

		private static string CurrentVersionString = null;
		private static WpfNotificationWindow notificationWindow;
		private static System.Threading.Timer timerToCheckForTimedOutNotifications;
		//private List<NotificationClass> notificationWithTimeouts = new List<NotificationClass>();

		public static void SetCurrentVersionDisplayed(string versionString)
		{
			CurrentVersionString = versionString;
			if (notificationWindow != null)
			{
				notificationWindow.Dispatcher.Invoke((Action)delegate
				{
					notificationWindow.versionStringLabel.Content = versionString;
					notificationWindow.versionStringLabel.Visibility = Visibility.Visible;
				});
			}
		}

		public WpfNotificationWindow()
		{
			InitializeComponent();

			Action actionOnAppClose = delegate
			{
				WpfNotificationWindow.CloseNotificationWindow();
				if (thread != null && thread.IsAlive)
					thread.Abort();
			};

#if WPF
			if (Application.Current != null)//It is an WPF application
				Application.Current.Dispatcher.Invoke((Action)delegate
				{
					Application.Current.SessionEnding += (sn, ev) => actionOnAppClose();
				});
#else
			System.Windows.Forms.Application.ApplicationExit += (sn, ev) => actionOnAppClose();
#endif

			this.Top = SystemParameters.WorkArea.Top;
			this.Left = SystemParameters.WorkArea.Left;

			this.Height = SystemParameters.WorkArea.Height;// +7;
			this.Width = SystemParameters.WorkArea.Width;// +7;

			notifications.CollectionChanged += delegate
			{
				Dispatcher.Invoke((Action)delegate
				{
					buttonCloseAllNotifications.Visibility = notifications.Count > 1 ? Visibility.Visible : Visibility.Hidden;
					versionStringLabel.Visibility = buttonCloseAllNotifications.Visibility;
				});
			};
			InitializeTimerToCheckForNotificationTimeouts();
		}

		private void InitializeTimerToCheckForNotificationTimeouts()
		{
			timerToCheckForTimedOutNotifications = new System.Threading.Timer(
				new System.Threading.TimerCallback(
					(obj) =>
					{
						DateTime startupTime;
						TimeSpan idleDuration;
						if (!Win32Api.GetLastInputInfo(out startupTime, out idleDuration)
							|| idleDuration.TotalSeconds < TimeSpan.FromSeconds(1).TotalSeconds)
						{
							DateTime now = DateTime.Now;
							for (int i = notifications.Count - 1; i >= 0; i--)
								if (notifications[i].TimeWhenTimedOut.HasValue && now.Subtract(notifications[i].TimeWhenTimedOut.Value).TotalSeconds > 0)
									//Notification has timed out and will close now
									InvokeFromSeparateThread((notif) => FadeOutAndRemoveNotificationFromBorder(GetBorderOfNotification(notif as NotificationClass), false), notifications[i]);
						}
					}),
				null,
				TimeSpan.FromMilliseconds(500),
				TimeSpan.FromMilliseconds(500));
		}

		private static Thread thread;
		public static void ShowNotification(
			string title, string message,
			ShowNoCallbackNotificationInterop.NotificationTypes notificationType,
			TimeSpan? timeout,
			Action<object> leftClickCallback, object leftClickCallbackArgument,
			Action<object> rightClickCallback, object rightClickCallbackArgument,
			Action<object> middleClickCallback, object middleClickCallbackArgument,
			Action<object, bool> onCloseCallback_WasClickedToCallback, object onCloseCallbackArgument)
		{
			thread = new Thread(() =>
			{
				bool justcreated = false;
				if (notificationWindow == null)
				{
					justcreated = true;
					notificationWindow = new WpfNotificationWindow();
					if (CurrentVersionString != null)
						SetCurrentVersionDisplayed(CurrentVersionString);
				}
				notificationWindow.Dispatcher.Invoke(
					(Action)delegate
					{
						var notif = new NotificationClass(
							//notifications.Count + " " + title, message,
							title, message,
							notificationType,
							timeout,
							leftClickCallback, leftClickCallbackArgument,
							rightClickCallback, rightClickCallbackArgument,
							middleClickCallback, middleClickCallbackArgument,
							onCloseCallback_WasClickedToCallback, onCloseCallbackArgument);

						try
						{
							if (justcreated)
								notificationWindow.listboxNotificationList.ItemsSource = notifications;
							notificationWindow.BringIntoView();
							notificationWindow.Topmost = !notificationWindow.Topmost;
							notificationWindow.Topmost = !notificationWindow.Topmost;
							notifications.Insert(0, notif);
							if (!notificationWindow.IsVisible)
								notificationWindow.ShowDialog();
						}
						catch
						{
						}
					});

			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			//notificationWindow.listboxNotificationList.UpdateLayout();
			//if (timeout.HasValue)
			//{
			//    new System.Threading.Timer(
			//        new System.Threading.TimerCallback(
			//            not => notificationWindow.InvokeFromSeparateThread((not1) => notificationWindow.FadeOutAndRemoveNotificationFromBorder(notificationWindow.GetBorderOfNotification(not1 as NotificationClass)), not)),
			//        notif,
			//        timeout.Value,
			//        TimeSpan.FromMilliseconds(-1));
			//}
		}

		protected void InvokeFromSeparateThread(Action<object> action, object arg1)
		{
			Dispatcher.Invoke(action, arg1);
		}

		public static void ShowNotification(
			string message,
			ShowNoCallbackNotificationInterop.NotificationTypes notificationType = ShowNoCallbackNotificationInterop.NotificationTypes.Subtle,
			TimeSpan? timeout = null,
			Action<object> leftClickCallback = null, object leftClickCallbackArgument = null,
			Action<object> rightClickCallback = null, object rightClickCallbackArgument = null,
			Action<object> middleClickCallback = null, object middleClickCallbackArgument = null,
			Action<object, bool> onCloseCallback_WasClickedToCallback = null, object onCloseCallbackArgument = null,
			string title = null)
		{
			ShowNotification(
				title ?? "Notification", message,
				notificationType,
				timeout,
				leftClickCallback, leftClickCallbackArgument,
				rightClickCallback, rightClickCallbackArgument,
				middleClickCallback, middleClickCallbackArgument,
				onCloseCallback_WasClickedToCallback, onCloseCallbackArgument);
		}

		public static void CloseNotificationWindow()
		{
			if (notificationWindow != null)
			{
				try
				{
					notificationWindow.Dispatcher.Invoke((Action)delegate
					{
						notificationWindow.Close();
						notificationWindow = null;
					});
				}
				catch //(Exception exc)
				{ }
			}
		}

		private void listboxNotificationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			listboxNotificationList.SelectedItem = null;
		}

		private void NotificationBorder_MouseDown(object sender, MouseButtonEventArgs e)
		{
			Border notifBorder = sender as Border;
			if (notifBorder == null) return;

			NotificationClass notif1 = notifBorder.DataContext as NotificationClass;
			
			//TODO: This was removed, otherwise close callback does not fire when clicked on the Notification itsself
			//if (notif1 != null)
			//    notif1.RemoveCloseCallback();

			//This happens if clicked inside border, not close button
			var notif = FadeOutAndRemoveNotificationFromBorder(
				notifBorder,
				e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right || e.ChangedButton == MouseButton.Middle);
			//notifications.Remove(notif);
			switch (e.ChangedButton)
			{
				//Perform the actions on a separate thread, otherwise they get terminated with the thread if the window closes
				case MouseButton.Left:
					if (notif.LeftClickCallback != null)
						ThreadingInterop.PerformOneArgFunctionSeperateThread(notif.LeftClickCallback, notif.LeftClickCallbackArgument);
					break;
				case MouseButton.Middle:
					if (notif.MiddleClickCallback != null)
						ThreadingInterop.PerformOneArgFunctionSeperateThread(notif.MiddleClickCallback, notif.MiddleClickCallbackArgument);
					break;
				case MouseButton.Right:
					if (notif.RightClickCallback != null)
						ThreadingInterop.PerformOneArgFunctionSeperateThread(notif.RightClickCallback, notif.RightClickCallbackArgument);
					break;
			}
		}

		private NotificationClass FadeOutAndRemoveNotificationFromBorder(Border borderWithNotificationDataContext, bool wasClosedViaClick)
		{
			if (borderWithNotificationDataContext == null)
				return null;
			NotificationClass notif = borderWithNotificationDataContext.DataContext as NotificationClass;
			if (notif == null) return null;

			notif.WasClosedViaClick = wasClosedViaClick;
			Storyboard sb = (Storyboard)this.FindResource("FadeOutStoryboard");
			DoubleAnimation da = sb.Children[0] as DoubleAnimation;
			Storyboard.SetTarget(da, borderWithNotificationDataContext);
			sb.Begin();
			return notif;
		}

		private void FadeOutStoryboard_Completed(object sender, EventArgs e)
		{
			ClockGroup cg = sender as ClockGroup;
			if (cg == null) return;
			if (cg.Children.Count == 0) return;
			DoubleAnimation da = cg.Timeline.Children[0] as DoubleAnimation;
			if (da == null) return;
			Border border = Storyboard.GetTarget(da) as Border;
			if (border == null) return;
			NotificationClass notif = border.DataContext as NotificationClass;
			if (notif == null) return;
			if (notif.OnCloseCallback_WasClickedToCallback != null)
				notif.OnCloseCallback_WasClickedToCallback(notif.OnCloseCallbackArgument, notif.WasClosedViaClick);
			notifications.Remove(notif);

			notif.Dispose();
			notif = null;
		}

		private void NotificationClosebutton_Click(object sender, RoutedEventArgs e)
		{
			Button closeBut = sender as Button;
			if (closeBut == null) return;

			FrameworkElement parent = closeBut.Parent as FrameworkElement;
			while (parent != null && !(parent is Border))
				parent = parent.Parent as FrameworkElement;
			Border border = parent as Border;
			if (border == null) return;
			FadeOutAndRemoveNotificationFromBorder(border, false);//Was closed via close button, not clicking on notification
		}

		bool busy = false;
		private void closeAllButton_Click(object sender, RoutedEventArgs e)
		{
			if (busy)
				return;

			busy = true;
			Environment.Exit(0);//Application.Current.Shutdown(0);
			
			int cnt = notifications.Count;
			for (int i = 0; i < cnt; i++)
			{
				var notif = notifications[0];//Just take first element every time
				if (notif.OnCloseCallback_WasClickedToCallback != null)
					notif.OnCloseCallback_WasClickedToCallback(notif.OnCloseCallbackArgument, false);
				notifications.Remove(notif);
			}
			//notifications.Clear();
			busy = false;
		}

		protected Border GetBorderOfNotification(NotificationClass notificationItem)
		{
			ListBoxItem myListBoxItem = listboxNotificationList.ItemContainerGenerator.ContainerFromItem(notificationItem) as ListBoxItem;
			if (myListBoxItem == null) return null;
			ContentPresenter myContentPresenter = FindVisualChild<ContentPresenter>(myListBoxItem);
			DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
			return myDataTemplate.FindName("NotificationMainBorder", myContentPresenter) as Border;
		}

		public static T FindVisualChild<T>(DependencyObject depObj) where T : DependencyObject
		{
			if (depObj != null)
			{
				for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
				{
					DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
					if (child != null && child is T)
					{
						return (T)child;
					}

					T childItem = FindVisualChild<T>(child);
					if (childItem != null) return childItem;
				}
			}
			return null;
		}
	}

	public class NotificationClass : IDisposable
	{
		public string Title { get; private set; }
		public string Message { get; private set; }
		private ShowNoCallbackNotificationInterop.NotificationTypes NotificationType { get; set; }

		public Action<object> LeftClickCallback { get; private set; }
		public object LeftClickCallbackArgument { get; private set; }

		public Action<object> RightClickCallback { get; private set; }
		public object RightClickCallbackArgument { get; private set; }

		public Action<object> MiddleClickCallback { get; private set; }
		public object MiddleClickCallbackArgument { get; private set; }

		public Action<object, bool> OnCloseCallback_WasClickedToCallback { get; private set; }
		public object OnCloseCallbackArgument { get; private set; }
		public DateTime? TimeWhenTimedOut { get; private set; }

		public bool WasClosedViaClick = false;

		public SolidColorBrush TitleFontColor
		{
			get
			{
				switch (NotificationType)
				{
					case ShowNoCallbackNotificationInterop.NotificationTypes.Subtle:
						return new SolidColorBrush(Color.FromArgb(150, 200, 200, 200));
					case ShowNoCallbackNotificationInterop.NotificationTypes.Info:
						return new SolidColorBrush(Colors.Yellow);
					case ShowNoCallbackNotificationInterop.NotificationTypes.Success:
						return new SolidColorBrush(Color.FromRgb(40, 200, 40));
					case ShowNoCallbackNotificationInterop.NotificationTypes.Warning:
						return new SolidColorBrush(Colors.Orange);
					case ShowNoCallbackNotificationInterop.NotificationTypes.Error:
						return new SolidColorBrush(Colors.Red);
					default:
						return new SolidColorBrush(Color.FromArgb(150, 200, 200, 200));
				}
			}
		}
		public SolidColorBrush MessageFontColor { get { return new SolidColorBrush(Colors.White); } }

		public NotificationClass(
			string Title, string Message,
			ShowNoCallbackNotificationInterop.NotificationTypes NotificationType,
			TimeSpan? TimeOut,
			Action<object> LeftClickCallback, object LeftClickCallbackArgument,
			Action<object> RightClickCallback, object RightClickCallbackArgument,
			Action<object> MiddleClickCallback, object MiddleClickCallbackArgument,
			Action<object, bool> OnCloseCallback_WasClickedToCallback, object OnCloseCallbackArgument)
		{
			this.Title = Title;
			this.Message = Message;
			this.NotificationType = NotificationType;

			this.TimeWhenTimedOut = null;
			if (TimeOut.HasValue)
				this.TimeWhenTimedOut = DateTime.Now.Add(TimeOut.Value);

			this.LeftClickCallback = LeftClickCallback;
			this.LeftClickCallbackArgument = LeftClickCallbackArgument;
			this.RightClickCallback = RightClickCallback;
			this.RightClickCallbackArgument = RightClickCallbackArgument;
			this.MiddleClickCallback = MiddleClickCallback;
			this.MiddleClickCallbackArgument = MiddleClickCallbackArgument;

			this.OnCloseCallback_WasClickedToCallback = OnCloseCallback_WasClickedToCallback;
			this.OnCloseCallbackArgument = OnCloseCallbackArgument;
		}
		~NotificationClass()
		{
			Dispose(false);
		}

		public void RemoveCloseCallback() { OnCloseCallback_WasClickedToCallback = null; }

		private bool IsDisposed=false;
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected void Dispose(bool Diposing)
		{
			if (!IsDisposed)
			{
				Title = null;
				Message = null;
				LeftClickCallback = null;
				LeftClickCallbackArgument = null;
				MiddleClickCallback = null;
				MiddleClickCallbackArgument = null;
				RightClickCallback = null;
				RightClickCallbackArgument = null;
			}
			IsDisposed = true;
		}
	}
}
