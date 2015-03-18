using Imp.CitpSharp.Packets;
using Imp.CitpSharp.Packets.Msex;
using Imp.CitpSharp.Packets.Pinf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;

namespace Imp.CitpSharp
{
	public class CitpService
	{
		static readonly int CITP_PLOC_FREQUENCY = 1000;
		static readonly int CITP_LSTA_FREQUENCY = 250;

		ICitpLogService _log;

		CitpNetworkService _networkService;
		DateTime _peerLocationMessageLastSent;
		DateTime _layerStatusMessageLastSent;
		ICitpMediaServerInfo _serverInfo;


		public CitpService(IPAddress nicAddress, 
			bool useOriginalMulticastIp,
			ICitpMediaServerInfo serverInfo,
			ICitpLogService log = null)
		{
			if (log == null)
				_log = new CitpConsoleLogger(CitpLoggerLevel.Info);
			else
				_log = log;

			_serverInfo = serverInfo;

			_networkService = new CitpNetworkService(nicAddress, useOriginalMulticastIp, _serverInfo, _log);
		}

		/// <summary>
		/// Processes all outstanding CITP messages.
		/// </summary>
		public void SendAndReceiveMessages()
		{
			_log.LogDebug("Started processing messages");

			if (_peerLocationMessageLastSent == null || (DateTime.Now - _peerLocationMessageLastSent).TotalMilliseconds >= CITP_PLOC_FREQUENCY)
			{
				sendPeerLocationPacket();
				_peerLocationMessageLastSent = DateTime.Now;
			}

			if (_layerStatusMessageLastSent == null || (DateTime.Now - _peerLocationMessageLastSent).TotalMilliseconds >= CITP_LSTA_FREQUENCY)
			{
				sendLayerStatusPacket();
				_layerStatusMessageLastSent = DateTime.Now;
			}

			while (_networkService.MessageQueue.Count > 0)
			{
				Tuple<CitpPeer, CitpPacket> message;

				if (_networkService.MessageQueue.TryDequeue(out message) == false)
					throw new InvalidOperationException("Failed to dequeue message");


				if (message.Item2 is GetElementLibraryInformationMessagePacket)
					getElementLibraryInfomation(message.Item1, message.Item2 as GetElementLibraryInformationMessagePacket);
				else if (message.Item2 is GetElementInformationMessagePacket)
					getElementInformation(message.Item1, message.Item2 as GetElementInformationMessagePacket);
				else if (message.Item2 is GetElementLibraryThumbnailMessagePacket)
					getElementLibraryThumbnail(message.Item1, message.Item2 as GetElementLibraryThumbnailMessagePacket);
				else if (message.Item2 is GetElementThumbnailMessagePacket)
					getElementThumbnail(message.Item1, message.Item2 as GetElementThumbnailMessagePacket);
			}

			_log.LogDebug("Finished processing messages");
		}

		public void Stop()
		{

		}

		public string Status { get; set; }


		// TODO: Move to network service
		void sendPeerLocationPacket()
		{
			var packet = new PeerLocationMessagePacket
			{
				IsListeningForTcpConnection = true,
				ListeningTcpPort = Convert.ToUInt16(_networkService.LocalTcpListenPort),
				Type = CitpPeerType.MediaServer,
				Name = _serverInfo.PeerName,
				State = Status
			};

			_networkService.SendMulticastPacket(packet);
		}

		void sendLayerStatusPacket()
		{
			var layers = _serverInfo.Layers.Select((l, i) =>
			{
				return new LayerStatusMessagePacket.LayerStatus
				{
					LayerNumber = (byte)i,
					PhysicalOutput = (byte)l.PhysicalOutput,
					MediaLibraryNumber = (byte)l.MediaLibraryIndex,
					MediaLibraryType = l.MediaLibraryType,
					MediaLibraryId = l.MediaLibraryId,
					MediaNumber = (byte)l.MediaIndex,
					MediaName = l.MediaName,
					MediaPosition = l.MediaFrame,
					MediaLength = l.MediaNumFrames,
					MediaFps = (byte)l.MediaFps,
					LayerStatusFlags = l.LayerStatusFlags
				};
			});

			var packet = new LayerStatusMessagePacket { LayerStatuses = layers.ToList() };
			_networkService.SendPacketToAllConnectedPeers(packet);
		}

		void sendElementLibraryUpdatedPackets()
		{
			foreach (var packet in _serverInfo.GetLibraryUpdateMessages())
				_networkService.SendPacketToAllConnectedPeers(packet);
		}



		void getElementLibraryInfomation(CitpPeer peer, GetElementLibraryInformationMessagePacket requestPacket)
		{
			List<ElementLibraryInformation> libraries;

			libraries = _serverInfo.GetElementLibraryInformation(requestPacket.LibraryType, 
				requestPacket.Version != MsexVersion.Version1_0 ? requestPacket.LibraryParentId : (MsexLibraryId?)null, 
				requestPacket.RequestedLibraryNumbers);

			var packet = new ElementLibraryInformationMessagePacket { LibraryType = requestPacket.LibraryType, Elements = libraries };
			_networkService.SendPacket(packet, peer, requestPacket.RequestResponseIndex);
		}

