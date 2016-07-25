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
	    const int CitpUdpPort = 4809;
	    static readonly IPAddress CitpMulticastIp = IPAddress.Parse("239.224.0.180");
	    static readonly IPAddress CitpMulticastLegacyIp = IPAddress.Parse("224.0.0.180");

	    private bool _isDisposed;
	    private readonly UdpClient _client;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly Task _listenTask;

	    public UdpService(bool isUseLegacyMulticastIp, NetworkInterface networkInterface = null)
	    {
	        var localIp = IPAddress.Any;

	        if (networkInterface != null)
	            localIp = networkInterface.GetIPProperties().UnicastAddresses.First().Address;


            MulticastIp = isUseLegacyMulticastIp ? CitpMulticastLegacyIp : CitpMulticastIp;

	        _client = new UdpClient
	        {
	            MulticastLoopback = false
	        };

            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            _client.Client.Bind(new IPEndPoint(localIp, CitpUdpPort));

            if (networkInterface != null)
	            _client.JoinMulticastGroup(MulticastIp);
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
                    UdpReceiveResult result;

                    try
                    {
                        //result = await Task.Run(_client.ReceiveAsync, ct).ConfigureAwait(false);
                        result = await _client.ReceiveAsync().ConfigureAwait(false);

                    }
                    catch (SocketException ex)
                    {
                        break;
                    }

                    CitpPacket packet;

                    try
                    {
                        packet = CitpPacket.FromByteArray(result.Buffer);
                    }
                    catch (InvalidOperationException ex)
                    {
                        continue;
                    }
                    catch (NotSupportedException ex)
                    {
                        continue;
                    }

                    PacketReceived?.Invoke(this, new CitpUdpPacketReceivedEventArgs(packet, result.RemoteEndPoint.Address));
                }
            }
            finally
            {
                _client.DropMulticastGroup(MulticastIp);
            }
        }

        private void deserializePacket(byte[] buffer)
        {
            
        }
    }


    internal class CitpUdpPacketReceivedEventArgs : EventArgs
    {
        public CitpUdpPacketReceivedEventArgs(CitpPacket packet, IPAddress ip)
        {
            Packet = packet;
            Ip = ip;
        }

        public CitpPacket Packet { get; }
        public IPAddress Ip { get; }
    }
}
