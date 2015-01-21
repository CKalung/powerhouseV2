using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Collections;
using System.Net;
using System.Net.Sockets;

namespace Client
{
    class TCPClient
    {
        private NetworkStream stm = null;
        private TcpClient tcpclnt = new TcpClient();

        public TCPClient()
        {             
        }

        public bool CheckConn(string hostIP, int port)
        {
            Disconnect();
            return Connect(hostIP, port);
        }

        private bool Connect(string hostIPFinnet, int port)
        {
            tcpclnt = new TcpClient();
            //Console.Clear();
            //Console.WriteLine("Connecting ...");
            int timeout = 3;
            while(true)
            {
                tcpclnt.Connect(hostIPFinnet, port);
                //tcpclnt.Connect("123.231.225.20", 7777);
                //tcpclnt.Connect("192.168.1.70", 7777);            
                //Console.WriteLine("Connected");
                if (tcpclnt.Connected)
                {
                    return true;
                }

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

        public byte[] Read(int timeout)
        {            
            int ctr = 5;
//            int timeout = 10;
            byte[] brecv = null;
            while (true)
            {
                System.Threading.Thread.Sleep(100);
                //Console.WriteLine("Awal Looping.");
                ctr--;
                if (ctr > 0)
                    continue;

                ctr = 20;

                // read response dari finnet                        
                int tLen = 0;
                byte[] bb = new byte[4096];
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                short msgLen = 0;
                while (stm.DataAvailable)
                {
                    int count = stm.Read(bb, 0, 4096);
                    tLen += count;
                    if (count == 0)
                    {
                        break;
                    }

                    if (tLen >= 4)
                    {
                        if (msgLen == 0)
                        {
                            string panjang = System.Text.ASCIIEncoding.ASCII.GetString(bb, 0, 4);
                            msgLen = Int16.Parse(panjang);
                            Console.WriteLine("Data Len = " + msgLen.ToString());
                        }
                    }
                    sb.Append(System.Text.Encoding.UTF8.GetString(bb, 0, count));
                }

                if ((short)(tLen - 4) == msgLen)
                {
                    //Console.WriteLine("Terima Ti Finnet = " + sb);
                    string res = sb.ToString().Substring(4, msgLen);
                    brecv = System.Text.ASCIIEncoding.ASCII.GetBytes(res);                    
                    break;
                }

                timeout--;
                if (timeout == 0)
                {
                    //Console.WriteLine("Beak timeout na.");
                    return null;
                    //break;
                }
                //Console.WriteLine("Sisa Timeout" + timeout.ToString());
            }            
            return brecv;
        }        
    }
}
