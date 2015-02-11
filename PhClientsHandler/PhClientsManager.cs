using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using PPOBClientHandler;
using LOG_Handler;
using StaticCommonLibrary;
//using NitrogenClientHandler;

namespace PhClientsManager
{
	public class Clients_Manager : IDisposable
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
		~Clients_Manager()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			//            FinnetKeeper.Dispose();
			//NitrogenOfflineProcessor.Dispose();
		}

		//int MAX_CLIENT = 200;

		List<Client_Handler> ClientList = new List<Client_Handler>();
		//List<ClientTesterHandler> ClientList = new List<ClientTesterHandler>();

		TcpListener TcpListen;
		//        FinnetConnectionKeeper.Keeper FinnetKeeper;

		bool fServerListen = false;

		PublicSettings.Settings CommonConfigs = new PublicSettings.Settings();
		//PublicSettings.Settings CommonConfigs = new PublicSettings.Settings();
		PPOBDatabase.PPOBdbLibs localDB;

		//NitroClient NitrogenOfflineProcessor;

		string appPath;

		public Clients_Manager(string AppPath, bool consoleMode)
		{
			LogWriter.ConsoleMode = consoleMode;
			appPath = AppPath;
			loadConfig();
			CommonLibrary.SessionMinutesTimeout = CommonConfigs.getInt("SessionMinutesTimeout");
		}

		public bool loadConfig()
		{
			//			string name = System.Configuration.con .ConfigurationManager. .AppSettings["OperatorName"];
			//			Console.WriteLine("Welcome " + name);
			//			string level = System.Configuration.ConfigurationManager.AppSettings["LoggerLevel"];
			//
			//			Console.WriteLine("Logger level: " + level);

			//            appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			using (CrossIniFile.INIFile a = new CrossIniFile.INIFile (appPath + "/config.ini")) {
				CommonConfigs.DbHost = a.GetValue("PostgreDB", "Host", "127.0.0.1");
				CommonConfigs.DbUser = a.GetValue("PostgreDB", "Username", "postgres");
				CommonConfigs.DbPort = a.GetValue("PostgreDB", "Port", 5432);
				CommonConfigs.DbPassw = a.GetValue("PostgreDB", "Password", "");
				CommonConfigs.DbName = a.GetValue("PostgreDB", "DBName", "");
				localDB = new PPOBDatabase.PPOBdbLibs(CommonConfigs.DbHost, CommonConfigs.DbPort,
					CommonConfigs.DbName, CommonConfigs.DbUser, CommonConfigs.DbPassw);

				CommonConfigs.localDb = localDB;
				CommonConfigs.ReloadSettings();
				if (CommonConfigs.SettingCollection == null) return false;

				if (!System.IO.Directory.Exists(CommonConfigs.getString("LogPath"))) 
					System.IO.Directory.CreateDirectory(CommonConfigs.getString("LogPath"));
				LOG_Handler.LogWriter.setPath(CommonConfigs.getString("LogPath"));
				return true;
			}
		}

		private int getFreeClientSlot()
		{
			int i = 0;
			try
			{
				for (i = 0; i < CommonConfigs.getInt("MAX_Client"); i++)
				{
					if (ClientList[i].isAvailable) return i;
				}
			}
			catch { }
			return -1;
		}

		void _listener_thread()
		{
			int j = 0;
			TcpListen.Start();
			string endPoint = "";
			string connTm = "";
			while (fServerListen)
			{
				int i = getFreeClientSlot();
				if (i < 0)
				{
					// not listening if the slot is empty;
					System.Threading.Thread.Sleep(200);
					continue;
				}
				j++;
				try 
				{
					// TODO : KUDU AYA PENGECEKAN WAKTU CLOSING UNTUK TUTUP SERVICE
					Socket s = TcpListen.AcceptSocket();
					if (s != null)
					{
						ClientList[i].setParams(s, CommonConfigs);         // New Connection accepted;
					}
					else
					{
						continue;
					}
				}
				catch(Exception exErr)
				{
					continue;
				}
				ClientList[i].SessionID = j;
				ClientList[i].ConnectionTime = DateTime.Now;
				try
				{
					endPoint = ClientList[i].Socket.RemoteEndPoint.ToString();
				}
				catch { endPoint = "Already disconnected"; }
				try
				{
					connTm = ClientList[i].ConnectionTime.ToString("dd-MM-yyyy HH:mm:ss");
				}
				catch { connTm = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");}
				string sDbg = ", Connection Time : " + connTm + ", " + "Class Index : " + i.ToString();
				LogWriter.show(this, "Connection accepted from " + endPoint +
					", Current Session ID :" + j + sDbg);

				ClientList[i].ApplicationPath = appPath;
				ClientList[i].LogPath = CommonConfigs.getString("LogPath");
			}

		}

		public bool onStart()
		{
			if(!loadConfig()) return false;
			// create clienthandlers
			for (int i = 0; i < CommonConfigs.getInt("MAX_Client"); i++)
			{
				ClientList.Add(new Client_Handler());
			}
			LogWriter.show(this, "Product Service is listening at port: " + CommonConfigs.getString("ServerListenPort"));
			TcpListen = new TcpListener(IPAddress.Parse("0.0.0.0"), CommonConfigs.getInt("ServerListenPort"));
			fServerListen = true;
			System.Threading.Thread Lstn = new System.Threading.Thread(new System.Threading.ThreadStart(_listener_thread));
			Lstn.Start();

			//NitrogenOfflineProcessor.Start();
			//FinnetKeeper.Start();

			return true;
		}

		public void onStop()
		{
			fServerListen = false;
			TcpListen.Stop();

			// close semua clients
			// Stop properly all of clients here
			LogWriter.show(this, "Stopping all connected clients...");
			foreach (Client_Handler ch in ClientList) ch.Dispose();
			LogWriter.show(this, "Stopping clients... Done");
			// tunggu sampe semua client closed, baru keluar procedure

			localDB.Dispose();
		}

	}
}
