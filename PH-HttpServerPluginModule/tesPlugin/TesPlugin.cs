using System;

using System.ComponentModel.Composition;

//using PHTcpServerPluginTemplate;
using PHClientsPluginsInterface;
//using PHConnectionCollectorInterface;

namespace tesPlugin
{
	[Export(typeof(IPlugin))]
	public class TesPlugin : BasePlugin
	{
		const string namaModule = "HttpServer Module";
		//IConnectionCollector connectionCollector = null;

		public TesPlugin ()
			: base(namaModule)
		{
			Console.WriteLine ("Modul dibuat > {0}", namaModule);
			//this.ConnectionCollectorModule = null;
			Console.WriteLine ("kolektor COnnector di buat");
		}

		private void LoadConfig(){
		}

//		public override void SetConnectionCollectorModule(IConnectionCollector ConnectionCollector) {
//			connectionCollector = ConnectionCollector; 
//		}

		public override void Start(string pluginPath ){ 
			//LoadConfig ();
			//base.StartListening (port, certFilePath);
			Console.WriteLine (" Di Start Pluginnya "+pluginPath);
		}

		public override void Stop(){
			//base.StopListening ();
		}


	}
}


