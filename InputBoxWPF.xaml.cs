using System;
using System.Windows;
using System.Windows.Input;

/// <summary>
/// Interaction logic for tmpUserControl.xaml
/// </summary>
public partial class InputBoxWPF : Window
{
	public static string Prompt(string PromptMessage, string Title = "Prompt", bool IsPassword = false)
	{
		InputBoxWPF inputBoxWPF = new InputBoxWPF(PromptMessage, Title, IsPassword);
		if (inputBoxWPF.ShowDialog() == true)
			return inputBoxWPF.ResponseText;
		else return null;
	}

	private bool IsPassword = false;

	public InputBoxWPF(string PromptMessage, string Title, bool IsPassword)
	{
		InitializeComponent();
		TextblockTitle.Text = Title;
		TextblockPromptMessage.Text = PromptMessage;
		this.IsPassword = IsPassword;
		ResponseTextBox.Visibility = IsPassword ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
		ResponsePasswordBox.Visibility = IsPassword ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
	}

	public string ResponseText
	{
		get
		{
			if (IsPassword)
				return ResponsePasswordBox.Password == null || ResponsePasswordBox.Password.Trim().Length == 0 ? null : ResponsePasswordBox.Password;
			else
				return ResponseTextBox.Text == null || ResponseTextBox.Text.Trim().Length == 0 ? null : ResponseTextBox.Text;
		}
	}

	private void Cancel()
	{
		this.DialogResult = false;
	}

	private void Accept()
	{
		this.DialogResult = true;
	}

	private void InputBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == System.Windows.Input.Key.Escape)
		{
			e.Handled = true;
			Cancel();
		}
		else if (e.Key == System.Windows.Input.Key.Enter)//&& Keyboard.Modifiers == ModifierKeys.Control)
		{
			e.Handled = true;
			Accept();
		}
	}

	private void InputBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
	{
		if (ResponseTextBox.Visibility == System.Windows.Visibility.Visible)
			ResponseTextBox.Focus();
		else if (ResponsePasswordBox.Visibility == System.Windows.Visibility.Visible)
			ResponsePasswordBox.Focus();
	}

	private void InputBox_Loaded(object sender, RoutedEventArgs e)
	{
		DragCanvas.DragCanvas.SetCanBeDragged(ButtonAccept, false);
		DragCanvas.DragCanvas.SetCanBeDragged(ButtonClose, false);

		double centreX = (dragCanvas1.ActualWidth - MainBorder.ActualWidth) / 2;
		double centreY = (dragCanvas1.ActualHeight - MainBorder.ActualHeight) / 2;
		DragCanvas.DragCanvas.SetLeft(MainBorder, centreX);
		DragCanvas.DragCanvas.SetTop(MainBorder, centreY);
	}

	private void ButtonClose_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		Cancel();
	}

	private void ButtonAccept_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		Accept();
	}
}