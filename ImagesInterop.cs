using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

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

		public static void ResizeImage(string originalPath, string resizedFilepath, int maxsidelength)
		{
			File.WriteAllBytes(
				resizedFilepath,
				ResizeFromByteArray(maxsidelength, File.ReadAllBytes(originalPath), originalPath));
		}

		public static byte[] ResizeFromByteArray(int MaxSideSize, Byte[] byteArrayIn, string fileNameToWatermarkIfFailed)
		{
			byte[] byteArray = null;  // really make this an error gif
			MemoryStream ms = new MemoryStream(byteArrayIn);
			byteArray = ResizeFromStream(MaxSideSize, ms, fileNameToWatermarkIfFailed);

			return byteArray;
		}
		public static byte[] ResizeFromStream(int MaxSideSize, Stream Buffer, string fileNameToWatermarkIfFailed)
		{
			byte[] byteArray = null;  // really make this an error gif 

			try
			{

				Bitmap bitMap = new Bitmap(Buffer);
				int intOldWidth = bitMap.Width;
				int intOldHeight = bitMap.Height;

				int intNewWidth;
				int intNewHeight;

				int intMaxSide;

				if (intOldWidth >= intOldHeight)
				{
					intMaxSide = intOldWidth;
				}
				else
				{
					intMaxSide = intOldHeight;
				}

				if (intMaxSide > MaxSideSize)
				{
					//set new width and height
					double dblCoef = MaxSideSize / (double)intMaxSide;
					intNewWidth = Convert.ToInt32(dblCoef * intOldWidth);
					intNewHeight = Convert.ToInt32(dblCoef * intOldHeight);
				}
				else
				{
					intNewWidth = intOldWidth;
					intNewHeight = intOldHeight;
				}

				Size ThumbNailSize = new Size(intNewWidth, intNewHeight);
				System.Drawing.Image oImg = System.Drawing.Image.FromStream(Buffer);
				System.Drawing.Image oThumbNail = new Bitmap(ThumbNailSize.Width, ThumbNailSize.Height);

				Graphics oGraphic = Graphics.FromImage(oThumbNail);
				oGraphic.CompositingQuality = CompositingQuality.HighQuality;
				oGraphic.SmoothingMode = SmoothingMode.HighQuality;
				oGraphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
				Rectangle oRectangle = new Rectangle
					(0, 0, ThumbNailSize.Width, ThumbNailSize.Height);

				oGraphic.DrawImage(oImg, oRectangle);

				MemoryStream ms = new MemoryStream();
				oThumbNail.Save(ms, ImageFormat.Jpeg);
				byteArray = new byte[ms.Length];
				ms.Position = 0;
				ms.Read(byteArray, 0, Convert.ToInt32(ms.Length));

				oGraphic.Dispose();
				oImg.Dispose();
				ms.Close();
				ms.Dispose();
			}
			catch (Exception)
			{
				int newSize = MaxSideSize - 20;
				Bitmap bitMap = new Bitmap(newSize, newSize);
				Graphics g = Graphics.FromImage(bitMap);
				g.FillRectangle(new SolidBrush(Color.Gray), new Rectangle(0, 0, newSize, newSize));

				Font font = new Font("Courier", 8);
				SolidBrush solidBrush = new SolidBrush(Color.Red);
				g.DrawString("Failed File", font, solidBrush, 10, 5);
				g.DrawString(fileNameToWatermarkIfFailed, font, solidBrush, 10, 50);

				MemoryStream ms = new MemoryStream();
				bitMap.Save(ms, ImageFormat.Jpeg);
				byteArray = new byte[ms.Length];
				ms.Position = 0;
				ms.Read(byteArray, 0, Convert.ToInt32(ms.Length));

				ms.Close();
				ms.Dispose();
				bitMap.Dispose();
				solidBrush.Dispose();
				g.Dispose();
				font.Dispose();

			}
			return byteArray;
		}
		public static byte[] AddWaterMark(Byte[] byteArrayIn, string watermarkText, Brush brushcolor)
		{
			byte[] byteArray = null;
			MemoryStream ms = new MemoryStream(byteArrayIn);
			Image img = System.Drawing.Image.FromStream(ms);

			ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);
			System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
			EncoderParameters myEncoderParameters = new EncoderParameters(1);
			EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 75L);
			myEncoderParameters.Param[0] = myEncoderParameter;

			Graphics gr = Graphics.FromImage(img);
			gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			gr.DrawString(watermarkText, new Font("Tahoma", 10), brushcolor, new Point(0, 0));

			MemoryStream output = new MemoryStream();
			img.Save(output, jgpEncoder, myEncoderParameters);
			byteArray = new byte[output.Length];
			output.Position = 0;
			output.Read(byteArray, 0, Convert.ToInt32(output.Length));
			return byteArray;
		}
		public static byte[] AddWaterMarkWithQualitySetting(Byte[] byteArrayIn, string watermarkText, Brush brushcolor)
		{
			byte[] byteArray = null;
			MemoryStream ms = new MemoryStream(byteArrayIn);
			Image img = System.Drawing.Image.FromStream(ms);

			ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);
			System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
			EncoderParameters myEncoderParameters = new EncoderParameters(1);
			EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 75L); //%75
			myEncoderParameters.Param[0] = myEncoderParameter;

			Graphics gr = Graphics.FromImage(img);
			gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			gr.DrawString(watermarkText, new Font("Tahoma", 10), brushcolor, new Point(0, 0));

			MemoryStream output = new MemoryStream();
			img.Save(output, jgpEncoder, myEncoderParameters);
			byteArray = new byte[output.Length];
			output.Position = 0;
			output.Read(byteArray, 0, Convert.ToInt32(output.Length));
			return byteArray;
		}
		private static ImageCodecInfo GetEncoder(ImageFormat format)
		{
			ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

			foreach (ImageCodecInfo codec in codecs)
			{
				if (codec.FormatID == format.Guid)
				{
					return codec;
				}
			}
			return null;
		}
	}
}
