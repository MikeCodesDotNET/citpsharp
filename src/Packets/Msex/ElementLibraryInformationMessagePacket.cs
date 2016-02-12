using System;
using System.Collections.Generic;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class ElementLibraryInformationMessagePacket : MsexPacket
	{
		public ElementLibraryInformationMessagePacket()
			: base(MsexMessageType.ElementLibraryInformationMessage) { }

		public MsexLibraryType LibraryType { get; set; }

		public List<CitpElementLibraryInformation> Elements { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			switch (Version)
			{
				case MsexVersion.Version1_0:
					writer.Write((byte)LibraryType);

					writer.Write((byte)Elements.Count);
					foreach (var e in Elements)
					{
						writer.Write(e.Number);
						writer.Write(e.DmxRangeMin);
						writer.Write(e.DmxRangeMax);
						writer.Write(e.Name);
						writer.Write((byte)e.ElementCount);
					}
					break;

				case MsexVersion.Version1_1:
					writer.Write((byte)LibraryType);

					writer.Write((byte)Elements.Count);
					foreach (var e in Elements)
					{
						if (!e.Id.HasValue)
							throw new InvalidOperationException("Element Id has no value. Required for MSEX V1.1");

						writer.Write(e.Id.Value.ToByteArray());
						writer.Write(e.DmxRangeMin);
						writer.Write(e.DmxRangeMax);
						writer.Write(e.Name);
						writer.Write((byte)e.LibraryCount);
						writer.Write((byte)e.ElementCount);
					}
					break;

				case MsexVersion.Version1_2:
					writer.Write((byte)LibraryType);

					writer.Write((ushort)Elements.Count);
					foreach (var e in Elements)
					{
						if (!e.Id.HasValue)
							throw new InvalidOperationException("Element Id has no value. Required for MSEX V1.2");

						writer.Write(e.Id.Value.ToByteArray());
						writer.Write(e.SerialNumber);
						writer.Write(e.DmxRangeMin);
						writer.Write(e.DmxRangeMax);
						writer.Write(e.Name);
						writer.Write(e.LibraryCount);
						writer.Write(e.ElementCount);
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

					int libraryCount = reader.ReadByte();
					Elements = new List<CitpElementLibraryInformation>(libraryCount);
					for (int i = 0; i < libraryCount; ++i)
					{
						Elements.Add(new CitpElementLibraryInformation
						{
							Number = reader.ReadByte(),
							DmxRangeMin = reader.ReadByte(),
							DmxRangeMax = reader.ReadByte(),
							Name = reader.ReadString(),
							ElementCount = reader.ReadByte()
						});
					}
				}
					break;

				case MsexVersion.Version1_1:
				{
					LibraryType = (MsexLibraryType)reader.ReadByte();

					int libraryCount = reader.ReadByte();
					Elements = new List<CitpElementLibraryInformation>(libraryCount);
					for (int i = 0; i < libraryCount; ++i)
					{
						Elements.Add(new CitpElementLibraryInformation
						{
							Number = reader.ReadByte(),
							DmxRangeMin = reader.ReadByte(),
							DmxRangeMax = reader.ReadByte(),
							Name = reader.ReadString(),
							LibraryCount = reader.ReadByte(),
							ElementCount = reader.ReadByte()
						});
					}
				}
					break;

				case MsexVersion.Version1_2:
				{
					LibraryType = (MsexLibraryType)reader.ReadByte();

					int libraryCount = reader.ReadUInt16();
					Elements = new List<CitpElementLibraryInformation>(libraryCount);
					for (int i = 0; i < libraryCount; ++i)
					{
						Elements.Add(new CitpElementLibraryInformation
						{
							Number = reader.ReadByte(),
							DmxRangeMin = reader.ReadByte(),
							DmxRangeMax = reader.ReadByte(),
							Name = reader.ReadString(),
							LibraryCount = reader.ReadUInt16(),
							ElementCount = reader.ReadUInt16()
						});
					}
				}
					break;
			}
		}
	}
}