
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Collections;
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;

using MyTcpClientServerV2;
using LOG_Handler;
using PHClientsPluginsInterface;
using PHClientProtocolTranslatorInterface;

using PHPlainJsonClientTranslator;
using PHCommonClientHandler;

namespace PHPlainJsonServerPlugin
{
	[Export(typeof(IConnectionModules))]
	public class PhPlainJsonServerPlugin : BaseConnectionPlugins
	{
		#region Disposable
		private bool disposed;
		public override void Dispose()
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
		~PhPlainJsonServerPlugin()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			if (server != null)
				server.Dispose ();
			CloseAllConnections ();
			//			if (connectionCollector != null)
			//				connectionCollector.Dispose ();
		}

		MyTcpServer server = null;
		X509Certificate2 certificateFile = null;
		//string certificatePassword = "";

		int indexConnection = 0;
		string configFilePath = "";
		const string namaModule = "PH Tcp Server Module Plugin";
		Hashtable ConnectionList = new Hashtable();

		//PhHttpConnectionCollector connectionCollector = null;

		//const string ConfigFile = "HttpConfig.ini";


		string certFilePath = "";
		string certPassword = "";
		int port = 0;

		public PhPlainJsonServerPlugin ()
			:base(namaModule) {
			//connectionCollector = new PhHttpConnectionCollector ();
		}

		private void LoadConfig(string ConfigFilePath){
			using (CrossIniFile.INIFile a = 
				new CrossIniFile.INIFile (ConfigFilePath)) {
				port = a.GetValue("HttpServerModule", "Port", 443);
				certFilePath = a.GetValue ("HttpServerModule", "CertificateFilePath", "");
				certPassword = a.GetValue ("HttpServerModule", "CertificatePassword", "");
				LogWriter.show (this,"Http Port = " + port.ToString () + ", Certificate = " + certFilePath);
				//"./SslKeys/server.pfx");
			}
		}

		public override void Start(string applicationPath, string ConfigFilePath){
			//ConfigFilePath = System.IO.Path.Combine (applicationPath, ConfigFile);
			//ConfigFilePath = configFilePath;
			//			Console.WriteLine ("Start plugin http");
			//			Console.WriteLine ("Paths : \r\n");
			//			Console.WriteLine (
			//				AppDomain.CurrentDomain.SetupInformation.ApplicationBase +"\r\n" +
			//				System.Reflection.Assembly.GetExecutingAssembly().Location +"\r\n"
			//			);

			configFilePath = ConfigFilePath;
			LoadConfig (ConfigFilePath);
			//connectionCollector.ConfigFilePath = ConfigFilePath;

			try
			{
				//            appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

				//Console.WriteLine ("["+this.ToString ()+"] CertFile = " + certFilePath);
				if(certFilePath != "" ){
					if (!File.Exists(certFilePath)){
						LogWriter.write (this, LogWriter.logCodeEnum.ERROR,"Certificate file not found " + certFilePath);
						return;
					}

					RemoteCertificateValidationCallback certValidationCallback = null;
					certValidationCallback = new RemoteCertificateValidationCallback(IgnoreCertificateErrorsCallback);

					certificateFile = new X509Certificate2(certFilePath, certPassword);	// "d4mpt");

					//Console.WriteLine ("["+this.ToString ()+"] STARTING SECURE SERVER");
					LogWriter.show (this,"STARTING SECURE SERVER");
					server = new MyTcpServer(port, certificateFile,
						new SecureConnectionResultsCallback(OnServerSecureConnectionAvailable));
				} else{
					//Console.WriteLine ("["+this.ToString ()+"] STARTING NONSECURE SERVER");
					LogWriter.show (this,"STARTING NONSECURE SERVER");
					server = new MyTcpServer(port, 
						new NonSecureConnectionResultsCallback(OnServerNonSecureConnectionAvailable));
				}

				server.StartListening();

			}
			catch (Exception ex)
			{
				LogWriter.show (this,ex.Message);
				return;
			}
		}

		public override void Stop(){
			server.StopListening ();
		}


		void OnServerNonSecureConnectionAvailable(object sender, NonSecureConnectionResults args)
		{
			if (args.AsyncException != null)
			{
				LogWriter.show (this,args.AsyncException.ToString ());
				return;
			}

			if (args.NonSecureStream == null)
			{
				LogWriter.show (this,"CLient already disconnected");
				return;
			}

			NewConnection (args.NonSecureStream, args.Client);
		}

		void OnServerSecureConnectionAvailable(object sender, SecureConnectionResults args)
		{
			if (args.AsyncException != null)
			{
				LogWriter.show (this,args.AsyncException.ToString ());
				return;
			}

			// konversi aman disini
			Stream stream = args.SecureStream;
			if (args.SecureStream == null)
			{
				LogWriter.show (this,"CLient already disconnected");
				return;
			}

			NewConnection (stream, args.Client);
		}


		bool IgnoreCertificateErrorsCallback(object sender,
			X509Certificate certificate,
			X509Chain chain,
			SslPolicyErrors sslPolicyErrors)
		{
			LogWriter.show (this,"IgnoreCertificateErrorsCallback: " + sslPolicyErrors.ToString ());
			if (sslPolicyErrors != SslPolicyErrors.None)
			{

				LogWriter.show (this,"IgnoreCertificateErrorsCallback: " + sslPolicyErrors.ToString ());
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

		//IPhClientHandler clientHandler;

		public void NewConnection(Stream stream, TcpClient client){
			if (ConnectionList.ContainsKey (indexConnection)) {
				if (client != null)
					client.Close();
				if (stream != null) {
					stream.Close();
					stream.Dispose ();
				}
				return;
			}

			PhCommonClientHandler HttpHandler = new PhCommonClientHandler (indexConnection, configFilePath);
			HttpHandler.Translator = new PhPlainJsonTranslator ();
			//HttpHandler.onDisconnected += HandleonDisconnected;
			HttpHandler.onDisconnected += new PhCommonClientHandler.onDisconnectedEvent (HandleonDisconnected);
			ConnectionList.Add (indexConnection, HttpHandler);
			((PhCommonClientHandler)(ConnectionList[indexConnection])).Start (stream, client);

			indexConnection++;
			if (indexConnection == int.MaxValue)
				indexConnection = 0;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		void HandleonDisconnected(int indexConn){

			//			Console.WriteLine ("["+this.ToString () + "] ON DISCONNECTED");

			if (ConnectionList.ContainsKey (indexConn)) {
				try{ ((PhCommonClientHandler)ConnectionList [indexConn]).Dispose (); }catch{
				}
				ConnectionList.Remove (indexConn);
			}

			//Console.WriteLine ("["+this.ToString () + "] Live connection: " + ConnectionList.Count.ToString ());
			LogWriter.show (this,"Live connection: " + ConnectionList.Count.ToString ());
		}

		private void CloseAllConnections(){
			foreach (DictionaryEntry aConn in ConnectionList) {
				if(aConn.Value!=null)
					((PhCommonClientHandler)aConn.Value).Dispose ();
			}
			ConnectionList.Clear ();
		}

	}
}

