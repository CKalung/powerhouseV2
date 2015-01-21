using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace PhCrypto
{
	internal sealed class AESCryptography
	{
		private const string Salt = "d5fg4df5sg4ds5fg45sdfg4";
		private const int SizeOfBuffer = 1024*8;

		public AESCryptography ()
		{
		}

		/// <summary>
		/// Encrypt the specified clearText, key and iv.
		/// </summary>
		/// <param name="clearText">Clear text.</param>
		/// <param name="key">24 bytes Key.</param>
		/// <param name="iv">16 bytes iv.</param>
		public string Encrypt(string clearText, byte[] key, byte[] iv)
		{
			if (key.Length != 24)
				return "";
			if (iv.Length != 16)
				return "";
			byte[] clearBytes = Encoding.GetEncoding(1252).GetBytes(clearText);

			using (Aes encryptor = Aes.Create())
			{
				encryptor.Key = key;	//pdb.GetBytes(32);
				encryptor.IV = iv;		//pdb.GetBytes(16);
				using (MemoryStream ms = new MemoryStream())
				{
					using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
					{
						cs.Write(clearBytes, 0, clearBytes.Length);
						cs.Close();
					}
					clearText = Encoding.GetEncoding(1252).GetString(ms.ToArray());
				}
			}
			return clearText;
		}

		/// <summary>
		/// Decrypt the specified cipherText, key and iv.
		/// </summary>
		/// <param name="cipherText">Cipher text.</param>
		/// <param name="key">24 bytes Key.</param>
		/// <param name="iv">16 bytes iv.</param>
		public string Decrypt(string cipherText, byte[] key, byte[] iv)
		{
			if (key.Length != 24)
				return "";
			if (iv.Length != 16)
				return "";
			byte[] cipherBytes = Encoding.GetEncoding(1252).GetBytes(cipherText);
			//byte[] cipherBytes = Convert.FromBase64String(cipherText);
			using (Aes encryptor = Aes.Create())
			{
				encryptor.KeySize = 24 * 8;
				encryptor.BlockSize = 8;
				encryptor.Key = key;		//pdb.GetBytes(32);
				encryptor.IV = iv;			//pdb.GetBytes(16);
				using (MemoryStream ms = new MemoryStream())
				{
					using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
					{
						cs.Write(cipherBytes, 0, cipherBytes.Length);
						cs.Close();
					}
					cipherText = Encoding.GetEncoding(1252).GetString(ms.ToArray());
					//cipherText = Encoding.Unicode.GetString(ms.ToArray());
				}
			}
			return cipherText;
		}

		public string Encrypt(string clearText)
		{
			string EncryptionKey = "MAKV2SPBNI99212";
			byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
			using (Aes encryptor = Aes.Create())
			{
				Rfc2898DeriveBytes pdb = new 
				                         Rfc2898DeriveBytes(EncryptionKey, new byte[] 
					{ 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
				encryptor.Key = pdb.GetBytes(32);
				encryptor.IV = pdb.GetBytes(16);
				using (MemoryStream ms = new MemoryStream())
				{
					using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
					{
						cs.Write(clearBytes, 0, clearBytes.Length);
						cs.Close();
					}
					clearText = Convert.ToBase64String(ms.ToArray());
				}
			}
			return clearText;
		}

		public string Decrypt(string cipherText)
		{
			string EncryptionKey = "MAKV2SPBNI99212";
			byte[] cipherBytes = Convert.FromBase64String(cipherText);
			using (Aes encryptor = Aes.Create())
			{
				Rfc2898DeriveBytes pdb = new 
				                     Rfc2898DeriveBytes(EncryptionKey, new byte[] 
					{ 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
				encryptor.Key = pdb.GetBytes(32);
				encryptor.IV = pdb.GetBytes(16);
				using (MemoryStream ms = new MemoryStream())
				{
					using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
					{
						cs.Write(cipherBytes, 0, cipherBytes.Length);
						cs.Close();
					}
					cipherText = Encoding.Unicode.GetString(ms.ToArray());
				}
			}
			return cipherText;
		}

		internal byte[] EncryptStringToBytes(string plainText, byte[] key, byte[] iv)
		{
			// Check arguments.
			if (plainText == null || plainText.Length <= 0)
			{
				throw new ArgumentNullException("plainText");
			}
			if (key == null || key.Length <= 0)
			{
				throw new ArgumentNullException("key");
			}
			if (iv == null || iv.Length <= 0)
			{
				throw new ArgumentNullException("key");
			}

			byte[] encrypted;
			// Create an RijndaelManaged object
			// with the specified key and IV.
			using (var rijAlg = new RijndaelManaged())
			{
				rijAlg.Key = key;
				rijAlg.IV = iv;

				// Create a decrytor to perform the stream transform.
				ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

				// Create the streams used for encryption.
				using (var msEncrypt = new MemoryStream())
				{
					using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
					{
						using (var swEncrypt = new StreamWriter(csEncrypt))
						{
							//Write all data to the stream.
							swEncrypt.Write(plainText);
						}
						encrypted = msEncrypt.ToArray();
					}
				}
			}


			// Return the encrypted bytes from the memory stream.
			return encrypted;

		}

		internal string DecryptStringFromBytes(byte[] cipherText, byte[] key, byte[] iv)
		{
			// Check arguments.
			if (cipherText == null || cipherText.Length <= 0)
				throw new ArgumentNullException("cipherText");
			if (key == null || key.Length <= 0)
				throw new ArgumentNullException("key");
			if (iv == null || iv.Length <= 0)
				throw new ArgumentNullException("key");

			// Declare the string used to hold
			// the decrypted text.
			string plaintext;

			// Create an RijndaelManaged object
			// with the specified key and IV.
			using (var rijAlg = new RijndaelManaged())
			{
				rijAlg.Key = key;
				rijAlg.IV = iv;

				// Create a decrytor to perform the stream transform.
				ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

				// Create the streams used for decryption.
				using (var msDecrypt = new MemoryStream(cipherText))
				{
					using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
					{
						using (var srDecrypt = new StreamReader(csDecrypt))
						{
							// Read the decrypted bytes from the decrypting stream
							// and place them in a string.
							plaintext = srDecrypt.ReadToEnd();
						}
					}
				}

			}
			return plaintext;
		}

		internal void EncryptFile(string inputPath, string outputPath, string password)
		{
			var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
			var output = new FileStream(outputPath, FileMode.OpenOrCreate, FileAccess.Write);

			// Essentially, if you want to use RijndaelManaged as AES you need to make sure that:
			// 1.The block size is set to 128 bits
			// 2.You are not using CFB mode, or if you are the feedback size is also 128 bits

			var algorithm = new RijndaelManaged {KeySize = 256, BlockSize = 128};
			var key = new Rfc2898DeriveBytes(password, Encoding.ASCII.GetBytes(Salt));

			algorithm.Key = key.GetBytes(algorithm.KeySize/8);
			algorithm.IV = key.GetBytes(algorithm.BlockSize/8);

			using (var encryptedStream = new CryptoStream(output, algorithm.CreateEncryptor(), CryptoStreamMode.Write))
			{
				CopyStream(input, encryptedStream);
			}
		}

		internal bool DecryptFile(string inputPath, string outputPath, string password, ref string ErrMessage)
		{
			var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
			var output = new FileStream(outputPath, FileMode.OpenOrCreate, FileAccess.Write);
			ErrMessage = "";

			// Essentially, if you want to use RijndaelManaged as AES you need to make sure that:
			// 1.The block size is set to 128 bits
			// 2.You are not using CFB mode, or if you are the feedback size is also 128 bits
			var algorithm = new RijndaelManaged {KeySize = 256, BlockSize = 128};
			var key = new Rfc2898DeriveBytes(password, Encoding.ASCII.GetBytes(Salt));

			algorithm.Key = key.GetBytes(algorithm.KeySize/8);
			algorithm.IV = key.GetBytes(algorithm.BlockSize/8);

			try
			{
				using (var decryptedStream = new CryptoStream(output, algorithm.CreateDecryptor(), CryptoStreamMode.Write))
				{
					CopyStream(input, decryptedStream);
				}
				return true;
			}
			catch (CryptographicException)
			{
				ErrMessage ="Incorrect password";
				return false;
			}
			catch (Exception ex)
			{
				ErrMessage =ex.Message;
				return false;
			}
		}

		private void CopyStream(Stream input, Stream output)
		{
			using (output)
			using (input)
			{
				byte[] buffer = new byte[SizeOfBuffer];
				int read;
				while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
				{
					output.Write(buffer, 0, read);
				}
			}
		}
	}
}

