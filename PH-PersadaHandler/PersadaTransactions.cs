using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPOBHttpRestData;
using LOG_Handler;
using Payment_Host_Interface;
using StaticCommonLibrary;

namespace PH_PersadaHandler
{
    public class PersadaTransactions: ITransactionInterface
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
            pm.Dispose();
            HTTPRestDataConstruct.Dispose();
            jsonConv.Dispose();
            tcp.Dispose();
            localDB.Dispose();
        }

        PersadaModule pm;
        TCPPersadaClient tcp;        
        HTTPRestConstructor HTTPRestDataConstruct;
        JsonLibs.MyJsonLib jsonConv;
        PublicSettings.Settings commonSettings;
        PPOBDatabase.PPOBdbLibs localDB;
        Exception xError;

        public string securityToken = "";

        public PersadaTransactions(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            LOG_Handler.LogWriter.showDEBUG(this, "Persada > "
                + commonSettings.getString("PersadaQueueHost") + ":"
                + commonSettings.getInt("PersadaQueuePort"));
            tcp = new TCPPersadaClient(commonSettings.getString("PersadaQueueHost"),
                commonSettings.getInt("PersadaQueuePort"));
            jsonConv = new JsonLibs.MyJsonLib();
            HTTPRestDataConstruct = new HTTPRestConstructor();
            pm = new PersadaModule(commonSettings);
            //localDB = new PPOBDatabase.PPOBdbLibs(commonSettings);
            localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
                commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
        }


        public bool productInquiry(string appID, string userId, string customerNumber,
            string productCode, int adminFee, bool fIncludeAdminFee, ref int productAmount, ref string HttpReply,
            ref int traceNumber, ref string strJson, ref DateTime trxTime, ref string strRecJson,
            ref DateTime trxRecTime, string trxNumber)
        {
            HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "Not impemented", "");
            return false;
        }

        // fungsi untuk pembelian pulsa lewat persada
        public bool productTransaction(string appID, string userId, string transactionReference,
            string providerProductCode, string providerAmount, ref string HttpReply, ref int traceNumber,
            ref string strJson, ref DateTime trxTime, ref string strRecJson, ref DateTime trxRecTime,
            ref string failedReason, ref bool canReversal, ref bool isSuccessPayment, int transactionType,
            string trxNumber)
        {
            int isoType = 3;

            trxTime = DateTime.Now;
            traceNumber = localDB.getNextProductTraceNumber();
            //string systemTrxId = traceNumber.ToString().PadLeft(12, '0');

            failedReason = "";
            canReversal = false;
            isSuccessPayment = false;

            //string sRecIso = "";
            //string productCode = "PRE25";
            //long reffNum = localDB.getNextProductReferenceNumber();
            long reffNum = traceNumber;

            // 1. create ISO MSG
            byte[] iso = pm.generateTransactionJson(transactionReference, providerProductCode,
                reffNum, ref strJson);

            try
            {
                tcp.CheckConn();
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Connect to Persada Queue host has failed : " + ex.getCompleteErrMsg());
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Connect to Persada Queue host has failed", "");
                strRecJson = ""; trxRecTime = trxTime;
                failedReason = "Failed to connect to Persada Queue host";
                return false;
            }

            //Console.WriteLine("DEBUG== Send Msg");
            try
            {
                // 2. Send ISO Msg
                tcp.Send(iso);
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error while send to Persada Queue host : " + ex.getCompleteErrMsg());
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Disconnected from Persada Queue host", "");
                strRecJson = ""; trxRecTime = trxTime;
                failedReason = "Failed while send the.StackTrace to Persada Queue host : " + ex.getCompleteErrMsg();
                return false;
            }

            canReversal = true;

            LogWriter.show(this, "Reading ISO");
            DateTime skrg = DateTime.Now;

            string fiToken = "TMPFIXED";
            //string trxNumber = localDB.getProductTrxNumber(out xError);

            // 3. Read Balasan ISO Msg
            byte[] ret;
            string sRet = "";
            try
            {
                ret = tcp.Read(commonSettings.getInt("BillerPersadaTimeOut"), ref sRet);
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error while read from Persada Queue host : " + ex.getCompleteErrMsg());
                if ((isoType == 3) && (transactionType == 0) && canReversal)
                {
                    jsonConv.Clear();
                    jsonConv.Add("fiToken", securityToken);
                    jsonConv.Add("fiPrivateData", "Need reversal");
                    jsonConv.Add("fiResponseCode", "99");
                    jsonConv.Add("fiTransactionId", "PD1" + traceNumber.ToString().PadLeft(6, '0'));
					//jsonConv.Add("fiToken", fiToken);
                    jsonConv.Add("fiTrxNumber", trxNumber);
                    // jika transaksi pembayaran, pake return fiReversalAllowed
                    if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "No response from Persada Queue host",
                        jsonConv.JSONConstruct());
                }
                else
                {
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "No response from Persada Queue host", "");
                }
                strRecJson = sRet; trxRecTime = skrg;
                failedReason = "Failed while read data from Persada queue host : " + ex.getCompleteErrMsg();
                return false;
            }
            skrg = DateTime.Now;
            strRecJson = sRet; trxRecTime = skrg;

            if (ret == null)
            {
                //Console.WriteLine("No response from provider");
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "No response from Persada Queue host");
                if ((isoType == 3) && (transactionType == 0) && canReversal)
                {
                    jsonConv.Clear();
                    jsonConv.Add("fiToken", securityToken);
                    jsonConv.Add("fiPrivateData", "Need reversal");
                    jsonConv.Add("fiResponseCode", "99");
                    jsonConv.Add("fiTransactionId", "PD1" + traceNumber.ToString().PadLeft(6, '0'));
					//jsonConv.Add("fiToken", fiToken);
                    jsonConv.Add("fiTrxNumber", trxNumber);
                    // jika transaksi pembayaran, pake return fiReversalAllowed
                    if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "No response from Persada Queue host",
                        jsonConv.JSONConstruct());
                }
                else
                {
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "No response from Persada Queue host", "");
                }
                failedReason = "Failed to process, return from Persada queue host is null";
                return false;
            }


            // 4. Check Data Signature apakah sama ?
            //if (!pm.CheckDataSignature(strRecJson, isoType))
            //{
            //    //Console.WriteLine("Signature beda");
            //    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Signature not match");
            //    if ((isoType == 3) && (transactionType == 0) && canReversal)
            //    {
            //        jsonConv.Clear();
            //        jsonConv.Add("fiToken", securityToken);
            //        jsonConv.Add("fiPrivateData", "Need reversal");
            //        jsonConv.Add("fiResponseCode", "99");
            //        jsonConv.Add("fiTransactionId", "PD1" + traceNumber.ToString().PadLeft(6, '0'));
			//        //jsonConv.Add("fiToken", fiToken);
            //        jsonConv.Add("fiTrxNumber", trxNumber);
            //        // jika transaksi pembayaran, pake return fiReversalAllowed
            //        if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

            //        HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Error signature code",
            //            jsonConv.JSONConstruct());
            //    }
            //    else
            //    {
            //        HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "Error signature code", "");
            //    }
            //    failedReason = "Data signature not match with Finnet queue host";
            //    return false;
            //}
            //Console.WriteLine("Signature Sarua");

            if (!jsonConv.JSONParse(System.Text.Encoding.UTF8.GetString(ret)))
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Could not parse data from host");
                if ((isoType == 3) && (transactionType == 0) && canReversal)
                {
                    jsonConv.Clear();
                    jsonConv.Add("fiToken", securityToken);
                    jsonConv.Add("fiPrivateData", "Need reversal");
                    jsonConv.Add("fiResponseCode", "99");
                    jsonConv.Add("fiTransactionId", "PD1" + traceNumber.ToString().PadLeft(6, '0'));
					//jsonConv.Add("fiToken", fiToken);
                    jsonConv.Add("fiTrxNumber", trxNumber);
                    // jika transaksi pembayaran, pake return fiReversalAllowed
                    if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Invalid data from host",
                        jsonConv.JSONConstruct());
                }
                else
                {
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "Invalid data from host", "");
                }
                failedReason = "Could not parse Json data from Persada queue host : " + System.Text.Encoding.UTF8.GetString(ret);
                return false;
            }

            //string fiAmount = ((string)jsonConv["Amount"]).Trim();
            string fiPrivateData = transactionReference;
            string fiResponseCode = ((string)jsonConv["RESPONSE_CODE"]).Trim();

            canReversal = false;
            // Jika response code ??, artinya reversal sebelumnya sudah sukses.
            if (fiResponseCode == "00") isSuccessPayment = true;

            if (fiPrivateData == "") fiPrivateData = "..";
            jsonConv.Clear();
            jsonConv.Add("fiToken", securityToken);
            jsonConv.Add("fiPrivateData", fiPrivateData);
            jsonConv.Add("fiResponseCode", fiResponseCode);
            jsonConv.Add("fiTransactionId", "PRD" + traceNumber.ToString());
			//jsonConv.Add("fiToken", fiToken);
            jsonConv.Add("fiTrxNumber", trxNumber);
            // jika transaksi pembayaran, pake return fiReversalAllowed
            if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

            HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
            LogWriter.write(this, LogWriter.logCodeEnum.INFO, "Transaction success to Persada : " + "PRD" + traceNumber.ToString());
            return true;

        }

    }
}
