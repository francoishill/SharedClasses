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
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.IO;
using DynamicDLLsInterop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class OverlayRibbon : Window
{
	public delegate void RequestToOpenWindowEventHandler(object sender, RequestToOpenWindowEventArgs e);
	public class RequestToOpenWindowEventArgs : EventArgs
	{
		public double ScalingFactor;
		public bool WasRightClick;
		public RequestToOpenWindowEventArgs(bool WasRightClick, double ScalingFactor = 1)
		{
			this.WasRightClick = WasRightClick;
			this.ScalingFactor = ScalingFactor;
		}
	}

	public event RequestToOpenWindowEventHandler MouseClickedRequestToOpenOverlayWindow;

	public OverlayRibbon()
	{
		InitializeComponent();

		//stretchableGrid.RenderTransform = new ScaleTransform(0.1, 1);
		//mainBorder.RenderTransform = new ScaleTransform(0.1, 0.1);
	}

	private void mainWindow_LocationChanged(object sender, EventArgs e)
	{
		System.Drawing.Rectangle workingArea = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(0, 0)).WorkingArea;
		if (this.Left != 0) this.Left = 0;
		if (this.Top + this.ActualHeight > workingArea.Bottom) this.Top = workingArea.Bottom - this.ActualHeight;
	}

	private void mainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if ((System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Control) == System.Windows.Forms.Keys.Control) this.DragMove();
		else CallEvent_MouseClickedRequestToOpenOverlayWindow(false);
	}

	private void mainWindow_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		CallEvent_MouseClickedRequestToOpenOverlayWindow(true);		
	}
	
	private void CallEvent_MouseClickedRequestToOpenOverlayWindow(bool WasRightButton)
	{
		if (MouseClickedRequestToOpenOverlayWindow != null) MouseClickedRequestToOpenOverlayWindow(this, new RequestToOpenWindowEventArgs(WasRightButton, 2.5));
	}

	private void mainWindow_DragEnter(object sender, DragEventArgs e)
	{
		CallEvent_MouseClickedRequestToOpenOverlayWindow(false);
	}

	//TODO: Read up a bit more on MeasureOverride and ArrangeOverride, see following line for website
	//http://www.dotnetfunda.com/articles/article900-wpf-tutorial--layoutpanelscontainers--layout-transformation-2-.aspx
	//protected override Size MeasureOverride(Size availableSize)
	//{
	//  return base.MeasureOverride(availableSize);
	//}
	//protected override Size ArrangeOverride(Size arrangeBounds)
	//{
	//  return base.ArrangeOverride(arrangeBounds);
	//}
}