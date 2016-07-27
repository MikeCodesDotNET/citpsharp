using System;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	/// <summary>
	///     Represents a unique position in an MSEX element library.
	/// </summary>
	[PublicAPI]
	public struct MsexLibraryId : IEquatable<MsexLibraryId>, IComparable<MsexLibraryId>
	{
		public static MsexLibraryId Root { get; } = new MsexLibraryId(0);

		public static MsexLibraryId FromMsexV1LibraryNumber(int libraryNumber) => new MsexLibraryId(1, libraryNumber);
		public static MsexLibraryId FromMsexV1LibraryNumber(byte libraryNumber) => new MsexLibraryId((byte)1, libraryNumber);

		public MsexLibraryId(int level, int subLevel1 = 0, int subLevel2 = 0, int subLevel3 = 0)
			: this((byte)level, (byte)subLevel1, (byte)subLevel2, (byte)subLevel3)
		{
			
		}

		public MsexLibraryId(byte level, byte subLevel1 = 0, byte subLevel2 = 0, byte subLevel3 = 0)
			: this()
		{
			if (level > 3)
				throw new ArgumentOutOfRangeException(nameof(level), level, "level must be in range 0-3");

			Level = level;
			
			SubLevel1 = Level > 0 ? subLevel1 : byte.MinValue;
			SubLevel2 = Level > 1 ? subLevel2 : byte.MinValue;
			SubLevel3 = Level > 2 ? subLevel3 : byte.MinValue;
		}

		private MsexLibraryId(MsexLibraryId other, byte? level = null, byte? subLevel1 = null, byte? subLevel2 = null, byte? subLevel3 = null)
			: this()
		{
			Level = level ?? other.Level;

			if (Level > 3)
				throw new ArgumentOutOfRangeException(nameof(level), level, "level must be in range 0-3");

			SubLevel1 = Level > 0 ? (subLevel1 ?? other.SubLevel1) : byte.MinValue;
			SubLevel2 = Level > 1 ? (subLevel2 ?? other.SubLevel2) : byte.MinValue;
			SubLevel3 = Level > 2 ? (subLevel3 ?? other.SubLevel3) : byte.MinValue;
		}

		private MsexLibraryId(MsexLibraryId other, int? level = null, int? subLevel1 = null, int? subLevel2 = null, int? subLevel3 = null)
			: this(other, (byte?)level, (byte?)subLevel1, (byte?)subLevel2, (byte?)subLevel3)
		{
			
		}

		public byte Level { get; }
		public byte SubLevel1 { get; }
		public byte SubLevel2 { get; }
		public byte SubLevel3 { get; }

		public byte LibraryNumber
		{
			get
			{
				switch (Level)
				{
					case 0:
						return 0;
					case 1:
						return SubLevel1;
					case 2:
						return SubLevel2;
					case 3:
						return SubLevel3;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public byte MsexV1LibraryNumber
		{
			get
			{
				if (Level != 1)
					throw new InvalidOperationException("This library ID cannot be represented as an MSEX V1.0");

				return SubLevel1;
			}
		}

		public bool IsRoot => Level == 0;

		public bool IsMsexV1Compatible => Level == 1;

		public bool CanHaveChildren => Level != 3;

		public MsexLibraryId SetLevel(byte value) => new MsexLibraryId(this, level: value);
		public MsexLibraryId SetLevel(int value) => new MsexLibraryId(this, level: value);
		public MsexLibraryId SetSubLevel1(byte value) => new MsexLibraryId(this, subLevel1: value);
		public MsexLibraryId SetSubLevel1(int value) => new MsexLibraryId(this, subLevel1: value);
		public MsexLibraryId SetSubLevel2(byte value) => new MsexLibraryId(this, subLevel2: value);
		public MsexLibraryId SetSubLevel2(int value) => new MsexLibraryId(this, subLevel2: value);
		public MsexLibraryId SetSubLevel3(byte value) => new MsexLibraryId(this, subLevel3: value);
		public MsexLibraryId SetSubLevel3(int value) => new MsexLibraryId(this, subLevel3: value);


		public MsexLibraryId SetLibraryNumber(byte value)
		{
			switch (Level)
			{
				case 0:
					return Root;
				case 1:
					return SetSubLevel1(value);
				case 2:
					return SetSubLevel2(value);
				case 3:
					return SetSubLevel3(value);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public MsexLibraryId SetLibraryNumber(int value)
		{
			switch (Level)
			{
				case 0:
					throw new InvalidOperationException("Library Id is root - cannot have a library number");
				case 1:
					return SetSubLevel1(value);
				case 2:
					return SetSubLevel2(value);
				case 3:
					return SetSubLevel3(value);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}


		public bool IsChildOf(MsexLibraryId other)
		{
			return other.Level == Level + 1 &&
				(other.Level == 1
				|| (other.Level == 2 && SubLevel1 == other.SubLevel1)
				|| (other.Level == 3 && SubLevel1 == other.SubLevel1 && SubLevel2 == other.SubLevel2));
		}

		public bool IsParentOf(MsexLibraryId other)
		{
			return other.IsChildOf(this);
		}
		 
		public bool IsDescendentOf(MsexLibraryId other)
		{
			return other.Level <= Level &&
				(other.Level == 1 
				||(other.Level == 2 && SubLevel1 == other.SubLevel1)
				|| (other.Level == 3 && SubLevel1 == other.SubLevel1 && SubLevel2 == other.SubLevel2));
		}

		public bool IsAncestorOf(MsexLibraryId other)
		{
			return other.IsDescendentOf(this);
		}

		

		public int CompareTo(MsexLibraryId other)
		{
			if (SubLevel1 < other.SubLevel1)
				return -1;

			if (SubLevel1 > other.SubLevel1)
				return 1;

			if (SubLevel2 < other.SubLevel2)
				return -1;

			if (SubLevel2 > other.SubLevel2)
				return 1;

			if (SubLevel3 < other.SubLevel3)
				return -1;

			if (SubLevel3 > other.SubLevel3)
				return 1;

			return 0;
		}

		public bool Equals(MsexLibraryId other)
		{
			return Level == other.Level && SubLevel1 == other.SubLevel1 && SubLevel2 == other.SubLevel2
			       && SubLevel3 == other.SubLevel3;
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			return obj is MsexLibraryId && Equals((MsexLibraryId)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = Level.GetHashCode();
				hashCode = (hashCode * 397) ^ SubLevel1.GetHashCode();
				hashCode = (hashCode * 397) ^ SubLevel2.GetHashCode();
				hashCode = (hashCode * 397) ^ SubLevel3.GetHashCode();
				return hashCode;
			}
		}


		public static MsexLibraryId FromByteArray([NotNull] byte[] array)
		{
			if (array == null)
				throw new ArgumentNullException(nameof(array));

			if (array.Length != 4)
				throw new ArgumentException("Array is incorrect length for library id.");

			if (array[0] > 3)
				throw new ArgumentException("Invalid MsexLibraryId, level must be in range 0-3");

			return new MsexLibraryId(array[0], array[1], array[2], array[3]);
		}

		public override string ToString() => $"{{{Level},{SubLevel1},{SubLevel2},{SubLevel3}}}";

		public byte[] ToByteArray() => new[] {Level, SubLevel1, SubLevel2, SubLevel3};


		public static bool operator ==(MsexLibraryId a, MsexLibraryId b) => a.Equals(b);

		public static bool operator !=(MsexLibraryId a, MsexLibraryId b) => !a.Equals(b);

		public static bool operator <(MsexLibraryId a, MsexLibraryId b) => a.CompareTo(b) == -1;

		public static bool operator >(MsexLibraryId a, MsexLibraryId b) => a.CompareTo(b) == 1;

		public static bool operator <=(MsexLibraryId a, MsexLibraryId b) => a.CompareTo(b) != 1;

		public static bool operator >=(MsexLibraryId a, MsexLibraryId b) => a.CompareTo(b) != -1;
	}
}