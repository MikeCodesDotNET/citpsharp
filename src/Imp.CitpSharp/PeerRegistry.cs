using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using Imp.CitpSharp.Packets.Pinf;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	internal class PeerInfo : IEquatable<PeerInfo>
	{
		public PeerInfo(CitpPeerType peerType, [NotNull] string name, [NotNull] string state, ushort listeningTcpPort,
			[NotNull] IPAddress ip)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			if (state == null)
				throw new ArgumentNullException(nameof(state));
			if (ip == null)
				throw new ArgumentNullException(nameof(ip));

			PeerType = peerType;
			Name = name;
			State = state;
			ListeningTcpPort = listeningTcpPort;
			Ip = ip;
		}

		public PeerInfo([NotNull] string name, [NotNull] IPAddress ip)
			: this(CitpPeerType.Unknown, name, string.Empty, 0, ip)
		{
		}

		public CitpPeerType PeerType { get; }
		public string Name { get; }
		public string State { get; }
		public ushort ListeningTcpPort { get; }
		public IPAddress Ip { get; }

		public override string ToString() => $"{Name} ({Ip})";

		public bool Equals([CanBeNull] PeerInfo other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return PeerType == other.PeerType && string.Equals(Name, other.Name) && Ip.Equals(other.Ip);
		}

		public override bool Equals([CanBeNull] object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj.GetType() == GetType() && Equals((PeerInfo)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (int)PeerType;
				hashCode = (hashCode * 397) ^ Name.GetHashCode();
				hashCode = (hashCode * 397) ^ Ip.GetHashCode();
				return hashCode;
			}
		}
	}



	internal class PeerRegistry
	{
		static readonly TimeSpan PeerTimeout = TimeSpan.FromMilliseconds(10000);

		private readonly Dictionary<PeerInfo, DateTime> _peers = new Dictionary<PeerInfo, DateTime>();

		public ImmutableHashSet<PeerInfo> Peers => _peers.Keys.ToImmutableHashSet();

		public PeerInfo AddPeer(PeerLocationPacket packet, IPAddress ip)
		{
			var peer = new PeerInfo(packet.PeerType, packet.Name, packet.State, packet.ListeningTcpPort, ip);

			if (_peers.ContainsKey(peer))
				_peers[peer] = DateTime.Now;
			else
				_peers.Add(peer, DateTime.Now);

			return peer;
		}

		public PeerInfo AddPeer(PeerNamePacket packet, IPAddress ip)
		{
			var peer = new PeerInfo(packet.Name, ip);

			if (_peers.ContainsKey(peer))
				_peers[peer] = DateTime.Now;
			else
				_peers.Add(peer, DateTime.Now);

			return peer;
		}

		public void RemoveInactivePeers()
		{
			var now = DateTime.Now;

			foreach (var pair in _peers.ToList())
			{
				if (now - pair.Value > PeerTimeout)
					_peers.Remove(pair.Key);
			}
		}
	}
}