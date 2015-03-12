using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;

using LOG_Handler;
using PPOBManager;
using PPOBHttpRestData;
using StaticCommonLibrary;
using MyTcpClientServerV2;

namespace PHClientHttpHandler
{
	public class PhHttpHandler : IDisposable {

		#region Disposable
		private bool disposed = false;
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
		~PhHttpHandler()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			dataHandler.Dispose ();
			if(CommonConfigs.localDb != null)
				CommonConfigs.localDb.Dispose ();
		}


		public delegate void onDisconnectedEvent(int index);
		public event onDisconnectedEvent onDisconnected;

		MyTcpStreamHandler dataHandler = new MyTcpStreamHandler ();
		int indexConn =0;
		const int TIMEOUT_07 = 7;
		const int TIMEOUT_10 = 10;
		const int TIMEOUT_15 = 15;
		const int TIMEOUT_60 = 60;
		int ctrTO = TIMEOUT_10;		// satuan detik
		int ctrTOPackage = TIMEOUT_15;		// satuan detik
		bool fExitThread = true;
		private Thread MyTimeOut;
		private Thread MyTimeOutPackage;

		ControlCenter PPOBProcessor;

		PublicSettings.Settings CommonConfigs;
		PPOBDatabase.PPOBdbLibs localDB;

		//string configFilePath = "";

		public PhHttpHandler (int indexConnection, string ConfigFilePath)
		{
			//configFilePath = ConfigFilePath;
			//commonSettings = CommonConfigs;
			indexConn = indexConnection;
			srecBuff = "";
			dataLength = 0;
			HTTPRestDataConstruct = new HTTPRestConstructor();
			PPOBProcessor = new ControlCenter();
			CommonConfigs = new PublicSettings.Settings ();


			LoadConfig (ConfigFilePath);
		}

		private void LoadConfig(string configFile){
			using (CrossIniFile.INIFile a = new CrossIniFile.INIFile (configFile)) {
				CommonConfigs.DbHost = a.GetValue("PostgreDB", "Host", "127.0.0.1");
				CommonConfigs.DbUser = a.GetValue("PostgreDB", "Username", "postgres");
				CommonConfigs.DbPort = a.GetValue("PostgreDB", "Port", 5432);
				CommonConfigs.DbPassw = a.GetValue("PostgreDB", "Password", "");
				CommonConfigs.DbName = a.GetValue("PostgreDB", "DBName", "");
				localDB = new PPOBDatabase.PPOBdbLibs(CommonConfigs.DbHost, CommonConfigs.DbPort,
					CommonConfigs.DbName, CommonConfigs.DbUser, CommonConfigs.DbPassw);

				CommonConfigs.localDb = localDB;
				CommonConfigs.ReloadSettings();
				if (CommonConfigs.SettingCollection == null) return;

//				if (!System.IO.Directory.Exists(CommonConfigs.getString("LogPath"))) 
//					System.IO.Directory.CreateDirectory(CommonConfigs.getString("LogPath"));
//				LOG_Handler.LogWriter.setPath(CommonConfigs.getString("LogPath"));

				CommonLibrary.SessionMinutesTimeout = CommonConfigs.getInt("SessionMinutesTimeout");
			}

		}

		/// <summary>
		/// Start the specified stream and client. Secure or non secure compatible.
		/// </summary>
		/// <param name="stream">Stream or SslStream.</param>
		/// <param name="client">TcpClient.</param>
		public void Start(Stream stream, TcpClient client){
//
//			Console.WriteLine ("["+this.ToString () + "] START Http Handler");
//
			dataHandler.Start (stream, client);
			dataHandler.onDataReceived += new MyTcpStreamHandler.onReceived (DataReceived);
			dataHandler.onDisconnected += new MyTcpStreamHandler.onDisconnectedEventArgs (onConnectionDisconnected);

			fExitThread = false;		// ditambah didieu
			ctrTO = TIMEOUT_15;		    // initial timeout 15 detik, untuk data pertama
			MyTimeOut = new Thread(new ThreadStart(ConnTimeOut));
			MyTimeOut.Start();
		}

//		public void StartSecure(SslStream stream, TcpClient client){
//
//			Console.WriteLine ("["+this.ToString () + "] START : SECURE");
//
//			dataHandler.Start (stream, client);
//			dataHandler.onDataReceived += new MyTcpStreamHandler.onReceived (DataReceived);
//			dataHandler.onDisconnected += new MyTcpStreamHandler.onDisconnectedEventArgs (onConnectionDisconnected);
//
//			fExitThread = false;		// ditambah didieu
//			ctrTO = TIMEOUT_15;		    // initial timeout 15 detik, untuk data pertama
//			MyTimeOut = new Thread(new ThreadStart(ConnTimeOut));
//			MyTimeOut.Start();
//		}

