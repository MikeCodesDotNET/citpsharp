using System;
using System.Collections.Immutable;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class MediaElementInformationPacket : MsexPacket
	{
		public MediaElementInformationPacket()
			: base(MsexMessageType.MediaElementInformationMessage) { }

		public byte LibraryNumber { get; set; }
		public MsexLibraryId? LibraryId { get; set; }

		public ImmutableSortedSet<MediaInformation> Media { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			switch (Version)
			{
				case MsexVersion.Version1_0:
					writer.Write(LibraryNumber);
					writer.Write(Media, TypeCode.Byte, m => m.Serialize(writer, Version.Value));
					break;

				case MsexVersion.Version1_1:
					if (!LibraryId.HasValue)
						throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.1");

					writer.Write(LibraryId.Value);
					writer.Write(Media, TypeCode.Byte, m => m.Serialize(writer, Version.Value));
					break;

				case MsexVersion.Version1_2:
					if (!LibraryId.HasValue)
						throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.2");

					writer.Write(LibraryId.Value);
					writer.Write(Media, TypeCode.UInt16, m => m.Serialize(writer, Version.Value));
					break;
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			switch (Version)
			{
				case MsexVersion.Version1_0:
					LibraryNumber = reader.ReadByte();
					Media = reader.ReadCollection(TypeCode.UInt16, () => MediaInformation.Deserialize(reader, Version.Value))
							.ToImmutableSortedSet();

					break;

				case MsexVersion.Version1_1:
					LibraryId = reader.ReadLibraryId();
					Media = reader.ReadCollection(TypeCode.Byte, () => MediaInformation.Deserialize(reader, Version.Value))
							.ToImmutableSortedSet();
				
					break;

				case MsexVersion.Version1_2:
					LibraryId = reader.ReadLibraryId();
					Media = reader.ReadCollection(TypeCode.UInt16, () => MediaInformation.Deserialize(reader, Version.Value))
							.ToImmutableSortedSet();

					break;
			}
		}
	}
}