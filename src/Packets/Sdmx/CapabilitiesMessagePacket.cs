using System.Collections.Generic;

namespace Imp.CitpSharp.Packets.Sdmx
{
	internal class CapabilitiesMessagePacket : SdmxPacket
	{
		public CapabilitiesMessagePacket()
			: base(SdmxMessageType.CapabilitiesMessage) { }

		public List<SdmxCapability> Capabilities { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write((ushort)Capabilities.Count);
			foreach (var c in Capabilities)
				writer.Write((ushort)c);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			ushort capabilitiesCount = reader.ReadUInt16();
			Capabilities = new List<SdmxCapability>(capabilitiesCount);
			for (int i = 0; i < capabilitiesCount; ++i)
				Capabilities.Add((SdmxCapability)reader.ReadUInt16());
		}
	}
}