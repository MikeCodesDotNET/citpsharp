using System;
using System.Text;

namespace Imp.CitpSharp.Packets
{
	internal abstract class SdmxPacket : CitpPacket
	{
		public static readonly int CitpMessageTypePosition = 20;

		protected SdmxPacket(SdmxMessageType messageType)
			: base(CitpLayerType.SendDmxLayer)
		{
			MessageType = messageType;
		}

		public SdmxMessageType MessageType { get; }

		public static SdmxMessageType? GetMessageType(byte[] data)
		{
			string typeString = Encoding.UTF8.GetString(data, CitpMessageTypePosition, 4);
			return CitpEnumHelper.GetEnumFromIdString<SdmxMessageType>(typeString);
		}

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(MessageType.GetCustomAttribute<CitpId>().Id);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (MessageType != CitpEnumHelper.GetEnumFromIdString<SdmxMessageType>(reader.ReadIdString()))
				throw new InvalidOperationException("Incorrect message type");
		}
	}
}