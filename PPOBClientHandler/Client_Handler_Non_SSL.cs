using System;
using System.Collections.Generic;
using System.Text;
//using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using PPOBHttpRestData;
using PPOBManager;
using System.Net;
using LOG_Handler;
using StaticCommonLibrary;

namespace PPOBClientHandler
{
    // State object for receiving data from remote device.
    public class StateObject
    {

        public Socket workSocket = null;                // Client socket.
        public const int BufferSize = 4096;             // Size of receive buffer.
        public byte[] buffer = new byte[BufferSize];    // Receive buffer.
        //public StringBuilder sb = new StringBuilder();  // Received data string.
    }

    public class Client_Handler : IDisposable
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
        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            HTTPRestDataConstruct.Dispose();
            PPOBProcessor.Dispose();
            disconnect();
        }
        ~Client_Handler()
        {
            this.Dispose(false);
        }
        #endregion

        bool fAvailable = true;
        Socket sc = null;
        string appPath = "";
        string logPath = "";
        DateTime connectionTime;
        int SessID;
        int bytesRead = 0;

        const int TIMEOUT_07 = 7;
        const int TIMEOUT_10 = 10;
        const int TIMEOUT_15 = 15;
        const int TIMEOUT_60 = 60;
        int ctrTO = TIMEOUT_10;		// satuan detik
        int ctrTOPackage = TIMEOUT_15;		// satuan detik
        bool fExitThread = true;

        private Thread MyTimeOut;
        private Thread MyTimeOutPackage;

        ControlCenter PPOBProcessor;

        PublicSettings.Settings CommonConfigs;
        //string dbHost = "";
        //int dbPort=0;
        //string dbUser="";
        //string dbPass="";
        //string dbName="";
        //string httpRestServicePath = "";
        //string httpRestServiceAccountPath = "";
        //string httpRestServiceProductTransactionPath = "";
        //string httpRestServiceApplicationsPath = "";
        //string sandraHost="";
        //int sandraPort=0;

        public Client_Handler()
        {
            srecBuff = "";
            dataLength = 0;
            HTTPRestDataConstruct = new HTTPRestConstructor();
            PPOBProcessor = new ControlCenter();
        }

        public bool isAvailable
        {
            get { return fAvailable; }
        }
        public int SessionID
        {
            get { return SessID; }
            set { SessID = value; }
        }
        public string ApplicationPath
        {
            get { return appPath; }
            set { appPath = value; }
        }
        public string LogPath
        {
            get { return logPath; }
            set { logPath = value; }
        }
        public DateTime ConnectionTime
        {
            get { return connectionTime; }
            set { connectionTime = value; }
        }
        public Socket Socket
        {
            get { return sc; }
        }

		public void setParams(Socket sock, PublicSettings.Settings commonConfigs)
		{
            CommonConfigs = commonConfigs;
			setSocket (sock);
		}

        public void setSocket(Socket sock)
        {
            fdisconnecting = false;
            fAvailable = false;
            sc = sock;
            srecBuff = "";
            dataLength = 0;
            try
            {
                readyReceive(sc, TIMEOUT_10);   // timeout pertama
                ctrTOPackage = TIMEOUT_15;		    // initial timeout 15 detik, untuk data pertama
                MyTimeOutPackage = new Thread(new ThreadStart(ConnTimeOutPackage));
                MyTimeOutPackage.Start();
            }
            catch (Exception e)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, e.getCompleteErrMsg());
                disconnect();
            }
        }

        bool fdisconnecting = false;
        void disconnect()
        {
            if (fdisconnecting) return;
            fdisconnecting = true;
            try
            {
                //Console.WriteLine("Disconnected by server");
                fExitThread = true;
                if ((MyTimeOut != null) && (MyTimeOut.IsAlive)) MyTimeOut.Join();
                if ((MyTimeOutPackage != null) && (MyTimeOutPackage.IsAlive)) MyTimeOutPackage.Join();
                if (sc != null) sc.Close();
            }
            catch { }
            finally
            {
                sc = null;
                bytesRead = 0;
                fAvailable = true;
            }
        }

        void disconnectByTO()
        {
            if (fdisconnecting) return;
            fdisconnecting = true;
            try
            {
                //Console.WriteLine("Disconnected by server");
                fExitThread = true;
                if (sc != null) sc.Close();
            }
            catch { }
            finally
            {
                sc = null;
                bytesRead = 0;
                fAvailable = true;
            }
        }

        // timeout untuk disconnect koneksi yg idle
        private void ConnTimeOut()
        {
            int dt = 10;
            //Console.WriteLine("Timeout = " + ctrTO);
            while (!fExitThread)
            {
                dt--;
                System.Threading.Thread.Sleep(100);	// supaya looping lebih halus, tong dibikin sleep (1000)
                if (dt <= 0)	// perdetik
                {
                    dt = 10;
                    ctrTO--;	// satuan detik
                    if (ctrTO <= 0)
                    {
                        // Timeout fired
                        if ((sc != null) || (fExitThread))
                        {
                            disconnectByTO();
                            LogWriter.show(this, "Timeout... Socket disconnected.");
                            return;
                        }
                        return;
                    }
                }
            }
        }

        // timeout untuk paket yang tidak kunjung lengkap
        private void ConnTimeOutPackage()
        {
            int dt = 10;
            //Console.WriteLine("Timeout = " + ctrTO);
            while (!fExitThread)
            {
                dt--;
                System.Threading.Thread.Sleep(100);	// supaya looping lebih halus, tong dibikin sleep (1000)
                if (dt <= 0)	// perdetik
                {
                    dt = 10;
                    ctrTOPackage--;	// satuan detik
                    if (ctrTOPackage <= 0)
                    {
                        // Timeout fired
                        if ((sc != null) || (fExitThread))
                        {
                            disconnectByTO();
                            LogWriter.show(this, "Timeout... Socket disconnected.");
                            return;
                        }
                        return;
                    }
                }
            }
        }

        private void readyReceive(Socket client, int recTO, bool retrigger = false)
        {
            if (client != null)
            {
                try
                {
                    // Create the state object.
                    StateObject state = new StateObject();
                    state.workSocket = client;

                    // Begin receiving the data from the remote device.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                    fExitThread = false;		// ditambah didieu
                    ctrTO = recTO;		    // initial timeout 15 detik, untuk data pertama
                    MyTimeOut = new Thread(new ThreadStart(ConnTimeOut));
                    MyTimeOut.Start();
                }
				catch	// (Exception e)
                {
                    //Console.WriteLine(e.StackTrace);
                    LogWriter.show(this,"Socket already disconnected.");
                    disconnect();
                }
            }
        }
        private void readyReceiveRetrigger(StateObject state, int recTO)
        {
            if (state.workSocket != null)
            {
                try
                {
                    //state.sb.Clear();

                    // Begin receiving the data from the remote device.
                    state.workSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                    ctrTO = recTO;
                }
				catch	// (Exception e)
                {
                    //Console.WriteLine(e.StackTrace);
                    LogWriter.show(this,"Socket already disconnected.");
                    disconnect();   // pastikan dan siapkan class untuk re-use
                }
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            //ctrTO = TIMEOUT_60;        // reset disconnect TIMEOUT
            ctrTO = TIMEOUT_07;        // reset disconnect TIMEOUT
            StateObject state;
            if (sc != null)
            {
                try
                {
                    // Retrieve the state object and the client socket 
                    // from the asynchronous state object.
                    state = (StateObject)ar.AsyncState;

                    // Read data from the remote device.
                    bytesRead = state.workSocket.EndReceive(ar);
                }
                catch
                {
                    LogWriter.show(this,"Disconnected");
                    sc = null;
                    return;
                }

                try
                {
                    if (bytesRead > 0)
                    {
                        // There might be more data, so store the data received so far.
                        //state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                        byte[] data2 = new byte[bytesRead];
                        Array.Copy(state.buffer, 0, data2, 0, bytesRead);
//                        Console.WriteLine(state.sb.ToString();
                        DataReceived(state.workSocket, data2);
                    }
                    else
                    {
                        // disconnected by client
                        //state.sb.Clear();
                        LogWriter.show(this,"Session ID : " + SessID + ", Disconnected by remote client");
                        disconnect();
                        return;
                    }
                    Thread.Sleep(10);
                    readyReceiveRetrigger(state, TIMEOUT_07);
                }
                catch (Exception e)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Client has disconnected "+e.getCompleteErrMsg());
                    disconnect();
                }
            }
        }

        private string getDataLeft(ref string dataPackage)
        {
            string hasil = "";
            int len = int.Parse(dataPackage.Substring(0, 2));
            hasil = dataPackage.Substring(2, len);
            dataPackage = dataPackage.Substring(len + 2);
            return hasil;
        }

        HTTPRestConstructor HTTPRestDataConstruct;
        string srecBuff = "";
        const int MAXRecBuff = 100 * 1024;  // max 100Kb
