using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	internal static class DateTimeHelpers
	{
		public static DateTime ConvertFromUnixTimestamp(ulong timestamp)
		{
			var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
			return origin.AddSeconds(timestamp);
		}

		public static ulong ConvertToUnixTimestamp(DateTime date)
		{
			var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
			var diff = date.ToUniversalTime() - origin;
			return (ulong)Math.Floor(diff.TotalSeconds);
		}
	}



	internal static class ArrayHelpers
	{
		public static byte[][] Split(this byte[] arrayIn, int length)
		{
			bool even = arrayIn.Length % length == 0;
			int totalLength = arrayIn.Length / length;
			if (!even)
				++totalLength;

			var newArray = new byte[totalLength][];
			for (int i = 0; i < totalLength; ++i)
			{
				int allocLength = length;
				if (!even && i == totalLength - 1)
					allocLength = arrayIn.Length % length;

				newArray[i] = new byte[allocLength];
				Buffer.BlockCopy(arrayIn, i * length, newArray[i], 0, allocLength);
			}
			return newArray;
		}
	}



	internal static class SequenceComparison
	{
		public static bool SequenceEqual<T>([CanBeNull] IEnumerable<T> a, [CanBeNull] IEnumerable<T> b)
		{
			if (ReferenceEquals(a, b))
				return true;

			if (a == null || b == null)
				return false;

			return a.SequenceEqual(b);
		}

		public static bool SequenceEqual<T>([CanBeNull] IEnumerable<T> a, [CanBeNull] IEnumerable<T> b,
			IEqualityComparer<T> comparer)
		{
			if (ReferenceEquals(a, b))
				return true;

			if (a == null || b == null)
				return false;

			return a.SequenceEqual(b, comparer);
		}

		public static bool SequenceEqual<T>([CanBeNull] ICollection<T> a, [CanBeNull] ICollection<T> b)
		{
			if (ReferenceEquals(a, b))
				return true;

			if (a == null || b == null)
				return false;

			return a.Count == b.Count && a.SequenceEqual(b);
		}

		public static bool SequenceEqual<T>([CanBeNull] ICollection<T> a, [CanBeNull] ICollection<T> b,
			IEqualityComparer<T> comparer)
		{
			if (ReferenceEquals(a, b))
				return true;

			if (a == null || b == null)
				return false;

			return a.Count == b.Count && a.SequenceEqual(b, comparer);
		}
	}



	internal class CitpBinaryWriter : BinaryWriter
	{
		public CitpBinaryWriter(Stream output)
			: base(output, Encoding.Unicode) { }

		public override void Write([CanBeNull] string value)
		{
			Write(value, false);
		}

		public void Write([CanBeNull] string value, bool isUtf8)
		{
			Write(isUtf8
				? Encoding.UTF8.GetBytes((value ?? string.Empty) + "\0")
				: Encoding.Unicode.GetBytes((value ?? string.Empty) + "\0"));
		}

		public void Write(Guid value)
		{
			Write(Encoding.UTF8.GetBytes(value.ToString("D")));
		}
	}



	internal class CitpBinaryReader : BinaryReader
	{
		public CitpBinaryReader(Stream input)
			: base(input, Encoding.Unicode) { }

		public override string ReadString()
		{
			return ReadString(false);
		}

		public string ReadString(bool isUtf8)
		{
			var result = new StringBuilder(32);

			for (int i = 0; i < BaseStream.Length; ++i)
			{
				char c = isUtf8 ? Convert.ToChar(ReadByte()) : ReadChar();

				if (c == 0)
					break;

				result.Append(c);
			}

			return result.ToString();
		}

		public string ReadIdString()
		{
			return Encoding.UTF8.GetString(ReadBytes(4), 0, 4);
		}

		public Guid ReadGuid()
		{
			return Guid.Parse(Encoding.UTF8.GetString(ReadBytes(36), 0, 36));
		}
	}
}