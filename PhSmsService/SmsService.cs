using System;
using System.Collections.Generic;
using WavecommSmsEngine;
using System.Threading;
using System.Security.Cryptography;
using System.Collections;

namespace PH_SmsService
{
	public class SmsService:IDisposable
	{
		#region IDisposable Members
		private bool disposed = false;
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if(!this.disposed)
			{
				if(disposing)
				{
					// Dispose managed resources.
					disposeAll();
				}

				disposed = true;
			}
		}

		~SmsService()
		{
			Dispose(false);
		}

		#endregion

		private void disposeAll() 
		{
			localDB.Dispose();
			isStopThread=true;
		}


		WavecommSms sms = new WavecommSms();

		PostGresDB localDB;
		string tbl_phSms = "ph_tblSms"; // sementara gini dulu
		Thread SmsDbScanThread=null;
		bool isStopThread=true;
		string appPath;
		Hashtable CommonConfigs=null;

		public struct anSmsToSendStruct
		{
			public int id;
			public string Receiver;
			public string Message;
		}

		public SmsService (string AppPath, bool consoleMode)
		{
			LogWriter.ConsoleMode = consoleMode;
			appPath = AppPath;

			sms.onDataReceived +=	new WavecommSms.SmsReceivedEventHandler (SmsReceived);
			sms.onCekPulsa += new WavecommSms.CekPulsaEventHandler (CekPulsaReceived);

		}

		public string getString(string key)
		{
			if (!CommonConfigs.ContainsKey(key)) return "";
			try
			{
				return ((string)CommonConfigs[key]).Trim();
			}
			catch { return ""; }
		}

