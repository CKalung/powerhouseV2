using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPOBHttpRestData;
using System.Collections;
using Process_MPAccountAccess;
using Process_ProductTransaction;
//using Process_IconOxHandler;
//using Process_PPOBTransaction;

namespace PPOBManager
{
    // Contoh data client
//POST /PPOB-Service/acc/0001 HTTP/1.1
//Date: Thu, 10 Oct 2013 09:21:30 GMT
//Authorization: DA01 dummy-01:Avy/VzZYHg8TwTAg+fVVaJQ2X/0=
//Content-Type: application/json
//Host: 123.231.225.20:7080
//Content-Length: 24

//{"fiFirstName":"kunyuk"}
    public class ControlCenter: IDisposable
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
        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            HTTPRestDataConstruct.Dispose();
            jsonConv.Dispose();
        }
        ~ControlCenter()
        {
            this.Dispose(false);
        }
        #endregion

//        const string ppobServicePath = "/MultiPayment-Service/";
		//        const string ppobServiceAccountPath = "acc/";
		//const string ppobServicePPOBTransactionPath = "ppob/";
		//const string ppobServiceApplicationsPath = "app/";

        PublicSettings.Settings commonSettings;

        //string logPath="";
        //string dbHost = "";
        //int dbPort=0;
        //string dbUser="";
        //string dbPass="";
        //string dbName="";
        string httpRestServicePath = "";
		string canonicalClientHostPath="";
        string httpRestServiceAccountPath = "";
        string httpRestServiceProductTransactionPath = "";
        string httpRestServiceApplicationsPath = "";
		string httpRestServiceEWalletTransactionPath="";
        //string sandraHost=""; int sandraPort=0;

		        //JsonLibs.JSONHandler jsonConv;
        JsonLibs.MyJsonLib jsonConv;
        HTTPRestConstructor HTTPRestDataConstruct;

        public ControlCenter()
        {
            HTTPRestDataConstruct = new HTTPRestConstructor();
            jsonConv = new JsonLibs.MyJsonLib();
        }

        string MakeATest()
        {
            jsonConv.Clear();
            jsonConv.Add("fiFirstName", "Careuh");
            jsonConv.Add("fiLastName", "Bulan");
            jsonConv.Add("fiPhone", "08123333333");
            jsonConv.Add("fiEmail", "careuh@bulan.co.id");
            jsonConv.Add("fiBirthPlace", "Bekasi");
            jsonConv.Add("fiBirthDate", "2012-03-25");
            jsonConv.Add("fiCity", "Bekasi");
            jsonConv.Add("fiGender", "Male");
            jsonConv.Add("fiUsername", "user");
            jsonConv.Add("fiPassword", "SHA1Pass");
            jsonConv.Add("fiNull", null);
            return jsonConv.JSONConstruct();
        }

        bool fastCompareString(string str1, string str2, int from1, int from2, int count)
        {
            try
            {
                int j = from2;
                for (int i = from1; i < (from1 + count); i++)
                {
                    if (str1[i] != str2[j++]) return false;
                }
                return true;
            }
            catch { return false; }
        }

