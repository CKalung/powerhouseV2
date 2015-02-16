

using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using MyTcpClientServerV2;

namespace PHTcpServerModule
{

	public class TcpServerModule : IDisposable
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
		~TcpServerModule()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
		}

		MyTcpServer server = null;
		X509Certificate2 certificateFile = null;
		string certificatePassword = "";

		string configFilePath = "";

		public delegate void onNewConnectionEvent(Stream stream, System.Net.Sockets.TcpClient client);
		public event onNewConnectionEvent onNewConnection;


		public TcpServerModule ()
		{
		}

		public string CertificatePassword{
			set { certificatePassword = value; }
			get { return certificatePassword; }
		}

		public string ConfigFilePath{
			set { configFilePath = value; }
			get { return configFilePath; }
		}

		public void StartListening(int Port){ 
			StartListening (Port,"");
		}
		public void StartListening(int Port, string certFilePath){ 
			int port = Port;
			try
			{
				//            appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

				if(certFilePath != "" ){
					if (!File.Exists(certFilePath)){
						Console.WriteLine ("Certificate file not found " + certFilePath);
						return;
					}

					RemoteCertificateValidationCallback certValidationCallback = null;
					certValidationCallback = new RemoteCertificateValidationCallback(IgnoreCertificateErrorsCallback);

					certificateFile = new X509Certificate2(certFilePath, certificatePassword);	// "d4mpt");

					server = new MyTcpServer(port, certificateFile,
						new SecureConnectionResultsCallback(OnServerSecureConnectionAvailable));
				} else{
					server = new MyTcpServer(port, 
						new NonSecureConnectionResultsCallback(OnServerNonSecureConnectionAvailable));
				}

				server.StartListening();

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}

			//sleep to avoid printing this text until after the callbacks have been invoked.
			//Thread.Sleep(4000);


		}
		public void StopListening(){
			if (server != null)
				server.Dispose();
			Console.WriteLine("Plugin stoped");
		}


		//		MyTcpStreamHandler dataHandler;
		void OnServerNonSecureConnectionAvailable(object sender, NonSecureConnectionResults args)
		{
			if (args.AsyncException != null)
			{
				Console.WriteLine(args.AsyncException);
				return;
			}

			Stream stream = args.NonSecureStream;
			if (stream == null)
			{
				Console.WriteLine("CLient already disconnected");
				return;
			}

			if (onNewConnection != null)
				onNewConnection (stream, args.Client);
			else {
				if (args.Client != null)
					args.Client.Close ();
				if (stream != null) {
					stream.Close ();
					stream.Dispose ();
				}
			}
			//			dataHandler = new MyTcpStreamHandler ();
			//			dataHandler.Start (stream, args.Client);
			//			dataHandler.onDataReceived += new MyTcpStreamHandler.onReceived (DataReceived);
			//
		}

		void OnServerSecureConnectionAvailable(object sender, SecureConnectionResults args)
		{
			if (args.AsyncException != null)
			{
				Console.WriteLine(args.AsyncException);
				return;
			}

			Stream stream = args.SecureStream;
			if (stream == null)
			{
				Console.WriteLine("CLient already disconnected");
				return;
			}

			if (onNewConnection != null)
				onNewConnection (stream, args.Client);
			else {
				if (args.Client != null)
					args.Client.Close ();
				if (stream != null) {
					stream.Close ();
					stream.Dispose ();
				}
			}
			//			dataHandler = new MyTcpStreamHandler ();
			//			dataHandler.Start (stream, args.Client);
			//			dataHandler.onDataReceived += new MyTcpStreamHandler.onReceived (DataReceived);

		}

		//		private void DataReceived(MyTcpStreamHandler.ConnectionStateObject State){
		//			Console.WriteLine ("Terima data = " + State.sb.ToString ());
		//			string balesan = "[" + DateTime.Now.ToString ("yy-MM-dd HH:mm:ss") + "] " + "Data di terima rojerrrr";
		//			Console.WriteLine ("Bales dengan = " + balesan);
		//			dataHandler.SendResponse (State, balesan);
		//			Console.WriteLine ("Siap diskonek");
		//
		//			if (State.client != null) {
		//				try{
		//					Console.WriteLine ("TcpClient CLose");
		//					State.client.Close ();
		//				} catch {
		//				}
		//				Console.WriteLine ("TcpClient null");
		//				State.client = null;
		//			}
		//
		//			if (State.stream != null) {
		//				Console.WriteLine ("Stream Close");
		//				State.stream.Close ();
		//				Console.WriteLine ("Stream Dispose");
		//				State.stream.Dispose ();
		//			}
		//
		//			dataHandler.Disconnect (State);
		//			Console.WriteLine ("Udah diskonek");
		//			dataHandler.Dispose ();
		//			Console.WriteLine ("Udah dispose");
		//
		//		}

		bool IgnoreCertificateErrorsCallback(object sender,
			X509Certificate certificate,
			X509Chain chain,
			SslPolicyErrors sslPolicyErrors)
		{
			Console.WriteLine("IgnoreCertificateErrorsCallback: {0}", sslPolicyErrors);
			if (sslPolicyErrors != SslPolicyErrors.None)
			{

				Console.WriteLine("IgnoreCertificateErrorsCallback: {0}", sslPolicyErrors);
				//you should implement different logic here...

				if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
				{
					foreach (X509ChainStatus chainStatus in chain.ChainStatus)
					{
						Console.WriteLine("\t" + chainStatus.Status);
					}
				}
			}

			//returning true tells the SslStream object you don't care about any errors.
			return true;
		}

	}
}

