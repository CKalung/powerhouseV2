using System;

using PHConnectionCollectorInterface;

namespace PHClientsPluginsInterface
{
	public interface IConnectionModules : IDisposable
	{
		void Start(string pluginPath, string ConfigFilePath);
		void Stop();
		string Name { get; }

//		void SetConnectionCollectorModule(IConnectionCollector ConnectionCollector);
		//IConnectionCollector ConnectionCollectorModule { set; }

//		void StartListening (int Port);
//		void StartListening (int Port, string certFilePath);
//		void StopListening ();

		//string SayHelloTo(string personName);
	}
}

