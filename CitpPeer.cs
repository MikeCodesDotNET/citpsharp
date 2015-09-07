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
		public DateTime LastUpdateReceived;
		public Guid? MediaServerUuid;
		public MsexVersion? MsexVersion;

		public string Name;
		public string State;

		public CitpPeerType Type;

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

		public IPAddress Ip { get; private set; }
		public int? RemoteTcpPort { get; private set; }
		public int? ListeningTcpPort { get; private set; }
		public bool IsConnected { get; private set; }

		public IPEndPoint RemoteEndPoint
		{
			get
			{
				if (RemoteTcpPort.HasValue == false)
					return null;
				return new IPEndPoint(Ip, RemoteTcpPort.Value);
			}
		}

		public bool Equals(CitpPeer other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return string.Equals(Name, other.Name) && Type == other.Type && string.Equals(State, other.State)
			       && LastUpdateReceived.Equals(other.LastUpdateReceived) && MsexVersion == other.MsexVersion
			       && MediaServerUuid.Equals(other.MediaServerUuid) && Equals(Ip, other.Ip)
			       && RemoteTcpPort == other.RemoteTcpPort && ListeningTcpPort == other.ListeningTcpPort
			       && IsConnected == other.IsConnected;
		}

		public void SetConnected(int remoteTcpPort)
		{
			RemoteTcpPort = remoteTcpPort;
			IsConnected = true;
		}

		public void SetDisconnected()
		{
			IsConnected = false;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj.GetType() == GetType() && Equals((CitpPeer)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (Name != null ? Name.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (int)Type;
				hashCode = (hashCode * 397) ^ (State != null ? State.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ LastUpdateReceived.GetHashCode();
				hashCode = (hashCode * 397) ^ MsexVersion.GetHashCode();
				hashCode = (hashCode * 397) ^ MediaServerUuid.GetHashCode();
				hashCode = (hashCode * 397) ^ (Ip != null ? Ip.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ RemoteTcpPort.GetHashCode();
				hashCode = (hashCode * 397) ^ ListeningTcpPort.GetHashCode();
				hashCode = (hashCode * 397) ^ IsConnected.GetHashCode();
				return hashCode;
			}
		}

		public override string ToString()
		{
			return string.Format("Peer: {0}, {1}:{2}", Name ?? "(Unknown)", Ip, RemoteTcpPort);
		}
	}
}