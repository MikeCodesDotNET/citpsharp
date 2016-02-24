namespace Imp.CitpSharp.Packets.Sdmx
{
	internal class SetExternalSourceMessagePacket : SdmxPacket
	{
		public SetExternalSourceMessagePacket()
			: base(SdmxMessageType.SetExternalSourceMessage) { }

		public CitpDmxConnectionString ConnectionString { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(ConnectionString.ToString(), true);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			ConnectionString = CitpDmxConnectionString.Parse(reader.ReadString(true));
		}
	}
}