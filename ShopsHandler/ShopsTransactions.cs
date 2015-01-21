
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

		public string NitrogenShopMakeOrder(){

			// INGAT INI TOKO NITROGEN, lain AliExpress.......

			string[] fields = { "fiApplicationId", "fiProductList",  "fiPhone", "fiGroupProductCode", "fiSAMCSN", "fiToken"};

			string appID = "";
			string groupProductCode = "";
			string userPhone = "";
			string securityToken = "";
			string SamCSN = "";
			JsonLibs.MyJsonArray productList;

			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}

			if(!checkMandatoryFields(jsonConv, fields))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory fields not found", "");
			}

			if (jsonConv.isExists ("fiSAMCSN")) {
				SamCSN = ((string)jsonConv["fiSAMCSN"]).Trim ();
			}

			try
			{
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
				userPhone = ((string)jsonConv["fiPhone"]).Trim ();
				groupProductCode = ((string)jsonConv["fiGroupProductCode"]).Trim ();
				securityToken = ((string)jsonConv["fiToken"]).Trim ();

				productList = (JsonLibs.MyJsonArray)jsonConv["fiProductList"];
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type or format", "");
			}

			ReformatPhoneNumber (ref userPhone);

			if (!cek_SecurityToken (userPhone, securityToken)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "504", "Invalid session or security", "");
			}


			int adminFee = 0;
			Exception xError = null;

			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct = null;
			try
			{
				providerProduct = localDB.getProviderProductInfo(groupProductCode, out xError);
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

			long TransactionRef_id = localDB.getTransactionReffIdSequence(out xError);
			xError = null;

			decimal TotalBelanja = 0;

			if (productList.Count == 0) {
				LogWriter.show(this, "Product list empty");
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "442", "No product in list", "");
			}

			for (int i = 0; i < productList.Count; i++) {
				JsonLibs.MyJsonLib aProduct = (JsonLibs.MyJsonLib)productList [i];
				string prdCode = ((string)aProduct ["fiProductCode"]).Trim ();
				string quantity = ((int)aProduct ["fiQuantity"]);

			}


			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "204", "Not implemented YET", "");
		}

	}
}
