

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPOBHttpRestData;
using LOG_Handler;
using StaticCommonLibrary;

namespace Process_MPAccountAccess
{
	public class LoginV2: IDisposable {
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
		~LoginV2()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			if (jsonConv != null)
				jsonConv.Dispose ();
			jsonConv = null;
			if (localDB != null)
				localDB.Dispose ();
			localDB = null;
		}

		/******************************************************************************/
		HTTPRestConstructor.HttpRestRequest clientData;
		PublicSettings.Settings commonSettings;
		JsonLibs.MyJsonLib jsonConv;
		HTTPRestConstructor HTTPRestDataConstruct;
		PPOBDatabase.PPOBdbLibs localDB;

		string cUserIDHeader="";

		public string securityToken = "";


		#region Kumpulan fungsi standar

		/*============================================================================*/
		/*   Kumpulan fungsi standar */
		/*============================================================================*/

		private void ReformatPhoneNumber(ref string phone)
		{
			phone = phone.Trim ().Replace(" ","").Replace("'","").Replace("-","");
			if (phone.Length < 2)
			{
				phone = "";
				return;
			}
			if (phone[0] == '0')
			{
				if (phone[1] == '6')
					phone = phone.Substring(1);
				else
					phone = "62" + phone.Substring(1);
			}
			else if (phone[0] == '+') phone = phone.Substring(1);
		}

		private bool checkMandatoryFields(JsonLibs.MyJsonLib sJson, string[] mandatoryFields)
		{
			foreach (string aField in mandatoryFields)
			{
				if (!sJson.ContainsKey(aField))
				{
					return false;
				}
			}
			return true;
		}

		private bool cek_SecurityToken(string userPhone, string token)
		{
			// cek detek sessionnya
			return CommonLibrary.isSessionExist (userPhone, token);
		}

		/*============================================================================*/
		/*============================================================================*/

		#endregion


		public LoginV2(HTTPRestConstructor.HttpRestRequest ClientData,
			PublicSettings.Settings CommonSettings)
		{
			clientData = ClientData;
			commonSettings = CommonSettings;
			cUserIDHeader = commonSettings.getString("UserIdHeader");

			HTTPRestDataConstruct = new HTTPRestConstructor();
			jsonConv = new JsonLibs.MyJsonLib();
			localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
				commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
		}


		public string InitialLogin(){

			string[] fields = { "fiApplicationId", "fiUser", "fiVersion" };

			string appID = "";
			string VirtualId = "";	// Sementara untuk ArtaJasa pake user "TesAJa" case sensitif
			string version = "";

			if (!jsonConv.JSONParse (clientData.Body)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data format", "");
			}

			if (!checkMandatoryFields (jsonConv, fields)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "406", "Mandatory fields not found", "");
			}

			try {
				appID = ((string)jsonConv ["fiApplicationId"]).Trim ();
				VirtualId = ((string)jsonConv ["fiUser"]).Trim ();
				version = ((string)jsonConv ["fiVersion"]).Trim ();
			} catch {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "408", "Invalid field type or format", "");
			}

			// GetRealUser(VirtualId)
			if (VirtualId != "TesAJa") {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "505", "Account not registered", "");
			}

			if (version != "1.2.0") {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "505", "Invalid version protocol", "");
			}

			//string randomChallenge = CommonLibrary.generateToken (64);
			string AJPhone = "628120000000001";
			string userId = "999" + AJPhone;

			// masih 1 login saja
			string randomChallenge = CommonLibrary.GenerateRandomChallenge(AJPhone, userId, clientData.ClientHost);


			jsonConv.Clear ();
			jsonConv.Add ("fiResponseCode", "00");
			jsonConv.Add ("fiRandomChallenge", randomChallenge);

			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct ());
		}

		public string DoLogin(){
			string[] fields = { "fiApplicationId", "fiUser", "fiVersion", "fiChallenge" };

			string appID = "";
			string VirtualId = "";	// Sementara untuk ArtaJasa pake user "TesAJa" case sensitif
			string version = "";
			string challenge = "";

			if (!jsonConv.JSONParse (clientData.Body)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data format", "");
			}

			if (!checkMandatoryFields (jsonConv, fields)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "406", "Mandatory fields not found", "");
			}

			try {
				appID = ((string)jsonConv ["fiApplicationId"]).Trim ();
				VirtualId = ((string)jsonConv ["fiUser"]).Trim ();
				version = ((string)jsonConv ["fiVersion"]).Trim ();
				challenge  = ((string)jsonConv ["fiChallenge"]).Trim ().ToUpper ();
			} catch {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "408", "Invalid field type or format", "");
			}

			// GetRealUser(VirtualId)
			if (VirtualId != "TesAJa") {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "505", "Account not registered", "");
			}

			if (version != "1.2.0") {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "505", "Invalid version protocol", "");
			}

			// Konversi userphone ke virtual Id
			string AJPhone = "628120000000001";
			string userId = "999" + AJPhone;

			string storedRandom = CommonLibrary.getStoredRandomChallenge (AJPhone);
			if (storedRandom == "") {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "504", "Invalid security", "");
			}

			// Password Aa12456, SHA2 = c4318372f98f4c46ed3a32c16ee4d7a76c832886d887631c0294b3314f34edf1
			string SHA2Passw = "c4318372f98f4c46ed3a32c16ee4d7a76c832886d887631c0294b3314f34edf1";
			string kalkChallenge = CommonLibrary.GetMd5Hash (storedRandom + SHA2Passw.ToUpper ());
			if (!CommonLibrary.StringCompare (challenge, kalkChallenge)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "504", "Invalid security", "");
			}

			// hitung Session Key dengan metoda ambil bytenya, bukan stringnya
			// session key = First24Byte(3DESDecrypt(RandomChallenge, First24byte(SHA2(password))))

			string token = CommonLibrary.AddSessionItem(AJPhone, userId, clientData.ClientHost);

			jsonConv.Clear ();
			jsonConv.Add ("fiResponseCode", "00");
			jsonConv.Add ("fiToken", token);

			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct ());
		}

	}
}