		public bool loadConfig()
		{
//            appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			using (CrossIniFile.INIFile a = new CrossIniFile.INIFile (appPath + "/config.ini")) {
				string DbHost; int DbPort; 
				string DbName; string DbUser; string DbPassw;

				DbHost = a.GetValue ("PostgreDB", "Host", "127.0.0.1");
				DbUser = a.GetValue ("PostgreDB", "Username", "postgres");
				DbPort = a.GetValue ("PostgreDB", "Port", 5432);
				DbPassw = a.GetValue ("PostgreDB", "Password", "");
				DbName = a.GetValue ("PostgreDB", "DBName", "");
				localDB = new PostGresDB();
				localDB.ConnectionString(DbHost, DbPort, DbName, DbUser, DbPassw);
				localDB.AddDataTable(tbl_phSms);

				CommonConfigs = getAppConfigurations ();
				if(CommonConfigs == null)
					return false;
				return true;
			}
		}

		public bool Start()
		{
			bool hasil = false;

			loadConfig();
			// load logPath dari database
			string logPath = getString ("LogPath");
//			if (!getLogpath (ref logPath, out ExError)) {
//				return false;
//			}
			LogWriter.setPath (logPath);

			// load seting port dari database
			string smsPort = getString ("SMS_MODEM_PORT");
			string ModemImei  = getString ("SMS_MODEM_IMEI");
//			if (!getSmsModemPort (ref ModemImei, ref smsPort, out ExError)) {
//				return false;
//			}

			if(ModemImei!=""){
				LogWriter.show (this,"Autofind modem dengan IMEI: " + ModemImei);
				hasil = sms.StartWithImei (ModemImei);		// Start modem yg menggunakan Imei tertentu saja
			}else{
				LogWriter.show (this,"Port " + smsPort);
				hasil = sms.Start (smsPort); //("/dev/ttyUSB0");
			}
			//LogWriter.show (this, "Hasil = " + hasil.ToString ());
			if (!hasil)
				return false;

			string KodeCekPulsa = getString ("Kode_CekPulsa");
			int PeriodeCekPulsa = int.Parse (getString ("Periode_CekPulsa"));
			sms.SetCekPulsa (KodeCekPulsa, PeriodeCekPulsa);

			// start thread untuk pengiriman sms dari database
			isStopThread = false;
			SmsDbScanThread = new Thread(new ThreadStart(ScanDbForSendSms));
			SmsDbScanThread.Start();
			return true;
		}

		public void Stop()
		{
			isStopThread = true;
			sms.Stop ();
		}

		public Hashtable getAppConfigurations()
		{
			Exception ExError;
			string sql = "SELECT * FROM configuration";
			ExError = null;
			int i = localDB.ExecQuerySql(sql, tbl_phSms, out ExError);
			if (ExError != null) return null;
			if (i <= 0) return null;
			Hashtable hasil = new Hashtable();
			for (int j = 0; j < i; j++)
			{
				hasil.Add(localDB.GetDataSet.Tables[tbl_phSms].Rows[j]["name"],
					localDB.GetDataSet.Tables[tbl_phSms].Rows[j]["value"]);
			}
			return hasil;
		}

		public bool getSmsModemPort(ref string ModemIMEI, ref string ModemPort, out Exception ExError)
		{
			ExError = null;
			ModemPort = "";
			ModemIMEI = "";
			//string sql = "select user_id from mp_account where phone = '" + phone + "'";
			//Console.WriteLine(sql);
			string sql = "SELECT value FROM configuration WHERE name = 'SMS_MODEM_PORT'";
			int i = localDB.ExecQuerySql(sql, tbl_phSms, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message);
				return false;
			}
			ModemPort = ((string)(localDB.GetDataItem (tbl_phSms, 0, "value"))).Trim ();
			return (i > 0);
		}

		public bool getLogpath(ref string LogPath, out Exception ExError)
		{
			ExError = null;
			LogPath = "";
			string sql = "SELECT value FROM configuration WHERE name = 'LogPath'";
			int i = localDB.ExecQuerySql(sql, tbl_phSms, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message);
				return false;
			}
			LogPath = ((string)(localDB.GetDataItem (tbl_phSms, 0, "value"))).Trim ();
			return (i > 0);
		}

		public bool getSmsToSend(ref List<anSmsToSendStruct> listSms, out Exception ExError)
		{
			ExError = null;
//			string sql = "SELECT id, applicant, request_time, destination_number, message FROM sms_send_request " +
//			             "WHERE is_sent = true ORDER BY id ASC LIMIT 10";
			string sql = "SELECT id, destination_number, message FROM sms_send_request " +
			             "WHERE is_sent = false ORDER BY id ASC LIMIT 10";

			//			informasi lainnya untuk keperluan move data....GAK PERLU MOVE, PAKE CEKLIST BadImageFormatException LAH, NGARAH GAMPANG STAThreadAttribute TABLE
			int i = localDB.ExecQuerySql(sql, tbl_phSms, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message);
				return false;
			}
			for (int j = 0; j < i; j++) {
				anSmsToSendStruct anSms = new anSmsToSendStruct ();
				anSms.id = int.Parse(localDB.GetDataItem (tbl_phSms, j, "id").ToString());
				anSms.Receiver = ((string)(localDB.GetDataItem (tbl_phSms, j, "destination_number"))).Trim ();
				anSms.Message = ((string)(localDB.GetDataItem (tbl_phSms, j, "message"))).Trim ();

				listSms.Add (anSms);
			}
			return true;
		}

		private void SmsReceived(List<WavecommSms.anSmsStruct> Messages)
		{
			Exception exError = null;
			Console.WriteLine ("TERIMA SMS");
			//Messages [0].Message;
			foreach (WavecommSms.anSmsStruct anSms in Messages) {
				LogWriter.show (this, "\r\nWaktu Terima: " + anSms.RecTime + "\r\n" +
					"Pengirim: "+anSms.Sender + "\r\n" +
					"Pesannya: "+anSms.Message + "\r\n" +
					"Hashnya: "+anSms.SmsHash);
				InsertSmsReceived (anSms.RecTime, anSms.Sender, anSms.Message, anSms.SmsHash, out exError);
			}
		}

		private void CekPulsaReceived(DateTime waktu, string msgCekPulsa)
		{
			LogWriter.show (this, "Pesan Cek Pulsa : \r\n" + 
				waktu.ToString("yyyy-MM-dd HH:mm:ss") + ": " + 
				msgCekPulsa);
			// Ke asupkeun database
			Exception exErr = null;
			InsertMsgCekPulsa (waktu, msgCekPulsa, out exErr);
		}

		private bool InsertMsgCekPulsa(DateTime waktu, string message
			, out Exception ExError)
		{
			ExError = null;
			string sql = "INSERT INTO sms_cekpulsa (check_time, message) values (";
			sql += "'" + waktu.ToString("yyyy-MM-dd HH:mm:ss") + "','" + message + "')";

			int i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				return false;
			}
			return (i > 0);
		}

		private bool InsertSmsSendRequest(string sender, string destination_number, string message
			, out Exception ExError)
		{
			ExError = null;
			string sql = "INSERT INTO sms_send_request (applicant, destination_number, message) values (";
			sql += "'" + sender + "','" + destination_number + "','" + message + "')";

			int i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				return false;
			}
			return (i > 0);
		}

		private bool UpdateSmsSent(int id, out Exception ExError)
		{
			ExError = null;
			string sql = "UPDATE sms_send_request SET is_sent = true, sent_time = NOW() WHERE id = ";
			sql += id.ToString();

			//LogWriter.show (this,"\"" + sql + "\"");
			int i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				LogWriter.show (this, "Gagal update sent sms" + ExError.StackTrace);
				return false;
			}
			return (i > 0);
		}

		private bool InsertSmsReceived(DateTime received_time, string sender, string message, string smshash
			, out Exception ExError)
		{
			ExError = null;
			string sqls = "SELECT id FROM sms_received " +
			              "WHERE smshash = '" + smshash.Trim () + "'";

			//			informasi lainnya untuk keperluan move data....GAK PERLU MOVE, PAKE CEKLIST BadImageFormatException LAH, NGARAH GAMPANG STAThreadAttribute TABLE
			int i = localDB.ExecQuerySql(sqls, tbl_phSms, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sqls +
					"\r\nResult: " + ExError.Message);
				return false;
			}
			if (i > 0)
				return true;		// jika sudah ada, gak usah insert lagi
			string sql = "INSERT INTO sms_received (received_time, sender, message, smshash) values (";
			sql += "'" + received_time.ToString("yyyy-MM-dd HH:mm:ss") + "','" + sender + "','" + 
			       message + "','" + smshash + "')";

			i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				return false;
			}
			return (i > 0);
		}

		private void ScanDbForSendSms()
		{
			int kCtr = 2 * 3;	// 3 detik
			int ctr = kCtr;
			List<anSmsToSendStruct> listSms = new List<anSmsToSendStruct> ();
			Exception ExError = null;
			listSms.Clear ();
			while (!isStopThread) {
				Thread.Sleep (500);
				if (isStopThread)
					break;
				ctr--;
				if (ctr <= 0) {
					ctr = kCtr;
					//Console.WriteLine ("Sms ready:" + sms.isReadyToSend);
					if (!sms.isReadyToSend)
						continue;
					listSms.Clear ();
					if (!getSmsToSend (ref listSms, out ExError))
						continue;
					LogWriter.show(this,"Sms to send:" + listSms.Count.ToString());
					foreach (anSmsToSendStruct anSms in listSms) {
						if (!sms.SendSMS (anSms.Receiver, anSms.Message)) {
							LogWriter.write (this, LogWriter.logCodeEnum.ERROR,"GAGAL Kirim SMS");
							break;
						}
						LogWriter.show (this,"BERHASIL Kirim SMS: \r\n" + 
							"idx : " + anSms.id.ToString() + "\r\n" + 
							"receiver : " + anSms.Receiver + "\r\n" + 
							"message : " + anSms.Message
						);
						UpdateSmsSent (anSms.id, out ExError);
					}

				}
			}
		}
	}
}

