#define USE_FILE_BASED_CERTIFICATE

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;
using System.IO;
using System.Threading;

/*
 * Simple SSL command line chat. Its purpose is to show how to setup client/server
 * connection using Secure Socket Layer connection with the SslStream class.
 *
 * To run it you need to create proper certificates, good start is here:
 *  http://blogs.technet.com/jhoward/archive/2005/02/02/365323.aspx
 *
 * Below you will find a crash course on proper certificate setup for
 * development purposes (it was created with the use of above great blog entry):
 *
 * You will need at least two certificates, the so called root certificate
 * (normally those certificates possess organizations like Verisign or Thawte),
 * next you create with it your own certificate that will be used to establish
 * SSL connection. So to create those certificates
 *
 * First call this (use "Open Visual Studio 2005 Command Prompt" to make sure all
 * paths are ok):
 * $ makecert -pe
 *            -n "CN=Test And Dev Root Authority"
 *            -ss my
 *            -sr LocalMachine
 *            -a sha1
 *            -sky signature
 *            -r "Test And Dev Root Authority.cer"
 *
 * Then call this:
 * $ makecert -pe
 *            -n "CN=MachineName"
 *            -ss my
 *            -sr LocalMachine
 *            -a sha1
 *            -sky exchange
 *            -eku 1.3.6.1.5.5.7.3.1
 *            -in "Test And Dev Root Authority"
 *            -is MY
 *            -ir LocalMachine
 *            -sp "Microsoft RSA SChannel Cryptographic Provider"
 *            -sy 12
 *            MachineName.cer
 *
 * Replace MachineName with your machine Full Computer Name (you may use what
 * Environment.MachineName returns).
 *
 * Then open mmc console and look into cerificates extension for your
 * Local Machine, copy "Test And Dev Root Authority" to Trusted Authorities
 * folder. This step is important. Remember that you will have to install
 * 'Test And Dev Root Authority.cer' in Trusted Authorities folder
 * certificate on every computer where you will want to run client. And
 * additionally 'MachineName.cer' on any machine where server will be run.
 *
 * Some of the errors I had to deal with:
 * RemoteCertificateNameMismatch - this was happening due to me writing bad
 *    server name on client side in call to AuthenticateAsClient(serverName).
 *    This name (XXXXX) should be whatever you put in above makecert
 *    command : "CN=XXXXX".
 *
 * RemoteCertificateChainErrors - this error was due to the missing root
 *    certificate, that was used to create sub certificate.
 */

namespace SSLtest
{
    class YangUdahJalan
    {
    }

	// State object for receiving data from remote device.
	public class SslStateObject
	{
		public SslStream workSslStream = null;                // Stream socket.
		public const int BufferSize = 4096;             // Size of receive buffer.
		public byte[] buffer = new byte[BufferSize];    // Receive buffer.
		//public StringBuilder sb = new StringBuilder();  // Received data string.
	}

	class SslMainTest
    {

		const int TIMEOUT_07 = 7;
		const int TIMEOUT_10 = 10;
		const int TIMEOUT_15 = 15;
		const int TIMEOUT_60 = 60;

		//static int bytesRead = 0;
		static string srecBuff = "";
		const int MAXRecBuff = 100 * 1024;  // max 100Kb
		//        byte[] dataBuffer = new byte[2048];
		static int dataLength = 0;

#if (USE_FILE_BASED_CERTIFICATE)
    static X509Certificate2 certFile = null;
#else
        static X509Certificate2 certFile = null;
#endif

        //
        // Server code

