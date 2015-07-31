using Imp.CitpSharp.Packets.Msex;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imp.CitpSharp
{
	internal sealed class CitpStreamingService
	{
		readonly ICitpLogService _log;
		readonly ICitpMediaServerInfo _serverInfo;
		readonly CitpNetworkService _networkService;

		readonly Dictionary<int, SourceStreamRequest> _streamRequests = new Dictionary<int, SourceStreamRequest>();

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

			request.AddRequestFormat(peerMsexVersion.HasValue ? peerMsexVersion.Value : MsexVersion.Version1_0, requestPacket);
		}

		public async Task ProcessStreamRequests()
		{
			foreach (var request in _streamRequests.Values.ToList())
			{
				Image frame = null;

				foreach (var formatRequest in request.Formats)
				{
					if (DateTime.Now < formatRequest.LastOutput + TimeSpan.FromSeconds(1.0f / request.Fps))
						break;

					if (frame == null)
					{
						frame = _serverInfo.GetVideoSourceFrame(request.SourceIdentifier, request.FrameWidth, request.FrameHeight);

						if (frame == null)
							break;
					}

					byte[] frameBuffer;

					switch(formatRequest.FrameFormat)
					{
						case MsexImageFormat.RGB8:
							frameBuffer = frame.ToRgb8ByteArray(formatRequest.Version == MsexVersion.Version1_0);
							break;
						case MsexImageFormat.JPEG:
							frameBuffer = frame.ToJpegByteArray();
							break;
						case MsexImageFormat.PNG:
							frameBuffer = frame.ToPngByteArray();
							break;
						default:
							throw new InvalidOperationException("Unknown image format type");
					}

					var packet = new StreamFrameMessagePacket
					{
						Version = formatRequest.Version,
						MediaServerUUID = _serverInfo.Uuid,
						SourceIdentifier = Convert.ToUInt16(request.SourceIdentifier),
						FrameFormat = formatRequest.FrameFormat,
						FrameWidth = Convert.ToUInt16(request.FrameWidth),
						FrameHeight = Convert.ToUInt16(request.FrameHeight),
						FrameBuffer = frameBuffer
					};

					await _networkService.SendMulticastPacket(packet);

					formatRequest.LastOutput = DateTime.Now;
				}


				request.RemoveTimedOutRequests();

				if (request.Formats.Count == 0)
					_streamRequests.Remove(request.SourceIdentifier);
			}
		}



		class SourceStreamRequest
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

			HashSet<RequestFormat> _formats = new HashSet<RequestFormat>();
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
					&& r.IsVersion1_2 == (peerMsexVersion == MsexVersion.Version1_2));

				if (format != null)
				{
					format.Version = peerMsexVersion;

					DateTime packetExpireAt = DateTime.Now + TimeSpan.FromSeconds(packet.Timeout);

					if (packetExpireAt > format.ExpireAt)
						format.ExpireAt = packetExpireAt;
				}
				else
				{
					_formats.Add(new RequestFormat
					{
						FrameFormat = packet.FrameFormat,
						Version = peerMsexVersion,
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
				public MsexImageFormat FrameFormat { get; set; }
				public MsexVersion Version { get; set; }

				public DateTime LastOutput { get; set; }
				public DateTime ExpireAt { get; set; }

				public bool IsVersion1_2
				{
					get { return Version == MsexVersion.Version1_2; }
				}

				public override bool Equals(object obj)
				{
					var m = obj as RequestFormat;
					if ((object)m == null)
						return false;

					return Equals(m);
				}

				public bool Equals(RequestFormat other)
				{
					return FrameFormat == other.FrameFormat
						&& IsVersion1_2 == other.IsVersion1_2;
				}

				public override int GetHashCode()
				{
					return FrameFormat.GetHashCode()
						^ IsVersion1_2.GetHashCode();
				}
			}
		}
	}
}
