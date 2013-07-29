using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for DebugTracerWindow.xaml
	/// </summary>
	public partial class DebugTracerWindow : Window
	{
		private DebugTracerWindow()
		{
			InitializeComponent();
		}

		private static DebugTracerWindow globalDebugWin = null;
		public static void DebugOut(string message)
		{
			if (globalDebugWin == null)
			{
				globalDebugWin = new DebugTracerWindow();
				globalDebugWin.Show();
			}

			globalDebugWin.textbox1.Text += string.Format("[{0}] {1}.", DateTime.Now.ToString("HH:mm:ss"), message + Environment.NewLine);
			globalDebugWin.textbox1.ScrollToEnd();
		}
	}
}