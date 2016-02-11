using System.Collections.Generic;

namespace Imp.CitpSharp.Packets
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

			ConnectionString = CitpDmxConnectionString.Parse(reader.ReadString(true));
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
			ConnectionString = CitpDmxConnectionString.Parse(reader.ReadString(true));
		}
	}
}