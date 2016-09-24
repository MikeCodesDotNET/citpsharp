using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
		private bool _isDisposed;

		private readonly ICitpLogService _logger;
		private readonly TcpListener _tcpListener;
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		private ImmutableHashSet<TcpServerConnection> _clients = ImmutableHashSet<TcpServerConnection>.Empty;

	    public static bool IsTcpPortAvailable(int port)
			=> IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().All(e => e.Port != port);

		public TcpServer(ICitpLogService logger, IPEndPoint localEndPoint)
		{
			_logger = logger;
			_tcpListener = new TcpListener(localEndPoint);

			_tcpListener.Start();
	        ListenPort = ((IPEndPoint)_tcpListener.LocalEndpoint).Port;

			listen(_cancellationTokenSource.Token);
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

	    public IEnumerable<TcpServerConnection> ConnectedClients => _clients;

		private async void listen(CancellationToken ct)
		{
			try
			{
				while (true)
				{
					try
					{
						var client = await Task.Run(() => _tcpListener.AcceptTcpClientAsync(), ct).ConfigureAwait(false);

						var clientWrapper = new TcpServerConnection(_logger, client);
	                    _clients = _clients.Add(clientWrapper);

						clientWrapper.ConnectionOpened += (s, e) => ConnectionOpened?.Invoke(s, e);
						clientWrapper.ConnectionClosed += (s, e) => ConnectionClosed?.Invoke(s, e);
						clientWrapper.ConnectionClosed += onClientWrapperClosed;

						clientWrapper.PacketReceived += (s, e) => PacketReceived?.Invoke(s, e);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
						Task.Run(() => clientWrapper.Listen(ct));
#pragma warning restore CS4014 

					}
					catch (SocketException ex)
					{
						_logger.LogError("Socket exception in TCP server");
						_logger.LogException(ex);
					}
				}
			}
			finally
			{
				_tcpListener.Stop();
			}
		}

		private void onClientWrapperClosed(object sender, TcpServerConnection e)
		{
			_clients = _clients.Remove(e);
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
