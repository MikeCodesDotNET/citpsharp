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
		private readonly CancellationTokenSource m_cancelTokenSource = new CancellationTokenSource();

		private readonly ConcurrentDictionary<IPEndPoint, ICitpTcpClient> m_clients =
			new ConcurrentDictionary<IPEndPoint, ICitpTcpClient>();

		private readonly IPEndPoint m_localEndpoint;
		private readonly ICitpLogService m_log;
		private TcpListener m_listener;



		public CitpTcpService(ICitpLogService log, IPAddress nicAddress, int port)
		{
			m_log = log;
			m_localEndpoint = new IPEndPoint(nicAddress, port);
		}



		public ConcurrentDictionary<IPEndPoint, ICitpTcpClient> Clients
		{
			get { return m_clients; }
		}

		public void Dispose()
		{
			if (m_listener != null)
				Close();

			if (m_cancelTokenSource != null)
				m_cancelTokenSource.Dispose();
		}



		public event EventHandler<ICitpTcpClient> ClientConnected;

		public event EventHandler<IPEndPoint> ClientDisconnected;

		public event EventHandler<Tuple<IPEndPoint, byte[]>> PacketReceieved;



		public void Close()
		{
			m_cancelTokenSource.Cancel();
			m_listener.Stop();
		}

		public bool StartListening()
		{
			m_listener = new TcpListener(m_localEndpoint);

			try
			{
				m_listener.Start();
			}
			catch (SocketException ex)
			{
				m_log.LogError(string.Format("Failed to start listening on TCP port {0}, socket exception.", m_localEndpoint.Port));
				m_log.LogException(ex);
				return false;
			}

			acceptClientsAsync(m_cancelTokenSource.Token);

			return true;
		}



		private async void acceptClientsAsync(CancellationToken cancelToken)
		{
			try
			{
				while (!cancelToken.IsCancellationRequested)
				{
					var client = await m_listener.AcceptTcpClientAsync().ConfigureAwait(false);

					var citpClient = new CitpTcpClient(m_log, client);


					citpClient.Disconnected += citpClient_Disconnected;
					citpClient.PacketReceieved += citpClient_PacketReceieved;

					m_clients.TryAdd(citpClient.RemoteEndPoint, citpClient);

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
			m_clients.TryRemove((sender as CitpTcpClient).RemoteEndPoint, out removedClient);

			if (ClientDisconnected != null)
				ClientDisconnected(this, removedClient.RemoteEndPoint);
		}

		private void citpClient_PacketReceieved(object sender, byte[] e)
		{
			if (PacketReceieved != null)
				PacketReceieved(this, Tuple.Create((sender as CitpTcpClient).RemoteEndPoint, e));
		}



		private class CitpTcpClient : ICitpTcpClient
		{
			private static readonly byte[] CitpSearchPattern = {0x43, 0x49, 0x54, 0x50, 0x01, 0x00};

			private readonly TcpClient m_client;
			private readonly ICitpLogService m_log;
			private readonly IPEndPoint m_remoteEndPoint;

			private byte[] m_currentPacket;
			private int m_packetBytesRemaining;

			private NetworkStream m_stream;


			public CitpTcpClient(ICitpLogService log, TcpClient client)
			{
				m_log = log;
				m_client = client;
				m_remoteEndPoint = m_client.Client.RemoteEndPoint as IPEndPoint;
			}



			public event EventHandler Disconnected;

			public event EventHandler<byte[]> PacketReceieved;



			public IPEndPoint RemoteEndPoint
			{
				get { return m_remoteEndPoint; }
			}

			public async Task<bool> SendAsync(byte[] data)
			{
				if (m_stream == null)
					throw new InvalidOperationException("Client is not ready to send data");

				try
				{
					await m_stream.WriteAsync(data, 0, data.Length);
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
				using (m_client)
				{
					try
					{
						var buffer = new byte[4096];

						try
						{
							m_stream = m_client.GetStream();
						}
						catch (InvalidOperationException)
						{
							m_log.LogError("Couldn't get network stream");
							return;
						}

						while (!ct.IsCancellationRequested)
						{
							//under some circumstances, it's not possible to detect
							//a client disconnecting if there's no data being sent
							//so it's a good idea to give them a timeout to ensure that 
							//we clean them up.
							var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15));
							var amountReadTask = m_stream.ReadAsync(buffer, 0, buffer.Length, ct);

							var completedTask = await Task.WhenAny(timeoutTask, amountReadTask)
								.ConfigureAwait(false);

							if (completedTask == timeoutTask)
							{
								m_log.LogInfo("Client timed out");
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
						m_log.LogInfo("Client closed connection");
					}
					catch (IOException)
					{
						m_log.LogInfo("Client closed connection");
					}
					finally
					{
						m_stream.Dispose();
						m_stream = null;

						if (Disconnected != null)
							Disconnected(this, EventArgs.Empty);
					}
				}
			}



			private int copyBytesToPacket(int srcOffset, int nBytesToCopy, byte[] source)
			{
				Buffer.BlockCopy(source, srcOffset, m_currentPacket,
					m_currentPacket.Length - m_packetBytesRemaining,
					nBytesToCopy);

				m_packetBytesRemaining -= nBytesToCopy;

				if (m_packetBytesRemaining == 0)
				{
					if (PacketReceieved != null)
						PacketReceieved(this, m_currentPacket);

					m_currentPacket = null;
				}

				return nBytesToCopy;
			}

			private void parseCitpPackets(int nBytesReceived, byte[] source)
			{
				int i = 0;
				while (i < nBytesReceived)
				{
					if (m_packetBytesRemaining > 0)
					{
						i += copyBytesToPacket(i, Math.Min(m_packetBytesRemaining, nBytesReceived), source);
					}
					else if (source.Skip(i).Take(CitpSearchPattern.Length).SequenceEqual(CitpSearchPattern))
					{
						uint packetLength = BitConverter.ToUInt32(source, i + 8);

						// Ignore packets reporting their length as over 5MB, they're probably wrong
						if (packetLength > 5000000)
						{
							m_log.LogWarning("Received a CITP packet with an invalid length of " + packetLength);
							continue;
						}

						m_packetBytesRemaining = (int)packetLength;
						m_currentPacket = new byte[packetLength];

						i += copyBytesToPacket(i, Math.Min(m_packetBytesRemaining, nBytesReceived), source);
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