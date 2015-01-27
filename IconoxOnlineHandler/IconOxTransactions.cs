using System;
using System.Globalization;
using Payment_Host_Interface;
using PPOBHttpRestData;
using StaticCommonLibrary;


namespace IconoxOnlineHandler
{
	public class IconOxTransactions : ITransactionInterface
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
		~IconOxTransactions()
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
		//Exception xError;

		// Additional Data berupa format json, dengan isi:
		//		TOPUP/PURCHASE	fiUserCardNumber
		//						fiTrxDateTime
		//						fiSAMCSN
		//						fiCertificate
		public JsonLibs.MyJsonLib AdditionalJson=null;

		public string productCode = "";
		public string securityToken = "";
		public string agentPhone="";
		public string providerCode="";
		public int trxAmount=0;
		public bool sudahBayar=false;
		public HTTPRestConstructor.HttpRestRequest clientData;

		//------------------------------------
		// Simpan data-data untuk penyimpanan database 
		// nanti setelah insert Transaction Log di class Process_Product
		// untuk mendapatkan transaction Id, karena transaction Id ada pada saat insert transaction log
		// karena ini KHUSUSON

		public long uCardLog_TransactionID=0; // ini belum bisa diisi di class ini
		public string uCardLog_SamCSN="";
		public string uCardLog_OutletCode="";
		public string uCardLog_CardPurchaseLog="";
		public string uCardLog_Description;
		public int uCardLog_PreviousBalance=0;
		public bool isCardTransaction=false;
		//------------------------------------

		public IconOxTransactions(PublicSettings.Settings CommonSettings)
		{
			commonSettings = CommonSettings;
			LOG_Handler.LogWriter.showDEBUG(this, "Iconox > "
				+ commonSettings.getString("IconoxQueueHost") + ":"
				+ commonSettings.getInt("IconoxQueuePort"));

			jsonConv = new JsonLibs.MyJsonLib();
			HTTPRestDataConstruct = new HTTPRestConstructor();
			localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
				commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
		}

		// Disini sudah gak perlu pake protokol deui
		private string RequestToIconoxServer(string msg){
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

		private bool ActivationProc(string appID, string userId, string transactionReference, 
			string providerProductCode, string providerAmount, ref string HttpReply, 
			ref int traceNumber, ref string strJson, ref DateTime trxTime, ref string strRecJson, 
			ref DateTime trxRecTime, ref string failedReason, ref bool canReversal,
			ref bool isSuccessPayment, int transactionType, string trxNumber){

			traceNumber = localDB.getNextProductTraceNumber();
			strJson = "";
			trxTime = DateTime.Now;
			strRecJson = "";
			trxRecTime = trxTime;
			failedReason = "-";
			canReversal = false;
			isSuccessPayment = false;

			// Ada tambahan data untuk transaksi iconox, subjson dari fiAdditional
			if ((!AdditionalJson.ContainsKey("fiTrxDateTime")) || 
				(!AdditionalJson.ContainsKey("fiSAMCSN")) || 
				(!AdditionalJson.ContainsKey("fiCardChallenge")) || 
				(!AdditionalJson.ContainsKey("fiCertificate")))
			{
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					"Mandatory field for smartcard trx not found", "");
				failedReason = "Mandatory field for smartcard trx not found";
				return false;
			}
			string fiStrTrxDateTime;
			string fiSAMCSN;
			string fiCertificate;
			string fiUserCardChallenge;
			string fiUserCardNumber;
			try{
				fiStrTrxDateTime = ((string)AdditionalJson["fiTrxDateTime"]).Trim();
				fiSAMCSN = ((string)AdditionalJson["fiSAMCSN"]).Trim();
				fiCertificate = ((string)AdditionalJson["fiCertificate"]).Trim();
				fiUserCardChallenge = ((string)AdditionalJson["fiCardChallenge"]).Trim();
				fiUserCardNumber = transactionReference.Trim();
			}
			catch{
				failedReason = "Invalid field type";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					failedReason, "");
				return false;
			}

			string formatDate = "yyMMddHHmmss";
			DateTime fiTrxDateTime;
			CultureInfo provider = CultureInfo.InvariantCulture;

