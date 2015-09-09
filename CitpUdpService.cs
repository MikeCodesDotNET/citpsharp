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
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Imp.CitpSharp
{
	internal sealed class CitpUdpService : IDisposable
	{
		public static readonly int MaximumUdpPacketLength = 65507;

		private static readonly int CitpUdpPort = 4809;
		private static readonly IPAddress CitpMulticastOriginalIp = IPAddress.Parse("224.0.0.180");
		private static readonly IPAddress CitpMulticastIp = IPAddress.Parse("239.224.0.180");
		private static readonly IPEndPoint CitpMulticastOriginalEndpoint = new IPEndPoint(CitpMulticastOriginalIp, CitpUdpPort);
		private static readonly IPEndPoint CitpMulticastEndpoint = new IPEndPoint(CitpMulticastIp, CitpUdpPort);

		private readonly ICitpLogService m_log;
		private readonly IPAddress m_nicIp;
		private readonly bool m_useOriginalMulticastIp;

		private UdpClient m_client;

		private bool m_isListenLoopRunning;


		public CitpUdpService(ICitpLogService log, IPAddress nicIp, bool useOriginalMulticastIp)
		{
			m_log = log;

			m_nicIp = nicIp;
			m_useOriginalMulticastIp = useOriginalMulticastIp;
		}

		public void Dispose()
		{
			if (m_client != null)
			{
				m_client.Close();
				m_client = null;
			}
		}

		public event EventHandler<Tuple<IPAddress, byte[]>> PacketReceived;

		public bool Start()
		{
			if (m_client != null)
			{
				m_client.Close();
				m_client = null;
			}

			m_client = new UdpClient();
			m_client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

			try
			{
				m_client.Client.Bind(new IPEndPoint(m_nicIp, CitpUdpPort));

				m_client.JoinMulticastGroup(m_useOriginalMulticastIp ? CitpMulticastOriginalIp : CitpMulticastIp);
			}
			catch (SocketException ex)
			{
				m_log.LogError("Failed to setup UDP socket");
				m_log.LogException(ex);

				m_client.Close();
				m_client = null;

				return false;
			}

			listen();

			return true;
		}

		public async Task<bool> SendAsync(byte[] data)
		{
			if (m_client == null)
				return false;

			try
			{
				await m_client.SendAsync(data, data.Length, m_useOriginalMulticastIp ? CitpMulticastOriginalEndpoint : CitpMulticastEndpoint);
			}
			catch (ObjectDisposedException)
			{
				return false;
			}
			catch (SocketException ex)
			{
				m_log.LogError("Failed to send data via UDP");
				m_log.LogException(ex);
				return false;
			}

			return true;
		}

		private async void listen()
		{
			if (m_isListenLoopRunning)
				return;

			try
			{
				m_isListenLoopRunning = true;

				while (m_client != null)
				{
					UdpReceiveResult result;

					try
					{
						result = await m_client.ReceiveAsync().ConfigureAwait(false);
					}
					catch (ObjectDisposedException)
					{
						return;
					}
					catch (SocketException ex)
					{
						m_log.LogError("Udp socket exception");
						m_log.LogException(ex);
						continue;
					}

					if (result.RemoteEndPoint.Address.Equals(m_nicIp))
						continue;

					if (PacketReceived != null)
						PacketReceived(this, Tuple.Create(result.RemoteEndPoint.Address, result.Buffer));
				}
			}
			finally
			{
				m_isListenLoopRunning = false;
			}
		}
	}
}