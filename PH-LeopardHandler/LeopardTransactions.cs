using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPOBHttpRestData;
using LOG_Handler;
using Payment_Host_Interface;
using StaticCommonLibrary;

namespace PH_LeopardHandler
{
	public class LeopardTransactions: ITransactionInterface
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
        ~LeopardTransactions()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
			lm.Dispose();
            HTTPRestDataConstruct.Dispose();
            jsonConv.Dispose();
			jsonTemp.Dispose ();
            tcp.Dispose();
            localDB.Dispose();
        }

		LeopardModule lm;
        TCPLeopardClient tcp;        
        HTTPRestConstructor HTTPRestDataConstruct;
        JsonLibs.MyJsonLib jsonConv;
		JsonLibs.MyJsonLib jsonTemp;
        PublicSettings.Settings commonSettings;
        PPOBDatabase.PPOBdbLibs localDB;
        Exception xError;

        public string securityToken = "";
		public PPOBDatabase.PPOBdbLibs.ProductTypeEnum TrxType;
		public JsonLibs.MyJsonLib AdditionalJson=null;

		private string inqBillReff = "";

        public LeopardTransactions(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            LOG_Handler.LogWriter.showDEBUG(this, "Leopard > "
                + commonSettings.getString("LeopardQueueHost") + ":"
                + commonSettings.getInt("LeopardQueuePort"));
            tcp = new TCPLeopardClient(commonSettings.getString("LeopardQueueHost"),
                commonSettings.getInt("LeopardQueuePort"));
            jsonConv = new JsonLibs.MyJsonLib();
			jsonTemp = new JsonLibs.MyJsonLib ();
            HTTPRestDataConstruct = new HTTPRestConstructor();
			lm = new LeopardModule(commonSettings);
            //localDB = new PPOBDatabase.PPOBdbLibs(commonSettings);
            localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
                commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
        }

		// fungsi untuk pembelian/pembayaran pulsa lewat leopard
        public bool productTransaction(string appID, string userId, string transactionReference,
            string providerProductCode, string providerAmount, ref string HttpReply, ref int traceNumber,
            ref string strJson, ref DateTime trxTime, ref string strRecJson, ref DateTime trxRecTime,
            ref string failedReason, ref bool canReversal, ref bool isSuccessPayment, int transactionType,
            string trxNumber)
        {
			int isoType = 3;	// pembelian

            trxTime = DateTime.Now;
            traceNumber = localDB.getNextProductTraceNumber();
            //string systemTrxId = traceNumber.ToString().PadLeft(12, '0');

			string provProductCode = providerProductCode;
			string sTrxType;

			try{
				if((AdditionalJson!= null) && (AdditionalJson.isExists ("fiBillReff"))){
					provProductCode += "." + ((string)AdditionalJson["fiBillReff"]).Trim ();
					sTrxType = "payment";
				} else 				
					sTrxType = "purchase";

			}catch{
				failedReason = "Additional billReff not found";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "494",failedReason,"");
				return false;
			}

