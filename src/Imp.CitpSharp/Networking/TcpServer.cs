using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Imp.CitpSharp.Packets;

namespace Imp.CitpSharp.Networking
{
    internal sealed class TcpServer : IDisposable
    {
        public const int AcceptTcpClientTimeoutMs = 250;

        private bool _isDisposed;

        private readonly ICitpLogService _logger;
        private readonly TcpListener _tcpListener;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public TcpServer(ICitpLogService logger, IPEndPoint localEndPoint)
        {
            _logger = logger;
            _tcpListener = new TcpListener(localEndPoint);

            ListenPort = localEndPoint.Port;

            Task.Run(() => listenThread(_cancellationTokenSource.Token));
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();

            _isDisposed = true;
        }

        public event EventHandler<TcpServerConnection> ConnectionOpened;
        public event EventHandler<TcpPacketReceivedEventArgs> PacketReceived;
        public event EventHandler<TcpServerConnection> ConnectionClosed;

        public int ListenPort { get; }

        private async void listenThread(CancellationToken ct)
        {
            _tcpListener.Start();

            try
            {
                while (true)
                {
                    try
                    {
                        var client = await Task.Run(() => _tcpListener.AcceptTcpClientAsync(), ct).ConfigureAwait(false);

                        var clientWrapper = new TcpServerConnection(_logger, client);
                        clientWrapper.ConnectionOpened += (s, e) => ConnectionOpened?.Invoke(s, e);
                        clientWrapper.ConnectionClosed += (s, e) => ConnectionClosed?.Invoke(s, e);
                        clientWrapper.PacketReceived += (s, e) => PacketReceived?.Invoke(s, e);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        Task.Run(() => clientWrapper.Listen(ct));
#pragma warning restore CS4014 

                    }
                    catch (SocketException ex)
                    {
                        
                    }
                }
            }
            finally
            {
                _tcpListener.Stop();
            }
        }
    }



    internal class TcpPacketReceivedEventArgs : EventArgs
    {
        public TcpPacketReceivedEventArgs(CitpPacket packet, TcpServerConnection client)
        {
            Packet = packet;
            Client = client;
        }

        public CitpPacket Packet { get; }
        public TcpServerConnection Client { get; }
    }
}
