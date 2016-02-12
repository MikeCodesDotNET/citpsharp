using System.Collections.Generic;

namespace Imp.CitpSharp.Packets.Sdmx
{
	internal class ChannelListMessagePacket : SdmxPacket
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
}