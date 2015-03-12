using System;
using System.IO;
using System.Collections;
using System.Runtime.CompilerServices;
using System.ComponentModel.Composition;

using PHClientHttpXmlProtocolHandler;
using PHClientsPluginsInterface;
using PHClientCommunicationObjectData;
using PHArtaJasaXmlTranslator;
using PHSimpleHttpServer;
using LOG_Handler;

namespace ConnectionServerPluginHttpXmlArtajasa
{
	[Export(typeof(IConnectionModules))]
	public class HttpServerArtaJasaXmlPlugin : BaseConnectionPlugins
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
		~HttpServerArtaJasaXmlPlugin()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			//			if (connectionCollector != null)
			//				connectionCollector.Dispose ();

			Stop ();
		}

		string configFilePath = "";
		const string namaModule = "PH Http Server ArtaJasa Module Plugin";

		PhSimpleTcpServer server;

		string certFilePath = "";
		string certPassword = "";
		int port = 0;

		// ==================== Spesific variable from config file ==============
		string ArtajasaUsername = "";
		string ArtajasaPassword = "";
		// ==================== Spesific variable from config file ==============

		public HttpServerArtaJasaXmlPlugin ()
			:base(namaModule) {
			//connectionCollector = new PhHttpConnectionCollector ();
		}

		private void LoadConfig(string ConfigFilePath){
			using (CrossIniFile.INIFile a = 
				new CrossIniFile.INIFile (ConfigFilePath)) {
				port = a.GetValue("HttpServerModule", "Port", 443);
				certFilePath = a.GetValue ("HttpServerModule", "CertificateFilePath", "");
				certPassword = a.GetValue ("HttpServerModule", "CertificatePassword", "");
				ArtajasaUsername = a.GetValue ("Artajasa", "Username", "username");
				ArtajasaPassword = a.GetValue ("Artajasa", "Password", "password");
				LogWriter.show (this,"Http Port = " + port.ToString () + ", Certificate = " + certFilePath);
				//"./SslKeys/server.pfx");
			}
		}

		public override void Start(string applicationPath, string ConfigFilePath){
			//ConfigFilePath = System.IO.Path.Combine (applicationPath, ConfigFile);
			//ConfigFilePath = configFilePath;
			//			Console.WriteLine ("Start plugin http");
			//			Console.WriteLine ("Paths : \r\n");
			//			Console.WriteLine (
			//				AppDomain.CurrentDomain.SetupInformation.ApplicationBase +"\r\n" +
			//				System.Reflection.Assembly.GetExecutingAssembly().Location +"\r\n"
			//			);

			configFilePath = ConfigFilePath;
			LoadConfig (ConfigFilePath);
			//connectionCollector.ConfigFilePath = ConfigFilePath;

			server = new PhSimpleTcpServer ();
			server.onRequestClientHandler += new PhSimpleTcpServer.onRequestClientHandlerEvent (onRequestHandler);
			server.Start (port, certFilePath, certPassword);

		}

		public override void Stop(){
			if (server != null) {
				try {
					server.Stop ();
				} catch {
				}
				server.Dispose ();
				server = null;
			}
		}

		void onRequestHandler(int indexConnection){
			PhHttpXmlClientHandler xmlClientHandler = new PhHttpXmlClientHandler (indexConnection, configFilePath);
			xmlClientHandler.Translator = new PhArtaJasaXmlObjectTranslator ();
			PHArtajasaBisPro.PhArtaJasaProcessor AjBisPro = new PHArtajasaBisPro.PhArtaJasaProcessor ();
			AjBisPro.SetArtajasaUsernamePassword(ArtajasaUsername,ArtajasaPassword);
			xmlClientHandler.BusinessProcessor = AjBisPro;
			server.ClientHandler = xmlClientHandler;
		}

	}
}

