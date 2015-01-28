using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPOBHttpRestData;
using System.Collections;
using PPOBServerHandler;
using TransferReguler;
using LOG_Handler;
using StaticCommonLibrary;

namespace Process_MPAccountAccess
{
    // jang transfer cashin dan cashout
//    INSERT INTO transfer_temp (id,customer_phone,loader_phone,transfer_date,token,is_success,is_del,host)
//VALUES (DEFAULT,'62xxxxxx','62xxxxxx','YYYY-MM-DD HH:mi:ss +7','123456','f','f','localhost');

    class AccountTransactions: IDisposable
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
        ~AccountTransactions()
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
        }

        JsonLibs.MyJsonLib jsonConv;
        HTTPRestConstructor HTTPRestDataConstruct;
        PPOBDatabase.PPOBdbLibs localDB;
        Exception xError;

        PublicSettings.Settings commonSettings;

        public AccountTransactions(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            HTTPRestDataConstruct = new HTTPRestConstructor();
            jsonConv = new JsonLibs.MyJsonLib();
            localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
                commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
        }

        private void ReformatPhoneNumber(ref string phone)
        {
            phone = phone.Trim();
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

        public string AccountTransfer(HTTPRestConstructor.HttpRestRequest clientData)
        {
            //using (QvaTransactions QVA = new QvaTransactions(commonSettings.getString("SandraHost"), commonSettings.getInt("SandraPort"), localDB))
            //{
            //    return QVA.TransferBalance(clientData);
            //}

            //              /gateway/010130
            //            {"sender" : <String, not null>, "amount" : <Double, not null>, 
            //              "receiver" : <String, not null>, "channelId" : <String, not null>,     
            //              "transactionCode" : <String, not null> } 
            if (clientData.Body.Length == 0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
            }
            if (!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }
            // TAMPUNG data json dari Client

			string passw = "";
			string securityToken = "";
			string userPhone = "";
            string targetPhone = "";
            int amount = 0;
			string appID = "";
            try
            {
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
				passw = ((string)jsonConv["fiPassword"]).Trim();
				//securityToken = ((string)jsonConv["fiToken"]).Trim();
				amount = ((int)jsonConv["fiAmount"]);
                targetPhone = ((string)jsonConv["fiTargetPhone"]).Trim();
                userPhone = ((string)jsonConv["fiPhone"]).Trim().Replace("-", "");
            }
            catch
            {
                // field tidak ditemukan atau formatnya salah
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not completed", "");
            }
            if (amount <= 0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid amount", "");
            }

            ReformatPhoneNumber(ref userPhone);
            ReformatPhoneNumber(ref targetPhone);

//			string httprepl = "";
//			if (!cek_TokenSecurity (userPhone, jsonConv, ref securityToken, ref httprepl)) {
//				return httprepl;
//			}
            if (!localDB.isUserPasswordEqual(userPhone, passw, out xError))
            {
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                }
                else
                {
                    // password error
                    return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Login failed", "");
                }
            }
            // password ok

            if (!localDB.isPhoneExistAndActive(targetPhone, out xError))
            {
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                }
                else
                {
                    // password error
                    return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Target phone not registered", "");
                }
            }

			string productCode = commonSettings.getString ("RegularTransferProductCode");
            string errCode = "";
            string errMessage = "";
			Exception exrr = null;
			int adminFee = 0;
			int productAmount = amount;

			string trxNumber = localDB.getProductTrxNumber (out xError);	// .getCashInTrxNumber(out xError);
			string traceNumb = localDB.getNextProductTraceNumberString (out xError);
			DateTime trxTime = DateTime.Now;

            long TransactionRef_id = localDB.getRegulerTrxReffIdSequence(out xError);

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

			try
			{
				//Console.WriteLine(" productAmount " + productAmount);
				//Console.WriteLine(" intNYA : " + int.Parse(productAmount));
				//if (!localDB.getAdminFeeAndCustomerFee(productCode, providerProduct.ProviderCode, amount,
				if (!localDB.getAdminFeeAndCustomerFee(productCode,1, appID, amount,
					ref adminFee, out xError))
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
			int nilaiTransaksiKeProvider = 0;
			int nilaiYangMasukLog = 0;

			if (providerProduct.fIncludeFee)
			{
				providerProduct.CurrentPrice = productAmount;
				nilaiYangMasukLog = productAmount - adminFee;
			}
			else
			{
				providerProduct.CurrentPrice = productAmount - adminFee;
				nilaiYangMasukLog = providerProduct.CurrentPrice;
			}
			if (providerProduct.CurrentPrice <= 0)
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Bad amount", "");
			}

			//productAmount = prdAmount.ToString();
			nilaiTransaksiKeProvider = providerProduct.CurrentPrice;
			string qvaInvoiceNumber = "";
			bool qvaReversalRequired = false;

			using (TransferReguler.CollectTransfer TransferReg =
                new TransferReguler.CollectTransfer(commonSettings))
            {
                int totalTransfer = amount;
                try
                {
					LogWriter.write(this, LogWriter.logCodeEnum.INFO, "Transfer from user " + userPhone + " to " + targetPhone);
					//LogWriter.show(this, "Transfer from user " + userPhone + " to " + targetPhone);
                    if (!TransferReg.TransferCustomerToCustomer(userPhone, targetPhone, 
						commonSettings.getString("TRANSFER_SUFIX_CUSTOMER_TO_CUSTOMER"), 
						totalTransfer,
                        TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.TransferReguler,
						"Simple regular transfer", ref qvaInvoiceNumber, ref qvaReversalRequired, 
                        ref errCode, ref errMessage))
                    {
						if(qvaReversalRequired)
							TransferReg.Reversal(TransactionRef_id,qvaInvoiceNumber, ref errCode, ref errMessage);
                        LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : [" + errCode + "]" + errMessage);
						return HTTPRestDataConstruct.constructHTTPRestResponse(400, errCode, errMessage, "");
                    }
                }
                catch (Exception ex)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : " + ex.getCompleteErrMsg());
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to Transfer", "");
                }
            }

			// Disini simpan transaction log seperti transaksi product
			LogWriter.showDEBUG(this, "=========== BERES TRANSAKSI LANGSUNG INSERT LOG");

			// Transfer reguler untuk Fee dan pembagian Pot Bagi dilakukan di service lain
			// dengan trigger dari insert ini
			if (!localDB.insertCompleteRegularTransactionLog(TransactionRef_id, productCode, providerProduct.ProviderProductCode,
				userPhone, targetPhone,
				amount.ToString(), traceNumb.ToString(), trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
				adminFee.ToString(), providerProduct.ProviderCode, providerProduct.CogsPriceId,
				0, 0, "", trxTime.ToString("yyyy-MM-dd HH:mm:ss"), "", trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
				"TransferCustomerToCustomer",
				trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
				"TransferCustomerToCustomer",
				trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
				true, "", trxNumber, false, providerProduct.fIncludeFee,
				out xError))
			{
				// Jadwalkan masuk database
//				TransactionLog.write(TransactionRef_id, productCode, providerProduct.ProviderProductCode, providerProduct.ProviderCode,
//					userId.Substring(commonSettings.getString("UserIdHeader").Length), customerProductNumber,
//					productAmount.ToString(), nilaiYangMasukLog, adminFee,
//					providerProduct.CogsPriceId,
//					traceNumb.ToString(), trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
//					0, 0, "", skrg.ToString("yyyy-MM-dd HH:mm:ss"), "", skrg.ToString("yyyy-MM-dd HH:mm:ss"), strJson,
//					trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
//					strRecJson,
//					trxRecTime.ToString("yyyy-MM-dd HH:mm:ss"),
//					isSuccessPayment, failedReason, canReversal);
			}

            Hashtable userInfo = localDB.getLoginInfoByUserPhone(userPhone, out xError);
            // siapkan json untuk notifikasi ke agen
            DateTime skrg = DateTime.Now;
            jsonConv.Clear();
            jsonConv.Add("fiAgentPhone", userPhone);
            jsonConv.Add("fiAgentName", (string)userInfo["firstName"] + " " + (string)userInfo["lastName"]);
            jsonConv.Add("fiAmount", amount);
            jsonConv.Add("fiTransferType", "03");
            jsonConv.Add("fiCode", "00");
            jsonConv.Add("fiNotificationDateTime", skrg.ToString("yyyy-MM-dd HH:mm:ss"));
            jsonConv.Add("fiTrxNumber", trxNumber);
            string subJson = jsonConv.JSONConstruct();

            // Insert di tabel notifikasi saja
            localDB.insertNotificationQueue(userPhone, targetPhone, skrg.ToString("yyyy-MM-dd HH:mm:ss"),
                "07", subJson, out xError);

            jsonConv.Clear();
            jsonConv.Add("fiReplyCode", "00");
