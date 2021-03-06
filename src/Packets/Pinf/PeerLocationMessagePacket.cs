﻿using System;

namespace Imp.CitpSharp.Packets.Pinf
{
	internal class PeerLocationMessagePacket : PinfPacket
	{
		public PeerLocationMessagePacket()
			: base(PinfMessageType.PeerLocationMessage) { }

		public bool IsListeningForTcpConnection { get; set; }

		public ushort ListeningTcpPort { get; set; }
		public CitpPeerType Type { get; set; }
		public string Name { get; set; }
		public string State { get; set; }



		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			if (IsListeningForTcpConnection)
				writer.Write(ListeningTcpPort);
			else
				writer.Write((ushort)0x0000);

			writer.Write(Type.ToString(), true);
			writer.Write(Name, true);
			writer.Write(State, true);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			ListeningTcpPort = reader.ReadUInt16();

			if (ListeningTcpPort == 0)
				IsListeningForTcpConnection = false;

			CitpPeerType peerType;
			Type = Enum.TryParse(reader.ReadString(true), out peerType) ? peerType : CitpPeerType.Unknown;

			Name = reader.ReadString(true);
			State = reader.ReadString(true);
		}
	}
}