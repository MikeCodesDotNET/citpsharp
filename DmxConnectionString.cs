using System;
using System.Text;

namespace Imp.CitpSharp
{
	public struct DmxConnectionString
	{
		static public DmxConnectionString FromString(string s)
		{
			var dmxString = new DmxConnectionString();

			string[] tokens = s.Split('/');

			if (tokens.Length < 2 || tokens.Length > 4)
				throw new FormatException("Invalid DmxConnectionString");

			try
			{
				dmxString.Protocol = (DmxProtocol)Enum.Parse(typeof(DmxProtocol), tokens[0]);

				switch (dmxString.Protocol)
				{
					case DmxProtocol.ArtNet:
						dmxString.Net = Int32.Parse(tokens[1]);
						dmxString.Universe = Int32.Parse(tokens[2]);
						dmxString.Channel = Int32.Parse(tokens[3]);
						break;
					case DmxProtocol.BSRE1_31:
						dmxString.Universe = Int32.Parse(tokens[1]);
						dmxString.Channel = Int32.Parse(tokens[2]);
						break;
					case DmxProtocol.ETCNet2:
						dmxString.Channel = Int32.Parse(tokens[1]);
						break;
					case DmxProtocol.MANet:
						dmxString.Type = Int32.Parse(tokens[1]);
						dmxString.Universe = Int32.Parse(tokens[2]);
						dmxString.Channel = Int32.Parse(tokens[3]);
						break;
				}

				return dmxString;
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Invalid DmxConnectionString", ex);
			}
		}

		public enum DmxProtocol
		{
			ArtNet,
			BSRE1_31,
			ETCNet2,
			MANet
		}

		public DmxProtocol Protocol { get; set; }

		public int Net { get; set; }
		public int Type { get; set; }

		public int Universe { get; set; }
		public int Channel { get; set; }


		public override string ToString()
		{
			switch (Protocol)
			{
				case DmxProtocol.ArtNet:
					return String.Format("ArtNet/{0}/{1}/{2}", Net, Universe, Channel);
				case DmxProtocol.BSRE1_31:
					return String.Format("BSRE1.31/{0}/{1}", Universe, Channel);
				case DmxProtocol.ETCNet2:
					return String.Format("ETCNet2/{0}", Channel);
				case DmxProtocol.MANet:
					return String.Format("MANet/{0}/{1}/{2}", Type, Universe, Channel);
				default:
					return null;
			}
		}

		public byte[] ToUTF8ByteArray()
		{
			return Encoding.UTF8.GetBytes(ToString());
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			if (!(obj is DmxConnectionString))
				return false;

			 return Equals((DmxConnectionString)obj);
		}

		public bool Equals(DmxConnectionString obj)
		{
			switch (Protocol)
			{
				case DmxProtocol.ArtNet:
					return obj.Protocol == DmxProtocol.ArtNet && obj.Net == Net && obj.Universe == Universe && obj.Channel == Channel;
				case DmxProtocol.BSRE1_31:
					return obj.Protocol == DmxProtocol.BSRE1_31 && obj.Universe == Universe && obj.Channel == Channel;
				case DmxProtocol.ETCNet2:
					return obj.Protocol == DmxProtocol.ETCNet2 && obj.Channel == Channel;
				case DmxProtocol.MANet:
					return obj.Protocol == DmxProtocol.MANet && obj.Type == Type && obj.Universe == Universe && obj.Channel == Channel;
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
				case DmxProtocol.BSRE1_31:
					return Protocol.GetHashCode() ^ Universe.GetHashCode() ^ Channel.GetHashCode();
				case DmxProtocol.ETCNet2:
					return Protocol.GetHashCode() ^ Channel.GetHashCode();
				case DmxProtocol.MANet:
					return Protocol.GetHashCode() ^ Type.GetHashCode() ^ Universe.GetHashCode() ^ Channel.GetHashCode();
				default:
					return Protocol.GetHashCode();
			}
		}

		public static bool operator ==(DmxConnectionString a, DmxConnectionString b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(DmxConnectionString a, DmxConnectionString b)
		{
			return !(a == b);
		}

	}
}
