using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Imp.CitpSharp
{
	public static class CitpImageHelpers
	{
		static public byte[] ToByteArray(this Image image, MsexImageFormat format, MsexVersion? version)
		{
			switch (format)
			{
				case MsexImageFormat.RGB8:
					if (version == MsexVersion.Version1_0)
						return image.ToRgb8ByteArray(true);
					else
						return image.ToRgb8ByteArray();

				case MsexImageFormat.JPEG:
					return image.ToJpegByteArray();

				case MsexImageFormat.PNG:
					return image.ToPngByteArray();

				default:
					return null;
			}
		}

		static public byte[] ToRgb8ByteArray(this Image image, bool isBgrOrder = false)
		{
			var bm = new Bitmap(image);

			if (bm.PixelFormat != PixelFormat.Format32bppArgb)
				return null;

			BitmapData bmd = bm.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, bm.PixelFormat);

			int length = Math.Abs(bmd.Stride) * image.Height;
			byte[] data = new byte[length];

			if (isBgrOrder)
			{
				unsafe
				{
					fixed (byte* dst = data)
					{
						byte* src = (byte*)bmd.Scan0.ToPointer();

						for (int i = 0, j = 0; j < length; i += 3, j += 4)
						{
							dst[i] = src[j + 3];
							dst[i + 1] = src[j + 2];
							dst[i + 2] = src[j + 1];
						}
					}
				}
			}
			else
			{
				unsafe
				{
					fixed (byte* dst = data)
					{
						byte* src = (byte*)bmd.Scan0.ToPointer();

						for (int i = 0, j = 0; j < length; i += 3, j += 4)
						{
							dst[i] = src[j + 1];
							dst[i + 1] = src[j + 2];
							dst[i + 2] = src[j + 3];
						}
					}
				}
			}

			

			bm.UnlockBits(bmd);

			return data;
		}

		static public byte[] ToJpegByteArray(this Image image)
		{
			byte[] data;

			using (var ms = new MemoryStream())
			{
				image.Save(ms, ImageFormat.Jpeg);
				data = ms.ToArray();
			}

			return data;
		}

		static public byte[] ToPngByteArray(this Image image)
		{
			byte[] data;

			using (var ms = new MemoryStream())
			{
				image.Save(ms, ImageFormat.Png);
				data = ms.ToArray();
			}

			return data;
		}

		static public Image Resize(this Image thumb, Size preferredSize, bool shouldPreserveAspect)
		{
			Size targetSize = new Size(Math.Min(preferredSize.Width, thumb.Width),
				Math.Min(preferredSize.Height, thumb.Height));

			if (shouldPreserveAspect)
			{
				Image resizedThumb = new Bitmap(targetSize.Width, targetSize.Height);
				Graphics graphic = Graphics.FromImage(resizedThumb);

				graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphic.SmoothingMode = SmoothingMode.HighQuality;
				graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphic.CompositingQuality = CompositingQuality.HighQuality;

				double ratioX = (double)targetSize.Width / (double)thumb.Width;
				double ratioY = (double)targetSize.Height / (double)thumb.Height;
				double ratio = ratioX < ratioY ? ratioX : ratioY;

				int newWidth = Convert.ToInt32(thumb.Width * ratio);
				int newHeight = Convert.ToInt32(thumb.Height * ratio);

				int imageTopLeftX = Convert.ToInt32((targetSize.Width - (thumb.Width * ratio)) / 2);
				int imageTopLeftY = Convert.ToInt32((targetSize.Height - (thumb.Height * ratio)) / 2);

				graphic.Clear(Color.Black);
				graphic.DrawImage(thumb, imageTopLeftX, imageTopLeftY, newWidth, newHeight);

				return resizedThumb;
			}
			else
			{
				return (Image)(new Bitmap(thumb, targetSize));
			}
		}
	}
}