//		public string messageProcessorDedicatedClientHost(HTTPRestConstructor.HttpRestRequest clientData, 
//			PublicSettings.Settings CommonSettings)
//		{
//			string httpRestServiceNetworkPath = commonSettings.getString("httpRestServiceNetworkPath");
//			if (fastCompareString (clientData.CanonicalPath, httpRestServiceNetworkPath, 
//				canonicalClientHostPath.Length, 0, httpRestServiceNetworkPath.Length)) {
//			}
//			else if (fastCompareString(clientData.CanonicalPath, httpRestServiceAccountPath, 
//				canonicalClientHostPath.Length, 0, httpRestServiceAccountPath.Length))
//			{
//				try
//				{
//					reqPathCode = int.Parse(clientData.CanonicalPath.Substring(
//						httpRestServicePath.Length + httpRestServiceAccountPath.Length).Replace("/",""));
//				}
//				catch 
//				{
//					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "205", "Invalid Query code", "");
//				}
//				// Account query
//				using (Process_Account ProcAccount = new Process_Account(commonSettings))
//				{
//					// pass clientData dan code dari hasil sisa canonical 
//					// balikannya: httpcode, serverCode, serve.StackTrace dan serverBody (json)
//					//return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", MakeATest());
//					return ProcAccount.Process(reqPathCode, clientData);
//				}
//			}
//			else if (fastCompareString(clientData.CanonicalPath, httpRestServiceApplicationsPath, 
//				canonicalClientHostPath.Length, 0, httpRestServiceApplicationsPath.Length))
//			{
//				try
//				{
//					reqPathCode = int.Parse(clientData.CanonicalPath.Substring(
//						httpRestServicePath.Length + httpRestServiceApplicationsPath.Length).Replace("/", ""));
//				}
//				catch
//				{
//					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "205", "Invalid Query code", "");
//				}
//				// Application query
//				using (Process_Applications ProcApplications = new Process_Applications(
//					commonSettings))
//				{
//					// pass clientData dan code dari hasil sisa canonical 
//					// balikannya: httpcode, serverCode, serve.StackTrace dan serverBody (json)
//					//return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", MakeATest());
//					return ProcApplications.Process(reqPathCode, clientData);
//				}
//			}
//			else if (fastCompareString(clientData.CanonicalPath, httpRestServiceProductTransactionPath, 
//				canonicalClientHostPath.Length, 0, httpRestServiceProductTransactionPath.Length))
//			{
//				try
//				{
//					reqPathCode = int.Parse(clientData.CanonicalPath.Substring(
//						httpRestServicePath.Length + httpRestServiceProductTransactionPath.Length).Replace("/", ""));
//				}
//				catch
//				{
//					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "205", "Invalid Query code", "");
//				}
//				// PPOBTransaction query
//				using (Process_Product ProcProducts = new Process_Product(commonSettings))
//				{
//					// pass clientData dan code dari hasil sisa canonical 
//					// balikannya: httpcode, serverCode, serve.StackTrace dan serverBody (json)
//					//return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", MakeATest());
//					return ProcProducts.Process(reqPathCode, clientData);
//				}
//				// return HTTPRestDataConstruct.constructHTTPRestResponse(200, "201", "Transaction not ready", "");
//			}
//			else
//			{
//				return HTTPRestDataConstruct.constructHTTPRestResponse(404, "404", "Not Found", "");
//			}
//
//		}

		private int getReqPathCode(ref string httpErrResp, string rootcanonicalpath, 
			string httpRestCanonPath){
			httpErrResp = "";
			try
			{
				return int.Parse(rootcanonicalpath.Substring(
					httpRestServicePath.Length + httpRestCanonPath.Length).Replace("/",""));
			}
			catch 
			{
				httpErrResp = HTTPRestDataConstruct.constructHTTPRestResponse(400, "205", "Invalid Query code", "");
				return 0;
			}
		}

        /// <summary>
        /// Proses message dari client, dan return dengan hasil yang harus di kirim ke client serta penentuan bisnis proses nya
        /// </summary>
        /// <param name="clientData">Data dari client</param>
        /// <returns></returns>
        //public string messageProcessor(HTTPRestConstructor.HttpRestRequest clientData, string LogPath, 
        //    string DbHost, int DbPort, string DbUser, string DbPassw, string DbName,
        //    string HttpRestServicePath,
        //    string HttpRestServiceAccountPath,
        //    string HttpRestServiceProductTransactionPath,
        //    string HttpRestServiceApplicationsPath,
        //    string SandraHost, int SandraPort
        //    )
        public string messageProcessor(HTTPRestConstructor.HttpRestRequest clientData, 
            PublicSettings.Settings CommonSettings)
        {
            int reqPathCode = 0;
            commonSettings = CommonSettings;
			commonSettings.ReloadSettings ();
            //logPath = LogPath;
            //dbHost = DbHost; dbPort = DbPort; dbUser = DbUser; dbPass = DbPassw; dbName = DbName;
            httpRestServicePath = commonSettings.getString("CanonicalPath");
            httpRestServiceAccountPath = commonSettings.getString("httpRestServiceAccountPath");
            httpRestServiceProductTransactionPath = commonSettings.getString("httpRestServiceProductTransactionPath");
            httpRestServiceApplicationsPath = commonSettings.getString("httpRestServiceApplicationsPath");
			httpRestServiceEWalletTransactionPath = commonSettings.getString("httpRestServiceEWalletTransactionPath");
            //sandraHost = SandraHost; sandraPort= SandraPort;

			canonicalClientHostPath = commonSettings.getString("CanonicalPathClientHost");

			//string hasil = "";
            if(clientData.Method != HTTPRestConstructor.enMethod.POST)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(405, "405", "Unexpected method", "");
            }

