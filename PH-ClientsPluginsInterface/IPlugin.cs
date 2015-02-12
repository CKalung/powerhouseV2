
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHClientsPluginsInterface
{
	public interface IPlugin : IDisposable
	{
		void Start(string pluginPath);
		void Stop();
		string Name { get; }
		//string SayHelloTo(string personName);
	}
}
