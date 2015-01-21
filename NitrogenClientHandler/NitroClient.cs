using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using LOG_Handler;
using StaticCommonLibrary;
//using LogToDBService;

namespace NitrogenClientHandler
{
	public class NitroClient : IDisposable
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
        ~NitroClient()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            localDB.Dispose();
        }

        private Thread NitroThread;
        PPOBDatabase.PPOBdbLibs localDB;
        MPTransactionLog TransactionLog;
        Exception xError;

        bool fExit = true;
        bool fExited = false;

        PublicSettings.Settings commonSettings;

        public NitroClient(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            NitroThread = new Thread(new ThreadStart(NitroTrxThread));
            TransactionLog = new LOG_Handler.MPTransactionLog();
			TransactionLog.setPath(System.IO.Path.Combine(commonSettings.getString("LogPath"), "TransactionLog"));
            localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
                    commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
            //Finnet = new FinnetTransactions(commonSettings);
        }

        public void Stop()
        {
            fExit = true;
            while (!fExited)
            {
                Thread.Sleep(100);
            }
        }

        public void Start()
        {
            fExited = false;
            fExit = false;
            NitroThread.Start();
        }

        private bool EksekusiTransaksiOffline(bool scanDelayed, ref bool fastScan)
        {
            Exception exErr;

            string userPhone = "";
            string appID = "";
            string productCode = "";
            DateTime trxDateTime = DateTime.Now;
            int quantity = 0;
            int productPrice = 0;
			string dbTraceNum = "";

            // cek apakah ada data 
			if (!localDB.getOneOfflineTransaction (scanDelayed, ref userPhone, ref appID, ref productCode,
				ref trxDateTime, ref quantity, ref productPrice, ref dbTraceNum, out exErr))
            {
                return false;
            }
            LogWriter.showDEBUG(this, "*********** ADA offline transaction **********");

            string userId = commonSettings.getString("UserIdHeader") + userPhone;

            // transaksikan langsung
            return ProviderTransaction(appID, userId, trxDateTime, productCode, quantity,
                productPrice, userPhone, !scanDelayed, ref fastScan);
        }

        private void NitroTrxThread()
        {
            //LogWriter.showDEBUG(this, "*********** MASUK SCAN offline transaction **********");
            //int ctr = 600;       // 10 = 1 detik, 600 = 1 menit
            int ctr = 120;       // 2 = 1 detik, 120 = 1 menit
            int cnt = 20;       // 10 detik pertama
            int ctrFailed = 240;       // per 2 menit jika terjadi gagal
            bool fastScan = true;
            bool scanDelayedToggle = false;
            while (!fExit)
            {
                try
                {
                    // do any background work
                    if (fastScan)
                        Thread.Sleep(50);
                    else
                        Thread.Sleep(500);
                    cnt--;
                    if (cnt > 0) continue;
                    cnt = ctr;
                    // periodik kesini setiap menit
                    //LogWriter.showDEBUG(this, "*********** SCAN offline transaction **********");
                    // set transaksi disini
                    scanDelayedToggle = !scanDelayedToggle;
                    if (!EksekusiTransaksiOffline(scanDelayedToggle, ref fastScan))
                    {
                        if (fastScan) cnt = 1;
                        else cnt = ctrFailed;        // jika gagal akses db, per 2 menit
                        continue;
                    }
                    if (fastScan) cnt = 1;
                    else cnt = ctr;
                }
                catch (Exception ex)
                {
                    // log errors
                    if (!scanDelayedToggle)
                    {
                        LogWriter.write(this, LogWriter.logCodeEnum.ERROR, ex.getCompleteErrMsg());
                    }
                }
            }
            fExited = true;
        }

		private bool getBaseAndFeeAmountFromProduct(string productCode, string appID,	 //string providerCode,
            ref int adminFee, int TotalAmount = 0)
        {

            if (!localDB.getAdminFeeAndCustomerFee(productCode, providerCode, TotalAmount,
                ref adminFee, out xError))
            {
                return false;
            }
            return true;
        }

        private bool ProviderTransaction(string appID, string userId, DateTime trxTime,
            string productCode, int productQuantity, int productPrice, string userPhone, 
            bool useLog, ref bool fastScan)
        {
            string errCode = "00"; string errMessage = "";
            DateTime trxRecTime = DateTime.Now;
            string errMsg = "";
            Exception exrr = null;

            string customerProductNumber = productCode;
            int productAmount = productPrice * productQuantity;

            // trik supaya product amount masih sama dengan class lain
            //int productQuantity = productAmount;

            //int baseAmount = 0;
            int adminFee = 0;
            //int customerFeeAmount = 0;
            PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
            //Console.WriteLine("ProductCode = " + productCode);
            try
            {
                providerProduct = localDB.getProviderProductInfo(productCode, out exrr);
                if (exrr != null)
                {
                    fastScan = false;
                    errMsg = "Provider product data not found";
                    localDB.updateOfflineTransactionLog(userPhone, appID, productCode, trxTime,
                        productQuantity, true, false, "428", errMsg, useLog, out xError);
                    return false;
                }
            }
            catch
            {
                fastScan = false;
                errMsg = "Failed on geting provider product data";
                localDB.updateOfflineTransactionLog(userPhone, appID, productCode, trxTime,
                    productQuantity, true, false, "428", errMsg, useLog, out xError);
                return false;
            }

            //productAmount = providerProduct.CurrentPrice * productQuantity;
            providerProduct.CurrentPrice = productPrice;

            // cek keberadaan user di database
            if (!localDB.isAccountExistById(userId, out exrr))
            {
                fastScan = true;
                errMsg = "No user found";
                localDB.updateOfflineTransactionLog(userPhone, appID, productCode, trxTime,
                    productQuantity, true, false, "428", errMsg, useLog, out xError);
                return false;
            }

            //Console.WriteLine("ProviderCode = " + providerProduct.ProviderCode);
            // ============ Product Transaction Filter
            if (providerProduct.ProviderCode != "121")  // Nitrogen
            {
                fastScan = true;
                errMsg = "Unregistered provider";
                localDB.updateOfflineTransactionLog(userPhone, appID, productCode, trxTime,
                    productQuantity, true, false, "428", errMsg, useLog, out xError);
                return false;
            }

            try
            {
                //Console.WriteLine(" productAmount " + productAmount);
                //Console.WriteLine(" intNYA : " + int.Parse(productAmount));
				//if (!getBaseAndFeeAmountFromProduct(productCode, providerProduct.ProviderCode,
				if (!getBaseAndFeeAmountFromProduct(productCode, appID,
                    ref adminFee, productAmount))
                {
                    fastScan = true;
                    errMsg = "Fee data not found";
                    localDB.updateOfflineTransactionLog(userPhone, appID, productCode, trxTime,
                        productQuantity, true, false, "428", errMsg, useLog, out xError);
                    return false;
                }
            }
            catch (Exception ex)
            {
                fastScan = false;
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error get fee data : " + ex.getCompleteErrMsg());
                //Console.WriteLine(ex.StackTrace);
                errMsg = "Error get fee data : " + ex.getCompleteErrMsg();
                localDB.updateOfflineTransactionLog(userPhone, appID, productCode, trxTime,
                    productQuantity, true, false, "428", errMsg, useLog, out xError);
                return false;
            }

            // SERVICE/PEMBAYARAN amount diambil dari productAmount dari client, 
            // jika pembelian, amount diambil dari currentPrice
            //int nilaiTransaksiKeProvider = providerProduct.CurrentPrice;
            //int nilaiYangMasukLog = providerProduct.CurrentPrice;
            int nilaiTransaksiKeProvider = productAmount;
            int nilaiYangMasukLog = productAmount;

            if (providerProduct.ProductType == PPOBDatabase.PPOBdbLibs.ProductTypeEnum.PRODUCT)
            {
                if (providerProduct.fIncludeFee)
                {
                    productAmount = providerProduct.CurrentPrice * productQuantity;
                    nilaiYangMasukLog = productAmount - adminFee;
                }
                else
                {
                    productAmount = (providerProduct.CurrentPrice + adminFee) * productQuantity;
                    nilaiYangMasukLog = providerProduct.CurrentPrice * productQuantity;
                }
            }
            else
            {
                if (providerProduct.fIncludeFee)
                {
                    providerProduct.CurrentPrice = productAmount;
                    nilaiYangMasukLog = productAmount - adminFee;
                }
                else
                {
                    providerProduct.CurrentPrice = productAmount - adminFee;
                    nilaiYangMasukLog = providerProduct.CurrentPrice * productQuantity;
                }
                if (providerProduct.CurrentPrice <= 0)
                {
                    fastScan = true;
                    errMsg = "Bad amount";
                    localDB.updateOfflineTransactionLog(userPhone, appID, productCode, trxTime,
                        productQuantity, true, false, "428", errMsg, useLog, out xError);
                    return false;
                }
            }

            //productAmount = prdAmount.ToString();
            nilaiTransaksiKeProvider = providerProduct.CurrentPrice;
            long TransactionRef_id = localDB.getTransactionReffIdSequence(out xError);
			string qvaInvoiceNumber = "";
			bool qvaReversalRequired = false;

            using (TransferReguler.CollectTransfer TransferReg =
                new TransferReguler.CollectTransfer(commonSettings))
            {
                try
                {
                    // ===== AMBIL PEMBAYARAN DARI CUSTOMER
                    LogWriter.show(this, "Get payment from ");
					if (!TransferReg.PayFromCustomer(userId,providerProduct.TransactionCodeSufix, 1, productAmount,
						TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.Nitrogen, 
						"Get payment from customer", ref qvaInvoiceNumber, ref qvaReversalRequired, 
						ref errCode, ref errMessage))
                    {
                        LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : [" + errCode + "]" + errMessage);
                        errMsg = "Failed to do payment";
                        localDB.updateOfflineTransactionLog(userPhone, appID, productCode, trxTime,
                            productQuantity, true, false, "428", errMsg, useLog, out xError);
                        fastScan = false;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    fastScan = false;
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : " + ex.getCompleteErrMsg());
                    errMsg = "Failed to do payment";
                    localDB.updateOfflineTransactionLog(userPhone, appID, productCode, trxTime,
                        productQuantity, true, false, "438", errMsg, useLog, out xError);
                    return false;
                }
            }

            // ===== Transaksi Product
            string strJson = "";
            string strRecJson = "";
            int traceNumb = 0;
            bool fTrx = true;
            DateTime skrg = DateTime.Now;
            string failedReason = "";
            string trxNumber = localDB.getProductTrxNumber(out xError);
            bool canReversal = false;
            bool isSuccessPayment = true;

            LogWriter.write(this, LogWriter.logCodeEnum.INFO, "Transaction in process from " + userId);

            // ============ Product Transaction Selector
            // disini untuk nitrogen hanya transfer aja ke penampung
            //if (providerProduct.ProviderCode == "004")  //Finnet
            //{
            //    using (FinnetHandler.FinnetTransactions Finnet =
            //        new FinnetHandler.FinnetTransactions(commonSettings))
            //    {
            //        Finnet.securityToken = securityToken;
            //        fTrx = Finnet.productTransaction(appID, userId, customerProductNumber,
            //            providerProduct.ProviderProductCode, nilaiTransaksiKeProvider.ToString(),
            //            ref httpReply, ref traceNumb, ref strJson, ref trxTime, ref strRecJson,
            //            ref trxRecTime, ref failedReason, ref canReversal, ref isSuccessPayment,
            //            PpobType, trxNumber);
            //    }
            //}
            // else untuk offline lain2 

            // transfer bonggol dilakukeun di trigger....
            //// ============= Transfer bongol nya
            //using (TransferReguler.CollectTransfer TransferReg =
            //    new TransferReguler.CollectTransfer(commonSettings))
            //{
            //    try
            //    {
            //        // ===== TRANSFER KE N2 OWNER
            //        LogWriter.show(this, "=== Bayar ke yang punya nitrogen ");
            //        if (!TransferReg.TransferToNitrogenOwner(nilaiYangMasukLog,
            //            TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.Nitrogen,
            //            "Transfer to N2 owner", ref errCode, ref errMessage))
            //        {
            //            LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : [" + errCode + "]" + errMessage);
            //            errMsg = "Failed to do payment";
            //            localDB.updateOfflineTransactionLog(userPhone, appID, productCode, trxTime,
            //                productQuantity, true, false, "428", errMsg, useLog, out xError);
            //            fastScan = false;
            //            isSuccessPayment = false;
            //            fTrx = false;
            //        }
            //        else
            //        {
            //            isSuccessPayment = true;
            //            fTrx = true;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        fastScan = false;
            //        LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : " + ex.getCompleteErrMsg());
            //        errMsg = "Failed to do payment";
            //        localDB.updateOfflineTransactionLog(userPhone, appID, productCode, trxTime,
            //            productQuantity, true, false, "438", errMsg, useLog, out xError);
            //        isSuccessPayment = false;
            //        fTrx = false;
            //    }
            //}


            // Untuk Nitrogen
            canReversal = false;
            // Update data di offline transaction dengan kode berhasil transaksi
            if (fTrx)
            {
                localDB.updateOfflineTransactionLog(userPhone, appID, productCode, trxTime,
                    productQuantity, true, true, "000", "", useLog, out xError);
                //if (!localDB.updateOfflineTransactionLog(userPhone, appID, productCode, trxTime,
                //    productQuantity, true, true, "000", "", useLog, out xError))
                //{
                //    fTrx = false;
                //    canReversal = false;
                //    LogWriter.showDEBUG(this, "=========== GAGAL TRANSAKSI LANGSUNG INSERT LOG");
                //}
            }

            LogWriter.showDEBUG(this, "=========== BERES TRANSAKSI LANGSUNG INSERT LOG");

            // Transfer reguler untuk Fee dan pembagian Pot Bagi dilakukan di service lain
            // dengan trigger dari insert ini
            if (!localDB.insertCompleteTransactionLog(TransactionRef_id, productCode, providerProduct.ProviderProductCode,
                        userId.Substring(commonSettings.getString("UserIdHeader").Length), customerProductNumber,
                        nilaiYangMasukLog.ToString(), traceNumb.ToString(), trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        adminFee.ToString(), providerProduct.ProviderCode, providerProduct.CogsPriceId,
                        0, 0, "", skrg.ToString("yyyy-MM-dd HH:mm:ss"), "", skrg.ToString("yyyy-MM-dd HH:mm:ss"),
                        strJson,
                        trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        strRecJson,
                        trxRecTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        isSuccessPayment, failedReason, trxNumber, canReversal, providerProduct.fIncludeFee,
                        out xError))
            {
                // Jadwalkan masuk database
//                TransactionLog.write(TransactionRef_id, productCode, providerProduct.ProviderProductCode, providerProduct.ProviderCode,
//                        userId.Substring(commonSettings.getString("UserIdHeader").Length), customerProductNumber,
//                        productAmount.ToString(), nilaiYangMasukLog, adminFee,
//                        providerProduct.CogsPriceId,
//                        traceNumb.ToString(), trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
//                        0, 0, "", skrg.ToString("yyyy-MM-dd HH:mm:ss"), "", skrg.ToString("yyyy-MM-dd HH:mm:ss"), strJson,
//                        trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
//                        strRecJson,
//                        trxRecTime.ToString("yyyy-MM-dd HH:mm:ss"),
//                        isSuccessPayment, failedReason, canReversal);
            }

            // Sisanya BISPRO transfer dilakukan oleh service lain untuk fee dan pembayaran ke finnet

            //if (providerProduct.ProductType == PPOBDatabase.PPOBdbLibs.ProductTypeEnum.SERVICE)
            //{
            //if ((!fTrx) && isSuccessPayment)
            if (!fTrx)
            {
                LogWriter.showDEBUG(this, "=========== BALIKIN PEMBAYARAN ==========");
                using (TransferReguler.CollectTransfer TransferReg =
                    new TransferReguler.CollectTransfer(commonSettings))
                {
                    try
                    {
                        // ===== KEMBALIKAN PEMBAYARAN DARI CUSTOMER
                        LogWriter.show(this, "Pay back to customer");
						// Disini tidak usah di deteksi gagal tidaknya, 
						// karena sudah masuk log dan dilakukan background jika belum berhasil
						TransferReg.Reversal(TransactionRef_id, qvaInvoiceNumber, ref errCode, ref errMessage);

//						if (!TransferReg.ReversalToCustomer(userId,
//							providerProduct.TransactionCodeSufix,
//                            productAmount, TransactionRef_id,
//                            PPOBDatabase.PPOBdbLibs.eTransactionType.Nitrogen, 
//                            "Transfer back to customer", ref errCode, ref errMessage))
//                        {
//                            fastScan = true;
//                            LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Reversal failed to " + userId +
//                                " TrxNumber: " + trxNumber + " : [" + errCode.ToString() + "]" + errMessage);
//                            errMsg = "Transaction failed, Failed to do reversal TrxNumber: " + trxNumber.ToString();
//                            return false;
//                        }
                    }
                    catch (Exception ex)
                    {
                        fastScan = true;
                        LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Reversal failed to " + userId +
                            " TrxNumber: " + trxNumber + " : [" + errCode.ToString() + "]" + ex.getCompleteErrMsg());
                        errMsg = "Transaction failed, Failed to do reversal TrxNumber: " + trxNumber.ToString();
                        return false;
                    }
                }
            }
            //}

            if (fTrx)
            {
                fastScan = true;
                LogWriter.write(this, LogWriter.logCodeEnum.INFO, "Transaction success from " + userId);
                return true ;
            }
            else
            {
                ////===== REVERSAL di skedulkan di servicelain
                LogWriter.write(this, LogWriter.logCodeEnum.INFO, "Transaction failed trxNumber: " + trxNumber.ToString());
                //TransferSchedLog.write(this,
                //        transactionType == 0 ?
                //                VATransferSchedLog.transactionTypeEnum.PAYMENT :
                //                VATransferSchedLog.transactionTypeEnum.PURCHASE,
                //        commonSettings.QVA_PENAMPUNG_DEBIT, userId, productAmount);

                fastScan = true;
                return true;
            }
        }

    }
}
