﻿using System;
using System.Windows;

public partial class InlineCommandsWindowWPF : Window
{
	public InlineCommandsWindowWPF()
	{
		InitializeComponent();
	}

	private void InlineCommandsWindowWPF1_Loaded(object sender, RoutedEventArgs e)
	{
		inlineCommandsUserControlWPF1.InitializeTreeViewNodes();
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
}