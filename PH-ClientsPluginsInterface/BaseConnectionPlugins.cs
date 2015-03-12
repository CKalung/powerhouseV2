using System;

//using PHConnectionCollectorInterface;

namespace PHClientsPluginsInterface
{
	public abstract class BaseConnectionPlugins : MarshalByRefObject, IConnectionModules
	{
		public BaseConnectionPlugins(string name)
		{
			Name = name;
		}

		public string Name { get; private set; }

		//public IConnectionCollector ConnectionCollectorModule { set; private get; }

		//public abstract string SayHelloTo(string personName);
//		public abstract void SetConnectionCollectorModule(IConnectionCollector ConnectionCollector);
		public abstract void Start(string pluginPath, string ConfigFilePath);
		public abstract void Stop();

//		public abstract void StartListening (int Port);
//		public abstract void StartListening (int Port, string certFilePath);
//		public abstract void StopListening ();

		public virtual void Dispose()
		{
			//TODO:
		}
	}
}

