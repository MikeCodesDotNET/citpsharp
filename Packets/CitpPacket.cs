//  This file is part of CitpSharp.
//
//  CitpSharp is free software: you can redistribute it and/or modify
//	it under the terms of the GNU Lesser General Public License as published by
//	the Free Software Foundation, either version 3 of the License, or
//	(at your option) any later version.

//	CitpSharp is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU Lesser General Public License for more details.

//	You should have received a copy of the GNU Lesser General Public License
//	along with CitpSharp.  If not, see <http://www.gnu.org/licenses/>.

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

		private readonly CitpLayerType _layerType;


		protected CitpPacket(CitpLayerType layerType)
		{
			_layerType = layerType;
			MessagePart = 1;
			MessagePartCount = 1;
		}

		public CitpLayerType LayerType
		{
			get { return _layerType; }
		}

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
				throw new InvalidOperationException(string.Format("Unrecognised CITP content type: {0}",
					Encoding.UTF8.GetString(layerTypeArray)));
			}

			switch (layerType)
			{
				case CitpLayerType.PeerInformationLayer:
				{
					var messageType = CitpPinfPacket.GetMessageType(data);

					if (messageType == null)
					{
						var messageTypeArray = new byte[4];
						Buffer.BlockCopy(data, CitpPinfPacket.CitpMessageTypePosition, messageTypeArray, 0, 4);
						throw new InvalidOperationException(string.Format("Unrecognised PING message type: {0}",
							Encoding.UTF8.GetString(messageTypeArray)));
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
							throw new InvalidOperationException("Unimplemented PINF message type");
					}

					break;
				}

				case CitpLayerType.MediaServerExtensionsLayer:
				{
					var messageType = CitpMsexPacket.GetMessageType(data);

					if (messageType == null)
					{
						var messageTypeArray = new byte[4];
						Buffer.BlockCopy(data, CitpMsexPacket.CitpMessageTypePosition, messageTypeArray, 0, 4);
						throw new InvalidOperationException(string.Format("Unrecognised MSEX message type: {0}",
							Encoding.UTF8.GetString(messageTypeArray)));
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
							throw new InvalidOperationException("Unimplemented MSEX message type");
					}

					break;
				}

				default:
					throw new InvalidOperationException("Unimplemented CITP content type");
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

			Buffer.BlockCopy(LayerType.GetAttributeOfType<CitpId>().Id, 0, data, 16, 4);
		}



		protected virtual void SerializeToStream(CitpBinaryWriter writer) { }

		protected virtual void DeserializeFromStream(CitpBinaryReader reader)
		{
			var header = reader.ReadBytes(20);
		}
	}



	internal abstract class CitpPinfPacket : CitpPacket
	{
		public static readonly int CitpMessageTypePosition = 20;

		private readonly PinfMessageType _messageType;

		public CitpPinfPacket(PinfMessageType messageType)
			: base(CitpLayerType.PeerInformationLayer)
		{
			_messageType = messageType;
		}

		public PinfMessageType MessageType
		{
			get { return _messageType; }
		}

		public static PinfMessageType? GetMessageType(byte[] data)
		{
			string typeString = Encoding.UTF8.GetString(data, CitpMessageTypePosition, 4);
			return CitpEnumHelper.GetEnumFromIdString<PinfMessageType>(typeString);
		}

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(MessageType.GetAttributeOfType<CitpId>().Id);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (MessageType != CitpEnumHelper.GetEnumFromIdString<PinfMessageType>(reader.ReadIdString()))
				throw new InvalidOperationException("Incorrect message type");
		}
	}



	internal abstract class CitpSdmxPacket : CitpPacket
	{
		public static readonly int CitpMessageTypePosition = 20;

		private readonly SdmxMessageType _messageType;

		public CitpSdmxPacket(SdmxMessageType messageType)
			: base(CitpLayerType.SendDmxLayer)
		{
			_messageType = messageType;
		}

		public SdmxMessageType MessageType
		{
			get { return _messageType; }
		}

		public static SdmxMessageType? GetMessageType(byte[] data)
		{
			string typeString = Encoding.UTF8.GetString(data, CitpMessageTypePosition, 4);
			return CitpEnumHelper.GetEnumFromIdString<SdmxMessageType>(typeString);
		}

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(MessageType.GetAttributeOfType<CitpId>().Id);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			if (MessageType != CitpEnumHelper.GetEnumFromIdString<SdmxMessageType>(reader.ReadIdString()))
				throw new InvalidOperationException("Incorrect message type");
		}
	}



	internal abstract class CitpMsexPacket : CitpPacket
	{
		public static readonly int CitpMessageTypePosition = 22;

		private readonly MsexMessageType _messageType;

		public CitpMsexPacket(MsexMessageType messageType)
			: base(CitpLayerType.MediaServerExtensionsLayer)
		{
			Version = null;
			_messageType = messageType;
		}

		public MsexMessageType MessageType
		{
			get { return _messageType; }
		}

		public MsexVersion? Version { get; set; }

		public static MsexMessageType? GetMessageType(byte[] data)
		{
			string typeString = Encoding.UTF8.GetString(data, CitpMessageTypePosition, 4);
			return CitpEnumHelper.GetEnumFromIdString<MsexMessageType>(typeString);
		}

		protected override void SerializeToStream(CitpBinaryWriter writer)
		{
			base.SerializeToStream(writer);

			writer.Write(Version.GetAttributeOfType<CitpVersion>().ToByteArray());
			writer.Write(MessageType.GetAttributeOfType<CitpId>().Id);
		}

		protected override void DeserializeFromStream(CitpBinaryReader reader)
		{
			base.DeserializeFromStream(reader);

			byte versionHi, versionLo;

			versionHi = reader.ReadByte();
			versionLo = reader.ReadByte();

			if (versionHi == 1 && versionLo == 0)
				Version = MsexVersion.Version10;
			else if (versionHi == 1 && versionLo == 1)
				Version = MsexVersion.Version11;
			else if (versionHi == 1 && versionLo == 2)
				Version = MsexVersion.Version12;
			else
				Version = MsexVersion.UnsupportedVersion;

			if (Version == MsexVersion.UnsupportedVersion)
				throw new InvalidOperationException("Incorrect or invalid MSEX version");

			if (MessageType != CitpEnumHelper.GetEnumFromIdString<MsexMessageType>(reader.ReadIdString()))
				throw new InvalidOperationException("Incorrect or invalid message type");
		}
	}
}