using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Imp.CitpSharp.Packets;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	internal sealed class CitpStreamingService
	{
		private const int MaximumImageBufferSize = 65000;

		private readonly ICitpLogService _log;
		private readonly CitpNetworkService _networkService;
		private readonly ICitpMediaServerInfo _serverInfo;

		private readonly Dictionary<int, SourceStreamRequest> _streamRequests = new Dictionary<int, SourceStreamRequest>();

		public CitpStreamingService(ICitpLogService log, ICitpMediaServerInfo serverInfo, CitpNetworkService networkService)
		{
			_log = log;
			_serverInfo = serverInfo;
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
				Image frame = null;

				foreach (var formatRequest in request.Formats)
				{
					if (Math.Abs(request.Fps) < float.Epsilon || DateTime.Now < formatRequest.LastOutput + TimeSpan.FromSeconds(1.0f / request.Fps))
						break;

					if (frame == null)
					{
						frame = _serverInfo.GetVideoSourceFrame(request.SourceIdentifier, request.FrameWidth, request.FrameHeight);

						if (frame == null)
							break;
					}

					byte[] frameBuffer;

					switch (formatRequest.FrameFormat)
					{
						case MsexImageFormat.Rgb8:
							frameBuffer = frame.ToRgb8ByteArray(formatRequest.Version == MsexVersion.Version1_0);
							break;
						case MsexImageFormat.Jpeg:
						case MsexImageFormat.FragmentedJpeg:
							frameBuffer = frame.ToJpegByteArray();
							break;
						case MsexImageFormat.Png:
						case MsexImageFormat.FragmentedPng:
							frameBuffer = frame.ToPngByteArray();
							break;
						default:
							throw new InvalidOperationException("Unknown image format type");
					}

					var packet = new StreamFrameMessagePacket
					{
						Version = formatRequest.Version,
						MediaServerUuid = _serverInfo.Uuid,
						SourceIdentifier = Convert.ToUInt16(request.SourceIdentifier),
						FrameFormat = formatRequest.FrameFormat,
						FrameWidth = Convert.ToUInt16(request.FrameWidth),
						FrameHeight = Convert.ToUInt16(request.FrameHeight),
					};


					if (formatRequest.FrameFormat == MsexImageFormat.FragmentedJpeg
					    || formatRequest.FrameFormat == MsexImageFormat.FragmentedPng)
					{
						var fragments = frameBuffer.Split(MaximumImageBufferSize);

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
						if (frameBuffer.Length > MaximumImageBufferSize)
						{
							_log.LogWarning("Cannot send streaming frame, image buffer too large");
							return;
						}

						packet.FrameBuffer = frameBuffer;
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
			private readonly HashSet<RequestFormat> _formats = new HashSet<RequestFormat>();

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

			public HashSet<RequestFormat> Formats
			{
				get { return _formats; }
			}



			public void AddRequestFormat(MsexVersion peerMsexVersion, RequestStreamMessagePacket packet)
			{
				if (SourceIdentifier == -1)
					SourceIdentifier = packet.SourceIdentifier;
				else if (SourceIdentifier != packet.SourceIdentifier)
					throw new InvalidOperationException("Cannot add request format, source id does not match");


				FrameWidth = Math.Max(FrameWidth, packet.FrameWidth);
				FrameHeight = Math.Max(FrameHeight, packet.FrameHeight);
				Fps = Math.Max(Fps, packet.Fps);


				var format = _formats.FirstOrDefault(r => r.FrameFormat == packet.FrameFormat
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
					_formats.Add(new RequestFormat(packet.FrameFormat, peerMsexVersion)
					{
						LastOutput = DateTime.MinValue,
						ExpireAt = DateTime.Now + TimeSpan.FromSeconds(packet.Timeout)
					});
				}
			}

			public void RemoveTimedOutRequests()
			{
				foreach (var format in _formats.ToList())
				{
					if (DateTime.Now >= format.ExpireAt)
						_formats.Remove(format);
				}
			}



			public class RequestFormat : IEquatable<RequestFormat>
			{
				public RequestFormat(MsexImageFormat frameFormat, MsexVersion version)
				{
					FrameFormat = frameFormat;
					Version = version;
				}

				public MsexImageFormat FrameFormat { get; private set; }
				public MsexVersion Version { get; private set; }

				public DateTime LastOutput { get; set; }
				public DateTime ExpireAt { get; set; }

				public bool IsVersion12
				{
					get { return Version == MsexVersion.Version1_2; }
				}

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
					return obj.GetType() == this.GetType() && Equals((RequestFormat)obj);
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