			try{
				fiTrxDateTime = DateTime.ParseExact( fiStrTrxDateTime, formatDate, provider);
			}
			catch{
				failedReason = "Invalid date format in Additional Data";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					failedReason, "");
				return false;
			}

			DateTime skr = DateTime.Now;
			TimeSpan dtdiff;
			if (fiTrxDateTime > skr)
				dtdiff = fiTrxDateTime - skr;
			else
				dtdiff = skr - fiTrxDateTime;
			if (dtdiff.TotalMinutes > 5) {
				// jika selisih lebih dari 5 menit, kadaluarsa
				failedReason = "Invalid transaction date range";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "414", 
					failedReason, "");
				return false;
			}

			//cek security token
			string httpReply = "";
			string tokenPhone = agentPhone;
			ReformatPhoneNumber(ref tokenPhone);
			if (!cek_TokenSecurity(tokenPhone, securityToken, ref httpReply))
			{
				failedReason = "Invalid security token";
				HttpReply = httpReply;
				return false;
			}

			CommonLibrary.SessionResetTimeOut (tokenPhone);

			// cek apakah kartu terdaftar di database
			Exception ExError = null;
			string fReason = "";
			decimal dbBalance = 0;
			DateTime lastModified=DateTime.Now;
			if(!localDB.isCardActivated(fiUserCardNumber, ref dbBalance, ref lastModified,
				ref fReason)){
				failedReason = "TopUp: "+fReason;
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "415", 
					failedReason, "");
				return false;
			}


			// Siapkan data untuk kirim ke server iconox untuk cek sertifikat aktivasi
			// TopUp Iconox
			//				fiTagCode
			//				fiAgentPhone
			//				fiDateTime
			//				fiSAMCSN
			//				fiCertificate
			//				fiUserCardResponse
			jsonConv.Clear ();
			jsonConv.Add ("fiTagCode","04");
			jsonConv.Add ("fiAgentPhone",agentPhone);
			jsonConv.Add ("fiDateTime",fiStrTrxDateTime);
			jsonConv.Add ("fiSAMCSN",fiSAMCSN);
			jsonConv.Add ("fiCertificate",fiCertificate);
			jsonConv.Add ("fiCardNumber",fiUserCardNumber);
			jsonConv.Add ("fiUserCardResponse",fiUserCardChallenge);

			string IconoxSvrResp = RequestToIconoxServer (jsonConv.JSONConstruct ());

			if (IconoxSvrResp.Length <= 0) {
				failedReason = "No data from Iconox Server";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "413", 
					failedReason, "");
				return false;
			}

			jsonConv.Clear();
			if (!jsonConv.JSONParse (IconoxSvrResp)) {
				failedReason = "Invalid data format from Iconox Server";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", 
					failedReason, "");
				return false;
			}

			string respCode = "";
			string respSam = "";
			// Ada tambahan data untuk transaksi iconox, subjson dari fiAdditional
			if ((!jsonConv.ContainsKey("fiResponseCode")) || 
				(!jsonConv.ContainsKey("fiResponseMessage")) || 
				(!jsonConv.ContainsKey("fiTagCode")))
			{
				failedReason = "Mandatory field from server not found";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "416", 
					failedReason, "");
				return false;
			}
			respCode = ((string)jsonConv["fiResponseCode"]).Trim();
			if(respCode!="00"){
				failedReason = "Iconox server message: " + ((string)jsonConv["fiResponseMessage"]).Trim ();
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "I"+respCode, 
					failedReason, "");
				return false;
			}
			if (!jsonConv.ContainsKey("fiServerSAMResponse"))
			{
				failedReason = "Iconox server: no SAM response";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "416", 
					failedReason, "");
				return false;
			}

			isSuccessPayment = true;
			strRecJson = "Success";
			//trxRecTime = DateTime.Now;

			respSam = ((string)jsonConv["fiServerSAMResponse"]).Trim ();

			jsonConv.Clear();
			jsonConv.Add("fiToken", securityToken);
			jsonConv.Add("fiPrivateData", respSam);
			jsonConv.Add("fiResponseCode", respCode);
			jsonConv.Add("fiTransactionId", "Icx" + traceNumber.ToString().PadLeft(6, '0'));
			//jsonConv.Add("fiToken", fiToken);
			jsonConv.Add("fiTrxNumber", trxNumber);
			jsonConv.Add("fiReversalAllowed", false);
			HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

			return true;
		}

		private bool TopUpProc(string appID, string userId, string transactionReference, 
				string providerProductCode, string providerAmount, ref string HttpReply, 
				ref int traceNumber, ref string strJson, ref DateTime trxTime, ref string strRecJson, 
				ref DateTime trxRecTime, ref string failedReason, ref bool canReversal,
				ref bool isSuccessPayment, int transactionType, string trxNumber){
			traceNumber = localDB.getNextProductTraceNumber();
			strJson = "";		//"Store: Order Request from: " + custPhone + " to: " + ownerPhone;
			trxTime = DateTime.Now;
			strRecJson = "";
			trxRecTime = trxTime;
			failedReason = "-";
			canReversal = false;
			isSuccessPayment = false;
			//string trxNumber = localDB.getProductTrxNumber(out xError);
			//long reffNum = traceNumber;
			//Exception ExError = null;

			// Ada tambahan data untuk transaksi iconox, subjson dari fiAdditional
			if ((!AdditionalJson.ContainsKey("fiTrxDateTime")) || 
				(!AdditionalJson.ContainsKey("fiSAMCSN")) || 
				(!AdditionalJson.ContainsKey("fiBalance")) || 
				(!AdditionalJson.ContainsKey("fiCardChallenge")) || 
				(!AdditionalJson.ContainsKey("fiCertificate")))
			{
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					"Mandatory field for smartcard trx not found", "");
				failedReason = "Mandatory field for smartcard trx not found";
				return false;
			}
			string fiStrTrxDateTime;
			string fiSAMCSN;
			int fiBalance;
			string fiCertificate;
			string fiUserCardChallenge;
			string fiUserCardNumber;
			try{
				fiBalance = (int)AdditionalJson["fiBalance"];
				fiStrTrxDateTime = ((string)AdditionalJson["fiTrxDateTime"]).Trim();
				fiSAMCSN = ((string)AdditionalJson["fiSAMCSN"]).Trim();
				fiCertificate = ((string)AdditionalJson["fiCertificate"]).Trim();
				fiUserCardChallenge = ((string)AdditionalJson["fiCardChallenge"]).Trim();
				fiUserCardNumber = transactionReference.Trim();
//				if (AdditionalJson.ContainsKey("fiUserCardNumber"))
//					fiUserCardNumber = ((string)AdditionalJson["fiUserCardNumber"]).Trim();
			}
			catch{
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					"Invalid field type", "");
				failedReason = "Invalid field type";
				return false;
			}

			string formatDate = "yyMMddHHmmss";
			DateTime fiTrxDateTime;
			CultureInfo provider = CultureInfo.InvariantCulture;

			try{
				fiTrxDateTime = DateTime.ParseExact( fiStrTrxDateTime, formatDate, provider);
			}
			catch{
				failedReason = "Invalid date format in Additional Data";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					failedReason, "");
				return false;
			}

			DateTime skr = DateTime.Now;
			TimeSpan dtdiff;
			if (fiTrxDateTime > skr)
				dtdiff = fiTrxDateTime - skr;
			else
				dtdiff = skr - fiTrxDateTime;
			if (dtdiff.TotalMinutes > 5) {
				// jika selisih lebih dari 5 menit, kadaluarsa
				failedReason = "Invalid transaction date range";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "414", 
					failedReason, "");
				return false;
			}

			//cek security token
			string httpReply = "";
			string tokenPhone = agentPhone;
			ReformatPhoneNumber(ref tokenPhone);
			if (!cek_TokenSecurity(tokenPhone, securityToken, ref httpReply))
			{
				failedReason = "Invalid security token";
				HttpReply = httpReply;
				return false;
			}

			CommonLibrary.SessionResetTimeOut (tokenPhone);

			// cek apakah kartu terdaftar di database
			Exception ExError = null;
			string fReason = "";
			decimal dbBalance = 0;
			DateTime lastModified=DateTime.Now;
			if(!localDB.isCardActivated(fiUserCardNumber, ref dbBalance, ref lastModified,
				ref fReason)){
				failedReason = "TopUp: "+fReason;
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "415", 
					failedReason, "");
				return false;
			}

			// Check Card Balance
			if (trxAmount<=0) {
				// gak boleh negatif
				failedReason = "TopUp: Invalid TopUp, negatif amount";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "417", 
					failedReason, "");
				return false;
			}

			int idbBalance = decimal.ToInt32 (decimal.Truncate (dbBalance));
			if (idbBalance < fiBalance) {
				// Update Last card status in DB dengan status blocked
				localDB.updateCardBlocked (fiUserCardNumber);

				// terjadi fraud di usercard, perintahkan untuk blok kartu
				failedReason = "TopUp: Invalid card Balance, BLOCK CARD";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "433", 
					failedReason, "");
				return false;
			}

			// update balance di db dengan yg terupdate
			if (lastModified < fiTrxDateTime) {		// harusnya selalu masuk sini, kan online real time
				// Update Last card balance in DB dengan yang terupdate
				if (!localDB.updateCardBalanceInDb (fiUserCardNumber, fiBalance, fiTrxDateTime)) {
					failedReason = "Failed to update usercard balance";
					HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse (400, "416", 
						failedReason, "");
					return false;
				}
			}


			// Siapkan data untuk kirim ke server topup
			// TopUp Iconox
			//				fiTagCode
			//				fiAgentPhone
			//				fiDateTime
			//				fiAmount
			//				fiSAMCSN
			//				fiCertificate
			//				fiUserCardResponse
			jsonConv.Clear ();
			// jika fiCertificate = "" maka topup tanpa sign dgn sam krn pake nfc
			if(fiCertificate != "")
				jsonConv.Add ("fiTagCode","01");
			else
				jsonConv.Add ("fiTagCode","05");
			jsonConv.Add ("fiAgentPhone",agentPhone);
			jsonConv.Add ("fiDateTime",fiStrTrxDateTime);
			jsonConv.Add ("fiAmount",trxAmount);
			jsonConv.Add ("fiSAMCSN",fiSAMCSN);
			jsonConv.Add ("fiCertificate",fiCertificate);
			jsonConv.Add ("fiCardNumber",fiUserCardNumber);
			jsonConv.Add ("fiUserCardResponse",fiUserCardChallenge);

			string IconoxSvrResp = RequestToIconoxServer (jsonConv.JSONConstruct ());

			if (IconoxSvrResp.Length <= 0) {
				failedReason = "No data from Iconox TopUp Server";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "413", 
					failedReason, "");
				return false;
			}

			jsonConv.Clear();
			if (!jsonConv.JSONParse (IconoxSvrResp)) {
				failedReason = "Invalid data format from Iconox TopUp Server";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", 
					failedReason, "");
				return false;
			}

			string respCode = "";
			string respSam = "";
			if ((!jsonConv.ContainsKey("fiResponseCode")) || 
				(!jsonConv.ContainsKey("fiResponseMessage")) || 
				(!jsonConv.ContainsKey("fiTagCode")))
			{
				failedReason = "Mandatory field from TopUp server not found";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "416", 
					failedReason, "");
				return false;
			}
			respCode = ((string)jsonConv["fiResponseCode"]).Trim();
			if(respCode!="00"){
				failedReason = "Iconox server message: " + ((string)jsonConv["fiResponseMessage"]).Trim ();
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "I"+respCode, 
					failedReason, "");
				return false;
			}
			if (!jsonConv.ContainsKey("fiServerSAMResponse"))
			{
				failedReason = "Iconox server: no SAM response";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "416", 
					failedReason, "");
				return false;
			}

			// Update Last card balance in DB dengan total topup dan balance sebelumnya
			int totalBalance = trxAmount + fiBalance;
			if(!localDB.updateCardBalanceInDb(fiUserCardNumber, totalBalance, fiTrxDateTime)){
				failedReason = "Failed to update usercard balance";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "416", 
					failedReason, "");
				return false;
			}

			isSuccessPayment = true;
			strRecJson = "Success";
			//trxRecTime = DateTime.Now;

			respSam = ((string)jsonConv["fiServerSAMResponse"]).Trim ();

			jsonConv.Clear();
			jsonConv.Add("fiToken", securityToken);
			jsonConv.Add("fiPrivateData", respSam);
			jsonConv.Add("fiResponseCode", respCode);
			jsonConv.Add("fiTransactionId", "Icx" + traceNumber.ToString().PadLeft(6, '0'));
			//jsonConv.Add("fiToken", fiToken);
			jsonConv.Add("fiTrxNumber", trxNumber);
			jsonConv.Add("fiReversalAllowed", false);
			HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

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

			return cek_TokenSecurity(userPhone, token, ref httpRepl);
		}

		private bool cek_TokenSecurity(string userPhone, string token, ref string httpRepl)
		{
			LOG_Handler.LogWriter.showDEBUG (this, "Cek Token Session: " + userPhone + 
				", token: " + token);

			// cek detek sessionnya
			if (!CommonLibrary.isSessionExist(userPhone, token))
			{
				httpRepl = HTTPRestDataConstruct.constructHTTPRestResponse(400, "504", "Invalid session", "");
				return false;
			}
			return true;
		}

		public bool isTransactionHasAlreadyDone(string appID,
			ref string HttpReply, ref string failedReason){

			// Ada tambahan data untuk transaksi iconox, subjson dari fiAdditional
			if ((!AdditionalJson.ContainsKey("fiTrxDateTime")) || 
				(!AdditionalJson.ContainsKey("fiBalance")) || 
				(!AdditionalJson.ContainsKey("fiSAMCSN")) || 
				(!AdditionalJson.ContainsKey("fiQuantity")) || 
				(!AdditionalJson.ContainsKey("fiCertificate")) ||
				(!AdditionalJson.ContainsKey("fiLogPurchase")) 
			)
			{
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					"Mandatory field for smartcard trx not found", "");
				failedReason = "Mandatory field for smartcard trx not found";
				return true;
			}
			int fiBalance;
			int fiQuantity;
			string fiStrTrxDateTime;
