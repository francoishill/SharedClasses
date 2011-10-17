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
using System.IO;
using System.Windows.Media.Effects;
using System.Windows.Interop;

/// <summary>
/// Interaction logic for tmpUserControl.xaml
/// </summary>
public partial class CommandUserControl : UserControl
{
	public event EventHandler AnimationComplete_Closing;
	public event EventHandler AnimationComplete_Activating;

	public UIElement currentFocusedElement = null;

	/// <summary>
	/// Use in XAML as follows: Opacity="{x:Static local:CommandUserControl.ScaleFactorWhenClosed}"
	/// </summary>
	public double DeactivatedScaleY { get { return 0.6; } }
	public double DeactivatedScaleX { get { return 0.6; } }
	public double DeactivatedOpacity { get { return 0.3; } }
	public double ActivatedScaleX { get { return 2.5; } }
	public double ActivatedScaleY { get { return 2.5; } }
	public double ActivatedOpacity { get { return 0.95; } }
	public CommandUserControl(string CommandTitle)
	{
		InitializeComponent();

		labelTitle.Content = CommandTitle;
		ResetToDeactivatedLayout();
		this.Opacity = DeactivatedOpacity;

		//ItemContainerStyle = new Style();
		//ItemContainerStyle.Resources.Add(SystemColors.HighlightBrushKey,
		//        Brushes.Transparent);
		//ItemContainerStyle.Resources.Add(SystemColors.ControlBrushKey,
		//        Brushes.Transparent);
	}

	public void ResetToDeactivatedLayout()
	{
		//if (!(this.LayoutTransform is MatrixTransform)) this.LayoutTransform = new MatrixTransform();
		if (!(this.RenderTransform is MatrixTransform)) this.RenderTransform = new MatrixTransform();

		if (!(this.LayoutTransform is ScaleTransform)) this.LayoutTransform = new ScaleTransform(DeactivatedScaleX, DeactivatedScaleY);
		if ((this.LayoutTransform as ScaleTransform).ScaleY != DeactivatedScaleY) (this.LayoutTransform as ScaleTransform).ScaleY = DeactivatedScaleY;
		if ((this.LayoutTransform as ScaleTransform).ScaleX != DeactivatedScaleX) (this.LayoutTransform as ScaleTransform).ScaleX = DeactivatedScaleX;
		if (this.Opacity != DeactivatedOpacity) this.Opacity = DeactivatedOpacity;
		this.Margin = autoLayoutMaginBeforeAnimation;
		Canvas.SetZIndex(this, 0);
		//this.Effect = new BlurEffect();
	}

	public void ResetToActivatedLayout()
	{
		//if (!(this.LayoutTransform is MatrixTransform)) this.LayoutTransform = new MatrixTransform();
		//if (!(this.RenderTransform is MatrixTransform)) this.RenderTransform = new MatrixTransform();
		if (!(this.LayoutTransform is ScaleTransform)) this.LayoutTransform = new ScaleTransform(ActivatedScaleX, ActivatedScaleY);
		if ((this.LayoutTransform as ScaleTransform).ScaleY != ActivatedScaleY) (this.LayoutTransform as ScaleTransform).ScaleY = ActivatedScaleY;
		if ((this.LayoutTransform as ScaleTransform).ScaleX != ActivatedScaleX) (this.LayoutTransform as ScaleTransform).ScaleX = ActivatedScaleX;
		this.UpdateLayout();
		if (this.Opacity != ActivatedOpacity) this.Opacity = ActivatedOpacity;
		if (this.Margin.Left != newLeftPosition && this.Margin.Top != newTopPosition) this.Margin = new Thickness(this.newLeftPosition, this.newTopPosition, 0, 0);
		Canvas.SetZIndex(this, 99);
		//this.Effect = null;
	}

	public void RemoveAndHideControls()
	{
		//expanderContainingTreeview.IsExpanded = false;
		expanderContainingTreeview.Visibility = System.Windows.Visibility.Collapsed;
		expanderContainingTextboxes.Visibility = System.Windows.Visibility.Collapsed;
		gridCustomArguments.Children.Clear();
		gridCustomArguments.RowDefinitions.Clear();
		treeViewPredefinedArguments.Items.Clear();
	}

	public void AddControl(string label, System.Windows.Controls.Control control, Color labelColor)
	{
		Label labelControl = new Label() { Content = label, Margin = new Thickness(3, 5, 3, 0), Foreground = new SolidColorBrush(labelColor), MinWidth = 50 };

		AddRowToGrid();

		gridCustomArguments.Children.Add(labelControl);
		Grid.SetColumn(labelControl, 0);
		Grid.SetRow(labelControl, gridCustomArguments.RowDefinitions.Count - 1);

		gridCustomArguments.Children.Add(control);
		Grid.SetColumn(control, 1);
		Grid.SetRow(control, gridCustomArguments.RowDefinitions.Count - 1);
		//expanderContainingTextboxes.IsExpanded = true;
		expanderContainingTextboxes.Visibility = System.Windows.Visibility.Visible;
	}

