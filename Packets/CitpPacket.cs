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
using System.IO;
using System.Text;

namespace Imp.CitpSharp.Packets
{
	internal abstract class CitpPacket
	{
		static readonly byte[] CITP_COOKIE = Encoding.UTF8.GetBytes("CITP");
		static readonly byte CITP_VERSION_MAJOR = 1;
		static readonly byte CITP_VERSION_MINOR = 0;

		static readonly int CITP_HEADER_LENGTH = 20;
		static readonly int CITP_CONTENT_TYPE_POSITION = 16;


		public CitpPacket(CitpLayerType layerType)
		{
			m_layerType = layerType;
			MessagePart = 1;
			MessagePartCount = 1;
		}

		public static CitpPacket FromByteArray(byte[] data)
		{
			CitpPacket packet = null;

			var layerType = getLayerType(data);

			if (layerType == null)
			{
				byte[] layerTypeArray = new byte[4];
				System.Buffer.BlockCopy(data, CitpPacket.CITP_CONTENT_TYPE_POSITION, layerTypeArray, 0, 4);
				throw new InvalidOperationException(String.Format("Unrecognised CITP content type: {0}", Encoding.UTF8.GetString(layerTypeArray)));
			}

			switch (layerType)
			{
				case CitpLayerType.PeerInformationLayer:
					{
						var messageType = CitpPinfPacket.GetMessageType(data);

						if (messageType == null)
						{
							byte[] messageTypeArray = new byte[4];
							System.Buffer.BlockCopy(data, CitpPinfPacket.CITP_MESSAGE_TYPE_POSITION, messageTypeArray, 0, 4);
							throw new InvalidOperationException(String.Format("Unrecognised PING message type: {0}", Encoding.UTF8.GetString(messageTypeArray)));
						}

						switch (messageType)
						{
							case PinfMessageType.PeerLocationMessage:
								packet = new Packets.Pinf.PeerLocationMessagePacket();
								break;
							case PinfMessageType.PeerNameMessage:
								packet = new Packets.Pinf.PeerNameMessagePacket();
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
							byte[] messageTypeArray = new byte[4];
							System.Buffer.BlockCopy(data, CitpMsexPacket.CITP_MESSAGE_TYPE_POSITION, messageTypeArray, 0, 4);
							throw new InvalidOperationException(String.Format("Unrecognised MSEX message type: {0}", Encoding.UTF8.GetString(messageTypeArray)));
						}

						switch (messageType)
						{
							case MsexMessageType.ClientInformationMessage:
								packet = new Packets.Msex.ClientInformationMessagePacket();
								break;
							case MsexMessageType.ServerInformationMessage:
								packet = new Packets.Msex.ServerInformationMessagePacket();
								break;
							case MsexMessageType.NegativeAcknowledgeMessage:
								packet = new Packets.Msex.NegativeAcknowledgeMessagePacket();
								break;
							case MsexMessageType.LayerStatusMessage:
								packet = new Packets.Msex.LayerStatusMessagePacket();
								break;
							case MsexMessageType.GetElementLibraryInformationMessage:
								packet = new Packets.Msex.GetElementLibraryInformationMessagePacket();
								break;
							case MsexMessageType.ElementLibraryInformationMessage:
								packet = new Packets.Msex.ElementLibraryInformationMessagePacket();
								break;
							case MsexMessageType.ElementLibraryUpdatedMessage:
								packet = new Packets.Msex.ElementLibraryUpdatedMessagePacket();
								break;
							case MsexMessageType.GetElementInformationMessage:
								packet = new Packets.Msex.GetElementInformationMessagePacket();
								break;
							case MsexMessageType.MediaElementInformationMessage:
								packet = new Packets.Msex.MediaElementInformationMessagePacket();
								break;
							case MsexMessageType.EffectElementInformationMessage:
								packet = new Packets.Msex.EffectElementInformationMessagePacket();
								break;
							case MsexMessageType.GenericElementInformationMessage:
								packet = new Packets.Msex.GenericElementInformationMessagePacket();
								break;
							case MsexMessageType.GetElementLibraryThumbnailMessage:
								packet = new Packets.Msex.GetElementLibraryThumbnailMessagePacket();
								break;
							case MsexMessageType.ElementLibraryThumbnailMessage:
								packet = new Packets.Msex.ElementLibraryThumbnailMessagePacket();
								break;
							case MsexMessageType.GetElementThumbnailMessage:
								packet = new Packets.Msex.GetElementThumbnailMessagePacket();
								break;
							case MsexMessageType.ElementThumbnailMessage:
								packet = new Packets.Msex.ElementThumbnailMessagePacket();
								break;
							case MsexMessageType.GetVideoSourcesMessage:
								packet = new Packets.Msex.GetVideoSourcesMessagePacket();
								break;
							case MsexMessageType.RequestStreamMessage:
								packet = new Packets.Msex.RequestStreamMessagePacket();
								break;
							case MsexMessageType.StreamFrameMessage:
								packet = new Packets.Msex.StreamFrameMessagePacket();
								break;
							default:
								throw new InvalidOperationException("Unimplemented MSEX message type");
						}

						break;
					}

				default:
					throw new InvalidOperationException("Unimplemented CITP content type");
			}



			using (CitpBinaryReader reader = new CitpBinaryReader(new MemoryStream(data)))
				packet.deserializeFromStream(reader);

			return packet;
		}

