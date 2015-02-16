
// KISS ===>>> KEEP IT SIMPLE... STUPID

using System;
using PPOBHttpRestData;
using System.Collections;
using System.Collections.Generic;
using LOG_Handler;
using StaticCommonLibrary;
using IconoxOnlineHandler;
using PPOBDatabase;

using System.Globalization;

namespace IconoxTrxHandlerV2
{
	public class IconoxTrxV2 : IDisposable {
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
		~IconoxTrxV2()
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



		HTTPRestConstructor HTTPRestDataConstruct;
		JsonLibs.MyJsonLib jsonConv;
		PublicSettings.Settings commonSettings;
		PPOBDatabase.PPOBdbLibs localDB;
		//Exception xError;

		// Additional Data berupa format json, dengan isi:
		//		TOPUP/PURCHASE	fiUserCardNumber
		//						fiTrxDateTime
		//						fiSAMCSN
		//						fiCertificate
		public JsonLibs.MyJsonLib AdditionalJson=null;

		public string securityToken = "";
		public string agentPhone="";
		public string providerCode="";
		public int trxAmount=0;
		public bool sudahBayar=false;
		public HTTPRestConstructor.HttpRestRequest clientData;

		string cUserIDHeader = "";


		public IconoxTrxV2 (HTTPRestConstructor.HttpRestRequest ClientData,
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

		private bool cek_SecurityToken(string userPhone, string token)
		{
			// cek detek sessionnya
			return CommonLibrary.isSessionExist (userPhone, token);
		}

		/*============================================================================*/
		/*============================================================================*/

		#endregion


		#region Akses Iconox Server
		/*============================================================================*/
		/*   Kumpulan fungsi standar */
		/*============================================================================*/


		private string RequestToIconoxAuthenticationServer(string msg){
			return RequestToIconoxTopUpServer(msg);
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

		/*============================================================================*/
		/*============================================================================*/

		#endregion


		decimal topUpPercentFee = 0;

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

		private string hitungFeeProductService(PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct, 
			int adminFee, ref int productAmount, ref int nilaiYangMasukLog){
			if (providerProduct.ProductType == PPOBDatabase.PPOBdbLibs.ProductTypeEnum.PRODUCT)
			{
				if (providerProduct.fIncludeFee)
				{
					productAmount = providerProduct.CurrentPrice;
					nilaiYangMasukLog = productAmount - adminFee;
				}
				else
				{
					productAmount = providerProduct.CurrentPrice + adminFee;
					nilaiYangMasukLog = providerProduct.CurrentPrice;
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
			return "";
		}

//		private bool getBaseAndFeeAmountFromProduct(string productCode, string appID,	//string providerCode, 
//			ref int adminFee, int TotalAmount = 0)
//		{
//			Exception xError = null;
//			//if (!localDB.getAdminFeeAndCustomerFee(productCode, providerCode, TotalAmount,
//			if (!localDB.getAdminFeeAndCustomerFee(productCode, appID, TotalAmount,
//				ref adminFee, out xError))
//			{
//				return false;
//			}
//			return true;
//		}

		private string bayarDariPetugasTopUpKePenampung(string userId, 
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

		private string bayarDariPetugasAutentikasiKePenampung(string userId, 
			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct,
			int amount, long TransactionRef_id, 
			ref string qvaInvoiceNumber, ref bool qvaReversalDone, 
			ref string errCode, ref string errMessage
		){

			int PpobType = 1;	// anggap pembelian
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

		public string DoTopUp(){
			string[] fields = { "fiApplicationId", "fiPhone", "fiToken", 
				"fiAdditional", "fiAmount", "fiUserCardNumber"};

			string[] addFields = { "fiBalance", "fiTrxDateTime", "fiSAMCSN",
				"fiCertificate", "fiCardChallenge"};
//			string[] addFields = { "fiBalance", "fiTrxDateTime", 
//				"fiCertificate", "fiCardChallenge"};

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
			//string outletCode = "";

			string productCode = commonSettings.getString ("IconoxTopUpClientProductCode");

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
				cardNumber = ((string)jsonConv["fiUserCardNumber"]).Trim ();
				additionalData = ((JsonLibs.MyJsonLib)jsonConv["fiAdditional"]);
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
			}

//			if (!additionalData.isExists ("fiOutletCode")) {
//				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory outletcode fields not found", "");
//			}

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
				//outletCode = ((string)jsonConv["fiOutletCode"]).Trim ();
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

//			// cek selisih waktu transaksi dengan sekarang
//			DateTime skr = DateTime.Now;
//			TimeSpan dtdiff;
//			if (trxDateTime > skr)
//				dtdiff = trxDateTime - skr;
//			else
//				dtdiff = skr - trxDateTime;
//			if (dtdiff.TotalMinutes > 5) {
//				// jika selisih lebih dari 5 menit, kadaluarsa
//				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "414", 
//					"Invalid transaction date range", "");
//			}

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
			//Exception ExError = null;
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

//			int idbBalance = decimal.ToInt32 (decimal.Truncate (dbBalance));
//			if (idbBalance < cardBalance) {
			LogWriter.showDEBUG (this,"\r\nDB Balance = " + dbBalance.ToString () + "\r\n"
				+ "     Card Balance = "+cardBalance.ToString ());
			if (dbBalance < cardBalance) {
				// Update Last card status in DB dengan status blocked
				localDB.updateCardBlocked (cardNumber);

				// terjadi fraud di usercard, perintahkan untuk blok kartu
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "433", 
					"TopUp: Invalid card Balance, BLOCK CARD", "");
			}

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

			// DISINI KURANGI QVA petugas topup
			//			int cardProductAmount = amount;	// 100% nya
			//int productAmountDenganKartu=0;
			int nilaiYangMasukLog = 0;//	providerProduct.CurrentPrice;

			DateTime skrg = DateTime.Now;


			// hitung fee
			//			int vaTopUp = 0;
//			int adminFee = 0;
//			int amountFinal = 0;
			int topUpFee = 0;

			// ieu mah hitungan jang purchase						100        99           1
			string strTemp = hitungFeeTrxKartu (providerProduct, amount, ref nilaiYangMasukLog, ref topUpFee);
			if (strTemp != "") {
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get topup fee data product code: " + productCode);
				return strTemp;
			}

//			// ambil base admin fee
//			try {
//				//LogWriter.showDEBUG (this, " productAmount: " + productAmount);
//				if (!getBaseAndFeeAmountFromProduct (productCode, providerProduct.ProviderCode,
//					ref adminFee, vaTopUp)) {
//					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Fee data not found", "");
//				}
//			} catch (Exception ex) {
//				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get fee data : " + ex.getCompleteErrMsg ());
//				//Console.WriteLine(ex.StackTrace);
//				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get fee data", "");
//			}
			//			amountFinal = vaTopUp;
//
//			// hitung fee include/exclude
//			strTemp = hitungFeeProductService (providerProduct, adminFee, ref amountFinal, ref nilaiYangMasukLog);
//			if (strTemp != "") {
//				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Bad topup fee value");
//				return strTemp;
//			}

			string qvaInvoiceNumber = "";
			bool qvaReversalDone = false;
			string errCode = "00"; string errMessage = "";

			// amountFinal adalah nilai yang harus di transferkan dari petugas topup ke rekening penampungan
			strTemp = bayarDariPetugasTopUpKePenampung(userId, providerProduct,
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
			if (!localDB.insertCompleteTransactionLog (TransactionRef_id, productCode, 
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
				//LogWriter.showDEBUG (this, "=========== GAGAL INSERT LOG ======");
				// return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Failed to save transaction log", "");
				// Dicatat di Log saja, untuk di insert di lain waktu, karena transaksi sudah terjadi
			}

			// insert log ucard_transaction
			if (!localDB.addCardTransactionLog (TransactionRef_id, "", "", cardBalance, appID,
				out xError)) {
				// sudah di catat
				//LogWriter.showDEBUG (this, "=========== GAGAL INSERT LOG KARTU ======");
			}

			respSam = ((string)jsonConv["fiServerSAMResponse"]).Trim ();

			jsonConv.Clear();
			//jsonConv.Add("fiToken", token);
			jsonConv.Add("fiPrivateData", respSam);
			jsonConv.Add("fiResponseCode", respCode);
			jsonConv.Add("fiTransactionId", "Icx" + traceNumber.ToString().PadLeft(6, '0'));
			jsonConv.Add("fiTrxNumber", trxNumber);
			jsonConv.Add("fiReversalAllowed", false);
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

		}

		public string DoAuthentication(){
			string[] fields = { "fiApplicationId", "fiPhone", "fiToken", 
				"fiUserCardNumber", "fiTrxDateTime", "fiCardChallenge", "fiProductCode",
				"fiFileCode", "fiKCRUDAD"};

			string appID = "";
			string userPhone = "";
			string token = "";
			string cardNumber = "";
			string strxDateTime = "";
			string cardChallenge = "";
			string productCode = "";
			string iconoxFileCode = "";
			string iconoxKcrudad = "";

			//productCode = commonSettings.getString ("IconoxActivationClientProductCode");

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
				userPhone = ((string)jsonConv["fiPhone"]).Trim ();
				token = ((string)jsonConv["fiToken"]).Trim ();
				cardNumber = ((string)jsonConv["fiUserCardNumber"]).Trim ();
				cardChallenge = ((string)jsonConv["fiCardChallenge"]).Trim ();
				strxDateTime = ((string)jsonConv["fiTrxDateTime"]).Trim ();
				productCode = ((string)jsonConv["fiProductCode"]).Trim ();
				iconoxFileCode = ((string)jsonConv["fiFileCode"]).Trim ();
				iconoxKcrudad = ((string)jsonConv["fiKCRUDAD"]).Trim ();
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

			ReformatPhoneNumber (ref userPhone);

			string userId = cUserIDHeader + userPhone;

			// cek token disini
			if (!cek_SecurityToken (userPhone, token)) {
				LOG_Handler.LogWriter.showDEBUG (this, "Cek Token Session: " + userPhone + 
					", token: " + token);
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "504", "Invalid session", "");
			}

			CommonLibrary.SessionResetTimeOut (userPhone);

			// cek apakah kartu terdaftar di database
			//Exception ExError = null;
			string fReason = "";
			decimal dbBalance = 0;
			DateTime lastModified=DateTime.Now;
			dbCardStatus cardStatus = localDB.getCardStatus (cardNumber);
			switch (cardStatus) {
//			case dbCardStatus.Actived:
//				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "415", 
//					"Card Status: Already active");
//				break;
			case dbCardStatus.Blocked:
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "415", 
					"Card Status: Blocked","");
			case dbCardStatus.Undistributed:
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "415", 
					"Card Status: Hasn't been distributed","");
			case dbCardStatus.dbFailed:
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "415", 
					"Card Status: Failed to query","");
			case dbCardStatus.Unregistered:
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "415", 
					"Card Status: Unregistered","");
			default:
//				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "415", 
//					"Card Status: Unknown","");
				break;
			}
				
			jsonConv.Clear ();
			// jika fiCertificate = "" maka topup tanpa sign dgn sam krn pake nfc
			//if(certificate != "")
			jsonConv.Add ("fiTagCode","06");
			//			else
			//				jsonConv.Add ("fiTagCode","05");
			jsonConv.Add ("fiAgentPhone",userPhone);
			jsonConv.Add ("fiDateTime",strxDateTime);
			jsonConv.Add ("fiCardNumber",cardNumber);
			jsonConv.Add ("fiUserCardResponse",cardChallenge);
			jsonConv.Add ("fiUserFileCode",iconoxFileCode);
			jsonConv.Add ("fiKCRUDAD",iconoxKcrudad);

			string strJson = jsonConv.JSONConstruct ();

			string IconoxSvrResp = RequestToIconoxAuthenticationServer(strJson);

			if (IconoxSvrResp.Length <= 0) {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "413", 
					"No data from Iconox Activation Server", "");
			}

			jsonConv.Clear();
			if (!jsonConv.JSONParse (IconoxSvrResp)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", 
					"Invalid data format from Iconox Activation Server", "");
			}