//        byte[] dataBuffer = new byte[2048];
        int dataLength = 0;
        HTTPRestConstructor.retParseCode retCode = HTTPRestConstructor.retParseCode.Uncompleted;

        private void DataReceived(Socket client, byte[] data)
        {
            srecBuff += Encoding.GetEncoding(1252).GetString(data);
            dataLength += data.Length;
			HTTPRestDataConstruct.parseClientRequest(srecBuff,
				(((IPEndPoint)client.RemoteEndPoint).Address.ToString ()), ref retCode);

            //Console.WriteLine("Return Code : " + retCode.ToString());
            //Console.WriteLine("Canonical : " + HTTPRestDataConstruct.HttpRestClientRequest.CanonicalPath);
            //Console.WriteLine("Method : " + HTTPRestDataConstruct.HttpRestClientRequest.Method);
            //Console.WriteLine("Host : " + HTTPRestDataConstruct.HttpRestClientRequest.Host);
            //Console.WriteLine("ContentType : " + HTTPRestDataConstruct.HttpRestClientRequest.ContentType);
            //Console.WriteLine("ContentLen : " + HTTPRestDataConstruct.HttpRestClientRequest.ContentLen);
            //Console.WriteLine("Date : " + HTTPRestDataConstruct.HttpRestClientRequest.Date);
            //Console.WriteLine("Body : " + HTTPRestDataConstruct.HttpRestClientRequest.Body);

            LogWriter.show(this, "==== RECEIVED :\r\n"+
                            "Return Code : " + retCode.ToString() + "\r\n" +
                            "Canonical : " + HTTPRestDataConstruct.HttpRestClientRequest.CanonicalPath + "\r\n" +
                            "Body : " + HTTPRestDataConstruct.HttpRestClientRequest.Body);

            switch (retCode)
            {
                case HTTPRestConstructor.retParseCode.Invalid:
                    srecBuff = "";
                    dataLength = 0;
                    disconnect();
                    break;
                case HTTPRestConstructor.retParseCode.Uncompleted:
                    if (srecBuff.Length > MAXRecBuff)
                    {
                        srecBuff = "";
                        dataLength = 0;
                        disconnect();
                    }
                    return;
                case HTTPRestConstructor.retParseCode.Completed:
                    fExitThread = true;
                    ctrTO = TIMEOUT_60;
                    ctrTOPackage = TIMEOUT_60;
                    ProcessDataReceived(client);
                    break;
                default:
                    srecBuff = "";
                    dataLength = 0;
                    disconnect();
                    break;
            }
            return;

            //string dataReal = Encoding.GetEncoding(1252).GetString(data);
            //Console.WriteLine(dataReal);
        }

        //void replyClient(Socket client, int httpcode, string respCode, string respmessage, string jsonBody)
        //{
        //    string resp = HTTPRestDataConstruct.constructHTTPRestResponse(405, "405", "Unexpected method", "");
        //    try
        //    {
        //        client.Send(Encoding.GetEncoding(1252).GetBytes(resp));
        //    }
        //    catch { }
        //    finally
        //    {
        //        disconnect();
        //    }
        //}
        private void ProcessDataReceived(Socket client)
        {
            //string resp = PPOBProcessor.messageProcessor(HTTPRestDataConstruct.HttpRestClientRequest, 
            //    logPath,dbHost,dbPort, dbUser,dbPass,dbName,httpRestServicePath,httpRestServiceAccountPath,
            //    httpRestServiceProductTransactionPath, httpRestServiceApplicationsPath, sandraHost,sandraPort);
            string resp = PPOBProcessor.messageProcessor(HTTPRestDataConstruct.HttpRestClientRequest,
                CommonConfigs);
            try
            {
                LogWriter.show(this, "SEND TO CLIENT: " + resp);
                if (resp.Length != 0) client.Send(Encoding.GetEncoding(1252).GetBytes(resp));
            }
            catch(Exception ex)
            {
                LogWriter.show(this, "ERROR: " + ex.getCompleteErrMsg());
            }
            disconnect();

            // reply ke Client dengan acknowledge OOKK+13
            //intSent = client.Send(Encoding.GetEncoding(1252).GetBytes("OOKK\r"));
        }

