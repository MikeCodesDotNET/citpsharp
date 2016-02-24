using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class ServerInformationMessagePacket : MsexPacket
	{
		public ServerInformationMessagePacket()
			: base(MsexMessageType.ServerInformationMessage) { }

		public Guid Uuid { get; set; }

		public string ProductName { get; set; }

		public byte ProductVersionMajor { get; set; }
		public byte ProductVersionMinor { get; set; }
		public byte ProductVersionBugfix { get; set; }

		public List<MsexVersion> SupportedMsexVersions { get; set; }

		public List<MsexLibraryType> SupportedLibraryTypes { get; set; }

		public List<MsexImageFormat> ThumbnailFormats { get; set; }
		public List<MsexImageFormat> StreamFormats { get; set; }

		public List<CitpDmxConnectionString> LayerDmxSources { get; set; }

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

					writer.Write((byte)LayerDmxSources.Count);
					foreach (var d in LayerDmxSources)
						writer.Write(d.ToString(), true);
					break;

				case MsexVersion.Version1_2:
					writer.Write(Encoding.UTF8.GetBytes(Uuid.ToString("D")));
					writer.Write(ProductName);

					writer.Write(ProductVersionMajor);
					writer.Write(ProductVersionMinor);
					writer.Write(ProductVersionBugfix);

					writer.Write((byte)SupportedMsexVersions.Count);
					foreach (var v in SupportedMsexVersions)
						writer.Write(v.GetCustomAttribute<CitpVersionAttribute>().ToByteArray());

					ushort supportedLibraryTypes = 0;
					foreach (var t in SupportedLibraryTypes)
						supportedLibraryTypes |= (ushort)(2 ^ (int)t);
					writer.Write(supportedLibraryTypes);

					writer.Write((byte)ThumbnailFormats.Count);
					foreach (var f in ThumbnailFormats)
						writer.Write(f.GetCustomAttribute<CitpId>().Id);

					writer.Write((byte)StreamFormats.Count);
					foreach (var f in StreamFormats)
						writer.Write(f.GetCustomAttribute<CitpId>().Id);

					writer.Write((byte)LayerDmxSources.Count);
					foreach (var d in LayerDmxSources)
						writer.Write(d.ToString(), true);
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

					int dmxSourcesCount = reader.ReadByte();
					LayerDmxSources = new List<CitpDmxConnectionString>(dmxSourcesCount);
					for (int i = 0; i < dmxSourcesCount; ++i)
						LayerDmxSources.Add(CitpDmxConnectionString.Parse(reader.ReadString(true)));
				}
					break;

				case MsexVersion.Version1_2:
				{
					Uuid = new Guid(reader.ReadString(true));

					ProductName = reader.ReadString();
					ProductVersionMajor = reader.ReadByte();
					ProductVersionMinor = reader.ReadByte();
					ProductVersionBugfix = reader.ReadByte();

					byte supportedVersionsCount = reader.ReadByte();
					SupportedMsexVersions = new List<MsexVersion>(supportedVersionsCount);
					for (int i = 0; i < supportedVersionsCount; ++i)
					{
						byte versionMajor = reader.ReadByte();
						byte versionMinor = reader.ReadByte();

						if (versionMajor == 1 && versionMinor == 0)
							SupportedMsexVersions.Add(MsexVersion.Version1_0);
						else if (versionMajor == 1 && versionMinor == 1)
							SupportedMsexVersions.Add(MsexVersion.Version1_1);
						else if (versionMajor == 1 && versionMinor == 2)
							SupportedMsexVersions.Add(MsexVersion.Version1_2);
						else
							SupportedMsexVersions.Add(MsexVersion.UnsupportedVersion);
					}

					SupportedLibraryTypes = new List<MsexLibraryType>();
					var supportedLibraryTypesBits = new BitArray(reader.ReadBytes(2));
					for (byte i = 0; i < supportedLibraryTypesBits.Length; ++i)
					{
						if (supportedLibraryTypesBits[i])
							SupportedLibraryTypes.Add((MsexLibraryType)i);
					}

					int thumbnailFormatsCount = reader.ReadByte();
					ThumbnailFormats = new List<MsexImageFormat>(thumbnailFormatsCount);
					for (int i = 0; i < thumbnailFormatsCount; ++i)
						ThumbnailFormats.Add(CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString()));

					int streamFormatsCount = reader.ReadByte();
					StreamFormats = new List<MsexImageFormat>(streamFormatsCount);
					for (int i = 0; i < streamFormatsCount; ++i)
						StreamFormats.Add(CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString()));

					int dmxSourcesCount = reader.ReadByte();
					LayerDmxSources = new List<CitpDmxConnectionString>(dmxSourcesCount);
					for (int i = 0; i < dmxSourcesCount; ++i)
						LayerDmxSources.Add(CitpDmxConnectionString.Parse(reader.ReadString(true)));
				}
					break;
			}
		}
	}
}