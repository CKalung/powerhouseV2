using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPOBServerHandler;
using PPOBHttpRestData;
using LOG_Handler;
using Payment_Host_Interface;

namespace PersadaHandler
{
    public class PersadaTransactions : ITransactionInterface
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
        ~PersadaTransactions()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            rs.Dispose();
            //jh.Dispose();
            MyJH.Dispose();
            HTTPRestDataConstruct.Dispose();
            localDB.Dispose();
        }

        private RestService rs;
        //private JsonLibs.JSONHandler jh;
        private JsonLibs.MyJsonLib MyJH;
        //int systemTrxIdNum = 0;
        HTTPRestConstructor HTTPRestDataConstruct;
        PPOBDatabase.PPOBdbLibs localDB;
        Exception xError;

        public string securityToken = "";

        //public struct lastError
        //{
        //    public string ServerCode;
        //    public string Serve.StackTrace;
        //}
        //lastError lastErr;

        //private void clearLastErr()
        //{
        //    lastErr.ServerCode = "";
        //    lastErr.Serve.StackTrace = "";
        //}

        PublicSettings.Settings commonSettings;

        public PersadaTransactions(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            MyJH = new JsonLibs.MyJsonLib();
            rs = new RestService();
            HTTPRestDataConstruct = new HTTPRestConstructor();
            localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
                commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
        }

        private void fillLastError(SandraLibs sandra)
        {
            if (rs.Response.serverResponseCode == null)
            {
                sandra.lastErr.ServerCode = "z888";
                sandra.lastErr.ServerMessage = "Unconditional error";
                return;
            }
            if (rs.Response.serverResponseCode != "")
            {
                sandra.lastErr.ServerCode = rs.Response.serverResponseCode;
                sandra.lastErr.ServerMessage = rs.Response.serverResponseMessage;
            }
            else
            {
                if (rs.Response.httpMessage != "")
                {
                    sandra.lastErr.ServerCode = "Z" + rs.Response.httpCode.ToString();
                    sandra.lastErr.ServerMessage = rs.Response.httpMessage;
                }
                else
                {
                    sandra.lastErr.ServerCode = "Z777";
                    sandra.lastErr.ServerMessage = "No error message";
                }
            }
        }

        // fungsi untuk pembelian pulsa lewat persada
        public bool productTransaction(string appID, string userId, string transactionReference, string providerProductCode,
            string providerAmount, ref string HttpReply, ref int traceNumber,
            ref string strJson, ref DateTime trxTime, ref string strRecJson, ref DateTime trxRecTime,
            ref string failedReason, ref bool canReversal, ref bool isSuccessPayment, int transactionType,
            string trxNumber)
        //public bool productTransaction(string customerNumber, string providerProductCode)
        {
            isSuccessPayment = false;
            canReversal = false;
			using (SandraLibs sandra = new SandraLibs(commonSettings.getInt("BillerPersadaTimeOut"),
				(commonSettings.getString("SandraUseTcpMethod").Trim().ToLower() == "true"),
				commonSettings.getString("SandraHmsUserAuth"),
				commonSettings.getString("SandraHmsSecretKey"),
				commonSettings.getString("SandraAuthId")))
            {
                sandra.clearLastErr();
                trxTime = DateTime.Now;
                //balance = 0;
				if (!sandra.RequestToken(commonSettings.getString("SandraHost"), 
					commonSettings.getInt("SandraPort"), 
					(commonSettings.getString("SandraIsHttps").ToLower() == "true"), 
					SandraLibs.TokenHostEnum.PERSADA))
                {
                    sandra.fillLastError();
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Get token failed");
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Get token failed", "");
                    strRecJson = ""; trxRecTime = trxTime;
                    failedReason = "Failed while get token from HMS, " + commonSettings.getString("SandraHost") + ":" + commonSettings.getInt("SandraPort");
                    return false;
                }
                rs.userAuth = sandra.getSessionUserAuth;
                rs.secretKey = sandra.getSessionSecretKey;

                string hostId = "persada";
                string sTransactionType = "purchase";
                if (transactionType == 0) sTransactionType = "payment";
                else sTransactionType = "purchase";
                //systemTrxIdNum++;
                //string systemTrxId = systemTrxIdNum.ToString().PadLeft(12, '0'); // di generate (numeric(12))  harus beda tiap transaksi contoh : "000000000001"
                //traceNumber = StaticCommonLibrary.CommonLibrary.getNextProductTraceNumber();
                traceNumber = localDB.getNextProductTraceNumber();
                string systemTrxId = traceNumber.ToString().PadLeft(12, '0');

                // http://123.231.225.20:7080/switching-ppob-gateway-service/rest/            
                rs.httpUri = "http://" + commonSettings.getString("SandraHost") + ":" + 
                    commonSettings.getInt("SandraPort").ToString() + "/switching-ppob-gateway-service/rest";
                rs.canonicalPath = "/gateway/010110";
                rs.authID = "DA01";
                rs.method = "POST";
                rs.contentType = "application/json";
                rs.bodyMessage = "{\"customer\":\"" + transactionReference + "\",\"product\":\"" + 
                    providerProductCode + "\",\"hostId\":\"" + 
                    hostId + "\",\"transactionType\":\"" + 
                    sTransactionType + "\",\"systemTrxId\":\"" + systemTrxId + "\"}";
                strJson = rs.bodyMessage;

                string sDbg = "httpUri = " + rs.httpUri + "\r\n" + 
                                "canonicalPath = " + rs.canonicalPath + "\r\n" + 
                                "authID = " + rs.authID + "\r\n" + 
                                "method = " + rs.method + "\r\n" + 
                                "contentType = " + rs.contentType + "\r\n" + 
                                "bodyMessage = " + rs.bodyMessage;
                LogWriter.show(this,"KIRIM KE SANDRA HOST\r\n"+sDbg);

                string res = rs.TCPRestSendRequest(commonSettings.getInt("BillerPersadaTimeOut"));
                

                if ((res == "") && ((rs.Response.httpMessage == null) || (rs.Response.httpCode == 0)))
                {
                    sandra.lastErr.ServerCode = "Z" + "999";
                    sandra.lastErr.ServerMessage = "No response from Sandra Host";
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, sandra.lastErr.ServerMessage);
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, sandra.lastErr.ServerCode, sandra.lastErr.ServerMessage, "");
                    strRecJson = ""; trxRecTime = trxTime;
                    failedReason = "No Json data from Sandra and null http code";
                    return false;
                }

                sDbg = "HttpCode: " + rs.Response.httpCode.ToString() + "\r\n" +
                        "HttpMessage: " + rs.Response.httpMessage + "\r\n" +
                        "HttpContentType: " + rs.Response.httpContentType + "\r\n" +
                        "HttpDate: " + rs.Response.httpDate + "\r\n" +
                        "ServerResponseCode: " + rs.Response.serverResponseCode + "\r\n" +
                        "ServerResponseMessage: " + rs.Response.serverResponseMessage + "\r\n" +
                        "HttpBody: " + rs.Response.httpBody;
                LogWriter.show(this, "TERIMA DARI SANDRA HOST\r\n" + sDbg);

                if (rs.Response.serverResponseCode != "00")
                {
                    //sandra.fillLastError();
                    fillLastError(sandra);
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, sandra.lastErr.ServerMessage);
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, sandra.lastErr.ServerCode, sandra.lastErr.ServerMessage, "");
                    strRecJson = ""; trxRecTime = trxTime;
                    failedReason = "Failed, Sandra serverCode : " + sandra.lastErr.ServerCode + ", " + sandra.lastErr.ServerMessage;
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
                        sandra.lastErr.ServerCode = "Z999";
                        sandra.lastErr.ServerMessage = "Bad JSON format from host";
                        LogWriter.write(this, LogWriter.logCodeEnum.ERROR, sandra.lastErr.ServerMessage);
                        HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, sandra.lastErr.ServerCode, sandra.lastErr.ServerMessage, "");
                        strRecJson = ""; trxRecTime = trxTime;
                        failedReason = "Success on Sandra serverCode \"00\", but bad Json data : " + res;
                        return false;
                    }

                    try
                    {
                        sDbg = "Response Purchase Pulsa Persada:" + "\r\n" +
                                "persadaTrxDate : " + MyJH["persadaTrxDate"] + "\r\n" +
                                "persadaAmount : " + MyJH["persadaAmount"] + "\r\n" +
                                "product : " + MyJH["product"] + "\r\n" +
                                "hostId : " + MyJH["hostId"] + "\r\n" +
                                "transactionType : " + MyJH["transactionType"] + "\r\n" +
                                "systemTrxId : " + MyJH["systemTrxId"] + "\r\n" +
                                "persadaTrxStatus : " + MyJH["persadaTrxStatus"];
                        LogWriter.showDEBUG(this,sDbg);

                        string fiToken = "TMPFIXED";
                        string trxStatus = (string)MyJH["persadaTrxStatus"];
                        string trxMsg = (string)MyJH["persadaTrxMessage"];
                        string[] trxMsgs = trxMsg.Split(':');
                        string trxSysTrxId = (string)MyJH["systemTrxId"];
                        string trxDate = (string)MyJH["persadaTrxDate"];
                        string trxStat = trxMsg;
                        //string trxNumber = localDB.getProductTrxNumber(out xError);

                        if (trxStatus == "00")
                        {
                            trxStat = trxMsgs[1];
                            isSuccessPayment = true;
                        }
                        canReversal = false;

                        MyJH.Clear();
                        MyJH.Add("fiToken", securityToken);
                        MyJH.Add("fiPrivateData", transactionReference);
                        MyJH.Add("fiResponseCode", trxStatus);
                        MyJH.Add("fiTransactionId", trxStat);
                        MyJH.Add("fiToken", fiToken);
                        MyJH.Add("fiTrxNumber", trxNumber);
                        MyJH.Add("fiReversalAllowed", false);

                        HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", MyJH.JSONConstruct());
                        strRecJson = strJson; trxRecTime = DateTime.Now;
                        LogWriter.write(this, LogWriter.logCodeEnum.INFO, "Transaction to Persada : " +
                            "Status: " + trxStatus + ", TrxPersadaTime: " + trxDate +
                            ", SystemTrxId: " + trxSysTrxId + ", Message: " + trxMsg);
                        failedReason = "";
                        return true;
                    }
                    catch(Exception ex)
                    {
                        // gagal parsing json dari sandra
                        LogWriter.showDEBUG(this,"Field not completed from Sandra host");
                        sandra.lastErr.ServerCode = "Z999";
                        sandra.lastErr.ServerMessage = "Uncompleted field from host";
                        LogWriter.write(this, LogWriter.logCodeEnum.ERROR,
                            sandra.lastErr.ServerMessage);
                        HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400,
                            sandra.lastErr.ServerCode, sandra.lastErr.ServerMessage, "");
                        strRecJson = res; trxRecTime = DateTime.Now;
                        failedReason = "May be success on transaction, but failed on getting Persada json field from sandra : " + ex.Message + "\r\n" + ex.StackTrace;
                        return false;
                    }
                }
                else
                {
                    //sandra.fillLastError();
                    fillLastError(sandra);
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "No JSon from Sandra - " + sandra.lastErr.ServerMessage);
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, sandra.lastErr.ServerCode, sandra.lastErr.ServerMessage, "");
                    strRecJson = ""; trxRecTime = trxTime;
                    failedReason = "No Persada JSon from Sandra";
                    return false;
                }
            }
        }

        public bool productInquiry(string appID, string userId, string customerNumber,
            string productCode, int adminFee, bool fIncludeAdminFee, ref int productAmount, ref string HttpReply,
            ref int traceNumber, ref string strJson, ref DateTime trxTime, ref string strRecJson,
            ref DateTime trxRecTime, string trxNumber)
        {
            HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "Not impemented", "");
            return false;
        }

    }
}
