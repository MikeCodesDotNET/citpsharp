using System;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	[PublicAPI]
	public sealed class VideoSourceInformation : IEquatable<VideoSourceInformation>,
		IComparable<VideoSourceInformation>
	{
		public VideoSourceInformation(ushort sourceIdentifier, string sourceName, MsexVideoSourcesFlags flags,
			ushort width, ushort height, byte? physicalOutput = null, byte? layerNumber = null)
		{
			SourceIdentifier = sourceIdentifier;
			SourceName = sourceName;
			PhysicalOutput = physicalOutput;
			LayerNumber = layerNumber;
			Flags = flags;
			Width = width;
			Height = height;
		}

		public ushort SourceIdentifier { get; }
		public string SourceName { get; }

		public byte? PhysicalOutput { get; }
		public byte? LayerNumber { get; }

		public MsexVideoSourcesFlags Flags { get; }

		public ushort Width { get; }
		public ushort Height { get; }

		public int CompareTo([CanBeNull] VideoSourceInformation other)
		{
			return ReferenceEquals(other, null) ? 1 : SourceIdentifier.CompareTo(other.SourceIdentifier);
		}

		public bool Equals([CanBeNull] VideoSourceInformation other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return SourceIdentifier == other.SourceIdentifier && string.Equals(SourceName, other.SourceName)
			       && PhysicalOutput == other.PhysicalOutput && LayerNumber == other.LayerNumber && Flags == other.Flags
			       && Width == other.Width && Height == other.Height;
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj is VideoSourceInformation && Equals((VideoSourceInformation)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = SourceIdentifier.GetHashCode();
				hashCode = (hashCode * 397) ^ SourceName.GetHashCode();
				hashCode = (hashCode * 397) ^ PhysicalOutput.GetHashCode();
				hashCode = (hashCode * 397) ^ LayerNumber.GetHashCode();
				hashCode = (hashCode * 397) ^ (int)Flags;
				hashCode = (hashCode * 397) ^ Width.GetHashCode();
				hashCode = (hashCode * 397) ^ Height.GetHashCode();
				return hashCode;
			}
		}
	}
}