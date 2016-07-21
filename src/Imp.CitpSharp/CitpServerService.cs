using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Imp.CitpSharp.Networking;
using Imp.CitpSharp.Packets;
using Imp.CitpSharp.Packets.Msex;

namespace Imp.CitpSharp
{
    public class CitpServerService : CitpService
    {
        public static readonly TimeSpan StreamTimerInterval = TimeSpan.FromMilliseconds(1000d / 60d);

        private bool _isDisposed;

        private readonly TcpServer _tcpServer;
        private readonly RegularTimer _streamTimer;

        public CitpServerService(ICitpLogService logger, bool isUseLegacyMulticastIp,
            NetworkInterface networkInterface = null)
            : base(logger, isUseLegacyMulticastIp, networkInterface)
        {
            var localIp = IPAddress.Any;
            if (networkInterface != null)
            {
                var ip = networkInterface.GetIPProperties().UnicastAddresses.FirstOrDefault();

                if (ip == null)
                    throw new InvalidOperationException("Network interface does not have a valid IPv4 unicast address");

                localIp = ip.Address;
            }
       
            _tcpServer = new TcpServer(logger, new IPEndPoint(localIp, 0));
            _tcpServer.ConnectionOpened += OnTcpConnectionOpened;
            _tcpServer.ConnectionClosed += OnTcpConnectionClosed;
            _tcpServer.PacketReceived += OnTcpPacketReceived;

            _streamTimer = new RegularTimer(StreamTimerInterval);
            _streamTimer.Elapsed += (s, e) => ProcessStreamFrameRequests();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (!_isDisposed)
            {
                if (isDisposing)
                {
                    _streamTimer.Dispose();
                    _tcpServer.Dispose();
                }
            }

            base.Dispose(isDisposing);
            _isDisposed = true;
        }


        public void ProcessStreamFrameRequests(int? sourceId = null)
        {

        }


        internal virtual void OnTcpConnectionOpened(object sender, TcpServerConnection client)
        {
           
        }

        internal virtual void OnTcpConnectionClosed(object sender, TcpServerConnection client)
        {

        }

        internal virtual void OnTcpPacketReceived(object sender, TcpPacketReceivedEventArgs e)
        {

        }

        internal virtual void OnPinfTcpPacketReceived(PinfPacket packet, TcpServerConnection client)
        {
            
        }

        internal virtual void OnMsexTcpPacketReceived(MsexPacket packet, TcpServerConnection client)
        {

        }

        internal virtual void OnClientInformationPacketReceived(ClientInformationPacket packet, TcpServerConnection client)
        {
            
        }

        internal virtual void OnGetVideoSourcesPacketReceived(GetVideoSourcesPacket packet, TcpServerConnection client)
        {

        }

        internal virtual void OnRequestStreamPacketReceived(ClientInformationPacket packet, TcpServerConnection client)
        {

        }

        public override int TcpListenPort => _tcpServer.ListenPort;
    }
}
