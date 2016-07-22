using System;
using System.Collections.Immutable;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class GetElementInformationPacket : MsexPacket
	{
		public GetElementInformationPacket()
			: base(MsexMessageType.GetElementInformationMessage) { }

		public MsexLibraryType LibraryType { get; set; }
		public byte LibraryNumber { get; set; }
		public MsexLibraryId? LibraryId { get; set; }

		public bool ShouldRequestAllElements { get; set; }
		public ImmutableSortedSet<byte> RequestedElementNumbers { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			switch (Version)
			{
				case MsexVersion.Version1_0:
					writer.Write((byte)LibraryType);
					writer.Write(LibraryNumber);

					if (ShouldRequestAllElements)
						writer.Write((byte)0x00);
					else
						writer.Write(RequestedElementNumbers, TypeCode.Byte, writer.Write);

					break;

				case MsexVersion.Version1_1:
					writer.Write((byte)LibraryType);

					if (!LibraryId.HasValue)
						throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.1");

					writer.Write(LibraryId.Value);

					if (ShouldRequestAllElements)
						writer.Write((byte)0x00);
					else
						writer.Write(RequestedElementNumbers, TypeCode.Byte, writer.Write);

					break;

				case MsexVersion.Version1_2:
					writer.Write((byte)LibraryType);

					if (!LibraryId.HasValue)
						throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.2");

					writer.Write(LibraryId.Value);

					if (ShouldRequestAllElements)
						writer.Write((ushort)0x00);
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
				
					LibraryType = (MsexLibraryType)reader.ReadByte();
					LibraryNumber = reader.ReadByte();

					RequestedElementNumbers = reader.ReadCollection(TypeCode.Byte, reader.ReadByte).ToImmutableSortedSet();

					if (RequestedElementNumbers.Count == 0)
						ShouldRequestAllElements = true;
				
					break;

				case MsexVersion.Version1_1:
				
					LibraryType = (MsexLibraryType)reader.ReadByte();
					LibraryId = reader.ReadLibraryId();

					RequestedElementNumbers = reader.ReadCollection(TypeCode.Byte, reader.ReadByte).ToImmutableSortedSet();

					if (RequestedElementNumbers.Count == 0)
						ShouldRequestAllElements = true;
				
					break;

				case MsexVersion.Version1_2:
				
					LibraryType = (MsexLibraryType)reader.ReadByte();
					LibraryId = reader.ReadLibraryId();

					RequestedElementNumbers = reader.ReadCollection(TypeCode.UInt16, reader.ReadByte).ToImmutableSortedSet();

					if (RequestedElementNumbers.Count == 0)
						ShouldRequestAllElements = true;
					
					break;
			}
		}
	}
}