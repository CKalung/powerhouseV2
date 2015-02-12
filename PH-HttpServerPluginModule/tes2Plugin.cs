using System;

using tesPlugin;

namespace PHHttpServerPluginModule
{
	public class tes2Plugin : TesPlugin
	{
		public tes2Plugin ()
		{
		}

		public override void Start(string pluginPath ){ 
			Console.WriteLine ("tes 2 ini");
			base.Start (pluginPath);
		}
	}
}

