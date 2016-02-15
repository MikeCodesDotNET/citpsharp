using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Imp.CitpSharp.Packets;
using Imp.CitpSharp.Packets.Msex;
using Imp.CitpSharp.Packets.Pinf;
using Imp.CitpSharp.Sockets;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	[PublicAPI]
	public sealed class CitpVisualizerService : IDisposable
	{
		private const int CitpPlocFrequency = 1000;
		private const int CitpLstaFrequency = 250;

		private readonly ICitpLogService _log;
		private readonly ICitpVisualizerDevice _device;
		private readonly CitpStreamingService _streamingService;

		private CitpNetworkService _networkService;
		private DateTime _peerLocationMessageLastSent;



		public CitpVisualizerService([NotNull] string nicIpAddress,
			bool useOriginalMulticastIp, [NotNull] ICitpVisualizerDevice device, bool isStreamingEnabled,
			ICitpLogService log = null)
		{
			if (nicIpAddress == null)
				throw new ArgumentNullException(nameof(nicIpAddress));

			if (device == null)
				throw new ArgumentNullException(nameof(device));

			_log = log ?? new CitpConsoleLogger(CitpLoggerLevel.Info);

			_device = device;

			IpAddress ip;

			if (!IpAddress.TryParse(nicIpAddress, out ip))
				throw new ArgumentException("Not a valid IPv4 address", nameof(nicIpAddress));

			_networkService = new CitpNetworkService(_log, ip, useOriginalMulticastIp, _device);

			IsStreamingEnabled = isStreamingEnabled;

			if (isStreamingEnabled)
				_streamingService = new CitpStreamingService(_log, _device, _networkService);
		}


		public string Status { get; set; }

		public bool IsStreamingEnabled { get; }

		public void Dispose()
		{
			if (_networkService != null)
			{
				_networkService.Dispose();
				_networkService = null;
			}
		}

		public Task<bool> StartAsync()
		{
			return _networkService.StartAsync();
		}


		/// <summary>
		///     Processes all outstanding CITP messages.
		/// </summary>
		public async Task SendAndReceiveMessagesAsync()
		{
			_log.LogDebug("Started processing messages");

			if ((DateTime.Now - _peerLocationMessageLastSent).TotalMilliseconds >= CitpPlocFrequency)
			{
				await sendPeerLocationPacketAsync().ConfigureAwait(false);
				_peerLocationMessageLastSent = DateTime.Now;
			}

			while (_networkService.MessageQueue.Count > 0)
			{
				Tuple<CitpPeer, CitpPacket> message;

				if (_networkService.MessageQueue.TryDequeue(out message) == false)
					throw new InvalidOperationException("Failed to dequeue message");

				// TODO: Implement message handling
			}

			_log.LogDebug("Finished processing messages");
		}

		public Task ProcessStreamRequestsAsync()
		{
			return _streamingService.ProcessStreamRequestsAsync();
		}


		private Task sendPeerLocationPacketAsync()
		{
			var packet = new PeerLocationMessagePacket
			{
				IsListeningForTcpConnection = true,
				ListeningTcpPort = Convert.ToUInt16(_networkService.LocalTcpListenPort),
				Type = CitpPeerType.Visualizer,
				Name = _device.PeerName,
				State = Status
			};

			return _networkService.SendMulticastPacketAsync(packet);
		}


		private Task getVideoSourcesAsync(CitpPeer peer, GetVideoSourcesMessagePacket requestPacket)
		{
			return
				_networkService.SendPacketAsync(new VideoSourcesMessagePacket {Sources = _device.VideoSources.Values.ToList()},
					peer);
		}
	}
}