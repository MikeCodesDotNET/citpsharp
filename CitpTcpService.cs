using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
 
namespace Imp.CitpSharp
{
	class SocketEventArgs : EventArgs
	{
		public SocketEventArgs(Socket socket)
		{
			Socket = socket;
		}

		public Socket Socket { get; private set; }
	}

	class SocketMessageEventArgs : SocketEventArgs
	{
		public SocketMessageEventArgs(Socket socket, byte[] message)
			: base(socket)
		{
			Message = message;
		}

		public byte[] Message { get; private set; }
	}




	class CitpTcpListenService
	{
		public event EventHandler<Tuple<IPEndPoint, byte[]>> PacketReceieved;
		public event EventHandler<ConnectedClient> ClientConnect;
		public event EventHandler<IPEndPoint> ClientDisconnect;

		IPEndPoint _ipLocal;

		ICitpLogService _log;

		Socket _socket;
		bool _isClosed;
		Dictionary<IPEndPoint, ConnectedClient> _clients = new Dictionary<IPEndPoint, ConnectedClient>();
		public Dictionary<IPEndPoint, ConnectedClient> Clients
		{
			get { return _clients; }
		}

		public CitpTcpListenService(IPAddress nicAddress, int port, ICitpLogService log)
		{
			_log = log;
			_ipLocal = new IPEndPoint(nicAddress, port);
		}

		public void StartListening()
		{
			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			try
			{
				_socket.Bind(_ipLocal);
			}
			catch (Exception ex)
			{
				_log.LogError(String.Format("Failed to start listening on TCP port {0}", _ipLocal.Port));
				_log.LogException(ex);
				return;
			}

			_socket.Listen(4);

			// Assign delegate that will be invoked when client connect.
			_socket.BeginAccept(new AsyncCallback(onClientConnection), null);
		}

		public void Close()
		{
			try
			{
				if (_socket != null)
				{
					_isClosed = true;

					// Close the clients
					foreach (var connectedClient in _clients.Values)
						connectedClient.Stop();

					_socket.Close();
					_socket = null;
				}
			}
			catch (ObjectDisposedException ex)
			{
				Debug.Fail(ex.ToString(), "Stop failed");
			}
		}



		void onClientConnection(IAsyncResult asyn)
		{
			if (_isClosed)
				return;

			try
			{
				Socket clientSocket = _socket.EndAccept(asyn);

				ConnectedClient connectedClient = new ConnectedClient(clientSocket);

				connectedClient.MessageRecived += onMessageRecived;
				connectedClient.Disconnected += onClientDisconnection;

				connectedClient.StartListen();

				var remoteEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;

				if (_clients.ContainsKey(remoteEndPoint))
				{
					throw new InvalidOperationException(String.Format("Client with IP EndPoint '{0}' already exists", remoteEndPoint));
				}

				_clients[remoteEndPoint] = connectedClient;
				_log.LogInfo(String.Format("Client at IP Endpoint {0} has been connected", remoteEndPoint));

				raiseClientConnected(connectedClient);

				// Assign delegate that will be invoked when next client connect.
				_socket.BeginAccept(new AsyncCallback(onClientConnection), null);
			}
			catch (ObjectDisposedException ex)
			{
				Debug.Fail(ex.ToString(), "OnClientConnection: Socket has been closed");
			}
			catch (Exception ex)
			{
				_log.LogError("OnClientConnection: Socket communication failed");
				_log.LogException(ex);
			}
		}

		void onClientDisconnection(object sender, SocketEventArgs e)
		{
			raiseClientDisconnected(e.Socket.RemoteEndPoint as IPEndPoint);

			var remoteEndPoint = e.Socket.RemoteEndPoint as IPEndPoint;

			if (_clients.ContainsKey(remoteEndPoint))
			{
				_clients.Remove(remoteEndPoint);
				_log.LogInfo(String.Format("Client at IP Endpoint {0} has been disconnected", e.Socket.RemoteEndPoint));
			}
			else
			{
				_log.LogError(String.Format("Unknown client '{0}' has been disconnected.", remoteEndPoint));
			}
		}

		void onMessageRecived(object sender, SocketMessageEventArgs e)
		{
			if (PacketReceieved != null)
				PacketReceieved(this, Tuple.Create(e.Socket.RemoteEndPoint as IPEndPoint, e.Message));
		}

		void raiseClientConnected(ConnectedClient client)
		{
			if (ClientConnect != null)
				ClientConnect(this, client);
		}

		void raiseClientDisconnected(IPEndPoint remoteEndpoint)
		{
			if (ClientDisconnect != null)
				ClientDisconnect(this, remoteEndpoint);
		}
	}


	class ConnectedClient
	{
		// Hold reference to client socket to allow sending messages to client
		
		SocketListener _listener;

		public ConnectedClient(Socket clientSocket)
		{
			ClientSocket = clientSocket;
			_listener = new SocketListener();
		}

		public Socket ClientSocket { get; private set; }

