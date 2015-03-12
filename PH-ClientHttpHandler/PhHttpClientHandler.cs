using System;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel.Composition;

using LOG_Handler;
using PPOBManager;
using PPOBHttpRestData;
using PHClientHandlerInterface;
using StaticCommonLibrary;
using MyTcpClientServerV2;

namespace PHClientHttpProtocolHandler
{
	[Export(typeof(IPhClientHandler))]
	public class PhHttpClientHandler : BaseClientHandlers 
	{
		#region Disposable
		private bool disposed;
		public override void Dispose()
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
		~PhHttpClientHandler()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			HTTPRestDataConstruct.Dispose ();
			base.Dispose ();
		}

		PublicSettings.Settings CommonConfigs;

		public PhHttpClientHandler (int indexConnection, string configFilePath)
			:base(indexConnection, configFilePath)
		{
			HTTPRestDataConstruct = new HTTPRestConstructor();
			CommonConfigs = new PublicSettings.Settings ();
			LoadConfig (configFilePath);
		}

		private void LoadConfig(string configFile){
			PPOBDatabase.PPOBdbLibs localDB;
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

//		string srecBuff = "";
//		const int MAXRecBuff = 100 * 1024;  // max 100Kb
//		//        byte[] dataBuffer = new byte[2048];
//		int dataLength = 0;
		HTTPRestConstructor HTTPRestDataConstruct;
		HTTPRestConstructor.retParseCode retCode = HTTPRestConstructor.retParseCode.Uncompleted;

		public void DataReceivedOverrideAsal(MyTcpStreamHandler.ConnectionStateObject State){
			TimeOutSecondsSetting = TIMEOUT_15;		// reset timeout untuk penerimaan berikutnya

			// Validasi Http protocol

			//srecBuff += Encoding.GetEncoding(1252).GetString(data);
			srecBuff += State.sb.ToString ();
			dataLength += State.DataLength;

			//			Console.WriteLine ("=======================TERIMAAAA========================");
			//			Console.WriteLine ("State Secure : " + State.isSecureConnection.ToString ());
			//			Console.WriteLine ("Data diterima : " + State.sb.ToString ());
			//			Console.WriteLine ("Data srecBuff : " + srecBuff);
			//			Console.WriteLine ("========================AKHIRRR=========================");

			srecBuff = Translator.TranslateFromClient (srecBuff);		// data lengkap
			retParseCode = (ParseCode)Translator.retParseCode;

			if (retParseCode == ParseCode.Completed) {

				HTTPRestDataConstruct.parseClientRequest (srecBuff,
					(((IPEndPoint)State.client.Client.RemoteEndPoint).Address.ToString ()), ref retCode);

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

				retParseCode = (ParseCode)retCode;
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

		protected override ParseCode ParseTranslatedData(MyTcpStreamHandler.ConnectionStateObject State, string TranslatedData){
			HTTPRestDataConstruct.parseClientRequest (TranslatedData,
				(((IPEndPoint)State.client.Client.RemoteEndPoint).Address.ToString ()), ref retCode);

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

			return (ParseCode)retCode;
		}

		protected override void ProcessCompletedData(MyTcpStreamHandler.ConnectionStateObject State)
		{
			string resp = "";
			using (ControlCenter PPOBProcessor = new ControlCenter ()) {
				resp = PPOBProcessor.messageProcessor (HTTPRestDataConstruct.HttpRestClientRequest,
					CommonConfigs);
			}

			FinalizeClientConnection (State, resp);
		}

	}
}