			string respCode = "";
			string respSam = "";
			if ((!jsonConv.ContainsKey("fiResponseCode")) || 
				(!jsonConv.ContainsKey("fiResponseMessage")) || 
				(!jsonConv.ContainsKey("fiTagCode")))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "416", 
					"Mandatory field from Activation server not found", "");
			}
			respCode = ((string)jsonConv["fiResponseCode"]).Trim();
			if(respCode!="00"){
				respSam = "Iconox server message: " + ((string)jsonConv["fiResponseMessage"]).Trim ();
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "I"+respCode, 
					respSam, "");
			}
			if (!jsonConv.ContainsKey("fiSAMResponse"))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "416", 
					"Iconox server: no SAM response", "");
			}

			//trxRecTime = DateTime.Now;

			int traceNumber = localDB.getNextProductTraceNumber();
			string trxNumber = localDB.getProductTrxNumber(out xError);
			long TransactionRef_id = localDB.getTransactionReffIdSequence(out xError);
			respSam = ((string)jsonConv["fiSAMResponse"]).Trim ();

			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
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

			// DISINI KURANGI QVA petugas topup
			//			int cardProductAmount = amount;	// 100% nya
			//int productAmountDenganKartu=0;
			int nilaiYangMasukLog = 0;//	providerProduct.CurrentPrice;

			DateTime skrg = DateTime.Now;


			// hitung fee
			//			int vaTopUp = 0;
			//			int adminFee = 0;
			//			int amountFinal = 0;
			int adminFee = 0;

			// ambil base admin fee
			try {
				//LogWriter.showDEBUG (this, " productAmount: " + productAmount);
				if (!localDB.getAdminFeeAndCustomerFee(productCode, 1, appID, providerProduct.CurrentPrice,
					ref adminFee, out xError))
				{
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Fee data not found", "");
				}

			} catch (Exception ex) {
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get fee data : " + ex.getCompleteErrMsg ());
				//Console.WriteLine(ex.StackTrace);
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get fee data", "");
			}
			int amountFinal = providerProduct.CurrentPrice;

			// hitung fee include/exclude
			string strTemp = hitungFeeProductService (providerProduct, adminFee, ref amountFinal, ref nilaiYangMasukLog);
			if (strTemp != "") {
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Bad amount value");
				return strTemp;
			}

			string qvaInvoiceNumber = "";
			bool qvaReversalDone = false;
			string errCode = "00"; string errMessage = "";

			// amountFinal adalah nilai yang harus di transferkan dari petugas ke rekening penampungan
			strTemp = bayarDariPetugasAutentikasiKePenampung(userId, providerProduct,
				amountFinal, TransactionRef_id, 
				ref qvaInvoiceNumber, ref qvaReversalDone, 
				ref errCode, ref errMessage);
			if (strTemp != "") {
				return strTemp;
			}

			// insert log transaksi
			if (!localDB.insertCompleteTransactionLog (TransactionRef_id, productCode, 
				providerProduct.ProviderProductCode,
				userId.Substring (commonSettings.getString ("UserIdHeader").Length), cardNumber,
				nilaiYangMasukLog.ToString (), traceNumber.ToString (), trxDateTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				adminFee.ToString (), providerProduct.ProviderCode, providerProduct.CogsPriceId,
				0, 0, "", skrg.ToString ("yyyy-MM-dd HH:mm:ss"), "", 
				skrg.ToString ("yyyy-MM-dd HH:mm:ss"),
				strJson,
				trxDateTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				IconoxSvrResp,
				skrg.ToString ("yyyy-MM-dd HH:mm:ss"),
				true, 
				"", trxNumber, false, providerProduct.fIncludeFee, "", "",
				out xError)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Failed to save transaction log", "");
				//LogWriter.showDEBUG (this, "=========== GAGAL INSERT LOG ======");
				// return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Failed to save transaction log", "");
				// Dicatat di Log saja, untuk di insert di lain waktu, karena transaksi sudah terjadi
			}

			jsonConv.Clear();
			//jsonConv.Add("fiToken", token);
			jsonConv.Add("fiAuthCode", respSam);
			jsonConv.Add("fiResponseCode", respCode);
			jsonConv.Add("fiTransactionId", "Icx" + traceNumber.ToString().PadLeft(6, '0'));
			jsonConv.Add("fiTrxNumber", trxNumber);
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

		}


	}
}

