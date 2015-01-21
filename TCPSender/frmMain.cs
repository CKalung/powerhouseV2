using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;

namespace TCPSender
{
    public partial class frmMain : Form
    {
        //private TcpClient client;
        private Socket client;

        public frmMain()
        {
            InitializeComponent();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {

        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            TCPCreateConnection();
        }

        private bool GetIP(string IP, ref IPAddress ipAddr)
        {
            try
            {
                ipAddr = IPAddress.Parse(IP);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool connect(string hostOrIpAddress, int port)
        {
            if (client != null) return false;

            //this.fManualDisconnect = false;
            IPAddress iAddr = null;
            try
            {
                if (!this.GetIP(hostOrIpAddress, ref iAddr))
                {
                    //iAddr = Dns.Resolve(hostOrIpAddress).AddressList[0];
                    iAddr = Dns.GetHostEntry(hostOrIpAddress).AddressList[0];
                }
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(iAddr, port);
                if (client != null) return true;
                else return false;
            }
            catch
            {
                if ((client != null) && client.Connected)
                {
                    try
                    {
                        client.Shutdown(SocketShutdown.Both);
                        client.Disconnect(false);
                    }
                    catch { }
                }
                try
                {
                    client.Close();
                }
                catch { }
                client = null;
                return false;
            }
        }

        private bool TCPCreateConnection()
        {
            try
            {
                //client = new System.Net.Sockets.TcpClient(txtHost.Text, int.Parse(txtPort.Text));
                return connect(txtHost.Text, int.Parse(txtPort.Text));
            }
            catch //(Exception ex)
            { return false; }
        }

        public struct httpResponse
        {
            public int httpCode;
            public string httpMessage;
            public string httpDate;
            public string httpContentType;
            public string httpBody;
            public string serverResponseCode;
            public string serverResponseMessage;
        }

        httpResponse httpResp;
        private bool isHttpMessageCompleted(string msg)
        {
            //Console.WriteLine("=========");
            //Console.WriteLine(msg);
            //Console.WriteLine("=========");

            // cari "Content-Length: "
            string[] lines = msg.Split('\n');
            string tStr = "";
            int bodylen = 0;
            bool fSearchBody = false;
            int startBody = 0;
            bool fSudahContinue = false;

            httpResp.httpCode = 0;
            httpResp.httpMessage = "";
            httpResp.httpContentType = "";
            httpResp.httpDate = "";
            httpResp.httpBody = "";
            httpResp.serverResponseCode = "";
            httpResp.serverResponseMessage = "";


            try
            {
                foreach (string line in lines)
                {
                    if (line.StartsWith("Content-Length: "))
                    {
                        // ambil length content
                        tStr = line.Substring(16, line.Length - 17);
                        bodylen = int.Parse(tStr);
                        fSearchBody = true;
                    }
                    else if (line.StartsWith("HTTP/"))
                    {
                        string[] httpStat = line.Split(' ');
                        httpResp.httpCode = int.Parse(httpStat[1]);
                        httpResp.httpMessage = line.Substring(httpStat[0].Length + httpStat[1].Length + 2).TrimEnd();
                    }
                    else if (line.StartsWith("Content-Type:"))
                    {
                        httpResp.httpContentType = line.Substring(14).TrimEnd();
                    }
                    else if (line.StartsWith("Date:"))
                    {
                        httpResp.httpDate = line.Substring(6).TrimEnd();
                    }
                    else if (line.StartsWith("responseCode:"))
                    {
                        httpResp.serverResponseCode = line.Substring(14).TrimEnd();
                    }
                    else if (line.StartsWith("responseMessage:"))
                    {
                        httpResp.serverResponseMessage = line.Substring(17).TrimEnd();
                    }
                    if (line == "\r")   // header beres
                    {
                        if (httpResp.httpCode == 100)
                        {
                            fSudahContinue = true;
                            continue;
                        }
                        if (bodylen == 0) return true;
                        break;
                    }
                }
                if (fSearchBody)
                {
                    if (bodylen == 0) return true;
                    for (int i = 0; i < msg.Length; i++)
                    {
                        if ((msg[i] == '\n') && ((msg[i + 1] == '\r') || (msg[i + 1] == '\n')))
                        {
                            if (fSudahContinue)
                            {
                                fSudahContinue = false;
                                //i += 10;
                                continue;
                            }
                            // maka i+2 = start body
                            startBody = i + 3;
                            httpResp.httpBody = msg.Substring(startBody);
                            if (msg.Length >= (startBody + bodylen)) return true;   // paket lengkap
                            else return false;
                        }
                    }
                    return false;
                }
                else if ((httpResp.httpCode != 200) && (httpResp.httpCode != 100))
                {
                    return true;
                }
                else return false;
            }
            catch
            {
                return false;
            }
        }

        private string sendByTCP()
        {

            string responseData = "";
            Byte[] bytes = new byte[2048];
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            bool fTO = true;
            int count = 0;
            string sTmp="";

            // Get the data to be sent as a byte array.
            Byte[] data = System.Text.Encoding.UTF8.GetBytes(txtSend.Text);
            //NetworkStream stream = null;

            try
            {
                // Send the.StackTrace to the connected TcpServer.
                //stream = client.GetStream();
                //client.Connect(txtHost.Text, int.Parse(txtPort.Text));

                client.SendTimeout = 60;
                client.Send(data);
                //stream.Write(data, 0, data.Length);
                //stream.Flush();

                while (true)
                {
                    // Receive the TcpServer.response.
                    fTO = true;
                    //for (int i = 0; i < 50; i++)
                    for (int i = 0; i < 300; i++)
                    {
                        if(client.Available>0)
                        //if (stream.DataAvailable)
                        {
                            fTO = false;
                            break;
                        }
                        System.Threading.Thread.Sleep(100);
                    }
                    if (fTO)
                    {
                        // timeout
                        txtReceived.Text += "Kena timeout\r\n";
                        client.Close();
                        client = null;
                        return "";
                    }

                    //while (stream.DataAvailable)
                    while (client.Available > 0)
                    {
                        Array.Resize(ref bytes, client.Available);
                        count = client.Receive(bytes);
                        //int count = stream.Read(bytes, 0, 2048);
                        //int count = sslStream.Read(bytes, 0, 1024);
                        if (count == 0) break;
                        sb.Append(System.Text.Encoding.UTF8.GetString(bytes, 0, count));
                    }
                    // jika received "Continue", perintahkan continue ke while
                    sTmp = sb.ToString();
                    if (sTmp.Equals("HTTP/1.1 100 Continue\r\n\r\n"))
                    {
                        Console.WriteLine("**** dapet continue doang *****");
                        continue;
                    }

                    if (sb.Length > 0)
                    {
                        txtReceived.Text += sTmp;
                        if (isHttpMessageCompleted(sTmp))
                        {
                            break;
                        }
                        // didieu teu kudu pake timeout mun data teu lengkap bae karena udah pake fTO diluhur
                        // kuduna didieu di cek kelengkapan data, supaya proses cepat, teu nunggu deui mun data geus lengkap
                        break;
                    }
                    else break;
                }
            }
            catch
            {
                txtReceived.Text += "error";
            }

            responseData = sb.ToString();
            //Console.WriteLine(responseData);
            // Close everything.
            try
            {
                if (client != null) client.Close();
            }
            catch { }
            client = null;
            return responseData;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (TCPCreateConnection())
                txtReceived.Text += sendByTCP();
            else
                txtReceived.Text += "Gagal open socket!\r\n";
        }
    }
}
