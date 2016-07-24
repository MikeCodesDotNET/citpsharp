using System;

namespace Imp.CitpSharp.Packets.Pinf
{
	internal class PeerLocationPacket : PinfPacket
	{
		public PeerLocationPacket()
			: base(PinfMessageType.PeerLocationMessage) { }

	    public PeerLocationPacket(bool isListeningForTcpConnection, ushort listeningTcpPort, CitpPeerType peerType, string name, string state)
	        : base(PinfMessageType.PeerLocationMessage)
	    {
	        IsListeningForTcpConnection = isListeningForTcpConnection;
	        ListeningTcpPort = listeningTcpPort;
	        PeerType = peerType;
	        Name = name;
	        State = state;
	    }

		public bool IsListeningForTcpConnection { get; private set; }

		public ushort ListeningTcpPort { get; private set; }
		public CitpPeerType PeerType { get; private set; }
		public string Name { get; private set; }
		public string State { get; private set; }



		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			if (IsListeningForTcpConnection)
				writer.Write(ListeningTcpPort);
			else
				writer.Write((ushort)0x0000);

			writer.Write(PeerType.ToString(), true);
			writer.Write(Name, true);
			writer.Write(State, true);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			ListeningTcpPort = reader.ReadUInt16();

			if (ListeningTcpPort == 0)
				IsListeningForTcpConnection = false;

			CitpPeerType citpPeerType;
			PeerType = Enum.TryParse(reader.ReadString(true), out citpPeerType) ? citpPeerType : CitpPeerType.Unknown;

			Name = reader.ReadString(true);
			State = reader.ReadString(true);
		}
	}
}