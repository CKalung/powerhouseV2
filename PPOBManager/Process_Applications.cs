using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPOBHttpRestData;
using PPOBServerHandler;
using System.Collections;
using LOG_Handler;
using System.Security.Cryptography;
using StaticCommonLibrary;
using Process_iVoteHandler;

namespace PPOBManager
{
    public class Process_Applications : IDisposable
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
        ~Process_Applications()
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

        JsonLibs.MyJsonLib jsonConv;
        HTTPRestConstructor HTTPRestDataConstruct;
        PPOBDatabase.PPOBdbLibs localDB;
        Exception xError;

//        bool localIP = false;       // Untuk debuging pake TCP Viewer
//        string EmailDomain = "@dummy2.com";
        PublicSettings.Settings commonSettings;

        //string logPath="";
        //string dbHost = "";
        //int dbPort=0;
        //string dbUser="";
        //string dbPass="";
        //string dbName="";

		public Process_Applications(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            //logPath = LogPath;
            //dbHost = DbHost; dbPort = DbPort; dbUser = DbUser; dbPass = DbPassw; dbName = DbName;
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

		private bool isPhoneValid(string data)
		{
			if (data.Contains (" ") || data.Contains ("'") || data.Contains ("\"") || data.Contains ("\r") || data.Contains ("\n")
				|| data.Contains ("\t"))
				return false;
			else
				return true;
		}
        
		private string Login(HTTPRestConstructor.HttpRestRequest clientData)
        {
            if (clientData.Body.Length == 0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
            }
            if (!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }

            string fiUserPhone = "";

            //string fiUserId = "";
            string fiPassword = "";

            if (jsonConv.ContainsKey("fiPhone"))
                fiUserPhone = ((string)jsonConv["fiPhone"]).Trim();
            //if (jsonConv.ContainsKey("fiUserId")) fiUserId = ((string)jsonConv["fiUserId"]).Trim().ToUpper();

            ReformatPhoneNumber(ref fiUserPhone);

            if (fiUserPhone.Length == 0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid user phone number", "");
            }
            try
            {
                fiPassword = ((string)jsonConv["fiPassword"]).Trim();
            }
            catch
            {
                // field tidak ditemukan atau formatnya salah
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not completed", "");
            }
            if ((fiPassword.Length == 0))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not completed", "");
            }

            if (!localDB.isUserPasswordEqual(fiUserPhone, fiPassword, out xError))
            {
                if (xError != null)
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                else
                    // password error
                    return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Login failed", "");
            }
            // password ok

            string fiStatus = "";
            // Cenah login
            Hashtable hasil = null;
            //Hashtable hasil = localDB.getLoginInfoTopline(fiUserPhone, out xError);
            //bool isTopline = false;
            //if (xError != null)
            //{
            //    //LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Database problem : " + xError.Message);
            //    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
            //}
            //else if (hasil == null)
            //{
            // bukan topline, user biasa atau firstline
            hasil = localDB.getLoginInfoByUserPhone(fiUserPhone, out xError);
            if (xError != null)
            {
                //LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Database problem : " + xError.Message);
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
            }
            else if (hasil == null)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "User not found", "");
            }
            //}
            //else
            //{
            //    // topline
            //    isTopline = true;
            //}

            // simpen di database kondisi heartbeat
            localDB.addHeartBeatLog(fiUserPhone, clientData.ClientHost, out xError);

            fiStatus = localDB.getUserStatus(fiUserPhone, out xError);
            if (xError != null)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
            }
            else if (fiStatus == "")
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "User not found", "");
            }

            string firstName = "";
            string lastName = "";
            string userId = "";
            try
            {
                firstName = ((string)hasil["firstName"]).Trim();
            }
            catch { firstName = ""; }
            try
            {
                lastName = ((string)hasil["lastName"]).Trim();
            }
            catch { lastName = ""; }
            //if (!isTopline)
            //{
            try
            {
                userId = ((string)hasil["userId"]).Trim();
            }
            catch { userId = ""; }
            //}
            //Console.WriteLine("DEBUG Name1 " + firstName);
            //Console.WriteLine("DEBUG Name2 " + lastName);

            // Generate Security Token

            string token = CommonLibrary.AddSessionItem(fiUserPhone, userId, clientData.ClientHost);

            jsonConv.Clear();
            jsonConv.Add("fiPhone", "+" + fiUserPhone);
            jsonConv.Add("fiStatus", fiStatus);
            jsonConv.Add("fiFirstName", firstName);
            jsonConv.Add("fiLastName", lastName);
            jsonConv.Add("fiToken", token);
            //if (!isTopline) 
            jsonConv.Add("fiQioskuAccount", userId);
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
        }

        private int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }

        private char intToChar(int a)
        {
            if (a < 10) return (char)(a + 0x30);
            return (char)(a + 0x40);
        }

        private string generateRandomPassword()
        {
            const int jumlahRandom = 6;
            Random random = new Random();
            System.Threading.Thread.Sleep(0);
            string hasil = "";
            int rd = 0;
            for (int i = 0; i < jumlahRandom; i++)
            {
                // Semua DEC
                //rd = random.Next(0, 9);
                //hasil += (char)(rd + 0x30);

                // Semua HEX
                //rd = random.Next(0, 15);
                //if (rd < 10) hasil += (char)(rd + 0x30);
                //else hasil += (char)(rd + 0x37);

                // Semua numerik dan char
                rd = random.Next(0, 35);
                if (rd < 10) hasil += (char)(rd + 0x30);
                else hasil += (char)(rd + 0x37);
            }
            return hasil;
        }

        private string GetMd5Hash(MD5 md5Hash, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();
            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        private string ChangeUserPassword(HTTPRestConstructor.HttpRestRequest clientData)
        {
            if (clientData.Body.Length == 0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
            }
            if (!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }

            string fiApplicationId;
            string fiPhone;
            string fiPassword;
            string fiNewPassword;

            if (
                (!jsonConv.ContainsKey("fiApplicationId")) || (!jsonConv.ContainsKey("fiPhone")) ||
                (!jsonConv.ContainsKey("fiPassword")) || (!jsonConv.ContainsKey("fiNewPassword"))
                )
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Mandatory field not found", "");
            }

            try
            {
                fiApplicationId = ((string)jsonConv["fiApplicationId"]).Trim();
                fiPhone = ((string)jsonConv["fiPhone"]).Trim();
                fiPassword = ((string)jsonConv["fiPassword"]).Trim();
                fiNewPassword = ((string)jsonConv["fiNewPassword"]).Trim();
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Invalid field data", "");
            }

            ReformatPhoneNumber(ref fiPhone);

            if (fiPhone.Length == 0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid user phone number", "");
            }

            if ((fiPassword.Length == 0))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid password", "");
            }

            if (!localDB.isUserPasswordEqual(fiPhone, fiPassword, out xError))
            {
                if (xError != null)
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                else
                    // password error
                    return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Wrong password", "");
            }
            // password ok

            //localDB.changeUserPassword(fiUserPhone, fiNewPassword, out xError);

            string fiStatus = "";
            // Cenah login
            Hashtable hasil;
            hasil = localDB.getLoginInfoByUserPhone(fiPhone, out xError);
            if (xError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Database problem : " + xError.Message);
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
            }
            else if (hasil == null)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "User not found", "");
            }

            // simpen di database kondisi heartbeat
            localDB.addHeartBeatLog(fiPhone, clientData.ClientHost, out xError);

            fiStatus = localDB.getUserStatus(fiPhone, out xError);
            if (xError != null)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
            }
            else if (fiStatus == "")
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "User not found", "");
            }

            localDB.changeUserPassword(fiPhone, fiNewPassword, out xError);
            
            string firstName = "";
            string lastName = "";
            try
            {
                firstName = ((string)hasil["firstName"]).Trim();
            }
            catch { firstName = ""; }
            try
            {
                lastName = ((string)hasil["lastName"]).Trim();
            }
            catch { lastName = ""; }
            //Console.WriteLine("DEBUG Name1 " + firstName);
            //Console.WriteLine("DEBUG Name2 " + lastName);
            jsonConv.Clear();
            jsonConv.Add("fiPhone", "+" + fiPhone);
            jsonConv.Add("fiStatus", fiStatus);
            jsonConv.Add("fiFirstName", firstName);
            jsonConv.Add("fiLastName", lastName);
            jsonConv.Add("fiQioskuAccount", commonSettings.getString("UserIdHeader") + fiPhone);
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
        }

        private string ResetUserPassword(HTTPRestConstructor.HttpRestRequest clientData)
        {
            if (clientData.Body.Length == 0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
            }
            if (!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }

            string fiApplicationId;
            string fiUserPhone;
            string fiFirstName;
            string fiLastName;
            string fiBirthPlace;
            string fiBirthDate;
            string fiMotherName;

            if (
                (!jsonConv.ContainsKey("fiApplicationId")) || (!jsonConv.ContainsKey("fiUserPhone")) ||
                (!jsonConv.ContainsKey("fiFirstName")) || (!jsonConv.ContainsKey("fiLastName")) ||
                (!jsonConv.ContainsKey("fiBirthPlace")) || (!jsonConv.ContainsKey("fiBirthDate")) ||
                (!jsonConv.ContainsKey("fiMotherName"))
                )
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Mandatory field not found", "");
            }

            try
            {
                fiApplicationId = ((string)jsonConv["fiApplicationId"]).Trim();
                fiUserPhone = ((string)jsonConv["fiUserPhone"]).Trim();
                fiFirstName = ((string)jsonConv["fiFirstName"]).Trim();
                fiLastName = ((string)jsonConv["fiLastName"]).Trim();
                fiBirthPlace = ((string)jsonConv["fiBirthPlace"]).Trim();
                fiBirthDate = ((string)jsonConv["fiBirthDate"]).Trim();
                fiMotherName = ((string)jsonConv["fiMotherName"]).Trim();
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Invalid field data", "");
            }

            ReformatPhoneNumber(ref fiUserPhone);

            if (fiUserPhone.Length == 0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid user phone number", "");
            }

            Hashtable userData = new Hashtable();
            localDB.loadCustomer(fiUserPhone, ref userData, out xError);

            string z = ((string)userData["birth_date"]);  // masih "MM-dd-yyyy"
            userData["birth_date"] = z.Substring(6, 4) + "-" + z.Substring(0, 2) + "-" + z.Substring(3, 2);

            //LogWriter.showDEBUG(this,
            //            ((string)userData["first_name"]).ToLower() + "::" + 
            //            fiFirstName.ToLower()  + "::" + 
            //            ((string)userData["last_name"]).ToLower()  + "::" + 
            //            fiLastName.ToLower()  + "::" + 
            //            ((string)userData["birth_place"]).ToLower()  + "::" + 
            //            fiBirthPlace.ToLower()  + "::" +
            //            ((string)userData["birth_date"]) + "::" +      
            //            fiBirthDate.ToLower()  + "::" + 
            //            ((string)userData["mother_name"]).ToLower()  + "::" + 
            //            fiMotherName.ToLower());

            if ((((string)userData["first_name"]).ToLower() != fiFirstName.ToLower()) || (((string)userData["last_name"]).ToLower() != fiLastName.ToLower()) ||
                (((string)userData["birth_place"]).ToLower() != fiBirthPlace.ToLower()) || (((string)userData["birth_date"]) != fiBirthDate) ||
                (((string)userData["mother_name"]).ToLower() != fiMotherName.ToLower()))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "409", "Request denied...", "");
            }

            string RandomPassword = generateRandomPassword().ToLower();
            string encRandomPassword = "";

            using (MD5 md5Hash = MD5.Create())
            {
                encRandomPassword = GetMd5Hash(md5Hash, RandomPassword);
            }

            localDB.changeUserPassword(fiUserPhone, encRandomPassword, out xError);

            jsonConv.Clear();
            jsonConv.Add("fiFlatPassword", RandomPassword);
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
        }

        private string HeartBeat(HTTPRestConstructor.HttpRestRequest clientData)
		{
			if (clientData.Body.Length == 0) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "406", "No data to process", "");
			}
			if (!jsonConv.JSONParse (clientData.Body)) {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "407", "Invalid data format", "");
			}

			string fiUserPhone = "";
			
			//string fiUserId = "";
			string fiPassword = "";
			string fiRequestCode = "";

            if ((!jsonConv.ContainsKey("fiPhone")) || (!jsonConv.ContainsKey("fiRequestCode")) || 
                (!jsonConv.ContainsKey("fiPassword")))
            {
                // harus ada yg wajib
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Mandatory field not found", "");
            }

            if ((jsonConv["fiPhone"] == null) || (jsonConv["fiRequestCode"] == null) ||
                (jsonConv["fiPassword"] == null))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Null value not allowed", "");
            }
            
            fiUserPhone = ((string)jsonConv["fiPhone"]).Trim();
			//if (jsonConv.ContainsKey("fiUserId")) fiUserId = ((string)jsonConv["fiUserId"]).Trim().ToUpper();

			ReformatPhoneNumber (ref fiUserPhone);

			//if((fiUserId.Length==0) && (fiUserPhone.Length==0))
            if (fiUserPhone.Length == 0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid user phone number", "");
            }
			try {
				fiRequestCode = ((string)jsonConv ["fiRequestCode"]).Trim ();
				fiPassword = ((string)jsonConv ["fiPassword"]).Trim ();
			} catch {
				// field tidak ditemukan atau formatnya salah
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not completed", "");
			}
			if ((fiPassword.Length == 0) || (fiRequestCode.Length == 0)) {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not completed", "");
			}

            if (!localDB.isUserPasswordEqual (fiUserPhone, fiPassword, out xError)) {
                if (xError != null)
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                else
                    // password error
                    return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Login failed", "");
			}
			// password ok

			CommonLibrary.SessionResetTimeOut (fiUserPhone);

			string fiStatus = "";
