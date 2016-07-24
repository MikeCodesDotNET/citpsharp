namespace Imp.CitpSharp.Packets.Msex
{
	internal class ElementLibraryThumbnailPacket : MsexPacket
	{
		public ElementLibraryThumbnailPacket()
			: base(MsexMessageType.ElementLibraryThumbnailMessage) { }

	    public ElementLibraryThumbnailPacket(MsexVersion version, MsexLibraryType libraryType, MsexId library,
	        MsexImageFormat thumbnailFormat, ushort thumbnailWidth, ushort thumbnailHeight, byte[] thumbnailBuffer,
            ushort requestResponseIndex = 0)
            : base(MsexMessageType.ElementLibraryThumbnailMessage, version, requestResponseIndex)
	    {
	        LibraryType = libraryType;
	        Library = library;
	        ThumbnailFormat = thumbnailFormat;
	        ThumbnailWidth = thumbnailWidth;
	        ThumbnailHeight = thumbnailHeight;
	        ThumbnailBuffer = thumbnailBuffer;
	    }

		public MsexLibraryType LibraryType { get; private set; }
		public MsexId Library { get; private set; }

		public MsexImageFormat ThumbnailFormat { get; private set; }

		public ushort ThumbnailWidth { get; private set; }
		public ushort ThumbnailHeight { get; private set; }
		public byte[] ThumbnailBuffer { get; private set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

            writer.Write((byte)LibraryType);
            writer.Write(Library, Version);
            writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);
            writer.Write(ThumbnailWidth);
            writer.Write(ThumbnailHeight);
            writer.Write((ushort)ThumbnailBuffer.Length);
            writer.Write(ThumbnailBuffer);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

            LibraryType = (MsexLibraryType)reader.ReadByte();
		    Library = reader.ReadMsexId(Version);

            ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());

            ThumbnailWidth = reader.ReadUInt16();
            ThumbnailHeight = reader.ReadUInt16();

            int thumbnailBufferLength = reader.ReadUInt16();
            ThumbnailBuffer = reader.ReadBytes(thumbnailBufferLength);
		}
	}
}