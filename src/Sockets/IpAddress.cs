using System;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Imp.CitpSharp.Sockets
{
	internal struct IpAddress : IEquatable<IpAddress>
	{
		private const int MulticastRangeLower = 224;
		private const int MulticastRangeUpper = 239;

		private static readonly Regex IpMatch = new Regex(
			@"^\b(?:(25[0-5]|2[0-4][0-9]|[01]?[0-9]?[0-9])\.(25[0-5]|2[0-4][0-9]|[01]?[0-9]?[0-9])\.(25[0-5]|2[0-4][0-9]|[01]?[0-9]?[0-9])\.(25[0-5]|2[0-4][0-9]|[01]?[0-9]?[0-9]))\b$");

		public static IpAddress Parse(string s)
		{
			var match = IpMatch.Match(s);

			if (!match.Success)
				throw new FormatException("String is not a valid IP address");

			return new IpAddress(byte.Parse(match.Groups[1].Value),
				byte.Parse(match.Groups[2].Value),
				byte.Parse(match.Groups[3].Value),
				byte.Parse(match.Groups[4].Value));
		}

		public static bool TryParse(string s, out IpAddress ipAddress)
		{
			var match = IpMatch.Match(s);

			if (!match.Success)
			{
				ipAddress = new IpAddress();
				return false;
			}

			ipAddress = new IpAddress(byte.Parse(match.Groups[1].Value),
				byte.Parse(match.Groups[2].Value),
				byte.Parse(match.Groups[3].Value),
				byte.Parse(match.Groups[4].Value));

			return true;
		}

		public static IpAddress Any => new IpAddress(0, 0, 0, 0);
		public static IpAddress Broadcast => new IpAddress(255, 255, 255, 255);
		public static IpAddress None => new IpAddress(255, 255, 255, 255);
		public static IpAddress Loopback => new IpAddress(127, 0, 0, 1);


		public IpAddress(byte byte1, byte byte2, byte byte3, byte byte4)
			: this()
		{
			Byte1 = byte1;
			Byte2 = byte2;
			Byte3 = byte3;
			Byte4 = byte4;
		}

		public byte Byte1 { get; }
		public byte Byte2 { get; }
		public byte Byte3 { get; }
		public byte Byte4 { get; }

		public bool IsMulticast => Byte1 >= MulticastRangeLower && Byte1 <= MulticastRangeUpper;

		public bool IsLoopback => this.Equals(Loopback);

		public override string ToString()
		{
			return $"{Byte1}.{Byte2}.{Byte3}.{Byte4}";
		}

		public bool Equals(IpAddress other)
		{
			return Byte1 == other.Byte1 && Byte2 == other.Byte2 && Byte3 == other.Byte3 && Byte4 == other.Byte4;
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			return obj is IpAddress && Equals((IpAddress)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = Byte1.GetHashCode();
				hashCode = (hashCode * 397) ^ Byte2.GetHashCode();
				hashCode = (hashCode * 397) ^ Byte3.GetHashCode();
				hashCode = (hashCode * 397) ^ Byte4.GetHashCode();
				return hashCode;
			}
		}

		public static bool operator ==(IpAddress left, IpAddress right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(IpAddress left, IpAddress right)
		{
			return !left.Equals(right);
		}
	}
}
