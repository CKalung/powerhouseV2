
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
using PHClientProtocolTranslatorInterface;

namespace PHClientHttpHandler
{
	public class PhClientHandlerTemplateAwal : IDisposable {

		#region Disposable
		private bool disposed = false;
		public virtual void Dispose()
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
		~PhClientHandlerTemplateAwal()
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
		protected const int TIMEOUT_07 = 7;
		protected const int TIMEOUT_10 = 10;
		protected const int TIMEOUT_15 = 15;
		protected const int TIMEOUT_60 = 60;
		protected int ctrTO = TIMEOUT_10;		// satuan detik
		protected int ctrTOPackage = TIMEOUT_15;		// satuan detik
		protected bool fExitThread = true;
		private Thread MyTimeOut;
		private Thread MyTimeOutPackage;

		protected ControlCenter PPOBProcessor;

		protected PublicSettings.Settings CommonConfigs;
		protected PPOBDatabase.PPOBdbLibs localDB;

		IPhClientTranslator phTranslator;

		string configFilePath = "";

		public PhClientHandlerTemplateAwal ()
		{
			//configFilePath = ConfigFilePath;
			//commonSettings = CommonConfigs;
			PPOBProcessor = new ControlCenter();
			CommonConfigs = new PublicSettings.Settings ();
		}

		public int IndexConnection { get { return indexConn; } set { indexConn = value; } }
		public string ConfigFilePath { 
			get { return configFilePath; } 
			set { 
				configFilePath = value; 
				LoadConfig (configFilePath);
			} 
		}

		public IPhClientTranslator Translator{
			get { return phTranslator; }
			set { phTranslator = value; }
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

		protected void Disconnect(MyTcpStreamHandler.ConnectionStateObject State){
			if (dataHandler != null) {
				dataHandler.Disconnect (State);
				onConnectionDisconnected();
			}
		}

		protected void CloseTimeOutThread()
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

		// abstract tuh HARUS di override, virtual boleh tidak
		public virtual void DataReceived(MyTcpStreamHandler.ConnectionStateObject State){
		}


	}
}

