using System;
using LOG_Handler;
using PPOBHttpRestData;

namespace BPJS_THT
{
	public class IBID_Handler : IDisposable
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
		~IBID_Handler()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			rs.Dispose();
		}

		string IBIDHost = "192.168.165.3";
		int IBIDPort = 8787;

		public struct lastError
		{
			public string ServerCode;
			public string ServerMessage;
		}

		private RestService rs;
		public lastError lastErr;
		private int recTimeOut=1200;
		private bool useTcp = false;
		HTTPRestConstructor HTTPRestDataConstruct;

		public IBID_Handler (int recTimeOut, bool UseTcp)
		{
			// init 
			HTTPRestDataConstruct = new HTTPRestConstructor();
			rs = new RestService();
			lastErr = new lastError();
			useTcp = UseTcp;
		}

		#region LastError
		public void fillLastError()
		{
			if (rs.Response.serverResponseCode == null) 
			{
				lastErr.ServerCode = "z888";
				lastErr.ServerMessage = "Unconditional error";
				return;
			}
			if (rs.Response.serverResponseCode != "")
			{
				lastErr.ServerCode = rs.Response.serverResponseCode;
				lastErr.ServerMessage = rs.Response.serverResponseMessage;
			}
			else
			{
				if (rs.Response.httpMessage != "")
				{
					lastErr.ServerCode = "Z" + rs.Response.httpCode.ToString();
					lastErr.ServerMessage = rs.Response.httpMessage;
				}
				else
				{
					lastErr.ServerCode = "Z777";
					lastErr.ServerMessage = "No error message";
				}
			}
		}

		public lastError LastError
		{
			get { return lastErr; }
		}

		public string getSessionUserAuth
		{
			get { return rs.userAuth; }
		}

		public string getSessionSecretKey
		{
			get { return rs.secretKey; }
		}
		// ===================================
		#endregion

//		nomor kps : 10022905128
//		msisdn : 6281288189738
//		messageid : ATM7889900000007
//		disusun menjadi
//		http://192.168.165.3:8787/bpjs/jht/10022905128/6281288189738/ATM7889900000007
		public string InquirySaldo(string nomorKPS, string msisdn, string messageId, 
			bool secureConnection){
			string IBIDUrl = "http://"+IBIDHost+":"+IBIDPort;
			string subCanonicalPath = "/"+nomorKPS+"/"+msisdn+"/"+messageId;


			string httpna = "";
			if(secureConnection)
				httpna = "https://";
			else
				httpna = "http://";
			rs.httpUri = httpna + IBIDHost + ":" + IBIDPort.ToString() + "/bpjs/jht";

			rs.canonicalPath = "/"+nomorKPS+"/"+msisdn+"/"+messageId;
			rs.authID = "";
			rs.method = "GET";	// "POST";
			rs.contentType = "application/json";
			//rs.userAuth = "dummy-01";
			//rs.secretKey = "dummy-01";
			//rs.bodyMessage = "{\"ficoCustomerId\":\"" + customerId + "\"}";
			rs.bodyMessage = "";

			string res = "";
			if(useTcp)
				res = rs.TCPRestSendRequest(recTimeOut);
			else
				res = rs.HttpRestSendRequest(recTimeOut);

			if ((res == "") && ((rs.Response.httpMessage == null) || (rs.Response.httpCode == 0)))
			{
				lastErr.ServerCode = "I" + "999";
				lastErr.ServerMessage = "No response from IBID Host";
				return "";
				//return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", lastErr.ServerMessage, "");
				//return false;
			}

//			LogWriter.showDEBUG("HttpCode: " + rs.Response.httpCode.ToString());
//			LogWriter.showDEBUG("HttpMessage: " + rs.Response.httpMessage);
//			LogWriter.showDEBUG("HttpContentType: " + rs.Response.httpContentType);
//			LogWriter.showDEBUG("HttpDate: " + rs.Response.httpDate);
//			LogWriter.showDEBUG("ServerResponseCode: " + rs.Response.serverResponseCode);
//			LogWriter.showDEBUG("ServerResponseMessage: " + rs.Response.serverResponseMessage);
//			LogWriter.showDEBUG("HttpBody: " + rs.Response.httpBody);

			string sDbg = "HttpCode: " + rs.Response.httpCode.ToString() + "\r\n"+
				"ServerResponseCode: " + rs.Response.serverResponseCode + "\r\n"+
				"ServerResponseMessage: " + rs.Response.serverResponseMessage + "\r\n" +
				"HttpBody: " + rs.Response.httpBody;

			LogWriter.show(this, sDbg);

			if (rs.Response.httpCode != 200)
			{
				fillLastError();
				LogWriter.show(this, "No response");
				return "";
//				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
//					"No success response: " + rs.Response.httpCode.ToString (), "");
				//return false;
			}

			//if (rs.Response.httpContentType == "application/json")
			if (rs.Response.httpBody != null)
				res = rs.Response.httpBody;
			else
				res = "";
			return res;
		}

//		public void testIBID(){
//		}
//
	}
}

