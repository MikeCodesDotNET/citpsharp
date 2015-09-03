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
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Imp.CitpSharp.Packets;
using Imp.CitpSharp.Packets.Msex;
using Imp.CitpSharp.Packets.Pinf;

namespace Imp.CitpSharp
{
	public sealed class CitpService : IDisposable
	{
		private const int CitpPlocFrequency = 1000;
		private const int CitpLstaFrequency = 250;

		private readonly ICitpLogService m_log;
		private readonly ICitpMediaServerInfo m_serverInfo;
		private readonly CitpStreamingService m_streamingService;
		private DateTime m_layerStatusMessageLastSent;

		private CitpNetworkService m_networkService;

		private DateTime m_peerLocationMessageLastSent;



		public CitpService(IPAddress nicAddress,
			bool useOriginalMulticastIp,
			ICitpMediaServerInfo serverInfo, bool isStreamingEnabled,
			ICitpLogService log = null)
		{
			m_log = log ?? new CitpConsoleLogger(CitpLoggerLevel.Info);

			m_serverInfo = serverInfo;

			m_networkService = new CitpNetworkService(m_log, nicAddress, useOriginalMulticastIp, m_serverInfo);

			IsStreamingEnabled = isStreamingEnabled;

			if (isStreamingEnabled)
				m_streamingService = new CitpStreamingService(m_log, m_serverInfo, m_networkService);
		}


		public string Status { get; set; }

		public bool IsStreamingEnabled { get; private set; }

		public void Dispose()
		{
			if (m_networkService != null)
			{
				m_networkService.Dispose();
				m_networkService = null;
			}
		}

		public bool Start()
		{
			return m_networkService.Start();
		}


		/// <summary>
		///     Processes all outstanding CITP messages.
		/// </summary>
		public async Task SendAndReceiveMessagesAsync()
		{
			m_log.LogDebug("Started processing messages");

			if (m_peerLocationMessageLastSent == null
			    || (DateTime.Now - m_peerLocationMessageLastSent).TotalMilliseconds >= CitpPlocFrequency)
			{
				await sendPeerLocationPacketAsync();
				m_peerLocationMessageLastSent = DateTime.Now;
			}

			if (m_layerStatusMessageLastSent == null
			    || (DateTime.Now - m_peerLocationMessageLastSent).TotalMilliseconds >= CitpLstaFrequency)
			{
				await sendLayerStatusPacketAsync();
				m_layerStatusMessageLastSent = DateTime.Now;
			}

			while (m_networkService.MessageQueue.Count > 0)
			{
				Tuple<CitpPeer, CitpPacket> message;

				if (m_networkService.MessageQueue.TryDequeue(out message) == false)
					throw new InvalidOperationException("Failed to dequeue message");

				try
				{
					if (message.Item2.LayerType == CitpLayerType.MediaServerExtensionsLayer)
					{
						var msexPacket = message.Item2 as CitpMsexPacket;

						if (message.Item1.MsexVersion.HasValue == false)
						{
							message.Item1.MsexVersion = msexPacket.Version;
						}
						else if (message.Item1.MsexVersion < msexPacket.Version)
						{
							m_log.LogWarning(
								"Received packet from peer with higher MSEX version than previously discovered for this client. Updating peer version");
							message.Item1.MsexVersion = msexPacket.Version;
						}
					}

					if (message.Item2 is GetElementLibraryInformationMessagePacket)
						await getElementLibraryInfomationAsync(message.Item1, message.Item2 as GetElementLibraryInformationMessagePacket);
					else if (message.Item2 is GetElementInformationMessagePacket)
						await getElementInformationAsync(message.Item1, message.Item2 as GetElementInformationMessagePacket);
					else if (message.Item2 is GetElementLibraryThumbnailMessagePacket)
						await getElementLibraryThumbnailAsync(message.Item1, message.Item2 as GetElementLibraryThumbnailMessagePacket);
					else if (message.Item2 is GetElementThumbnailMessagePacket)
						await getElementThumbnailAsync(message.Item1, message.Item2 as GetElementThumbnailMessagePacket);
					else if (message.Item2 is GetVideoSourcesMessagePacket)
						await getVideoSourcesAsync(message.Item1, message.Item2 as GetVideoSourcesMessagePacket);
					else if (message.Item2 is RequestStreamMessagePacket)
						m_streamingService.AddStreamRequest(message.Item1.MsexVersion, message.Item2 as RequestStreamMessagePacket);
				}
				catch (InvalidOperationException ex)
				{
					m_log.LogError("Failed to process message");
					m_log.LogException(ex);
				}
				catch (SocketException ex)
				{
					m_log.LogError("Socket exception whilst processing message");
					m_log.LogException(ex);
				}
			}

			m_log.LogDebug("Finished processing messages");
		}

		public async Task ProcessVideoStreamsAsync()
		{
			await m_streamingService.ProcessStreamRequestsAsync();
		}