		static CitpLayerType? getLayerType(byte[] data)
		{
			string typeString = Encoding.UTF8.GetString(data, CITP_CONTENT_TYPE_POSITION, 4);
			return CitpEnumHelper.GetEnumFromIdString<CitpLayerType>(typeString);
		}


		public byte[] ToByteArray()
		{
			byte[] data;


			using (CitpBinaryWriter writer = new CitpBinaryWriter(new MemoryStream()))
			{
				// Leave space for the header to be written in later
				writer.Write(new byte[CITP_HEADER_LENGTH]);

				serializeToStream(writer);

				data = ((MemoryStream)writer.BaseStream).ToArray();
			}

			writeInHeader(data);

			return data;
		}

		readonly CitpLayerType m_layerType;
		public CitpLayerType LayerType { get { return m_layerType; } }

		public ushort RequestResponseIndex { get; set; }
		public ushort MessagePartCount { get; set; }
		public ushort MessagePart { get; set; }


		void writeInHeader(byte[] data)
		{
			System.Buffer.BlockCopy(CITP_COOKIE, 0, data, 0, 4);

			data[4] = CITP_VERSION_MAJOR;
			data[5] = CITP_VERSION_MINOR;

			unchecked
			{
				data[6] = (byte)(RequestResponseIndex);
				data[7] = (byte)(RequestResponseIndex >> 8);

				data[8] = (byte)(data.Length);
				data[9] = (byte)(data.Length >> 8);
				data[10] = (byte)(data.Length >> 16);
				data[11] = (byte)(data.Length >> 24);

				data[12] = (byte)(MessagePartCount);
				data[13] = (byte)(MessagePartCount >> 8);

				data[14] = (byte)(MessagePart);
				data[15] = (byte)(MessagePart >> 8);
			}

			System.Buffer.BlockCopy(LayerType.GetAttributeOfType<CitpId>().Id, 0, data, 16, 4);
		}

		

		protected virtual void serializeToStream(CitpBinaryWriter writer) { }

		protected virtual void deserializeFromStream(CitpBinaryReader reader)
		{
			byte[] header = reader.ReadBytes(20);
		}
	}

	internal abstract class CitpPinfPacket : CitpPacket
	{
		public static readonly int CITP_MESSAGE_TYPE_POSITION = 20;

		public static PinfMessageType? GetMessageType(byte[] data)
		{
			string typeString = Encoding.UTF8.GetString(data, CITP_MESSAGE_TYPE_POSITION, 4);
			return CitpEnumHelper.GetEnumFromIdString<PinfMessageType>(typeString);
		}

