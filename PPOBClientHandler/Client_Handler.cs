using System;
using System.Collections.Generic;
using System.Text;
//using System.Threading.Tasks;
using System.Threading;
using PPOBHttpRestData;
using PPOBManager;
using LOG_Handler;
using StaticCommonLibrary;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PPOBClientHandler
{
    // State object for receiving data from remote device.
    public class SslStateObject
    {
        public SslStream workSslStream = null;                // Stream socket.
        public const int BufferSize = 16384;             // Size of receive buffer.
        public byte[] buffer = new byte[BufferSize];    // Receive buffer.
        //public StringBuilder sb = new StringBuilder();  // Received data string.
    }

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
        SslStream sl = null;
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

        #region SSL
        X509Certificate2 serverCert;
        //RemoteCertificateValidationCallback certValidationCallback;
        //SecureConnectionResultsCallback connectionCallback;
        //AsyncCallback onAcceptConnection;
        AsyncCallback onAuthenticateAsServer;

        bool clientCertificateRequired = false;
        bool checkCertifcateRevocation = false;
        SslProtocols sslProtocols = SslProtocols.Default;

        #endregion

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

            if (CommonConfigs.getString("SslEnabled").ToUpper().Trim() == "TRUE")
            {
                //System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, ce, ca, p) => true;
                //string certPath = System.IO.Path.Combine(
                //    CommonConfigs.getString("SslCertificatePath"), "serverCert.cer");
                string certPath = System.IO.Path.Combine(
                    CommonConfigs.getString("SslCertificatePath"),
                    CommonConfigs.getString("SslCertificateFilename"));
				LogWriter.show(this,"Loading Server Cert From: " + certPath);
                //X509Certificate serverCert = X509Certificate.CreateFromCertFile(certPath);
                //X509Certificate serverCert = new X509Certificate(certPath);
				X509Certificate2 serverCert = new X509Certificate2(certPath, 
					CommonConfigs.getString("SslCertificatePassword"));

				LogWriter.showDEBUG(this,"Create Async Callback SSL Server");
                onAuthenticateAsServer = new AsyncCallback(OnAuthenticateAsServer);
				LogWriter.showDEBUG(this,"Beres create Async Callback SSL Server");

                this.serverCert = serverCert;
                this.checkCertifcateRevocation = false;
                this.clientCertificateRequired = false;
                this.sslProtocols = SslProtocols.Default;
                //this.sslProtocols = SslProtocols.Tls12;

				LogWriter.showDEBUG(this,"Panggil setSoketSSL ");
                setSocketForSSL(sock);
            }
            else
            {
                setSocket(sock);
            }
        }

        public void setSocketForSSL(Socket sock)
        {
            fdisconnecting = false;
            fAvailable = false;
            sc = sock;
            srecBuff = "";
            dataLength = 0;
            try
            {
                readyAuthenticateSSL(sc, TIMEOUT_10);   // timeout pertama
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
                if (sl != null)
                {
                    sl.Dispose();
                    sl = null;
                }
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
                if (sl != null)
                {
                    sl.Dispose();
                    sl = null;
                }
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

        private void readyAuthenticateSSL(Socket client, int recTO, bool retrigger = false)
        {
            SslStream sslStream = null;
			if (client != null) {
				try {
					bool leaveStreamOpen = false;//close the socket when done

					LogWriter.show (this, "Get SSL Stream");
					sslStream = new SslStream (new NetworkStream (client), leaveStreamOpen);
					sl = sslStream;

					LogWriter.show (this, "Begin authenticate");
					sslStream.BeginAuthenticateAsServer (this.serverCert,
						this.clientCertificateRequired,
						this.sslProtocols,
						this.checkCertifcateRevocation,//checkCertifcateRevocation
						this.onAuthenticateAsServer,
						sslStream);

					fExitThread = false;		// ditambah didieu
					ctrTO = recTO;		    // initial timeout 15 detik, untuk data pertama
					MyTimeOut = new Thread (new ThreadStart (ConnTimeOut));
					MyTimeOut.Start ();

				} catch (Exception ex) {
					LogWriter.show (this, "Socket already disconnected.");
					disconnect ();
				}
			} else {
				LogWriter.show(this, "Socket already disconnected.");
				disconnect();
			}
        }

        void OnAuthenticateAsServer(IAsyncResult result)
        {
			LogWriter.show(this,"Siap end authenticate");
            SslStream sslStream = null;
            try
            {
                sslStream = result.AsyncState as SslStream;
//				LogWriter.show (this, "End kan TLS authenticate"); 
//				//Console.WriteLine("End kan authenticate");
//                sslStream.EndAuthenticateAsServer(result);
            }
            catch (Exception ex)
            {
                //Console.WriteLine("CUEKkeun Errorna " + ex.Message);
				LogWriter.show (this, "CUEKkeun Errorna " + ex.Message);
				disconnect();
            }

            try
            {
				LogWriter.show (this, "Beres authenticate TLS");
                //this.connectionCallback(this, new SecureConnectionResults(sslStream));

				// coba ieu di pindah ka dieu, ngarah teu leungit data pertama na
				LogWriter.show (this, "End kan TLS authenticate"); 
				//Console.WriteLine("End kan authenticate");
				sslStream.EndAuthenticateAsServer(result);

				SslStateObject state = new SslStateObject();
                state.workSslStream = sslStream;

                sslStream.BeginRead(state.buffer, 0, state.buffer.Length,
                        new AsyncCallback(ReadCallbackSSL), state);

            }
            catch (Exception ex)
            {
                //Console.WriteLine("Errorna " + ex.Message);
                LogWriter.show(this, "Socket already disconnected.");
                disconnect();
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
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, 
                        new AsyncCallback(ReceiveCallback), state);
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

        private void readyReceiveRetriggerSSL(SslStateObject state, int recTO)
        {
            if (state.workSslStream != null)
            {
                try
                {
                    //state.sb.Clear();

                    // Begin receiving the data from the remote device.
                    state.workSslStream.BeginRead(state.buffer, 0, state.buffer.Length,
                            new AsyncCallback(ReadCallbackSSL), state);
                    ctrTO = recTO;
                }
                catch	// (Exception e)
                {
                    //Console.WriteLine(e.StackTrace);
                    LogWriter.show(this, "Socket already disconnected.");
                    disconnect();   // pastikan dan siapkan class untuk re-use
                }
            }
        }

        void ReadCallbackSSL(IAsyncResult ar)
        {
            // Read the  message sent by the server. 
            //ctrTO = TIMEOUT_60;      // reset disconnect TIMEOUT
            ctrTO = TIMEOUT_07;        // reset disconnect TIMEOUT
            SslStateObject state = null;
            SslStream sslStream = null;
            int byteCount = -1;
            try
            {
                state = (SslStateObject)ar.AsyncState;
                sslStream = state.workSslStream;
                if (sslStream == null) return;

				LogWriter.show(this, "Reading data from the server.");
                byteCount = sslStream.EndRead(ar);
            }
            catch(Exception ex)
            {
                LogWriter.show(this, "SSL Disconnected : " + ex.Message);
                disconnect();
                return;
            }

            try
            {
                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    //state.sb.Append(Encoding.GetEncoding(1252).GetString(state.buffer, 0, bytesRead));
                    byte[] data2 = new byte[bytesRead];
                    Array.Copy(state.buffer, 0, data2, 0, bytesRead);
                    //                        Console.WriteLine(state.sb.ToString();
                    DataReceivedSSL(state.workSslStream, data2);
                }
                else
                {
                    // disconnected by client
                    //state.sb.Clear();
                    LogWriter.show(this, "Session ID : " + SessID + ", Disconnected by remote client");
                    disconnect();
                    return;
                }
                Thread.Sleep(10);
                readyReceiveRetriggerSSL(state, TIMEOUT_07);
            }
            catch (Exception e)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Client has disconnected " + e.getCompleteErrMsg());
                disconnect();
            }

        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            //ctrTO = TIMEOUT_60;      // reset disconnect TIMEOUT
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
					//sc = null;	COBA DI remark 14-11-06
					disconnect ();	// ditambahan, 14-11-06
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
                (((IPEndPoint)client.RemoteEndPoint).Address.ToString()), ref retCode);

            //Console.WriteLine("Return Code : " + retCode.ToString());
            //Console.WriteLine("Canonical : " + HTTPRestDataConstruct.HttpRestClientRequest.CanonicalPath);
            //Console.WriteLine("Method : " + HTTPRestDataConstruct.HttpRestClientRequest.Method);
            //Console.WriteLine("Host : " + HTTPRestDataConstruct.HttpRestClientRequest.Host);
            //Console.WriteLine("ContentType : " + HTTPRestDataConstruct.HttpRestClientRequest.ContentType);
            //Console.WriteLine("ContentLen : " + HTTPRestDataConstruct.HttpRestClientRequest.ContentLen);
            //Console.WriteLine("Date : " + HTTPRestDataConstruct.HttpRestClientRequest.Date);
            //Console.WriteLine("Body : " + HTTPRestDataConstruct.HttpRestClientRequest.Body);

			LogWriter.show (this, "==== RECEIVED FROM CLIENT :\r\n" +
				"Return Code : " + retCode.ToString () + "\r\n" +
				"Canonical : " + HTTPRestDataConstruct.HttpRestClientRequest.CanonicalPath + "\r\n" +
				"Body : " + HTTPRestDataConstruct.HttpRestClientRequest.Body + "\r\n" +
				"FULL : \r\n" + srecBuff);


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

        private void DataReceivedSSL(SslStream clientStream, byte[] data)
        {
            srecBuff += Encoding.GetEncoding(1252).GetString(data);
            dataLength += data.Length;

            HTTPRestDataConstruct.parseClientRequest(srecBuff,
                (((IPEndPoint)sc.RemoteEndPoint).Address.ToString()), ref retCode);

            //Console.WriteLine("Return Code : " + retCode.ToString());
            //Console.WriteLine("Canonical : " + HTTPRestDataConstruct.HttpRestClientRequest.CanonicalPath);
            //Console.WriteLine("Method : " + HTTPRestDataConstruct.HttpRestClientRequest.Method);
            //Console.WriteLine("Host : " + HTTPRestDataConstruct.HttpRestClientRequest.Host);
            //Console.WriteLine("ContentType : " + HTTPRestDataConstruct.HttpRestClientRequest.ContentType);
            //Console.WriteLine("ContentLen : " + HTTPRestDataConstruct.HttpRestClientRequest.ContentLen);
            //Console.WriteLine("Date : " + HTTPRestDataConstruct.HttpRestClientRequest.Date);
            //Console.WriteLine("Body : " + HTTPRestDataConstruct.HttpRestClientRequest.Body);

            LogWriter.show(this, "==== RECEIVED :\r\n" +
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
                    ProcessDataReceivedSSL(clientStream);
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
            catch (Exception ex)
            {
				// disini reply ke client tidak bisa diterima client
                LogWriter.show(this, "ERROR: " + ex.getCompleteErrMsg());
            }
            disconnect();

            // reply ke Client dengan acknowledge OOKK+13
            //intSent = client.Send(Encoding.GetEncoding(1252).GetBytes("OOKK\r"));
        }

        private void ProcessDataReceivedSSL(SslStream clientStream)
        {
            //string resp = PPOBProcessor.messageProcessor(HTTPRestDataConstruct.HttpRestClientRequest, 
            //    logPath,dbHost,dbPort, dbUser,dbPass,dbName,httpRestServicePath,httpRestServiceAccountPath,
            //    httpRestServiceProductTransactionPath, httpRestServiceApplicationsPath, sandraHost,sandraPort);
            string resp = PPOBProcessor.messageProcessor(HTTPRestDataConstruct.HttpRestClientRequest,
                CommonConfigs);
            try
            {
                LogWriter.show(this, "SEND TO CLIENT: " + resp);
                if (resp.Length != 0)
                    clientStream.Write(Encoding.GetEncoding(1252).GetBytes(resp));
                    //client.Send(Encoding.GetEncoding(1252).GetBytes(resp));
            }
            catch (Exception ex)
            {
                LogWriter.show(this, "ERROR: " + ex.getCompleteErrMsg());
            }
            disconnect();

            // reply ke Client dengan acknowledge OOKK+13
            //intSent = client.Send(Encoding.GetEncoding(1252).GetBytes("OOKK\r"));
        }

    }
}
