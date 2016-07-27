using System;
using System.Net;
using Imp.CitpSharp.Networking;
using Imp.CitpSharp.Packets;
using Imp.CitpSharp.Packets.Pinf;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
    /// <summary>
    ///     Base class for CITP services implementing multicast UDP and CITP peer discovery services
    /// </summary>
    [PublicAPI]
    public abstract class CitpService : IDisposable
    {
        public static readonly TimeSpan PeerLocationPacketInterval = TimeSpan.FromSeconds(1);
        private readonly ICitpDevice _device;

        private readonly bool _isUseLegacyMulticastIp;

		private readonly RegularTimer _peerLocationTimer;

        
        

        private bool _isDisposed;

        protected CitpService(ICitpLogService logger, ICitpDevice device, CitpServiceFlags flags, IPAddress localIp = null)
        {
            Logger = logger;
            _device = device;

	        Flags = flags;

            _isUseLegacyMulticastIp = flags.HasFlag(CitpServiceFlags.UseLegacyMulticastIp);

            UdpService = new UdpService(logger, _isUseLegacyMulticastIp, localIp);
			UdpService.PacketReceived += (s, e) => onUdpPacketReceived(e.Packet, e.Ip);

            _peerLocationTimer = new RegularTimer(PeerLocationPacketInterval);
            _peerLocationTimer.Elapsed += (s, e) => SendPeerLocationPacket();
            _peerLocationTimer.Start();
        }

        public abstract CitpPeerType DeviceType { get; }

		public CitpServiceFlags Flags { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!_isDisposed)
            {
                if (isDisposing)
                {
                    _peerLocationTimer.Dispose();
                    UdpService.Dispose();
                }
            }

            _isDisposed = true;
        }


		// TODO: Change accessors on these to 'private protected' when C#7 comes out
	    internal ICitpLogService Logger { get; }
	    internal PeerRegistry PeerRegistry { get; } = new PeerRegistry();
	    internal UdpService UdpService { get; }


        protected virtual void SendPeerLocationPacket()
        {
            UdpService.SendPacket(new PeerLocationPacket(false, 0, DeviceType, _device.PeerName, _device.State));
        }

        internal virtual void OnPinfPacketReceived(PinfPacket packet, IPAddress ip)
        {
            switch (packet.MessageType)
            {
                case PinfMessageType.PeerLocationMessage:
                    Logger.LogDebug($"PINF Peer Location packet received from {ip}");
					PeerRegistry.AddPeer((PeerLocationPacket)packet, ip);
                    break;

                case PinfMessageType.PeerNameMessage:
					Logger.LogDebug($"PINF Peer Name packet received from {ip}");
					PeerRegistry.AddPeer((PeerNamePacket)packet, ip);
                    break;
            }
        }

        internal virtual void OnMsexPacketReceived(MsexPacket packet, IPAddress ip) { }



        private void onUdpPacketReceived(CitpPacket packet, IPAddress ip)
        {
            switch (packet.LayerType)
            {
                case CitpLayerType.PeerInformationLayer:
                    OnPinfPacketReceived((PinfPacket)packet, ip);
                    break;

                case CitpLayerType.MediaServerExtensionsLayer:
                    OnMsexPacketReceived((MsexPacket)packet, ip);
                    break;
            }
        }
    }
}