		void getElementInformation(CitpPeer peer, GetElementInformationMessagePacket requestPacket)
		{
			CitpPacket packet;

			if (requestPacket.LibraryType == MsexLibraryType.Media)
			{
				var mediaPacket = new MediaElementInformationMessagePacket
				{
					LibraryNumber = requestPacket.LibraryNumber,
					LibraryId = requestPacket.LibraryId
				};

				mediaPacket.Media = _serverInfo.GetMediaElementInformation(new MsexId(requestPacket.LibraryId, requestPacket.LibraryNumber), requestPacket.RequestedElementNumbers);

				packet = mediaPacket;
			}
			else if (requestPacket.LibraryType == MsexLibraryType.Effects)
			{
				var effectPacket = new EffectElementInformationMessagePacket
				{
					LibraryNumber = requestPacket.LibraryNumber,
					LibraryId = requestPacket.LibraryId
				};

				effectPacket.Effects = _serverInfo.GetEffectElementInformation(new MsexId(requestPacket.LibraryId, requestPacket.LibraryNumber), requestPacket.RequestedElementNumbers);

				packet = effectPacket;
			}
			else
			{
				// There must be a library Id as generic elements are unsupported in MSEX V1.0
				Debug.Assert(requestPacket.LibraryId.HasValue);

				var genericPacket = new GenericElementInformationMessagePacket
				{
					LibraryId = requestPacket.LibraryId.Value,
					LibraryType = requestPacket.LibraryType
				};

				genericPacket.Information = _serverInfo.GetGenericElementInformation(requestPacket.LibraryType, requestPacket.LibraryId.Value, requestPacket.RequestedElementNumbers);

				packet = genericPacket;
			}
			
			_networkService.SendPacket(packet, peer, requestPacket.RequestResponseIndex);
		}

		void getElementLibraryThumbnail(CitpPeer peer, GetElementLibraryThumbnailMessagePacket requestPacket)
		{
			List<MsexId> msexIds;

			if (requestPacket.Version == MsexVersion.Version1_0)
				msexIds = requestPacket.LibraryNumbers.Select(n => new MsexId(n)).ToList();
			else
				msexIds = requestPacket.LibraryIds.Select(i => new MsexId(i)).ToList();

			var thumbs = _serverInfo.GetElementLibraryThumbnails(requestPacket.LibraryType, msexIds);

			var packets = thumbs.Select(t =>
			{
				Image resizedThumb = t.Item2.Resize(new Size(requestPacket.ThumbnailWidth, requestPacket.ThumbnailHeight),
					requestPacket.ThumbnailFlags.HasFlag(MsexThumbnailFlags.PreserveAspectRatio));

				return new ElementLibraryThumbnailMessagePacket
				{
					LibraryType = requestPacket.LibraryType,
					LibraryNumber = t.Item1.LibraryNumber.GetValueOrDefault(),
					LibraryId = t.Item1.LibraryId.GetValueOrDefault(),
					ThumbnailFormat = requestPacket.ThumbnailFormat,
					ThumbnailWidth = (ushort)resizedThumb.Width,
					ThumbnailHeight = (ushort)resizedThumb.Height,
					ThumbnailBuffer = resizedThumb.ToByteArray(requestPacket.ThumbnailFormat, requestPacket.Version)
				};
			});

			foreach (var packet in packets)
				_networkService.SendPacket(packet, peer, requestPacket.RequestResponseIndex);
		}

		void getElementThumbnail(CitpPeer peer, GetElementThumbnailMessagePacket requestPacket)
		{
			List<Tuple<byte, Image>> thumbs;

			MsexId msexId;

			if (requestPacket.Version == MsexVersion.Version1_0)
				msexId = new MsexId(requestPacket.LibraryNumber);
			else
				msexId = new MsexId(requestPacket.LibraryId.Value);

			thumbs = _serverInfo.GetElementThumbnails(requestPacket.LibraryType, msexId, requestPacket.ElementNumbers);

			var packets = thumbs.Select(t =>
			{
				Image resizedThumb = t.Item2.Resize(new Size(requestPacket.ThumbnailWidth, requestPacket.ThumbnailHeight),
					requestPacket.ThumbnailFlags.HasFlag(MsexThumbnailFlags.PreserveAspectRatio));

				return new ElementThumbnailMessagePacket
				{
					LibraryType = requestPacket.LibraryType,
					LibraryNumber = requestPacket.LibraryNumber,
					LibraryId = requestPacket.LibraryId,
					ElementNumber = t.Item1,
					ThumbnailFormat = requestPacket.ThumbnailFormat,
					ThumbnailWidth = (ushort)resizedThumb.Width,
					ThumbnailHeight = (ushort)resizedThumb.Height,
					ThumbnailBuffer = resizedThumb.ToByteArray(requestPacket.ThumbnailFormat, requestPacket.Version)
				};

			});

			foreach (var packet in packets)
				_networkService.SendPacket(packet, peer, requestPacket.RequestResponseIndex);
		}
	}
}
