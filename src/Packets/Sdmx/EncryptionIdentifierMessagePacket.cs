namespace Imp.CitpSharp.Packets.Sdmx
{
	internal class EncryptionIdentifierMessagePacket : SdmxPacket
	{
		public EncryptionIdentifierMessagePacket()
			: base(SdmxMessageType.EncryptionIdentifierMessage) { }

		public string Identifier { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(Identifier, true);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			Identifier = reader.ReadString(true);
		}
	}
}