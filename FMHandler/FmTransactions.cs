using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Payment_Host_Interface;
using PPOBHttpRestData;
using LOG_Handler;
using StaticCommonLibrary;

namespace FMHandler
{
    public class FmTransactions : ITransactionInterface
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
        ~FmTransactions()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            try
            { fm.Dispose(); }
            catch { //LogWriter.showDEBUG(this, "ASUP EX1"); 
            }
            try
            { HTTPRestDataConstruct.Dispose(); }
            catch { //LogWriter.showDEBUG(this, "ASUP EX2"); 
            }
            try
            { jsonConv.Dispose(); }
            catch { //LogWriter.showDEBUG(this, "ASUP EX3"); 
            }
            try
            { tcp.Dispose(); }
            catch { //LogWriter.showDEBUG(this, "ASUP EX4"); 
            }
            try
            { localDB.Dispose(); }
            catch { //LogWriter.showDEBUG(this, "ASUP EX5"); 
            }
        }

        FMModule fm;
        HostTCPClient tcp;        // sementara
        HTTPRestConstructor HTTPRestDataConstruct;
        JsonLibs.MyJsonLib jsonConv;
        PublicSettings.Settings commonSettings;
        PPOBDatabase.PPOBdbLibs localDB;
        Exception xError;

        public string securityToken = "";

        public FmTransactions(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            tcp = new HostTCPClient(commonSettings.getString("FmQueueHost"), commonSettings.getInt("FmQueuePort"));
            jsonConv = new JsonLibs.MyJsonLib();
            HTTPRestDataConstruct = new HTTPRestConstructor();
            fm = new FMModule(commonSettings);
            localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
                commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
        }

        public bool productTransactionReversal(string appID, string userId, string transactionReference, string providerProductCode,
            string providerAmount, ref string HttpReply, ref int traceNumber,
            ref string strJson, ref DateTime trxTime, ref string strRecJson, ref DateTime trxRecTime,
            ref string failedReason, int isoType, ref bool canReversal, ref bool isSuccessReversal,
            int transactionType, string trxNumber)
        {
            failedReason = "";
            string ncode = "";
            isSuccessReversal = false;
            canReversal = false;
            // isoType = 3 ==> transaksi
            ncode = "000";

            fm.trxAmount = providerAmount.PadLeft(12, '0');

            fm.bit48 = transactionReference;    //.Trim();

            fm.bit2 = providerProductCode;

            // 1. create ISO MSG
            //traceNumber = StaticCommonLibrary.CommonLibrary.getNextProductTraceNumber();
            traceNumber = localDB.getNextProductTraceNumber();
            long refNum = localDB.getNextProductReferenceNumber();
            byte[] iso = fm.generateTransactionJson(ncode, isoType, traceNumber,
                refNum,
                userId, "6014", commonSettings.getString("CommonTerminalID"), commonSettings.getString("CommonMerchantID"), 
                ref strJson, ref trxTime);

            if (iso == null) return false;

            try
            {
                tcp.CheckConn();
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Connect to FM Queue host has failed : " + ex.getCompleteErrMsg());
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Connect to FM Queue host has failed", "");
                strRecJson = ""; trxRecTime = trxTime;
                failedReason = "Failed to connect to FM Queue host";
                return false;
            }

            try
            {
                // 2. Send ISO Msg
                tcp.Send(iso);
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error while send to FM Queue host : " + ex.getCompleteErrMsg());
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Disconnected from FM Queue host", "");
                strRecJson = ""; trxRecTime = trxTime;
                failedReason = "Failed while send the.StackTrace to FM Queue host : " + ex.getCompleteErrMsg();
                return false;
            }

            canReversal = true;

            // 3. Read Balasan ISO Msg
            LogWriter.show(this, "DEBUG== Reading ISO");
            DateTime skrg = DateTime.Now;

            string fiToken = "TMPFIXED";
            //string trxNumber = localDB.getProductTrxNumber(out xError);

            // 3. Read Balasan ISO Msg
            byte[] ret;
            string sRet = "";
            try
            {
                //ret = tcp.Read(1210, ref sRet);
                ret = tcp.Read(commonSettings.getInt("BillerFmTimeOut"), ref sRet);
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error while read from FM Queue host : " + ex.getCompleteErrMsg());
                if ((isoType == 3) && (transactionType == 0) && canReversal)
                {
                    jsonConv.Clear();
                    jsonConv.Add("fiToken", securityToken);
                    jsonConv.Add("fiPrivateData", "Need reversal");
                    jsonConv.Add("fiResponseCode", "99");
                    jsonConv.Add("fiTransactionId", "FM1" + traceNumber.ToString().PadLeft(6, '0'));
                    jsonConv.Add("fiToken", fiToken);
                    jsonConv.Add("fiTrxNumber", trxNumber);
                    // jika transaksi pembayaran, pake return fiReversalAllowed
                    if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "No response from FM Queue host", 
                        jsonConv.JSONConstruct());
                }
                else
                {
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "No response from FM Queue host", "");
                }
                strRecJson = sRet; trxRecTime = skrg;
                failedReason = "Failed while read data from FM queue host : " + ex.Message + "\r\n" + ex.StackTrace;
                return false;
            }
            skrg = DateTime.Now;
            strRecJson = sRet; trxRecTime = skrg;

            if (ret == null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "No response from FM Queue host");
                if ((isoType == 3) && (transactionType == 0) && canReversal)
                {
                    jsonConv.Clear();
                    jsonConv.Add("fiToken", securityToken);
                    jsonConv.Add("fiPrivateData", "Need reversal");
                    jsonConv.Add("fiResponseCode", "99");
                    jsonConv.Add("fiTransactionId", "FM1" + traceNumber.ToString().PadLeft(6, '0'));
                    jsonConv.Add("fiToken", fiToken);
                    jsonConv.Add("fiTrxNumber", trxNumber);
                    // jika transaksi pembayaran, pake return fiReversalAllowed
                    //if ((isoType == 3) && (transactionType == 0)) 
                    jsonConv.Add("fiReversalAllowed", canReversal);

                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Null response from FM Queue host", jsonConv.JSONConstruct());
                }
                else
                {
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "No response from FM Queue host", "");
                }
                failedReason = "Failed to process, return from FM queue host is null";
                return false;
            }

            // 4. Check Data Signature apakah sama ?
            if (!fm.CheckDataSignature(strRecJson, isoType))
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Signature not match with FM queue host");
                if ((isoType == 3) && (transactionType == 0) && canReversal)
                {
                    jsonConv.Clear();
                    jsonConv.Add("fiToken", securityToken);
                    jsonConv.Add("fiPrivateData", "Need reversal");
                    jsonConv.Add("fiResponseCode", "99");
                    jsonConv.Add("fiTransactionId", "FM1" + traceNumber.ToString().PadLeft(6, '0'));
                    jsonConv.Add("fiToken", fiToken);
                    jsonConv.Add("fiTrxNumber", trxNumber);
                    // jika transaksi pembayaran, pake return fiReversalAllowed
                    if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Signature error with FM Queue host", jsonConv.JSONConstruct());
                }
                else
                {
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "Error signature code", "");
                }
                failedReason = "Data signature not match with FM queue host";
                return false;
            }

            if (!jsonConv.JSONParse(System.Text.Encoding.UTF8.GetString(ret)))
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Could not parse data from host");
                if ((isoType == 3) && (transactionType == 0) && canReversal)
                {
                    jsonConv.Clear();
                    jsonConv.Add("fiToken", securityToken);
                    jsonConv.Add("fiPrivateData", "Need reversal");
                    jsonConv.Add("fiResponseCode", "99");
                    jsonConv.Add("fiTransactionId", "FM1" + traceNumber.ToString().PadLeft(6, '0'));
                    jsonConv.Add("fiToken", fiToken);
                    jsonConv.Add("fiTrxNumber", trxNumber);
                    // jika transaksi pembayaran, pake return fiReversalAllowed
                    if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Invalid data from host", jsonConv.JSONConstruct());
                }
                else
                {
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "Invalid data from host", "");
                }
                failedReason = "Could not parse Json data from Finnet queue host : " + System.Text.Encoding.UTF8.GetString(ret);
                return false;
            }

            string fiAmount = ((string)jsonConv["Amount"]).Trim();
            string fiPrivateData = ((string)jsonConv["Bit48"]).Trim();
            string fiResponseCode = ((string)jsonConv["ResponseCode"]).Trim();

            canReversal = false;
            // Jika response code 94, artinya reversal sebelumnya sudah sukses.
            if ((fiResponseCode == "00") || (fiResponseCode == "94")) isSuccessReversal = true;

            if (fiPrivateData == "") fiPrivateData = "..";
            jsonConv.Clear();
            jsonConv.Add("fiToken", securityToken);
            jsonConv.Add("fiPrivateData", fiPrivateData);
            jsonConv.Add("fiResponseCode", fiResponseCode);
            jsonConv.Add("fiTransactionId", "FM1" + traceNumber.ToString().PadLeft(6,'0'));
            jsonConv.Add("fiToken", fiToken);
            jsonConv.Add("fiTrxNumber", trxNumber);
            // jika transaksi pembayaran, pake return fiReversalAllowed
            if((isoType==3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

            HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
            LogWriter.write(this, LogWriter.logCodeEnum.INFO, "Transaction success to FM : " + "FM1" + traceNumber.ToString().PadLeft(6,'0'));
            return true;
        }

        public bool productTransaction(string appID, string userId, string transactionReference, string providerProductCode,
            string providerAmount, ref string HttpReply, ref int traceNumber,
            ref string strJson, ref DateTime trxTime, ref string strRecJson,
            ref DateTime trxRecTime, ref string failedReason, ref bool canReversal,
            ref bool isSuccessPayment, int transactionType, string trxNumber)
        {
            int isoType = 3;
            return productTransactionReversal(appID, userId, transactionReference, providerProductCode,
                providerAmount, ref HttpReply, ref traceNumber,
                ref strJson, ref trxTime, ref strRecJson, ref trxRecTime,
                ref failedReason, isoType, ref canReversal, ref isSuccessPayment, transactionType,
                trxNumber);
        }

        public bool productReversal(string appID, string userId, string transactionReference, string providerProductCode,
            string providerAmount, ref string HttpReply, ref int traceNumber,
            ref string strJson, ref DateTime trxTime, ref string strRecJson,
            ref DateTime trxRecTime, ref bool isSuccessReversal, ref string failedReason, string trxNumber)
        {
            int isoType = 4;
            bool canReversal = false;
            return productTransactionReversal(appID, userId, transactionReference, providerProductCode,
                providerAmount, ref HttpReply, ref traceNumber,
                ref strJson, ref trxTime, ref strRecJson, ref trxRecTime,
                ref failedReason, isoType, ref canReversal, ref isSuccessReversal, 0, trxNumber);
        }

        public bool productInquiry(string appID, string userId, string customerNumber,
            string providerProductCode, int adminFee, bool fIncludeAdminFee, ref int productAmount, ref string HttpReply, 
            ref int traceNumber, ref string strJson, ref DateTime trxTime, ref string strRecJson,
            ref DateTime trxRecTime, string trxNumber)
        {
            string ncode = "";
            int isoType;
            ncode = "000";
            isoType = 2;        // inquiry

            // reset
            fm.rC = "";
            fm.trxAmount = "000000000000";

            fm.bit48 = customerNumber;

            fm.bit2 = providerProductCode;

            // 1. create ISO MSG
            strJson = "";
            strRecJson = "";
            traceNumber = localDB.getNextProductTraceNumber();
            long reffNum = localDB.getNextProductReferenceNumber();
            byte[] iso = fm.generateTransactionJson(ncode, isoType,
                //StaticCommonLibrary.CommonLibrary.getNextProductTraceNumber(),
                //StaticCommonLibrary.CommonLibrary.getNextProductReferenceNumber(),
                traceNumber, reffNum,
                userId, "6014", commonSettings.getString("CommonTerminalID"), commonSettings.getString("CommonMerchantID"), ref strJson, ref trxTime);

            try
            {
                // 2. Send ISO Msg
                tcp.CheckConn();
                tcp.Send(iso);
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error while send to FM Queue host : " + ex.getCompleteErrMsg());
                strRecJson = ""; trxRecTime = trxTime;
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Disconnected from FM Queue host", "");
                return false;
            }

            LogWriter.show(this,"DEBUG== Reading ISO");
            DateTime skrg = DateTime.Now;

            // 3. Read Balasan ISO Msg
            byte[] ret;
            try
            {
                ret = tcp.Read(150, ref strRecJson);
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error while read from FM Queue host : " + ex.getCompleteErrMsg());
                trxRecTime = skrg;
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "No response from FM Queue host", "");
                return false;
            }
            trxRecTime = DateTime.Now;

            if (ret == null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "No response from FM Queue host");
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "No response from FM Queue host", "");
                return false;
            }

            // 4. Check Data Signature apakah sama ?
            if (!fm.CheckDataSignature(strRecJson, isoType))
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Signature not match with Finnet Queue host");
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "Error signature not match with Finnet Queue host", "");
                return false;
            }

            if (!jsonConv.JSONParse(System.Text.Encoding.UTF8.GetString(ret)))
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Invalid data from FM Queue host " + System.Text.Encoding.UTF8.GetString(ret));
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "Invalid data from FM Queue host", "");
                return false;
            }

            string fiAmount;
            string fiPrivateData;
            string fiResponseCode;
            string fiToken = "TMPFIXED";
            int fiAdminFee = adminFee;

            if (fIncludeAdminFee)
            {
                fiAdminFee = 0;
            }

            try
            {
                fiAmount = ((string)jsonConv["Amount"]).Trim();
                fiPrivateData = ((string)jsonConv["Bit48"]).Trim();
                fiResponseCode = ((string)jsonConv["ResponseCode"]).Trim();
                productAmount = int.Parse(fiAmount);
            }
            catch
            {
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Invalid data fields from host", "");
                return false;
            }
            jsonConv.Clear();
            jsonConv.Add("fiToken", securityToken);
            jsonConv.Add("fiAmount", productAmount);
            jsonConv.Add("fiPrivateData", fiPrivateData);
            jsonConv.Add("fiResponseCode", fiResponseCode);
            jsonConv.Add("fiToken", fiToken);
            jsonConv.Add("fiAdminFee", fiAdminFee);
            jsonConv.Add("fiTrxNumber", trxNumber);

            HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
            return true;

        }

    }
}
