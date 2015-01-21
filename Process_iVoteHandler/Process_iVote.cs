using System;
using PPOBHttpRestData;
using System.Collections.Generic;

namespace Process_iVoteHandler
{
	public class Process_iVote: IDisposable
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
		~Process_iVote()
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

		PublicSettings.Settings commonSettings;
		JsonLibs.MyJsonLib jsonConv;
		HTTPRestConstructor HTTPRestDataConstruct;
		PPOBDatabase.PPOBdbLibs localDB;
		Exception xError;

		public Process_iVote (PublicSettings.Settings CommonSettings)
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


		public string QueryProvince(HTTPRestConstructor.HttpRestRequest clientData)
		{
			// input dari user diambil dari username dan password, baru bisa inquiry ke host
			if (!jsonConv.JSONParse (clientData.Body)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data format", "");
			}

//			if ((!jsonConv.ContainsKey ("fiUserPhone")) || (!jsonConv.ContainsKey ("fiPassword"))) {
//				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "406", "Mandatory fields no found", "");
//			}

			//string userPhone;
			//string passw;
//			try {
//				userPhone = ((string)jsonConv ["fiUserPhone"]).Trim ();
//				//	passw = ((string)jsonConv ["fiPassword"]).Trim ();
//			} catch {
//				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data of one or more fields", "");
//			}

			//ReformatPhoneNumber (ref userPhone);

//			if (!localDB.isUserPasswordEqual (userPhone, passw, out xError)) {
//				if (xError != null) {
//					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Server database error", "");
//				} else {
//					// password error
//					return HTTPRestDataConstruct.constructHTTPRestResponse (401, "401", "Invalid Phone or password", "");
//				}
//			}
			// password ok

			JsonLibs.MyJsonArray arProp = new JsonLibs.MyJsonArray ();
			arProp.Name = "Propinsi";
			// Ambil query propinsi dari database
			List<PPOBDatabase.PPOBdbLibs.IdNameStruct> propList = localDB.getProvince(out xError);

			if (propList == null) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "493", "No province data", "");
			}

			for (int j = 0; j < propList.Count; j++)
			{
				JsonLibs.MyJsonLib prop = new JsonLibs.MyJsonLib ();
				prop.Add ("id", propList[j].id);
				prop.Add ("nama", propList[j].name);
				arProp.Add (prop);
			}

			JsonLibs.MyJsonLib jsr = new JsonLibs.MyJsonLib ();
			jsr.Add ("Propinsi", arProp);

			string repl = jsr.JSONConstruct();
			return HTTPRestDataConstruct.constructHTTPRestResponse (200, "00", "Success", repl);
		}

		public string QueryKabupatenKota(HTTPRestConstructor.HttpRestRequest clientData)
		{
			// input dari user diambil dari username dan password, baru bisa inquiry ke host
			if (!jsonConv.JSONParse (clientData.Body)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data format", "");
			}

			if (//(!jsonConv.ContainsKey ("fiUserPhone")) 
				//|| (!jsonConv.ContainsKey ("fiPassword"))
				//|| 
				(!jsonConv.ContainsKey("fiIdProvinsi"))) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "406", "Mandatory fields not found", "");
			}

			//string userPhone;
			//string passw;
			string IdProvinsi; 
			try {
				//userPhone = ((string)jsonConv ["fiUserPhone"]).Trim ();
				//passw = ((string)jsonConv ["fiPassword"]).Trim ();
				IdProvinsi = ((string)jsonConv ["fiIdProvinsi"]).Trim ();
			} catch {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data of one or more fields", "");
			}

			//ReformatPhoneNumber (ref userPhone);

