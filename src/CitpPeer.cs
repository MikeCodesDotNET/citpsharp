using System;
using Imp.CitpSharp.Sockets;
using JetBrains.Annotations;

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

		public CitpPeer(IpAddress ip, string name)
		{
			Ip = ip;
			Name = name;

			LastUpdateReceived = DateTime.Now;
		}

		public CitpPeer(IpEndpoint remoteEndPoint)
		{
			Ip = remoteEndPoint.Address;
			SetConnected(remoteEndPoint.Port);

			LastUpdateReceived = DateTime.Now;
		}

		public IpAddress Ip { get; }
		public int? RemoteTcpPort { get; private set; }
		public int? ListeningTcpPort { get; private set; }
		public bool IsConnected { get; private set; }

		public IpEndpoint RemoteEndPoint => new IpEndpoint(Ip, RemoteTcpPort ?? 0);

		public bool Equals([CanBeNull] CitpPeer other)
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

		public override bool Equals([CanBeNull] object obj)
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
				int hashCode = Name?.GetHashCode() ?? 0;
				hashCode = (hashCode * 397) ^ (int)Type;
				hashCode = (hashCode * 397) ^ (State?.GetHashCode() ?? 0);
				hashCode = (hashCode * 397) ^ LastUpdateReceived.GetHashCode();
				hashCode = (hashCode * 397) ^ MsexVersion.GetHashCode();
				hashCode = (hashCode * 397) ^ MediaServerUuid.GetHashCode();
				hashCode = (hashCode * 397) ^ Ip.GetHashCode();
				hashCode = (hashCode * 397) ^ RemoteTcpPort.GetHashCode();
				hashCode = (hashCode * 397) ^ ListeningTcpPort.GetHashCode();
				hashCode = (hashCode * 397) ^ IsConnected.GetHashCode();
				return hashCode;
			}
		}

		public override string ToString()
		{
			return $"Peer: {Name ?? "(Unknown)"}, {Ip}:{RemoteTcpPort}";
		}
	}
}