//            jsonConv.Add("fiAdminFee", adminFee);
            jsonConv.Add("fiAmount", amount);
            jsonConv.Add("fiCustomerPhone", targetPhone);
            jsonConv.Add("fiTrxNumber", trxNumber);
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
            //return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", "{\"fiReplyCode\":\"00\"}");
        }

		public string AccountRootTransfer(HTTPRestConstructor.HttpRestRequest clientData)
		{
			//              /gateway/010130
			//            {"sender" : <String, not null>, "amount" : <Double, not null>, 
			//              "receiver" : <String, not null>, "channelId" : <String, not null>,     
			//              "transactionCode" : <String, not null> } 
			if (clientData.Body.Length == 0)
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
			}
			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}
			// TAMPUNG data json dari Client

			string RootPassw = "";
			string RootPhone = "";
			string sourceAccount = "";
			string targetPhone = "";
			int amount = 0;
			try
			{
				RootPhone = ((string)jsonConv["fiPhone"]).Trim().Replace("-", "");
				RootPassw = ((string)jsonConv["fiPassword"]).Trim();
				amount = ((int)jsonConv["fiAmount"]);
				sourceAccount = ((string)jsonConv["fiFromAccount"]).Trim();
				targetPhone = ((string)jsonConv["fiTargetPhone"]).Trim();
			}
			catch
			{
				// field tidak ditemukan atau formatnya salah
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not completed", "");
			}
			if (amount <= 0)
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid amount", "");
			}

			ReformatPhoneNumber(ref RootPhone);
			ReformatPhoneNumber(ref targetPhone);

			if (!localDB.isRootPasswordEqual(RootPhone, RootPassw, out xError))
			{
				if (xError != null)
				{
					LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Server database error");
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
				}
				else
				{
					// password error
					LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Root login failed using phone number: " + RootPhone);
					return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Login failed", "");
				}
			}

			// password ok

			string errCode = "";
			string errMessage = "";
			string trxNumber = localDB.getProductTrxNumber (out xError);	// .getCashInTrxNumber(out xError);
			string productCode = commonSettings.getString ("RegularTransferProductCode");
			Exception exrr = null;
			int adminFee = 0;
			//int productAmount = amount;

			long TransactionRef_id = localDB.getRegulerTrxReffIdSequence(out xError);
			string qvaInvoiceNumber = "";
			bool qvaReversalRequired = false;

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

			using (TransferReguler.CollectTransfer TransferReg =
				new TransferReguler.CollectTransfer(commonSettings))
			{
				int totalTransfer = amount;
				try
				{
					LogWriter.write(this,LogWriter.logCodeEnum.INFO, "Transfer from account " + sourceAccount + " to " + targetPhone);
					if (!TransferReg.TransferAccountToCustomer(sourceAccount, targetPhone, 
						commonSettings.getString("TRANSFER_SUFIX_CUSTOMER_TO_CUSTOMER"), totalTransfer,
						TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.TransferReguler,
						"Root regular transfer", ref qvaInvoiceNumber, ref qvaReversalRequired, 
						ref errCode, ref errMessage))
					{
						if(qvaReversalRequired)
							TransferReg.Reversal(TransactionRef_id,qvaInvoiceNumber, ref errCode, ref errMessage);
						LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : [" + errCode.ToString() + "]" + errMessage);
						return HTTPRestDataConstruct.constructHTTPRestResponse(400, errCode, errMessage, "");
					}
				}
				catch (Exception ex)
				{
					LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : " + ex.getCompleteErrMsg());
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to Transfer", "");
				}
			}


			string traceNumb = localDB.getNextProductTraceNumberString (out xError);
			DateTime trxTime = DateTime.Now;

//			localDB.insertCompleteTransactionLog (TransactionRef_id, productCode, providerProduct.ProviderProductCode,
//				RootPhone, targetPhone,
//				amount.ToString (), traceNumb.ToString (), trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
//				adminFee.ToString (), providerProduct.ProviderCode, providerProduct.CogsPriceId,
//				0, 0, "", trxTime.ToString ("yyyy-MM-dd HH:mm:ss"), "", trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
//				"TransferRootToCustomer",
//				trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
//				"TransferRootToCustomer",
//				trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
//				true, "", trxNumber, false, providerProduct.fIncludeFee,
//				out xError);

			localDB.insertCompleteRegularTransactionLog (TransactionRef_id, productCode, providerProduct.ProviderProductCode,
				RootPhone, "Account " + sourceAccount + " to " + targetPhone,
				//sourceAccount, targetPhone,
				//RootPhone, targetPhone,
				amount.ToString (), traceNumb.ToString (), trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				adminFee.ToString (), providerProduct.ProviderCode, providerProduct.CogsPriceId,
				0, 0, "", trxTime.ToString ("yyyy-MM-dd HH:mm:ss"), "", trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				"TransferAccountToCustomer",
				trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				"TransferAccountToCustomer",
				trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				true, "", trxNumber, false, providerProduct.fIncludeFee,
				out xError);



			//Hashtable userInfo = localDB.getLoginInfoByUserPhone(RootPhone, out xError);
			// siapkan json untuk notifikasi ke agen
			DateTime skrg = DateTime.Now;
			jsonConv.Clear();
			jsonConv.Add("fiAgentPhone", "PowerHouse");
			jsonConv.Add("fiAgentName", "Admin");
			jsonConv.Add("fiAmount", amount);
			jsonConv.Add("fiTransferType", "03");
			jsonConv.Add("fiCode", "00");
			jsonConv.Add("fiNotificationDateTime", skrg.ToString("yyyy-MM-dd HH:mm:ss"));
			jsonConv.Add("fiTrxNumber", trxNumber);
			string subJson = jsonConv.JSONConstruct();

			// Insert di tabel notifikasi saja
			//			localDB.insertNotificationQueue("PowerHouse Admin", targetPhone, skrg.ToString("yyyy-MM-dd HH:mm:ss"),
			//	"07", subJson, out xError);
			localDB.insertNotificationQueue(RootPhone, targetPhone, skrg.ToString("yyyy-MM-dd HH:mm:ss"),
				"07", subJson, out xError);

			jsonConv.Clear();
			jsonConv.Add("fiReplyCode", "00");
//            jsonConv.Add("fiAdminFee", adminFee);
			jsonConv.Add("fiAmount", amount);
			jsonConv.Add("fiSourceAccount", sourceAccount);
			jsonConv.Add("fiCustomerPhone", targetPhone);
			jsonConv.Add("fiTrxNumber", trxNumber);
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
			//return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", "{\"fiReplyCode\":\"00\"}");
		}

		public string TestQvaConnection(HTTPRestConstructor.HttpRestRequest clientData){
			LogWriter.show (this, "TESSS QVA TOKEN");
			using (SandraLibs sandra = new SandraLibs (commonSettings.getInt ("QvaTimeOut"),
				                           (commonSettings.getString ("SandraUseTcpMethod").ToLower () == "true"),
				                           commonSettings.getString ("SandraHmsUserAuth"),
				                           commonSettings.getString ("SandraHmsSecretKey"),
				                           commonSettings.getString ("SandraAuthId"))) {
				if (sandra.RequestToken (
					    commonSettings.getString ("SandraHost"), 
					commonSettings.getInt ("SandraPort"), true, SandraLibs.TokenHostEnum.QVA)){
					LogWriter.show (this, "Token OK");
					LogWriter.showDEBUG (this, "Token OK");
					return "";
				}
				LogWriter.show (this, "Token Gagal");
				LogWriter.showDEBUG (this, "Token Gagal");
				return "";
			}
		}

		public string AccountFREETransfer(HTTPRestConstructor.HttpRestRequest clientData)
		{
			if (clientData.Body.Length == 0)
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
			}
			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}
			// TAMPUNG data json dari Client

			string userPassword = "";
			string userLogin = "";
			string userPhone = "";
			string sourceAccount = "";
			string targetAccount = "";
			string qvaTransactionCode =  commonSettings.getString ("TRANSFER_FROM_CUSTOMER_TO_CUSTOMER") + commonSettings.getString("TRANSFER_SUFIX_CUSTOMER_TO_CUSTOMER");
			string qvaSourceId = commonSettings.getString("QVA_Registration_Source");
			int amount = 0;
			try
			{
				userLogin = ((string)jsonConv["fiUserLogin"]).Trim().ToLower();
				userPassword = ((string)jsonConv["fiLoginPassword"]).Trim();
				userPhone = ((string)jsonConv["fiUserPhone"]).Trim().Replace("-", "");

				amount = ((int)jsonConv["fiAmount"]);
//				RootPhone = ((string)jsonConv["fiPhone"]).Trim().Replace("-", "");
//				RootPassw = ((string)jsonConv["fiPassword"]).Trim();
				sourceAccount = ((string)jsonConv["fiFromAccount"]).Trim();
				targetAccount = ((string)jsonConv["fiTargetAccount"]).Trim();
			}
			catch
			{
				// field tidak ditemukan atau formatnya salah
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not completed", "");
			}

			try
			{
				if(jsonConv.isExists("fiTransactionCode"))
					qvaTransactionCode = ((string)jsonConv["fiTransactionCode"]).Trim();
			}
			catch
			{
				// field tidak ditemukan atau formatnya salah
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid TransactionCode", "");
			}
			try
			{
				if(jsonConv.isExists("fiSourceId"))
					qvaSourceId = ((string)jsonConv["fiSourceId"]).Trim();
			}
			catch
			{
				// field tidak ditemukan atau formatnya salah
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid SourceId", "");
			}


			if (amount <= 0)
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid amount", "");
			}

			//ReformatPhoneNumber(ref RootPhone);

			if(!localDB.isUserHasRights(userLogin, userPassword, "administrator-free_transfer", out xError))
				//if (!localDB.isRootPasswordEqual(RootPhone, RootPassw, out xError))
			{
				if (xError != null)
				{
					LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Server database error");
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
				}
				else
				{
					// password error
					//LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Root login failed using phone number: " + RootPhone);
					LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Login failed using userId: " + userLogin + " and phone: " + userPhone);
					return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Login failed using userId: " + userLogin + " and userPhone: " + userPhone, "");
				}
			}

			// password ok

			string errCode = "";
			string errMessage = "";
			string productCode = commonSettings.getString ("RegularTransferProductCode");
			Exception exrr = null;
			int adminFee = 0;
			//int productAmount = amount;

			long TransactionRef_id = localDB.getRegulerTrxReffIdSequence(out xError);
			string qvaInvoiceNumber = "";
			bool qvaReversalRequired = false;

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

			using (TransferReguler.CollectTransfer TransferReg =
				new TransferReguler.CollectTransfer(commonSettings))
			{
				int totalTransfer = amount;
				try
				{
					LogWriter.write(this,LogWriter.logCodeEnum.INFO, "Transfer from account " + sourceAccount + " to " + targetAccount);
					if (!TransferReg.TransferAccountToAccount(sourceAccount, targetAccount, 
						commonSettings.getString("TRANSFER_SUFIX_CUSTOMER_TO_CUSTOMER"), totalTransfer,
						TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.TransferReguler,
						"FREE regular transfer", ref qvaInvoiceNumber, ref qvaReversalRequired, 
						ref errCode, ref errMessage, qvaSourceId, qvaTransactionCode))
					{
						if(qvaReversalRequired)
							TransferReg.Reversal(TransactionRef_id,qvaInvoiceNumber, ref errCode, ref errMessage);
						LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : [" + errCode.ToString() + "]" + errMessage);
						return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to Transfer", "");
					}
				}
				catch (Exception ex)
				{
					LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : " + ex.getCompleteErrMsg());
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to Transfer", "");
				}
			}


			string traceNumb = localDB.getNextProductTraceNumberString (out xError);
			string trxNumber = localDB.getProductTrxNumber (out xError);	// .getCashInTrxNumber(out xError);
			DateTime trxTime = DateTime.Now;

