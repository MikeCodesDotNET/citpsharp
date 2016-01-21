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

		private readonly ICitpLogService _log;
		private readonly IPAddress _nicIp;
		private readonly bool _useOriginalMulticastIp;

		private UdpClient _client;

		private bool _isListenLoopRunning;


		public CitpUdpService(ICitpLogService log, IPAddress nicIp, bool useOriginalMulticastIp)
		{
			_log = log;

			_nicIp = nicIp;
			_useOriginalMulticastIp = useOriginalMulticastIp;
		}

		public void Dispose()
		{
			if (_client != null)
			{
				_client.Close();
				_client = null;
			}
		}

		public event EventHandler<Tuple<IPAddress, byte[]>> PacketReceived;

		public bool Start()
		{
			if (_client != null)
			{
				_client.Close();
				_client = null;
			}

			_client = new UdpClient();
			_client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

			try
			{
				_client.Client.Bind(new IPEndPoint(_nicIp, CitpUdpPort));

				_client.JoinMulticastGroup(_useOriginalMulticastIp ? CitpMulticastOriginalIp : CitpMulticastIp);
			}
			catch (SocketException ex)
			{
				_log.LogError("Failed to setup UDP socket");
				_log.LogException(ex);

				_client.Close();
				_client = null;

				return false;
			}

			listen();

			return true;
		}

		public async Task<bool> SendAsync(byte[] data)
		{
			if (_client == null)
				return false;

			try
			{
				await _client.SendAsync(data, data.Length, 
					_useOriginalMulticastIp ? CitpMulticastOriginalEndpoint : CitpMulticastEndpoint)
					.ConfigureAwait(false);
			}
			catch (ObjectDisposedException)
			{
				return false;
			}
			catch (SocketException ex)
			{
				_log.LogError("Failed to send data via UDP");
				_log.LogException(ex);
				return false;
			}

			return true;
		}

		private async void listen()
		{
			if (_isListenLoopRunning)
				return;

			try
			{
				_isListenLoopRunning = true;

				while (_client != null)
				{
					UdpReceiveResult result;

					try
					{
						result = await _client.ReceiveAsync().ConfigureAwait(false);
					}
					catch (ObjectDisposedException)
					{
						return;
					}
					catch (SocketException ex)
					{
						_log.LogError("Udp socket exception");
						_log.LogException(ex);
						continue;
					}

					if (result.RemoteEndPoint.Address.Equals(_nicIp))
						continue;

					if (PacketReceived != null)
						PacketReceived(this, Tuple.Create(result.RemoteEndPoint.Address, result.Buffer));
				}
			}
			finally
			{
				_isListenLoopRunning = false;
			}
		}
	}
}