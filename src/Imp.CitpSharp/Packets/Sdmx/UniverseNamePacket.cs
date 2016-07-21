namespace Imp.CitpSharp.Packets.Sdmx
{
	internal class UniverseNamePacket : SdmxPacket
	{
		public UniverseNamePacket()
			: base(SdmxMessageType.UniverseNameMessage) { }

		public byte UniverseIndex { get; set; }
		public string UniverseName { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(UniverseIndex);
			writer.Write(UniverseName, true);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			UniverseIndex = reader.ReadByte();
			UniverseName = reader.ReadString(true);
		}
	}
}