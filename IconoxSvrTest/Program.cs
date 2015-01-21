using System;
using OnlineSmartCardClient;

namespace IconoxSvrTest
{
	class MainClass
	{
		static SmartCardReaderClient iconox = new SmartCardReaderClient (125, false);
		public static void Main (string[] args)
		{
			string statw="";
			string respw="";

			Console.WriteLine ("Hello World!");
			iconox.SendApduOnline ("127.0.0.1", 1333, "/Iconox", "/SamServer/63001", false,
				"00A40000023F00", ref statw, ref respw);

			Console.WriteLine ("StatusWord: "+statw);
			Console.WriteLine ("Respond: "+respw);

			iconox.Dispose ();
		}
	}
}
