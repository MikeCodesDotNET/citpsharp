namespace Imp.CitpSharp.Packets.Pinf
{
	internal class PeerNameMessagePacket : PinfPacket
	{
		public PeerNameMessagePacket()
			: base(PinfMessageType.PeerNameMessage) { }

		public string Name { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(Name, true);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			Name = reader.ReadString(true);
		}
	}
}