//			if (!localDB.isUserPasswordEqual (userPhone, passw, out xError)) {
//				if (xError != null) {
//					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Server database error", "");
//				} else {
//					// password error
//					return HTTPRestDataConstruct.constructHTTPRestResponse (401, "401", "Invalid Phone or password", "");
//				}
//			}
			// password ok

			JsonLibs.MyJsonArray arKabKot = new JsonLibs.MyJsonArray ();
			arKabKot.Name = "KabupatenKota";
			// Ambil query propinsi dari database
			List<PPOBDatabase.PPOBdbLibs.IdNameStruct> kabKotList = localDB.getKabupatenKota(IdProvinsi, out xError);

			if (kabKotList == null) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "493", "No kabupaten kota data", "");
			}

			for (int j = 0; j < kabKotList.Count; j++)
			{
				JsonLibs.MyJsonLib kabkot = new JsonLibs.MyJsonLib ();
				kabkot.Add ("id", kabKotList[j].id);
				kabkot.Add ("nama", kabKotList[j].name);
				arKabKot.Add (kabkot);
			}

			JsonLibs.MyJsonLib jsr = new JsonLibs.MyJsonLib ();
			jsr.Add ("KabKota", arKabKot);

			string repl = jsr.JSONConstruct();
			return HTTPRestDataConstruct.constructHTTPRestResponse (200, "00", "Success", repl);
		}

		public string QueryKecamatan(HTTPRestConstructor.HttpRestRequest clientData)
		{
			// input dari user diambil dari username dan password, baru bisa inquiry ke host
			if (!jsonConv.JSONParse (clientData.Body)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data format", "");
			}

			if (
				//(!jsonConv.ContainsKey ("fiUserPhone")) 
				//|| (!jsonConv.ContainsKey ("fiPassword"))
				//|| 
				(!jsonConv.ContainsKey("fiIdKabKota"))) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "406", "Mandatory fields not found", "");
			}

			//string userPhone;
			//string passw;
			string IdKabKota; 
			try {
				//userPhone = ((string)jsonConv ["fiUserPhone"]).Trim ();
				//passw = ((string)jsonConv ["fiPassword"]).Trim ();
				IdKabKota = ((string)jsonConv ["fiIdKabKota"]).Trim ();
			} catch {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data of one or more fields", "");
			}

			//ReformatPhoneNumber (ref userPhone);

//			if (!localDB.isUserPasswordEqual (userPhone, passw, out xError)) {
//				if (xError != null) {
//					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Server database error", "");
//				} else {
//					// password error
//					return HTTPRestDataConstruct.constructHTTPRestResponse (401, "401", "Invalid Phone or password", "");
//				}
//			}
			// password ok

			JsonLibs.MyJsonArray arKecamatan = new JsonLibs.MyJsonArray ();
			arKecamatan.Name = "Kecamatan";
			// Ambil query propinsi dari database
			List<PPOBDatabase.PPOBdbLibs.IdNameStruct> kecamatanList = localDB.getKecamatan(IdKabKota, out xError);

			if (kecamatanList == null) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "493", "No kecamatan data", "");
			}

			for (int j = 0; j < kecamatanList.Count; j++)
			{
				JsonLibs.MyJsonLib kecamatan = new JsonLibs.MyJsonLib ();
				kecamatan.Add ("id", kecamatanList[j].id);
				kecamatan.Add ("nama", kecamatanList[j].name);
				arKecamatan.Add (kecamatan);
			}

			JsonLibs.MyJsonLib jsr = new JsonLibs.MyJsonLib ();
			jsr.Add ("Kecamatan", arKecamatan);

			string repl = jsr.JSONConstruct();
			return HTTPRestDataConstruct.constructHTTPRestResponse (200, "00", "Success", repl);
		}

		public string QueryKelurahan(HTTPRestConstructor.HttpRestRequest clientData)
		{
			// input dari user diambil dari username dan password, baru bisa inquiry ke host
			if (!jsonConv.JSONParse (clientData.Body)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data format", "");
			}

			if (
				//(!jsonConv.ContainsKey ("fiUserPhone")) 
				//|| (!jsonConv.ContainsKey ("fiPassword"))
				//|| 
				(!jsonConv.ContainsKey("fiIdKecamatan"))) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "406", "Mandatory fields not found", "");
			}

			//string userPhone;
			//string passw;
			string IdKecamatan; 
			try {
				//userPhone = ((string)jsonConv ["fiUserPhone"]).Trim ();
				//passw = ((string)jsonConv ["fiPassword"]).Trim ();
				IdKecamatan = ((string)jsonConv ["fiIdKecamatan"]).Trim ();
			} catch {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data of one or more fields", "");
			}

			//ReformatPhoneNumber (ref userPhone);

