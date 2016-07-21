using System;
using System.Collections.Generic;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class EffectElementInformationPacket : MsexPacket
	{
		public EffectElementInformationPacket()
			: base(MsexMessageType.EffectElementInformationMessage) { }

		public byte LibraryNumber { get; set; }
		public MsexLibraryId? LibraryId { get; set; }

		public List<CitpEffectInformation> Effects { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			switch (Version)
			{
				case MsexVersion.Version1_0:
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
					break;

				case MsexVersion.Version1_1:
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
					break;

				case MsexVersion.Version1_2:
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
					break;

				case MsexVersion.Version1_1:
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
					break;

				case MsexVersion.Version1_2:
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
					break;
			}
		}
	}
}