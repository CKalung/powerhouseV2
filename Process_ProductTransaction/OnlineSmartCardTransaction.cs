using System;
using System.Globalization;

using PPOBHttpRestData;
using System.Collections;
using System.Collections.Generic;
using IconoxOnlineHandler;
using StaticCommonLibrary;
using LOG_Handler;

namespace Process_ProductTransaction
{
	public class OnlineSmartCardTransaction : IDisposable {
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
		~OnlineSmartCardTransaction()
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

		/*============================================================================*/
		/*============================================================================*/

		#endregion


		public OnlineSmartCardTransaction (HTTPRestConstructor.HttpRestRequest ClientData,
			PublicSettings.Settings CommonSettings)
		{
			clientData = ClientData;
			commonSettings = CommonSettings;
			HTTPRestDataConstruct = new HTTPRestConstructor();
			jsonConv = new JsonLibs.MyJsonLib();
			localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
				commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
		}

		// Disini sudah gak perlu pake protokol deui
		private string RequestToIconoxServer(string msg){
			using (IconoxTcpClient tcpI = new IconoxTcpClient (commonSettings.getString ("IconoxQueuePaymentHost"),
				commonSettings.getInt ("IconoxQueuePaymentPort"))) {
				//Console.WriteLine ("Connecting to iconox server...");

				if (!tcpI.Connect ()) {
					//Console.WriteLine ("Gagal konek ke iconox server...");
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

		public string IconoxInitPayment(){
			string[] fields = { "fiApplicationId", "fiUsercardResponse",  "fiPhone", "fiKeyAddress", "fiToken"};

			string appID = "";
			string userCardResponse = "";
			string userPhone = "";
			string securityToken = "";
			string keyAddress = "";

			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}

			if(!checkMandatoryFields(fields))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields not found", "");
			}

			try
			{
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
				userPhone = ((string)jsonConv["fiPhone"]).Trim ();
				userCardResponse = ((string)jsonConv["fiUsercardResponse"]).Trim ();
				securityToken = ((string)jsonConv["fiToken"]).Trim ();
				keyAddress = ((string)jsonConv["fiKeyAddress"]).Trim ();
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
			}

			ReformatPhoneNumber (ref userPhone);

			if (!cek_SecurityToken (userPhone, securityToken)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "504", "Invalid session or security", "");
			}

			// konek ka server iconox online payment
			jsonConv.Clear ();
			// jika fiCertificate = "" maka topup tanpa sign dgn sam krn pake nfc
			jsonConv.Add ("fiTagCode","01");		// payment Challenge
			jsonConv.Add ("fiAgentPhone",userPhone);
			//jsonConv.Add ("fiDateTime",fiStrTrxDateTime);
			//jsonConv.Add ("fiKeyAddress",commonSettings.getString ("IconoxEwallet1-KeyAddress"));
			jsonConv.Add ("fiKeyAddress",keyAddress);
			jsonConv.Add ("fiResponseUserCard",userCardResponse);

			string IconoxSvrResp = RequestToIconoxServer (jsonConv.JSONConstruct ());

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

			string respCodeSvr = ((string)jsonConv["fiResponseCode"]).Trim ();
			string respMsgSvr = ((string)jsonConv["fiResponseMessage"]).Trim ();
			string respPSAMResp = ((string)jsonConv["fiPaySAMResponse"]).Trim ();

			jsonConv.Clear();
			jsonConv.Add ("fiPSAMAuthorization", respPSAMResp);
			jsonConv.Add ("fiResponseMessage", respMsgSvr);
			jsonConv.Add ("fiResponseCode",respCodeSvr);
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct ());
		}


		public string IconoxConfirmPayment(){
			string[] fields = { "fiApplicationId", "fiUsercardResponse",  "fiPhone"};

			string appID = "";
			string userCardResponse = "";
			string userPhone = "";

			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}

