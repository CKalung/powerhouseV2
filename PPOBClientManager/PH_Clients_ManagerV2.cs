using System;
using System.Collections.Generic;

using PHClientsPluginsInterface;
using LOG_Handler;
using StaticCommonLibrary;

namespace PPOBClientsManager
{
	public class PH_Clients_ManagerV2 : IDisposable
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
		~PH_Clients_ManagerV2()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			//            FinnetKeeper.Dispose();
			//NitrogenOfflineProcessor.Dispose();
			onStop ();
		}

		string appPath;
		List<IConnectionModules> listPlugins = new List<IConnectionModules>();

		public PH_Clients_ManagerV2(string AppPath, bool consoleMode)
		{
			LogWriter.ConsoleMode = consoleMode;
			appPath = AppPath;
			LoadCommonConfig(System.IO.Path.Combine (AppPath, "commonconfig.txt"));
			//CommonLibrary.SessionMinutesTimeout = CommonConfigs.getInt("SessionMinutesTimeout");
			//MAX_CLIENT = TotClients;
			//NitrogenOfflineProcessor = new NitroClient(CommonConfigs);
		}

		private void LoadCommonConfig(string ConfigFilePath){
			string logPath = "";
			using (CrossIniFile.INIFile a = 
				new CrossIniFile.INIFile (ConfigFilePath)) {
				logPath = a.GetValue ("Common", "LogPath", "/powerhouse/phLogs");
			}
			if (!System.IO.Directory.Exists(logPath)) 
				System.IO.Directory.CreateDirectory(logPath);
			LOG_Handler.LogWriter.setPath(logPath);

		}


		//		ConnectionServerPluginHttp.HttpServerPlugin httpPlug;
		//		ConnectionServerPluginHttp.HttpServerPlugin httpsPlug;

		//		ConnectionServerPluginHttp.HttpTcpServerPlugin httpPlug;
		//		ConnectionServerPluginHttp.HttpTcpServerPlugin httpsPlug;
		ConnectionServerPluginHttp.HttpServerJsonPlugin httpJsonPlug;
		ConnectionServerPluginHttp.HttpServerJsonPlugin httpsJsonPlug;

		PHPlainJsonServerPlugin.PhPlainJsonServerPlugin nonSslPlainJsonPlug;

		ConnectionServerPluginHttpXmlArtajasa.HttpServerArtaJasaXmlPlugin httpArtaJasaPlug;

		public bool onStart()
		{
			httpJsonPlug = new ConnectionServerPluginHttp.HttpServerJsonPlugin ();
			httpJsonPlug.Start (appPath, System.IO.Path.Combine (appPath, "HttpConfig.ini"));
			listPlugins.Add (httpJsonPlug);

			httpsJsonPlug = new ConnectionServerPluginHttp.HttpServerJsonPlugin ();
			//httpsJsonPlug.Start (appPath, System.IO.Path.Combine (appPath, "HttpsConfig.ini"));
			listPlugins.Add (httpsJsonPlug);

			nonSslPlainJsonPlug = new PHPlainJsonServerPlugin.PhPlainJsonServerPlugin ();
			nonSslPlainJsonPlug.Start (appPath, System.IO.Path.Combine (appPath, "NonSslPlainJsonConfig.ini"));
			listPlugins.Add (nonSslPlainJsonPlug);

			httpArtaJasaPlug = new ConnectionServerPluginHttpXmlArtajasa.HttpServerArtaJasaXmlPlugin ();
			//httpArtaJasaPlug.Start (appPath, System.IO.Path.Combine (appPath, "HttpArtaJasaConfig.ini"));
			listPlugins.Add (httpArtaJasaPlug);

			return true;
		}

		public bool onStop()
		{
			foreach (IConnectionModules aModule in listPlugins) {
				try{
					aModule.Stop ();} catch{
				}
				if(aModule != null)
					aModule.Dispose ();
			}
			listPlugins.Clear ();

			return true;
		}

	}
}