//			string fiFirstName = "";
//			string fiLastName = "";

            // simpen di database kondisi heartbeat
            localDB.addHeartBeatLog(fiUserPhone, clientData.ClientHost, out xError);

            jsonConv.Clear();
            switch (fiRequestCode)
            {
			case "00":
                    //tinggal didieu jang heartbeat
				jsonConv.Add ("fiReplyCode", "00");

                    // Ambil notifikasi jika ada
				PPOBDatabase.PPOBdbLibs.NotificationValues notifData = localDB.getNotificationSchedule (fiUserPhone, out xError);

                    if (notifData.mobile_phone_number_to != "")
                    {
                        jsonConv.Add("fiNotificationType", notifData.notification_type_code);
                        jsonConv.Add("fiData", notifData.notification_value);

					localDB.moveNotification(notifData.id, notifData.mobile_phone_number_from,
						notifData.mobile_phone_number_to, notifData.notification_time,
						notifData.notification_type_code, notifData.notification_value,
						DateTime.Now, out xError);
                    }
                    else
                    {
                        //				jsonConv.Add("fiUserId", fiUserId);
                        // sementara ambil dr database
                        fiStatus = localDB.getUserStatus(fiUserPhone, out xError);
                        if (xError != null)
                        {
                            return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                        }
                        else if (fiStatus == "")
                        {
                            return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "User not found", "");
                        }
                        jsonConv.Add("fiNotificationType", "00");
                        jsonConv.Add("fiData", "{\"fiServerTime\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\"}");
                    }
                    return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
                case "01":
                    // Menta status

                    // simpen di database kondisi heartbeat
                    localDB.addHeartBeatLog(fiUserPhone, clientData.ClientHost, out xError);
                    if (xError != null)
                    {
                        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                    }

                    jsonConv.Clear();
                    // Ambil notifikasi jika ada
                    // sementara ambil dr database
                    fiStatus = localDB.getUserStatus(fiUserPhone, out xError);
                    if (xError != null)
                    {
                        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                    }
                    else if (fiStatus == "")
                    {
                        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "User not found", "");
                    }
                    jsonConv.Add("fiReplyCode", "00");
                    jsonConv.Add("fiNotificationType", "01");
                    jsonConv.Add("fiData", "{\"fiStatus\":\"" + fiStatus + "\"}");
                    return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
                default:
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "404", "Not implemented", "");
            }
		}

        public string Process(int reqPathCode, HTTPRestConstructor.HttpRestRequest clientData)
        {
            if (reqPathCode == commonSettings.getInt("CommandApplicationLogin"))
            {
                return Login(clientData);
            }
            else if (reqPathCode == commonSettings.getInt("CommandApplicationHeartBeat"))
            {
                return HeartBeat(clientData);
            }
            else if (reqPathCode == commonSettings.getInt("CommandChangeUserPassword"))
            {
                return ChangeUserPassword(clientData);
            }
            else if (reqPathCode == commonSettings.getInt("CommandResetUserPassword"))
            {
                return ResetUserPassword(clientData);
            }
			else if (reqPathCode == commonSettings.getInt("CommandApplicationVoteQueryProvince"))
			{
				using (Process_iVote iVote = new Process_iVote(commonSettings))
				{
					return iVote.QueryProvince(clientData);
				}
			}
			else if (reqPathCode == commonSettings.getInt("CommandApplicationVoteQueryKabupatenKota"))
			{
				using (Process_iVote iVote = new Process_iVote(commonSettings))
				{
					return iVote.QueryKabupatenKota(clientData);
				}
			}
			else if (reqPathCode == commonSettings.getInt("CommandApplicationVoteQueryKecamatan"))
			{
				using (Process_iVote iVote = new Process_iVote(commonSettings))
				{
					return iVote.QueryKecamatan(clientData);
				}
			}
			else if (reqPathCode == commonSettings.getInt("CommandApplicationVoteQueryKelurahan"))
			{
				using (Process_iVote iVote = new Process_iVote(commonSettings))
				{
					return iVote.QueryKelurahan(clientData);
				}
			}
			else if (reqPathCode == commonSettings.getInt("CommandApplicationVoteSendTpsVoteResult"))
			{
				using (Process_iVote iVote = new Process_iVote(commonSettings))
				{
					return iVote.SendTpsVoteResult(clientData);
				}
			}
			else if (reqPathCode == commonSettings.getInt("CommandApplicationVoteSendSurveyorData"))
			{
				using (Process_iVote iVote = new Process_iVote(commonSettings))
				{
					return iVote.SendSurveyorData(clientData);
				}
			}
			else if (reqPathCode == commonSettings.getInt("CommandApplicationVoteQueryTpsResult"))
			{
				using (Process_iVote iVote = new Process_iVote(commonSettings))
				{
					return iVote.QueryTpsVoteResult(clientData);
				}
			}
//			else if (reqPathCode == commonSettings.getInt("CommandApplicationVoteQueryTotalVote"))
//			{
//			}
            else
            {
                // reject
                return HTTPRestDataConstruct.constructHTTPRestResponse(200, "204", "Not implemented", "");
            }
        }

    }
}
