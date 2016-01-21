//  This file is part of CitpSharp.
//
//  CitpSharp is free software: you can redistribute it and/or modify
//	it under the terms of the GNU Lesser General Public License as published by
//	the Free Software Foundation, either version 3 of the License, or
//	(at your option) any later version.

//	CitpSharp is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU Lesser General Public License for more details.

//	You should have received a copy of the GNU Lesser General Public License
//	along with CitpSharp.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Imp.CitpSharp.Packets
{
	internal class ClientInformationMessagePacket : CitpMsexPacket
	{
		public ClientInformationMessagePacket()
			: base(MsexMessageType.ClientInformationMessage) { }

		public List<MsexVersion> SupportedMsexVersions { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write((byte)SupportedMsexVersions.Count);
			foreach (var v in SupportedMsexVersions)
				writer.Write(v.GetCustomAttribute<CitpVersionAttribute>().ToByteArray());
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			byte supportedVersionsCount = reader.ReadByte();
			SupportedMsexVersions = new List<MsexVersion>(supportedVersionsCount);
			for (int i = 0; i < supportedVersionsCount; ++i)
			{
				byte versionMinor = reader.ReadByte();
				byte versionMajor = reader.ReadByte();

				if (versionMajor == 1 && versionMinor == 0)
					SupportedMsexVersions.Add(MsexVersion.Version1_0);
				else if (versionMajor == 1 && versionMinor == 1)
					SupportedMsexVersions.Add(MsexVersion.Version1_1);
				else if (versionMajor == 1 && versionMinor == 2)
					SupportedMsexVersions.Add(MsexVersion.Version1_2);
				else
					SupportedMsexVersions.Add(MsexVersion.UnsupportedVersion);
			}
		}
	}



	internal class ServerInformationMessagePacket : CitpMsexPacket
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

			if (Version == MsexVersion.Version1_0 || Version == MsexVersion.Version1_1)
			{
				writer.Write(ProductName);
				writer.Write(ProductVersionMajor);
				writer.Write(ProductVersionMinor);

				writer.Write((byte)LayerDmxSources.Count);
				foreach (var d in LayerDmxSources)
					writer.Write(d.ToUtf8ByteArray());
			}
			else if (Version == MsexVersion.Version1_2)
			{
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
					writer.Write(d.ToUtf8ByteArray());
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (Version == MsexVersion.Version1_0 || Version == MsexVersion.Version1_1)
			{
				ProductName = reader.ReadString();
				ProductVersionMajor = reader.ReadByte();
				ProductVersionMinor = reader.ReadByte();

				int dmxSourcesCount = reader.ReadByte();
				LayerDmxSources = new List<CitpDmxConnectionString>(dmxSourcesCount);
				for (int i = 0; i < dmxSourcesCount; ++i)
					LayerDmxSources.Add(CitpDmxConnectionString.Parse(reader.ReadString(true)));
			}
			else if (Version == MsexVersion.Version1_2)
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
		}
	}



	internal class NegativeAcknowledgeMessagePacket : CitpMsexPacket
	{
		public NegativeAcknowledgeMessagePacket()
			: base(MsexMessageType.NegativeAcknowledgeMessage) { }

		public MsexMessageType ReceivedContentType { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(ReceivedContentType.GetCustomAttribute<CitpId>().Id);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			ReceivedContentType = CitpEnumHelper.GetEnumFromIdString<MsexMessageType>(reader.ReadIdString());
		}
	}



	internal class LayerStatusMessagePacket : CitpMsexPacket
	{
		public LayerStatusMessagePacket()
			: base(MsexMessageType.LayerStatusMessage) { }

		public List<LayerStatus> LayerStatuses { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			if (Version == MsexVersion.Version1_0 || Version == MsexVersion.Version1_1)
			{
				writer.Write((byte)LayerStatuses.Count);
				foreach (var l in LayerStatuses)
				{
					writer.Write(l.LayerNumber);
					writer.Write(l.PhysicalOutput);
					writer.Write(l.MediaLibraryNumber);
					writer.Write(l.MediaNumber);
					writer.Write(l.MediaName);
					writer.Write(l.MediaPosition);
					writer.Write(l.MediaLength);
					writer.Write(l.MediaFps);
					writer.Write((uint)l.LayerStatusFlags);
				}
			}
			else if (Version == MsexVersion.Version1_2)
			{
				writer.Write((byte)LayerStatuses.Count);
				foreach (var l in LayerStatuses)
				{
					writer.Write(l.LayerNumber);
					writer.Write(l.PhysicalOutput);
					writer.Write((byte)l.MediaLibraryType);

					if (!l.MediaLibraryId.HasValue)
						throw new InvalidOperationException("MediaLibraryId has no value. Required for MSEX V1.2");

					writer.Write(l.MediaLibraryId.Value.ToByteArray());
					writer.Write(l.MediaNumber);
					writer.Write(l.MediaName);
					writer.Write(l.MediaPosition);
					writer.Write(l.MediaLength);
					writer.Write(l.MediaFps);
					writer.Write((uint)l.LayerStatusFlags);
				}
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (Version == MsexVersion.Version1_0 || Version == MsexVersion.Version1_1)
			{
				int layerStatusCount = reader.ReadByte();
				LayerStatuses = new List<LayerStatus>(layerStatusCount);
				for (int i = 0; i < layerStatusCount; ++i)
				{
					LayerStatuses.Add(new LayerStatus
					{
						LayerNumber = reader.ReadByte(),
						PhysicalOutput = reader.ReadByte(),
						MediaLibraryNumber = reader.ReadByte(),
						MediaNumber = reader.ReadByte(),
						MediaName = reader.ReadString(),
						MediaPosition = reader.ReadUInt32(),
						MediaLength = reader.ReadUInt32(),
						MediaFps = reader.ReadByte(),
						LayerStatusFlags = (MsexLayerStatusFlags)reader.ReadUInt32()
					});
				}
			}
			else if (Version == MsexVersion.Version1_2)
			{
				int layerStatusCount = reader.ReadByte();
				LayerStatuses = new List<LayerStatus>(layerStatusCount);
				for (int i = 0; i < layerStatusCount; ++i)
				{
					LayerStatuses.Add(new LayerStatus
					{
						LayerNumber = reader.ReadByte(),
						PhysicalOutput = reader.ReadByte(),
						MediaLibraryType = (MsexLibraryType)reader.ReadByte(),
						MediaLibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4)),
						MediaNumber = reader.ReadByte(),
						MediaName = reader.ReadString(),
						MediaPosition = reader.ReadUInt32(),
						MediaLength = reader.ReadUInt32(),
						MediaFps = reader.ReadByte(),
						LayerStatusFlags = (MsexLayerStatusFlags)reader.ReadUInt32()
					});
				}
			}
		}



		public class LayerStatus
		{
			public byte LayerNumber { get; set; }
			public byte PhysicalOutput { get; set; }

			public byte MediaLibraryNumber { get; set; }

			public MsexLibraryType MediaLibraryType { get; set; }
			public MsexLibraryId? MediaLibraryId { get; set; }

			public byte MediaNumber { get; set; }
			public string MediaName { get; set; }
			public uint MediaPosition { get; set; }
			public uint MediaLength { get; set; }
			public byte MediaFps { get; set; }

			public MsexLayerStatusFlags LayerStatusFlags { get; set; }
		}
	}



	internal class GetElementLibraryInformationMessagePacket : CitpMsexPacket
	{
		public GetElementLibraryInformationMessagePacket()
			: base(MsexMessageType.GetElementLibraryInformationMessage) { }

		public MsexLibraryType LibraryType { get; set; }
		public MsexLibraryId? LibraryParentId { get; set; }
		public bool ShouldRequestAllLibraries { get; set; }
		public List<byte> RequestedLibraryNumbers { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			if (Version == MsexVersion.Version1_0)
			{
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
			}
			else if (Version == MsexVersion.Version1_1)
			{
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
			}
			else if (Version == MsexVersion.Version1_2)
			{
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
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (Version == MsexVersion.Version1_0)
			{
				LibraryType = (MsexLibraryType)reader.ReadByte();

				int libraryNumberCount = reader.ReadByte();
				RequestedLibraryNumbers = new List<byte>(libraryNumberCount);
				for (int i = 0; i < libraryNumberCount; ++i)
					RequestedLibraryNumbers.Add(reader.ReadByte());

				if (libraryNumberCount == 0)
					ShouldRequestAllLibraries = true;
			}
			else if (Version == MsexVersion.Version1_1)
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
			else if (Version == MsexVersion.Version1_2)
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
		}
	}



	internal class ElementLibraryInformationMessagePacket : CitpMsexPacket
	{
		public ElementLibraryInformationMessagePacket()
			: base(MsexMessageType.ElementLibraryInformationMessage) { }

		public MsexLibraryType LibraryType { get; set; }

		public List<CitpElementLibraryInformation> Elements { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			if (Version == MsexVersion.Version1_0)
			{
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
			}
			else if (Version == MsexVersion.Version1_1)
			{
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
			}
			else if (Version == MsexVersion.Version1_2)
			{
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
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (Version == MsexVersion.Version1_0)
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
			else if (Version == MsexVersion.Version1_1)
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
			else if (Version == MsexVersion.Version1_2)
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
		}
	}



	internal class ElementLibraryUpdatedMessagePacket : CitpMsexPacket
	{
		public ElementLibraryUpdatedMessagePacket()
			: base(MsexMessageType.ElementLibraryUpdatedMessage) { }

		public MsexLibraryType LibraryType { get; set; }
		public byte LibraryNumber { get; set; }
		public MsexLibraryId? LibraryId { get; set; }

		public MsexElementLibraryUpdatedFlags UpdateFlags { get; set; }

		public List<byte> AffectedElements { get; set; }
		public List<byte> AffectedLibraries { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			if (Version == MsexVersion.Version1_0)
			{
				writer.Write((byte)LibraryType);
				writer.Write(LibraryNumber);

				writer.Write((byte)UpdateFlags);
			}
			else if (Version == MsexVersion.Version1_1)
			{
				writer.Write((byte)LibraryType);

				if (!LibraryId.HasValue)
					throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.1");

				writer.Write(LibraryId.Value.ToByteArray());

				writer.Write((byte)UpdateFlags);
			}
			else if (Version == MsexVersion.Version1_2)
			{
				writer.Write((byte)LibraryType);

				if (!LibraryId.HasValue)
					throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.2");

				writer.Write(LibraryId.Value.ToByteArray());

				writer.Write((byte)UpdateFlags);

				var affectedElements = new BitArray(256);
				foreach (byte a in AffectedElements)
					affectedElements[a] = true;
				var affectedElementsBytes = new byte[32];
				affectedElements.CopyTo(affectedElementsBytes, 0);
				writer.Write(affectedElementsBytes);

				var affectedLibraries = new BitArray(256);
				foreach (byte a in AffectedLibraries)
					affectedLibraries[a] = true;
				var affectedLibrariesBytes = new byte[32];
				affectedLibraries.CopyTo(affectedLibrariesBytes, 0);
				writer.Write(affectedLibrariesBytes);
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (Version == MsexVersion.Version1_0)
			{
				LibraryType = (MsexLibraryType)reader.ReadByte();
				LibraryNumber = reader.ReadByte();
				UpdateFlags = (MsexElementLibraryUpdatedFlags)reader.ReadByte();
			}
			else if (Version == MsexVersion.Version1_1)
			{
				LibraryType = (MsexLibraryType)reader.ReadByte();
				LibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));
				UpdateFlags = (MsexElementLibraryUpdatedFlags)reader.ReadByte();
			}
			else if (Version == MsexVersion.Version1_2)
			{
				LibraryType = (MsexLibraryType)reader.ReadByte();
				LibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));
				UpdateFlags = (MsexElementLibraryUpdatedFlags)reader.ReadByte();

				AffectedElements = new List<byte>();
				var affectedElementsArray = new BitArray(reader.ReadBytes(32));
				for (byte i = 0; i <= 255; ++i)
				{
					if (affectedElementsArray[i])
						AffectedElements.Add(i);
				}

				AffectedLibraries = new List<byte>();
				var affectedLibrariesArray = new BitArray(reader.ReadBytes(32));
				for (byte i = 0; i <= 255; ++i)
				{
					if (affectedLibrariesArray[i])
						AffectedLibraries.Add(i);
				}
			}
		}
	}



	internal class GetElementInformationMessagePacket : CitpMsexPacket
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

			if (Version == MsexVersion.Version1_0)
			{
				writer.Write((byte)LibraryType);
				writer.Write(LibraryNumber);

				if (ShouldRequestAllElements)
					writer.Write((byte)0x00);
				else
					writer.Write((byte)RequestedElementNumbers.Count);

				foreach (byte e in RequestedElementNumbers)
					writer.Write(e);
			}
			else if (Version == MsexVersion.Version1_1)
			{
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
			}
			else if (Version == MsexVersion.Version1_2)
			{
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
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (Version == MsexVersion.Version1_0)
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
			else if (Version == MsexVersion.Version1_1)
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
			else if (Version == MsexVersion.Version1_2)
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
		}
	}



	internal class MediaElementInformationMessagePacket : CitpMsexPacket
	{
		public MediaElementInformationMessagePacket()
			: base(MsexMessageType.MediaElementInformationMessage) { }

		public byte LibraryNumber { get; set; }
		public MsexLibraryId? LibraryId { get; set; }

		public List<CitpMediaInformation> Media { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			if (Version == MsexVersion.Version1_0)
			{
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
			}
			else if (Version == MsexVersion.Version1_1)
			{
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
			}
			else if (Version == MsexVersion.Version1_2)
			{
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
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (Version == MsexVersion.Version1_0)
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
			else if (Version == MsexVersion.Version1_1)
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
			else if (Version == MsexVersion.Version1_2)
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
		}
	}



	internal class EffectElementInformationMessagePacket : CitpMsexPacket
	{
		public EffectElementInformationMessagePacket()
			: base(MsexMessageType.EffectElementInformationMessage) { }

		public byte LibraryNumber { get; set; }
		public MsexLibraryId? LibraryId { get; set; }

		public List<CitpEffectInformation> Effects { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			if (Version == MsexVersion.Version1_0)
			{
				writer.Write(LibraryNumber);

				writer.Write((byte)Effects.Count);
				foreach (var e in Effects)
				{
					writer.Write(e.ElementNumber);
					writer.Write(e.DmxRangeMin);
					writer.Write(e.DmxRangeMax);
					writer.Write(e.Name);

					writer.Write((byte)e.EffectParameterNames.Count);
					foreach (string n in e.EffectParameterNames)
						writer.Write(n);
				}
			}
			else if (Version == MsexVersion.Version1_1)
			{
				if (!LibraryId.HasValue)
					throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.1");

				writer.Write(LibraryId.Value.ToByteArray());

				writer.Write((byte)Effects.Count);
				foreach (var e in Effects)
				{
					writer.Write(e.ElementNumber);
					writer.Write(e.DmxRangeMin);
					writer.Write(e.DmxRangeMax);
					writer.Write(e.Name);

					writer.Write((byte)e.EffectParameterNames.Count);
					foreach (string n in e.EffectParameterNames)
						writer.Write(n);
				}
			}
			else if (Version == MsexVersion.Version1_2)
			{
				if (!LibraryId.HasValue)
					throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.2");

				writer.Write(LibraryId.Value.ToByteArray());

				writer.Write((ushort)Effects.Count);
				foreach (var e in Effects)
				{
					writer.Write(e.ElementNumber);
					writer.Write(e.SerialNumber);
					writer.Write(e.DmxRangeMin);
					writer.Write(e.DmxRangeMax);
					writer.Write(e.Name);

					writer.Write((byte)e.EffectParameterNames.Count);
					foreach (string n in e.EffectParameterNames)
						writer.Write(n);
				}
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (Version == MsexVersion.Version1_0)
			{
				LibraryNumber = reader.ReadByte();

				int effectCount = reader.ReadByte();
				Effects = new List<CitpEffectInformation>(effectCount);
				for (int i = 0; i < effectCount; ++i)
				{
					var e = new CitpEffectInformation
					{
						ElementNumber = reader.ReadByte(),
						DmxRangeMin = reader.ReadByte(),
						DmxRangeMax = reader.ReadByte(),
						Name = reader.ReadString()
					};

					int effectParameterNameCount = reader.ReadByte();
					e.EffectParameterNames = new List<string>(effectParameterNameCount);
					for (int j = 0; j < effectParameterNameCount; ++j)
						e.EffectParameterNames.Add(reader.ReadString());

					Effects.Add(e);
				}
			}
			else if (Version == MsexVersion.Version1_1)
			{
				LibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));

				int effectCount = reader.ReadByte();
				Effects = new List<CitpEffectInformation>(effectCount);
				for (int i = 0; i < effectCount; ++i)
				{
					var e = new CitpEffectInformation
					{
						ElementNumber = reader.ReadByte(),
						DmxRangeMin = reader.ReadByte(),
						DmxRangeMax = reader.ReadByte(),
						Name = reader.ReadString()
					};

					int effectParameterNameCount = reader.ReadByte();
					e.EffectParameterNames = new List<string>(effectParameterNameCount);
					for (int j = 0; j < effectParameterNameCount; ++j)
						e.EffectParameterNames.Add(reader.ReadString());

					Effects.Add(e);
				}
			}
			else if (Version == MsexVersion.Version1_2)
			{
				LibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));

				int effectCount = reader.ReadUInt16();
				Effects = new List<CitpEffectInformation>(effectCount);
				for (int i = 0; i < effectCount; ++i)
				{
					var e = new CitpEffectInformation
					{
						ElementNumber = reader.ReadByte(),
						SerialNumber = reader.ReadUInt32(),
						DmxRangeMin = reader.ReadByte(),
						DmxRangeMax = reader.ReadByte(),
						Name = reader.ReadString()
					};

					int effectParameterNameCount = reader.ReadByte();
					e.EffectParameterNames = new List<string>(effectParameterNameCount);
					for (int j = 0; j < effectParameterNameCount; ++j)
						e.EffectParameterNames.Add(reader.ReadString());

					Effects.Add(e);
				}
			}
		}
	}



	internal class GenericElementInformationMessagePacket : CitpMsexPacket
	{
		public GenericElementInformationMessagePacket()
			: base(MsexMessageType.GenericElementInformationMessage) { }

		public MsexLibraryType LibraryType { get; set; }
		public MsexLibraryId LibraryId { get; set; }

		public List<CitpGenericInformation> Information { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			if (Version == MsexVersion.Version1_1)
			{
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
			}
			else if (Version == MsexVersion.Version1_2)
			{
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
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (Version == MsexVersion.Version1_1)
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
			else if (Version == MsexVersion.Version1_2)
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
		}
	}



	internal class GetElementLibraryThumbnailMessagePacket : CitpMsexPacket
	{
		public GetElementLibraryThumbnailMessagePacket()
			: base(MsexMessageType.GetElementLibraryThumbnailMessage) { }

		public MsexImageFormat ThumbnailFormat { get; set; }

		public int ThumbnailWidth { get; set; }
		public int ThumbnailHeight { get; set; }
		public MsexThumbnailFlags ThumbnailFlags { get; set; }

		public MsexLibraryType LibraryType { get; set; }

		public bool ShouldRequestAllThumbnails { get; set; }

		public List<byte> LibraryNumbers { get; set; }
		public List<MsexLibraryId> LibraryIds { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			if (Version == MsexVersion.Version1_0)
			{
				writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);
				writer.Write(ThumbnailWidth);
				writer.Write(ThumbnailHeight);

				writer.Write((byte)ThumbnailFlags);

				writer.Write((byte)LibraryType);

				if (ShouldRequestAllThumbnails)
				{
					writer.Write((byte)0x00);
				}
				else
				{
					writer.Write((byte)LibraryIds.Count);
					foreach (var l in LibraryIds)
						writer.Write(l.ToByteArray());
				}
			}
			else if (Version == MsexVersion.Version1_1)
			{
				writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);
				writer.Write(ThumbnailWidth);
				writer.Write(ThumbnailHeight);

				writer.Write((byte)ThumbnailFlags);

				writer.Write((byte)LibraryType);

				if (ShouldRequestAllThumbnails)
				{
					writer.Write((byte)0x00);
				}
				else
				{
					writer.Write((byte)LibraryIds.Count);
					foreach (var l in LibraryIds)
						writer.Write(l.ToByteArray());
				}
			}
			else if (Version == MsexVersion.Version1_2)
			{
				writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);
				writer.Write(ThumbnailWidth);
				writer.Write(ThumbnailHeight);

				writer.Write((byte)ThumbnailFlags);

				writer.Write((byte)LibraryType);

				if (ShouldRequestAllThumbnails)
				{
					writer.Write((ushort)0x0000);
				}
				else
				{
					writer.Write((ushort)LibraryIds.Count);
					foreach (var l in LibraryIds)
						writer.Write(l.ToByteArray());
				}
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (Version == MsexVersion.Version1_0)
			{
				ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
				ThumbnailWidth = reader.ReadUInt16();
				ThumbnailHeight = reader.ReadUInt16();
				ThumbnailFlags = (MsexThumbnailFlags)reader.ReadByte();
				LibraryType = (MsexLibraryType)reader.ReadByte();

				int libraryNumberCount = reader.ReadByte();
				LibraryNumbers = new List<byte>(libraryNumberCount);
				for (int i = 0; i < libraryNumberCount; ++i)
					LibraryNumbers.Add(reader.ReadByte());

				if (libraryNumberCount == 0)
					ShouldRequestAllThumbnails = true;
			}
			else if (Version == MsexVersion.Version1_1)
			{
				ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
				ThumbnailWidth = reader.ReadUInt16();
				ThumbnailHeight = reader.ReadUInt16();
				ThumbnailFlags = (MsexThumbnailFlags)reader.ReadByte();
				LibraryType = (MsexLibraryType)reader.ReadByte();

				int libraryIdCount = reader.ReadByte();
				LibraryIds = new List<MsexLibraryId>(libraryIdCount);
				for (int i = 0; i < libraryIdCount; ++i)
					LibraryIds.Add(MsexLibraryId.FromByteArray(reader.ReadBytes(4)));

				if (libraryIdCount == 0)
					ShouldRequestAllThumbnails = true;
			}
			else if (Version == MsexVersion.Version1_2)
			{
				ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
				ThumbnailWidth = reader.ReadUInt16();
				ThumbnailHeight = reader.ReadUInt16();
				ThumbnailFlags = (MsexThumbnailFlags)reader.ReadByte();
				LibraryType = (MsexLibraryType)reader.ReadByte();

				int libraryIdCount = reader.ReadUInt16();
				LibraryIds = new List<MsexLibraryId>(libraryIdCount);
				for (int i = 0; i < libraryIdCount; ++i)
					LibraryIds.Add(MsexLibraryId.FromByteArray(reader.ReadBytes(4)));

				if (libraryIdCount == 0)
					ShouldRequestAllThumbnails = true;
			}
		}
	}



	internal class ElementLibraryThumbnailMessagePacket : CitpMsexPacket
	{
		public ElementLibraryThumbnailMessagePacket()
			: base(MsexMessageType.ElementLibraryThumbnailMessage) { }

		public MsexLibraryType LibraryType { get; set; }
		public byte LibraryNumber { get; set; }
		public MsexLibraryId? LibraryId { get; set; }

		public MsexImageFormat ThumbnailFormat { get; set; }

		public ushort ThumbnailWidth { get; set; }
		public ushort ThumbnailHeight { get; set; }
		public byte[] ThumbnailBuffer { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			if (Version == MsexVersion.Version1_0)
			{
				writer.Write((byte)LibraryType);
				writer.Write(LibraryNumber);

				writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);

				writer.Write(ThumbnailWidth);
				writer.Write(ThumbnailHeight);
				writer.Write((ushort)ThumbnailBuffer.Length);
			}
			else if (Version == MsexVersion.Version1_1 || Version == MsexVersion.Version1_2)
			{
				writer.Write((byte)LibraryType);

				if (!LibraryId.HasValue)
					throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.1 & V1.2");

				writer.Write(LibraryId.Value.ToByteArray());

				writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);

				writer.Write(ThumbnailWidth);
				writer.Write(ThumbnailHeight);
				writer.Write((ushort)ThumbnailBuffer.Length);
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (Version == MsexVersion.Version1_0)
			{
				LibraryType = (MsexLibraryType)reader.ReadByte();
				LibraryNumber = reader.ReadByte();

				ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());

				ThumbnailWidth = reader.ReadUInt16();
				ThumbnailHeight = reader.ReadUInt16();

				int thumbnailBufferLength = reader.ReadUInt16();
				ThumbnailBuffer = reader.ReadBytes(thumbnailBufferLength);
			}
			else if (Version == MsexVersion.Version1_1 || Version == MsexVersion.Version1_2)
			{
				LibraryType = (MsexLibraryType)reader.ReadByte();
				LibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));

				ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());

				ThumbnailWidth = reader.ReadUInt16();
				ThumbnailHeight = reader.ReadUInt16();

				int thumbnailBufferLength = reader.ReadUInt16();
				ThumbnailBuffer = reader.ReadBytes(thumbnailBufferLength);
			}
		}
	}



	internal class GetElementThumbnailMessagePacket : CitpMsexPacket
	{
		public GetElementThumbnailMessagePacket()
			: base(MsexMessageType.GetElementThumbnailMessage) { }

		public MsexImageFormat ThumbnailFormat { get; set; }

		public int ThumbnailWidth { get; set; }
		public int ThumbnailHeight { get; set; }
		public MsexThumbnailFlags ThumbnailFlags { get; set; }

		public MsexLibraryType LibraryType { get; set; }
		public byte LibraryNumber { get; set; }
		public MsexLibraryId? LibraryId { get; set; }

		public bool ShouldRequestAllThumbnails { get; set; }

		public List<byte> ElementNumbers { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			if (Version == MsexVersion.Version1_0)
			{
				writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);

				writer.Write(ThumbnailWidth);
				writer.Write(ThumbnailHeight);

				writer.Write((byte)ThumbnailFlags);

				writer.Write((byte)LibraryType);
				writer.Write(LibraryNumber);

				if (ShouldRequestAllThumbnails)
				{
					writer.Write((byte)0x00);
				}
				else
				{
					writer.Write((byte)ElementNumbers.Count);
					foreach (byte e in ElementNumbers)
						writer.Write(e);
				}
			}
			else if (Version == MsexVersion.Version1_1)
			{
				writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);

				writer.Write(ThumbnailWidth);
				writer.Write(ThumbnailHeight);

				writer.Write((byte)ThumbnailFlags);

				writer.Write((byte)LibraryType);

				if (!LibraryId.HasValue)
					throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.1");

				writer.Write(LibraryId.Value.ToByteArray());

				if (ShouldRequestAllThumbnails)
				{
					writer.Write((byte)0x00);
				}
				else
				{
					writer.Write((byte)ElementNumbers.Count);
					foreach (byte e in ElementNumbers)
						writer.Write(e);
				}
			}
			else if (Version == MsexVersion.Version1_2)
			{
				writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);

				writer.Write(ThumbnailWidth);
				writer.Write(ThumbnailHeight);

				writer.Write((byte)ThumbnailFlags);

				writer.Write((byte)LibraryType);

				if (!LibraryId.HasValue)
					throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.2");

				writer.Write(LibraryId.Value.ToByteArray());

				if (ShouldRequestAllThumbnails)
				{
					writer.Write((ushort)0x0000);
				}
				else
				{
					writer.Write((ushort)ElementNumbers.Count);
					foreach (byte e in ElementNumbers)
						writer.Write(e);
				}
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (Version == MsexVersion.Version1_0)
			{
				ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
				ThumbnailWidth = reader.ReadUInt16();
				ThumbnailHeight = reader.ReadUInt16();
				ThumbnailFlags = (MsexThumbnailFlags)reader.ReadByte();
				LibraryType = (MsexLibraryType)reader.ReadByte();
				LibraryNumber = reader.ReadByte();

				int elementNumberCount = reader.ReadByte();
				ElementNumbers = new List<byte>(elementNumberCount);
				for (int i = 0; i < elementNumberCount; ++i)
					ElementNumbers.Add(reader.ReadByte());

				if (elementNumberCount == 0)
					ShouldRequestAllThumbnails = true;
			}
			else if (Version == MsexVersion.Version1_1)
			{
				ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
				ThumbnailWidth = reader.ReadUInt16();
				ThumbnailHeight = reader.ReadUInt16();
				ThumbnailFlags = (MsexThumbnailFlags)reader.ReadByte();
				LibraryType = (MsexLibraryType)reader.ReadByte();
				LibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));

				int elementNumberCount = reader.ReadByte();
				ElementNumbers = new List<byte>(elementNumberCount);
				for (int i = 0; i < elementNumberCount; ++i)
					ElementNumbers.Add(reader.ReadByte());

				if (elementNumberCount == 0)
					ShouldRequestAllThumbnails = true;
			}
			else if (Version == MsexVersion.Version1_2)
			{
				ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
				ThumbnailWidth = reader.ReadUInt16();
				ThumbnailHeight = reader.ReadUInt16();
				ThumbnailFlags = (MsexThumbnailFlags)reader.ReadByte();
				LibraryType = (MsexLibraryType)reader.ReadByte();
				LibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));

				int elementNumberCount = reader.ReadUInt16();
				ElementNumbers = new List<byte>(elementNumberCount);
				for (int i = 0; i < elementNumberCount; ++i)
					ElementNumbers.Add(reader.ReadByte());

				if (elementNumberCount == 0)
					ShouldRequestAllThumbnails = true;
			}
		}
	}



	internal class ElementThumbnailMessagePacket : CitpMsexPacket
	{
		public ElementThumbnailMessagePacket()
			: base(MsexMessageType.ElementThumbnailMessage) { }

		public MsexLibraryType LibraryType { get; set; }
		public byte LibraryNumber { get; set; }
		public MsexLibraryId? LibraryId { get; set; }

		public byte ElementNumber { get; set; }

		public MsexImageFormat ThumbnailFormat { get; set; }
		public ushort ThumbnailWidth { get; set; }
		public ushort ThumbnailHeight { get; set; }
		public byte[] ThumbnailBuffer { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			if (Version == MsexVersion.Version1_0)
			{
				writer.Write((byte)LibraryType);
				writer.Write(LibraryNumber);
				writer.Write(ElementNumber);
				writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);
				writer.Write(ThumbnailWidth);
				writer.Write(ThumbnailHeight);
				writer.Write((ushort)ThumbnailBuffer.Length);
				writer.Write(ThumbnailBuffer);
			}
			else if (Version == MsexVersion.Version1_1 || Version == MsexVersion.Version1_2)
			{
				writer.Write((byte)LibraryType);

				if (!LibraryId.HasValue)
					throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.1 & V1.2");

				writer.Write(LibraryId.Value.ToByteArray());
				writer.Write(ElementNumber);
				writer.Write(ThumbnailFormat.GetCustomAttribute<CitpId>().Id);
				writer.Write(ThumbnailWidth);
				writer.Write(ThumbnailHeight);
				writer.Write((ushort)ThumbnailBuffer.Length);
				writer.Write(ThumbnailBuffer);
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (Version == MsexVersion.Version1_0)
			{
				LibraryType = (MsexLibraryType)reader.ReadByte();
				LibraryNumber = reader.ReadByte();
				ElementNumber = reader.ReadByte();

				ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());

				ThumbnailWidth = reader.ReadUInt16();
				ThumbnailHeight = reader.ReadUInt16();

				int thumbnailBufferLength = reader.ReadUInt16();
				ThumbnailBuffer = reader.ReadBytes(thumbnailBufferLength);
			}
			else if (Version == MsexVersion.Version1_1 || Version == MsexVersion.Version1_2)
			{
				LibraryType = (MsexLibraryType)reader.ReadByte();
				LibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));
				ElementNumber = reader.ReadByte();

				ThumbnailFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());

				ThumbnailWidth = reader.ReadUInt16();
				ThumbnailHeight = reader.ReadUInt16();

				int thumbnailBufferLength = reader.ReadUInt16();
				ThumbnailBuffer = reader.ReadBytes(thumbnailBufferLength);
			}
		}
	}



	internal class GetVideoSourcesMessagePacket : CitpMsexPacket
	{
		public GetVideoSourcesMessagePacket()
			: base(MsexMessageType.GetVideoSourcesMessage) { }
	}



	internal class VideoSourcesMessagePacket : CitpMsexPacket
	{
		public VideoSourcesMessagePacket()
			: base(MsexMessageType.VideoSourcesMessage) { }

		public List<CitpVideoSourceInformation> Sources { get; set; }


		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write((ushort)Sources.Count);
			foreach (var s in Sources)
			{
				writer.Write(s.SourceIdentifier);
				writer.Write(s.SourceName);

				if (s.PhysicalOutput.HasValue)
					writer.Write(s.PhysicalOutput.Value);
				else
					writer.Write((byte)0xFF);

				if (s.LayerNumber.HasValue)
					writer.Write(s.LayerNumber.Value);
				else
					writer.Write((byte)0xFF);

				writer.Write((ushort)s.Flags);

				writer.Write(s.Width);
				writer.Write(s.Height);
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			int sourcesCount = reader.ReadUInt16();
			Sources = new List<CitpVideoSourceInformation>(sourcesCount);
			for (int i = 0; i < sourcesCount; ++i)
			{
				var s = new CitpVideoSourceInformation
				{
					SourceIdentifier = reader.ReadUInt16(),
					SourceName = reader.ReadString(),
					PhysicalOutput = reader.ReadByte(),
					LayerNumber = reader.ReadByte(),
					Flags = (MsexVideoSourcesFlags)reader.ReadUInt16(),
					Width = reader.ReadUInt16(),
					Height = reader.ReadUInt16()
				};

				Sources.Add(s);
			}
		}
	}



	internal class RequestStreamMessagePacket : CitpMsexPacket
	{
		public RequestStreamMessagePacket()
			: base(MsexMessageType.RequestStreamMessage) { }

		public ushort SourceIdentifier { get; set; }
		public MsexImageFormat FrameFormat { get; set; }

		public ushort FrameWidth { get; set; }
		public ushort FrameHeight { get; set; }
		public byte Fps { get; set; }
		public byte Timeout { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(SourceIdentifier);
			writer.Write(FrameFormat.GetCustomAttribute<CitpId>().Id);
			writer.Write(FrameWidth);
			writer.Write(FrameHeight);
			writer.Write(Fps);
			writer.Write(Timeout);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			SourceIdentifier = reader.ReadUInt16();
			FrameFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
			FrameWidth = reader.ReadUInt16();
			FrameHeight = reader.ReadUInt16();
			Fps = reader.ReadByte();
			Timeout = reader.ReadByte();
		}
	}



	internal class StreamFrameMessagePacket : CitpMsexPacket
	{
		public StreamFrameMessagePacket()
			: base(MsexMessageType.StreamFrameMessage) { }

		public Guid MediaServerUuid { get; set; }
		public ushort SourceIdentifier { get; set; }
		public MsexImageFormat FrameFormat { get; set; }
		public ushort FrameWidth { get; set; }
		public ushort FrameHeight { get; set; }
		public byte[] FrameBuffer { get; set; }
		public FragmentPreamble FragmentInfo { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			if (Version == MsexVersion.Version1_0 || Version == MsexVersion.Version1_1)
			{
				writer.Write(SourceIdentifier);
				writer.Write(FrameFormat.GetCustomAttribute<CitpId>().Id);
				writer.Write(FrameWidth);
				writer.Write(FrameHeight);
				writer.Write((ushort)FrameBuffer.Length);
				writer.Write(FrameBuffer);
			}
			else if (Version == MsexVersion.Version1_2)
			{
				writer.Write(MediaServerUuid);
				writer.Write(SourceIdentifier);
				writer.Write(FrameFormat.GetCustomAttribute<CitpId>().Id);
				writer.Write(FrameWidth);
				writer.Write(FrameHeight);
				
				if (FrameFormat == MsexImageFormat.FragmentedJpeg || FrameFormat == MsexImageFormat.FragmentedPng)
				{
					if (FragmentInfo == null)
						throw new InvalidOperationException("FragmentInfo must be set when sending a fragmented image format");

					writer.Write((ushort)(FrameBuffer.Length + FragmentPreamble.ByteLength));

					writer.Write(FragmentInfo.FrameIndex);
					writer.Write(FragmentInfo.FragmentCount);
					writer.Write(FragmentInfo.FragmentIndex);
					writer.Write(FragmentInfo.FragmentByteOffset);
				}
				else
				{
					writer.Write((ushort)FrameBuffer.Length);
				}

				writer.Write(FrameBuffer);
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (Version == MsexVersion.Version1_0 || Version == MsexVersion.Version1_1)
			{
				SourceIdentifier = reader.ReadUInt16();
				FrameFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
				FrameWidth = reader.ReadUInt16();
				FrameHeight = reader.ReadUInt16();

				int frameBufferLength = reader.ReadUInt16();
				FrameBuffer = reader.ReadBytes(frameBufferLength);
			}
			else if (Version == MsexVersion.Version1_2)
			{
				MediaServerUuid = reader.ReadGuid();
				SourceIdentifier = reader.ReadUInt16();
				FrameFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
				FrameWidth = reader.ReadUInt16();
				FrameHeight = reader.ReadUInt16();

				int frameBufferLength = reader.ReadUInt16();
				FrameBuffer = reader.ReadBytes(frameBufferLength);
			}
		}


		public class FragmentPreamble
		{
			public const int ByteLength = 12;

			public uint FrameIndex { get; set; }
			public ushort FragmentCount { get; set; } 
			public ushort FragmentIndex { get; set; }
			public uint FragmentByteOffset { get; set; }
		}
	}
}