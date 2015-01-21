using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LOG_Handler;

namespace FinnetHandler
{    
    public class FinnetModule: IDisposable
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
        ~FinnetModule()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            jsonH.Dispose();
            conv.Dispose();
        }

        private JsonLibs.MyJsonLib jsonH = new JsonLibs.MyJsonLib();
        private Convertion conv = new Convertion();        
        public byte[] isoMsg = null;
        public string TraceNumber = "";        
        public string rC = "";
        public string trxAmount = "";
        public string bit61 = "";
        public string bit103 = "";
        public string bit60 = "";
        public string bit90 = "";

        public int rsaSize = 1024;
        public byte[] modulus = null;
        public byte[] exponentM = null;
        //static byte[] exponentD = null;
        static byte[] client_modulus = null;
        static byte[] client_exponentM = null;
        static byte[] client_exponentD = null;

        //private int trNum = 0;
        public bool reversalUlang = false;

        PublicSettings.Settings commonSettings;

        public FinnetModule(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            //executePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            // public key na server antrian
			modulus = conv.StringToBytes(commonSettings.getString("Finnet_Queue_RsaKey_Modulus"));
			exponentM = conv.StringToBytes(commonSettings.getString("Finnet_Queue_RsaKey_ExponentM"));

            // public & private key na client
			client_modulus = conv.StringToBytes(commonSettings.getString("Finnet_Client_RsaKey_Modulus"));
			client_exponentM = conv.StringToBytes(commonSettings.getString("Finnet_Client_RsaKey_ExponentM"));
			client_exponentD = conv.StringToBytes(commonSettings.getString("Finnet_Client_RsaKey_ExponentD"));
        }

        //private void updateTraceNumber(string SectionName, string key, string value)
        //{
            //if(SectionName == "")
            //    SectionName = "IsoCfg";

            //// for win32
            //using (CrossIniFile.INIFile a = new CrossIniFile.INIFile(executePath + "\\config.ini"))
            //// for linux                       
            ////using (CrossIniFile.INIFile a = new CrossIniFile.INIFile(executePath + "/config.ini"))
            //{
            //    a.SetValue(SectionName, key, value);
            //}
        //}

        //private string getNextTraceNumber()
        //{            
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
        //    trNum++;
        //    TraceNumber = trNum.ToString().PadLeft(6, '0');
        //    return        trNum.ToString().PadLeft(6, '0');
        //}

        //private string getNextTransamitDate()
        //{
        //    return DateTime.Now.ToString("MMddHHmmss");            
        //}

        public byte[] generateTransactionJson(string network_code, int isoType,
            int traceNumberSeq, long refNumSeq, string sPAN, string MerchantType,
            string TerminalID, string MerchantID, ref string sJson, ref DateTime trxTime)
        {
            string signParam = "";
            jsonH.Clear();
            string strJson = "";
            string tracenum = "";
            string referencenum = "";
            string PAN = sPAN.Length.ToString().PadLeft(2, '0') + sPAN;
            string terminalID = TerminalID.PadLeft(8, '0');
            string merchantID = MerchantID.PadLeft(15,'0');
            string merchantType = MerchantType.ToString().PadLeft(4, '0');

            tracenum = traceNumberSeq.ToString().PadLeft(6, '0');
            referencenum =refNumSeq.ToString().PadLeft(12, '0');

//            RSACryptography rsa;
//            rsa = new RSACryptography(rsaSize, modulus, exponentM, null);
            trxTime = DateTime.Now;
            DateTime skrg = trxTime;

            if (isoType == 1)
            {
                jsonH.Add("MTI", "0800");
                signParam = "0800";

                jsonH.Add("BitMap", "8220000000000000");
                signParam += "8220000000000000";

                jsonH.Add("Bit1", "0400000000000000");
                signParam += "0400000000000000";

                string transmitdate = skrg.ToString("MMddHHmmss");
                jsonH.Add("TransmitionDate", transmitdate);
                signParam += transmitdate;

                jsonH.Add("TraceNumber", tracenum);
                signParam += tracenum;

                jsonH.Add("NetworkManagementInformationCode", network_code);
                signParam += network_code;

                // createa signature                    
				using (RSACryptography rsa = new RSACryptography (rsaSize, modulus, exponentM, null)) {
					string sign = conv.byteArrayToString (rsa.createServerDataSignature (signParam));
					jsonH.Add ("SIGNATURE", sign);
				}
                //Console.WriteLine("jumlah: " + jsonH.Count.ToString());
                strJson = jsonH.JSONConstruct();
                //Console.WriteLine(strJson);
            }
            else if (isoType == 2 || isoType == 3 || isoType == 4) // inqury & transaksi & reversal
            {
                if (isoType != 4) // jika selain reversal
                {
                    jsonH.Add("MTI", "0200");
                    signParam = "0200";

                    jsonH.Add("BitMap", "F23E402188E08008");
                    signParam += "F23E402188E08008";

                    jsonH.Add("Bit1", "0000000002000000");
                    signParam += "0000000002000000";
                }
                else
                {
                    if (!reversalUlang)
                    {
                        jsonH.Add("MTI", "0420");
                        signParam = "0420";
                    }
                    else
                    {
                        jsonH.Add("MTI", "0421");
                        signParam = "0421";
                    }

                    jsonH.Add("BitMap", "F22A00210E208010");
                    signParam += "F22A00210E208010";
                    //jsonH.Add("BitMap", "F22A402108208010");  + bit 18, kkhusus three postpaid
                    //signParam += "F22A402108208010";
                    
                    jsonH.Add("Bit1", "0000004002000000");
                    signParam += "0000004002000000";
                }

                //jsonH.Add("PrimaryAccountNumber", "18604844001027071375"); // => masih contoh.
                //signParam += "18604844001027071375";
                jsonH.Add("PrimaryAccountNumber", PAN); 
                signParam += PAN;

                int lenb61 = 0;
                if ((isoType == 2) && (this.bit61.Length>7))
                {
                    lenb61 = int.Parse(this.bit61.Substring(0, 3));
                    if ((lenb61>=4) && this.bit61.Substring(3, 4) == "0195")  // FINPAY
                    {
                        jsonH.Add("ProcessingCode", "381099");
                        signParam += "381099";
                    }
                    else
                    {
                        jsonH.Add("ProcessingCode", "380099");
                        signParam += "380099";
                    }
                }
                else
                {
                    jsonH.Add("ProcessingCode", "501099");
                    signParam += "501099";
                }

                jsonH.Add("Amount", trxAmount);
                signParam += trxAmount;

                string transmitdate = skrg.ToString("MMddHHmmss");
                jsonH.Add("TransmitionDate", transmitdate);
                signParam += transmitdate;

                //string tn = getNextTraceNumber();
                jsonH.Add("TraceNumber", tracenum);
                signParam += tracenum;

                if (isoType != 4)
                {
                    string trxsTime = skrg.ToString("HHmmss");
                    jsonH.Add("TrxTime", trxsTime); // hhmmss
                    signParam += trxsTime;
                }

                string trxDate = skrg.ToString("MMdd");
                jsonH.Add("TrxDate", trxDate); // MMDD
                signParam += trxDate;

                if (isoType != 4)  //jika bukan reversal
                {
                    jsonH.Add("DateExpiration", "0000"); // MMDD
                    signParam += "0000";
                }

                string dateSettlement = skrg.ToString("MM");
                string d = (int.Parse(skrg.ToString("dd")) + 1).ToString();
                d = d.PadLeft(2, '0');
                jsonH.Add("DateSettlement", dateSettlement + d); // MMDD + 1
                signParam += dateSettlement + d;

                if (isoType != 4)  //jika bukan reversal
                {
                    //jsonH.Add("MerchantType", "6012"); // kalau gk salah 6012 teh  POS, tapi jiga na kudu di ganti jadi 6015 = Kios
                    //signParam += "6012";
                    jsonH.Add("MerchantType", merchantType); // kalau gk salah 6012 teh  POS, tapi jiga na kudu di ganti jadi 6015 = Kios
                    signParam += merchantType;
                }

                //if (!((lenb61 >= 4) && this.bit61.Substring(3, 4) == "0195"))  // FINPAY
                //{
                //    if (isoType != 4) // jika payment, buang data ini
                //    {
                        jsonH.Add("AuthIDResponseLength", "6");
                        signParam += "6";
                //    }
                //}

                jsonH.Add("AcquiringID", "03167"); //LLVAR: kode bank QNB = 167
                signParam += "03167";

                if (isoType != 4)
                {
                    jsonH.Add("ForwadingInstitutionID", "03167"); //LLVAR: kode bank QNB = 167
                    signParam += "03167";
                }

                jsonH.Add("RetrivalReferenceNumber", referencenum);
                signParam += referencenum;

                if (isoType == 4)   // jika reversal
                {
                    jsonH.Add("AuthIDResponse", "000000");
                    signParam += "000000";
                    jsonH.Add("ResponseCode", "68");
                    signParam += "68";
                }
                else
                {
                    // jika bukan reversal
                    //jsonH.Add("TerminalID", "BCIEDC02"); // => masih dummy jigana. ke kudu di ubah teuing naon tapi
                    //signParam += "BCIEDC02";
                    jsonH.Add("TerminalID", terminalID); // => masih dummy jigana. ke kudu di ubah teuing naon tapi
                    signParam += terminalID;

                    //jsonH.Add("MerchantID", "123456789012345"); // => masih dummy jigana. ke kudu di ubah teuing naon tapi
                    //signParam += "123456789012345";
                    jsonH.Add("MerchantID", merchantID); // => masih dummy jigana. ke kudu di ubah teuing naon tapi
                    signParam += merchantID;
                }

                string cardAccName = "MENARA BCD LT.16                        ";
                jsonH.Add("CardAcceptorName", cardAccName); // => masih dummy jigana. ke kudu di ubah teuing naon tapi, hati2 fix length aya white space an!!!
                signParam += cardAccName;

                jsonH.Add("CurrentcyCode", "360");
                signParam += "360";

                if (isoType != 4)
                {
                    jsonH.Add("PrivateData2", bit61); // LLLVAR: KASUS UNTUK Tagihan Telpon Rumah, di isi nomnor telpon rumah. 013 + 0021004415015 . aya di doc PDF na finnet
                    signParam += bit61;
                }
                else
                {       // jika reversal
                    //  "003090"; // 090: Timeout, 050: canceled, 032:system error, 030: hardwre error.
                    //string bit60 = "003090";
                    jsonH.Add("PrivateData1", bit60);
                    signParam += bit60;

                    jsonH.Add("OriginalDataElement", bit90);
                    signParam += bit90;
                }

                jsonH.Add("AccountIdentification2", bit103); // Ieu Kode Product, aya di doc PDF na finnet
                signParam += bit103;

                // createa signature                        
                //rsa = new RSACryptography(rsaSize, modulus, exponentM, null);
				using (RSACryptography rsa = new RSACryptography (rsaSize, modulus, exponentM, null)) {
					string sign = conv.byteArrayToString (rsa.createServerDataSignature (signParam));
					jsonH.Add ("SIGNATURE", sign);
				}

                //Console.WriteLine("jumlah: " + jsonH.Count.ToString());
                strJson = jsonH.JSONConstruct();
                //Console.WriteLine(strJson);
                //LogWriter.showDEBUG(this, strJson);
            }
            sJson = strJson;
            return System.Text.ASCIIEncoding.ASCII.GetBytes(strJson);
        }

        //public byte[] CreateMsgJSONx(string network_code, int isoType)
        //{            
        //    string signParam = "";
        //    jsonH.Clear();
        //    string strJson = "";
        //    RSACryptography rsa;

        //    if(isoType == 1)
        //    {                
        //        jsonH.Add("MTI", "0800");
        //        signParam = "0800";

        //        jsonH.Add("BitMap", "8220000000000000");
        //        signParam += "8220000000000000";

        //        jsonH.Add("Bit1", "0400000000000000");
        //        signParam += "0400000000000000";

        //        string transmitdate = getNextTransamitDate();
        //        jsonH.Add("TransmitionDate", transmitdate);
        //        signParam += transmitdate;

        //        string tn = getNextTraceNumber();
        //        jsonH.Add("TraceNumber", tn);
        //        signParam += tn;

        //        jsonH.Add("NetworkManagementInformationCode", network_code);
        //        signParam += network_code;

        //        // createa signature                             
        //        rsa = new RSACryptography(rsaSize, modulus, exponentM, null);
        //        string sign = conv.byteArrayToString(rsa.createServerDataSignature(signParam));
        //        jsonH.Add("SIGNATURE", sign);

        //        //Console.WriteLine("jumlah: " + jsonH.Count.ToString());
        //        strJson = jsonH.JSONConstruct();
        //        //Console.WriteLine(strJson);
        //    }
        //    else if(isoType == 2 || isoType == 3) // inqury & transaksi
        //    {
        //        jsonH.Add("MTI", "0200");
        //        signParam = "0200";

        //        jsonH.Add("BitMap", "F23E402188E08008");
        //        signParam += "F23E402188E08008";

        //        jsonH.Add("Bit1", "0000000002000000");
        //        signParam += "0000000002000000";

        //        jsonH.Add("PrimaryAccountNumber", "18604844001027071375"); // => masih contoh.
        //        signParam += "18604844001027071375";

        //        if(isoType == 2)
        //        {
        //            jsonH.Add("ProcessingCode", "380099");
        //            signParam += "380099";                    
        //        }
        //        else
        //        {
        //            jsonH.Add("ProcessingCode", "501099");
        //            signParam += "501099";
        //        }

        //        jsonH.Add("Amount", trxAmount);
        //        signParam += trxAmount;

        //        string transmitdate = getNextTransamitDate();
        //        jsonH.Add("TransmitionDate", transmitdate);
        //        signParam += transmitdate;

        //        string tn = getNextTraceNumber();
        //        jsonH.Add("TraceNumber", tn);
        //        signParam += tn;
                
        //        string trxTime = DateTime.Now.ToString("HHmmss");
        //        jsonH.Add("TrxTime", trxTime); // hhmmss
        //        signParam += trxTime;

        //        string trxDate = DateTime.Now.ToString("MMdd");
        //        jsonH.Add("TrxDate", trxDate); // MMDD
        //        signParam += trxDate;

        //        jsonH.Add("DateExpiration", "0000"); // MMDD
        //        signParam += "0000";

        //        string dateSettlement = DateTime.Now.ToString("MM");
        //        string d = (int.Parse(DateTime.Now.ToString("dd")) + 1).ToString();
        //        d = d.PadLeft(2, '0');
        //        jsonH.Add("DateSettlement", dateSettlement + d); // MMDD + 1
        //        signParam += dateSettlement + d;

        //        jsonH.Add("MerchantType", "6012"); // kalau gk salah 6012 teh  POS, tapi jiga na kudu di ganti jadi 6015 = Kios
        //        signParam += "6012";

        //        jsonH.Add("AuthIDResponseLength", "6");
        //        signParam += "6";

        //        jsonH.Add("AcquiringID", "03167"); //LLVAR: kode bank
        //        signParam += "03167";

        //        jsonH.Add("ForwadingInstitutionID", "03167"); //LLVAR: kode bank
        //        signParam += "03167";

        //        //string rrn = tnumer.ToString().PadLeft(12, '0');
        //        string rrn = TraceNumber.PadLeft(12, '0');
        //        jsonH.Add("RetrivalReferenceNumber", rrn);
        //        signParam += rrn;

        //        //jsonH.Add("TerminalID", "BCIEDC02"); // => masih dummy jigana. ke kudu di ubah teuing naon tapi
        //        jsonH.Add("TerminalID", commonSettings.getString("CommonTerminalID")); // => masih dummy jigana. ke kudu di ubah teuing naon tapi
        //        signParam += "BCIEDC02";

        //        //jsonH.Add("MerchantID", "123456789012345"); // => masih dummy jigana. ke kudu di ubah teuing naon tapi
        //        jsonH.Add("MerchantID", commonSettings.getString("CommonMerchantID")); // => masih dummy jigana. ke kudu di ubah teuing naon tapi
        //        signParam += "123456789012345";

        //        jsonH.Add("CardAcceptorName", "MENARA BCD LT.16                        "); // => masih dummy jigana. ke kudu di ubah teuing naon tapi, hati2 fix length aya white space an!!!
        //        signParam += "MENARA BCD LT.16                        ";

        //        jsonH.Add("CurrentcyCode", "360");
        //        signParam += "360";

        //        jsonH.Add("PrivateData2", bit61); // LLLVAR: KASUS UNTUK Tagihan Telpon Rumah, di isi nomnor telpon rumah. 013 + 0021004415015 . aya di doc PDF na finnet
        //        signParam += bit61;                

        //        jsonH.Add("AccountIdentification2", bit103); // Ieu Kode Product, aya di doc PDF na finnet
        //        signParam += bit103;                

        //        // createa signature                        
        //        rsa = new RSACryptography(rsaSize, modulus, exponentM, null);
        //        string sign = conv.byteArrayToString(rsa.createServerDataSignature(signParam));
        //        jsonH.Add("SIGNATURE", sign);

        //        //Console.WriteLine("jumlah: " + jsonH.Count.ToString());
        //        strJson = jsonH.JSONConstruct();
        //        //Console.WriteLine(strJson);
                                                                                
        //    }

        //    // update sequence trace number
        //    if (TraceNumber == "999999")
        //        updateTraceNumber("IsoCfg", "TraceNumber", "0");
        //    else
        //    {
        //        int tn_ = int.Parse(TraceNumber);
        //        updateTraceNumber("IsoCfg", "TraceNumber", tn_.ToString());
        //    }

        //    return System.Text.ASCIIEncoding.ASCII.GetBytes(strJson);                                
        //}

        //public bool CheckDataSignature(byte[] brecv, int isoType)
        //{            
        //    // parsing dan cek server signature cikan sarua teu
        //    string sJson = System.Text.ASCIIEncoding.ASCII.GetString(brecv);
        public bool CheckDataSignature(string srecv, int isoType)
        {            
            // parsing dan cek server signature cikan sarua teu
            //string sJson = System.Text.ASCIIEncoding.ASCII.GetString(brecv);
            string sJson = srecv;
            //Console.WriteLine("Received Data : " + sJson);
            RSACryptography rsa;

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
                    LogWriter.showDEBUG(this, "Gagal Parsing JSON Data");
                    return false;
                }

                // cek bae
                if (isoMTI == "" || TraceNumber == "" || dataSign == "")
                {
                    LogWriter.showDEBUG(this, "Data Gak Lengkap");
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
