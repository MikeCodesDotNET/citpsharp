using System.Collections.Generic;
using System.Collections.Immutable;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class GetElementLibraryThumbnailPacket : MsexPacket
	{
		public GetElementLibraryThumbnailPacket()
			: base(MsexMessageType.GetElementLibraryThumbnailMessage) { }

	    public GetElementLibraryThumbnailPacket(MsexVersion version, MsexImageFormat thumbnailFormat, ushort thumbnailWidth,
	        ushort thumbnailHeight, MsexThumbnailFlags thumbnailFlags, MsexLibraryType libraryType,
	        IEnumerable<MsexId> requestedLibraries, ushort requestResponseIndex = 0)
	        : base(MsexMessageType.GetElementLibraryThumbnailMessage, version, requestResponseIndex)
	    {
	        ThumbnailFormat = thumbnailFormat;
	        ThumbnailWidth = thumbnailWidth;
	        ThumbnailHeight = thumbnailHeight;
	        ThumbnailFlags = thumbnailFlags;
	        LibraryType = libraryType;
	        ShouldRequestAllThumbnails = false;
	        RequestedLibraries = requestedLibraries.ToImmutableSortedSet();
	    }

        public GetElementLibraryThumbnailPacket(MsexVersion version, MsexImageFormat thumbnailFormat, ushort thumbnailWidth,
            ushort thumbnailHeight, MsexThumbnailFlags thumbnailFlags, MsexLibraryType libraryType, ushort requestResponseIndex = 0)
            : base(MsexMessageType.GetElementLibraryThumbnailMessage, version, requestResponseIndex)
        {
            ThumbnailFormat = thumbnailFormat;
            ThumbnailWidth = thumbnailWidth;
            ThumbnailHeight = thumbnailHeight;
            ThumbnailFlags = thumbnailFlags;
            LibraryType = libraryType;
            ShouldRequestAllThumbnails = true;
            RequestedLibraries = ImmutableSortedSet<MsexId>.Empty;
        }

        public MsexImageFormat ThumbnailFormat { get; private set; }

		public ushort ThumbnailWidth { get; private set; }
		public ushort ThumbnailHeight { get; private set; }
		public MsexThumbnailFlags ThumbnailFlags { get; private set; }

		public MsexLibraryType LibraryType { get; private set; }

		public bool ShouldRequestAllThumbnails { get; private set; }

		public ImmutableSortedSet<MsexId> RequestedLibraries { get; private set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

            writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);
            writer.Write(ThumbnailWidth);
            writer.Write(ThumbnailHeight);
            writer.Write((byte)ThumbnailFlags);
            writer.Write((byte)LibraryType);
            if (ShouldRequestAllThumbnails)
                writer.Write((byte)0);
            else
                writer.Write(RequestedLibraries, GetCollectionLengthType(),
                   i => writer.Write(i, Version));
        }

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

            ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
            ThumbnailWidth = reader.ReadUInt16();
            ThumbnailHeight = reader.ReadUInt16();
            ThumbnailFlags = (MsexThumbnailFlags)reader.ReadByte();
            LibraryType = (MsexLibraryType)reader.ReadByte();

            RequestedLibraries = reader.ReadCollection(GetCollectionLengthType(), 
                () => reader.ReadMsexId(Version)).ToImmutableSortedSet();

            if (RequestedLibraries.Count == 0)
                ShouldRequestAllThumbnails = true;
		}
	}
}