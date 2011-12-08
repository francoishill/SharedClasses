using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharedClasses
{
	public class IconsInterop
	{
		/// <summary>
		/// Create an icon with text on it, choosing color, font color and font.
		/// </summary>
		/// <param name="TextOnIcon">Text displayed on icon.</param>
		/// <param name="backColor">Background color of icon.</param>
		/// <param name="fontColorBrush">Font color of text on icon.</param>
		/// <param name="font">Font of text on icon (example new Font("Arial", 17, FontStyle.Regular)).</param>
		/// <returns>The icon.</returns>
		public static System.Drawing.Icon GetIcon(String TextOnIcon, System.Drawing.Color backColor, System.Drawing.Brush fontColorBrush, System.Drawing.Font font)
		{
			//Create a bitmap, the size of an icon
			System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(32, 32);
			//Create Graphics object for the bitmap (all drawing to the graphics object will be drawn on the bitmap)
			System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);
			//Create a smokewhite background,draw the circle and the number
			g.Clear(backColor);
			//g.DrawEllipse(Pens.Black, 0, 0, 32, 32);
			System.Drawing.SizeF s = g.MeasureString(TextOnIcon, font);//to center the string
			g.DrawString(TextOnIcon, font, fontColorBrush, (32 - s.Width) / 2, (32 - s.Height) / 2);
			//And finally, get the icon out the icon handle of the bitmap
			return System.Drawing.Icon.FromHandle(bmp.GetHicon());
		}

		/// <summary>
		/// Shows a notification icon in the system tray, for the given duration then removes it.
		/// </summary>
		/// <param name="icon">The icon of the notify icon.</param>
		/// <param name="Duration">The duration to show the icon, in milliseconds.</param>
		/// <param name="Title">Title of the message.</param>
		/// <param name="Message">The message to show.</param>
		/// <param name="tooltipIcon">The tooltip icon to use.</param>
		public static void ShowNotifyIcon(System.Drawing.Icon icon, int Duration, String Title, String Message, System.Windows.Forms.ToolTipIcon tooltipIcon)
		{
			System.Windows.Forms.NotifyIcon tmpIcon = new System.Windows.Forms.NotifyIcon();
			tmpIcon.Icon = icon;
			tmpIcon.Visible = true;
			tmpIcon.ShowBalloonTip(Duration, Title, Message, tooltipIcon);
			System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
			timer.Interval = Duration;
			timer.Tick += delegate
			{
				try
				{
					tmpIcon.Dispose();
					tmpIcon = null;
				}
				catch { }
				timer.Stop();
				timer.Dispose();
			};
			timer.Start();
		}

		/// <summary>
		/// 
		/// Extracts the icon associated with any file on your system.
		/// Author: WidgetMan http://softwidgets.com
		/// 
		/// </summary>
		/// <remarks>
		/// 
		/// Class requires the IconSize enumeration that is implemented in this
		/// same file. For best results, draw an icon from within a control's Paint
		/// event via the e.Graphics.DrawIcon method.
		/// 
		/// </remarks>  
		public class IconExtractor
		{
			public enum IconSize
			{
				Small,
				Large
			}
			private const uint SHGFI_ICON = 0x100;
			private const uint SHGFI_LARGEICON = 0x0;
			private const uint SHGFI_SMALLICON = 0x1;

			[StructLayout(LayoutKind.Sequential)]
			private struct SHFILEINFO
			{
				public IntPtr hIcon;
				public IntPtr iIcon;
				public uint dwAttributes;
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
				public string szDisplayName;
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
				public string szTypeName;
			};

			[DllImport("shell32.dll")]
			private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

			public IconExtractor()
			{
			}

			public System.Drawing.Icon Extract(string File, IconSize Size)
			{
				IntPtr hIcon;
				SHFILEINFO shinfo = new SHFILEINFO();

				if (Size == IconSize.Large)
				{
					hIcon = SHGetFileInfo(File, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);
				}
				else
				{
					hIcon = SHGetFileInfo(File, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);
				}

				return System.Drawing.Icon.FromHandle(shinfo.hIcon);
			}

			public System.Drawing.Icon Extract(string File)
			{
				return this.Extract(File, IconSize.Small);
			}
		}
	}
}
