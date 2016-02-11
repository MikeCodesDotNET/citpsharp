using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Imp.CitpSharp.Packets;
using Imp.CitpSharp.Sockets;

namespace Imp.CitpSharp
{
	/// <summary>
	///     Class which manages the TCP and UDP services, maintains a list of CITP peers and handles messages related to peer
	///     discovery
	/// </summary>
	internal sealed class CitpNetworkService : IDisposable
	{
		private static readonly int CitpPeerExpiryTime = 10;

		private readonly ICitpLogService _log;
		private readonly IpAddress _nicAddress;

		private readonly List<CitpPeer> _peers = new List<CitpPeer>();
		private readonly ICitpMediaServerInfo _serverInfo;

		private readonly CitpTcpService _tcpListenService;
		private readonly CitpUdpService _udpService;



		private CitpNetworkService(ICitpLogService log, IpAddress nicAddress, bool useOriginalMulticastIp)
		{
			_nicAddress = nicAddress;
			_log = log;

			_tcpListenService = new CitpTcpService(_log, _nicAddress);
			_tcpListenService.ClientConnected += tcpListenService_ClientConnect;
			_tcpListenService.ClientDisconnected += tcpListenService_ClientDisconnect;
			_tcpListenService.MessageReceived += tcpListenService_PacketReceived;

			_udpService = new CitpUdpService(_log, _nicAddress, useOriginalMulticastIp);
			_udpService.MessageReceived += udpServiceMessageReceived;
		}


		public CitpNetworkService(ICitpLogService log, IpAddress nicAddress, bool useOriginalMulticastIp,
			ICitpMediaServerInfo serverInfo)
			: this(log, nicAddress, useOriginalMulticastIp)
		{
			_serverInfo = serverInfo;
		}

		public void Dispose()
		{
			_tcpListenService.Dispose();
			_udpService.Dispose();
		}



		public int LocalTcpListenPort => _tcpListenService.ListenPort;

		public IReadOnlyList<CitpPeer> Peers => _peers;

		public ConcurrentQueue<Tuple<CitpPeer, CitpPacket>> MessageQueue { get; } =
			new ConcurrentQueue<Tuple<CitpPeer, CitpPacket>>();

		public ConcurrentQueue<Tuple<IpAddress, StreamFrameMessagePacket>> FrameQueue { get; } =
			new ConcurrentQueue<Tuple<IpAddress, StreamFrameMessagePacket>>();



		public async Task<bool> StartAsync()
		{
			bool udpResult = await _udpService.StartAsync().ConfigureAwait(false);

			if (udpResult == false)
				return false;

			bool tcpResult = await _tcpListenService.StartAsync().ConfigureAwait(false);

			if (tcpResult == false)
				return false;

			return true;
		}



		public async Task<bool> SendPacketAsync(CitpPacket packet, CitpPeer peer, int requestResponseIndex = 0)
		{
			if (peer.IsConnected == false)
				throw new InvalidOperationException("Cannot send packet, peer is not connected");

			packet.MessagePart = 0;
			packet.MessagePartCount = 1;
			packet.RequestResponseIndex = Convert.ToUInt16(requestResponseIndex);

			if (packet.LayerType == CitpLayerType.MediaServerExtensionsLayer)
			{
				var msexPacket = (CitpMsexPacket)packet;

				if (peer.MsexVersion.HasValue == false)
					throw new InvalidOperationException("Peer MSEX version is unknown");

				if (msexPacket.Version.HasValue)
				{
					if (msexPacket.Version.Value > peer.MsexVersion)
						_log.LogWarning("Attempting to send an MSEX message with a higher version number than the peer supports");
				}
				else
				{
					((CitpMsexPacket)packet).Version = peer.MsexVersion;
				}
			}

			return await sendDataToPeerAsync(peer, packet.ToByteArray()).ConfigureAwait(false);
		}

