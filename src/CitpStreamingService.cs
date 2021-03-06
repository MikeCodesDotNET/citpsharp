﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Imp.CitpSharp.Packets.Msex;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	internal sealed class CitpStreamingService
	{
		private const int MaximumImageBufferSize = 65000;

		private readonly ICitpLogService _log;
		private readonly CitpNetworkService _networkService;
		private readonly ICitpStreamProvider _streamProvider;

		private readonly Dictionary<int, SourceStreamRequest> _streamRequests = new Dictionary<int, SourceStreamRequest>();

		public CitpStreamingService(ICitpLogService log, ICitpStreamProvider streamProvider, CitpNetworkService networkService)
		{
			_log = log;
			_streamProvider = streamProvider;
			_networkService = networkService;
		}

		public void AddStreamRequest(MsexVersion? peerMsexVersion, RequestStreamMessagePacket requestPacket)
		{
			SourceStreamRequest request;

			if (_streamRequests.TryGetValue(requestPacket.SourceIdentifier, out request) == false)
			{
				request = new SourceStreamRequest();
				_streamRequests.Add(requestPacket.SourceIdentifier, request);
			}

			request.AddRequestFormat(peerMsexVersion ?? MsexVersion.Version1_0, requestPacket);
		}

		public async Task ProcessStreamRequestsAsync()
		{
			foreach (var request in _streamRequests.Values.ToList())
			{
				foreach (var formatRequest in request.Formats)
				{
					if (Math.Abs(request.Fps) < float.Epsilon
					    || DateTime.Now < formatRequest.LastOutput + TimeSpan.FromSeconds(1.0f / request.Fps))
						break;

					var imageRequest = new CitpImageRequest(request.FrameWidth, request.FrameHeight, formatRequest.FrameFormat, true,
						formatRequest.FrameFormat == MsexImageFormat.Rgb8 && formatRequest.Version == MsexVersion.Version1_0);

					var frame = _streamProvider.GetVideoSourceFrame(request.SourceIdentifier, imageRequest);

					if (frame == null)
						break;

					var packet = new StreamFrameMessagePacket
					{
						Version = formatRequest.Version,
						MediaServerUuid = _streamProvider.Uuid,
						SourceIdentifier = Convert.ToUInt16(request.SourceIdentifier),
						FrameFormat = formatRequest.FrameFormat,
						FrameWidth = Convert.ToUInt16(frame.ActualWidth),
						FrameHeight = Convert.ToUInt16(frame.ActualHeight)
					};


					if (formatRequest.FrameFormat == MsexImageFormat.FragmentedJpeg
					    || formatRequest.FrameFormat == MsexImageFormat.FragmentedPng)
					{
						var fragments = frame.Data.Split(MaximumImageBufferSize);

						packet.FragmentInfo = new StreamFrameMessagePacket.FragmentPreamble()
						{
							FrameIndex = request.FrameCounter,
							FragmentCount = (ushort)fragments.Length
						};

						if (fragments.Length > ushort.MaxValue)
						{
							_log.LogWarning("Cannot send streaming frame, too many image fragments");
							return;
						}

						for (uint i = 0; i < fragments.Length; ++i)
						{
							packet.FragmentInfo.FragmentIndex = (ushort)i;
							packet.FragmentInfo.FragmentByteOffset = MaximumImageBufferSize * i;
							packet.FrameBuffer = fragments[i];
							await _networkService.SendMulticastPacketAsync(packet).ConfigureAwait(false);
						}
					}
					else
					{
						if (frame.Data.Length > MaximumImageBufferSize)
						{
							_log.LogWarning($"Cannot send streaming frame request '{imageRequest}', image buffer too large");
							return;
						}

						packet.FrameBuffer = frame.Data;
						await _networkService.SendMulticastPacketAsync(packet).ConfigureAwait(false);
					}

					formatRequest.LastOutput = DateTime.Now;
				}

				request.RemoveTimedOutRequests();

				if (request.Formats.Count == 0)
					_streamRequests.Remove(request.SourceIdentifier);

				++request.FrameCounter;
			}
		}



		private class SourceStreamRequest
		{
			public SourceStreamRequest()
			{
				SourceIdentifier = -1;
				FrameWidth = 0;
				FrameHeight = 0;
				Fps = 0;
			}

			public int SourceIdentifier { get; set; }

			public int FrameWidth { get; set; }
			public int FrameHeight { get; set; }
			public float Fps { get; set; }

			public uint FrameCounter { get; set; }

			public HashSet<RequestFormat> Formats { get; } = new HashSet<RequestFormat>();



			public void AddRequestFormat(MsexVersion peerMsexVersion, RequestStreamMessagePacket packet)
			{
				if (SourceIdentifier == -1)
					SourceIdentifier = packet.SourceIdentifier;
				else if (SourceIdentifier != packet.SourceIdentifier)
					throw new InvalidOperationException("Cannot add request format, source id does not match");


				FrameWidth = Math.Max(FrameWidth, packet.FrameWidth);
				FrameHeight = Math.Max(FrameHeight, packet.FrameHeight);
				Fps = Math.Max(Fps, packet.Fps);


				var format = Formats.FirstOrDefault(r => r.FrameFormat == packet.FrameFormat
				                                         && r.IsVersion12 == (peerMsexVersion == MsexVersion.Version1_2));

				if (format != null)
				{
					Formats.Remove(format);
					format = new RequestFormat(format.FrameFormat, peerMsexVersion);
					Formats.Add(format);

					var packetExpireAt = DateTime.Now + TimeSpan.FromSeconds(packet.Timeout);

					if (packetExpireAt > format.ExpireAt)
						format.ExpireAt = packetExpireAt;
				}
				else
				{
					Formats.Add(new RequestFormat(packet.FrameFormat, peerMsexVersion)
					{
						LastOutput = DateTime.MinValue,
						ExpireAt = DateTime.Now + TimeSpan.FromSeconds(packet.Timeout)
					});
				}
			}

			public void RemoveTimedOutRequests()
			{
				foreach (var format in Formats.ToList())
				{
					if (DateTime.Now >= format.ExpireAt)
						Formats.Remove(format);
				}
			}



			public class RequestFormat : IEquatable<RequestFormat>
			{
				public RequestFormat(MsexImageFormat frameFormat, MsexVersion version)
				{
					FrameFormat = frameFormat;
					Version = version;
				}

				public MsexImageFormat FrameFormat { get; }
				public MsexVersion Version { get; }

				public DateTime LastOutput { get; set; }
				public DateTime ExpireAt { get; set; }

				public bool IsVersion12 => Version == MsexVersion.Version1_2;

				public bool Equals([CanBeNull] RequestFormat other)
				{
					if (ReferenceEquals(null, other))
						return false;
					if (ReferenceEquals(this, other))
						return true;
					return FrameFormat == other.FrameFormat && IsVersion12 == other.IsVersion12;
				}

				public override bool Equals([CanBeNull] object obj)
				{
					if (ReferenceEquals(null, obj))
						return false;
					if (ReferenceEquals(this, obj))
						return true;
					return obj.GetType() == GetType() && Equals((RequestFormat)obj);
				}

				public override int GetHashCode()
				{
					unchecked
					{
						return ((int)FrameFormat * 397) ^ IsVersion12.GetHashCode();
					}
				}
			}
		}
	}
}