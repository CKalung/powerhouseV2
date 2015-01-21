using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace FMHandler
{
    class Convertion: IDisposable
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
        ~Convertion()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
        }

        public Convertion() 
        {
 
        }

        public string StringToBinary(string data)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in data.ToCharArray())
            {
                sb.Append(Convert.ToString(c, 2).PadLeft(8, '0'));
            }
            return sb.ToString();
        }

        public string BinaryToString(string data)
        {
            List<Byte> byteList = new List<Byte>();

            for (int i = 0; i < data.Length; i += 8)
            {
                byteList.Add(Convert.ToByte(data.Substring(i, 8), 2));
            }
            return Encoding.ASCII.GetString(byteList.ToArray());
        }

        public string IntToHex(int value)
        {
            return String.Format("0x{0:X}", value);
        }

        //public int HexToInt(string value)
        //{
        //    // strip the leading 0x
        //    if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        //    {
        //        value = value.Substring(2);
        //    }
        //    return Int32.Parse(value, NumberStyles.HexNumber);
        //}

        public byte[] StringToBytes(string data)
        {
            byte[] hsl = new byte[data.Length / 2];
            int j = 0;
            for (int i = 0; i < data.Length; i += 2)
            {
                hsl[j] = byte.Parse(data.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
                j++;
            }
            return hsl;
        }

        public byte[] stringToBytes(string data)
        {
            byte[] hsl = new byte[data.Length / 2];
            int j = 0;
            for (int i = 0; i < data.Length; i += 2)
            {
                hsl[j] = byte.Parse(data.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
                j++;
            }
            return hsl;
        }

        public string byteArrayToString(byte[] data)
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

        public long ConvertBytesToBCD(byte[] bcdNumber)
        {
            long result = 0;
            foreach (byte b in bcdNumber)
            {
                int digit1 = b >> 4;
                int digit2 = b & 0x0f;
                result = (result * 100) + digit1 * 10 + digit2;
            }
            //Console.WriteLine("{0}", result); //12345678            
            return result;
        }

        public byte[] ToBcd(int value, int len)
        {
            if (value < 0 || value > 99999999)
                throw new ArgumentOutOfRangeException("value");
            byte[] ret = new byte[len];
            for (int i = 0; i < len; i++)
            {
                ret[i] = (byte)(value % 10);
                value /= 10;
                ret[i] |= (byte)((value % 10) << 4);
                value /= 10;
            }
            return ret;
        }

        public string HexAsciiConvert(string hex)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i <= hex.Length - 2; i += 2)
            {
                sb.Append(Convert.ToString(Convert.ToChar(Int32.Parse(hex.Substring(i, 2),
                System.Globalization.NumberStyles.HexNumber))));
            }
            return sb.ToString();
        }

        public byte[] IntToBCD(int input)
        {
            if (input > 9999 || input < 0)
                throw new ArgumentOutOfRangeException("input");

            int thousands = input / 1000;
            int hundreds = (input -= thousands * 1000) / 100;
            int tens = (input -= hundreds * 100) / 10;
            int ones = (input -= tens * 10);

            byte[] bcd = new byte[] {
        (byte)(thousands << 4 | hundreds),
        (byte)(tens << 4 | ones)
    };

            return bcd;
        }

        public byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        // Convert a byte array to an Object
        public Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);
            return obj;
        }

        public static string toHexstring(byte[] ba)
        {
            StringBuilder Result = new StringBuilder();
            string HexAlphabet = "0123456789ABCDEF";

            foreach (byte B in ba)
            {
                Result.Append(HexAlphabet[(int)(B >> 4)]);
                Result.Append(HexAlphabet[(int)(B & 0xF)]);
            }

            return Result.ToString();
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
                hexOutput += string.Format("{0:X}", value);
                //else
                //    hexOutput += " " + string.Format("{0:X}", value);
                //Console.WriteLine("Hexadecimal value of {0} is {1}", letter, hexOutput);
                i++;
            }
            return hexOutput;

        }


        public string hexTostring(string hexValues)
        {
            //hexValues = "48 65 6C 6C 6F 20 57 6F 72 6C 64 21";
            string[] hexValuesSplit = hexValues.Split('-');
            string stringValue = "";
            foreach (string hex in hexValuesSplit)
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

        
        public string ByteArrayToHexString(byte[] Bytes)
        {
            StringBuilder Result = new StringBuilder();
            string HexAlphabet = "0123456789ABCDEF";

            foreach (byte B in Bytes)
            {
                Result.Append(HexAlphabet[(int)(B >> 4)]);
                Result.Append(HexAlphabet[(int)(B & 0xF)]);
            }

            return Result.ToString();
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
    }
}
