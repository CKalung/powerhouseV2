using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Collections;
using System.Net;
using System.Net.Sockets;

namespace IconoxOnlineHandler
{
	public class IconoxTcpClient: IDisposable
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
		~IconoxTcpClient()
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

		// Kirim
		//			Protocol TLV
		//			Tag: "@@TopUp"
		//			Length: 3 character
		//			Value: Data json
		// Terima
		//			Protocol TLV
		//			Tag: "##ICONOX"
		//			Length: 3 character
		//			Value: Data json
		string TAGSend = "@@ICONOXREQUEST";
		string TAGReceive = "##ICONOX";
		private NetworkStream stm = null;
		private TcpClient tcpclnt = new TcpClient();
		private byte[] buffer = new byte[4096];
		string iconoxQueueHost = "";
		int iconoxQueuePort = 0;

		public IconoxTcpClient(string IconoxQueueHost, int IconoxQueuePort)
		{
			iconoxQueueHost = IconoxQueueHost;
			iconoxQueuePort = IconoxQueuePort;
		}

		public bool CheckConn()
		{
			Disconnect();
			return Connect();
		}

		public bool Connect()
		{
			tcpclnt = new TcpClient();
			//Console.Clear();
			//Console.WriteLine("Connecting ...");
			int timeout = 3;
			LOG_Handler.LogWriter.showDEBUG(this, "Iconox Server > " + iconoxQueueHost+ ":" + iconoxQueuePort.ToString());
			while(true)
			{
				try{
					tcpclnt.Connect(iconoxQueueHost, iconoxQueuePort);
					//tcpclnt.Connect("123.231.225.20", 7816);
					//tcpclnt.Connect("192.168.1.70", 7816);            
					//Console.WriteLine("Connected");
					if (tcpclnt.Connected)
					{
						return true;
					}
				}catch{}
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


		public bool SendPlusProtocol(string sData)
		{
			string slen = sData.Length.ToString ().PadLeft (3, '0');
			try{
				sData=TAGSend + slen + sData;
				//byte[] ba = System.Text.Encoding.UTF7.GetBytes (sData);
				//byte[] ba = System.Text.ASCIIEncoding.ASCII.GetBytes (sData);
				byte[] ba = System.Text.Encoding.GetEncoding(1252).GetBytes (sData);
				stm = tcpclnt.GetStream();
				stm.Write(ba, 0, ba.Length);
				stm.Flush();
				return true;
			}catch{
				return false;
			}
		}

		private bool isMessageComplete(string msg, ref string strRec){
			// Kirim
//			Protocol TLV
//			Tag: "@@TopUp"
//			Length: 3 character
//			Value: Data json
			// Terima
//			Protocol TLV
//			Tag: "##ICONOX"
//			Length: 3 character
//			Value: Data json
			strRec = "";

			//Console.WriteLine ("DEBUG: Data iconox terima: " + msg);

			if (msg.Length <= 12) 
				return false;
			if(!msg.StartsWith(TAGReceive))
				return false;
			string slen = msg.Substring (8, 3);
			int iLen = 0;
			try{
				iLen = int.Parse(slen);
			}
			catch{
				return false;
			}
			if ((iLen + 11) <= msg.Length) {
				strRec = msg.Substring(11, iLen);
				return true;
			}
			else
				return false;
		}

		public string Read(int timeout)
		{
			bool fTO = true;
			//int ctr = 5;
			//            int timeout = 10;
			//string srecv = null;
			//string res="";
			int tLen = 0;
			byte[] bb = new byte[4096];
			StringBuilder sb = new StringBuilder();
			//short msgLen = 0;
			int count = 0;
			//string panjang = "";
			string strRec = "";

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
					return "";
				}

				while (stm.DataAvailable)
				{
					count = stm.Read(bb, 0, 4096);
					tLen += count;
					if (count == 0)
					{
						break;
					}

					//sb.Append(System.Text.Encoding.UTF8.GetString(bb, 0, count));
					sb.Append(System.Text.Encoding.GetEncoding(1252).GetString(bb, 0, count));
				}

				// cek protocol
				if(isMessageComplete(sb.ToString(), ref strRec)){
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
			return strRec;
		}        
	}
}
