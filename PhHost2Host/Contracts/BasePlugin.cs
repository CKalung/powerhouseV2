using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Contracts
{
    public abstract class BasePlugin : MarshalByRefObject, IPlugin
    {
        public BasePlugin(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

		public abstract string SayHelloTo(string personName);

		public abstract bool Start();

        public virtual void Dispose()
        {
            //TODO:
        }
    }
}
