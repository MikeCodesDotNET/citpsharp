using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Imp.CitpSharp
{
	internal sealed class CitpUdpService : IDisposable
	{
		public static readonly int MaximumUdpPacketLength = 65507;

		private static readonly int CitpUdpPort = 4809;
		private static readonly IPAddress CitpMulticastOriginalIp = IPAddress.Parse("224.0.0.180");
		private static readonly IPAddress CitpMulticastIp = IPAddress.Parse("239.224.0.180");
		private static readonly IPEndPoint CitpMulticastOriginalEndpoint = new IPEndPoint(CitpMulticastOriginalIp, CitpUdpPort);
		private static readonly IPEndPoint CitpMulticastEndpoint = new IPEndPoint(CitpMulticastIp, CitpUdpPort);

		private readonly ICitpLogService _log;
		private readonly IPAddress _nicIp;
		private readonly bool _useOriginalMulticastIp;

		private readonly UdpSocketMulticastClient _client;


		public CitpUdpService(ICitpLogService log, IPAddress nicIp, bool useOriginalMulticastIp)
		{
			_log = log;
			_nicIp = nicIp;
			_useOriginalMulticastIp = useOriginalMulticastIp;

			_client = new UdpSocketMulticastClient();
			_client.MessageReceived += messageReceived;
		}

		public void Dispose()
		{
			_client.MessageReceived -= messageReceived;
			_client.Dispose();
		}

		public event EventHandler<MessageReceivedEventArgs> MessageReceived;

		public async Task<bool> StartAsync()
		{
			var interfaces = await CommsInterface.GetAllInterfacesAsync().ConfigureAwait(false);

			var nicInterface = interfaces.FirstOrDefault(i => i.IPAddress == _nicIp.ToString());

			if (nicInterface == null)
			{
				_log.LogError($"Could not locate network interface with IP '{_nicIp}'");
				return false;
			}

			await _client.JoinMulticastGroupAsync(_useOriginalMulticastIp
				? CitpMulticastOriginalIp.ToString()
				: CitpMulticastIp.ToString(), CitpUdpPort, nicInterface).ConfigureAwait(false);

			return true;
		}

		private void messageReceived(object sender, UdpSocketMessageReceivedEventArgs e)
		{
			MessageReceived?.Invoke(this, new MessageReceivedEventArgs(
				new IPEndPoint(IPAddress.Parse(e.RemoteAddress), int.Parse(e.RemotePort)),
				e.ByteData));
		}

		public async Task<bool> SendAsync(byte[] data)
		{
			if (_client == null)
				return false;

			await _client.SendMulticastAsync(data).ConfigureAwait(false);

			return true;
		}



		public class MessageReceivedEventArgs : EventArgs
		{
			public MessageReceivedEventArgs(IPEndPoint endpoint, byte[] data)
			{
				Endpoint = endpoint;
				Data = data;
			}

			public IPEndPoint Endpoint { get; }
			public byte[] Data { get; }
		}
	}
}