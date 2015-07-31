//  This file is part of CitpSharp.
//
//  CitpSharp is free software: you can redistribute it and/or modify
//	it under the terms of the GNU Lesser General Public License as published by
//	the Free Software Foundation, either version 3 of the License, or
//	(at your option) any later version.

//	CitpSharp is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU Lesser General Public License for more details.

//	You should have received a copy of the GNU Lesser General Public License
//	along with CitpSharp.  If not, see <http://www.gnu.org/licenses/>.

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
			return String.Format("{{{0},{1},{2},{3}}}", Level, SubLevel1, SubLevel2, SubLevel3);
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

		public bool Equals(MsexLibraryId other)
		{
			return other.Level == Level 
				&& other.SubLevel1 == SubLevel1 
				&& other.SubLevel2 == SubLevel2 
				&& other.SubLevel3 == SubLevel3;
		}

		public override int GetHashCode()
		{
			return Level.GetHashCode() 
				^ SubLevel1.GetHashCode() 
				^ SubLevel2.GetHashCode() 
				^ SubLevel3.GetHashCode();
		}

		public static bool operator ==(MsexLibraryId a, MsexLibraryId b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(MsexLibraryId a, MsexLibraryId b)
		{
			return !(a.Equals(b));
		}
	}
}
