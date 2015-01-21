using System;
using PPOBHttpRestData;
using System.Collections;
using System.Collections.Generic;
using LOG_Handler;
using StaticCommonLibrary;
using System.Globalization;

namespace NitrogenClientHandler
{
	public class SettlementTrx : IDisposable {
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
		~SettlementTrx()
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

		string cUserIDHeader = "";

		public long uCardLog_TransactionID=0; // ini belum bisa diisi di class ini
		public string uCardLog_SamCSN="";
		public string uCardLog_CardPurchaseLog=""; 
		public int uCardLog_PreviousBalance=0;
		public bool isCardTransaction=false;

		/******************************************************************************/

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

			return true;
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
				httpRepl = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid token field", "");
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

		private bool cek_TokenSecurity(string userPhone, string token, ref string httpRepl)
		{
			// cek detek sessionnya
			if (!CommonLibrary.isSessionExist(userPhone, token))
			{
				httpRepl = HTTPRestDataConstruct.constructHTTPRestResponse(400, "504", "Invalid session", "");
				return false;
			}
			return true;
		}


		/*============================================================================*/
		/*============================================================================*/

		#endregion

		public SettlementTrx (HTTPRestConstructor.HttpRestRequest ClientData,
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


		public string Settlement()
		{
			string[] fields = { "fiPhone", "fiApplicationId", "fiProductCode", 
				"fiQuantity", "fiTrxDateTime", "fiToken" };
			string hasil = "";
			string userPhone = "";
			string userId = "";
			if (!standardFieldsCheck(clientData, fields, ref hasil, ref userPhone, ref userId))
			{
				return hasil;
			}

			string appID = "";
			string productCode = "";
			string dateTime = "";
			DateTime trxDateTime;
			int quantity = 0;

			try
			{
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
				productCode = ((string)jsonConv["fiProductCode"]).Trim();
				quantity = (int)jsonConv["fiQuantity"];
				dateTime = ((string)jsonConv["fiTrxDateTime"]).Trim();
				trxDateTime = DateTime.ParseExact(dateTime, "yyyy-MM-dd HH:mm:ss", null);
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
			}

			//cek security token
			string token = "";
			string httpReply = "";
			if (!cek_TokenSecurity(userPhone, jsonConv, ref token, ref httpReply))
			{
				return httpReply;
			}
			CommonLibrary.SessionResetTimeOut (userPhone);

			// ====  apakah sudah renew token session?

			// simpan datanya di database
			Exception ExError = null;
			string dbTraceNum = "";
			if (localDB.isOfflineTransactionExist(userPhone, appID, productCode, trxDateTime, 
				quantity,ref dbTraceNum,  true, out ExError))
			{
				// anggap sukses
				// perbaharui token 
				//token = CommonLibrary.RenewTokenSession(userPhone);
				jsonConv.Clear();
				jsonConv.Add("fiToken", token);
				jsonConv.Add("fiResponseCode", "00");
				return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
				// return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Data already exist", "");
			}
			if (ExError != null)
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Can not access database", "");
			}

			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
			try
			{
				providerProduct = localDB.getProviderProductInfo(productCode, out ExError);
				if (ExError != null)
				{
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Can not access database", "");
				}
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Can not access database", "");
			}

			int traceNumber = localDB.getNextProductTraceNumber();

			if (!localDB.saveOfflineTransaction(userPhone, appID, productCode, trxDateTime,
				quantity,providerProduct.CurrentPrice, clientData.Host, traceNumber,true, out ExError))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Can not store to database", "");
			}

			// perbaharui token 
			//token = CommonLibrary.RenewTokenSession(userPhone);

			jsonConv.Clear();
			jsonConv.Add("fiToken", token);
			jsonConv.Add("fiResponseCode", "00");

			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
		}


		string appID="";
		string customerProductNumber = "";
		string productCode = "";
		int transactionType = 0;
		int amount = 0;
		string securityToken = "";
		string requestCode = "";


		// additionalJson
		int fiBalance;
		int fiQuantity;
		string fiStrTrxDateTime;
		string fiSAMCSN;
		string fiCertificate;
		string fiUserCardNumber;
		string fiPurchaseLog;


