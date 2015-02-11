//

// TODO : SEMUA disini belum 

using System;
using PPOBHttpRestData;
using System.Collections;
using System.Collections.Generic;
using LOG_Handler;
using StaticCommonLibrary;
using IconoxOnlineHandler;

using System.Globalization;

namespace BPJS_THT
{
	public class Terminal_Handler : IDisposable {
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
		~Terminal_Handler()
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

		/*============================================================================*/
		/*============================================================================*/

		#endregion


		public Terminal_Handler (HTTPRestConstructor.HttpRestRequest ClientData,
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

		private bool cek_SecurityToken(string userPhone, string token)
		{
			// cek detek sessionnya
			return CommonLibrary.isSessionExist (userPhone, token);
		}

		public string InfoSaldo(){
			string[] fields = { "fiApplicationId", "fiToken", "fiNoKPS", "fiMsisdn", "fiMessageId" };

			string appID = "";
			string MsIsdn = "";
			string strJson = "";
			string securityToken = "";
			string HttpReply = ""; 
			string NoKPS = "";
			string MessageId = "";

			Exception xError = null;
			string productCode = commonSettings.getString ("ProductCode_BPJS_Multipayment");

			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}
			strJson = clientData.Body;

			if(!checkMandatoryFields(jsonConv, fields))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields not found", "");
			}

			try
			{
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
				//securityToken = ((string)jsonConv["fiToken"]).Trim();
				MsIsdn = ((string)jsonConv["fiMsisdn"]).Trim ();
				NoKPS = ((string)jsonConv["fiNoKPS"]).Trim();
				MessageId = ((string)jsonConv["fiMessageId"]).Trim();
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
			}

			ReformatPhoneNumber (ref MsIsdn);

			//			if (!cek_SecurityToken (userPhone, securityToken)) {
			//				LOG_Handler.LogWriter.showDEBUG (this, "Cek Token Session: " + userPhone + 
			//					", token: " + securityToken);
			//				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "504", "Invalid session", "");
			//			}

			// konek ka server IBID Swicth tanpa json

			// TEs hungkul
			//MsIsdn = 

			string IbidResponse = "";
			using (IBID_Handler ibid = new IBID_Handler(1200, false)){
				IbidResponse = ibid.InquirySaldo (NoKPS,MsIsdn,MessageId,false);
			}

			string failedReason = "";
			if (IbidResponse.Length <= 0) {
				failedReason = "No data from IBID host";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "413", 
					failedReason, "");
				return HttpReply;
			}

