using System;
using System.ComponentModel.Composition;

using PHClientsPluginsInterface;
using PHTcpServerModule;
using PHClientHttpConnectionCollector;

namespace ConnectionServerPluginHttp
{
	[Export(typeof(IConnectionModules))]
	public class HttpServerPlugin : BaseConnectionPlugins
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
		~HttpServerPlugin()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			if (server != null)
				server.Dispose ();
			if (connectionCollector != null)
				connectionCollector.Dispose ();
		}

		TcpServerModule server;
		PhHttpConnectionCollector connectionCollector = null;

		const string namaModule = "PH HttpServer Module";

		string certFilePath = "";
		int port = 0;

		public HttpServerPlugin ()
			:base(namaModule) {
			connectionCollector = new PhHttpConnectionCollector ();
			server = new TcpServerModule ();
			server.onNewConnection += HandleonNewConnection; 
		}

		void HandleonNewConnection (System.IO.Stream stream, System.Net.Sockets.TcpClient client)
		{
			if (connectionCollector != null) {
				connectionCollector.NewConnection (stream, client);
			}
		}

		private void LoadConfig(){
			using (CrossIniFile.INIFile a = new CrossIniFile.INIFile ("httpconfig.ini")) {
				port = a.GetValue("HttpServerModule", "Port", 443);
				Console.WriteLine ("Port = " + port.ToString ());
				certFilePath = a.GetValue ("HttpServerModule", "CertificateFilePath", "");
				Console.WriteLine ("Certificate = " + certFilePath);
				//"./SslKeys/server.pfx");
			}
		}

		public override void Start(string pluginPath ){ 
			Console.WriteLine ("Start plugin http");
			Console.WriteLine ("Paths : \r\n");
			Console.WriteLine (
				AppDomain.CurrentDomain.SetupInformation.ApplicationBase +"\r\n" +
				System.Reflection.Assembly.GetExecutingAssembly().Location +"\r\n"
				);
			LoadConfig ();
			server.StartListening (port, certFilePath);
		}

		public override void Stop(){
			server.StopListening ();
		}



	}
}

