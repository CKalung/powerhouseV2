
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPOBHttpRestData;
using LOG_Handler;
using StaticCommonLibrary;

namespace ShopsHandler
{
	public class ShopsTransactions: IDisposable {
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
		~ShopsTransactions()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			if (jsonConv != null)
				jsonConv.Dispose ();
			jsonConv = null;
			if (localDB != null)
				localDB.Dispose ();
			localDB = null;
		}

		/******************************************************************************/
		HTTPRestConstructor.HttpRestRequest clientData;
		PublicSettings.Settings commonSettings;
		JsonLibs.MyJsonLib jsonConv;
		HTTPRestConstructor HTTPRestDataConstruct;
		PPOBDatabase.PPOBdbLibs localDB;

		string cUserIDHeader="";

		public string securityToken = "";


		#region Kumpulan fungsi standar

		/*============================================================================*/
		/*   Kumpulan fungsi standar */
		/*============================================================================*/

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

		private bool checkMandatoryFields(JsonLibs.MyJsonLib sJson, string[] mandatoryFields)
		{
			foreach (string aField in mandatoryFields)
			{
				if (!sJson.ContainsKey(aField))
				{
					return false;
				}
			}
			return true;
		}

		private bool cek_SecurityToken(string userPhone, string token)
		{
			// cek detek sessionnya
			return CommonLibrary.isSessionExist (userPhone, token);
		}

		/*============================================================================*/
		/*============================================================================*/

		#endregion


		public ShopsTransactions(HTTPRestConstructor.HttpRestRequest ClientData,
			PublicSettings.Settings CommonSettings)
		{
			clientData = ClientData;
			commonSettings = CommonSettings;
			cUserIDHeader = commonSettings.getString("UserIdHeader");

			HTTPRestDataConstruct = new HTTPRestConstructor();
			jsonConv = new JsonLibs.MyJsonLib();
			localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
				commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
		}


//		public bool productTransaction()
//		{
//			// simpan di database order_request
//			//INSERT INTO order_request (product_code,owner_phone,distributor_phone,amount,order_time,description,host)
//			//VALUES ('PRD00055','081218877246','082218877123','150000',NOW(),'description','127.0.0.1')
//			// 
//			//traceNumber = StaticCommonLibrary.CommonLibrary.getNextProductTraceNumber();
//			traceNumber = localDB.getNextProductTraceNumber();
//			strJson = "Store: Order Request from: " + custPhone + " to: " + ownerPhone;
//			trxTime = DateTime.Now;
//			strRecJson = "Success";
//			trxRecTime = DateTime.Now;
//			failedReason = "-";
//			canReversal = false;
//			isSuccessPayment = true;
//			string fiToken = "TMPFIXED";
//			//string trxNumber = localDB.getProductTrxNumber(out xError);
//
//			localDB.TokoOnline_SaveOrder(providerProductCode, ownerPhone, custPhone, int.Parse(providerAmount),
//				trxTime, transactionReference, host, trxNumber);
//
//			Exception ExError = null;
//			System.Collections.Hashtable custInfo = localDB.getLoginInfoByUserPhone(
//				custPhone, out ExError);
//			// buat notifikasi ke penjual
//			// OK, semua sudah masuk persyaratan, sekarang kirim notifikasi ke penyelia
//			// siapkan json untuk notifikasi ke agen
//			jsonConv.Clear();
//			jsonConv.Add("fiToken", securityToken);
//			jsonConv.Add("fiBuyerPhone", custPhone);
//			jsonConv.Add("fiNotificationDateTime", trxTime.ToString("yyyy-MM-dd HH:mm:ss"));
//			jsonConv.Add("fiProductInfo", transactionReference);
//			jsonConv.Add("fiProductPrice", int.Parse(providerAmount));
//			if (custInfo != null)
//			{
//				jsonConv.Add("fiBuyerName", (string)custInfo["first_name"] + " " + (string)custInfo["last_name"]);
//			}
//
//			string notifJson = jsonConv.JSONConstruct();
//
//			localDB.insertNotificationQueue(custPhone, ownerPhone, trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
//				"11", notifJson, out ExError);
//
//			jsonConv.Clear();
//			jsonConv.Add("fiToken", securityToken);
//			jsonConv.Add("fiPrivateData", transactionReference);
//			jsonConv.Add("fiResponseCode", "00");
//			jsonConv.Add("fiTransactionId", "Shop" + traceNumber.ToString().PadLeft(6, '0'));
//			jsonConv.Add("fiToken", fiToken);
//			jsonConv.Add("fiTrxNumber", trxNumber);
//			jsonConv.Add("fiReversalAllowed", false);
//			HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
//
//			return true;
//		}