			jsonConv.Clear ();
			if (!jsonConv.JSONParse (IbidResponse)) {
				failedReason = "Invalid data format from IBID host";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", 
					failedReason, "");
				LogWriter.showDEBUG (this, "IBID server Reply : " + IbidResponse);
				return HttpReply;
			}

			int fiReturnCode = 0;
			string fiAddValues = "";
			string fiMessage = "";
			string fiKode = "";
			if(jsonConv.isExists ("addValues"))
				fiAddValues = ((string)jsonConv ["addValues"]).Trim ();

			if (HttpReply == "") {
				try {
					fiReturnCode = (int)jsonConv ["ret"];
					fiMessage = ((string)jsonConv ["message"]).Trim ();
					fiKode = ((string)jsonConv ["kode"]).Trim ();
				} catch (Exception ex) {
					failedReason = "Incomplete fields from payment server";
					LogWriter.write (this, LogWriter.logCodeEnum.ERROR, failedReason + ": " + ex.getCompleteErrMsg ());
					HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse (400, "492",
						failedReason, "");
					return HttpReply;
				}
			}

			jsonConv.Clear();
			jsonConv.Add ("fiReturnCode", fiReturnCode);
			jsonConv.Add ("fiAddValues", fiAddValues);
			jsonConv.Add ("fiMessage",fiMessage);
			jsonConv.Add ("fiKode", fiKode);
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct ());
		}

		private string RequestToIconoxTopUpServer(string msg){
			using (IconoxTcpClient tcpI = new IconoxTcpClient (commonSettings.getString ("IconoxQueueHost"),
				commonSettings.getInt ("IconoxQueuePort"))) {
				//Console.WriteLine ("Connecting to iconox server...");

				if (!tcpI.Connect ()) {
					//Console.WriteLine ("Gagal konek ke iconox server...");
					return "";
				}
				//Console.WriteLine ("Kirim data to iconox server...");
				if (!tcpI.SendPlusProtocol (msg)){
					Console.WriteLine ("Gagal kirim ke iconox server...");
					return "";
				}
				//Console.WriteLine ("Baca balikan dari iconox server...");
				return tcpI.Read (commonSettings.getInt ("IconoxQueueTimeout"));
			}
		}

		// Disini sudah gak perlu pake protokol deui
		private string RequestToIconoxPaymentServer(string msg){
			using (IconoxTcpClient tcpI = new IconoxTcpClient (commonSettings.getString ("IconoxQueuePaymentHost"),
				commonSettings.getInt ("IconoxQueuePaymentPort"))) {
				//Console.WriteLine ("Connecting to iconox server...");

				try{
				if (!tcpI.Connect ()) {
					//Console.WriteLine ("Gagal konek ke iconox server...");
					return "";
				}
				}catch{
					return "";
				}
				//Console.WriteLine ("Kirim data to iconox server...");
				if (!tcpI.SendPlusProtocol (msg)){
					Console.WriteLine ("Gagal kirim ke iconox payment server...");
					return "";
				}
				//Console.WriteLine ("Baca balikan dari iconox server...");
				return tcpI.Read (commonSettings.getInt ("IconoxQueueTimeout"));
			}
		}

		decimal topUpPercentFee = 0;
		public int trxAmount=0;
		private string hitungFeeTrxKartu(PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct, 
			int topupAmount, ref int vaAmount, ref int topUpFee){
			Exception exrr = null;
			try {
				//					if (!localDB.getPercentAdminFee (commonSettings.getString ("IconoxTopUpClientProductCode"),
				//						    providerProduct.ProviderCode, ref topUpPercentFee, out xError)) {
				//						LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get TopUp fee percent data");
				//						return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get TopUp fee percent data", "");
				//					}
				// ProviderCode diganti 000 khusus untuk ambil data topup
				if (!localDB.getPercentAdminFee (commonSettings.getString ("IconoxTopUpClientProductCode"),
					"000", ref topUpPercentFee, out exrr)) {
					LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get TopUp fee percent data");
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get TopUp fee percent data", "");
				}
				// NOTE : Disini nanti dana diambil dari Account Iconox TITIPAN atau bukan tergantung dengan kartu atau bukannya

				if (providerProduct.ProductType == PPOBDatabase.PPOBdbLibs.ProductTypeEnum.PRODUCT){
					trxAmount = providerProduct.CurrentPrice;
				}

				LogWriter.showDEBUG (this, "productAmount = "+trxAmount.ToString ());

				// potong 1 % untuk transaksi virtual accountnya
				topUpFee = ((int)Math.Ceiling (topupAmount * (topUpPercentFee / 100)));
				vaAmount = topupAmount - topUpFee; // 99% nya
				//topupAmount = vaAmount;

				if (providerProduct.ProductType == PPOBDatabase.PPOBdbLibs.ProductTypeEnum.PRODUCT){
					providerProduct.CurrentPrice = topupAmount;
				}

				LogWriter.showDEBUG (this, "productAmountDenganKartu = "+vaAmount.ToString ());

				//			Console.WriteLine ("DEBUG -- topUpPercentFee= "+topUpPercentFee);
				//			Console.WriteLine ("DEBUG -- cardProductAmount= "+cardProductAmount);
				return "";
			} catch (Exception ex) {
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get topup fee data : " + ex.getCompleteErrMsg ());
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get topup fee data", "");
			}
		}

		private string bayarDariPetugasKePenampung(string userId, 
			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct,
			int amount, long TransactionRef_id, 
			ref string qvaInvoiceNumber, ref bool qvaReversalDone, 
			ref string errCode, ref string errMessage
		){

			int PpobType = 2;	// topup
			bool qvaReversalRequired=false;
			qvaReversalDone = qvaReversalRequired;

			using (TransferReguler.CollectTransfer TransferReg =
				new TransferReguler.CollectTransfer(commonSettings))
			{
				try
				{
					// ===== AMBIL PEMBAYARAN DARI CUSTOMER
					LogWriter.show(this, "Get payment from Customer");
					if (!TransferReg.PayFromCustomer(userId, 
						providerProduct.TransactionCodeSufix, PpobType, amount,
						TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.PPOB, 
						"Get payment from customer", ref qvaInvoiceNumber, ref qvaReversalRequired, 
						ref errCode, ref errMessage))
					{
						if(qvaReversalRequired)
							TransferReg.Reversal(TransactionRef_id,qvaInvoiceNumber, ref errCode, ref errMessage);
						qvaReversalDone = qvaReversalRequired;
						LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : [" + errCode + "]" + errMessage);
						return HTTPRestDataConstruct.constructHTTPRestResponse(400, errCode, errMessage, "");
					}
					return "";
				}
				catch (Exception ex)
				{
					LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Transfer failed : " + ex.getCompleteErrMsg());
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "495", "Failed to do payment", "");
				}
			}
		}

		public string PaymentOnline(){
			string[] fields = { "fiApplicationId", "fiUser", "fiToken", 
				"fiPurchaseChallenge", "fiTrxDateTime", "fiCardBalance", "fiAmount", 
				"fiUserCardNumber"};

			string appID = "";
			string user = "";		// sebelumnya dari fiPhone
			string token = "";
			string purchaseChallenge = "";
			string strxDateTime = "";
			string cardNumber = "";
			int cardBalance = 0;
			int totalAmount = 0;

			Exception xError=null;

			string productCode = "PRD00101";


			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}

			if(!checkMandatoryFields(jsonConv, fields))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields not found", "");
			}

