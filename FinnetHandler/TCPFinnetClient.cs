using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Collections;
using System.Net;
using System.Net.Sockets;

namespace FinnetHandler
{
    class TCPFinnetClient: IDisposable
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
        ~TCPFinnetClient()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            try{
                Disconnect();
                stm.Dispose();
            }
            catch{}
        }

        private NetworkStream stm = null;
        private TcpClient tcpclnt = new TcpClient();
        private byte[] buffer = new byte[4096];
        string finnetQueueHost = ""; 
        int finnetQueuePort=0;

        public TCPFinnetClient(string FinnetQueueHost, int FinnetQueuePort)
        {
            finnetQueueHost = FinnetQueueHost;
            finnetQueuePort = FinnetQueuePort;
        }

        public bool CheckConn()
        {
            Disconnect();
            return Connect();
        }

        private bool Connect()
        {
            tcpclnt = new TcpClient();
            //Console.Clear();
            //Console.WriteLine("Connecting ...");
            int timeout = 3;
            while(true)
            {
                tcpclnt.Connect(finnetQueueHost, finnetQueuePort);
                //tcpclnt.Connect("123.231.225.20", 7777);
                //tcpclnt.Connect("192.168.1.70", 7777);            
                //Console.WriteLine("Connected");
                if (tcpclnt.Connected)
                {
                    return true;
                }
                Thread.Sleep(100);

                timeout--;
                if (timeout == 0)
                {
                    //Console.WriteLine("Beak timeout na.");
                    return false;
                }
            }
            //return false;
        }

        public void Disconnect()
        {
            if (tcpclnt.Connected)
            {
                stm.Close();
                tcpclnt.Close();
            } 
        }

        public void Send(byte[] ba)
        {
            stm = tcpclnt.GetStream();
            stm.Write(ba, 0, ba.Length);
            stm.Flush();
        }


        public byte[] Read2(int timeout)
        {
            bool fTO = true;
            int count = 0;
            int totRec = 0;
            try{
                while (true)
                {
                    // Receive the TcpServer.response.
                    fTO = true;
                    //for (int i = 0; i < 50; i++)
                    for (int i = 0; i < 100; i++)
                    {
                        if (stm.DataAvailable)
                        {
                            fTO = false;
                            break;
                        }
                        System.Threading.Thread.Sleep(100);
                    }
                    if (fTO)
                    {
                        return null;
                    }

                    while (stm.DataAvailable)
                    {
                        count = stm.Read(buffer, totRec, 4096);
                        //int count = sslStream.Read(bytes, 0, 1024);
                        totRec += count;
                        if (count == 0)
                        {
                            break;
                        }
                        Thread.Sleep(50);   //kasih kesempatan lagi jika kemungkinan masih ada
                    }
                }
            }
            catch
            {
                return null;
            }
            //byte[] recv = new byte[totRec];
            //Array.Copy(buffer, recv, totRec);
            //return recv;
        }

        public byte[] Read(int timeout, ref string strISO)
        {
            bool fTO = true;
            //int ctr = 5;
//            int timeout = 10;
            byte[] brecv = null;
            //string res="";
            int tLen = 0;
            byte[] bb = new byte[4096];
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            short msgLen = 0;
            int count = 0;
            //string panjang = "";
            strISO = "";

            while (true)
            {
                fTO = true;
                //for (int i = 0; i < 50; i++)
                for (int i = 0; i < timeout; i++)
                {
                    if (stm.DataAvailable)
                    {
                        fTO = false;
                        break;
                    }
                    System.Threading.Thread.Sleep(100);
                }
                if (fTO)
                {
                    //Console.WriteLine("Beak timeout na.");
                    return null;
                }

                //Console.WriteLine("Awal Looping.");
                //ctr--;
                //if (ctr > 0)
                //    continue;

                //ctr = 20;

                // read response dari finnet                        
                //tLen = 0;
                //byte[] bb = new byte[4096];
                //System.Text.StringBuilder sb = new System.Text.StringBuilder();
                //msgLen = 0;
                while (stm.DataAvailable)
                {
                    count = stm.Read(bb, 0, 4096);
                    tLen += count;
                    if (count == 0)
                    {
                        break;
                    }

                    if ((msgLen == 0) && (tLen >= 4))
                    {
                        // Ambil panjang ISO
                        //panjang = System.Text.ASCIIEncoding.ASCII.GetString(bb, 0, 4);
                        msgLen = Int16.Parse(System.Text.ASCIIEncoding.ASCII.GetString(bb, 0, 4));
                        //Console.WriteLine("Data Len = " + msgLen.ToString());
                    }
                    sb.Append(System.Text.Encoding.UTF8.GetString(bb, 0, count));
                }

                // Cek jika panjang message sudah sesuai dengan panjang yang diinginkan
                if ((short)(tLen - 4) == msgLen)
                {
                    // Ambil ISO messagenya untuk returnnya
                    //Console.WriteLine("Terima Ti Finnet = " + sb);
                    strISO = sb.ToString().Substring(4, msgLen);
                    brecv = System.Text.ASCIIEncoding.ASCII.GetBytes(strISO);
                    break;
                }
                // Jika belum sesuai, tunggu paket berikutnya

                //timeout--;
                //if (timeout == 0)
                //{
                //    Console.WriteLine("Beak timeout na.");
                //    return null;
                //    //break;
                //}
                //Console.WriteLine("Sisa Timeout" + timeout.ToString());
            }            
            return brecv;
        }        
    }
}
