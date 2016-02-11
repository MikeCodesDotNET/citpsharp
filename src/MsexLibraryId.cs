using System;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	[PublicAPI]
	public struct MsexLibraryId : IEquatable<MsexLibraryId>
	{
		public byte Level { get; private set; }
		public byte SubLevel1 { get; private set; }
		public byte SubLevel2 { get; private set; }
		public byte SubLevel3 { get; private set; }

		public MsexLibraryId(byte level, byte subLevel1 = 0, byte subLevel2 = 0, byte subLevel3 = 0)
			: this()
		{
			Level = level;
			SubLevel1 = subLevel1;
			SubLevel2 = subLevel2;
			SubLevel3 = subLevel3;
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


		public static MsexLibraryId FromByteArray(byte[] array)
		{
			if (array.Length != 4)
				throw new InvalidOperationException("Array is incorrect length for library id.");

			var id = new MsexLibraryId
			{
				Level = array[0],
				SubLevel1 = array[1],
				SubLevel2 = array[2],
				SubLevel3 = array[3]
			};


			return id;
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
}