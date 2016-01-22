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
using System.Threading;
using System.Threading.Tasks;
using Imp.CitpSharp.Sockets;
using Sockets.Plugin;
using Sockets.Plugin.Abstractions;

namespace Imp.CitpSharp
{
	internal interface IRemoteCitpTcpClient
	{
		IpEndpoint RemoteEndPoint { get; }
		Task<bool> SendAsync(byte[] data);
	}



	internal sealed class CitpTcpService : IDisposable
	{
		private readonly ConcurrentDictionary<IpEndpoint, IRemoteCitpTcpClient> _clients =
			new ConcurrentDictionary<IpEndpoint, IRemoteCitpTcpClient>();

		private readonly IpAddress _nicAddress;
		private readonly ICitpLogService _log;
		private readonly TcpSocketListener _listener;
		private readonly CancellationTokenSource _cancellationTokenSource;



		public CitpTcpService(ICitpLogService log, IpAddress nicAddress)
		{
			_log = log;
			_nicAddress = nicAddress;
			_listener = new TcpSocketListener();
			_listener.ConnectionReceived += connectionReceived;
			_cancellationTokenSource = new CancellationTokenSource();
		}

		public void Dispose()
		{
			_listener.Dispose();
		}



		public event EventHandler<IRemoteCitpTcpClient> ClientConnected;

		public event EventHandler<IpEndpoint> ClientDisconnected;

		public event EventHandler<MessageReceivedEventArgs> MessageReceived;



		public ConcurrentDictionary<IpEndpoint, IRemoteCitpTcpClient> Clients
		{
			get { return _clients; }
		}

		public int ListenPort => _listener.LocalPort;



		public async Task<bool> StartAsync()
		{
			var interfaces = await CommsInterface.GetAllInterfacesAsync().ConfigureAwait(false);

			var nicInterface = interfaces.FirstOrDefault(i => i.IpAddress == _nicAddress.ToString());

			if (nicInterface == null)
			{
				_log.LogError($"Could not locate network interface with IP '{_nicAddress}'");
				return false;
			}

			await _listener.StartListeningAsync(0, nicInterface).ConfigureAwait(false);

			return true;
		}

		private void connectionReceived(object sender, TcpSocketListenerConnectEventArgs e)
		{
			var citpClient = new RemoteCitpTcpClient(_log, e.SocketClient, _cancellationTokenSource.Token);

			citpClient.Disconnected += clientDisconnected;
			citpClient.MessageReceived += clientMessageReceived;
			_clients.TryAdd(citpClient.RemoteEndPoint, citpClient);

			ClientConnected?.Invoke(this, citpClient);
		}

		private void clientDisconnected(object sender, EventArgs e)
		{
			IRemoteCitpTcpClient removedClient;
			_clients.TryRemove(((RemoteCitpTcpClient)sender).RemoteEndPoint, out removedClient);

			ClientDisconnected?.Invoke(this, removedClient.RemoteEndPoint);
		}

		private void clientMessageReceived(object sender, byte[] e)
		{
			MessageReceived?.Invoke(this, new MessageReceivedEventArgs(((RemoteCitpTcpClient)sender).RemoteEndPoint, e));
		}



		public class MessageReceivedEventArgs : EventArgs
		{
			public MessageReceivedEventArgs(IpEndpoint endpoint, byte[] data)
			{
				Endpoint = endpoint;
				Data = data;
			}

			public IpEndpoint Endpoint { get; }
			public byte[] Data { get; }
		}


		private class RemoteCitpTcpClient : IRemoteCitpTcpClient
		{
			private const int TcpBufferSize = 2048;
			private const int TcpReadTimeoutMs = 15000;
			private static readonly byte[] CitpSearchPattern = {0x43, 0x49, 0x54, 0x50, 0x01, 0x00};

			private readonly ICitpLogService _log;
			private readonly ITcpSocketClient _client;
			private readonly CancellationToken _cancellationToken;

			private byte[] _currentPacket;
			private int _packetBytesRemaining;


			public RemoteCitpTcpClient(ICitpLogService log, ITcpSocketClient client, CancellationToken cancellationToken)
			{
				_log = log;
				_client = client;
				_cancellationToken = cancellationToken;
				RemoteEndPoint = IpEndpoint.Parse(client.RemoteAddress);

#pragma warning disable 4014
				Task.Run(openStreamAsync).ConfigureAwait(false);
#pragma warning restore 4014
			}



			public event EventHandler Disconnected;

			public event EventHandler<byte[]> MessageReceived;



			public IpEndpoint RemoteEndPoint { get; }

			public async Task<bool> SendAsync(byte[] data)
			{
				await _client.WriteStream.WriteAsync(data, 0, data.Length, _cancellationToken).ConfigureAwait(false);
				

				return true;
			}

			private async Task openStreamAsync()
			{
				var buffer = new byte[TcpBufferSize];

				while (!_cancellationToken.IsCancellationRequested)
				{
					var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(TcpReadTimeoutMs));
					var amountReadTask = _client.ReadStream.ReadAsync(buffer, 0, buffer.Length, _cancellationToken);

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

				Disconnected?.Invoke(this, EventArgs.Empty);
			}



			private int copyBytesToPacket(int srcOffset, int nBytesToCopy, byte[] source)
			{
				Buffer.BlockCopy(source, srcOffset, _currentPacket,
					_currentPacket.Length - _packetBytesRemaining,
					nBytesToCopy);

				_packetBytesRemaining -= nBytesToCopy;

				if (_packetBytesRemaining == 0)
				{
					MessageReceived?.Invoke(this, _currentPacket);
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