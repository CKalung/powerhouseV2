using System;

using PHConnectionCollectorInterface;

namespace PHClientsPluginsInterface
{
	public abstract class BaseConnectionPlugins : MarshalByRefObject, IConnectionModules
	{
		public BaseConnectionPlugins(string name)
		{
			Name = name;
		}

		public string Name { get; private set; }

		public IConnectionCollector ConnectionCollectorModule { set; private get; }

		//public abstract string SayHelloTo(string personName);

		public abstract void Start(string pluginPath);
		public abstract void Stop();

		public virtual void Dispose()
		{
			//TODO:
		}
	}
}