//			string fiSAMCSN;
//			string fiCertificate;
//			string fiUserCardNumber;
//			string fiPurchaseLog;
			try{
				fiBalance = (int)AdditionalJson["fiBalance"];
				fiQuantity = (int)AdditionalJson["fiQuantity"];
				fiStrTrxDateTime = ((string)AdditionalJson["fiTrxDateTime"]).Trim();
//				fiSAMCSN = ((string)AdditionalJson["fiSAMCSN"]).Trim();
//				fiCertificate = ((string)AdditionalJson["fiCertificate"]).Trim();
//				fiPurchaseLog = ((string)AdditionalJson["fiLogPurchase"]).Trim();
//				fiUserCardNumber = transactReff.Trim();
				//				if (AdditionalJson.ContainsKey("fiUserCardNumber"))
				//					fiUserCardNumber = ((string)AdditionalJson["fiUserCardNumber"]).Trim();
			}
			catch{
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					"Invalid field value", "");
				failedReason = "Invalid field value";
				return true;
			}

			string formatDate = "yyMMddHHmmss";
			DateTime fiTrxDateTime;
			CultureInfo provider = CultureInfo.InvariantCulture;

			try{
				fiTrxDateTime = DateTime.ParseExact( fiStrTrxDateTime, formatDate, provider);
			}
			catch{
				failedReason = "Invalid date format in Additional Data";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					failedReason, "");
				return true;
			}

			Exception ExError = null;
			string dbTraceNum = "";
			if (localDB.isOfflineTransactionExist(agentPhone, appID, productCode, fiTrxDateTime, 
				fiQuantity, ref dbTraceNum, true, out ExError))
			{
				// Data sudah ada di database, anggap sukses
				//				// perbaharui token 
				//				token = CommonLibrary.RenewTokenSession(userPhone);
//				traceNumber = int.Parse (dbTraceNum);
//				alreadyPaid = true;
//				isSuccessPayment = true;
//				strRecJson = "Success";
				jsonConv.Clear();
				jsonConv.Add("fiToken", securityToken);
				jsonConv.Add("fiPrivateData", "");
				jsonConv.Add("fiResponseCode", "00");
				//jsonConv.Add("fiTransactionId", "IcP" + tracenumber.ToString().PadLeft(6, '0'));
				jsonConv.Add("fiTransactionId", "IcP" + dbTraceNum);
				//jsonConv.Add("fiToken", fiToken);
				jsonConv.Add("fiTrxNumber", "NoTrxNum");
				//jsonConv.Add("fiTrxNumber", trxNumber);
				jsonConv.Add("fiReversalAllowed", false);
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

				return true;
			}
			if (ExError != null)
			{
				failedReason = "Can not access database";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
					failedReason, "");
				return true;
			}
			return false;
		}

		private bool Nitrogen_SmartCardTransaction (string appID, DateTime fiTrxDateTime,
			string fiUserCardNumber, int fiBalance, int fiQuantity, ref bool isSuccessPayment,
			ref string strRecJson, ref int traceNumber, string trxNumber,
			string fiStrTrxDateTime, string fiSAMCSN, string fiOutletCode, string fiCertificate, string fiPurchaseLog,
			ref string failedReason, ref string HttpReply, ref bool alreadyPaid){

			traceNumber = localDB.getNextProductTraceNumber();
			alreadyPaid = false;

			uCardLog_PreviousBalance = fiBalance;
			uCardLog_CardPurchaseLog = fiPurchaseLog;
			uCardLog_SamCSN = fiSAMCSN;
			uCardLog_OutletCode = fiOutletCode;

			// cek apakah kartu terdaftar di database
			string fReason = "";
			decimal dbBalance = 0;
			DateTime lastModified=DateTime.Now;
			if(!localDB.isCardActivated(fiUserCardNumber, ref dbBalance, ref lastModified,
				ref fReason)){
				failedReason = "Purchase: "+fReason;
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "506", 
					failedReason, "");
				return false;
			}

			int totalBalance = fiBalance - trxAmount;
			if ((totalBalance < 0) || (trxAmount<=0)) {
				// gak boleh negatif
				failedReason = "Purchase: Invalid purchase, negatif balance";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "503", 
					failedReason, "");
				return false;
			}

			// Check Card Balance
			//			if (lastModified < fiTrxDateTime) {
			//				int idbBalance = decimal.ToInt32 (decimal.Truncate (dbBalance));
			//				if (idbBalance < fiBalance) {
			//					// Update Last card status in DB dengan status blocked
			//					localDB.updateCardBlocked (fiUserCardNumber);
			//
			//					// terjadi fraud di usercard, perintahkan untuk blok kartu
			//					failedReason = "Purchase: Invalid card Balance, BLOCK CARD";
			//					HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse (400, "433", 
			//						failedReason, "");
			//					return false;
			//				}
			//
			//				//if (idbBalance != totalBalance) {
			//				// Update Last card balance in DB
			//				if (!localDB.updateCardBalanceInDb (fiUserCardNumber, totalBalance, fiTrxDateTime)) {
			//					failedReason = "Failed to update usercard balance";
			//					HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse (400, "416", 
			//						failedReason, "");
			//					return false;
			//				}
			//				//}
			//			}

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
			jsonConv.Add ("fiCardNumber",fiUserCardNumber);
			jsonConv.Add ("fiAgentPhone",agentPhone);
			jsonConv.Add ("fiDateTime",fiStrTrxDateTime);
			jsonConv.Add ("fiAmount",trxAmount);
			jsonConv.Add ("fiSAMCSN",fiSAMCSN);
			jsonConv.Add ("fiCertificate",fiCertificate);
			jsonConv.Add ("fiPurchaseLog", fiPurchaseLog);

			string IconoxSvrResp = RequestToIconoxServer (jsonConv.JSONConstruct ());

			if (IconoxSvrResp.Length <= 0) {
				failedReason = "No data from Iconox Server";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "413", 
					failedReason, "");
				return false;
			}

			jsonConv.Clear();
			if (!jsonConv.JSONParse (IconoxSvrResp)) {
				failedReason = "Invalid data format from Iconox Server";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", 
					failedReason, "");
				return false;
			}

			string respCode = "";
			//			string respMsg = "";
			//			string respTag = "";
			//string respSam = "";
			// Ada tambahan data untuk transaksi iconox, subjson dari fiAdditional
			if ((!jsonConv.ContainsKey("fiResponseCode")) || 
				(!jsonConv.ContainsKey("fiResponseMessage")) || 
				(!jsonConv.ContainsKey("fiTagCode")))
			{
				failedReason = "Mandatory field from Iconox server not found";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "416", 
					failedReason, "");
				return false;
			}
			respCode = ((string)jsonConv["fiResponseCode"]).Trim();
			if(respCode!="00"){
				failedReason = "Iconox server message: " + ((string)jsonConv["fiResponseMessage"]).Trim ();
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "I"+respCode, 
					failedReason, "");
				return false;
			}

			Exception ExError = null;
			// persiapan simpan datanya di database

			string dbTraceNum = "";
			if (localDB.isOfflineTransactionExist(agentPhone, appID, productCode, fiTrxDateTime, 
				fiQuantity, ref dbTraceNum, true, out ExError))
			{
				// Data sudah ada di database, anggap sukses
				//				// perbaharui token 
				//				token = CommonLibrary.RenewTokenSession(userPhone);
				traceNumber = int.Parse (dbTraceNum);
				alreadyPaid = true;
				isSuccessPayment = true;
				strRecJson = "Success";
				jsonConv.Clear();
				jsonConv.Add("fiToken", securityToken);
				jsonConv.Add("fiPrivateData", "");
				jsonConv.Add("fiResponseCode", respCode);
				//jsonConv.Add("fiTransactionId", "IcP" + tracenumber.ToString().PadLeft(6, '0'));
				jsonConv.Add("fiTransactionId", "IcP" + dbTraceNum);
				//jsonConv.Add("fiToken", fiToken);
				jsonConv.Add("fiTrxNumber", trxNumber);
				jsonConv.Add("fiReversalAllowed", false);
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

				return true;
			}
			if (ExError != null)
			{
				failedReason = "Can not access database";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
					failedReason, "");
				return false;
			}

			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
			try
			{
				providerProduct = localDB.getProviderProductInfo(productCode, out ExError);
				if (ExError != null)
				{
					failedReason = "Can not access database";
					HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
						failedReason, "");
					return false;
				}
			}
			catch
			{
				failedReason = "Can not access database";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
					failedReason, "");
				return false;
			}

			if (!localDB.saveOfflineTransaction(agentPhone, appID, productCode, fiTrxDateTime,
				fiQuantity,providerProduct.CurrentPrice, clientData.Host, traceNumber,true, out ExError))
			{
				failedReason = "Can not store to database";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
					failedReason, "");
				return false;
			}


			isSuccessPayment = true;
			strRecJson = "Success";
			//trxRecTime = DateTime.Now;
			//respSam = ((string)jsonConv["fiServerSAMResponse"]).Trim ();

			jsonConv.Clear();
			jsonConv.Add("fiToken", securityToken);
			jsonConv.Add("fiPrivateData", "");
			jsonConv.Add("fiResponseCode", respCode);
			jsonConv.Add("fiTransactionId", "IcP" + traceNumber.ToString().PadLeft(6, '0'));
			//jsonConv.Add("fiToken", fiToken);
			jsonConv.Add("fiTrxNumber", trxNumber);
			jsonConv.Add("fiReversalAllowed", false);
			HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

			return true;
		}

		private bool Nitrogen_CashTransaction (string appID, DateTime fiTrxDateTime,
			string fiUserCardNumber, int fiBalance, int fiQuantity, ref bool isSuccessPayment,
			ref string strRecJson, ref int traceNumber, string trxNumber,
			string fiStrTrxDateTime, string fiSAMCSN, string fiOutletCode, string fiCertificate, string fiPurchaseLog,
			ref string failedReason, ref string HttpReply, ref bool alreadyPaid){

			uCardLog_SamCSN = fiSAMCSN;
			uCardLog_OutletCode = fiOutletCode;

			traceNumber = localDB.getNextProductTraceNumber();
			alreadyPaid = false;

			DateTime lastModified=DateTime.Now;

			// Siapkan data untuk kirim ke server iconox
			jsonConv.Clear ();
			jsonConv.Add ("fiTagCode","03");
			jsonConv.Add ("fiCardNumber",fiUserCardNumber);
			jsonConv.Add ("fiAgentPhone",agentPhone);
			jsonConv.Add ("fiDateTime",fiStrTrxDateTime);
			jsonConv.Add ("fiAmount",trxAmount);
			jsonConv.Add ("fiSAMCSN",fiSAMCSN);
			jsonConv.Add ("fiCertificate",fiCertificate);
			jsonConv.Add ("fiPurchaseLog", fiPurchaseLog);


			Exception ExError = null;
			// simpan datanya di database
			string dbTraceNum = "";
			if (localDB.isOfflineTransactionExist(agentPhone, appID, productCode, fiTrxDateTime, 
				fiQuantity, ref dbTraceNum, true, out ExError))
			{
				// Data sudah ada di database, anggap sukses
				//				// perbaharui token 
				//				token = CommonLibrary.RenewTokenSession(userPhone);
				traceNumber = int.Parse (dbTraceNum);
				isSuccessPayment = true;
				alreadyPaid = true;
				strRecJson = "Success";
				jsonConv.Clear();
				jsonConv.Add("fiToken", securityToken);
				jsonConv.Add("fiPrivateData", "");
				jsonConv.Add("fiResponseCode", "00");
				//jsonConv.Add("fiTransactionId", "IcP" + traceNumber.ToString().PadLeft(6, '0'));
				jsonConv.Add("fiTransactionId", "IcP" + dbTraceNum);
				//jsonConv.Add("fiToken", fiToken);
				jsonConv.Add("fiTrxNumber", trxNumber);
				jsonConv.Add("fiReversalAllowed", false);
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

				return true;
			}
			if (ExError != null)
			{
				failedReason = "Can not access database";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
					failedReason, "");
				return false;
			}

			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
			try
			{
				providerProduct = localDB.getProviderProductInfo(productCode, out ExError);
				if (ExError != null)
				{
					failedReason = "Can not access database";
					HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
						failedReason, "");
					return false;
				}
			}
			catch
			{
				failedReason = "Can not access database";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
					failedReason, "");
				return false;
			}

			if (!localDB.saveOfflineTransaction(agentPhone, appID, productCode, fiTrxDateTime,
				fiQuantity,providerProduct.CurrentPrice, clientData.Host,traceNumber,true, out ExError))
			{
				failedReason = "Can not store to database";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", 
					failedReason, "");
				return false;
			}

			isSuccessPayment = true;
			strRecJson = "Success";
			//trxRecTime = DateTime.Now;
			//respSam = ((string)jsonConv["fiServerSAMResponse"]).Trim ();

			jsonConv.Clear();
			jsonConv.Add("fiToken", securityToken);
			jsonConv.Add("fiPrivateData", "");
			jsonConv.Add("fiResponseCode", "00");
			jsonConv.Add("fiTransactionId", "IcP" + traceNumber.ToString().PadLeft(6, '0'));
			//jsonConv.Add("fiToken", fiToken);
			jsonConv.Add("fiTrxNumber", trxNumber);
			jsonConv.Add("fiReversalAllowed", false);
			HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

			return true;
		}

		private bool PurchaseProc(string appID, string userId, string transactionReference, 
			string providerProductCode, string providerAmount, ref string HttpReply, 
			ref int traceNumber, ref string strJson, ref DateTime trxTime, ref string strRecJson, 
			ref DateTime trxRecTime, ref string failedReason, ref bool canReversal,
			ref bool isSuccessPayment, int transactionType, string trxNumber){

			strJson = "";		//"Store: Order Request from: " + custPhone + " to: " + ownerPhone;
			trxTime = DateTime.Now;
			strRecJson = "";
			trxRecTime = trxTime;
			failedReason = "-";
			canReversal = false;
			isSuccessPayment = false;

			// Ada tambahan data untuk transaksi iconox, subjson dari fiAdditional
			if ((!AdditionalJson.ContainsKey("fiTrxDateTime")) || 
				(!AdditionalJson.ContainsKey("fiBalance")) || 
//				(!AdditionalJson.ContainsKey("fiSAMCSN")) || 
				(!AdditionalJson.ContainsKey("fiOutletCode")) || 
				(!AdditionalJson.ContainsKey("fiQuantity")) || 
				(!AdditionalJson.ContainsKey("fiCertificate")) ||
				(!AdditionalJson.ContainsKey("fiLogPurchase")) 
				)
			{
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					"Mandatory field for smartcard trx not found", "");
				failedReason = "Mandatory field for smartcard trx not found";
				return false;
			}
			int fiBalance;
			int fiQuantity;
			string fiStrTrxDateTime;
			string fiSAMCSN="";
			string fiCertificate;
			string fiUserCardNumber;
			string fiPurchaseLog;
			string fiOutletCode;

			if(AdditionalJson.isExists ("fiSAMCSN")){
				fiSAMCSN = ((string)AdditionalJson["fiSAMCSN"]).Trim();
			}
			try{
				fiBalance = (int)AdditionalJson["fiBalance"];
				fiQuantity = (int)AdditionalJson["fiQuantity"];
				fiStrTrxDateTime = ((string)AdditionalJson["fiTrxDateTime"]).Trim();
				fiOutletCode = ((string)AdditionalJson["fiOutletCode"]).Trim();
				fiCertificate = ((string)AdditionalJson["fiCertificate"]).Trim();
				fiPurchaseLog = ((string)AdditionalJson["fiLogPurchase"]).Trim();
				fiUserCardNumber = transactionReference.Trim();
				//				if (AdditionalJson.ContainsKey("fiUserCardNumber"))
				//					fiUserCardNumber = ((string)AdditionalJson["fiUserCardNumber"]).Trim();
			}
			catch{
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					"Invalid field value", "");
				failedReason = "Invalid field value";
				return false;
			}

			try{
				uCardLog_Description = ((string)AdditionalJson["fiDescription"]).Trim();
				LOG_Handler.LogWriter.showDEBUG (this, "uCardLog_Description = " + uCardLog_Description);
			}catch{
			}

