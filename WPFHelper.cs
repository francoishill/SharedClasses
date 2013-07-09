﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.IO;

namespace SharedClasses
{
    public static class WPFHelper
    {
        #region GetVisualChild

		public static IntPtr GetHandle(this Window window)
		{
			return new WindowInteropHelper(window).Handle;
		}

        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);

            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }

            return child;
        }

        #endregion GetVisualChild

        #region FindVisualParent

        public static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element;
            while (parent != null)
            {
                T correctlyTyped = parent as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }

            return null;
        }

        #endregion FindVisualParent

        #region FindPartByName

        public static DependencyObject FindPartByName(DependencyObject ele, string name)
        {
            DependencyObject result;
            if (ele == null)
            {
                return null;
            }
            if (name.Equals(ele.GetValue(FrameworkElement.NameProperty)))
            {
                return ele;
            }

            int numVisuals = VisualTreeHelper.GetChildrenCount(ele);
            for (int i = 0; i < numVisuals; i++)
            {
                DependencyObject vis = VisualTreeHelper.GetChild(ele, i);
                if ((result = FindPartByName(vis, name)) != null)
                {
                    return result;
                }
            }
            return null;
        }

        #endregion FindPartByName

		public static bool DoesFrameworkElementContainMouse(FrameworkElement frameworkElement, int ignoreBorderWidth = 0)
		{
			frameworkElement.UpdateLayout();
			var elementRect = new Rect(frameworkElement.PointToScreen(
				new Point(0, 0)),
				new Size(frameworkElement.ActualWidth, frameworkElement.ActualHeight));
			elementRect = Rect.Inflate(elementRect, -ignoreBorderWidth, -ignoreBorderWidth);
			var mousePos = MouseLocation.GetMousePosition();

			Win32Api.Win32Point p;
			if (Win32Api.GetCursorPos(out p))
			{
				IntPtr handleOfWindowBelowMouse = Win32Api.WindowFromPoint(p);
				if (handleOfWindowBelowMouse != IntPtr.Zero)
				{
					var window = WPFHelper.FindVisualParent<Window>(frameworkElement);
					if (window != null)
					{
						IntPtr windowHandle = window.GetHandle();
						if (elementRect.Contains(mousePos))
							Console.WriteLine("TRUE (type=" + frameworkElement.GetType().ToString() + "): Mousepos: " + mousePos.ToString() + ", elementRect: " + elementRect.ToString());
						else
							Console.WriteLine("FALSE (type=" + frameworkElement.GetType().ToString() + "): Mousepos: " + mousePos.ToString() + ", elementRect: " + elementRect.ToString());
						return windowHandle == handleOfWindowBelowMouse || elementRect.Contains(mousePos);
					}
				}
			}

			return elementRect.Contains(mousePos);
		}

		public static class MouseLocation
		{
			public static Point GetMousePosition()
			{
				Win32Api.Win32Point w32Mouse = new Win32Api.Win32Point();
				Win32Api.GetCursorPos(out w32Mouse);
				return new Point(w32Mouse.X, w32Mouse.Y);
			}
		}

		/// <summary>
		/// Copies a UI element to the clipboard as an image.
		/// </summary>
		/// <param name="element">The element to copy.</param>
		public static BitmapSource GetImageFromUIElement(FrameworkElement element, Size? fitToSize = null)
		{
			//http://elegantcode.com/2010/12/09/wpf-copy-uielement-as-image-to-clipboard/

			double width = element.ActualWidth;
			double height = element.ActualHeight;
			if (fitToSize.HasValue)
			{
				width = fitToSize.Value.Width;
				height = fitToSize.Value.Height;
			}

			if (width == 0 || height == 0)
				return null;

			if (width < 30)
			{
				double factor = 30D / width;
				width *= factor;
				height *= factor;
			}

			//double minratio = 0.1D;
			//if (width/height < minratio)
			//    width = height * minratio;
			//if (width < 30)
			//    width = 30;

			RenderTargetBitmap bmpCopied = new RenderTargetBitmap(
				(int)Math.Round(width),
				(int)Math.Round(height),
				96, 96, PixelFormats.Default);
			DrawingVisual dv = new DrawingVisual();
			using (DrawingContext dc = dv.RenderOpen())
			{
				VisualBrush vb = new VisualBrush(element);
				vb.Stretch = Stretch.UniformToFill;
				dc.DrawRectangle(vb, null, new Rect(new Point(), new Size(width, height)));
			}
			bmpCopied.Render(dv);
			//Clipboard.SetImage(bmpCopied);
			return bmpCopied;
		}

		public static BitmapImage BitmapSourceToBitmapImage(BitmapSource bitmapSource)
		{
			if (bitmapSource == null)
				return null;
			JpegBitmapEncoder encoder = new JpegBitmapEncoder();
			MemoryStream memoryStream = new MemoryStream();
			BitmapImage bImg = new BitmapImage();

			encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
			encoder.Save(memoryStream);

			bImg.BeginInit();
			bImg.StreamSource = new MemoryStream(memoryStream.ToArray());
			bImg.EndInit();

			memoryStream.Close();

			return bImg;
		}
    }
}
