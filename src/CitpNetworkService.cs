using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Imp.CitpSharp.Packets;
using Imp.CitpSharp.Packets.Msex;
using Imp.CitpSharp.Packets.Pinf;
using Imp.CitpSharp.Sockets;
using JetBrains.Annotations;

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


		private readonly CitpPeerType _deviceType;
		private readonly ICitpDevice _device;

		private readonly CitpTcpListenService _tcpListenService;
		private readonly CitpUdpService _udpService;



		private CitpNetworkService(ICitpLogService log, IpAddress nicAddress, bool useOriginalMulticastIp)
		{
			_nicAddress = nicAddress;
			_log = log;

			_tcpListenService = new CitpTcpListenService(_log, _nicAddress);
			_tcpListenService.ClientConnected += tcpListenService_ClientConnect;
			_tcpListenService.ClientDisconnected += tcpListenService_ClientDisconnect;
			_tcpListenService.MessageReceived += tcpListenService_PacketReceived;

			_udpService = new CitpUdpService(_log, _nicAddress, useOriginalMulticastIp);
			_udpService.MessageReceived += udpServiceMessageReceived;
		}


		public CitpNetworkService(ICitpLogService log, IpAddress nicAddress, bool useOriginalMulticastIp,
			ICitpMediaServerDevice device)
			: this(log, nicAddress, useOriginalMulticastIp)
		{
			_device = device;
			_deviceType = CitpPeerType.MediaServer;
		}

		public CitpNetworkService(ICitpLogService log, IpAddress nicAddress, bool useOriginalMulticastIp,
			ICitpVisualizerDevice device)
			: this(log, nicAddress, useOriginalMulticastIp)
		{
			_device = device;
			_deviceType = CitpPeerType.Visualizer;
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



		public async Task<bool> SendPacketAsync(CitpPacket packet, CitpPeer peer, [CanBeNull] CitpPacket requestPacket = null)
		{
			if (peer.IsConnected == false)
				throw new InvalidOperationException("Cannot send packet, peer is not connected");

			packet.MessagePart = 0;
			packet.MessagePartCount = 1;
			packet.RequestResponseIndex = Convert.ToUInt16(requestPacket?.RequestResponseIndex ?? 0);

			if (packet.LayerType == CitpLayerType.MediaServerExtensionsLayer)
			{
				var msexPacket = (MsexPacket)packet;

				if (peer.MsexVersion.HasValue == false)
					throw new InvalidOperationException("Peer MSEX version is unknown");

				if (msexPacket.Version.HasValue)
				{
					if (msexPacket.Version.Value > peer.MsexVersion)
						_log.LogWarning("Attempting to send an MSEX message with a higher version number than the peer supports");
				}
				else
				{
					((MsexPacket)packet).Version = peer.MsexVersion;
				}
			}

			return await sendDataToPeerAsync(peer, packet.ToByteArray(), requestPacket?.RemoteEndpoint?.Port).ConfigureAwait(false);
		}

		public async Task SendPacketToAllConnectedPeersAsync(CitpPacket packet)
		{
			var connectedPeers = Peers.Where(p => p.IsConnected).ToList();

			// If it's possible that different peers might need different versions of the packet,
			// run this special routine to avoid serializing the packet more than once for each version.
			if (packet.LayerType == CitpLayerType.MediaServerExtensionsLayer)
			{
				var msexPacket = (MsexPacket)packet;

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
			_log.LogDebug($"TCP client connected from {e.RemoteEndPoint}");

			await e.SendAsync(createPeerNamePacket().ToByteArray()).ConfigureAwait(false);
			await e.SendAsync(createServerInfoPacket(MsexVersion.Version1_0).ToByteArray()).ConfigureAwait(false);
		}

		private void tcpListenService_ClientDisconnect(object sender, IpEndpoint e)
		{
			_log.LogDebug($"TCP client disconnected from {e}");

			var peer = Peers.FirstOrDefault(p => p.RemoteTcpPorts.Contains(e.Port));

			if (peer == null)
			{
				_log.LogDebug("Failed to identify disconnecting peer");
				return;
			}

			peer.RemoveTcpConnection(e.Port);
			peer.LastUpdateReceived = DateTime.Now;

			_log.LogInfo($"CITP Peer '{peer}' disconnected on TCP Port {e.Port}");
		}

		private async void tcpListenService_PacketReceived(object sender, CitpTcpListenService.MessageReceivedEventArgs e)
		{
			_log.LogDebug($"TCP packet ({e.Data.Length} bytes) received from {e.Endpoint}");

			CitpPacket packet;

			try
			{
				packet = CitpPacket.FromByteArray(e.Data, e.Endpoint);
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

			var peer = _peers.FirstOrDefault(p => p.Ip == e.Endpoint.Address && p.RemoteTcpPorts.Contains(e.Endpoint.Port));

			if (peer == null)
			{
				_log.LogDebug($"Failed to identify peer for received TCP packet, ignoring...");
				return;
			}

			_log.LogDebug($"Packet identified as from CITP Peer '{peer}'");

			if (packet.LayerType == CitpLayerType.MediaServerExtensionsLayer)
			{
				var packetVersion = ((MsexPacket)packet).Version;
				Debug.Assert(packetVersion.HasValue, "Because we should not be able to deserialize a packet without finding the version");

				if (!peer.MsexVersion.HasValue || peer.MsexVersion < packetVersion)
					peer.MsexVersion = packetVersion;
			}

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
				packet = CitpPacket.FromByteArray(e.Data, e.Endpoint);
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
				_log.LogError($"Unrecognised/Invalid packet received via UDP from {e.Endpoint}");
			}
		}

		private void receivedPeerNameMessage(PeerNameMessagePacket message, IpEndpoint remoteEndpoint)
		{
			var peer = _peers.FirstOrDefault(p => p.Ip == remoteEndpoint.Address && p.Name == message.Name);

			if (peer != null)
			{
				peer.AddTcpConnection(remoteEndpoint.Port);
				_log.LogInfo($"Known CITP Peer '{peer}' identified on TCP port {remoteEndpoint.Port}");
			}
			else
			{
				peer = new CitpPeer(remoteEndpoint.Address, message.Name);
				peer.AddTcpConnection(remoteEndpoint.Port);
				
				_peers.Add(peer);
				_log.LogInfo($"New CITP Peer '{peer}' identified from on TCP port {remoteEndpoint.Port}");
			}

			peer.Name = message.Name;
			peer.LastUpdateReceived = DateTime.Now;
		}

		private void receivedPeerLocationMessage(PeerLocationMessagePacket message, IpAddress remoteIp)
		{
			// Filter out the local CITP peer
			if (remoteIp == _nicAddress && message.Name == _device.PeerName && message.ListeningTcpPort == LocalTcpListenPort)
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

			return SendPacketAsync(createServerInfoPacket(MsexVersion.Version1_2), peer, message);
		}

		private void removeInactivePeers()
		{
			_peers.RemoveAll(
				p => p.IsConnected == false && (DateTime.Now - p.LastUpdateReceived).TotalSeconds > CitpPeerExpiryTime);
		}

		private Task<bool> sendDataToPeerAsync(CitpPeer peer, byte[] data, int? remoteTcpPort = null)
		{
			IRemoteCitpTcpClient client;

			Debug.Assert(peer.IsConnected, "Can't send data to an unconnected peer");

			var endpoint = new IpEndpoint(peer.Ip, remoteTcpPort ?? peer.RemoteTcpPorts.First());

			return _tcpListenService.Clients.TryGetValue(endpoint, out client)
				? client.SendAsync(data)
				: Task.FromResult(false);
		}

		private PeerNameMessagePacket createPeerNamePacket()
		{
			return new PeerNameMessagePacket
			{
				Name = _device.PeerName
			};
		}

		private ServerInformationMessagePacket createServerInfoPacket(MsexVersion? version)
		{
			Debug.Assert(_device is ICitpMediaServerDevice, "Only media servers can send the server information packet");

			var serverDevice = (ICitpMediaServerDevice)_device;

			return new ServerInformationMessagePacket
			{
				Version = version,
				Uuid = serverDevice.Uuid,
				ProductName = serverDevice.ProductName,
				ProductVersionMajor = Convert.ToByte(serverDevice.ProductVersionMajor),
				ProductVersionMinor = Convert.ToByte(serverDevice.ProductVersionMinor),
				ProductVersionBugfix = Convert.ToByte(serverDevice.ProductVersionBugfix),
				SupportedMsexVersions = serverDevice.SupportedMsexVersions.ToList(),
				SupportedLibraryTypes = serverDevice.SupportedLibraryTypes.ToList(),
				ThumbnailFormats = serverDevice.SupportedThumbnailFormats.ToList(),
				StreamFormats = serverDevice.SupportedStreamFormats.ToList(),
				LayerDmxSources = serverDevice.Layers.Where(l => l.DmxSource.Protocol != CitpDmxConnectionString.DmxProtocol.None).Select(l => l.DmxSource).ToList()
			};
		}
	}
}