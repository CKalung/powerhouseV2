using System;
using System.IO;
using System.Collections.Generic;
using Contracts;
using Mark42;

namespace PhGateProxy
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine("PowerHouse Gate Proxy starting....");

			var pluginsFolderPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "GateProxyPlugins");
			if(!Directory.Exists (pluginsFolderPath)){
				Directory.CreateDirectory (pluginsFolderPath);
			}

			PluginService<IPlugin> pluginService = new PluginService<IPlugin>(pluginsFolderPath, "*.dll", true);
			pluginService.PluginsAdded += pluginService_PluginAdded;
			pluginService.PluginsChanged += pluginService_PluginChanged;
			pluginService.PluginsRemoved += pluginService_PluginRemoved;


			pluginService.Start();

			Console.WriteLine("PowerHouse Gate Proxy started, Press Enter to stop....");
			//Console.ReadKey();
			Console.ReadLine ();

			pluginService.Stop();
		}

		#region Event handlers

		private static void pluginService_PluginRemoved(PluginService<IPlugin> sender, List<IPlugin> plugins)
		{
			foreach (var plugin in plugins)
			{
				Console.WriteLine("Gate Proxy: PluginRemoved: {0}.", plugin.Name);
				plugin.Dispose();
			}
		}

		private static void pluginService_PluginChanged(PluginService<IPlugin> sender, List<IPlugin> oldPlugins, List<IPlugin> newPlugins)
		{
			Console.WriteLine("Gate Proxy: PluginChanged: {0} plugins -> {1} plugins.", oldPlugins.Count, newPlugins.Count);
			foreach (var plugin in oldPlugins)
			{
				Console.WriteLine("Gate Proxy: ~removed: {0}.", plugin.Name);
				plugin.Dispose();
			}
			foreach (var plugin in newPlugins)
			{
				Console.WriteLine("Gate Proxy: ~added: {0}.", plugin.Name);
			}
		}

		private static void pluginService_PluginAdded(PluginService<IPlugin> sender, List<IPlugin> plugins)
		{
			foreach (var plugin in plugins)
			{
				Console.WriteLine("Gate Proxy: PluginAdded: {0}.", plugin.Name);
				//Console.WriteLine(plugin.SayHelloTo("Tony Stark"));
				if (!plugin.Start ()) {
					Console.WriteLine("Gate Proxy: Failed to start GateProxy plugins : " + plugin.Name);
				}
			}
		}

		#endregion
	}
}
