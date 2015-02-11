
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


namespace MyTcpClientServerV2
{
	class MyHttpTest
	//class Program
	{
		static void Main(string[] args)
		{
			MyTcpServer server = null;
			//SecureTcpServer server = null;
			//MyTcpClient client = null;

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

				//Console.WriteLine ("AYA YEUH");

				//server = new SecureTcpServer(port, serverCert,
				//server = new SecureTcpServer(port, certFile,
//				server = new MyTcpServer(port, 
//					new NonSecureConnectionResultsCallback(OnServerNonSecureConnectionAvailable));

				server = new MyTcpServer(port, certFile,
					new SecureConnectionResultsCallback(OnServerSecureConnectionAvailable));

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
		static MyTcpStreamHandler dataHandler;



		static void OnServerNonSecureConnectionAvailable(object sender, NonSecureConnectionResults args)
		{
			if (args.AsyncException != null)
			{
				Console.WriteLine(args.AsyncException);
				return;
			}

			Stream stream = args.NonSecureStream;
			if (stream == null)
			{
				Console.WriteLine("CLient already disconnected");
				return;
			}

			dataHandler = new MyTcpStreamHandler ();
			dataHandler.Start (stream, args.Client);
			dataHandler.onDataReceived += new MyTcpStreamHandler.onReceived (DataReceived);

		}

		static void OnServerSecureConnectionAvailable(object sender, SecureConnectionResults args)
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

			dataHandler = new MyTcpStreamHandler ();
			dataHandler.Start (stream, args.Client);
			dataHandler.onDataReceived += new MyTcpStreamHandler.onReceived (DataReceived);

		}

		static private void DataReceived(MyTcpStreamHandler.ConnectionStateObject State){
			Console.WriteLine ("Terima data = " + State.sb.ToString ());
			string balesan = "[" + DateTime.Now.ToString ("yy-MM-dd HH:mm:ss") + "] " + "Data di terima rojerrrr";
			Console.WriteLine ("Bales dengan = " + balesan);
			dataHandler.SendResponse (State, balesan);
			Console.WriteLine ("Siap diskonek");

			if (State.client != null) {
				try{
					Console.WriteLine ("TcpClient CLose");
					State.client.Close ();
				} catch {
				}
				Console.WriteLine ("TcpClient null");
				State.client = null;
			}

			if (State.stream != null) {
				Console.WriteLine ("Stream Close");
				State.stream.Close ();
				Console.WriteLine ("Stream Dispose");
				State.stream.Dispose ();
			}

			dataHandler.Disconnect (State);
			Console.WriteLine ("Udah diskonek");
			dataHandler.Dispose ();
			Console.WriteLine ("Udah dispose");

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