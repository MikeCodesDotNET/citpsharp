using System;
using System.Net;
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

	    private readonly ICitpLogService _logger;

	    private bool _isDisposed;
	    private readonly UdpClient _client;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

	    public UdpService(ICitpLogService logger, bool isUseLegacyMulticastIp, IPAddress localIp = null)
	    {
		    _logger = logger;

			_logger.LogInfo("Starting UDP service...");

            MulticastIp = isUseLegacyMulticastIp ? CitpMulticastLegacyIp : CitpMulticastIp;

	        _client = new UdpClient
	        {
				EnableBroadcast = true,
	            MulticastLoopback = false
	        };

            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _client.Client.Bind(new IPEndPoint(localIp ?? IPAddress.Any, CitpUdpPort));
            _client.JoinMulticastGroup(MulticastIp);

            listen(_cancellationTokenSource.Token);

			_logger.LogInfo("Started UDP service");
		}


	    public void Dispose()
	    {
		    if (_isDisposed)
			    return;

			_logger.LogInfo("Stopping UDP service...");

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();

#if !NET45
			_client.Dispose();
#endif

		    _isDisposed = true;

			_logger.LogInfo("UDP service stopped");
		}


        public event EventHandler<CitpUdpPacketReceivedEventArgs> PacketReceived;

        public IPAddress MulticastIp { get; }


        public void SendPacket(CitpPacket packet)
        {
            var buffer = packet.ToByteArray();
	        var sendTask = _client.SendAsync(buffer, buffer.Length, new IPEndPoint(MulticastIp, CitpUdpPort));
			sendTask.Wait();

	        if (sendTask.Result != buffer.Length)
		        _logger.LogWarning($"Failed to send UDP packet, {sendTask.Result}/{buffer.Length} bytes sent");
        }

        private async void listen(CancellationToken ct)
        {
			_logger.LogInfo("UDP listen thread started");

            try
            {
                while (true)
                {
                    UdpReceiveResult result;

                    try
                    {
                        result = await Task.Run(_client.ReceiveAsync, ct).ConfigureAwait(false);

                    }
                    catch (SocketException ex)
                    {
						_logger.LogError("Exception whilst receiving from UDP socket");
						_logger.LogException(ex);
						break;
                    }

					if (result.Buffer.Length < CitpPacket.MinimumPacketLength
						|| result.Buffer[0] != CitpPacket.CitpCookie[0]
						|| result.Buffer[1] != CitpPacket.CitpCookie[1]
						|| result.Buffer[2] != CitpPacket.CitpCookie[2]
						|| result.Buffer[3] != CitpPacket.CitpCookie[3])
	                {
						_logger.LogInfo("Received non-CITP UDP packet");
		                continue;
	                }

                    CitpPacket packet;

	                try
	                {
		                packet = CitpPacket.FromByteArray(result.Buffer);
	                }
	                catch (InvalidOperationException ex)
	                {
		                _logger.LogWarning($"Received malformed CITP packet: {ex.Message}");
		                continue;
	                }
	                catch (NotSupportedException ex)
	                {
		                _logger.LogWarning($"Recieved unsupported CITP packet: {ex.Message}");
		                continue;
	                }
	                catch (Exception ex)
	                {
		                _logger.LogError("Received unexpected exception type whilst deserializing CITP packet");
						_logger.LogException(ex);
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
