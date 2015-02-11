using System;

using PHTcpServerPluginTemplate;
using PHClientHttpConnectionCollector;

namespace PHHttpServerPluginModule
{
	public class HttpServerModule : TcpServerModuleTemplate
	{
		const string namaModule = "HttpServer Module";

		string certFilePath = "";
		int port = 0;


		public HttpServerModule ()
			: base(namaModule)
		{
			//base.ConnectionCollector = new PhHttpConnectionCollector ();
			base.ConnectionCollectorModule = new PhHttpConnectionCollector ();
		}

		private void LoadConfig(){
			using (CrossIniFile.INIFile a = new CrossIniFile.INIFile ("./httpconfig.ini")) {
				port = a.GetValue("HttpServerModule", "Port", 81);
				certFilePath = a.GetValue ("HttpServerModule", "CertificateFilePath", "");
				//"./SslKeys/server.pfx");
			}
		}

		public override void Start(string pluginPath ){ 
			LoadConfig ();
			base.StartListening (port, certFilePath);
		}

		public override void Stop(){
			base.StopListening ();
		}


	}
}

