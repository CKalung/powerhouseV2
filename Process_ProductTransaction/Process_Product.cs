using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPOBHttpRestData;
using PPOBServerHandler;
using System.Collections;
using LOG_Handler;
using StaticCommonLibrary;
using BPJS_THT;

namespace Process_ProductTransaction
{
    public class Process_Product: IDisposable
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
        ~Process_Product()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            jsonConv.Clear();
            jsonConv.Dispose();
            HTTPRestDataConstruct.Dispose();
            localDB.Dispose();
            TransactionLog.Dispose();
        }

        JsonLibs.MyJsonLib jsonConv;
        HTTPRestConstructor HTTPRestDataConstruct;
        PPOBDatabase.PPOBdbLibs localDB;
        MPTransactionLog TransactionLog;
        VATransferSchedLog TransferSchedLog;
        Exception xError;

        

        PublicSettings.Settings commonSettings;
		string cUserIDHeader = "99";       // header userId untuk CustomerId account di QVA
        //bool localIP = false;       // Untuk debuging pake TCP Viewer
        //string sandraHost = "123.231.225.20";    // host sandra
        //int sandraPort = 7080;

        //string logPath="";
        //string dbHost = "";
        //int dbPort=0;
        //string dbUser="";
        //string dbPass="";
        //string dbName="";

		HTTPRestConstructor.HttpRestRequest clientDataSource;


        public Process_Product(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            cUserIDHeader = commonSettings.getString("UserIdHeader");
            //logPath = LogPath;
            //dbHost = DbHost; dbPort = DbPort; dbUser = DbUser; dbPass = DbPassw; dbName = DbName;
            //sandraPort = SandraPort;sandraHost = SandraHost;
            HTTPRestDataConstruct = new HTTPRestConstructor();
            jsonConv = new JsonLibs.MyJsonLib();
            localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
                    commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
            TransactionLog = new LOG_Handler.MPTransactionLog();
			TransactionLog.setPath(System.IO.Path.Combine(commonSettings.getString("LogPath") , "TransactionLog"));
            TransferSchedLog = new VATransferSchedLog();
			TransferSchedLog.setPath(System.IO.Path.Combine(commonSettings.getString("LogPath") , "VATransferLog"));
        }

        private void ReformatPhoneNumber(ref string phone)
        {
			phone = phone.Trim ().Replace(" ","").Replace("'","").Replace("-","");
            if (phone.Length < 2)
            {
                phone = "";
                return;
            }
            if (phone[0] == '0')
            {
                if (phone[1] == '6')
                    phone = phone.Substring(1);
                else
                    phone = "62" + phone.Substring(1);
            }
            else if (phone[0] == '+') phone = phone.Substring(1);
        }

        //private bool getBaseAndFeeAmountFromProduct(string productCode, string providerCode,
        //    ref int adminFee, ref int customerFeeAmount, int TotalAmount = 0)
		private bool getBaseAndFeeAmountFromProduct(string productCode, int quantity, string appId,	//string providerCode, 
			ref int adminFee, int TotalAmount = 0)
        {

			//if (!localDB.getAdminFeeAndCustomerFee(productCode, providerCode, TotalAmount,
			if (!localDB.getAdminFeeAndCustomerFee(productCode, quantity, appId, TotalAmount,
                ref adminFee, out xError))
            {
                return false;
            }

            //nilaiTransaksiKeProvider = decimal.ToInt32(totalAmount - adminFee);

            ////int adminFee = 0;
            //int sellerFee = 0;
            //if (!localDB.getAdminFeeAndCustomerFee(productCode,providerCode, ref adminFee, ref sellerFee, out xError))
            //{
            //    return false;
            //}
            ////Console.WriteLine("CEK DATA 3");

            //baseAmount = productAmount - adminFee;
            //customerFeeAmount = sellerFee;

            //Console.WriteLine("CEK DATA 4");
            return true;
        }

		private bool getAdminAndCustomerFeeFromProduct(string productCode, int quantity, string appId,	//string providerCode, 
			ref int adminFee, ref int customerFee, int TotalAmount = 0)
		{

			//if (!localDB.getAdminFeeAndCustomerFee(productCode, providerCode, TotalAmount,
			if (!localDB.getAdminFeeAndCustomerFee(productCode, quantity, appId, TotalAmount,
				ref adminFee, out xError))
			{
				return false;
			}

			//nilaiTransaksiKeProvider = decimal.ToInt32(totalAmount - adminFee);

			////int adminFee = 0;
			//int sellerFee = 0;
			//if (!localDB.getAdminFeeAndCustomerFee(productCode,providerCode, ref adminFee, ref sellerFee, out xError))
			//{
			//    return false;
			//}
			////Console.WriteLine("CEK DATA 3");

			//baseAmount = productAmount - adminFee;
			//customerFeeAmount = sellerFee;

			//Console.WriteLine("CEK DATA 4");
			return true;
		}

        //private bool InsertTransactionLog(string product_code, string distributor_phone, string reff_number,
        //    string amount, string trace_number, string trx_time)
        //{
        //    if (!localDB.insertTransactionLog(product_code, distributor_phone, reff_number,
        //            amount, trace_number, trx_time, out xError))
        //    {
        //        return false;
        //    }
        //    return true;
        //}

        private string productTransactionReversal()
        {
            return "";
        }

        public string ProviderInquiry(string appID, string userId, string securityToken, 
            string customerProductNumber, string productCode)
        {
            string httpReply = ""; // Finnet.productInquiry(appID, userId, customerProductNumber, productCode);

            bool fTrx = false;
            int productAmount = 0;
            int traceNumb = 0;
            string strJson = "";
            DateTime skrg = DateTime.Now;
            DateTime trxInquiryTime = skrg;
            string strRecJson = "";
            DateTime trxInquiryRecTime = skrg;
            Exception exrr = null;
            int adminFee = 0;
            string trxNumber = localDB.getProductTrxNumber(out xError);

            PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
            try
            {
                providerProduct = localDB.getProviderProductInfo(productCode, out exrr);
                if (exrr != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider product data not found", "");
                }
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting provider product data", "");
            }
            if (providerProduct.ProductType == PPOBDatabase.PPOBdbLibs.ProductTypeEnum.PRODUCT)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Could not inquiry for a product", "");
            }

            try
            {
                //Console.WriteLine(" productAmount " + productAmount);
                //Console.WriteLine(" intNYA : " + int.Parse(productAmount));
				//if (!getBaseAndFeeAmountFromProduct(productCode, providerProduct.ProviderCode,
				if (!getBaseAndFeeAmountFromProduct(productCode, 1, appID,
                    ref adminFee))
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Fee data not found", "");
                }
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error get fee data : " + ex.getCompleteErrMsg());
                //Console.WriteLine(ex.StackTrace);
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Error get fee data", "");
            }

            LogWriter.show(this, "Inquiry from " + userId);
            if (providerProduct.ProviderCode == "004")  //Finnet 
            {
                using (FinnetHandler.FinnetTransactions Finnet = 
                    new FinnetHandler.FinnetTransactions(commonSettings))
                {
                    Finnet.securityToken = securityToken;
                    fTrx = Finnet.productInquiry(appID, userId, customerProductNumber, providerProduct.ProviderProductCode,
                        adminFee, providerProduct.fIncludeFee,
                        ref productAmount, ref httpReply, ref traceNumb, ref strJson, ref trxInquiryTime,
                        ref strRecJson, ref trxInquiryRecTime, trxNumber);
                }
            }
            else if (providerProduct.ProviderCode == "117")  //FM
            {
                using (FMHandler.FmTransactions FM =
                    new FMHandler.FmTransactions(commonSettings))
                {
                    FM.securityToken = securityToken;
                    fTrx = FM.productInquiry(appID, userId, customerProductNumber, providerProduct.ProviderProductCode,
                        adminFee, providerProduct.fIncludeFee,
                        ref productAmount, ref httpReply, ref traceNumb, ref strJson, ref trxInquiryTime,
                        ref strRecJson, ref trxInquiryRecTime, trxNumber);
                }
            }
			else if (providerProduct.ProviderCode == "124")  // LEOPARD
			{
				using (PH_LeopardHandler.LeopardTransactions Leopard =
					new PH_LeopardHandler.LeopardTransactions(commonSettings))
				{
					Leopard.securityToken = securityToken;
					fTrx = Leopard.productInquiry(appID, userId, customerProductNumber, providerProduct.ProviderProductCode,
						adminFee, providerProduct.fIncludeFee,
						ref productAmount, ref httpReply, ref traceNumb, ref strJson, ref trxInquiryTime,
						ref strRecJson, ref trxInquiryRecTime, trxNumber);
				}
			}
            else
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "No provider found for this service", "");
            }

            if (!localDB.insertCompleteInquiryLog(productCode, providerProduct.ProviderProductCode,
                userId.Substring(commonSettings.getString("UserIdHeader").Length), customerProductNumber,
                productAmount.ToString(), traceNumb.ToString(), trxInquiryTime.ToString("yyyy-MM-dd HH:mm:ss"),
                0, 0, 
                strJson,
                trxInquiryTime.ToString("yyyy-MM-dd HH:mm:ss"),
                strRecJson,
                trxInquiryRecTime.ToString("yyyy-MM-dd HH:mm:ss"),
                "",skrg.ToString("yyyy-MM-dd HH:mm:ss"),"",skrg.ToString("yyyy-MM-dd HH:mm:ss"),
                fTrx,
                out xError))
            {
                // Jadwalkan masuk database
                //TransactionLog.writeInquiry(productCode, userId.Substring(2), customerProductNumber,
                //        productAmount, productAmount, 0, traceNumb.ToString(),
                //        (trxInquiryTime == null) ? skrg.ToString("yyyy-MM-dd HH:mm:ss") : trxInquiryTime.ToString("yyyy-MM-dd HH:mm:ss"),
                //        0, 0, strJson,
                //        (trxInquiryTime == null) ? skrg.ToString("yyyy-MM-dd HH:mm:ss") : trxInquiryTime.ToString("yyyy-MM-dd HH:mm:ss"),
                //        "", null, strJson, trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
                //        strRecJson, trxRecTime.ToString("yyyy-MM-dd HH:mm:ss"), fTrx);
            }
            return httpReply;
        }

		public string Process_PrepaidInquiry(string appID, string productCode)
		{
			string httpReply = "";

			Exception exrr = null;
			int adminFee = 0;
			int distributorFee = 0;
			int sellPrice = 0;
			string productName = "";

			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
			try
			{
				providerProduct = localDB.getProviderProductInfo(productCode, out exrr);
				if (exrr != null)
				{
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider product data not found", "");
				}
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting provider product data", "");
			}
//			if (providerProduct.ProductType == PPOBDatabase.PPOBdbLibs.ProductTypeEnum.PRODUCT)
//			{
//				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Could not inquiry for a product", "");
//			}

			try
			{
				//Console.WriteLine(" productAmount " + productAmount);
				//Console.WriteLine(" intNYA : " + int.Parse(productAmount));
				if (!localDB.getPriceAndFeeProduct(productCode, 1, providerProduct.ProviderCode,0, 
					ref productName, ref sellPrice, ref adminFee, ref distributorFee,out exrr))
				{
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Fee data not found", "");
				}
			}
			catch (Exception ex)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error get fee data : " + ex.getCompleteErrMsg());
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Error get fee data", "");
			}

			jsonConv.Clear();
			jsonConv.Add("fiProductCode", productCode);
			jsonConv.Add("fiProductName", productName);
			jsonConv.Add("fiProductPrice", sellPrice);
			jsonConv.Add("fiDistributorFee", distributorFee);
			jsonConv.Add("fiResponseCode", "00");
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
		}

		//public string publicCustomParameter1 = "";
		//public JsonLibs.MyJsonLib jsonTrxFromClient;

        private string ProviderTransaction(string appID, string userId, string securityToken, int PpobType,
			string customerProductNumber, string productCode, int productAmount, int Quantity,
            string userPhone = "", string ownerId = "", string ownerPhone = "")
        {
            string errCode = "00"; string errMessage = "";
			string httpReply = "";
            Exception exrr = null;

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
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider product data not found", "");
                }
            }
			catch(Exception ex)
            {
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error get provider product data : " + ex.getCompleteErrMsg());
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting provider product data", "");
            }

            // cek keberadaan user di database
            if (!localDB.isAccountExistById(userId, out exrr))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "493", "No user found", "");
            }

            if (ownerId != "")
            {
                if (!localDB.isAccountExistById(ownerId, out exrr))
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "493", "No seller found", "");
                }
            }

            //Console.WriteLine("ProviderCode = " + providerProduct.ProviderCode);
            // ============ Product Transaction Filter
            if (
                (providerProduct.ProviderCode != "004")  //Finnet 
				&& (providerProduct.ProviderCode != "113")  //Persada
				&& (providerProduct.ProviderCode != "124")  //LEOPARD
                && (providerProduct.ProviderCode != "117")  //FM
				&& (providerProduct.ProviderCode != "119")  //Toko Online
				&& (providerProduct.ProviderCode != "121")  //Nitrogen transactions
				&& (providerProduct.ProviderCode != "000")  //DAM .... Top Up dan purchase Iconox
                )
            {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Unregistered provider: " + providerProduct.ProviderCode, "");
            }

			//			Console.WriteLine ("DEBUG -- productAmount= "+productAmount);
			//			Console.WriteLine ("DEBUG -- adminFee= "+adminFee);

			int cardProductAmount = productAmount;	// 100% nya
			decimal topUpPercentFee = 0;
			bool isPembayaranDenganKartu = false;

			providerProduct.CurrentPrice *= Quantity;	// pengali jika produk

			// SERVICE/PEMBAYARAN amount diambil dari productAmount dari client, 
			// jika pembelian, amount diambil dari currentPrice
			int nilaiTransaksiKeProvider = providerProduct.CurrentPrice;
			int nilaiYangMasukLog = providerProduct.CurrentPrice;

			int productAmountDenganKartu=0;
			//			hitungan ini disimpan dimana????
			//			karena ini product, bukan pembayaran
			if ((providerProduct.ProductGroupId == 14) // Grup nitrogen
				&& (((string)((JsonLibs.MyJsonLib)jsonConv["fiAdditional"])["fiLogPurchase"]).Length!=0)		// Khusus penggunaan kartu prabayar (Iconox)
				// Selain TopUp
				&& (providerProduct.ProviderProductCode != commonSettings.getString ("IconoxTopUpProviderProductCode"))
				&& (providerProduct.ProviderProductCode != commonSettings.getString ("IconoxActivationProviderProductCode"))) {
				// Disini ada kekhususan,dimana nilai yang di transaksikan harus di kurangi dulu dengan admin fee saat topup, 
				// untuk supaya match dengan dana di ACCOUNT TITIPAN. Baru setelah itu, hasilnya di proses seperti
				// pengambilan fee biasa
				if (providerProduct.ProductType != PPOBDatabase.PPOBdbLibs.ProductTypeEnum.PRODUCT) {
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "496", "Unimplemented yet!", "");
				}

				isPembayaranDenganKartu = true;

				try {
					//					if (!localDB.getPercentAdminFee (commonSettings.getString ("IconoxTopUpClientProductCode"),
					//						    providerProduct.ProviderCode, ref topUpPercentFee, out xError)) {
					//						LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get TopUp fee percent data");
					//						return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get TopUp fee percent data", "");
					//					}
					// ProviderCode diganti 000 khusus untuk ambil data topup
					if (!localDB.getPercentAdminFee (commonSettings.getString ("IconoxTopUpClientProductCode"),
						"000", ref topUpPercentFee, out xError)) {
						LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get TopUp fee percent data");
						return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get TopUp fee percent data", "");
					}
					// NOTE : Disini nanti dana diambil dari Account Iconox TITIPAN atau bukan tergantung dengan kartu atau bukannya

					productAmount = providerProduct.CurrentPrice;
					cardProductAmount = productAmount;	// 100% nya

					LogWriter.showDEBUG (this, "productAmount = "+productAmount.ToString ());

					productAmountDenganKartu = productAmount - ((int)Math.Ceiling (productAmount * (topUpPercentFee / 100))); // 99% nya
					productAmount = productAmountDenganKartu;

					// TRIK supaya bisa masuk bispro purchase
					providerProduct.CurrentPrice = productAmount;

					LogWriter.showDEBUG (this, "productAmountDenganKartu = "+productAmountDenganKartu.ToString ());

					//			Console.WriteLine ("DEBUG -- topUpPercentFee= "+topUpPercentFee);
					//			Console.WriteLine ("DEBUG -- cardProductAmount= "+cardProductAmount);

				} catch (Exception ex) {
					LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get topup fee data : " + ex.getCompleteErrMsg ());
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get topup fee data", "");
				}
			} 

			if (isPembayaranDenganKartu) {
				try {
					//LogWriter.showDEBUG (this, " productAmount: " + productAmount);
					//if (!getBaseAndFeeAmountFromProduct (productCode, providerProduct.ProviderCode,
					if (!getBaseAndFeeAmountFromProduct (productCode, Quantity, appID,
						ref adminFee, cardProductAmount)) {
						return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Fee data not found", "");
					}
					//  cardProductAmount = 100.000, adminFee=4000, ke nu masuk db productAmount: 99.000, adminFee 4000
				} catch (Exception ex) {
					LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get fee data : " + ex.getCompleteErrMsg ());
					//Console.WriteLine(ex.StackTrace);
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get fee data", "");
				}
			} else {
				try {
					//LogWriter.showDEBUG (this, " productAmount: " + productAmount);
					//if (!getBaseAndFeeAmountFromProduct (productCode, providerProduct.ProviderCode,
					if (!getBaseAndFeeAmountFromProduct (productCode, Quantity, appID,
						   ref adminFee, productAmount)) {
						return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Fee data not found", "");
					}
				} catch (Exception ex) {
					LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get fee data : " + ex.getCompleteErrMsg ());
					//Console.WriteLine(ex.StackTrace);
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get fee data", "");
				}
			}


			if (providerProduct.ProductType == PPOBDatabase.PPOBdbLibs.ProductTypeEnum.PRODUCT)
			{
				LogWriter.showDEBUG (this,"Asup Product Purchase");
				if (providerProduct.fIncludeFee)
				{
					LogWriter.showDEBUG (this,"Asup Product Purchase Include fee");
					productAmount = providerProduct.CurrentPrice;		// untuk kartu, disini udah 99.000 dari 100.000
					nilaiYangMasukLog = productAmount - adminFee;		// disini adminfee udah 4000, dari 4%
					LogWriter.showDEBUG (this,"1. Product Purchase productAmount = " + productAmount.ToString ());
				}
				else
				{
					LogWriter.showDEBUG (this,"Asup Product Purchase Exclude fee");
					productAmount = providerProduct.CurrentPrice + adminFee;
					nilaiYangMasukLog = providerProduct.CurrentPrice;
				}
			}
			else
			{
				LogWriter.showDEBUG (this,"Asup Service Payment");
				if (providerProduct.fIncludeFee)
				{
					LogWriter.showDEBUG (this,"Asup Service Payment Include fee");
					providerProduct.CurrentPrice = productAmount;
					nilaiYangMasukLog = productAmount - adminFee;
				}
				else
				{
					LogWriter.showDEBUG (this,"Asup Service Payment Exclude fee");
					providerProduct.CurrentPrice = productAmount - adminFee;
					nilaiYangMasukLog = providerProduct.CurrentPrice;
				}

//				LogWriter.showDEBUG (this, "DEBUG -- fIncludeFee= "+providerProduct.fIncludeFee);
//				LogWriter.showDEBUG (this, "DEBUG -- CurrentPrice= "+providerProduct.CurrentPrice);
//				LogWriter.showDEBUG (this, "DEBUG -- nilaiYangMasukLog= "+nilaiYangMasukLog);
//
				if (providerProduct.CurrentPrice <= 0)
				{
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Bad amount", "");
				}
			}

			LogWriter.showDEBUG (this, "nilaiYangMasukLog = "+nilaiYangMasukLog.ToString () 
				+ "\r\nproductAmount= " + productAmount.ToString ());


			// ==============================================================
			// ==== KHUSUSON UNTUK NITROGEN.... = TRANSAKSI OFFLINE
			// ==============================================================
			string SamCSN="";
			string OutletCode="";
			string trxUCardLog="";
			int cardBalance=0;
			// Untuk Provider DAM atau Nitrogen saja untuk pembayaran Iconox
			// ------------------------------------------------------
			// UNTUK KOMPATIBILITAS DENGAN PEMBAYARAN PRODUK LAIN
			bool pembayaranDariEwallet=false;	// transaksi purchase saja yang dengan kartu
			bool transaksiKartu = false;		// transaksi berhubungan dengan kartu, kecuali purchase tanpa kartu
			// ------------------------------------------------------

			if (((providerProduct.ProviderCode=="000") || (providerProduct.ProviderCode=="121")) &&

				(providerProduct.ProductGroupId == 14) &&		// Khusus penggunaan kartu prabayar (Iconox)
				// Selain TopUp
				(providerProduct.ProviderProductCode != commonSettings.getString("IconoxTopUpProviderProductCode"))){
				// ===== AMBIL PEMBAYARAN DARI AKUN TITIP Ewallet ICONOX
				// nilai yang di transferkan adalah nilai yang sudah di kurangi admin fee topup (1%)
				using (IconoxOnlineHandler.IconOxTransactions IconoxTrx =
					new IconoxOnlineHandler.IconOxTransactions(commonSettings))
				{
					IconoxTrx.securityToken = securityToken;
					IconoxTrx.trxAmount = cardProductAmount;
					IconoxTrx.agentPhone = userPhone;
					IconoxTrx.productCode = productCode;
					IconoxTrx.clientData = clientDataSource;
					IconoxTrx.providerCode = providerProduct.ProviderCode;
					try{
					IconoxTrx.AdditionalJson = ((JsonLibs.MyJsonLib)jsonConv["fiAdditional"]);
					}catch{
					}

					if(IconoxTrx.isPurchase (providerProduct.ProviderProductCode)){
						// cek jika sudah pernah terjadi transaksinya
						string failedREason = "No Problemo";
						if (IconoxTrx.isTransactionHasAlreadyDone (appID, ref httpReply, ref failedREason)) {
							// Jadi gak usah ambil pembayaran ulang...
							LogWriter.showDEBUG (this, failedREason + " => Naon tah hasilna");
							return httpReply;
						}
						LogWriter.showDEBUG (this, "ICONOX - Masuk 1");

						if(customerProductNumber==""){
							// ambil bayar dr akun petugas
							pembayaranDariEwallet=false;
						}else{
							// ambil bayar dari akun iconox
							pembayaranDariEwallet = true;
							//productAmount = productAmountDenganKartu;		// 99.000
						}

						LogWriter.showDEBUG (this, "ICONOX - Masuk : pembayaranDariEwallet = " + pembayaranDariEwallet.ToString () + ", productAmount = "+productAmount.ToString ());
						LogWriter.showDEBUG (this,"2. Product Purchase productAmount = " + productAmount.ToString ());

					}else{
						// transaksi topup atau aktivasi, atau pembayaran tanpa kartu, ambil bayar dari akun petugas
						pembayaranDariEwallet=false;

						LogWriter.showDEBUG (this, "ICONOX - BUkan Purchase");

					}
				}
			}else{
				pembayaranDariEwallet=false;

				LogWriter.showDEBUG (this, "ICONOX - Bukan transaksi ICONOX");

			}

			LogWriter.showDEBUG (this,"3. Product Purchase productAmount = " + productAmount.ToString ());

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
					if(productAmount>0){		// kalo pembayaran gratis, gak usah transfer
						if(pembayaranDariEwallet){
							// ====== PEMBAYARAN DARI EWALLET
							LogWriter.show(this, "Get payment from EWallet");
							//LogWriter.showDEBUG (this,"4. Product Purchase productAmount = " + productAmount.ToString ());
							if (!TransferReg.PayFromCustomerEwallet( 
								providerProduct.TransactionCodeSufix, PpobType, productAmount,
								TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.PPOB, 
								"Get payment from customer Ewallet", ref qvaInvoiceNumber, ref qvaReversalRequired, 
								ref errCode, ref errMessage))
							{
								if(qvaReversalRequired)
									TransferReg.Reversal(TransactionRef_id,qvaInvoiceNumber, ref errCode, ref errMessage);
								LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : [" + errCode + "]" + errMessage);
								return HTTPRestDataConstruct.constructHTTPRestResponse(400, errCode, errMessage, "");
							}
						}else{
		                    // ===== AMBIL PEMBAYARAN DARI CUSTOMER
							LogWriter.show(this, "Get payment from Customer");
							//LogWriter.showDEBUG (this,"5. Product Purchase productAmount = " + productAmount.ToString ());
		                    if (!TransferReg.PayFromCustomer(userId, 
								providerProduct.TransactionCodeSufix, PpobType, productAmount,
								TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.PPOB, 
								"Get payment from customer", ref qvaInvoiceNumber, ref qvaReversalRequired, 
								ref errCode, ref errMessage))
		                    {
								if(qvaReversalRequired)
									TransferReg.Reversal(TransactionRef_id,qvaInvoiceNumber, ref errCode, ref errMessage);
		                        LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : [" + errCode + "]" + errMessage);
								return HTTPRestDataConstruct.constructHTTPRestResponse(400, errCode, errMessage, "");
							}
						}
					}
				}
                catch (Exception ex)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : " + ex.getCompleteErrMsg());
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to do payment", "");
                }
            }

            // ===== Transaksi Product
            DateTime trxTime = DateTime.Now;
            DateTime trxRecTime = DateTime.Now;
            string strJson = "";
            string strRecJson = "";
            int traceNumb = 0;
            bool fTrx = false;
            DateTime skrg = DateTime.Now;
            string failedReason = "";
            string trxNumber = localDB.getProductTrxNumber(out xError);
            bool canReversal = false;
            bool isSuccessPayment = false;

            LogWriter.write(this, LogWriter.logCodeEnum.INFO, "Transaction in process from " + userId);

            // ============ Product Transaction Selector
            if (providerProduct.ProviderCode == "004")  //Finnet
            {
                using (FinnetHandler.FinnetTransactions Finnet =
                    new FinnetHandler.FinnetTransactions(commonSettings))
                {
                    Finnet.securityToken = securityToken;
                    fTrx = Finnet.productTransaction(appID, userId, customerProductNumber,
                        providerProduct.ProviderProductCode, nilaiTransaksiKeProvider.ToString(),
                        ref httpReply, ref traceNumb, ref strJson, ref trxTime, ref strRecJson,
                        ref trxRecTime, ref failedReason, ref canReversal, ref isSuccessPayment,
                        PpobType, trxNumber);
                }
            }
            else if (providerProduct.ProviderCode == "113")  //Persada
            {
                //using (PersadaHandler.PersadaTransactions Persada =
                //    new PersadaHandler.PersadaTransactions(commonSettings))
                //{
                //    //fTrx = Persada.productTransaction(customerProductNumber, providerProduct.ProviderProductCode);
                //    Persada.securityToken = securityToken;
                //    fTrx = Persada.productTransaction(appID, userId, customerProductNumber,
                //        providerProduct.ProviderProductCode, nilaiTransaksiKeProvider.ToString(),
                //        ref httpReply, ref traceNumb, ref strJson, ref trxTime, ref strRecJson,
                //        ref trxRecTime, ref failedReason, ref canReversal, ref isSuccessPayment,
                //        PpobType, trxNumber);
                //}

                using (PH_PersadaHandler.PersadaTransactions Persada =
                    new PH_PersadaHandler.PersadaTransactions(commonSettings))
                {
                    //fTrx = Persada.productTransaction(customerProductNumber, providerProduct.ProviderProductCode);
                    Persada.securityToken = securityToken;
                    fTrx = Persada.productTransaction(appID, userId, customerProductNumber,
                        providerProduct.ProviderProductCode, nilaiTransaksiKeProvider.ToString(),
                        ref httpReply, ref traceNumb, ref strJson, ref trxTime, ref strRecJson,
                        ref trxRecTime, ref failedReason, ref canReversal, ref isSuccessPayment,
                        PpobType, trxNumber);
                }

            }
			else if (providerProduct.ProviderCode == "124")  //LEOPARD
			{
				using (PH_LeopardHandler.LeopardTransactions Leopard =
					new PH_LeopardHandler.LeopardTransactions(commonSettings))
				{
					//fTrx = Persada.productTransaction(customerProductNumber, providerProduct.ProviderProductCode);
					//providerProduct.ProductType == PPOBDatabase.PPOBdbLibs.ProductTypeEnum.PRODUCT
					Leopard.TrxType = providerProduct.ProductType;
					Leopard.securityToken = securityToken;

//					if((jsonTrxFromClient != null) && (jsonTrxFromClient.isExists ("fiBillReff")))
//						Leopard.inqBillReff = (string)jsonTrxFromClient["fiBillReff"];

//					if(jsonConv.isExists ("fiBillReff"))
//						Leopard.inqBillReff = (string)jsonConv["fiBillReff"];
					try{
					if(jsonConv.isExists ("fiAdditional"))
						Leopard.AdditionalJson = ((JsonLibs.MyJsonLib)jsonConv["fiAdditional"]);
					}catch{
						return HTTPRestDataConstruct.constructHTTPRestResponse(400, "494", "Invalid additional json", "");
					}

					fTrx = Leopard.productTransaction(appID, userId, customerProductNumber,
						providerProduct.ProviderProductCode, nilaiTransaksiKeProvider.ToString(),
						ref httpReply, ref traceNumb, ref strJson, ref trxTime, ref strRecJson,
						ref trxRecTime, ref failedReason, ref canReversal, ref isSuccessPayment,
						PpobType, trxNumber);
				}

			}
            else if (providerProduct.ProviderCode == "117")  //FM
            {
                using (FMHandler.FmTransactions FM =
                    new FMHandler.FmTransactions(commonSettings))
                {
                    FM.securityToken = securityToken;
                    fTrx = FM.productTransaction(appID, userId, customerProductNumber,
                        providerProduct.ProviderProductCode, nilaiTransaksiKeProvider.ToString(),
                        ref httpReply, ref traceNumb, ref strJson, ref trxTime, ref strRecJson,
                        ref trxRecTime, ref failedReason, ref canReversal, ref isSuccessPayment,
                        PpobType, trxNumber);
                }
            }
            else if (providerProduct.ProviderCode == "119")  // Toko Online
            {
                using (PasarHandler.PasarTransactions Pasar =
                    new PasarHandler.PasarTransactions(commonSettings))
                {
					// Butuh parameter tambahan
                    Pasar.OwnerId = ownerId;
                    Pasar.OwnerPhone = ownerPhone;
                    Pasar.CustomerPhone = userPhone;
                    Pasar.securityToken = securityToken;
                    fTrx = Pasar.productTransaction(appID, userId, customerProductNumber,
                        providerProduct.ProviderProductCode, nilaiTransaksiKeProvider.ToString(),
                        ref httpReply, ref traceNumb, ref strJson, ref trxTime, ref strRecJson,
                        ref trxRecTime, ref failedReason, ref canReversal, ref isSuccessPayment,
                        PpobType, trxNumber);
                }
            }
			else if ((providerProduct.ProviderCode == "000")  // DAM
				|| (providerProduct.ProviderCode == "121"))		// dan NITROGEN
			{
				using (IconoxOnlineHandler.IconOxTransactions IconoxTrx =
					new IconoxOnlineHandler.IconOxTransactions(commonSettings))
				{
					IconoxTrx.securityToken = securityToken;
					IconoxTrx.trxAmount = cardProductAmount;
					IconoxTrx.agentPhone = userPhone;
					IconoxTrx.productCode = productCode;
					IconoxTrx.clientData = clientDataSource;
					IconoxTrx.providerCode = providerProduct.ProviderCode;
					IconoxTrx.uCardLog_Quantity = Quantity;
					IconoxTrx.AdditionalJson = ((JsonLibs.MyJsonLib)jsonConv["fiAdditional"]);

					fTrx = IconoxTrx.productTransaction(appID, userId, customerProductNumber,
						providerProduct.ProviderProductCode, nilaiTransaksiKeProvider.ToString(),
						ref httpReply, ref traceNumb, ref strJson, ref trxTime, ref strRecJson,
						ref trxRecTime, ref failedReason, ref canReversal, ref isSuccessPayment,
						PpobType, trxNumber);

					transaksiKartu = IconoxTrx.isCardTransaction;
					SamCSN = IconoxTrx.uCardLog_SamCSN;
					OutletCode = IconoxTrx.uCardLog_OutletCode;
					trxUCardLog = IconoxTrx.uCardLog_CardPurchaseLog;
					cardBalance = IconoxTrx.uCardLog_PreviousBalance;
					if ((failedReason == "") || (failedReason=="-"))
						failedReason = IconoxTrx.uCardLog_Description;

					LOG_Handler.LogWriter.showDEBUG (this, "uCardLog_Description = " + IconoxTrx.uCardLog_Description + 
						"\r\nfailedReason = " + failedReason);

//					if (IconoxTrx.sudahBayar) {
//
//					}
				}
			}
            // else untuk lain2 

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
				//isSuccessPayment, 
				(isSuccessPayment && fTrx), 
				failedReason, trxNumber, canReversal, providerProduct.fIncludeFee, SamCSN, OutletCode, 
                        out xError))
            {
				LogWriter.showDEBUG (this, "Gagal Insert Log....!! CEK LOG DI FILE");
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
//					//isSuccessPayment, 
//					(isSuccessPayment && fTrx), 
//					failedReason, canReversal);
				// Sementara lanjutkan proses...
            }

			// JIka transaksi kartu, maka simpan di table ucard_transaction
			if (transaksiKartu) {
				LogWriter.showDEBUG (this, "== Add CardTransactionLog ");
				if (!localDB.addCardTransactionLog (TransactionRef_id,OutletCode,trxUCardLog,cardBalance,
					out xError)) {
					LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "FATAL Failed to save Card Transaction Log");
//					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492",
//						"Failed to save card transaction log", "");
				}
			}

            // Sisanya BISPRO transfer dilakukan oleh service lain untuk fee dan pembayaran ke finnet

            //if (providerProduct.ProductType == PPOBDatabase.PPOBdbLibs.ProductTypeEnum.SERVICE)
            //{
            if (((!fTrx) && (!canReversal)) || (fTrx && (!isSuccessPayment)))  // hanya untuk 
            {
				if (productAmount > 0) {	// jika ada nilai, boleh reversal
					LogWriter.showDEBUG (this, "=========== BALIKIN PEMBAYARAN ==========");
					using (TransferReguler.CollectTransfer TransferReg =
						                  new TransferReguler.CollectTransfer (commonSettings)) {
						try {
							// ===== KEMBALIKAN PEMBAYARAN DARI CUSTOMER
							LogWriter.show (this, "Pay back to customer");
							// Disini tidak usah di deteksi gagal tidaknya, 
							// karena sudah masuk log dan dilakukan background jika belum berhasil
							TransferReg.Reversal (TransactionRef_id, qvaInvoiceNumber, ref errCode, ref errMessage);

//						if (!TransferReg.ReversalToCustomer(userId,
//							providerProduct.TransactionCodeSufix, productAmount, TransactionRef_id,
//                            PPOBDatabase.PPOBdbLibs.eTransactionType.PPOB, "Transfer back to customer",
//                            ref errCode, ref errMessage))
//                        {
//                            LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Reversal failed to " + userId +
//                                " TrxNumber: " + trxNumber + " : [" + errCode.ToString() + "]" + errMessage);
//							return HTTPRestDataConstruct.constructHTTPRestResponse(400, errCode.ToString(), "Transaction failed, Failed to do reversal TrxNumber: " + trxNumber.ToString(), "");
//                        }
						} catch (Exception ex) {
							LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Reversal failed to " + userId +
							" TrxNumber: " + trxNumber + " : [" + errCode.ToString () + "]" + ex.getCompleteErrMsg ());
							return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Transaction failed, Failed to do reversal TrxNumber: " + trxNumber.ToString (), "");
						}
					}
				}
            }
            //}

			//LogWriter.showDEBUG(this, "---- DEBUG  TRX 4");
            if (fTrx && isSuccessPayment)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.INFO, "Transaction success from " + userId);
                return httpReply;
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

                return httpReply;
            }
        }

        bool revUlang = false;
        private string ProviderReversalUlang(string appID, string userId, string securityToken, int transactionType,
            string customerProductNumber, string productCode, int productAmount)
        {
            revUlang = true;
            return ProviderReversal(appID, userId, securityToken, transactionType,
            customerProductNumber, productCode, productAmount);
        }

        //ProviderReversal
        private string ProviderReversal(string appID, string userId, string securityToken, int transactionType,
            string customerProductNumber, string productCode, int productAmount)
        {
            string errCode = "00"; string errMessage = "";
            string httpReply = "";

            //int baseAmount = 0;
            int adminFee = 0;
            //int customerFeeAmount = 0;
            Exception exrr = null;
            PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
            //Console.WriteLine("ProductCode = " + productCode);
            try
            {
                providerProduct = localDB.getProviderProductInfo(productCode, out exrr);
                if (exrr != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider product data not found", "");
                }
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting provider product data", "");
            }

            //Console.WriteLine("ProviderCode = " + providerProduct.ProviderCode);
            // ============ Product Transaction Filter
            if (
                (providerProduct.ProviderCode != "004") && //Finnet 
                //&& (providerProduct.ProviderCode != "113")  //Persada
                //&& (providerProduct.ProviderCode != "117")  //FM
                (providerProduct.ProviderCode != "117")  //FM
                )
            {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Unregistered provider: " + providerProduct.ProviderCode, "");
            }

			int Quantity = 1;

            try
            {
				//if (!getBaseAndFeeAmountFromProduct(productCode, providerProduct.ProviderCode,
				if (!getBaseAndFeeAmountFromProduct(productCode, Quantity, appID,
                    ref adminFee, productAmount))
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Fee data not found", "");
                }
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error get fee data : " + ex.getCompleteErrMsg());
                //Console.WriteLine(ex.StackTrace);
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Error get fee data", "");
            }

            // SERVICE/PEMBAYARAN amount diambil dari productAmount dari client, 
            // jika pembelian, amount diambil dari currentPrice

            if (providerProduct.ProductType == PPOBDatabase.PPOBdbLibs.ProductTypeEnum.PRODUCT)
            {
                productAmount = providerProduct.CurrentPrice + adminFee;
            }
            else
            {
                providerProduct.CurrentPrice = productAmount - adminFee;
                if (providerProduct.CurrentPrice <= 0)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Bad amount", "");
                }
            }
            //productAmount = prdAmount.ToString();
			int nilaiTransaksiKeProvider = providerProduct.CurrentPrice;

            //bool bypassReversal = false;
            bool isLastTransaction = false;

            long trxRefId = 0;
            // lakukan reversal ke provider, jika berhasil reversal ke provider maka lakukan 
            // transfer reguler pengembalian dana ke customer sejumlah nilaiTransaksiKeProvider
            //adminFee nu udah ke bagi gimana???
            // cek apakah sudah ada di log transaksi
            if (!localDB.isThisUserLastTransaction(productCode, providerProduct.ProviderProductCode,
                userId.Substring(commonSettings.getString("UserIdHeader").Length), customerProductNumber, nilaiTransaksiKeProvider.ToString(),
                adminFee.ToString(), providerProduct.ProviderCode, providerProduct.CogsPriceId,
                true, ref trxRefId, out xError))
            {
                isLastTransaction = false;
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Database connection problem", "");
                }
                //bypassReversal = true;
                LogWriter.write(this, LogWriter.logCodeEnum.INFO, "TrxID : " + trxRefId.ToString() + " : Last transaction data TO REVERSAL NOT FOUND\r\n" +
                    productCode + ":" + providerProduct.ProviderProductCode);
                //LogWriter.showDEBUG(this, "REVERSAL DATA NOT FOUND");
                //return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Transaction can not reverse", "");
            }
            else
            {
                isLastTransaction = true;
                LogWriter.write(this, LogWriter.logCodeEnum.INFO, "TrxID : " + trxRefId.ToString() + " : Last transaction data TO REVERSAL FOUND\r\n" +
                    productCode + ":" + providerProduct.ProviderProductCode);
            }

            DateTime trxTime = DateTime.Now;
            DateTime trxRecTime = DateTime.Now;
            string strJson = "";
            string strRecJson = "";
            int traceNumb = 0;
            bool fReversal = false;
            DateTime skrg = DateTime.Now;
            string failedReason = "";
            bool isSuccessReversal = false;

            string trxNumber = localDB.getProductTrxNumber(out xError);

            LogWriter.write(this, LogWriter.logCodeEnum.INFO, "REVERSAL in process from " + userId);
            // Lakukan reversal
            if (providerProduct.ProviderCode == "117")  //FM
            {
                // REVERSAL
                using (FMHandler.FmTransactions FM =
                    new FMHandler.FmTransactions(commonSettings))
                {
                    FM.securityToken = securityToken;
                    fReversal = FM.productReversal(appID, userId, customerProductNumber,
                        providerProduct.ProviderProductCode, nilaiTransaksiKeProvider.ToString(),
                        ref httpReply, ref traceNumb, ref strJson, ref trxTime,
                        ref strRecJson, ref trxRecTime, ref isSuccessReversal, ref failedReason, trxNumber);
                }
            }
            else if (providerProduct.ProviderCode == "004")  //Finnet
            {
                // REVERSAL
                using (FinnetHandler.FinnetTransactions Finnet =
                    new FinnetHandler.FinnetTransactions(commonSettings))
                {
                    isSuccessReversal = revUlang;       // nebeng flag reversal ulang
                    Finnet.securityToken = securityToken;
                    fReversal = Finnet.productReversal(appID, userId, customerProductNumber,
                        providerProduct.ProviderProductCode, nilaiTransaksiKeProvider.ToString(),
                        ref httpReply, ref traceNumb, ref strJson, ref trxTime,
                        ref strRecJson, ref trxRecTime, ref isSuccessReversal, ref failedReason,
                        trxNumber);
                }
            }
            //else providerProduct lain

            //if (!fReversal)
            //{
            //    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "494", "Failed to do reversal", "");
            //}

            // Masukan log reversal .... HADOOHHHH table lageee
            //long trxRefId = localDB.getTransactionReffIdSequence(out xError);

            localDB.insertReversalLog(PPOBDatabase.PPOBdbLibs.eTransactionType.PPOB, trxRefId,
                skrg.ToString("yyyy-MM-dd HH:mm:ss"), nilaiTransaksiKeProvider,
                isLastTransaction, isSuccessReversal, failedReason, out xError);

            //if (isSuccessReversal && (!bypassReversal))
            if (isSuccessReversal && isLastTransaction)
            {
//                LogWriter.showDEBUG(this, "PROCESS REVERSAL REAL");

                localDB.updateUserLastTransactionLog(trxRefId, out xError);
            //}

            //// transfer balik amount ke customer
            //if (isSuccessReversal && (!bypassReversal) && fReversal)
            //{
                using (TransferReguler.CollectTransfer TransferReg =
                    new TransferReguler.CollectTransfer(commonSettings))
                {
                    try
                    {
                        // ===== KEMBALIKAN PEMBAYARAN DARI CUSTOMER
                        LogWriter.show(this, "Pay back to customer");
						// FIXME : Disini kudu bikin transfer yang harus berhasil
						// TODO : ATAU DISINI musti query supaya dapet invoice number qva
						TransferReg.TransferBackToCustomer(userId, 
							providerProduct.TransactionCodeSufix, productAmount,
							trxRefId, PPOBDatabase.PPOBdbLibs.eTransactionType.PPOB,
							"Reversal transfer triggered from client request",
							ref errCode, ref errMessage);
						// didalam fungsi TransferReg.TransferBackToCustomer diatas, meskipun transfer gagal
						// proses transfer akan dilanjutkan secara background sampai berhasil

//                        if (!TransferReg.ReversalToCustomer(userId,
//							providerProduct.TransactionCodeSufix, productAmount,
//                            trxRefId, PPOBDatabase.PPOBdbLibs.eTransactionType.PPOB,
//                            "Reversal transfer triggered from client request",
//                            ref errCode, ref errMessage))
//                        {
//                            LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Reversal failed to " + userId +
//                                " TrxNumber: " + trxNumber + " : [" + errCode.ToString() + "]" + errMessage);
//                            return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Transaction failed, Failed to do reversal TrxNumber: " + trxNumber.ToString(), "");
//                        }
                    }
                    catch (Exception ex)
                    {
                        LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Reversal failed to " + userId +
                            " TrxNumber: " + trxNumber + " : [" + errCode.ToString() + "]" + ex.getCompleteErrMsg());
                        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Transaction failed, Failed to do reversal TrxNumber: " + trxNumber.ToString(), "");
                    }
                }
            }
            //else
            //    LogWriter.showDEBUG(this, "PROCESS REVERSAL FAKE");

            return httpReply;

            //            return HTTPRestDataConstruct.constructHTTPRestResponse(400, "477", "UNCOMPLETED CODE", "");

        }

		private string ProductPrepaidInquiry(HTTPRestConstructor.HttpRestRequest clientData)
		{
			int productAmount = 0;
			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}

			LogWriter.show(this, "Json received from client :\r\n" + clientData.Body);

			if ((!jsonConv.ContainsKey("fiApplicationId")) || 
				(!jsonConv.ContainsKey("fiProductCode")))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory field not found", "");
			}

			string appID="";
			string productCode = "";
			try
			{
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
				productCode = ((string)jsonConv["fiProductCode"]).Trim();
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type", "");
			}
			return Process_PrepaidInquiry (appID, productCode);
		}

        private string ProductTransaction(HTTPRestConstructor.HttpRestRequest clientData)
        {
            // Jika TransactionType : 0 => Pembayaran, 1 => Pembelian
            // input dari user diambil dari username dan password, baru bisa inquiry ke host
            int productAmount = 0;
            if (!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }

            LogWriter.show(this, "Json received from client :\r\n" + clientData.Body);

            if ((!jsonConv.ContainsKey("fiPhone")) || 
                (!jsonConv.ContainsKey("fiApplicationId")) || 
                (!jsonConv.ContainsKey("fiRequestCode")) || 
                (!jsonConv.ContainsKey("fiCustomerNumber")) || 
                (!jsonConv.ContainsKey("fiProductCode")) ||
                (!jsonConv.ContainsKey("fiTransactionType")))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory field not found", "");
            }

            string appID="";
			string unFormatedUserPhone = "";
            string userPhone="";
            string customerProductNumber = "";
            string productCode = "";
            int transactionType = 0;
            string ownerPhone = "";

            try
            {
                appID = ((string)jsonConv["fiApplicationId"]).Trim();
                userPhone = ((string)jsonConv["fiPhone"]).Trim();
                customerProductNumber = ((string)jsonConv["fiCustomerNumber"]).Trim();
                productCode = ((string)jsonConv["fiProductCode"]).Trim();
                transactionType = (int)jsonConv["fiTransactionType"];
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type", "");
            }
			unFormatedUserPhone = userPhone;
            ReformatPhoneNumber(ref userPhone);

            if (jsonConv.ContainsKey("fiAmount"))
            {
                try
                {
                    productAmount = (int)jsonConv["fiAmount"];
                }
                catch
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Bad amount", "");
                }
            }

            if (jsonConv.ContainsKey("fiOwnerPhone"))
            {
                try
                {
                    ownerPhone = ((string)jsonConv["fiOwnerPhone"]).Trim();
                    ReformatPhoneNumber(ref ownerPhone);
                }
                catch
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Bad owner phone format", "");
                }
            }

            int requestCode = 0;
            
            try{
                requestCode = int.Parse(((string)jsonConv["fiRequestCode"]).Trim());
            }
            catch {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Undefined request code", "");
            }
            string userId = cUserIDHeader + userPhone;
            string ownerId = cUserIDHeader + ownerPhone;

            if (userPhone.Length == 0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
            }

			int quantity = 1;
			if (jsonConv.isExists ("fiQuantity")) {
				quantity = (int)jsonConv["fiQuantity"];
			}

			// perbaharui token hanya pada saat login saja
			string securityToken = "";