//			if (!jsonConv.isExists ("fiOutletCode")) {
//				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory outletcode fields not found", "");
//			}

			try
			{
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
				user = ((string)jsonConv["fiUser"]).Trim ();
				token = ((string)jsonConv["fiToken"]).Trim ();
				purchaseChallenge = ((string)jsonConv["fiPurchaseChallenge"]).Trim ();
				strxDateTime = ((string)jsonConv["fiTrxDateTime"]).Trim ();
				cardBalance = (int)jsonConv["fiCardBalance"];
				cardNumber = ((string)jsonConv["fiUserCardNumber"]).Trim ();
				totalAmount = (int)jsonConv["fiAmount"];
				//outletCode = ((string)jsonConv["fiOutletCode"]).Trim ();
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
			}
			DateTime trxDateTime;
			try{
				trxDateTime = DateTime.ParseExact(strxDateTime, "yyMMddHHmmss", null);
			} catch{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field date format", "");
			}

			ReformatPhoneNumber (ref user);

			string userId = cUserIDHeader + user;


			// cek token disini
			if (!cek_SecurityToken (user, token)) {
				LOG_Handler.LogWriter.showDEBUG (this, "Cek Token Session: " + user + 
					", token: " + token);
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "504", "Invalid session", "");
			}

			// konek ka server iconox online payment 
			jsonConv.Clear ();
			// jika fiCertificate = "" maka topup tanpa sign dgn sam krn pake nfc
			jsonConv.Add ("fiTagCode","01");		// payment Challenge
			jsonConv.Add ("fiAgentPhone",user);
			jsonConv.Add ("fiKeyAddress",commonSettings.getString ("IconoxEwallet1-KeyAddress"));
			jsonConv.Add ("fiResponseUserCard",purchaseChallenge);

			string strJson = jsonConv.JSONConstruct ();

			string IconoxSvrResp = RequestToIconoxPaymentServer (strJson);

			string failedReason = "";
			string HttpReply = ""; 
			if (IconoxSvrResp.Length <= 0) {
				failedReason = "No data from Iconox Payment Server";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "413", 
					failedReason, "");
				return HttpReply;
			}

			jsonConv.Clear();
			if (!jsonConv.JSONParse (IconoxSvrResp)) {
				failedReason = "Invalid data format from Iconox Server";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", 
					failedReason, "");
				return HttpReply;
			}

			string[] repFields = { 
				"fiResponseCode", "fiResponseMessage", "fiPaySAMResponse", "fiLastTransactionLog"
			};

			if(!checkMandatoryFields(jsonConv, repFields))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "PSAM Server mandatory fields not found", "");
			}

			string respCodeSvr = ((string)jsonConv["fiResponseCode"]).Trim ();
			string respMsgSvr = ((string)jsonConv["fiResponseMessage"]).Trim ();
			string respPSAMResp = ((string)jsonConv["fiPaySAMResponse"]).Trim ();
			string respLastTrxLog = ((string)jsonConv["fiLastTransactionLog"]).Trim ();

			if(respCodeSvr!="00"){
				failedReason = "Iconox payment server message: " + ((string)jsonConv["fiResponseMessage"]).Trim ();
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "I"+respCodeSvr, 
					respMsgSvr, "");
				return HttpReply;
			}

			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
			//Console.WriteLine("ProductCode = " + productCode);
			try
			{
				providerProduct = localDB.getProviderProductInfo(productCode, out xError);
				if (xError != null)
				{
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider product data not found", "");
				}
			}
			catch(Exception ex)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error get provider product data : " + ex.getCompleteErrMsg());
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting provider product data", "");
			}

			// FIXME JANG DEMO konek langsung bae ka Power-T, harusnya dilakukan settlement perwaktu tertentu
			// tembak ka power-T

			int traceNumber = localDB.getNextProductTraceNumber();
			DateTime skrg = DateTime.Now;
			string trxNumber = localDB.getProductTrxNumber(out xError);

			// Ambil pembayaran dari penitipan ke penampungan
			// ProviderCode diganti 000 khusus untuk ambil data topup
			if (!localDB.getPercentAdminFee (commonSettings.getString ("IconoxTopUpClientProductCode"),
				"000", ref topUpPercentFee, out xError)) {
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get TopUp fee percent data");
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get TopUp fee percent data", "");
			}


			int productAmount = totalAmount;

			LogWriter.showDEBUG (this, "productAmount = "+productAmount.ToString ());

			int productAmountDenganKartu = productAmount - ((int)Math.Ceiling (productAmount * (topUpPercentFee / 100))); // 99% nya
			productAmount = productAmountDenganKartu;

			decimal adminFee = 0;
			int nilaiYangMasukLog = 0;

			try {
				//LogWriter.showDEBUG (this, " productAmount: " + productAmount);
				//if (!getBaseAndFeeAmountFromProduct (productCode, providerProduct.ProviderCode,
				if (!localDB.getAdminFeeAndCustomerFee(productCode, 1, appID, totalAmount,
					ref adminFee, out xError)){
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Fee data not found", "");
				}
				//  cardProductAmount = 100.000, adminFee=4000, ke nu masuk db productAmount: 99.000, adminFee 4000
			} catch (Exception ex) {
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get fee data : " + ex.getCompleteErrMsg ());
				//Console.WriteLine(ex.StackTrace);
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get fee data", "");
			}

			// Product, Include fee
			nilaiYangMasukLog = productAmount - decimal.ToInt32 (adminFee);		// disini adminfee udah 4000, dari 4%

			//productAmount = prdAmount.ToString();
			int nilaiTransaksiKeProvider = productAmount;
			long TransactionRef_id = localDB.getTransactionReffIdSequence(out xError);
			string qvaInvoiceNumber = "";
			bool qvaReversalRequired = false;

			LogWriter.showDEBUG (this, " ============ DEBUG ========= \r\n" +
				"nilaiYangMasukLog = " + nilaiYangMasukLog.ToString () + "\r\n" + 
				"productAmount = " + productAmount.ToString () + "\r\n" +
				"adminFee = " + adminFee.ToString () + "\r\n" +
				"totalAmount = " + totalAmount.ToString () + "\r\n" +
				"productAmountDenganKartu = " + productAmountDenganKartu.ToString () + "\r\n" +
				"totalAmount = " + totalAmount.ToString () + "\r\n" +
				"topUpPercentFee = " + topUpPercentFee.ToString () + "\r\n" +
				" ============ DEBUG ========= \r\n"
			);

			string errCode = "";
			string errMessage = "";
			using (TransferReguler.CollectTransfer TransferReg =
				       new TransferReguler.CollectTransfer (commonSettings)) {
				try {
					if (productAmount > 0) {		// kalo pembayaran gratis, gak usah transfer
						if (!TransferReg.PayFromCustomerEwallet (
							 providerProduct.TransactionCodeSufix, 1, productAmount,
							 TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.PPOB, 
							 "Get payment from customer Ewallet", ref qvaInvoiceNumber, ref qvaReversalRequired, 
							 ref errCode, ref errMessage)) {
							if (qvaReversalRequired)
								TransferReg.Reversal (TransactionRef_id, qvaInvoiceNumber, ref errCode, ref errMessage);
							LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Transfer failed : [" + errCode + "]" + errMessage);
							return HTTPRestDataConstruct.constructHTTPRestResponse (400, errCode, errMessage, "");
						}
					}
				} catch (Exception ex) {
					LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Transfer failed : " + ex.getCompleteErrMsg ());
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Failed to do payment", "");
				}
			}

			// insert log transaksi
			if (!localDB.insertCompleteTransactionLog (TransactionRef_id, productCode, providerProduct.ProviderProductCode,
				    userId.Substring (commonSettings.getString ("UserIdHeader").Length), cardNumber,
				    nilaiYangMasukLog.ToString (), traceNumber.ToString (), trxDateTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				    adminFee.ToString (), providerProduct.ProviderCode, providerProduct.CogsPriceId,
				    0, 0, "", skrg.ToString ("yyyy-MM-dd HH:mm:ss"), "", skrg.ToString ("yyyy-MM-dd HH:mm:ss"),
				    strJson,
				    trxDateTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				    IconoxSvrResp,
				    skrg.ToString ("yyyy-MM-dd HH:mm:ss"),
				    true, 
				    failedReason, trxNumber, false, providerProduct.fIncludeFee, "", "",
				    out xError)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Failed to save transaction log", "");
			}

			// insert log ucard_transaction
			if (!localDB.addCardTransactionLog (TransactionRef_id, "", respLastTrxLog, cardBalance, appID,
				out xError)) {
				// sudah di catat
			}

			jsonConv.Clear();
			jsonConv.Add ("fiPSAMChallenge", respPSAMResp);
			jsonConv.Add ("fiResponseMessage", respMsgSvr);
			jsonConv.Add ("fiResponseCode",respCodeSvr);
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct ());

		}

		public string TopUpOnline(){
			string[] fields = { "fiApplicationId", "fiPhone", "fiToken", 
				"fiAdditional", "fiAmount", "fiCustomerNumber"};

			string[] addFields = { "fiBalance", "fiTrxDateTime", "fiSAMCSN",
				"fiCertificate", "fiCardChallenge"};

			string appID = "";
			string user = "";		// sebelumnya dari fiPhone
			string token = "";
			int amount = 0;
			string cardNumber = "";
			JsonLibs.MyJsonLib additionalData;

			int cardBalance = 0;
			string strxDateTime = "";
			string SamCSN = "";
			string certificate = "";
			string cardChallenge = "";

			//string productCodeTopUpOnline = "PRD00108";
			string productCodeTopUpOnline = commonSettings.getString ("IconoxTopUpClientProductCode");

			Exception xError=null;

			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}

			if(!checkMandatoryFields(jsonConv,fields))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields not found", "");
			}

			try
			{
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
				user = ((string)jsonConv["fiPhone"]).Trim ();
				token = ((string)jsonConv["fiToken"]).Trim ();
				amount = (int)jsonConv["fiAmount"];
				cardNumber = ((string)jsonConv["fiCustomerNumber"]).Trim ();
				additionalData = ((JsonLibs.MyJsonLib)jsonConv["fiAdditional"]);
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
			}

			if(!checkMandatoryFields(additionalData, addFields))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields not found", "");
			}

			try
			{
				cardChallenge = ((string)additionalData["fiCardChallenge"]).Trim ();
				strxDateTime = ((string)additionalData["fiTrxDateTime"]).Trim ();
				cardBalance = (int)additionalData["fiBalance"];
				SamCSN = ((string)additionalData["fiSAMCSN"]).Trim ();
				certificate = ((string)additionalData["fiCertificate"]).Trim ();
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
			}


			DateTime trxDateTime;
			try{
				trxDateTime = DateTime.ParseExact(strxDateTime, "yyMMddHHmmss", null);
			} catch{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field date format", "");
			}

			// cek selisih waktu transaksi dengan sekarang
			DateTime skr = DateTime.Now;
			TimeSpan dtdiff;
			if (trxDateTime > skr)
				dtdiff = trxDateTime - skr;
			else
				dtdiff = skr - trxDateTime;
			if (dtdiff.TotalMinutes > 5) {
				// jika selisih lebih dari 5 menit, kadaluarsa
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "414", 
					"Invalid transaction date range", "");
			}

			ReformatPhoneNumber (ref user);

			string userId = cUserIDHeader + user;

			// cek token disini
			if (!cek_SecurityToken (user, token)) {
				LOG_Handler.LogWriter.showDEBUG (this, "Cek Token Session: " + user + 
					", token: " + token);
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "504", "Invalid session", "");
			}

			CommonLibrary.SessionResetTimeOut (user);

			// cek apakah kartu terdaftar di database
			Exception ExError = null;
			string fReason = "";
			decimal dbBalance = 0;
			DateTime lastModified=DateTime.Now;
			if(!localDB.isCardActivated(cardNumber, ref dbBalance, ref lastModified,
				ref fReason)){
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "415", 
					"TopUp: "+fReason, "");
			}

			// Check Card Balance
			if (amount<=0) {
				// gak boleh negatif
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "417", 
					"TopUp: Invalid TopUp amount", "");
			}

			//int idbBalance = decimal.ToInt32 (decimal.Truncate (dbBalance));
			//if (idbBalance < cardBalance) {
			LogWriter.showDEBUG (this,"\r\nDB Balance = " + dbBalance.ToString () + "\r\n"
				+ "     Card Balance = "+cardBalance.ToString ());
			if (dbBalance < cardBalance) {
				// Update Last card status in DB dengan status blocked
				localDB.updateCardBlocked (cardNumber);

				// terjadi fraud di usercard, perintahkan untuk blok kartu
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "433", 
					"TopUp: Invalid card Balance, BLOCK CARD", "");
			}

			// update balance di db dengan yg terupdate
			//if (lastModified < trxDateTime) {		// harusnya selalu masuk sini, kan online real time
				// Update Last card balance in DB dengan yang terupdate
				if (!localDB.updateCardBalanceInDb (cardNumber, cardBalance, trxDateTime)) {
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "416", 
						"Failed to update usercard balance", "");
				}
			//}

			jsonConv.Clear ();
			// jika fiCertificate = "" maka topup tanpa sign dgn sam krn pake nfc
			if(certificate != "")
				jsonConv.Add ("fiTagCode","01");
			else
				jsonConv.Add ("fiTagCode","05");
			jsonConv.Add ("fiAgentPhone",user);
			jsonConv.Add ("fiDateTime",strxDateTime);
			jsonConv.Add ("fiAmount",amount);
			jsonConv.Add ("fiSAMCSN",SamCSN);
			jsonConv.Add ("fiCertificate",certificate);
			jsonConv.Add ("fiCardNumber",cardNumber);
			jsonConv.Add ("fiUserCardResponse",cardChallenge);

			string strJson = jsonConv.JSONConstruct ();

			string IconoxSvrResp = RequestToIconoxTopUpServer (strJson);

			if (IconoxSvrResp.Length <= 0) {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "413", 
					"No data from Iconox TopUp Server", "");
			}

			jsonConv.Clear();
			if (!jsonConv.JSONParse (IconoxSvrResp)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", 
					"Invalid data format from Iconox TopUp Server", "");
			}

			string respCode = "";
			string respSam = "";
			if ((!jsonConv.ContainsKey("fiResponseCode")) || 
				(!jsonConv.ContainsKey("fiResponseMessage")) || 
				(!jsonConv.ContainsKey("fiTagCode")))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "416", 
					"Mandatory field from TopUp server not found", "");
			}
			respCode = ((string)jsonConv["fiResponseCode"]).Trim();
			if(respCode!="00"){
				respSam = "Iconox server message: " + ((string)jsonConv["fiResponseMessage"]).Trim ();
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "I"+respCode, 
					respSam, "");
			}
			if (!jsonConv.ContainsKey("fiServerSAMResponse"))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "416", 
					"Iconox server: no SAM response", "");
			}

			//trxRecTime = DateTime.Now;

			int traceNumber = localDB.getNextProductTraceNumber();
			string trxNumber = localDB.getProductTrxNumber(out xError);
			long TransactionRef_id = localDB.getTransactionReffIdSequence(out xError);
			respSam = ((string)jsonConv["fiServerSAMResponse"]).Trim ();

			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
			try
			{
				providerProduct = localDB.getProviderProductInfo(productCodeTopUpOnline, out xError);
				if (xError != null)
				{
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider product data not found", "");
				}
			}
			catch(Exception ex)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error get provider product data : " + ex.getCompleteErrMsg());
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting provider product data", "");
			}

			// FIXME JANG DEMO konek langsung bae ka Power-T, harusnya dilakukan settlement perwaktu tertentu
			// tembak ka power-T

			int nilaiYangMasukLog = 0;//	providerProduct.CurrentPrice;
			DateTime skrg = DateTime.Now;
			int topUpFee = 0;

			// ieu mah hitungan jang purchase						100        99           1
			string strTemp = hitungFeeTrxKartu (providerProduct, amount, ref nilaiYangMasukLog, ref topUpFee);
			if (strTemp != "") {
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get topup fee data product code: " + productCodeTopUpOnline);
				return strTemp;
			}

			string qvaInvoiceNumber = "";
			bool qvaReversalDone = false;
			string errCode = "00"; string errMessage = "";

			// amountFinal adalah nilai yang harus di transferkan dari petugas topup ke rekening penampungan
			strTemp = bayarDariPetugasKePenampung(userId, providerProduct,
				amount, TransactionRef_id, 
				ref qvaInvoiceNumber, ref qvaReversalDone, 
				ref errCode, ref errMessage);
			if (strTemp != "") {
				return strTemp;
			}

			// Update Last card balance in DB dengan total topup dan balance sebelumnya
			int totalBalance = amount + cardBalance;
			if(!localDB.updateCardBalanceInDb(cardNumber, totalBalance, trxDateTime)){
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "416", 
					"Failed to update usercard balance", "");
			}


			// insert log transaksi
			if (!localDB.insertCompleteTransactionLog (TransactionRef_id, productCodeTopUpOnline, 
				providerProduct.ProviderProductCode,
				userId.Substring (commonSettings.getString ("UserIdHeader").Length), cardNumber,
				nilaiYangMasukLog.ToString (), traceNumber.ToString (), trxDateTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				topUpFee.ToString (), providerProduct.ProviderCode, providerProduct.CogsPriceId,
				cardBalance, totalBalance, "", skrg.ToString ("yyyy-MM-dd HH:mm:ss"), "", 
				skrg.ToString ("yyyy-MM-dd HH:mm:ss"),
				strJson,
				trxDateTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				IconoxSvrResp,
				skrg.ToString ("yyyy-MM-dd HH:mm:ss"),
				true, 
				"", trxNumber, false, providerProduct.fIncludeFee, "", "",
				out xError)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Failed to save transaction log", "");
			}

			// insert log ucard_transaction
			if (!localDB.addCardTransactionLog (TransactionRef_id,"", "", cardBalance, appID,
				out xError)) {
				// sudah di catat
			}

			respSam = ((string)jsonConv["fiServerSAMResponse"]).Trim ();

			jsonConv.Clear();
			jsonConv.Add("fiToken", token);
			jsonConv.Add("fiPrivateData", respSam);
			jsonConv.Add("fiResponseCode", respCode);
			jsonConv.Add("fiTransactionId", "Icx" + traceNumber.ToString().PadLeft(6, '0'));
			jsonConv.Add("fiTrxNumber", trxNumber);
			jsonConv.Add("fiReversalAllowed", false);
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

		}


	}
}