//			// TRIK : Untuk memasukkan purchase log di Transaction Log di class Process_Product.cs
//			if (fiPurchaseLog != "")
//				strJson = "SmartCardLog: "+fiPurchaseLog;

			string formatDate = "yyMMddHHmmss";
			DateTime fiTrxDateTime;
			CultureInfo provider = CultureInfo.InvariantCulture;

			try{
				fiTrxDateTime = DateTime.ParseExact( fiStrTrxDateTime, formatDate, provider);
			}
			catch{
				failedReason = "Invalid date format in Additional Data";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", 
					failedReason, "");
				return false;
			}

			//cek security token
			string httpReply = "";
			string tokenPhone = agentPhone;
			ReformatPhoneNumber(ref tokenPhone);
			if (!cek_TokenSecurity(tokenPhone, securityToken, ref httpReply))
			{
				failedReason = "Invalid security token";
				HttpReply = httpReply;
				return false;
			}
			CommonLibrary.SessionResetTimeOut (tokenPhone);

			// DISINI di cabangkan berdasarkan transaksi menggunakan kartu atau cash
			if (fiUserCardNumber == "") {
				// Transaksi menggunakan cash
				// dana di debet dari rekening petugas
				isCardTransaction=false;
				return Nitrogen_CashTransaction (appID, fiTrxDateTime, fiUserCardNumber, 
					fiBalance, fiQuantity, ref isSuccessPayment, ref strRecJson, ref traceNumber, 
					trxNumber, fiStrTrxDateTime, fiSAMCSN, fiOutletCode, fiCertificate, fiPurchaseLog,
					ref failedReason, ref HttpReply, ref sudahBayar);
			} else {
				// Transaksi menggunakan kartu
				// dana di debet dari rekening titipan iconox
				isCardTransaction=true;
				return Nitrogen_SmartCardTransaction (appID, fiTrxDateTime, fiUserCardNumber, 
					fiBalance, fiQuantity, ref isSuccessPayment, ref strRecJson, ref traceNumber, 
					trxNumber, fiStrTrxDateTime, fiSAMCSN, fiOutletCode, fiCertificate, fiPurchaseLog,
					ref failedReason, ref HttpReply, ref sudahBayar);
			}
		}

		public bool isPurchase(string providerProductCode){
			if (providerProductCode == commonSettings.getString ("IconoxTopUpProviderProductCode")) {		//"DAMPRE001") {
				// Topup
				return false;
			} else if (providerProductCode == commonSettings.getString ("IconoxActivationProviderProductCode")) {
				// Activation
				return false;
			} else {
				// purchase
				return true;
			}
		}

		public bool productTransaction(string appID, string userId, string transactionReference, 
			string providerProductCode, string providerAmount, ref string HttpReply, 
			ref int traceNumber, ref string strJson, ref DateTime trxTime, ref string strRecJson, 
			ref DateTime trxRecTime, ref string failedReason, ref bool canReversal,
			ref bool isSuccessPayment, int transactionType, string trxNumber)
		{
			// TODO : PENTING... Tabel baru untuk log kartu ucard_transaction harus diisi didalem sini
			if (providerProductCode == commonSettings.getString ("IconoxTopUpProviderProductCode")) {		//"DAMPRE001") {
				if (providerCode != "000") {
					strJson = "";
					trxTime = DateTime.Now;
					strRecJson = "";
					trxRecTime = trxTime;
					canReversal = false;
					isSuccessPayment = true;
					failedReason = "Invalid Provider " + providerCode;
					HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse (400, "493", 
						failedReason, "");
					return false;
				}
				isCardTransaction=true;
				return TopUpProc (appID, userId, transactionReference, 
					providerProductCode, providerAmount, ref HttpReply, 
					ref traceNumber, ref strJson, ref trxTime, ref strRecJson, 
					ref trxRecTime, ref failedReason, ref canReversal,
					ref isSuccessPayment, transactionType, trxNumber);
			} else if (providerProductCode == commonSettings.getString ("IconoxActivationProviderProductCode")) {
				isCardTransaction=true;
				return ActivationProc (appID, userId, transactionReference, 
					providerProductCode, providerAmount, ref HttpReply, 
					ref traceNumber, ref strJson, ref trxTime, ref strRecJson, 
					ref trxRecTime, ref failedReason, ref canReversal,
					ref isSuccessPayment, transactionType, trxNumber);
			} else {
//				if (providerCode != "121") {
//					strJson = "";
//					trxTime = DateTime.Now;
//					strRecJson = "";
//					trxRecTime = trxTime;
//					canReversal = false;
//					isSuccessPayment = true;
//					failedReason = "Invalid Provider " + providerCode;
//					HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "493", 
//						failedReason, "");
//					return false;
//				}
				return PurchaseProc(appID, userId, transactionReference, 
					providerProductCode, providerAmount, ref HttpReply, 
					ref traceNumber, ref strJson, ref trxTime, ref strRecJson, 
					ref trxRecTime, ref failedReason, ref canReversal,
					ref isSuccessPayment, transactionType, trxNumber);
			}

		}

		public bool productInquiry(string appID, string userId, string customerNumber, 
			string productCode, int adminFee, bool fIncludeAdminFee, ref int productAmount, 
			ref string HttpReply, ref int traceNumber, ref string strJson, ref DateTime trxTime,
			ref string strRecJson, ref DateTime trxRecTime, string trxNumber)
		{
			isCardTransaction=false;
			throw new NotImplementedException();
		}

	}
}

