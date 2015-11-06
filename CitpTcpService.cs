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

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Imp.CitpSharp
{
	internal interface ICitpTcpClient
	{
		IPEndPoint RemoteEndPoint { get; }
		event EventHandler Disconnected;

		event EventHandler<byte[]> PacketReceieved;
		Task<bool> SendAsync(byte[] data);
	}



	internal sealed class CitpTcpService : IDisposable
	{
		private readonly CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();

		private readonly ConcurrentDictionary<IPEndPoint, ICitpTcpClient> _clients =
			new ConcurrentDictionary<IPEndPoint, ICitpTcpClient>();

		private readonly IPEndPoint _localEndpoint;
		private readonly ICitpLogService _log;
		private TcpListener _listener;



		public CitpTcpService(ICitpLogService log, IPAddress nicAddress, int port)
		{
			_log = log;
			_localEndpoint = new IPEndPoint(nicAddress, port);
		}



		public ConcurrentDictionary<IPEndPoint, ICitpTcpClient> Clients
		{
			get { return _clients; }
		}

		public void Dispose()
		{
			if (_listener != null)
				Close();

			if (_cancelTokenSource != null)
				_cancelTokenSource.Dispose();
		}



		public event EventHandler<ICitpTcpClient> ClientConnected;

		public event EventHandler<IPEndPoint> ClientDisconnected;

		public event EventHandler<Tuple<IPEndPoint, byte[]>> PacketReceieved;



		public void Close()
		{
			_cancelTokenSource.Cancel();
			_listener.Stop();
		}

		public bool StartListening()
		{
			_listener = new TcpListener(_localEndpoint);

			try
			{
				_listener.Start();
			}
			catch (SocketException ex)
			{
				_log.LogError(string.Format("Failed to start listening on TCP port {0}, socket exception.", _localEndpoint.Port));
				_log.LogException(ex);
				return false;
			}

			acceptClientsAsync(_cancelTokenSource.Token);

			return true;
		}



		private async void acceptClientsAsync(CancellationToken cancelToken)
		{
			try
			{
				while (!cancelToken.IsCancellationRequested)
				{
					var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);

					var citpClient = new CitpTcpClient(_log, client);


					citpClient.Disconnected += citpClient_Disconnected;
					citpClient.PacketReceieved += citpClient_PacketReceieved;

					_clients.TryAdd(citpClient.RemoteEndPoint, citpClient);

					citpClient.OpenStream(cancelToken);

					if (ClientConnected != null)
						ClientConnected(this, citpClient);
				}
			}
			catch (ObjectDisposedException) { }
		}

		private void citpClient_Disconnected(object sender, EventArgs e)
		{
			ICitpTcpClient removedClient;
			_clients.TryRemove(((CitpTcpClient)sender).RemoteEndPoint, out removedClient);

			if (ClientDisconnected != null)
				ClientDisconnected(this, removedClient.RemoteEndPoint);
		}

		private void citpClient_PacketReceieved(object sender, byte[] e)
		{
			if (PacketReceieved != null)
				PacketReceieved(this, Tuple.Create(((CitpTcpClient)sender).RemoteEndPoint, e));
		}



		private class CitpTcpClient : ICitpTcpClient
		{
			private static readonly byte[] CitpSearchPattern = {0x43, 0x49, 0x54, 0x50, 0x01, 0x00};

			private readonly TcpClient _client;
			private readonly ICitpLogService _log;
			private readonly IPEndPoint _remoteEndPoint;

			private byte[] _currentPacket;
			private int _packetBytesRemaining;

			private NetworkStream _stream;


			public CitpTcpClient(ICitpLogService log, TcpClient client)
			{
				_log = log;
				_client = client;
				_remoteEndPoint = _client.Client.RemoteEndPoint as IPEndPoint;
			}



			public event EventHandler Disconnected;

			public event EventHandler<byte[]> PacketReceieved;



			public IPEndPoint RemoteEndPoint
			{
				get { return _remoteEndPoint; }
			}

			public async Task<bool> SendAsync(byte[] data)
			{
				if (_stream == null)
					throw new InvalidOperationException("Client is not ready to send data");

				try
				{
					await _stream.WriteAsync(data, 0, data.Length);
				}
				catch (IOException)
				{
					return false;
				}
				catch (SocketException)
				{
					return false;
				}
				catch (InvalidOperationException)
				{
					return false;
				}

				return true;
			}

			public async void OpenStream(CancellationToken ct)
			{
				using (_client)
				{
					try
					{
						var buffer = new byte[4096];

						try
						{
							_stream = _client.GetStream();
						}
						catch (InvalidOperationException)
						{
							_log.LogError("Couldn't get network stream");
							return;
						}

						while (!ct.IsCancellationRequested)
						{
							//under some circumstances, it's not possible to detect
							//a client disconnecting if there's no data being sent
							//so it's a good idea to give them a timeout to ensure that 
							//we clean them up.
							var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15));
							var amountReadTask = _stream.ReadAsync(buffer, 0, buffer.Length, ct);

							var completedTask = await Task.WhenAny(timeoutTask, amountReadTask)
								.ConfigureAwait(false);

							if (completedTask == timeoutTask)
							{
								_log.LogInfo("Client timed out");
								break;
							}

							int amountRead = amountReadTask.Result;

							if (amountRead == 0)
								break;

							parseCitpPackets(amountRead, buffer);
						}
					}
					catch (AggregateException)
					{
						_log.LogInfo("Client closed connection");
					}
					catch (IOException)
					{
						_log.LogInfo("Client closed connection");
					}
					finally
					{
						_stream.Dispose();
						_stream = null;

						if (Disconnected != null)
							Disconnected(this, EventArgs.Empty);
					}
				}
			}



			private int copyBytesToPacket(int srcOffset, int nBytesToCopy, byte[] source)
			{
				Buffer.BlockCopy(source, srcOffset, _currentPacket,
					_currentPacket.Length - _packetBytesRemaining,
					nBytesToCopy);

				_packetBytesRemaining -= nBytesToCopy;

				if (_packetBytesRemaining == 0)
				{
					if (PacketReceieved != null)
						PacketReceieved(this, _currentPacket);

					_currentPacket = null;
				}

				return nBytesToCopy;
			}

			private void parseCitpPackets(int nBytesReceived, byte[] source)
			{
				int i = 0;
				while (i < nBytesReceived)
				{
					if (_packetBytesRemaining > 0)
					{
						i += copyBytesToPacket(i, Math.Min(_packetBytesRemaining, nBytesReceived), source);
					}
					else if (source.Skip(i).Take(CitpSearchPattern.Length).SequenceEqual(CitpSearchPattern))
					{
						uint packetLength = BitConverter.ToUInt32(source, i + 8);

						// Ignore packets reporting their length as over 5MB, they're probably wrong
						if (packetLength > 5000000)
						{
							_log.LogWarning("Received a CITP packet with an invalid length of " + packetLength);
							continue;
						}

						_packetBytesRemaining = (int)packetLength;
						_currentPacket = new byte[packetLength];

						i += copyBytesToPacket(i, Math.Min(_packetBytesRemaining, nBytesReceived), source);
					}
					else
					{
						++i;
					}
				}
			}
		}
	}
}