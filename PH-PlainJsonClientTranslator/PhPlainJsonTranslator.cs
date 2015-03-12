using System;

using JsonLibs;
using PHClientProtocolTranslatorInterface;

namespace PHPlainJsonClientTranslator
{
	public class PhPlainJsonTranslator :  IPhClientTranslator, IDisposable
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
		~PhPlainJsonTranslator()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
		}

		const string header1= "POST ";
		string canonicalPath="/MultiPayment-Service/product/21004";
		const string header1a= " HTTP/1.1\r\nDate: ";
		string header2Date = "Wed, 25 Feb 2015, 03:46:25 GMT+00:00";
		const string header3 = "\r\nAuthorization: DA01 dummy-01:Avy/VzZYHg8TwTAg+fVVaJQ2X/0=\r\n";
		const string header4 = "Content-Type: application/json\r\n";
		const string header5 = "Host: powerhouse.network:";	// port number server 
		string header5PortNumber = "13939";
		const string header5a = "\r\n";
		const string header6 = "Content-Length: ";	//400 desimal
		string header6ContentLength = "400";
		const string header6a = "\r\n";
		const string header7 = "\r\n";

		const string responseCode = "responseCode:";
		const string responseMessage = "responseMessage:";

		public PhPlainJsonTranslator ()
		{
		}


		int parseReturn = 0;
		enum rParseCode { Completed = 0, Uncompleted = 1, Invalid = 2 }
		public int retParseCode{
			get { return parseReturn; }
			set { parseReturn = value; }
		}

		PPOBDatabase.PPOBdbLibs localDb = null;
		public PPOBDatabase.PPOBdbLibs DbConnect { 
			get { return localDb; }
			set { localDb = value; }
		}

//		private DateTime parseDateHeader(string sDate){
//			//"Wed, 25 Feb 2015, 03:46:25 GMT+00:00"
////			var date = DateTime.ParseExact(
////				"Mon Jan 13 2014 00:00:00 GMT+0000 (GMT Standard Time)",
////				"ddd MMM dd yyyy HH:mm:ss 'GMT'K '(GMT Standard Time)'",
////				CultureInfo.InvariantCulture);
//			var date = DateTime.ParseExact(
//				"Wed, 25 Feb 2015, 03:46:25 GMT+00:00",
//				"ddd MMM dd yyyy HH:mm:ss 'GMT'K '(GMT Standard Time)'",
//				CultureInfo.InvariantCulture);
//		}

		private string getHeaderCurrentGmtDate{
			get{
				//                                     "Wed, 25 Feb 2015, 03:46:25 GMT+00:00"
				return DateTimeOffset.UtcNow.ToString ("ddd, dd MMM yyyy, HH:mm:ss 'GMT'%K");
			}
		}

		private string constructHttpMessageFromDataClient (string CanonicalPath, string contentBody){
			header2Date = getHeaderCurrentGmtDate;
			header6ContentLength = contentBody.Trim ().Length.ToString ();
			string httpMsg = header1 + CanonicalPath + header1a + header2Date 
				+ header3 + header4 + header5 + header5PortNumber + header5a + header6
				+ header6ContentLength + header6a + header7;
			httpMsg += contentBody;
			return httpMsg;
		}

		public string TranslateFromClient (string data){
			Console.WriteLine ("=== TranslateClient : " + data);
			string body = "";
			canonicalPath = "";
			string fiUser = "";
			string fiPhone = "";
			Exception xError = null;

			if (data.Length == 0) {
				retParseCode = (int)rParseCode.Uncompleted;	//incomplete
				return "";
			}
			if (data [0] != '{') {
				retParseCode = (int)rParseCode.Invalid;	//incomplete
				return data;
			}

			using (MyJsonLib json = new MyJsonLib ()) {
				if (!json.JSONParse (data)) {
					retParseCode = (int)rParseCode.Uncompleted;	//incomplete
					return data;
				}
				if (json.isExists ("CanonicalPath")) 
					canonicalPath = ((string)json ["CanonicalPath"]).Trim ();
				json.Remove ("CanonicalPath");

				// ubah fiUser menjadi fiPhone
				if (json.isExists ("fiUser")) {
					fiUser = ((string)json ["fiUser"]).Trim ();
					//json.Remove ("fiUser");
					fiPhone = localDb.getUserPhoneFromAlias (fiUser, out xError);

					//json.Add ("XUser", fiUser);
					json.Add ("fiPhone", fiPhone);
					//json.Add ("fiUser", fiPhone);
				} else if (json.isExists ("fiPhone")) {
					fiPhone = ((string)json ["fiPhone"]).Trim ();
					json.Add ("fiUser", fiPhone);
				}

				body = json.JSONConstruct ();
			}
			retParseCode = (int)rParseCode.Completed;	//completed
			string hasil = constructHttpMessageFromDataClient(canonicalPath,body);
			Console.WriteLine ("=== Translate To Server : " + hasil);
			return hasil;
		}

		public string TranslateToClient (string data){
			Console.WriteLine ("=== TranslateServer : " + data);
			char[] splt = { '\n' };
			string[] lines = data.Split (splt, 15, StringSplitOptions.None);
			string retCode = "";
			string retMsg = "";
			string jsonBody = "";
			bool isFirstLine = true;
			string aLine = "";

			char[] splt2 = { ':' };
			string[] lines2 ;

			// dapetin httpResponseCode dan httpResponseMessage
			for (int i =0;i<lines.Length;i++) {
				aLine = lines [i].Trim ();
				if ((aLine.Length == 0) && (isFirstLine))
					continue;
				if (!isFirstLine && aLine.Length == 0)	// jika berikutnya itu contentBody
					break;
				if ((aLine.Length > 0) && (isFirstLine))
					isFirstLine = false;
				if (aLine.StartsWith (responseCode)) {
					//ambil responseCode
					lines2 = aLine.Split (splt2, StringSplitOptions.None);
					retCode = lines2 [1].Trim ();
				}
				if (aLine.StartsWith (responseMessage)) {
					//ambil responseCode
					lines2 = aLine.Split (splt2, StringSplitOptions.None);
					retMsg = lines2 [1].Trim ();
				}
			}

			if (retCode == "00") {
				// dapetin json body
				isFirstLine = true;
				for (int i = 0; i < data.Length; i++) {
					if ((data [i] == '\n') && (data.Length > (i + 4)) && (data [i + 2] == '\n')) {
						jsonBody = data.Substring (i + 3);
						break;
					}
				}
			}

			string hasil = "";
			using (MyJsonLib json = new MyJsonLib ()) {
				if (jsonBody.Length > 0) 
					json.JSONParse (jsonBody);
				if (!json.isExists ("fiResponseCode")) {
					json.Add ("fiResponseCode", retCode);
					json.Add ("fiResponseMessage", retMsg);
				}
				if(!json.isExists ("fiResponseMessage"))
					json.Add ("fiResponseMessage", retMsg);
				hasil = json.JSONConstruct ();
			}

			Console.WriteLine ("=== Translate To Client : " + hasil);

			return hasil;
		}

	}
}

