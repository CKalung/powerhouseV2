using System;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Threading;

using LOG_Handler;
using PPOBManager;
using StaticCommonLibrary;
using MyTcpClientServerV2;

using PHClientProtocolTranslatorInterface;

namespace PHClientHandlerInterface
{
	public abstract class BaseClientHandlers : MarshalByRefObject, IPhClientHandler
	{
		public BaseClientHandlers(int indexConnection, string configFilePath)
		{
			srecBuff = "";
			dataLength = 0;
			IndexConnection = indexConnection;
			ConfigFilePath = configFilePath;
		}

		bool disposedOnce=false;
		public virtual void Dispose()
		{
			//TODO:
			if (!disposedOnce) {
				disposedOnce = true;
				dataHandler.Dispose ();
			}
		}

		public delegate void onDisconnectedEvent(int index);
		public event onDisconnectedEvent onDisconnected;

		//public string Name { get; private set; }
		//public string IndexConnection { get; private set; }
		//public string ConfigFilePath { get; private set; }

		MyTcpStreamHandler dataHandler = new MyTcpStreamHandler ();
		protected enum ParseCode { Completed = 0, Uncompleted = 1, Invalid = 2 }
		protected ParseCode retParseCode = ParseCode.Uncompleted;

		protected const int TIMEOUT_07 = 7;
		protected const int TIMEOUT_10 = 10;
		protected const int TIMEOUT_15 = 15;
		protected const int TIMEOUT_60 = 60;
		int ctrTO = TIMEOUT_10;		// satuan detik
		int ctrTOPackage = TIMEOUT_15;		// satuan detik
		bool fExitThread = true;
		Thread MyTimeOut;
		//private Thread MyTimeOutPackage;

		protected PPOBDatabase.PPOBdbLibs localDB;

		string configFilePath = "";
		int indexConn=0;

		IPhClientTranslator phTranslator;

		public int IndexConnection { get { return indexConn; } set { indexConn = value; } }
		public string ConfigFilePath { 
			get { return configFilePath; } 
			set { 
				configFilePath = value; 
			} 
		}

		public IPhClientTranslator Translator{
			get { return phTranslator; }
			set { 
				phTranslator = value; 
				phTranslator.DbConnect = localDB;
			}
		}

		//public IConnectionCollector ConnectionCollectorModule { set; private get; }

		//public abstract string SayHelloTo(string personName);
//		public abstract void SetConnectionCollectorModule(IConnectionCollector ConnectionCollector);
//		public abstract void Start(string pluginPath, string ConfigFilePath);
//		public abstract void Stop();

//		public abstract void StartListening (int Port);
//		public abstract void StartListening (int Port, string certFilePath);
//		public abstract void StopListening ();

		/// <summary>
		/// Start the specified stream and client. Secure or non secure compatible.
		/// </summary>
		/// <param name="stream">Stream or SslStream.</param>
		/// <param name="client">TcpClient.</param>
		public void Start(Stream stream, TcpClient client){
			//
			//Console.WriteLine ("["+this.ToString () + "] START Base Client Handler");
			//
			dataHandler.onDataReceived += new MyTcpStreamHandler.onReceived (DataReceived);
			dataHandler.onDisconnected += new MyTcpStreamHandler.onDisconnectedEventArgs (onConnectionDisconnected);
			dataHandler.Start (stream, client);

			fExitThread = false;		// ditambah didieu
			ctrTO = TIMEOUT_15;		    // initial timeout 15 detik, untuk data pertama
			MyTimeOut = new Thread(new ThreadStart(ConnTimeOut));
			MyTimeOut.Start();
		}

		bool eventOnce = false;
		private void onConnectionDisconnected(){
//			Console.WriteLine ("["+this.ToString () + "] Disconnected trigger");
			if (!eventOnce) {
				eventOnce = true;
				if (onDisconnected != null)
					onDisconnected (indexConn);
			}
		}

		protected void Disconnect(MyTcpStreamHandler.ConnectionStateObject State){
//			Console.WriteLine ("["+this.ToString () + "] Disconnect MANUAL");
			if (dataHandler != null) {
				dataHandler.Disconnect (State);
				onConnectionDisconnected();
			}
		}

		protected void DisableTimeOut()
		{
			fExitThread = true;
			ctrTO = TIMEOUT_60;
			ctrTOPackage = TIMEOUT_60;
		}

		// timeout untuk disconnect koneksi yg idle
		private void ConnTimeOut()
		{
			int dt = 10;
			//Console.WriteLine("Timeout = " + ctrTO);
			while (!fExitThread)
			{
				dt--;
				System.Threading.Thread.Sleep(100);	// supaya looping lebih halus, tong dibikin sleep (1000)
				if (dt <= 0)	// perdetik
				{
					dt = 10;
					ctrTO--;	// satuan detik
					if (ctrTO <= 0)
					{
						// Timeout fired
						fExitThread = true;
						MyTcpStreamHandler.ConnectionStateObject state = 
							dataHandler.CurrentConnectionState;
						Disconnect (state);
						return;
					}
				}
			}
		}

