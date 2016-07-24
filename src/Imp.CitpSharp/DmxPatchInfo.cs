using System;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	/// <summary>
	///     Represents DMX patching information for an MSEX layer
	/// </summary>
	[PublicAPI]
	public struct DmxPatchInfo : IEquatable<DmxPatchInfo>
	{
		public const string ProtocolNameArtNet = "ArtNet";
		public const string ProtocolNameBsre131 = "BSRE1.31";
		public const string ProtocolNameEtcNet2 = "ETCNet2";
		public const string ProtocolNameMaNet = "MANet";

		private static Regex _dmxConnectionStringRegex =
			new Regex(
				@"^(?:(?<p>ArtNet)\/(?<n>\d+)\/(?<u>\d+)\/(?<c>\d+)|(?<p>BSRE1\.31)\/(?<u>\d+)\/(?<c>\d+)|(?<p>ETCNet2)\/(?<c>\d+)|(?<p>MANet)\/(?<t>\d+)\/(?<u>\d+)\/(?<c>\d+))(?:\/PersonalityID\/(?<pId>{[0-9A-Fa-f]{8}-?[0-9A-Fa-f]{4}-?[0-9A-Fa-f]{4}-?[0-9A-Fa-f]{4}-?[0-9A-Fa-f]{12}}))?$",
				RegexOptions.CultureInvariant);


	    /// <summary>
	    ///     Parse <see cref="DmxPatchInfo"/> from it's string-formatted representation.
	    /// </summary>
	    /// <param name="s">String formatted <see cref="DmxPatchInfo"/></param>
	    /// <returns><see cref="DmxPatchInfo"/> represented by string</returns>
	    /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null" />.</exception>
	    /// <exception cref="FormatException">String is not a valid <see cref="DmxPatchInfo"/> string</exception>
	    public static DmxPatchInfo Parse([NotNull] string s)
		{
			if (s == null)
				throw new ArgumentNullException(nameof(s));

			var result = TryParse(s);

			if (!result.HasValue)
				throw new FormatException("String is not a valid DmxPatchInfo string");

			return result.Value;
		}

        /// <summary>
        ///     Try and parse <see cref="DmxPatchInfo"/> from it's string-formatted representation.
        /// </summary>
        /// <param name="s">String formatted <see cref="DmxPatchInfo"/></param>
        /// <param name="value"><see cref="DmxPatchInfo"/> represented by string, or default value if parse fails</param>
        /// <returns>True if parse was successful, otherwise false</returns>
		public static bool TryParse([CanBeNull] string s, out DmxPatchInfo value)
		{
			value = default(DmxPatchInfo);

			var result = TryParse(s);

			if (!result.HasValue)
				return false;

			value = result.Value;
			return true;
		}

        /// <summary>
        ///     Try and parse <see cref="DmxPatchInfo"/> from it's string-formatted representation.
        /// </summary>
        /// <param name="s">String formatted <see cref="DmxPatchInfo"/></param>
        /// <returns><see cref="DmxPatchInfo"/> represented by string, or null value if parse fails</returns>
        [CanBeNull]
		public static DmxPatchInfo? TryParse([CanBeNull] string s)
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


        /// <summary>
        ///     Creates <see cref="DmxPatchInfo"/> for a layer patched to Art-Net.
        /// </summary>
        /// <param name="net">Art-Net net value (Use 0 for Art-Net 2)</param>
        /// <param name="subNetAndUniverse">Art-Net combined sub-net and universe value</param>
        /// <param name="channel">Art-Net channel value</param>
        /// <param name="personalityId">Patched personality identifier</param>
        /// <returns><see cref="DmxPatchInfo"/> for Art-Net values</returns>
		public static DmxPatchInfo FromArtNet(int net, int subNetAndUniverse, int channel, Guid? personalityId = null)
		{
			return new DmxPatchInfo(DmxProtocol.ArtNet, channel, net, subNetAndUniverse, null, personalityId);
		}

        /// <summary>
        ///     Creates <see cref="DmxPatchInfo"/> for a layer patched to Art-Net.
        /// </summary>
        /// <param name="net">Art-Net net value (Use 0 for Art-Net 2)</param>
        /// <param name="subnet">Art-Net sub-net value</param>
        /// <param name="universe">Art-Net universe value</param>
        /// <param name="channel">Art-Net channel value</param>
        /// <param name="personalityId">Patched personality identifier</param>
        /// <returns><see cref="DmxPatchInfo"/> for Art-Net values</returns>
        public static DmxPatchInfo FromArtNet(int net, int subnet, int universe, int channel, Guid? personalityId = null)
        {
            return new DmxPatchInfo(DmxProtocol.ArtNet, channel, net, (subnet << 4) + universe, null, personalityId);
        }

        /// <summary>
        ///     Creates <see cref="DmxPatchInfo"/> for a layer patched to E131 (sACN)
        /// </summary>
        /// <param name="universe">E1.31 universe value</param>
        /// <param name="channel">E1.31 channel value</param>
        /// <param name="personalityId">Patched personality identifier</param>
        /// <returns><see cref="DmxPatchInfo"/> for E1.31 values</returns>
        public static DmxPatchInfo FromBsre131(int universe, int channel, Guid? personalityId = null)
		{
			return new DmxPatchInfo(DmxProtocol.Bsre131, channel, null, universe, null, personalityId);
		}

        /// <summary>
        ///     Creates <see cref="DmxPatchInfo"/> for a layer patched to ETC-Net
        /// </summary>
        /// <param name="channel">ETC-Net channel value</param>
        /// <param name="personalityId">Patched personality identifier</param>
        /// <returns><see cref="DmxPatchInfo"/> for ETC-Net values</returns>
		public static DmxPatchInfo FromEtcNet2(int channel, Guid? personalityId = null)
		{
			return new DmxPatchInfo(DmxProtocol.EtcNet2, channel, null, null, null, personalityId);
		}

        /// <summary>
        ///     Creates <see cref="DmxPatchInfo"/> for a layer patched to MA-Net
        /// </summary>
        /// <param name="type">MA-Net type value</param>
        /// <param name="universe">MA-Net universe value</param>
        /// <param name="channel">MA-Net channel value</param>
        /// <param name="personalityId">Patched personality identifier</param>
        /// <returns><see cref="DmxPatchInfo"/> for MA-Net values</returns>
		public static DmxPatchInfo FromMaNet(int type, int universe, int channel, Guid? personalityId = null)
		{
			return new DmxPatchInfo(DmxProtocol.MaNet, channel, null, universe, type, personalityId);
		}



		private DmxPatchInfo(DmxProtocol protocol, int channel, int? net, int? universe, int? type,
			Guid? personalityId)
			: this()
		{
			Protocol = protocol;
			Net = net;
			Universe = universe;
			Channel = channel;
			Type = type;
			PersonalityId = personalityId;
		}


        /// <summary>
        ///     DMX IP protocol type
        /// </summary>
		[PublicAPI]
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


	    [NotNull]
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

		public bool Equals(DmxPatchInfo other)
		{
			return Protocol == other.Protocol && Net == other.Net && Type == other.Type && Universe == other.Universe
			       && Channel == other.Channel && PersonalityId.Equals(other.PersonalityId);
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			return obj is DmxPatchInfo && Equals((DmxPatchInfo)obj);
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

		public static bool operator ==(DmxPatchInfo left, DmxPatchInfo right) => left.Equals(right);

		public static bool operator !=(DmxPatchInfo left, DmxPatchInfo right) => !left.Equals(right);

		public static implicit operator string(DmxPatchInfo value) => value.ToString();
	}
}