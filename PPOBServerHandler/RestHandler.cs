using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Net.NetworkInformation;
using LOG_Handler;
using StaticCommonLibrary;
using System.Security.Cryptography.X509Certificates;


namespace PPOBServerHandler
{
	public class DummyPolicy : ICertificatePolicy {

		public bool CheckValidationResult(
			ServicePoint srvPoint
			, X509Certificate certificate
			, WebRequest request
			, int certificateProblem) {

			//Return True to force the certificate to be accepted.
			//sException += "\n* Certificate Accepted.";                 
			return true;

		} // end CheckValidationResult
	} // class MyPolicy

    class RestHandler: IDisposable
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
        ~RestHandler()
        {
            this.Dispose(false);
        }
        #endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			cryp.Dispose();
		}

		private HttpWebRequest request;
		private TcpClient client;
		private NetworkStream clientStream= null;
		private SslStream sslClientStream;

        //private string httpUri = "http://123.231.225.20:7080/hms/rest/requestToken";
        //private string baseAddress = "123.231.225.20:7080";
        //private string baseAddress = "123.231.225.20";
        //private string serverPort = "7080";
        //private string bodyMessage = "{\"sourceHostId\":\"switching-gateway-service\"}";
        //private string contentType = "application/json";
        //private string secretKey = "dummy-01";
        //private string userAuth = "dummy-01";
        //private string canonicalPath = "/requestToken";
        //private string authID = "DA01";

        //private string baseDir = "/hms/rest";
        private string rest_httpUri = "";
        private string rest_canonicalPath = "";
        private string rest_method = "POST";
        private string rest_secretKey = "";
        private string rest_userAuth = "";
        private string rest_contentType = "";
        private string rest_bodyMessage = "";
        private string rest_authID = "";
        private byte[] babodyMessage = null;

        private string uriHost = "";
        private int uriPort = 0;
        private string uriLocalPath = "";
        private string authParam = "";
        //private string contentParam = "DA01 dummy-01:";
        private string httpDate = "";

        private string requestHeaders = "";
        private string requestMethod = "";

		//private bool fUsingSSL = false;

		httpResponse httpResp;
		lastError Lasterr;
		//int rest_receiveTimeOut = 0;

        private Cryptograph cryp;

        public struct lastError
        {
            public string Code;
            public string Message;
        }

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

