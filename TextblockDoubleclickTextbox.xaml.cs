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
using System.Windows.Threading;
using System.Globalization;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for TextblockDoubleclickTextbox.xaml
	/// </summary>
	public partial class TextblockDoubleclickTextbox : UserControl
	{
		public TextblockDoubleclickTextbox()
		{
			InitializeComponent();
		}

		private string previousValue = null;
		public event DependencyPropertyChangedEventHandler TextChanged_EachChange = delegate { };
		public event DependencyPropertyChangedEventHandler TextChanged_LostFocus = delegate { };

		public bool OnlyfocusOnDoubleClick
		{
			get { return (bool)GetValue(OnlyfocusOnDoubleClickProperty); }
			set { SetValue(OnlyfocusOnDoubleClickProperty, value); }
		}
		public static readonly DependencyProperty OnlyfocusOnDoubleClickProperty =
        DependencyProperty.Register("OnlyfocusOnDoubleClick", typeof(bool), typeof(TextblockDoubleclickTextbox), new UIPropertyMetadata(true));

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}
		// Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(TextblockDoubleclickTextbox), new UIPropertyMetadata(OnTextPropertyChanged));
		private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var textblockDoubleclickTextbox = (TextblockDoubleclickTextbox)d;
			textblockDoubleclickTextbox.TextChanged_EachChange(d, e);
		}

		public Brush InnerTextboxForeground
		{
			get { return (Brush)GetValue(InnerTextboxForegroundProperty); }
			set { SetValue(InnerTextboxForegroundProperty, value); }
		}
		public static readonly DependencyProperty InnerTextboxForegroundProperty =
        DependencyProperty.Register("InnerTextboxForeground", typeof(Brush), typeof(TextblockDoubleclickTextbox), new UIPropertyMetadata(Brushes.Black));

		public void OnGotFocus()
		{
			previousValue = textBlockName.Text;

			textBoxName.Visibility = Visibility.Visible;
			textBlockName.Visibility = Visibility.Collapsed;
			this.Dispatcher.BeginInvoke((Action)delegate
			{
				Keyboard.Focus(textBoxName);
			}, DispatcherPriority.Render);

			if (textBlockName.Foreground == Brushes.White)
				textBoxName.Foreground = Brushes.Black;
			textBoxName.UpdateLayout();

			//txtBox.Focus();
			//Keyboard.Focus(txtBox);
		}

		private void OnLostFocus()
		{
			textBlockName.Visibility = Visibility.Visible;
			textBoxName.Visibility = Visibility.Collapsed;
			if (!this.Text.Equals(previousValue))
			{
				string prevval = previousValue;
				previousValue = this.Text;
				TextChanged_LostFocus(this, new DependencyPropertyChangedEventArgs(TextProperty, prevval, this.Text));
			}
		}

		private void textBoxName_LostFocus(object sender, RoutedEventArgs e)
		{
			OnLostFocus();
		}

		private void textBlockName_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				if (!OnlyfocusOnDoubleClick || e.ClickCount == 2)
				{
					e.Handled = true;
					OnGotFocus();
					TextBlock textBlock = (TextBlock)sender;
					TextBox txtBox = (TextBox)((Grid)(textBlock).Parent).Children[1];
					txtBox.CaretIndex = FindClosestCharIndex(txtBox, e.GetPosition(txtBox));
				}
			}
		}

		private int FindClosestCharIndex(TextBox txtBox, Point mousePos)
		{
			if (txtBox.Text.Length == 0)
				return 0;

			double currentCharClosestXToMouse = 9000000;
			int currentCharIndex = 0;

			for (int i = 0; i < txtBox.Text.Length; i++)
			{
				Rect r = txtBox.GetRectFromCharacterIndex(i);
				r.Width = new FormattedText(
					txtBox.Text[i].ToString(),
					CultureInfo.GetCultureInfo("en-us"),
					FlowDirection.LeftToRight,
					new Typeface(txtBox.FontFamily.ToString()),
					txtBox.FontSize,
					Brushes.Black
					).WidthIncludingTrailingWhitespace;

				//bool match = false;
				double centerLeftHalveX = r.Left + (r.Width / 4);
				if (Math.Abs(mousePos.X - centerLeftHalveX) < currentCharClosestXToMouse)
				{
					currentCharClosestXToMouse = Math.Abs(mousePos.X - centerLeftHalveX);
					currentCharIndex = i;
					//match = true;
				}
				double centerRightHalveX = r.Right - (r.Width / 4);
				if (Math.Abs(mousePos.X - centerRightHalveX) < currentCharClosestXToMouse)
				{
					currentCharClosestXToMouse = Math.Abs(mousePos.X - centerRightHalveX);
					currentCharIndex = i + 1;
					//match = true;
				}
				//if (match && i == txtBox.Text.Length - 1 && mousePos.X > r.Right)
				//    currentCharIndex++;
			}
			return currentCharIndex;
		}

		private void textBoxName_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				this.Text = previousValue;
				OnLostFocus();
			}
			else if (e.Key == Key.Enter)
			{
				OnLostFocus();
			}
		}
	}
}
