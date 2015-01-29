using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Contracts
{
    public interface IPlugin : IDisposable
    {
        string Name { get; }
		string SayHelloTo(string personName);
		bool Start();
    }
}
