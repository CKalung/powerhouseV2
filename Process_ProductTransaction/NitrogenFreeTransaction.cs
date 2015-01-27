using System;
using PPOBHttpRestData;
using System.Collections;
using System.Collections.Generic;
using LOG_Handler;
using StaticCommonLibrary;

namespace Process_ProductTransaction
{
	public class NitrogenFreeTransaction : IDisposable {
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
		~NitrogenFreeTransaction()
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


		public NitrogenFreeTransaction (HTTPRestConstructor.HttpRestRequest ClientData,
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

		public string FreePurchase(){
			// Jika TransactionType : 0 => Pembayaran, 1 => Pembelian
			// input dari user diambil dari username dan password, baru bisa inquiry ke host
			int productAmount = 0;
			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}

			LogWriter.show(this, "Json received from client :\r\n" + clientData.Body);

			if ((!jsonConv.ContainsKey("fiPhone")) || 
				(!jsonConv.ContainsKey("fiApplicationId")) || 
				(!jsonConv.ContainsKey("fiRequestCode")) || 
				(!jsonConv.ContainsKey("fiCustomerNumber")) || 
				(!jsonConv.ContainsKey("fiProductCode")) ||
				(!jsonConv.ContainsKey("fiTransactionType")))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory field not found", "");
			}

			string appID="";
			string unFormatedUserPhone = "";
			string userPhone="";
			string customerProductNumber = "";
			string productCode = "";
			int transactionType = 0;

			string description = "";

			try
			{
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
				userPhone = ((string)jsonConv["fiPhone"]).Trim();
				customerProductNumber = ((string)jsonConv["fiCustomerNumber"]).Trim();
				productCode = ((string)jsonConv["fiProductCode"]).Trim();
				transactionType = (int)jsonConv["fiTransactionType"];
				description = (string)((JsonLibs.MyJsonLib)jsonConv["fiAdditional"])["fiDescription"];
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid data field", "");
			}

			unFormatedUserPhone = userPhone;
			ReformatPhoneNumber(ref userPhone);

			if (jsonConv.ContainsKey("fiAmount"))
			{
				try
				{
					productAmount = (int)jsonConv["fiAmount"];
				}
				catch
				{
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Bad amount", "");
				}
			}

			int requestCode = 0;

			try{
				requestCode = int.Parse(((string)jsonConv["fiRequestCode"]).Trim());
			}
			catch {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Undefined request code", "");
			}
			string userId = cUserIDHeader + userPhone;

			if (userPhone.Length == 0)
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
			}

			// perbaharui token hanya pada saat login saja
			string securityToken = "";
			if (jsonConv.ContainsKey("fiToken"))
			{
				try
				{
					securityToken = ((string)jsonConv["fiToken"]).Trim();
				}
				catch{ 
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No fiToken field", "");
				}
			}else 
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No fiToken field", "");

			// cek token disini
			if (!cek_SecurityToken (userPhone, securityToken)) {
				LOG_Handler.LogWriter.showDEBUG (this, "Cek Token Session: " + userPhone + 
					", token: " + securityToken);
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "504", "Invalid session", "");
			}


			//  Yang harus disiapkan
			// switch berdasarkan requestCode
			switch(requestCode)
			{
			//case 0: // inquiry
				//return ProviderInquiry(appID, userId, securityToken, customerProductNumber, productCode);
				//return Finnet.productInquiry(appID, userId, customerNumber, productCode);
			case 1: //transaction
				return SaveFreeTransaction(appID, userId, securityToken, transactionType,
						customerProductNumber, productCode, productAmount,
					description, userPhone);
//			case 2: //reversal
//				return ProviderReversal(appID, userId, securityToken, transactionType,
//					customerProductNumber, productCode, productAmount);
//			case 3: //reversal
//				return ProviderReversalUlang(appID, userId, securityToken, transactionType,
//					customerProductNumber, productCode, productAmount);
				//return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "UnImplmented yet", "");
			default:
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Undefined request code", "");
			}
		}

		private string SaveFreeTransaction(string appID, string userId, string securityToken, int PpobType,
				string customerProductNumber, string productCode, int productAmount,
				string description, string userPhone = ""){

			Exception xError = null;

			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct = null;
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

			string providerCode = providerProduct.ProviderCode;

			long TransactionRef_id = localDB.getTransactionReffIdSequence(out xError);

			xError = null;

			DateTime skrg = DateTime.Now;
			int traceNumber = localDB.getNextProductTraceNumber();
			string trxNumber = localDB.getProductTrxNumber(out xError);

			if (!localDB.insertCompleteTransactionLog(TransactionRef_id, productCode, providerProduct.ProviderProductCode,
				userId.Substring(commonSettings.getString("UserIdHeader").Length), customerProductNumber,
				"0", traceNumber.ToString(), skrg.ToString("yyyy-MM-dd HH:mm:ss"),
				"0", providerProduct.ProviderCode, providerProduct.CogsPriceId,
				0, 0, "", skrg.ToString("yyyy-MM-dd HH:mm:ss"), "", skrg.ToString("yyyy-MM-dd HH:mm:ss"),
				"",
				skrg.ToString("yyyy-MM-dd HH:mm:ss"),
				"",
				skrg.ToString("yyyy-MM-dd HH:mm:ss"),
				true,
				description, trxNumber, false, true,
				"","",out xError))
			{
				// gagal masuk database
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on insert Transaction Log for Free purchase");
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "497", "Failed on insert Transaction Log for Free purchase", "");
			}

			jsonConv.Clear();
			jsonConv.Add("fiToken", securityToken);
			jsonConv.Add("fiPrivateData", "");
			jsonConv.Add("fiResponseCode", "00");
			jsonConv.Add("fiTransactionId", "IcP" + traceNumber.ToString().PadLeft(6, '0'));
			jsonConv.Add("fiTrxNumber", trxNumber);
			jsonConv.Add("fiReversalAllowed", false);
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());

		}

	}
}

