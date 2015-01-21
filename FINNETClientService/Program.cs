using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client
{
    class Program
    {
        
        static FinnetModule fm = new FinnetModule();
        static TCPClient tcp = new TCPClient();
                        

        static void TestFinnetModule()
        {
            try
            {                                                
                Console.Clear();
                string msg = "0";                
                Console.WriteLine("-----------------------------");
                Console.WriteLine("CODE         PROSES          ");
                Console.WriteLine("-----------------------------");                
                Console.WriteLine("1            Echo Test");
                Console.WriteLine("2            Sign ON");
                Console.WriteLine("3            Inquiry Saja");
                Console.WriteLine("4            Transaksi Saja");
                Console.WriteLine("5            Inquiry Langsung Transaksi");                
                Console.WriteLine("6            Reversal");
                Console.WriteLine("7            Cut Off");
                Console.WriteLine("8            Sign Off");
                Console.WriteLine("9            Exit");
                string str = "10";
                while(true)
                {
                    Console.Write("CODE : ");
                    str = Console.ReadLine();
                    break;
                }                               

                msg += str;
                int mytype = int.Parse(str);
                if(str == "9")
                {                    
                    return;
                }                

                string ncode = "";
                int isoType;

                if (mytype == 1) // Test echo
                {
                    ncode = "301";
                    isoType = 1;
                    
                    // 1. create ISO MSG
                    byte[] iso = fm.CreateMsgJSON(ncode,isoType);                              

                    // 2. Send ISO Msg
                    tcp.CheckConn("123.231.225.20", 7777);
                    tcp.Send(iso);

                    // 3. Read Balasan ISO Msg
                    byte[] ret = tcp.Read(10);

                    // 4. Check Data Signature apakah sama ?
                    if (fm.CheckDataSignature(ret,isoType))
                    { 
                        Console.WriteLine("Signature Sarua");
                    }
                }
                else if (mytype == 2) // sign on
                {
                    ncode = "001";
                    isoType = 1;
                    
                    // 1. create ISO MSG
                    byte[] iso = fm.CreateMsgJSON(ncode,isoType);                              

                    // 2. Send ISO Msg
                    tcp.CheckConn("123.231.225.20", 7777);
                    tcp.Send(iso);

                    // 3. Read Balasan ISO Msg
                    byte[] ret = tcp.Read(10);

                    // 4. Check Data Signature apakah sama ?
                    if (fm.CheckDataSignature(ret,isoType))
                    { 
                        Console.WriteLine("Signature Sarua");
                    }
                }
                else if (mytype == 7) // cut off
                {
                    ncode = "201";
                    isoType = 1;
                    
                    // 1. create ISO MSG
                    byte[] iso = fm.CreateMsgJSON(ncode,isoType);                              

                    // 2. Send ISO Msg
                    tcp.CheckConn("123.231.225.20", 7777);
                    tcp.Send(iso);

                    // 3. Read Balasan ISO Msg
                    byte[] ret = tcp.Read(10);

                    // 4. Check Data Signature apakah sama ?
                    if (fm.CheckDataSignature(ret,isoType))
                    { 
                        Console.WriteLine("Signature Sarua");
                    }
                }
                else if (mytype == 8) // sign off
                {
                    ncode = "002";                    
                    isoType = 1;
                    
                    // 1. create ISO MSG
                    byte[] iso = fm.CreateMsgJSON(ncode,isoType);                              

                    // 2. Send ISO Msg
                    tcp.CheckConn("123.231.225.20", 7777);
                    tcp.Send(iso);

                    // 3. Read Balasan ISO Msg
                    byte[] ret = tcp.Read(10);

                    // 4. Check Data Signature apakah sama ?
                    if (fm.CheckDataSignature(ret,isoType))
                    { 
                        Console.WriteLine("Signature Sarua");
                    }
                }
                else if (mytype == 3) // Inquiry
                {
                    ncode = "000";
                    isoType = 2;

                    // reset
                    fm.rC = "";
                    fm.trxAmount = "";
                    fm.bit61 = "";
                    fm.bit103 = "";

                    Console.Write("Masukan Amount [kosongkan utk pake fix data test] = ");
                    string get1 = Console.ReadLine();
                    if (get1 != "")
                        fm.trxAmount = get1.PadLeft(12, '0');

                    if (fm.trxAmount == "")
                        fm.trxAmount = "000000000000";

                    Console.Write("Masukan Bit 61 [kosongkan utk pake fix data test] = ");
                    string get2 = Console.ReadLine();
                    if (get2 != "")
                        fm.bit61 = get2;

                    if (fm.bit61 == "")
                        fm.bit61 = "0130021004415015";

                    Console.Write("Masukan Bit 103 [kosongkan utk pake fix data test] = ");
                    string get3 = Console.ReadLine();
                    if (get3 != "")
                        fm.bit103 = get3;
                    
                    if (fm.bit103 == "")
                        fm.bit103 = "06001001";

                    Console.WriteLine("Amount = "+ fm.trxAmount);
                    Console.WriteLine("Bit61 = " + fm.bit61);
                    Console.WriteLine("Bit103 = " + fm.bit103);

                    // 1. create ISO MSG
                    byte[] iso = fm.CreateMsgJSON(ncode, isoType);

                    // 2. Send ISO Msg
                    tcp.CheckConn("123.231.225.20", 7777);
                    tcp.Send(iso);

                    // 3. Read Balasan ISO Msg
                    byte[] ret = tcp.Read(10);

                    // 4. Check Data Signature apakah sama ?
                    if (fm.CheckDataSignature(ret, isoType))
                    {
                        Console.WriteLine("Signature Sarua");
                    }
                }                
                else if (mytype == 4) // Transaksi saja
                {
                    ncode = "000";
                    isoType = 3;

                    Console.Write("Masukan Amount [kosongkan jika ambil dari inquiry] = ");
                    string get1 = Console.ReadLine();
                    if (get1 != "")
                        fm.trxAmount = get1.PadLeft(12, '0');                    

                    Console.Write("Masukan Bit 61 [kosongkan jika ambil dari inquiry] = ");
                    string get2 = Console.ReadLine();
                    if (get2 != "")
                        fm.bit61 = get2;
                    
                    Console.Write("Masukan Bit 103 [kosongkan jika ambil dari inquiry] = ");
                    string get3 = Console.ReadLine();
                    if (get3 != "")
                        fm.bit103 = get3;                    

                    // 1. create ISO MSG
                    byte[] iso = fm.CreateMsgJSON(ncode, isoType);

                    // 2. Send ISO Msg
                    tcp.CheckConn("123.231.225.20", 7777);
                    tcp.Send(iso);

                    // 3. Read Balasan ISO Msg
                    byte[] ret = tcp.Read(10);

                    // 4. Check Data Signature apakah sama ?
                    if (fm.CheckDataSignature(ret, isoType))
                    {
                        Console.WriteLine("Signature Sarua");
                    }
                }
                else if (mytype == 5) // Inquiry & Transaksi
                {
                    ncode = "000";
                    isoType = 2;

                    // reset
                    fm.rC = "";
                    fm.trxAmount = "";
                    fm.bit61 = "";
                    fm.bit103 = "";

                    Console.Write("Masukan Amount [kosongkan utk pake fix data test] = ");
                    string get1 = Console.ReadLine();
                    if (get1 != "")
                        fm.trxAmount = get1.PadLeft(12, '0');

                    if (fm.trxAmount == "")
                        fm.trxAmount = "000000000000";

                    Console.Write("Masukan Bit 61 [kosongkan utk pake fix data test] = ");
                    string get2 = Console.ReadLine();
                    if (get2 != "")
                        fm.bit61 = get2;

                    if (fm.bit61 == "")
                        fm.bit61 = "0130021004415015";

                    Console.Write("Masukan Bit 103 [kosongkan utk pake fix data test] = ");
                    string get3 = Console.ReadLine();
                    if (get3 != "")
                        fm.bit103 = get3;

                    if (fm.bit103 == "")
                        fm.bit103 = "06001001";

                    Console.WriteLine("Amount = " + fm.trxAmount);
                    Console.WriteLine("Bit61 = " + fm.bit61);
                    Console.WriteLine("Bit103 = " + fm.bit103);

                    // 1. create ISO MSG
                    byte[] iso = fm.CreateMsgJSON(ncode, isoType);

                    // 2. Send ISO Msg
                    tcp.CheckConn("123.231.225.20", 7777);
                    tcp.Send(iso);

                    // 3. Read Balasan ISO Msg
                    byte[] ret = tcp.Read(10);

                    // 4. Check Data Signature apakah sama ?
                    if (fm.CheckDataSignature(ret, isoType))
                    {
                        Console.WriteLine("Signature Sarua");
                    }

                    if (fm.rC == "00")
                    {
                        ncode = "000";
                        isoType = 3;

                        // 1. create ISO MSG
                        byte[] isoTrx = fm.CreateMsgJSON(ncode, isoType);

                        // 2. Send ISO Msg
                        tcp.Send(isoTrx);

                        // 3. Read Balasan ISO Msg
                        byte[] retTrx = tcp.Read(10);

                        // 4. Check Data Signature apakah sama ?
                        if (fm.CheckDataSignature(retTrx, isoType))
                        {
                            Console.WriteLine("Signature Sarua");
                        }
                    }
                    else 
                    {
                        Console.WriteLine("Inquiry Gagal.");
                    }

                }
                else if (mytype == 6) // reversal
                {
                    Console.WriteLine("Acan di jieun heheheee :D");                    
                    Console.ReadLine();
                }
                Console.WriteLine("Done.");
                Console.WriteLine("");
                Console.Write("Clear Console [y/n] ?");
                
                string mclear = Console.ReadLine();
                if (mclear.ToLower() == "y")
                    Console.Clear();

                TestFinnetModule();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("Error:");
                Console.WriteLine("-----------------------");
                Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                Console.WriteLine("-----------------------");
                Console.WriteLine("Press Enter To Continue");
                Console.ReadLine();
                TestFinnetModule(); 
            }
        }

        

        static void Main(string[] args)
        {
            try
            {                
                TestFinnetModule();                
            }
            catch (Exception e)
            {
                Console.WriteLine("Error..... " + e.Message + "\r\n" + e.StackTrace);
            }
        }

    }
}
