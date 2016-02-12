using System.Collections.Generic;

namespace Imp.CitpSharp.Packets.Sdmx
{
	internal class ChannelBlockMessagePacket : SdmxPacket
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
}