	private void AddRowToGrid()
	{
		gridCustomArguments.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
	}

	public void CollapseCustomInput()
	{
		expanderContainingTextboxes.IsExpanded = false;
	}

	public void CollapseTreeview()
	{
		expanderContainingTreeview.IsExpanded = false;
	}

	public void ExpandCustomInputs()
	{
		expanderContainingTextboxes.IsExpanded = true;
	}

	public void ExpandTreeviewInput()
	{
		expanderContainingTreeview.IsExpanded = true;
	}

	public void AddTreeviewItem(object itemToAdd)
	{
		//if (tmpTreeview == null)
		//{
		//  tmpTreeview = new TreeView()
		//  {
		//    MinWidth = 150,
		//    MinHeight = 20,
		//    //MaxHeight = 80,
		//    VerticalAlignment = System.Windows.VerticalAlignment.Top,
		//    Focusable = true,
		//    UseLayoutRounding = true
		//  };

		//  tmpTreeview.ItemContainerStyle = new Style(typeof(TreeViewItem));
		//  //tmpTreeview.ItemContainerStyle.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty, new Binding() { Path = new PropertyPath(TreeViewItem.IsExpandedProperty), Mode = BindingMode.TwoWay }));
		//  //tmpTreeview.ItemContainerStyle.Setters.Add(new Setter(TreeViewItem.IsSelectedProperty, new Binding() { Path = new PropertyPath(TreeViewItem.IsSelectedProperty), Mode = BindingMode.TwoWay }));
		//  //tmpTreeview.ItemContainerStyle.Setters.Add(new Setter(TreeViewItem.FontWeightProperty, FontWeights.Normal));
		//  Trigger tmpTrigger = new Trigger() { Property = TreeViewItem.IsSelectedProperty, Value = true };
		//  tmpTrigger.Setters.Add(new Setter(TreeViewItem.ForegroundProperty, Brushes.DarkBlue));
		//  tmpTreeview.ItemContainerStyle.Triggers.Add(tmpTrigger);

		//  gridTable.Children.Add(tmpTreeview);
		//  Grid.SetColumn(tmpTreeview, 2);
		//  if (gridTable.RowDefinitions.Count == 0) AddRowToGrid();
		//  Grid.SetRow(tmpTreeview, 0);
		//  Grid.SetRowSpan(tmpTreeview, 1000);
		//}
		//tmpTreeview.Items.Add(itemToAdd);
		treeViewPredefinedArguments.Items.Add(itemToAdd);
		expanderContainingTreeview.Visibility = System.Windows.Visibility.Visible;
		//expanderContainingTreeview.IsExpanded = true;
	}
	//TreeView tmpTreeview = null;

	private void border_Closebutton_MouseUp(object sender, MouseButtonEventArgs e)
	{
		//TODO: This does not work exactly right yet as it jumps back on the call of ResetToDeactivatedLayout()
		DeactivateControl(0.1);
	}

	public void DeactivateControl(double overWriteScale = -1)
	{
		double newScaleX = overWriteScale == -1 ? DeactivatedScaleX : overWriteScale;
		double newScaleY = overWriteScale == -1 ? DeactivatedScaleY : overWriteScale;

		Duration animationDuration = new Duration(TimeSpan.FromSeconds(0.5));

		double currentYscale = 1;
		double currentXscale = 1;
		if (!(parentUsercontrol.LayoutTransform is ScaleTransform)) parentUsercontrol.LayoutTransform = new ScaleTransform();
		else
		{
			currentYscale = (parentUsercontrol.LayoutTransform as ScaleTransform).ScaleY;
			currentXscale = (parentUsercontrol.LayoutTransform as ScaleTransform).ScaleX;
		}

		this.LayoutTransform = new ScaleTransform();
		DoubleAnimation opacityAnimation = new DoubleAnimation() { To = DeactivatedOpacity, Duration = animationDuration };
		DoubleAnimation scaleyAnimation = new DoubleAnimation() { From = currentYscale, To = newScaleY, Duration = animationDuration };
		DoubleAnimation scalexAnimation = new DoubleAnimation() { From = currentXscale, To = newScaleX, Duration = animationDuration };
		ThicknessAnimation marginAnimation = new ThicknessAnimation() { From = this.Margin, To = autoLayoutMaginBeforeAnimation, Duration = animationDuration };

		Storyboard storyboard = new Storyboard()
		{
			Name = "storyboardFadeout",
			AutoReverse = false,
			RepeatBehavior = new RepeatBehavior(1),
			FillBehavior = FillBehavior.Stop
		};
		Storyboard.SetTargetName(storyboard, parentUsercontrol.Name);
		Storyboard.SetTargetProperty(opacityAnimation, (PropertyPath)new PropertyPathConverter().ConvertFromString("Opacity"));
		Storyboard.SetTargetProperty(scaleyAnimation, (PropertyPath)new PropertyPathConverter().ConvertFromString("(FrameworkElement.LayoutTransform).(ScaleTransform.ScaleY)"));
		Storyboard.SetTargetProperty(scalexAnimation, (PropertyPath)new PropertyPathConverter().ConvertFromString("(FrameworkElement.LayoutTransform).(ScaleTransform.ScaleX)"));
		Storyboard.SetTargetProperty(marginAnimation, new PropertyPath(UserControl.MarginProperty));
		storyboard.Children.Add(opacityAnimation);
		storyboard.Children.Add(scaleyAnimation);
		storyboard.Children.Add(scalexAnimation);
		storyboard.Children.Add(marginAnimation);

		storyboard.Completed += delegate
		{
			if (AnimationComplete_Closing != null)
				AnimationComplete_Closing(this, new EventArgs());
			ResetToDeactivatedLayout();
		};
		storyboard.Begin(this);
	}

