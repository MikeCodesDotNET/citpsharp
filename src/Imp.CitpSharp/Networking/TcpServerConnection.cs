using System;
using System.Collections.Immutable;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Imp.CitpSharp.Packets;

namespace Imp.CitpSharp.Networking
{
    internal class TcpServerConnection
    {
        private readonly ICitpLogService _logger;
        private readonly TcpClient _client;

        public TcpServerConnection(ICitpLogService logger, TcpClient client)
        {
            _logger = logger;
            _client = client;
        }

        public event EventHandler<TcpServerConnection> ConnectionOpened;
        public event EventHandler<TcpServerConnection> ConnectionClosed;
        public event EventHandler<TcpPacketReceivedEventArgs> PacketReceived;

        public void Listen(CancellationToken ct)
        {
            const int bufferLength = 2048;

            var buffer = new byte[2048];

            ConnectionOpened?.Invoke(this, this);

            var stream = _client.GetStream();

            while (true)
            {
                int numBytesReceived;

                try
                {
                    numBytesReceived = stream.Read(buffer, 0, bufferLength);
                }
                catch (IOException ex)
                {
                    break;
                }
                catch (ObjectDisposedException ex)
                {
                    break;
                }

                if (numBytesReceived == 0)
                {
                    break;
                }
            }
        }

        public void SendPacket(CitpPacket packet)
        {
            var buffer = packet.ToByteArray();

            try
            {
                _client.GetStream().Write(buffer, 0, buffer.Length);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public PeerInfo Peer { get; private set; }

        public ImmutableHashSet<MsexVersion> SupportedMsexVersions { get; private set; } = ImmutableHashSet<MsexVersion>.Empty;



        private int findCitpCookie()
        {
            return 0;
        }

        private void deserializePacket()
        {
            
        }
    }
}
