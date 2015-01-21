using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace SSLtest
{
    class Cryptograph: IDisposable
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
        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
        }
        ~Cryptograph()
        {
            this.Dispose(false);
        }
        #endregion

        public Cryptograph()
        {

        }
       
        public byte[] ToByteArray(string input)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(input);
        }

        public string stringToHex(string input)
        {
            char[] values = input.ToCharArray();
            string hexOutput = "";
            int i = 0;
            foreach (char letter in values)
            {
                // Get the integral value of the character.
                int value = Convert.ToInt32(letter);
                // Convert the decimal value to a hexadecimal value in string form.
                //if (i == 0)
                hexOutput += String.Format("{0:X}", value);
                //else
                //    hexOutput += " " + String.Format("{0:X}", value);
                //Console.WriteLine("Hexadecimal value of {0} is {1}", letter, hexOutput);
                i++;
            }
            return hexOutput;
        }

        public string HexToString(string hexValues)
        {
            //hexValues = "48 65 6C 6C 6F 20 57 6F 72 6C 64 21";
            string[] hexValuesSplit = hexValues.Split('-');
            string stringValue = "";
            foreach (String hex in hexValuesSplit)
            {
                // Convert the number expressed in base-16 to an integer.
                int value = Convert.ToInt32(hex, 16);
                // Get the character corresponding to the integral value.
                stringValue += Char.ConvertFromUtf32(value);
                char charValue = (char)value;
                //Console.WriteLine("hexadecimal value = {0}, int value = {1}, char value = {2} or {3}",hex, value, stringValue, charValue);
            }
            return stringValue;

        }

        public byte[] HexStringToByteArray(string Hex)
        {
            byte[] Bytes = new byte[Hex.Length / 2];
            int[] HexValue = new int[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x0B, 0x0C, 0x0D,
                                 0x0E, 0x0F };

            for (int x = 0, i = 0; i < Hex.Length; i += 2, x += 1)
            {
                Bytes[x] = (byte)(HexValue[Char.ToUpper(Hex[i + 0]) - '0'] << 4 |
                                  HexValue[Char.ToUpper(Hex[i + 1]) - '0']);
            }

            return Bytes;
        }

        public string ByteArrayToString(byte[] data)
        {
            StringBuilder sDataOut;

            if (data != null)
            {
                sDataOut = new StringBuilder(data.Length * 2);
                for (int nI = 0; nI < data.Length; nI++)
                    sDataOut.AppendFormat("{0:X02}", data[nI]);
            }
            else
                sDataOut = new StringBuilder();
            
            return sDataOut.ToString();
        }

        public string getMac(string input, string key)
        {            
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            byte[] password = encoding.GetBytes(key);

            HMACSHA1 hmacsha1 = new HMACSHA1(password);
            byte[] hash = hmacsha1.ComputeHash(encoding.GetBytes(input));

            //Console.WriteLine("==== Signature-Hex ====");
            //Console.WriteLine(ByteArrayToString(hash) + "\n");

            String hashBase64 = Convert.ToBase64String(hash);            
            return hashBase64;
        }
            

        public byte[] getHash(string datain)
        {
            //byte[] data = HexStringToByteArray(datain); kalau inputan nya hex string

            // kalau inputanya asscii
            byte[] data = new byte[datain.Length];
            int i;
            for (i = 0; i < datain.Length; i++)
            {
                data[i] = Convert.ToByte(datain[i]);
            }            
           
            SHA1 sha256 = new SHA1Managed();
            byte[] result = sha256.ComputeHash(data);
            return result;
        }

        public string getMd5(string datain)
        {
            //byte[] data = HexStringToByteArray(datain); kalau inputan nya hex string

            // kalau inputanya asscii
            byte[] data = new byte[datain.Length];
            int i;
            for (i = 0; i < datain.Length; i++)
            {
                data[i] = Convert.ToByte(datain[i]);
            }

            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(data);
            return ByteArrayToString(result);            
        }        
    }
}
