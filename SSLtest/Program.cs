using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;


namespace SSLtest
{
    class Program
    {
		private static TcpClient client;
		private static NetworkStream clientStream= null;
		private static SslStream sslClientStream;
		private static string rest_method = "POST";

		private static RestService rs = new RestService();
		private static string SandraHost = "123.231.225.20";
		private static int SandraPort = 7080;


//		private static string uriHost = "";
//		private static int uriPort = 0;
//		private static string uriLocalPath = "";
//		private static string authParam = "";
//		//private string contentParam = "DA01 dummy-01:";
//		private static string httpDate = "";
//
//		private static string requestHeaders = "";
//		private static string requestMethod = "";

		static void Main3(string[] args)
		{
			rs.httpUri = "http://" + SandraHost + ":" + SandraPort.ToString () + "/hms/rest";
			rs.canonicalPath = "/requestToken";
			rs.authID = "DA01";
			rs.method = "POST";
			rs.contentType = "application/json";
			rs.userAuth = "dam";
			rs.secretKey = "21R5MF6X4E6IVLMJMDE6869XH87BGQ01OGWLI178ZZ568TBP6W53MT6C9QE0M930";
			rs.bodyMessage = "{\"sourceHostId\":\"switching-gateway-service\"}";

			//string res = rs.TCPRestSendRequest(120);
			string res = rs.HttpRestSendRequest (120); // timeout dalam 12 detik

			if ((res == "") && ((rs.Response.httpMessage == null) || (rs.Response.httpCode == 0))) {
				Console.WriteLine ("No response from Sandra Host");
				return ;
			}

		}

		static void Main2a(string[] args)
		{
			Stream stream;
//			ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
		WebClient wbclient = new WebClient ();
//			wbclient.Headers.Add(htt
			try
			{
				Uri uri = new Uri("https://www.facebook.com");
//				Uri uri = new Uri("http://www.detik.com");

				WebRequest http = HttpWebRequest.Create(uri);
				HttpWebResponse response = (HttpWebResponse)http.GetResponse();
				stream = response.GetResponseStream();
			}
			catch(UriFormatException e)
			{
				Console.WriteLine("Invalid URL");
				return;
			}
			catch(IOException e)
			{
				Console.WriteLine("Could not connect to URL");
				return;
			}
			string request = "GET / HTTP/1.1";

			Byte[] data = System.Text.Encoding.UTF8.GetBytes(request + "\r\n");

			stream.Write (data,0,data.Length);
			stream.Flush ();

			byte[] bytes = new byte[1024];

			//System.Threading.Thread.Sleep (1000);
			// Display the response
			int bytesRead = stream.Read(bytes, 0, bytes.Length);

			string serverResponse = Encoding.ASCII.GetString(bytes, 0, bytesRead);
			Console.WriteLine("Server said: " + serverResponse);

			Console.ReadLine();

			// Close everything.
			stream.Close();
		}

		static void Main2(string[] args)
		{
			client = new System.Net.Sockets.TcpClient("www.facebook.com", 443);
			//string requestMethod = rest_method + " " + "www.facebook.com/" + " HTTP/1.1\r\n";
			clientStream = client.GetStream();

			sslClientStream = new SslStream(clientStream, false, 
			new RemoteCertificateValidationCallback (ValidateServerCertificate), 
			null);
			sslClientStream = new SslStream(clientStream, false, 
			                                new RemoteCertificateValidationCallback (IgnoreCertificateErrorsCallback), 
			                                null);

			try 
			{
				//sslStream.AuthenticateAsClient(serverName);
				sslClientStream.AuthenticateAsClient("www.facebook.com");
			} 
			catch (AuthenticationException e)
			{
				Console.WriteLine("Exception: {0}", e.Message);
				if (e.InnerException != null)
				{
					Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
				}
				Console.WriteLine ("Authentication failed - closing the connection.");
				client.Close();
				return;
			}

			// GET http://www.myfavoritewebsite.com:8080/chatware/chatroom.php HTTP/1.1
			// GET /chatware/chatroom.php HTTP/1.1

			string request = "GET / HTTP/1.1";

			Byte[] data = System.Text.Encoding.UTF8.GetBytes(request + "\r\n");

			sslClientStream.Write (data);
			sslClientStream.Flush ();

			byte[] bytes = new byte[1024];

			//System.Threading.Thread.Sleep (1000);
			// Display the response
			int bytesRead = sslClientStream.Read(bytes, 0, bytes.Length);

			string serverResponse = Encoding.ASCII.GetString(bytes, 0, bytesRead);
			Console.WriteLine("Server said: " + serverResponse);

			Console.ReadLine();

			// Close everything.
			sslClientStream.Close();
			clientStream.Close ();
			client.Close();

		}

