using System;
using PPOBHttpRestData;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using LOG_Handler;
using PPOBDatabase;
using StaticCommonLibrary;
using IconoxOnlineHandler;

namespace OfflineTransaction
{
	public class Settlement : IDisposable {
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
		~Settlement()
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

		public Settlement(HTTPRestConstructor.HttpRestRequest ClientData,
				PublicSettings.Settings CommonSettings){

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

		private string getAmountAfterTopupFee(string appId, string productCode, int productAmount, 
			ref int amountAfterTopupFee,ref int adminFee){
			Exception xError = null;
			decimal topUpPercentFee = 0;
			decimal cardProductAmount = 0;
			try {
				// ProviderCode diganti 000 khusus untuk ambil data topup
				if (!localDB.getPercentAdminFee (commonSettings.getString ("IconoxTopUpClientProductCode"),
					"000", ref topUpPercentFee, out xError)) {
					LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get TopUp fee percent data");
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get TopUp fee percent data", "");
				}
				// Disini nanti dana diambil dari Account Iconox TITIPAN 

				cardProductAmount = productAmount;	// 100% nya

				productAmount = productAmount - ((int)Math.Ceiling (cardProductAmount * (topUpPercentFee / 100))); // 99% nya

				LogWriter.showDEBUG (this, "productAmountDenganKartu 99% = "+productAmount.ToString ());

				//			Console.WriteLine ("DEBUG -- topUpPercentFee= "+topUpPercentFee);
				//			Console.WriteLine ("DEBUG -- cardProductAmount= "+cardProductAmount);

			} catch (Exception ex) {
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error get topup fee data : " + ex.getCompleteErrMsg ());
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get topup fee data", "");
			}

			amountAfterTopupFee = productAmount;
			adminFee = 0;

			if (!localDB.getAdminFeeAndCustomerFee(productCode, 1, appId, cardProductAmount,
				ref adminFee, out xError))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Error get admin fee data ", "");
			}
			return "";
		}

		private string isTrxWasDone(string agentPhone, string appID, string productCode, DateTime trxDateTime,
			ref string dbTraceNum){
			Exception ExError = null;

			LogWriter.showDEBUG (this, "==**== ASUP A");

			if (localDB.isOfflineTransactionExist(agentPhone, appID, productCode, trxDateTime, 
				1, ref dbTraceNum, true, out ExError))
			{
				// Data sudah ada di database, anggap sukses
				//				// perbaharui token 
				//				traceNumber = int.Parse (dbTraceNum);
				//				alreadyPaid = true;
				//				isSuccessPayment = true;
				//				strRecJson = "Success";
				jsonConv.Clear();
				jsonConv.Add("fiResponseCode", "00");
				//jsonConv.Add("fiTransactionId", "IcP" + tracenumber.ToString().PadLeft(6, '0'));
				jsonConv.Add("fiTransactionId", "IcP" + dbTraceNum);
				jsonConv.Add("fiTrxNumber", "NoTrxNum");
				//jsonConv.Add("fiTrxNumber", trxNumber);
				jsonConv.Add("fiTrxNumber", "NoTrxNum");
				return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
			}
			if (ExError != null)
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
					"Can not access database", "");
			}
			return "";
		}

		private string getPaymentFromCardSuspendAccount(long TransactionRef_id,
			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct, int amount){
			//productAmount = prdAmount.ToString();
			//nilaiTransaksiKeProvider = providerProduct.CurrentPrice;

			string qvaInvoiceNumber = "";
			bool qvaReversalRequired = false;

			string errCode = "";
			string errMessage = "";

			using (TransferReguler.CollectTransfer TransferReg =
				new TransferReguler.CollectTransfer(commonSettings))
			{
				try
				{
					int transactionType = 0;	// 0 = Pembayaran, 1 = pembelian
					// ====== PEMBAYARAN DARI EWALLET
					//LogWriter.show(this, "Get payment from EWallet");
					//LogWriter.showDEBUG (this,"4. Product Purchase productAmount = " + productAmount.ToString ());
					if (!TransferReg.PayFromCustomerEwallet( 
						providerProduct.TransactionCodeSufix, transactionType, amount,
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

		public string Iconox_Settlement(){
			string[] fields = { "fiApplicationId", "fiUser", "fiToken", 
				"fiTrxDateTime", "fiCardBalance", "fiTotalAmount", "fiLogPurchase",
				"fiCertificate", "fiSAMCSN", "fiUserCardNumber"};

			string appID = "";
			string userCardNumber = "";
			string user = "";		// sebelumnya dari fiPhone
			string token = "";
			string sTrxDateTime = "";
			string samCSN = "";
			string logPurchase = "";
			string certificate = "";
			int cardBalance = 0;
			int totalAmount = 0;

			string productCode = commonSettings.getString ("ProductCode-MultiPaymentIconoxOffline");

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
				user = ((string)jsonConv["fiUser"]).Trim ();
				userCardNumber = ((string)jsonConv["fiUserCardNumber"]).Trim ();
				token = ((string)jsonConv["fiToken"]).Trim ();
				sTrxDateTime = ((string)jsonConv["fiTrxDateTime"]).Trim ();
				samCSN = ((string)jsonConv["fiSAMCSN"]).Trim ();
				cardBalance = (int)jsonConv["fiCardBalance"];
				totalAmount = (int)jsonConv["fiTotalAmount"];
				certificate = ((string)jsonConv["fiCertificate"]).Trim ();
				logPurchase = ((string)jsonConv["fiLogPurchase"]).Trim ();
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
			}

			Exception xError = null;
			// konversi alias dari user ke nomor HP
			string userPhone = localDB.getUserPhoneFromAlias(user,out xError);
			if (xError != null) {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to access database", "");
			}
			if (userPhone.Length==0) {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "No user defined", "");
			}

			string formatDate = "yyMMddHHmmss";
			DateTime trxDateTime;
			CultureInfo provider = CultureInfo.InvariantCulture;

			try{
				trxDateTime = DateTime.ParseExact( sTrxDateTime, formatDate, provider);
			}
			catch{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					"Invalid date format", "");
			}

			if ((logPurchase.Length == 0) || (samCSN.Length == 0)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					"Invalid data", "");
			}

			ReformatPhoneNumber (ref userPhone);

			// cek token disini
			if (!cek_SecurityToken (userPhone, token)) {
				LogWriter.showDEBUG (this, "Cek Token Session: " + user + 
					", token: " + token);
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "504", "Invalid session", "");
			}
			CommonLibrary.SessionResetTimeOut (userPhone);

			// cek apakah kartu terdaftar di database
			string fReason = "";
			decimal dbBalance = 0;
			DateTime lastModified=DateTime.Now;
			if(!localDB.isCardActivated(userCardNumber, ref dbBalance, ref lastModified,
				ref fReason)){
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "506", 
					"Purchase: "+fReason, "");
			}

			string dbTraceNum = "";
			string hasil = isTrxWasDone (userPhone, appID, productCode, trxDateTime, ref dbTraceNum);
			if (hasil.Length != 0)
				return hasil;

			int totalBalance = cardBalance - totalAmount;
			if ((totalBalance < 0) || (totalAmount<=0)) {
				// gak boleh negatif
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "503", 
					"Purchase: Invalid purchase, negatif balance", "");
			}


			// CEK Certificate
			// Siapkan data untuk kirim ke server iconox
			// Server Iconox
			//				fiTagCode
			//				fiAgentPhone
			//				fiDateTime
			//				fiAmount
			//				fiSAMCSN
			//				fiCertificate
			jsonConv.Clear ();
			jsonConv.Add ("fiTagCode","03");
			jsonConv.Add ("fiCardNumber",userCardNumber);
			jsonConv.Add ("fiAgentPhone",userPhone);
			jsonConv.Add ("fiDateTime",sTrxDateTime);
			jsonConv.Add ("fiAmount",totalAmount);
			jsonConv.Add ("fiSAMCSN",samCSN);
			jsonConv.Add ("fiCertificate",certificate);
			jsonConv.Add ("fiPurchaseLog", logPurchase);

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

			string[] repFields = { 
				"fiResponseCode", "fiResponseMessage", "fiTagCode"
			};

			if(!checkMandatoryFields(repFields)){
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Incomplete fields from iconox server", "");
			}


			string respCodeSvr;
			string respMsgSvr;
			try{
				respCodeSvr = ((string)jsonConv["fiResponseCode"]).Trim ();
				respMsgSvr = ((string)jsonConv["fiResponseMessage"]).Trim ();
			} catch {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407",
					"Invalid data from Iconox Server", "");
			}

			// ========== BYPASS JANG DEMO
