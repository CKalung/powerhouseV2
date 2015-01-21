using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LOG_Handler
{
    public class VATransferSchedLog: IDisposable
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
        ~VATransferSchedLog()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
        }

        // Log dengan format:
        // SenderID : 99xxxxxx
        // ReceiverID : 99xxxxxx
        // ScheduledTime : yyyy-MM-dd HH:mm:ss
        // TransactionType : TRANSFER
        // Amount : 1

        // Saat masuk database, pake sql ieu:
        // INSERT INTO transfer_record
        // (sender_id,receiver_id,scheduled_time,transaction_type,amount,transfer_time,host)
        // VALUES
        // ('6281111111','6282111111','YYYY-MM-DD HH:mi:ss','FEE','1000000',NOW(),'127.0.0.1');


        public enum transactionTypeEnum { FEE, REVERSAL, PURCHASE, PAYMENT, TRANSFER }

        //transactionTypeEnum TrxType = transactionTypeEnum.TRANSFER;
        string logPath = "";
        string transferFile = "VaXfer";

        public void setPath(string LogPath)
        {
            logPath = LogPath;
        }

        public void write(object classSource, transactionTypeEnum TrxType, 
            string SenderID, string ReceiverID, int Amount)
        {
            DateTime skrg = DateTime.Now;
            string message =
                "SenderID : " + SenderID + "\r\n" +
                "ReceiverID : " + ReceiverID + "\r\n" +
                "ScheduledTime : " + skrg.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\r\n" +
                "TransactionType : " + TrxType.ToString() + "\r\n" +
                "Amount : " + Amount.ToString();
            //Console.WriteLine.StackTrace);

            if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);
			string newPath = Path.Combine(logPath , "ToTransfer");

            if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);

			//string path = newPath + "\\" + transferFile + SenderID +ReceiverID+ skrg.ToString("yyMMddHHmmssfff") + ".txt";
			string path = Path.Combine (newPath, transferFile + SenderID + ReceiverID + skrg.ToString ("yyMMddHHmmssfff") + ".txt");

            while (File.Exists(path))
            {
                System.Threading.Thread.Sleep(1);
				//path = newPath + "\\" + transferFile + SenderID + ReceiverID + DateTime.Now.ToString("yyMMddHHmmssfff") + ".txt";
				path = Path.Combine (newPath, transferFile + SenderID + ReceiverID + DateTime.Now.ToString ("yyMMddHHmmssfff") + ".txt");
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
