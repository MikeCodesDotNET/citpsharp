using System;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	/// <summary>
	/// Contains DMX patch information, serializes to a string
	/// </summary>
	[PublicAPI]
	public struct CitpDmxConnectionString : IEquatable<CitpDmxConnectionString>
	{
		public const string ProtocolNameArtNet = "ArtNet";
		public const string ProtocolNameBsre131 = "BSRE1.31";
		public const string ProtocolNameEtcNet2 = "ETCNet2";
		public const string ProtocolNameMaNet = "MANet";

		static Regex _dmxConnectionStringRegex = new Regex(@"^(?:(?<p>ArtNet)\/(?<n>\d+)\/(?<u>\d+)\/(?<c>\d+)|(?<p>BSRE1\.31)\/(?<u>\d+)\/(?<c>\d+)|(?<p>ETCNet2)\/(?<c>\d+)|(?<p>MANet)\/(?<t>\d+)\/(?<u>\d+)\/(?<c>\d+))(?:\/PersonalityID\/(?<pId>{[0-9A-Fa-f]{8}-?[0-9A-Fa-f]{4}-?[0-9A-Fa-f]{4}-?[0-9A-Fa-f]{4}-?[0-9A-Fa-f]{12}}))?$", 
			RegexOptions.CultureInvariant);

		

		public static CitpDmxConnectionString Parse([NotNull] string s)
		{
			if (s == null)
				throw new ArgumentNullException(nameof(s));

			var result = TryParse(s);

			if (!result.HasValue)
				throw new FormatException("String is not a valid CITP DmxConnectionString");

			return result.Value;
		}

		public static bool TryParse([CanBeNull] string s, out CitpDmxConnectionString value)
		{
			value = default(CitpDmxConnectionString);

			var result = TryParse(s);

			if (!result.HasValue)
				return false;

			value = result.Value;
			return true;
		}

		public static CitpDmxConnectionString? TryParse([CanBeNull] string s)
		{
			if (s == null)
				return null;

			var match = _dmxConnectionStringRegex.Match(s);

			if (!match.Success)
				return null;

			const int indexProtocol = 1;
			const int indexNet = 2;
			const int indexUniverse = 3;
			const int indexChannel = 4;
			const int indexType = 5;
			const int indexPersonalityId = 6;

			Guid? personalityId = null;

			if (match.Groups[indexPersonalityId].Success)
				personalityId = Guid.Parse(match.Groups[indexPersonalityId].Captures[0].Value);

			switch (match.Groups[indexProtocol].Captures[0].Value)
			{
				case ProtocolNameArtNet:
					return FromArtNet(int.Parse(match.Groups[indexNet].Captures[0].Value),
						int.Parse(match.Groups[indexUniverse].Captures[0].Value),
						int.Parse(match.Groups[indexChannel].Captures[0].Value),
						personalityId);

				case ProtocolNameBsre131:
					return FromBsre131(int.Parse(match.Groups[indexUniverse].Captures[0].Value),
						int.Parse(match.Groups[indexChannel].Captures[0].Value),
						personalityId);

				case ProtocolNameEtcNet2:
					return FromEtcNet2(int.Parse(match.Groups[indexChannel].Captures[0].Value),
						personalityId);

				case ProtocolNameMaNet:
					return FromMaNet(int.Parse(match.Groups[indexType].Captures[0].Value),
						int.Parse(match.Groups[indexUniverse].Captures[0].Value),
						int.Parse(match.Groups[indexChannel].Captures[0].Value),
						personalityId);

				default:
					return null;
			}
		}



		public static CitpDmxConnectionString FromArtNet(int net, int universe, int channel, Guid? personalityId = null)
		{
			return new CitpDmxConnectionString(DmxProtocol.ArtNet, channel, net, universe, null, personalityId);
		}

		public static CitpDmxConnectionString FromBsre131(int universe, int channel, Guid? personalityId = null)
		{
			return new CitpDmxConnectionString(DmxProtocol.Bsre131, channel, null, universe, null, personalityId);
		}

		public static CitpDmxConnectionString FromEtcNet2(int channel, Guid? personalityId = null)
		{
			return new CitpDmxConnectionString(DmxProtocol.EtcNet2, channel, null, null, null, personalityId);
		}

		public static CitpDmxConnectionString FromMaNet(int type, int universe, int channel, Guid? personalityId = null)
		{
			return new CitpDmxConnectionString(DmxProtocol.MaNet, channel, null, universe, type, personalityId);
		}



		CitpDmxConnectionString(DmxProtocol protocol, int channel, int? net, int? universe, int? type, Guid? personalityId)
			: this()
		{
			Protocol = protocol;
			Net = net;
			Universe = universe;
			Channel = channel;
			Type = type;
			PersonalityId = personalityId;
		}



		public enum DmxProtocol
		{
			None = 0,
			ArtNet,
			Bsre131,
			EtcNet2,
			MaNet
		}



		public DmxProtocol Protocol { get; }

		public int? Net { get; }
		public int? Type { get; }

		public int? Universe { get; }
		public int Channel { get; }

		public Guid? PersonalityId { get; }


		public override string ToString()
		{
			string personalityId = PersonalityId.HasValue ? "/PersonalityID/" + PersonalityId.Value.ToString("B") : string.Empty;

			switch (Protocol)
			{
				case DmxProtocol.ArtNet:
					return $"{ProtocolNameArtNet}/{Net}/{Universe}/{Channel}{personalityId}";
				case DmxProtocol.Bsre131:
					return $"{ProtocolNameBsre131}/{Universe}/{Channel}{personalityId}";
				case DmxProtocol.EtcNet2:
					return $"{ProtocolNameEtcNet2}/{Channel}{personalityId}";
				case DmxProtocol.MaNet:
					return $"{ProtocolNameMaNet}/{Type}/{Universe}/{Channel}{personalityId}";
				default:
					return string.Empty;
			}
		}

		public bool Equals(CitpDmxConnectionString other)
		{
			return Protocol == other.Protocol && Net == other.Net && Type == other.Type && Universe == other.Universe && Channel == other.Channel && PersonalityId.Equals(other.PersonalityId);
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
				hashCode = (hashCode * 397) ^ Net.GetHashCode();
				hashCode = (hashCode * 397) ^ Type.GetHashCode();
				hashCode = (hashCode * 397) ^ Universe.GetHashCode();
				hashCode = (hashCode * 397) ^ Channel;
				hashCode = (hashCode * 397) ^ PersonalityId.GetHashCode();
				return hashCode;
			}
		}

		public static bool operator ==(CitpDmxConnectionString left, CitpDmxConnectionString right) => left.Equals(right);

		public static bool operator !=(CitpDmxConnectionString left, CitpDmxConnectionString right) => !left.Equals(right);

		public static implicit operator string(CitpDmxConnectionString value) => value.ToString();
	}
}