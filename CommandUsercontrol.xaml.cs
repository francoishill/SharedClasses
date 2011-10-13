using System;
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
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;

	/// <summary>
	/// Interaction logic for tmpUserControl.xaml
	/// </summary>
public partial class CommandUserControl : UserControl
{
	public event EventHandler AnimationComplete_Closing;
	public event EventHandler AnimationComplete_Activating;

	public UIElement currentFocusedElement = null;

	public CommandUserControl(string CommandTitle)
	{
		InitializeComponent();

		labelTitle.Content = CommandTitle;
	}

	public void ResetLayoutNow()
	{
		if (!(this.LayoutTransform is MatrixTransform)) this.LayoutTransform = new MatrixTransform();
		if (!(this.RenderTransform is MatrixTransform)) this.RenderTransform = new MatrixTransform();
		if (this.Opacity != 1) this.Opacity = 1;
	}

	public void AddControl(string label, System.Windows.Controls.Control control, Color labelColor)
	{
		Label labelControl = new Label() { Content = label, Margin = new Thickness(3, 5, 3, 0), MinWidth = 50, Foreground = Brushes.White };

		AddRowToGrid();

		gridTable.Children.Add(labelControl);
		Grid.SetColumn(labelControl, 0);
		Grid.SetRow(labelControl, gridTable.RowDefinitions.Count - 1);

		gridTable.Children.Add(control);
		Grid.SetColumn(control, 1);
		Grid.SetRow(control, gridTable.RowDefinitions.Count - 1);
	}

	private void AddRowToGrid()
	{
		gridTable.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
	}

	public void AddTreeviewItem(object itemToAdd)
	{
		if (tmpTreeview == null)
		{
			tmpTreeview = new TreeView() { MinWidth = 100, MinHeight = 20, MaxHeight = 40, VerticalAlignment = System.Windows.VerticalAlignment.Top };
			gridTable.Children.Add(tmpTreeview);
			Grid.SetColumn(tmpTreeview, 2);
			if (gridTable.RowDefinitions.Count == 0) AddRowToGrid();
			Grid.SetRow(tmpTreeview, 0);
			Grid.SetRowSpan(tmpTreeview, 1000);
		}
		tmpTreeview.Items.Add(itemToAdd);
	}
	TreeView tmpTreeview = null;

	private void border_Closebutton_MouseUp(object sender, MouseButtonEventArgs e)
	{
		CloseControl();
	}

	public void CloseControl()
	{
		this.LayoutTransform = new ScaleTransform();
		DoubleAnimation opacityAnimation = new DoubleAnimation() { To = OpacityWhenClosed };
		DoubleAnimation scaleyAnimation = new DoubleAnimation() { To = ScaleFactorWhenClosed };
		DoubleAnimation scalexAnimation = new DoubleAnimation() { To = ScaleFactorWhenClosed };

		Storyboard storyboard = new Storyboard()
		{
			Name = "storyboardFadeout",
			AutoReverse = false,
			RepeatBehavior = new RepeatBehavior(1),
			Duration = new Duration(TimeSpan.FromSeconds(0.8)),
			FillBehavior = FillBehavior.Stop
		};
		Storyboard.SetTargetName(storyboard, parentUsercontrol.Name);
		Storyboard.SetTargetProperty(opacityAnimation, (PropertyPath)new PropertyPathConverter().ConvertFromString("Opacity"));
		Storyboard.SetTargetProperty(scaleyAnimation, (PropertyPath)new PropertyPathConverter().ConvertFromString("(FrameworkElement.LayoutTransform).(ScaleTransform.ScaleY)"));
		Storyboard.SetTargetProperty(scalexAnimation, (PropertyPath)new PropertyPathConverter().ConvertFromString("(FrameworkElement.LayoutTransform).(ScaleTransform.ScaleX)"));
		storyboard.Children.Add(opacityAnimation);
		storyboard.Children.Add(scaleyAnimation);
		storyboard.Children.Add(scalexAnimation);

		storyboard.Completed += delegate
		{
			if (AnimationComplete_Closing != null)
				AnimationComplete_Closing(this, new EventArgs());
			this.Opacity = OpacityWhenClosed;
			this.LayoutTransform = new ScaleTransform(ScaleFactorWhenClosed, ScaleFactorWhenClosed);
			//this.Opacity = findResource("ScaleFactorWhenClosed")
		};
		storyboard.Begin(this);

		//while (1==1)//storyboard.GetCurrentState == 
		//  System.Windows.Forms.MessageBox.Show(storyboard.GetCurrentState(this).ToString());
		//this.RenderTransform = new ScaleTransform(1, 1, this.ActualWidth / 2, this.ActualHeight / 2);
		//this.Opacity = System.Windows.Visibility.Collapsed;
	}

