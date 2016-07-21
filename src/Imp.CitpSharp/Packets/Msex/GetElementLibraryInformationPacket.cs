using System;
using System.Collections.Generic;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class GetElementLibraryInformationPacket : MsexPacket
	{
		public GetElementLibraryInformationPacket()
			: base(MsexMessageType.GetElementLibraryInformationMessage) { }

		public MsexLibraryType LibraryType { get; set; }
		public MsexLibraryId? LibraryParentId { get; set; }
		public bool ShouldRequestAllLibraries { get; set; }
		public List<byte> RequestedLibraryNumbers { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			switch (Version)
			{
				case MsexVersion.Version1_0:
					writer.Write((byte)LibraryType);

					if (ShouldRequestAllLibraries)
					{
						writer.Write((byte)0);
					}
					else
					{
						writer.Write((byte)RequestedLibraryNumbers.Count);
						foreach (byte n in RequestedLibraryNumbers)
							writer.Write(n);
					}
					break;

				case MsexVersion.Version1_1:
					writer.Write((byte)LibraryType);

					if (!LibraryParentId.HasValue)
						throw new InvalidOperationException("LibraryParentId has no value. Required for MSEX V1.1");

					writer.Write(LibraryParentId.Value.ToByteArray());

					if (ShouldRequestAllLibraries)
					{
						writer.Write((byte)0);
					}
					else
					{
						writer.Write((byte)RequestedLibraryNumbers.Count);
						foreach (byte n in RequestedLibraryNumbers)
							writer.Write(n);
					}
					break;

				case MsexVersion.Version1_2:
					writer.Write((byte)LibraryType);

					if (!LibraryParentId.HasValue)
						throw new InvalidOperationException("LibraryParentId has no value. Required for MSEX V1.2");

					writer.Write(LibraryParentId.Value.ToByteArray());

					if (ShouldRequestAllLibraries)
					{
						writer.Write((ushort)0);
					}
					else
					{
						writer.Write((byte)RequestedLibraryNumbers.Count);
						foreach (byte n in RequestedLibraryNumbers)
							writer.Write(n);
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
				{
					LibraryType = (MsexLibraryType)reader.ReadByte();

					int libraryNumberCount = reader.ReadByte();
					RequestedLibraryNumbers = new List<byte>(libraryNumberCount);
					for (int i = 0; i < libraryNumberCount; ++i)
						RequestedLibraryNumbers.Add(reader.ReadByte());

					if (libraryNumberCount == 0)
						ShouldRequestAllLibraries = true;
				}
					break;

				case MsexVersion.Version1_1:
				{
					LibraryType = (MsexLibraryType)reader.ReadByte();
					LibraryParentId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));

					int libraryNumberCount = reader.ReadByte();
					RequestedLibraryNumbers = new List<byte>(libraryNumberCount);
					for (int i = 0; i < libraryNumberCount; ++i)
						RequestedLibraryNumbers.Add(reader.ReadByte());

					if (libraryNumberCount == 0)
						ShouldRequestAllLibraries = true;
				}
					break;

				case MsexVersion.Version1_2:
				{
					LibraryType = (MsexLibraryType)reader.ReadByte();
					LibraryParentId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));

					int libraryNumberCount = reader.ReadUInt16();
					RequestedLibraryNumbers = new List<byte>(libraryNumberCount);
					for (int i = 0; i < libraryNumberCount; ++i)
						RequestedLibraryNumbers.Add(reader.ReadByte());

					if (libraryNumberCount == 0)
						ShouldRequestAllLibraries = true;
				}
					break;
			}
		}
	}
}