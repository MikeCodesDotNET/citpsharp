using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Imp.CitpSharp.Networking;
using Imp.CitpSharp.Packets;

namespace Imp.CitpSharp
{
    public abstract class CitpService : IDisposable
    {
        public static readonly TimeSpan PeerLocationPacketInterval = TimeSpan.FromSeconds(1000);

        private bool _isDisposed;

        private readonly ICitpLogService _logger;
        private readonly UdpService _udpService;
        private readonly PeerRegistry _peerRegistry;

        private readonly RegularTimer _peerLocationTimer;

        private readonly bool _isUseLegacyMulticastIp;

        public static IEnumerable<NetworkInterface> GetNetworkInterfaces(bool isGetUpOnly = true)
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                            && (n.OperationalStatus == OperationalStatus.Up || !isGetUpOnly)
                            && !n.IsReceiveOnly && n.SupportsMulticast && n.Supports(NetworkInterfaceComponent.IPv4));
        }

        protected CitpService(ICitpLogService logger, bool isUseLegacyMulticastIp, NetworkInterface networkInterface = null)
        {
            _logger = logger;

            _isUseLegacyMulticastIp = isUseLegacyMulticastIp;

            _udpService = new UdpService();

            _peerLocationTimer = new RegularTimer(PeerLocationPacketInterval);
            _peerLocationTimer.Elapsed += (s, e) => sendPeerLocationPacket();
        }

       

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!_isDisposed)
            {
                if (isDisposing)
                {
                    _peerLocationTimer.Dispose();
                }
            }

            _isDisposed = true;
        }

        public virtual int TcpListenPort => 0;



        internal void OnUdpPacketReceived(CitpPacket packet, IPAddress address)
        {
            
        }

        internal virtual void OnPinfPacketReceived(PinfPacket packet, IPAddress address)
        {
            
        }

        internal virtual  void OnMsexPacketReceived(MsexPacket packet, IPAddress address)
        {

        }



        private void sendPeerLocationPacket()
        {
            
        }
    }
}
