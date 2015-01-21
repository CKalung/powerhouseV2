using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
using JsonLibs;
using LOG_Handler;
using System.Security.Cryptography;

namespace PPOBServerHandler
{
    public class SandraLibs : IDisposable
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
            rs.Dispose();
            //jh.Dispose();
            MyJH.Dispose();
        }
        ~SandraLibs()
        {
            this.Dispose(false);
        }
        #endregion

        private RestService rs;
        //private JsonLibs.JSONHandler jh;
        private JsonLibs.MyJsonLib MyJH;


        public struct lastError
        {
            public string ServerCode;
            public string ServerMessage;
        }
        public lastError lastErr;
        private int recTimeOut=1200;
		private string userAuth = "";
		private string secretKey = "";
		private string sandraAuthId = "";
		private bool useTcp = false;

        //public SandraLibs(string _baseAddress, string _serverPort, string _baseDir, string _canonicalPath, string _method, string _userAuth, string _secreteKey, string _contentType, string _bodyMessage, string _contentParam)
		public SandraLibs(int recTimeOut, bool UseTcp, string HmsUserAuth, string HmsSecretKey, string SandraAuthId)
        {
            // init 
            rs = new RestService();
            //jh = new JsonLibs.JSONHandler();
            MyJH = new MyJsonLib();
            lastErr = new lastError();
			userAuth = HmsUserAuth;
			secretKey = HmsSecretKey;
			sandraAuthId = SandraAuthId;
			useTcp = UseTcp;
        }

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

		public bool TestHttps(string SandraHost, int SandraPort, bool secureConnection, 
			TokenHostEnum TokenHost)
		{
//			rs.httpUri = "https://qiosku.com:443";
//			rs.canonicalPath = "/tes.php";
//			rs.httpUri = "https://qva.qnbkesawan.co.id/hms/rest";
			rs.httpUri = "https://10.211.250.22:443/hms/rest";
			//			rs.httpUri = "https://" + SandraHost + ":" + SandraPort.ToString() + "/hms/rest";

			LogWriter.show(this, "Tes konek https: " + rs.httpUri);

			rs.canonicalPath = "/requestToken";
			rs.authID = sandraAuthId;
			rs.method = "POST";
			rs.contentType = "application/json";
			rs.userAuth = userAuth;
			rs.secretKey = secretKey;

			rs.bodyMessage = "{\"sourceHostId\":\"switching-gateway-service\"}";

			string res = "";
			res = rs.HttpRestSendRequest(recTimeOut);

			if ((res == "") && ((rs.Response.httpMessage == null) || (rs.Response.httpCode == 0)))
			{
				lastErr.ServerCode = "Z" + "999";
				lastErr.ServerMessage = "No response from Sandra Host";
				return false;
			}
			string sDbg = "HttpCode: " + rs.Response.httpCode.ToString() + "\r\n" +
			                          "ServerResponseCode: " + rs.Response.serverResponseCode + "\r\n" +
			                          "ServerResponseMessage: " + rs.Response.serverResponseMessage + "\r\n" +
			                          "HttpBody: " + rs.Response.httpBody;
			LogWriter.show(this, sDbg);
			return true;

		}

        public enum TokenHostEnum { QVA, PERSADA }
		public bool RequestToken(string SandraHost, int SandraPort, bool secureConnection, 
			TokenHostEnum TokenHost)
        {
            LogWriter.show(this, "Request Token");
			string httpna = "";
			if(secureConnection)
				httpna = "https://";
			else
				httpna = "http://";
			rs.httpUri = httpna + SandraHost + ":" + SandraPort.ToString() + "/hms/rest";
            rs.canonicalPath = "/requestToken";
			rs.authID = sandraAuthId;
            rs.method = "POST";
            rs.contentType = "application/json";
			rs.userAuth = userAuth;
			rs.secretKey = secretKey;
            if (TokenHost == TokenHostEnum.QVA)
                rs.bodyMessage = "{\"sourceHostId\":\"switching-gateway-service\"}";
            else if (TokenHost == TokenHostEnum.PERSADA)
                rs.bodyMessage = "{\"sourceHostId\":\"switching-ppob-gateway-service\"}";  
            else return false;

//			LogWriter.show(this, "URL: " + rs.httpUri + "\r\n" + 
//				"Body: " + rs.bodyMessage + "\r\n" + 
//				"useTcp " + useTcp.ToString());

			string res = "";
			if(useTcp)
				res = rs.TCPRestSendRequest(recTimeOut);
			else
				res = rs.HttpRestSendRequest(recTimeOut);

            if ((res == "") && ((rs.Response.httpMessage == null) || (rs.Response.httpCode == 0)))
            {
                lastErr.ServerCode = "Z" + "999";
                lastErr.ServerMessage = "No response from Sandra Host";
                return false;
            }

            //Console.WriteLine("HttpCode: " + rs.Response.httpCode.ToString());
            //Console.WriteLine("HttpMessage: " + rs.Response.httpMessage);
            //Console.WriteLine("HttpContentType: " + rs.Response.httpContentType);
            //Console.WriteLine("HttpDate: " + rs.Response.httpDate);
            //Console.WriteLine("ServerResponseCode: " + rs.Response.serverResponseCode);
            //Console.WriteLine("ServerResponseMessage: " + rs.Response.serverResponseMessage);
            //Console.WriteLine("HttpBody: " + rs.Response.httpBody);

            string sDbg = "HttpCode: " + rs.Response.httpCode.ToString() + "\r\n" +
                            "ServerResponseCode: " + rs.Response.serverResponseCode + "\r\n" +
                            "ServerResponseMessage: " + rs.Response.serverResponseMessage + "\r\n" +
                            "HttpBody: " + rs.Response.httpBody;
            LogWriter.show(this, sDbg);
            if (rs.Response.httpCode != 200)
            {
                fillLastError();
                LogWriter.showDEBUG(this, "No response");
                return false;
            }

            if (rs.Response.httpContentType == "application/json")
                res = rs.Response.httpBody;

            //object djson;
            //JObject o;
            if (res != "")
            {
                //djson = JsonConvert.DeserializeObject(res);
                MyJH.Clear();
                if (!MyJH.JSONParse(res))
                {
                    // gagal parsing json dari sandra
                    LogWriter.showDEBUG(this, "Bad JSON format from host");
                    lastErr.ServerCode = "Z999";
                    lastErr.ServerMessage = "Bad JSON format from host";
                    return false;
                }
                // cokot value na
                //o = jh.JSONReadObject(djson.ToString());
                try
                {
                    //Console.WriteLine("Request Token :");
                    //Console.WriteLine("requestId : " + MyJH["requestId"]);
                    //Console.WriteLine("secretToken : " + MyJH["secretToken"]);
                    //Console.WriteLine("baseUrl : " + MyJH["baseUrl"]);
                    //Console.WriteLine("");

                    rs.userAuth = (string)MyJH["requestId"];
                    rs.secretKey = (string)MyJH["secretToken"];
                }
                catch
                {
                    // gagal parsing json dari sandra
                    LogWriter.showDEBUG(this, "Field not found from HMS host");
                    lastErr.ServerCode = "Z999";
                    lastErr.ServerMessage = "Uncompleted field from HMS host";
                    return false;
                }
                return true;
            }
            else
            {
                fillLastError();
                LogWriter.showDEBUG(this, "Gagal Json");
                return false;
            }
        }

        public void clearLastErr()
        {
            lastErr.ServerCode = "";
            lastErr.ServerMessage = "";
        }

		public bool Inquiry(string SandraHost, int SandraPort, 
			bool secureConnection, string customerId, 
            ref double balance)
        {
            clearLastErr();
            balance = 0;
			if (!RequestToken(SandraHost, SandraPort, secureConnection, TokenHostEnum.QVA))
            {
                fillLastError();
                return false;
            }

            // http://123.231.225.20:7080/switching-gateway-service/rest/gateway/040158/
            //string customerId = "0000000000000995";

			string httpna = "";
			if(secureConnection)
				httpna = "https://";
			else
				httpna = "http://";
			rs.httpUri = httpna + SandraHost + ":" + SandraPort.ToString() + "/switching-gateway-service/rest";

            rs.canonicalPath = "/gateway/040158";
			rs.authID = sandraAuthId;
            rs.method = "POST";
            rs.contentType = "application/json";
            //rs.userAuth = "dummy-01";
            //rs.secretKey = "dummy-01";
            rs.bodyMessage = "{\"ficoCustomerId\":\"" + customerId + "\"}";

			string res = "";
			if(useTcp)
				res = rs.TCPRestSendRequest(recTimeOut);
			else
				res = rs.HttpRestSendRequest(recTimeOut);

            if ((res == "") && ((rs.Response.httpMessage == null) || (rs.Response.httpCode == 0)))
            {
                lastErr.ServerCode = "Z" + "999";
                lastErr.ServerMessage = "No response from Sandra Host";
                return false;
            }

            //Console.WriteLine("HttpCode: " + rs.Response.httpCode.ToString());
            //Console.WriteLine("HttpMessage: " + rs.Response.httpMessage);
            //Console.WriteLine("HttpContentType: " + rs.Response.httpContentType);
            //Console.WriteLine("HttpDate: " + rs.Response.httpDate);
            //Console.WriteLine("ServerResponseCode: " + rs.Response.serverResponseCode);
            //Console.WriteLine("ServerResponseMessage: " + rs.Response.serverResponseMessage);
            //Console.WriteLine("HttpBody: " + rs.Response.httpBody);

            string sDbg = "HttpCode: " + rs.Response.httpCode.ToString() + "\r\n"+
                            "ServerResponseCode: " + rs.Response.serverResponseCode + "\r\n"+
                            "ServerResponseMessage: " + rs.Response.serverResponseMessage + "\r\n" +
                            "HttpBody: " + rs.Response.httpBody;
            LogWriter.show(this, sDbg);

            if (rs.Response.httpCode != 200)
            {
                fillLastError();
                LogWriter.show(this, "No response");
                return false;
            }

            if (rs.Response.httpContentType == "application/json")
                res = rs.Response.httpBody;

            //object djson;
            //JObject o;
            if (res != "")
            {
                //djson = JsonConvert.DeserializeObject(res);
                MyJH.Clear();
                if (!MyJH.JSONParse(res))
                {
                    // gagal parsing json dari sandra
                    LogWriter.show(this, "Bad JSON format from host");
                    lastErr.ServerCode = "Z999";
                    lastErr.ServerMessage = "Bad JSON format from host";
                    return false;
                }

                // cokot value na
                //o = jh.JSONReadObject(djson.ToString());
                try
                {
                    if (MyJH.ContainsKey("ficoCustomerBalance"))
                        balance = (double)MyJH["ficoCustomerBalance"];
                    else
                        balance = 0;
                    //Console.WriteLine("Inquiry :");
                    //Console.WriteLine("ficoCustomerId : " + MyJH["ficoCustomerId"]);
                    //Console.WriteLine("ficoCustomerBalance: " + balance);
                    //Console.WriteLine("");
                    sDbg = "Inquiry : ficoCustomerId = " + MyJH["ficoCustomerId"] + " , " +
                                    "ficoCustomerBalance: " + balance.ToString();
                    LogWriter.show(this, sDbg);
                    //balance = double.Parse( MyJH["ficoCustomerBalance"].ToString());
                    return true;
                }
                catch
                {
                    // gagal parsing json dari sandra
                    LogWriter.show(this, "Field not found from host");
                    lastErr.ServerCode = "Z999";
                    lastErr.ServerMessage = "Uncompleted field from host";
                    return false;
                }
            }
            else
            {
                fillLastError();
                LogWriter.show(this, "Gagal Json");
                return false;
            }
        }

		public bool GetLastTransactionHistory(string SandraHost, int SandraPort, 
			bool secureConnection, string customerId,
            int limit, ref MyJsonArray historyLog)
        {
            clearLastErr();
			if (!RequestToken(SandraHost, SandraPort, secureConnection, TokenHostEnum.QVA))
            {
                fillLastError();
                return false;
            }

            // http://123.231.225.20:7080/switching-gateway-service/rest/gateway/040158/
            //string customerId = "0000000000000995";

			string httpna = "";
			if(secureConnection)
				httpna = "https://";
			else
				httpna = "http://";
			rs.httpUri = httpna + SandraHost + ":" + SandraPort.ToString() + "/switching-gateway-service/rest";

            rs.canonicalPath = "/gateway/040432";
			rs.authID = sandraAuthId;
            rs.method = "POST";
            rs.contentType = "application/json";
            //rs.userAuth = "dummy-01";
            //rs.secretKey = "dummy-01";
            rs.bodyMessage = "{\"ficoCustomerId\":\"" + customerId + "\",\"limit\":" + limit.ToString() +"}";

			string res = "";
			if(useTcp)
				res = rs.TCPRestSendRequest(recTimeOut);
			else
				res = rs.HttpRestSendRequest(recTimeOut);

            if ((res == "") && ((rs.Response.httpMessage == null) || (rs.Response.httpCode == 0)))
            {
                lastErr.ServerCode = "Z" + "999";
                lastErr.ServerMessage = "No response from Sandra Host";
                return false;
            }

            string sDbg = "HttpCode: " + rs.Response.httpCode.ToString() + "\r\n" +
                            "ServerResponseCode: " + rs.Response.serverResponseCode + "\r\n" +
                            "ServerResponseMessage: " + rs.Response.serverResponseMessage + "\r\n" +
                            "HttpBody: " + rs.Response.httpBody;
            LogWriter.show(this, sDbg);

            if (rs.Response.httpCode != 200)
            {
                fillLastError();
                LogWriter.show(this, "No response");
                return false;
            }

            if (rs.Response.httpContentType == "application/json")
                res = rs.Response.httpBody;

            if (res != "")
            {
                //djson = JsonConvert.DeserializeObject(res);
                MyJH.Clear();
                if (!MyJH.JSONParse(res))
                {
                    // gagal parsing json dari sandra
                    LogWriter.show(this, "Bad JSON format from host");
                    lastErr.ServerCode = "Z999";
                    lastErr.ServerMessage = "Bad JSON format from host";
                    return false;
                }

                // cokot value na
                if (MyJH.ContainsKey("ficoCustomerId"))
                    sDbg = "Get History : ficoCustomerId = " + (string)MyJH["ficoCustomerId"];
                else
                    sDbg = "Get History : field ficoCustomerId not found, no history found";
                LogWriter.show(this, sDbg);

                if (MyJH.ContainsKey("customerReportModelsList"))
                    historyLog = (MyJsonArray)MyJH["customerReportModelsList"];
                else
                {
                    historyLog = new MyJsonArray();
                }
                return true;
            }
            else
            {
                fillLastError();
                LogWriter.show(this, "Gagal Json");
                return false;
            }
        }

		public bool GetDetailTransaction(string SandraHost, int SandraPort, 
			bool secureConnection, 
            string invoiceId, ref MyJsonLib detailTransaction)
        {
            clearLastErr();
            detailTransaction = null;
			if (!RequestToken(SandraHost, SandraPort, secureConnection, TokenHostEnum.QVA))
            {
                fillLastError();
                return false;
            }

            // http://123.231.225.20:7080/switching-gateway-service/rest/gateway/040158/
            //string customerId = "0000000000000995";

			string httpna = "";
			if(secureConnection)
				httpna = "https://";
			else
				httpna = "http://";
			rs.httpUri = httpna + SandraHost + ":" + SandraPort.ToString() + "/switching-gateway-service/rest";

            rs.canonicalPath = "/gateway/040433";
			rs.authID = sandraAuthId;
            rs.method = "POST";
            rs.contentType = "application/json";
            //rs.userAuth = "dummy-01";
            //rs.secretKey = "dummy-01";
            rs.bodyMessage = "{\"ficoInvoice\":\"" + invoiceId + "\"}";

			string res = "";
			if(useTcp)
				res = rs.TCPRestSendRequest(recTimeOut);
			else
				res = rs.HttpRestSendRequest(recTimeOut);

            if ((res == "") && ((rs.Response.httpMessage == null) || (rs.Response.httpCode == 0)))
            {
                lastErr.ServerCode = "Z" + "999";
                lastErr.ServerMessage = "No response from Sandra Host";
                return false;
            }

            string sDbg = "HttpCode: " + rs.Response.httpCode.ToString() + "\r\n" +
                            "ServerResponseCode: " + rs.Response.serverResponseCode + "\r\n" +
                            "ServerResponseMessage: " + rs.Response.serverResponseMessage + "\r\n" +
                            "HttpBody: " + rs.Response.httpBody;
            LogWriter.show(this, sDbg);

            if (rs.Response.httpCode != 200)
            {
                fillLastError();
                LogWriter.show(this, "No response");
                return false;
            }

            if (rs.Response.httpContentType == "application/json")
                res = rs.Response.httpBody;

            if (res != "")
            {
                //djson = JsonConvert.DeserializeObject(res);
                MyJH.Clear();
                if (!MyJH.JSONParse(res))
                {
                    // gagal parsing json dari sandra
                    LogWriter.show(this, "Bad JSON format from host");
                    lastErr.ServerCode = "Z999";
                    lastErr.ServerMessage = "Bad JSON format from host";
                    return false;
                }

                // cokot value na
                sDbg = "Get Trx detail of InvoiceId : " + invoiceId;
                detailTransaction = MyJH;
                LogWriter.show(this, sDbg);
                return true;
            }
            else
            {
                fillLastError();
                LogWriter.show(this, "Gagal Json");
                return false;
            }
        }

		public bool RegisterCustomer(string SandraHost, int SandraPort, 
			bool secureConnection, ref MyJsonLib customerData)
        {
			if (!RequestToken(SandraHost, SandraPort, secureConnection, TokenHostEnum.QVA))
            {
                fillLastError();
                return false;
            }

			string httpna = "";
			if(secureConnection)
				httpna = "https://";
			else
				httpna = "http://";
			rs.httpUri = httpna + SandraHost + ":" + SandraPort.ToString() + "/switching-gateway-service/rest";

            rs.canonicalPath = "/gateway/040210";
			rs.authID = sandraAuthId;
            rs.method = "POST";
            rs.contentType = "application/json";
            //rs.userAuth = "dummy-01";
            //rs.secretKey = "dummy-01";

            // buat jsonnya untuk ke host sandra
            rs.bodyMessage = customerData.JSONConstruct();
            //rs.bodyMessage = jh.JSONSerialize(customerData);
            LogWriter.show(this, "Json Ke SANDRA: " + rs.bodyMessage);

			string res = "";
			if(useTcp)
				res = rs.TCPRestSendRequest(recTimeOut);
			else
				res = rs.HttpRestSendRequest(recTimeOut);

            if ((res == "") && ((rs.Response.httpMessage == null) || (rs.Response.httpCode == 0)))
            {
                lastErr.ServerCode = "Z" + "999";
                lastErr.ServerMessage = "No response from Sandra Host";
                return false;
            }

            //Console.WriteLine("HttpCode: " + rs.Response.httpCode.ToString());
            //Console.WriteLine("HttpMessage: " + rs.Response.httpMessage);
            //Console.WriteLine("HttpContentType: " + rs.Response.httpContentType);
            //Console.WriteLine("HttpDate: " + rs.Response.httpDate);
            //Console.WriteLine("ServerResponseCode: " + rs.Response.serverResponseCode);
            //Console.WriteLine("ServerResponseMessage: " + rs.Response.serverResponseMessage);
            //Console.WriteLine("HttpBody: " + rs.Response.httpBody);

            string sDbg = "HttpCode: " + rs.Response.httpCode.ToString() + "\r\n" +
                            "ServerResponseCode: " + rs.Response.serverResponseCode + "\r\n" +
                            "ServerResponseMessage: " + rs.Response.serverResponseMessage + "\r\n" +
                            "HttpBody: " + rs.Response.httpBody;
            LogWriter.show(this, sDbg);
            if (rs.Response.httpCode != 200)
            {
                fillLastError();
                LogWriter.show(this, "No response");
                return false;
            }

            if (rs.Response.httpContentType == "application/json")
                res = rs.Response.httpBody;

            //object djson;
            //JObject o;
            if (res != "")
            {
                //djson = JsonConvert.DeserializeObject(res);
                if (!customerData.JSONParse(res))
                {
                    lastErr.ServerCode = "409";
                    lastErr.ServerMessage = "Invalid host message";
                    return false;
                }
                try
                {
                    sDbg = "ficoCustomerId =" + customerData["ficoCustomerId"] + "\r\n" +
                            "ficoCustomerName =" + customerData["ficoCustomerName"] + "\r\n" +
                            "ficoCustomerPhone =" + customerData["ficoCustomerPhone"] + "\r\n" +
                            "ficoCustomerPhone2 =" + customerData["ficoCustomerPhone2"] + "\r\n" +
                            "ficoCustomerEmail =" + customerData["ficoCustomerEmail"] + "\r\n" +
                            "ficoCustomerBirthDate =" + customerData["ficoCustomerBirthDate"] + "\r\n" +
                            "ficoCustomerBirthPlace =" + customerData["ficoCustomerBirthPlace"] + "\r\n" +
                            "ficoCustomerCity =" + customerData["ficoCustomerCity"] + "\r\n" +
                            "ficoCustomerCity2 =" + customerData["ficoCustomerCity2"] + "\r\n" +
                            "ficoCustomerAddress =" + customerData["ficoCustomerAddress"] + "\r\n" +
                            "ficoCustomerTrxAllowed =" + customerData["ficoCustomerTrxAllowed"] + "\r\n" +
                            "ficoCustomerAddress2 =" + customerData["ficoCustomerAddress2"] + "\r\n" +
                            "ficoCustomerAddress3 =" + customerData["ficoCustomerAddress3"] + "\r\n" +
                            "ficoCustomerZipCode =" + customerData["ficoCustomerZipCode"] + "\r\n" +
                            "ficoCustomerZipCode2 =" + customerData["ficoCustomerZipCode2"] + "\r\n" +
                            "ficoCustomerCustomField1 =" + customerData["ficoCustomerCustomField1"] + "\r\n" +
                            "ficoCustomerCustomField2 =" + customerData["ficoCustomerCustomField2"] + "\r\n" +
                            "ficoCustomerCustomField3 =" + customerData["ficoCustomerCustomField3"] + "\r\n" +
                            "ficoCustomerCustomField4 =" + customerData["ficoCustomerCustomField4"] + "\r\n" +
                            "ficoCustomerCustomField5 =" + customerData["ficoCustomerCustomField5"] + "\r\n" +
                            "ficoCustomerCardNumber =" + customerData["ficoCustomerCardNumber"] + "\r\n" +
                            "ficoCustomerCardIdentityType =" + customerData["ficoCustomerCardIdentityType"] + "\r\n" +
                            "ficoCustomerIdentityCardNumber =" + customerData["ficoCustomerIdentityCardNumber"] + "\r\n" +
                            "ficoCustomerIdentityCardValidDate =" + customerData["ficoCustomerIdentityCardValidDate"] + "\r\n" +
                            "ficoCustomerNpwp =" + customerData["ficoCustomerNpwp"] + "\r\n" +
                            "ficoCustomerNickname =" + customerData["ficoCustomerNickname"] + "\r\n" +
                            "ficoCustomerRef1 =" + customerData["ficoCustomerRef1"] + "\r\n" +
                            "ficoCustomerRef2 =" + customerData["ficoCustomerRef2"] + "\r\n" +
                            "ficoCustomerRef3 =" + customerData["ficoCustomerRef3"] + "\r\n" +
                            "ficoCustomerGender =" + customerData["ficoCustomerGender"] + "\r\n" +
                            "ficoCustomerMotherName =" + customerData["ficoCustomerMotherName"] + "\r\n" +
                            "ficoCustomerPassword =" + customerData["ficoCustomerPassword"] + "\r\n" +
                            "ficoCustomerUsername =" + customerData["ficoCustomerUsername"] + "\r\n" +
                            "source =" + customerData["source"] + "\r\n" +
                            "ficoCustomerGroupName =" + customerData["ficoCustomerGroupName"] + "\r\n" +
                            "ficoCustomerBankAccount =" + customerData["ficoCustomerBankAccount"] + "\r\n" +
                            "ficoRegistrationDate =" + customerData["ficoRegistrationDate"] + "\r\n" +
                            "ficoCustomerStatus =" + customerData["ficoCustomerStatus"];
                    LogWriter.show(this, sDbg);
                }
                catch
                {
                    lastErr.ServerCode = "409";
                    lastErr.ServerMessage = "Invalid host message";
                    return false;
                }
                /* Contoh hasilnya
                    ficoCustomerGroupName =DAKSA
                    ficoCustomerBankAccount =1111111111111
                    ficoRegistrationDate =10/14/2013 11:28:45 PM
                    ficoCustomerStatus =PENDING
                    Tekan Enter....
                 */
                return true;
            }
            else
            {
                fillLastError();
                LogWriter.show(this, "Gagal Json");
                return false;
            }
        }

		public bool EditCustomer(string SandraHost, int SandraPort, 
			bool secureConnection, MyJsonLib customerData)
        {
			if (!RequestToken(SandraHost, SandraPort, secureConnection, TokenHostEnum.QVA))
            {
                fillLastError();
                return false;
            }

			string httpna = "";
			if(secureConnection)
				httpna = "https://";
			else
				httpna = "http://";
			rs.httpUri = httpna + SandraHost + ":" + SandraPort.ToString() + "/switching-gateway-service/rest";

            rs.canonicalPath = "/gateway/040211";
			rs.authID = sandraAuthId;
            rs.method = "POST";
            rs.contentType = "application/json";
            //rs.userAuth = "dummy-01";
            //rs.secretKey = "dummy-01";

            // buat jsonnya untuk ke host sandra
            rs.bodyMessage = customerData.JSONConstruct();

			string res = "";
			if(useTcp)
				res = rs.TCPRestSendRequest(recTimeOut);
			else
				res = rs.HttpRestSendRequest(recTimeOut);

            if ((res == "") && ((rs.Response.httpMessage == null) || (rs.Response.httpCode == 0)))
            {
                lastErr.ServerCode = "Z" + "999";
                lastErr.ServerMessage = "No response from Sandra Host";
                return false;
            }

            //Console.WriteLine("HttpCode: " + rs.Response.httpCode.ToString());
            //Console.WriteLine("HttpMessage: " + rs.Response.httpMessage);
            //Console.WriteLine("HttpContentType: " + rs.Response.httpContentType);
            //Console.WriteLine("HttpDate: " + rs.Response.httpDate);
            //Console.WriteLine("ServerResponseCode: " + rs.Response.serverResponseCode);
            //Console.WriteLine("ServerResponseMessage: " + rs.Response.serverResponseMessage);
            //Console.WriteLine("HttpBody: " + rs.Response.httpBody);

            string sDbg = "HttpCode: " + rs.Response.httpCode.ToString() + "\r\n" +
                            "ServerResponseCode: " + rs.Response.serverResponseCode + "\r\n" +
                            "ServerResponseMessage: " + rs.Response.serverResponseMessage + "\r\n" +
                            "HttpBody: " + rs.Response.httpBody;
            LogWriter.show(this, sDbg);

            if (rs.Response.httpCode != 200)
            {
                fillLastError();
                LogWriter.show(this, "Failed to edit ");
                return false;
            }

            if (rs.Response.httpContentType == "application/json")
                res = rs.Response.httpBody;

            //object djson;
            //JObject o;
            if (res != "")
            {
                //djson = JsonConvert.DeserializeObject(res);
                if (!customerData.JSONParse(res))
                {
                    lastErr.ServerCode = "409";
                    lastErr.ServerMessage = "Invalid host message";
                    return false;
                }
                // cokot value na
                //o = jh.JSONReadObject(djson.ToString());
                try
                {
                    sDbg =
                    "ficoCustomerId =" + customerData["ficoCustomerId"] + "\r\n" +
                    "ficoCustomerName =" + customerData["ficoCustomerName"] + "\r\n" +
                    "ficoCustomerPhone =" + customerData["ficoCustomerPhone"] + "\r\n" +
                    "ficoCustomerPhone2 =" + customerData["ficoCustomerPhone2"] + "\r\n" +
                    "ficoCustomerEmail =" + customerData["ficoCustomerEmail"] + "\r\n" +
                    "ficoCustomerBirthDate =" + customerData["ficoCustomerBirthDate"] + "\r\n" +
                    "ficoCustomerBirthPlace =" + customerData["ficoCustomerBirthPlace"] + "\r\n" +
                    "ficoCustomerCity =" + customerData["ficoCustomerCity"] + "\r\n" +
                    "ficoCustomerCity2 =" + customerData["ficoCustomerCity2"] + "\r\n" +
                    "ficoCustomerAddress =" + customerData["ficoCustomerAddress"] + "\r\n" +
                    "ficoCustomerTrxAllowed =" + customerData["ficoCustomerTrxAllowed"] + "\r\n" +
                    "ficoCustomerAddress2 =" + customerData["ficoCustomerAddress2"] + "\r\n" +
                    "ficoCustomerAddress3 =" + customerData["ficoCustomerAddress3"] + "\r\n" +
                    "ficoCustomerZipCode =" + customerData["ficoCustomerZipCode"] + "\r\n" +
                    "ficoCustomerZipCode2 =" + customerData["ficoCustomerZipCode2"] + "\r\n" +
                    "ficoCustomerCustomField1 =" + customerData["ficoCustomerCustomField1"] + "\r\n" +
                    "ficoCustomerCustomField2 =" + customerData["ficoCustomerCustomField2"] + "\r\n" +
                    "ficoCustomerCustomField3 =" + customerData["ficoCustomerCustomField3"] + "\r\n" +
                    "ficoCustomerCustomField4 =" + customerData["ficoCustomerCustomField4"] + "\r\n" +
                    "ficoCustomerCustomField5 =" + customerData["ficoCustomerCustomField5"] + "\r\n" +
                    "ficoCustomerCardNumber =" + customerData["ficoCustomerCardNumber"] + "\r\n" +
                    "ficoCustomerCardIdentityType =" + customerData["ficoCustomerCardIdentityType"] + "\r\n" +
                    "ficoCustomerIdentityCardNumber =" + customerData["ficoCustomerIdentityCardNumber"] + "\r\n" +
                    "ficoCustomerIdentityCardValidDate =" + customerData["ficoCustomerIdentityCardValidDate"] + "\r\n" +
                    "ficoCustomerNpwp =" + customerData["ficoCustomerNpwp"] + "\r\n" +
                    "ficoCustomerNickname =" + customerData["ficoCustomerNickname"] + "\r\n" +
                    "ficoCustomerRef1 =" + customerData["ficoCustomerRef1"] + "\r\n" +
                    "ficoCustomerRef2 =" + customerData["ficoCustomerRef2"] + "\r\n" +
                    "ficoCustomerRef3 =" + customerData["ficoCustomerRef3"] + "\r\n" +
                    "ficoCustomerGender =" + customerData["ficoCustomerGender"] + "\r\n" +
                    "ficoCustomerMotherName =" + customerData["ficoCustomerMotherName"] + "\r\n" +
                    "ficoCustomerPassword =" + customerData["ficoCustomerPassword"] + "\r\n" +
                    "ficoCustomerUsername =" + customerData["ficoCustomerUsername"] + "\r\n" +
                    "source =" + customerData["source"] + "\r\n" +
                    "ficoCustomerGroupName =" + customerData["ficoCustomerGroupName"] + "\r\n" +
                    "ficoCustomerBankAccount =" + customerData["ficoCustomerBankAccount"] + "\r\n" +
                    "ficoCustomerStatus =" + customerData["ficoCustomerStatus"];
                    LogWriter.show(this, sDbg);

                    return true;
                }
                catch
                {
                    lastErr.ServerCode = "409";
                    lastErr.ServerMessage = "Invalid host message";
                    return false;
                }
            }
            else
            {
                fillLastError();
                LogWriter.show(this, "Gagal Json");
                return false;
            }
        }

		private string generateRandomInvoice()
		{
			const int jumlahRandom = 32;
			Random random = new Random();
			string hasil = "";
			int rd = 0;
			for (int i = 0; i < jumlahRandom; i++)
			{
				// Semua DEC
				//rd = random.Next(0, 9);
				//hasil += (char)(rd + 0x30);

				// Semua HEX
				//rd = random.Next(0, 15);
				//if (rd < 10) hasil += (char)(rd + 0x30);
				//else hasil += (char)(rd + 0x37);

				// Semua numerik dan char
				rd = random.Next(0, 35);
				if (rd < 10) hasil += (char)(rd + 0x30);
				else hasil += (char)(rd + 0x37);
			}
			return hasil;
		}

		public string CalculateMD5Hash(string input)
		{
			// step 1, calculate MD5 hash from input
			MD5 md5 = System.Security.Cryptography.MD5.Create();
			byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
			byte[] hash = md5.ComputeHash(inputBytes);

			// step 2, convert byte array to hex string
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < hash.Length; i++)
			{
				sb.Append(hash[i].ToString("X2"));
			}
			return sb.ToString();
		}

		public string GenerateInvoice(string TransferCode, string customerId, string receiverId, double amount){
			// uniqueCode + hash(PH+TransferCode+TransferCode+customerId+receiverId+amount+uniqueCode)
			string KonstantaPH = "PowerHouse2014";
			string unique = DateTime.Now.ToString ("yyyyMMddHHmmssff");
			string sadded = CalculateMD5Hash(KonstantaPH + TransferCode + customerId + receiverId + amount.ToString () + unique);
			return (unique + sadded.Substring (unique.Length));
			// MD5 :     4b2b128d0eb5a8ef66f652dca49fae3a
			// Invoice : yyyyMMddHHmmssff66f652dca49fae3a
		}

		public bool Transfer(string SandraHost, int SandraPort, 
			bool secureConnection, string ChannelId, 
            string TransferCode, string customerId, string receiverId, double amount, 
			ref string invoiceNumber, string sourceId, ref bool fQvaResponded)
        {
			// Uji coba 
			string qvaInvoice = GenerateInvoice (TransferCode, customerId, receiverId, amount); 	//generateRandomInvoice (); 
			invoiceNumber = qvaInvoice;

            clearLastErr();
			if (!RequestToken(SandraHost, SandraPort, secureConnection, TokenHostEnum.QVA))
            {
                fillLastError();
                return false;
            }

            // http://123.231.225.20:7080/switching-ppob-gateway-service/rest/gateway/040170/
            // http://123.231.225.20:7080/switching-gateway-service/rest/gateway/040170/
            //string customerId = "0000000000000995";



            string commandCode = "040170";    // regular transfer

			string httpna = "";
			if(secureConnection)
				httpna = "https://";
			else
				httpna = "http://";
			rs.httpUri = httpna + SandraHost + ":" + SandraPort.ToString() + "/switching-gateway-service/rest";
            //rs.httpUri = "http://" + SandraHost + ":" + SandraPort.ToString() + "/switching-ppob-gateway-service/rest";

            rs.canonicalPath = "/gateway/" + commandCode;
			rs.authID = sandraAuthId;
            rs.method = "POST";
            rs.contentType = "application/json";
            //rs.userAuth = "dummy-01";
            //rs.secretKey = "dummy-01";
            MyJH.Clear();
            MyJH.Add("sender", customerId);
            MyJH.Add("receiver", receiverId);
            MyJH.Add("channelId", ChannelId);
            MyJH.Add("transactionCode", TransferCode);
			MyJH.Add("source",sourceId);
			MyJH.Add("amount",amount);
			if(qvaInvoice != "")
				MyJH.Add("ficoInvoice",qvaInvoice);
			qvaInvoice = "";

            rs.bodyMessage = MyJH.JSONConstruct();

			LogWriter.write(this, LogWriter.logCodeEnum.INFO, 
				"Send to: " + rs.httpUri + "\r\n"
				+ "Canonical: " + rs.canonicalPath + "\r\n"
				+ "AuthId: " + rs.authID + "\r\n"
				+ "Method: " + rs.method + "\r\n"
				+ "Body" + rs.bodyMessage
				);


			string res = "";
			if(useTcp)
				res = rs.TCPRestSendRequest(recTimeOut);
			else
				res = rs.HttpRestSendRequest(recTimeOut);

            if ((res == "") && ((rs.Response.httpMessage == null) || (rs.Response.httpCode == 0)))
            {
				fQvaResponded = false;
                lastErr.ServerCode = "Z" + "999";
				lastErr.ServerMessage = "No response from Sandra Host, should do reversal";
                return false;
            }
			fQvaResponded = true;

            //Console.WriteLine("HttpCode: " + rs.Response.httpCode.ToString());
            //Console.WriteLine("HttpMessage: " + rs.Response.httpMessage);
            //Console.WriteLine("HttpContentType: " + rs.Response.httpContentType);
            //Console.WriteLine("HttpDate: " + rs.Response.httpDate);
            //Console.WriteLine("ServerResponseCode: " + rs.Response.serverResponseCode);
            //Console.WriteLine("ServerResponseMessage: " + rs.Response.serverResponseMessage);
            //Console.WriteLine("HttpBody: " + rs.Response.httpBody);

            string sDbg = "HttpCode: " + rs.Response.httpCode.ToString() + "\r\n" +
                            "ServerResponseCode: " + rs.Response.serverResponseCode + "\r\n" +
                            "ServerResponseMessage: " + rs.Response.serverResponseMessage + "\r\n" +
                            "HttpBody: " + rs.Response.httpBody;
			LogWriter.write(this, LogWriter.logCodeEnum.INFO, sDbg);

            if (rs.Response.httpCode != 200)
            {
                fillLastError();
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed!\r\n" + sDbg);
                return false;
            }

			if (rs.Response.httpContentType == "application/json")
				res = rs.Response.httpBody;
			else {
				res = "";
			}

            if (res != "")
            {
                MyJH.Clear();
                if (!MyJH.JSONParse(res))
                {
                    // gagal parsing json dari sandra
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Bad JSON format from host");
                    lastErr.ServerCode = "Z999";
                    lastErr.ServerMessage = "Bad JSON format from host";
					fQvaResponded = false;
                    return false;
                }

                // cokot value na
				//string qvaInvoice = "";
                string transactionTime = "";
                try
                {
                    if (MyJH.ContainsKey("ficoInvoice"))
                    {
                        qvaInvoice = (string)MyJH["ficoInvoice"];
                        invoiceNumber = qvaInvoice;
                    }
                    else
                    {
                        lastErr.ServerCode = "Z" + "999";
                        lastErr.ServerMessage = "No invoice number from QVA Host";
						return false;
                    }
                    if (MyJH.ContainsKey("ficoTransactionTime"))
                        transactionTime = (string)MyJH["ficoTransactionTime"];
                    else
                    {
                        lastErr.ServerCode = "Z" + "999";
                        lastErr.ServerMessage = "No transaction time from QVA Host";
						return false;
                    }

                    sDbg = 
                        "Transfer : ficoInvoice : " + qvaInvoice + ", ficoTransactionTime: " + transactionTime;
                    LogWriter.show(this, sDbg);
                    
                    //balance = double.Parse( MyJH["ficoCustomerBalance"].ToString());
                    return true;
                }
                catch
                {
					fQvaResponded = false;
                    // gagal parsing json dari sandra
					LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Fields not found from host, should reversal to QVA");
                    lastErr.ServerCode = "Z999";
                    lastErr.ServerMessage = "Uncompleted field from host";
                    return false;
                }
            }
            else
            {
				fQvaResponded = false;
                fillLastError();
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Gagal parsing Json dari qva");
                return false;
            }
        }

		public bool Reversal(string SandraHost, int SandraPort, 
			bool secureConnection, string invoiceNumber)
		{
			bool fQvaResponded = false;

			clearLastErr();
			if (!RequestToken(SandraHost, SandraPort, secureConnection, TokenHostEnum.QVA))
			{
				fillLastError();
				return false;
			}

			// http://123.231.225.20:7080/switching-ppob-gateway-service/rest/gateway/040140/
			// http://123.231.225.20:7080/switching-gateway-service/rest/gateway/040140/
			//string customerId = "0000000000000995";


			string commandCode = "040140";    // reversal /gateway/040140

			string httpna = "";
			if(secureConnection)
				httpna = "https://";
			else
				httpna = "http://";
			rs.httpUri = httpna + SandraHost + ":" + SandraPort.ToString() + "/switching-gateway-service/rest";
			//rs.httpUri = "http://" + SandraHost + ":" + SandraPort.ToString() + "/switching-ppob-gateway-service/rest";

			rs.canonicalPath = "/gateway/" + commandCode;
			rs.authID = sandraAuthId;
			rs.method = "POST";
			rs.contentType = "application/json";
			//rs.userAuth = "dummy-01";
			//rs.secretKey = "dummy-01";
			MyJH.Clear();
			MyJH.Add("ficoInvoice", invoiceNumber);

			rs.bodyMessage = MyJH.JSONConstruct();

			LogWriter.write(this, LogWriter.logCodeEnum.INFO, 
				"Send to: " + rs.httpUri + "\r\n"
				+ "Canonical: " + rs.canonicalPath + "\r\n"
				+ "AuthId: " + rs.authID + "\r\n"
				+ "Method: " + rs.method + "\r\n"
				+ "Body" + rs.bodyMessage
			);


			string res = "";
			if(useTcp)
				res = rs.TCPRestSendRequest(recTimeOut);
			else
				res = rs.HttpRestSendRequest(recTimeOut);

			if ((res == "") && ((rs.Response.httpMessage == null) || (rs.Response.httpCode == 0)))
			{
				lastErr.ServerCode = "Z" + "999";
				lastErr.ServerMessage = "No response from Sandra Host";
				fQvaResponded = false;
				return false;
			}
			fQvaResponded = true;

			//Console.WriteLine("HttpCode: " + rs.Response.httpCode.ToString());
			//Console.WriteLine("HttpMessage: " + rs.Response.httpMessage);
			//Console.WriteLine("HttpContentType: " + rs.Response.httpContentType);
			//Console.WriteLine("HttpDate: " + rs.Response.httpDate);
			//Console.WriteLine("ServerResponseCode: " + rs.Response.serverResponseCode);
			//Console.WriteLine("ServerResponseMessage: " + rs.Response.serverResponseMessage);
			//Console.WriteLine("HttpBody: " + rs.Response.httpBody);

			string sDbg = "HttpCode: " + rs.Response.httpCode.ToString() + "\r\n" +
			              "ServerResponseCode: " + rs.Response.serverResponseCode + "\r\n" +
			              "ServerResponseMessage: " + rs.Response.serverResponseMessage + "\r\n" +
			              "HttpBody: " + rs.Response.httpBody;
			LogWriter.write(this, LogWriter.logCodeEnum.INFO, sDbg);

			if (rs.Response.httpCode != 200)
			{
				fillLastError();
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed!\r\n" + sDbg);
				return false;
			}

			if (rs.Response.httpContentType == "application/json")
				res = rs.Response.httpBody;
			else {
				res = "";
			}

			if (res != "")
			{
				MyJH.Clear();
				if (!MyJH.JSONParse(res))
				{
					// gagal parsing json dari sandra
					LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Bad JSON format from host");
					lastErr.ServerCode = "Z999";
					lastErr.ServerMessage = "Bad JSON format from host";
					return false;
				}

				// cokot value na
				//string qvaInvoice = "";
				string clientType = "";
				try
				{
					if (MyJH.ContainsKey("ficoInvoice"))
					{
						invoiceNumber = (string)MyJH["ficoInvoice"];
					}
//					else
//					{
//						lastErr.ServerCode = "Z" + "999";
//						lastErr.ServerMessage = "No invoice number from QVA Host";
//						return false;
//					}
					if (MyJH.ContainsKey("ficoClientType"))
						clientType = (string)MyJH["ficoClientType"];

					sDbg = 
						"Reversal : ficoInvoice : " + invoiceNumber + ", ficoClientType: " + clientType;
					LogWriter.show(this, sDbg);

					//balance = double.Parse( MyJH["ficoCustomerBalance"].ToString());
					return true;
				}
				catch
				{
					// gagal parsing json dari sandra
					LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Fields not found from host");
					lastErr.ServerCode = "Z999";
					lastErr.ServerMessage = "Uncompleted field from host";
					return false;
				}
			}
			else
			{
				fQvaResponded = false;
				fillLastError();
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Gagal parsing Json dari qva");
				return false;
			}
		}
    }
}
