using System;
using System.Collections.Generic;
using System.Text;

namespace PPOBHttpRestData
{
    // contoh data query
    //POST /hms/rest/requestToken HTTP/1.1
    //Host: 123.231.225.20:7080
    //Date: Mon, 21 Oct 2013 07:35:23 GMT
    //Authorization: DA01 dummy-01:K8pnapEfA2kjkQdl0DEXAvK8CTE=
    //Content-Type: application/json
    //Content-Length: 44
    //
    //{"sourceHostId":"switching-gateway-service"}

    // Contoh data respon
    //HTTP/1.1 200 OK
    //X-Powered-By: Servlet/3.0 JSP/2.2 (GlassFish Server Open Source Edition 3.1.2 Java/Oracle Corporation/1.7)
    //Server: GlassFish Server Open Source Edition 3.1.2
    //responseCode: 00
    //responseMessage: Success
    //Content-Type: application/json
    //Content-Length: 1108
    //Date: Mon, 21 Oct 2013 07:35:24 GMT
    //
    //{"ficoCustomerRef3":"-","ficoCustomerAddress3":"-","ficoCustomerRef2":"-","ficoCustomerCity":"Bandung","ficoCustomerRef1":"-","ficoCustomerIdentityCardNumber":"0000000000000777","ficoCustomerAddress2":"-","ficoCustomerEmail":"cacingkalung@yahoo.com","ficoCustomerMotherName":"Ema","ficoCustomerCardIdentityType":"KTP","ficoCustomerZipCode":"-","ficoCustomerBirthDate":"07-07-2008","ficoCustomerPhone":"-","ficoCustomerNickname":"cacingkalung","ficoCustomerTrxAllowed":"CD","ficoCustomerPassword":"7ae120da4497a68a8fbc09c66dfbda05","ficoCustomerGender":"M","ficoCustomerCardNumber":"-","ficoCustomerCustomField1":"-","ficoCustomerCustomField3":"-","ficoCustomerId":"0000000000000777","ficoCustomerCustomField2":"-","ficoCustomerUsername":"cacingkalung","ficoCustomerAddress":"Bekasi","ficoCustomerCustomField5":"-","ficoCustomerCustomField4":"-","ficoCustomerPhone2":"-","source":"","ficoCustomerIdentityCardValidDate":"01-01-2020","ficoCustomerCity2":"Bekasi","ficoCustomerZipCode2":"-","ficoCustomerGroupId":"91111","ficoCustomerNpwp":"-","ficoCustomerBirthPlace":"Bekasi","ficoCustomerName":"CacingKalung"}


