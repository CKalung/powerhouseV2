using System;

namespace PHClientProtocolTranslatorInterface
{
	public interface IPhClientTranslator : IDisposable
	{
		//enum ParseCode { Completed = 0, Uncompleted = 1, Invalid = 2 }
		int retParseCode { get; set; }
		PPOBDatabase.PPOBdbLibs DbConnect { get; set; }
		string TranslateFromClient (string data);
		string TranslateToClient (string data);
	}
}

