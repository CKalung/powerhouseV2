using System;

namespace PushMessagingExample
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Hello World!");

			RestService rs = new RestService ();

			rs.httpUri = "https://www.qiosku.com/tes.php";
			//rs.httpUri = "http://" + SandraHost + ":" + SandraPort.ToString() + "/switching-ppob-gateway-service/rest";

			//rs.canonicalPath = "/gateway/" + commandCode;
			rs.canonicalPath = "";
			//rs.authID = sandraAuthId;
			rs.method = "POST";
			//rs.method = "GET";
			rs.contentType = "application/json";
			//rs.userAuth = "dummy-01";
			//rs.secretKey = "dummy-01";

			rs.bodyMessage = "{ \"tes\": 123 }";

//			LogWriter.write(this, LogWriter.logCodeEnum.INFO, 
//				"Send to: " + rs.httpUri + "\r\n"
//				+ "Canonical: " + rs.canonicalPath + "\r\n"
//				+ "AuthId: " + rs.authID + "\r\n"
//				+ "Method: " + rs.method + "\r\n"
//				+ "Body" + rs.bodyMessage
//			);


			string res = "";
//			if(useTcp)
//				res = rs.TCPRestSendRequest(recTimeOut);
//			else
			res = rs.HttpRestSendRequest(1200);

			if ((res == "") && ((rs.Response.httpMessage == null) || (rs.Response.httpCode == 0)))
			{
				Console.WriteLine( "Z" + "999");
				return;
			}
			Console.WriteLine ("Ayaan: " + res);

		}
	}
}
