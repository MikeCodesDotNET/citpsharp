using System.Collections.Generic;
using System.Collections.Immutable;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class MediaElementInformationPacket : MsexPacket
	{
		public MediaElementInformationPacket()
			: base(MsexMessageType.MediaElementInformationMessage) { }

	    public MediaElementInformationPacket(MsexVersion version, MsexId library, IEnumerable<MediaInformation> media,
	        ushort requestResponseIndex = 0)
	        : base(MsexMessageType.MediaElementInformationMessage, version, requestResponseIndex)
	    {
	        Library = library;
	        Media = media.ToImmutableSortedSet();
	    }

		public MsexId Library { get; private set; }

		public ImmutableSortedSet<MediaInformation> Media { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

            writer.Write(Library, Version);
            writer.Write(Media, GetCollectionLengthType(), m => m.Serialize(writer, Version));
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

            Library = reader.ReadMsexId(Version);
            Media = reader.ReadCollection(GetCollectionLengthType(), () => MediaInformation.Deserialize(reader, Version))
                    .ToImmutableSortedSet();
		}
	}
}