		// Register directly to SocketListener event
		public event EventHandler<SocketMessageEventArgs> MessageRecived
		{
			add { _listener.MessageRecived += value; }
			remove { _listener.MessageRecived -= value; }
		}

		// Register directly to SocketListener event
		public event EventHandler<SocketEventArgs> Disconnected
		{
			add { _listener.Disconnected += value; }
			remove { _listener.Disconnected -= value; }
		}

		public void StartListen()
		{
			_listener.StartReciving(ClientSocket);
		}

		public void Send(byte[] buffer)
		{
			if (ClientSocket == null)
				throw new InvalidOperationException("Can't send data. ConnectedClient is closed.");

			ClientSocket.Send(buffer);
		}

		public void Stop()
		{
			_listener.StopListening();
			ClientSocket = null;
		}
	}



	class SocketListener
	{
		const int BUFFER_LENGTH = 2048;
		readonly byte[] CITP_SEARCH_PATTERN = new byte[] { 0x43, 0x49, 0x54, 0x50, 0x01, 0x00 };

		AsyncCallback _workerCallBack;
		Socket _socketWorker;

		byte[] _currentPacket;
		int _packetBytesRemaining;

		public event EventHandler<SocketMessageEventArgs> MessageRecived;
		public event EventHandler<SocketEventArgs> Disconnected;

		public void StartReciving(Socket socket)
		{
			_socketWorker = socket;
			waitForData(socket);
		}

		public void StopListening()
		{
			if (_socketWorker != null)
			{
				_socketWorker.Close();
				_socketWorker = null;
			}
		}

		


		void waitForData(Socket socket)
		{
			try
			{
				if (_workerCallBack == null)
					_workerCallBack = new AsyncCallback(onDataReceived);

				var packet = Tuple.Create(socket, new byte[BUFFER_LENGTH]);

				// Start waiting asynchronously for single data packet
				socket.BeginReceive(
					   packet.Item2,
					   0,
					   packet.Item2.Length,
					   SocketFlags.None,
					   _workerCallBack,
					   packet);
			}
			catch (SocketException ex)
			{
				Debug.Fail(ex.ToString(), "WaitForData: Socket failed");
			}

		}

		void onDataReceived(IAsyncResult asyn)
		{
			var packet = asyn.AsyncState as Tuple<Socket, byte[]>;
			Socket socket = packet.Item1;

			if (!socket.Connected)
				return;

			try
			{
				int nBytesReceived;
				try
				{
					nBytesReceived = socket.EndReceive(asyn);
				}
				catch (SocketException)
				{
					Debug.Write("Client has been closed and cannot answer.");

					onConnectionDropped(socket);
					return;
				}

				if (nBytesReceived == 0)
				{
					Debug.Write("Client socket has been closed.");

					onConnectionDropped(socket);
					return;
				}

				parseCitpPackets(nBytesReceived, packet.Item2);

				// Wait for the next package
				waitForData(_socketWorker);
			}
			catch (Exception ex)
			{
				Debug.Fail(ex.ToString(), "OnClientConnection: Socket failed");
			}
		}

		void parseCitpPackets(int nBytesReceived, byte[] source)
		{
			int i = 0;
			while (i < nBytesReceived)
			{
				if (_packetBytesRemaining > 0)
				{
					i += copyBytesToPacket(i, Math.Min(_packetBytesRemaining, nBytesReceived), source);
				}
				else if (source.Skip(i).Take(CITP_SEARCH_PATTERN.Length).SequenceEqual(CITP_SEARCH_PATTERN))
				{
					UInt32 packetLength = BitConverter.ToUInt32(source, i + 8);

					// Ignore packets reporting their length as over 5MB, they're probably wrong
					if (packetLength > 5000000)
					{
						Console.WriteLine("Received a CITP packet with an invalid length of " + packetLength);
						continue;
					}

					_packetBytesRemaining = (int)packetLength;
					_currentPacket = new byte[packetLength];

					i += copyBytesToPacket(i, Math.Min(_packetBytesRemaining, nBytesReceived), source);
				}
				else
				{
					++i;
				}
			}
			
		}

		int copyBytesToPacket(int srcOffset, int nBytesToCopy, byte[] source)
		{
			Buffer.BlockCopy(source, srcOffset, _currentPacket,
				_currentPacket.Length - _packetBytesRemaining,
				nBytesToCopy);

			_packetBytesRemaining -= nBytesToCopy;

			if (_packetBytesRemaining == 0)
			{
				raiseMessageRecived(_currentPacket);
				_currentPacket = null;
			}

			return nBytesToCopy;
		}


		void raiseMessageRecived(byte[] buffer)
		{
			if (MessageRecived != null)
				MessageRecived(this, new SocketMessageEventArgs(_socketWorker, buffer));
		}

		void onDisconnection(Socket socket)
		{
			if (Disconnected != null)
				Disconnected(this, new SocketEventArgs(socket));
		}

		void onConnectionDropped(Socket socket)
		{
			_socketWorker = null;
			onDisconnection(socket);
		}
	}
}