    public class HTTPRestConstructor: IDisposable
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
            clientRequest.Authorization = "";
            clientRequest.Body = "";
            clientRequest.CanonicalPath = "";
            clientRequest.ContentLen = 0;
            clientRequest.ContentType = "";
            clientRequest.Date = "";
            clientRequest.Host = "";
            clientRequest.Method = enMethod.Unknown;
        }
        ~HTTPRestConstructor()
        {
            this.Dispose(false);
        }
        #endregion

        const string HTTPStart = "HTTP/1.1 ";
		const string X_Powered = "X-Powered-By: PowerHouse-Server/2.5 .NET/4.5\r\n";
		const string X_HttpServer = "Server: PowerHouse-HTTPREST Server DAM\r\n";
        const string strResCode = "responseCode: ";
        const string strResMsg = "responseMessage: ";
		const string contentTypeJson = "Content-Type: application/json\r\n";
		const string contentTypeXml = "Content-Type: application/xml\r\n";
		const string contentTypeAny = "Content-Type: application\r\n";
        const string contentLen = "Content-Length: ";
        const string strDate = "Date: ";
        string HTTPRestMsg = "";

        //List<string> msgReq = new List<string>();

        public enum retParseCode { Completed = 0, Uncompleted = 1, Invalid = 2 }
        public enum enMethod { POST, GET, Unknown }
        public struct HttpRestRequest
        {
			public string ClientHost;
            public enMethod Method;
            public string CanonicalPath;
            public string Host;
            public string Date;
            public string Authorization;
            public string ContentType;
            public int ContentLen;
            public string Body;
        }

        HttpRestRequest clientRequest = new HttpRestRequest();

        public HttpRestRequest HttpRestClientRequest
        {
            get { return clientRequest; }
        }

		public bool parseClientRequest(string httpRestMsg, string clientHost, ref retParseCode retCode)
        {
			clientRequest.ClientHost = clientHost;
            clientRequest.Authorization="";
            clientRequest.Body="";
            clientRequest.CanonicalPath="";
            clientRequest.ContentLen=0;
            clientRequest.ContentType="";
            clientRequest.Date="";
            clientRequest.Host="";
            clientRequest.Method= enMethod.Unknown;
            retCode = retParseCode.Uncompleted;

            //msgReq.Clear();
            // parsing percharacter biar cepat
            string line = "";
            //string body = "";
            bool seekBody = false;
            bool completed = false;
            bool firstLine = true;
            for (int i = 0; i < httpRestMsg.Length; i++)
            {
                if ((httpRestMsg[i] != '\r') && (httpRestMsg[i] != '\n'))
                {
                    line += httpRestMsg[i];
                    continue;
                }
                else if (httpRestMsg[i] == '\n') continue;
                //else if (httpRestMsg[i] == '\r')
                else   // berarti disini == '\r'
                {
                    // definisikeun line
                    // tah satu baris
                    //msgReq.Add(line);
                    if (seekBody && (line.Length == 0))
                    {
                        // berarti sisanya adalah body http
                        if ((i + 2) < httpRestMsg.Length)
                        {
                            clientRequest.Body = httpRestMsg.Substring(i + 2);
                            //body = httpRestMsg.Substring(i + 2);
                            completed = true;
                            break;
                        }
                        else if ((i + 2) == httpRestMsg.Length)
                        {
                            //clientRequest.Body = httpRestMsg.Substring(i + 2);
                            completed = true;
                            break;
                        }
                        else
                        {
                            // blm beres terimanya
                            return false;
                        }
                    }

                    if ((firstLine) && (line.StartsWith("POST ") || line.StartsWith("GET ")))
                    {
                        firstLine = false;
                        seekBody = true;
                        // ambil method nya dan canonical path
                        if (line[0] == 'P')
                        {
                            clientRequest.Method = enMethod.POST;
                            for (int j = 5; j < line.Length; j++)
                            {
                                if ((line[j] != ' ') && (line[j] != '\r') && (line[j] != '\n')) clientRequest.CanonicalPath += line[j];
                                else break;
                            }
                        }
                        else clientRequest.Method = enMethod.GET;  // perlunya cuman POST doang, yg get mah ignored
                    }
                    else if (firstLine)
                    {
                        clientRequest.Method = enMethod.Unknown;
                        firstLine = false;
                        retCode = retParseCode.Invalid;
                        return false;
                    }
                    else if (line.Length != 0)
                    {
                        if (firstLine)
                        {
                            retCode = retParseCode.Invalid;
                            return false;
                        }
                        // parse per header
                        //Host: 123.231.225.20:7080
                        //Date: Mon, 21 Oct 2013 07:35:23 GMT
                        //Authorization: DA01 dummy-01:K8pnapEfA2kjkQdl0DEXAvK8CTE=
                        //Content-Type: application/json
                        //Content-Length: 44
                        // spy mempercepat pembandingan, dibuat spt dibawah ini
                        if ((clientRequest.Host.Length == 0) && (line.StartsWith("Host: "))) clientRequest.Host = line.Substring(6);
                        else if ((clientRequest.Date.Length == 0) && (line.StartsWith("Date: "))) clientRequest.Date = line.Substring(6);
                        else if ((clientRequest.ContentType.Length == 0) && (line.StartsWith("Content-Type: "))) clientRequest.ContentType = line.Substring(14);
                        else if ((clientRequest.Authorization.Length == 0) && (line.StartsWith("Authorization: "))) clientRequest.Authorization = line.Substring(15);
                        else if (line.StartsWith("Content-Length: "))
                        {
                            try { clientRequest.ContentLen = int.Parse(line.Substring(16)); }
                            catch
                            {
                                retCode = retParseCode.Invalid;
                                return false;
                            }
                        }
                        // selain yg udah ditentukan, abaikan header lainnya
                        // ....
                    }
                    line = "";
                }

                //lanjut parsing
            }
            if (!seekBody)
            {
                retCode = retParseCode.Invalid;
                return false;
            }
            if (!completed) return false;

            // cek contentlen bandingkan dengan body
            if (clientRequest.ContentLen == clientRequest.Body.Length)
            {
                // lengkap sudah
                retCode = retParseCode.Completed;
                return true;
            }
            if (clientRequest.ContentLen < clientRequest.Body.Length)
            {
                // jika panjang data melebihi panjang seharusnya apakah di sebut completed atau invalid???
                retCode = retParseCode.Completed;       // gini aja dulu, tapi potong sisanya

				//disini juga decrypt secured body

                clientRequest.Body = clientRequest.Body.Substring(0, clientRequest.ContentLen);
                return true;
            }
            else return false;
        }

		public enum httpBodyType
		{
			Others = 0,
			JSON = 1,
			XML = 2
		}
		public string constructHTTPRestResponse(int httpcode, string respCode, string respmessage, string contentBody, httpBodyType isJSONBodyType = httpBodyType.JSON)
        {
            HTTPRestMsg = HTTPStart + httpcode + " ";
            switch (httpcode)
            {
                case 100: HTTPRestMsg += "Continue\r\n"; break;
                case 200: HTTPRestMsg += "OK\r\n"; break;
                case 201: HTTPRestMsg += "Created\r\n"; break;
                case 202: HTTPRestMsg += "Accepted\r\n"; break;
                    // jika gak diterima ama server host
                case 203: HTTPRestMsg += "Non-Authoritative Information\r\n"; break;
                    // jika data dari server bersambung
                case 206: HTTPRestMsg += "Partial Content\r\n"; break;
                    // jika requestnya ngaco
                case 400: HTTPRestMsg += "Bad Request\r\n"; break;
                    // jika akses tanpa otentikasi
                case 401: HTTPRestMsg += "Unauthorized\r\n"; break;
                    // jika blm bayar
                case 402: HTTPRestMsg += "Payment Required\r\n"; break;
                    // jika akses path yg forbidden
                case 403: HTTPRestMsg += "Forbidden\r\n"; break;
                    // jika path gak sesuai
                case 404: HTTPRestMsg += "Not Found\r\n"; break;
                    // jika method gak pake POST
                case 405: HTTPRestMsg += "Method Not Allowed\r\n"; break;
                case 406: HTTPRestMsg += "Not Acceptable\r\n"; break;
                    // jika request belum lengkap dari client dan kena timeout
                case 408: HTTPRestMsg += "Request Timeout\r\n"; break;
                case 409: HTTPRestMsg += "Conflict\r\n"; break;
                    // jika path sudah tidak ada
                case 410: HTTPRestMsg += "Gone\r\n"; break;
                    // jika butuh data dari client
                case 411: HTTPRestMsg += "Length Required\r\n"; break;
                    // jika request guede banget
                case 413: HTTPRestMsg += "Request Entity Too Large\r\n"; break;
                    // Jika header Expect: dari request gak bisa di penuhi
                case 417: HTTPRestMsg += "Expectation Failed\r\n"; break;
                    // jika total panjang header terlalu panjang
                case 431: HTTPRestMsg += "Request Header Fields Too Large\r\n"; break;
                case 500: HTTPRestMsg += "Internal Server Error\r\n"; break;
                case 501: HTTPRestMsg += "Not Implemented\r\n"; break;
                case 502: HTTPRestMsg += "Bad Gateway\r\n"; break;
                    // jika system sedang maintenance
                case 503: HTTPRestMsg += "Service Unavailable\r\n"; break;
                    // jika data dari host timeout
                case 504: HTTPRestMsg += "Gateway Timeout\r\n"; break;
                    // Jika versi http selain 1.1
                case 505: HTTPRestMsg += "HTTP Version Not Supported\r\n"; break;
                default: HTTPRestMsg += "Bad Request\r\n"; break;
            }
            HTTPRestMsg += X_Powered;
			HTTPRestMsg += X_HttpServer;
            HTTPRestMsg += strResCode+ respCode.Trim() + "\r\n";
            HTTPRestMsg += strResMsg + respmessage.Trim() + "\r\n";
			if (contentBody.Length != 0)
            {
				if(isJSONBodyType == httpBodyType.JSON)
                	HTTPRestMsg += contentTypeJson;
				else if(isJSONBodyType == httpBodyType.XML)
					HTTPRestMsg += contentTypeXml;
				else
					HTTPRestMsg += contentTypeAny;

				HTTPRestMsg += contentLen + contentBody.Length.ToString() + "\r\n";
            }
            HTTPRestMsg += strDate + DateTime.Now.ToString("r") + "\r\n";
            HTTPRestMsg += "\r\n";  // akhir dari header, siap ke body message

			//Disini disiapkan encrypt untuk secured Body message

			if (contentBody.Length != 0) HTTPRestMsg += contentBody;

            return HTTPRestMsg;
        }

    }
}
