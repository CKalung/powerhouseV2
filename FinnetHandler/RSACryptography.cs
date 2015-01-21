using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace FinnetHandler
{
	class RSACryptography: IDisposable
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
		~RSACryptography()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			RSA.Dispose();
			conv.Dispose();
		}

        private Convertion conv = new Convertion();        
        public byte[] pModulus = null;
        public byte[] pExponent = new byte[3];
        public byte[] pD = null;
        private BigInteger _exponent = null;
        private BigInteger _modulus = null;
        private BigInteger _d = null;
        //public RandomNumberGenerator rGen;
        //public HashAlgorithm Hash;
        //public byte[] Encrypted;
        //public byte[] Decrtypted;

        private RSACryptoServiceProvider RSA;

        public RSACryptography(int rsaSize, byte[] _pModulus, byte[] _pExponent, byte[] _pD)
        {
            RSA = new RSACryptoServiceProvider(rsaSize);
            int len = (rsaSize / 8);
                                    
            // set rsa key
            pModulus = new byte[len];
            pExponent = new byte[3];
            pD = new byte[len];

            if (_pModulus != null)
            {
                Array.Copy(_pModulus, 0, pModulus, 0, len);
                _modulus = new BigInteger(pModulus); 
            }

            if (_pExponent != null)
            {
                Array.Copy(_pExponent, 0, pExponent, 0, 3);
                _exponent = new BigInteger(pExponent);
            }

            if (_pD != null)
            {
                Array.Copy(_pD, 0, pD, 0, len);
                _d = new BigInteger(pD);
            }         
        }

        public byte[] createServerDataSignature(string signParam)
        {
            byte[] baSignParam = System.Text.ASCIIEncoding.ASCII.GetBytes(signParam);
            
            // 1. create hash of data sign param
            byte[] hash = getHash(baSignParam);

            // 2. create signature, encrypt with public key client
            byte[] ret = PublicEncrypt_v15(hash);

            return ret;
        }

        public int checkServerDataSignature(string dataSign, string myIsoMsg)
        {
            byte[] baSign = conv.HexStringToByteArray(dataSign); //System.Text.ASCIIEncoding.ASCII.GetBytes(strSign);            
            byte[] baData = System.Text.ASCIIEncoding.ASCII.GetBytes(myIsoMsg);            

            // 1. Bikin Hash dari data iso
            byte[] hash1 = getHash(baData);

            // 2. decript signature dengan private key urang
            byte[] hash2 = PrivateDecrypt_v15(baSign);
            bool areEqual = hash1.SequenceEqual(hash2);
            if (!areEqual)
            {
                return 0;
            }

            return 1;
        }


        public byte[] getHash(byte[] data)
        {
            //byte[] data = HexStringToByteArray(datain);
            SHA1 sha256 = new SHA1Managed();
            byte[] result = sha256.ComputeHash(data);
            return result;
            //return BitConverter.ToString(result).Replace("-", "");//Convert.ToBase64String(resu?lt);
        }


        private byte[] PrivateEncryption(byte[] data)
        {

            // Converting the byte array data into a BigInteger instance
            BigInteger bnData = new BigInteger(data);

            // (bnData ^ D) % Modulus - This Encrypt the data using the private Exponent: D
            BigInteger encData = bnData.modPow(_d, _modulus);
            return encData.getBytes();
        }

        public byte[] PrivateEncrypt_v15(byte[] M)
        {
            try
            {
                RandomNumberGenerator rGen = System.Security.Cryptography.RandomNumberGenerator.Create();
                int size = RSA.KeySize / 8;
                if (M.Length > size - 11)
                    Console.WriteLine("Encrypt Private Key, message too long");
                    //throw new CryptographicException("message too long");

                int PSLength = System.Math.Max(8, (size - M.Length - 3));
                byte[] PS = new byte[PSLength];
                //rng.GetNonZeroBytes(PS);

                for (int i = 0; i < PS.Length; i++)
                    PS[i] = (byte)0xFF;

                byte[] EM = new byte[size];
                EM[1] = 0x01;

                Buffer.BlockCopy(PS, 0, EM, 2, PSLength);
                Buffer.BlockCopy(M, 0, EM, (size - M.Length), M.Length);

                byte[] m = OS2IP(EM);
                //byte[] c = //RSAEP(rsa, m);
                byte[] c = PrivateEncryption(m);
                byte[] C = I2OSP(c, size);
                return C;
            }
            catch
            {
                //MessageBox.Show("ENCRYPT V 1.5 ERROR");
                Console.WriteLine("ENCRYPT Private Key V 1.5 ERROR");
                return null;
            }
        }

        private byte[] PublicEncryption(byte[] data)
        {
            // Converting the byte array data into a BigInteger instance
            BigInteger bnData = new BigInteger(data);

            // (bnData ^ Exponent) % Modulus - This Encrypt the data using the public Exponent
            BigInteger encData = bnData.modPow(_exponent, _modulus);
            return encData.getBytes();
        }

        public byte[] PublicEncrypt_v15(byte[] M)
        {
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            try
            {
                int size = RSA.KeySize / 8;
                if (M.Length > size - 11)
                    Console.WriteLine("Public Encrypt Key, message too long");
                    //throw new CryptographicException("message too long");
                int PSLength = System.Math.Max(8, (size - M.Length - 3));
                byte[] PS = new byte[PSLength];
                rng.GetNonZeroBytes(PS);

                //for (int i = 0; i < PS.Length; i++)
                //    PS[i] = (byte)0xFF;

                byte[] EM = new byte[size];
                EM[1] = 0x02;

                Buffer.BlockCopy(PS, 0, EM, 2, PSLength);
                Buffer.BlockCopy(M, 0, EM, (size - M.Length), M.Length);

                byte[] m = OS2IP(EM);
                //byte[] c = //RSAEP(rsa, m);
                byte[] c = PublicDecryption(m);//PrivateEncryption(m);
                byte[] C = I2OSP(c, size);
                return C;
            }
            catch
            {
                //MessageBox.Show("ENCRYPT V 1.5 ERROR");
                Console.WriteLine("ENCRYPT With Public Key V 1.5 ERROR");
                return null;
            }
        }

        private byte[] PrivateDecryption(byte[] encryptedData)
        {
            // Converting the encrypted data byte array data into a BigInteger instance
            BigInteger encData = new BigInteger(encryptedData);

            // (encData ^ D) % Modulus - This Decrypt the data using the private Exponent: D
            BigInteger bnData = encData.modPow(_d, _modulus);
            return bnData.getBytes();
        }
        
        public byte[] PrivateDecrypt_v15(byte[] C) 
        {
            try
            {
                int size = RSA.KeySize >> 3; // div by 8
                if ((size < 11) || (C.Length > size))
                    Console.WriteLine("DECRTYP WITH PRIVATE KEY V 1.5 ERROR");
                    //throw new CryptographicException("decryption error");

                byte[] c = OS2IP(C);
                byte[] m = PrivateDecryption(c);//PublicDecryption(c);//RSADP(rsa, c);
                byte[] EM = I2OSP(m, size);

                if ((EM[0] != 0x00) || (EM[1] != 0x02))
                    return null;

                int mPos = 10;
                // PS is a minimum of 8 bytes + 2 bytes for header
                while ((EM[mPos] != 0x00) && (mPos < EM.Length))
                    mPos++;
                if (EM[mPos] != 0x00)
                    return null;
                mPos++;
                byte[] M = new byte[EM.Length - mPos];
                Buffer.BlockCopy(EM, mPos, M, 0, M.Length);
                return M;
            }
            catch
            {
                //MessageBox.Show("DECRTYP V 1.5 ERROR");
                Console.WriteLine("DECRTYP WITH PRIVATE KEY V 1.5 ERROR");
                return null;
            }
        }        
       
        private byte[] PublicDecryption(byte[] encryptedData)
        {
            // Converting the encrypted data byte array data into a BigInteger instance
            BigInteger encData = new BigInteger(encryptedData);

            // (encData ^ Exponent) % Modulus - This Decrypt the data using the p
            BigInteger bnData = encData.modPow(_exponent, _modulus);
            return bnData.getBytes();
        }
        
        public byte[] PublicDecrypt_v15(byte[] C)
        {
            try
            {
                int size = RSA.KeySize >> 3; // div by 8
                if ((size < 11) || (C.Length > size))
                    Console.WriteLine("DECRTYP WITH PUBLIC KEY V 1.5 ERROR");
                    //throw new CryptographicException("decryption error");

                byte[] c = OS2IP(C);
                byte[] m = PublicDecryption(c);//RSADP(rsa, c);
                byte[] EM = I2OSP(m, size);

                if ((EM[0] != 0x00) || (EM[1] != 0x01))
                    return null;

                int mPos = 10;
                // PS is a minimum of 8 bytes + 2 bytes for header
                while ((EM[mPos] != 0x00) && (mPos < EM.Length))
                    mPos++;
                if (EM[mPos] != 0x00)
                    return null;
                mPos++;
                byte[] M = new byte[EM.Length - mPos];
                Buffer.BlockCopy(EM, mPos, M, 0, M.Length);

                return M;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + " DECRTYP V 1.5 ERROR");
                return null;
            }
        }

        private byte[] OS2IP(byte[] x)
        {
            int i = 0;
            while ((x[i++] == 0x00) && (i < x.Length))
            {
                //confuse compiler into reporting a warning with {}
            }
            i--;
            if (i > 0)
            {
                byte[] result = new byte[x.Length - i];
                Buffer.BlockCopy(x, i, result, 0, result.Length);
                return result;
            }
            else
                return x;
        }

        private byte[] I2OSP(byte[] x, int size)
        {
            byte[] result = new byte[size];
            Buffer.BlockCopy(x, 0, result, (result.Length - x.Length), x.Length);
            return result;
        }

        private byte[] I2OSP(int x, int size)
        {
            byte[] array = BitConverter.GetBytes(x);
            Array.Reverse(array, 0, array.Length);
            return I2OSP(array, size);
        }
    }
}