//			if(respCodeSvr!="00"){
//				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "I"+respCodeSvr, 
//					"Iconox server message: " + respMsgSvr, "");
//			}

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

			int adminFee = 0;

			int nilaiSetelahTopupFee = 0;
			int nilaiYangMasukLog = 0;


			hasil = getAmountAfterTopupFee (appID,productCode,totalAmount,
				ref nilaiSetelahTopupFee, ref adminFee);
			if (hasil.Length > 0)
				return hasil;

			nilaiYangMasukLog = nilaiSetelahTopupFee - adminFee;

			Console.WriteLine ("=====  DEBUG : \n"+
				"TotalAmount = " + totalAmount + "\n" + 
				"SetelahTopupFee = " + nilaiSetelahTopupFee + "\n" + 
				"AdminFee = " + adminFee + "\n" + 
				"NilaiYangMasukLog = " + nilaiYangMasukLog
			);

			long TransactionRef_id = localDB.getTransactionReffIdSequence(out xError);

			hasil = getPaymentFromCardSuspendAccount (TransactionRef_id, providerProduct, nilaiSetelahTopupFee);
			if (hasil.Length > 0)
				return hasil;

			int traceNumber = localDB.getNextProductTraceNumber();
			string trxNumber = localDB.getProductTrxNumber(out xError);

			if (!localDB.saveOfflineTransaction(userPhone, appID, productCode, trxDateTime,
				1,totalAmount, clientData.Host, traceNumber,true, out xError))
			{
				failedReason = "Can't store to database";
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
					failedReason, "");
			}

			string userId = cUserIDHeader + userPhone;
			DateTime skrg = DateTime.Now;
			string strJson = "";

			// insert log transaksi
			if (!localDB.insertCompleteTransactionLog (TransactionRef_id, productCode, 
				providerProduct.ProviderProductCode,
				userId.Substring (commonSettings.getString ("UserIdHeader").Length), userCardNumber,
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
			}

			// JIka transaksi kartu, maka simpan di table ucard_transaction
			LogWriter.showDEBUG (this, "== Add CardTransactionLog ");
			if (!localDB.addCardTransactionLog (TransactionRef_id,"",logPurchase,cardBalance,
				out xError)) {
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "FATAL Failed to save Card Transaction Log");
				//					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492",
				//						"Failed to save card transaction log", "");
			}

			jsonConv.Clear();
			jsonConv.Add("fiResponseCode", "00");
			jsonConv.Add("fiResponseMessage", "Success");
			jsonConv.Add("fiTransactionId", "IcP" + traceNumber.ToString().PadLeft(6, '0'));
			jsonConv.Add("fiTrxNumber", trxNumber);
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct ());

		}
	}
}