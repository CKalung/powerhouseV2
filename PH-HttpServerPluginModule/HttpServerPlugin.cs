

using System;
using System.IO;

using PHClientsPluginsInterface;
using PHConnectionCollectorInterface;

using PHClientHttpConnectionCollector;
using PHTcpServerPluginTemplate;

namespace PHHttpServerPluginModule
{
	//[Export(typeof(IConnectionModules))]
	public class HttpServerPluginModule : TcpServerPluginTemplate
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
		~HttpServerPluginModule()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
		}

		IConnectionCollector connectionCollector = null;

		const string namaModule = "HttpServer Module";

		string certFilePath = "";
		int port = 0;


		public HttpServerPluginModule ()
			: base(namaModule)
		{
			Console.WriteLine ("Modul dibuat > {0}", namaModule);
			//base.ConnectionCollectorModule = new PhHttpConnectionCollector ();
			//base.SetConnectionCollectorModule (new PhHttpConnectionCollector ());
			SetConnectionCollectorModule (new PhHttpConnectionCollector ());
			connectionCollector = new PhHttpConnectionCollector ();
			Console.WriteLine ("kolektor COnnector di buat");
		}

		private void LoadConfig(){
			using (CrossIniFile.INIFile a = new CrossIniFile.INIFile ("./httpconfig.ini")) {
				port = a.GetValue("HttpServerModule", "Port", 81);
				Console.WriteLine ("Port = " + port.ToString ());
				certFilePath = a.GetValue ("HttpServerModule", "CertificateFilePath", "");
				Console.WriteLine ("Certificate = " + certFilePath);
				//"./SslKeys/server.pfx");
			}
		}

		public override void Start(string pluginPath ){ 
			Console.WriteLine ("Start plugin http");
			LoadConfig ();
			base.StartListening (port, certFilePath);
		}

		public override void Stop(){
			StopListening ();
		}

		public void StartListening(int Port){ 
			StartListening (Port,"");
		}
		public void StartListening(int Port, string certFilePath){ 
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

