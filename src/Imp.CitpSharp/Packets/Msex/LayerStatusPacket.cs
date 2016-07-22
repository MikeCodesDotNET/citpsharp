using System;
using System.Collections.Immutable;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class LayerStatusPacket : MsexPacket
	{
		public LayerStatusPacket()
			: base(MsexMessageType.LayerStatusMessage) { }

		public ImmutableList<LayerStatus> LayerStatuses { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			switch (Version)
			{
				case MsexVersion.Version1_0:
				case MsexVersion.Version1_1:
					writer.Write(LayerStatuses, TypeCode.Byte, l =>
					{
						writer.Write(l.LayerNumber);
						writer.Write(l.PhysicalOutput);
						writer.Write(l.MediaLibraryNumber);
						writer.Write(l.MediaNumber);
						writer.Write(l.MediaName);
						writer.Write(l.MediaPosition);
						writer.Write(l.MediaLength);
						writer.Write(l.MediaFps);
						writer.Write((uint)l.LayerStatusFlags);
					});
					break;

				case MsexVersion.Version1_2:
					writer.Write(LayerStatuses, TypeCode.Byte, l =>
					{
						writer.Write(l.LayerNumber);
						writer.Write(l.PhysicalOutput);
						writer.Write((byte)l.MediaLibraryType);

						if (!l.MediaLibraryId.HasValue)
							throw new InvalidOperationException("MediaLibraryId has no value. Required for MSEX V1.2");

						writer.Write(l.MediaLibraryId.Value);
						writer.Write(l.MediaNumber);
						writer.Write(l.MediaName);
						writer.Write(l.MediaPosition);
						writer.Write(l.MediaLength);
						writer.Write(l.MediaFps);
						writer.Write((uint)l.LayerStatusFlags);
					});
					break;
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			switch (Version)
			{
				case MsexVersion.Version1_0:
				case MsexVersion.Version1_1:
				
					LayerStatuses = reader.ReadCollection(TypeCode.Byte, () => new LayerStatus
					{
						LayerNumber = reader.ReadByte(),
						PhysicalOutput = reader.ReadByte(),
						MediaLibraryNumber = reader.ReadByte(),
						MediaNumber = reader.ReadByte(),
						MediaName = reader.ReadString(),
						MediaPosition = reader.ReadUInt32(),
						MediaLength = reader.ReadUInt32(),
						MediaFps = reader.ReadByte(),
						LayerStatusFlags = (MsexLayerStatusFlags)reader.ReadUInt32()

					}).ToImmutableList();
				
					break;

				case MsexVersion.Version1_2:
				
					LayerStatuses = reader.ReadCollection(TypeCode.Byte, () => new LayerStatus
					{
						LayerNumber = reader.ReadByte(),
						PhysicalOutput = reader.ReadByte(),
						MediaLibraryType = (MsexLibraryType)reader.ReadByte(),
						MediaLibraryId = reader.ReadLibraryId(),
						MediaNumber = reader.ReadByte(),
						MediaName = reader.ReadString(),
						MediaPosition = reader.ReadUInt32(),
						MediaLength = reader.ReadUInt32(),
						MediaFps = reader.ReadByte(),
						LayerStatusFlags = (MsexLayerStatusFlags)reader.ReadUInt32()

					}).ToImmutableList();
				
					break;
			}
		}


		internal class LayerStatus 
		{
			public byte LayerNumber { get; set; }
			public byte PhysicalOutput { get; set; }

			public byte MediaLibraryNumber { get; set; }

			public MsexLibraryType MediaLibraryType { get; set; }
			public MsexLibraryId? MediaLibraryId { get; set; }

			public byte MediaNumber { get; set; }
			public string MediaName { get; set; }
			public uint MediaPosition { get; set; }
			public uint MediaLength { get; set; }
			public byte MediaFps { get; set; }

			public MsexLayerStatusFlags LayerStatusFlags { get; set; }
		}
	}
}