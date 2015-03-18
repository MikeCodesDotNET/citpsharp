using System;
using System.Net;

namespace Imp.CitpSharp
{
	class CitpPeer : IEquatable<IPEndPoint>
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
