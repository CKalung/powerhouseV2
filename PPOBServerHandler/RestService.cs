using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace PPOBServerHandler
{
    public class RestService: IDisposable
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
        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            rh.Dispose();
        }
        ~RestService()
        {
            this.Dispose(false);
        }
        #endregion

        private RestHandler rh;

        //public struct httpRestParameter
        //{
        //    public string httpUri;
        //    public string canonicalPath;
        //    public string method;
        //    public string secretKey;
        //    public string userAuth;
        //    public string contentType;
        //    public string bodyMessage;
        //    public string authID;
        //}
        public struct httpResponse
        {
            public int httpCode;
            public string httpMessage;
            public string httpDate;
            public string httpContentType;
            public string httpBody;
            public string serverResponseCode;
            public string serverResponseMessage;
        }
        private httpResponse resp;
        //private httpRestParameter parm;

        public RestService()
        {
            rh = new RestHandler();
            resp = new httpResponse();
            //parm = new httpRestParameter();
        }

        public RestService(string _baseAddress, string _serverPort, string _baseDir, 
            string _canonicalPath, string _method, string _userAuth, string _secreteKey,
            string _contentType, string _bodyMessage, string _contentParam, string anu)
        {
            rh = new RestHandler(_baseAddress, _serverPort, _baseDir, _canonicalPath, _method, 
                _userAuth, _secreteKey, _contentType, _bodyMessage, _contentParam);
        }

        public string httpUri { get { return rh.httpUri; } set { rh.httpUri = value; } }
        public string canonicalPath { get { return rh.canonicalPath; } set { rh.canonicalPath = value; } }
        public string method { get { return rh.method; } set { rh.method = value; } }
        public string secretKey { get { return rh.secretKey; } set { rh.secretKey = value; } }
        public string userAuth { get { return rh.userAuth; } set { rh.userAuth = value; } }
        public string contentType { get { return rh.contentType; } set { rh.contentType = value; } }
        public string bodyMessage { get { return rh.bodyMessage; } set { rh.bodyMessage = value; } }
        public string authID { get { return rh.authID; } set { rh.authID = value; } }

		// Fungsi untuk otentikasi via http web request
		public string HttpRestSendRequest(int timeout)
		{
			if (!rh.HTTPCreateConnection(timeout)) return "";
			rh.CreateSignature();
			rh.HTTPHeaderBuilder();
			if (!rh.HTTPSendBody ())
				return "";
			return rh.HTTPReceiveRequest();
		}

        // Fungsi untuk send request
        public string TCPRestSendRequest(int recTimeOut)
        {
            if (!rh.TCPCreateConnection()) return "";
            rh.CreateSignature();
            rh.TCPHeaderBuilder();
            return rh.TCPReceiveRequest(recTimeOut);
        }

        // Fungsi untuk otentikasi via tcp socket
        public string TCPRestAuth(int recTimeOut)
        {
            if (!rh.TCPCreateConnection()) return "";
            rh.CreateSignature();
            rh.TCPHeaderBuilder();
            return rh.TCPReceiveRequest(recTimeOut);
        }

        public string TCPRestInqury(int recTimeOut)
        {
            rh.TCPCreateConnection();
            rh.CreateSignature();
            rh.TCPHeaderBuilder();
            return rh.TCPReceiveRequest(recTimeOut);
        }

        //public httpRestParameter Params
        //{
        //    get {
        //        parm.httpUri = rh.httpUri;
        //        parm.canonicalPath = rh.canonicalPath;
        //        parm.method = rh.method;
        //        parm.secretKey = rh.secretKey;
        //        parm.userAuth = rh.userAuth;
        //        parm.contentType = rh.contentType;
        //        parm.bodyMessage = rh.bodyMessage;
        //        parm.authID = rh.authID;
        //        return parm;
        //    }
        //    set {
        //        rh.httpUri = value.httpUri;
        //        rh.canonicalPath = value.canonicalPath;
        //        rh.method = value.method;
        //        rh.secretKey = value.secretKey;
        //        rh.userAuth = value.userAuth;
        //        rh.contentType = value.contentType;
        //        rh.bodyMessage = value.bodyMessage;
        //        rh.authID = value.authID;
        //    }
            
        //}

        public httpResponse Response
        {
            get
            {
                resp.httpBody = rh.Response.httpBody;
                resp.httpCode = rh.Response.httpCode;
                resp.httpContentType = rh.Response.httpContentType;
                resp.httpDate = rh.Response.httpDate;
                resp.httpMessage = rh.Response.httpMessage;
                resp.serverResponseCode = rh.Response.serverResponseCode;
                resp.serverResponseMessage = rh.Response.serverResponseMessage;
                return resp;
            }
        }
    }
}
