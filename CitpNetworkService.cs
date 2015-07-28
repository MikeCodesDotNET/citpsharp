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

		bool _useOriginalMulticastIp;

		int _tcpListenPort;
		
		CitpUdpService _udpService;
		CitpTcpListenService _tcpListenService;

		ICitpMediaServerInfo _serverInfo;

		


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

			_tcpListenService = new CitpTcpListenService(_nicAddress, _tcpListenPort, _log);
			_tcpListenService.ClientConnect += tcpListenService_ClientConnect;
			_tcpListenService.ClientDisconnect += tcpListenService_ClientDisconnect;
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
		public List<CitpPeer> Peers
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




		public void SendPacket(CitpPacket packet, CitpPeer peer, int requestResponseIndex = 0)
		{
			if (peer.IsConnected == false)
				throw new InvalidOperationException("Cannot send packet, peer is not connected");

			packet.MessagePart = 0;
			packet.MessagePartCount = 1;
			packet.RequestResponseIndex = Convert.ToUInt16(requestResponseIndex);

			if (packet.LayerType == CitpLayerType.MediaServerExtensionsLayer)
			{
				var msexPacket = packet as CitpMsexPacket;

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

			sendDataToPeer(peer, packet.ToByteArray());
		}

		public void SendPacketToAllConnectedPeers(CitpPacket packet)
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

						peersWithVersion.ForEach(p => sendDataToPeer(p, data));
					}
				}
			}
			else
			{
				foreach (var peer in connectedPeers)
					SendPacket(packet, peer);
			}
		}

		public void SendMulticastPacket(CitpPacket packet, int requestResponseIndex = 0)
		{
			// TODO: Deal with packet splitting

			packet.MessagePart = 0;
			packet.MessagePartCount = 1;
			packet.RequestResponseIndex = Convert.ToUInt16(requestResponseIndex);

			byte[] data = packet.ToByteArray();

			_udpService.Send(data);
		}

		



		void tcpListenService_ClientConnect(object sender, ConnectedClient e)
		{
			//var peer = Peers.FirstOrDefault(p => new IPEndPoint(p.Ip, p.RemoteTcpPort.Value) == e.ClientSocket.RemoteEndPoint);

			//if (peer != null)
			//	throw new InvalidOperationException(String.Format("Peer on {0} is already connected", e.ToString()));

			var peerNamePacket = createPeerNamePacket();
			e.Send(peerNamePacket.ToByteArray());

			var serverInfoPacket = createServerInfoPacket(MsexVersion.Version1_0);
			e.Send(serverInfoPacket.ToByteArray());
		}

		void tcpListenService_ClientDisconnect(object sender, IPEndPoint e)
		{
			var peer = Peers.FirstOrDefault(p => p.Equals(e));

			if (peer == null)
				throw new InvalidOperationException(String.Format("No peer registered on {0}", e.ToString()));

			peer.IsConnected = false;
			peer.LastUpdateReceived = DateTime.Now;
		}

		void tcpListenService_PacketReceived(object sender, Tuple<IPEndPoint, byte[]> e)
		{
			CitpPacket packet = null;

			try
			{
				packet = CitpPacket.FromByteArray(e.Item2);
			}
			catch (Exception ex)
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

			var peer = Peers.FirstOrDefault(p => p.Equals(e.Item1));

			if (peer == null)
				throw new InvalidOperationException("Message received via TCP from unrecognised peer.");

			if (packet is ClientInformationMessagePacket)
			{
				receivedClientInformationMessage(packet as ClientInformationMessagePacket, peer);
			}
			else
			{
				

				MessageQueue.Enqueue(Tuple.Create(peer, packet));
			}
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

		void receivedPeerNameMessage(PeerNameMessagePacket message, IPEndPoint remoteEndpoint)
		{
			var peer = Peers.FirstOrDefault(p => p.Ip.Equals(remoteEndpoint.Address) && p.Name == message.Name);

			if (peer == null)
			{
				peer = new CitpPeer(remoteEndpoint.Address, message.Name);
				Peers.Add(peer);
			}

			peer.RemoteTcpPort = remoteEndpoint.Port;
			peer.IsConnected = true;
			peer.LastUpdateReceived = DateTime.Now;
		}

		void receivedPeerLocationMessage(PeerLocationMessagePacket message, IPAddress remoteIp)
		{
			if (remoteIp.Equals(_nicAddress) && message.Name == _serverInfo.PeerName)
				return;

			var peer = Peers.FirstOrDefault(p => p.Ip.Equals(remoteIp) && p.Name == message.Name);

			if (peer == null)
			{
				peer = new CitpPeer(remoteIp, message.Name);
				Peers.Add(peer);
			}

			peer.Type = message.Type;
			peer.State = message.State;
			peer.LastUpdateReceived = DateTime.Now;
		}

		void receivedClientInformationMessage(ClientInformationMessagePacket message, CitpPeer peer)
		{
			if (message.SupportedMsexVersions.Contains(MsexVersion.Version1_2))
			{
				peer.MsexVersion = MsexVersion.Version1_2;
				var packet = createServerInfoPacket(MsexVersion.Version1_2);
				SendPacket(packet, peer, message.RequestResponseIndex);
			}
		}

		void removeInactivePeers()
		{
			Peers.RemoveAll(p => p.IsConnected == false && (DateTime.Now - p.LastUpdateReceived).TotalSeconds > CITP_PEER_EXPIRY_TIME);
		}

		void sendDataToPeer(CitpPeer peer, byte[] data)
		{
			ConnectedClient client;
			if (_tcpListenService.Clients.TryGetValue(peer.RemoteEndPoint, out client))
				client.Send(data);
			else
				throw new InvalidOperationException("Cannot send packet, peer is not connected.");
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
				SupportedMsexVersions = _serverInfo.SupportedMsexVersions,
				SupportedLibraryTypes = _serverInfo.SupportedLibraryTypes,
				ThumbnailFormats = _serverInfo.SupportedThumbnailFormats,
				StreamFormats = _serverInfo.SupportedStreamFormats,
				LayerDmxSources = _serverInfo.Layers.Select(l => l.DmxSource).ToList()
			};
		}
	}
}
