using System;
using System.Collections.Generic;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class MediaElementInformationPacket : MsexPacket
	{
		public MediaElementInformationPacket()
			: base(MsexMessageType.MediaElementInformationMessage) { }

		public byte LibraryNumber { get; set; }
		public MsexLibraryId? LibraryId { get; set; }

		public List<CitpMediaInformation> Media { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			switch (Version)
			{
				case MsexVersion.Version1_0:
					writer.Write(LibraryNumber);
					writer.Write((byte)Media.Count);

					foreach (var m in Media)
					{
						writer.Write(m.ElementNumber);
						writer.Write(m.DmxRangeMin);
						writer.Write(m.DmxRangeMax);
						writer.Write(m.Name);
						writer.Write(DateTimeHelpers.ConvertToUnixTimestamp(m.MediaVersionTimestamp));
						writer.Write(m.MediaWidth);
						writer.Write(m.MediaHeight);
						writer.Write(m.MediaLength);
						writer.Write(m.MediaFps);
					}
					break;

				case MsexVersion.Version1_1:
					if (!LibraryId.HasValue)
						throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.1");

					writer.Write(LibraryId.Value.ToByteArray());
					writer.Write((byte)Media.Count);

					foreach (var m in Media)
					{
						writer.Write(m.ElementNumber);
						writer.Write(m.DmxRangeMin);
						writer.Write(m.DmxRangeMax);
						writer.Write(m.Name);
						writer.Write(DateTimeHelpers.ConvertToUnixTimestamp(m.MediaVersionTimestamp));
						writer.Write(m.MediaWidth);
						writer.Write(m.MediaHeight);
						writer.Write(m.MediaLength);
						writer.Write(m.MediaFps);
					}
					break;

				case MsexVersion.Version1_2:
					if (!LibraryId.HasValue)
						throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.2");

					writer.Write(LibraryId.Value.ToByteArray());
					writer.Write((ushort)Media.Count);

					foreach (var m in Media)
					{
						writer.Write(m.ElementNumber);
						writer.Write(m.SerialNumber);
						writer.Write(m.DmxRangeMin);
						writer.Write(m.DmxRangeMax);
						writer.Write(m.Name);
						writer.Write(DateTimeHelpers.ConvertToUnixTimestamp(m.MediaVersionTimestamp));
						writer.Write(m.MediaWidth);
						writer.Write(m.MediaHeight);
						writer.Write(m.MediaLength);
						writer.Write(m.MediaFps);
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
					LibraryNumber = reader.ReadByte();

					int mediaCount = reader.ReadByte();
					Media = new List<CitpMediaInformation>(mediaCount);
					for (int i = 0; i < mediaCount; ++i)
					{
						Media.Add(new CitpMediaInformation
						{
							ElementNumber = reader.ReadByte(),
							DmxRangeMin = reader.ReadByte(),
							DmxRangeMax = reader.ReadByte(),
							Name = reader.ReadString(),
							MediaVersionTimestamp = DateTimeHelpers.ConvertFromUnixTimestamp(reader.ReadUInt64()),
							MediaWidth = reader.ReadUInt16(),
							MediaHeight = reader.ReadUInt16(),
							MediaLength = reader.ReadUInt32(),
							MediaFps = reader.ReadByte()
						});
					}
				}
					break;

				case MsexVersion.Version1_1:
				{
					LibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));

					int mediaCount = reader.ReadByte();
					Media = new List<CitpMediaInformation>(mediaCount);
					for (int i = 0; i < mediaCount; ++i)
					{
						Media.Add(new CitpMediaInformation
						{
							ElementNumber = reader.ReadByte(),
							DmxRangeMin = reader.ReadByte(),
							DmxRangeMax = reader.ReadByte(),
							Name = reader.ReadString(),
							MediaVersionTimestamp = DateTimeHelpers.ConvertFromUnixTimestamp(reader.ReadUInt64()),
							MediaWidth = reader.ReadUInt16(),
							MediaHeight = reader.ReadUInt16(),
							MediaLength = reader.ReadUInt32(),
							MediaFps = reader.ReadByte()
						});
					}
				}
					break;

				case MsexVersion.Version1_2:
				{
					LibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));

					int mediaCount = reader.ReadUInt16();
					Media = new List<CitpMediaInformation>(mediaCount);
					for (int i = 0; i < mediaCount; ++i)
					{
						Media.Add(new CitpMediaInformation
						{
							ElementNumber = reader.ReadByte(),
							SerialNumber = reader.ReadUInt32(),
							DmxRangeMin = reader.ReadByte(),
							DmxRangeMax = reader.ReadByte(),
							Name = reader.ReadString(),
							MediaVersionTimestamp = DateTimeHelpers.ConvertFromUnixTimestamp(reader.ReadUInt64()),
							MediaWidth = reader.ReadUInt16(),
							MediaHeight = reader.ReadUInt16(),
							MediaLength = reader.ReadUInt32(),
							MediaFps = reader.ReadByte()
						});
					}
				}
					break;
			}
		}
	}
}