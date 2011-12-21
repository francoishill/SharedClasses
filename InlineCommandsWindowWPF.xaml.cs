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
using System.Reflection;
using System.IO;
using System.Diagnostics;
using InlineCommands;
/// <summary>
/// Interaction logic for InlineCommandsWindowWPF.xaml
/// </summary>
public partial class InlineCommandsWindowWPF : Window
{
	public InlineCommandsWindowWPF()
	{
		InitializeComponent();
	}

	private void InlineCommandsWindowWPF1_Loaded(object sender, RoutedEventArgs e)
	{
		inlineCommandsUserControlWPF1.InitializeTreeViewNodes(
			true,
			delegate { this.Hide(); },
			delegate { this.Close(); },
			"hide/close",
			"Left-click hides. Right-click closes");
	}

	private void InlineCommandsWindowWPF1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
	{
		//e.Cancel = true;
		//this.Hide();
		//Application.Current.MainWindow.Close();
	}

	private void InlineCommandsWindowWPF1_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
	{
		if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
			DragMove();
	}

	private void InlineCommandsWindowWPF1_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
	{
		this.Hide();
	}

	private void ThumbButtonInfo_Click(object sender, EventArgs e)
	{
		this.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
		this.TaskbarItemInfo.ProgressValue = 0.8;		
	}
}