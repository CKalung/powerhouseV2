using System;
using PPOBHttpRestData;
using System.Collections;
using System.Collections.Generic;
using LOG_Handler;
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

		public string BPJS_SettlementRequest(){
			string[] fields = { "fiApplicationId", "fiUser", "fiToken", 
				"fiCertificate", "fiTrxDateTime", "fiCardBalance", "fiTotalAmount", 
				"fiSAMCSN", "fiUserCardNumber"};

			string appID = "";
			string userCardResponse = "";
			string user = "";		// sebelumnya dari fiPhone
			string token = "";
			string certificate = "";
			string strxDateTime = "";
			string samCSN = "";
			int cardBalance = 0;
			int totalAmount = 0;

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
				userCardResponse = ((string)jsonConv["fiUsercardResponse"]).Trim ();
				token = ((string)jsonConv["fiToken"]).Trim ();
				certificate = ((string)jsonConv["fiCertificate"]).Trim ();
				strxDateTime = ((string)jsonConv["fiTrxDateTime"]).Trim ();
				samCSN = ((string)jsonConv["fiSAMCSN"]).Trim ();
				cardBalance = (int)jsonConv["fiUser"];
				totalAmount = (int)jsonConv["fiTotalAmount"];
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
			}

			ReformatPhoneNumber (ref user);

//			// cek token disini
//			if (!cek_SecurityToken (user, token)) {
//				LOG_Handler.LogWriter.showDEBUG (this, "Cek Token Session: " + user + 
//					", token: " + token);
//				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "504", "Invalid session", "");
//			}


			// konek ka server iconox online payment 
			jsonConv.Clear ();
			// jika fiCertificate = "" maka topup tanpa sign dgn sam krn pake nfc
			jsonConv.Add ("fiTagCode","01");		// payment Challenge
			jsonConv.Add ("fiAgentPhone",user);
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

			string[] repFields = { 
				"fiResponseCode", "fiResponseMessage", "fiPaySAMResponse", "fiLastTransactionLog"
			};

			string respCodeSvr = ((string)jsonConv["fiResponseCode"]).Trim ();
			string respMsgSvr = ((string)jsonConv["fiResponseMessage"]).Trim ();
			string respPSAMResp = ((string)jsonConv["fiPaySAMResponse"]).Trim ();
			string respLastTrxLog = ((string)jsonConv["fiLastTransactionLog"]).Trim ();


			jsonConv.Clear();
			jsonConv.Add ("fiPSAMChallenge", respPSAMResp);
			jsonConv.Add ("fiResponseMessage", respMsgSvr);
			jsonConv.Add ("fiResponseCode",respCodeSvr);
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct ());

		}
	}
}