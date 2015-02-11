using System;
using System.IO;
using System.Net.Sockets;

namespace PHConnectionCollectorInterface
{
	public interface IConnectionCollector : IDisposable
	{
		void NewConnection(Stream stream, TcpClient client);
		string Name { get; }
	}
}

