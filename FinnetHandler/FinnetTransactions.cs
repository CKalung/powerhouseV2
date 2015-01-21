using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPOBHttpRestData;
using LOG_Handler;
using Payment_Host_Interface;
using StaticCommonLibrary;

namespace FinnetHandler
{
    public class FinnetTransactions: ITransactionInterface
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
        ~FinnetTransactions()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            fm.Dispose();
            HTTPRestDataConstruct.Dispose();
            jsonConv.Dispose();
            tcp.Dispose();
            localDB.Dispose();
        }

        FinnetModule fm;
        TCPFinnetClient tcp;        // sementara
        HTTPRestConstructor HTTPRestDataConstruct;
        JsonLibs.MyJsonLib jsonConv;
        PublicSettings.Settings commonSettings;
        PPOBDatabase.PPOBdbLibs localDB;
        Exception xError;

        public string securityToken = "";

        public FinnetTransactions(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            tcp = new TCPFinnetClient(commonSettings.getString("FinnetQueueHost"),
                commonSettings.getInt("FinnetQueuePort"));
            jsonConv = new JsonLibs.MyJsonLib();
            HTTPRestDataConstruct = new HTTPRestConstructor();
            fm = new FinnetModule(commonSettings);
            //localDB = new PPOBDatabase.PPOBdbLibs(commonSettings);
            localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
                commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
        }

        private bool productIsoType1(string nCode)
        {
            string ncode = nCode;
            int isoType = 1;
            DateTime trxTime = DateTime.Now;
            string sRecIso = "";
            string strJson = "";
            int traceNumber = localDB.getNextProductTraceNumber();
            try
            {
                // 1. create ISO MSG
                //byte[] iso = fm.CreateMsgJSON(ncode, isoType);
                byte[] iso = fm.generateTransactionJson(ncode, isoType,
//                    StaticCommonLibrary.CommonLibrary.getNextProductTraceNumber(),
                    traceNumber,
                    1,
                    "", "6014", commonSettings.getString("CommonTerminalID"),
                    commonSettings.getString("CommonMerchantID"), 
                    ref strJson, ref trxTime);

                // 2. Send ISO Msg
                try
                {
                    // 2. Send ISO Msg
                    tcp.CheckConn();
                    tcp.Send(iso);
                }
                catch (Exception ex)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error while send to Queue host : " + ex.getCompleteErrMsg());
                    return false;
                }

                // 3. Read Balasan ISO Msg
                byte[] ret = tcp.Read(150,ref sRecIso);

                if (ret == null)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.INFO, "No response from Queue");
                    return false;
                }
                // 4. Check Data Signature apakah sama ?
                //if (fm.CheckDataSignature(ret, isoType))
                if (fm.CheckDataSignature(sRecIso, isoType))
                {
                    //Console.WriteLine("Signature Sarua");
                    return true;
                }
                else
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.INFO, "Signature not match");
                    return false;
                }
            }
            catch (Exception ex) {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error while send to Queue host : " + ex.getCompleteErrMsg());
                return false; 
            }
        }

        public bool productEchoTest()
        {
            LogWriter.show(this, "EchoTest");
            return productIsoType1("301");
        }

        public bool productSignOn()
        {
            LogWriter.show(this, "SignOn");
            return productIsoType1("001");
        }

        public bool productSignOff()
        {
            LogWriter.show(this, "SignOff");
            return productIsoType1("002");
        }

        public bool productCutOff()
        {
            LogWriter.show(this, "CutOff");
            return productIsoType1("201");
        }

        public bool productInquiry(string appID, string userId, string customerNumber,
            string productCode, int adminFee, bool fIncludeAdminFee, ref int productAmount, ref string HttpReply,
            ref int traceNumber, ref string strJson, ref DateTime trxTime, ref string strRecJson,
            ref DateTime trxRecTime, string trxNumber)
        {
            //DateTime trxTime;
            string ncode = "";
            int isoType;
            ncode = "000";
            isoType = 2;
            // reset
            fm.rC = "";
            fm.trxAmount = "";
            fm.bit61 = "";
            fm.bit103 = "";
            fm.trxAmount = "000000000000";
            string merchantType = "6012";

            productAmount = 0;

            //Console.Write("Masukan Bit 61 [kosongkan utk pake fix data test] = ");
            //fm.bit61 = "0130021004415015";
            fm.bit61 = customerNumber.Trim();
            fm.bit61 = fm.bit61.Length.ToString().PadLeft(3, '0') + fm.bit61;

            //if (fm.bit61.Length > 7)
            //{
            //    // cek jika FINPAY
            //    if (fm.bit61.Substring(3, 4).Equals("0195"))
            //    {
            //        merchantType = "6011";
            //    }
            //}

            //Console.Write("Masukan Bit 103 [kosongkan utk pake fix data test] = ");
            //fm.bit103 = "06001001";
            fm.bit103 = productCode.Length.ToString().PadLeft(2, '0') + productCode;

            //Console.WriteLine("Amount = " + fm.trxAmount);
            //Console.WriteLine("Bit61 = " + fm.bit61);
            //Console.WriteLine("Bit103 = " + fm.bit103);

            //Console.WriteLine("DEBUG== Create ISO");
            // 1. create ISO MSG
            //byte[] iso = fm.CreateMsgJSON(ncode, isoType);
            //string 
            strJson = "";
            //string 
            strRecJson = "";

            traceNumber = localDB.getNextProductTraceNumber();
            long reffNum = localDB.getNextProductReferenceNumber();
            byte[] iso = fm.generateTransactionJson(ncode, isoType,
                //StaticCommonLibrary.CommonLibrary.getNextProductTraceNumber(),
                //StaticCommonLibrary.CommonLibrary.getNextProductReferenceNumber(),
                traceNumber, reffNum,
                userId, merchantType, commonSettings.getString("CommonTerminalID"), commonSettings.getString("CommonMerchantID"), ref strJson, ref trxTime);

            //Console.WriteLine("DEBUG== Send Msg");
            try
            {
                // 2. Send ISO Msg
                tcp.CheckConn();
                tcp.Send(iso);
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error while send to Finnet Queue host : " + ex.getCompleteErrMsg());
                strRecJson = ""; trxRecTime = trxTime;
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Disconnected from Finnet Queue host", "");
                return false;
            }

            LogWriter.show(this, "Reading ISO");
            DateTime skrg = DateTime.Now;

            // 3. Read Balasan ISO Msg
            byte[] ret;
            try
            {
                ret = tcp.Read(150, ref strRecJson);
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error while read from Finnet Queue host : " + ex.getCompleteErrMsg());
                trxRecTime = skrg;
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "No response from Finnet Queue host", "");
                return false;
            }
            trxRecTime = DateTime.Now;

            if (ret == null)
            {
                //Console.WriteLine("No response from finnet");
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "No response from Finnet Queue host");
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "No response from Finnet host", "");
                return false;
            }

            //Console.WriteLine("DEBUG== Cek Signature ");
            //Console.WriteLine("DEBUG== Ret : " + ret);
            //Console.WriteLine("DEBUG== IsoType : " + isoType);
            // 4. Check Data Signature apakah sama ?
            //if (!fm.CheckDataSignature(ret, isoType))
            if (!fm.CheckDataSignature(strRecJson, isoType))
            {
                //Console.WriteLine("Signature beda");
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Signature not match with Finnet Queue host");
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "Error signature not match with Finnet Queue host", "");
                return false;
            }
            //Console.WriteLine("Signature Sarua");

            //Console.WriteLine("DEBUG== Parse Json");
            if (!jsonConv.JSONParse(System.Text.Encoding.UTF8.GetString(ret)))
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Invalid data from Finnet Queue host " + System.Text.Encoding.UTF8.GetString(ret));
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "Invalid data from Finnet Queue host", "");
                return false;
            }

