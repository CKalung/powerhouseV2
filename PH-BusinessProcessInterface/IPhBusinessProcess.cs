using System;

namespace PHBusinessProcessInterface
{
	public interface IPhBusinessProcess : IDisposable
	{
		string TranslateFromClient (string data);
		string TranslateToClient (string data);
	}
}