//			if (jsonConv.ContainsKey("fiToken"))
//			{
//			try
//			{
//				securityToken = ((string)jsonConv["fiToken"]).Trim();
//			}
//			catch{ 
//				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid token field", "");
//			}
//			}	//else securityToken = CommonLibrary.generateToken();

			string httprepl = "";
			if (!cek_TokenSecurity (userPhone, jsonConv, ref securityToken, ref httprepl)) {
				return httprepl;
			}

			//  Yang harus disiapkan
            // switch berdasarkan requestCode
            switch(requestCode)
            {
                case 0: // inquiry
                    return ProviderInquiry(appID, userId, securityToken, customerProductNumber, productCode);
                    //return Finnet.productInquiry(appID, userId, customerNumber, productCode);
                case 1: //transaction
                    if (ownerPhone == "")
                    {
                        return ProviderTransaction(appID, userId, securityToken, transactionType,
						customerProductNumber, productCode, productAmount, quantity, unFormatedUserPhone);
                    }
                    else
                    {
                        return ProviderTransaction(appID, userId, securityToken, transactionType,
							customerProductNumber, productCode, productAmount, quantity,
                            userPhone, ownerId, ownerPhone);
                    }
                case 2: //reversal
                    return ProviderReversal(appID, userId, securityToken, transactionType,
                            customerProductNumber, productCode, productAmount);
                case 3: //reversal
                    return ProviderReversalUlang(appID, userId, securityToken, transactionType,
                            customerProductNumber, productCode, productAmount);
                //return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "UnImplmented yet", "");
                default:
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Undefined request code", "");
            }
        }

        private bool getDate(string data, string format, out DateTime result)
        {
            return DateTime.TryParseExact(data, format,
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, out result);
        }

        private bool checkAirlinesProductCode(string productCode, ref string reply,
            ref PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct)
        {
            Exception exrr = null;
            string providerCode = "";
            
            try
            {
                providerProduct = localDB.getProviderProductInfo(productCode, out exrr);
                if (exrr != null)
                {
                    reply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider product data not found", "");
                    return false;
                }
            }
            catch
            {
                reply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting provider product data", "");
                return false;
            }
            providerCode = providerProduct.ProviderCode;
            if (providerCode == "115")  //Citilink
            {
                reply = "";
                return true;
            }
            else
            {
                reply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "No provider found for this service", "");
                return false;
            }
        }

        private string ProductAirlinesGetAvailability(HTTPRestConstructor.HttpRestRequest clientData)
        {
            if (!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }

            LogWriter.show(this, "Json received from client :\r\n" + clientData.Body);

            if ((!jsonConv.ContainsKey("fiPhone")) ||
                (!jsonConv.ContainsKey("fiApplicationId")) ||
                (!jsonConv.ContainsKey("fiOutboundBeginDate")) ||
                (!jsonConv.ContainsKey("fiOutboundEndDate")) ||
                (!jsonConv.ContainsKey("fiInboundBeginDate")) ||
                (!jsonConv.ContainsKey("fiInboundEndDate")) ||
                (!jsonConv.ContainsKey("fiDepartureStation")) ||
                (!jsonConv.ContainsKey("fiArrivalStation")) ||
                (!jsonConv.ContainsKey("fiPaxCountADT")) ||
                (!jsonConv.ContainsKey("fiPaxCountCHD")) ||
                (!jsonConv.ContainsKey("fiPaxCountINF")) ||
                (!jsonConv.ContainsKey("fiInboundOutbound")) ||
                (!jsonConv.ContainsKey("fiProductCode")) ||
                (!jsonConv.ContainsKey("fiSignature")))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory field not found", "");
            }

            string appID = "";
            string userPhone = "";
            string productCode = "";
            string clientSignature = "";
            DateTime outboundBeginDate;
            DateTime outboundEndDate;
            DateTime inboundBeginDate;
            DateTime inboundEndDate;
            string DepartureStation;
            string ArrivalStation;
            int PaxCountADT;
            int PaxCountCHD;
            int PaxCountINF;
            string InboundOutbound;

            try
            {
                appID = ((string)jsonConv["fiApplicationId"]).Trim();
                userPhone = ((string)jsonConv["fiPhone"]).Trim();
                productCode = ((string)jsonConv["fiProductCode"]).Trim();
                outboundBeginDate = DateTime.ParseExact((string)jsonConv["fiOutboundBeginDate"], "yyyy-MM-dd", null);
                outboundEndDate = DateTime.ParseExact((string)jsonConv["fiOutboundEndDate"], "yyyy-MM-dd", null);
                inboundBeginDate = DateTime.ParseExact((string)jsonConv["fiInboundBeginDate"], "yyyy-MM-dd", null);
                inboundEndDate = DateTime.ParseExact((string)jsonConv["fiInboundEndDate"], "yyyy-MM-dd", null);
                DepartureStation = ((string)jsonConv["fiDepartureStation"]);
                ArrivalStation = ((string)jsonConv["fiArrivalStation"]);
                PaxCountADT = ((int)jsonConv["fiPaxCountADT"]);
                PaxCountCHD = ((int)jsonConv["fiPaxCountCHD"]);
                PaxCountINF = ((int)jsonConv["fiPaxCountINF"]);
                InboundOutbound = ((string)jsonConv["fiInboundOutbound"]);
                clientSignature = (string)jsonConv["fiSignature"];
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
            }

			ReformatPhoneNumber(ref userPhone);

			string token = "";
			string httprepl = "";
			if (!cek_TokenSecurity (userPhone, jsonConv, ref token, ref httprepl)) {
				return httprepl;
			}

            string userId = cUserIDHeader + userPhone;

            if (userPhone.Length == 0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid phone number", "");
            }