	public Thickness autoLayoutMaginBeforeAnimation = new Thickness();
	double newLeftPosition = 0;
	double newTopPosition = 0;
	public void ActivateControl()
	{
		Duration animationDuration = new Duration(TimeSpan.FromSeconds(0.3));

		double currentYscale = 1;
		double currentXscale = 1;
		if (!(parentUsercontrol.LayoutTransform is ScaleTransform)) parentUsercontrol.LayoutTransform = new ScaleTransform();
		else
		{
			currentYscale = (parentUsercontrol.LayoutTransform as ScaleTransform).ScaleY;
			currentXscale = (parentUsercontrol.LayoutTransform as ScaleTransform).ScaleX;
		}

		Rect workingArea = SystemParameters.WorkArea;
		newLeftPosition = workingArea.Left + (workingArea.Width - this.ActualWidth * ActivatedScaleX) / 2;
		newTopPosition = workingArea.Top + (workingArea.Height - this.ActualHeight * ActivatedScaleY) / 2;

		this.RenderTransform = new MatrixTransform();
		this.LayoutTransform = new ScaleTransform();
		DoubleAnimation opacityAnimation = new DoubleAnimation() { To = ActivatedOpacity, Duration = animationDuration };
		DoubleAnimation scaleyAnimation = new DoubleAnimation() { From = currentYscale, To = ActivatedScaleY, Duration = animationDuration };
		DoubleAnimation scalexAnimation = new DoubleAnimation() { From = currentXscale, To = ActivatedScaleX, Duration = animationDuration };
		ThicknessAnimation marginAnimation = new ThicknessAnimation() { From = this.Margin, To = new Thickness(newLeftPosition, newTopPosition, 0, 0), Duration = animationDuration };

		Storyboard storyboard = new Storyboard()
		{
			Name = "storyboardFadeout",
			AutoReverse = false,
			RepeatBehavior = new RepeatBehavior(1),
			FillBehavior = FillBehavior.Stop
		};
		Storyboard.SetTargetName(storyboard, parentUsercontrol.Name);
		Storyboard.SetTargetProperty(opacityAnimation, (PropertyPath)new PropertyPathConverter().ConvertFromString("Opacity"));
		Storyboard.SetTargetProperty(scaleyAnimation, (PropertyPath)new PropertyPathConverter().ConvertFromString("(FrameworkElement.LayoutTransform).(ScaleTransform.ScaleY)"));
		Storyboard.SetTargetProperty(scalexAnimation, (PropertyPath)new PropertyPathConverter().ConvertFromString("(FrameworkElement.LayoutTransform).(ScaleTransform.ScaleX)"));
		Storyboard.SetTargetProperty(marginAnimation, new PropertyPath(UserControl.MarginProperty));
		storyboard.Children.Add(opacityAnimation);
		storyboard.Children.Add(scaleyAnimation);
		storyboard.Children.Add(scalexAnimation);
		storyboard.Children.Add(marginAnimation);

		storyboard.Completed += delegate
		{
			if (AnimationComplete_Activating != null)
				AnimationComplete_Activating(this, new EventArgs());
			ResetToActivatedLayout();
		};
		Canvas.SetZIndex(this, 99);
		storyboard.Begin(this);
	}

	//private object findResource(string name)
	//{
	//  return this.FindResource(name);
	//}

	public void FocusRelevantInputElement()
	{
		if (this.Tag != null && this.Tag is OverlayWindow.OverlayChildManager && (this.Tag as OverlayWindow.OverlayChildManager).FirstUIelementToFocus != null)
		{
			if (currentFocusedElement == null)
			{
				(this.Tag as OverlayWindow.OverlayChildManager).FirstUIelementToFocus.Focus();
				currentFocusedElement = (this.Tag as OverlayWindow.OverlayChildManager).FirstUIelementToFocus;
				currentFocusedElement.Focus();
			}
			else
			{
				currentFocusedElement.Focus();
			}
		}
	}
}