namespace Imp.CitpSharp.Packets.Msex
{
	internal class RequestStreamMessagePacket : MsexPacket
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
}