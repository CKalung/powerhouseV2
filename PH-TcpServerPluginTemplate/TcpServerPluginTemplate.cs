﻿using System;
using System.IO;
using System.Net.Security;
using System.ComponentModel.Composition;
using System.Security.Cryptography.X509Certificates;

using MyTcpClientServerV2;
using PHClientsPluginsInterface;
using PHConnectionCollectorInterface;

namespace PHTcpServerPluginTemplate
{
	[Export(typeof(IConnectionModules))]
	public class TcpServerPluginTemplate : BaseConnectionPlugins
		//: base("Plugin1")
	{
		#region Disposable
		private bool disposed;
		public override void Dispose()
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
		~TcpServerPluginTemplate()
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

		IConnectionCollector connectionCollector = null;

		public TcpServerPluginTemplate (string name)
			: base(name)
		{
			Console.WriteLine("ctor_{0}", Name);
			Console.WriteLine ("Template TcpServer created dari modul {0}", Name);
		}

//		public IConnectionCollector ConnectionCollectorModule {
//			set { connectionCollector = value; }
//		}

		public override void SetConnectionCollectorModule(IConnectionCollector ConnectionCollector) {
			connectionCollector = ConnectionCollector; 
		}

		public override void Start(string pluginPath ){ 
			Console.WriteLine ("Start di panggil di " + this.ToString ());
		}

		public override void Stop(){
			Console.WriteLine ("Stop di panggil di " + this.ToString ());
		}

		public override void StartListening(int Port){ 
			StartListening (Port,"");
		}
		public override void StartListening(int Port, string certFilePath){ 
			//IConnectionCollector ConnectionCollectorModule){

			//commonSettings = commonConfigs;
			//ConnectionCollector = ConnectionCollectorModule;

			int port = Port;
			try
			{
				Console.WriteLine (Name + " plugin starting...");

				//            appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

				Console.WriteLine ("Executing path = " + System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
				Console.WriteLine ("Calling Path = " + System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly ().Location));
				Console.WriteLine ("Entry path = " + System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly ().Location));

				if(certFilePath != "" ){
					if (!File.Exists(certFilePath)){
						Console.WriteLine ("Certificate file not found " + certFilePath);
						return;
					}

					RemoteCertificateValidationCallback certValidationCallback = null;
					certValidationCallback = new RemoteCertificateValidationCallback(IgnoreCertificateErrorsCallback);

					certificateFile = new X509Certificate2(certFilePath, "d4mpt");

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
		public override void StopListening(){
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

			if (connectionCollector != null)
				connectionCollector.NewConnection (stream, args.Client);
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

			if(connectionCollector!=null)
				connectionCollector.NewConnection (stream, args.Client);
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