//			localDB.insertCompleteTransactionLog (TransactionRef_id, productCode, providerProduct.ProviderProductCode,
//				RootPhone, targetPhone,
//				amount.ToString (), traceNumb.ToString (), trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
//				adminFee.ToString (), providerProduct.ProviderCode, providerProduct.CogsPriceId,
//				0, 0, "", trxTime.ToString ("yyyy-MM-dd HH:mm:ss"), "", trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
//				"TransferRootToCustomer",
//				trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
//				"TransferRootToCustomer",
//				trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
//				true, "", trxNumber, false, providerProduct.fIncludeFee,
//				out xError);

			localDB.insertCompleteRegularTransactionLog (TransactionRef_id, productCode, providerProduct.ProviderProductCode,
				userPhone, sourceAccount + " to " + targetAccount,
				amount.ToString (), traceNumb.ToString (), trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				adminFee.ToString (), providerProduct.ProviderCode, providerProduct.CogsPriceId,
				0, 0, "", trxTime.ToString ("yyyy-MM-dd HH:mm:ss"), "", trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				"TransferAccountToAccount",
				trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				"TransferAccountToAccount",
				trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				true, "", trxNumber, false, providerProduct.fIncludeFee,
				out xError);



			//Hashtable userInfo = localDB.getLoginInfoByUserPhone(RootPhone, out xError);
			// siapkan json untuk notifikasi ke agen
			DateTime skrg = DateTime.Now;

			jsonConv.Clear();
			jsonConv.Add("fiReplyCode", "00");
//            jsonConv.Add("fiAdminFee", adminFee);
			jsonConv.Add("fiAmount", amount);
			jsonConv.Add("fiSourceAccount", sourceAccount);
			jsonConv.Add("fiCustomerAccount", targetAccount);
			jsonConv.Add("fiTrxNumber", trxNumber);
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
		}

        public string AccountLastTransactionHistory(HTTPRestConstructor.HttpRestRequest clientData)
        {
            if (clientData.Body.Length == 0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
            }
            if (!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }

            string userPhone = "";
            string userPassword = "";
            int historyLimit = 1;
            try
            {
                userPhone = ((string)jsonConv["fiPhone"]).Trim();
                userPassword = ((string)jsonConv["fiPassword"]).Trim();
                historyLimit = (int)jsonConv["fiLimit"];
            }
            catch
            {
                // field tidak ditemukan atau formatnya salah
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not found", "");
            }
            if (userPhone.Length <= 5)
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "409", "Bad phone number", "");

            ReformatPhoneNumber(ref userPhone);

            //  Yang harus disiapkan
            string userId = commonSettings.getString("UserIdHeader") + userPhone;

            // cek dengan database, apakah password sama?
            if (!localDB.isUserPasswordEqual(userPhone, userPassword, out xError))
            {
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                }
                else
                {
                    // password error
                    return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Wrong password", "");
                }
            }
            // password ok

			using (SandraLibs sandra = new SandraLibs(commonSettings.getInt("QvaTimeOut"),
				(commonSettings.getString("SandraUseTcpMethod").ToLower() == "true"),
				commonSettings.getString("SandraHmsUserAuth"),
				commonSettings.getString("SandraHmsSecretKey"),
				commonSettings.getString("SandraAuthId")))
            {
                JsonLibs.MyJsonArray historyLog = null;
                double custBalance = 0;
                if ((sandra.GetLastTransactionHistory(
					commonSettings.getString("SandraHost"),commonSettings.getInt("SandraPort"), 
					(commonSettings.getString("SandraIsHttps").ToLower() == "true"), 
					userId, historyLimit, ref historyLog))
                    && (sandra.Inquiry(commonSettings.getString("SandraHost"),
						commonSettings.getInt("SandraPort"), 
						(commonSettings.getString("SandraIsHttps").ToLower() == "true"), 
						userId, ref custBalance))
                    )
                {
                    //Console.WriteLine("Sandra OK");
                    jsonConv.Clear();
                    if (historyLog != null)
                        jsonConv.Add("fiHistoryLogs", historyLog);
                    else
                        jsonConv.Add("fiHistoryLogs", null);

                    jsonConv.Add("fiBalance", custBalance);
                    jsonConv.Add("fiPhone", userPhone);
                    //jsonConv.Add("fiResponseCode", "00");
                    jsonConv.Add("fiReplyCode", "00");

                    // kirim respon ke client
                    return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
                }
                else
                {
                    LogWriter.show(this, "Sandra not OK\r\n" +
                                    "Error code: " + sandra.LastError.ServerCode + "\r\n" +
                                    "Error message: " + sandra.LastError.ServerMessage);
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, sandra.LastError.ServerCode, sandra.LastError.ServerMessage, "");
                }
            }
        }

        public string AccountGetDetailTransaction(HTTPRestConstructor.HttpRestRequest clientData)
        {
            if (clientData.Body.Length == 0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
            }
            if (!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }

            string userPhone = "";
            string userPassword = "";
            string invoiceId = "";
            try
            {
                userPhone = ((string)jsonConv["fiPhone"]).Trim();
                userPassword = ((string)jsonConv["fiPassword"]).Trim();
                invoiceId = ((string)jsonConv["fiInvoiceId"]).Trim();
            }
            catch
            {
                // field tidak ditemukan atau formatnya salah
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not found", "");
            }
            if (userPhone.Length <= 5)
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "409", "Bad phone number", "");

            ReformatPhoneNumber(ref userPhone);

            //  Yang harus disiapkan
			//string userId = commonSettings.getString("UserIdHeader") + userPhone;

            // cek dengan database, apakah password sama?
