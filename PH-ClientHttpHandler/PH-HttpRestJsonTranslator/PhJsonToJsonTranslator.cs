using System;
using PHClientProtocolTranslatorInterface;

namespace PHHttpRestJsonTranslator
{
	public class PhJsonToJsonTranslator :  IPhClientTranslator, IDisposable
	{
		#region Disposable
		private bool disposed;
		public void Dispose()
		{
			if (!this.disposed)
			{
				this.Dispose(true);
				GC.SuppressFinalize(this);
			}
		}
		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					this.disposeAll();
				}
				this.disposed = true;
			}
		}
		~PhJsonToJsonTranslator()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
		}

		public PhJsonToJsonTranslator ()
		{
		}

		int parseReturn = 0;
		//protected enum ParseCode { Completed = 0, Uncompleted = 1, Invalid = 2 }

		public int retParseCode{
			get { return parseReturn; }
			set { parseReturn = value; }
		}

		PPOBDatabase.PPOBdbLibs localDb = null;
		public PPOBDatabase.PPOBdbLibs DbConnect { 
			get { return localDb; }
			set { localDb = value; }
		}

		public string TranslateFromClient (string data){
			parseReturn = 0;	//completed
			return data;	// :D Gak ada transalasi krn defaultnya http rest json
		}

		public string TranslateToClient (string data){
			return data;	// :D Gak ada transalasi krn defaultnya http rest json
		}

	}
}

