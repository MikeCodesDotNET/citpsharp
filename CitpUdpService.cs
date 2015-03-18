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

namespace Imp.CitpSharp
{
	class CitpUdpService
	{
		static readonly int CITP_UDP_PORT = 4809;
		static readonly IPAddress CITP_MULTICAST_ORIGINAL_IP = IPAddress.Parse("224.0.0.180");
		static readonly IPAddress CITP_MULTICAST_IP = IPAddress.Parse("239.224.0.180");
		static readonly IPEndPoint CITP_MULTICAST_ORIGINAL_ENDPOINT = new IPEndPoint(CITP_MULTICAST_ORIGINAL_IP, CITP_UDP_PORT);
		static readonly IPEndPoint CITP_MULTICAST_ENDPOINT = new IPEndPoint(CITP_MULTICAST_IP, CITP_UDP_PORT);

		UdpClient _client;
		bool _useOriginalMulticastIp;
		IPAddress _nicIp;

		public CitpUdpService(IPAddress nicIp, bool useOriginalMulticastIp)
		{
			_nicIp = nicIp;
			_useOriginalMulticastIp = useOriginalMulticastIp;

			_client = new UdpClient();
			_client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			_client.Client.Bind(new IPEndPoint(_nicIp, CITP_UDP_PORT));

			if (_useOriginalMulticastIp)
				_client.JoinMulticastGroup(CITP_MULTICAST_ORIGINAL_IP);
			else
				_client.JoinMulticastGroup(CITP_MULTICAST_IP);

			_client.BeginReceive(new AsyncCallback(packetReceived), null);
		}

		public event EventHandler<Tuple<IPAddress, byte[]>> PacketReceived;

		public void Send(byte[] data)
		{
			if (_useOriginalMulticastIp)
				_client.Send(data, data.Length, CITP_MULTICAST_ORIGINAL_ENDPOINT);
			else
				_client.Send(data, data.Length, CITP_MULTICAST_ENDPOINT);
		}

		void packetReceived(IAsyncResult res)
		{
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, CITP_UDP_PORT);
			byte[] data = _client.EndReceive(res, ref endpoint);

			if (PacketReceived != null)
				PacketReceived(this, Tuple.Create(endpoint.Address, data));

			_client.BeginReceive(new AsyncCallback(packetReceived), null);
		}
	}
}
