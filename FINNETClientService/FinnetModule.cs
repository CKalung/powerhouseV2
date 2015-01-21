using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client
{    
    class FinnetModule
    {
        private JsonLibs.MyJsonLib jsonH = new JsonLibs.MyJsonLib();
        private Convertion conv = new Convertion();        
        public byte[] isoMsg = null;
        public string TraceNumber = "";        
        public string rC = "";
        public string trxAmount = "";
        public string bit61 = "";
        public string bit103 = "";
        public int rsaSize = 1024;
        public byte[] modulus = null;
        public byte[] exponentM = null;
        //static byte[] exponentD = null;
        static byte[] client_modulus = null;
        static byte[] client_exponentM = null;
        static byte[] client_exponentD = null;
        private RSACryptography rsa;

        private int trNum = 0;

        public string executePath = "";

        public FinnetModule()
        {
            executePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            
            // public key na server antrian
            modulus = conv.StringToBytes("9A938A4463F1B6FCBDB35436C8753241672CE654675D3C03B559553E851206C95F116098C27DB4B55879AA6B4F7C0316AD9B59714247DD99E85549A130FBC3C301C9BC7C57228F4D2969870D14EF182E653D08270CCD2316FE1414D16B3C705BD625604ACF983949B2D78DB22C44D64CE6CD4646DE6EB6099D9C6C3D9BF18629");
            exponentM = conv.StringToBytes("010001");

            // public & private key na client
            client_modulus = conv.StringToBytes("A204C3821AE7A67A22DEE6F16E9705BEB9DB09FCF32E450B7CD3D44A1246C369306946F950F9D2589B7395ABD0F7BD2B97F7304D5E79D149EA9FF7D53D61446152AC77B0B87B46ACAA46A93F192C9D7190254B3B28F2BB7B34E89C620CE6122E4C77AA8B1330627F089E4B4EA6CB2942D6A73DB9B43EBC2B54675C280F5F4AAB");
            client_exponentM = conv.StringToBytes("010001");
            client_exponentD = conv.StringToBytes("4C3DE1A2BFF672A8D6EFFDCD6F353246E63EE51C5B73529A6D4B6182D9C6E2FE0502059C1D36F27D2FE9DC6CD6113EBBDCEF3D93AAF9B83B0865EEC231F82BACC101260F38D47D1547C9494A78EABEE19D8B8591983A1F11EBC45985B410E2CECD459BF9C0AF99BB152A3128ED038912DBCD1C7E9704ECF6A6B284310A48EE01");
        }

        private void updateTraceNumber(string SectionName, string key, string value)
        {
            //if(SectionName == "")
            //    SectionName = "IsoCfg";

            //// for win32
            //using (CrossIniFile.INIFile a = new CrossIniFile.INIFile(executePath + "\\config.ini"))
            //// for linux                       
            ////using (CrossIniFile.INIFile a = new CrossIniFile.INIFile(executePath + "/config.ini"))
            //{
            //    a.SetValue(SectionName, key, value);
            //}
        }

        private string getNextTraceNumber()
        {            
            //// for win32
            //using (CrossIniFile.INIFile a = new CrossIniFile.INIFile(executePath + "\\config.ini"))
            //// for linux            
            ////using (CrossIniFile.INIFile a = new CrossIniFile.INIFile(executePath + "/config.ini"))
            //{
            //    int tn = int.Parse(a.GetValue("IsoCfg", "TraceNumber", "5"));
            //    tn++;
            //    TraceNumber = tn.ToString().PadLeft(6,'0');
            //}
            //return TraceNumber;
            trNum++;
            TraceNumber = trNum.ToString().PadLeft(6, '0');
            return trNum.ToString().PadLeft(6, '0');
        }

        private string getNextTransamitDate()
        {
            return DateTime.Now.ToString("MMddHHmmss");            
        }

        public byte[] CreateMsgJSON(string network_code, int isoType)
        {            
            string signParam = "";
            jsonH.Clear();
            string strJson = "";

            if(isoType == 1)
            {                
                jsonH.Add("MTI", "0800");
                signParam = "0800";

                jsonH.Add("BitMap", "8220000000000000");
                signParam += "8220000000000000";

                jsonH.Add("Bit1", "0400000000000000");
                signParam += "0400000000000000";

                string transmitdate = getNextTransamitDate();
                jsonH.Add("TransmitionDate", transmitdate);
                signParam += transmitdate;

                string tn = getNextTraceNumber();
                jsonH.Add("TraceNumber", tn);
                signParam += tn;

                jsonH.Add("NetworkManagementInformationCode", network_code);
                signParam += network_code;

                // createa signature                             
                rsa = new RSACryptography(rsaSize, modulus, exponentM, null);
                string sign = conv.byteArrayToString(rsa.createServerDataSignature(signParam));
                jsonH.Add("SIGNATURE", sign);

                //Console.WriteLine("jumlah: " + jsonH.Count.ToString());
                strJson = jsonH.JSONConstruct();
                //Console.WriteLine(strJson);
            }
            else if(isoType == 2 || isoType == 3) // inqury & transaksi
            {
                jsonH.Add("MTI", "0200");
                signParam = "0200";

                jsonH.Add("BitMap", "F23E402188E08008");
                signParam += "F23E402188E08008";

                jsonH.Add("Bit1", "0000000002000000");
                signParam += "0000000002000000";

                jsonH.Add("PrimaryAccountNumber", "18604844001027071375"); // => masih contoh.
                signParam += "18604844001027071375";

                if(isoType == 2)
                {
                    jsonH.Add("ProcessingCode", "380099");
                    signParam += "380099";                    
                }
                else
                {
                    jsonH.Add("ProcessingCode", "501099");
                    signParam += "501099";
                }

                jsonH.Add("Amount", trxAmount);
                signParam += trxAmount;

                string transmitdate = getNextTransamitDate();
                jsonH.Add("TransmitionDate", transmitdate);
                signParam += transmitdate;

                string tn = getNextTraceNumber();
                jsonH.Add("TraceNumber", tn);
                signParam += tn;
                
                string trxTime = DateTime.Now.ToString("HHmmss");
                jsonH.Add("TrxTime", trxTime); // hhmmss
                signParam += trxTime;

                string trxDate = DateTime.Now.ToString("MMdd");
                jsonH.Add("TrxDate", trxDate); // MMDD
                signParam += trxDate;

                jsonH.Add("DateExpiration", "0000"); // MMDD
                signParam += "0000";

                string dateSettlement = DateTime.Now.ToString("MM");
                string d = (int.Parse(DateTime.Now.ToString("dd")) + 1).ToString();
                d = d.PadLeft(2, '0');
                jsonH.Add("DateSettlement", dateSettlement + d); // MMDD + 1
                signParam += dateSettlement + d;

                jsonH.Add("MerchantType", "6012"); // kalau gk salah 6012 teh  POS, tapi jiga na kudu di ganti jadi 6015 = Kios
                signParam += "6012";

                jsonH.Add("AuthIDResponseLength", "6");
                signParam += "6";

                jsonH.Add("AcquiringID", "03167"); //LLVAR: kode bank
                signParam += "03167";

                jsonH.Add("ForwadingInstitutionID", "03167"); //LLVAR: kode bank
                signParam += "03167";

                //string rrn = tnumer.ToString().PadLeft(12, '0');
                string rrn = TraceNumber.PadLeft(12, '0');
                jsonH.Add("RetrivalReferenceNumber", rrn);
                signParam += rrn;

                jsonH.Add("TerminalID", "BCIEDC02"); // => masih dummy jigana. ke kudu di ubah teuing naon tapi
                signParam += "BCIEDC02";

                jsonH.Add("MerchantID", "123456789012345"); // => masih dummy jigana. ke kudu di ubah teuing naon tapi
                signParam += "123456789012345";

                jsonH.Add("CardAcceptorName", "MENARA BCD LT.16                        "); // => masih dummy jigana. ke kudu di ubah teuing naon tapi, hati2 fix length aya white space an!!!
                signParam += "MENARA BCD LT.16                        ";

                jsonH.Add("CurrentcyCode", "360");
                signParam += "360";

                jsonH.Add("PrivateData2", bit61); // LLLVAR: KASUS UNTUK Tagihan Telpon Rumah, di isi nomnor telpon rumah. 013 + 0021004415015 . aya di doc PDF na finnet
                signParam += bit61;                

                jsonH.Add("AccountIdentification2", bit103); // Ieu Kode Product, aya di doc PDF na finnet
                signParam += bit103;                

                // createa signature                        
                rsa = new RSACryptography(rsaSize, modulus, exponentM, null);
                string sign = conv.byteArrayToString(rsa.createServerDataSignature(signParam));
                jsonH.Add("SIGNATURE", sign);

                //Console.WriteLine("jumlah: " + jsonH.Count.ToString());
                strJson = jsonH.JSONConstruct();
                //Console.WriteLine(strJson);
                                                                                
            }

            // update sequence trace number
            if (TraceNumber == "999999")
                updateTraceNumber("IsoCfg", "TraceNumber", "0");
            else
            {
                int tn_ = int.Parse(TraceNumber);
                updateTraceNumber("IsoCfg", "TraceNumber", tn_.ToString());
            }

            return System.Text.ASCIIEncoding.ASCII.GetBytes(strJson);                                
        }

        public bool CheckDataSignature(byte[] brecv, int isoType)
        {            
            // parsing dan cek server signature cikan sarua teu
            string sJson = System.Text.ASCIIEncoding.ASCII.GetString(brecv);
            Console.WriteLine("Received Data : " + sJson);

            if (sJson != "")
            {
                jsonH.Clear();
                string dataSign = "";
                string isoMTI = "";
                string myIsoMsg = "";

                if (jsonH.JSONParse(sJson))
                {
                    isoMTI = jsonH["MTI"].ToString();
                    TraceNumber = jsonH["TraceNumber"].ToString();
                    dataSign = jsonH["SIGNATURE"].ToString();
                }
                else
                {
                    Console.WriteLine("Gagal Parsing JSON Data");
                    return false;
                }

                // cek bae
                if (isoMTI == "" || TraceNumber == "" || dataSign == "")
                {
                    Console.WriteLine("Data Gak Lengkap");
                    return false;
                }
                else
                {
                    rsa = new RSACryptography(rsaSize, client_modulus, client_exponentM, client_exponentD);
                    if (isoType == 1) // Network management, test echo, sign on, sign off, cut off
                    {
                        myIsoMsg = jsonH["MTI"].ToString();
                        myIsoMsg += jsonH["BitMap"].ToString();
                        myIsoMsg += jsonH["Bit1"].ToString();
                        myIsoMsg += jsonH["TransmitionDate"].ToString();
                        myIsoMsg += jsonH["TraceNumber"].ToString();
                        myIsoMsg += jsonH["ResponseCode"].ToString();
                        myIsoMsg += jsonH["NetworkManagementInformationCode"].ToString();
                        rC = jsonH["ResponseCode"].ToString();

                        // pengecekan data signture na                        
                        if (rsa.checkServerDataSignature(dataSign, myIsoMsg) == 0)
                        {
                            //Console.WriteLine("Signature Beda!!!");
                            return false;
                        }
                    }
                    else if (isoType == 2) // inqury & transaksi
                    {
                        myIsoMsg = jsonH["MTI"].ToString();
                        myIsoMsg += jsonH["BitMap"].ToString();
                        myIsoMsg += jsonH["Bit1"].ToString();
                        myIsoMsg += jsonH["PrimaryAccountNumber"].ToString();
                        myIsoMsg += jsonH["ProcessingCode"].ToString();
                        myIsoMsg += jsonH["Amount"].ToString();
                        myIsoMsg += jsonH["TransmitionDate"].ToString();
                        myIsoMsg += jsonH["TraceNumber"].ToString();
                        myIsoMsg += jsonH["DateSettlement"].ToString();
                        myIsoMsg += jsonH["AuthIDResponseLength"].ToString();
                        myIsoMsg += jsonH["AcquiringID"].ToString();
                        myIsoMsg += jsonH["ForwadingInstitutionID"].ToString();
                        myIsoMsg += jsonH["RetrivalReferenceNumber"].ToString();
                        myIsoMsg += jsonH["AuthIDResponse"].ToString();
                        myIsoMsg += jsonH["ResponseCode"].ToString();
                        myIsoMsg += jsonH["TerminalID"].ToString();
                        myIsoMsg += jsonH["CurrentcyCode"].ToString();
                        myIsoMsg += jsonH["PrivateData2"].ToString();
                        myIsoMsg += jsonH["AccountIdentification2"].ToString();

                        // TODO: disini kudu aya pengecekan data signture na
                        if (rsa.checkServerDataSignature(dataSign, myIsoMsg) == 0)
                        {
                            //Console.WriteLine("Signature Beda!!!");
                            return false;
                        }

                        // set trx variable
                        rC = jsonH["ResponseCode"].ToString();
                        trxAmount = jsonH["Amount"].ToString();
                        bit61 = jsonH["PrivateData2"].ToString();
                        bit103 = jsonH["AccountIdentification2"].ToString();
                    }
                    //Console.WriteLine("Signature Sarua Cuy.");                    
                }
            }
            else 
            {
                return false;
            }

            return true;
        }                        
    }
}
