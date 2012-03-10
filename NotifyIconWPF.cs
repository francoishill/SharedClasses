using System.Windows.Markup;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Input;
using System;
using System.ComponentModel;
using Forms = System.Windows.Forms;
using System.Windows.Media;

namespace SharedClasses
{
	/// <summary>
	/// Represents a thin wrapper for <see cref="Forms.NotifyIcon"/>
	/// </summary>
	[ContentProperty("Text")]
	[DefaultEvent("MouseDoubleClick")]
	public class NotificationAreaIcon : FrameworkElement
	{
		/*		
		Usage is as follows in XAML
		 * using a resource file in the root (of the visual studio project) named "app.ico":
		 * The MenuItem with Text="-" will be a separator
		 * Note also a reference to the System.Windows.Forms & System.Drawing assemblies will be required
		
		 * Add the following to your window's namespace lines:
			xmlns:sharedclasses='clr-namespace:SharedClasses'
			xmlns:forms="clr-namespace:System.Windows.Forms;assembly=System.Windows.Forms"
		 
		 * Inside the Window's first child (by default the Grid), CANNOT be inside the window root
		<sharedclasses:NotificationAreaIcon x:Name='trayIcon'
											Text="Text when hovering over tray icon"
											Icon="app.ico">
			<sharedclasses:NotificationAreaIcon.MenuItems>
				<forms:MenuItem Text="Show"
								Click="OnMenuItemShowClick"
								DefaultItem="True" />
				<forms:MenuItem Text="-" />
				<forms:MenuItem Text="Exit"
								Click="OnMenuItemExitClick" />
			</sharedclasses:NotificationAreaIcon.MenuItems>
		</sharedclasses:NotificationAreaIcon>*/
		System.Windows.Forms.NotifyIcon notifyIcon;

		public static readonly RoutedEvent MouseClickEvent = EventManager.RegisterRoutedEvent(
			"MouseClick", RoutingStrategy.Bubble, typeof(MouseButtonEventHandler), typeof(NotificationAreaIcon));

		public static readonly RoutedEvent MouseDoubleClickEvent = EventManager.RegisterRoutedEvent(
			"MouseDoubleClick", RoutingStrategy.Bubble, typeof(MouseButtonEventHandler), typeof(NotificationAreaIcon));

		//public static readonly RoutedEvent BalloonTipClickedEvent = EventManager.RegisterRoutedEvent(
		//    "BalloonTipClicked", RoutingStrategy.Bubble, typeof(EventHandler), typeof(NotificationAreaIcon));

		public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(ImageSource), typeof(NotificationAreaIcon));