		public async Task SendPacketToAllConnectedPeersAsync(CitpPacket packet)
		{
			var connectedPeers = Peers.Where(p => p.IsConnected).ToList();

			// If it's possible that different peers might need different versions of the packet,
			// run this special routine to avoid serializing the packet more than once for each version.
			if (packet.LayerType == CitpLayerType.MediaServerExtensionsLayer)
			{
				var msexPacket = (CitpMsexPacket)packet;

				if (msexPacket.Version.HasValue == false)
				{
					msexPacket.MessagePart = 0;
					msexPacket.MessagePartCount = 1;
					msexPacket.RequestResponseIndex = 0;

					foreach (MsexVersion version in Enum.GetValues(typeof(MsexVersion)))
					{
						if (version == MsexVersion.UnsupportedVersion)
							continue;

						var peersWithVersion = connectedPeers.Where(p => p.MsexVersion == version).ToList();

						if (peersWithVersion.Count == 0)
							continue;

						msexPacket.Version = version;
						var data = msexPacket.ToByteArray();

						foreach (var peer in peersWithVersion)
							await sendDataToPeerAsync(peer, data).ConfigureAwait(false);
					}
				}
			}
			else
			{
				foreach (var peer in connectedPeers)
					await SendPacketAsync(packet, peer).ConfigureAwait(false);
			}
		}

		public async Task SendMulticastPacketAsync(CitpPacket packet, int requestResponseIndex = 0)
		{
			foreach (var data in packet.ToByteArray(CitpUdpService.MaximumUdpPacketLength, requestResponseIndex))
			{
				await _udpService.SendAsync(data).ConfigureAwait(false);
			}
		}



		private async void tcpListenService_ClientConnect(object sender, IRemoteCitpTcpClient e)
		{
			var peer = _peers.FirstOrDefault(p => e.RemoteEndPoint.Address.Equals(p.Ip));

			if (peer != null)
				peer.SetConnected(e.RemoteEndPoint.Port);
			else
				_peers.Add(new CitpPeer(e.RemoteEndPoint));

			await e.SendAsync(createPeerNamePacket().ToByteArray()).ConfigureAwait(false);
			await e.SendAsync(createServerInfoPacket(MsexVersion.Version1_0).ToByteArray()).ConfigureAwait(false);
		}

		private void tcpListenService_ClientDisconnect(object sender, IpEndpoint e)
		{
			var peer = Peers.FirstOrDefault(p => e.Equals(p.RemoteEndPoint));

			if (peer == null)
				throw new InvalidOperationException($"Unregistered peer disconnected from TCP,  remote endpoint {e}");

			peer.SetDisconnected();
			peer.LastUpdateReceived = DateTime.Now;
		}

		private async void tcpListenService_PacketReceived(object sender, CitpTcpService.MessageReceivedEventArgs e)
		{
			CitpPacket packet;

			try
			{
				packet = CitpPacket.FromByteArray(e.Data);
			}
			catch (InvalidOperationException ex)
			{
				_log.LogError($"Error: Failed to deserialize TCP packet from {e.Endpoint}");
				_log.LogException(ex);
				return;
			}
			catch (NotImplementedException ex)
			{
				_log.LogError($"Error: Failed to deserialize TCP packet from {e.Endpoint}, CITP content type not implemented");
				_log.LogException(ex);
				return;
			}

			if (packet is PeerNameMessagePacket)
			{
				receivedPeerNameMessage((PeerNameMessagePacket)packet, e.Endpoint);
				return;
			}

			var peer = _peers.FirstOrDefault(p => e.Endpoint == p.RemoteEndPoint);

			if (peer == null)
				throw new InvalidOperationException("Message received via TCP from unrecognized peer.");


			if (packet is ClientInformationMessagePacket)
				await receivedClientInformationMessageAsync((ClientInformationMessagePacket)packet, peer).ConfigureAwait(false);
			else
				MessageQueue.Enqueue(Tuple.Create(peer, packet));
		}