//			// Disini pengecekan untuk protokol dari ClientHost, bukan dari mobile atau web qiosku
//			if (fastCompareString (clientData.CanonicalPath, canonicalClientHostPath, 0, 0, canonicalClientHostPath.Length)) {
//				// disini lempar ke ClientHost protocol
//				return messageProcessorDedicatedClientHost (clientData, CommonSettings);
//			}

			//string SvcPath = clientData.CanonicalPath;
			if (!fastCompareString(clientData.CanonicalPath, httpRestServicePath,0,0,httpRestServicePath.Length))
            {
				// reject
				return HTTPRestDataConstruct.constructHTTPRestResponse (404, "204", "Path not found", "");
            }

			string httpErrMsg = "";
			if (fastCompareString(clientData.CanonicalPath, httpRestServiceAccountPath, 
				httpRestServicePath.Length, 0, httpRestServiceAccountPath.Length))
            {
				reqPathCode = getReqPathCode (ref httpErrMsg, clientData.CanonicalPath, httpRestServiceAccountPath);
				if (httpErrMsg != "")
					return httpErrMsg;

//                try
//                {
//					reqPathCode = int.Parse(clientData.CanonicalPath.Substring(
//						httpRestServicePath.Length + httpRestServiceAccountPath.Length).Replace("/",""));
//                }
//                catch 
//                {
//                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "205", "Invalid Query code", "");
//                }
                // Account query
				using (Process_Account ProcAccount = new Process_Account(commonSettings))
                {
                    // pass clientData dan code dari hasil sisa canonical 
                    // balikannya: httpcode, serverCode, serve.StackTrace dan serverBody (json)
                    //return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", MakeATest());
                    return ProcAccount.Process(reqPathCode, clientData);
                }
            }
			else if (fastCompareString(clientData.CanonicalPath, httpRestServiceApplicationsPath, 
				httpRestServicePath.Length, 0, httpRestServiceApplicationsPath.Length))
            {
				reqPathCode = getReqPathCode (ref httpErrMsg, clientData.CanonicalPath, httpRestServiceApplicationsPath);
				if (httpErrMsg != "")
					return httpErrMsg;

                // Application query
				using (Process_Applications ProcApplications = new Process_Applications(
					commonSettings))
                {
                    // pass clientData dan code dari hasil sisa canonical 
                    // balikannya: httpcode, serverCode, serve.StackTrace dan serverBody (json)
                    //return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", MakeATest());
                    return ProcApplications.Process(reqPathCode, clientData);
                }
            }
			else if (fastCompareString(clientData.CanonicalPath, httpRestServiceProductTransactionPath, 
				httpRestServicePath.Length, 0, httpRestServiceProductTransactionPath.Length))
            {
				reqPathCode = getReqPathCode (ref httpErrMsg, clientData.CanonicalPath, httpRestServiceProductTransactionPath);
				if (httpErrMsg != "")
					return httpErrMsg;

                // PPOBTransaction query
                using (Process_Product ProcProducts = new Process_Product(commonSettings))
                {
                    // pass clientData dan code dari hasil sisa canonical 
                    // balikannya: httpcode, serverCode, serve.StackTrace dan serverBody (json)
                    //return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", MakeATest());
                    return ProcProducts.Process(reqPathCode, clientData);
                }
				// return HTTPRestDataConstruct.constructHTTPRestResponse(200, "201", "Transaction not ready", "");
            }
//			else if (fastCompareString(clientData.CanonicalPath, httpRestServiceEWalletTransactionPath, 
//				httpRestServicePath.Length, 0, httpRestServiceEWalletTransactionPath.Length))
//			{
//				try
//				{
//					reqPathCode = int.Parse(clientData.CanonicalPath.Substring(
//						httpRestServicePath.Length + httpRestServiceEWalletTransactionPath.Length).Replace("/", ""));
//				}
//				catch
//				{
//					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "205", "Invalid Query code", "");
//				}
//				// EWallet Topup query
//				using (Process_IconOx ProcIconox = new Process_IconOx (commonSettings))
//				{
//					return ProcIconox.Process(reqPathCode, clientData);
//				}
//			}
            else
            {
                // reject
                return HTTPRestDataConstruct.constructHTTPRestResponse(404, "404", "Not Found", "");
            }

            //CustomerClientJson 
        }

    }
}
