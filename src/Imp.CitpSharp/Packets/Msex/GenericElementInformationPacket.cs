using System;
using System.Collections.Immutable;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class GenericElementInformationPacket : MsexPacket
	{
		public GenericElementInformationPacket()
			: base(MsexMessageType.GenericElementInformationMessage) { }

		public MsexLibraryType LibraryType { get; set; }
		public MsexLibraryId LibraryId { get; set; }

		public ImmutableSortedSet<GenericInformation> Information { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			switch (Version)
			{
				case MsexVersion.Version1_1:
					writer.Write(LibraryId);
					writer.Write(Information, TypeCode.Byte, e => e.Serialize(writer, Version.Value));
					break;

				case MsexVersion.Version1_2:
					writer.Write((byte)LibraryType);
					writer.Write(LibraryId);
					writer.Write(Information, TypeCode.UInt16, e => e.Serialize(writer, Version.Value));
					break;
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			switch (Version)
			{
				case MsexVersion.Version1_1:
					LibraryId = reader.ReadLibraryId();
					Information = reader.ReadCollection(TypeCode.Byte, () => GenericInformation.Deserialize(reader, Version.Value))
							.ToImmutableSortedSet();
				
					break;

				case MsexVersion.Version1_2:
					LibraryType = (MsexLibraryType)reader.ReadByte();
					LibraryId = reader.ReadLibraryId();
					Information = reader.ReadCollection(TypeCode.UInt16, () => GenericInformation.Deserialize(reader, Version.Value))
							.ToImmutableSortedSet();
					
					break;
			}
		}
	}
}