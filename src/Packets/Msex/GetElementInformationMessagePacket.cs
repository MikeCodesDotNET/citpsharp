using System;
using System.Collections.Generic;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class GetElementInformationMessagePacket : MsexPacket
	{
		public GetElementInformationMessagePacket()
			: base(MsexMessageType.GetElementInformationMessage) { }

		public MsexLibraryType LibraryType { get; set; }
		public byte LibraryNumber { get; set; }
		public MsexLibraryId? LibraryId { get; set; }

		public bool ShouldRequestAllElements { get; set; }
		public List<byte> RequestedElementNumbers { get; set; }

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
						writer.Write((byte)RequestedElementNumbers.Count);

					foreach (byte e in RequestedElementNumbers)
						writer.Write(e);
					break;

				case MsexVersion.Version1_1:
					writer.Write((byte)LibraryType);

					if (!LibraryId.HasValue)
						throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.1");

					writer.Write(LibraryId.Value.ToByteArray());

					if (ShouldRequestAllElements)
						writer.Write((byte)0x00);
					else
						writer.Write((byte)RequestedElementNumbers.Count);

					foreach (byte e in RequestedElementNumbers)
						writer.Write(e);
					break;

				case MsexVersion.Version1_2:
					writer.Write((byte)LibraryType);

					if (!LibraryId.HasValue)
						throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.2");

					writer.Write(LibraryId.Value.ToByteArray());

					if (ShouldRequestAllElements)
						writer.Write((ushort)0x00);
					else
						writer.Write((ushort)RequestedElementNumbers.Count);

					foreach (byte e in RequestedElementNumbers)
						writer.Write(e);
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
					LibraryNumber = reader.ReadByte();

					int requestedElementNumbersCount = reader.ReadByte();
					RequestedElementNumbers = new List<byte>(requestedElementNumbersCount);
					for (int i = 0; i < requestedElementNumbersCount; ++i)
						RequestedElementNumbers.Add(reader.ReadByte());

					if (requestedElementNumbersCount == 0)
						ShouldRequestAllElements = true;
				}
					break;

				case MsexVersion.Version1_1:
				{
					LibraryType = (MsexLibraryType)reader.ReadByte();
					LibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));

					int requestedElementNumbersCount = reader.ReadByte();
					RequestedElementNumbers = new List<byte>(requestedElementNumbersCount);
					for (int i = 0; i < requestedElementNumbersCount; ++i)
						RequestedElementNumbers.Add(reader.ReadByte());

					if (requestedElementNumbersCount == 0)
						ShouldRequestAllElements = true;
				}
					break;

				case MsexVersion.Version1_2:
				{
					LibraryType = (MsexLibraryType)reader.ReadByte();
					LibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));

					int requestedElementNumbersCount = reader.ReadUInt16();
					RequestedElementNumbers = new List<byte>(requestedElementNumbersCount);
					for (int i = 0; i < requestedElementNumbersCount; ++i)
						RequestedElementNumbers.Add(reader.ReadByte());

					if (requestedElementNumbersCount == 0)
						ShouldRequestAllElements = true;
				}
					break;
			}
		}
	}
}