//			if (inqBillReff != "")	// pembayaran
//				provProductCode += "." + inqBillReff;

            failedReason = "";
            canReversal = false;
            isSuccessPayment = false;

            //string sRecIso = "";
            //string productCode = "PRE25";
            //long reffNum = localDB.getNextProductReferenceNumber();
            long reffNum = traceNumber;

            // 1. create ISO MSG
			byte[] iso = lm.generateTransactionJson(transactionReference, provProductCode,
				reffNum, sTrxType, ref strJson);

            try
            {
                tcp.CheckConn();
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Connect to Leopard Queue host has failed : " + ex.getCompleteErrMsg());
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Connect to Leopard Queue host has failed", "");
                strRecJson = ""; trxRecTime = trxTime;
                failedReason = "Failed to connect to Leopard Queue host";
                return false;
            }

            //Console.WriteLine("DEBUG== Send Msg");
            try
            {
                // 2. Send ISO Msg
                tcp.Send(iso);
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error while send to Leopard Queue host : " + ex.getCompleteErrMsg());
                HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Disconnected from Leopard Queue host", "");
                strRecJson = ""; trxRecTime = trxTime;
                failedReason = "Failed while send the.StackTrace to Leopard Queue host : " + ex.getCompleteErrMsg();
                return false;
            }

            canReversal = true;

            LogWriter.show(this, "Reading ISO");
            DateTime skrg = DateTime.Now;

            string fiToken = "TMPFIXED";
            //string trxNumber = localDB.getProductTrxNumber(out xError);

            // 3. Read Balasan ISO Msg
            byte[] ret;
            string sRet = "";
            try
            {
                ret = tcp.Read(commonSettings.getInt("BillerLeopardTimeOut"), ref sRet);
            }
            catch (Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error while read from Leopard Queue host : " + ex.getCompleteErrMsg());
                if ((isoType == 3) && (transactionType == 0) && canReversal)
                {
                    jsonConv.Clear();
                    jsonConv.Add("fiToken", securityToken);
					jsonTemp.Clear ();
					jsonTemp.Add ("MESSAGE", "Need reversal");
					jsonConv.Add("fiPrivateData", jsonTemp);
                    jsonConv.Add("fiResponseCode", "99");
					jsonConv.Add("fiTransactionId", "LEO" + traceNumber.ToString().PadLeft(6, '0'));
					//jsonConv.Add("fiToken", fiToken);
                    jsonConv.Add("fiTrxNumber", trxNumber);
                    // jika transaksi pembayaran, pake return fiReversalAllowed
                    if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "No response from Leopard Queue host",
                        jsonConv.JSONConstruct());
                }
                else
                {
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "No response from Leopard Queue host", "");
                }
                strRecJson = sRet; trxRecTime = skrg;
                failedReason = "Failed while read data from Leopard queue host : " + ex.getCompleteErrMsg();
                return false;
            }
            skrg = DateTime.Now;
            strRecJson = sRet; trxRecTime = skrg;

            if (ret == null)
            {
                //Console.WriteLine("No response from provider");
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "No response from Leopard Queue host");
                if ((isoType == 3) && (transactionType == 0) && canReversal)
                {
                    jsonConv.Clear();
                    jsonConv.Add("fiToken", securityToken);
					jsonTemp.Clear ();
					jsonTemp.Add ("MESSAGE", "Need reversal");
					jsonConv.Add("fiPrivateData", jsonTemp);
                    jsonConv.Add("fiResponseCode", "99");
					jsonConv.Add("fiTransactionId", "LEO" + traceNumber.ToString().PadLeft(6, '0'));
					//jsonConv.Add("fiToken", fiToken);
                    jsonConv.Add("fiTrxNumber", trxNumber);
                    // jika transaksi pembayaran, pake return fiReversalAllowed
                    if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "No response from Leopard Queue host",
                        jsonConv.JSONConstruct());
                }
                else
                {
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "No response from Leopard Queue host", "");
                }
                failedReason = "Failed to process, return from Leopard queue host is null";
                return false;
            }


            // 4. Check Data Signature apakah sama ?
            //if (!pm.CheckDataSignature(strRecJson, isoType))
            //{
            //    //Console.WriteLine("Signature beda");
            //    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Signature not match");
            //    if ((isoType == 3) && (transactionType == 0) && canReversal)
            //    {
            //        jsonConv.Clear();
            //        jsonConv.Add("fiToken", securityToken);
            //        jsonConv.Add("fiPrivateData", "Need reversal");
            //        jsonConv.Add("fiResponseCode", "99");
			//        jsonConv.Add("fiTransactionId", "LEO" + traceNumber.ToString().PadLeft(6, '0'));
			//        //jsonConv.Add("fiToken", fiToken);
            //        jsonConv.Add("fiTrxNumber", trxNumber);
            //        // jika transaksi pembayaran, pake return fiReversalAllowed
            //        if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

            //        HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Error signature code",
            //            jsonConv.JSONConstruct());
            //    }
            //    else
            //    {
            //        HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "Error signature code", "");
            //    }
            //    failedReason = "Data signature not match with Finnet queue host";
            //    return false;
            //}
            //Console.WriteLine("Signature Sarua");

            if (!jsonConv.JSONParse(System.Text.Encoding.UTF8.GetString(ret)))
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Could not parse data from host");
                if ((isoType == 3) && (transactionType == 0) && canReversal)
                {
                    jsonConv.Clear();
                    jsonConv.Add("fiToken", securityToken);
					jsonTemp.Clear ();
					jsonTemp.Add ("MESSAGE", "Need reversal");
					jsonConv.Add("fiPrivateData", jsonTemp);
                    jsonConv.Add("fiResponseCode", "99");
					jsonConv.Add("fiTransactionId", "LEO" + traceNumber.ToString().PadLeft(6, '0'));
					//jsonConv.Add("fiToken", fiToken);
                    jsonConv.Add("fiTrxNumber", trxNumber);
                    // jika transaksi pembayaran, pake return fiReversalAllowed
                    if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Invalid data from host",
                        jsonConv.JSONConstruct());
                }
                else
                {
                    HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "Invalid data from host", "");
                }
                failedReason = "Could not parse Json data from Leopard queue host : " + System.Text.Encoding.UTF8.GetString(ret);
                return false;
            }

			// Balasan dr Antrian Leopard :
