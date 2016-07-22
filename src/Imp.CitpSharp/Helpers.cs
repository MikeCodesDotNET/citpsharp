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
}