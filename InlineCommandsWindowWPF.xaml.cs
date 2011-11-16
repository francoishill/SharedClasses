using System;
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
}