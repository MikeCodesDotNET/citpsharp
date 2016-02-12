using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Imp.CitpSharp.Packets.Msex;
using Imp.CitpSharp.Packets.Pinf;

namespace Imp.CitpSharp.Packets
{
	internal abstract class CitpPacket
	{
		private static readonly byte[] CitpCookie = Encoding.UTF8.GetBytes("CITP");
		private static readonly byte CitpVersionMajor = 1;
		private static readonly byte CitpVersionMinor = 0;

		private static readonly int CitpHeaderLength = 20;
		private static readonly int CitpContentTypePosition = 16;


		protected CitpPacket(CitpLayerType layerType)
		{
			LayerType = layerType;
			MessagePart = 1;
			MessagePartCount = 1;
		}

		public CitpLayerType LayerType { get; }

		public ushort RequestResponseIndex { get; set; }
		public ushort MessagePartCount { get; set; }
		public ushort MessagePart { get; set; }

		public static CitpPacket FromByteArray(byte[] data)
		{
			CitpPacket packet;

			var layerType = getLayerType(data);

			if (layerType == null)
			{
				var layerTypeArray = new byte[4];
				Buffer.BlockCopy(data, CitpContentTypePosition, layerTypeArray, 0, 4);
				throw new InvalidOperationException(
					$"Unrecognised CITP content type: {Encoding.UTF8.GetString(layerTypeArray, 0, layerTypeArray.Length)}");
			}

			switch (layerType)
			{
				case CitpLayerType.PeerInformationLayer:
				{
					var messageType = PinfPacket.GetMessageType(data);

					if (messageType == null)
					{
						var messageTypeArray = new byte[4];
						Buffer.BlockCopy(data, PinfPacket.CitpMessageTypePosition, messageTypeArray, 0, 4);
						throw new InvalidOperationException(
							$"Unrecognised PING message type: {Encoding.UTF8.GetString(messageTypeArray, 0, messageTypeArray.Length)}");
					}

					switch (messageType)
					{
						case PinfMessageType.PeerLocationMessage:
							packet = new PeerLocationMessagePacket();
							break;
						case PinfMessageType.PeerNameMessage:
							packet = new PeerNameMessagePacket();
							break;
						default:
							throw new NotImplementedException("Unimplemented PINF message type");
					}

					break;
				}

				case CitpLayerType.MediaServerExtensionsLayer:
				{
					var messageType = MsexPacket.GetMessageType(data);

					if (messageType == null)
					{
						var messageTypeArray = new byte[4];
						Buffer.BlockCopy(data, MsexPacket.CitpMessageTypePosition, messageTypeArray, 0, 4);
						throw new InvalidOperationException(
							$"Unrecognised MSEX message type: {Encoding.UTF8.GetString(messageTypeArray, 0, messageTypeArray.Length)}");
					}

					switch (messageType)
					{
						case MsexMessageType.ClientInformationMessage:
							packet = new ClientInformationMessagePacket();
							break;
						case MsexMessageType.ServerInformationMessage:
							packet = new ServerInformationMessagePacket();
							break;
						case MsexMessageType.NegativeAcknowledgeMessage:
							packet = new NegativeAcknowledgeMessagePacket();
							break;
						case MsexMessageType.LayerStatusMessage:
							packet = new LayerStatusMessagePacket();
							break;
						case MsexMessageType.GetElementLibraryInformationMessage:
							packet = new GetElementLibraryInformationMessagePacket();
							break;
						case MsexMessageType.ElementLibraryInformationMessage:
							packet = new ElementLibraryInformationMessagePacket();
							break;
						case MsexMessageType.ElementLibraryUpdatedMessage:
							packet = new ElementLibraryUpdatedMessagePacket();
							break;
						case MsexMessageType.GetElementInformationMessage:
							packet = new GetElementInformationMessagePacket();
							break;
						case MsexMessageType.MediaElementInformationMessage:
							packet = new MediaElementInformationMessagePacket();
							break;
						case MsexMessageType.EffectElementInformationMessage:
							packet = new EffectElementInformationMessagePacket();
							break;
						case MsexMessageType.GenericElementInformationMessage:
							packet = new GenericElementInformationMessagePacket();
							break;
						case MsexMessageType.GetElementLibraryThumbnailMessage:
							packet = new GetElementLibraryThumbnailMessagePacket();
							break;
						case MsexMessageType.ElementLibraryThumbnailMessage:
							packet = new ElementLibraryThumbnailMessagePacket();
							break;
						case MsexMessageType.GetElementThumbnailMessage:
							packet = new GetElementThumbnailMessagePacket();
							break;
						case MsexMessageType.ElementThumbnailMessage:
							packet = new ElementThumbnailMessagePacket();
							break;
						case MsexMessageType.GetVideoSourcesMessage:
							packet = new GetVideoSourcesMessagePacket();
							break;
						case MsexMessageType.RequestStreamMessage:
							packet = new RequestStreamMessagePacket();
							break;
						case MsexMessageType.StreamFrameMessage:
							packet = new StreamFrameMessagePacket();
							break;
						default:
							throw new NotImplementedException("Unimplemented MSEX message type");
					}

					break;
				}

				default:
					throw new NotImplementedException("Unimplemented CITP content type");
			}



			using (var reader = new CitpBinaryReader(new MemoryStream(data)))
				packet.DeserializeFromStream(reader);

			return packet;
		}

