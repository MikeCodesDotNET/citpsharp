using System;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Imp.CitpSharp.Sockets
{
	internal struct IpEndpoint : IEquatable<IpEndpoint>
	{
		private const int PortRangeMin = 0;
		private const int PortRangeMax = 65535;

		private static readonly Regex EndpointMatch =
			new Regex(@"^(([0-1]?\d{1,2})|(2[0-5]{2}))(\.(([0-1]?\d{1,2})|(2[0-5]{2}))){3}:\d{1,5}$");

		public static IpEndpoint Parse(string s)
		{
			if (!EndpointMatch.IsMatch(s))
				throw new FormatException("String is not a valid IP endpoint");

			var tokens = s.Split(':');

			return new IpEndpoint(IpAddress.Parse(tokens[0]), int.Parse(tokens[1]));
		}

		public static bool TryParse(string s, out IpEndpoint ipEndpoint)
		{
			if (!EndpointMatch.IsMatch(s))
			{
				ipEndpoint = new IpEndpoint();
				return false;
			}

			var tokens = s.Split(':');
			ipEndpoint = new IpEndpoint(IpAddress.Parse(tokens[0]), int.Parse(tokens[1]));

			return true;
		}

		public IpEndpoint(IpAddress ipAddress, int port)
			: this()
		{
			if (port < PortRangeMin || port > PortRangeMax)
				throw new ArgumentOutOfRangeException(nameof(port), "Invalid port number");

			Address = ipAddress;
			Port = port;
		}

		public IpAddress Address { get; }
		public int Port { get; }



		public override string ToString()
		{
			return $"{Address}:{Port}";
		}

		public bool Equals(IpEndpoint other)
		{
			return Address.Equals(other.Address) && Port == other.Port;
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			return obj is IpEndpoint && Equals((IpEndpoint)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (Address.GetHashCode() * 397) ^ Port;
			}
		}

		public static bool operator ==(IpEndpoint left, IpEndpoint right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(IpEndpoint left, IpEndpoint right)
		{
			return !left.Equals(right);
		}
	}
}