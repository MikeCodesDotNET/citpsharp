using System;
using System.Collections.Generic;
using Imp.CitpSharp.Packets;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	[PublicAPI]
	public sealed class CitpElementLibraryInformation : IEquatable<CitpElementLibraryInformation>
	{
		public byte Number { get; set; }
		public MsexLibraryId? Id { get; set; }
		public uint SerialNumber { get; set; }
		public byte DmxRangeMin { get; set; }
		public byte DmxRangeMax { get; set; }
		public string Name { get; set; }
		public ushort LibraryCount { get; set; }
		public ushort ElementCount { get; set; }

		public bool Equals([CanBeNull] CitpElementLibraryInformation other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return Number == other.Number && Id.Equals(other.Id) && SerialNumber == other.SerialNumber && DmxRangeMin == other.DmxRangeMin && DmxRangeMax == other.DmxRangeMax && string.Equals(Name, other.Name) && LibraryCount == other.LibraryCount && ElementCount == other.ElementCount;
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj is CitpElementLibraryInformation && Equals((CitpElementLibraryInformation)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = Number.GetHashCode();
				hashCode = (hashCode * 397) ^ Id.GetHashCode();
				hashCode = (hashCode * 397) ^ (int)SerialNumber;
				hashCode = (hashCode * 397) ^ DmxRangeMin.GetHashCode();
				hashCode = (hashCode * 397) ^ DmxRangeMax.GetHashCode();
				hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ LibraryCount.GetHashCode();
				hashCode = (hashCode * 397) ^ ElementCount.GetHashCode();
				return hashCode;
			}
		}
	}


	[PublicAPI]
	public sealed class CitpElementLibraryUpdatedInformation : IEquatable<CitpElementLibraryUpdatedInformation>
	{
		public MsexLibraryType LibraryType { get; set; }
		public byte LibraryNumber { get; set; }
		public MsexLibraryId? LibraryId { get; set; }

		public MsexElementLibraryUpdatedFlags UpdateFlags { get; set; }

		public List<byte> AffectedElements { get; set; }
		public List<byte> AffectedLibraries { get; set; }

		public bool Equals([CanBeNull] CitpElementLibraryUpdatedInformation other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return LibraryType == other.LibraryType 
				&& LibraryNumber == other.LibraryNumber 
				&& LibraryId.Equals(other.LibraryId) 
				&& UpdateFlags == other.UpdateFlags 
				&& SequenceComparison.SequenceEqual(AffectedElements, other.AffectedElements)
				&& SequenceComparison.SequenceEqual(AffectedLibraries, other.AffectedLibraries);
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj is CitpElementLibraryUpdatedInformation && Equals((CitpElementLibraryUpdatedInformation)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (int)LibraryType;
				hashCode = (hashCode * 397) ^ LibraryNumber.GetHashCode();
				hashCode = (hashCode * 397) ^ LibraryId.GetHashCode();
				hashCode = (hashCode * 397) ^ (int)UpdateFlags;
				hashCode = (hashCode * 397) ^ (AffectedElements != null ? AffectedElements.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (AffectedLibraries != null ? AffectedLibraries.GetHashCode() : 0);
				return hashCode;
			}
		}

		internal ElementLibraryUpdatedMessagePacket ToPacket()
		{
			return new ElementLibraryUpdatedMessagePacket
			{
				LibraryType = LibraryType,
				LibraryNumber = LibraryNumber,
				LibraryId = LibraryId,
				UpdateFlags = UpdateFlags,
				AffectedElements = AffectedElements,
				AffectedLibraries = AffectedLibraries
			};
		}
	}


	[PublicAPI]
	public abstract class CitpElementInformation
	{
		public byte ElementNumber { get; set; }
		public uint SerialNumber { get; set; }
		public byte DmxRangeMin { get; set; }
		public byte DmxRangeMax { get; set; }
		public string Name { get; set; }

		protected bool Equals([CanBeNull] CitpElementInformation other)
		{
			if (other == null)
				return false;

			return ElementNumber == other.ElementNumber
			       && SerialNumber == other.SerialNumber
			       && DmxRangeMin == other.DmxRangeMin
			       && DmxRangeMax == other.DmxRangeMax
			       && Name == other.Name;
		}

		public override int GetHashCode()
		{
			return ElementNumber.GetHashCode()
			       ^ SerialNumber.GetHashCode()
			       ^ DmxRangeMin.GetHashCode()
			       ^ DmxRangeMax.GetHashCode()
			       ^ (Name != null ? Name.GetHashCode() : 0);
		}
	}


	[PublicAPI]
	public sealed class CitpMediaInformation : CitpElementInformation, IEquatable<CitpMediaInformation>
	{
		public DateTime MediaVersionTimestamp { get; set; }

		public ushort MediaWidth { get; set; }
		public ushort MediaHeight { get; set; }
		public uint MediaLength { get; set; }
		public byte MediaFps { get; set; }

		public bool Equals([CanBeNull] CitpMediaInformation other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;

			return base.Equals(other) && MediaVersionTimestamp.Equals(other.MediaVersionTimestamp) && MediaWidth == other.MediaWidth && MediaHeight == other.MediaHeight && MediaLength == other.MediaLength && MediaFps == other.MediaFps;
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj is CitpMediaInformation && Equals((CitpMediaInformation)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = base.GetHashCode();
				hashCode = (hashCode * 397) ^ MediaVersionTimestamp.GetHashCode();
				hashCode = (hashCode * 397) ^ MediaWidth.GetHashCode();
				hashCode = (hashCode * 397) ^ MediaHeight.GetHashCode();
				hashCode = (hashCode * 397) ^ (int)MediaLength;
				hashCode = (hashCode * 397) ^ MediaFps.GetHashCode();
				return hashCode;
			}
		}
	}


	[PublicAPI]
	public sealed class CitpEffectInformation : CitpElementInformation, IEquatable<CitpEffectInformation>
	{
		public List<string> EffectParameterNames { get; set; }

		public bool Equals([CanBeNull] CitpEffectInformation other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;

			return base.Equals(other) && Equals(EffectParameterNames, other.EffectParameterNames);
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj is CitpEffectInformation && Equals((CitpEffectInformation)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (base.GetHashCode() * 397) ^ (EffectParameterNames != null ? EffectParameterNames.GetHashCode() : 0);
			}
		}
	}


	[PublicAPI]
	public sealed class CitpGenericInformation : CitpElementInformation, IEquatable<CitpGenericInformation>
	{
		public DateTime VersionTimestamp { get; set; }

		public bool Equals([CanBeNull] CitpGenericInformation other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return base.Equals(other) && VersionTimestamp.Equals(other.VersionTimestamp);
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj is CitpGenericInformation && Equals((CitpGenericInformation)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (base.GetHashCode() * 397) ^ VersionTimestamp.GetHashCode();
			}
		}
	}


	[PublicAPI]
	public sealed class CitpVideoSourceInformation : IEquatable<CitpVideoSourceInformation>
	{
		public ushort SourceIdentifier { get; set; }
		public string SourceName { get; set; }

		public byte? PhysicalOutput { get; set; }
		public byte? LayerNumber { get; set; }

		public MsexVideoSourcesFlags Flags { get; set; }

		public ushort Width { get; set; }
		public ushort Height { get; set; }

		public bool Equals([CanBeNull] CitpVideoSourceInformation other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return SourceIdentifier == other.SourceIdentifier && string.Equals(SourceName, other.SourceName) && PhysicalOutput == other.PhysicalOutput && LayerNumber == other.LayerNumber && Flags == other.Flags && Width == other.Width && Height == other.Height;
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj is CitpVideoSourceInformation && Equals((CitpVideoSourceInformation)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = SourceIdentifier.GetHashCode();
				hashCode = (hashCode * 397) ^ (SourceName != null ? SourceName.GetHashCode() : 0);
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