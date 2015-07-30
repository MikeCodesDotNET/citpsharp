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
	internal class CitpPeer : IEquatable<CitpPeer>
	{
		public CitpPeer(IPAddress ip, string name)
		{
			Ip = ip;
			Name = name;

			LastUpdateReceived = DateTime.Now;
		}

		public CitpPeer(IPEndPoint remoteEndPoint)
		{
			Ip = remoteEndPoint.Address;
			SetConnected(remoteEndPoint.Port);

			LastUpdateReceived = DateTime.Now;
		}

		public void SetConnected(int remoteTcpPort)
		{
			RemoteTcpPort = remoteTcpPort;
			IsConnected = true;
		}

		public void SetDisconnected()
		{
			RemoteTcpPort = null;
			IsConnected = false;
		}

		public IPAddress Ip { get; private set; }
		public int? RemoteTcpPort { get; private set; }
		public int? ListeningTcpPort { get; private set; }
		public bool IsConnected { get; private set; }

		public string Name;

		public CitpPeerType Type;
		public string State;
		
		public DateTime LastUpdateReceived;
		public MsexVersion MsexVersion = MsexVersion.Version1_0;
		public Guid? MediaServerUuid;

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

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			if (!(obj is CitpPeer))
				return false;

			return Equals((CitpPeer)obj);
		}

		public bool Equals(CitpPeer other)
		{
			if (other == null)
				return false;

			return Ip.Equals(other.Ip)
				&& RemoteTcpPort == other.RemoteTcpPort
				&& ListeningTcpPort == other.ListeningTcpPort
				&& Name == other.Name
				&& Type == other.Type
				&& State == other.State
				&& IsConnected == other.IsConnected
				&& LastUpdateReceived == other.LastUpdateReceived
				&& MsexVersion == other.MsexVersion
				&& MediaServerUuid == other.MediaServerUuid;
		}

		public override int GetHashCode()
		{
			return (Ip != null ? Ip.GetHashCode() : 0)
				^ (RemoteTcpPort != null ? RemoteTcpPort.GetHashCode() : 0)
				^ (ListeningTcpPort != null ? ListeningTcpPort.GetHashCode() : 0)
				^ (Name != null ? Name.GetHashCode() : 0)
				^ Type.GetHashCode()
				^ State.GetHashCode()
				^ IsConnected.GetHashCode()
				^ LastUpdateReceived.GetHashCode()
				^ MsexVersion.GetHashCode()
				^ (MediaServerUuid != null ? MediaServerUuid.GetHashCode() : 0);
		}

		public override string ToString()
		{
			return String.Format("Peer: {0}, {1}:{2}", Name ?? "(Unknown)", Ip, RemoteTcpPort);
		}
	}
}
