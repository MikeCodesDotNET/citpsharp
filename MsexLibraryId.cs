using System;

namespace Imp.CitpSharp
{
	public struct MsexLibraryId
	{
		public byte Level { get; set; }
		public byte SubLevel1 { get; set; }
		public byte SubLevel2 { get; set; }
		public byte SubLevel3 { get; set; }

		public MsexLibraryId(byte level, byte subLevel1, byte subLevel2, byte subLebel3)
			: this()
		{
			Level = level;
			SubLevel1 = subLevel1;
			SubLevel2 = subLevel2;
			SubLevel3 = subLebel3;
		}

		public MsexLibraryId(int level, int subLevel1, int subLevel2, int subLebel3)
			: this((byte)level, (byte)subLevel1, (byte)subLevel2, (byte)subLebel3)
		{
		}

		static public MsexLibraryId FromByteArray(byte[] array)
		{
			if (array.Length != 4)
				throw new InvalidOperationException("Array is incorrect length for library id.");

			var id = new MsexLibraryId();

			id.Level = array[0];
			id.SubLevel1 = array[1];
			id.SubLevel2 = array[2];
			id.SubLevel3 = array[3];

			return id;
		}

		public override string ToString()
		{
			return String.Format("{{0},{1},{2},{3}}");
		}

		public byte[] ToByteArray()
		{
			return new byte[] { Level, SubLevel1, SubLevel2, SubLevel3 };
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			if (!(obj is MsexLibraryId))
				return false;

			return Equals((MsexLibraryId)obj);
		}

		public bool Equals(MsexLibraryId obj)
		{
			return obj.Level == Level && obj.SubLevel1 == SubLevel1 && obj.SubLevel2 == SubLevel2 && obj.SubLevel3 == SubLevel3;
		}

		public override int GetHashCode()
		{
			return Level.GetHashCode() ^ SubLevel1.GetHashCode() ^ SubLevel2.GetHashCode() ^ SubLevel3.GetHashCode();
		}

		public static bool operator ==(MsexLibraryId a, MsexLibraryId b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(MsexLibraryId a, MsexLibraryId b)
		{
			return !(a == b);
		}
	}
}