		private static CitpLayerType? getLayerType(byte[] data)
		{
			string typeString = Encoding.UTF8.GetString(data, CitpContentTypePosition, 4);
			return CitpEnumHelper.GetEnumFromIdString<CitpLayerType>(typeString);
		}


		public byte[] ToByteArray()
		{
			return serializePacket();
		}

		public IEnumerable<byte[]> ToByteArray(int maximumPacketSize, int requestResponseIndex = 0)
		{
			var packets = new List<byte[]>();

			var fullData = serializePacket(false);

			int maximumDataSize = maximumPacketSize - CitpHeaderLength;

			int numPackets = (int)Math.Ceiling(fullData.Length / (float)maximumDataSize);

			for (int i = 0; i < numPackets; ++i)
			{
				int packetDataLength;

				if (i == numPackets - 1)
					packetDataLength = fullData.Length % maximumDataSize;
				else
					packetDataLength = maximumDataSize;

				var packet = new byte[packetDataLength + CitpHeaderLength];

				Buffer.BlockCopy(fullData, i * maximumDataSize, packet, CitpHeaderLength, packetDataLength);

				writeInHeader(packet, requestResponseIndex, i, numPackets);

				packets.Add(packet);
			}

			return packets;
		}

		private byte[] serializePacket(bool isAddHeader = true)
		{
			byte[] data;

			using (var writer = new CitpBinaryWriter(new MemoryStream()))
			{
				if (isAddHeader)
					writer.Write(new byte[CitpHeaderLength]);

				SerializeToStream(writer);

				data = ((MemoryStream)writer.BaseStream).ToArray();
			}

			if (isAddHeader)
				writeInHeader(data, RequestResponseIndex, MessagePart, MessagePartCount);

			return data;
		}

		private void writeInHeader(byte[] data, int requestResponseIndex, int messagePart, int messagePartCount)
		{
			Buffer.BlockCopy(CitpCookie, 0, data, 0, 4);

			data[4] = CitpVersionMajor;
			data[5] = CitpVersionMinor;

			unchecked
			{
				data[6] = (byte)requestResponseIndex;
				data[7] = (byte)(requestResponseIndex >> 8);

				data[8] = (byte)data.Length;
				data[9] = (byte)(data.Length >> 8);
				data[10] = (byte)(data.Length >> 16);
				data[11] = (byte)(data.Length >> 24);

				data[12] = (byte)messagePartCount;
				data[13] = (byte)(messagePartCount >> 8);

				data[14] = (byte)messagePart;
				data[15] = (byte)(messagePart >> 8);
			}

			Buffer.BlockCopy(LayerType.GetCustomAttribute<CitpId>().Id, 0, data, 16, 4);
		}



		protected virtual void SerializeToStream(CitpBinaryWriter writer) { }

		protected virtual void DeserializeFromStream(CitpBinaryReader reader)
		{
			// Read Header
			reader.ReadBytes(CitpHeaderLength);
		}
	}
}