using System;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	/// <summary>
	///     Contains byte data of an image generated in response to a <see cref="CitpImageRequest" />
	/// </summary>
	[PublicAPI]
	public class CitpImage
	{
		public CitpImage(CitpImageRequest request, [NotNull] byte[] data, int actualWidth, int actualHeight)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			Request = request;
			Data = data;
			ActualWidth = actualWidth;
			ActualHeight = actualHeight;
		}

		/// <summary>
		///     The request the image was generated in response to
		/// </summary>
		public CitpImageRequest Request { get; }

		/// <summary>
		///     The byte data of the image
		/// </summary>
		public byte[] Data { get; }

		/// <summary>
		///     The actual width of the image contained in <see cref="Data" />
		/// </summary>
		public int ActualWidth { get; }

		/// <summary>
		///     The actual height of the image contained in <see cref="Data" />
		/// </summary>
		public int ActualHeight { get; }
	}



	/// <summary>
	///     Represents a request for either a library/element thumbnail or streaming frame from a CITP peer.
	/// </summary>
	/// <seealso cref="CitpImage" />
	[PublicAPI]
	public struct CitpImageRequest : IEquatable<CitpImageRequest>
	{
		internal CitpImageRequest(int frameWidth, int frameHeight, MsexImageFormat format, bool isPreserveAspectRatio = false,
			bool isBgrOrder = false)
		{
			FrameWidth = frameWidth;
			FrameHeight = frameHeight;
			Format = format;
			IsPreserveAspectRatio = isPreserveAspectRatio;
			IsBgrOrder = isBgrOrder;
		}

		/// <summary>
		///     The requested width for the image.
		/// </summary>
		public int FrameWidth { get; }

		/// <summary>
		///     The requested height for the image
		/// </summary>
		public int FrameHeight { get; }

		/// <summary>
		///     The requested format for the image
		/// </summary>
		public MsexImageFormat Format { get; }

		/// <summary>
		///     When true, indicates that the requested image should be scaled to fit the requested width and height without
		///     changing the image aspect ratio.
		/// </summary>
		public bool IsPreserveAspectRatio { get; }

		/// <summary>
		///     When true, and when <see cref="Format" /> is equal to RGB, indicates that the ordering of the bytes should be BGR
		///     rather than RGB.
		/// </summary>
		/// <remarks>This will only be true when communicating with MSEX 1.0 clients</remarks>
		public bool IsBgrOrder { get; }



		public bool Equals(CitpImageRequest other)
		{
			return FrameWidth == other.FrameWidth && FrameHeight == other.FrameHeight && Format == other.Format
			       && IsBgrOrder == other.IsBgrOrder;
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			return obj is CitpImageRequest && Equals((CitpImageRequest)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = FrameWidth;
				hashCode = (hashCode * 397) ^ FrameHeight;
				hashCode = (hashCode * 397) ^ (int)Format;
				hashCode = (hashCode * 397) ^ IsBgrOrder.GetHashCode();
				return hashCode;
			}
		}

		public static bool operator ==(CitpImageRequest left, CitpImageRequest right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(CitpImageRequest left, CitpImageRequest right)
		{
			return !left.Equals(right);
		}

		public override string ToString()
		{
			return $"{FrameWidth} x {FrameHeight}"
			       + (IsPreserveAspectRatio ? " (Preserve Aspect)" : string.Empty) +
			       ", {Format}"
			       + (IsBgrOrder ? " BGR Order" : string.Empty);
		}
	}
}