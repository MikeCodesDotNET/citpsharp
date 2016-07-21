using System.Collections.Generic;

namespace Imp.CitpSharp.Packets.Msex
{
	internal class VideoSourcesPacket : MsexPacket
	{
		public VideoSourcesPacket()
			: base(MsexMessageType.VideoSourcesMessage) { }

		public List<CitpVideoSourceInformation> Sources { get; set; }


		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write((ushort)Sources.Count);
			foreach (var s in Sources)
			{
				writer.Write(s.SourceIdentifier);
				writer.Write(s.SourceName);

				if (s.PhysicalOutput.HasValue)
					writer.Write(s.PhysicalOutput.Value);
				else
					writer.Write((byte)0xFF);

				if (s.LayerNumber.HasValue)
					writer.Write(s.LayerNumber.Value);
				else
					writer.Write((byte)0xFF);

				writer.Write((ushort)s.Flags);

				writer.Write(s.Width);
				writer.Write(s.Height);
			}
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			int sourcesCount = reader.ReadUInt16();
			Sources = new List<CitpVideoSourceInformation>(sourcesCount);
			for (int i = 0; i < sourcesCount; ++i)
			{
				var s = new CitpVideoSourceInformation
				{
					SourceIdentifier = reader.ReadUInt16(),
					SourceName = reader.ReadString(),
					PhysicalOutput = reader.ReadByte(),
					LayerNumber = reader.ReadByte(),
					Flags = (MsexVideoSourcesFlags)reader.ReadUInt16(),
					Width = reader.ReadUInt16(),
					Height = reader.ReadUInt16()
				};

				Sources.Add(s);
			}
		}
	}
}