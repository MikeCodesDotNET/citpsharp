using System;
using System.Text;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	[PublicAPI]
	public struct CitpDmxConnectionString : IEquatable<CitpDmxConnectionString>
	{
		public static CitpDmxConnectionString Parse([NotNull] string s)
		{
			if (s == null)
				throw new ArgumentNullException(nameof(s));

			var tokens = s.Split('/');

			if (tokens.Length < 2 || tokens.Length > 4)
				throw new FormatException("Invalid DmxConnectionString");

			try
			{
				switch ((DmxProtocol)Enum.Parse(typeof(DmxProtocol), tokens[0]))
				{
					case DmxProtocol.ArtNet:
						return FromArtNet(int.Parse(tokens[1]), int.Parse(tokens[2]), int.Parse(tokens[3]));

					case DmxProtocol.Bsre131:
						return FromBsre131(int.Parse(tokens[1]), int.Parse(tokens[2]));

					case DmxProtocol.EtcNet2:
						return FromEtcNet2(int.Parse(tokens[1]));

					case DmxProtocol.MaNet:
						return FromMaNet(int.Parse(tokens[1]), int.Parse(tokens[2]), int.Parse(tokens[3]));

					default:
						throw new FormatException("Invalid DmxConnectionString");
				}
			}
			catch (FormatException ex)
			{
				throw new InvalidOperationException("Invalid DmxConnectionString", ex);
			}
		}

		public static CitpDmxConnectionString FromArtNet(int net, int universe, int channel)
		{
			return new CitpDmxConnectionString(DmxProtocol.ArtNet, net, universe, channel, 0);
		}

		public static CitpDmxConnectionString FromBsre131(int universe, int channel)
		{
			return new CitpDmxConnectionString(DmxProtocol.Bsre131, 0, universe, channel, 0);
		}

		public static CitpDmxConnectionString FromEtcNet2(int channel)
		{
			return new CitpDmxConnectionString(DmxProtocol.Bsre131, 0, 0, channel, 0);
		}

		public static CitpDmxConnectionString FromMaNet(int type, int universe, int channel)
		{
			return new CitpDmxConnectionString(DmxProtocol.Bsre131, 0, universe, channel, type);
		}



		CitpDmxConnectionString(DmxProtocol protocol, int net, int universe, int channel, int type)
			: this()
		{
			Protocol = protocol;
			Net = net;
			Universe = universe;
			Channel = channel;
			Type = type;
		}



		public enum DmxProtocol
		{
			ArtNet,
			Bsre131,
			EtcNet2,
			MaNet
		}



		public DmxProtocol Protocol { get; }

		public int Net { get; }
		public int Type { get; }

		public int Universe { get; }
		public int Channel { get; }


		public override string ToString()
		{
			switch (Protocol)
			{
				case DmxProtocol.ArtNet:
					return $"ArtNet/{Net}/{Universe}/{Channel}";
				case DmxProtocol.Bsre131:
					return $"BSRE1.31/{Universe}/{Channel}";
				case DmxProtocol.EtcNet2:
					return $"ETCNet2/{Channel}";
				case DmxProtocol.MaNet:
					return $"MANet/{Type}/{Universe}/{Channel}";
				default:
					return string.Empty;
			}
		}

		public byte[] ToUtf8ByteArray()
		{
			return Encoding.UTF8.GetBytes(ToString());
		}

		public bool Equals(CitpDmxConnectionString other)
		{
			return Protocol == other.Protocol && Net == other.Net && Type == other.Type && Universe == other.Universe
			       && Channel == other.Channel;
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			return obj is CitpDmxConnectionString && Equals((CitpDmxConnectionString)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (int)Protocol;
				hashCode = (hashCode * 397) ^ Net;
				hashCode = (hashCode * 397) ^ Type;
				hashCode = (hashCode * 397) ^ Universe;
				hashCode = (hashCode * 397) ^ Channel;
				return hashCode;
			}
		}

		public static bool operator ==(CitpDmxConnectionString left, CitpDmxConnectionString right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(CitpDmxConnectionString left, CitpDmxConnectionString right)
		{
			return !left.Equals(right);
		}
	}
}