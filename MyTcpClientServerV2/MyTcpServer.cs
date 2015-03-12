
﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;

namespace MyTcpClientServerV2
{
	public class MyTcpServer : IDisposable {
		#region Disposable
		private bool disposed = false;
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
		~MyTcpServer()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			if (this.listenerV4 != null)
				listenerV4.Stop();
			if (this.listenerV6 != null)
				listenerV6.Stop();

			listenerV4 = null;
			listenerV6 = null;
		}

		X509Certificate serverCert;
		RemoteCertificateValidationCallback certValidationCallback;
		//SecureConnectionResultsCallback connectionCallback;
		SecureConnectionResultsCallback secureConnectionCallback;
		NonSecureConnectionResultsCallback nonSecureConnectionCallback;
		AsyncCallback onAcceptConnection;
		AsyncCallback onAuthenticateAsServer;

		bool started;

		int listenPort;
		TcpListener listenerV4;
		TcpListener listenerV6;
		bool clientCertificateRequired;
		bool checkCertifcateRevocation;
		SslProtocols sslProtocols;

		public MyTcpServer(int listenPort, 
			NonSecureConnectionResultsCallback callback){
			this.serverCert = null;

			if (listenPort < 0 || listenPort > UInt16.MaxValue)
				throw new ArgumentOutOfRangeException("listenPort");

			if (callback == null)
				throw new ArgumentNullException("callback");

			onAcceptConnection = new AsyncCallback(OnAcceptConnection);

			this.nonSecureConnectionCallback = callback;
			this.listenPort = listenPort;
			//this.disposed = false;

		}

		public MyTcpServer(int listenPort, X509Certificate serverCertificate,
			SecureConnectionResultsCallback callback)
			: this(listenPort, serverCertificate, callback, null)
		{
		}

		public MyTcpServer(int listenPort, X509Certificate serverCertificate,
			SecureConnectionResultsCallback callback,
			RemoteCertificateValidationCallback certValidationCallback)
		{
			if (listenPort < 0 || listenPort > UInt16.MaxValue)
				throw new ArgumentOutOfRangeException("listenPort");

			if (serverCertificate == null)
				throw new ArgumentNullException("serverCertificate");

			if (callback == null)
				throw new ArgumentNullException("callback");

			onAcceptConnection = new AsyncCallback(OnAcceptConnection);
			onAuthenticateAsServer = new AsyncCallback(OnAuthenticateAsServer);

			this.serverCert = serverCertificate;
			this.certValidationCallback = certValidationCallback;
			this.secureConnectionCallback = callback;
			this.listenPort = listenPort;
			//this.disposed = 0;
			this.checkCertifcateRevocation = false;
			this.clientCertificateRequired = false;
			this.sslProtocols = SslProtocols.Default;
		}

//		~MyTcpServer()
//		{
//			Dispose();
//		}

		public SslProtocols SslProtocols
		{
			get { return sslProtocols; }
			set { sslProtocols = value; }
		}

		public bool CheckCertifcateRevocation
		{
			get { return checkCertifcateRevocation; }
			set { checkCertifcateRevocation = value; }
		}


		public bool ClientCertificateRequired
		{
			get { return clientCertificateRequired; }
			set { clientCertificateRequired = value; }
		}

		public void StartListening()
		{
			if (started)
				throw new InvalidOperationException("Already started...");

			IPEndPoint localIP;
			if (Socket.SupportsIPv4 && listenerV4 == null)
			{
				localIP = new IPEndPoint(IPAddress.Any, listenPort);
				//Console.WriteLine("SecureTcpServer: Started listening on {0}", localIP);
				listenerV4 = new TcpListener(localIP);
			}

//			if (Socket.OSSupportsIPv6 && listenerV6 == null)
//			{
//				localIP = new IPEndPoint(IPAddress.IPv6Any, listenPort);
//				//Console.WriteLine("SecureTcpServer: Started listening on {0}", localIP);
//				listenerV6 = new TcpListener(localIP);
//			}

			if (listenerV4 != null)
			{
				listenerV4.Start();
				listenerV4.BeginAcceptTcpClient(onAcceptConnection, listenerV4);
			}

//			if (listenerV6 != null)
//			{
//				listenerV6.Start();
//				listenerV6.BeginAcceptTcpClient(onAcceptConnection, listenerV6);
//			}

			started = true;
		}

