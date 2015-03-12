

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

using PHClientHandlerInterface;

namespace PHClientHttpHandler
{
	public class PhClientHttpHandler : BaseClientHandlers	//, IDisposable
	{
		#region Disposable
		private bool disposed = false;
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
		~PhClientHttpHandler()
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

		public PhClientHttpHandler (int indexConnection, string ConfigFilePath)
			: base(indexConnection,ConfigFilePath)
		{
			srecBuff = "";
			dataLength = 0;
			HTTPRestDataConstruct = new HTTPRestConstructor();
		}

		HTTPRestConstructor HTTPRestDataConstruct;
		string srecBuff = "";
		const int MAXRecBuff = 100 * 1024;  // max 100Kb
		//        byte[] dataBuffer = new byte[2048];
		int dataLength = 0;
		HTTPRestConstructor.retParseCode retCode = HTTPRestConstructor.retParseCode.Uncompleted;

		public override void DataReceived(MyTcpStreamHandler.ConnectionStateObject State){
			TimeOutSecondsSetting = TIMEOUT_15; // reset timeout untuk penerimaan berikutnya

			// cek data http, jika lengkap set fExitThread = true supaya thread timout keluar

			//fExitThread = true;

			// Validasi Http protocol

			//srecBuff += Encoding.GetEncoding(1252).GetString(data);
			srecBuff += State.sb.ToString ();
			dataLength += State.DataLength;

			srecBuff = Translator.TranslateFromClient (srecBuff);

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
				DisableTimeOut ();		// CloseTimeOutThread ();
				ProcessDataReceived(State);
				break;
			default:
				srecBuff = "";
				dataLength = 0;
				Disconnect(State);
				break;
			}
		}

		ControlCenter PPOBProcessor;

		private void ProcessDataReceived(MyTcpStreamHandler.ConnectionStateObject State)
		{
			if (Translator == null) {
				throw new Exception ("No Client Protocol Translator found...");
			}

			//string resp = PPOBProcessor.messageProcessor(HTTPRestDataConstruct.HttpRestClientRequest, 
			//    logPath,dbHost,dbPort, dbUser,dbPass,dbName,httpRestServicePath,httpRestServiceAccountPath,
			//    httpRestServiceProductTransactionPath, httpRestServiceApplicationsPath, sandraHost,sandraPort);
			string resp = PPOBProcessor.messageProcessor(HTTPRestDataConstruct.HttpRestClientRequest,
				CommonConfigs);

			try
			{
				LogWriter.show(this, "SEND TO CLIENT: " + resp);
				if (resp.Length != 0) {
					if(!SendResponse (State,resp))
						LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Send to already disconnected client: " + resp);
				}
				else
					LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "No message to send to client. Something went wrong. ");

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

