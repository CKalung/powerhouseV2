using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LOG_Handler
{
    public class MPTransactionLog: IDisposable
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
        ~MPTransactionLog()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
        }

        // Log dengan format sbb (1 file per transaksi)
        // DistributorPhone : distributor_phone
        // ProductCode : product_code
        // ProviderCode : provider_code
        // CogsPriceId : cogs_price_id
        // CustomerProductNumber : customerProductNumber
        // Amount : amount
        // BaseAmount : baseAmount
        // CustomerFee : customerFeeAmount
        // TraceNumber : trace_number
        // TransactionTime : yyyy-MM-dd HH:mm:ss
        // BeginBalance : beginBalance
        // EndBalance : endBalance
        // JsonIsoInquiry : json_iso_inquiry
        // JsonIsoInquiryTime : json_iso_inquiry_send_time
        // JsonIsoInquiryReceived : json_iso_inquiry_received
        // JsonIsoTrxInquiryTime : json_iso_trx_inquiry_time
        // JsonIsoTrx : json_iso_trx
        // JsonIsoTrxSendTime : json_iso_trx_send_time
        // JsonIsoTrxReceived : json_iso_trx_received
        // JsonIsoTrxReceivedTime : json_iso_trx_received_time
        // IsSuccess : is_success

        string logPath = "";
        string transactionFile = "Transaction";

        public void setPath(string LogPath)
        {
            logPath = LogPath;
        }

        public void write(long TransactionReffId, string product_code, string provider_product_code, string provider_code, 
            string distributor_phone,
            string customerProductNumber, string amount, int baseAmount, int AdminFee, 
            int cogs_price_id,
            string trace_number, string trx_time, 
            int beginBalance, int endBalance, string json_iso_inquiry, 
            string json_iso_inquiry_send_time, string json_iso_inquiry_received, 
            string json_iso_trx_inquiry_time, string json_iso_trx, string json_iso_trx_send_time,
            string json_iso_trx_received, string json_iso_trx_received_time, bool is_success,
            string failedReason, bool canReversal)
        {
            DateTime skrg = DateTime.Now;
            string message =
            "DistributorPhone : " + distributor_phone + "\r\n\r\n" +
            "ProviderProductCode : " + provider_product_code + "\r\n\r\n" +
            "ProviderCode : " + provider_code + "\r\n\r\n" +
            "CogsPriceId : " + cogs_price_id.ToString() + "\r\n\r\n" +
            "CustomerProductNumber : " + customerProductNumber + "\r\n\r\n" +
            "Amount : " + amount + "\r\n\r\n" +
            "BaseAmount : " + baseAmount.ToString() + "\r\n\r\n" +
            "AdminFee : " + AdminFee.ToString() + "\r\n\r\n" +
            "TraceNumber : " + trace_number + "\r\n\r\n" +
            "TransactionTime : " + trx_time + "\r\n\r\n" +
            "BeginBalance : " + beginBalance + "\r\n\r\n" +
            "EndBalance : " + endBalance + "\r\n\r\n" +
            "JsonIsoInquiry : " + json_iso_inquiry + "\r\n\r\n" +
            "JsonIsoInquiryTime : " + json_iso_inquiry_send_time + "\r\n\r\n" +
            "JsonIsoInquiryReceived : " + json_iso_inquiry_received + "\r\n\r\n" +
            "JsonIsoTrxInquiryTime : " + json_iso_trx_inquiry_time + "\r\n\r\n" +
            "JsonIsoTrx : " + json_iso_trx + "\r\n\r\n" +
            "JsonIsoTrxSendTime : " + json_iso_trx_send_time + "\r\n\r\n" +
            "JsonIsoTrxReceived : " + json_iso_trx_received + "\r\n\r\n" +
            "JsonIsoTrxReceivedTime : " + json_iso_trx_received_time + "\r\n\r\n" +
            "IsSuccess : " + is_success.ToString().ToUpper() + "\r\n\r\n" +
            "FailedReason : " + failedReason + "\r\n\r\n" +
            "ReversalAllowed : " + canReversal.ToString() + "\r\n\r\n" +
            "TransactionReffId : " + TransactionReffId.ToString() + "\r\n\r\n";
            //Console.WriteLine.StackTrace);

            //if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);
            string newPath = logPath;

            if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);

			//string path = newPath + "\\" + transactionFile + distributor_phone + skrg.ToString("yyMMddHHmmssfff") + ".txt";
			string path = Path.Combine(newPath, transactionFile + distributor_phone + skrg.ToString("yyMMddHHmmssfff") + ".txt");

            while (File.Exists(path))
            {
                System.Threading.Thread.Sleep(1);
				//path = newPath + "\\" + transactionFile + distributor_phone + DateTime.Now.ToString("yyMMddHHmmssfff") + ".txt";
				path = Path.Combine (newPath, transactionFile + distributor_phone + DateTime.Now.ToString ("yyMMddHHmmssfff") + ".txt");
            }

            try
            {
                if (!File.Exists(path))
                {
                    using (StreamWriter sw = File.CreateText(path))
                    {
                        sw.WriteLine(message);
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(path))
                    {
                        sw.WriteLine(message);
                    }
                }
            }
            catch { }
        }

    }
}
