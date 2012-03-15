using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SharedClasses;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for TempFormAppManager.xaml
	/// </summary>
	public partial class TempFormAppManager : Window
	{
		public TempFormAppManager()
		{
			InitializeComponent();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
			source.AddHook(WndProc);
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			string errMsg;
			if (!WindowMessagesInterop.ApplicationManagerHandleMessage(msg, wParam, lParam, out errMsg))
				MessageBox.Show(errMsg);
			return IntPtr.Zero;
		}
	}
}
