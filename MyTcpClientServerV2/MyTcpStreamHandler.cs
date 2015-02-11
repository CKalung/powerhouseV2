using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MyTcpClientServerV2
{
	public class MyTcpStreamHandler : IDisposable
	{
		#region Disposable
		private bool disposed;
		public void Dispose()
		{
			if (!this.disposed)
			{
				this.Dispose(true);
				GC.SuppressFinalize(this);
			}
		}
		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					this.disposeAll();
				}
				this.disposed = true;
			}
		}
		~MyTcpStreamHandler()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			Disconnect (ConnectionState);
		}

		public void Disconnect(ConnectionStateObject State){
//			writer.Close();
//			reader.Close();

			if (State != null)
				State.Dispose ();
			State = null;
		}

		public delegate void onDisconnectedEventArgs();
		public delegate void onReceived(ConnectionStateObject dataRec);

		public event onDisconnectedEventArgs onDisconnected;
		public event onReceived onDataReceived;

		ConnectionStateObject ConnectionState;

		public class ConnectionStateObject : IDisposable
		{
			#region Disposable
			private bool disposed;
			public void Dispose()
			{
				if (!this.disposed)
				{
					this.Dispose(true);
					GC.SuppressFinalize(this);
				}
			}
			private void Dispose(bool disposing)
			{
				if (!this.disposed)
				{
					if (disposing) {
						if (sb != null)
							sb.Clear ();
						if(client!=null) {
//							if (client.Client != null) {
//								client.Client.Close ();
//								client.Client = null;
//							}
							try{client.Close (); }catch{
							}
							//Console.WriteLine ("TcpClient CLose");
							client = null;
						}
						if (stream != null) {
							stream.Close ();
							//Console.WriteLine ("Stream Close");
							stream.Dispose ();
							//Console.WriteLine ("Stream Dispose");
						}
					}
					this.disposed = true;
				}
			}
			~ConnectionStateObject()
			{
				this.Dispose(false);
			}
			#endregion

			public TcpClient client = null;                // Stream socket.
			public Stream stream = null;                // Stream socket.
			public const int BufferSize = 16384;             // Size of receive buffer.
			public byte[] buffer = new byte[BufferSize];    // Receive buffer.
			public int DataLength=0;
			public StringBuilder sb = new StringBuilder();  // Received data string.
		}

		public ConnectionStateObject CurrentConnectionState {
			get { return ConnectionState; }
		}

		public void Start (Stream stream, TcpClient client)
		{
			ConnectionState = new ConnectionStateObject ();
			ConnectionState.buffer = new byte[16384];
			ConnectionState.client = client;
			ConnectionState.stream = stream;

			//NetworkStream strm = new NetworkStream (socket);
			try
			{
				stream.BeginRead(ConnectionState.buffer, 0, ConnectionState.buffer.Length,
					new AsyncCallback(ReadCallback), ConnectionState);
			}
			catch(Exception ex)
			{
//				Console.WriteLine ("Client disconnected : " + ex.Message);
				Disconnect (ConnectionState);
				if (onDisconnected != null)
					onDisconnected ();
			}
		}

		public void Stop(){
			Disconnect (ConnectionState);
		}

		void ReadCallback(IAsyncResult ar)
		{
			// Read the  message sent by the server. 
			//ctrTO = TIMEOUT_60;      // reset disconnect TIMEOUT
			//			ctrTO = TIMEOUT_07;        // reset disconnect TIMEOUT
			ConnectionStateObject state = null;
			Stream stream = null;
			int bytesRead = -1;
			try
			{
				state = (ConnectionStateObject)ar.AsyncState;
				if (state == null) return;
				stream = state.stream;
				if (stream == null) return;

				bytesRead = stream.EndRead(ar);
				//Console.WriteLine (byteCount.ToString () + " bytes read.");
			}
			catch(Exception ex)
			{
//				Console.WriteLine ("Client disconnected : " + ex.Message);
				Disconnect (state);
				if (onDisconnected != null)
					onDisconnected ();
				return;
			}

			try
			{
				if (bytesRead > 0)
				{
					// There might be more data, so store the data received so far.
					//state.sb.Append(Encoding.GetEncoding(1252).GetString(state.buffer, 0, bytesRead));
					byte[] data = new byte[bytesRead];
					Array.Copy(state.buffer, 0, data, 0, bytesRead);
					state.DataLength += bytesRead;
					state.sb.Append(Encoding.GetEncoding(1252).GetString(state.buffer, 0, bytesRead));
					//                        Console.WriteLine(state.sb.ToString();
					DataReceived(state);
				}
				else
				{
					// disconnected by client
					//state.sb.Clear();
					//Console.WriteLine ("Session ID : " + SessID + ", Disconnected by remote client");
					Disconnect(state);
					if (onDisconnected != null)
						onDisconnected ();
					return;
				}
				//Thread.Sleep(10);

				stream.BeginRead(state.buffer, 0, state.buffer.Length,
					new AsyncCallback(ReadCallback), state);
			}
			catch (Exception ex)
			{
				//Console.WriteLine ("Client has disconnected " + e.StackTrace);
//				Console.WriteLine ("Client has disconnected ");
				Disconnect(state);
				if (onDisconnected != null)
					onDisconnected ();
			}

		}

		StringBuilder sb = new StringBuilder();

		void DataReceived(ConnectionStateObject state){
			// There might be more data, so store the data received so far.
			if (onDataReceived != null)
				onDataReceived (state);
		}

		public bool SendResponse(string data){
			try{
				if (ConnectionState.stream != null) {
					ConnectionState.stream.Write (Encoding.GetEncoding(1252).GetBytes (data), 0, data.Length);
					ConnectionState.stream.Flush ();
					return true;
				}
			}catch{
			}
			return false;
		}

		public bool SendResponse(ConnectionStateObject State, string data){
			try{
				if (State.stream != null) {
					State.stream.Write (Encoding.GetEncoding(1252).GetBytes (data), 0, data.Length);
					State.stream.Flush ();
					return true;
				}
			}catch{
			}
			return false;
		}


	}
}

