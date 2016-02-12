namespace Imp.CitpSharp.Packets.Msex
{
	internal class NegativeAcknowledgeMessagePacket : MsexPacket
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
}