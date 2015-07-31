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

using Imp.CitpSharp.Packets;
using Imp.CitpSharp.Packets.Msex;
using Imp.CitpSharp.Packets.Pinf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Imp.CitpSharp
{
	/// <summary>
	/// Class which manages the TCP and UDP services, maintains a list of CITP peers and handles messages related to peer discovery
	/// </summary>
	internal sealed class CitpNetworkService : IDisposable
	{
		static readonly int CITP_PEER_EXPIRY_TIME = 10;

		readonly ICitpLogService _log;
		readonly IPAddress _nicAddress;
		readonly bool _useOriginalMulticastIp;
		readonly ICitpMediaServerInfo _serverInfo;

		int _tcpListenPort;
		
		CitpUdpService _udpService;
		CitpTcpService _tcpListenService;

		

		


		CitpNetworkService(ICitpLogService log, IPAddress nicAddress, bool useOriginalMulticastIp)
		{
			_nicAddress = nicAddress;
			_useOriginalMulticastIp = useOriginalMulticastIp;
			_log = log;
		}


		public CitpNetworkService(ICitpLogService log, IPAddress nicAddress, bool useOriginalMulticastIp, 
			ICitpMediaServerInfo serverInfo)
			: this(log, nicAddress, useOriginalMulticastIp)
		{
			_serverInfo = serverInfo;
		}


		public bool Start()
		{
			_udpService = new CitpUdpService(_log, _nicAddress, _useOriginalMulticastIp);
			_udpService.PacketReceived += udpService_PacketReceived;

			bool udpResult = _udpService.Start();

			if (udpResult == false)
				return false;

			_tcpListenPort = getAvailableTcpPort();

			_tcpListenService = new CitpTcpService(_log, _nicAddress, _tcpListenPort);
			_tcpListenService.ClientConnected += tcpListenService_ClientConnect;
			_tcpListenService.ClientDisconnected += tcpListenService_ClientDisconnect;
			_tcpListenService.PacketReceieved += tcpListenService_PacketReceived;

			bool tcpResult = _tcpListenService.StartListening();

			if (tcpResult == false)
				return false;

			return true;
		}

		public void Dispose()
		{
			if (_tcpListenService != null)
			{
				_tcpListenService.Dispose();
				_tcpListenService = null;
			}

			if (_udpService != null)
			{
				_udpService.Dispose();
				_udpService = null;
			}
		}


		public int LocalTcpListenPort
		{
			get { return _tcpListenPort; }
		}



		List<CitpPeer> _peers = new List<CitpPeer>();
		public IReadOnlyList<CitpPeer> Peers
		{
			get { return _peers; }
		}

		ConcurrentQueue<Tuple<CitpPeer, CitpPacket>> _messageQueue = new ConcurrentQueue<Tuple<CitpPeer, CitpPacket>>();
		public ConcurrentQueue<Tuple<CitpPeer, CitpPacket>> MessageQueue
		{
			get { return _messageQueue; }
		}

		ConcurrentQueue<Tuple<IPAddress, StreamFrameMessagePacket>> _frameQueue = new ConcurrentQueue<Tuple<IPAddress,StreamFrameMessagePacket>>();
		public ConcurrentQueue<Tuple<IPAddress, StreamFrameMessagePacket>> FrameQueue
		{
			get { return _frameQueue; }
		}




		public async Task SendPacket(CitpPacket packet, CitpPeer peer, int requestResponseIndex = 0)
		{
			if (peer.IsConnected == false)
				throw new InvalidOperationException("Cannot send packet, peer is not connected");

			packet.MessagePart = 0;
			packet.MessagePartCount = 1;
			packet.RequestResponseIndex = Convert.ToUInt16(requestResponseIndex);

			if (packet.LayerType == CitpLayerType.MediaServerExtensionsLayer)
			{
				var msexPacket = packet as CitpMsexPacket;

				if (peer.MsexVersion.HasValue == false)
					throw new InvalidOperationException("Peer MSEX version is unknown");

				if (msexPacket.Version.HasValue)
				{
					if (msexPacket.Version.Value > peer.MsexVersion)
						_log.LogWarning("Attempting to send an MSEX message with a higher version number than the peer supports");
				}
				else
				{
					(packet as CitpMsexPacket).Version = peer.MsexVersion;
				}
			}

			await sendDataToPeer(peer, packet.ToByteArray());
		}

		public async Task SendPacketToAllConnectedPeers(CitpPacket packet)
		{
			var connectedPeers = Peers.Where(p => p.IsConnected == true).ToList();

			// If it's possible that different peers might need different versions of the packet,
			// run this special routine to avoid serializing the packet more than once for each version.
			if (packet.LayerType == CitpLayerType.MediaServerExtensionsLayer)
			{
				var msexPacket = packet as CitpMsexPacket;

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
						byte[] data = msexPacket.ToByteArray();

						foreach (var peer in peersWithVersion)
							await sendDataToPeer(peer, data);
					}
				}
			}
			else
			{
				foreach (var peer in connectedPeers)
					await SendPacket(packet, peer);
			}
		}

		public async Task SendMulticastPacket(CitpPacket packet, int requestResponseIndex = 0)
		{
			// TODO: Deal with packet splitting

			packet.MessagePart = 0;
			packet.MessagePartCount = 1;
			packet.RequestResponseIndex = Convert.ToUInt16(requestResponseIndex);

			byte[] data = packet.ToByteArray();

			await _udpService.Send(data);
		}





		async void tcpListenService_ClientConnect(object sender, ICitpTcpClient e)
		{
			var peer = _peers.FirstOrDefault(p => e.RemoteEndPoint.Address.Equals(p.Ip));

			if (peer != null)
				peer.SetConnected(e.RemoteEndPoint.Port);
			else
				_peers.Add(new CitpPeer(e.RemoteEndPoint));

			await e.Send(createPeerNamePacket().ToByteArray());
			await e.Send(createServerInfoPacket(MsexVersion.Version1_0).ToByteArray());
		}

		void tcpListenService_ClientDisconnect(object sender, IPEndPoint e)
		{
			var peer = Peers.FirstOrDefault(p => e.Equals(p.RemoteEndPoint));

			if (peer == null)
				throw new InvalidOperationException(String.Format("Unregistered peer disconnected from TCP,  remote endpoint {0}", e));

			peer.SetDisconnected();
			peer.LastUpdateReceived = DateTime.Now;
		}

		async void tcpListenService_PacketReceived(object sender, Tuple<IPEndPoint, byte[]> e)
		{
			CitpPacket packet = null;

			try
			{
				packet = CitpPacket.FromByteArray(e.Item2);
			}
			catch (InvalidOperationException ex)
			{
				_log.LogError(String.Format("Error: Failed to deserialize TCP packet from {0}", e.Item1.ToString()));
				_log.LogException(ex);
				return;
			}

			if (packet is PeerNameMessagePacket)
			{
				receivedPeerNameMessage(packet as PeerNameMessagePacket, e.Item1);
				return;
			}

			var peer = _peers.FirstOrDefault(p => e.Item1.Equals(p.RemoteEndPoint));

			if (peer == null)
				throw new InvalidOperationException("Message received via TCP from unrecognised peer.");


			if (packet is ClientInformationMessagePacket)
				await receivedClientInformationMessage(packet as ClientInformationMessagePacket, peer);
			else
				MessageQueue.Enqueue(Tuple.Create(peer, packet));
		}

		void udpService_PacketReceived(object sender, Tuple<IPAddress, byte[]> e)
		{
			CitpPacket packet;

			try
			{
				packet = CitpPacket.FromByteArray(e.Item2);
			}
			catch (InvalidOperationException ex)
			{
				_log.LogError(String.Format("Failed to deserialize UDP packet from {0}", e.Item1.ToString()));
				_log.LogException(ex);
				return;
			}

			if (packet is StreamFrameMessagePacket)
			{
				FrameQueue.Enqueue(Tuple.Create(e.Item1, packet as StreamFrameMessagePacket));
			}
			else if (packet is PeerLocationMessagePacket)
			{
				receivedPeerLocationMessage(packet as PeerLocationMessagePacket, e.Item1);
			}
			else
			{
				_log.LogError(String.Format("Invalid packet received via UDP from {0}", e.Item1.ToString()));
			}

		}



		int getAvailableTcpPort()
		{
			const int portStartIndex = 1024;
			const int portEndIndex = 49151;
			var properties = IPGlobalProperties.GetIPGlobalProperties();
			IPEndPoint[] tcpEndPoints = properties.GetActiveTcpListeners();

			List<int> usedPorts = tcpEndPoints.Select(p => p.Port).ToList<int>();
			int unusedPort = 0;

			for (int port = portStartIndex; port < portEndIndex; ++port)
			{
				if (!usedPorts.Contains(port))
				{
					unusedPort = port;
					break;
				}
			}
			return unusedPort;
		}

		void receivedPeerNameMessage(PeerNameMessagePacket message, IPEndPoint remoteEndPoint)
		{
			var peer = _peers.FirstOrDefault(p => remoteEndPoint.Address.Equals(p.Ip));

			if (peer == null)
				throw new InvalidOperationException("Received peer name message for unconnected peer");

			peer.Name = message.Name;
			peer.LastUpdateReceived = DateTime.Now;
		}

		void receivedPeerLocationMessage(PeerLocationMessagePacket message, IPAddress remoteIp)
		{
			// Filter out this CITP peer
			if (remoteIp.Equals(_nicAddress) && message.Name == _serverInfo.PeerName && message.ListeningTcpPort == _tcpListenPort)
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

		async Task receivedClientInformationMessage(ClientInformationMessagePacket message, CitpPeer peer)
		{
			if (message.SupportedMsexVersions.Contains(MsexVersion.Version1_2))
			{
				peer.MsexVersion = MsexVersion.Version1_2;
				var packet = createServerInfoPacket(MsexVersion.Version1_2);
				await SendPacket(packet, peer, message.RequestResponseIndex);
			}
		}

		void removeInactivePeers()
		{
			_peers.RemoveAll(p => p.IsConnected == false && (DateTime.Now - p.LastUpdateReceived).TotalSeconds > CITP_PEER_EXPIRY_TIME);
		}

		async Task sendDataToPeer(CitpPeer peer, byte[] data)
		{
			ICitpTcpClient client;
			if (_tcpListenService.Clients.TryGetValue(peer.RemoteEndPoint, out client))
			{
				bool result = await client.Send(data);

				if (result == false)
					throw new InvalidOperationException("Failed to send packet");

			}
			else
			{
				throw new InvalidOperationException("Cannot send packet, peer is not connected.");
			}
		}

		PeerNameMessagePacket createPeerNamePacket()
		{
			return new PeerNameMessagePacket
			{
				Name = _serverInfo.PeerName
			};
		}

		ServerInformationMessagePacket createServerInfoPacket(MsexVersion? version)
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
