namespace Imp.CitpSharp.Packets.Sdmx
{
	internal class SetExternalUniverseSourcePacket : SdmxPacket
	{
		public SetExternalUniverseSourcePacket()
			: base(SdmxMessageType.SetExternalUniverseSourceMessage) { }

		public byte UniverseIndex { get; set; }
		public DmxPatchInfo PatchInfo { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(UniverseIndex);
			writer.Write(PatchInfo, true);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			UniverseIndex = reader.ReadByte();
			PatchInfo = DmxPatchInfo.Parse(reader.ReadString(true));
		}
	}
}