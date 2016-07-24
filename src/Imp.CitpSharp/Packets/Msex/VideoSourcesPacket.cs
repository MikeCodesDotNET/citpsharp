using System;
using System.Collections.Immutable;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class VideoSourcesPacket : MsexPacket
	{
		public VideoSourcesPacket()
			: base(MsexMessageType.VideoSourcesMessage) { }

		public ImmutableSortedSet<VideoSourceInformation> Sources { get; set; }


		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write((ushort)Sources.Count);
			foreach (var s in Sources)
			{
				writer.Write(s.SourceIdentifier);
				writer.Write(s.SourceName);

				if (s.PhysicalOutput.HasValue)
					writer.Write(s.PhysicalOutput.Value);
				else
					writer.Write((byte)0xFF);

				if (s.LayerNumber.HasValue)
					writer.Write(s.LayerNumber.Value);
				else
					writer.Write((byte)0xFF);

				writer.Write((ushort)s.Flags);

				writer.Write(s.Width);
				writer.Write(s.Height);
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			Sources = reader.ReadCollection(TypeCode.UInt16, () =>
			{
				ushort sourceIdentifier = reader.ReadUInt16();
				string sourceName = reader.ReadString();
				byte physicalOutput = reader.ReadByte();
				byte layerNumber = reader.ReadByte();
				var flags = (MsexVideoSourcesFlags)reader.ReadUInt16();
				ushort width = reader.ReadUInt16();
				ushort height = reader.ReadUInt16();

				return new VideoSourceInformation(sourceIdentifier, sourceName, flags, width, height, physicalOutput,
					layerNumber);

			}).ToImmutableSortedSet();
		}
	}
}