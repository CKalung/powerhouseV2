using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

using PPOBHttpRestData;
using StaticCommonLibrary;
using LOG_Handler;

namespace Process_ProductTransaction
{
	public class BarcodePayment : IDisposable {
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
		~BarcodePayment()
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


		public BarcodePayment (HTTPRestConstructor.HttpRestRequest ClientData,
			PublicSettings.Settings CommonSettings)
		{
			clientData = ClientData;
			commonSettings = CommonSettings;
			HTTPRestDataConstruct = new HTTPRestConstructor();
			jsonConv = new JsonLibs.MyJsonLib();
			localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
				commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
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
				httpRepl = HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No session token or invalid token field", "");
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

		public string TransferByProduct(string productCode){
			string[] fields = { "fiApplicationId", "fiTargetPhone",  "fiPhone", "fiAmount", "fiToken" };

			string appID = "";
			string targetPhone = "";
			string userPhone = "";
			string securityToken = "";
			int amount = 0;
			Exception xError = null;

			if (!jsonConv.JSONParse (clientData.Body)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data format", "");
			}

			if (!checkMandatoryFields (fields)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "406", "Mandatory fields not found", "");
			}

			try
			{
				appID = ((string)jsonConv["fiApplicationId"]).Trim();
				securityToken = ((string)jsonConv["fiToken"]).Trim();
				amount = ((int)jsonConv["fiAmount"]);
				targetPhone = ((string)jsonConv["fiTargetPhone"]).Trim();
				userPhone = ((string)jsonConv["fiPhone"]).Trim().Replace("-", "");
			}
			catch
			{
				// field tidak ditemukan atau formatnya salah
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not completed", "");
			}
			if (amount <= 0)
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid amount", "");
			}

			ReformatPhoneNumber(ref userPhone);
			ReformatPhoneNumber(ref targetPhone);

			string httprepl = "";
			if (!cek_TokenSecurity (userPhone, jsonConv, ref securityToken, ref httprepl)) {
				return httprepl;
			}
			CommonLibrary.SessionResetTimeOut (userPhone);

			// password ok

			if (!localDB.isPhoneExistAndActive(targetPhone, out xError))
			{
				if (xError != null)
				{
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
				}
				else
				{
					// password error
					return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Target phone not registered", "");
				}
			}


			string errCode = "";
			string errMessage = "";
			Exception exrr = null;
			int adminFee = 0;
			int productAmount = amount;

			string trxNumber = localDB.getProductTrxNumber (out xError);	// .getCashInTrxNumber(out xError);
			string traceNumb = localDB.getNextProductTraceNumberString (out xError);
			DateTime trxTime = DateTime.Now;

			long TransactionRef_id = localDB.getRegulerTrxReffIdSequence(out xError);

			PPOBDatabase.PPOBdbLibs.ProviderProductInfo providerProduct;
			//Console.WriteLine("ProductCode = " + productCode);
			try
			{
				providerProduct = localDB.getProviderProductInfo(productCode, out exrr);
				if (exrr != null)
				{
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Provider product data not found", "");
				}
			}
			catch
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed on geting provider product data", "");
			}