//			if (!localDB.isUserPasswordEqual (userPhone, passw, out xError)) {
//				if (xError != null) {
//					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Server database error", "");
//				} else {
//					// password error
//					return HTTPRestDataConstruct.constructHTTPRestResponse (401, "401", "Invalid Phone or password", "");
//				}
//			}
			// password ok

			JsonLibs.MyJsonArray arKelurahan = new JsonLibs.MyJsonArray ();
			arKelurahan.Name = "Kelurahan";
			// Ambil query propinsi dari database
			List<PPOBDatabase.PPOBdbLibs.IdNameStruct> kelurahanList = localDB.getKelurahan(IdKecamatan, out xError);

			if (kelurahanList == null) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "493", "No kelurahan data", "");
			}

			for (int j = 0; j < kelurahanList.Count; j++)
			{
				JsonLibs.MyJsonLib kelurahan = new JsonLibs.MyJsonLib ();
				kelurahan.Add ("id", kelurahanList[j].id);
				kelurahan.Add ("nama", kelurahanList[j].name);
				arKelurahan.Add (kelurahan);
			}

			JsonLibs.MyJsonLib jsr = new JsonLibs.MyJsonLib ();
			jsr.Add ("Kelurahan", arKelurahan);

			string repl = jsr.JSONConstruct();
			return HTTPRestDataConstruct.constructHTTPRestResponse (200, "00", "Success", repl);
		}

		public string SendTpsVoteResult(HTTPRestConstructor.HttpRestRequest clientData)
		{
			// input dari user diambil dari username dan password, baru bisa inquiry ke host
			if (!jsonConv.JSONParse (clientData.Body)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data format", "");
			}

			if (
				(!jsonConv.ContainsKey ("fiUserPhone")) 
				|| (!jsonConv.ContainsKey ("fiPassword"))
				|| (!jsonConv.ContainsKey ("fiIdTps"))
				|| (!jsonConv.ContainsKey ("fiCount1"))
				|| (!jsonConv.ContainsKey ("fiCount2"))) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "406", "Mandatory fields not found", "");
			}

			string userPhone;
			string passw;
			string IdTps;
			int Vote1; 
			int Vote2; 
			try {
				userPhone = ((string)jsonConv ["fiUserPhone"]).Trim ();
				passw = ((string)jsonConv ["fiPassword"]).Trim ();
				IdTps = ((string)jsonConv ["fiIdTps"]).Trim ();
				Vote1 = (int)jsonConv ["fiCount1"];
				Vote2 = (int)jsonConv ["fiCount2"];
			} catch {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data of one or more fields", "");
			}

			if (IdTps.Length != 13) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid length of Id TPS", "");
			}

			ReformatPhoneNumber (ref userPhone);

			if (!localDB.isUserPasswordEqual (userPhone, passw, out xError)) {
				if (xError != null) {
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Server database error", "");
				} else {
					// password error
					return HTTPRestDataConstruct.constructHTTPRestResponse (401, "401", "Invalid Phone or password", "");
				}
			}
			// password ok

			// pilah2 kode area
			string propinsi = IdTps.Substring (0, 2);
			string kabkota = propinsi+IdTps.Substring (2, 2);
			string kecamatan = kabkota+IdTps.Substring (4, 3);
			string kelurahan = kecamatan+IdTps.Substring (7, 3);
			string tps = IdTps;  //IdTps.Substring (10);

			if (!localDB.insertVotes (userPhone, propinsi, kabkota, kecamatan, kelurahan, tps, Vote1, Vote2, out xError)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "494", "Failed to insert to database", "");
			}

//			JsonLibs.MyJsonLib jsr = new JsonLibs.MyJsonLib ();
//			jsr.Add ("Kelurahan", arKelurahan);
//
//			string repl = jsr.JSONConstruct();
			return HTTPRestDataConstruct.constructHTTPRestResponse (200, "00", "Success", "{}");
		}

		public string SendSurveyorData(HTTPRestConstructor.HttpRestRequest clientData)
		{
			if (!jsonConv.JSONParse (clientData.Body)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data format", "");
			}

			if (
				(!jsonConv.ContainsKey ("fiUserPhone")) 
				|| (!jsonConv.ContainsKey ("fiPassword"))
				|| (!jsonConv.ContainsKey ("fiIdArea"))) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "406", "Mandatory fields not found", "");
			}

			string userPhone;
			string passw;
			string IdArea;
			try {
				userPhone = ((string)jsonConv ["fiUserPhone"]).Trim ();
				passw = ((string)jsonConv ["fiPassword"]).Trim ();
				IdArea = ((string)jsonConv ["fiIdArea"]).Trim ();
			} catch {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data of one or more fields", "");
			}

			if (IdArea.Length != 10) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid length of Id Area", "");
			}

			ReformatPhoneNumber (ref userPhone);

			if (!localDB.isUserPasswordEqual (userPhone, passw, out xError)) {
				if (xError != null) {
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Server database error", "");
				} else {
					// password error
					return HTTPRestDataConstruct.constructHTTPRestResponse (401, "401", "Invalid Phone or password", "");
				}
			}
			// password ok

			// pilah2 kode area
			string propinsi = IdArea.Substring (0, 2);
			string kabkota = IdArea.Substring (0, 4);
			string kecamatan = IdArea.Substring (0, 7);
			string kelurahan = IdArea;

			if (!localDB.insertSurveyor (userPhone, propinsi, kabkota, kecamatan, kelurahan, out xError)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "494", "Failed to insert to database", "");
			}

