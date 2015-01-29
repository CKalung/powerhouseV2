using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Contracts;

namespace ArtaJasa_plugins
{
	[Export(typeof(IPlugin))]
	public class Plugin : BasePlugin
	{
		[ImportingConstructor]
		public Plugin()
			: base("Plugin2")
		{
			Console.WriteLine("ctor_{0}", Name);
		}

		public override string SayHelloTo(string personName)
		{
			string hello = string.Format("Hello {0} from {1}.", personName, Name);

			return hello;
		}

		public override void Dispose()
		{
			Console.WriteLine("dispose_{0}", Name);
		}

		public override bool Start()
		{

			return true;
		}
	}
}

