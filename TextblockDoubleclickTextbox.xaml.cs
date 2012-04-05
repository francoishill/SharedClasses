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

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(TextblockDoubleclickTextbox), new UIPropertyMetadata());

		private void textBoxName_LostFocus(object sender, RoutedEventArgs e)
		{
			var txtBlock = (TextBlock)((Grid)((TextBox)sender).Parent).Children[0];

			txtBlock.Visibility = Visibility.Visible;
			((TextBox)sender).Visibility = Visibility.Collapsed;
		}

		private void textBlockName_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
			{
				var txtBox = (TextBox)((Grid)((TextBlock)sender).Parent).Children[1];
				txtBox.Visibility = Visibility.Visible;
				((TextBlock)sender).Visibility = Visibility.Collapsed;
				this.Dispatcher.BeginInvoke((Action)delegate
				{
					Keyboard.Focus(txtBox);
				}, DispatcherPriority.Render);

				txtBox.UpdateLayout();
				txtBox.CaretIndex = FindClosestCharIndex(txtBox, e.GetPosition(txtBox));

				//txtBox.Focus();
				//Keyboard.Focus(txtBox);
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

				bool match = false;
				double centerLeftHalveX = r.Left + (r.Width / 4);
				if (Math.Abs(mousePos.X - centerLeftHalveX) < currentCharClosestXToMouse)
				{
					currentCharClosestXToMouse = Math.Abs(mousePos.X - centerLeftHalveX);
					currentCharIndex = i;
					match = true;
				}
				double centerRightHalveX = r.Right - (r.Width / 4);
				if (Math.Abs(mousePos.X - centerRightHalveX) < currentCharClosestXToMouse)
				{
					currentCharClosestXToMouse = Math.Abs(mousePos.X - centerRightHalveX);
					currentCharIndex = i + 1;
					match = true;
				}
				//if (match && i == txtBox.Text.Length - 1 && mousePos.X > r.Right)
				//    currentCharIndex++;
			}
			return currentCharIndex;
		}
	}
}
