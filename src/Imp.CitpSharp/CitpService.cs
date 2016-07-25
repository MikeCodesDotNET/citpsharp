using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Imp.CitpSharp.Networking;
using Imp.CitpSharp.Packets;
using Imp.CitpSharp.Packets.Pinf;

namespace Imp.CitpSharp
{
    /// <summary>
    ///     Base class for CITP services implementing multicast UDP and CITP peer discovery services
    /// </summary>
    public abstract class CitpService : IDisposable
    {
        public static readonly TimeSpan PeerLocationPacketInterval = TimeSpan.FromSeconds(1);
        private readonly ICitpDevice _device;

        private readonly bool _isUseLegacyMulticastIp;

        private readonly ICitpLogService _logger;

        private readonly RegularTimer _peerLocationTimer;
        private readonly PeerRegistry _peerRegistry = new PeerRegistry();

        private readonly UdpService _udpService;

        private bool _isDisposed;

        protected CitpService(ICitpLogService logger, ICitpDevice device, bool isUseLegacyMulticastIp,
            NetworkInterface networkInterface = null)
        {
            _logger = logger;
            _device = device;

            _isUseLegacyMulticastIp = isUseLegacyMulticastIp;

            _udpService = new UdpService(isUseLegacyMulticastIp, networkInterface);
            _udpService.PacketReceived += (s, e) => onUdpPacketReceived(e.Packet, e.Ip);

            _peerLocationTimer = new RegularTimer(PeerLocationPacketInterval);
            _peerLocationTimer.Elapsed += (s, e) => SendPeerLocationPacket();
            _peerLocationTimer.Start();
        }

        public abstract CitpPeerType DeviceType { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public static IEnumerable<NetworkInterface> GetNetworkInterfaces(bool isGetUpOnly = true)
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                            && (n.OperationalStatus == OperationalStatus.Up || !isGetUpOnly)
                            && !n.IsReceiveOnly && n.SupportsMulticast && n.Supports(NetworkInterfaceComponent.IPv4));
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!_isDisposed)
            {
                if (isDisposing)
                {
                    _peerLocationTimer.Dispose();
                    _udpService.Dispose();
                }
            }

            _isDisposed = true;
        }



        internal void SendUdpPacket(CitpPacket packet)
        {
            _udpService.SendPacket(packet);
        }

        protected virtual void SendPeerLocationPacket()
        {
            SendUdpPacket(new PeerLocationPacket(false, 0, DeviceType, _device.PeerName, _device.State));
        }

        internal virtual void OnPinfPacketReceived(PinfPacket packet, IPAddress ip)
        {
            switch (packet.MessageType)
            {
                case PinfMessageType.PeerLocationMessage:
                    _logger.LogDebug($"PINF Peer Location packet received from {ip}");
                    _peerRegistry.AddPeer((PeerLocationPacket)packet, ip);
                    break;

                case PinfMessageType.PeerNameMessage:
                    _logger.LogDebug($"PINF Peer Name packet received from {ip}");
                    _peerRegistry.AddPeer((PeerNamePacket)packet, ip);
                    break;
            }
        }

        internal virtual void OnMsexPacketReceived(MsexPacket packet, IPAddress ip) { }



        private void onUdpPacketReceived(CitpPacket packet, IPAddress ip)
        {
            switch (packet.LayerType)
            {
                case CitpLayerType.PeerInformationLayer:
                    OnPinfPacketReceived((PinfPacket)packet, ip);
                    break;

                case CitpLayerType.MediaServerExtensionsLayer:
                    OnMsexPacketReceived((MsexPacket)packet, ip);
                    break;
            }
        }
    }
}