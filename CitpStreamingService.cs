using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Imp.CitpSharp.Packets.Msex;

namespace Imp.CitpSharp
{
	internal sealed class CitpStreamingService
	{
		private readonly ICitpLogService m_log;
		private readonly CitpNetworkService m_networkService;
		private readonly ICitpMediaServerInfo m_serverInfo;

		private readonly Dictionary<int, SourceStreamRequest> m_streamRequests = new Dictionary<int, SourceStreamRequest>();

		public CitpStreamingService(ICitpLogService log, ICitpMediaServerInfo serverInfo, CitpNetworkService networkService)
		{
			m_log = log;
			m_serverInfo = serverInfo;
			m_networkService = networkService;
		}

		public void AddStreamRequest(MsexVersion? peerMsexVersion, RequestStreamMessagePacket requestPacket)
		{
			SourceStreamRequest request;

			if (m_streamRequests.TryGetValue(requestPacket.SourceIdentifier, out request) == false)
			{
				request = new SourceStreamRequest();
				m_streamRequests.Add(requestPacket.SourceIdentifier, request);
			}

			request.AddRequestFormat(peerMsexVersion ?? MsexVersion.Version10, requestPacket);
		}

		public async Task ProcessStreamRequestsAsync()
		{
			foreach (var request in m_streamRequests.Values.ToList())
			{
				Image frame = null;

				foreach (var formatRequest in request.Formats)
				{
					if (request.Fps == 0 || DateTime.Now < formatRequest.LastOutput + TimeSpan.FromSeconds(1.0f / request.Fps))
						break;

					if (frame == null)
					{
						frame = m_serverInfo.GetVideoSourceFrame(request.SourceIdentifier, request.FrameWidth, request.FrameHeight);

						if (frame == null)
							break;
					}

					byte[] frameBuffer;

					switch (formatRequest.FrameFormat)
					{
						case MsexImageFormat.Rgb8:
							frameBuffer = frame.ToRgb8ByteArray(formatRequest.Version == MsexVersion.Version10);
							break;
						case MsexImageFormat.Jpeg:
							frameBuffer = frame.ToJpegByteArray();
							break;
						case MsexImageFormat.Png:
							frameBuffer = frame.ToPngByteArray();
							break;
						default:
							throw new InvalidOperationException("Unknown image format type");
					}

					var packet = new StreamFrameMessagePacket
					{
						Version = formatRequest.Version,
						MediaServerUuid = m_serverInfo.Uuid,
						SourceIdentifier = Convert.ToUInt16(request.SourceIdentifier),
						FrameFormat = formatRequest.FrameFormat,
						FrameWidth = Convert.ToUInt16(request.FrameWidth),
						FrameHeight = Convert.ToUInt16(request.FrameHeight),
						FrameBuffer = frameBuffer
					};

					await m_networkService.SendMulticastPacketAsync(packet);

					formatRequest.LastOutput = DateTime.Now;
				}


				request.RemoveTimedOutRequests();

				if (request.Formats.Count == 0)
					m_streamRequests.Remove(request.SourceIdentifier);
			}
		}



		private class SourceStreamRequest
		{
			private readonly HashSet<RequestFormat> m_formats = new HashSet<RequestFormat>();

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

			public HashSet<RequestFormat> Formats
			{
				get { return m_formats; }
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


				var format = m_formats.FirstOrDefault(r => r.FrameFormat == packet.FrameFormat
				                                           && r.IsVersion12 == (peerMsexVersion == MsexVersion.Version12));

				if (format != null)
				{
					format.Version = peerMsexVersion;

					var packetExpireAt = DateTime.Now + TimeSpan.FromSeconds(packet.Timeout);

					if (packetExpireAt > format.ExpireAt)
						format.ExpireAt = packetExpireAt;
				}
				else
				{
					m_formats.Add(new RequestFormat
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
				foreach (var format in m_formats.ToList())
				{
					if (DateTime.Now >= format.ExpireAt)
						m_formats.Remove(format);
				}
			}



			public class RequestFormat : IEquatable<RequestFormat>
			{
				public MsexImageFormat FrameFormat { get; set; }
				public MsexVersion Version { get; set; }

				public DateTime LastOutput { get; set; }
				public DateTime ExpireAt { get; set; }

				public bool IsVersion12
				{
					get { return Version == MsexVersion.Version12; }
				}

				public bool Equals(RequestFormat other)
				{
					return FrameFormat == other.FrameFormat
					       && IsVersion12 == other.IsVersion12;
				}

				public override bool Equals(object obj)
				{
					var m = obj as RequestFormat;
					if (m == null)
						return false;

					return Equals(m);
				}

				public override int GetHashCode()
				{
					return FrameFormat.GetHashCode()
					       ^ IsVersion12.GetHashCode();
				}
			}
		}
	}
}