        public static void StartAsServer()
        {
#if (USE_FILE_BASED_CERTIFICATE)
			/*
			 * SUPAYA bisa digunakan di mono linux, 
			 * setelah generate sertifikat file .cer/.crt, .csr, .key, .key.org, .key.secure, 
			 * kita butuh generate sertifikat yang didalamnya ada privatekey juga. tapi terencrypt dengan password
			 * yang mempunyai extension .pfx / pkcs#12
			 * Gunakan perintah (jika nama filenya public key "server.crt" dan private key "server.key"):
			 * 		$ openssl pkcs12 -export -in server.crt -inkey server.key -out mycert.pfx
			 * isi passwordnya.
			 * Baru pasang di koding pake: 
			 * 		X509Certificate2 cert = new X509Certificate2("server.pfx","password");
			*/


			//			string cfile = Environment.MachineName + ".cer";
			//string cfile = "powerhouse.crt";
			//string cfile = "server.crt";
			string cfile = "server.pfx";
			//string certFilePath = @"C:\Projects\536\Samples\SSLSample\" + cfile;
			string certFilePath = @"/home/kusumah/Projects/PowerHouse/PPOB-Gate/SSLtest/bin/Debug/SSLkeys/" + cfile;
			//string certFilePath = @"/etc/ssl/certs/" + cfile;
      if (!File.Exists(certFilePath))
        certFilePath = cfile;
      certFile = new X509Certificate2(certFilePath, "d4mpt");
#else
            X509Store store = new X509Store(StoreName.My,
              StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
			X509Certificate2Collection certs2 =
				store.Certificates;
			X509Certificate2Collection certs =
				store.Certificates.Find(X509FindType.FindBySubjectName,
					Environment.MachineName, false);
            certFile = certs[0];
            store.Close();
#endif
            Console.WriteLine("Certificate subject: {0}", certFile.Subject);
            TcpListener listener = new TcpListener(IPAddress.Any, 8088);
            listener.Start();
            System.Console.WriteLine(
              "Waiting for client on port 8088... (use Ctrl+C to stop)");
            TcpClient tcpClient = listener.AcceptTcpClient();
            ProcessClient(tcpClient);
			Console.ReadLine ();
        }

		private static void disconnect(){
			try{
				sslstr.Close();
			} catch{
			}
			try{
				clienttr.Close();
			} catch{
			}
		}

		private static void readyReceiveRetriggerSSL(SslStateObject state, int recTO)
		{
			if (state.workSslStream != null)
			{
				try
				{
					//state.sb.Clear();

					// Begin receiving the data from the remote device.
					state.workSslStream.BeginRead(state.buffer, 0, state.buffer.Length,
						new AsyncCallback(ReadCallbackSSL), state);
					//ctrTO = recTO;
				}
				catch	// (Exception e)
				{
					//Console.WriteLine(e.StackTrace);
					Console.WriteLine ("Socket already disconnected.");
					disconnect();   // pastikan dan siapkan class untuk re-use
				}
			}
		}

		static void ReadCallbackSSL(IAsyncResult ar)
		{
			// Read the  message sent by the server. 
			//ctrTO = TIMEOUT_60;      // reset disconnect TIMEOUT
//			ctrTO = TIMEOUT_07;        // reset disconnect TIMEOUT
			SslStateObject state = null;
			SslStream sslStream = null;
			int byteCount = -1;
			try
			{
				state = (SslStateObject)ar.AsyncState;
				//sslStream = state.workSslStream;
				sslStream = sslstr;
				if (sslStream == null) return;

//				Console.WriteLine ("Reading data from the server.");
				byteCount = sslStream.EndRead(ar);
				Console.WriteLine (byteCount.ToString () + " bytes read.");
			}
			catch(Exception ex)
			{
				Console.WriteLine ("SSL Disconnected : " + ex.Message);
				disconnect();
				return;
			}

			try
			{
				if (byteCount > 0)
				{
					// There might be more data, so store the data received so far.
					//state.sb.Append(Encoding.GetEncoding(1252).GetString(state.buffer, 0, bytesRead));
					byte[] data2 = new byte[byteCount];
					Array.Copy(state.buffer, 0, data2, 0, byteCount);
					//                        Console.WriteLine(state.sb.ToString();
					DataReceivedSSL(state.workSslStream, data2);
				}
				else
				{
					// disconnected by client
					//state.sb.Clear();
					//Console.WriteLine ("Session ID : " + SessID + ", Disconnected by remote client");
					disconnect();
					return;
				}
				Thread.Sleep(10);
				readyReceiveRetriggerSSL(state, TIMEOUT_07);
			}
			catch (Exception e)
			{
				Console.WriteLine ("Client has disconnected " + e.StackTrace);
				disconnect();
			}

		}

		private static void DataReceivedSSL(SslStream clientStream, byte[] data)
		{
			srecBuff += Encoding.GetEncoding(1252).GetString(data);
			dataLength += data.Length;

			Console.WriteLine ("Terima data: " + srecBuff);

			srecBuff = "";
			dataLength = 0;
			disconnect();
		}

		static SslStream sslstr = null;
		static TcpClient clienttr = null;

		public static void ProcessClient(TcpClient client)
        {
			Console.WriteLine ("Koneksi masuk...");
            SslStream ssls = new SslStream(client.GetStream(), false);

			sslstr = ssls;
			clienttr = client;

            try
            {
				SslStateObject state = new SslStateObject();
				state.workSslStream = ssls;

//				ssls.BeginRead(state.buffer, 0, state.buffer.Length,
//					new AsyncCallback(ReadCallbackSSL), state);
//
                ssls.AuthenticateAsServer(certFile, false,
					SslProtocols.Default, true);

				ssls.BeginRead(state.buffer, 0, state.buffer.Length,
					new AsyncCallback(ReadCallbackSSL), state);

				Console.WriteLine ("Otentikasi beres...");
//                ssls.ReadTimeout = 500000;
 //               ssls.WriteTimeout = 500000;
//                StartReaderSenderThreads(ssls);
            }
            catch (AuthenticationException ex)
            {
                System.Console.WriteLine(ex.Message);
                System.Console.WriteLine(ex.InnerException.Message);
				disconnect ();
            }
            finally
            {
//                ssls.Close();
//                client.Close();
            }
        }

        //
        // Client code

        // The following method is invoked by the
        //  RemoteCertificateValidationDelegate.
        public static bool ValidateServerCertificate(object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
			Console.WriteLine ("==== ASUP VALIDATION Certificate");
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

			if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateNotAvailable)
				return false;

			string msg = "";
			bool accept = true;
			if ((sslPolicyErrors &
				SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.RemoteCertificateChainErrors)
			{
				foreach (X509ChainStatus item in chain.ChainStatus)
				{
					msg += " 1 " + item.Status;
					if (item.Status != X509ChainStatusFlags.RevocationStatusUnknown &&
						item.Status != X509ChainStatusFlags.OfflineRevocation)
						break;

					if (item.Status != X509ChainStatusFlags.NoError)
					{
						msg += "\r\n    -" + item.StatusInformation;
						accept = false;
					}
				}
			}
			Console.WriteLine (msg + ": Accept = " + accept);

			return accept;
			//return true;
            // Do not allow this client to communicate with unauthenticated servers.
			//return false;
        }

        public static void StartAsClient(String server, string serverName, int port)
        {
            TcpClient client = new TcpClient(server, port);
            Console.WriteLine("Client connected..");
            SslStream ssls = new SslStream(client.GetStream(), false,
              new RemoteCertificateValidationCallback(ValidateServerCertificate),
              null);
            try
            {
                ssls.AuthenticateAsClient(serverName);
                StartReaderSenderThreads(ssls);
            }
            catch (AuthenticationException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.InnerException.Message);
            }
            finally
            {
                //ssls.Close();
                client.Close();
            }
        }