		int productAmount=0;
		PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
		int totalProductAmount=0;
		int topupFee=0;

		public string PrepareAllParameter()
		{
			string[] fields = { "fiPhone", "fiApplicationId", "fiProductCode", 
				"fiToken", "fiRequestCode", "fiCustomerNumber", "fiTransactionType", "fiAmount", "fiAdditional"
			};
			string[] additionalFields = { "fiBalance",  
				"fiQuantity", "fiTrxDateTime", "fiSAMCSN", "fiCertificate", "fiLogPurchase"
			};
			string hasil = "";
			string userPhone = "";
			string userId = "";
			if (!standardFieldsCheck (clientData, fields, ref hasil, ref userPhone, ref userId)) {
				return hasil;
			}

			JsonLibs.MyJsonLib additionalJson;

			try{
				additionalJson = (JsonLibs.MyJsonLib)jsonConv["fiAdditional"];
			}catch{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Invalid field type", "");
			}

			if(!checkMandatoryFields (additionalFields, additionalJson)){
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory additional field not found", "");
			}

			try
			{
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
				customerProductNumber = ((string)jsonConv["fiCustomerNumber"]).Trim();
				productCode = ((string)jsonConv["fiProductCode"]).Trim();
				transactionType = (int)jsonConv["fiTransactionType"];
				amount = (int)jsonConv["fiAmount"];
				securityToken = ((string)jsonConv["fiToken"]).Trim();
				requestCode = ((string)jsonConv["fiRequestCode"]).Trim();
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type", "");
			}

			try{
				fiBalance = (int)additionalJson["fiBalance"];
				fiQuantity = (int)additionalJson["fiQuantity"];
				fiStrTrxDateTime = ((string)additionalJson["fiTrxDateTime"]).Trim();
				fiSAMCSN = ((string)additionalJson["fiSAMCSN"]).Trim();
				fiCertificate = ((string)additionalJson["fiCertificate"]).Trim();
				fiPurchaseLog = ((string)additionalJson["fiLogPurchase"]).Trim();
				fiUserCardNumber = customerProductNumber.Trim();
			}
			catch{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					"Invalid field value", "");
			}

			string formatDate = "yyMMddHHmmss";
			DateTime fiTrxDateTime;
			CultureInfo provider = CultureInfo.InvariantCulture;

			try{
				fiTrxDateTime = DateTime.ParseExact( fiStrTrxDateTime, formatDate, provider);
			}
			catch{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					"Invalid date format in Additional Data", "");
			}

			//cek security token
			string httpReply = "";
			if (!cek_TokenSecurity(userPhone, securityToken, ref httpReply)){
				return httpReply;
			}
			CommonLibrary.SessionResetTimeOut (userPhone);

			return "";
		}

		private string hitungTopUpFee(ref decimal topUpPercentFee){
			string hasil = "";
			Exception exer;
			try {
				// ProviderCode diganti 000 khusus untuk ambil data topup
				if (!localDB.getPercentAdminFee (commonSettings.getString ("IconoxTopUpClientProductCode"),
					"000", ref topUpPercentFee, out exer)) {
					LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get TopUp fee percent data");
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get TopUp fee percent data", "");
				}
				// NOTE : Disini nanti dana diambil dari Account Iconox TITIPAN atau bukan tergantung dengan kartu atau bukannya

				productAmount = providerProduct.CurrentPrice;   // 100% nya
				totalProductAmount = productAmount;	

				LogWriter.showDEBUG (this, "productAmount = "+productAmount.ToString ());

				topupFee = ((int)Math.Ceiling (totalProductAmount * (topUpPercentFee / 100)));

				productAmount = totalProductAmount - topupFee; // 99% nya

				LogWriter.showDEBUG (this, "productAmountDenganKartu = "+productAmount.ToString ());

			} catch (Exception ex) {
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get topup fee data : " + ex.getCompleteErrMsg ());
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get topup fee data", "");
			}
			return hasil;
		}

		private bool getBaseAndFeeAmountFromProduct(string productCode, string appID,	//string providerCode, 
			ref decimal adminFee, int TotalAmount = 0)
		{
			Exception exer;
			//if (!localDB.getAdminFeeAndCustomerFee(productCode, providerCode, TotalAmount,
			if (!localDB.getAdminFeeAndCustomerFee(productCode, appID, TotalAmount,
				ref adminFee, out exer))
				return false;
			else
				return true;
		}

		public string ProcessAsOnline()
		{
			Exception exer;

			string hasil = PrepareAllParameter ();
			if (hasil != "")
				return hasil;

			bool isTrxKartu = false;
			if (fiPurchaseLog.Length > 0)
				isTrxKartu = true;

			try
			{
				providerProduct = localDB.getProviderProductInfo(productCode, out exer);
				if (exer != null)
				{
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider product data not found", "");
				}
			}
			catch(Exception ex)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error get provider product data : " + ex.getCompleteErrMsg());
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting provider product data", "");
			}

			if (isTrxKartu)
				return NextStepWithCard ();
