using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedClasses
{
	public class ImagesInterop
	{
		/// <summary>
		/// Converts an image into an icon (either using objects or physical file paths).
		/// </summary>
		/// <param name="img">The image that shall become an icon</param>
		/// <param name="size">The width and height of the icon. Standard
		/// sizes are 16x16, 32x32, 48x48, 64x64.</param>
		/// <param name="keepAspectRatio">Whether the image should be squashed into a
		/// square or whether whitespace should be put around it.</param>
		/// <returns>An icon!!</returns>
		public static Icon ImageToIcon(Image img, int size, bool keepAspectRatio)
		{
			Bitmap square = new Bitmap(size, size); // create new bitmap
			Graphics g = Graphics.FromImage(square); // allow drawing to it

			int x, y, w, h; // dimensions for new image

			if (!keepAspectRatio || img.Height == img.Width)
			{
				// just fill the square
				x = y = 0; // set x and y to 0
				w = h = size; // set width and height to size
			}
			else
			{
				// work out the aspect ratio
				float r = (float)img.Width / (float)img.Height;

				// set dimensions accordingly to fit inside size^2 square
				if (r > 1)
				{ // w is bigger, so divide h by r
					w = size;
					h = (int)((float)size / r);
					x = 0; y = (size - h) / 2; // center the image
				}
				else
				{ // h is bigger, so multiply w by r
					w = (int)((float)size * r);
					h = size;
					y = 0; x = (size - w) / 2; // center the image
				}
			}

			// make the image shrink nicely by using HighQualityBicubic mode
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			g.DrawImage(img, x, y, w, h); // draw image with specified dimensions
			g.Flush(); // make sure all drawing operations complete before we get the icon

			// following line would work directly on any image, but then
			// it wouldn't look as nice.
			return Icon.FromHandle(square.GetHicon());
		}

		/// <summary>
		/// Converts an image into an icon (either using objects or physical file paths).
		/// </summary>
		/// <param name="ImagePath">Physical path of image file</param>
		/// <param name="IconSize">The width and height of the icon. Standard
		/// sizes are 16x16, 32x32, 48x48, 64x64 </param>
		/// <param name="KeepAspectRatio">Whether the image should be squashed into a
		/// square or whether whitespace should be put around it.</param>
		/// <param name="IconSameDirAndName">Whether the new icon should be created inside the same directory as
		/// the image, having the same name but .ico as extension</param>
		/// <param name="IconPathIfNotSameAsImage">If "IconSameDirAndName" is false, what is the path of the icon</param>
		public static void ImageToIcon(String ImagePath, int IconSize, Boolean KeepAspectRatio, Boolean IconSameDirAndName, String IconPathIfNotSameAsImage)
		{
			//String FileName = @"C:\DelphiProjects\SwiftMeters\Images\Copy of Splash.jpg";
			Icon icon = ImageToIcon(new Bitmap(ImagePath), IconSize, KeepAspectRatio);
			System.IO.FileStream fileStream = new System.IO.FileStream(
					IconSameDirAndName ?
					System.IO.Path.GetDirectoryName(ImagePath) + @"\" + System.IO.Path.GetFileNameWithoutExtension(ImagePath) + ".ico"
					: IconPathIfNotSameAsImage,
					System.IO.FileMode.Create);
			icon.Save(fileStream);
			fileStream.Close();
		}
	}
}