//            Console.WriteLine("DEBUG== Get fields");
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
                fiPrivateData = ((string)jsonConv["PrivateData2"]);
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

        public bool productTransactionReversal(string appID, string userId, 
            string transactionReference, string providerProductCode, string providerAmount, 
            ref string HttpReply, ref int traceNumber, ref string strJson, ref DateTime trxTime, 
            ref string strRecJson, ref DateTime trxRecTime, ref string failedReason, int isoType,
            ref bool canReversal, ref bool isSuccessReversal, int transactionType, string trxNumber)
        {
            failedReason = "";
            canReversal = false;

            fm.reversalUlang = isSuccessReversal; // ambil flag reversal ulang
            isSuccessReversal = false;

            string ncode = "";
            ncode = "000";
            // reset
            fm.rC = "";
            fm.trxAmount = "";
            fm.bit60 = "";
            fm.bit61 = "";
            fm.bit90 = "";
            fm.bit103 = "";

            fm.trxAmount = providerAmount.PadLeft(12, '0');

            if (isoType != 4)  // Jika transaksi, bukan reversal
            {
                //Console.Write("Masukan Bit 61 [kosongkan jika ambil dari inquiry] = ");
                //fm.bit61 = "0130021004415015";

//data log  : 08800210044150150200052208A800528       953959209A228834       584164WARREN CUCCURULLO653          1252160        
//payment  : 00210044150150200052208A800528       953959209A228834       584164

                fm.bit61 = transactionReference;
                if (fm.bit61.Length < 48)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Invalid customer reference number from client");
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "493", "Invalid customer reference number from client", "");
                    strRecJson = ""; trxRecTime = trxTime = DateTime.Now;
                    failedReason = "Invalid customer reference number";
                    return false;
                }
                //int reflen = 0; ;
                //try
                //{
                //    reflen = int.Parse( fm.bit61.Substring(0, 3));
                //}
                //catch {
                //    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Invalid customer reference number from client");
                //    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "493", "Invalid customer reference number from client", "");
                //    strRecJson = ""; trxRecTime = trxTime = DateTime.Now;
                //    failedReason = "Invalid customer reference number";
                //    return false;
                //}
                // parsing dari depan
                try
                {
                    //fm.bit61 = fm.bit61.Substring(3, reflen - 45);
                    //fm.bit61 = fm.bit61.Length.ToString().PadLeft(3, '0') + fm.bit61;
                    int jmlTagihan = int.Parse(fm.bit61.Substring(22, 1));
                    //int idxNama = 23 + (jmlTagihan * (11 + 12));
                    int lenPrivData2 = 20 + (jmlTagihan * (11 + 12));
                    fm.bit61 = fm.bit61.Substring(3, lenPrivData2);
                    fm.bit61 = fm.bit61.Length.ToString().PadLeft(3, '0') + fm.bit61;
                }
                catch
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Invalid customer reference number from client, " + fm.bit61);
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "493", "Invalid customer reference number from client", "");
                    strRecJson = ""; trxRecTime = trxTime = DateTime.Now;
                    failedReason = "Invalid customer reference number";
                    return false;
                }

                LogWriter.showDEBUG(this, fm.bit61);
            }
            else
            {
                //  "003090"; // 090: Timeout, 050: canceled, 032:system error, 030: hardwre error.
                //string bit60 = "003090";
                fm.bit60 = "003090";    // timeout

                string origDataEl = "";
                string traceNumbRev = "";
                string trxTimeRev = "";

                localDB.getCustomerLastTransactionLog(transactionReference, ref traceNumbRev, 
                    ref trxTimeRev, out xError);
                if (xError != null) return false;

                origDataEl = "0200"; // mti
                origDataEl += traceNumbRev.PadLeft(6,'0');
                origDataEl += trxTimeRev;      //iso8583.isoBit7; // orign transmition date time

                String bank_code = commonSettings.getString("BankCodeForFinnetProvider");         //"167";
                //String padd = String.format("% -8s", bank_code);

                origDataEl += bank_code+"        "; // original institution ID => di isi kode bank qnb                
                origDataEl += bank_code+"        "; // original institution forwader ID => di isi kode bank qnb
                fm.bit90 = origDataEl;
            }

            //Console.Write("Masukan Bit 103 [kosongkan jika ambil dari inquiry] = ");
            //fm.bit103 = "06001001";
            fm.bit103 = providerProductCode.Length.ToString().PadLeft(2, '0') + providerProductCode;

            // 1. create ISO MSG
            //byte[] iso = fm.CreateMsgJSON(ncode, isoType);
            //traceNumber = StaticCommonLibrary.CommonLibrary.getNextProductTraceNumber();
            traceNumber = localDB.getNextProductTraceNumber();
            long reffNum = localDB.getNextProductReferenceNumber();
            byte[] iso = fm.generateTransactionJson(ncode, isoType,
                traceNumber,
                //StaticCommonLibrary.CommonLibrary.getNextProductReferenceNumber(),
                reffNum,
                userId, "6012", commonSettings.getString("CommonTerminalID"), commonSettings.getString("CommonMerchantID"), 
                ref strJson, ref trxTime);

            try
            {
                tcp.CheckConn();
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Connect to Finnet Queue host has failed : " + ex.getCompleteErrMsg());
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Connect to Finnet Queue host has failed", "");
                strRecJson = ""; trxRecTime = trxTime;
                failedReason = "Failed to connect to Finnet Queue host";
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
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error while send to Finnet Queue host : " + ex.getCompleteErrMsg());
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Disconnected from Finnet Queue host", "");
                strRecJson = ""; trxRecTime = trxTime;
                failedReason = "Failed while send the.StackTrace to Finnet Queue host : " + ex.getCompleteErrMsg();
                return false;
            }

            canReversal = true;

            LogWriter.show(this, "Reading ISO");
            DateTime skrg = DateTime.Now;

            string fiToken = "TMPFIXED";
            //string trxNumber = localDB.getProductTrxNumber(out xError);

            // 3. Read Balasan ISO Msg
            byte[] ret;
            string sRet="";
            try
            {
                ret = tcp.Read(commonSettings.getInt("BillerFinnetTimeOut"), ref sRet);
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error while read from FinnetQueue host : " + ex.getCompleteErrMsg());
                if ((isoType == 3) && (transactionType == 0) && canReversal)
                {
                    jsonConv.Clear();
                    jsonConv.Add("fiToken", securityToken);
                    jsonConv.Add("fiPrivateData", "Need reversal");
                    jsonConv.Add("fiResponseCode", "99");
                    jsonConv.Add("fiTransactionId", "FN1" + traceNumber.ToString().PadLeft(6, '0'));
                    jsonConv.Add("fiToken", fiToken);
                    jsonConv.Add("fiTrxNumber", trxNumber);
                    // jika transaksi pembayaran, pake return fiReversalAllowed
                    if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "No response from Finnet Queue host",
                        jsonConv.JSONConstruct());
                }
                else
                {
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "No response from Finnet Queue host", "");
                }
                strRecJson = sRet; trxRecTime = skrg;
                failedReason = "Failed while read data from Finnet queue host : " + ex.getCompleteErrMsg();
                return false;
            }
            skrg = DateTime.Now;
            strRecJson = sRet; trxRecTime = skrg;

            if (ret ==null)
            {
                //Console.WriteLine("No response from provider");
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "No response from Finnet Queue host");
                if ((isoType == 3) && (transactionType == 0) && canReversal)
                {
                    jsonConv.Clear();
                    jsonConv.Add("fiToken", securityToken);
                    jsonConv.Add("fiPrivateData", "Need reversal");
                    jsonConv.Add("fiResponseCode", "99");
                    jsonConv.Add("fiTransactionId", "FN1" + traceNumber.ToString().PadLeft(6, '0'));
                    jsonConv.Add("fiToken", fiToken);
                    jsonConv.Add("fiTrxNumber", trxNumber);
                    // jika transaksi pembayaran, pake return fiReversalAllowed
                    if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "No response from Finnet Queue host",
                        jsonConv.JSONConstruct());
                }
                else
                {
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "No response from Finnet Queue host", "");
                }
                failedReason = "Failed to process, return from Finnet queue host is null";
                return false;
            }

            // 4. Check Data Signature apakah sama ?
            //if (!fm.CheckDataSignature(ret, isoType))
            if (!fm.CheckDataSignature(strRecJson, isoType))
            {
                //Console.WriteLine("Signature beda");
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Signature not match");
                if ((isoType == 3) && (transactionType == 0) && canReversal)
                {
                    jsonConv.Clear();
                    jsonConv.Add("fiToken", securityToken);
                    jsonConv.Add("fiPrivateData", "Need reversal");
                    jsonConv.Add("fiResponseCode", "99");
                    jsonConv.Add("fiTransactionId", "FN1" + traceNumber.ToString().PadLeft(6, '0'));
                    jsonConv.Add("fiToken", fiToken);
                    jsonConv.Add("fiTrxNumber", trxNumber);
                    // jika transaksi pembayaran, pake return fiReversalAllowed
                    if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Error signature code",
                        jsonConv.JSONConstruct());
                }
                else
                {
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "Error signature code", "");
                }
                failedReason = "Data signature not match with Finnet queue host";
                return false;
            }
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
                    jsonConv.Add("fiTransactionId", "FN1" + traceNumber.ToString().PadLeft(6, '0'));
                    jsonConv.Add("fiToken", fiToken);
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
                failedReason = "Could not parse Json data from Finnet queue host : " + System.Text.Encoding.UTF8.GetString(ret);
                return false;
            }

            string fiAmount = ((string)jsonConv["Amount"]).Trim();
            string fiPrivateData = ((string)jsonConv["PrivateData2"]);
            string fiResponseCode = ((string)jsonConv["ResponseCode"]).Trim();

            canReversal = false;
            // Jika response code ??, artinya reversal sebelumnya sudah sukses.
            if (fiResponseCode == "00") isSuccessReversal = true;

            if (fiPrivateData == "") fiPrivateData = "..";
            jsonConv.Clear();
            jsonConv.Add("fiToken", securityToken);
            jsonConv.Add("fiPrivateData", fiPrivateData);
            jsonConv.Add("fiResponseCode", fiResponseCode);
            jsonConv.Add("fiTransactionId", "FIN"+traceNumber.ToString());
            jsonConv.Add("fiToken", fiToken);
            jsonConv.Add("fiTrxNumber", trxNumber);
            // jika transaksi pembayaran, pake return fiReversalAllowed
            if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

            HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
            LogWriter.write(this, LogWriter.logCodeEnum.INFO, "Transaction success to Finnet : " + "FIN" + traceNumber.ToString());
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
                ref failedReason,isoType, ref canReversal, ref isSuccessPayment, transactionType,
                trxNumber);
        }

        public bool productReversal(string appID, string userId, string transactionReference, string providerProductCode,
            string providerAmount, ref string HttpReply, ref int traceNumber,
            ref string strJson, ref DateTime trxTime, ref string strRecJson,
            ref DateTime trxRecTime, ref bool isSuccessReversal, ref string failedReason,
            string trxNumber)
        //public bool productReversal(string appID, string userId, string transactionReference, string providerProductCode,
        //    string providerAmount, ref string HttpReply, ref int traceNumber,
        //    ref string strJson, ref DateTime trxTime, ref string strRecJson,
        //    ref DateTime trxRecTime, ref string failedReason, int transactionType)
        {
            int isoType = 4;
            bool canReversal = false;
            // isSuccessReversal => nebeng Flag reversal ulang
            return productTransactionReversal(appID, userId, transactionReference, providerProductCode,
                providerAmount, ref HttpReply, ref traceNumber,
                ref strJson, ref trxTime, ref strRecJson, ref trxRecTime,
                ref failedReason, isoType, ref canReversal, ref isSuccessReversal, 0, trxNumber);
        }


    }
}