		bool eventOnce = false;
		private void onConnectionDisconnected(){
			if (!eventOnce) {
				eventOnce = true;
				if (onDisconnected != null)
					onDisconnected (indexConn);
			}
		}

		private void Disconnect(MyTcpStreamHandler.ConnectionStateObject State){
			if (dataHandler != null) {
				dataHandler.Disconnect (State);
				onConnectionDisconnected();
			}
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


		HTTPRestConstructor HTTPRestDataConstruct;
		string srecBuff = "";
		const int MAXRecBuff = 100 * 1024;  // max 100Kb
		//        byte[] dataBuffer = new byte[2048];
		int dataLength = 0;
		HTTPRestConstructor.retParseCode retCode = HTTPRestConstructor.retParseCode.Uncompleted;

		private void DataReceived(MyTcpStreamHandler.ConnectionStateObject State){
			ctrTO = TIMEOUT_15;		// reset timeout untuk penerimaan berikutnya
			// cek data http, jika lengkap set fExitThread = true supaya thread timout keluar

			//fExitThread = true;

			// Validasi Http protocol

			//srecBuff += Encoding.GetEncoding(1252).GetString(data);
			srecBuff += State.sb.ToString ();
			dataLength += State.DataLength;

//			Console.WriteLine ("=======================TERIMAAAA========================");
//			Console.WriteLine ("State Secure : " + State.isSecureConnection.ToString ());
//			Console.WriteLine ("Data diterima : " + State.sb.ToString ());
//			Console.WriteLine ("Data srecBuff : " + srecBuff);
//			Console.WriteLine ("========================AKHIRRR=========================");

			HTTPRestDataConstruct.parseClientRequest(srecBuff,
				(((IPEndPoint)State.client.Client.RemoteEndPoint).Address.ToString()), ref retCode);

			//Console.WriteLine("Return Code : " + retCode.ToString());
			//Console.WriteLine("Canonical : " + HTTPRestDataConstruct.HttpRestClientRequest.CanonicalPath);
			//Console.WriteLine("Method : " + HTTPRestDataConstruct.HttpRestClientRequest.Method);
			//Console.WriteLine("Host : " + HTTPRestDataConstruct.HttpRestClientRequest.Host);
			//Console.WriteLine("ContentType : " + HTTPRestDataConstruct.HttpRestClientRequest.ContentType);
			//Console.WriteLine("ContentLen : " + HTTPRestDataConstruct.HttpRestClientRequest.ContentLen);
			//Console.WriteLine("Date : " + HTTPRestDataConstruct.HttpRestClientRequest.Date);
			//Console.WriteLine("Body : " + HTTPRestDataConstruct.HttpRestClientRequest.Body);

			LogWriter.show (this, "==== RECEIVED FROM CLIENT :\r\n" +
				"Return Code : " + retCode.ToString () + "\r\n" +
				"Canonical : " + HTTPRestDataConstruct.HttpRestClientRequest.CanonicalPath + "\r\n" +
				"Body : " + HTTPRestDataConstruct.HttpRestClientRequest.Body + "\r\n" +
				"FULL : \r\n" + srecBuff);


			switch (retCode)
			{
			case HTTPRestConstructor.retParseCode.Invalid:
				srecBuff = "";
				dataLength = 0;
				Disconnect(State);
				break;
			case HTTPRestConstructor.retParseCode.Uncompleted:
				if (srecBuff.Length > MAXRecBuff)
				{
					srecBuff = "";
					dataLength = 0;
					Disconnect(State);
				}
				return;
			case HTTPRestConstructor.retParseCode.Completed:
				fExitThread = true;
				ctrTO = TIMEOUT_60;
				ctrTOPackage = TIMEOUT_60;
				ProcessDataReceived(State);
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

		private void ProcessDataReceived(MyTcpStreamHandler.ConnectionStateObject State)
		{

			//string resp = PPOBProcessor.messageProcessor(HTTPRestDataConstruct.HttpRestClientRequest, 
			//    logPath,dbHost,dbPort, dbUser,dbPass,dbName,httpRestServicePath,httpRestServiceAccountPath,
			//    httpRestServiceProductTransactionPath, httpRestServiceApplicationsPath, sandraHost,sandraPort);
			string resp = PPOBProcessor.messageProcessor(HTTPRestDataConstruct.HttpRestClientRequest,
				CommonConfigs);
			try
			{
				LogWriter.show(this, "SEND TO CLIENT: " + resp);
				if (resp.Length != 0) {
					if(dataHandler!=null){
						dataHandler.SendResponse (State,resp);
					}
				}
			}
			catch (Exception ex)
			{
				// disini reply ke client tidak bisa diterima client
				LogWriter.show(this, "ERROR: " + ex.getCompleteErrMsg());
			}
			Disconnect(State);

			// reply ke Client dengan acknowledge OOKK+13
			//intSent = client.Send(Encoding.GetEncoding(1252).GetBytes("OOKK\r"));
		}

	}
}

