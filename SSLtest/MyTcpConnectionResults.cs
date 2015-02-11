using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net.Security;


namespace SSLtest
{

	public delegate void SecureConnectionResultsCallback(object sender, 
		SecureConnectionResults args);

	public delegate void NonSecureConnectionResultsCallback(object sender, 
		NonSecureConnectionResults args);

	public class SecureConnectionResults
    {
		private SslStream secureStream;
		private TcpClient tcpClient;
        private Exception asyncException;

		internal SecureConnectionResults(TcpClient client, SslStream sslStream)
        {
			this.tcpClient = client;
			this.secureStream = sslStream;
        }

		internal SecureConnectionResults(TcpClient client, Exception exception)
        {
			this.tcpClient = client;
            this.asyncException = exception;
        }

        public Exception AsyncException { get { return asyncException; } }
		public TcpClient Client { get { return tcpClient; } }
        public SslStream SecureStream { get { return secureStream; } }
    }

	public class NonSecureConnectionResults
	{
		private Stream nonSecureStream;
		private TcpClient tcpClient;
		private Exception asyncException;

		internal NonSecureConnectionResults(TcpClient client, Stream stream)
		{
			this.tcpClient = client;
			this.nonSecureStream = stream;
		}

		internal NonSecureConnectionResults(TcpClient client, Exception exception)
		{
			this.tcpClient = client;
			this.asyncException = exception;
		}

		public Exception AsyncException { get { return asyncException; } }
		public TcpClient Client { get { return tcpClient; } }
		public Stream NonSecureStream { get { return nonSecureStream; } }
	}
}
