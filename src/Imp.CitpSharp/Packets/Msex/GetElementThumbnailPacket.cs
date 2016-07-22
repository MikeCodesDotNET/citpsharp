using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class GetElementThumbnailPacket : MsexPacket
	{
		public GetElementThumbnailPacket()
			: base(MsexMessageType.GetElementThumbnailMessage) { }

		public MsexImageFormat ThumbnailFormat { get; set; }

		public int ThumbnailWidth { get; set; }
		public int ThumbnailHeight { get; set; }
		public MsexThumbnailFlags ThumbnailFlags { get; set; }

		public MsexLibraryType LibraryType { get; set; }
		public byte LibraryNumber { get; set; }
		public MsexLibraryId? LibraryId { get; set; }

		public bool ShouldRequestAllThumbnails { get; set; }

		public ImmutableSortedSet<byte> RequestedElementNumbers { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			switch (Version)
			{
				case MsexVersion.Version1_0:
					writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);
					writer.Write(ThumbnailWidth);
					writer.Write(ThumbnailHeight);
					writer.Write((byte)ThumbnailFlags);
					writer.Write((byte)LibraryType);
					writer.Write(LibraryNumber);

					if (ShouldRequestAllThumbnails)
						writer.Write((byte)0x00);
					else
						writer.Write(RequestedElementNumbers, TypeCode.Byte, writer.Write);
			
					break;

				case MsexVersion.Version1_1:
					writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);
					writer.Write(ThumbnailWidth);
					writer.Write(ThumbnailHeight);
					writer.Write((byte)ThumbnailFlags);
					writer.Write((byte)LibraryType);

					if (!LibraryId.HasValue)
						throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.1");

					writer.Write(LibraryId.Value);

					if (ShouldRequestAllThumbnails)
						writer.Write((byte)0x00);
					else
						writer.Write(RequestedElementNumbers, TypeCode.Byte, writer.Write);

					break;

				case MsexVersion.Version1_2:
					writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);
					writer.Write(ThumbnailWidth);
					writer.Write(ThumbnailHeight);
					writer.Write((byte)ThumbnailFlags);
					writer.Write((byte)LibraryType);

					if (!LibraryId.HasValue)
						throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.2");

					writer.Write(LibraryId.Value);

					if (ShouldRequestAllThumbnails)
						writer.Write((byte)0x00);
					else
						writer.Write(RequestedElementNumbers, TypeCode.UInt16, writer.Write);

					break;
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			switch (Version)
			{
				case MsexVersion.Version1_0:
					ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
					ThumbnailWidth = reader.ReadUInt16();
					ThumbnailHeight = reader.ReadUInt16();
					ThumbnailFlags = (MsexThumbnailFlags)reader.ReadByte();
					LibraryType = (MsexLibraryType)reader.ReadByte();
					LibraryNumber = reader.ReadByte();

					RequestedElementNumbers = reader.ReadCollection(TypeCode.Byte, reader.ReadByte).ToImmutableSortedSet();

					if (RequestedElementNumbers.Count == 0)
						ShouldRequestAllThumbnails = true;
					
					break;

				case MsexVersion.Version1_1:
				
					ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
					ThumbnailWidth = reader.ReadUInt16();
					ThumbnailHeight = reader.ReadUInt16();
					ThumbnailFlags = (MsexThumbnailFlags)reader.ReadByte();
					LibraryType = (MsexLibraryType)reader.ReadByte();
					LibraryId = reader.ReadLibraryId();

					RequestedElementNumbers = reader.ReadCollection(TypeCode.Byte, reader.ReadByte).ToImmutableSortedSet();

					if (RequestedElementNumbers.Count == 0)
						ShouldRequestAllThumbnails = true;

					break;

				case MsexVersion.Version1_2:
				
					ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
					ThumbnailWidth = reader.ReadUInt16();
					ThumbnailHeight = reader.ReadUInt16();
					ThumbnailFlags = (MsexThumbnailFlags)reader.ReadByte();
					LibraryType = (MsexLibraryType)reader.ReadByte();
					LibraryId = reader.ReadLibraryId();

					RequestedElementNumbers = reader.ReadCollection(TypeCode.UInt16, reader.ReadByte).ToImmutableSortedSet();

					if (RequestedElementNumbers.Count == 0)
						ShouldRequestAllThumbnails = true;

					break;
			}
		}
	}
}