namespace Imp.CitpSharp.Packets.Sdmx
{
	internal class SetExternalSourcePacket : SdmxPacket
	{
		public SetExternalSourcePacket()
			: base(SdmxMessageType.SetExternalSourceMessage) { }

		public DmxPatchInfo PatchInfo { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(PatchInfo, true);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			PatchInfo = DmxPatchInfo.Parse(reader.ReadString(true));
		}
	}
}