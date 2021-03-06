﻿using System;
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
	public sealed class CitpMediaServerService : IDisposable
	{
		private const int CitpPlocFrequency = 1000;
		private const int CitpLstaFrequency = 250;

		private readonly ICitpLogService _log;
		private readonly ICitpMediaServerDevice _device;
		private readonly CitpStreamingService _streamingService;

		private DateTime _layerStatusMessageLastSent;
		private CitpNetworkService _networkService;
		private DateTime _peerLocationMessageLastSent;



		public CitpMediaServerService([NotNull] string nicIpAddress,
			bool useOriginalMulticastIp, [NotNull] ICitpMediaServerDevice device, bool isStreamingEnabled,
			ICitpLogService log = null)
		{
			if (nicIpAddress == null)
				throw new ArgumentNullException(nameof(nicIpAddress));

			if (device == null)
				throw new ArgumentNullException(nameof(device));

			_log = log ?? new CitpDebugLogger(CitpLoggerLevel.Info);

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
			if ((DateTime.Now - _peerLocationMessageLastSent).TotalMilliseconds >= CitpPlocFrequency)
			{
				await sendPeerLocationPacketAsync().ConfigureAwait(false);
				_peerLocationMessageLastSent = DateTime.Now;
			}

			if ((DateTime.Now - _layerStatusMessageLastSent).TotalMilliseconds >= CitpLstaFrequency)
			{
				await sendLayerStatusPacketAsync().ConfigureAwait(false);
				_layerStatusMessageLastSent = DateTime.Now;
			}

			while (_networkService.MessageQueue.Count > 0)
			{
				Tuple<CitpPeer, CitpPacket> message;

				if (_networkService.MessageQueue.TryDequeue(out message) == false)
					throw new InvalidOperationException("Failed to dequeue message");

				try
				{
					if (message.Item2 is GetElementLibraryInformationMessagePacket)
					{
						await getElementLibraryInformationAsync(message.Item1, (GetElementLibraryInformationMessagePacket)message.Item2)
							.ConfigureAwait(false);
					}
					else if (message.Item2 is GetElementInformationMessagePacket)
					{
						await getElementInformationAsync(message.Item1, (GetElementInformationMessagePacket)message.Item2)
							.ConfigureAwait(false);
					}
					else if (message.Item2 is GetElementLibraryThumbnailMessagePacket)
					{
						await getElementLibraryThumbnailAsync(message.Item1, (GetElementLibraryThumbnailMessagePacket)message.Item2)
							.ConfigureAwait(false);
					}
					else if (message.Item2 is GetElementThumbnailMessagePacket)
					{
						await getElementThumbnailAsync(message.Item1, (GetElementThumbnailMessagePacket)message.Item2)
							.ConfigureAwait(false);
					}
					else if (message.Item2 is GetVideoSourcesMessagePacket)
					{
						await getVideoSourcesAsync(message.Item1, (GetVideoSourcesMessagePacket)message.Item2)
							.ConfigureAwait(false);
					}
					else if (message.Item2 is RequestStreamMessagePacket)
					{
						_streamingService.AddStreamRequest(message.Item1.MsexVersion, (RequestStreamMessagePacket)message.Item2);
					}
				}
				catch (InvalidOperationException ex)
				{
					_log.LogError("Failed to process message");
					_log.LogException(ex);
				}
			}
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
				Type = CitpPeerType.MediaServer,
				Name = _device.PeerName,
				State = Status
			};

			return _networkService.SendMulticastPacketAsync(packet);
		}

		private Task sendLayerStatusPacketAsync()
		{
			var layers = _device.Layers.Select((l, i) => new LayerStatusMessagePacket.LayerStatus
			{
				LayerNumber = (byte)i,
				PhysicalOutput = (byte)l.PhysicalOutput,
				MediaLibraryNumber = (byte)l.MediaLibraryIndex,
				MediaLibraryType = l.MediaLibraryType,
				MediaLibraryId = l.MediaLibraryId,
				MediaNumber = (byte)l.MediaIndex,
				MediaName = l.MediaName,
				MediaPosition = l.MediaFrame,
				MediaLength = l.MediaNumFrames,
				MediaFps = (byte)l.MediaFps,
				LayerStatusFlags = l.LayerStatusFlags
			});

			return
				_networkService.SendPacketToAllConnectedPeersAsync(new LayerStatusMessagePacket {LayerStatuses = layers.ToList()});
		}

		private async Task sendElementLibraryUpdatedPacketsAsync()
		{
			foreach (var message in _device.GetLibraryUpdateMessages())
			{
				await _networkService.SendPacketToAllConnectedPeersAsync(message.ToPacket()).ConfigureAwait(false);
			}
		}



		private Task getElementLibraryInformationAsync(CitpPeer peer,
			GetElementLibraryInformationMessagePacket requestPacket)
		{
			var libraries = _device.GetElementLibraryInformation(requestPacket.LibraryType,
				requestPacket.Version != MsexVersion.Version1_0 ? requestPacket.LibraryParentId : null,
				requestPacket.RequestedLibraryNumbers);

			var packet = new ElementLibraryInformationMessagePacket
			{
				LibraryType = requestPacket.LibraryType,
				Elements = libraries.ToList()
			};

			return _networkService.SendPacketAsync(packet, peer, requestPacket);
		}

		private Task getElementInformationAsync(CitpPeer peer, GetElementInformationMessagePacket requestPacket)
		{
			CitpPacket packet;

			switch (requestPacket.LibraryType)
			{
				case MsexLibraryType.Media:
					var mediaPacket = new MediaElementInformationMessagePacket
					{
						LibraryNumber = requestPacket.LibraryNumber,
						LibraryId = requestPacket.LibraryId,
						Media = _device.GetMediaElementInformation(new MsexId(requestPacket.LibraryId, requestPacket.LibraryNumber),
							requestPacket.RequestedElementNumbers).ToList()
					};


					packet = mediaPacket;
					break;

				case MsexLibraryType.Effects:
					var effectPacket = new EffectElementInformationMessagePacket
					{
						LibraryNumber = requestPacket.LibraryNumber,
						LibraryId = requestPacket.LibraryId,
						Effects =
							_device.GetEffectElementInformation(new MsexId(requestPacket.LibraryId, requestPacket.LibraryNumber),
								requestPacket.RequestedElementNumbers).ToList()
					};


					packet = effectPacket;
					break;

				default:
					Debug.Assert(requestPacket.LibraryId.HasValue, "Generic elements are unsupported in MSEX V1.0");

					var genericPacket = new GenericElementInformationMessagePacket
					{
						LibraryId = requestPacket.LibraryId.Value,
						LibraryType = requestPacket.LibraryType,
						Information = _device.GetGenericElementInformation(requestPacket.LibraryType,
							requestPacket.LibraryId.Value, requestPacket.RequestedElementNumbers).ToList()
					};


					packet = genericPacket;
					break;
			}

			return _networkService.SendPacketAsync(packet, peer, requestPacket);
		}

		private async Task getElementLibraryThumbnailAsync(CitpPeer peer,
			GetElementLibraryThumbnailMessagePacket requestPacket)
		{
			var msexIds = requestPacket.Version == MsexVersion.Version1_0
				? requestPacket.LibraryNumbers.Select(n => new MsexId(n)).ToList()
				: requestPacket.LibraryIds.Select(i => new MsexId(i)).ToList();

			var imageRequest = new CitpImageRequest(requestPacket.ThumbnailWidth, requestPacket.ThumbnailHeight,
				requestPacket.ThumbnailFormat,
				requestPacket.ThumbnailFlags.HasFlag(MsexThumbnailFlags.PreserveAspectRatio),
				requestPacket.ThumbnailFormat == MsexImageFormat.Rgb8 && requestPacket.Version == MsexVersion.Version1_0);

			var thumbs = _device.GetElementLibraryThumbnails(imageRequest, requestPacket.LibraryType, msexIds);

			var packets = thumbs.Select(t => new ElementLibraryThumbnailMessagePacket
			{
				LibraryType = requestPacket.LibraryType,
				LibraryNumber = t.Item1.LibraryNumber.GetValueOrDefault(),
				LibraryId = t.Item1.LibraryId.GetValueOrDefault(),
				ThumbnailFormat = requestPacket.ThumbnailFormat,
				ThumbnailWidth = (ushort)t.Item2.ActualWidth,
				ThumbnailHeight = (ushort)t.Item2.ActualHeight,
				ThumbnailBuffer = t.Item2.Data,
			});

			foreach (var packet in packets)
				await _networkService.SendPacketAsync(packet, peer, requestPacket).ConfigureAwait(false);
		}

		private async Task getElementThumbnailAsync(CitpPeer peer, GetElementThumbnailMessagePacket requestPacket)
		{
			MsexId msexId;
			if (requestPacket.Version == MsexVersion.Version1_0)
			{
				msexId = new MsexId(requestPacket.LibraryNumber);
			}
			else
			{
				Debug.Assert(requestPacket.LibraryId.HasValue, "LibraryId must have value for MSEX 1.1 or 1.2");
				msexId = new MsexId(requestPacket.LibraryId.Value);
			}

			var imageRequest = new CitpImageRequest(requestPacket.ThumbnailWidth, requestPacket.ThumbnailHeight,
				requestPacket.ThumbnailFormat,
				requestPacket.ThumbnailFlags.HasFlag(MsexThumbnailFlags.PreserveAspectRatio),
				requestPacket.ThumbnailFormat == MsexImageFormat.Rgb8 && requestPacket.Version == MsexVersion.Version1_0);

			var thumbs = _device.GetElementThumbnails(imageRequest, requestPacket.LibraryType, msexId,
				requestPacket.ElementNumbers);

			var packets = thumbs.Select(t => new ElementThumbnailMessagePacket
			{
				LibraryType = requestPacket.LibraryType,
				LibraryNumber = requestPacket.LibraryNumber,
				LibraryId = requestPacket.LibraryId,
				ElementNumber = t.Item1,
				ThumbnailFormat = requestPacket.ThumbnailFormat,
				ThumbnailWidth = (ushort)t.Item2.ActualWidth,
				ThumbnailHeight = (ushort)t.Item2.ActualHeight,
				ThumbnailBuffer = t.Item2.Data
			});

			foreach (var packet in packets)
				await _networkService.SendPacketAsync(packet, peer, requestPacket).ConfigureAwait(false);
		}


		private Task getVideoSourcesAsync(CitpPeer peer, GetVideoSourcesMessagePacket requestPacket)
		{
			return
				_networkService.SendPacketAsync(new VideoSourcesMessagePacket { Sources = _device.VideoSources.Values.ToList() }, peer, requestPacket);
		}
	}
}