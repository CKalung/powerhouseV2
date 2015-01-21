using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LOG_Handler;
using StaticCommonLibrary;

namespace FMHandler
{    
    public class FMModule: IDisposable
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
        ~FMModule()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
        }

        private JsonLibs.MyJsonLib jsonH = new JsonLibs.MyJsonLib();
        private Convertion conv = new Convertion();        
        public byte[] isoMsg = null;
        public string TraceNumber = "";        
        public string rC = "";
        public string trxAmount = "";
        public string bit48 = "";
        public string bit2 = "";
        public int rsaSize = 1024;
        public byte[] modulus = null;
        public byte[] exponentM = null;
        //static byte[] exponentD = null;
        static byte[] client_modulus = null;
        static byte[] client_exponentM = null;
        static byte[] client_exponentD = null;

        //private int trNum = 0;

        PublicSettings.Settings commonSettings;

        public FMModule(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            //executePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

			// public key na server antrian
			modulus = conv.StringToBytes(commonSettings.getString("Fm_Queue_RsaKey_Modulus"));
			exponentM = conv.StringToBytes(commonSettings.getString("Fm_Queue_RsaKey_ExponentM"));

			// public & private key na client
			client_modulus = conv.StringToBytes(commonSettings.getString("Fm_Client_RsaKey_Modulus"));
			client_exponentM = conv.StringToBytes(commonSettings.getString("Fm_Client_RsaKey_ExponentM"));
			client_exponentD = conv.StringToBytes(commonSettings.getString("Fm_Client_RsaKey_ExponentD"));
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
            //trNum++;
            //TraceNumber = trNum.ToString().PadLeft(6, '0');
            //return trNum.ToString().PadLeft(6, '0');
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

            //string PAN = sPAN.Length.ToString().PadLeft(2, '0') + sPAN;
            string terminalID = TerminalID.PadLeft(8, '0');
            string merchantID = MerchantID.PadLeft(15, '0');
            string merchantType = MerchantType.Length.ToString().PadLeft(4, '0');

            tracenum = traceNumberSeq.ToString().PadLeft(6, '0');
            referencenum = refNumSeq.ToString().PadLeft(12, '0');

            RSACryptography rsa;
            rsa = new RSACryptography(rsaSize, modulus, exponentM, null);
            trxTime = DateTime.Now;
            DateTime skrg = trxTime;

            //if (isoType == 2 || isoType == 3) // inqury & transaksi
            //{
                if (isoType == 2)
                    jsonH.Add("DataType", "00");        // inquiry
                else if (isoType == 3)
                    jsonH.Add("DataType", "01");        // transaction
                else              // reversal isoType = 4
                    jsonH.Add("DataType", "02");        // reversal

                jsonH.Add("TraceNumber", tracenum);
                signParam += tracenum;

				//if (isoType == 3)
			if ((isoType == 3) || (isoType == 4)) {
				try {
					LogWriter.showDEBUG (this, "=== REKONSTRUKSI bit 48 untuk FM === isotype : " + isoType.ToString());
					// proses bit48 anu ti client jang kirimeun ka FM host
					string subsciberID = bit48.Substring (3, 12); // ieu ID Pelanggan/ Subscriber ID fix length 12 digit.
					//string pelangganName = bit48.Substring(15, 20); // bisi aya keperluan ambil nama pelanggan. fix length 20 digit.
					string periodeTayang = bit48.Substring (35, 21); // Periode tayang dd/mm/yyyy-dd/mm/yyyy fix length 21 digit.
					string jumlahTagihan = bit48.Substring (56, 12); // total tagihan left pad 0. fix length 12 digit.
					string billRef = bit48.Substring (68, 8); // nomor referensi tagihan fix length 8 digit.
					//int sLenBit48 = int.Parse(bit48.Substring(0, 3));
					//int len = (sLenBit48 - 76);
					//string keterangan = bit48.Substring(76, 100); // keterangan, fix length 100 digit.
					//string fix1 = "xyz"; // sesuai sample
					string kodeInstansiPembayar = commonSettings.getString("Fm_KodeInstansiPembayar");

					// teu kabeh isi bit48 di jebluskeun. tapi cuman : subsciberID + billRef + periodeTayang + jumlahTagihan + fix1
					//$res['trxbit48'] = $subsciberID . $billRef . $periodeTayang . $jumlahTagihan . $fix1;
					//bit48 = subsciberID + billRef + periodeTayang + jumlahTagihan + fix1;
					bit48 = subsciberID + billRef + periodeTayang + jumlahTagihan + kodeInstansiPembayar;
				} catch (Exception ex) {
					LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Connect to FM Queue host has failed : " + ex.getCompleteErrMsg ());
					return null;
				}
			} else {
				LogWriter.showDEBUG (this, "=== TIDAK REKONSTRUKSI bit 48 untuk FM === isotype : " + isoType.ToString());

			}

                jsonH.Add("TrxData", bit48);
                signParam += bit48;

                signParam += trxAmount;
                jsonH.Add("TrxAmount", trxAmount);
                signParam += trxAmount;

                //if (isoType == 3)
                //{
                //    bit2 = bit2.Substring(2, 5);
                //}

                jsonH.Add("ProductCode", bit2);
                signParam += bit2;

                // createa signature                        
                //rsa = new RSACryptography(rsaSize, modulus, exponentM, null);
                string sign = conv.byteArrayToString(rsa.createServerDataSignature(signParam));
                jsonH.Add("SIGNATURE", sign);

                //Console.WriteLine("jumlah: " + jsonH.Count.ToString());
                strJson = jsonH.JSONConstruct();
                //Console.WriteLine(strJson);

            //}
            sJson = strJson;
            return System.Text.ASCIIEncoding.ASCII.GetBytes(strJson);
        }

        //public byte[] CreateMsgJSON(int isoType)
        //{
        //    string signParam = "";
        //    jsonH.Clear();
        //    string strJson = "";

        //    if (isoType == 1 || isoType == 2) // inqury & transaksi
        //    {
        //        if (isoType == 1)
        //            jsonH.Add("DataType", "00");
        //        else
        //            jsonH.Add("DataType", "01");

        //        string tn = getNextTraceNumber();
        //        jsonH.Add("TraceNumber", tn);
        //        signParam += tn;

        //        if (isoType == 2)
        //        {
        //            int sLenBit48 = int.Parse(bit48.Substring(0, 3));

        //            string subsciberID = bit48.Substring(3, 12); // ieu ID Pelanggan/ Subscriber ID fix length 12 digit.
        //            string pelangganName = bit48.Substring(15, 20); // bisi aya keperluan ambil nama pelanggan. fix length 20 digit.
        //            string periodeTayang = bit48.Substring(35, 21); // Periode tayang dd/mm/yyyy-dd/mm/yyyy fix length 21 digit.
        //            string jumlahTagihan = bit48.Substring(56, 12); // total tagihan left pad 0. fix length 12 digit.
        //            string billRef = bit48.Substring(68, 8); // nomor referensi tagihan fix length 8 digit.
        //            string keterangan = bit48.Substring(76, 100); // keterangan, fix length 100 digit.
        //            string fix1 = "xyz"; // sesuai sample

        //            // teu kabeh isi bit48 di jebluskeun. tapi cuman : subsciberID + billRef + periodeTayang + jumlahTagihan + fix1
        //            bit48 = subsciberID + billRef + periodeTayang + jumlahTagihan + fix1;

        //            // khususon 2 <-------------------------
        //            // cokot ti bit4 hasil inquiry isi na kudu sarua jeung totalan di hasil inquiry di bit48
        //            // conto bit4 hasil inquiry isina       = 000000129500
        //            // berarti bit4 pas transaksi ge kudu   = 000000129500
        //            // begitu.
        //            // cek dulu apakah jumlahTagihan == Amount
        //            if ((int.Parse(trxAmount)) == (int.Parse(jumlahTagihan)))
        //            {
        //                //trxAmount = jsonH["Amount"].ToString();                                                
        //            }
        //            else
        //            {
        //                Console.WriteLine("Bit4 Amount tidak sesuai/ tidak sama dengan jumlah Tagihan di Bit48.");
        //                return null;
        //            }

        //        }
        //        jsonH.Add("TrxData", bit48);
        //        signParam += bit48;

        //        signParam += trxAmount;
        //        jsonH.Add("TrxAmount", trxAmount);
        //        signParam += trxAmount;

        //        if (isoType == 2)
        //        {
        //            bit2 = bit2.Substring(2, 5);
        //        }

        //        jsonH.Add("ProductCode", bit2);
        //        signParam += bit2;

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

        public bool CheckDataSignature(string brecv, int isoType)
        {
            // parsing dan cek server signature cikan sarua teu
            //string sJson = System.Text.ASCIIEncoding.ASCII.GetString(brecv);
            string sJson = brecv;
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
                    if (isoType == 2 || isoType == 3) // inqury & transaksi
                    {
                        myIsoMsg = jsonH["MTI"].ToString();
                        myIsoMsg += jsonH["BitMap"].ToString();
                        myIsoMsg += jsonH["PrimaryAccountNumber"].ToString();
                        myIsoMsg += jsonH["ProcessingCode"].ToString();
                        myIsoMsg += jsonH["Amount"].ToString();
                        myIsoMsg += jsonH["TransmitionDate"].ToString();
                        myIsoMsg += jsonH["TraceNumber"].ToString();
                        myIsoMsg += jsonH["TrxTime"].ToString();
                        myIsoMsg += jsonH["TrxDate"].ToString();
                        myIsoMsg += jsonH["DateSettlement"].ToString();
                        myIsoMsg += jsonH["MerchantType"].ToString();
                        myIsoMsg += jsonH["AcquiringID"].ToString();
                        myIsoMsg += jsonH["RetrivalReferenceNumber"].ToString();
                        myIsoMsg += jsonH["ResponseCode"].ToString();
                        myIsoMsg += jsonH["TerminalID"].ToString();
                        myIsoMsg += jsonH["Bit48"].ToString();
                        myIsoMsg += jsonH["CurrentcyCode"].ToString();
                        myIsoMsg += jsonH["InstutionID"].ToString();

                        // TODO: disini kudu aya pengecekan data signture na
                        if (rsa.checkServerDataSignature(dataSign, myIsoMsg) == 0)
                        {
                            //Console.WriteLine("Signature Beda!!!");
                            return false;
                        }

                        // set trx variable
                        rC = jsonH["ResponseCode"].ToString();
                        trxAmount = jsonH["Amount"].ToString();
                        bit48 = jsonH["Bit48"].ToString();
                        bit2 = jsonH["PrimaryAccountNumber"].ToString();
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
