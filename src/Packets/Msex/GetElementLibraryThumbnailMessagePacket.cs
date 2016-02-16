using System.Collections.Generic;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class GetElementLibraryThumbnailMessagePacket : MsexPacket
	{
		public GetElementLibraryThumbnailMessagePacket()
			: base(MsexMessageType.GetElementLibraryThumbnailMessage) { }

		public MsexImageFormat ThumbnailFormat { get; set; }

		public int ThumbnailWidth { get; set; }
		public int ThumbnailHeight { get; set; }
		public MsexThumbnailFlags ThumbnailFlags { get; set; }

		public MsexLibraryType LibraryType { get; set; }

		public bool ShouldRequestAllThumbnails { get; set; }

		public List<byte> LibraryNumbers { get; set; }
		public List<MsexLibraryId> LibraryIds { get; set; }

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
					{
						writer.Write((byte)0x00);
					}
					else
					{
						writer.Write((byte)LibraryIds.Count);
						foreach (var l in LibraryIds)
							writer.Write(l.ToByteArray());
					}
					break;

				case MsexVersion.Version1_1:
					writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);
					writer.Write(ThumbnailWidth);
					writer.Write(ThumbnailHeight);

					writer.Write((byte)ThumbnailFlags);

					writer.Write((byte)LibraryType);

					if (ShouldRequestAllThumbnails)
					{
						writer.Write((byte)0x00);
					}
					else
					{
						writer.Write((byte)LibraryIds.Count);
						foreach (var l in LibraryIds)
							writer.Write(l.ToByteArray());
					}
					break;

				case MsexVersion.Version1_2:
					writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);
					writer.Write(ThumbnailWidth);
					writer.Write(ThumbnailHeight);

					writer.Write((byte)ThumbnailFlags);

					writer.Write((byte)LibraryType);

					if (ShouldRequestAllThumbnails)
					{
						writer.Write((ushort)0x0000);
					}
					else
					{
						writer.Write((ushort)LibraryIds.Count);
						foreach (var l in LibraryIds)
							writer.Write(l.ToByteArray());
					}
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

					int libraryNumberCount = reader.ReadByte();
					LibraryNumbers = new List<byte>(libraryNumberCount);
					for (int i = 0; i < libraryNumberCount; ++i)
						LibraryNumbers.Add(reader.ReadByte());

					if (libraryNumberCount == 0)
						ShouldRequestAllThumbnails = true;
					break;

				case MsexVersion.Version1_1:
				{
					ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
					ThumbnailWidth = reader.ReadUInt16();
					ThumbnailHeight = reader.ReadUInt16();
					ThumbnailFlags = (MsexThumbnailFlags)reader.ReadByte();
					LibraryType = (MsexLibraryType)reader.ReadByte();

					int libraryIdCount = reader.ReadByte();
					LibraryIds = new List<MsexLibraryId>(libraryIdCount);
					for (int i = 0; i < libraryIdCount; ++i)
						LibraryIds.Add(MsexLibraryId.FromByteArray(reader.ReadBytes(4)));

					if (libraryIdCount == 0)
						ShouldRequestAllThumbnails = true;
				}
					break;

				case MsexVersion.Version1_2:
				{
					ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
					ThumbnailWidth = reader.ReadUInt16();
					ThumbnailHeight = reader.ReadUInt16();
					ThumbnailFlags = (MsexThumbnailFlags)reader.ReadByte();
					LibraryType = (MsexLibraryType)reader.ReadByte();

					int libraryIdCount = reader.ReadUInt16();
					LibraryIds = new List<MsexLibraryId>(libraryIdCount);
					for (int i = 0; i < libraryIdCount; ++i)
						LibraryIds.Add(MsexLibraryId.FromByteArray(reader.ReadBytes(4)));

					if (libraryIdCount == 0)
						ShouldRequestAllThumbnails = true;
				}
					break;
			}
		}
	}
}