		public CitpPinfPacket(PinfMessageType messageType)
			: base(CitpLayerType.PeerInformationLayer)
		{
			m_messageType = messageType;
		}

		readonly PinfMessageType m_messageType;
		public PinfMessageType MessageType { get { return m_messageType; } }

		protected override void serializeToStream(CitpBinaryWriter writer)
		{
			base.serializeToStream(writer);

			writer.Write(MessageType.GetAttributeOfType<CitpId>().Id);
		}

		protected override void deserializeFromStream(CitpBinaryReader reader)
		{
			base.deserializeFromStream(reader);

			if (MessageType != CitpEnumHelper.GetEnumFromIdString<PinfMessageType>(reader.ReadIdString()))
				throw new InvalidOperationException("Incorrect message type");
		}
	}

	internal abstract class CitpSdmxPacket : CitpPacket
	{
		public static readonly int CITP_MESSAGE_TYPE_POSITION = 20;

		public static SdmxMessageType? GetMessageType(byte[] data)
		{
			string typeString = Encoding.UTF8.GetString(data, CITP_MESSAGE_TYPE_POSITION, 4);
			return CitpEnumHelper.GetEnumFromIdString<SdmxMessageType>(typeString);
		}

		public CitpSdmxPacket(SdmxMessageType messageType)
			: base(CitpLayerType.SendDMXLayer)
		{
			m_messageType = messageType;
		}

		readonly SdmxMessageType m_messageType;
		public SdmxMessageType MessageType {get { return m_messageType; } }

		protected override void serializeToStream(CitpBinaryWriter writer)
		{
			base.serializeToStream(writer);

			writer.Write(MessageType.GetAttributeOfType<CitpId>().Id);
		}

		protected override void deserializeFromStream(CitpBinaryReader reader)
		{
			base.deserializeFromStream(reader);

			if (MessageType != CitpEnumHelper.GetEnumFromIdString<SdmxMessageType>(reader.ReadIdString()))
				throw new InvalidOperationException("Incorrect message type");
		}
	}

	internal abstract class CitpMsexPacket : CitpPacket
	{
		public static readonly int CITP_MESSAGE_TYPE_POSITION = 22;

		public static MsexMessageType? GetMessageType(byte[] data)
		{
			string typeString = Encoding.UTF8.GetString(data, CITP_MESSAGE_TYPE_POSITION, 4);
			return CitpEnumHelper.GetEnumFromIdString<MsexMessageType>(typeString);
		}

		public CitpMsexPacket(MsexMessageType messageType)
			: base(CitpLayerType.MediaServerExtensionsLayer)
		{
			_messageType = messageType;
		}

		readonly MsexMessageType _messageType;
		public MsexMessageType MessageType { get { return _messageType; } }

		MsexVersion? _version = null;
		public MsexVersion? Version
		{
			get { return _version; }
			set { _version = value; }
		}

		protected override void serializeToStream(CitpBinaryWriter writer)
		{
			base.serializeToStream(writer);

			writer.Write(Version.GetAttributeOfType<CitpVersion>().ToByteArray());
			writer.Write(MessageType.GetAttributeOfType<CitpId>().Id);
		}

		protected override void deserializeFromStream(CitpBinaryReader reader)
		{
			base.deserializeFromStream(reader);

			byte versionHi, versionLo;

			versionHi = reader.ReadByte();
			versionLo = reader.ReadByte();

			if (versionHi == 1 && versionLo == 0)
				Version = MsexVersion.Version1_0;
			else if (versionHi == 1 && versionLo == 1)
				Version = MsexVersion.Version1_1;
			else if (versionHi == 1 && versionLo == 2)
				Version = MsexVersion.Version1_2;
			else
				Version = MsexVersion.UnsupportedVersion;

			if (Version == MsexVersion.UnsupportedVersion)
				throw new InvalidOperationException("Incorrect or invalid MSEX version");

			if (MessageType != CitpEnumHelper.GetEnumFromIdString<MsexMessageType>(reader.ReadIdString()))
				throw new InvalidOperationException("Incorrect or invalid message type");

		}
	}



	
	


}
