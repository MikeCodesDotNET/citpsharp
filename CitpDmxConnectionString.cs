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
using System.Text;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	public struct CitpDmxConnectionString : IEquatable<CitpDmxConnectionString>
	{
		public static CitpDmxConnectionString FromString(string s)
		{
			var dmxString = new CitpDmxConnectionString();

			var tokens = s.Split('/');

			if (tokens.Length < 2 || tokens.Length > 4)
				throw new FormatException("Invalid DmxConnectionString");

			try
			{
				dmxString.Protocol = (DmxProtocol)Enum.Parse(typeof(DmxProtocol), tokens[0]);

				switch (dmxString.Protocol)
				{
					case DmxProtocol.ArtNet:
						dmxString.Net = int.Parse(tokens[1]);
						dmxString.Universe = int.Parse(tokens[2]);
						dmxString.Channel = int.Parse(tokens[3]);
						break;
					case DmxProtocol.Bsre131:
						dmxString.Universe = int.Parse(tokens[1]);
						dmxString.Channel = int.Parse(tokens[2]);
						break;
					case DmxProtocol.EtcNet2:
						dmxString.Channel = int.Parse(tokens[1]);
						break;
					case DmxProtocol.MaNet:
						dmxString.Type = int.Parse(tokens[1]);
						dmxString.Universe = int.Parse(tokens[2]);
						dmxString.Channel = int.Parse(tokens[3]);
						break;
				}

				return dmxString;
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Invalid DmxConnectionString", ex);
			}
		}

		public static CitpDmxConnectionString FromArtNet(int net, int universe, int channel)
		{
			return new CitpDmxConnectionString(DmxProtocol.ArtNet, net, universe, channel, 0);
		}

		public static CitpDmxConnectionString FromBsre131( int universe, int channel)
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



		public DmxProtocol Protocol { get; private set; }

		public int Net { get; private set; }
		public int Type { get; private set; }

		public int Universe { get; private set; }
		public int Channel { get; private set; }


		public override string ToString()
		{
			switch (Protocol)
			{
				case DmxProtocol.ArtNet:
					return string.Format("ArtNet/{0}/{1}/{2}", Net, Universe, Channel);
				case DmxProtocol.Bsre131:
					return string.Format("BSRE1.31/{0}/{1}", Universe, Channel);
				case DmxProtocol.EtcNet2:
					return string.Format("ETCNet2/{0}", Channel);
				case DmxProtocol.MaNet:
					return string.Format("MANet/{0}/{1}/{2}", Type, Universe, Channel);
				default:
					return string.Empty;
			}
		}

		public byte[] ToUtf8ByteArray()
		{
			return Encoding.UTF8.GetBytes(ToString());
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (!(obj is CitpDmxConnectionString))
				return false;

			return Equals((CitpDmxConnectionString)obj);
		}

		public bool Equals(CitpDmxConnectionString obj)
		{
			switch (Protocol)
			{
				case DmxProtocol.ArtNet:
					return obj.Protocol == DmxProtocol.ArtNet && obj.Net == Net && obj.Universe == Universe && obj.Channel == Channel;
				case DmxProtocol.Bsre131:
					return obj.Protocol == DmxProtocol.Bsre131 && obj.Universe == Universe && obj.Channel == Channel;
				case DmxProtocol.EtcNet2:
					return obj.Protocol == DmxProtocol.EtcNet2 && obj.Channel == Channel;
				case DmxProtocol.MaNet:
					return obj.Protocol == DmxProtocol.MaNet && obj.Type == Type && obj.Universe == Universe && obj.Channel == Channel;
				default:
					return false;
			}
		}

		public override int GetHashCode()
		{
			switch (Protocol)
			{
				case DmxProtocol.ArtNet:
					return Protocol.GetHashCode() ^ Net.GetHashCode() ^ Universe.GetHashCode() ^ Channel.GetHashCode();
				case DmxProtocol.Bsre131:
					return Protocol.GetHashCode() ^ Universe.GetHashCode() ^ Channel.GetHashCode();
				case DmxProtocol.EtcNet2:
					return Protocol.GetHashCode() ^ Channel.GetHashCode();
				case DmxProtocol.MaNet:
					return Protocol.GetHashCode() ^ Type.GetHashCode() ^ Universe.GetHashCode() ^ Channel.GetHashCode();
				default:
					return Protocol.GetHashCode();
			}
		}

		public static bool operator ==(CitpDmxConnectionString a, CitpDmxConnectionString b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(CitpDmxConnectionString a, CitpDmxConnectionString b)
		{
			return !(a == b);
		}
	}
}