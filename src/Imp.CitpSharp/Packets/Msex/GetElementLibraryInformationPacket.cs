using System;
using System.Collections.Immutable;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class GetElementLibraryInformationPacket : MsexPacket
	{
		public GetElementLibraryInformationPacket()
			: base(MsexMessageType.GetElementLibraryInformationMessage) { }

		public MsexLibraryType LibraryType { get; set; }
		public MsexLibraryId? LibraryParentId { get; set; }
		public bool ShouldRequestAllLibraries { get; set; }
		public ImmutableSortedSet<byte> RequestedLibraryNumbers { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			switch (Version)
			{
				case MsexVersion.Version1_0:
					writer.Write((byte)LibraryType);

					if (ShouldRequestAllLibraries)
						writer.Write((byte)0);
					else
						writer.Write(RequestedLibraryNumbers, TypeCode.Byte, writer.Write);
					
					break;

				case MsexVersion.Version1_1:
					writer.Write((byte)LibraryType);

					if (!LibraryParentId.HasValue)
						throw new InvalidOperationException("LibraryParentId has no value. Required for MSEX V1.1");

					writer.Write(LibraryParentId.Value);

					if (ShouldRequestAllLibraries)
						writer.Write((byte)0);
					else
						writer.Write(RequestedLibraryNumbers, TypeCode.Byte, writer.Write);

					break;

				case MsexVersion.Version1_2:
					writer.Write((byte)LibraryType);

					if (!LibraryParentId.HasValue)
						throw new InvalidOperationException("LibraryParentId has no value. Required for MSEX V1.2");

					writer.Write(LibraryParentId.Value);

					if (ShouldRequestAllLibraries)
						writer.Write((ushort)0);
					else
						writer.Write(RequestedLibraryNumbers, TypeCode.UInt16, writer.Write);

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

					RequestedLibraryNumbers = reader.ReadCollection(TypeCode.Byte, reader.ReadByte).ToImmutableSortedSet();

					if (RequestedLibraryNumbers.Count == 0)
						ShouldRequestAllLibraries = true;
				
					break;

				case MsexVersion.Version1_1:
				
					LibraryType = (MsexLibraryType)reader.ReadByte();
					LibraryParentId = reader.ReadLibraryId();

					RequestedLibraryNumbers = reader.ReadCollection(TypeCode.Byte, reader.ReadByte).ToImmutableSortedSet();

					if (RequestedLibraryNumbers.Count == 0)
						ShouldRequestAllLibraries = true;

					break;

				case MsexVersion.Version1_2:
				
					LibraryType = (MsexLibraryType)reader.ReadByte();
					LibraryParentId = reader.ReadLibraryId();

					RequestedLibraryNumbers = reader.ReadCollection(TypeCode.UInt16, reader.ReadByte).ToImmutableSortedSet();

					if (RequestedLibraryNumbers.Count == 0)
						ShouldRequestAllLibraries = true;

					break;
			}
		}
	}
}