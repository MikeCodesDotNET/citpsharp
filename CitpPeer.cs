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

namespace Imp.CitpSharp
{
	internal class CitpPeer : IEquatable<IPEndPoint>
	{
		public CitpPeer(IPAddress ip, string name)
		{
			Ip = ip;
			Name = name;
		}

		public IPAddress Ip;
		public int? RemoteTcpPort;
		public int? ListeningTcpPort;
		public string Name;

		public CitpPeerType Type;
		public string State;
		public bool IsConnected;
		public DateTime LastUpdateReceived;
		public MsexVersion MsexVersion = MsexVersion.Version1_0;
		public Guid? MediaServerUUID;

		public IPEndPoint RemoteEndPoint
		{
			get
			{
				if (RemoteTcpPort.HasValue == false)
					return null;
				else
					return new IPEndPoint(Ip, RemoteTcpPort.Value);
			}
		}

		public bool Equals(IPEndPoint endpoint)
		{
			if (RemoteTcpPort.HasValue == false)
				return false;

			return Ip.Equals(endpoint.Address) && RemoteTcpPort == endpoint.Port; 
		}
	}
}
