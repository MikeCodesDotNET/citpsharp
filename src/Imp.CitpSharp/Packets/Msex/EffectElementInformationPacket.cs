using System;
using System.Collections.Immutable;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class EffectElementInformationPacket : MsexPacket
	{
		public EffectElementInformationPacket()
			: base(MsexMessageType.EffectElementInformationMessage) { }

		public byte LibraryNumber { get; set; }
		public MsexLibraryId? LibraryId { get; set; }

		public ImmutableSortedSet<EffectInformation> Effects { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			switch (Version)
			{
				case MsexVersion.Version1_0:
					writer.Write(LibraryNumber);
					writer.Write(Effects, TypeCode.Byte, e => e.Serialize(writer, Version.Value));
					break;

				case MsexVersion.Version1_1:
					if (!LibraryId.HasValue)
						throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.1");

					writer.Write(LibraryId.Value);
					writer.Write(Effects, TypeCode.Byte, e => e.Serialize(writer, Version.Value));
					break;

				case MsexVersion.Version1_2:
					if (!LibraryId.HasValue)
						throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.2");

					writer.Write(LibraryId.Value);
					writer.Write(Effects, TypeCode.UInt16, e => e.Serialize(writer, Version.Value));
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
					Effects = reader.ReadCollection(TypeCode.Byte, () => EffectInformation.Deserialize(reader, Version.Value))
						.ToImmutableSortedSet();
				
					break;

				case MsexVersion.Version1_1:
				
					LibraryId = reader.ReadLibraryId();
					Effects = reader.ReadCollection(TypeCode.Byte, () => EffectInformation.Deserialize(reader, Version.Value))
						.ToImmutableSortedSet();

					break;

				case MsexVersion.Version1_2:
				
					LibraryId = reader.ReadLibraryId();
					Effects = reader.ReadCollection(TypeCode.UInt16, () => EffectInformation.Deserialize(reader, Version.Value))
						.ToImmutableSortedSet();

					break;
			}
		}
	}
}