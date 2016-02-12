using System;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class ElementLibraryThumbnailMessagePacket : MsexPacket
	{
		public ElementLibraryThumbnailMessagePacket()
			: base(MsexMessageType.ElementLibraryThumbnailMessage) { }

		public MsexLibraryType LibraryType { get; set; }
		public byte LibraryNumber { get; set; }
		public MsexLibraryId? LibraryId { get; set; }

		public MsexImageFormat ThumbnailFormat { get; set; }

		public ushort ThumbnailWidth { get; set; }
		public ushort ThumbnailHeight { get; set; }
		public byte[] ThumbnailBuffer { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			switch (Version)
			{
				case MsexVersion.Version1_0:
					writer.Write((byte)LibraryType);
					writer.Write(LibraryNumber);

					writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);

					writer.Write(ThumbnailWidth);
					writer.Write(ThumbnailHeight);
					writer.Write((ushort)ThumbnailBuffer.Length);
					break;

				case MsexVersion.Version1_1:
				case MsexVersion.Version1_2:
					writer.Write((byte)LibraryType);

					if (!LibraryId.HasValue)
						throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.1 & V1.2");

					writer.Write(LibraryId.Value.ToByteArray());

					writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);

					writer.Write(ThumbnailWidth);
					writer.Write(ThumbnailHeight);
					writer.Write((ushort)ThumbnailBuffer.Length);
					break;
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			switch (Version)
			{
				case MsexVersion.Version1_0:
				{
					LibraryType = (MsexLibraryType)reader.ReadByte();
					LibraryNumber = reader.ReadByte();

					ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());

					ThumbnailWidth = reader.ReadUInt16();
					ThumbnailHeight = reader.ReadUInt16();

					int thumbnailBufferLength = reader.ReadUInt16();
					ThumbnailBuffer = reader.ReadBytes(thumbnailBufferLength);
				}
					break;

				case MsexVersion.Version1_1:
				case MsexVersion.Version1_2:
				{
					LibraryType = (MsexLibraryType)reader.ReadByte();
					LibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));

					ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());

					ThumbnailWidth = reader.ReadUInt16();
					ThumbnailHeight = reader.ReadUInt16();

					int thumbnailBufferLength = reader.ReadUInt16();
					ThumbnailBuffer = reader.ReadBytes(thumbnailBufferLength);
				}
					break;
			}
		}
	}
}