		public static bool ValidateServerCertificate(
			object sender,
			X509Certificate certificate,
			X509Chain chain,
			SslPolicyErrors sslPolicyErrors)
		{
//			return true;

			if (sslPolicyErrors == SslPolicyErrors.None)
				return true;

			Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

			// Do not allow this client to communicate with unauthenticated servers. 
			return false;
		}

        static void Main(string[] args)
        {
            SecureTcpServer server = null;
            SecureTcpClient client = null;

            try
            {
                int port = 8889;

                RemoteCertificateValidationCallback certValidationCallback = null;
                certValidationCallback = new RemoteCertificateValidationCallback(IgnoreCertificateErrorsCallback);

                string certPath = System.Reflection.Assembly.GetEntryAssembly().Location;
				//certPath = Path.GetDirectoryName(certPath);
				certPath = "/etc/ssl/certs";
                //certPath = Path.Combine(certPath, "server.crt");
                certPath = Path.Combine(certPath, "qioskuserver.crt");
                
                Console.WriteLine("Loading Server Cert From: " + certPath);
				X509Certificate serverCert = X509Certificate.CreateFromCertFile(certPath);
				//X509Certificate2 serverCert = X509Certificate2.;
                Console.WriteLine("Sertifikat sudah loaded");

                server = new SecureTcpServer(port, serverCert,
                    new SecureConnectionResultsCallback(OnServerConnectionAvailable));

                server.StartListening();

                Console.WriteLine("Server siap");

                client = new SecureTcpClient(new SecureConnectionResultsCallback(OnClientConnectionAvailable),
                    certValidationCallback);

                client.StartConnecting("localhost", new IPEndPoint(IPAddress.Loopback, port));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            //sleep to avoid printing this text until after the callbacks have been invoked.
            Thread.Sleep(4000);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();

            if (server != null)
                server.Dispose();
            if (client != null)
                client.Dispose();

        }

        static void OnServerConnectionAvailable(object sender, SecureConnectionResults args)
        {
            Console.WriteLine("Asup kadieu");
            
            if (args.AsyncException != null)
            {
                Console.WriteLine(args.AsyncException);
                return;
            }

            SslStream stream = args.SecureStream;

            
            Console.WriteLine("Server Connection secured: " + stream.IsAuthenticated);



            StreamWriter writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            writer.WriteLine("Hello from server!");

            StreamReader reader = new StreamReader(stream);
            string line = reader.ReadLine();
            Console.WriteLine("Server Recieved: '{0}'", line == null ? "<NULL>" : line);

            writer.Close();
            reader.Close();
            stream.Close();
        }

        static void OnClientConnectionAvailable(object sender, SecureConnectionResults args)
        {
            if (args.AsyncException != null)
            {
                Console.WriteLine(args.AsyncException);
                return;
            }
            SslStream stream = args.SecureStream;

            Console.WriteLine("Client Connection secured: " + stream.IsAuthenticated);

            StreamWriter writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            writer.WriteLine("Hello from client!");

            StreamReader reader = new StreamReader(stream);
            string line = reader.ReadLine();
            Console.WriteLine("Client Recieved: '{0}'", line == null ? "<NULL>" : line);

            writer.Close();
            reader.Close();
            stream.Close();
        }

        static bool IgnoreCertificateErrorsCallback(object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors != SslPolicyErrors.None)
            {

                Console.WriteLine("IgnoreCertificateErrorsCallback: {0}", sslPolicyErrors);
                //you should implement different logic here...

                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
                {
                    foreach (X509ChainStatus chainStatus in chain.ChainStatus)
                    {
                        Console.WriteLine("\t" + chainStatus.Status);
                    }
                }
            }

            //returning true tells the SslStream object you don't care about any errors.
            return true;
        }
    }
}
