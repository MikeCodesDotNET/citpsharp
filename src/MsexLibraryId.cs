using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	/// <summary>
	///     Represents a unique position in an MSEX element library.
	/// </summary>
	[PublicAPI]
	public struct MsexLibraryId : IEquatable<MsexLibraryId>, IComparable<MsexLibraryId>
	{
		public byte Level { get; }
		public byte SubLevel1 { get; }
		public byte SubLevel2 { get; }
		public byte SubLevel3 { get; }

		public MsexLibraryId(byte level, byte subLevel1 = 0, byte subLevel2 = 0, byte subLevel3 = 0)
			: this()
		{
			if (level > 3)
				throw new ArgumentOutOfRangeException(nameof(level), level, "level must be in range 0-3");

			Level = level;
			SubLevel1 = subLevel1;
			SubLevel2 = subLevel2;
			SubLevel3 = subLevel3;
		}

		public int CompareTo(MsexLibraryId other)
		{
			throw new NotImplementedException();
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

		public override string ToString()
		{
			return $"{{{Level},{SubLevel1},{SubLevel2},{SubLevel3}}}";
		}

		public byte[] ToByteArray()
		{
			return new[] {Level, SubLevel1, SubLevel2, SubLevel3};
		}


		public static bool operator ==(MsexLibraryId a, MsexLibraryId b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(MsexLibraryId a, MsexLibraryId b)
		{
			return !a.Equals(b);
		}
	}



	/// <summary>
	///     Value representing either a byte indicating a library number (MSEX 1.0) or a <see cref="MsexLibraryId" /> (MSEX
	///     1.1+)
	/// </summary>
	[PublicAPI]
	public struct MsexId : IEquatable<MsexId>, IComparable<MsexId>
	{
		public MsexId(MsexLibraryId? libraryId, byte? libraryNumber)
		{
			if (libraryId == null && libraryNumber == null)
				throw new ArgumentException("Cannot create an MsexId where both values are null");

			LibraryId = libraryId;
			LibraryNumber = libraryNumber;
		}

		public MsexId(MsexLibraryId? libraryId, int? libraryNumber)
			: this(libraryId, (byte?)libraryNumber) { }

		public MsexId(MsexLibraryId libraryId)
			: this(libraryId, null) { }

		public MsexId(byte libraryNumber)
			: this(null, libraryNumber) { }

		public MsexId(int libraryNumber)
			: this(null, (byte)libraryNumber) { }


		public MsexLibraryId? LibraryId { get; }
		public byte? LibraryNumber { get; }

		public bool IsVersion10 => LibraryNumber.HasValue;



		public int CompareTo(MsexId other)
		{
			if (IsVersion10 && !other.IsVersion10)
				return -1;

			if (!IsVersion10 && other.IsVersion10)
				return 1;

			if (IsVersion10 && other.IsVersion10)
			{
				Debug.Assert(LibraryNumber.HasValue && other.LibraryNumber.HasValue);
				return LibraryNumber.Value.CompareTo(other.LibraryNumber.Value);
			}

			Debug.Assert(LibraryId.HasValue && other.LibraryId.HasValue);
			return LibraryId.Value.CompareTo(other.LibraryId.Value);
		}

		public bool Equals(MsexId other)
		{
			return LibraryId.Equals(other.LibraryId) || LibraryNumber == other.LibraryNumber;
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			return obj is MsexId && Equals((MsexId)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (LibraryId.GetHashCode() * 397) ^ LibraryNumber.GetHashCode();
			}
		}

		public static bool operator ==(MsexId left, MsexId right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(MsexId left, MsexId right)
		{
			return !left.Equals(right);
		}
	}
}