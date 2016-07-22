using System;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	[PublicAPI]
	public sealed class ElementLibraryInformation : IEquatable<ElementLibraryInformation>, IComparable<ElementLibraryInformation>
	{
		internal static ElementLibraryInformation Deserialize(CitpBinaryReader reader, MsexVersion version)
		{
			byte number = 0;
			MsexLibraryId? id = null;
			uint serialNumber;
			byte dmxRangeMin;
			byte dmxRangeMax;
			string name;
			ushort libraryCount = 0;
			ushort elementCount;

			switch (version)
			{
				case MsexVersion.Version1_0:
					number = reader.ReadByte();
					break;
				case MsexVersion.Version1_1:
				case MsexVersion.Version1_2:
					id = reader.ReadLibraryId();
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(version), version, null);
			}

			serialNumber = reader.ReadUInt32();
			dmxRangeMin = reader.ReadByte();
			dmxRangeMax = reader.ReadByte();
			name = reader.ReadString();

			switch (version)
			{
				case MsexVersion.Version1_0:
					elementCount = reader.ReadByte();
					break;
				case MsexVersion.Version1_1:
					libraryCount = reader.ReadByte();
					elementCount = reader.ReadByte();
					break;
				case MsexVersion.Version1_2:
					libraryCount = reader.ReadUInt16();
					elementCount = reader.ReadUInt16();
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(version), version, null);
			}

			return new ElementLibraryInformation(number, id, dmxRangeMin, dmxRangeMax, name, libraryCount, elementCount,
				serialNumber);
		}

		public ElementLibraryInformation(byte number, byte dmxRangeMin, byte dmxRangeMax, [NotNull] string name,
			ushort elementCount)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			Number = number;
			SerialNumber = 0;
			DmxRangeMin = dmxRangeMin;
			DmxRangeMax = dmxRangeMax;
			Name = name;
			LibraryCount = 1;
			ElementCount = elementCount;
		}

		public ElementLibraryInformation(MsexLibraryId id, byte dmxRangeMin, byte dmxRangeMax, [NotNull] string name,
			ushort libraryCount, ushort elementCount, uint serialNumber = 0)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			Id = id;
			SerialNumber = serialNumber;
			DmxRangeMin = dmxRangeMin;
			DmxRangeMax = dmxRangeMax;
			Name = name;
			LibraryCount = libraryCount;
			ElementCount = elementCount;
		}

		internal ElementLibraryInformation(byte number, MsexLibraryId? id, byte dmxRangeMin, byte dmxRangeMax, [NotNull] string name,
			ushort libraryCount, ushort elementCount, uint serialNumber)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			Number = number;
			Id = id;
			SerialNumber = serialNumber;
			DmxRangeMin = dmxRangeMin;
			DmxRangeMax = dmxRangeMax;
			Name = name;
			LibraryCount = libraryCount;
			ElementCount = elementCount;
		}

		public byte Number { get; }
		public MsexLibraryId? Id { get; }
		public uint SerialNumber { get; }
		public byte DmxRangeMin { get; }
		public byte DmxRangeMax { get; }
		public string Name { get; }
		public ushort LibraryCount { get; }
		public ushort ElementCount { get; }

		internal void Serialize(CitpBinaryWriter writer, MsexVersion version)
		{
			switch (version)
			{
				case MsexVersion.Version1_0:
					writer.Write(Number);
					break;
				case MsexVersion.Version1_1:
				case MsexVersion.Version1_2:
					if (!Id.HasValue)
						throw new InvalidOperationException("Element Id has no value. Required for MSEX V1.1+");

					writer.Write(Id.Value);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(version), version, null);
			}

			writer.Write(SerialNumber);
			writer.Write(DmxRangeMin);
			writer.Write(DmxRangeMax);
			writer.Write(Name);

			switch (version)
			{
				case MsexVersion.Version1_0:
					writer.Write((byte)ElementCount);
					break;
				case MsexVersion.Version1_1:
					writer.Write((byte)LibraryCount);
					writer.Write((byte)ElementCount);
					break;
				case MsexVersion.Version1_2:
					writer.Write(LibraryCount);
					writer.Write(ElementCount);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(version), version, null);
			}
		}

		public bool Equals([CanBeNull] ElementLibraryInformation other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return Number == other.Number && Id.Equals(other.Id) && SerialNumber == other.SerialNumber && DmxRangeMin == other.DmxRangeMin && DmxRangeMax == other.DmxRangeMax && string.Equals(Name, other.Name) && LibraryCount == other.LibraryCount && ElementCount == other.ElementCount;
		}

		public int CompareTo([CanBeNull] ElementLibraryInformation other)
		{
			if (ReferenceEquals(other, null))
				return 1;

			if (Id.HasValue && !other.Id.HasValue)
				return -1;
			if (!Id.HasValue && other.Id.HasValue)
				return 1;
			if (Id.HasValue && other.Id.HasValue)
				return Id.Value.CompareTo(other.Id.Value);

			return Number.CompareTo(other.Number);
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj is ElementLibraryInformation && Equals((ElementLibraryInformation)obj);
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
				hashCode = (hashCode * 397) ^ Name.GetHashCode();
				hashCode = (hashCode * 397) ^ LibraryCount.GetHashCode();
				hashCode = (hashCode * 397) ^ ElementCount.GetHashCode();
				return hashCode;
			}
		}
	}
}