//            if (!localDB.isAccountExist(userPhone, out xError))
//            {
//                if (xError != null)
//                {
//                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to check user in database", "");
//                }
//                else
//                {
//                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "User unregistered", "");
//                }
//            }
//
            string resp="";
            PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct = null;
            if (!checkAirlinesProductCode(productCode, ref resp, ref providerProduct))
            {
                return resp;
            }
            string providerCode = providerProduct.ProviderCode;

            bool isError = false;
            string errMssg = "";
            JsonLibs.MyJsonLib ret;

            if (providerCode == "115")      // jika citilink
            {
                //commonSettings.SettingCollection.Clear();
                commonSettings.ReloadSettings();
                CitilinkLib.CitilinkHandler ch = new CitilinkLib.CitilinkHandler(
                    commonSettings.getString("Citilink_AgenName"),
                    commonSettings.getString("Citilink_AgenPassword"),
                    commonSettings.getString("Citilink_DomainCode")
                    );
                ch.agentSignature = clientSignature;
                ret = ch.getAvailabilityWrap(DepartureStation, ArrivalStation, (short)PaxCountADT, (short)PaxCountCHD,
                    outboundBeginDate, outboundEndDate, inboundBeginDate, inboundEndDate,
                    (InboundOutbound == "Outbound") ?
                    CitilinkLib.CitilinkHandler.InOutbound.Outbound :
                    CitilinkLib.CitilinkHandler.InOutbound.Both, clientSignature, ref isError);
                errMssg = ch.lastErrorMessage;
                ch.Dispose();
            }
            else
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid product code", "");
            }

            if((isError) || (ret == null))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", errMssg, "");
            }

            string repl = ret.JSONConstruct();
            ret.Dispose();
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", repl);
        }

        private bool checkMandatoryFields(string[] mandatoryFields)
        {
            foreach (string aField in mandatoryFields)
            {
                if (!jsonConv.ContainsKey(aField))
                {
                    return false;
                }
            }
            return true;
        }

        private bool checkMandatoryFields(string[] mandatoryFields, JsonLibs.MyJsonLib aJsonLib)
        {
            foreach (string aField in mandatoryFields)
            {
                if (!aJsonLib.ContainsKey(aField)) return false;
            }
            return true;
        }

        private bool standardFieldsCheck(HTTPRestConstructor.HttpRestRequest clientData,
            string[] mandatoryFields, ref string hasil, ref string userPhone, ref string userId)
        {
            if (!jsonConv.JSONParse(clientData.Body))
            {
                hasil = HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
                return false;
            }

            //LogWriter.show(this, "Json received from client :\r\n" + clientData.Body);

            if(!checkMandatoryFields(mandatoryFields))
            {
                hasil = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory standard field not found", "");
                return false;
            }

            try
            {
                userPhone = ((string)jsonConv["fiPhone"]).Trim();
            }
            catch
            {
                hasil = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
                return false;
            }

            ReformatPhoneNumber(ref userPhone);

            userId = cUserIDHeader + userPhone;

            if (userPhone.Length == 0)
            {
                hasil = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid phone number", "");
                return false;
            }

            if (!localDB.isAccountExist(userPhone, out xError))
            {
                if (xError != null)
                {
                    hasil = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to check user in database", "");
                    return false;
                }
                else
                {
                    hasil = HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "User unregistered", "");
                    return false;
                }
            }
            return true;
        }

        private string ProductAirlinesINFSellRequest(HTTPRestConstructor.HttpRestRequest clientData)
        {
            string[] fields = { "fiPhone", "fiApplicationId", "fiProductCode", 
                                "fiDepatureStation", "fiArrivalStation","fiInfantCount",
                                "fiCarrierCodeOutbound","fiFlightNumberOutbound","fiSTDOutbound", 
                                "fiCarrierCodeInbound","fiFlightNumberInbound","fiSTDInbound", 
                                "fiSignature"};
            string hasil="";
            string userPhone = "";
            string userId = "";
            if (!standardFieldsCheck(clientData, fields, ref hasil, ref userPhone, ref userId))
            {
                return hasil;
            }

            string appID = "";
            string productCode = "";
            string clientSignature = "";
            int PaxINF;
            string DepatureStation = "";
            string ArrivalStation = "";
            string CarrierCodeOutbound = "";
            string FlightNumberOutbound = "";
            DateTime STDOutbound;
            string CarrierCodeInbound = "";
            string FlightNumberInbound = "";
            DateTime STDInbound;
            try
            {
                appID = ((string)jsonConv["fiApplicationId"]).Trim();
                productCode = ((string)jsonConv["fiProductCode"]).Trim();
                clientSignature = (string)jsonConv["fiSignature"];
                DepatureStation = (string)jsonConv["fiDepatureStation"];
                ArrivalStation = (string)jsonConv["fiArrivalStation"];
                PaxINF = ((int)jsonConv["fiInfantCount"]);
                CarrierCodeOutbound = (string)jsonConv["fiCarrierCodeOutbound"];
                FlightNumberOutbound = (string)jsonConv["fiFlightNumberOutbound"];
                STDOutbound = DateTime.ParseExact((string)jsonConv["fiSTDOutbound"], "yyyy-MM-dd HH:mm:ss", null);
                CarrierCodeInbound = (string)jsonConv["fiCarrierCodeInbound"];
                FlightNumberInbound = (string)jsonConv["fiFlightNumberInbound"];
                if (FlightNumberInbound != "")
                    STDInbound = DateTime.ParseExact((string)jsonConv["fiSTDInbound"], "yyyy-MM-dd HH:mm:ss", null);
                else STDInbound = STDOutbound;
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
            }

			string token = "";
			string httprepl = "";
			if (!cek_TokenSecurity (userPhone, jsonConv, ref token, ref httprepl)) {
				return httprepl;
			}

            string[] CarrierCodes;
            string[] FlightNumbers;
            DateTime[] STDs;

            if (FlightNumberInbound != "")
            {
                CarrierCodes = new string[2];
                FlightNumbers = new string[2];
                STDs = new DateTime[2];
                CarrierCodes[1] = CarrierCodeInbound;
                FlightNumbers[1] = FlightNumberInbound;
                STDs[1] = STDInbound;

            }
            else
            {
                CarrierCodes = new string[1];
                FlightNumbers = new string[1];
                STDs = new DateTime[1];
            }
            CarrierCodes[0] = CarrierCodeOutbound;
            FlightNumbers[0] = FlightNumberOutbound;
            STDs[0] = STDOutbound;

            bool isError = false;
            string errMssg = "";
            JsonLibs.MyJsonLib ret;

            string resp = "";
            PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct = null;
            if (!checkAirlinesProductCode(productCode, ref resp, ref providerProduct))
            {
                return resp;
            }
            string providerCode = providerProduct.ProviderCode;

            if (providerCode == "115")      // jika citilink
            {
                CitilinkLib.CitilinkHandler ch = new CitilinkLib.CitilinkHandler(
                    commonSettings.getString("Citilink_AgenName"),
                    commonSettings.getString("Citilink_AgenPassword"),
                    commonSettings.getString("Citilink_DomainCode")
                    );
                ch.agentSignature = clientSignature;
                ret = ch.sellRequestINF2(PaxINF, "IDR", CarrierCodes, FlightNumbers,
                    STDs, DepatureStation, ArrivalStation, clientSignature, false, ref isError, false);
                errMssg = ch.lastErrorMessage;
                ch.Dispose();
            }
            else
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid product code", "");
            }

            if ((isError) || (ret == null))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", errMssg, "");
            }

            string repl = ret.JSONConstruct();
            ret.Dispose();
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", repl);
        }

        private bool getQvaBalance(string userId, ref double CustBalance)
        {
            // konek ke host sandra untuk inquiry
            //if (localIP) sandraHost = "127.0.0.1";
			using (SandraLibs sandra = new SandraLibs(commonSettings.getInt("QvaTimeOut"),
				(commonSettings.getString("SandraUseTcpMethod").Trim().ToLower() == "true"),
				commonSettings.getString("SandraHmsUserAuth"),
				commonSettings.getString("SandraHmsSecretKey"),
				commonSettings.getString("SandraAuthId")))
            {
                if (sandra.Inquiry(commonSettings.getString("SandraHost"),
					commonSettings.getInt("SandraPort"),
					(commonSettings.getString("SandraIsHttps").ToLower() == "true"), 
					userId, ref CustBalance))
                {
                    return true;
                }
                else
                {
                    LogWriter.show(this, "Sandra not OK\r\n" +
                                    "Error code: " + sandra.LastError.ServerCode + "\r\n" +
                                    "Error message: " + sandra.LastError.ServerMessage);
                    return true;
                }
            }
        }

        private string ProductAirlinesSellRequestAll(HTTPRestConstructor.HttpRestRequest clientData)
        {
            string[] fields = { "fiPhone", "fiApplicationId", "fiProductCode", "fiSignature",
                                "fiJourneySellKeyOutbound", "fiFareSellKeyOutbound", 
                                "fiInfSellRequestOutbound", 
                                "fiPaxCountADT", "fiPaxCountCHD", 
                                "fiInfantCount"
                                };
            string hasil = "";
            string userPhone = "";
            string userId = "";
            if (!standardFieldsCheck(clientData, fields, ref hasil, ref userPhone, ref userId))
            {
                return hasil;
            }

            string appID = "";
            string productCode = "";
            string clientSignature = "";
            int PaxCountADT;
            int PaxCountCHD;
            string JourneySellKeyOutbound = "";
            string JourneySellKeyInbound = "";
            JsonLibs.MyJsonArray FareSellKeyOutbound;   // array of string
            JsonLibs.MyJsonArray InfSellRequestOutbound = null; // array of json
            JsonLibs.MyJsonArray FareSellKeyInbound = null;
            JsonLibs.MyJsonArray InfSellRequestInbound = null;
            int PaxINF;

            try
            {
                appID = ((string)jsonConv["fiApplicationId"]).Trim();
                productCode = ((string)jsonConv["fiProductCode"]).Trim();
                PaxCountADT = ((int)jsonConv["fiPaxCountADT"]);
                PaxCountCHD = ((int)jsonConv["fiPaxCountCHD"]);
                PaxINF = ((int)jsonConv["fiInfantCount"]);
                JourneySellKeyOutbound = (string)jsonConv["fiJourneySellKeyOutbound"];
                FareSellKeyOutbound = (JsonLibs.MyJsonArray)jsonConv["fiFareSellKeyOutbound"];
                if (jsonConv.isExists("fiJourneySellKeyInbound"))
                {
                    if (!jsonConv.isExists("fiFareSellKeyInbound"))
                    {
                        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory standard field not found", "");
                    }
                    JourneySellKeyInbound = (string)jsonConv["fiJourneySellKeyInbound"];
                    FareSellKeyInbound = (JsonLibs.MyJsonArray)jsonConv["fiFareSellKeyInbound"];
                }
                if (PaxINF > 0)
                {
                    if (!jsonConv.isExists("fiInfSellRequestOutbound"))
                    {
                        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory standard field not found", "");
                    }
                    InfSellRequestOutbound = (JsonLibs.MyJsonArray)jsonConv["fiInfSellRequestOutbound"];
                    if (jsonConv.isExists("fiInfSellRequestInbound"))
                    {
                        InfSellRequestInbound = (JsonLibs.MyJsonArray)jsonConv["fiInfSellRequestInbound"];
                    }
                }
                clientSignature = (string)jsonConv["fiSignature"];
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, ex.getCompleteErrMsg());
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
            }

			string token = "";
			string httprepl = "";
			if (!cek_TokenSecurity (userPhone, jsonConv, ref token, ref httprepl)) {
				return httprepl;
			}

            //public struct InfSellReq
            //{
            //    public string CarrierCode;
            //    public string FlightNumber;
            //    public DateTime STD;
            //    public string DepartureStation;
            //    public string ArrivalStation;
            //}

            string resp = "";
            PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct = null;
            if (!checkAirlinesProductCode(productCode, ref resp, ref providerProduct))
            {
                return resp;
            }
            string providerCode = providerProduct.ProviderCode;

            if (providerCode != "115")      // jika citilink saja sementara
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid product code", "");
            }

            int i = 0;
            CitilinkLib.CitilinkHandler.InfSellReq[] infSellReqs = null;
            if (PaxINF > 0)
            {
                if (InfSellRequestInbound == null)
                    infSellReqs = new CitilinkLib.CitilinkHandler.InfSellReq[InfSellRequestOutbound.Count];
                else
                    infSellReqs = new CitilinkLib.CitilinkHandler.InfSellReq[InfSellRequestInbound.Count + InfSellRequestOutbound.Count];
                try
                {
                    //foreach (JsonLibs.MyJsonLib infSellReqJs in InfSellRequestOutbound)
                    for (int j = 0; j < InfSellRequestOutbound.Count; j++)
                    {
                        JsonLibs.MyJsonLib infSellReqJs = (JsonLibs.MyJsonLib)InfSellRequestOutbound[j];
                        infSellReqs[i].DepartureStation = (string)infSellReqJs["fiDepartureStation"];
                        infSellReqs[i].ArrivalStation = (string)infSellReqJs["fiArrivalStation"];
                        infSellReqs[i].CarrierCode = (string)infSellReqJs["fiCarrierCode"];
                        infSellReqs[i].FlightNumber = (string)infSellReqJs["fiFlightNumber"];
                        infSellReqs[i].STD = DateTime.ParseExact((string)infSellReqJs["fiSTD"], "yyyy-MM-dd HH:mm:ss", null);
                        i++;
                    }
                    //foreach (JsonLibs.MyJsonLib infSellReqJs in InfSellRequestInbound)
                    if (InfSellRequestInbound != null)
                    {
                        for (int j = 0; j < InfSellRequestInbound.Count; j++)
                        {
                            JsonLibs.MyJsonLib infSellReqJs = (JsonLibs.MyJsonLib)InfSellRequestInbound[j];
                            if (infSellReqJs.isNull("fiSTD")) break;
                            infSellReqs[i].DepartureStation = (string)infSellReqJs["fiDepartureStation"];
                            infSellReqs[i].ArrivalStation = (string)infSellReqJs["fiArrivalStation"];
                            infSellReqs[i].CarrierCode = (string)infSellReqJs["fiCarrierCode"];
                            infSellReqs[i].FlightNumber = (string)infSellReqJs["fiFlightNumber"];
                            infSellReqs[i].STD = DateTime.ParseExact((string)infSellReqJs["fiSTD"], "yyyy-MM-dd HH:mm:ss", null);
                            i++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, ex.getCompleteErrMsg());
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
                }
            }

            CitilinkLib.CitilinkHandler.SellKey[] SellKeys;
            if (FareSellKeyInbound == null)
                SellKeys = new CitilinkLib.CitilinkHandler.SellKey[1];
            else
                SellKeys = new CitilinkLib.CitilinkHandler.SellKey[2];

            i = 0;
            try
            {
                string faresellkey = "";
                for (int j = 0; j < FareSellKeyOutbound.Count; j++)
                {
                    if (faresellkey != "") faresellkey += "^";
                    faresellkey += (string)FareSellKeyOutbound[j];
                }
                SellKeys[0].FareSellKey = faresellkey;
                SellKeys[0].JourneySellKey = JourneySellKeyOutbound;
                faresellkey = "";
                if (FareSellKeyInbound != null)
                {
                    for (int j = 0; j < FareSellKeyInbound.Count; j++)
                    {
                        if (faresellkey != "") faresellkey += "^";
                        faresellkey += (string)FareSellKeyInbound[j];
                    }
                    SellKeys[1].FareSellKey = faresellkey;
                    SellKeys[1].JourneySellKey = JourneySellKeyInbound;
                }
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, ex.getCompleteErrMsg());
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
            }

            // ======== DISINI pengecekan Saldo QVA ==========
            double evaBalance =0;
            if (!getQvaBalance(userId, ref evaBalance))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "411", "Failed on inquiry balance", "");
            }

            bool isError = false;
            string errMssg = "";
            JsonLibs.MyJsonLib ret;

            CitilinkLib.CitilinkHandler ch = new CitilinkLib.CitilinkHandler(
                commonSettings.getString("Citilink_AgenName"),
                commonSettings.getString("Citilink_AgenPassword"),
                commonSettings.getString("Citilink_DomainCode")
                );
            ch.agentSignature = clientSignature;
            ret = ch.sellRequestALL(SellKeys,
                (short)PaxCountADT, (short)PaxCountCHD, (short)PaxINF, "IDR", infSellReqs,
                clientSignature, ref isError);
            errMssg = ch.lastErrorMessage;

            //decimal totalBaseAmount = 0;
            //decimal totalAmount = 0;
            //if (!ch.getPaymentAmount(ref totalAmount, ref totalBaseAmount, clientSignature))
            //{
            //    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "412", "Failed to get detail payment", "");
            //}

            //LogWriter.showDEBUG(this, "Total Base Amount: " + totalBaseAmount.ToString() + "\r\n" +
            //                        "Total Amount: " + totalAmount.ToString());

            ch.Dispose();

            if ((isError) || (ret == null))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", errMssg, "");
            }

            // tambahkan balance
            ret.Add("fiAgentBalance",evaBalance);
            string repl = ret.JSONConstruct();
            ret.Dispose();
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", repl);
        }


        private string ProductAirlinesSellRequestAll2(HTTPRestConstructor.HttpRestRequest clientData)
        {
            string[] fields = { "fiPhone", "fiApplicationId", "fiProductCode", 
                                "fiJourneySellKeyOutbound", "fiFareSellKeyOutbound", 
                                "fiJourneySellKeyInbound", "fiFareSellKeyInbound", 
                                "fiPaxCountADT", "fiPaxCountCHD", 
                                "fiDepatureStation", "fiArrivalStation","fiInfantCount",
                                "fiCarrierCodeOutbound","fiFlightNumberOutbound","fiSTDOutbound", 
                                "fiCarrierCodeInbound","fiFlightNumberInbound","fiSTDInbound", 
                                "fiSignature"};
            string hasil = "";
            string userPhone = "";
            string userId = "";
            if (!standardFieldsCheck(clientData, fields, ref hasil, ref userPhone, ref userId))
            {
                return hasil;
            }

            string appID = "";
            string productCode = "";
            string clientSignature = "";
            int PaxCountADT;
            int PaxCountCHD;
            string JourneySellKeyOutbound = "";
            string FareSellKeyOutbound = "";
            string JourneySellKeyInbound = "";
            string FareSellKeyInbound = "";
            int PaxINF;
            string DepatureStation = "";
            string ArrivalStation = "";
            string CarrierCodeOutbound = "";
            string FlightNumberOutbound = "";
            DateTime STDOutbound;
            string CarrierCodeInbound = "";
            string FlightNumberInbound = "";
            DateTime STDInbound;
            try
            {
                appID = ((string)jsonConv["fiApplicationId"]).Trim();
                productCode = ((string)jsonConv["fiProductCode"]).Trim();
                PaxCountADT = ((int)jsonConv["fiPaxCountADT"]);
                PaxCountCHD = ((int)jsonConv["fiPaxCountCHD"]);
                JourneySellKeyOutbound = (string)jsonConv["fiJourneySellKeyOutbound"];
                FareSellKeyOutbound = (string)jsonConv["fiFareSellKeyOutbound"];
                JourneySellKeyInbound = (string)jsonConv["fiJourneySellKeyInbound"];
                FareSellKeyInbound = (string)jsonConv["fiFareSellKeyInbound"];
                clientSignature = (string)jsonConv["fiSignature"];
                DepatureStation = (string)jsonConv["fiDepatureStation"];
                ArrivalStation = (string)jsonConv["fiArrivalStation"];
                PaxINF = ((int)jsonConv["fiInfantCount"]);
                CarrierCodeOutbound = (string)jsonConv["fiCarrierCodeOutbound"];
                FlightNumberOutbound = (string)jsonConv["fiFlightNumberOutbound"];
                STDOutbound = DateTime.ParseExact((string)jsonConv["fiSTDOutbound"], "yyyy-MM-dd HH:mm:ss", null);
                CarrierCodeInbound = (string)jsonConv["fiCarrierCodeInbound"];
                FlightNumberInbound = (string)jsonConv["fiFlightNumberInbound"];
                if (FlightNumberInbound != "")
                    STDInbound = DateTime.ParseExact((string)jsonConv["fiSTDInbound"], "yyyy-MM-dd HH:mm:ss", null);
                else STDInbound = STDOutbound;
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
            }

			string token = "";
			string httprepl = "";
			if (!cek_TokenSecurity (userPhone, jsonConv, ref token, ref httprepl)) {
				return httprepl;
			}

            string[] CarrierCodes;
            string[] FlightNumbers;
            DateTime[] STDs;

            if (FlightNumberInbound != "")
            {
                CarrierCodes = new string[2];
                FlightNumbers = new string[2];
                STDs = new DateTime[2];
                CarrierCodes[1] = CarrierCodeInbound;
                FlightNumbers[1] = FlightNumberInbound;
                STDs[1] = STDInbound;

            }
            else
            {
                CarrierCodes = new string[1];
                FlightNumbers = new string[1];
                STDs = new DateTime[1];
            }
            CarrierCodes[0] = CarrierCodeOutbound;
            FlightNumbers[0] = FlightNumberOutbound;
            STDs[0] = STDOutbound;

            string[] JourneySellKeys;
            string[] FareSellKeys;
            if (JourneySellKeyInbound == "")
            {
                JourneySellKeys = new string[1];
                FareSellKeys = new string[1];
            }
            else
            {
                JourneySellKeys = new string[2];
                FareSellKeys = new string[2];
                FareSellKeys[1] = FareSellKeyInbound;
                JourneySellKeys[1] = JourneySellKeyInbound;
            }
            FareSellKeys[0] = FareSellKeyOutbound;
            JourneySellKeys[0] = JourneySellKeyOutbound;

            bool isError = false;
            string errMssg = "";
            JsonLibs.MyJsonLib ret=null;

            string resp = "";
            PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct = null;
            if (!checkAirlinesProductCode(productCode, ref resp, ref providerProduct))
            {
                return resp;
            }
            string providerCode = providerProduct.ProviderCode;

            if (providerCode != "115")      // jika citilink saja sementara
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid product code", "");
            }

            //if (providerCode == "115")      // jika citilink
            //{
            //    CitilinkLib.CitilinkHandler ch = new CitilinkLib.CitilinkHandler();
            //    ch.agentSignature = clientSignature;
            //    ret = ch.sellRequestALL(JourneySellKeys, FareSellKeys,
            //        (short)PaxCountADT, (short)PaxCountCHD, (short)PaxINF, "IDR", CarrierCodes, FlightNumbers, STDs,
            //        DepatureStation, ArrivalStation, clientSignature, ref isError);
            //    errMssg = ch.lastErrorMessage;
            //    ch.Dispose();
            //}
            //else
            //{
            //    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid product code", "");
            //}

            if ((isError) || (ret == null))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", errMssg, "");
            }

            string repl = ret.JSONConstruct();
            ret.Dispose();
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", repl);
        }

        string[] contactFields = { "fiContactFirstName", "fiContactLastName", "fiContactTitle", 
                                         "fiContactEmailAddress", "fiContactHomePhone", 
                                         "fiContactWorkPhone", "fiContactOtherPhone", "fiContactFax", 
                                         "fiContactCompanyName", "fiContactAddressLine1", 
                                         "fiContactAddressLine2", "fiContactAddressLine3", 
                                         "fiContactCity", "fiContactProvinceState", 
                                         "fiContactPostalCode", "fiCountryCode", "fiCultureCode", 
                                         "fiDistributionOption", "fiCustomerNumber", 
                                         "fiNotificationPreference", "fiSourceOrganization" };
        string[] passengerFields = { "DOB",
                                           "LastName", "Nationality", "Title", "Gender", 
                                           "FirstName", "PaxTypes", "WeightCategory" };
        string[] passengerINFFields = { "InfantNationality", 
                                           "InfantDOB", "InfantTitle", "InfantFirstName", 
                                           "InfantLastName", "InfantGender", "InfantMiddleName" };

        private string ProductAirlinesUpdateContactAndPassengers(
            HTTPRestConstructor.HttpRestRequest clientData)
        {
            string[] fields = { "fiPhone", "fiApplicationId", "fiProductCode", "fiSignature",
                        "fiPassengers", "fiPassengersINF", "fiContactPerson" };
            string hasil = "";
            string userPhone = "";
            string userId = "";
            if (!standardFieldsCheck(clientData, fields, ref hasil, ref userPhone, ref userId))
            {
                return hasil;
            }

            string appID = "";
            string productCode = "";
            string clientSignature = "";

            JsonLibs.MyJsonArray Passengers;
            JsonLibs.MyJsonArray PassengersINF;
            JsonLibs.MyJsonLib ContactPerson;

            try
            {
                appID = ((string)jsonConv["fiApplicationId"]).Trim();
                productCode = ((string)jsonConv["fiProductCode"]).Trim();
                clientSignature = (string)jsonConv["fiSignature"];
                Passengers = (JsonLibs.MyJsonArray)jsonConv["fiPassengers"];
                PassengersINF = (JsonLibs.MyJsonArray)jsonConv["fiPassengersINF"];
                ContactPerson = (JsonLibs.MyJsonLib)jsonConv["fiContactPerson"];
                if (Passengers.Count < 1)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "410", "Invalid json structure", "");
                }
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
            }

			string token = "";
			string httprepl = "";
			if (!cek_TokenSecurity (userPhone, jsonConv, ref token, ref httprepl)) {
				return httprepl;
			}

            if (!checkMandatoryFields(contactFields, ContactPerson))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory field not found of contact", "");
            }
            try
            {
                for (int i = 0; i < Passengers.Count; i++)
                {
                    JsonLibs.MyJsonLib aJson = (JsonLibs.MyJsonLib)Passengers[i];
                    if (!checkMandatoryFields(passengerFields, aJson))
                    {
                        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory field not found of passenger[" + i.ToString() + "]", "");
                    }
                }
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "410", "Invalid json structure", "");
            }

            if (PassengersINF.Count > 0)
            {
                try
                {
                    for (int i = 0; i < PassengersINF.Count; i++)
                    {
                        JsonLibs.MyJsonLib aJson = (JsonLibs.MyJsonLib)PassengersINF[i];
                        if (!checkMandatoryFields(passengerINFFields, aJson))
                        {
                            return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory field not found of passengerINF[" + i.ToString() + "]", "");
                        }
                    }
                }
                catch
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "410", "Invalid json structure", "");
                }
            }

            string resp = "";
            PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct = null;
            if (!checkAirlinesProductCode(productCode, ref resp, ref providerProduct))
            {
                return resp;
            }
            string providerCode = providerProduct.ProviderCode;

            if (providerCode != "115")      // jika citilink saja sementara
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid product code", "");
            }

            CitilinkLib.CitilinkHandler.CitilinkContact Contact = new CitilinkLib.CitilinkHandler.CitilinkContact();
            try
            {
                Contact.AddressLine1 = (string)ContactPerson["fiContactAddressLine1"];
                Contact.AddressLine2 = (string)ContactPerson["fiContactAddressLine2"];
                Contact.AddressLine3 = (string)ContactPerson["fiContactAddressLine3"];
                Contact.City = (string)ContactPerson["fiContactCity"];
                Contact.CompanyName = (string)ContactPerson["fiContactCompanyName"];
                Contact.CountryCode = (string)ContactPerson["fiCountryCode"];
                Contact.CultureCode = (string)ContactPerson["fiCultureCode"];
                Contact.CustomerNumber = (string)ContactPerson["fiCustomerNumber"];
                //Contact.DistributionOption = (string)ContactPerson[""];
                Contact.DistributionOption = "";
                Contact.EmailAddress = (string)ContactPerson["fiContactEmailAddress"];
                Contact.Fax = (string)ContactPerson["fiContactFax"];
                Contact.FirstName = (string)ContactPerson["fiContactFirstName"];
                Contact.HomePhone = (string)ContactPerson["fiContactHomePhone"];
                Contact.LastName = (string)ContactPerson["fiContactLastName"];
                Contact.NotificationPreference = (string)ContactPerson["fiNotificationPreference"];
                Contact.OtherPhone = (string)ContactPerson["fiContactOtherPhone"];
                Contact.PostalCode = (string)ContactPerson["fiContactPostalCode"];
                Contact.ProvinceState = (string)ContactPerson["fiContactProvinceState"];
                Contact.SourceOrganization = (string)ContactPerson["fiSourceOrganization"];
                //Contact.State = (string)ContactPerson[""];
                Contact.State = "";
                Contact.Title = (string)ContactPerson["fiContactTitle"];
                Contact.WorkPhone = (string)ContactPerson["fiContactWorkPhone"];
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format of contact", "");
            }

            CitilinkLib.CitilinkHandler.CitilinkPassenger[] Passgrs = new
                CitilinkLib.CitilinkHandler.CitilinkPassenger[Passengers.Count];
            try
            {
                for (int i = 0; i < Passengers.Count; i++)
                {
                    CitilinkLib.CitilinkHandler.CitilinkPassenger aPassenger =
                        new CitilinkLib.CitilinkHandler.CitilinkPassenger();
                    JsonLibs.MyJsonLib aJsonPassenger = (JsonLibs.MyJsonLib)Passengers[i];
                    //aPassenger.IsInfant = (bool)aJsonPassenger["IsInfant"];
                    aPassenger.IsInfant = false;
                    if (aPassenger.IsInfant)
                    {
                        aPassenger.InfantDOB = (string)aJsonPassenger["InfantDOB"];
                        aPassenger.InfantFirstName = (string)aJsonPassenger["InfantFirstName"];
                        aPassenger.InfantLastName = (string)aJsonPassenger["InfantLastName"];
                        aPassenger.InfantGender = (string)aJsonPassenger["InfantGender"];
                        aPassenger.InfantMiddleName = (string)aJsonPassenger["InfantMiddleName"];
                        aPassenger.InfantNationality = (string)aJsonPassenger["InfantNationality"];
                        aPassenger.InfantSuffix = "";
                        aPassenger.InfantTitle = (string)aJsonPassenger["InfantTitle"];
                    }
                    else
                    {
                        aPassenger.DOB = (string)aJsonPassenger["DOB"];
                        aPassenger.FirstName = (string)aJsonPassenger["FirstName"];
                        aPassenger.LastName = (string)aJsonPassenger["LastName"];
                        aPassenger.MiddleName = "";
                        aPassenger.Gender = (string)aJsonPassenger["Gender"];
                        aPassenger.Nationality = (string)aJsonPassenger["Nationality"];
                        aPassenger.Suffix = "";
                        aPassenger.Title = (string)aJsonPassenger["Title"];
                        aPassenger.WeightCategory = (string)aJsonPassenger["WeightCategory"];
                        aPassenger.PaxType = (string)aJsonPassenger["PaxTypes"];
                    }

                    Passgrs[i] = aPassenger;
                }
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format of passengers", "");
            }

            CitilinkLib.CitilinkHandler.CitilinkPassenger[] PassgrsINF = new
                CitilinkLib.CitilinkHandler.CitilinkPassenger[PassengersINF.Count];
            try
            {
                for (int i = 0; i < PassengersINF.Count; i++)
                {
                    CitilinkLib.CitilinkHandler.CitilinkPassenger aPassengerINF =
                        new CitilinkLib.CitilinkHandler.CitilinkPassenger();
                    JsonLibs.MyJsonLib aJsonPassengerINF = (JsonLibs.MyJsonLib)PassengersINF[i];
                    aPassengerINF.IsInfant = true;
                    aPassengerINF.InfantDOB = (string)aJsonPassengerINF["InfantDOB"];
                    aPassengerINF.InfantFirstName = (string)aJsonPassengerINF["InfantFirstName"];
                    aPassengerINF.InfantLastName = (string)aJsonPassengerINF["InfantLastName"];
                    aPassengerINF.InfantGender = (string)aJsonPassengerINF["InfantGender"];
                    aPassengerINF.InfantMiddleName = (string)aJsonPassengerINF["InfantMiddleName"];
                    aPassengerINF.InfantNationality = (string)aJsonPassengerINF["InfantNationality"];
                    aPassengerINF.InfantSuffix = "";
                    aPassengerINF.InfantTitle = (string)aJsonPassengerINF["InfantTitle"];

                    PassgrsINF[i] = aPassengerINF;
                }
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format of passengers INF", "");
            }

            bool isError = false;
            string errMssg = "";
            JsonLibs.MyJsonLib ret;

            CitilinkLib.CitilinkHandler ch = new CitilinkLib.CitilinkHandler(
                commonSettings.getString("Citilink_AgenName"),
                commonSettings.getString("Citilink_AgenPassword"),
                commonSettings.getString("Citilink_DomainCode")
                );
            ch.agentSignature = clientSignature;
            ret = ch.updateContactAndPassengers(
                Contact, commonSettings.getString("ItenaryCollectorEmail"), Passgrs, PassgrsINF, "IDR", clientSignature, ref isError);
            errMssg = ch.lastErrorMessage;
            ch.Dispose();

            if ((isError) || (ret == null))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", errMssg, "");
            }

            string repl = ret.JSONConstruct();
            ret.Dispose();
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", repl);
        }

        private string ProductAirlinesAddPayment(
                HTTPRestConstructor.HttpRestRequest clientData)
        {
            string[] fields = { "fiPhone", "fiApplicationId", "fiProductCode", "fiSignature", 
                                  "fiTotalAmount" };
            string hasil = "";
            string userPhone = "";
            string userId = "";
            if (!standardFieldsCheck(clientData, fields, ref hasil, ref userPhone, ref userId))
            {
                return hasil;
            }

            string appID = "";
            string productCode = "";
            string clientSignature = "";

            int TotalAmount = 0;

            try
            {
                appID = ((string)jsonConv["fiApplicationId"]).Trim();
                productCode = ((string)jsonConv["fiProductCode"]).Trim();
                clientSignature = (string)jsonConv["fiSignature"];
                TotalAmount = (int)jsonConv["fiTotalAmount"];
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
            }

			string token = "";
			string httprepl = "";
			if (!cek_TokenSecurity (userPhone, jsonConv, ref token, ref httprepl)) {
				return httprepl;
			}

            bool isError = false;
            string errMssg = "";
            JsonLibs.MyJsonLib ret;

            string resp = "";
            PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct = null;
            if (!checkAirlinesProductCode(productCode, ref resp, ref providerProduct))
            {
                return resp;
            }
            string providerCode = providerProduct.ProviderCode;

            if (providerCode == "115")      // jika citilink
            {
                CitilinkLib.CitilinkHandler ch = new CitilinkLib.CitilinkHandler(
                    commonSettings.getString("Citilink_AgenName"),
                    commonSettings.getString("Citilink_AgenPassword"),
                    commonSettings.getString("Citilink_DomainCode")
                    );
                ch.agentSignature = clientSignature;
                ret = ch.addPayment(TotalAmount, "IDR", clientSignature, ref isError);
                errMssg = ch.lastErrorMessage;
                ch.Dispose();
            }
            else
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid product code", "");
            }

            if (ret == null)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", errMssg, "");
            }

            if (isError)
            {
                string erCode = "419";
                string erMsg = "AddPayment failed";
                if (ret != null)
                {
                    erCode = (string)ret["fiResponseCode"];
                    erMsg = (string)ret["fiResponseMessage"];
                    ret.Dispose();
                }
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, erCode, erMsg, "");
            }
            string repl = ret.JSONConstruct();
            ret.Dispose();
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", repl);
        }

		private bool getAirlinesPaymentFromClient(string appID, string userId, string trxCode_sufix, string productCode,
            string ProviderCode, int TotalAmount, long TransactionRef_id, ref string ErrRepl)
        {
            //int baseAmount = 0;
            int adminFee = 0;
            //int customerFeeAmount = 0;

            try
            {
				//if (!getBaseAndFeeAmountFromProduct(productCode, ProviderCode,
				if (!getBaseAndFeeAmountFromProduct(productCode, 1, appID,
                    ref adminFee, TotalAmount))
                {
                    ErrRepl = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Fee data not found", "");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error get fee data : " + ex.getCompleteErrMsg());
                //Console.WriteLine(ex.StackTrace);
                ErrRepl = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Error get fee data", "");
                return false;
            }

			string qvaInvoiceNumber = "";
			bool qvaReversalRequired = false;

			using (TransferReguler.CollectTransfer TransferReg =
                new TransferReguler.CollectTransfer(commonSettings))
            {
                try
                {
                    string errMessage = "";
                    string errCode = "";
                    // ===== AMBIL PEMBAYARAN DARI CUSTOMER
                    LogWriter.show(this, "Get payment from customer for Airlines payment");
                    if (!TransferReg.PayFromCustomer(userId, 
						trxCode_sufix, 1, TotalAmount, TransactionRef_id, 
						PPOBDatabase.PPOBdbLibs.eTransactionType.PPOB, "Get payment from customer", 
						ref qvaInvoiceNumber, ref qvaReversalRequired, 
						ref errCode, ref errMessage))
					{
						if(qvaReversalRequired)
							TransferReg.Reversal(TransactionRef_id,qvaInvoiceNumber, ref errCode, ref errMessage);
						LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : [" + errCode + "]" + errMessage);
						ErrRepl = HTTPRestDataConstruct.constructHTTPRestResponse(400, errCode, errMessage, "");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : " + ex.getCompleteErrMsg());
                    ErrRepl = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to do payment", "");
                    return false;
                }
            }
            return true;
        }

		private bool payBackAirlinesPaymentToClient(string userId, string trxCode_sufix, 
            int TotalAmount, long TransactionRef_id, string trxNumber, ref string ErrRepl)
        {
            using (TransferReguler.CollectTransfer TransferReg =
                new TransferReguler.CollectTransfer(commonSettings))
            {
                string errMessage = "";
                string errCode = "";
                try
                {
                    // ===== KEMBALIKAN PEMBAYARAN DARI CUSTOMER
                    LogWriter.show(this, "Pay back to customer");
					// TODO : DISINI musti query supaya dapet invoice number qva
					// FIXME : DISINI musti query supaya dapet invoice number qva
					TransferReg.TransferBackToCustomer(userId, 
						trxCode_sufix, TotalAmount,
						TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.PPOB,
						"Reversal transfer triggered from client request",
						ref errCode, ref errMessage);
					// didalam fungsi TransferReg.TransferBackToCustomer diatas, meskipun transfer gagal
					// proses transfer akan dilanjutkan secara background sampai berhasil

//					if (!TransferReg.ReversalToCustomer(userId,
//						trxCode_sufix, 
//                        TotalAmount, TransactionRef_id,
//                        PPOBDatabase.PPOBdbLibs.eTransactionType.PPOB, "Transfer back to customer",
//                        ref errCode, ref errMessage))
//                    {
//                        LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Reversal failed to " + userId +
//                            " TrxNumber: " + trxNumber + " : [" + errCode.ToString() + "]" + errMessage);
//                        ErrRepl = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Transaction failed, Failed to do reversal TrxNumber: " + trxNumber.ToString(), "");
//                        return false;
//                    }
                }
                catch (Exception ex)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Reversal failed to " + userId +
                        " TrxNumber: " + trxNumber + " : " + ex.getCompleteErrMsg());
                    ErrRepl = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Transaction failed, Failed to do reversal TrxNumber: " + trxNumber.ToString(), "");
                    return false;
                }
            }
            return true;
        }

        private string ProductAirlinesCommit(HTTPRestConstructor.HttpRestRequest clientData)
        {
            string[] fields = { "fiPhone", "fiToken", "fiApplicationId", "fiProductCode", 
                                  "fiSignature", "fiPaxCount" };
            string hasil = "";
            string userPhone = "";
            string userId = "";
            if (!standardFieldsCheck(clientData, fields, ref hasil, ref userPhone, ref userId))
            {
                return hasil;
            }

            string appID = "";
            string productCode = "";
            string clientSignature = "";

            int paxCount = 0;

            try
            {
                appID = ((string)jsonConv["fiApplicationId"]).Trim();
                productCode = ((string)jsonConv["fiProductCode"]).Trim();
                clientSignature = (string)jsonConv["fiSignature"];
                paxCount = (int)jsonConv["fiPaxCount"];
                //password = ((string)jsonConv["fiPassword"]).Trim().ToLower();
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
            }

			string token = "";
			string httprepl = "";
			if (!cek_TokenSecurity (userPhone, jsonConv, ref token, ref httprepl)) {
				return httprepl;
			}

//            // cek dengan database, apakah password sama?
//            if (!localDB.isUserPasswordEqual(userPhone, password, out xError))
//            {
//                if (xError != null)
//                {
//                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
//                }
//                else
//                {
//                    // password error
//                    return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Wrong password", "");
//                }
//            }
//            // password ok
//
            bool isError = false;
            string errMssg = "";
            JsonLibs.MyJsonLib ret;

            string resp = "";
            PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct = null;
            if (!checkAirlinesProductCode(productCode, ref resp, ref providerProduct))
            {
                return resp;
            }
            string providerCode = providerProduct.ProviderCode;

            string trxNumber = localDB.getProductTrxNumber(out xError);
            decimal totalBaseAmount = 0;
            decimal totalAmount = 0;
            string ErrRepl = "";
            string recordLocator="";

            long TransactionRef_id = localDB.getTransactionReffIdSequence(out xError);
            //int traceNumber = StaticCommonLibrary.CommonLibrary.getNextProductTraceNumber();
            int traceNumber = localDB.getNextProductTraceNumber();
            DateTime trxTime = DateTime.Now;
            bool doReversal = false;
            int adminFee = 0;
            int nilaiTransaksiKeProvider = 0;

            if (providerCode != "115")      // jika citilink
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid product code", "");
            }
            else
            {
                CitilinkLib.CitilinkHandler ch = new CitilinkLib.CitilinkHandler(
                    commonSettings.getString("Citilink_AgenName"),
                    commonSettings.getString("Citilink_AgenPassword"),
                    commonSettings.getString("Citilink_DomainCode")
                    );
                ch.agentSignature = clientSignature;

                if (!ch.getPaymentAmount(ref totalAmount, ref totalBaseAmount, clientSignature))
                {
                    ch.Dispose();
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "412", "Failed to get detail payment", "");
                }

				//if (!localDB.getAdminFeeAndCustomerFee(productCode, providerProduct.ProviderCode,
				if (!localDB.getAdminFeeAndCustomerFee(productCode,1, appID,
                    totalBaseAmount,
                    ref adminFee, out xError))
                {
					ch.Dispose ();
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid product code", "");
                }

                LogWriter.showDEBUG(this, "Total Base Amount: " + totalBaseAmount.ToString() + "\r\n" +
                                        "Total Amount: " + totalAmount.ToString() + "\r\n" +
                                        "Admin Fee: " + adminFee.ToString());

                nilaiTransaksiKeProvider = decimal.ToInt32(totalAmount - adminFee);

                //ch.Dispose();
                //return HTTPRestDataConstruct.constructHTTPRestResponse(200, "208", "Sementara saja", "");
				if (!getAirlinesPaymentFromClient(appID, userId, providerProduct.TransactionCodeSufix, 
					productCode, providerCode,
                    decimal.ToInt32(totalAmount), TransactionRef_id, ref ErrRepl))
                {
                    ch.Dispose();
                    isError = true;
                    errMssg = "Failed to get payment from agent";
                    ret = null;
                    //return HTTPRestDataConstruct.constructHTTPRestResponse(400, "412", "Failed to get payment from agent", "");
                }
                else
                {
                    ret = ch.commitPayment((short)paxCount, "IDR", ref recordLocator,
                        clientSignature, ref isError);
                    if (isError || (ret == null)) doReversal = true;
                }
                errMssg = ch.lastErrorMessage;
                ch.Dispose();
            }
            DateTime rxTime = DateTime.Now;

            LogWriter.showDEBUG(this, "=========== BERES TRANSAKSI LANGSUNG INSERT LOG");

            // Transfer reguler untuk Fee dan pembagian Pot Bagi dilakukan di service lain
            // dengan trigger dari insert ini
            if (!localDB.insertCompleteTransactionLog(TransactionRef_id, productCode, providerProduct.ProviderProductCode,
                        userId.Substring(commonSettings.getString("UserIdHeader").Length), recordLocator,
                        nilaiTransaksiKeProvider.ToString(), traceNumber.ToString(), trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        adminFee.ToString(), providerProduct.ProviderCode, providerProduct.CogsPriceId,
                        0, 0, "", trxTime.ToString("yyyy-MM-dd HH:mm:ss"), "", trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        "COMMIT",
                        trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        ((isError == false) ? "Success":"Failed"),
                        rxTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        (isError == false), errMssg, trxNumber, false, providerProduct.fIncludeFee,
						"", "", out xError))
            {
                // Jadwalkan masuk database
                TransactionLog.write(TransactionRef_id, productCode, providerProduct.ProviderProductCode, providerProduct.ProviderCode,
                        userId.Substring(commonSettings.getString("UserIdHeader").Length), recordLocator,
                        nilaiTransaksiKeProvider.ToString(), decimal.ToInt32(totalAmount), adminFee,
                        providerProduct.CogsPriceId,
                        traceNumber.ToString(), trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        0, 0, "", trxTime.ToString("yyyy-MM-dd HH:mm:ss"), "",
                        trxTime.ToString("yyyy-MM-dd HH:mm:ss"), "COMMIT",
                        trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        ((isError == false) ? "Success" : "Failed"),
                        rxTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        (isError == false), errMssg, false);
            }

            if (doReversal)
            {
                // reversal payment disini===================================
				if (!payBackAirlinesPaymentToClient(userId, 
					providerProduct.TransactionCodeSufix, 
					decimal.ToInt32(totalAmount),
                    TransactionRef_id, trxNumber, ref ErrRepl))
                {
                    return ErrRepl;
                }
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "418", "Commit failed, transaction aborted!", "");
                //return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", errMssg, "");
            }
            if (ret == null)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "418", "Commit failed, transaction aborted!", "");
            }
            string repl = ret.JSONConstruct();
            ret.Dispose();
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", repl);
        }

		private string AirlinesCitilinkChangeAgentPassword(HTTPRestConstructor.HttpRestRequest clientData)
        {
			string[] fields = { "fiLogin", "fiPassword", "fiCitilinkOldPassword", "fiCitilinkNewPassword"};

			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}

			if ((!jsonConv.ContainsKey("fiLoginId")) || (!jsonConv.ContainsKey("fiPassword")) ||
				(!jsonConv.ContainsKey("fiCitilinkOldPassword")) || (!jsonConv.ContainsKey("fiCitilinkNewPassword")))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields not found", "");
			}

			string userLogin;
			string userPassword;
			string CitilinkOldPassword;
			string CitilinkNewPassword;

            try
            {
				userLogin = ((string)jsonConv["fiLoginId"]).Trim();
				userPassword = ((string)jsonConv["fiPassword"]).Trim();
				CitilinkOldPassword = ((string)jsonConv["fiCitilinkOldPassword"]).Trim();
				CitilinkNewPassword = ((string)jsonConv["fiCitilinkNewPassword"]).Trim();
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
            }

			if (CitilinkOldPassword != commonSettings.getString ("Citilink_AgenPassword")) {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "409", "Invalid password", "");
			}

            string errMssg = "";

			CitilinkLib.CitilinkHandler ch = new CitilinkLib.CitilinkHandler (
                          commonSettings.getString ("Citilink_AgenName"),
                          commonSettings.getString ("Citilink_AgenPassword"),
                          commonSettings.getString ("Citilink_DomainCode")
                      );
			if (!ch.ChangeAgentPassword (CitilinkNewPassword)) {
				errMssg = ch.lastErrorMessage;
				ch.Dispose ();
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "410", 
					"Failed to change Citilink agent password: " + errMssg, "");
			}

			ch.Dispose ();

			localDB.updateCitilinkPassword (CitilinkNewPassword, out xError);

			commonSettings.ReloadSettings ();

			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", "");
        }

        private bool cek_TokenSecurity(string userPhone, JsonLibs.MyJsonLib jsont, 
            ref string token, ref string httpRepl)
        {
            token = "";
            try
            {
                token = ((string)jsont["fiToken"]).Trim();
            }
            catch
            {
                 httpRepl = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No session token or invalid token field", "");
                 return false;
            }

            // cek detek sessionnya
            if (!CommonLibrary.isSessionExist(userPhone, token))
            {
				LOG_Handler.LogWriter.showDEBUG (this, "Cek Token Session: " + userPhone + 
					", token: " + token);
				httpRepl = HTTPRestDataConstruct.constructHTTPRestResponse(400, "504", "Invalid session", "");
                return false;
            }
            return true;
        }

		public string Process(int reqPathCode, HTTPRestConstructor.HttpRestRequest clientData)
        {
			clientDataSource = clientData;
            if (reqPathCode == commonSettings.getInt("CommandProductTransaction"))
            {
                return ProductTransaction(clientData);
            }
			if (reqPathCode == commonSettings.getInt("CommandProductNitrogenShopTransaction"))
			{
				// Toko khusus nitrogen saja
				using (ShopsHandler.ShopsTransactions NitrogenShop = new ShopsHandler.ShopsTransactions(clientData, commonSettings)){
					return NitrogenShop.NitrogenShopMakeOrder();
				}
			}
            else if (reqPathCode == commonSettings.getInt("CommandProductCitilinkGetAvailability"))
            {
                return ProductAirlinesGetAvailability(clientData);
            }
            else if (reqPathCode == commonSettings.getInt("CommandProductCitilinkSellRequestAll"))
            {
                return ProductAirlinesSellRequestAll(clientData);
            }
            else if (reqPathCode == commonSettings.getInt("CommandProductCitilinkSellRequest"))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(200, "204", "Not implemented", "");
                //return ProductAirlinesSellRequest(clientData);
            }
            else if (reqPathCode == commonSettings.getInt("CommandProductCitilinkSellRequestSsrINF"))
            {
                return ProductAirlinesINFSellRequest(clientData);
            }
            else if (reqPathCode == commonSettings.getInt("CommandProductCitilinkUpdateContactPassenger"))
            {
                return ProductAirlinesUpdateContactAndPassengers(clientData);
            }
            else if (reqPathCode == commonSettings.getInt("CommandProductCitilinkAddPayment"))
            {
                return ProductAirlinesAddPayment(clientData);
            }
            else if (reqPathCode == commonSettings.getInt("CommandProductCitilinkCommitRequest"))
            {
                return ProductAirlinesCommit(clientData);
            }
			else if (reqPathCode == commonSettings.getInt("CommandProductCitilinkChangePassword"))
			{
				return AirlinesCitilinkChangeAgentPassword(clientData);
			}
			else if (reqPathCode == commonSettings.getInt("CommandProductPrepaidInquiry"))
			{
				return ProductPrepaidInquiry(clientData);
			}
			else if (reqPathCode == commonSettings.getInt("CommandProductInquiryProductGroupList"))
			{
				using (ProductQuery pq = new ProductQuery(clientData, commonSettings)){
					return pq.ProductListInquiry();
				}
			}
			else if (reqPathCode == commonSettings.getInt("CommandProductIconoxEWalletPaymentInit"))
			{
				using (OnlineSmartCardTransaction OSC = new OnlineSmartCardTransaction(clientData, commonSettings)){
					return OSC.IconoxInitPayment();
				}
			}
			else if (reqPathCode == commonSettings.getInt("CommandProductIconoxEWalletPaymentConfirm"))
			{
				using (OnlineSmartCardTransaction OSC = new OnlineSmartCardTransaction(clientData, commonSettings)){
					//return OSC.IconoxConfirmPayment();
					return OSC.IconoxConfirmPaymentWithLog();
				}
			}
			else if (reqPathCode == commonSettings.getInt("CommandProductIconoxEWalletTopUp"))
			{
				using (IconoxTrxHandlerV2.IconoxTrxV2 IconoxTrx = new IconoxTrxHandlerV2.IconoxTrxV2(clientData, commonSettings)){
					return IconoxTrx.DoTopUp();
				}
			}
			else if (reqPathCode == commonSettings.getInt("CommandProductIconoxAuthentication"))
			{
				using (IconoxTrxHandlerV2.IconoxTrxV2 IconoxTrx = new IconoxTrxHandlerV2.IconoxTrxV2(clientData, commonSettings)){
					return IconoxTrx.DoAuthentication();
				}
			}
			else if (reqPathCode == commonSettings.getInt("CommandProductNitrogenSettlement"))
			{
				using (NitrogenClientHandler.SettlementTrx NitrogenTRx = 
					new NitrogenClientHandler.SettlementTrx (clientData, commonSettings)){
					return NitrogenTRx.ProcessAsOnline();
				}
			}
			else if (reqPathCode == commonSettings.getInt("CommandProductNitrogenFreePurchase"))
			{
				using (NitrogenFreeTransaction NitroFree = new NitrogenFreeTransaction(clientData, commonSettings)){
					return NitroFree.FreePurchase();
				}
			}

			// Untuk DEMO BPJS SAJA.....
			#region DEMO BPJS Saja.....................
			else if (reqPathCode == commonSettings.getInt("CommandProductBPJS-THT-InquiryBalance"))
			{
				using (BPJS_THT.Terminal_Handler BpjsTht = new BPJS_THT.Terminal_Handler(clientData, commonSettings)){
					return BpjsTht.InfoSaldo();
				}
			}
			else if (reqPathCode == commonSettings.getInt("CommandProductBPJS-THT-PaymentOnline"))
			{
				using (BPJS_THT.Terminal_Handler BpjsTht = new BPJS_THT.Terminal_Handler(clientData, commonSettings)){
					return BpjsTht.PaymentOnline();
				}
			}
			else if (reqPathCode == commonSettings.getInt("CommandProductBPJS-THT-TopUpOnline"))
			{
				using (BPJS_THT.Terminal_Handler BpjsTht = new BPJS_THT.Terminal_Handler(clientData, commonSettings)){
					return BpjsTht.TopUpOnline();
				}
			}
			#endregion


