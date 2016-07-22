using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class ServerInformationPacket : MsexPacket
	{
		public ServerInformationPacket()
			: base(MsexMessageType.ServerInformationMessage) { }

		public Guid Uuid { get; set; }

		public string ProductName { get; set; }

		public byte ProductVersionMajor { get; set; }
		public byte ProductVersionMinor { get; set; }
		public byte ProductVersionBugfix { get; set; }

		public ImmutableHashSet<MsexVersion> SupportedMsexVersions { get; set; }

		public ImmutableHashSet<MsexLibraryType> SupportedLibraryTypes { get; set; }

		public ImmutableHashSet<MsexImageFormat> ThumbnailFormats { get; set; }
		public ImmutableHashSet<MsexImageFormat> StreamFormats { get; set; }

		public ImmutableList<CitpDmxConnectionString> LayerDmxSources { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			switch (Version)
			{
				case MsexVersion.Version1_0:
				case MsexVersion.Version1_1:
					writer.Write(ProductName);
					writer.Write(ProductVersionMajor);
					writer.Write(ProductVersionMinor);
					writer.Write(LayerDmxSources, TypeCode.Byte, d => writer.Write(d, true));
					break;

				case MsexVersion.Version1_2:
					writer.Write(Uuid);
					writer.Write(ProductName);

					writer.Write(ProductVersionMajor);
					writer.Write(ProductVersionMinor);
					writer.Write(ProductVersionBugfix);

					writer.Write(SupportedMsexVersions, TypeCode.Byte, v => writer.Write(v, true));

					ushort supportedLibraryTypes = 0;
					foreach (var t in SupportedLibraryTypes)
						supportedLibraryTypes |= (ushort)(2 ^ (int)t);
					writer.Write(supportedLibraryTypes);

					writer.Write(ThumbnailFormats, TypeCode.Byte, f => writer.Write(f.GetCustomAttribute<CitpId>().Id));
					writer.Write(StreamFormats, TypeCode.Byte, f => writer.Write(f.GetCustomAttribute<CitpId>().Id));
					writer.Write(LayerDmxSources, TypeCode.Byte, d => writer.Write(d, true));

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
				{
					ProductName = reader.ReadString();
					ProductVersionMajor = reader.ReadByte();
					ProductVersionMinor = reader.ReadByte();

					LayerDmxSources = reader.ReadCollection(TypeCode.Byte, 
						() => CitpDmxConnectionString.Parse(reader.ReadString(true))).ToImmutableList();
				}
					break;

				case MsexVersion.Version1_2:
				{
					Uuid = reader.ReadGuid();

					ProductName = reader.ReadString();
					ProductVersionMajor = reader.ReadByte();
					ProductVersionMinor = reader.ReadByte();
					ProductVersionBugfix = reader.ReadByte();

					SupportedMsexVersions = reader.ReadCollection(TypeCode.Byte, () => reader.ReadMsexVersion(true)).ToImmutableHashSet();

					var supportedLibraryTypesList = new List<MsexLibraryType>();
					var supportedLibraryTypesBits = new BitArray(reader.ReadBytes(2));
					for (byte i = 0; i < supportedLibraryTypesBits.Length; ++i)
					{
						if (supportedLibraryTypesBits[i])
							supportedLibraryTypesList.Add((MsexLibraryType)i);
					}
					SupportedLibraryTypes = supportedLibraryTypesList.ToImmutableHashSet();

					ThumbnailFormats = reader.ReadCollection(TypeCode.Byte,
						() => CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString())).ToImmutableHashSet();

					StreamFormats = reader.ReadCollection(TypeCode.Byte,
						() => CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString())).ToImmutableHashSet();

					LayerDmxSources = reader.ReadCollection(TypeCode.Byte,
							() => CitpDmxConnectionString.Parse(reader.ReadString(true))).ToImmutableList();
				}
					break;
			}
		}
	}
}