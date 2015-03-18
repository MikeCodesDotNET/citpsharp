using System;

namespace Imp.CitpSharp.Packets.Pinf
{
	public class PeerNameMessagePacket : CitpPinfPacket
	{
		public PeerNameMessagePacket()
			: base(PinfMessageType.PeerNameMessage)
		{

		}

		public string Name { get; set; }

		protected override void serializeToStream(CitpBinaryWriter writer)
		{
			base.serializeToStream(writer);

			writer.Write(Name, true);
		}

		protected override void deserializeFromStream(CitpBinaryReader reader)
		{
			base.deserializeFromStream(reader);

			Name = reader.ReadString(true); 
		}
	}

	public class PeerLocationMessagePacket : CitpPinfPacket
	{
		public PeerLocationMessagePacket()
			: base(PinfMessageType.PeerLocationMessage)
		{

		}

		public bool IsListeningForTcpConnection { get; set; }

		public ushort ListeningTcpPort { get; set; }
		public CitpPeerType Type { get; set; }
		public string Name { get; set; }
		public string State { get; set; }

		

		protected override void serializeToStream(CitpBinaryWriter writer)
		{
			base.serializeToStream(writer);

			if (IsListeningForTcpConnection)
				writer.Write(ListeningTcpPort);
			else
				writer.Write((ushort)0x0000);

			writer.Write(Type.ToString(), true);
			writer.Write(Name, true);
			writer.Write(State, true);
		}

		protected override void deserializeFromStream(CitpBinaryReader reader)
		{
			base.deserializeFromStream(reader);

			ListeningTcpPort = reader.ReadUInt16();

			if (ListeningTcpPort == 0)
				IsListeningForTcpConnection = false;

			CitpPeerType peerType;
			if (Enum.TryParse<CitpPeerType>(reader.ReadString(true), out peerType))
				Type = peerType;
			else
				Type = CitpPeerType.Unknown;

			Name = reader.ReadString(true);
			State = reader.ReadString(true);
		}
	}
}
