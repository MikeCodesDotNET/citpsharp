using Imp.CitpSharp.Packets.Msex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imp.CitpSharp
{
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

		public override bool Equals(object obj)
		{
			var m = obj as CitpElementLibraryInformation;
			if ((object)m == null)
				return false;

			return Equals(m);
		}

		public bool Equals(CitpElementLibraryInformation other)
		{
			if (other == null)
				return false;

			return Number == other.Number
				&& Id == other.Id
				&& SerialNumber == other.SerialNumber
				&& DmxRangeMin == other.DmxRangeMin
				&& DmxRangeMax == other.DmxRangeMax
				&& Name == other.Name
				&& LibraryCount == other.LibraryCount
				&& ElementCount == other.ElementCount;
		}

		public override int GetHashCode()
		{
			return Number.GetHashCode()
				^ (Id != null ? Id.GetHashCode() : 0)
				^ SerialNumber.GetHashCode()
				^ DmxRangeMin.GetHashCode()
				^ DmxRangeMax.GetHashCode()
				^ (Name != null ? Name.GetHashCode() : 0)
				^ LibraryCount.GetHashCode()
				^ ElementCount.GetHashCode();
		}
	}

	public sealed class CitpElementLibraryUpdatedInformation : IEquatable<CitpElementLibraryUpdatedInformation>
	{
		public MsexLibraryType LibraryType { get; set; }
		public byte LibraryNumber { get; set; }
		public MsexLibraryId? LibraryId { get; set; }

		public MsexElementLibraryUpdatedFlags UpdateFlags { get; set; }

		public List<byte> AffectedElements { get; set; }
		public List<byte> AffectedLibraries { get; set; }

		public override bool Equals(object obj)
		{
			var m = obj as CitpElementLibraryUpdatedInformation;
			if ((object)m == null)
				return false;

			return Equals(m);
		}

		public bool Equals(CitpElementLibraryUpdatedInformation other)
		{
			if (other == null)
				return false;

			return LibraryType == other.LibraryType
				&& LibraryNumber == other.LibraryNumber
				&& LibraryId == other.LibraryId
				&& UpdateFlags == other.UpdateFlags
				&& AffectedElements.ScrambledEquals(other.AffectedElements)
				&& AffectedLibraries.ScrambledEquals(other.AffectedLibraries);
		}

		public override int GetHashCode()
		{
			return LibraryType.GetHashCode()
				^ LibraryNumber.GetHashCode()
				^ LibraryId.GetHashCode()
				^ UpdateFlags.GetHashCode()
				^ AffectedElements.GetHashCode()
				^ AffectedLibraries.GetHashCode();
		}

		internal ElementLibraryUpdatedMessagePacket ToPacket()
		{
			return new ElementLibraryUpdatedMessagePacket
			{
				LibraryType = this.LibraryType,
				LibraryNumber = this.LibraryNumber,
				LibraryId = this.LibraryId,
				UpdateFlags = this.UpdateFlags,
				AffectedElements = this.AffectedElements,
				AffectedLibraries = this.AffectedLibraries
			};
		}
	}

	public abstract class CitpElementInformation
	{
		public byte ElementNumber { get; set; }
		public uint SerialNumber { get; set; }
		public byte DmxRangeMin { get; set; }
		public byte DmxRangeMax { get; set; }
		public string Name { get; set; }

		protected bool Equals(CitpElementInformation other)
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

	public sealed class CitpMediaInformation : CitpElementInformation, IEquatable<CitpMediaInformation>
	{
		public DateTime MediaVersionTimestamp { get; set; }

		public ushort MediaWidth { get; set; }
		public ushort MediaHeight { get; set; }
		public uint MediaLength { get; set; }
		public byte MediaFps { get; set; }

		public override bool Equals(object obj)
		{
			var m = obj as CitpMediaInformation;
			if ((object)m == null)
				return false;

			return Equals(m);
		}

		public bool Equals(CitpMediaInformation other)
		{
			if (other == null)
				return false;

			return base.Equals(other)
				&& MediaVersionTimestamp == other.MediaVersionTimestamp
				&& MediaWidth == other.MediaWidth
				&& MediaHeight == other.MediaHeight
				&& MediaLength == other.MediaLength
				&& MediaFps == other.MediaFps;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode()
				^ MediaVersionTimestamp.GetHashCode()
				^ MediaWidth.GetHashCode()
				^ MediaHeight.GetHashCode()
				^ MediaLength.GetHashCode()
				^ MediaFps.GetHashCode();
		}
	}

	public sealed class CitpEffectInformation : CitpElementInformation, IEquatable<CitpEffectInformation>
	{
		public List<string> EffectParameterNames { get; set; }

		public override bool Equals(object obj)
		{
			var m = obj as CitpEffectInformation;
			if ((object)m == null)
				return false;

			return Equals(m);
		}

		public bool Equals(CitpEffectInformation other)
		{
			if (other == null)
				return false;

			return base.Equals(other)
				&& EffectParameterNames.ScrambledEquals(other.EffectParameterNames);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode()
				^ (EffectParameterNames != null ? EffectParameterNames.GetHashCode() : 0);
		}
	}

	public sealed class CitpGenericInformation : CitpElementInformation, IEquatable<CitpGenericInformation>
	{
		public DateTime VersionTimestamp { get; set; }

		public override bool Equals(object obj)
		{
			var m = obj as CitpGenericInformation;
			if ((object)m == null)
				return false;

			return Equals(m);
		}

		public bool Equals(CitpGenericInformation other)
		{
			if (other == null)
				return false;

			return base.Equals(other)
				&& VersionTimestamp == other.VersionTimestamp;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode()
				^ VersionTimestamp.GetHashCode();
		}
	}

	public sealed class CitpVideoSourceInformation : IEquatable<CitpVideoSourceInformation>
	{
		public ushort SourceIdentifier { get; set; }
		public string SourceName { get; set; }

		public byte? PhysicalOutput { get; set; }
		public byte? LayerNumber { get; set; }

		public MsexVideoSourcesFlags Flags { get; set; }

		public ushort Width { get; set; }
		public ushort Height { get; set; }

		public override bool Equals(object obj)
		{
			var m = obj as CitpVideoSourceInformation;
			if ((object)m == null)
				return false;

			return Equals(m);
		}

		public bool Equals(CitpVideoSourceInformation other)
		{
			if (other == null)
				return false;

			return SourceIdentifier == other.SourceIdentifier
				&& SourceName == other.SourceName
				&& PhysicalOutput == other.PhysicalOutput
				&& LayerNumber == other.LayerNumber
				&& Flags == other.Flags
				&& Width == other.Width
				&& Height == other.Height;
		}

		public override int GetHashCode()
		{
			return SourceIdentifier.GetHashCode()
				^ (SourceName != null ? SourceName.GetHashCode() : 0)
				^ (PhysicalOutput != null ? PhysicalOutput.GetHashCode() : 0)
				^ (LayerNumber != null ? LayerNumber.GetHashCode() : 0)
				^ Flags.GetHashCode()
				^ Width.GetHashCode()
				^ Height.GetHashCode();
		}
	}
}