		// TODO: Move to network service
		private async Task sendPeerLocationPacketAsync()
		{
			var packet = new PeerLocationMessagePacket
			{
				IsListeningForTcpConnection = true,
				ListeningTcpPort = Convert.ToUInt16(m_networkService.LocalTcpListenPort),
				Type = CitpPeerType.MediaServer,
				Name = m_serverInfo.PeerName,
				State = Status
			};

			await m_networkService.SendMulticastPacketAsync(packet);
		}

		private async Task sendLayerStatusPacketAsync()
		{
			var layers = m_serverInfo.Layers.Select((l, i) => new LayerStatusMessagePacket.LayerStatus
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
			});

			var packet = new LayerStatusMessagePacket {LayerStatuses = layers.ToList()};
			await m_networkService.SendPacketToAllConnectedPeersAsync(packet);
		}

		private async Task sendElementLibraryUpdatedPacketsAsync()
		{
			foreach (var message in m_serverInfo.GetLibraryUpdateMessages())
			{
				await m_networkService.SendPacketToAllConnectedPeersAsync(message.ToPacket());
			}
		}



		private async Task getElementLibraryInfomationAsync(CitpPeer peer,
			GetElementLibraryInformationMessagePacket requestPacket)
		{
			var libraries = m_serverInfo.GetElementLibraryInformation(requestPacket.LibraryType,
				requestPacket.Version != MsexVersion.Version10 ? requestPacket.LibraryParentId : null,
				requestPacket.RequestedLibraryNumbers);

			var packet = new ElementLibraryInformationMessagePacket
			{
				LibraryType = requestPacket.LibraryType,
				Elements = libraries
			};
			await m_networkService.SendPacketAsync(packet, peer, requestPacket.RequestResponseIndex);
		}

		private async Task getElementInformationAsync(CitpPeer peer, GetElementInformationMessagePacket requestPacket)
		{
			CitpPacket packet;

			if (requestPacket.LibraryType == MsexLibraryType.Media)
			{
				var mediaPacket = new MediaElementInformationMessagePacket
				{
					LibraryNumber = requestPacket.LibraryNumber,
					LibraryId = requestPacket.LibraryId
				};

				mediaPacket.Media =
					m_serverInfo.GetMediaElementInformation(new MsexId(requestPacket.LibraryId, requestPacket.LibraryNumber),
						requestPacket.RequestedElementNumbers);

				packet = mediaPacket;
			}
			else if (requestPacket.LibraryType == MsexLibraryType.Effects)
			{
				var effectPacket = new EffectElementInformationMessagePacket
				{
					LibraryNumber = requestPacket.LibraryNumber,
					LibraryId = requestPacket.LibraryId
				};

				effectPacket.Effects =
					m_serverInfo.GetEffectElementInformation(new MsexId(requestPacket.LibraryId, requestPacket.LibraryNumber),
						requestPacket.RequestedElementNumbers);

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

				genericPacket.Information = m_serverInfo.GetGenericElementInformation(requestPacket.LibraryType,
					requestPacket.LibraryId.Value, requestPacket.RequestedElementNumbers);

				packet = genericPacket;
			}

			await m_networkService.SendPacketAsync(packet, peer, requestPacket.RequestResponseIndex);
		}

		private async Task getElementLibraryThumbnailAsync(CitpPeer peer,
			GetElementLibraryThumbnailMessagePacket requestPacket)
		{
			List<MsexId> msexIds;

			if (requestPacket.Version == MsexVersion.Version10)
				msexIds = requestPacket.LibraryNumbers.Select(n => new MsexId(n)).ToList();
			else
				msexIds = requestPacket.LibraryIds.Select(i => new MsexId(i)).ToList();

			var thumbs = m_serverInfo.GetElementLibraryThumbnails(requestPacket.LibraryType, msexIds);

			var packets = thumbs.Select(t =>
			{
				var resizedThumb = t.Item2.Resize(new Size(requestPacket.ThumbnailWidth, requestPacket.ThumbnailHeight),
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
				await m_networkService.SendPacketAsync(packet, peer, requestPacket.RequestResponseIndex);
		}

		private async Task getElementThumbnailAsync(CitpPeer peer, GetElementThumbnailMessagePacket requestPacket)
		{
			List<Tuple<byte, Image>> thumbs;

			MsexId msexId;

			if (requestPacket.Version == MsexVersion.Version10)
				msexId = new MsexId(requestPacket.LibraryNumber);
			else
				msexId = new MsexId(requestPacket.LibraryId.Value);

			thumbs = m_serverInfo.GetElementThumbnails(requestPacket.LibraryType, msexId, requestPacket.ElementNumbers);

			var packets = thumbs.Select(t =>
			{
				var resizedThumb = t.Item2.Resize(new Size(requestPacket.ThumbnailWidth, requestPacket.ThumbnailHeight),
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
				await m_networkService.SendPacketAsync(packet, peer, requestPacket.RequestResponseIndex);
		}


		private async Task getVideoSourcesAsync(CitpPeer peer, GetVideoSourcesMessagePacket requestPacket)
		{
			var packet = new VideoSourcesMessagePacket {Sources = m_serverInfo.VideoSources.Values.ToList()};
			await m_networkService.SendPacketAsync(packet, peer);
		}
	}
}