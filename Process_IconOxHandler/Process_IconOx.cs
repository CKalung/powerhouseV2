using System;
using PPOBHttpRestData;

namespace Process_IconOxHandler
{
	public class Process_IconOx: IDisposable
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
		~Process_IconOx()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			jsonConv.Clear();
			jsonConv.Dispose();
			HTTPRestDataConstruct.Dispose();
			localDB.Dispose();
		}

		static SmartCardReaderClient iconox = new SmartCardReaderClient (125, false);
		PublicSettings.Settings commonSettings;
		JsonLibs.MyJsonLib jsonConv;
		HTTPRestConstructor HTTPRestDataConstruct;
		PPOBDatabase.PPOBdbLibs localDB;
		Exception xError;

		public Process_IconOx (PublicSettings.Settings CommonSettings)
		{
			commonSettings = CommonSettings;
			HTTPRestDataConstruct = new HTTPRestConstructor();
			jsonConv = new JsonLibs.MyJsonLib();
			localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
				commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
		}

		private void ReformatPhoneNumber(ref string phone)
		{
			phone = phone.Trim ().Replace(" ","").Replace("'","").Replace("-","");
			if (phone.Length < 2) {
				phone = "";
				return;
			}
			if (phone [0] == '0') {
				if(phone[1] == '6')
					phone = phone.Substring (1);
				else
					phone = "62" + phone.Substring (1);
			}
			else if (phone[0] == '+') phone = phone.Substring(1);
		}

		private byte[] SAMRequestChallenge(byte[] usercard_challenge)
		{
			// send data ke server smartcard SAM
			if (usercard_challenge.Length != 121) {
				return null;
			}
			byte[][] res = new byte[2][];
			byte[] apdu = new byte[5 + 6 + 121];
			apdu[0] = 0x90;
			apdu[1] = 0x84;
			apdu[2] = 0x68;
			apdu[3] = 0x01;
			apdu[4] = 0x7F;
			byte[] key_address = new byte[] { 0x00, 0x01, 0x10, 0x01, 0x00, 0x00 };
			Array.Copy(key_address, 0, apdu, 5, 6);
			Array.Copy(usercard_challenge,0,apdu,11,121);

				res = sendAPDU(apdu, 0);

			byte[] st = new byte[] { 0x61, 0x58 };
			bool areEqual = st.SequenceEqual(res[0]);
			if (!areEqual)
			{
				return res[0];
			}                        

			// ambil datanya pakae 00C0000058
			byte[][] res1 = new byte[2][];
			byte[] apdu1 = new byte[5];
			apdu1[0] = 0x00;
			apdu1[1] = 0xC0;
			apdu1[2] = 0x00;
			apdu1[3] = 0x00;
			apdu1[4] = res[0][1];

				res1 = sendAPDU(apdu1, 1);

			byte[] st1 = new byte[] { 0x90, 0x00 };
			bool areEqual1 = st1.SequenceEqual(res1[0]);
			if (!areEqual1)
			{
				return res1[0];
			}

			return res1[1];

		}

		private string IconoxTopUpRequest(HTTPRestConstructor.HttpRestRequest clientData)
		{
			if (clientData.Body.Length == 0)
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
			}
			if (!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}
			// TAMPUNG data json dari Client
			string fiApplicationID = "";
			string fiPhone = "";
			string fiProductCode = "";
			int fiValue = "";
			string fiTrxDateTime = "";
			string fiCSN = "";
			string fiCardNumber = "";
			string fiCardChallenge = "";
			string fiPassword = "";

			try
			{
				fiApplicationID = ((string)jsonConv["fiApplicationId"]).Trim();
				fiPhone = ((string)jsonConv["fiPhone"]).Trim();
				fiProductCode = ((string)jsonConv["fiProductCode"]).Trim();
				fiValue = ((int)jsonConv["fiValue"]);
				fiTrxDateTime = ((string)jsonConv["fiTrxDateTime"]).Trim();
				fiCSN = ((string)jsonConv["fiCSN"]).Trim();
				fiCardNumber = ((string)jsonConv["fiCardNumber"]).Trim();
				fiCardChallenge = ((string)jsonConv["fiCardChallenge"]).Trim();
				fiPassword = ((string)jsonConv["fiPassword"]).Trim();
			}
			catch
			{
				// field tidak ditemukan atau formatnya salah
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not completed", "");
			}
			ReformatPhoneNumber (ref fiPhone);
			if (fiPhone.Length == 0)
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid phone number", "");
			}

			// cek dengan database, apakah password sama?
			if (!localDB.isUserPasswordEqual(fiPhone, fiPassword, out xError))
			{
				if (xError != null)
				{
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
				}
				else
				{
					// password error
					return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Login failed", "");
				}
			}
			// password ok

			// cek Balance agen, apakah cukup untuk topup?


			// reply sukses
			jsonConv.Clear();
			jsonConv.Add("fiTopUpData", fiTopUpData);

			// kirim respon ke client
			string repl = jsonConv.JSONConstruct();
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", repl);
		}


		public string Process(int reqPathCode, HTTPRestConstructor.HttpRestRequest clientData)
		{
			if (reqPathCode == commonSettings.getInt("CommandProductIconoxEWalletTopUp"))
			{
				return IconoxTopUpRequest(clientData);
			}
//			else if (reqPathCode == 123456)
//			{
//				// AccountLastTransactionHistory
//				// Transfer dari dan ke semua rekening oleh Root
//				using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
//				{
//					return AccTransac.TestQvaConnection(clientData);
//				}
//			}
			else
			{
				// reject
				return HTTPRestDataConstruct.constructHTTPRestResponse(200, "204", "Not implemented", "");
			}
		}


	}
}

