//  This file is part of CitpSharp.
//
//  CitpSharp is free software: you can redistribute it and/or modify
//	it under the terms of the GNU Lesser General Public License as published by
//	the Free Software Foundation, either version 3 of the License, or
//	(at your option) any later version.

//	CitpSharp is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU Lesser General Public License for more details.

//	You should have received a copy of the GNU Lesser General Public License
//	along with CitpSharp.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Imp.CitpSharp
{
	internal static class CitpImageHelpers
	{
		private const long JpegEncodingQuality = 50;
		private static readonly ImageCodecInfo JpegEncoder = getEncoder(ImageFormat.Jpeg);
		private static readonly EncoderParameters JpegEncoderParameters;

		static CitpImageHelpers()
		{
			JpegEncoderParameters = new EncoderParameters(1);
			var encoder = Encoder.Quality;
			JpegEncoderParameters.Param[0] = new EncoderParameter(encoder, JpegEncodingQuality);
		}

		public static byte[] ToByteArray(this Image image, MsexImageFormat format, MsexVersion? version)
		{
			switch (format)
			{
				case MsexImageFormat.Rgb8:
					if (version == MsexVersion.Version10)
						return image.ToRgb8ByteArray(true);
					return image.ToRgb8ByteArray();

				case MsexImageFormat.Jpeg:
					return image.ToJpegByteArray();

				case MsexImageFormat.Png:
					return image.ToPngByteArray();

				default:
					return null;
			}
		}

		public static byte[] ToRgb8ByteArray(this Image image, bool isBgrOrder = false)
		{
			var bm = new Bitmap(image);

			if (bm.PixelFormat != PixelFormat.Format32bppArgb)
				return null;

			var bmd = bm.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, bm.PixelFormat);

			int length = Math.Abs(bmd.Stride) * image.Height;
			var data = new byte[length];

			if (isBgrOrder)
			{
				unsafe
				{
					fixed (byte* dst = data)
					{
						var src = (byte*)bmd.Scan0.ToPointer();

						for (int i = 0, j = 0; j < length; i += 3, j += 4)
						{
							dst[i] = src[j];
							dst[i + 1] = src[j + 1];
							dst[i + 2] = src[j + 2];
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
						var src = (byte*)bmd.Scan0.ToPointer();

						for (int i = 0, j = 0; j < length; i += 3, j += 4)
						{
							dst[i] = src[j + 2];
							dst[i + 1] = src[j + 1];
							dst[i + 2] = src[j];
						}
					}
				}
			}



			bm.UnlockBits(bmd);

			return data;
		}

		public static byte[] ToJpegByteArray(this Image image)
		{
			byte[] data;

			using (var ms = new MemoryStream())
			{
				image.Save(ms, JpegEncoder, JpegEncoderParameters);
				data = ms.ToArray();
			}

			return data;
		}

		public static byte[] ToPngByteArray(this Image image)
		{
			byte[] data;

			using (var ms = new MemoryStream())
			{
				image.Save(ms, ImageFormat.Png);
				data = ms.ToArray();
			}

			return data;
		}

		public static Image Resize(this Image thumb, Size preferredSize, bool shouldPreserveAspect)
		{
			var targetSize = new Size(Math.Min(preferredSize.Width, thumb.Width),
				Math.Min(preferredSize.Height, thumb.Height));

			if (shouldPreserveAspect)
			{
				Image resizedThumb = new Bitmap(targetSize.Width, targetSize.Height);
				var graphic = Graphics.FromImage(resizedThumb);

				graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphic.SmoothingMode = SmoothingMode.HighQuality;
				graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphic.CompositingQuality = CompositingQuality.HighQuality;

				double ratioX = targetSize.Width / (double)thumb.Width;
				double ratioY = targetSize.Height / (double)thumb.Height;
				double ratio = ratioX < ratioY ? ratioX : ratioY;

				int newWidth = Convert.ToInt32(thumb.Width * ratio);
				int newHeight = Convert.ToInt32(thumb.Height * ratio);

				int imageTopLeftX = Convert.ToInt32((targetSize.Width - (thumb.Width * ratio)) / 2);
				int imageTopLeftY = Convert.ToInt32((targetSize.Height - (thumb.Height * ratio)) / 2);

				graphic.Clear(Color.Black);
				graphic.DrawImage(thumb, imageTopLeftX, imageTopLeftY, newWidth, newHeight);

				return resizedThumb;
			}
			return new Bitmap(thumb, targetSize);
		}



		private static ImageCodecInfo getEncoder(ImageFormat format)
		{
			var codecs = ImageCodecInfo.GetImageDecoders();

			return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid);
		}
	}
}