		private string getProviderInfo(string productCode, out PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct){
			Exception exrr = null;
			providerProduct = null;
			//int baseAmount = 0;
			int adminFee = 0;
			//int customerFeeAmount = 0;
			//PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
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

			return "";
		}

		private bool getBaseAndFeeAmountFromProduct(string productCode, string appId,	//string providerCode, 
			ref decimal adminFee, int TotalAmount = 0)
		{
			Exception xError = null;
			int admFee = 0;
			//if (!localDB.getAdminFeeAndCustomerFee(productCode, providerCode, TotalAmount,
			if (!localDB.getAdminFeeAndCustomerFee(productCode, appId, TotalAmount,
				ref admFee, out xError))
			{
				return false;
			}
			adminFee = admFee;
			return true;
		}

		private string getAdminFee(string productCode, string appID, decimal productAmount, ref decimal adminFee){
			try {
				//LogWriter.showDEBUG (this, " productAmount: " + productAmount);
				//if (!getBaseAndFeeAmountFromProduct (productCode, providerProduct.ProviderCode,
				if (!getBaseAndFeeAmountFromProduct (productCode, appID,
					ref adminFee, decimal.ToInt32 (productAmount))) {
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Fee data not found", "");
				}
			} catch (Exception ex) {
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get fee data : " + ex.getCompleteErrMsg ());
				//Console.WriteLine(ex.StackTrace);
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get fee data", "");
			}
			return "";
		}

		private void getBonggol(PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct, 
			decimal adminFee, ref decimal bonggol, ref decimal nilaiMasukLog){
			LogWriter.showDEBUG (this,"Asup Product Purchase");
			if (providerProduct.fIncludeFee)
			{
				LogWriter.showDEBUG (this,"Asup Product Toko Include fee");
				bonggol = providerProduct.CurrentPrice;
				nilaiMasukLog = bonggol - adminFee;
			}
			else
			{
				LogWriter.showDEBUG (this,"Asup Product Toko Exclude fee");
				bonggol = providerProduct.CurrentPrice + adminFee;
				nilaiMasukLog = providerProduct.CurrentPrice;
			}
		}

		private string transferPaymentFromCustomer(string userId, 
			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct, decimal paymentAmount,
			long TransactionRef_id, ref string qvaInvoiceNumber, ref bool qvaReversalRequired){
			// ===== AMBIL PEMBAYARAN DARI CUSTOMER
			LogWriter.show (this, "Get payment from Customer " + paymentAmount.ToString ());
			using (TransferReguler.CollectTransfer TransferReg =
				new TransferReguler.CollectTransfer (commonSettings)) {
				try {
					string errMessage = "";
					string errCode = "";

					if (!TransferReg.PayFromCustomer (userId, 
						providerProduct.TransactionCodeSufix, 1, decimal.ToInt32 (paymentAmount),
						TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.PPOB, 
						"Get payment from shop customer", ref qvaInvoiceNumber, ref qvaReversalRequired, 
						ref errCode, ref errMessage)) {
						if (qvaReversalRequired)
							TransferReg.Reversal (TransactionRef_id, qvaInvoiceNumber, ref errCode, ref errMessage);
						LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Transfer failed : [" + errCode + "]" + errMessage);
						return HTTPRestDataConstruct.constructHTTPRestResponse (400, errCode, errMessage, "");
					}
				} catch (Exception ex) {
					LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Transfer failed : " + ex.getCompleteErrMsg ());
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Failed to do payment", "");
				}
			}
			return "";
		}

		public string NitrogenShopMakeOrder(){

			// INGAT INI TOKO NITROGEN, lain AliExpress.......

			string[] fields = { "fiApplicationId", "fiProductList",  "fiPhone", "fiGroupProductCode", "fiSAMCSN", "fiToken" };

			string appID = "";
			string groupProductCode = "";
			string userPhone = "";
			string securityToken = "";
			string SamCSN = "";
			string OutletCode = "";
			JsonLibs.MyJsonArray productList;

			if (!jsonConv.JSONParse (clientData.Body)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data format", "");
			}

			if (!checkMandatoryFields (jsonConv, fields)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "406", "Mandatory fields not found", "");
			}

			if (jsonConv.isExists ("fiSAMCSN")) {
				SamCSN = ((string)jsonConv ["fiSAMCSN"]).Trim ();
			}

