using System;
using System.Collections.Generic;
using Imp.CitpSharp.Sockets;

namespace Imp.CitpSharp
{
	internal class CitpPeer
	{
		private readonly HashSet<int> _remoteTcpPorts = new HashSet<int>(); 

		public CitpPeer(IpAddress ip, string name)
		{
			Ip = ip;
			Name = name;

			LastUpdateReceived = DateTime.Now;
		}

		public void AddTcpConnection(int tcpPort)
		{
			_remoteTcpPorts.Add(tcpPort);
		}

		public void RemoveTcpConnection(int tcpPort)
		{
			_remoteTcpPorts.Remove(tcpPort);
		}

		public DateTime LastUpdateReceived { get; set; }
		public Guid? MediaServerUuid { get; set; }
		public MsexVersion? MsexVersion { get; set; }

		public string Name { get; set; }
		public string State { get; set; }

		public CitpPeerType Type { get; set; }

		public IpAddress Ip { get; }

		public IEnumerable<int> RemoteTcpPorts => _remoteTcpPorts;

		public int? ListeningTcpPort { get; set; }

		public bool IsConnected => _remoteTcpPorts.Count > 0;

		public override string ToString()
		{
			return $"{Name ?? "(Unknown)"}, {Ip}";
		}
	}
}