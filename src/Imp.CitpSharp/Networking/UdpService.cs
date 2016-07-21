using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace Imp.CitpSharp.Networking
{
    public class UdpService : IDisposable
    {
	    const int BufferKLength = 65536;
	    const int UdpPort = 4809;
	    static readonly IPAddress CitpMulticastIp = IPAddress.Parse("239.224.0.180");
	    static readonly IPAddress CitpMulticastOriginalIp = IPAddress.Parse("224.0.0.180");

	    private bool _isDisposed;
	    private readonly UdpClient _client;

	    public UdpService()
	    {
		    
	    }

	    public void Dispose()
	    {
		    if (_isDisposed)
			    return;

			_client.Dispose();

		    _isDisposed = true;
	    }
    }
}