			try {
				appID = ((string)jsonConv ["fiApplicationId"]).Trim ();
				userPhone = ((string)jsonConv ["fiPhone"]).Trim ();
				groupProductCode = ((string)jsonConv ["fiGroupProductCode"]).Trim ();
				securityToken = ((string)jsonConv ["fiToken"]).Trim ();
				OutletCode = ((string)jsonConv ["fiOutletCode"]).Trim ();
				productList = (JsonLibs.MyJsonArray)jsonConv ["fiProductList"];
			} catch {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "406", "Invalid field type or format", "");
			}

			if(productList.Count <=0)
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "426", "1 product minimum to buy", "");

			ReformatPhoneNumber (ref userPhone);

			if (!cek_SecurityToken (userPhone, securityToken)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "504", "Invalid session or security", "");
			}


			int adminFee = 0;
			Exception xError = null;

			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProductGroup = null;
			string rslt = getProviderInfo (groupProductCode, out providerProductGroup);
			if (rslt != "")
				return rslt;

			xError = null;

			if (productList.Count == 0) {
				LogWriter.show (this, "Product list empty");
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "442", "No product in list", "");
			}

			decimal totalHarga = 0;
			decimal totalAdmin = 0;
			string prdCode = "";
			int quantity = 0;
			decimal admFee = 0;
			decimal totalBonggol = 0;
			decimal bongol = 0;
			decimal totalNilaiMasukLog = 0;
			decimal tmpNilaiMasukLog = 0;
			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct = null;
			for (int i = 0; i < productList.Count; i++) {
				JsonLibs.MyJsonLib aProduct = (JsonLibs.MyJsonLib)productList [i];
				prdCode = ((string)aProduct ["fiProductCode"]).Trim ();
				quantity = ((int)aProduct ["fiQuantity"]);
				rslt = getProviderInfo (prdCode, out providerProduct);
				if (rslt != "")
					return rslt;
				totalHarga += providerProduct.CurrentPrice;
				rslt = getAdminFee (prdCode, appID, providerProduct.CurrentPrice, ref admFee);
				if (rslt != "")
					return rslt;

				getBonggol (providerProduct, admFee, ref bongol, ref tmpNilaiMasukLog);
				totalBonggol += bongol;
				totalNilaiMasukLog += tmpNilaiMasukLog;
				totalAdmin += admFee;
			}

			string userId = cUserIDHeader + userPhone;
			long TransactionRef_id = localDB.getTransactionReffIdSequence (out xError);
			bool qvaReversalRequired = false;
			string qvaInvoiceNumber = "";

			// ===== AMBIL PEMBAYARAN DARI CUSTOMER
			rslt = transferPaymentFromCustomer (userId, providerProduct, totalHarga,
				TransactionRef_id, ref qvaInvoiceNumber, ref qvaReversalRequired);

			if (rslt != "")
				return rslt;
		
			string trxNumber = localDB.getProductTrxNumber(out xError);
			int traceNumber = localDB.getNextProductTraceNumber();
			DateTime skrg = DateTime.Now;
			DateTime trxTime = skrg;
			DateTime trxRecTime = skrg;

			if (!localDB.insertCompleteTransactionLog (TransactionRef_id, groupProductCode, providerProductGroup.ProviderProductCode,
				    userId.Substring (commonSettings.getString ("UserIdHeader").Length), "SHOP TRX",
				totalNilaiMasukLog.ToString (), traceNumber.ToString (), trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				totalAdmin.ToString (), providerProductGroup.ProviderCode, providerProductGroup.CogsPriceId,
				    0, 0, "", skrg.ToString ("yyyy-MM-dd HH:mm:ss"), "", skrg.ToString ("yyyy-MM-dd HH:mm:ss"),
				    "",
				    trxTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				clientData.Body,
				    trxRecTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				    true, 
				"", trxNumber, false, providerProduct.fIncludeFee, SamCSN, OutletCode,
				    out xError)) {
				LogWriter.showDEBUG (this, "Gagal Insert Log....!! CEK LOG DI FILE");
			}

			LogWriter.write(this, LogWriter.logCodeEnum.INFO, "Transaction toko success from " + userId);

			jsonConv.Clear ();
			jsonConv.Add ("fiResponseCode", "00");
			jsonConv.Add ("fiTransactionId", TransactionRef_id);
			jsonConv.Add ("fiTrxNumber", trxNumber);

			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "200", "Success", jsonConv.JSONConstruct ());
		}

	}
}
