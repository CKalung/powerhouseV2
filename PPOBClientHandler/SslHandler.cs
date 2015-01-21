using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PPOBClientHandler
{
    public class SslStateObject
    {
        public SslStream workSslStream = null;                // Stream socket.
        public const int BufferSize = 4096;             // Size of receive buffer.
        public byte[] buffer = new byte[BufferSize];    // Receive buffer.
        //public StringBuilder sb = new StringBuilder();  // Received data string.
    }

    public class SslHandler
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
        }
        ~SslHandler()
        {
            this.Dispose(false);
        }
        #endregion


        PublicSettings.Settings CommonConfigs;
        X509Certificate serverCert;
        RemoteCertificateValidationCallback certValidationCallback;
        SecureConnectionResultsCallback connectionCallback;
        AsyncCallback onAcceptConnection;
        AsyncCallback onAuthenticateAsServer;

        bool clientCertificateRequired;
        bool checkCertifcateRevocation;
        SslProtocols sslProtocols;

        public SslHandler(PublicSettings.Settings commonConfigs)
        {
            CommonConfigs = commonConfigs;
            string certPath = System.IO.Path.Combine(
                CommonConfigs.getString("SslCertificatePath"), "serverCert.cer");
            Console.WriteLine("Loading Server Cert From: " + certPath);
            X509Certificate serverCert = X509Certificate.CreateFromCertFile(certPath);

            onAuthenticateAsServer = new AsyncCallback(OnAuthenticateAsServer);

            this.serverCert = serverCert;
            this.checkCertifcateRevocation = false;
            this.clientCertificateRequired = false;
            this.sslProtocols = SslProtocols.Default;
        }

        public void processSocket(Socket sock)
        {
            SslStream sslStream = null;

            try
            {
                bool leaveStreamOpen = false;//close the socket when done

                sslStream = new SslStream(new NetworkStream(sock), leaveStreamOpen);
                
                sslStream.BeginAuthenticateAsServer(this.serverCert,
                    this.clientCertificateRequired,
                    this.sslProtocols,
                    this.checkCertifcateRevocation,//checkCertifcateRevocation
                    this.onAuthenticateAsServer,
                    sslStream);

            }
            catch (Exception ex)
            {
                if (sslStream != null)
                {
                    sslStream.Dispose();
                    sslStream = null;
                }
                this.connectionCallback(this, new SecureConnectionResults(ex));
            }
        }

        void OnAuthenticateAsServer(IAsyncResult result)
        {
            SslStream sslStream = null;
            try
            {
                sslStream = result.AsyncState as SslStream;
                sslStream.EndAuthenticateAsServer(result);

                this.connectionCallback(this, new SecureConnectionResults(sslStream));

                SslStateObject state = new SslStateObject();
                state.workSslStream = sslStream;

                sslStream.BeginRead(state.buffer, 0, state.buffer.Length,
                        new AsyncCallback(ReadCallback), state);

            }
            catch (Exception ex)
            {
                if (sslStream != null)
                {
                    sslStream.Dispose();
                    sslStream = null;
                }

                this.connectionCallback(this, new SecureConnectionResults(ex));
            }
        }

        static void ReadCallback(IAsyncResult ar)
        {
            // Read the  message sent by the server. 
            // The end of the message is signaled using the 
            // "<EOF>" marker.
            //SslStream sslStream = (SslStream)ar.AsyncState;
            SslStateObject state = (SslStateObject)ar.AsyncState;
            SslStream sslStream = state.workSslStream;
            int byteCount = -1;
            try
            {
                Console.WriteLine("Reading data from the server.");
                byteCount = sslStream.EndRead(ar);
                // Use Decoder class to convert from bytes to encoding 1252 
                // in case a character spans two buffers.
                Decoder decoder = Encoding.GetEncoding(1252).GetDecoder();
                char[] chars = new char[decoder.GetCharCount(state.buffer, 0, byteCount)];
                decoder.GetChars(state.buffer, 0, byteCount, chars, 0);
                readData.Append(chars);
                // Check for EOF or an empty message. 
                if (readData.ToString().IndexOf("<EOF>") == -1 && byteCount != 0)
                {
                    // We are not finished reading. 
                    // Asynchronously read more message data from  the server.
                    sslStream.BeginRead(state.buffer, 0, state.buffer.Length,
                            new AsyncCallback(ReadCallback), state);
                    stream.BeginRead(buffer, 0, buffer.Length,
                        new AsyncCallback(ReadCallback),
                        stream);
                }
                else
                {
                    Console.WriteLine("Message from the server: {0}", readData.ToString());
                }
            }
            catch (Exception readException)
            {
                e = readException;
                complete = true;
                return;
            }
            complete = true;
        }

    }
}
