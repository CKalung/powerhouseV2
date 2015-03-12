using System;
using System.IO;
using System.Net.Sockets;

using PHClientProtocolTranslatorInterface;

namespace PHClientHandlerInterface
{
	//public delegate void onRequestClientHandlerEvent();

	public interface IPhClientHandler : IDisposable
	{
//		public string Name { get; private set; }

		//event onRequestClientHandlerEvent onRequestClientHandler;


//		int IndexConnection { get; }
//		string ConfigFilePath { get; }

		IPhClientTranslator Translator { set; }

		void Start (Stream stream, TcpClient client);

//		string TranslateFromClient (string data);
//		string TranslateToClient (string data);
	}
}

