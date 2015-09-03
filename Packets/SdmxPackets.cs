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

using System.Collections.Generic;

namespace Imp.CitpSharp.Packets.Sdmx
{
	internal class CapabilitiesMessagePacket : CitpSdmxPacket
	{
		public CapabilitiesMessagePacket()
			: base(SdmxMessageType.CapabilitiesMessage) { }

		public List<SdmxCapability> Capabilities { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write((ushort)Capabilities.Count);
			foreach (var c in Capabilities)
				writer.Write((ushort)c);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			ushort capabilitiesCount = reader.ReadUInt16();
			Capabilities = new List<SdmxCapability>(capabilitiesCount);
			for (int i = 0; i < capabilitiesCount; ++i)
				Capabilities.Add((SdmxCapability)reader.ReadUInt16());
		}
	}



	internal class UniverseNameMessagePacket : CitpSdmxPacket
	{
		public UniverseNameMessagePacket()
			: base(SdmxMessageType.UniverseNameMessage) { }

		public byte UniverseIndex { get; set; }
		public string UniverseName { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(UniverseIndex);
			writer.Write(UniverseName, true);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			UniverseIndex = reader.ReadByte();
			UniverseName = reader.ReadString(true);
		}
	}



	internal class EncryptionIdentifierMessagePacket : CitpSdmxPacket
	{
		public EncryptionIdentifierMessagePacket()
			: base(SdmxMessageType.EncryptionIdentifierMessage) { }

		public string Identifier { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(Identifier, true);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			Identifier = reader.ReadString(true);
		}
	}



	internal class ChannelBlockMessagePacket : CitpSdmxPacket
	{
		public ChannelBlockMessagePacket()
			: base(SdmxMessageType.ChannelBlockMessage) { }

		public bool IsBlind { get; set; }
		public byte UniverseIndex { get; set; }
		public ushort FirstChannel { get; set; }
		public List<byte> ChannelLevels { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(IsBlind);
			writer.Write(UniverseIndex);
			writer.Write(FirstChannel);
			writer.Write((ushort)ChannelLevels.Count);
			foreach (byte c in ChannelLevels)
				writer.Write(c);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			IsBlind = reader.ReadBoolean();
			UniverseIndex = reader.ReadByte();
			FirstChannel = reader.ReadUInt16();

			ushort channelLevelsCount = reader.ReadUInt16();
			ChannelLevels = new List<byte>(channelLevelsCount);
			for (int i = 0; i < channelLevelsCount; ++i)
				ChannelLevels.Add(reader.ReadByte());
		}
	}



	internal class ChannelListMessagePacket : CitpSdmxPacket
	{
		public ChannelListMessagePacket()
			: base(SdmxMessageType.ChannelListMessage) { }

		public List<ChannelLevel> Levels { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write((ushort)Levels.Count);
			foreach (var l in Levels)
			{
				writer.Write(l.UniverseIndex);
				writer.Write(l.Channel);
				writer.Write(l.Level);
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			ushort levelsCount = reader.ReadUInt16();
			Levels = new List<ChannelLevel>(levelsCount);
			for (int i = 0; i < levelsCount; ++i)
			{
				Levels.Add(new ChannelLevel
				{
					UniverseIndex = reader.ReadByte(),
					Channel = reader.ReadUInt16(),
					Level = reader.ReadByte()
				});
			}
		}



		public class ChannelLevel
		{
			public byte UniverseIndex { get; set; }
			public ushort Channel { get; set; }
			public byte Level { get; set; }
		}
	}



	internal class SetExternalSourceMessagePacket : CitpSdmxPacket
	{
		public SetExternalSourceMessagePacket()
			: base(SdmxMessageType.SetExternalSourceMessage) { }

		public CitpDmxConnectionString ConnectionString { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(ConnectionString.ToUtf8ByteArray());
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			ConnectionString = CitpDmxConnectionString.FromString(reader.ReadString(true));
		}
	}



	internal class SetExternalUniverseSourceMessagePacket : CitpSdmxPacket
	{
		public SetExternalUniverseSourceMessagePacket()
			: base(SdmxMessageType.SetExternalUniverseSourceMessage) { }

		public byte UniverseIndex { get; set; }
		public CitpDmxConnectionString ConnectionString { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(UniverseIndex);
			writer.Write(ConnectionString.ToUtf8ByteArray());
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			UniverseIndex = reader.ReadByte();
			ConnectionString = CitpDmxConnectionString.FromString(reader.ReadString(true));
		}
	}
}