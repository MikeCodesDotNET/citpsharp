using System.Collections.Generic;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class GenericElementInformationMessagePacket : MsexPacket
	{
		public GenericElementInformationMessagePacket()
			: base(MsexMessageType.GenericElementInformationMessage) { }

		public MsexLibraryType LibraryType { get; set; }
		public MsexLibraryId LibraryId { get; set; }

		public List<CitpGenericInformation> Information { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			switch (Version)
			{
				case MsexVersion.Version1_1:
					writer.Write(LibraryId.ToByteArray());

					writer.Write((byte)Information.Count);
					foreach (var i in Information)
					{
						writer.Write(i.ElementNumber);
						writer.Write(i.DmxRangeMin);
						writer.Write(i.DmxRangeMax);
						writer.Write(i.Name);
						writer.Write(DateTimeHelpers.ConvertToUnixTimestamp(i.VersionTimestamp));
					}
					break;

				case MsexVersion.Version1_2:
					writer.Write((byte)LibraryType);
					writer.Write(LibraryId.ToByteArray());

					writer.Write((ushort)Information.Count);
					foreach (var i in Information)
					{
						writer.Write(i.ElementNumber);
						writer.Write(i.SerialNumber);
						writer.Write(i.DmxRangeMin);
						writer.Write(i.DmxRangeMax);
						writer.Write(i.Name);
						writer.Write(DateTimeHelpers.ConvertToUnixTimestamp(i.VersionTimestamp));
					}
					break;
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			switch (Version)
			{
				case MsexVersion.Version1_1:
				{
					LibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));

					int elementCount = reader.ReadByte();
					Information = new List<CitpGenericInformation>(elementCount);
					for (int i = 0; i < elementCount; ++i)
					{
						Information.Add(new CitpGenericInformation
						{
							ElementNumber = reader.ReadByte(),
							DmxRangeMin = reader.ReadByte(),
							DmxRangeMax = reader.ReadByte(),
							Name = reader.ReadString(),
							VersionTimestamp = DateTimeHelpers.ConvertFromUnixTimestamp(reader.ReadUInt64())
						});
					}
				}
					break;

				case MsexVersion.Version1_2:
				{
					LibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));

					int elementCount = reader.ReadUInt16();
					Information = new List<CitpGenericInformation>(elementCount);
					for (int i = 0; i < elementCount; ++i)
					{
						Information.Add(new CitpGenericInformation
						{
							ElementNumber = reader.ReadByte(),
							SerialNumber = reader.ReadUInt32(),
							DmxRangeMin = reader.ReadByte(),
							DmxRangeMax = reader.ReadByte(),
							Name = reader.ReadString(),
							VersionTimestamp = DateTimeHelpers.ConvertFromUnixTimestamp(reader.ReadUInt64())
						});
					}
				}
					break;
			}
		}
	}
}