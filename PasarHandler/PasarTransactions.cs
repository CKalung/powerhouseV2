using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Payment_Host_Interface;
using PPOBHttpRestData;
using LOG_Handler;
using StaticCommonLibrary;

namespace PasarHandler
{
    public class PasarTransactions : ITransactionInterface
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
        ~PasarTransactions()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            try
            { HTTPRestDataConstruct.Dispose(); }
            catch { //LogWriter.showDEBUG(this, "ASUP EX2"); 
            }
            try
            { jsonConv.Dispose(); }
            catch { //LogWriter.showDEBUG(this, "ASUP EX3"); 
            }
            try
            { localDB.Dispose(); }
            catch { //LogWriter.showDEBUG(this, "ASUP EX5"); 
            }
        }

        HTTPRestConstructor HTTPRestDataConstruct;
        JsonLibs.MyJsonLib jsonConv;
        PublicSettings.Settings commonSettings;
        PPOBDatabase.PPOBdbLibs localDB;
        Exception xError;

        public string securityToken = "";

        string ownerId = "";
        string ownerPhone = "";
        string custPhone = "";
        string host = "";

        public string OwnerId
        {
            set
            {
                ownerId = value;
            }
        }
        public string OwnerPhone
        {
            set
            {
                ownerPhone = value;
            }
        }
        public string CustomerPhone
        {
            set
            {
                custPhone = value;
            }
        }
        public string Hostname
        {
            set
            {
                host = value;
            }
        }

        public PasarTransactions(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            jsonConv = new JsonLibs.MyJsonLib();
            HTTPRestDataConstruct = new HTTPRestConstructor();
            localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
                commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
        }

        public bool productTransaction(string appID, string userId, string transactionReference, 
            string providerProductCode, string providerAmount, ref string HttpReply, 
            ref int traceNumber, ref string strJson, ref DateTime trxTime, ref string strRecJson, 
            ref DateTime trxRecTime, ref string failedReason, ref bool canReversal,
            ref bool isSuccessPayment, int transactionType, string trxNumber)
        {
            // simpan di database order_request
//INSERT INTO order_request (product_code,owner_phone,distributor_phone,amount,order_time,description,host)
//VALUES ('PRD00055','081218877246','082218877123','150000',NOW(),'description','127.0.0.1')
            // 
            //traceNumber = StaticCommonLibrary.CommonLibrary.getNextProductTraceNumber();
            traceNumber = localDB.getNextProductTraceNumber();
            strJson = "Store: Order Request from: " + custPhone + " to: " + ownerPhone;
            trxTime = DateTime.Now;
            strRecJson = "Success";
            trxRecTime = DateTime.Now;
            failedReason = "-";
            canReversal = false;
            isSuccessPayment = true;
            string fiToken = "TMPFIXED";
            //string trxNumber = localDB.getProductTrxNumber(out xError);

            localDB.TokoOnline_SaveOrder(providerProductCode, ownerPhone, custPhone, int.Parse(providerAmount),
                trxTime, transactionReference, host, trxNumber);

            Exception ExError = null;
            System.Collections.Hashtable custInfo = localDB.getLoginInfoByUserPhone(
                custPhone, out ExError);
            // buat notifikasi ke penjual
            // OK, semua sudah masuk persyaratan, sekarang kirim notifikasi ke penyelia
            // siapkan json untuk notifikasi ke agen
            jsonConv.Clear();
            jsonConv.Add("fiToken", securityToken);
            jsonConv.Add("fiBuyerPhone", custPhone);
            jsonConv.Add("fiNotificationDateTime", trxTime.ToString("yyyy-MM-dd HH:mm:ss"));
            jsonConv.Add("fiProductInfo", transactionReference);
            jsonConv.Add("fiProductPrice", int.Parse(providerAmount));
            if (custInfo != null)
            {
                jsonConv.Add("fiBuyerName", (string)custInfo["first_name"] + " " + (string)custInfo["last_name"]);
            }

            string notifJson = jsonConv.JSONConstruct();

            localDB.insertNotificationQueue(custPhone, ownerPhone, trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
                "11", notifJson, out ExError);

            jsonConv.Clear();
            jsonConv.Add("fiToken", securityToken);
            jsonConv.Add("fiPrivateData", transactionReference);
            jsonConv.Add("fiResponseCode", "00");
            jsonConv.Add("fiTransactionId", "Shop" + traceNumber.ToString().PadLeft(6, '0'));
            jsonConv.Add("fiToken", fiToken);
            jsonConv.Add("fiTrxNumber", trxNumber);
            jsonConv.Add("fiReversalAllowed", false);
            HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

            return true;
        }

        public bool productInquiry(string appID, string userId, string customerNumber, 
            string productCode, int adminFee, bool fIncludeAdminFee, ref int productAmount, 
            ref string HttpReply, ref int traceNumber, ref string strJson, ref DateTime trxTime,
            ref string strRecJson, ref DateTime trxRecTime, string trxNumber)
        {
            throw new NotImplementedException();
        }

    }
}