//			else if (reqPathCode == commonSettings.getInt("CommandProductInquiryShopProductCategory"))
//			{
//				using (ProductQuery pq = new ProductQuery(clientData, commonSettings)){
//					return pq.ShopProductCategoryInquiry();
//				}
//			}
//			else if (reqPathCode == commonSettings.getInt("CommandProductInquiryShopProduct"))
//			{
//				using (ProductQuery pq = new ProductQuery(clientData, commonSettings)){
//					return pq.ShopProductInquiry();
//				}
//			}

//			else if (reqPathCode == commonSettings.getInt("CommandProductInquiryPrefixPrepaidPhone"))
//			{
//				using (ProductQuery pq = new ProductQuery(clientData, commonSettings)){
//					return pq.PrepaidPhonePrefixInquiry();
//				}
//			}
//			else if (reqPathCode == commonSettings.getInt("CommandProductInquiryPrepaidPhoneProduct"))
//			{
//				using (ProductQuery pq = new ProductQuery(clientData, commonSettings)){
//					return pq.PrepaidPhoneProductInquiry();
//				}
//			}
//			else if (reqPathCode == commonSettings.getInt("CommandProductInquiryCommonProduct"))
//			{
//				using (ProductQuery pq = new ProductQuery(clientData, commonSettings)){
//					return pq.CommonProductInquiry();
//				}
//			}
            else
            {
                // reject
                return HTTPRestDataConstruct.constructHTTPRestResponse(200, "204", "Not implemented", "");
            }
        }

    }
}
