using System;
using System.IO;
using System.Text;

namespace Imp.CitpSharp
{
	class DateTimeHelpers
	{
		public static DateTime ConvertFromUnixTimestamp(ulong timestamp)
		{
			DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
			return origin.AddSeconds(timestamp);
		}

		public static ulong ConvertToUnixTimestamp(DateTime date)
		{
			DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
			TimeSpan diff = date.ToUniversalTime() - origin;
			return (ulong)Math.Floor(diff.TotalSeconds);
		}
	}

	public struct MsexId
	{
		public MsexId(MsexLibraryId? libraryId, byte libaryNumber)
		{
			LibraryId = libraryId;
			LibraryNumber = libaryNumber;
		}

		public MsexId(MsexLibraryId? libraryId, int libraryNumber)
			: this(libraryId, (byte)libraryNumber)
		{
		}

		public MsexId(MsexLibraryId libraryId)
		{
			LibraryId = libraryId;
			LibraryNumber = null;
		}

		public MsexId(byte libraryNumber)
		{
			LibraryNumber = libraryNumber;
			LibraryId = null;
		}

		public MsexId(int libraryNumber)
			: this((byte)libraryNumber)
		{

		}

		public MsexLibraryId? LibraryId;
		public byte? LibraryNumber;
	}

	public class CitpBinaryWriter : BinaryWriter
	{
		public CitpBinaryWriter(Stream output)
			: base(output, Encoding.Unicode)
		{

		}

		public override void Write(string value)
		{
			Write(value, false);
		}

		public void Write(string value, bool isUtf8)
		{
			if (isUtf8)
				Write(Encoding.UTF8.GetBytes((value ?? String.Empty) + "\0"));
			else
				Write(Encoding.Unicode.GetBytes((value ?? String.Empty) + "\0"));
		}
	}

	public class CitpBinaryReader : BinaryReader
	{
		public CitpBinaryReader(Stream input)
			: base(input, Encoding.Unicode)
		{

		}

		public override string ReadString()
		{
			return ReadString(false);
		}

		public string ReadString(bool isUtf8)
		{
			var result = new StringBuilder(32);
			char c;

			for (int i = 0; i < this.BaseStream.Length; ++i)
			{
				if (isUtf8)
					c = Convert.ToChar(ReadByte());
				else
					c = this.ReadChar();

				if (c == 0)
					break;

				result.Append(c);
			}

			return result.ToString();
		}

		public string ReadIdString()
		{
			return Encoding.UTF8.GetString(ReadBytes(4));
		}
	}
}