//			JsonLibs.MyJsonLib jsr = new JsonLibs.MyJsonLib ();
//			jsr.Add ("Kelurahan", arKelurahan);
//
//			string repl = jsr.JSONConstruct();
			return HTTPRestDataConstruct.constructHTTPRestResponse (200, "00", "Success", "{}");
		}

		public string QueryTpsVoteResult(HTTPRestConstructor.HttpRestRequest clientData)
		{
			if (!jsonConv.JSONParse (clientData.Body)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data format", "");
			}

			if (
				(!jsonConv.ContainsKey ("fiUserPhone")) 
				|| (!jsonConv.ContainsKey ("fiIdTps"))) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "406", "Mandatory fields not found", "");
			}

			string userPhone;
			string IdTps;
			try {
				userPhone = ((string)jsonConv ["fiUserPhone"]).Trim ();
				IdTps = ((string)jsonConv ["fiIdTps"]).Trim ();
			} catch {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data of one or more fields", "");
			}

			if (IdTps.Length != 13) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid length of Id TPS", "");
			}

			ReformatPhoneNumber (ref userPhone);

			if (!localDB.isPhoneExistAndActive(userPhone, out xError)) {
				if (xError != null) {
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, "492", "Server database error", "");
				} else {
					// password error
					return HTTPRestDataConstruct.constructHTTPRestResponse (401, "402", "Phone number not registered", "");
				}
			}

			// pilah2 kode area
			string propinsi = IdTps.Substring (0, 2);
			string kabkota = propinsi+IdTps.Substring (2, 2);
			string kecamatan = kabkota+IdTps.Substring (4, 3);
			string kelurahan = kecamatan+IdTps.Substring (7, 3);
			string tps = IdTps;  //IdTps.Substring (10);

			int nasionalCount1=0; 
			int nasionalCount2=0; 
			int propinsiCount1=0; 
			int propinsiCount2=0;
			int kabkotaCount1=0; 
			int kabkotaCount2=0;
			int kecamatanCount1=0; 
			int kecamatanCount2=0;
			int kelurahanCount1=0; 
			int kelurahanCount2=0;
			int tpsCount1=0; 
			int tpsCount2=0;

			if (!localDB.getVoteResult(tps, 
				ref nasionalCount1, ref nasionalCount2, 
				ref propinsiCount1, ref propinsiCount2, 
				ref kabkotaCount1, ref kabkotaCount2, 
				ref kecamatanCount1, ref kecamatanCount2, 
				ref kelurahanCount1, ref kelurahanCount2, 
				ref tpsCount1, ref tpsCount2, out xError)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "494", "No data to get, yet.", "");
			}

			JsonLibs.MyJsonLib jsr = new JsonLibs.MyJsonLib ();
			jsr.Add ("fiIdTps", tps);
			jsr.Add ("fiTpsCount1", tpsCount1);
			jsr.Add ("fiTpsCount2", tpsCount2);
			jsr.Add ("fiKelurahanCount1", kelurahanCount1);
			jsr.Add ("fiKelurahanCount2", kelurahanCount2);
			jsr.Add ("fiKecamatanCount1", kecamatanCount1);
			jsr.Add ("fiKecamatanCount2", kecamatanCount2);
			jsr.Add ("fiKabKotaCount1", kabkotaCount1);
			jsr.Add ("fiKabKotaCount2", kabkotaCount2);
			jsr.Add ("fiPropinsiCount1", propinsiCount1);
			jsr.Add ("fiPropinsiCount2", propinsiCount2);
			jsr.Add ("fiTotalCount1", nasionalCount1);
			jsr.Add ("fiTotalCount2", nasionalCount2);

			string repl = jsr.JSONConstruct();
			return HTTPRestDataConstruct.constructHTTPRestResponse (200, "00", "Success", repl);
		}

	}
}

