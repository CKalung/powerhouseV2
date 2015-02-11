using System;

using PHConnectionCollectorInterface;

namespace PHClientsPluginsInterface
{
	public interface IConnectionModules : IDisposable
	{
		void Start(string pluginPath);
		void Stop();
		string Name { get; }
		IConnectionCollector ConnectionCollectorModule { set; }

		//string SayHelloTo(string personName);
	}
}