	public void ActivateControl()
	{
		this.RenderTransform = new MatrixTransform();
		this.LayoutTransform = new ScaleTransform();
		DoubleAnimation opacityAnimation = new DoubleAnimation() { To = 1 };
		DoubleAnimation scaleyAnimation = new DoubleAnimation() { To = 1 };
		DoubleAnimation scalexAnimation = new DoubleAnimation() { To = 1 };

		Storyboard storyboard = new Storyboard()
		{
			Name = "storyboardFadeout",
			AutoReverse = false,
			RepeatBehavior = new RepeatBehavior(1),
			Duration = new Duration(TimeSpan.FromSeconds(0.8)),
			FillBehavior = FillBehavior.Stop
		};
		Storyboard.SetTargetName(storyboard, parentUsercontrol.Name);
		Storyboard.SetTargetProperty(opacityAnimation, (PropertyPath)new PropertyPathConverter().ConvertFromString("Opacity"));
		Storyboard.SetTargetProperty(scaleyAnimation, (PropertyPath)new PropertyPathConverter().ConvertFromString("(FrameworkElement.LayoutTransform).(ScaleTransform.ScaleY)"));
		Storyboard.SetTargetProperty(scalexAnimation, (PropertyPath)new PropertyPathConverter().ConvertFromString("(FrameworkElement.LayoutTransform).(ScaleTransform.ScaleX)"));
		storyboard.Children.Add(opacityAnimation);
		storyboard.Children.Add(scaleyAnimation);
		storyboard.Children.Add(scalexAnimation);

		storyboard.Completed += delegate
		{
			if (AnimationComplete_Activating != null)
				AnimationComplete_Activating(this, new EventArgs());
			ResetLayoutNow();
			//this.Opacity = findResource("ScaleFactorWhenClosed")
		};
		storyboard.Begin(this);

		//while (1==1)//storyboard.GetCurrentState == 
		//  System.Windows.Forms.MessageBox.Show(storyboard.GetCurrentState(this).ToString());
		//this.RenderTransform = new ScaleTransform(1, 1, this.ActualWidth / 2, this.ActualHeight / 2);
		//this.Opacity = System.Windows.Visibility.Collapsed;
	}

	/// <summary>
	/// Use in XAML as follows: Opacity="{x:Static local:CommandUserControl.ScaleFactorWhenClosed}"
	/// </summary>
	public double ScaleFactorWhenClosed { get { return 0.3; } }
	public double OpacityWhenClosed { get { return 0.1; } }

	//public static readonly double ScaleFactorWhenClosed = 0.1;
	//public static readonly double OpacityWhenClosed = 0.3;
	private void storyboardFadeout_Completed(object sender, EventArgs e)
	{
		//System.Windows.Forms.MessageBox.Show(findResource("ScaleFactorWhenClosed").ToString());
		//double scaleFactorWhenclosed = double.Parse(findResource("ScaleFactorWhenClosed").ToString());
		//this.RenderTransform = new ScaleTransform(scaleFactorWhenclosed, scaleFactorWhenclosed);
		//this.Visibility = System.Windows.Visibility.Collapsed;
		//this.LayoutTransform = new ScaleTransform(0.1, 0.1);
	}

	private object findResource(string name)
	{
		return this.FindResource(name);
	}

	private void parentUsercontrol_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		this.Focus();
		if (this.Tag != null && this.Tag is OverlayWindow.OverlayChildManager && (this.Tag as OverlayWindow.OverlayChildManager).FirstUIelementToFocus != null)
		{
			if (currentFocusedElement == null)
			{
				(this.Tag as OverlayWindow.OverlayChildManager).FirstUIelementToFocus.Focus();
				currentFocusedElement = (this.Tag as OverlayWindow.OverlayChildManager).FirstUIelementToFocus;
			}
			else
			{
				currentFocusedElement.Focus();
			}
		}
		
		//System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
		//timer.Interval = 100;
		//timer.Tick += delegate
		//{
		//  DoLargeScale();
		//  timer.Stop();
		//  timer.Dispose();
		//  timer = null;
		//};
		//timer.Start();
	}

	private void parentUsercontrol_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		//DoLargeScale();
	}

	private bool LargeScalingWasDone = false;
	private void parentUsercontrol_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
	{
		//DoLargeScale();
	}

	private void DoLargeScale()
	{
		//if (this.RenderTransform is MatrixTransform)
		//{
		//this.Opacity = 1;
		Canvas.SetZIndex(this, 99);

		TransformGroup transformGroup = new TransformGroup();
		//transformGroup.Children.Add(new TranslateTransform(100, 100));
		transformGroup.Children.Add(new ScaleTransform(3, 3));
		transformGroup.Children.Add(new SkewTransform(10, 10));
		//this.LayoutTransform = transformGroup;
		this.RenderTransform = transformGroup;

		LargeScalingWasDone = true;
		//}
		////else if (this.LayoutTransform is ScaleTransform)
		////{
		////  ScaleTransform scaleTransform = this.LayoutTransform as ScaleTransform;
		////  System.Windows.Forms.MessageBox.Show(scaleTransform.ScaleX + ", " + scaleTransform.ScaleY);
		////}
	}

	private void parentUsercontrol_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
	{
		//ScaleToOriginal();
	}

	private void ScaleToOriginal()
	{
		if (LargeScalingWasDone)
		{
			//this.LayoutTransform = new MatrixTransform();
			this.RenderTransform = new MatrixTransform();
			Canvas.SetZIndex(this, 0);
			this.UpdateLayout();
		}
	}

	private void parentUsercontrol_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (this.Opacity != 1)
		{
			ActivateControl();
		}
	}

    private void parentUsercontrol_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            e.Handled = true;
            string[] filesDropped = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            foreach (string filedropped in filesDropped)
                UserMessages.ShowInfoMessage("File " + filedropped + " was dropped onto " + labelTitle.Content.ToString());
        }
    }

	//private void storyboardFadein_Completed(object sender, EventArgs e)
	//{

	//}

	//private void mainGrid_GotFocus(object sender, RoutedEventArgs e)
	//{
	//  System.Windows.Forms.MessageBox.Show("Test");
	//  if (this.Tag != null && this.Tag is Control)
	//    (this.Tag as Control).Focus();
	//}
}