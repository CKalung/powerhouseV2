using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LOG_Handler;

namespace PH_LeopardHandler
{    
    public class LeopardModule: IDisposable
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
        ~LeopardModule()
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

        public LeopardModule(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            //executePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

			// public key na server antrian
			modulus = conv.StringToBytes(commonSettings.getString("Leopard_Queue_RsaKey_Modulus"));
			exponentM = conv.StringToBytes(commonSettings.getString("Leopard_Queue_RsaKey_ExponentM"));

			// public & private key na client
			client_modulus = conv.StringToBytes(commonSettings.getString("Leopard_Client_RsaKey_Modulus"));
			client_exponentM = conv.StringToBytes(commonSettings.getString("Leopard_Client_RsaKey_ExponentM"));
			client_exponentD = conv.StringToBytes(commonSettings.getString("Leopard_Client_RsaKey_ExponentD"));
		}

        public byte[] generateTransactionJson(string transactionReference, 
			string providerProductCode, long refNumSeq, string strTrxType, ref string sJson)
        {
            string signParam = "";
            jsonH.Clear();
            string strJson = "";
            //string tracenum = "";
            string referencenum = "";
            //string terminalID = TerminalID.PadLeft(8, '0');
            //string merchantID = MerchantID.PadLeft(15, '0');
            //string merchantType = MerchantType.ToString().PadLeft(4, '0');

            //tracenum = traceNumberSeq.ToString().PadLeft(6, '0');
            referencenum = refNumSeq.ToString().PadLeft(12, '0');

            RSACryptography rsa;
            rsa = new RSACryptography(rsaSize, modulus, exponentM, null);
            //trxTime = DateTime.Now;
            //DateTime skrg = trxTime;

            jsonH.Add("customerId", transactionReference);
            signParam += transactionReference;

            jsonH.Add("productCode", providerProductCode);
            signParam += providerProductCode;

            jsonH.Add("systemTrxId", referencenum);
            signParam += referencenum;

            //jsonH.Add("DataType", "inquiry");
			//jsonH.Add("DataType", "purchase");
			jsonH.Add("DataType", strTrxType);

            // createa signature                        
            //rsa = new RSACryptography(rsaSize, modulus, exponentM, null);
            string sign = conv.byteArrayToString(rsa.createServerDataSignature(signParam));
            jsonH.Add("SIGNATURE", sign);

            //Console.WriteLine("jumlah: " + jsonH.Count.ToString());
            strJson = jsonH.JSONConstruct();
            //Console.WriteLine(strJson);
            //LogWriter.showDEBUG(this, strJson);

            sJson = strJson;
            return System.Text.ASCIIEncoding.ASCII.GetBytes(strJson);
        }

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
                string myIsoMsg = "";

                if (jsonH.JSONParse(sJson))
                {
                    TraceNumber = jsonH["TraceNumber"].ToString();
                    dataSign = jsonH["SIGNATURE"].ToString();
                }
                else
                {
                    LogWriter.showDEBUG(this, "Gagal Parsing JSON Data");
                    return false;
                }

                // cek bae
                rsa = new RSACryptography(rsaSize, client_modulus, client_exponentM, client_exponentD);
                try
                {
                    myIsoMsg = jsonH["RESPONSE_CODE"].ToString();

                    if (myIsoMsg == "00")
                    {
                        myIsoMsg += jsonH["TRXID"].ToString();
                        myIsoMsg += jsonH["TGL"].ToString();
                        myIsoMsg += jsonH["PRODUK"].ToString();
                        myIsoMsg += jsonH["DENOM"].ToString();
                        myIsoMsg += jsonH["CID"].ToString();
                        myIsoMsg += jsonH["VA_DEBET"].ToString();
                        myIsoMsg += jsonH["MESSAGE"].ToString();
                        myIsoMsg += jsonH["TRANS_ID"].ToString();
                        myIsoMsg += jsonH["BALANCE"].ToString();
                        dataSign = jsonH["SIGNATURE"].ToString();

                        // TODO: disini kudu aya pengecekan data signture na
                        if (rsa.checkServerDataSignature(dataSign, myIsoMsg) == 0)
                        {
                            //Console.WriteLine("Signature Beda!!!");
                            LogWriter.showDEBUG(this, "Beda signature");
                            return false;
                        }

                        // set trx variable
                        //rC = jsonH["ResponseCode"].ToString();
                        //trxAmount = jsonH["Amount"].ToString();
                        //bit61 = jsonH["PrivateData2"].ToString();
                        //bit103 = jsonH["AccountIdentification2"].ToString();

                    }
                    else
                    {
                        myIsoMsg += jsonH["MESSAGE"].ToString();
                        dataSign = jsonH["SIGNATURE"].ToString();

                        // disini kudu aya pengecekan data signture na
                        if (rsa.checkServerDataSignature(dataSign, myIsoMsg) == 0)
                        {
                            //Console.WriteLine("Signature Beda!!!");
                            return false;
                        }
                        //Console.WriteLine("GAGAL RC = " + jsonH["RESPONSE_CODE"].ToString() + " " + jsonH["MESSAGE"].ToString());
                    }
                    //Console.WriteLine("Signature Sarua Cuy.");                    
                }
                catch //(Exception ex)
                {
                    return false;
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