			if(!checkMandatoryFields(fields))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields not found", "");
			}

			try
			{
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
				userPhone = ((string)jsonConv["fiPhone"]).Trim ();
				userCardResponse = ((string)jsonConv["fiUsercardResponse"]).Trim ();
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
			}

			ReformatPhoneNumber (ref userPhone);

			// konek ka server iconox online payment
			jsonConv.Clear ();
			// jika fiCertificate = "" maka topup tanpa sign dgn sam krn pake nfc
			jsonConv.Add ("fiTagCode","02");		// payment Challenge
			jsonConv.Add ("fiAgentPhone",userPhone);
			//jsonConv.Add ("fiDateTime",fiStrTrxDateTime);
			jsonConv.Add ("fiKeyAddress",commonSettings.getString ("IconoxEwallet1-KeyAddress"));
			jsonConv.Add ("fiResponseUserCard",userCardResponse);

			string IconoxSvrResp = RequestToIconoxServer (jsonConv.JSONConstruct ());

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

			// Disini Cek responscode dari iconox server, jika oke "00" maka lanjut
			// transfer qva dari titipan ke rekening user, sebesar amount - topup fee
			// dan simpan di log sebagai transaksi payment dengan include fee

			Exception xError = null;
			long TransactionRef_id = localDB.getTransactionReffIdSequence(out xError);

			string respCodeSvr = ((string)jsonConv["fiResponseCode"]).Trim ();
			string respMsgSvr = ((string)jsonConv["fiResponseMessage"]).Trim ();
			string respPSAMResp = ((string)jsonConv["fiPaySAMResponse"]).Trim ();

			jsonConv.Clear();
			//jsonConv.Add ("fiPSAMAuthorization", respPSAMResp);
			jsonConv.Add ("fiResponseMessage", respMsgSvr);
			jsonConv.Add ("fiResponseCode",respCodeSvr);
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct ());
		}

		private bool cek_SecurityToken(string userPhone, string token)
		{
			// cek detek sessionnya
			return CommonLibrary.isSessionExist (userPhone, token);
		}

		private void sendToPowerT(){
			// TODO : Pengiriman data transaksi ke PowerT
		}

		public string IconoxConfirmPaymentWithLogBlm(string ProductCode){
			string[] fields = { "fiApplicationId", "fiCardNumber", "fiCardBalance", "fiUsercardResponse", "fiToken",
				"fiPhone", "fiTotalAmount", "fiTrxDateTime"};

			string appID = "";
			string userCardResponse = "";
			string userPhone = "";
			int hargaTransaksi = 0;
			string fiTrxTime = "";
			string strJson = "";
			int cardBalance = 0;
			string securityToken = "";
			string HttpReply = ""; 
			string cardNumber = "";

			Exception xError = null;
			string productCode = ProductCode;	//commonSettings.getString ("ProductCode_BPJS_Multipayment");

			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}
			strJson = clientData.Body;

			if(!checkMandatoryFields(fields))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields not found", "");
			}

			try
			{
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
				userPhone = ((string)jsonConv["fiPhone"]).Trim ();
				userCardResponse = ((string)jsonConv["fiUsercardResponse"]).Trim ();
				fiTrxTime = ((string)jsonConv["fiTrxTime"]).Trim ();
				hargaTransaksi = (int)jsonConv["fiTotalAmount"];
				cardBalance = (int)jsonConv["fiCardBalance"];
				securityToken = ((string)jsonConv["fiToken"]).Trim();
				cardNumber = ((string)jsonConv["fiCardNumber"]).Trim();
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
			}

			ReformatPhoneNumber (ref userPhone);

			if (!cek_SecurityToken (userPhone, securityToken)) {
				LOG_Handler.LogWriter.showDEBUG (this, "Cek Token Session: " + userPhone + 
					", token: " + securityToken);
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "504", "Invalid session", "");
			}

			string formatDate = "yyMMddHHmmss";
			DateTime fiTrxDateTime;
			CultureInfo provider = CultureInfo.InvariantCulture;

			try{
				fiTrxDateTime = DateTime.ParseExact( fiTrxTime, formatDate, provider);
			}
			catch{
				return  HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					"Invalid datetime format", "");
			}

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

			string userId = commonSettings.getString("UserIdHeader") + userPhone;
			// cek keberadaan user di database
			if (!localDB.isAccountExistById(userId, out xError))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "493", "No user found", "");
			}

			// konek ka server iconox online payment
			jsonConv.Clear ();
			// jika fiCertificate = "" maka topup tanpa sign dgn sam krn pake nfc
			jsonConv.Add ("fiTagCode","02");		// payment Challenge
			jsonConv.Add ("fiAgentPhone",userPhone);
			//jsonConv.Add ("fiDateTime",fiStrTrxDateTime);
			jsonConv.Add ("fiKeyAddress",commonSettings.getString ("IconoxEwallet1-KeyAddress"));
			jsonConv.Add ("fiResponseUserCard",userCardResponse);

			string IconoxSvrResp = RequestToIconoxServer (jsonConv.JSONConstruct ());

			string failedReason = "";
			if (IconoxSvrResp.Length <= 0) {
				failedReason = "No data from Iconox Payment Server";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "413", 
					failedReason, "");
				//return HttpReply;
			}

			if (HttpReply == "") {
				jsonConv.Clear ();
				if (!jsonConv.JSONParse (IconoxSvrResp)) {
					failedReason = "Invalid data format from Iconox Server";
					HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", 
						failedReason, "");
					//return HttpReply;
				}
			}

			string respCodeSvr = "";
			string respMsgSvr = "";
			string respPSAMResp = "";
			string trxUCardLog = "";
			if (HttpReply == "") {
				try {
					respCodeSvr = ((string)jsonConv ["fiResponseCode"]).Trim ();
					respMsgSvr = ((string)jsonConv ["fiResponseMessage"]).Trim ();
					respPSAMResp = ((string)jsonConv ["fiPaySAMResponse"]).Trim ();
					trxUCardLog = ((string)jsonConv ["fiLastTransactionLog"]).Trim ();
				} catch (Exception ex) {
					failedReason = "Incomplete fields from payment server";
					LogWriter.write (this, LogWriter.logCodeEnum.ERROR, failedReason + ": " + ex.getCompleteErrMsg ());
					HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse (400, "492",
						failedReason, "");
				}
			}

			// Urusan Transfer QVA



			long TransactionRef_id = localDB.getTransactionReffIdSequence(out xError);
			string trxNumber = localDB.getProductTrxNumber(out xError);

			int traceNumber = localDB.getNextProductTraceNumber();
			//long reffNum = localDB.getNextProductReferenceNumber();
			int adminFee = 0;
			DateTime skrg = DateTime.Now;
			string strRecJson = IconoxSvrResp;
			DateTime trxRecTime = skrg;

			// TODO : Disini perlu di cek jika transaksi sudah tercatat, krn meski payment online, 
			// tapi perilaku seperti transksi offline 
			// dimana bisa terjadi pengulangan pengiriman data transaksi yang sama dari client

			//ASUPKEUN ka table transaction dan ucard_transaction
			if (!localDB.insertCompleteTransactionLog (TransactionRef_id, productCode, providerProduct.ProviderProductCode,
				userPhone, cardNumber,	// userCardResponse,
				hargaTransaksi.ToString (), traceNumber.ToString (), fiTrxDateTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				adminFee.ToString (), providerProduct.ProviderProductCode, providerProduct.CogsPriceId,
				0, 0, "", skrg.ToString ("yyyy-MM-dd HH:mm:ss"), "", skrg.ToString ("yyyy-MM-dd HH:mm:ss"),
				strJson,
				fiTrxDateTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				strRecJson,
				trxRecTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				true, 
				failedReason, trxNumber, false, providerProduct.fIncludeFee,
				"", "", out xError)) {
				// Jadwalkan masuk database
				//return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492",
				//	"Failed to save transaction log", "");
			}

			// cek offline_transaction log
			string dbtraceNum = "";
			bool sudahTerjadiTrx = false;		// Jika sudah pernah masuk database
			xError = null;

			if (localDB.isOfflineTransactionExist (userPhone, appID, productCode, fiTrxDateTime,
				   1, ref dbtraceNum, true, out xError)) {
				sudahTerjadiTrx = true;
			}
			if (xError != null) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "482",
					"Failed to access database", "");
			}

			// ASUPKEUN ka table offline_transaction
			if (sudahTerjadiTrx) {
				if (!localDB.updateOfflineTransactionLog (userPhone, appID, productCode, fiTrxDateTime, 1, 
					true, true, "00","Success",true, out xError)) {
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492",
						"Failed to update transaction log", "");
				}
			} else {
				if (!localDB.saveOfflineTransaction (userPhone, appID, productCode, fiTrxDateTime, 1, 
					   hargaTransaksi, "PowerHouse", traceNumber, true, out xError)) {
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492",
						"Failed to save transaction log", "");
				}
				// TODO : disini transfer fee nya
				// Disini Cek responscode dari iconox server, jika oke "00" maka lanjut
				// transfer qva dari titipan ke rekening user, sebesar amount - topup fee
				// dan simpan di log sebagai transaksi payment dengan include fee


			}

			string SamCSN = "";	// untuk purchase online
			string OutletCode = "";	// untuk purchase online
			// masukkeun ditable ucard_transaction
			if (!localDB.addCardTransactionLog (TransactionRef_id, OutletCode,trxUCardLog,cardBalance,
				out xError)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492",
					"Failed to save card transaction log", "");
			}

			if (HttpReply != "") {
				return HttpReply;
			}

			// TODO : DIsini DIJADWALKAN pengiriman data ke POWER-T (PowerHouse keneh) 
			// dilakukan settlement perwaktu tertentu tembak ka power-T

			sendToPowerT ();

			jsonConv.Clear();
			//jsonConv.Add ("fiPSAMAuthorization", respPSAMResp);
			jsonConv.Add ("fiResponseMessage", respMsgSvr);
			jsonConv.Add ("fiResponseCode",respCodeSvr);
			jsonConv.Add ("fiTransactionId", "OIP" + traceNumber.ToString().PadLeft(6, '0'));
			jsonConv.Add ("fiTrxNumber", trxNumber);
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct ());
		}

		public string IconoxConfirmPaymentWithLog(string ProductCode){
			string[] fields = { "fiApplicationId", "fiCardNumber", "fiCardBalance", "fiUsercardResponse", "fiToken",
				"fiPhone", "fiTotalAmount", "fiTrxDateTime"};

			string appID = "";
			string userCardResponse = "";
			string userPhone = "";
			int hargaTransaksi = 0;
			string fiTrxTime = "";
			string strJson = "";
			int cardBalance = 0;
			string securityToken = "";
			string HttpReply = ""; 
			string cardNumber = "";

			Exception xError = null;
			string productCode = ProductCode;	//commonSettings.getString ("ProductCode_BPJS_Multipayment");

			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}
			strJson = clientData.Body;

			if(!checkMandatoryFields(fields))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields not found", "");
			}

			try
			{
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
				userPhone = ((string)jsonConv["fiPhone"]).Trim ();
				userCardResponse = ((string)jsonConv["fiUsercardResponse"]).Trim ();
				fiTrxTime = ((string)jsonConv["fiTrxTime"]).Trim ();
				hargaTransaksi = (int)jsonConv["fiTotalAmount"];
				cardBalance = (int)jsonConv["fiCardBalance"];
				securityToken = ((string)jsonConv["fiToken"]).Trim();
				cardNumber = ((string)jsonConv["fiCardNumber"]).Trim();
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
			}

			ReformatPhoneNumber (ref userPhone);

			if (!cek_SecurityToken (userPhone, securityToken)) {
				LOG_Handler.LogWriter.showDEBUG (this, "Cek Token Session: " + userPhone + 
					", token: " + securityToken);
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "504", "Invalid session", "");
			}

			string formatDate = "yyMMddHHmmss";
			DateTime fiTrxDateTime;
			CultureInfo providerDtFormat = CultureInfo.InvariantCulture;

			try{
				fiTrxDateTime = DateTime.ParseExact( fiTrxTime, formatDate, providerDtFormat);
			}
			catch{
				return  HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					"Invalid datetime format", "");
			}

			string userId = commonSettings.getString("UserIdHeader") + userPhone;

			// cek di data offline transaction, krn meski payment online, tapi sifatnya offline card payment
			Exception ExError = null;
			string dbTraceNum = "";
			if (localDB.isOfflineTransactionExist(userPhone, appID, productCode, fiTrxDateTime, 
				1,ref dbTraceNum,  true, out ExError))
			{
				// anggap sukses
				//token = CommonLibrary.RenewTokenSession(userPhone);
				jsonConv.Clear();
				jsonConv.Add ("fiResponseCode", "00");
				jsonConv.Add ("fiResponseMessage", "Success");
				//jsonConv.Add ("fiTransactionId", "OIP" + traceNumber.ToString().PadLeft(6, '0'));
				jsonConv.Add ("fiTrxNumber", dbTraceNum);
				return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
				// return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Data already exist", "");
			}
			if (ExError != null)
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Can not access database", "");
			}


			// konek ka server iconox online payment
			jsonConv.Clear ();
			// jika fiCertificate = "" maka topup tanpa sign dgn sam krn pake nfc
			jsonConv.Add ("fiTagCode","02");		// payment Challenge
			jsonConv.Add ("fiAgentPhone",userPhone);
			jsonConv.Add ("fiKeyAddress",commonSettings.getString ("IconoxEwallet1-KeyAddress"));
			jsonConv.Add ("fiResponseUserCard",userCardResponse);

			string IconoxSvrResp = RequestToIconoxServer (jsonConv.JSONConstruct ());

			string failedReason = "";
			if (IconoxSvrResp.Length <= 0) {
				failedReason = "No data from Iconox Payment Server";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "413", 
					failedReason, "");
				//return HttpReply;
			}

			if (HttpReply == "") {
				jsonConv.Clear ();
				if (!jsonConv.JSONParse (IconoxSvrResp)) {
					failedReason = "Invalid data format from Iconox Server";
					HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", 
						failedReason, "");
					//return HttpReply;
				}
			}

			string respCodeSvr = "";
			string respMsgSvr = "";
			string respPSAMResp = "";
			string trxUCardLog = "";
			if (HttpReply == "") {
				try {
					respCodeSvr = ((string)jsonConv ["fiResponseCode"]).Trim ();
					respMsgSvr = ((string)jsonConv ["fiResponseMessage"]).Trim ();
					respPSAMResp = ((string)jsonConv ["fiPaySAMResponse"]).Trim ();
					trxUCardLog = ((string)jsonConv ["fiLastTransactionLog"]).Trim ();
				} catch (Exception ex) {
					failedReason = "Incomplete fields from payment server";
					LogWriter.write (this, LogWriter.logCodeEnum.ERROR, failedReason + ": " + ex.getCompleteErrMsg ());
					HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse (400, "492",
						failedReason, "");
				}
			}

			// Urusan Transfer QVA


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
			decimal topUpPercentFee = 0;

			// Ambil pembayaran dari penitipan ke penampungan
			// ProviderCode diganti 000 khusus untuk ambil data topup
			if (!localDB.getPercentAdminFee (commonSettings.getString ("IconoxTopUpClientProductCode"),
				"000", ref topUpPercentFee, out xError)) {
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get TopUp fee percent data");
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get TopUp fee percent data", "");
			}


			int productAmount = hargaTransaksi;

			LogWriter.showDEBUG (this, "productAmount = "+productAmount.ToString ());

			int productAmountDenganKartu = productAmount - ((int)Math.Ceiling (productAmount * (topUpPercentFee / 100))); // 99% nya
			productAmount = productAmountDenganKartu;

			decimal adminFee = 0;
			int nilaiYangMasukLog = 0;

			try {
				//LogWriter.showDEBUG (this, " productAmount: " + productAmount);
				//if (!getBaseAndFeeAmountFromProduct (productCode, providerProduct.ProviderCode,
				if (!localDB.getAdminFeeAndCustomerFee(productCode, 1, appID, hargaTransaksi,
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
				"productAmountDenganKartu = " + productAmountDenganKartu.ToString () + "\r\n" +
				"totalAmount = " + hargaTransaksi.ToString () + "\r\n" +
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
				nilaiYangMasukLog.ToString (), traceNumber.ToString (), fiTrxDateTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				adminFee.ToString (), providerProduct.ProviderCode, providerProduct.CogsPriceId,
				0, 0, "", skrg.ToString ("yyyy-MM-dd HH:mm:ss"), "", skrg.ToString ("yyyy-MM-dd HH:mm:ss"),
				strJson,
				fiTrxDateTime.ToString ("yyyy-MM-dd HH:mm:ss"),
				IconoxSvrResp,
				skrg.ToString ("yyyy-MM-dd HH:mm:ss"),
				true, 
				failedReason, trxNumber, false, providerProduct.fIncludeFee, "", "",
				out xError)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Failed to save transaction log", "");
			}

			// insert log ucard_transaction
			if (!localDB.addCardTransactionLog (TransactionRef_id, "", trxUCardLog, cardBalance, appID,
				out xError)) {
				// sudah di catat
			}

			if (!localDB.saveOfflineTransaction (userPhone, appID, productCode, fiTrxDateTime, 1, 
				hargaTransaksi, "PowerHouse", traceNumber, true, out xError)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492",
					"Failed to save transaction log", "");
				// sudah catat
			}

			jsonConv.Clear();
			//jsonConv.Add ("fiPSAMAuthorization", respPSAMResp);
			jsonConv.Add ("fiResponseMessage", respMsgSvr);
			jsonConv.Add ("fiResponseCode",respCodeSvr);
			jsonConv.Add ("fiTransactionId", "OIP" + traceNumber.ToString().PadLeft(6, '0'));
			jsonConv.Add ("fiTrxNumber", trxNumber);
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct ());
		}
	}
}

