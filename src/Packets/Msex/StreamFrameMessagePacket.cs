using System;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class StreamFrameMessagePacket : MsexPacket
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

			switch (Version)
			{
				case MsexVersion.Version1_0:
				case MsexVersion.Version1_1:
					writer.Write(SourceIdentifier);
					writer.Write(FrameFormat.GetCustomAttribute<CitpId>().Id);
					writer.Write(FrameWidth);
					writer.Write(FrameHeight);
					writer.Write((ushort)FrameBuffer.Length);
					writer.Write(FrameBuffer);
					break;

				case MsexVersion.Version1_2:
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
					SourceIdentifier = reader.ReadUInt16();
					FrameFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
					FrameWidth = reader.ReadUInt16();
					FrameHeight = reader.ReadUInt16();

					int frameBufferLength = reader.ReadUInt16();
					FrameBuffer = reader.ReadBytes(frameBufferLength);
				}
					break;

				case MsexVersion.Version1_2:
				{
					MediaServerUuid = reader.ReadGuid();
					SourceIdentifier = reader.ReadUInt16();
					FrameFormat = CitpEnumHelper.GetEnumFromIdString<MsexImageFormat>(reader.ReadIdString());
					FrameWidth = reader.ReadUInt16();
					FrameHeight = reader.ReadUInt16();

					int frameBufferLength = reader.ReadUInt16();
					FrameBuffer = reader.ReadBytes(frameBufferLength);
				}
					break;
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