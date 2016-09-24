using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Imp.CitpSharp.Packets;
using JetBrains.Annotations;

namespace Imp.CitpSharp.Networking
{
	internal class TcpServerConnection
	{
	    private const int ReceivedBufferLength = 2048;
	    private const int PacketBufferLength = 65536;

		private readonly ICitpLogService _logger;
		private readonly TcpClient _client;

		public TcpServerConnection(ICitpLogService logger, TcpClient client)
		{
			_logger = logger;
			_client = client;
		}

		public event EventHandler<TcpServerConnection> ConnectionOpened;
		public event EventHandler<TcpServerConnection> ConnectionClosed;
		public event EventHandler<TcpPacketReceivedEventArgs> PacketReceived;

		public void Listen(CancellationToken ct)
		{
			const int bufferLength = 2048;

			var receivedbuffer = new byte[ReceivedBufferLength];
	        var packetBuffer = new byte[PacketBufferLength];

	        int packetSize = 0;
	        int packetBytesRemaining = 0;

			_logger.LogInfo($"{_client}: Connection opened");
			ConnectionOpened?.Invoke(this, this);

			var stream = _client.GetStream();

			while (true)
			{
				int numBytesReceived;

				try
				{
					numBytesReceived = stream.Read(receivedbuffer, 0, bufferLength);
				}
				catch (IOException)
				{
					_logger.LogInfo($"{_client}: Connection unexpectedly closed");
					break;
				}
				catch (ObjectDisposedException)
				{
					_logger.LogInfo($"{_client}: Connection unexpectedly closed");
					break;
				}

				if (numBytesReceived == 0)
				{
					_logger.LogInfo($"{_client}: Connection closed");
					break;
				}

	            int i = 0;
	            while (i < numBytesReceived)
	            {
		            if (packetBytesRemaining == 0)
		            {
			            i = findCitpCookie(receivedbuffer, i);

			            if (i == -1)
				            break;

			            packetSize = receivedbuffer[i + CitpPacket.PacketLengthIndex]
			                         + (receivedbuffer[i + CitpPacket.PacketLengthIndex + 1] << 8)
			                         + (receivedbuffer[i + CitpPacket.PacketLengthIndex + 2] << 16)
			                         + (receivedbuffer[i + CitpPacket.PacketLengthIndex + 3] << 24);

						packetBytesRemaining = packetSize;

						if (packetSize > packetBuffer.Length)
						{
							_logger.LogError($"CITP packet of reported length {packetSize}, buffer to small to reassemble");
							packetSize = 0;
							packetBytesRemaining = 0;
						}
					}

		            int bytesToCopy = Math.Min(packetBytesRemaining, receivedbuffer.Length - i);
					Buffer.BlockCopy(receivedbuffer, i, packetBuffer, packetSize - packetBytesRemaining, bytesToCopy);

		            i += bytesToCopy;
		            packetBytesRemaining -= bytesToCopy;

		            if (packetBytesRemaining == 0)
		            {
			            deserializePacket(packetBuffer);
			            packetSize = 0;
		            }
	            }
			}
			
			ConnectionClosed?.Invoke(this, this);
		}

		public void SendPacket(CitpPacket packet)
		{
			var buffer = packet.ToByteArray();

			try
			{
				_client.GetStream().Write(buffer, 0, buffer.Length);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Exception whilst sending TCP CITP packet to client {this}");
				_logger.LogException(ex);
			}
		}

		[CanBeNull]
		public PeerInfo Peer { get; set; }

		public ImmutableHashSet<MsexVersion> SupportedMsexVersions { get; set; } = ImmutableHashSet<MsexVersion>.Empty;

		public IPAddress Ip => ((IPEndPoint)_client.Client.RemoteEndPoint).Address;


	    public override string ToString() => Peer != null ? $"TCP Peer {Peer}" : $"TCP Client {_client.Client.RemoteEndPoint}";


	    private static int findCitpCookie(byte[] bytes, int offset)
		{
			Debug.Assert(offset < bytes.Length);

			for (int i = offset, end = bytes.Length - CitpPacket.CitpCookie.Length; i < end; i++)
			{
				bool isFound = false;
				for (int j = 0; j < CitpPacket.CitpCookie.Length && !isFound; j++)
					isFound = bytes[i + j] == CitpPacket.CitpCookie[j];
				
				if (isFound)
					return i;
			}

		    return -1;
		}

		private void deserializePacket(byte[] packetBuffer)
		{
	        CitpPacket packet;

			try
			{
				packet = CitpPacket.FromByteArray(packetBuffer);
			}
			catch (InvalidOperationException ex)
			{
				_logger.LogWarning($"Received malformed CITP packet: {ex.Message}");
				return;
			}
			catch (NotSupportedException ex)
			{
				_logger.LogWarning($"Recieved unsupported CITP packet: {ex.Message}");
				return;
			}
			catch (Exception ex)
			{
				_logger.LogError("Received unexpected exception type whilst deserializing CITP packet");
				_logger.LogException(ex);
				return;
			}

			PacketReceived?.Invoke(this, new TcpPacketReceivedEventArgs(packet, this));
		}
	}
}
