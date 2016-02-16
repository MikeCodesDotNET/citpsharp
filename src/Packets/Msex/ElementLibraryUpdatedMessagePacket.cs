using System;
using System.Collections;
using System.Collections.Generic;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class ElementLibraryUpdatedMessagePacket : MsexPacket
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

			switch (Version)
			{
				case MsexVersion.Version1_0:
					writer.Write((byte)LibraryType);
					writer.Write(LibraryNumber);

					writer.Write((byte)UpdateFlags);
					break;

				case MsexVersion.Version1_1:
					writer.Write((byte)LibraryType);

					if (!LibraryId.HasValue)
						throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.1");

					writer.Write(LibraryId.Value.ToByteArray());

					writer.Write((byte)UpdateFlags);
					break;

				case MsexVersion.Version1_2:
					writer.Write((byte)LibraryType);

					if (!LibraryId.HasValue)
						throw new InvalidOperationException("LibraryId has no value. Required for MSEX V1.2");

					writer.Write(LibraryId.Value.ToByteArray());

					writer.Write((byte)UpdateFlags);

					var affectedElements = new BitArray(256);
					foreach (byte a in AffectedElements)
						affectedElements[a] = true;
					var affectedElementsBytes = new byte[32];
					((ICollection)affectedElements).CopyTo(affectedElementsBytes, 0);
					writer.Write(affectedElementsBytes);

					var affectedLibraries = new BitArray(256);
					foreach (byte a in AffectedLibraries)
						affectedLibraries[a] = true;
					var affectedLibrariesBytes = new byte[32];
					((ICollection)affectedLibraries).CopyTo(affectedLibrariesBytes, 0);
					writer.Write(affectedLibrariesBytes);
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
					UpdateFlags = (MsexElementLibraryUpdatedFlags)reader.ReadByte();
					break;

				case MsexVersion.Version1_1:
					LibraryType = (MsexLibraryType)reader.ReadByte();
					LibraryId = MsexLibraryId.FromByteArray(reader.ReadBytes(4));
					UpdateFlags = (MsexElementLibraryUpdatedFlags)reader.ReadByte();
					break;

				case MsexVersion.Version1_2:
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
					break;
			}
		}
	}
}