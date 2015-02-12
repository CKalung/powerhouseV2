using System;
using System.IO;
using System.Collections.Generic;

using Mark42;
using PHClientsPluginsInterface;

namespace PhClientsManager
{
	public class TesLoader : IDisposable
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
		~TesLoader()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			try{
				pluginService.Stop ();}catch{
			}
		}

		PluginService<IPlugin> pluginService=null;

		string pluginsFolderPath = "";

		//PublicSettings.Settings CommonConfigs = new PublicSettings.Settings();

		public TesLoader (string AppPath, bool consoleMode){
			//pluginsFolderPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "ClientConnectionPlugins");
			pluginsFolderPath = Path.Combine(AppPath, "ClientConnectionPlugins");
			if (!System.IO.Directory.Exists (pluginsFolderPath)) {
				System.IO.Directory.CreateDirectory (pluginsFolderPath);
			}

			pluginService = new PluginService<IPlugin>(pluginsFolderPath, "*.dll", true);
			pluginService.PluginsAdded += pluginService_PluginAdded;
			pluginService.PluginsChanged += pluginService_PluginChanged;
			pluginService.PluginsRemoved += pluginService_PluginRemoved;

			//loadConfig ();
		}

		public void Start(){
			pluginService.Start();
		}
		public void Stop(){
			pluginService.Stop();
		}

		#region Event handlers

		private void pluginService_PluginRemoved(PluginService<IPlugin> sender, 
			List<IPlugin> plugins)
		{
			foreach (var plugin in plugins)
			{
				Console.WriteLine("PluginRemoved: {0}.", plugin.Name);
				plugin.Dispose();
			}
		}

		private void pluginService_PluginChanged(PluginService<IPlugin> sender, List<IPlugin> oldPlugins, List<IPlugin> newPlugins)
		{
			Console.WriteLine("PluginChanged: {0} plugins -> {1} plugins.", oldPlugins.Count, newPlugins.Count);
			foreach (var plugin in oldPlugins)
			{
				Console.WriteLine("~removed: {0}.", plugin.Name);
				plugin.Dispose();
			}
			foreach (var plugin in newPlugins)
			{
				Console.WriteLine("~added: {0}.", plugin.Name);
				try{
					plugin.Start ("");
				}catch (Exception ex) {
					Console.WriteLine ("Failed to start plugin " + plugin.Name + "\r\n " + ex.Message);
				}
			}
		}

		private void pluginService_PluginAdded(PluginService<IPlugin> sender, List<IPlugin> plugins)
		{
			foreach (var plugin in plugins)
			{
				Console.WriteLine("PluginAdded: {0}.", plugin.Name);
				//Console.WriteLine(plugin.SayHelloTo("Tony Stark"));
				try{
					plugin.Start ("");
				}catch (Exception ex) {
					Console.WriteLine ("Failed to start plugin " + plugin.Name + "\r\n " + ex.Message);
				}
			}
		}

		#endregion

		//		public bool loadConfig()
		//		{
		//			//			string name = System.Configuration.con .ConfigurationManager. .AppSettings["OperatorName"];
		//			//			Console.WriteLine("Welcome " + name);
		//			//			string level = System.Configuration.ConfigurationManager.AppSettings["LoggerLevel"];
		//			//
		//			//			Console.WriteLine("Logger level: " + level);
		//
		//			//            appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
		//			using (CrossIniFile.INIFile a = new CrossIniFile.INIFile (pluginsFolderPath + "/config.ini")) {
		//				CommonConfigs.DbHost = a.GetValue("PostgreDB", "Host", "127.0.0.1");
		//				CommonConfigs.DbUser = a.GetValue("PostgreDB", "Username", "postgres");
		//				CommonConfigs.DbPort = a.GetValue("PostgreDB", "Port", 5432);
		//				CommonConfigs.DbPassw = a.GetValue("PostgreDB", "Password", "");
		//				CommonConfigs.DbName = a.GetValue("PostgreDB", "DBName", "");
		//
		//				CommonConfigs.localDb = new PPOBDatabase.PPOBdbLibs(CommonConfigs.DbHost, CommonConfigs.DbPort,
		//					CommonConfigs.DbName, CommonConfigs.DbUser, CommonConfigs.DbPassw);
		//				CommonConfigs.ReloadSettings();
		//				if (CommonConfigs.SettingCollection == null) return false;
		//
		//				if (!System.IO.Directory.Exists(CommonConfigs.getString("LogPath"))) 
		//					System.IO.Directory.CreateDirectory(CommonConfigs.getString("LogPath"));
		//				LOG_Handler.LogWriter.setPath(CommonConfigs.getString("LogPath"));
		//				return true;
		//			}
		//		}

	}
}

