using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using Imp.CitpSharp.Networking;
using Imp.CitpSharp.Packets;
using Imp.CitpSharp.Packets.Msex;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
    /// <summary>
    ///     Runs CITP services for a media server device.
    /// </summary>
    [PublicAPI]
    public class CitpMediaServerService : CitpServerService
    {
        private readonly ICitpMediaServerDevice _device;

        public CitpMediaServerService(ICitpLogService logger, ICitpMediaServerDevice device, bool isUseLegacyMulticastIp, bool isRunStreamTimer,
			NetworkInterface networkInterface = null)
            : base(logger, device, isUseLegacyMulticastIp, isRunStreamTimer, networkInterface)
        {
            _device = device;
        }

        public override CitpPeerType DeviceType => CitpPeerType.MediaServer;

	    internal override void OnClientInformationPacketReceived(ClientInformationPacket packet, TcpServerConnection client)
	    {
		    base.OnClientInformationPacketReceived(packet, client);

		    var responsePacket = new ServerInformationPacket(packet.Version, _device.Uuid, _device.ProductName, 
				(byte)_device.ProductVersionMajor, (byte)_device.ProductVersionMinor, (byte)_device.ProductVersionBugfix,
			    _device.SupportedMsexVersions, _device.SupportedLibraryTypes, _device.SupportedThumbnailFormats,
			    _device.SupportedStreamFormats, _device.Layers.Select(l => l.DmxSource), packet.RequestResponseIndex);

			client.SendPacket(responsePacket);
	    }


	    internal override void OnMsexTcpPacketReceived(MsexPacket packet, TcpServerConnection client)
	    {
		    switch (packet.MessageType)
		    {
			    case MsexMessageType.GetElementLibraryInformationMessage:
					onGetElementLibraryInformationPacketReceived((GetElementLibraryInformationPacket)packet, client);
				    break;

				case MsexMessageType.GetElementInformationMessage:
					onGetElementInformationPacketReceived((GetElementInformationPacket)packet, client);
				    break;

				case MsexMessageType.GetElementLibraryThumbnailMessage:
					onGetElementLibraryThumbnailPacketReceived((GetElementLibraryThumbnailPacket)packet, client);
				    break;

				case MsexMessageType.GetElementThumbnailMessage:
					onGetElementThumbnailPacketReceived((GetElementThumbnailPacket)packet, client);
				    break;

				default:
					base.OnMsexTcpPacketReceived(packet, client);
				    break;
		    }
	    }

	    private void onGetElementLibraryInformationPacketReceived(GetElementLibraryInformationPacket packet,
		    TcpServerConnection client)
	    {
		    Logger.LogInfo($"{client}: Get element library information packet received");

		  
	    }

		private void onGetElementInformationPacketReceived(GetElementInformationPacket packet,
			TcpServerConnection client)
		{
			Logger.LogInfo($"{client}: Get element information packet received");

			
			
		}

		private void onGetElementLibraryThumbnailPacketReceived(GetElementLibraryThumbnailPacket packet,
			TcpServerConnection client)
		{
			Logger.LogInfo($"{client}: Get element library thumbnail packet received");

			
		}

		private void onGetElementThumbnailPacketReceived(GetElementThumbnailPacket packet,
			TcpServerConnection client)
		{
			Logger.LogInfo($"{client}: Get element thumbnail packet received");
		}
	}
}