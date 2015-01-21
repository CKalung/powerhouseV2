using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LOG_Handler;
using System.Net;
using StaticCommonLibrary;

namespace SMSHandler
{
    public class SMSSender: IDisposable
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
        ~SMSSender()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
			localDB.Dispose();
        }

        PublicSettings.Settings commonSettings;
        
		PPOBDatabase.PPOBdbLibs localDB;
		Exception xError;

		string outboxPath = "";
		string FtpSmsUrl=""; 
		string FtpSmsUsername=""; 
		string FtpSmsPassword="";
		bool isUsingFtp = false;
		bool isUsingDB = false;


        public SMSSender(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
			localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
				commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
			outboxPath = commonSettings.getString("SmsOutBoxPath");
			FtpSmsUrl = commonSettings.getString("SmsFtpUrl");
			FtpSmsUsername = commonSettings.getString("SmsFtpUsername");
			FtpSmsPassword = commonSettings.getString("SmsFtpPassword");
			isUsingFtp = (commonSettings.getString("SmsMethod").ToLower() == "ftp");
			isUsingDB = (commonSettings.getString("SmsMethod").ToLower() == "db");
        }

		private void SendSmsFtp(string fileName,string ServerUrl, string ftpUserName, string ftpPassword)
		{
			try{
				var request = (FtpWebRequest)WebRequest.Create(new Uri(ServerUrl + fileName));

				byte[] file = System.IO.File.ReadAllBytes (fileName);

				request.Method = WebRequestMethods.Ftp.UploadFile;
				request.UsePassive = false;
				request.Credentials = new NetworkCredential(ftpUserName, ftpPassword);
				request.ContentLength = file.Length;

				var requestStream = request.GetRequestStream();
				requestStream.Write(file, 0, file.Length);
				requestStream.Close();

				var response = (FtpWebResponse)request.GetResponse();
				if (response != null)
					response.Close();
			} catch (Exception ex){
				LogWriter.write(this,LogWriter.logCodeEnum.ERROR, "Send SMS file to FTP failed : " + fileName + "\r\n" +ex.getCompleteErrMsg());
				return;
			}
		}

		private bool SendSmsDb(string phoneNumber, string message){
			return localDB.insertSmsToSend ("PowerHouse", DateTime.Now, phoneNumber, message);
		}

		public bool SendSMS(string phoneNumber, string message)
        {
            DateTime skrg = DateTime.Now;

			if(isUsingDB){
				return SendSmsDb(phoneNumber, message);
			}

            string filemessage = phoneNumber + "\r\n" +
                message + "\r\n";

            string fName = phoneNumber.Replace("+", "");

            if (!Directory.Exists(outboxPath))
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "SMS Outbox path not found.");

			string path = System.IO.Path.Combine(outboxPath,"QioskuSMS" + fName + "-" + skrg.ToString("yyMMddHH"));
            string fullPath = path + ".txt";

            try
            {
                int ctr=1;
                while (true)
                {
                    if (!File.Exists(fullPath)) break;
                    fullPath = path + ctr.ToString() + ".txt";
                    ctr++;
                }
                LogWriter.showDEBUG(this, "SMS : " + fullPath);
                using (StreamWriter sw = File.CreateText(fullPath))
                {
                    sw.Write(filemessage);
                }
                //using (StreamWriter sw = File.CreateText("C:\\SMSGate\\Services\\QioskuSMS" + fName + "-" + skrg.ToString("yyMMddHH")))
                //{
                //    sw.Write(filemessage);
                //}

				//send ke FTP
				if(isUsingFtp){
					SendSmsFtp(fullPath, FtpSmsUrl, FtpSmsUsername, FtpSmsPassword);
				}

                LogWriter.showDEBUG(this, filemessage);
                return true;
            }
            catch {
                LogWriter.showDEBUG(this, "SMS GAGAL : " + fullPath);
                return false;
            }

        }

    }
}
