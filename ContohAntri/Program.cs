using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using System.Threading;
using System.Runtime.CompilerServices;

namespace ContohAntri
{
    public class koneksiFinnet
    {
        public int kirim(int data)
        {
            return (data + 10);
        }
    }

    public static class KelasAntri
    {
        static koneksiFinnet cuma1Koneksi = new koneksiFinnet();

        static int datatabrak = 0;

         [MethodImpl(MethodImplOptions.Synchronized)]
        public static int AmbilReturn1(int data)
        {
            datatabrak = data;
            lock (_lockObject)
            {
                Thread.Sleep(600);       // ngarah nu manggil tabrakan sengaja
            }
            return cuma1Koneksi.kirim(datatabrak);
        }

        private static object _lockObject = new object();  
        public static int AmbilReturn2(int data) {  
            lock (_lockObject)
            {
                Thread.Sleep(600);       // ngarah nu manggil tabrakan sengaja
                return cuma1Koneksi.kirim(data);
            }  
        } 

    }


    class Program
    {
        static bool fMethodLock = true;
        static bool fexit = false;
        static Hashtable ASyncList = new Hashtable();
        static Hashtable SyncList = Hashtable.Synchronized(ASyncList);

		static string dataStr ="LAYANAN:001001|NO TLP:0021008201593|NAMA: ADITYA RIYADI SOEROSO MSEE|LEMBAR TAG:1|BL/TH:DES14|ADMIN:RP. 3.000|RP TAG:RP. 503.058|TOTAL BAYAR:RP. 506.058";

		static private int ambilTotalBayar(string msg){
			int amount;
			string MSG = msg.ToUpper ();
			int indx = MSG.IndexOf ("TOTAL");
			if (indx < 0)
				return 0;
			indx = MSG.IndexOf ("BAYAR",indx);
			if (indx < 0)
				return 0;
			MSG = MSG.Substring (indx);
			indx = MSG.IndexOf ("RP");	// cari RP setelah TOTAL BAYAR
			if (indx < 0)
				return 0;
			indx += 3;	// dari MSG
			MSG = MSG.Substring (indx);
			// cari char berikutnya sampe ujungnya bukan angka atau spasi
			string val = "";

			Console.WriteLine ("Masuk");

			foreach(char ch in MSG){
				if (char.IsDigit (ch))
					val += ch;
				else if (ch == '.')
					continue;
				else if (!char.IsWhiteSpace (ch))
					break;
			}

			int.TryParse (val,out indx);
			if (indx < 0)
				return 0;
			return indx;
		}

		static void Main(string[] args)
		{
			// Tes fungsi 
			Console.WriteLine (dataStr);
			Console.WriteLine (ambilTotalBayar (dataStr));
		}

		static void MainEncrypt(string[] args)
		{
			byte[] key = { 
				0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 
				0x76, 0x65, 0x64, 0x65, 0x76, 0x33, 0x15, 0xc2,
				0x61, 0x6e, 0x64, 0x65, 0x49, 0x76, 0x76, 0x33
			};
			byte[] iv = { 
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
			};
			AESCryptography aes = new AESCryptography ();
			string encr = aes.EncryptClearText("Kick off a new thread",key,iv);
			Console.WriteLine ("Encrypted = "+encr);
			string decr = aes.DecryptClearText(encr,key,iv);
			Console.WriteLine ("Decrypted = "+decr);

		}

		static void Main2(string[] args)
        {
            Console.WriteLine(DateTime.Now.ToString("yyMMddHHmmssfff"));
            Thread t1 = new Thread(multithread1);          // Kick off a new thread
            t1.Start();                               // running WriteY()
            Thread t2 = new Thread(multithread2);          // Kick off a new thread
            t2.Start();                               // running WriteY()
            Thread t3 = new Thread(multithread3);          // Kick off a new thread
            t3.Start();                               // running WriteY()

            // Simultaneously, do something on the main thread.
            Console.Write("Enter untuk exit");
            Console.ReadLine();
            fexit = true;
            Console.Write("Exiting");
        }
        static void multithread1()
        {
            int data;
            int hasil;
            Random rnd = new Random();
            rnd.Next();

            while (!fexit)
            {
                data = rnd.Next(0, 100);
                if (fMethodLock)
                {
                    hasil = KelasAntri.AmbilReturn1(data);
                }
                else
                {
                    hasil = KelasAntri.AmbilReturn2(data);
                }
                Console.Write("Kirim1 " + data.ToString() + ", return " + hasil.ToString());
                if ((data + 10) == hasil) Console.WriteLine("  hasil = SINCHRONIZED");
                else Console.WriteLine("  hasil = BENTROK");
                Thread.Sleep(100);
            }
        }
        static void multithread2()
        {
            int data;
            int hasil;
            Random rnd = new Random();
            rnd.Next();

            while (!fexit)
            {
                data = rnd.Next(200, 300);
                if (fMethodLock)
                {
                    hasil = KelasAntri.AmbilReturn1(data);
                }
                else
                {
                    hasil = KelasAntri.AmbilReturn2(data);
                }
                Console.Write("Kirim2 " + data.ToString() + ", return " + hasil.ToString());
                if ((data + 10) == hasil) Console.WriteLine("  hasil = SINCHRONIZED");
                else Console.WriteLine("  hasil = BENTROK");
                Thread.Sleep(150);
            }
        }
        static void multithread3()
        {
            int data;
            int hasil;
            Random rnd = new Random();

            while (!fexit)
            {
                data = rnd.Next(400, 500);
                if (fMethodLock)
                {
                    hasil = KelasAntri.AmbilReturn1(data);
                }
                else
                {
                    hasil = KelasAntri.AmbilReturn2(data);
                }
                Console.Write("Kirim3 " + data.ToString() + ", return " + hasil.ToString());
                if ((data + 10) == hasil) Console.WriteLine("  hasil = SINCHRONIZED");
                else Console.WriteLine("  hasil = BENTROK");
                Thread.Sleep(130);
            }
        }
    }
}