//			{
//				"MESSAGE":"GAGAL BALANCE 0 / TIDAK DITEMUKAN",
//				"CUSTNO":"082214441898",
//				"BALANCE":"~",
//				"SIGNATURE":"4835385981EE2DBD17DA6BF436E409205C0567CE83D53FAEC267C046119654E960B049AE0BBAE94F187E0F65EE76B6D1161FFFB897B6E71DC14061AF8B8C7BDD30654BEE85D98F0E184D6C3EF15ACD4FCC372D9E8395B6F1B1DF61E6CC32AE145334EA2E7BAAD906BCCD70C5AC98F99E8AF631198D4996A72A3A371C083DF7EF",
//				"TRXID":"000000003330",
//				"DEBET":"~",
//				"RC":"61",
//				"BILLREFF":"53000590422",
//				"DATE":"2014-11-27,00",
//				"TRX":"HTS"
//			}

			// DEBUG
			LogWriter.showDEBUG (this, "Dari Leopard Queue: " + jsonConv.JSONConstruct ());

			string fiResponseCode;
			string message;
			try{
				message = ((string)jsonConv["MESSAGE"]).Trim();
				fiResponseCode = ((string)jsonConv["RC"]).Trim();
			}catch{
				failedReason = "No MESSAGE or RC Field from Leopard Queue";
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", failedReason, "");
				return false;
			}

			//string fiAmount = ((string)jsonConv["Amount"]).Trim();
			//string fiResponseCode = ((string)jsonConv["RC"]).Trim();
			//string fiPrivateData = transactionReference;
			//JsonLibs.MyJsonLib fiPrivateData;

            canReversal = false;
            // Jika response code ??, artinya reversal sebelumnya sudah sukses.
			//if (fiResponseCode == "00") isSuccessPayment = true;
			if (fiResponseCode == "00") {
				isSuccessPayment = true;
			} else if (fiResponseCode == "68") {
				// Time out anggap berhasil ALIAS, GANTUNG BAE
				canReversal = true;			// supaya di gantung
				isSuccessPayment = true;
			} else {
				//if (fiPrivateData == "") fiPrivateData = "..";
				//jsonReply.Add("fiPrivateData", fiPrivateData);
				canReversal = false;		// supaya otomatis di reversal
				isSuccessPayment = false;
			}

			//JsonLibs.MyJsonLib jsonReply = new JsonLibs.MyJsonLib ();
			jsonConv.Clear();
			jsonConv.Add("fiToken", securityToken);

			jsonTemp.Clear ();
			jsonTemp.Add("fiMessage", message);

			jsonConv.Add ("fiPrivateData", jsonTemp);		//jsonConv ["MESSAGE"]);

			jsonConv.Add("fiResponseCode", fiResponseCode);
			jsonConv.Add("fiTransactionId", "LEO" + traceNumber.ToString());
			//jsonConv.Add("fiToken", fiToken);
			jsonConv.Add("fiTrxNumber", trxNumber);
            // jika transaksi pembayaran, pake return fiReversalAllowed
			if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

			HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
			LogWriter.write(this, LogWriter.logCodeEnum.INFO, "Transaction success to Leopard : " + "LEO" + traceNumber.ToString());
			//jsonReply.Dispose ();
			if (canReversal && isSuccessPayment)
				return false; // gantung
			else
	            return true;

        }

		public bool productInquiry(string appID, string userId, string customerNumber,
			string providerProductCode, int adminFee, bool fIncludeAdminFee, ref int productAmount, ref string HttpReply,
			ref int traceNumber, ref string strJson, ref DateTime trxTime, ref string strRecJson,
			ref DateTime trxRecTime, string trxNumber)
		{
			int isoType = 2;	// inquiry

			trxTime = DateTime.Now;
			traceNumber = localDB.getNextProductTraceNumber();
			//string systemTrxId = traceNumber.ToString().PadLeft(12, '0');

			//long reffNum = localDB.getNextProductReferenceNumber();
			long reffNum = traceNumber;

			// 1. create ISO MSG
			byte[] iso = lm.generateTransactionJson(customerNumber, 
				providerProductCode.Replace (".PAY",".INQ"),	// tina BPL.PAY jadi BPL.INQ
				reffNum, "inquiry", ref strJson);

			try
			{
				tcp.CheckConn();
			}
			catch (Exception ex)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Connect to Leopard Queue host has failed : " + ex.getCompleteErrMsg());
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Connect to Leopard Queue host has failed", "");
				strRecJson = ""; trxRecTime = trxTime;
				return false;
			}

			//Console.WriteLine("DEBUG== Send Msg");
			try
			{
				// 2. Send ISO Msg
				tcp.Send(iso);
			}
			catch (Exception ex)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error while send to Leopard Queue host : " + ex.getCompleteErrMsg());
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Disconnected from Leopard Queue host", "");
				strRecJson = ""; trxRecTime = trxTime;
				return false;
			}

			LogWriter.show(this, "Reading ISO");
			DateTime skrg = DateTime.Now;

			//string trxNumber = localDB.getProductTrxNumber(out xError);

			// 3. Read Balasan ISO Msg
			byte[] ret;
			string sRet = "";
			try
			{
				ret = tcp.Read(commonSettings.getInt("BillerLeopardTimeOut"), ref sRet);
			}
			catch (Exception ex)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Error while read from Leopard Queue host : " + ex.getCompleteErrMsg());
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "No response from Leopard Queue host", "");
				strRecJson = sRet; trxRecTime = skrg;
				return false;
			}
			skrg = DateTime.Now;
			strRecJson = sRet; trxRecTime = skrg;

			if (ret == null)
			{
				//Console.WriteLine("No response from provider");
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "No response from Leopard Queue host");
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "No response from Leopard Queue host", "");
				return false;
			}


			// 4. Check Data Signature apakah sama ?
			//if (!pm.CheckDataSignature(strRecJson, isoType))
			//{
			//    //Console.WriteLine("Signature beda");
			//    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Signature not match");
			//    if ((isoType == 3) && (transactionType == 0) && canReversal)
			//    {
			//        jsonConv.Clear();
			//        jsonConv.Add("fiToken", securityToken);
			//        jsonConv.Add("fiPrivateData", "Need reversal");
			//        jsonConv.Add("fiResponseCode", "99");
			//        jsonConv.Add("fiTransactionId", "LEO" + traceNumber.ToString().PadLeft(6, '0'));
			//        //jsonConv.Add("fiToken", fiToken);
			//        jsonConv.Add("fiTrxNumber", trxNumber);
			//        // jika transaksi pembayaran, pake return fiReversalAllowed
			//        if ((isoType == 3) && (transactionType == 0)) jsonConv.Add("fiReversalAllowed", canReversal);

			//        HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Error signature code",
			//            jsonConv.JSONConstruct());
			//    }
			//    else
			//    {
			//        HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "Error signature code", "");
			//    }
			//    failedReason = "Data signature not match with Finnet queue host";
			//    return false;
			//}
			//Console.WriteLine("Signature Sarua");

			LogWriter.show (this,"Balikan Leopard sRet = " + sRet + "\r\nRetByte = " + 
				System.Text.Encoding.UTF8.GetString(ret));

			if (!jsonConv.JSONParse(System.Text.Encoding.UTF8.GetString(ret)))
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Could not parse data from host");
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "499", "Invalid data from host", "");
				return false;
			}

			// Balasan dr Antrian Leopard :
