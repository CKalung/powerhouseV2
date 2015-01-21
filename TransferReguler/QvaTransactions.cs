using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPOBHttpRestData;
using PPOBServerHandler;

namespace TransferReguler
{
    public class QvaTransactions: IDisposable
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
        ~QvaTransactions()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            HTTPRestDataConstruct.Dispose();
            jsonConv.Clear();
            jsonConv.Dispose();
        }

        PublicSettings.Settings commonSettings;
        HTTPRestConstructor HTTPRestDataConstruct;
        JsonLibs.MyJsonLib jsonConv;
        PPOBDatabase.PPOBdbLibs localDB;
        Exception xError;

        public QvaTransactions(PPOBDatabase.PPOBdbLibs LocalDB, PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            HTTPRestDataConstruct = new HTTPRestConstructor();
            jsonConv = new JsonLibs.MyJsonLib();
            localDB = LocalDB;
        }

        private void ReformatPhoneNumber(ref string phone)
        {
            phone = phone.Trim();
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

        //public string TransferBalance(HTTPRestConstructor.HttpRestRequest clientData)
        //{
        //    //              /gateway/010130
        //    //            {"sender" : <String, not null>, "amount" : <Double, not null>, 
        //    //              "receiver" : <String, not null>, "channelId" : <String, not null>,     
        //    //              "transactionCode" : <String, not null> } 
        //    if (clientData.Body.Length == 0)
        //    {
        //        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
        //    }
        //    if (!jsonConv.JSONParse(clientData.Body))
        //    {
        //        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
        //    }
        //    // TAMPUNG data json dari Client

        //    string passw = "";
        //    string userPhone = "";
        //    string targetPhone = "";
        //    int amount = 0;
        //    try
        //    {
        //        passw = ((string)jsonConv["fiPassword"]).Trim();
        //        amount = ((int)jsonConv["fiAmount"]);
        //        targetPhone = ((string)jsonConv["fiTargetPhone"]).Trim();
        //        userPhone = ((string)jsonConv["fiPhone"]).Trim().Replace("-", "");
        //    }
        //    catch
        //    {
        //        // field tidak ditemukan atau formatnya salah
        //        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not completed", "");
        //    }
        //    if (amount <=0)
        //    {
        //        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid amount", "");
        //    }

        //    ReformatPhoneNumber(ref userPhone);
        //    ReformatPhoneNumber(ref targetPhone);

        //    if (!localDB.isUserPasswordEqual(userPhone, passw, out xError))
        //    {
        //        if (xError != null)
        //        {
        //            return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
        //        }
        //        else
        //        {
        //            // password error
        //            return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Login failed", "");
        //        }
        //    }
        //    // password ok

        //    string fromId = commonSettings.getString("UserIdHeader") + userPhone;
        //    string targetId = commonSettings.getString("UserIdHeader") + targetPhone;
        //    string channelId = "6014";
        //    string transCode = "400014";

        //    string errCode ="";
        //    string errMessage = "";

        //    if (!Transfer(fromId, targetId, amount, channelId, transCode, 
        //        ref errCode, ref errMessage))
        //        return HTTPRestDataConstruct.constructHTTPRestResponse(400, errCode, errMessage, "");
        //    else
        //    {
        //        return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", "{\"fiReplyCode\":\"00\"}");
        //    }

        //}

        public bool TransferTerjadwal(string senderId, string receiverId, int amount,
            string channelId, string transactionCode,
            long TransactionRef_id,
            PPOBDatabase.PPOBdbLibs.eTransactionType transactionType,
            string description, bool isFee,
            ref string errorCode, ref string errorMessage)
        {
            // masukan log disini
            DateTime skrg = DateTime.Now;
            localDB.insertQVATransferLog(
                senderId,
                receiverId,
                amount, skrg.ToString("yyyy-MM-dd HH:mm:ss"),
                TransactionRef_id, transactionType, description, isFee, false,
				skrg.ToString("yyyy-MM-dd HH:mm:ss"),"",transactionCode, out xError);
            return true;
        }

		public bool Transfer(string senderId, string receiverId, int amount, 
			string channelId, string transactionCode, 
			long TransactionRef_id,
			PPOBDatabase.PPOBdbLibs.eTransactionType transactionType,
			string description, bool isFee, ref string invoiceNumber,
			ref string errorCode, ref string errorMessage, ref bool qvaReversalReq, string qvaSourceId="")
		{
			return Transfer (senderId, receiverId, amount, channelId, transactionCode, 
				TransactionRef_id, transactionType, description, isFee, ref invoiceNumber, false,
				ref errorCode, ref errorMessage, ref qvaReversalReq, qvaSourceId);
		}

        public bool Transfer(string senderId, string receiverId, int amount, 
            string channelId, string transactionCode, 
            long TransactionRef_id,
            PPOBDatabase.PPOBdbLibs.eTransactionType transactionType,
			string description, bool isFee, ref string invoiceNumber, bool fFORCE_TRANSFER,
			ref string errorCode, ref string errorMessage, ref bool qvaReversalReq, string qvaSourceId="")
        {
			DateTime tTransferTime = DateTime.Now;
			using (SandraLibs sandra = new SandraLibs(commonSettings.getInt("QvaTimeOut"),
				(commonSettings.getString("SandraUseTcpMethod").ToLower() == "true"),
				commonSettings.getString("SandraHmsUserAuth"),
				commonSettings.getString("SandraHmsSecretKey"),
				commonSettings.getString("SandraAuthId")))
            {
				bool reversalReq = false;
				string qvaSource = commonSettings.getString ("QVA_Registration_Source");
				if (qvaSourceId != "")
					qvaSource = qvaSourceId;

                if (sandra.Transfer(
					commonSettings.getString("SandraHost"), commonSettings.getInt("SandraPort"), 
					(commonSettings.getString("SandraIsHttps").ToLower() == "true"), 
					channelId, transactionCode, senderId, receiverId, amount, ref invoiceNumber,
					qvaSource, ref reversalReq
				))
                {
                    errorCode = "00";
                    errorMessage = "";
                    // masukan log disini

                    //LOG_Handler.LogWriter.write(this, LOG_Handler.LogWriter.logCodeEnum.DEBUG,
                    //    "Transfer BERHASIL, inject transfer log");

                    localDB.insertQVATransferLog(
                        senderId,
                        receiverId,
						amount, tTransferTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        TransactionRef_id, transactionType, description, isFee, true,
						DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), invoiceNumber, 
						transactionCode, out xError);
                    return true;
                }
                else
                {

                    //LOG_Handler.LogWriter.write(this, LOG_Handler.LogWriter.logCodeEnum.DEBUG,
                    //    "Transfer GAGAL, tidak inject transfer log");

					if (fFORCE_TRANSFER) {
						localDB.insertQVATransferLog(
							senderId,
							receiverId,
							amount, tTransferTime.ToString("yyyy-MM-dd HH:mm:ss"),
							TransactionRef_id, transactionType, description, isFee, false,
							DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), invoiceNumber, 
							transactionCode, out xError);
					} else {
						localDB.insertQVATransferFailedLog (
							senderId,
							receiverId,
							amount, tTransferTime.ToString ("yyyy-MM-dd HH:mm:ss"),
							TransactionRef_id, transactionType, description, isFee, true,
							DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss"), invoiceNumber, 
							transactionCode, reversalReq, out xError);
					}
					errorCode = sandra.LastError.ServerCode;
                    errorMessage = sandra.LastError.ServerMessage;
                    return false;
                }
            }
        }

		public bool Reversal(long transactionRefId, string qvaInvoiceNumber, 
			ref string errorCode, ref string errorMessage)
		{
			DateTime tTransferTime = DateTime.Now;
			using (SandraLibs sandra = new SandraLibs(commonSettings.getInt("QvaTimeOut"),
				(commonSettings.getString("SandraUseTcpMethod").ToLower() == "true"),
				commonSettings.getString("SandraHmsUserAuth"),
				commonSettings.getString("SandraHmsSecretKey"),
				commonSettings.getString("SandraAuthId")))
			{
				bool hasil = false;

				if (sandra.Reversal(
					commonSettings.getString("SandraHost"), commonSettings.getInt("SandraPort"), 
					(commonSettings.getString("SandraIsHttps").ToLower() == "true"), qvaInvoiceNumber
				))
				{
					errorCode = "00";
					errorMessage = "";
					// masukan log disini

					//LOG_Handler.LogWriter.write(this, LOG_Handler.LogWriter.logCodeEnum.DEBUG,
					//    "Transfer BERHASIL, inject transfer log");

					hasil = true;
				}
				else
				{

					//LOG_Handler.LogWriter.write(this, LOG_Handler.LogWriter.logCodeEnum.DEBUG,
					//    "Transfer GAGAL, tidak inject transfer log");

					errorCode = sandra.LastError.ServerCode;
					errorMessage = sandra.LastError.ServerMessage;
					hasil = false;
				}
				localDB.insertQvaReversalLog (transactionRefId, qvaInvoiceNumber, hasil, 
					errorCode, errorMessage, out xError);
				// jika error tidak perlu lagi di tulis di log, krn sudah dilakukan didalam fungsi insertQvaReversalLog

				return hasil;
			}

		}

    }
}