//        private void ProcessDataReceived2(Socket client)
//        {
//            // masukkan ke database saja langsung, untuk ke file dulu nanti saja versi berikutnya
//            // proses cek validasi dan ke EVA di service lain
//            // Protokol : 
//            //      PaketData + ENTER(13)
//            // PaketData : struktur LV, dimana L = 2 char untuk length dalam teks
//            //      traceNum + cardNum + mID + tID + trxTime + amount + mac + trxCode + issAcqID + 
//            //      samID + cardUID
//
//            //      public int InsertTable(int traceNum, string cardNum, string mID, string tID, 
//            //      DateTime trxTime, int amount, string mac, string trxCode, string issAcqID, 
//            //      string samID, string cardUID)
//
//            byte[] dataReal = new byte[dataLength];
//            string dataPackage = "";
//            int traceNum;
//            string cardNum;
//            string mID;
//            string tID;
//            string trxTime;
//            int amount;
//            string mac;
//            string trxCode;
//            string issAcqID;
//            string samID;
//            string cardUID;
//            int intSent;
//
//            // Parsing protokol dari dataBuffer sepanjang dataLength
//            // cari ending ENTER
//            if(dataBuffer[ dataLength -1] != 13) return;
//            if (dataLength < 50) 
//            {
//                dataLength = 0;
//                return;
//            }
//
//            Array.Copy(dataBuffer,dataReal,dataLength);
//            dataPackage = Encoding.GetEncoding(1252).GetString(dataReal);
//            //Console.WriteLine(dataReal);
//
//            Console.Write("Data received ");
//            try
//            {
//                traceNum = int.Parse(getDataLeft(ref dataPackage));
//                cardNum = getDataLeft(ref dataPackage);
//                mID = getDataLeft(ref dataPackage);
//                tID = getDataLeft(ref dataPackage);
//                trxTime = getDataLeft(ref dataPackage);   // harus dalam format "yyyy-MM-dd HH:mm:ss"
//                amount = int.Parse(getDataLeft(ref dataPackage));
//                mac = getDataLeft(ref dataPackage);
//                trxCode = getDataLeft(ref dataPackage);
//                issAcqID = getDataLeft(ref dataPackage);
//                samID = getDataLeft(ref dataPackage);
//                cardUID = getDataLeft(ref dataPackage);
//            }
//            catch
//            {
//                Console.WriteLine("has not been completed yet!");
//                return;
//            }
//            dataLength = 0;
//            Console.WriteLine("has been completed!");
//
//            // reply ke Client dengan acknowledge OOKK+13
//            intSent = client.Send(Encoding.GetEncoding(1252).GetBytes("OOKK\r"));
//        }
    }
}
