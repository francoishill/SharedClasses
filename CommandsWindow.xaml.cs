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
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using SharedClasses;
/// <summary>
/// Interaction logic for CommandsWindow.xaml
/// </summary>
public partial class CommandsWindow : Window
{
	System.Windows.Forms.Form MainFormUsedForShuttingDownServers;
	public CommandsUsercontrol GetCommandsUsercontrol() { return commandsUsercontrol1; }

	public CommandsWindow(System.Windows.Forms.Form mainFormUsedForShuttingDownServers)
	{
		InitializeComponent();
		MainFormUsedForShuttingDownServers = mainFormUsedForShuttingDownServers;
	}

	private void CommandsWindow1_Loaded(object sender, RoutedEventArgs e)
	{
		System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
		timer.Interval = 1;
		timer.Tick += delegate
		{
			timer.Stop();
			timer.Dispose(); timer = null;
			
			commandsUsercontrol1.InitializeTreeViewNodes(
				MainFormUsedForShuttingDownServers,
				true,
				delegate { this.Hide(); },
				delegate { this.Close(); },
				"hide/close",
				"Left-click hides. Right-click closes");
		};
		timer.Start();
	}

	private void CommandsWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
	{
		//e.Cancel = true;
		//this.Hide();
		//Application.Current.MainWindow.Close();
	}

	private void CommandsWindow1_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
	{
		if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
			DragMove();
	}

	private void CommandsWindow1_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
	{
		//this.Hide();
	}

	private void ThumbButtonInfo_Click(object sender, EventArgs e)
	{
		this.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
		this.TaskbarItemInfo.ProgressValue = 0.8;		
	}
}