		public void StopListening()
		{
			if (!started)
				return;

			started = false;

			if (listenerV4 != null)
				listenerV4.Stop();
			if (listenerV6 != null)
				listenerV6.Stop();
		}

		void OnAcceptConnection(IAsyncResult result)
		{
			TcpListener listener = result.AsyncState as TcpListener;
			TcpClient client = null;
			SslStream sslStream = null;
			Stream stream = null;

			try
			{
				if (this.started)
				{
					//start accepting the next connection...
					try{
						listener.BeginAcceptTcpClient(this.onAcceptConnection, listener);
					}catch{
						// disini karena service di stop.
						return;
					}
				}
				else
				{
					//someone called Stop() - don't call EndAcceptTcpClient because
					//it will throw an ObjectDisposedException
					return;
				}

				//complete the last operation...
				client = listener.EndAcceptTcpClient(result);


				bool leaveStreamOpen = false;//close the socket when done

				if(this.serverCert != null){
					// Jika SSL
					if (this.certValidationCallback != null)
						sslStream = new SslStream(client.GetStream(), leaveStreamOpen, this.certValidationCallback);
					else
						sslStream = new SslStream(client.GetStream(), leaveStreamOpen);

					sslStream.BeginAuthenticateAsServer(this.serverCert,
						this.clientCertificateRequired,
						this.sslProtocols,
						this.checkCertifcateRevocation,//checkCertifcateRevocation
						this.onAuthenticateAsServer,
						new SecureConnectionResults(client, sslStream));
						//sslStream);
				} else {
					// jika non SSL
					stream = client.GetStream();
					this.nonSecureConnectionCallback(this, new NonSecureConnectionResults (client, stream));
				}

			}
			catch (Exception ex) {
				if (this.serverCert != null) {
					// Jika SSL
					if (sslStream != null) {
						sslStream.Dispose ();
						sslStream = null;
					}
					this.secureConnectionCallback (this, new SecureConnectionResults (ex));
				} else {
					// jika non SSL
					if (stream != null) {
						stream.Dispose ();
						stream = null;
					}
					this.nonSecureConnectionCallback (this, new NonSecureConnectionResults (ex));
				}
			}
		}

		void OnAuthenticateAsServer(IAsyncResult result)
		{
			SecureConnectionResults scr = null;
			SslStream sslStream = null;
			TcpClient client = null;
			try
			{
				//sslStream = result.AsyncState as SslStream;
				scr = result.AsyncState as SecureConnectionResults;
				client = scr.Client;
				sslStream = scr.SecureStream;
				sslStream.EndAuthenticateAsServer(result);

				//this.secureConnectionCallback(this, new SecureConnectionResults(scr.Client, sslStream));
				this.secureConnectionCallback(this, scr);
			}
			catch (Exception ex)
			{
				if (client != null) {
					try{client.Close ();} catch{
					}
					client = null;
				}
				if (sslStream != null){
					try{sslStream.Close ();} catch{
					}
					sslStream.Dispose();
					sslStream = null;
				}
				this.secureConnectionCallback(this, new SecureConnectionResults(ex));
				//this.secureConnectionCallback(this, ex);
			}
		}

//		public void Dispose()
//		{
//			if (System.Threading.Interlocked.Increment(ref disposed) == 1)
//			{
//				if (this.listenerV4 != null)
//					listenerV4.Stop();
//				if (this.listenerV6 != null)
//					listenerV6.Stop();
//
//				listenerV4 = null;
//				listenerV6 = null;
//
//				GC.SuppressFinalize(this);
//			}
//		}
	}
}