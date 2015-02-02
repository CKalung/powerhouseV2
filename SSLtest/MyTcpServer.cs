using System;
using System.Security.Cryptography.X509Certificates;

namespace SSLtest
{
	public class MyTcpServer : SecureTcpServer
	{
		public MyTcpServer(int listenPort, X509Certificate serverCertificate,
			SecureConnectionResultsCallback callback)
			: this(listenPort, serverCertificate, callback, null)
		{
		}

		public MyTcpServer(int listenPort, X509Certificate serverCertificate,
			SecureConnectionResultsCallback callback,
			RemoteCertificateValidationCallback certValidationCallback)
		{
		}


	}
}

