using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading;
using LOG_Handler;
using StaticCommonLibrary;

namespace LogToDBService
{
    class ProcLogFile: IDisposable
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
        ~ProcLogFile()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            ToProcessList.Clear();
            localDB.Dispose();
        }

        object ProcessListSync = new object();
        object isDoneFlag = new object();
        private List<string> ToProcessList;
        private bool thProcExit = true;
        private Thread thrProcess;
        public bool isDone = true;
        PPOBDatabase.PPOBdbLibs localDB;
        PublicSettings.Settings commonSettings;

        string BadFilePath = "";

        public ProcLogFile(string badFilePath, PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
                commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
            BadFilePath = badFilePath;
            if (!BadFilePath.EndsWith("\\")) BadFilePath += "\\";
            ToProcessList = new List<string>();
            
            //string _pathSetting = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\setting.ini";
            //using (CrossIniFile.INIFile a = new CrossIniFile.INIFile(_pathSetting))
            //{
            //    _outbox = a.GetValue("SMSGatePath", "outbox", @"C:\SMSGate\Services\outbox");
            //}


        }

        public void addFile(string pfile)
        {
            lock (ProcessListSync)
            {
                isDone = false;
                ToProcessList.Add(pfile);
            }
        }

        private bool MoveFile(string sourceFile, string targetFile)
        {
            if (System.IO.File.Exists(targetFile)) KillFile(targetFile);
            try
            {
                System.IO.File.SetAttributes(sourceFile, System.IO.FileAttributes.Archive);
                System.IO.File.Move(sourceFile, targetFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool KillFile(string aFile)
        {
            try
            {
                System.IO.File.SetAttributes(aFile, System.IO.FileAttributes.Archive);
                System.IO.File.Delete(aFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        struct TransLogStruct
        {
            public string DistributorPhone;
            public string ProductCode;
            public string ProviderProductCode;
            public string ProviderCode;
            public string CustomerProductNumber;
            public string CogsPriceId;
            public string Amount;
            public string BaseAmount;
            public string CustomerFee;
            public string TraceNumber;
            public string TransactionTime;
            public string BeginBalance;
            public string EndBalance;
            public string JsonIsoInquiry;
            public string JsonIsoInquiryTime;
            public string JsonIsoInquiryReceived;
            public string JsonIsoTrxInquiryTime;
            public string JsonIsoTrx;
            public string JsonIsoTrxSendTime;
            public string JsonIsoTrxReceived;
            public string JsonIsoTrxReceivedTime;
            public bool IsSuccess;
            public string FailedReason;
            public string TrxNumber;
            public bool ReversalAllowed;
            public string TransactionReffId;
            public bool IsIncludeFee;
        }

        TransLogStruct getStruct(string data)
        {
            // Isi file sbb:
            //"DistributorPhone : " + distributor_phone + "\r\n" +
            //"ProductCode : " + product_code + "\r\n" +
            //"ProviderProductCode : " + provider_product_code + "\r\n" +
            //"ProviderCode : " + provider_code + "\r\n" +
            //"CogsPriceId : " + cogs_price_id + "\r\n" +
            //"CustomerProductNumber : " + customerProductNumber + "\r\n" +
            //"Amount : " + amount + "\r\n" +
            //"BaseAmount : " + baseAmount.ToString() + "\r\n" +
            //"CustomerFee : " + customerFee.ToString() + "\r\n" +
            //"TraceNumber : " + trace_number + "\r\n" +
            //"TransactionTime : " + trx_time + "\r\n" +
            //"BeginBalance : " + beginBalance + "\r\n" +
            //"EndBalance : " + endBalance + "\r\n" +
            //"JsonIsoInquiry : " + json_iso_inquiry + "\r\n" +
            //"JsonIsoInquiryTime : " + json_iso_inquiry_send_time + "\r\n" +
            //"JsonIsoInquiryReceived : " + json_iso_inquiry_received + "\r\n" +
            //"JsonIsoTrxInquiryTime : " + json_iso_trx_inquiry_time + "\r\n" +
            //"JsonIsoTrx : " + json_iso_trx + "\r\n" +
            //"JsonIsoTrxSendTime : " + json_iso_trx_send_time + "\r\n" +
            //"JsonIsoTrxReceived : " + json_iso_trx_received + "\r\n" +
            //"JsonIsoTrxReceivedTime : " + json_iso_trx_received_time + "\r\n" +
            //"IsSuccess : " + is_success.ToString() + "\r\n";

            TransLogStruct transLog = new TransLogStruct();
            string[] splitter = { "\r\n\r\n" };
            string[] lines = data.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            string aLine = "";
            foreach (string line in lines)
            {
                aLine = line.Trim();
                //                              1         2         3
                //                    0123456789012345678901234567890
                if (aLine.StartsWith("DistributorPhone"))
                {
                    transLog.DistributorPhone = aLine.Substring(19).Trim();
                }
                else if (aLine.StartsWith("ProductCode"))
                {
                    transLog.ProductCode = aLine.Substring(14).Trim();
                }
                else if (aLine.StartsWith("ProviderProductCode"))
                {
                    transLog.ProviderProductCode = aLine.Substring(22).Trim();
                }
                else if (aLine.StartsWith("ProviderCode"))
                {
                    transLog.ProviderCode = aLine.Substring(15).Trim();
                }
                else if (aLine.StartsWith("CogsPriceId"))
                {
                    transLog.CogsPriceId = aLine.Substring(14).Trim();
                }
                else if (aLine.StartsWith("CustomerProductNumber"))
                {
                    transLog.CustomerProductNumber = aLine.Substring(24).Trim();
                }
                else if (aLine.StartsWith("Amount"))
                {
                    transLog.Amount = aLine.Substring(9).Trim();
                }
                else if (aLine.StartsWith("BaseAmount"))
                {
                    transLog.BaseAmount = aLine.Substring(13).Trim();
                }
                else if (aLine.StartsWith("CustomerFee"))
                {
                    transLog.CustomerFee = aLine.Substring(14).Trim();
                }
                else if (aLine.StartsWith("TraceNumber"))
                {
                    transLog.TraceNumber = aLine.Substring(14).Trim();
                }
                else if (aLine.StartsWith("TransactionTime"))
                {
                    transLog.TransactionTime = aLine.Substring(18).Trim();
                }
                else if (aLine.StartsWith("BeginBalance"))
                {
                    transLog.BeginBalance = aLine.Substring(15).Trim();
                }
                else if (aLine.StartsWith("EndBalance"))
                {
                    transLog.EndBalance = aLine.Substring(13).Trim();
                }
                else if (aLine.StartsWith("JsonIsoInquiry"))
                {
                    transLog.JsonIsoInquiry = aLine.Substring(17).Trim();
                }
                else if (aLine.StartsWith("JsonIsoInquiryTime"))
                {
                    transLog.JsonIsoInquiryTime = aLine.Substring(21).Trim();
                }
                else if (aLine.StartsWith("JsonIsoInquiryReceived"))
                {
                    transLog.JsonIsoInquiryReceived = aLine.Substring(25).Trim();
                }
                else if (aLine.StartsWith("JsonIsoTrxInquiryTime"))
                {
                    transLog.JsonIsoTrxInquiryTime = aLine.Substring(24).Trim();
                }
                else if (aLine.StartsWith("JsonIsoTrx"))
                {
                    transLog.JsonIsoTrx = aLine.Substring(13).Trim();
                }
                else if (aLine.StartsWith("JsonIsoTrxSendTime"))
                {
                    transLog.JsonIsoTrxSendTime = aLine.Substring(21).Trim();
                }
                else if (aLine.StartsWith("JsonIsoTrxReceived"))
                {
                    transLog.JsonIsoTrxReceived = aLine.Substring(21).Trim();
                }
                else if (aLine.StartsWith("JsonIsoTrxReceivedTime"))
                {
                    transLog.JsonIsoTrxReceivedTime = aLine.Substring(25).Trim();
                }
                else if (aLine.StartsWith("IsSuccess"))
                {
                    transLog.IsSuccess = (aLine.Substring(12).Trim() == "TRUE");
                }
                else if (aLine.StartsWith("FailedReason"))
                {
                    transLog.FailedReason = aLine.Substring(15).Trim();
                }
                else if (aLine.StartsWith("TrxNumber"))
                {
                    transLog.TrxNumber = aLine.Substring(12).Trim();
                }
                else if (aLine.StartsWith("ReversalAllowed"))
                {
                    transLog.ReversalAllowed = (aLine.Substring(18).Trim() == "TRUE");
                }
                else if (aLine.StartsWith("TransactionReffId"))
                {
                    transLog.TransactionReffId = aLine.Substring(20).Trim();
                }
            }
            return transLog;
        }

        private bool processData(string data, ref bool fDbError)
        {
            try
            {
                Exception xError= null;
                TransLogStruct structData = getStruct(data);
                // insert ke database
                //if (!InsertCompleteTransactionLog(productCode, userId.Substring(2), customerProductNumber,
                //    productAmount, traceNumb.ToString(), trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
                //    0, 0, "", null, "", null, strJson, trxTime.ToString("yyyy-MM-dd HH:mm:ss"),
                //    strRecJson, trxRecTime.ToString("yyyy-MM-dd HH:mm:ss"), fTrx))
                //{
                //    // TODO : disini harus dijadwalkan masuk ke log
                //    //return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Error server database", "");
                //}
                fDbError = false;
                if (!localDB.insertCompleteTransactionLog(long.Parse( structData.TransactionReffId), 
                        structData.ProductCode,
                        structData.ProviderProductCode,
                        structData.DistributorPhone, structData.CustomerProductNumber,
                        structData.Amount, structData.TraceNumber, structData.TransactionTime,
                        structData.CustomerFee,structData.ProviderCode,int.Parse(structData.CogsPriceId),
                        int.Parse(structData.BeginBalance), int.Parse(structData.EndBalance),
                        structData.JsonIsoInquiry,
                        structData.JsonIsoInquiryTime,
                        structData.JsonIsoInquiryReceived,
                        structData.JsonIsoTrxInquiryTime,
                        structData.JsonIsoTrx,
                        structData.JsonIsoTrxSendTime,
                        structData.JsonIsoTrxReceived,
                        structData.JsonIsoTrxReceivedTime,
                        structData.IsSuccess, structData.FailedReason, structData.TrxNumber,
                        structData.ReversalAllowed,structData.IsIncludeFee,
                        out xError))
                {
                    if (xError != null) fDbError = true;
                    return false;
                }
                if (xError != null) fDbError = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Start()
        {
            isDone = false;
            thProcExit = false;
            thrProcess = new System.Threading.Thread(thProcessQue);
            thrProcess.IsBackground = true;
            thrProcess.Start();
        }

        public void Stop()
        {
            thProcExit = true;
            while (!isDone) Thread.Sleep(100);
        }

        private void thProcessQue()
        {
            string data = "";
            string aFile = "";
            string filename = "";
            bool facc = false;
            bool fDbError = false;
            while (!thProcExit)
            {
                data = "";
                if (ToProcessList.Count > 0)
                {
                    try
                    {
                        lock (ProcessListSync) aFile = ToProcessList[0];
                        if (!System.IO.File.Exists(aFile))
                        {
                            lock (ProcessListSync) ToProcessList.RemoveAt(0);
                            continue;
                            //Console.WriteLine("dihapus dilist " + aFile);
                        }
                        else
                        {
                            data = System.IO.File.ReadAllText(aFile).Trim();
                            if (processData(data, ref fDbError))
                            {
                                lock (ProcessListSync) ToProcessList.RemoveAt(0);
                                facc = true;

                                //Console.WriteLine(data);
                                KillFile(aFile);
                                //Console.WriteLine("dihapus file " + aFile);
                            }
                            else
                            {
                                if (fDbError)
                                {
                                    facc = false;
                                    System.Threading.Thread.Sleep(500);
                                    continue;
                                }
                                // pindahkan ke log yang gagal proses
                                filename = System.IO.Path.GetFileName(aFile);
                                MoveFile(aFile,BadFilePath + aFile);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWriter.showDEBUG(this, ex.getCompleteErrMsg());
                        facc = false;
                    }
                    //if (System.IO.File.Exists(aFile)) 
                    //{
                    //    //KillFile(aFile);
                    //    filename = System.IO.Path.GetFileName(aFile);
                    //    MoveFile(aFile, BadFilePath + aFile);
                    //}
                }
                else
                {
                    lock (ProcessListSync) isDone = true;
                }
                if (facc)
                    System.Threading.Thread.Sleep(20);
                else
                    System.Threading.Thread.Sleep(100);
            }
            isDone = true;
        }

    }
}
