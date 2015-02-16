using System;
using System.Collections.Generic;
using System.Text;
//using System.Threading.Tasks;
using System.Reflection;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using PPOBClientHandler;
using LOG_Handler;
using StaticCommonLibrary;
//using NitrogenClientHandler;

namespace PPOBClientsManager
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
            //MAX_CLIENT = TotClients;
			//NitrogenOfflineProcessor = new NitroClient(CommonConfigs);
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

                //Hashtable config = localDB.getAppConfigurations();
                //if (config == null) return false;
                CommonConfigs.localDb = localDB;
                //CommonConfigs.SettingCollection = localDB.getAppConfigurations();
                CommonConfigs.ReloadSettings();
                //CommonConfigs.SettingCollection.Add("ConsoleMode", fConsoleMode);
                if (CommonConfigs.SettingCollection == null) return false;

                //LogWriter.show(this,"Setting nama agen: " + CommonConfigs.getString("Citilink_AgenName") + "\r\n" +
                //    "Setting password agen: " + CommonConfigs.getString("Citilink_AgenPassword") + "\r\n" +
                //    "Setting domain code: " + CommonConfigs.getString("Citilink_DomainCode")
                //    );

                if (!System.IO.Directory.Exists(CommonConfigs.getString("LogPath"))) 
                    System.IO.Directory.CreateDirectory(CommonConfigs.getString("LogPath"));
                LOG_Handler.LogWriter.setPath(CommonConfigs.getString("LogPath"));
                return true;

                //try
                //{
                //    //listeningPort = a.GetValue("TCPServer", "PORT", "10039");

                //    CommonConfigs.ServerListenPort = int.Parse(((string)config["ServerListenPort"]).Trim());
                //    CommonConfigs.getString("LogPath") = ((string)config["LogPath"]).Trim();
                //    CommonConfigs.MAX_Client = int.Parse(((string)config["MAX_Client"]).Trim());

                //    CommonConfigs.httpRestServicePath = ((string)config["CanonicalPath"]).Trim();
                //    CommonConfigs.httpRestServiceAccountPath = ((string)config["httpRestServiceAccountPath"]).Trim();
                //    CommonConfigs.httpRestServiceProductTransactionPath = ((string)config["httpRestServiceProductTransactionPath"]).Trim();
                //    CommonConfigs.httpRestServiceApplicationsPath = ((string)config["httpRestServiceApplicationsPath"]).Trim();

                //    CommonConfigs.SandraHost = ((string)config["SandraHost"]).Trim();
                //    CommonConfigs.SandraPort = int.Parse(((string)config["SandraPort"]).Trim());

                //    CommonConfigs.QVA_PENAMPUNG_KREDIT = ((string)config["QVA_PENAMPUNG_KREDIT"]).Trim();
                //    CommonConfigs.QVA_PENAMPUNG_DEBIT = ((string)config["QVA_PENAMPUNG_DEBIT"]).Trim();
                //    CommonConfigs.QVA_ESCROW_DAM_KREDIT = ((string)config["QVA_ESCROW_DAM_KREDIT"]).Trim();
                //    CommonConfigs.QVA_ESCROW_QNB_KREDIT = ((string)config["QVA_ESCROW_QNB_KREDIT"]).Trim();
                //    CommonConfigs.QVA_ESCROW_FINNET_KREDIT = ((string)config["QVA_ESCROW_FINNET_KREDIT"]).Trim();

                //    CommonConfigs.UserIdHeader = ((string)config["UserIdHeader"]).Trim();

                //    CommonConfigs.CommandProductTransaction = int.Parse(((string)config["CommandProductTransaction"]).Trim());

                //    CommonConfigs.CommandAccountActivateFirstLine = int.Parse(((string)config["CommandAccountActivateFirstLine"]).Trim());
                //    CommonConfigs.CommandAccountHapusUmum = int.Parse(((string)config["CommandAccountHapusUmum"]).Trim());
                //    CommonConfigs.CommandAccountRegistration = int.Parse(((string)config["CommandAccountRegistration"]).Trim());
                //    CommonConfigs.CommandAccountUpdate = int.Parse(((string)config["CommandAccountUpdate"]).Trim());
                //    CommonConfigs.CommandAccountInquiry = int.Parse(((string)config["CommandAccountInquiry"]).Trim());
                //    CommonConfigs.CommandAccountActivation = int.Parse(((string)config["CommandAccountActivation"]).Trim());

                //    CommonConfigs.CommandAccountTransfer = int.Parse(((string)config["CommandAccountTransfer"]).Trim());
                //    CommonConfigs.CommandAccountCashInRequest = int.Parse(((string)config["CommandAccountCashInRequest"]).Trim());
                //    CommonConfigs.CommandAccountCashOutRequest = int.Parse(((string)config["CommandAccountCashOutRequest"]).Trim());

                //    CommonConfigs.CommandAccountCashInApproval = int.Parse(((string)config["CommandAccountCashInApproval"]).Trim());
                //    CommonConfigs.CommandAccountCashOutApproval = int.Parse(((string)config["CommandAccountCashOutApproval"]).Trim());

                //    CommonConfigs.CashInProductCode = ((string)config["CashInProductCode"]).Trim();
                //    CommonConfigs.CashOutProductCode = ((string)config["CashOutProductCode"]).Trim();

                //    CommonConfigs.CommandApplicationLogin = int.Parse(((string)config["CommandApplicationLogin"]).Trim());
                //    CommonConfigs.CommandApplicationHeartBeat = int.Parse(((string)config["CommandApplicationHeartBeat"]).Trim());
                //    CommonConfigs.CommandChangeUserPassword = int.Parse(((string)config["CommandChangeUserPassword"]).Trim());
                //    CommonConfigs.CommandResetUserPassword = int.Parse(((string)config["CommandResetUserPassword"]).Trim());
                    
                //    CommonConfigs.FinnetQueueHost = ((string)config["FinnetQueueHost"]).Trim();
                //    CommonConfigs.FinnetQueuePort = int.Parse(((string)config["FinnetQueuePort"]).Trim());

                //    CommonConfigs.FmQueueHost = ((string)config["FmQueueHost"]).Trim();
                //    CommonConfigs.FmQueuePort = int.Parse(((string)config["FmQueuePort"]).Trim());

                //    CommonConfigs.CommonTerminalID = ((string)config["CommonTerminalID"]).Trim();
                //    CommonConfigs.CommonMerchantID = ((string)config["CommonMerchantID"]).Trim();

                //    CommonConfigs.CommandAccountInvoice = int.Parse(((string)config["CommandAccountInvoice"]).Trim());
                //    CommonConfigs.CommandAccountInvoiceApproval = int.Parse(((string)config["CommandAccountInvoiceApproval"]).Trim());

                //    CommonConfigs.BillerPersadaTimeOut = int.Parse(((string)config["BillerPersadaTimeOut"]).Trim());
                //    CommonConfigs.BillerFinnetTimeOut = int.Parse(((string)config["BillerFinnetTimeOut"]).Trim());
                //    CommonConfigs.BillerFmTimeOut = int.Parse(((string)config["BillerFmTimeOut"]).Trim());


                //    //CommonConfigs.InvoiceProductCode = ((string)config["InvoiceProductCode"]).Trim();

                //    //                    FinnetKeeper = new FinnetConnectionKeeper.Keeper(CommonConfigs);

                //    if (!System.IO.Directory.Exists(CommonConfigs.getString("LogPath"))) System.IO.Directory.CreateDirectory(CommonConfigs.getString("LogPath"));
                //    LOG_Handler.LogWriter.setPath(CommonConfigs.getString("LogPath"));
                //    return true;
                //}
                //catch
                //{
                //    return false;
                //}
			}
        }

        public int getFreeClientSlot()
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
                        //CommonConfigs.SettingCollection.Clear();
                        //CommonConfigs.ReloadSettings();
                        //if (CommonConfigs.SettingCollection == null) return false;
                    }
                    else
                    {
                        //Console.WriteLine("KONEKSI BARU GAGAL : 1 CLOSE SERVER");
                        //break;   // closing server
                        continue;
                    }
                }
				catch
				//catch(Exception exErr)
                {
                    // server stopped 
                    //break;
                    //Console.WriteLine("KONEKSI BARU GAGAL : " + exErr.getCompleteErrMsg());
                    //LogWriter.write(this, LogWriter.logCodeEnum.ERROR, exErr.getCompleteErrMsg());
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
                //Console.WriteLine("USED Class : " + usedClass().ToString());
                //Console.WriteLine("UNUSED Class : " + unusedClass().ToString());
                ClientList[i].ApplicationPath = appPath;
                ClientList[i].LogPath = CommonConfigs.getString("LogPath");
                //Console.WriteLine("Hit Enter to quit!!");
                //ConsoleKeyInfo mykey = Console.ReadKey();
                //if (mykey.Key == ConsoleKey.Enter)
                //{
                //    myList.Stop();
                //}
            }

        }

        //System.Threading.Thread Lstn;
        //System.Threading.ThreadStart thStart;
        public bool onStart()
        {
            if(!loadConfig()) return false;
            // create clienthandlers
            for (int i = 0; i < CommonConfigs.getInt("MAX_Client"); i++)
            {
                //ClientList.Add(new ClientTesterHandler());
                ClientList.Add(new Client_Handler());
			}
            LogWriter.show(this, "Product Service is listening at port: " + CommonConfigs.getString("ServerListenPort"));
            TcpListen = new TcpListener(IPAddress.Parse("0.0.0.0"), CommonConfigs.getInt("ServerListenPort"));
            fServerListen = true;
            System.Threading.Thread Lstn = new System.Threading.Thread(new System.Threading.ThreadStart(_listener_thread));
            //thStart = new System.Threading.ThreadStart(_listener_thread);
            //Lstn = new System.Threading.Thread(thStart);
            //Console.WriteLine("AKTIFKAN THREAD");
            Lstn.Start();

			//NitrogenOfflineProcessor.Start();
            //FinnetKeeper.Start();

            return true;
        }

        public void onStop()
        {
            fServerListen = false;
            TcpListen.Stop();

			//NitrogenOfflineProcessor.Stop();

            //FinnetKeeper.Stop();
            
            // close semua clients
            // Stop properly all of clients here
            LogWriter.show(this, "Stopping all connected clients...");
            //foreach (ClientTesterHandler ch in ClientList) ch.Dispose();
            foreach (Client_Handler ch in ClientList) ch.Dispose();
            LogWriter.show(this, "Stopping clients... Done");
            // tunggu sampe semua client closed, baru keluar procedure

            localDB.Dispose();
        }

    }
}
