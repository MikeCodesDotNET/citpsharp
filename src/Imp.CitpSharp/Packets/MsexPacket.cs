using System;
using System.Text;

namespace Imp.CitpSharp.Packets
{
	internal abstract class MsexPacket : CitpPacket
	{
		public static readonly int CitpMessageTypePosition = 22;

		protected MsexPacket(MsexMessageType messageType)
			: base(CitpLayerType.MediaServerExtensionsLayer)
		{
			Version = null;
			MessageType = messageType;
		}

		public MsexMessageType MessageType { get; }

		public MsexVersion? Version { get; set; }

		public static MsexMessageType? GetMessageType(byte[] data)
		{
			string typeString = Encoding.UTF8.GetString(data, CitpMessageTypePosition, 4);
			return CitpEnumHelper.GetEnumFromIdString<MsexMessageType>(typeString);
		}

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			if (!Version.HasValue)
				throw new InvalidOperationException("Version has no value. Required for MSEX packets");

			writer.Write(Version.Value, false);
			writer.Write(MessageType.GetCustomAttribute<CitpId>().Id);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			Version = reader.ReadMsexVersion(false);

			if (Version == MsexVersion.UnsupportedVersion)
				throw new InvalidOperationException("Incorrect or invalid MSEX version");

			if (MessageType != CitpEnumHelper.GetEnumFromIdString<MsexMessageType>(reader.ReadIdString()))
				throw new InvalidOperationException("Incorrect or invalid message type");
		}
	}
}