//		public bool UsingSSL
//		{
//			get { return fUsingSSL;}
//			set { fUsingSSL = value;}
//		}

        public lastError LastError
        {
            get { return Lasterr; }
        }

        public RestHandler()
        {
            cryp = new Cryptograph();
            httpResp = new httpResponse();
        }

        public RestHandler(string _baseAddress, string _serverPort, string _baseDir, 
            string _canonicalPath, string _method, string _userAuth, string _secreteKey, 
            string _contentType, string _bodyMessage, string _contentParam) //, int _receiveTimeOut)
        {
            //baseAddress = _baseAddress;
            //serverPort = _serverPort;
            //baseDir = _baseDir;
            //httpUri = "http://" + _baseAddress + ":" + serverPort + _baseDir + _canonicalPath;
            rest_canonicalPath = _canonicalPath;
            rest_method = _method;
            rest_secretKey = _secreteKey;
            rest_userAuth = _userAuth;
            rest_contentType = _contentType;
            rest_bodyMessage = _bodyMessage;
            //rest_receiveTimeOut = _receiveTimeOut;
            //contentParam = _contentParam;

			//fUsingSSL = _usingSSL;

            requestHeaders = "";

            babodyMessage = new byte[rest_bodyMessage.Length];
            //babodyMessage = System.Text.Encoding.ASCII.GetBytes(bodyMessage);
            babodyMessage = UTF8Encoding.UTF8.GetBytes(rest_bodyMessage.ToString());

            cryp = new Cryptograph();
            httpResp = new httpResponse();
        }

        public string httpUri
        {
            get { return rest_httpUri; }
            set
            {
                rest_httpUri = value;
                // jigana moal matak memleak lah.... euweuh disposena si uri teh
				try{
                System.Uri uri = new Uri(rest_httpUri);
                uriLocalPath = uri.LocalPath;
                uriHost = uri.Host;
                uriPort = uri.Port;
				}
				catch (Exception ex){
					LogWriter.write (this, LogWriter.logCodeEnum.ERROR, ex.getCompleteErrMsg ());
				}
            }
        }
        public string canonicalPath { get { return rest_canonicalPath; } set { rest_canonicalPath = value; } }
        public string method { get { return rest_method; } set { rest_method = value; } }
        public string secretKey { get { return rest_secretKey; } set { rest_secretKey = value; } }
        public string userAuth { get { return rest_userAuth; } set { rest_userAuth = value; } }
        public string contentType { get { return rest_contentType; } set { rest_contentType = value; } }
        public string bodyMessage
        {
            get { return rest_bodyMessage; }
            set
            {
                rest_bodyMessage = value;
                if (babodyMessage != null) Array.Resize(ref babodyMessage, rest_bodyMessage.Length);
                else babodyMessage = new byte[rest_bodyMessage.Length];
                babodyMessage = UTF8Encoding.UTF8.GetBytes(rest_bodyMessage);
            }
        }
        public string authID { get { return rest_authID; } set { rest_authID = value; } }

		public bool HTTPCreateConnection(int timeout)
		{
			System.Net.ServicePointManager.CertificatePolicy = new DummyPolicy();

			try
			{
				//HttpWebRequest request = (HttpWebRequest)WebRequest.Create(rest_httpUri);
				//request = (HttpWebRequest)WebRequest.Create(rest_httpUri);
				request = (HttpWebRequest)WebRequest.Create(rest_httpUri + rest_canonicalPath);
				request.Timeout = timeout * 100;
				//				request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:8.0) Gecko/20100101 Firefox/8.0";
				//				request.Referer = "https://www.majesticseo.com/account/login";
				//				request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,**;q=0.8";
				//				request.UnsafeAuthenticatedConnectionSharing = true;
				//				request.Method = "POST";
				//				request.KeepAlive = true;
				//				request.ContentType = "application/x-www-form-urlencoded";
				//				request.AllowAutoRedirect = true;

				//request.UserAgent = "PowerHouse/1.0 (Windows NT 7.0; WOW64; rv:8.0)";
				//request.Referer = "http://www.qiosku.com";
				//request.Accept = "application/json";
				request.UnsafeAuthenticatedConnectionSharing = true;
				request.Method = "POST";
				request.KeepAlive = true;
				request.ContentType = "application/json";
				request.AllowAutoRedirect = true;
				return true;

			}
			catch //(Exception ex)
			{

				return false;
			}
		}

        public bool TCPCreateConnection()
        {
            try
            {
                //System.Uri uri = new Uri(rest_httpUri);
                //client = new System.Net.Sockets.TcpClient(uri.Host, uri.Port);
                //requestMethod = "POST " + uri.LocalPath + " HTTP/1.1\r\n";                //System.Uri uri = new Uri(rest_httpUri);
				client = new System.Net.Sockets.TcpClient(uriHost, uriPort);
				requestMethod = rest_method + " " + uriLocalPath + rest_canonicalPath + " HTTP/1.1\r\n";
				clientStream = client.GetStream();

//				if(fUsingSSL)
//				{
//					sslClientStream = new SslStream(clientStream);
//					sslClientStream.AuthenticateAsClient(uriHost);
//				}
				return true;
            }
            catch (Exception ex)
            {
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Failed to connect to server : " + ex.getCompleteErrMsg ());
                return false;
            }
        }

        public void CreateSignature()
        {
            httpDate = DateTime.UtcNow.ToString("r");

            string bodyMd5 = "";
            bodyMd5 = cryp.getMd5(rest_bodyMessage);

            string dataParam = "";
            dataParam += rest_method + "\n";
            dataParam += bodyMd5.ToLower() + "\n";
            dataParam += rest_contentType + "\n";
            dataParam += httpDate + "\n";
            dataParam += rest_canonicalPath;
            //Console.WriteLine("==== StringToSign ====");
            //Console.WriteLine(dataParam + "\n");

            // bikin mac 
            string mac = cryp.getMac(dataParam, rest_secretKey);
            //Console.WriteLine("==== Signature-Base64 ====");
            //Console.WriteLine(mac + "\n");

            //contentParam += mac;
            authParam = rest_authID + " " + rest_userAuth + ":" + mac;

            //Console.WriteLine("==== Authorization ====");
            //Console.WriteLine(contentParam + "\n");            
        }

		public void HTTPHeaderBuilder()
		{
			try
			{
				//request.Host = baseAddress;
				//request.Headers.Add(HttpRequestHeader.Host, baseAddress);
				//request.Headers.Add(HttpRequestHeader.Host, uriHost+":"+uriPort.ToString());
				request.Method = rest_method;
				request.Date = DateTime.Parse(httpDate);
				//request.Headers.Add(HttpRequestHeader.Date, httpDate);

				// cek apakah ada param auth
				if (rest_userAuth != "" && rest_secretKey != ""){
					request.Headers.Add(HttpRequestHeader.Authorization, authParam);
				//                    request.Headers.Add(HttpRequestHeader.Authorization, contentParam);
					LogWriter.showDEBUG(this, "Authorization: " + authParam);
				}

				request.ContentType = rest_contentType;

				request.ContentLength = babodyMessage.Length;
				//request.KeepAlive = true;
				request.KeepAlive = false;
				//request.ServicePoint.Expect100Continue = false;

				//request.Headers.Add("X-Requested-With", "XMLHttpRequest");
				//request.Timeout = 20000;

			}
			catch (Exception ex)
			{
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Failed on create http headers : " + ex.getCompleteErrMsg ());
			}

			//Console.Write("==== Request Headers: ==== ");
			//Console.WriteLine(request.Headers.ToString());
		}

        public void TCPHeaderBuilder()
        {
            try
            {
                requestHeaders = "";
                //requestHeaders += "Host: " + baseAddress + "\r\n";      // fix dulu
                requestHeaders += "Host: " + uriHost + ":" + uriPort.ToString() + "\r\n";      // fix dulu
                requestHeaders += "Date: " + httpDate + "\r\n";

                // cek apakah ada param auth
                if (rest_userAuth != "" && rest_secretKey != "")
                {
                    //                    requestHeaders += "Authorization: " + contentParam + "\r\n";
                    requestHeaders += "Authorization: " + authParam + "\r\n";
                }
                requestHeaders += "Content-Type: " + rest_contentType + "\r\n";
                requestHeaders += "Content-Length: " + babodyMessage.Length + "\r\n";
                //requestHeaders += "Expect: 100-continue\r\n";
                //requestHeaders += "Connection: Keep-Alive\r\n";
                requestHeaders += "\r\n";
            }
            catch //(Exception ex)
            {
                requestHeaders = "";
            }

            //Console.Write("==== Request Headers: ==== ");
            //Console.WriteLine(request.Headers.ToString());
        }

        // Ambil Response dari server
        public string TCPReceiveRequest(int recTimeOut)
        {
            //return sendByTCP(rest_httpUri + rest_canonicalPath, requestHeaders, rest_bodyMessage, "");
            return sendByTCP(requestHeaders, rest_bodyMessage, "", recTimeOut);
        }

		public bool HTTPSendBody()
		{
			// send body
			//			string SendHttpString = "Send HTTP REST : \r\n";

//			foreach (string header in request.Headers) {
//				SendHttpString += header +"\r\n";
//			}
//			SendHttpString += rest_bodyMessage;
//
//			LogWriter.showDEBUG (this, SendHttpString);

			try{
				Stream os = request.GetRequestStream();
				os.Write(babodyMessage, 0, babodyMessage.Length);            
				os.Close();
				return true;
			}
			catch(Exception ex) {
				//				LogWriter.showDEBUG (this, ex.getCompleteErrMsg ());
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, ex.getCompleteErrMsg());
				return false;
			}
		}

		public string HTTPReceiveRequest()
		{
			httpResp.httpCode=0;
			httpResp.httpMessage = "";
			httpResp.httpContentType = "";
			httpResp.httpDate = "";
			httpResp.httpBody = "";
			httpResp.serverResponseCode = "";
			httpResp.serverResponseMessage = "";
			try{
				HttpWebResponse response = null;
				string responseValue = string.Empty;

				try
				{
					response = request.GetResponse() as HttpWebResponse;
				}
				catch (WebException ex)
				{
					response = ex.Response as HttpWebResponse;
				}
				catch(UriFormatException e)
				{
					LogWriter.write(this,LogWriter.logCodeEnum.ERROR, "Invalid URL");
					if(response != null)
						response.Dispose();
					return responseValue;
				}
				catch(IOException e)
				{
					LogWriter.write(this,LogWriter.logCodeEnum.ERROR, "Could not connect to URL");
					if(response != null)
						response.Dispose();
					return responseValue;
				}

				//using (var response = (HttpWebResponse)request.GetResponse())
				//{
					// disini parse response.Headers untuk dapet responseCode dan responseMessage
					WebHeaderCollection headers = response.Headers;

//					if (response.StatusCode != HttpStatusCode.OK)
//					{
//						//						var message = String.Format("Request failed. Received HTTP {0}", response.StatusCode);
//						//						throw new ApplicationException(message);
//						httpResp.httpCode=(int)response.StatusCode;
//						httpResp.httpMessage = response.StatusDescription;
//						return "error";
//					}

					// grab the response
					using (var responseStream = response.GetResponseStream())
					{
						if (responseStream != null)
							using (var reader = new StreamReader(responseStream))
						{
							responseValue = reader.ReadToEnd();
						}
					}
					httpResp.httpCode=(int)response.StatusCode;
					httpResp.httpMessage = response.StatusDescription;
					httpResp.httpContentType = response.ContentType;
					httpResp.httpDate = headers["Date"];
					httpResp.httpBody = responseValue;
					httpResp.serverResponseCode = headers["responseCode"];
					httpResp.serverResponseMessage = headers["responseMessage"];

					response.Dispose();
					return responseValue;
				//}
			}
			catch (Exception ex) {
				int idx = 0;
				int idy = 0;
				string ercode = "";
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Failed to get Http response : " + ex.getCompleteErrMsg ());

				try{
					while ((ex.Message[idx] != '(') && (idx < ex.Message.Length))
						idx ++;
					if (idx < ex.Message.Length) {
						idy = idx;
						idy ++;
						while ((ex.Message[idy] != ')') && (idy < ex.Message.Length)) {
							ercode += ex.Message [idy];
							idy ++;
						}
					} else
						ercode = "404";
				} catch {
					// Jika error respon null atau kondisi lain
					ercode = "404";
					httpResp.httpCode=404;
					httpResp.httpMessage = "Not found";
					return ex.Message;
				}

				idy ++;
				try{
					httpResp.httpCode=int.Parse(ercode);
					httpResp.httpMessage = ex.Message.Substring(idy);
				}
				catch {
					httpResp.httpCode=404;
					httpResp.httpMessage = "Not found";
				}
				return ex.Message;
			}
		}

        private bool isHttpMessageCompleted(string msg)
        {
            //Console.WriteLine("=========");
            //Console.WriteLine(msg);
            //Console.WriteLine("=========");

            // cari "Content-Length: "
            string[] lines = msg.Split('\n');
            string tStr = "";
            int bodylen = 0;
            bool fSearchBody = false;
            int startBody = 0;
            bool fSudahContinue = false;

            httpResp.httpCode=0;
            httpResp.httpMessage = "";
            httpResp.httpContentType = "";
            httpResp.httpDate = "";
            httpResp.httpBody = "";
            httpResp.serverResponseCode = "";
            httpResp.serverResponseMessage = "";


            try
            {
                foreach (string line in lines)
                {
                    if (line.StartsWith("Content-Length: "))
                    {
                        // ambil length content
                        tStr = line.Substring(16, line.Length - 17);
                        bodylen = int.Parse(tStr);
                        fSearchBody = true;
                    }
                    else if (line.StartsWith("HTTP/"))
                    {
                        string[] httpStat = line.Split(' ');
                        httpResp.httpCode = int.Parse(httpStat[1]);
                        httpResp.httpMessage = line.Substring(httpStat[0].Length+httpStat[1].Length+2).TrimEnd();
                    }
                    else if (line.StartsWith("Content-Type:"))
                    {
                        httpResp.httpContentType = line.Substring(14).TrimEnd();
                    }
                    else if (line.StartsWith("Date:"))
                    {
                        httpResp.httpDate = line.Substring(6).TrimEnd();
                    }
                    else if (line.StartsWith("responseCode:"))
                    {
                        httpResp.serverResponseCode = line.Substring(14).TrimEnd();
                    }
                    else if (line.StartsWith("responseMessage:"))
                    {
                        httpResp.serverResponseMessage = line.Substring(17).TrimEnd();
                    }
                    if (line == "\r")   // header beres
                    {
                        if (httpResp.httpCode == 100)
                        {
                            fSudahContinue = true;
                            continue;
                        }
                        if (bodylen == 0) return true;
                        break;
                    }
                }
                if (fSearchBody)
                {
                    if (bodylen == 0) return true;
                    for (int i = 0; i < msg.Length; i++)
                    {
                        if ((msg[i] == '\n') && ((msg[i + 1] == '\r') || (msg[i + 1] == '\n')))
                        {
                            if (fSudahContinue)
                            {
                                fSudahContinue = false;
                                //i += 10;
                                continue;
                            }
                            // maka i+2 = start body
                            startBody = i + 3;
                            httpResp.httpBody = msg.Substring(startBody);
                            if (msg.Length >= (startBody + bodylen)) return true;   // paket lengkap
                            else return false;
                        }
                    }
                    return false;
                }
                else if ((httpResp.httpCode!=200) && (httpResp.httpCode!=100))
                {
                    return true;
                }
                else return false;
            }catch
            {
                return false;
            }
        }

        public httpResponse Response
        {
            get { return httpResp; }
        }

        //private string sendByTCP(string requestUrl, string requestHeaders, string body, string parameters)
        private string sendByTCP(string requestHeaders, string body, string parameters, int recTimeOut)
        {
            //rest_receiveTimeOut = timeOut;
            string responseData = "";
            Byte[] bytes = new byte[2048];
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            bool fTO = true;
            string sTmp = "";

            // Clean the text to send
            // body = HttpUtility.UrlEncode(body, System.Text.Encoding.UTF8);

            //if (body.Length > 0) parameters += "body=" + body;

            parameters += body;

            // Get the data to be sent as a byte array.
            LogWriter.show(this, "SEND: " + requestMethod + requestHeaders + parameters + "\r\n");

            Byte[] data = System.Text.Encoding.UTF8.GetBytes(requestMethod + requestHeaders + parameters + "\r\n");
            httpResp.httpCode = 0;
            httpResp.httpMessage = "";
            httpResp.httpContentType = "";
            httpResp.httpDate = "";
            httpResp.httpBody = "";
            httpResp.serverResponseCode = "";
            httpResp.serverResponseMessage = "";

            try
            {
                // Send the.StackTrace to the connected TcpServer.

//				if(fUsingSSL)
//				{
//					sslClientStream.Write(data, 0, data.Length);
//					sslClientStream.Flush();
//				}
//				else
//				{
	                clientStream.Write(data, 0, data.Length);
	                clientStream.Flush();
//				}

                while (true)
                {
                    // Receive the TcpServer.response.
                    fTO = true;
                    //for (int i = 0; i < 50; i++)
                    //for (int i = 0; i < 100; i++)
                    for (int i = 0; i < recTimeOut ; i++)
                    {
                        if (client.Client == null)
                        {
                            fTO = true;
                            break;
                        }
                        else if (clientStream.DataAvailable)
                        {
                            fTO = false;
                            break;
                        }
                        System.Threading.Thread.Sleep(100);
                    }
                    if (fTO)
                    {
                        Lasterr.Code = "z405";
                        Lasterr.Message = "No response from Sandra host";
                        client.Close();
                        return "";
                    }

					while (clientStream.DataAvailable)
                    {
						int count = 0;
//						if(fUsingSSL)
//							count = sslClientStream.Read(bytes, 0, 2048);
//						else
	                        count = clientStream.Read(bytes, 0, 2048);
                        //int count = sslStream.Read(bytes, 0, 1024);
                        if (count == 0)
                        {
                            break;
                        }
                        sb.Append(System.Text.Encoding.UTF8.GetString(bytes, 0, count));
                    }
                    // jika received "Continue", perintahkan continue ke while
                    sTmp = sb.ToString();
                    if (sTmp == "HTTP/1.1 100 Continue\r\n\r\n")
                    {
                        LogWriter.show(this, "**** dapet continue doang *****");
                        continue;
                    }

                    if (sb.Length > 0)
                    {
                        LogWriter.show(this, "=== PARSE ===\r\n "+ sTmp);
                        if (isHttpMessageCompleted(sTmp))
                        {
                            break;
                        }
                        // didieu teu kudu pake timeout mun data teu lengkap bae karena udah pake fTO diluhur
                    }
                    else break;
                }
            }
            catch
            {
                Lasterr.Code = "z404";
                Lasterr.Message = "Sandra host down";
            }

            responseData = sb.ToString();
            LogWriter.show(this, "*=*=*=*=*=  TERIMA dr host REST =========\r\n" + responseData);
            // Close everything.
			try{
				if(sslClientStream != null) sslClientStream.Close();
			}
			catch{
			}
			try{
				clientStream.Close();
			}
			catch{
			}
            try
            {
                if (client != null) client.Close();
            }
            catch { }

            return responseData;
        }

        public string getHttpDateFormat()
        {
            return DateTime.UtcNow.ToString("r");
        }
        
        

    }
}
