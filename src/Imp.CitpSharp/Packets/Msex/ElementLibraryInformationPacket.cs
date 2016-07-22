using System;
using System.Collections.Immutable;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class ElementLibraryInformationPacket : MsexPacket
	{
		public ElementLibraryInformationPacket()
			: base(MsexMessageType.ElementLibraryInformationMessage) { }

		public MsexLibraryType LibraryType { get; set; }

		public ImmutableSortedSet<ElementLibraryInformation> Elements { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			switch (Version)
			{
				case MsexVersion.Version1_0:
				case MsexVersion.Version1_1:
					writer.Write((byte)LibraryType);
					writer.Write(Elements, TypeCode.Byte, e => e.Serialize(writer, Version.Value));
					break;

				case MsexVersion.Version1_2:
					writer.Write((byte)LibraryType);
					writer.Write(Elements, TypeCode.UInt16, e => e.Serialize(writer, Version.Value));
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
					LibraryType = (MsexLibraryType)reader.ReadByte();
					Elements = reader.ReadCollection(TypeCode.Byte, () => ElementLibraryInformation.Deserialize(reader, Version.Value))
						.ToImmutableSortedSet();
				
					break;

				case MsexVersion.Version1_2:
					LibraryType = (MsexLibraryType)reader.ReadByte();
					Elements = reader.ReadCollection(TypeCode.UInt16, () => ElementLibraryInformation.Deserialize(reader, Version.Value))
												.ToImmutableSortedSet();
					break;
			}
		}
	}
}