//            if (!localDB.isUserPasswordEqual(userPhone, userPassword, out xError))
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
            // password ok

			using (SandraLibs sandra = new SandraLibs(commonSettings.getInt("QvaTimeOut"),
				(commonSettings.getString("SandraUseTcpMethod").Trim().ToLower() == "true"),
				commonSettings.getString("SandraHmsUserAuth"),
				commonSettings.getString("SandraHmsSecretKey"),
				commonSettings.getString("SandraAuthId")))
            {
                JsonLibs.MyJsonLib jsDetailTrx = null;
                if (sandra.GetDetailTransaction(
					commonSettings.getString("SandraHost"),commonSettings.getInt("SandraPort"), 
					(commonSettings.getString("SandraIsHttps").ToLower() == "true"), 
					invoiceId, ref jsDetailTrx))
                {
                    //Console.WriteLine("Sandra OK");
                    jsonConv.Clear();
                    if (jsDetailTrx != null)
                        jsonConv.Add("fiDetailTransaction", jsDetailTrx);
                    else
                        jsonConv.Add("fiDetailTransaction", null);

                    jsonConv.Add("fiInvoiceId", invoiceId);
                    //jsonConv.Add("fiResponseCode", "00");
                    jsonConv.Add("fiReplyCode", "00");

                    // kirim respon ke client
                    return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
                }
                else
                {
                    LogWriter.show(this, "Sandra not OK\r\n" +
                                    "Error code: " + sandra.LastError.ServerCode + "\r\n" +
                                    "Error message: " + sandra.LastError.ServerMessage);
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, sandra.LastError.ServerCode, sandra.LastError.ServerMessage, "");
                }
            }
        }

        public string AccountInquiry(HTTPRestConstructor.HttpRestRequest clientData)
        {
            // input dari user diambil dari username dan password, baru bisa inquiry ke host
            if (!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }
            if (!jsonConv.ContainsKey("fiPhone"))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No fiPhone field found", "");
            }
            string userPhone = ((string)jsonConv["fiPhone"]).Trim();
            ReformatPhoneNumber(ref userPhone);

            string userId = commonSettings.getString("UserIdHeader") + userPhone;

            if (userPhone.Length == 0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
            }
            //  Yang harus disiapkan
            double custBalance = 0;

            // konek ke host sandra untuk inquiry
            //if (localIP) sandraHost = "127.0.0.1";
			using (SandraLibs sandra = new SandraLibs(commonSettings.getInt("QvaTimeOut"),
				(commonSettings.getString("SandraUseTcpMethod").Trim().ToLower() == "true"),
				commonSettings.getString("SandraHmsUserAuth"),
				commonSettings.getString("SandraHmsSecretKey"),
				commonSettings.getString("SandraAuthId")))
            {
                if (sandra.Inquiry(
					commonSettings.getString("SandraHost"), commonSettings.getInt("SandraPort"), 
					(commonSettings.getString("SandraIsHttps").ToLower() == "true"), userId, ref custBalance))
                {
                    //Console.WriteLine("Sandra OK");
                    jsonConv.Clear();
                    jsonConv.Add("fiPhone", userPhone);
                    jsonConv.Add("fiBalance", custBalance);
                    // masukkan di database

                    // kirim respon ke client
                    return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
                }
                else
                {
                    LogWriter.show(this, "Sandra not OK\r\n" +
                                    "Error code: " + sandra.LastError.ServerCode + "\r\n" +
                                    "Error message: " + sandra.LastError.ServerMessage);
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, sandra.LastError.ServerCode, sandra.LastError.ServerMessage, "");
                }
            }
        }

        public string AccountRequestTopUp(HTTPRestConstructor.HttpRestRequest clientData)
        {
            // input dari user diambil dari username dan password, baru bisa inquiry ke host
            if (!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }

            if ((!jsonConv.ContainsKey("fiPhone")) || (!jsonConv.ContainsKey("fiAgentPhone")) ||
                //                (!jsonConv.ContainsKey("fiTraceNumber")) || (!jsonConv.ContainsKey("fiReferenceNumber")) ||
                (!jsonConv.ContainsKey("fiDateTime")) || (!jsonConv.ContainsKey("fiPassword")) ||
                (!jsonConv.ContainsKey("fiAmount")) || (!jsonConv.ContainsKey("fiFeeIncluded"))
                )
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields no found", "");
            }
            string userPhone;
            string agentPhone;
            string passw;
            string trxTime;
            bool feeIncluded = false;
			string appID = "";
            try
            {
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
                userPhone = ((string)jsonConv["fiPhone"]).Trim();
                agentPhone = ((string)jsonConv["fiAgentPhone"]).Trim();
                passw = ((string)jsonConv["fiPassword"]).Trim();
                trxTime = ((string)jsonConv["fiDateTime"]).Trim();   //yyyy-MM-dd HH:mm:ss
                feeIncluded = ((bool)jsonConv["fiFeeIncluded"]);
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Bad data", "");
            }

            int amount = 0;
            try
            {
                amount = ((int)jsonConv["fiAmount"]);
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Bad amount", "");
            }

            ReformatPhoneNumber(ref userPhone);
            ReformatPhoneNumber(ref agentPhone);

            if (userPhone == agentPhone)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Phone number can not be equal with agent phone", "");
            }

            if (!localDB.isAccountExist(agentPhone, out xError))
            {
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                }
                else
                {
                    // password error
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Agent phone not registered", "");
                }
            }

            if (!localDB.isUserPasswordEqual(userPhone, passw, out xError))
            {
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                }
                else
                {
                    // password error
                    return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Wrong password", "");
                }
            }
            // password ok

			//string userId = commonSettings.getString("UserIdHeader") + userPhone;
            Hashtable userInfo = localDB.getLoginInfoByUserPhone(userPhone, out xError);

            if (userInfo == null)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(401, "492", "Failed to get customer name", "");
            }

            // ambil info provider dari productCode
            Exception exrr = null;
            PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
            try
            {
                providerProduct = localDB.getProviderProductInfo(commonSettings.getString("CashInProductCode"), out exrr);
                if (exrr != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider product data not found", "");
                }
                if(providerProduct.ProviderCode=="")
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider product data not found", "");
                }
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting provider product data", "");
            }

            int adminFee = 0;
            //int sellerFee = 0;
            //if (!localDB.getAdminFeeAndCustomerFee(commonSettings.getString("CashInProductCode"), providerProduct.ProviderCode, ref adminFee, ref sellerFee, out xError))
            if (!localDB.getAdminFeeAndCustomerFee(commonSettings.getString("CashInProductCode"),
				//providerProduct.ProviderCode, (decimal)amount, ref adminFee, out xError))
				1,appID, (decimal)amount, ref adminFee, out xError))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting admin fee", "");
            }

            if (feeIncluded)
            {
                if(amount<=adminFee)
                {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Invalid amount, Admin fee is " + adminFee.ToString(), "");
                }
                amount -= adminFee;
            }

            // insert ke table sementara untuk verifikasi agen
            if (!localDB.insertCashTrxRequest(trxTime, userPhone, agentPhone, amount, adminFee, "",
                PPOBDatabase.PPOBdbLibs.CashTrxType.CashIn, false, ""))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on write request", "");
            }

            // siapkan json untuk notifikasi ke agen
            jsonConv.Clear();
            jsonConv.Add("fiCustomerPhone", userPhone);
            jsonConv.Add("fiCustomerName", (string)userInfo["firstName"] + " " + (string)userInfo["lastName"]);
            jsonConv.Add("fiAmount", amount);
            jsonConv.Add("fiAdminFee", adminFee);
            jsonConv.Add("fiRequestDateTime", trxTime);
            jsonConv.Add("fiNotificationDateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            string subJson = jsonConv.JSONConstruct();

            // Insert di tabel notifikasi saja
            localDB.insertNotificationQueue(userPhone, agentPhone, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                "03", subJson, out xError);

            jsonConv.Clear();
            jsonConv.Add("fiReplyCode", "00");
            jsonConv.Add("fiAdminFee", adminFee);
            jsonConv.Add("fiAmount", amount);
            jsonConv.Add("fiAgentPhone", agentPhone);
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

        }

        private bool writeLogCashIn(long TransactionReffId, PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct,
            string userPhone,string customerPhone, int amount, int adminFee, 
            bool isSuccess, string sendTime, string productCode, string trxNumber
            )
        {
            //int traceNumber = StaticCommonLibrary.CommonLibrary.getNextProductTraceNumber();
            int traceNumber = localDB.getNextProductTraceNumber();
            xError = null;
            //// masukkan log cash in
            return localDB.insertCashInLog(TransactionReffId,providerProduct.ProviderCode, providerProduct.ProviderProductCode,
                providerProduct.CogsPriceId.ToString(), userPhone, customerPhone, amount, adminFee, traceNumber.ToString(),
                "", isSuccess,
                sendTime, "",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "localhost", productCode,trxNumber, out xError);
        }

        public string AccountTopUpApproval(HTTPRestConstructor.HttpRestRequest clientData)
        {
            string productCode = commonSettings.getString("CashInProductCode");

            // input dari user diambil dari username dan password, baru bisa inquiry ke host
            if (!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }

            if ((!jsonConv.ContainsKey("fiPhone")) || (!jsonConv.ContainsKey("fiCustomerPhone")) ||
                //                (!jsonConv.ContainsKey("fiTraceNumber")) || (!jsonConv.ContainsKey("fiReferenceNumber")) ||
                (!jsonConv.ContainsKey("fiRequestDateTime")) || (!jsonConv.ContainsKey("fiPassword")) ||
				(!jsonConv.ContainsKey("fiAmount")) || (!jsonConv.ContainsKey("fiApplicationId"))
                )
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields no found", "");
            }
			string appID = ((string)jsonConv["fiApplicationId"]).Trim();
            string userPhone = ((string)jsonConv["fiPhone"]).Trim();
            string customerPhone = ((string)jsonConv["fiCustomerPhone"]).Trim();
            string passw = ((string)jsonConv["fiPassword"]).Trim();
            string requestTime = ((string)jsonConv["fiRequestDateTime"]).Trim();   //yyyy-MM-dd HH:mm:ss

            int amount = 0;
            try
            {
                amount = ((int)jsonConv["fiAmount"]);
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Bad amount", "");
            }

            ReformatPhoneNumber(ref userPhone);
            ReformatPhoneNumber(ref customerPhone);

            if (userPhone == customerPhone)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Phone number can not be equal with customer phone", "");
            }

            if (!localDB.isAccountExist(customerPhone, out xError))
            {
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                }
                else
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Customer phone not registered", "");
                }
            }

            if (!localDB.isUserPasswordEqual(userPhone, passw, out xError))
            {
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                }
                else
                {
                    // password error
                    return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Wrong password", "");
                }
            }
            // password ok

            // OK disini lakukan verifikasi dari table request cashin
            if (!localDB.isCashTrxRequestExists(requestTime, customerPhone, userPhone, amount, "",
                PPOBDatabase.PPOBdbLibs.CashTrxType.CashIn))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(401, "492", "No request from customer", "");
            }

            DateTime sendTime = DateTime.Now;

            // ambil info provider dari productCode
            Exception exrr = null;
            PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
            try
            {
                providerProduct = localDB.getProviderProductInfo(commonSettings.getString("CashInProductCode"), out exrr);
                if (exrr != null)
                {
                    //writeLogCashIn(providerProduct, userPhone, customerPhone, amount, 0, false,
                    //    sendTime.ToString("yyyy-MM-dd HH:mm:ss"), productCode);
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider product data not found", "");
                }
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting provider product data", "");
            }

            //int sellerFee = 0;
            int adminFee = 0;
            if (!localDB.getAdminFeeAndCustomerFee(commonSettings.getString("CashInProductCode"), 
				1,appID,(decimal) amount, ref adminFee, out xError))
				//providerProduct.ProviderCode,(decimal) amount, ref adminFee, out xError))
            {
                //writeLogCashIn(providerProduct, userPhone, customerPhone, amount, adminFee, false,
                //    sendTime.ToString("yyyy-MM-dd HH:mm:ss"), productCode);
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting admin fee", "");
            }

            string trxNumber = localDB.getCashInTrxNumber(out xError);

            long TransactionRef_id = localDB.getCashInReffIdSequence(out xError);
			string qvaInvoiceNumber = "";
			bool qvaReversalRequired = false;

            // lakukan transfer langsung dari agen ke customer lalu ambil dari customer admin fee nya
            // berdasarkan informasi dari database
            using (TransferReguler.CollectTransfer TransferReg =
                new TransferReguler.CollectTransfer(commonSettings))
            {
                int totalTransfer = amount + adminFee;
                string errCode = ""; string errMessage = "";
                try
                {
                    LogWriter.show(this, "Tranfer CashIn from user " + userPhone + " to " + customerPhone);
                    if (!TransferReg.TransferTopUpToCustomer(userPhone, customerPhone, 
						providerProduct.TransactionCodeSufix, totalTransfer,
                        TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.CashIn, 
						"TopUp customer", ref qvaInvoiceNumber, ref qvaReversalRequired, 
                        ref errCode, ref errMessage))
                    {
						if(qvaReversalRequired)
							TransferReg.Reversal(TransactionRef_id,qvaInvoiceNumber, ref errCode, ref errMessage);
                        LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : [" + errCode.ToString() + "]" + errMessage);
						return HTTPRestDataConstruct.constructHTTPRestResponse(400, errCode, "Failed to TopUp: " + errMessage, "");
                    }
                }
                catch (Exception ex)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : " + ex.getCompleteErrMsg());
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to TopUp", "");
                }
            }
            // DISINI harusnya user di lock sampe transfer admin Fee selesai

            // == TRANSFER AMBIL admin FEE dari customer
            using (TransferReguler.CollectTransfer TransferReg =
                new TransferReguler.CollectTransfer(commonSettings))
            {
                int totalTransfer = adminFee;
                string errCode=""; string errMessage="";
                try
                {
                    LogWriter.show(this, "Tranfer adminFee from user " + customerPhone + " to escrow");
					if (!TransferReg.TransferGetAdminFeeFromCustomer(customerPhone,
						providerProduct.TransactionCodeSufix, totalTransfer,
                        TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.CashIn,
						"TopUp admin fee transfer", ref qvaInvoiceNumber, ref qvaReversalRequired, 
						ref errCode, ref errMessage))
					{
						// TODO : Transfer harus berhasil, ATAU bispronya di ubah, harus ke penampung dulu
						// FIXME : HARUSNYA gak usah di reversal, tapi transfer ini harus berhasil
						if(qvaReversalRequired)
							TransferReg.Reversal(TransactionRef_id,qvaInvoiceNumber, ref errCode, ref errMessage);
						LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer admin Fee "+totalTransfer+" failed from " +
                            customerPhone+ " to ESCROW : [" + errCode.ToString() + "]" + errMessage);
                        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to TopUp", "");
                    }
                }
                catch (Exception ex)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer admin Fee " + totalTransfer + " failed from " +
                        customerPhone + " to ESCROW : " + ex.getCompleteErrMsg());
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to TopUp", "");
                }
            }

            writeLogCashIn(TransactionRef_id,providerProduct, userPhone, customerPhone, amount, adminFee, true, 
                sendTime.ToString("yyyy-MM-dd HH:mm:ss"),productCode,trxNumber);

            //    // set database udah disetujui dina log cashinout
            //    localDB. 
            if (!localDB.approveCashTrxRequest(requestTime, customerPhone, userPhone, amount, adminFee,
                "", PPOBDatabase.PPOBdbLibs.CashTrxType.CashIn,true, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
            {
                //return HTTPRestDataConstruct.constructHTTPRestResponse(401, "492", 
                //    "DB error but TopUp Approved, no notification", "");
            }

            string userId = commonSettings.getString("UserIdHeader") + userPhone;
            Hashtable userInfo = localDB.getLoginInfoByUserPhone(userPhone, out xError);


            // siapkan json untuk notifikasi ke customer
            //fiNotificationDateTime, fiAgenPhone, fiAgenName, fiAmount, , fiAdminFee, fiCode, fiTransferType
            jsonConv.Clear();
            jsonConv.Add("fiAgentPhone", userPhone);
            if (userInfo == null)
            {
                jsonConv.Add("fiAgentName", " ");
            }
            else
            {
                jsonConv.Add("fiAgentName", (string)userInfo["firstName"] + " " + (string)userInfo["lastName"]);
            }
            jsonConv.Add("fiAmount", amount);
            jsonConv.Add("fiAdminFee", adminFee);
            jsonConv.Add("fiNotificationDateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            jsonConv.Add("fiCode", "00");   // berhasil
            jsonConv.Add("fiTransferType", "01"); //cashin
            jsonConv.Add("fiTrxNumber", trxNumber);
            string subJson = jsonConv.JSONConstruct();

            // Insert di tabel notifikasi saja
            localDB.insertNotificationQueue(userPhone, customerPhone, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                "04", subJson, out xError);

            jsonConv.Clear();
            jsonConv.Add("fiReplyCode", "00");
            jsonConv.Add("fiAdminFee", adminFee);
            jsonConv.Add("fiAmount", amount);
            jsonConv.Add("fiCustomerPhone", customerPhone);
            jsonConv.Add("fiTrxNumber", trxNumber);
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

        }


        private int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }

        private char intToChar(int a)
        {
            if (a < 10) return (char)(a + 0x30);
            return (char)(a + 0x40);
        }

        private string generateToken()
        {
            const int jumlahRandom = 6;
            Random random = new Random();
            random.Next(0, 9);
            string hasil = "";
            int rd = 0;
            for (int i = 0; i < jumlahRandom; i++)
            {
                // Semua DEC
                rd = random.Next(0, 9);
                hasil += (char)(rd + 0x30);

                // Semua HEX
                //rd = random.Next(0, 15);
                //if (rd < 10) hasil += (char)(rd + 0x30);
                //else hasil += (char)(rd + 0x37);

                // Semua numerik dan char
                //rd = random.Next(0, 35);
                //if (rd < 10) hasil += (char)(rd + 0x30);
                //else hasil += (char)(rd + 0x37);
            }
            return hasil;
        }

        public string AccountRequestCashOut(HTTPRestConstructor.HttpRestRequest clientData)
        {
            if (!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }

            if ((!jsonConv.ContainsKey("fiPhone")) || (!jsonConv.ContainsKey("fiAgentPhone")) ||
                //                (!jsonConv.ContainsKey("fiTraceNumber")) || (!jsonConv.ContainsKey("fiReferenceNumber")) ||
                (!jsonConv.ContainsKey("fiDateTime")) || (!jsonConv.ContainsKey("fiPassword")) ||
                (!jsonConv.ContainsKey("fiAmount"))
                )
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields no found", "");
            }

            string userPhone;
            string agentPhone;
            string passw;
            string trxTime;
			string appID = "";

            try
            {
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
                userPhone = ((string)jsonConv["fiPhone"]).Trim();
                agentPhone = ((string)jsonConv["fiAgentPhone"]).Trim();
                passw = ((string)jsonConv["fiPassword"]).Trim();
                trxTime = ((string)jsonConv["fiDateTime"]).Trim();   //yyyy-MM-dd HH:mm:ss
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Bad data", "");
            }

            int amount = 0;
            try
            {
                amount = ((int)jsonConv["fiAmount"]);
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Bad amount", "");
            }

            ReformatPhoneNumber(ref userPhone);
            ReformatPhoneNumber(ref agentPhone);

            if (userPhone == agentPhone)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Phone number can not be equal with agent phone", "");
            }

            if (!localDB.isAccountExist(agentPhone, out xError))
            {
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                }
                else
                {
                    // password error
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Agent phone not registered", "");
                }
            }

            if (!localDB.isUserPasswordEqual(userPhone, passw, out xError))
            {
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                }
                else
                {
                    // password error
                    return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Wrong password", "");
                }
            }
            // password ok

			//string userId = commonSettings.getString("UserIdHeader") + userPhone;
            Hashtable userInfo = localDB.getLoginInfoByUserPhone(userPhone, out xError);

            if (userInfo == null)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(401, "492", "Failed to get customer name", "");
            }

            // ambil info provider dari productCode
            Exception exrr = null;
            PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
            try
            {
                providerProduct = localDB.getProviderProductInfo(
                    commonSettings.getString("CashOutProductCode"), out exrr);
                if (exrr != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider product data not found", "");
                }
                if (providerProduct.ProviderCode == "")
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider product data not found", "");
                }
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting provider product data", "");
            }

            int adminFee = 0;
            //int sellerFee = 0;
			//if (!localDB.getAdminFeeAndCustomerFee(commonSettings.getString("CashOutProductCode"), providerProduct.ProviderCode,(decimal)amount, ref adminFee, out xError))
			if (!localDB.getAdminFeeAndCustomerFee(commonSettings.getString("CashOutProductCode"),1, appID,(decimal)amount, ref adminFee, out xError))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting admin fee", "");
            }

            string token = generateToken();

            // insert ke table sementara untuk verifikasi agen
            if (!localDB.insertCashTrxRequest(trxTime, userPhone, agentPhone, amount, adminFee, token,
                PPOBDatabase.PPOBdbLibs.CashTrxType.CashOut, false, ""))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on db write request", "");
            }

            // siapkan json untuk notifikasi ke agen
            jsonConv.Clear();
            jsonConv.Add("fiCustomerPhone", userPhone);
            jsonConv.Add("fiCustomerName", (string)userInfo["firstName"] + " " + (string)userInfo["lastName"]);
            jsonConv.Add("fiAmount", amount);
            jsonConv.Add("fiAdminFee", adminFee);
            jsonConv.Add("fiRequestDateTime", trxTime);
            jsonConv.Add("fiNotificationDateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            string subJson = jsonConv.JSONConstruct();

            // Insert di tabel notifikasi saja
            localDB.insertNotificationQueue(userPhone, agentPhone, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                "05", subJson, out xError);

            jsonConv.Clear();
            jsonConv.Add("fiReplyCode", "00");
            jsonConv.Add("fiAdminFee", adminFee);
            jsonConv.Add("fiAmount", amount);
            jsonConv.Add("fiToken", token);
            jsonConv.Add("fiAgentPhone", agentPhone);
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

        }

        private bool writeLogCashOut(long TransactionRefId, PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct,
            string userPhone, string customerPhone, int amount, int adminFee,
            bool isSuccess, string sendTime, string productCode, string trxNumber
            )
        {
            //int traceNumber = StaticCommonLibrary.CommonLibrary.getNextProductTraceNumber();
            int traceNumber = localDB.getNextProductTraceNumber();
            xError = null;
            // masukkan log cash Out
            return localDB.insertCashOutLog(TransactionRefId, providerProduct.ProviderCode, providerProduct.ProviderProductCode,
                providerProduct.CogsPriceId.ToString(), userPhone, customerPhone, amount, adminFee, traceNumber.ToString(),
                "", isSuccess,
                sendTime, "",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "localhost", productCode, trxNumber, out xError);
        }

        public string AccountCashOutApproval(HTTPRestConstructor.HttpRestRequest clientData)
        {
            string productCode = commonSettings.getString("CashOutProductCode");

            // input dari user diambil dari username dan password, baru bisa inquiry ke host
            if (!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }

            if ((!jsonConv.ContainsKey("fiPhone")) || 
                (!jsonConv.ContainsKey("fiCustomerPhone")) ||
                (!jsonConv.ContainsKey("fiToken")) ||
                (!jsonConv.ContainsKey("fiRequestDateTime")) || 
                (!jsonConv.ContainsKey("fiPassword")) ||
				(!jsonConv.ContainsKey("fiApplicationId")) ||
                (!jsonConv.ContainsKey("fiAmount"))
                )
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields no found", "");
            }
			string appID = ((string)jsonConv["fiApplicationId"]).Trim();
            string userPhone = ((string)jsonConv["fiPhone"]).Trim();
            string customerPhone = ((string)jsonConv["fiCustomerPhone"]).Trim();
            string passw = ((string)jsonConv["fiPassword"]).Trim();
            string requestTime = ((string)jsonConv["fiRequestDateTime"]).Trim();   //yyyy-MM-dd HH:mm:ss
            string token = ((string)jsonConv["fiToken"]).Trim();

            int amount = 0;
            try
            {
                amount = ((int)jsonConv["fiAmount"]);
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Bad amount", "");
            }

            ReformatPhoneNumber(ref userPhone);
            ReformatPhoneNumber(ref customerPhone);

            if (userPhone == customerPhone)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Phone number can not be equal with customer phone", "");
            }

            if (!localDB.isAccountExist(customerPhone, out xError))
            {
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                }
                else
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Customer phone not registered", "");
                }
            }

            if (!localDB.isUserPasswordEqual(userPhone, passw, out xError))
            {
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                }
                else
                {
                    // password error
                    return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Wrong password", "");
                }
            }
            // password ok

            // OK disini lakukan verifikasi dari table request cashin
            if (!localDB.isCashTrxRequestExists(requestTime, customerPhone, userPhone, amount, token,
                PPOBDatabase.PPOBdbLibs.CashTrxType.CashOut))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(401, "493", "Request not found or invalid token", "");
            }

            DateTime sendTime = DateTime.Now;

            // ambil info provider dari productCode
            Exception exrr = null;
            PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
            try
            {
                providerProduct = localDB.getProviderProductInfo(commonSettings.getString("CashOutProductCode"), out exrr);
                if (exrr != null)
                {
                    //writeLogCashOut(providerProduct, userPhone, customerPhone, amount, 0, false,
                    //    sendTime.ToString("yyyy-MM-dd HH:mm:ss"), productCode);
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider product data not found", "");
                }
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting provider product data", "");
            }

            //int sellerFee = 0;
            int adminFee = 0;
            if (!localDB.getAdminFeeAndCustomerFee(commonSettings.getString("CashOutProductCode"), 
				//providerProduct.ProviderCode,(decimal)amount, ref adminFee, out xError))
				1,appID,(decimal)amount, ref adminFee, out xError))
            {
                //writeLogCashOut(providerProduct, userPhone, customerPhone, amount, adminFee, false,
                //    sendTime.ToString("yyyy-MM-dd HH:mm:ss"), productCode);
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting admin fee", "");
            }

            // cek balance si customer >= amount + adminfee
            string trxNumber = localDB.getCashOutTrxNumber(out xError);
            int balance = 0;
            double dBalance = 0;
			using (SandraLibs sandra = new SandraLibs(commonSettings.getInt("QvaTimeOut"),
				(commonSettings.getString("SandraUseTcpMethod").Trim().ToLower() == "true"),
				commonSettings.getString("SandraHmsUserAuth"),
				commonSettings.getString("SandraHmsSecretKey"),
				commonSettings.getString("SandraAuthId")))
            {
                if (!sandra.Inquiry(
					commonSettings.getString("SandraHost"), commonSettings.getInt("SandraPort"), 
					(commonSettings.getString("SandraIsHttps").ToLower() == "true"),
					commonSettings.getString("UserIdHeader") + customerPhone, ref dBalance))
                {
                    //writeLogCashOut(providerProduct, userPhone, customerPhone, amount, adminFee, false,
                    //    sendTime.ToString("yyyy-MM-dd HH:mm:ss"), productCode,trxNumber);
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting user balance", "");
                }
            }
            balance = Convert.ToInt32(Math.Floor(dBalance));
            if (balance < (amount + adminFee))
            {
                //writeLogCashOut(providerProduct, userPhone, customerPhone, amount, adminFee, false,
                //    sendTime.ToString("yyyy-MM-dd HH:mm:ss"), productCode,trxNumber);
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Insufficient Customer balance ", "");
            }

            long TransactionRef_id = localDB.getCashOutReffIdSequence(out xError);
			string qvaInvoiceNumber = "";
			bool qvaReversalRequired = false;

            // lakukan transfer langsung dari customer ke agen lalu ambil dari customer admin fee nya
            // berdasarkan informasi dari database
            using (TransferReguler.CollectTransfer TransferReg =
                new TransferReguler.CollectTransfer(commonSettings))
            {
                //int totalTransfer = amount + adminFee;
                int totalTransfer = amount;
                string errCode = ""; string errMessage = "";
                try
                {
                    LogWriter.show(this, "Tranfer CashOut from user " + userPhone + " to " + customerPhone);
                    if (!TransferReg.TransferCashOutFromCustomer(userPhone, customerPhone, 
						providerProduct.TransactionCodeSufix, totalTransfer, 
                        TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.CashOut,
						"CashOut transfer", ref qvaInvoiceNumber, ref qvaReversalRequired, 
						ref errCode, ref errMessage))
					{
						if(qvaReversalRequired)
							TransferReg.Reversal(TransactionRef_id,qvaInvoiceNumber, ref errCode, ref errMessage);
						LogWriter.write(this, LogWriter.logCodeEnum.ERROR, 
                            "Transfer failed : [" + errCode.ToString() + "]" + errMessage);
						if(errMessage!="")
							return HTTPRestDataConstruct.constructHTTPRestResponse(400, "z"+errCode, 
								errMessage, "");
						else
							return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
								"Failed to Cash Out", "");

                    }
                }
                catch (Exception ex)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : " + ex.getCompleteErrMsg());
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
                        "Failed to Cash out", "");
                }
            }
            // DISINI harusnya user di lock sampe transfer admin Fee selesai

            // == TRANSFER AMBIL admin FEE dari customer
            using (TransferReguler.CollectTransfer TransferReg =
                new TransferReguler.CollectTransfer(commonSettings))
            {
                int totalTransfer = adminFee;
                string errCode = ""; string errMessage = "";
                try
                {
                    LogWriter.show(this, "Tranfer adminFee from user " + customerPhone + " to escrow");
					if (!TransferReg.TransferGetAdminFeeFromCustomer(customerPhone,
						providerProduct.TransactionCodeSufix, totalTransfer,
                        TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.CashOut,
						"CashOut admin fee transfer", ref qvaInvoiceNumber, ref qvaReversalRequired, 
						ref errCode, ref errMessage))
					{
						// TODO : kuduna diubah bispro na 
						// FIXME : kuduna teu meunang reversal, paksa transfer, jeung sakuduna lewat penampung, jd ubah bispro
						if(qvaReversalRequired)
							TransferReg.Reversal(TransactionRef_id,qvaInvoiceNumber, ref errCode, ref errMessage);

                        LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer admin Fee " + totalTransfer + " failed from " +
                            customerPhone + " to ESCROW : [" + errCode.ToString() + "]" + errMessage);
                        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to TopUp", "");
                    }
                }
                catch (Exception ex)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer admin Fee " + totalTransfer + " failed from " +
                        customerPhone + " to ESCROW : " + ex.getCompleteErrMsg());
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to TopUp", "");
                }
            }

            writeLogCashOut(TransactionRef_id,providerProduct, userPhone, customerPhone, amount, adminFee, true,
                sendTime.ToString("yyyy-MM-dd HH:mm:ss"), productCode, trxNumber);

            //    // set database udah disetujui dina log cashinout
            if (!localDB.approveCashTrxRequest(requestTime, customerPhone, userPhone, amount, adminFee,
                "", PPOBDatabase.PPOBdbLibs.CashTrxType.CashOut, true, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
            {
                //return HTTPRestDataConstruct.constructHTTPRestResponse(401, "492", 
                //    "DB error but Cash Out Approved, no notification", "");
            }

			//string userId = commonSettings.getString("UserIdHeader") + userPhone;
            Hashtable userInfo = localDB.getLoginInfoByUserPhone(userPhone, out xError);

            // siapkan json untuk notifikasi ke customer
            //fiNotificationDateTime, fiAgenPhone, fiAgenName, fiAmount, , fiAdminFee, fiCode, fTranferType
            jsonConv.Clear();
            jsonConv.Add("fiAgentPhone", userPhone);
            if (userInfo == null)
            {
                jsonConv.Add("fiAgentName", " ");
            }
            else
            {
                jsonConv.Add("fiAgentName", (string)userInfo["firstName"] + " " + (string)userInfo["lastName"]);
            }
            jsonConv.Add("fiAmount", amount);
            jsonConv.Add("fiAdminFee", adminFee);
            jsonConv.Add("fiNotificationDateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            jsonConv.Add("fiCode", "00");   // berhasil
            jsonConv.Add("fiTransferType", "02"); //cashout
            jsonConv.Add("fiTrxNumber", trxNumber);
            string subJson = jsonConv.JSONConstruct();

            // Insert di tabel notifikasi saja
            localDB.insertNotificationQueue(userPhone, customerPhone, 
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "06", subJson, out xError);

            jsonConv.Clear();
            jsonConv.Add("fiReplyCode", "00");
            jsonConv.Add("fiAdminFee", adminFee);
            jsonConv.Add("fiAmount", amount);
            jsonConv.Add("fiCustomerPhone", customerPhone);
            jsonConv.Add("fiTrxNumber", trxNumber);
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

        }

        public string AccountSendInvoice(HTTPRestConstructor.HttpRestRequest clientData)
        {
            // input dari user diambil dari username dan password, baru bisa inquiry ke host
            if (!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }

            if ((!jsonConv.ContainsKey("fiPhone")) || (!jsonConv.ContainsKey("fiCustomerPhone")) ||
                (!jsonConv.ContainsKey("fiApplicationId")) || (!jsonConv.ContainsKey("fiInvoiceNumber")) ||
                (!jsonConv.ContainsKey("fiDescription")) || (!jsonConv.ContainsKey("fiFooterNote")) ||
                (!jsonConv.ContainsKey("fiInvoiceDateTime")) || (!jsonConv.ContainsKey("fiPassword")) ||
				(!jsonConv.ContainsKey("fiAmount")) || (!jsonConv.ContainsKey("fiProductCode")) ||
				(!jsonConv.ContainsKey("fiApplicationId"))
			)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields no found", "");
            }

            string userPhone;
            string customerPhone;
            string passw;
            string invoiceDateTime;
            string applicationId;
            string invoiceNumber;
            string invoiceDescription;
            string footerNote;
            string productCode;
			string appID = "";
            try
            {
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
                userPhone = ((string)jsonConv["fiPhone"]).Trim();
                customerPhone = ((string)jsonConv["fiCustomerPhone"]).Trim();
                passw = ((string)jsonConv["fiPassword"]).Trim();
                invoiceDateTime = ((string)jsonConv["fiInvoiceDateTime"]).Trim();   //yyyy-MM-dd HH:mm:ss
                applicationId = ((string)jsonConv["fiApplicationId"]).Trim();
                invoiceNumber = ((string)jsonConv["fiInvoiceNumber"]).Trim();
                invoiceDescription = ((string)jsonConv["fiDescription"]).Trim();
                footerNote = ((string)jsonConv["fiFooterNote"]).Trim();
                productCode = ((string)jsonConv["fiProductCode"]).Trim();
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid dats of one or more fields", "");
            }

            if (invoiceNumber.Length > 30)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invoice number too long (30 char MAX)", "");
            }
            if ((invoiceDescription.Length > 500) && (footerNote.Length > 500))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "409", "Description or Footer note too long", "");
            }

            int amount = 0;
            try
            {
                amount = ((int)jsonConv["fiAmount"]);
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Bad amount", "");
            }

            ReformatPhoneNumber(ref userPhone);
            ReformatPhoneNumber(ref customerPhone);

            if (userPhone == customerPhone)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Phone number can not be equal with customer phone", "");
            }

            if (!localDB.isAccountExist(customerPhone, out xError))
            {
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                }
                else
                {
                    // password error
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Customer phone not registered", "");
                }
            }

            if (!localDB.isUserPasswordEqual(userPhone, passw, out xError))
            {
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                }
                else
                {
                    // password error
                    return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Wrong password", "");
                }
            }
            // password ok

			//string userId = commonSettings.getString("UserIdHeader") + userPhone;
            Hashtable userInfo = localDB.getLoginInfoByUserPhone(userPhone, out xError);

            if (userInfo == null)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(401, "492", "Failed to get merchant name", "");
            }

            // ambil info provider dari productCode
            Exception exrr = null;
            PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
            try
            {
                providerProduct = localDB.getProviderProductInfo(productCode, out exrr);
                if (exrr != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider invoice product not found", "");
                }
                if (providerProduct.ProviderCode == "")
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider invoice product not found", "");
                }
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting provider invoice product ", "");
            }

			if (!providerProduct.QvaAccountCredit.StartsWith (commonSettings.getString ("UserIdHeader"))) {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "493", "No Qva account destination for this invoice ", "");
			}

            int adminFee = 0;
            //int sellerFee = 0;
			//if (!localDB.getAdminFeeAndCustomerFee(productCode, providerProduct.ProviderCode,(decimal) amount, ref adminFee, out xError))
			if (!localDB.getAdminFeeAndCustomerFee(productCode, 1, appID,(decimal) amount, ref adminFee, out xError))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting admin fee", "");
            }

            if (amount <= adminFee)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Amount less than admin fee is not allowed", "");
            }

            // insert ke table sementara untuk verifikasi agen
            if (!localDB.insertInvoiceSent(applicationId, customerPhone, userPhone, productCode,
                amount, invoiceNumber, invoiceDateTime, invoiceDescription, footerNote, false, ""))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on write invoice request", "");
            }

            // siapkan json untuk notifikasi ke agen
            jsonConv.Clear();
            jsonConv.Add("fiMerchantPhone", userPhone);
            jsonConv.Add("fiMerchantName", (string)userInfo["firstName"] + " " + (string)userInfo["lastName"]);
            jsonConv.Add("fiAmount", amount);
            jsonConv.Add("fiProductCode", productCode);
            jsonConv.Add("fiProductName", providerProduct.ProductName);
            jsonConv.Add("fiInvoiceNumber", invoiceNumber);
            jsonConv.Add("fiInvoiceDateTime", invoiceDateTime);
            jsonConv.Add("fiDescription", invoiceDescription);
            jsonConv.Add("fiNotificationDateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            string subJson = jsonConv.JSONConstruct();

            // Insert di tabel notifikasi saja
            localDB.insertNotificationQueue(userPhone, customerPhone,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "08", subJson, out xError);

            jsonConv.Clear();
            jsonConv.Add("fiResponseCode", "00");
            jsonConv.Add("fiAdminFee", adminFee);
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
        }


        public string AccountInvoicePayment(HTTPRestConstructor.HttpRestRequest clientData)
        {

            // input dari user diambil dari username dan password, baru bisa inquiry ke host
            if (!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }

            if ((!jsonConv.ContainsKey("fiPhone")) || (!jsonConv.ContainsKey("fiMerchantPhone")) ||
                (!jsonConv.ContainsKey("fiInvoiceDateTime")) || (!jsonConv.ContainsKey("fiInvoiceNumber")) ||
                (!jsonConv.ContainsKey("fiPassword")) || (!jsonConv.ContainsKey("fiPayDateTime")) ||
				(!jsonConv.ContainsKey("fiAmount")) || (!jsonConv.ContainsKey("fiProductCode")) ||
				(!jsonConv.ContainsKey("fiApplicationId"))
                )
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields no found", "");
            }

            string userPhone;
            string merchantPhone;
            string passw;
            string invoiceTime;
            string invoiceNumber;
            string payTime;
            string productCode;
			string appID = "";
            try
            {
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
                userPhone = ((string)jsonConv["fiPhone"]).Trim();
                merchantPhone = ((string)jsonConv["fiMerchantPhone"]).Trim();
                passw = ((string)jsonConv["fiPassword"]).Trim();
                invoiceTime = ((string)jsonConv["fiInvoiceDateTime"]).Trim();   //yyyy-MM-dd HH:mm:ss
                invoiceNumber = ((string)jsonConv["fiInvoiceNumber"]).Trim();
                payTime = ((string)jsonConv["fiPayDateTime"]).Trim();
                productCode = ((string)jsonConv["fiProductCode"]).Trim();
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid dats of one or more fields", "");
            }

            int amount = 0;
            try
            {
                amount = ((int)jsonConv["fiAmount"]);
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Bad amount", "");
            }

            ReformatPhoneNumber(ref userPhone);
            ReformatPhoneNumber(ref merchantPhone);

            if (userPhone == merchantPhone)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Phone number can not be equal with merchant phone", "");
            }

            if (!localDB.isAccountExist(merchantPhone, out xError))
            {
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                }
                else
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Customer phone not registered", "");
                }
            }

            if (!localDB.isUserPasswordEqual(userPhone, passw, out xError))
            {
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                }
                else
                {
                    // password error
                    return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Wrong password", "");
                }
            }
            // password ok

            // OK disini lakukan verifikasi dari table request cashin
            int dbAmount = 0;
            string invFooterNote = "";
            string invDescription = "";
            if (!localDB.isInvoiceExist(userPhone, merchantPhone, productCode, invoiceNumber,
                invoiceTime, ref dbAmount, ref invDescription, ref invFooterNote))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(401, "492", "No invoice found from merchant", "");
            }

            if ((amount != dbAmount) || (amount == 0))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(401, "494", "Incorrect amount for this invoice", "");
            }

            DateTime sendTime = DateTime.Now;

            // ambil info provider dari productCode
            Exception exrr = null;
            PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
            try
            {
                providerProduct = localDB.getProviderProductInfo(productCode, out exrr);
                if (exrr != null)
                {
                    //writeLog(providerProduct, userPhone, customerPhone, amount, 0, false,
                    //    sendTime.ToString("yyyy-MM-dd HH:mm:ss"), productCode);
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider product data not found", "");
                }
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting provider product data", "");
            }

			if (!providerProduct.QvaAccountCredit.StartsWith (commonSettings.getString ("UserIdHeader"))) {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "493", "No Qva account destination for this invoice ", "");
			}

            //int distributorFee = 0;
            int adminFee = 0;
			//if (!localDB.getAdminFeeAndCustomerFee(productCode, providerProduct.ProviderCode,
			if (!localDB.getAdminFeeAndCustomerFee(productCode, 1, appID,
                (decimal)amount,ref adminFee, out xError))
            {
                //writeLogCashIn(providerProduct, userPhone, customerPhone, amount, adminFee, false,
                //    sendTime.ToString("yyyy-MM-dd HH:mm:ss"), productCode);
                adminFee = 0;
                //distributorFee = 0;
                //return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting admin fee", "");
            }

            if (amount < adminFee)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Incorrect amount for this invoice", "");
            }

            long TransactionRef_id = localDB.getInvoiceReffIdSequence(out xError);
			string qvaInvoiceNumber = "";
			bool qvaReversalRequired = false;

            // lakukan transfer langsung dari customer ke merchant lalu ambil dari merchant admin fee nya
            // berdasarkan informasi dari database
            using (TransferReguler.CollectTransfer TransferReg =
                new TransferReguler.CollectTransfer(commonSettings))
            {
                int totalTransfer = amount;
                string errCode = ""; string errMessage = "";
                try
                {
					//LogWriter.show(this, "Tranfer Invoice from user " + userPhone + " to " + merchantPhone);
					LogWriter.show(this, "Tranfer Invoice from user " + userPhone + " to " + merchantPhone);
					//if (!TransferReg.TransferInvoiceFromCustomer(userPhone, merchantPhone, 
					if (!TransferReg.TransferInvoiceFromCustomerToPenampung(userPhone, 
						providerProduct.TransactionCodeSufix, totalTransfer,
                        TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.Invoicing,
						"Invoice total transfer", ref qvaInvoiceNumber, ref qvaReversalRequired, 
						ref errCode, ref errMessage))
					{
						if(qvaReversalRequired)
							TransferReg.Reversal(TransactionRef_id,qvaInvoiceNumber, ref errCode, ref errMessage);
						LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : [" + errCode.ToString() + "]" + errMessage);
                        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to pay invoice, " + errMessage, "");
                    }
                }
                catch (Exception ex)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : " + ex.getCompleteErrMsg());
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to pay invoice", "");
                }
            }
            // DISINI harusnya user di lock sampe transfer admin Fee selesai