//			FORMAT JSON = {
//				"TRXID":"140305038871",
//				"RC":"00",
//				"BILLREFF":"5000004056",
//				"CUSTNO":"543400042616",
//				"DATE":"2014-06-18,07",
//				"MESSAGE":"IDPEL : 543400042616|TOTAL BAYAR : Rp. 170.618|ADMIN : Rp. 1.600|NAMA : JASIP BIN GENI|
//					LEMBAR TAG : 1|RP TAG PLN : Rp. 169.018|BL/TH : JUN14|TARIF/DAYA : R1/1300VA",
//				"TRX":"BPL"
//			}

			// DEBUG
			//LogWriter.showDEBUG (this, "Dari Leopard Queue: " + jsonConv.JSONConstruct ());

			//string fiAmount = ((string)jsonConv["Amount"]).Trim();
			string fiResponseCode = "";
			string fiAmount;
			string billReff;
			string sDate;
			string trxId;

			string sTrxDetail;
			string message;

			try
			{
//				fiPrivateData=(JsonLibs.MyJsonLib)jsonConv["MESSAGE"];
//				if(fiPrivateData.isExists ("TOTAL BAYAR")){
//					fiAmount = ((string)fiPrivateData["TOTAL BAYAR"]).Trim().ToUpper ().Replace ("RP","").Replace (".","");
//				}
//				else 
//					fiAmount = "0";

				message = ((string)jsonConv["MESSAGE"]).Trim();
				// ambil total bayar
				//fiAmount = ambilTotalBayar(message);

				fiResponseCode = ((string)jsonConv["RC"]).Trim();
				productAmount = ambilTotalBayar(message);	//int.Parse(fiAmount);
				billReff = ((string)jsonConv["BILLREFF"]).Trim();
				trxId = ((string)jsonConv["TRXID"]).Trim();
				sDate = ((string)jsonConv["DATE"]).Trim();
			} catch {
				HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Invalid data fields from host", "");
				return false;
			}

			int fiAdminFee = adminFee;

			if (fIncludeAdminFee)
			{
				fiAdminFee = 0;
			}

			if ((trxId == null) || (trxId == ""))
				trxId = "\"\"";

			jsonTemp.Clear ();
			jsonTemp.Add("fiBillerBillReff", billReff);
			jsonTemp.Add("fiBillerTrxId", trxId);
			jsonTemp.Add("fiBillerTrxDate", sDate);
			jsonTemp.Add("fiMessage", message);

			jsonConv.Clear();
			jsonConv.Add("fiAmount", productAmount);
			jsonConv.Add("fiPrivateData", jsonTemp);
			jsonConv.Add("fiResponseCode", fiResponseCode);
			jsonConv.Add("fiToken", securityToken);
			jsonConv.Add("fiTrxNumber", trxNumber);
			jsonConv.Add("fiAdminFee", fiAdminFee);
			//jsonConv.Add ("fiServerAdditional", jsonTemp);

			HttpReply = HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
			return true;
		}

		//LAYANAN:001001|NO TLP:0021008201593|NAMA: ADITYA RIYADI SOEROSO MSEE|LEMBAR TAG:1|BL/TH:DES14|ADMIN:RP. 3.000|RP TAG:RP. 503.058|TOTAL BAYAR:RP. 506.058

		private int ambilTotalBayar(string msg){
			int amount;
			string MSG = msg.ToUpper ();
			int indx = MSG.IndexOf ("TOTAL");
			if (indx < 0)
				return 0;
			indx = MSG.IndexOf ("BAYAR",indx);
			if (indx < 0)
				return 0;
			MSG = MSG.Substring (indx);
			indx = MSG.IndexOf ("RP");	// cari RP setelah TOTAL BAYAR
			if (indx < 0)
				return 0;
			indx += 3;	// dari MSG
			MSG = MSG.Substring (indx);
			// cari char berikutnya sampe ujungnya bukan angka atau spasi
			string val = "";

			foreach(char ch in MSG){
				if (char.IsDigit (ch))
					val += ch;
				else if (ch == '.')
					continue;
				else if (!char.IsWhiteSpace (ch))
					break;
			}

			int.TryParse (val,out indx);
			if (indx < 0)
				return 0;
			return indx;
		}
    }
}
