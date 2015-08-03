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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Imp.CitpSharp
{
	internal sealed class CitpUdpService : IDisposable
	{
		public static readonly int MaximumUdpPacketLength = 65507;

		static readonly int CITP_UDP_PORT = 4809;
		static readonly IPAddress CITP_MULTICAST_ORIGINAL_IP = IPAddress.Parse("224.0.0.180");
		static readonly IPAddress CITP_MULTICAST_IP = IPAddress.Parse("239.224.0.180");
		static readonly IPEndPoint CITP_MULTICAST_ORIGINAL_ENDPOINT = new IPEndPoint(CITP_MULTICAST_ORIGINAL_IP, CITP_UDP_PORT);
		static readonly IPEndPoint CITP_MULTICAST_ENDPOINT = new IPEndPoint(CITP_MULTICAST_IP, CITP_UDP_PORT);

		readonly ICitpLogService _log;
		readonly IPAddress _nicIp;
		readonly bool _useOriginalMulticastIp;

		UdpClient _client;

		bool _isListenLoopRunning;


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
				_client.Client.Bind(new IPEndPoint(_nicIp, CITP_UDP_PORT));

				if (_useOriginalMulticastIp)
					_client.JoinMulticastGroup(CITP_MULTICAST_ORIGINAL_IP);
				else
					_client.JoinMulticastGroup(CITP_MULTICAST_IP);

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

		public async Task<bool> Send(byte[] data)
		{
			if (_client == null)
				return false;

			try
			{
				if (_useOriginalMulticastIp)
					await _client.SendAsync(data, data.Length, CITP_MULTICAST_ORIGINAL_ENDPOINT);
				else
					await _client.SendAsync(data, data.Length, CITP_MULTICAST_ENDPOINT);
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

		async void listen()
		{
			if (_isListenLoopRunning == true)
				return;

			try
			{
				_isListenLoopRunning = true;

				while (_client != null)
				{
					UdpReceiveResult result;

					try
					{
						result = await _client.ReceiveAsync();
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