		// timeout untuk paket yang tidak kunjung lengkap
		private void ConnTimeOutPackage()
		{
			int dt = 10;
			//Console.WriteLine("Timeout = " + ctrTO);
			while (!fExitThread)
			{
				dt--;
				System.Threading.Thread.Sleep(100);	// supaya looping lebih halus, tong dibikin sleep (1000)
				if (dt <= 0)	// perdetik
				{
					dt = 10;
					ctrTOPackage--;	// satuan detik
					if (ctrTOPackage <= 0)
					{
						// Timeout fired
						fExitThread = true;
						//						MyTcpStreamHandler.ConnectionStateObject state = 
						//							dataHandler.CurrentConnectionState;
						//						Disconnect (state);
						Disconnect (dataHandler.CurrentConnectionState);
						return;
					}
				}
			}
		}

		protected int TimeOutSecondsSetting{
			set { ctrTO = value; }
		}

		protected bool SendResponse(MyTcpStreamHandler.ConnectionStateObject State, string data){
			try{
				if (State.stream != null) {
					State.stream.Write (Encoding.GetEncoding(1252).GetBytes (data), 0, data.Length);
					State.stream.Flush ();
					return true;
				}
			}catch{
			}
			return false;
		}

		protected string srecBuff = "";
		protected const int MAXRecBuff = 100 * 1024;  // max 100Kb
		//        byte[] dataBuffer = new byte[2048];
		protected int dataLength = 0;

		// abstract tuh HARUS di override, virtual boleh tidak
		private void DataReceived(MyTcpStreamHandler.ConnectionStateObject State){
			TimeOutSecondsSetting = TIMEOUT_15;		// reset timeout untuk penerimaan berikutnya

			if (Translator == null) {
				throw new Exception ("No Client Protocol Translator found...");
			}

			// Validasi Http protocol

			//srecBuff += Encoding.GetEncoding(1252).GetString(data);
			srecBuff += State.sb.ToString ();
			dataLength += State.DataLength;

			//			Console.WriteLine ("=======================TERIMAAAA========================");
			//			Console.WriteLine ("State Secure : " + State.isSecureConnection.ToString ());
			//			Console.WriteLine ("Data diterima : " + State.sb.ToString ());
			//			Console.WriteLine ("Data srecBuff : " + srecBuff);
			//			Console.WriteLine ("========================AKHIRRR=========================");

			//Console.WriteLine ("==== SEBELUM translate client: " + srecBuff);

			srecBuff = Translator.TranslateFromClient (srecBuff);		// data lengkap
			retParseCode = (ParseCode)Translator.retParseCode;

			//Console.WriteLine ("==== SETELAH translate client: " + srecBuff);

			if (retParseCode == ParseCode.Completed) {
				retParseCode = ParseTranslatedData (State, srecBuff);
			}

			switch (retParseCode)	//retCode)
			{
			case ParseCode.Invalid:
				//case HTTPRestConstructor.retParseCode.Invalid:
				srecBuff = "";
				dataLength = 0;
				Disconnect(State);
				break;
			case ParseCode.Uncompleted:
				//case HTTPRestConstructor.retParseCode.Uncompleted:
				if (srecBuff.Length > MAXRecBuff)
				{
					srecBuff = "";
					dataLength = 0;
					Disconnect(State);
				}
				return;
			case ParseCode.Completed:
				//case HTTPRestConstructor.retParseCode.Completed:
				DisableTimeOut ();
				ProcessCompletedData(State);
				break;
			default:
				srecBuff = "";
				dataLength = 0;
				Disconnect(State);
				break;
			}
			return;

			//string dataReal = Encoding.GetEncoding(1252).GetString(data);
			//Console.WriteLine(dataReal);
		}

		protected virtual ParseCode ParseTranslatedData(MyTcpStreamHandler.ConnectionStateObject State, string TranslatedData){
			return ParseCode.Completed;
		}

		protected virtual void ProcessCompletedData(MyTcpStreamHandler.ConnectionStateObject State){
		}

		protected void FinalizeClientConnection(MyTcpStreamHandler.ConnectionStateObject State, string respondToClient){
			try {
				respondToClient = Translator.TranslateToClient (respondToClient);
				LogWriter.show (this, "SEND TO CLIENT: " + respondToClient);
				if (respondToClient.Length != 0) {
					if (State.client != null) {
						SendResponse (State, respondToClient);
					}
				}
			} catch (Exception ex) {
				// disini reply ke client tidak bisa diterima client
				LogWriter.show (this, "ERROR: " + ex.getCompleteErrMsg ());
			}
			Disconnect (State);
		}

	}
}

