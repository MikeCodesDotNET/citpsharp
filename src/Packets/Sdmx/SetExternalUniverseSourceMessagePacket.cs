namespace Imp.CitpSharp.Packets.Sdmx
{
	internal class SetExternalUniverseSourceMessagePacket : SdmxPacket
	{
		public SetExternalUniverseSourceMessagePacket()
			: base(SdmxMessageType.SetExternalUniverseSourceMessage) { }

		public byte UniverseIndex { get; set; }
		public CitpDmxConnectionString ConnectionString { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(UniverseIndex);
			writer.Write(ConnectionString.ToUtf8ByteArray());
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			UniverseIndex = reader.ReadByte();
			ConnectionString = CitpDmxConnectionString.Parse(reader.ReadString(true));
		}
	}
}