		private void udpServiceMessageReceived(object sender, CitpUdpService.MessageReceivedEventArgs e)
		{
			CitpPacket packet;

			try
			{
				packet = CitpPacket.FromByteArray(e.Data);
			}
			catch (InvalidOperationException ex)
			{
				_log.LogError($"Failed to deserialize UDP packet from {e.Endpoint}");
				_log.LogException(ex);
				return;
			}

			if (packet is StreamFrameMessagePacket)
			{
				FrameQueue.Enqueue(Tuple.Create(e.Endpoint.Address, (StreamFrameMessagePacket)packet));
			}
			else if (packet is PeerLocationMessagePacket)
			{
				receivedPeerLocationMessage((PeerLocationMessagePacket)packet, e.Endpoint.Address);
			}
			else
			{
				_log.LogError($"Invalid packet received via UDP from {e.Endpoint}");
			}
		}

		private void receivedPeerNameMessage(PeerNameMessagePacket message, IpEndpoint remoteEndPoint)
		{
			var peer = _peers.FirstOrDefault(p => remoteEndPoint.Address.Equals(p.Ip));

			if (peer == null)
			{
				peer = new CitpPeer(remoteEndPoint.Address, message.Name);
				_peers.Add(peer);
			}

			peer.Name = message.Name;
			peer.LastUpdateReceived = DateTime.Now;
		}

		private void receivedPeerLocationMessage(PeerLocationMessagePacket message, IpAddress remoteIp)
		{
			// Filter out the local CITP peer
			if (remoteIp.Equals(_nicAddress) && message.Name == _serverInfo.PeerName
			    && message.ListeningTcpPort == LocalTcpListenPort)
				return;

			var peer = Peers.FirstOrDefault(p => p.Ip.Equals(remoteIp) && p.Name == message.Name);

			if (peer == null)
			{
				peer = new CitpPeer(remoteIp, message.Name);
				_peers.Add(peer);
			}

			peer.Type = message.Type;
			peer.State = message.State;
			peer.LastUpdateReceived = DateTime.Now;
		}

		private Task receivedClientInformationMessageAsync(ClientInformationMessagePacket message, CitpPeer peer)
		{
			if (!message.SupportedMsexVersions.Contains(MsexVersion.Version1_2))
				return Task.FromResult(false);

			peer.MsexVersion = MsexVersion.Version1_2;

			return SendPacketAsync(createServerInfoPacket(MsexVersion.Version1_2), peer, message.RequestResponseIndex);
		}

		private void removeInactivePeers()
		{
			_peers.RemoveAll(
				p => p.IsConnected == false && (DateTime.Now - p.LastUpdateReceived).TotalSeconds > CitpPeerExpiryTime);
		}

		private Task<bool> sendDataToPeerAsync(CitpPeer peer, byte[] data)
		{
			IRemoteCitpTcpClient client;

			return _tcpListenService.Clients.TryGetValue(peer.RemoteEndPoint, out client)
				? client.SendAsync(data)
				: Task.FromResult(false);
		}

		private PeerNameMessagePacket createPeerNamePacket()
		{
			return new PeerNameMessagePacket
			{
				Name = _serverInfo.PeerName
			};
		}

		private ServerInformationMessagePacket createServerInfoPacket(MsexVersion? version)
		{
			return new ServerInformationMessagePacket
			{
				Version = version,
				Uuid = _serverInfo.Uuid,
				ProductName = _serverInfo.ProductName,
				ProductVersionMajor = Convert.ToByte(_serverInfo.ProductVersionMajor),
				ProductVersionMinor = Convert.ToByte(_serverInfo.ProductVersionMinor),
				ProductVersionBugfix = Convert.ToByte(_serverInfo.ProductVersionBugfix),
				SupportedMsexVersions = _serverInfo.SupportedMsexVersions.ToList(),
				SupportedLibraryTypes = _serverInfo.SupportedLibraryTypes.ToList(),
				ThumbnailFormats = _serverInfo.SupportedThumbnailFormats.ToList(),
				StreamFormats = _serverInfo.SupportedStreamFormats.ToList(),
				LayerDmxSources = _serverInfo.Layers.Select(l => l.DmxSource).ToList()
			};
		}
	}
}