//			else
//				return NextStepWithoutCard ();
			return "";
		}


		private string getPaymentFromCardSuspendAccount(){
			//productAmount = prdAmount.ToString();
			//nilaiTransaksiKeProvider = providerProduct.CurrentPrice;

			Exception exer;
			long TransactionRef_id = localDB.getTransactionReffIdSequence(out exer);
			string qvaInvoiceNumber = "";
			bool qvaReversalRequired = false;

			string errCode = "";
			string errMessage = "";

			using (TransferReguler.CollectTransfer TransferReg =
				new TransferReguler.CollectTransfer(commonSettings))
			{
				try
				{
						// ====== PEMBAYARAN DARI EWALLET
					//LogWriter.show(this, "Get payment from EWallet");
					LogWriter.showDEBUG (this,"4. Product Purchase productAmount = " + productAmount.ToString ());
					if (!TransferReg.PayFromCustomerEwallet( 
						providerProduct.TransactionCodeSufix, transactionType, productAmount,
						TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.PPOB, 
						"Get payment from customer Ewallet", ref qvaInvoiceNumber, ref qvaReversalRequired, 
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
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to do payment", "");
				}
			}
			return "";
		}

		private int hitungNilaiTransaksiMasukLog(decimal adminFee, ref string httpReply){
			decimal nilaiMasukLog = 0;
			adminFee = Math.Ceiling (adminFee);
			httpReply = "";

			if (providerProduct.ProductType == PPOBDatabase.PPOBdbLibs.ProductTypeEnum.PRODUCT)
			{
				LogWriter.showDEBUG (this,"Nitrogen: Asup Product Purchase");
				if (providerProduct.fIncludeFee)
				{
					LogWriter.showDEBUG (this,"Nitrogen: Asup Product Purchase Include fee");
					nilaiMasukLog = productAmount - adminFee;		// disini adminfee udah 4000, dari 4%
					LogWriter.showDEBUG (this,"Nitrogen: 1. Product Purchase productAmount = " + productAmount.ToString ());
				}
				else
				{
					LogWriter.showDEBUG (this,"Nitrogen: Asup Product Purchase Exclude fee");
					nilaiMasukLog = productAmount;
					productAmount = productAmount + decimal.ToInt32 (adminFee);
				}
			}
			else
			{
				LogWriter.showDEBUG (this,"Nitrogen: Asup Service Payment");
				if (providerProduct.fIncludeFee)
				{
					LogWriter.showDEBUG (this,"Nitrogen: Asup Service Payment Include fee");
					nilaiMasukLog = productAmount - adminFee;
				}
				else
				{
					LogWriter.showDEBUG (this,"Nitrogen: Asup Service Payment Exclude fee");
					nilaiMasukLog = productAmount;
					productAmount = productAmount + decimal.ToInt32 (adminFee);
				}

				if (productAmount <= 0)
				{
					httpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Bad amount", "");
					return -1;
				}
			}

			return decimal.ToInt32 (nilaiMasukLog);
		}

		private string NextStepWithCard(){
			// Hitungan dari Topup Fee
			string hasil = "";

			decimal topUpFee=0;

			decimal adminFee = 0;

			int nilaiYangMasukLog = 0;

			hasil = hitungTopUpFee (ref topUpFee);
			if (hasil.Length > 0)
				return hasil;

			try {
				//LogWriter.showDEBUG (this, " productAmount: " + productAmount);
				// hitung adminFee dari total product amount, bukan setelah dipotong topup
				//if (!getBaseAndFeeAmountFromProduct (productCode, providerProduct.ProviderCode,
				if (!getBaseAndFeeAmountFromProduct (productCode, appID,
					ref adminFee, totalProductAmount)) {
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Fee data not found", "");
				}
				//  cardProductAmount = 100.000, adminFee=4000, ke nu masuk db productAmount: 99.000, adminFee 4000
			} catch (Exception ex) {
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get fee data : " + ex.getCompleteErrMsg ());
				//Console.WriteLine(ex.StackTrace);
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get fee data", "");
			}

			string httpRepl = "";
			nilaiYangMasukLog = hitungNilaiTransaksiMasukLog (adminFee, ref httpRepl);
			if (httpRepl != "")
				return httpRepl;

			// getPayment from card account
			hasil = getPaymentFromCardSuspendAccount ();
			if (hasil.Length > 0)
				return hasil;

			//hasil = ExecuteCardTransaction ();	// kudu di unremark
			//if (hasil.Length > 0)
				return hasil;

		}


		private bool ExecuteCardTransaction (string appID, DateTime fiTrxDateTime,
			string fiUserCardNumber, int fiBalance, int fiQuantity, ref bool isSuccessPayment,
			ref string strRecJson, ref int traceNumber, string trxNumber,
			string fiStrTrxDateTime, string fiSAMCSN, string fiCertificate, string fiPurchaseLog,
			ref string failedReason, ref string HttpReply, ref bool alreadyPaid){

//			traceNumber = localDB.getNextProductTraceNumber();
//			alreadyPaid = false;
//
//			uCardLog_PreviousBalance = fiBalance;
//			uCardLog_CardPurchaseLog = fiPurchaseLog;
//			uCardLog_SamCSN = fiSAMCSN;
//
//			// cek apakah kartu terdaftar di database
//			string fReason = "";
//			decimal dbBalance = 0;
//			DateTime lastModified=DateTime.Now;
//			if(!localDB.isCardActivated(fiUserCardNumber, ref dbBalance, ref lastModified,
//				ref fReason)){
//				failedReason = "Purchase: "+fReason;
//				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "415", 
//					failedReason, "");
//				return false;
//			}
//
//			int totalBalance = fiBalance - trxAmount;
//			if ((totalBalance < 0) || (trxAmount<=0)) {
//				// gak boleh negatif
//				failedReason = "Purchase: Invalid purchase, negatif balance";
//				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "417", 
//					failedReason, "");
//				return false;
//			}
//
//			// CEK Certificate
//
//			// Siapkan data untuk kirim ke server iconox
//			// Server Iconox
//			//				fiTagCode
//			//				fiAgentPhone
//			//				fiDateTime
//			//				fiAmount
//			//				fiSAMCSN
//			//				fiCertificate
//			jsonConv.Clear ();
//			jsonConv.Add ("fiTagCode","03");
//			jsonConv.Add ("fiCardNumber",fiUserCardNumber);
//			jsonConv.Add ("fiAgentPhone",agentPhone);
//			jsonConv.Add ("fiDateTime",fiStrTrxDateTime);
//			jsonConv.Add ("fiAmount",trxAmount);
//			jsonConv.Add ("fiSAMCSN",fiSAMCSN);
//			jsonConv.Add ("fiCertificate",fiCertificate);
//			jsonConv.Add ("fiPurchaseLog", fiPurchaseLog);
//
//			string IconoxSvrResp = RequestToIconoxServer (jsonConv.JSONConstruct ());
//
//			if (IconoxSvrResp.Length <= 0) {
//				failedReason = "No data from Iconox Server";
//				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "413", 
//					failedReason, "");
//				return false;
//			}
//
//			jsonConv.Clear();
//			if (!jsonConv.JSONParse (IconoxSvrResp)) {
//				failedReason = "Invalid data format from Iconox Server";
//				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", 
//					failedReason, "");
//				return false;
//			}
//
//			string respCode = "";
//			//			string respMsg = "";
//			//			string respTag = "";
//			//string respSam = "";
//			// Ada tambahan data untuk transaksi iconox, subjson dari fiAdditional
//			if ((!jsonConv.ContainsKey("fiResponseCode")) || 
//				(!jsonConv.ContainsKey("fiResponseMessage")) || 
//				(!jsonConv.ContainsKey("fiTagCode")))
//			{
//				failedReason = "Mandatory field from Iconox server not found";
//				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "416", 
//					failedReason, "");
//				return false;
//			}
//			respCode = ((string)jsonConv["fiResponseCode"]).Trim();
//			if(respCode!="00"){
//				failedReason = "Iconox server message: " + ((string)jsonConv["fiResponseMessage"]).Trim ();
//				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "I"+respCode, 
//					failedReason, "");
//				return false;
//			}
//
//			Exception ExError = null;
//			// persiapan simpan datanya di database
//
//			string dbTraceNum = "";
//			if (localDB.isOfflineTransactionExist(agentPhone, appID, productCode, fiTrxDateTime, 
//				fiQuantity, ref dbTraceNum, true, out ExError))
//			{
//				// Data sudah ada di database, anggap sukses
//				//				// perbaharui token 
//				//				token = CommonLibrary.RenewTokenSession(userPhone);
//				traceNumber = int.Parse (dbTraceNum);
//				alreadyPaid = true;
//				isSuccessPayment = true;
//				strRecJson = "Success";
//				jsonConv.Clear();
//				jsonConv.Add("fiToken", securityToken);
//				jsonConv.Add("fiPrivateData", "");
//				jsonConv.Add("fiResponseCode", respCode);
//				//jsonConv.Add("fiTransactionId", "IcP" + tracenumber.ToString().PadLeft(6, '0'));
//				jsonConv.Add("fiTransactionId", "IcP" + dbTraceNum);
//				//jsonConv.Add("fiToken", fiToken);
//				jsonConv.Add("fiTrxNumber", trxNumber);
//				jsonConv.Add("fiReversalAllowed", false);
//				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
//
//				return true;
//			}
//			if (ExError != null)
//			{
//				failedReason = "Can not access database";
//				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
//					failedReason, "");
//				return false;
//			}
//
//			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
//			try
//			{
//				providerProduct = localDB.getProviderProductInfo(productCode, out ExError);
//				if (ExError != null)
//				{
//					failedReason = "Can not access database";
//					HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
//						failedReason, "");
//					return false;
//				}
//			}
//			catch
//			{
//				failedReason = "Can not access database";
//				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
//					failedReason, "");
//				return false;
//			}
//
//			if (!localDB.saveOfflineTransaction(agentPhone, appID, productCode, fiTrxDateTime,
//				fiQuantity,providerProduct.CurrentPrice, clientData.Host, traceNumber,true, out ExError))
//			{
//				failedReason = "Can't store to database";
//				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
//					failedReason, "");
//				return false;
//			}
//
//
//			isSuccessPayment = true;
//			strRecJson = "Success";
//			//trxRecTime = DateTime.Now;
//			//respSam = ((string)jsonConv["fiServerSAMResponse"]).Trim ();
//
//			jsonConv.Clear();
//			jsonConv.Add("fiToken", securityToken);
//			jsonConv.Add("fiPrivateData", "");
//			jsonConv.Add("fiResponseCode", respCode);
//			jsonConv.Add("fiTransactionId", "IcP" + traceNumber.ToString().PadLeft(6, '0'));
//			//jsonConv.Add("fiToken", fiToken);
//			jsonConv.Add("fiTrxNumber", trxNumber);
//			jsonConv.Add("fiReversalAllowed", false);
//			HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

			return true;
		}


	}
}

