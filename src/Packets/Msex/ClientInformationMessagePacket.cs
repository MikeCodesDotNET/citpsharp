using System.Collections.Generic;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class ClientInformationMessagePacket : MsexPacket
	{
		public ClientInformationMessagePacket()
			: base(MsexMessageType.ClientInformationMessage) { }

		public List<MsexVersion> SupportedMsexVersions { get; set; }

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write((byte)SupportedMsexVersions.Count);
			foreach (var v in SupportedMsexVersions)
				writer.Write(v.GetCustomAttribute<CitpVersionAttribute>().ToByteArray());
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			byte supportedVersionsCount = reader.ReadByte();
			SupportedMsexVersions = new List<MsexVersion>(supportedVersionsCount);
			for (int i = 0; i < supportedVersionsCount; ++i)
			{
				byte versionMajor = reader.ReadByte();
				byte versionMinor = reader.ReadByte();

				if (versionMajor == 1 && versionMinor == 0)
					SupportedMsexVersions.Add(MsexVersion.Version1_0);
				else if (versionMajor == 1 && versionMinor == 1)
					SupportedMsexVersions.Add(MsexVersion.Version1_1);
				else if (versionMajor == 1 && versionMinor == 2)
					SupportedMsexVersions.Add(MsexVersion.Version1_2);
				else
					SupportedMsexVersions.Add(MsexVersion.UnsupportedVersion);
			}
		}
	}
}