		public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(NotificationAreaIcon));

		public static readonly DependencyProperty FormsContextMenuProperty =
            DependencyProperty.Register("MenuItems", typeof(List<System.Windows.Forms.MenuItem>), typeof(NotificationAreaIcon), new PropertyMetadata(new List<Forms.MenuItem>()));

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);


			// Create and initialize the window forms notify icon based
			notifyIcon = new System.Windows.Forms.NotifyIcon();
			notifyIcon.Text = Text;
			if (!DesignerProperties.GetIsInDesignMode(this))
			{
				notifyIcon.Icon = FromImageSource(Icon);
			}
			notifyIcon.Visible = FromVisibility(Visibility);

			if (this.MenuItems != null && this.MenuItems.Count > 0)
			{
				notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(this.MenuItems.ToArray());
			}

			notifyIcon.MouseDown += OnMouseDown;
			notifyIcon.MouseUp += OnMouseUp;
			notifyIcon.MouseClick += OnMouseClick;
			notifyIcon.MouseDoubleClick += OnMouseDoubleClick;
			notifyIcon.BalloonTipClicked += OnBalloonTipClicked;

			Dispatcher.ShutdownStarted += OnDispatcherShutdownStarted;
		}

		private void OnDispatcherShutdownStarted(object sender, EventArgs e)
		{
			notifyIcon.Dispose();
		}

		private void OnMouseDown(object sender, Forms.MouseEventArgs e)
		{
			OnRaiseEvent(MouseDownEvent, new MouseButtonEventArgs(
				InputManager.Current.PrimaryMouseDevice, 0, ToMouseButton(e.Button)));
		}

		private void OnMouseUp(object sender, Forms.MouseEventArgs e)
		{
			OnRaiseEvent(MouseUpEvent, new MouseButtonEventArgs(
				InputManager.Current.PrimaryMouseDevice, 0, ToMouseButton(e.Button)));
		}

		private void OnMouseDoubleClick(object sender, Forms.MouseEventArgs e)
		{
			OnRaiseEvent(MouseDoubleClickEvent, new MouseButtonEventArgs(
				InputManager.Current.PrimaryMouseDevice, 0, ToMouseButton(e.Button)));
		}

		private void OnBalloonTipClicked(object sender, EventArgs e)
		{
			BalloonTipClicked(sender, e);
			//OnRaiseEvent(BalloonTipClickedEvent, new EventArgs());
		}

		private void OnMouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			OnRaiseEvent(MouseClickEvent, new MouseButtonEventArgs(
				InputManager.Current.PrimaryMouseDevice, 0, ToMouseButton(e.Button)));
		}

		private void OnRaiseEvent(RoutedEvent handler, MouseButtonEventArgs e)
		{
			e.RoutedEvent = handler;
			RaiseEvent(e);
		}

		private void OnRaiseEvent(RoutedEvent handler, RoutedEventArgs e)
		{
			e.RoutedEvent = handler;
			RaiseEvent(e);
		}

		public List<Forms.MenuItem> MenuItems
		{
			get { return (List<Forms.MenuItem>)GetValue(FormsContextMenuProperty); }
			set { SetValue(FormsContextMenuProperty, value); }
		}

		public ImageSource Icon
		{
			get { return (ImageSource)GetValue(IconProperty); }
			set { SetValue(IconProperty, value); }
		}

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		public void ShowBalloonTip(int timeout)
		{
			notifyIcon.ShowBalloonTip(timeout);
		}

		public void ShowBalloonTip(int timeout, string tipTitle, string tipText, Forms.ToolTipIcon tipIcon)
		{
			notifyIcon.ShowBalloonTip(timeout, tipTitle, tipText, tipIcon);
		}

		public event MouseButtonEventHandler MouseClick
		{
			add { AddHandler(MouseClickEvent, value); }
			remove { RemoveHandler(MouseClickEvent, value); }
		}

		public event MouseButtonEventHandler MouseDoubleClick
		{
			add { AddHandler(MouseDoubleClickEvent, value); }
			remove { RemoveHandler(MouseDoubleClickEvent, value); }
		}

		public event EventHandler BalloonTipClicked = new EventHandler(delegate { });
		//{
		//    add { AddHandler(BalloonTipClickedEvent, value); }
		//    remove { RemoveHandler(BalloonTipClickedEvent, value); }
		//}

		#region Conversion members

		private static System.Drawing.Icon FromImageSource(System.Windows.Media.ImageSource icon)
		{
			if (icon == null)
			{
				return null;
			}
			Uri iconUri = new Uri(icon.ToString());
			return new System.Drawing.Icon(Application.GetResourceStream(iconUri).Stream);
		}

		private static bool FromVisibility(Visibility visibility)
		{
			return visibility == Visibility.Visible;
		}

		private MouseButton ToMouseButton(Forms.MouseButtons button)
		{
			switch (button)
			{
				case Forms.MouseButtons.Left:
					return MouseButton.Left;
				case Forms.MouseButtons.Right:
					return MouseButton.Right;
				case Forms.MouseButtons.Middle:
					return MouseButton.Middle;
				case Forms.MouseButtons.XButton1:
					return MouseButton.XButton1;
				case Forms.MouseButtons.XButton2:
					return MouseButton.XButton2;
			}
			throw new InvalidOperationException();
		}

		#endregion
	}
}