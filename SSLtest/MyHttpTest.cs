
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;


namespace SSLtest
{
	class MyHttpTest
	//class Program
	{
		static void Main(string[] args)
		{
			SecureTcpServer server = null;
			SecureTcpClient client = null;

			try
			{
				int port = 8080;

				RemoteCertificateValidationCallback certValidationCallback = null;
				certValidationCallback = new RemoteCertificateValidationCallback(IgnoreCertificateErrorsCallback);

//				string certPath = System.Reflection.Assembly.GetEntryAssembly().Location;
//				certPath = Path.GetDirectoryName(certPath);
//				certPath = Path.Combine(certPath, "serverCert.cer");
//				Console.WriteLine("Loading Server Cert From: " + certPath);
//				X509Certificate serverCert = X509Certificate.CreateFromCertFile(certPath);
//
				string cfile = "server.pfx";
				string certFilePath = @"/home/kusumah/Projects/PowerHouse/PPOB-Gate/SSLtest/bin/Debug/SSLkeys/" + cfile;
				if (!File.Exists(certFilePath))
					certFilePath = cfile;
				X509Certificate2 certFile = new X509Certificate2(certFilePath, "d4mpt");

				//server = new SecureTcpServer(port, serverCert,
				server = new SecureTcpServer(port, certFile,
					new SecureConnectionResultsCallback(OnServerConnectionAvailable));

				server.StartListening();

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}

			//sleep to avoid printing this text until after the callbacks have been invoked.
			//Thread.Sleep(4000);
			Console.WriteLine("Press any key to close...");
			Console.ReadKey();

			if (server != null)
				server.Dispose();
			//            if (client != null)
			//                client.Dispose();

			Console.WriteLine("BERES...");
		}

//		static void OnServerConnectionAvailable(object sender, SecureConnectionResults args)
//		{
//			if (args.AsyncException != null)
//			{
//				Console.WriteLine(args.AsyncException);
//				return;
//			}
//
//			Stream stream = args.SecureStream;
//
//
//			//Console.WriteLine("Server Connection secured: " + stream.IsAuthenticated);
//
//
//
//			StreamWriter writer = new StreamWriter(stream);
//			writer.AutoFlush = true;
//
//			Thread.Sleep (5000);
//
//			writer.WriteLine("Hello from server!");
//
//			StreamReader reader = new StreamReader(stream);
//			string line = reader.ReadLine();
//			Console.WriteLine("Server Recieved: '{0}'", line == null ? "<NULL>" : line);
//
//			writer.Close();
//			reader.Close();
//			stream.Close();
//		}


		// Baru 1 koneksi, nanti bikin max 200 koneksi
		static ConnectionStreamHandler dataHandler = new ConnectionStreamHandler ();



		static void OnServerConnectionAvailable(object sender, SecureConnectionResults args)
		{
			if (args.AsyncException != null)
			{
				Console.WriteLine(args.AsyncException);
				return;
			}

			Stream stream = args.SecureStream;
			if (stream == null)
			{
				Console.WriteLine("CLient already disconnected");
				return;
			}

			dataHandler.Start (stream);

		}

		static bool IgnoreCertificateErrorsCallback(object sender,
			X509Certificate certificate,
			X509Chain chain,
			SslPolicyErrors sslPolicyErrors)
		{
			Console.WriteLine("IgnoreCertificateErrorsCallback: {0}", sslPolicyErrors);
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

//		static void disconnect(){
//			writer.Close();
//			reader.Close();
//			stream.Close();
//		}


	}
}