//            // == TRANSFER AMBIL admin FEE dari merchant
//            if (adminFee >= 0)
//            {
//                using (TransferReguler.CollectTransfer TransferReg =
//                    new TransferReguler.CollectTransfer(commonSettings))
//                {
//                    int totalTransfer = adminFee;
//                    string errCode = ""; string errMessage = "";
//                    try
//                    {
//                        LogWriter.show(this, "Tranfer adminFee from merchant " + merchantPhone + " to escrow");
//                        if (!TransferReg.TransferGetAdminFeeFromMerchant(merchantPhone, 
//							providerProduct.TransactionCodeSufix, totalTransfer,
//                            TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.Invoicing,
//                            "Invoice fee transfer",
//                            ref errCode, ref errMessage))
//                        {
//                            LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer admin Fee " + totalTransfer + " failed from " +
//                                merchantPhone + " to ESCROW : [" + errCode.ToString() + "]" + errMessage);
//                            //                        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to ", "");
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer admin Fee " + totalTransfer + " failed from " +
//                            merchantPhone + " to ESCROW : " + ex.getCompleteErrMsg());
//                        //                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to get adminfee from merchant", "");
//                    }
//                }
//            }

            // ============================================================================
            // DISINI DIPASANG PENCATATAN LOG INVOICE UNTUK PEMBAGIAN FEE
            // ============================================================================
            string trxNumber = localDB.getInvoiceTrxNumber(out xError);

			localDB.insertInvoicingLog(TransactionRef_id, productCode, providerProduct.ProviderProductCode,
                merchantPhone, userPhone, invoiceNumber, amount - adminFee, invoiceTime, adminFee,
                providerProduct.ProviderCode, providerProduct.CogsPriceId,
                invDescription, invFooterNote, trxNumber, out xError);

            //    // set database udah disetujui dina log cashinout
            //    localDB. 
            if (!localDB.payInvoiceUpdate(userPhone, merchantPhone, productCode, invoiceNumber,
                 invoiceTime,amount, true, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
            {
                //return HTTPRestDataConstruct.constructHTTPRestResponse(401, "492", 
                //    "DB error but TopUp Approved, no notification", "");
            }

			//string userId = commonSettings.getString("UserIdHeader") + userPhone;
            Hashtable userInfo = localDB.getLoginInfoByUserPhone(userPhone, out xError);
            
            // siapkan json untuk notifikasi ke merchant
            jsonConv.Clear();
            jsonConv.Add("fiCustomerPhone", userPhone);
            if (userInfo == null)
            {
                jsonConv.Add("fiCustomerName", " ");
            }
            else
            {
                jsonConv.Add("fiCustomerName", (string)userInfo["firstName"] + " " + (string)userInfo["lastName"]);
            }
            jsonConv.Add("fiAmount", amount);
            jsonConv.Add("fiAdminFee", adminFee);
            jsonConv.Add("fiNotificationDateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            jsonConv.Add("fiInvoiceDateTime", invoiceTime); // waktu invoice dari merchant
            jsonConv.Add("fiInvoiceNumber", invoiceNumber);
            jsonConv.Add("fiPayDateTime", payTime);
            jsonConv.Add("fiProductCode", productCode);
            jsonConv.Add("fiProductName", providerProduct.ProductName);
            jsonConv.Add("fiTrxNumber", trxNumber);
            string subJson = jsonConv.JSONConstruct();

            // Insert di tabel notifikasi saja
            localDB.insertNotificationQueue(userPhone, merchantPhone, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                "09", subJson, out xError);

            jsonConv.Clear();
            jsonConv.Add("fiResponseCode", "00");
            jsonConv.Add("fiDescription", invDescription);
            jsonConv.Add("fiFooterNote", invFooterNote);
            jsonConv.Add("fiTrxNumber", trxNumber);
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

        }

		public string AccountInquiryPenampung(HTTPRestConstructor.HttpRestRequest clientData)
		{
			// input dari user diambil dari username dan password, baru bisa inquiry ke host
			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}
			if (!jsonConv.ContainsKey("fiLoginId"))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No fiLoginId field found", "");
			}
			if (!jsonConv.ContainsKey("fiType"))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No fiType field found", "");
			}
			string fType = ((string)jsonConv["fiType"]).Trim();
			string userLogin = ((string)jsonConv["fiLoginId"]).Trim();

			if((!fType.Equals("CREDIT")) && (!fType.Equals("DEBIT"))){
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid fiType", "");
			}

			//  Yang harus disiapkan
			double penampungBalance = 0;

			// ambil account penampung
			string accountPenampung; 
			if(fType.Equals("CREDIT")) accountPenampung = commonSettings.getString("QVA_PENAMPUNG_KREDIT");
			else accountPenampung = commonSettings.getString("QVA_PENAMPUNG_DEBIT");

			// konek ke host sandra untuk inquiry
			//if (localIP) sandraHost = "127.0.0.1";
			using (SandraLibs sandra = new SandraLibs(commonSettings.getInt("QvaTimeOut"),
				(commonSettings.getString("SandraUseTcpMethod").Trim().ToLower() == "true"),
				commonSettings.getString("SandraHmsUserAuth"),
				commonSettings.getString("SandraHmsSecretKey"),
				commonSettings.getString("SandraAuthId")))
			{
				if (sandra.Inquiry(
					commonSettings.getString("SandraHost"), commonSettings.getInt("SandraPort"), 
					(commonSettings.getString("SandraIsHttps").ToLower() == "true"),accountPenampung, ref penampungBalance))
				{
					//Console.WriteLine("Sandra OK");
					jsonConv.Clear();
					jsonConv.Add("fiBalance", penampungBalance);
					// masukkan di database

					// kirim respon ke client
					return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
				}
				else
				{
					LogWriter.show(this, "Sandra not OK\r\n" +
						"Error code: " + sandra.LastError.ServerCode + "\r\n" +
						"Error message: " + sandra.LastError.ServerMessage);
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, sandra.LastError.ServerCode, sandra.LastError.ServerMessage, "");
				}
			}
		}

		public string AccountLastTransactionHistoryPenampung(HTTPRestConstructor.HttpRestRequest clientData)
		{
			if (clientData.Body.Length == 0)
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
			}
			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}

			string userLogin = "";
			string userPassword = "";
			int historyLimit = 1;
			try
			{
				userLogin = ((string)jsonConv["fiLoginId"]).Trim();
				userPassword = ((string)jsonConv["fiPassword"]).Trim();
				historyLimit = (int)jsonConv["fiLimit"];
			}
			catch
			{
				// field tidak ditemukan atau formatnya salah
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Bad fields found", "");
			}
			if (userLogin.Length <= 3)
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "409", "Bad login length", "");
			if (!jsonConv.ContainsKey("fiType"))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No fiType field found", "");
			}
			string fType = ((string)jsonConv["fiType"]).Trim();

			if((!fType.Equals("CREDIT")) && (!fType.Equals("DEBIT"))){
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid fiType", "");
			}

			//  Yang harus disiapkan, ambil account penampung
			// ambil account penampung
			string accountPenampung; 
			if(fType.Equals("CREDIT")) accountPenampung = commonSettings.getString("QVA_PENAMPUNG_KREDIT");
			else accountPenampung = commonSettings.getString("QVA_PENAMPUNG_DEBIT");

			// cek dengan database, apakah password sama? (Untuk login member web, bukan Phonenumber

			// password ok

			using (SandraLibs sandra = new SandraLibs(commonSettings.getInt("QvaTimeOut"),
				(commonSettings.getString("SandraUseTcpMethod").Trim().ToLower() == "true"),
				commonSettings.getString("SandraHmsUserAuth"),
				commonSettings.getString("SandraHmsSecretKey"),
				commonSettings.getString("SandraAuthId")))
			{
				JsonLibs.MyJsonArray historyLog = null;
				double penampungBalance = 0;
				if ((sandra.GetLastTransactionHistory(
					commonSettings.getString("SandraHost"),commonSettings.getInt("SandraPort"), 
					(commonSettings.getString("SandraIsHttps").ToLower() == "true"), 
					accountPenampung, historyLimit, ref historyLog))
					&& (sandra.Inquiry(commonSettings.getString("SandraHost"),
						commonSettings.getInt("SandraPort"), 
						(commonSettings.getString("SandraIsHttps").ToLower() == "true"), 
						accountPenampung, ref penampungBalance))
				)
				{
					//Console.WriteLine("Sandra OK");
					jsonConv.Clear();
					if (historyLog != null)
						jsonConv.Add("fiHistoryLogs", historyLog);
					else
						jsonConv.Add("fiHistoryLogs", null);

					jsonConv.Add("fiBalance", penampungBalance);

					// kirim respon ke client
					return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
				}
				else
				{
					LogWriter.show(this, "Sandra not OK\r\n" +
						"Error code: " + sandra.LastError.ServerCode + "\r\n" +
						"Error message: " + sandra.LastError.ServerMessage);
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, sandra.LastError.ServerCode, sandra.LastError.ServerMessage, "");
				}
			}
		}

    }
}