        //
        // Common methods

        static Thread readerThread = null;
        static Thread senderThread = null;
        public static void ReaderThread(Object str)
        {
            SslStream ssls = str as SslStream;
            while (true)
            {
                string message = ReadMessage(ssls);
				if (message.Length == 0)
					break;
                System.Console.WriteLine("Remote: " + message);
                if (message.ToLower().Equals("\\quit"))
                {
                    ssls.Write(Encoding.UTF8.GetBytes("\\quit<EOF>"));
                    ssls.Flush();
                    break;
                }
            }
        }

        public static void SenderThread(Object str)
        {
            SslStream ssls = str as SslStream;
            while (true)
            {
                string message = Console.ReadLine();
                if (readerThread.IsAlive)
                {
                    ssls.Write(Encoding.UTF8.GetBytes(message + "<EOF>"));
                    ssls.Flush();
                }
                if (message.ToLower().Equals("\\quit"))
                    break;
            }
        }

        public static void StartReaderSenderThreads(SslStream ssls)
        {
            ParameterizedThreadStart readerStarter =
              new ParameterizedThreadStart(ReaderThread);
            ParameterizedThreadStart senderStarter =
              new ParameterizedThreadStart(SenderThread);
            readerThread = new Thread(readerStarter);
            senderThread = new Thread(senderStarter);
            readerThread.Start(ssls);
            senderThread.Start(ssls);
            readerThread.Join();
            senderThread.Join();
        }

        public static string ReadMessage(SslStream stream)
        {
            byte[] buffer = new byte[2048];
            StringBuilder sb = new StringBuilder();
            int bytes = -1;
            do
            {
                bytes = stream.Read(buffer, 0, buffer.Length);
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[bytes];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                sb.Append(chars);
                if (sb.ToString().IndexOf("<EOF>") != -1)
                {
                    sb.Replace("<EOF>", "");
                    break;
                }
            } while (bytes > 0);

            return sb.ToString();
        }

        //
        // Main method

        static void Main(string[] args)
        {
//			StartAsServer();
//			return;

            if (args.Length != 4 && args.Length != 1 ||
                  args[0].ToLower().Equals("-h"))
            {
                Console.WriteLine(
                 "Required format:\n" +
                 "  [-h|-c|-s] [ServerName] [ServerMachine] [ServerPort]\n" +
                 "  Use -h to display this help screen\n" +
                 "  Use -s with no arguments to start as a server\n" +
                 "  Use -c with ServerName, ServerMachine and ServerPort arguments\n" +
                 "   to start as a client\n\n" +
                 "  For example: \n\n" +
                 "  C:\\SSLChat.exe -s\n" +
                 "    will start a server\n\n" +
                 "  C:\\SSLChat.exe -c localhost MachineName 8088\n" +
                 "    will start a client, and connect to server\n" +
                 "    located on localhost whose name is MachineName and\n" +
                 "    which is listening on port 8088\n\n" +
                 "  Use \\quit on command line to exit session\n");
                Environment.Exit(-1);
            }
            if (args[0].ToLower().Equals("-c"))
                StartAsClient(args[1], args[2], Convert.ToInt32(args[3]));
            if (args[0].ToLower().Equals("-s"))
                StartAsServer();
        }
    }
}