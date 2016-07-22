using System;
using System.Collections.Immutable;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class GetElementLibraryThumbnailPacket : MsexPacket
	{
		public GetElementLibraryThumbnailPacket()
			: base(MsexMessageType.GetElementLibraryThumbnailMessage) { }

		public MsexImageFormat ThumbnailFormat { get; set; }

		public int ThumbnailWidth { get; set; }
		public int ThumbnailHeight { get; set; }
		public MsexThumbnailFlags ThumbnailFlags { get; set; }

		public MsexLibraryType LibraryType { get; set; }

		public bool ShouldRequestAllThumbnails { get; set; }

		public ImmutableSortedSet<byte> RequestedLibraryNumbers { get; set; }
		public ImmutableSortedSet<MsexLibraryId> RequestedLibraryIds { get; set; }

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
					if (ShouldRequestAllThumbnails)
						writer.Write((byte)0);
					else
						writer.Write(RequestedLibraryNumbers, TypeCode.Byte, writer.Write);

					break;

				case MsexVersion.Version1_1:
					writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);
					writer.Write(ThumbnailWidth);
					writer.Write(ThumbnailHeight);
					writer.Write((byte)ThumbnailFlags);
					writer.Write((byte)LibraryType);
					if (ShouldRequestAllThumbnails)
						writer.Write((byte)0);
					else
						writer.Write(RequestedLibraryIds, TypeCode.Byte, writer.Write);

					break;

				case MsexVersion.Version1_2:
					writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);
					writer.Write(ThumbnailWidth);
					writer.Write(ThumbnailHeight);
					writer.Write((byte)ThumbnailFlags);
					writer.Write((byte)LibraryType);
					if (ShouldRequestAllThumbnails)
						writer.Write((byte)0);
					else
						writer.Write(RequestedLibraryIds, TypeCode.UInt16, writer.Write);

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

					RequestedLibraryNumbers = reader.ReadCollection(TypeCode.Byte, reader.ReadByte).ToImmutableSortedSet();

					if (RequestedLibraryNumbers.Count == 0)
						ShouldRequestAllThumbnails = true;

					break;

				case MsexVersion.Version1_1:
					ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
					ThumbnailWidth = reader.ReadUInt16();
					ThumbnailHeight = reader.ReadUInt16();
					ThumbnailFlags = (MsexThumbnailFlags)reader.ReadByte();
					LibraryType = (MsexLibraryType)reader.ReadByte();

					RequestedLibraryIds = reader.ReadCollection(TypeCode.Byte, reader.ReadLibraryId).ToImmutableSortedSet();

					if (RequestedLibraryIds.Count == 0)
						ShouldRequestAllThumbnails = true;

					break;

				case MsexVersion.Version1_2:
					ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
					ThumbnailWidth = reader.ReadUInt16();
					ThumbnailHeight = reader.ReadUInt16();
					ThumbnailFlags = (MsexThumbnailFlags)reader.ReadByte();
					LibraryType = (MsexLibraryType)reader.ReadByte();

					RequestedLibraryIds = reader.ReadCollection(TypeCode.UInt16, reader.ReadLibraryId).ToImmutableSortedSet();

					if (RequestedLibraryIds.Count == 0)
							ShouldRequestAllThumbnails = true;
				
					break;
			}
		}
	}
}