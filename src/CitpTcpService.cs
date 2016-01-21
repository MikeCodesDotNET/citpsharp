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
	internal interface ICitpTcpClient
	{
		IpEndpoint RemoteEndPoint { get; }
		Task<bool> SendAsync(byte[] data);
	}



	internal sealed class CitpTcpService : IDisposable
	{
		private readonly ConcurrentDictionary<IpEndpoint, ICitpTcpClient> _clients =
			new ConcurrentDictionary<IpEndpoint, ICitpTcpClient>();

		private readonly IpEndpoint _localEndpoint;
		private readonly ICitpLogService _log;
		private readonly TcpSocketListener _listener;



		public CitpTcpService(ICitpLogService log, IpAddress nicAddress, int port)
		{
			_log = log;
			_localEndpoint = new IpEndpoint(nicAddress, port);
			_listener = new TcpSocketListener();
			_listener.ConnectionReceived += connectionReceived;
		}

		public ConcurrentDictionary<IpEndpoint, ICitpTcpClient> Clients
		{
			get { return _clients; }
		}

		public void Dispose()
		{
			_listener.Dispose();
		}



		public event EventHandler<ICitpTcpClient> ClientConnected;

		public event EventHandler<IpEndpoint> ClientDisconnected;

		public event EventHandler<MessageReceivedEventArgs> MessageReceived;


		public async Task<bool> StartAsync()
		{
			var interfaces = await CommsInterface.GetAllInterfacesAsync().ConfigureAwait(false);

			var nicInterface = interfaces.FirstOrDefault(i => i.IpAddress == _localEndpoint.Address.ToString());

			if (nicInterface == null)
			{
				_log.LogError($"Could not locate network interface with IP '{_localEndpoint.Address}'");
				return false;
			}

			await _listener.StartListeningAsync(_localEndpoint.Port, nicInterface).ConfigureAwait(false);

			return true;
		}

		private void connectionReceived(object sender, TcpSocketListenerConnectEventArgs e)
		{
			var citpClient = new CitpTcpClient(_log, e.SocketClient);

			citpClient.Disconnected += clientDisconnected;
			citpClient.MessageReceived += clientMessageReceived;
			_clients.TryAdd(citpClient.RemoteEndPoint, citpClient);

			ClientConnected?.Invoke(this, citpClient);
		}

		private void clientDisconnected(object sender, EventArgs e)
		{
			ICitpTcpClient removedClient;
			_clients.TryRemove(((CitpTcpClient)sender).RemoteEndPoint, out removedClient);

			ClientDisconnected?.Invoke(this, removedClient.RemoteEndPoint);
		}

		private void clientMessageReceived(object sender, byte[] e)
		{
			MessageReceived?.Invoke(this, new MessageReceivedEventArgs(((CitpTcpClient)sender).RemoteEndPoint, e));
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


		private class CitpTcpClient : ICitpTcpClient
		{
			private static readonly byte[] CitpSearchPattern = {0x43, 0x49, 0x54, 0x50, 0x01, 0x00};

			private readonly ITcpSocketClient _client;
			private readonly ICitpLogService _log;

			private byte[] _currentPacket;
			private int _packetBytesRemaining;


			public CitpTcpClient(ICitpLogService log, ITcpSocketClient client)
			{
				_log = log;
				_client = client;
				RemoteEndPoint = IpEndpoint.Parse(client.RemoteAddress);
			}



			public event EventHandler Disconnected;

			public event EventHandler<byte[]> MessageReceived;



			public IpEndpoint RemoteEndPoint { get; }

			public async Task<bool> SendAsync(byte[] data)
			{
				try
				{
					await _client.WriteStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
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

						Disconnected?.Invoke(this, EventArgs.Empty);
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
					if (MessageReceived != null)
						MessageReceived(this, _currentPacket);

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