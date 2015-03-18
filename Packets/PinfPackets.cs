﻿//  This file is part of CitpSharp.
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
