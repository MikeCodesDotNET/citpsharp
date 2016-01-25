using System;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	[PublicAPI]
	public class CitpImage
	{
	}



	[PublicAPI]
	public struct CitpImageRequest : IEquatable<CitpImageRequest>
	{
		internal CitpImageRequest(int frameWidth, int frameHeight, MsexImageFormat format, bool isBgrOrder = false)
		{
			FrameWidth = frameWidth;
			FrameHeight = frameHeight;
			Format = format;
			IsBgrOrder = isBgrOrder;
		}



		public int FrameWidth { get; }
		public int FrameHeight { get; }
		public MsexImageFormat Format { get; }
		public bool IsBgrOrder { get; } 



		public bool Equals(CitpImageRequest other)
		{
			return FrameWidth == other.FrameWidth && FrameHeight == other.FrameHeight && Format == other.Format && IsBgrOrder == other.IsBgrOrder;
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
	}
}