			try
			{
				//Console.WriteLine(" productAmount " + productAmount);
				//Console.WriteLine(" intNYA : " + int.Parse(productAmount));
				//if (!localDB.getAdminFeeAndCustomerFee(productCode, providerProduct.ProviderCode, amount,
				if (!localDB.getAdminFeeAndCustomerFee(productCode,1, appID, amount,
					ref adminFee, out xError))
				{
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Fee data not found", "");
				}

			}
			catch (Exception ex)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error get fee data : " + ex.getCompleteErrMsg());
				//Console.WriteLine(ex.StackTrace);
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Error get fee data", "");
			}


			// SERVICE/PEMBAYARAN amount diambil dari productAmount dari client, 
			// jika pembelian, amount diambil dari currentPrice
			int nilaiTransaksiKeProvider = 0;
			int nilaiYangMasukLog = 0;

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
			if (providerProduct.CurrentPrice <= 0)
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Bad amount", "");
			}

			//productAmount = prdAmount.ToString();
			nilaiTransaksiKeProvider = providerProduct.CurrentPrice;
			string qvaInvoiceNumber = "";
			bool qvaReversalRequired = false;

			using (TransferReguler.CollectTransfer TransferReg =
				new TransferReguler.CollectTransfer(commonSettings))
			{
				int totalTransfer = amount;
				try
				{
					LogWriter.write(this, LogWriter.logCodeEnum.INFO, "Transfer from user " + userPhone + " to " + targetPhone);
					//LogWriter.show(this, "Transfer from user " + userPhone + " to " + targetPhone);
					if (!TransferReg.TransferCustomerToCustomer(userPhone, targetPhone, 
						commonSettings.getString("TRANSFER_SUFIX_CUSTOMER_TO_CUSTOMER"), 
						totalTransfer,
						TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.TransferReguler,
						"Simple regular transfer", ref qvaInvoiceNumber, ref qvaReversalRequired, 
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
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Failed to Transfer", "");
				}
			}

			// Disini simpan transaction log seperti transaksi product
			LogWriter.showDEBUG(this, "=========== BERES TRANSAKSI LANGSUNG INSERT LOG");

			// Transfer reguler untuk Fee dan pembagian Pot Bagi dilakukan di service lain
			// dengan trigger dari insert ini
			if (!localDB.insertCompleteRegularTransactionLog(TransactionRef_id, productCode, providerProduct.ProviderProductCode,
				userPhone, targetPhone,
				nilaiYangMasukLog.ToString(), traceNumb.ToString(), trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
				adminFee.ToString(), providerProduct.ProviderCode, providerProduct.CogsPriceId,
				0, 0, "", trxTime.ToString("yyyy-MM-dd HH:mm:ss"), "", trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
				"TransferCustomerToCustomer",
				trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
				"TransferCustomerToCustomer",
				trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
				true, "", trxNumber, false, providerProduct.fIncludeFee,
				out xError))
			{
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "FATAL ERROR, Can't insert transaction log.");
				// Jadwalkan masuk database
				//				TransactionLog.write(TransactionRef_id, productCode, providerProduct.ProviderProductCode, providerProduct.ProviderCode,
				//					userId.Substring(commonSettings.getString("UserIdHeader").Length), customerProductNumber,
				//					productAmount.ToString(), nilaiYangMasukLog, adminFee,
				//					providerProduct.CogsPriceId,
				//					traceNumb.ToString(), trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
				//					0, 0, "", skrg.ToString("yyyy-MM-dd HH:mm:ss"), "", skrg.ToString("yyyy-MM-dd HH:mm:ss"), strJson,
				//					trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
				//					strRecJson,
				//					trxRecTime.ToString("yyyy-MM-dd HH:mm:ss"),
				//					isSuccessPayment, failedReason, canReversal);
			}

			Hashtable userInfo = localDB.getLoginInfoByUserPhone(userPhone, out xError);
			// siapkan json untuk notifikasi ke agen
			DateTime skrg = DateTime.Now;
			jsonConv.Clear();
			jsonConv.Add("fiAgentPhone", userPhone);
			jsonConv.Add("fiAgentName", (string)userInfo["firstName"] + " " + (string)userInfo["lastName"]);
			jsonConv.Add("fiAmount", amount);
			jsonConv.Add("fiTransferType", "03");
			jsonConv.Add("fiCode", "00");
			jsonConv.Add("fiNotificationDateTime", skrg.ToString("yyyy-MM-dd HH:mm:ss"));
			jsonConv.Add("fiTrxNumber", trxNumber);
			string subJson = jsonConv.JSONConstruct();

			// Insert di tabel notifikasi saja
			localDB.insertNotificationQueue(userPhone, targetPhone, skrg.ToString("yyyy-MM-dd HH:mm:ss"),
				"07", subJson, out xError);

			jsonConv.Clear();
			jsonConv.Add("fiReplyCode", "00");
			//            jsonConv.Add("fiAdminFee", adminFee);
			jsonConv.Add("fiAmount", amount);
			jsonConv.Add("fiCustomerPhone", targetPhone);
			jsonConv.Add("fiTrxNumber", trxNumber);
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
			//return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", "{\"fiReplyCode\":\"00\"}");
		}

	}
}

