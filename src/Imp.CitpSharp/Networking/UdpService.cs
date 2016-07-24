using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Imp.CitpSharp.Packets;

namespace Imp.CitpSharp.Networking
{
    internal sealed class UdpService : IDisposable
    {
	    const int BufferLength = 65536;
	    const int CitpUdpPort = 4809;
	    static readonly IPAddress CitpMulticastIp = IPAddress.Parse("239.224.0.180");
	    static readonly IPAddress CitpMulticastLegacyIp = IPAddress.Parse("224.0.0.180");

	    private bool _isDisposed;
	    private readonly UdpClient _client;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly Task _listenTask;

	    public UdpService(bool isUseLegacyMulticastIp, NetworkInterface networkInterface = null)
	    {
	        MulticastIp = isUseLegacyMulticastIp ? CitpMulticastLegacyIp : CitpMulticastIp;

	        _client = new UdpClient(CitpUdpPort)
	        {
	            MulticastLoopback = false,
	            ExclusiveAddressUse = false
	        };

	        if (networkInterface != null)
	            _client.JoinMulticastGroup(MulticastIp, networkInterface.GetIPProperties().UnicastAddresses.First().Address);
            else
                _client.JoinMulticastGroup(MulticastIp);

            _listenTask = listenAsync(_cancellationTokenSource.Token);
	    }

	    public void Dispose()
	    {
		    if (_isDisposed)
			    return;

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
	        _listenTask.Wait();
			_client.Dispose();

		    _isDisposed = true;
	    }


        public event EventHandler<CitpUdpPacketReceivedEventArgs> PacketReceived;

        public IPAddress MulticastIp { get; }


        public void SendPacket(CitpPacket packet)
        {
            var data = packet.ToByteArray();
            _client.SendAsync(data, data.Length, new IPEndPoint(MulticastIp, CitpUdpPort)).Wait();
        }

        private async Task listenAsync(CancellationToken ct)
        {
            try
            {
                while (true)
                {
                    try
                    {
                        var result = await Task.Run(() =>_client.ReceiveAsync(), ct).ConfigureAwait(false);

                    }
                    catch (SocketException ex)
                    {

                    }
                }
            }
            finally
            {
                _client.DropMulticastGroup(MulticastIp);
            }
        }
    }


    internal class CitpUdpPacketReceivedEventArgs : EventArgs
    {
        CitpUdpPacketReceivedEventArgs(CitpPacket packet, IPAddress ip)
        {
            Packet = packet;
            Ip = ip;
        }

        public CitpPacket Packet { get; }
        public IPAddress Ip { get; }
    }
}
