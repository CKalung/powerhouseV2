using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPOBHttpRestData;
using PPOBServerHandler;
using System.Collections;
using TransferReguler;
using LOG_Handler;
using SMSHandler;

namespace Process_MPAccountAccess
{
    public class Process_Account : IDisposable
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
        ~Process_Account()
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
            SMS.Dispose();
        }

        JsonLibs.MyJsonLib jsonConv;
        HTTPRestConstructor HTTPRestDataConstruct;
        PPOBDatabase.PPOBdbLibs localDB;
        Exception xError;

        SMSHandler.SMSSender SMS;

        PublicSettings.Settings commonSettings;
//        string cUserIDHeader = "";       // header userId untuk CustomerId account di QVA
        //bool localIP = false;       // Untuk debuging pake TCP Viewer
        //string EmailDomain = "@hadeuh.com";
        //string sandraHost = "123.231.225.20";    // host sandra
        //int sandraPort = 7080;

        //string logPath="";
        //string dbHost = "";
        //int dbPort=0;
        //string dbUser="";
        //string dbPass="";
        //string dbName="";

		struct stRegisterAccount
        {
			public string fiApplicationId;
            public string fiDistributor;
            public string fiFirstName;
            public string fiLastName;
            public string fiEmail;
            public string fiUserEmail;
            public string fiPhone;
            public string fiCardIDNumber;
            public string fiBirthPlace;
            public string fiBirthDate;
            public string fiGender;
            public string fiAddress;
            public string fiCity;
            public string fiZipCode;
            public string fiProvince;
            public string fiMotherName;
//            public string fiMemberType;
//            public string fiUserId;
            public string fiPassword;
        }

		public Process_Account(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            //EmailDomain = "@" + commonSettings.getString("EmailDomain");
            //cUserIDHeader = commonSettings.getString("UserIdHeader");
            //logPath = LogPath;
            //dbHost = DbHost; dbPort = DbPort; dbUser = DbUser; dbPass = DbPassw; dbName = DbName;
            //sandraPort = SandraPort;sandraHost = SandraHost;
            HTTPRestDataConstruct = new HTTPRestConstructor();
            jsonConv = new JsonLibs.MyJsonLib();
            localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
                commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
            SMS = new SMSSender(commonSettings);
        }

        private bool isRegisteredInSandra(string userID, out bool error, ref string errCode,ref string errMessage)
        {
            // inquiry
            double balance = 0;
			using (SandraLibs sandra = new SandraLibs(commonSettings.getInt("QvaTimeOut"),
				(commonSettings.getString("SandraUseTcpMethod").Trim().ToLower() == "true"),
				commonSettings.getString("SandraHmsUserAuth"),
				commonSettings.getString("SandraHmsSecretKey"),
				commonSettings.getString("SandraAuthId")))
            {
                if (sandra.Inquiry(
					commonSettings.getString("SandraHost"), commonSettings.getInt("SandraPort"), 
					(commonSettings.getString("SandraIsHttps").ToLower() == "true"), userID, ref balance))
                {
                    error = false;
                    return true;
                }
                else
                {
                    if (sandra.LastError.ServerCode == "12") // berarti memang can aya
                    {
                        error = false;
                        return false;
                    }
                    if (sandra.LastError.ServerCode == "E1") // berarti memang can aya
                    {
                        error = false;
                        return false;
                    }
                    if (sandra.LastError.ServerCode == "") // berarti error
                    {
                        errCode = "z999";
                        errMessage = "No response from host"; //No response from host
                        error = true;
                        return false;
                    }
                    else
                    {
                        // nah bisa jadi sandra error
                        errCode = sandra.LastError.ServerCode;
                        errMessage = sandra.LastError.ServerMessage;
                        error = true;
                        return false;
                    }
                }
            }
        }

        private bool isRegisteredInDB(string userID)
        {
            return localDB.isAccountExist(userID, out xError);
        }

        private void REGISTRASI_KE_SANDRA()
        {
            //jsonConv.Clear();
            //jsonConv.Add("ficoCustomerGroupId", "91111");
            //jsonConv.Add("ficoCustomerId", sTmp);
            //jsonConv.Add("ficoCustomerName", stRegAcc.fiFirstName + " " + stRegAcc.fiLastName);
            //jsonConv.Add("ficoCustomerPhone", stRegAcc.fiPhone);
            //jsonConv.Add("ficoCustomerPhone2", "-");
            //jsonConv.Add("ficoCustomerEmail", stRegAcc.fiEmail);
            //jsonConv.Add("ficoCustomerBirthDate", stRegAcc.fiBirthDate);
            //jsonConv.Add("ficoCustomerBirthPlace", stRegAcc.fiBirthPlace);
            //jsonConv.Add("ficoCustomerCity", stRegAcc.fiCity);
            //jsonConv.Add("ficoCustomerCity2", "-");
            //jsonConv.Add("ficoCustomerAddress", stRegAcc.fiAddress);
            //jsonConv.Add("ficoCustomerTrxAllowed", "CD");
            //jsonConv.Add("ficoCustomerAddress2", stRegAcc.fiProvince);
            //jsonConv.Add("ficoCustomerAddress3", "-");
            //jsonConv.Add("ficoCustomerZipCode", stRegAcc.fiZipCode);
            //jsonConv.Add("ficoCustomerZipCode2", "-");
            //jsonConv.Add("ficoCustomerCustomField1", stRegAcc.fiDistributor);
            //jsonConv.Add("ficoCustomerCustomField2", stRegAcc.fiMemberType);
            //jsonConv.Add("ficoCustomerCustomField3", "-");
            //jsonConv.Add("ficoCustomerCustomField4", "-");
            //jsonConv.Add("ficoCustomerCustomField5", "-");
            //jsonConv.Add("ficoCustomerCardNumber", "-");
            //jsonConv.Add("ficoCustomerCardIdentityType", "KTP");
            //jsonConv.Add("ficoCustomerIdentityCardNumber", stRegAcc.fiCardIDNumber);
            //jsonConv.Add("ficoCustomerIdentityCardValidDate", "01-01-2999");
            //jsonConv.Add("ficoCustomerNpwp", "-");
            //jsonConv.Add("ficoCustomerNickname", "-");
            //jsonConv.Add("ficoCustomerRef1", "-");
            //jsonConv.Add("ficoCustomerRef2", "-");
            //jsonConv.Add("ficoCustomerRef3", "-");
            //jsonConv.Add("ficoCustomerGender", stRegAcc.fiGender);
            //jsonConv.Add("ficoCustomerMotherName", stRegAcc.fiMotherName);
            //jsonConv.Add("ficoCustomerPassword", stRegAcc.fiPassword);
            //jsonConv.Add("ficoCustomerUsername", stRegAcc.fiUsername);
            //jsonConv.Add("source", "");

            //// konek ke host sandra untuk registrasi
			//if (localIP) sandraHost = "127.0.0.1";
            //using (SandraLibs sandra = new SandraLibs())
            //{
			//    if (sandra.RegisterCustomer(sandraHost, sandraPort, jsonConv))
            //    {
            //        Console.WriteLine("Sandra OK");
            //        jsonConv.Clear();
            //        jsonConv.Add("fiUserId", sTmp);
            //        jsonConv.Add("fiDistributor", stRegAcc.fiDistributor);
            //        jsonConv.Add("fiFirstName", stRegAcc.fiFirstName);
            //        jsonConv.Add("fiLastName", stRegAcc.fiLastName);
            //        jsonConv.Add("fiPhone", stRegAcc.fiPhone);
            //        jsonConv.Add("fiEmail", stRegAcc.fiEmail);
            //        jsonConv.Add("fiCardIDNumber", stRegAcc.fiCardIDNumber);
            //        jsonConv.Add("fiBirthPlace", stRegAcc.fiBirthPlace);
            //        jsonConv.Add("fiBirthDate", stRegAcc.fiBirthDate);
            //        jsonConv.Add("fiGender", stRegAcc.fiGender);
            //        jsonConv.Add("fiAddress", stRegAcc.fiAddress);
            //        jsonConv.Add("fiCity", stRegAcc.fiCity);
            //        jsonConv.Add("fiZipCode", stRegAcc.fiZipCode);
            //        jsonConv.Add("fiProvince", stRegAcc.fiProvince);
            //        jsonConv.Add("fiMotherName", stRegAcc.fiMotherName);
            //        jsonConv.Add("fiMemberType", stRegAcc.fiMemberType);
            //        jsonConv.Add("fiStatus", "PENDING");
            //        // masukkan di database

            //        // kirim respon ke client
            //        return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
            //    }
            //    else
            //    {
            //        Console.WriteLine("Sandra not OK");
            //        Console.WriteLine("Error code: " + sandra.LastError.ServerCode);
            //        Console.WriteLine("Error message: " + sandra.LastError.Serve.StackTrace);
            //        return HTTPRestDataConstruct.constructHTTPRestResponse(400, sandra.LastError.ServerCode, sandra.LastError.Serve.StackTrace, "");
            //    }
            //}
        }

        private bool requestQVARegistration(Hashtable data, ref string erCode, ref string erMsg)
        {
            // cek balance dulu

            jsonConv.Clear();
            jsonConv.Add("ficoCustomerGroupId", commonSettings.getString("QVA_Registration_GroupId"));
            jsonConv.Add("ficoCustomerId", (string)data["user_id"]);
            jsonConv.Add("ficoCustomerName", data["first_name"] + " " + data["last_name"]);
            jsonConv.Add("ficoCustomerPhone", data["phone"]);
            jsonConv.Add("ficoCustomerPhone2", "-");
            jsonConv.Add("ficoCustomerEmail", data["email"]);
            //jsonConv.Add("ficoCustomerBirthDate", ((DateTime)data["birth_date"]).ToString("MM-dd-yyyy"));
            jsonConv.Add("ficoCustomerBirthDate", (string)data["birth_date"]);
            jsonConv.Add("ficoCustomerBirthPlace", data["birth_place"]);
            jsonConv.Add("ficoCustomerCity", data["city"]);
            jsonConv.Add("ficoCustomerCity2", "-");
            jsonConv.Add("ficoCustomerAddress", data["address"]);
            jsonConv.Add("ficoCustomerTrxAllowed", "CD");
            jsonConv.Add("ficoCustomerAddress2", data["province"]);
            jsonConv.Add("ficoCustomerAddress3", "-");
            jsonConv.Add("ficoCustomerZipCode", data["zipcode"]);
            jsonConv.Add("ficoCustomerZipCode2", "-");
            jsonConv.Add("ficoCustomerCustomField1", data["distributor_id"]);
            jsonConv.Add("ficoCustomerCustomField2", data["member_type"].ToString());
            jsonConv.Add("ficoCustomerCustomField3", "-");
            jsonConv.Add("ficoCustomerCustomField4", "-");
            jsonConv.Add("ficoCustomerCustomField5", "-");
            jsonConv.Add("ficoCustomerCardNumber", "-");
            jsonConv.Add("ficoCustomerCardIdentityType", "KTP");
            jsonConv.Add("ficoCustomerIdentityCardNumber", data["card_id_number"]);
            jsonConv.Add("ficoCustomerIdentityCardValidDate", "01-01-2999");
//            jsonConv.Add("ficoCustomerBankAccount", "-");
            jsonConv.Add("ficoCustomerNpwp", "-");
            jsonConv.Add("ficoCustomerNickname", "-");
            jsonConv.Add("ficoCustomerRef1", "-");
            jsonConv.Add("ficoCustomerRef2", "-");
            jsonConv.Add("ficoCustomerRef3", "-");
            jsonConv.Add("ficoCustomerGender", data["gender"]);
            jsonConv.Add("ficoCustomerMotherName", data["mother_name"]);
            jsonConv.Add("ficoCustomerUsername", data["user_id"]);
            jsonConv.Add("ficoCustomerPassword", data["user_password"]);
//            jsonConv.Add("ficoRegistrationDate", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.sss+07:00"));
            jsonConv.Add("source", commonSettings.getString("QVA_Registration_Source"));
            

            // konek ke host sandra untuk registrasi
			//if (localIP) sandraHost = "127.0.0.1";
            erCode = "";
            erMsg = "";
            bool hasil = false;
			using (SandraLibs sandra = new SandraLibs(commonSettings.getInt("QvaTimeOut"),
				(commonSettings.getString("SandraUseTcpMethod").Trim().ToLower() == "true"),
				commonSettings.getString("SandraHmsUserAuth"),
				commonSettings.getString("SandraHmsSecretKey"),
				commonSettings.getString("SandraAuthId")))
            {
                if (sandra.RegisterCustomer(
					commonSettings.getString("SandraHost"), commonSettings.getInt("SandraPort"), 
					(commonSettings.getString("SandraIsHttps").ToLower() == "true"), ref jsonConv))
                {
                    hasil = true;
                }
                else
                {
                    erCode = sandra.LastError.ServerCode;
                    erMsg = sandra.LastError.ServerMessage;
					LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Failed QVA Registration : Sandra errorcode: " + erCode + " : SandraMessage: " + erMsg);

                    hasil = false;
                }
            }
            return hasil;
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

        private string generateActivationCode()
        {
            const int jumlahRandom = 8;
            Random random = new Random();
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

		private string AccountRegistration(HTTPRestConstructor.HttpRestRequest clientData, 
            bool isWebRegistration)
        {
            stRegisterAccount stRegAcc = new stRegisterAccount();
            if(clientData.Body.Length==0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
            }
            if(!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }
            // TAMPUNG data json dari Client
            DateTime ttl;
            try
            {
                string dtcl = (string)jsonConv["fiBirthDate"];
                //// dd-MM-yyyy
                //DateTime ttl = new DateTime(int.Parse(dtcl.Substring(6, 4)), int.Parse(dtcl.Substring(3, 2)), int.Parse(dtcl.Substring(0, 2)));
                // yyyy-MM-dd
                ttl = new DateTime(int.Parse(dtcl.Substring(0, 4)), int.Parse(dtcl.Substring(5, 2)), int.Parse(dtcl.Substring(8, 2)));
            }
            catch
            {
                // field tidak ditemukan atau formatnya salah
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field BirthDate not found or date format error", "");
            }

            try
            {
				stRegAcc.fiApplicationId = ((string)jsonConv["fiApplicationId"]).Trim();
                stRegAcc.fiDistributor = ((string)jsonConv["fiDistributor"]).Trim().Replace("-", "");
                stRegAcc.fiFirstName = ((string)jsonConv["fiFirstName"]).Trim();
                stRegAcc.fiLastName = ((string)jsonConv["fiLastName"]).Trim();
                stRegAcc.fiPhone = ((string)jsonConv["fiPhone"]).Trim().Replace("-", "");
                stRegAcc.fiUserEmail = ((string)jsonConv["fiEmail"]).Trim();
                stRegAcc.fiEmail = "";
                stRegAcc.fiCardIDNumber = ((string)jsonConv["fiCardIDNumber"]).Trim();
                stRegAcc.fiBirthPlace = ((string)jsonConv["fiBirthPlace"]).Trim();
                stRegAcc.fiBirthDate = ((string)jsonConv["fiBirthDate"]).Trim();
                stRegAcc.fiGender = ((string)jsonConv["fiGender"]).Trim();
                stRegAcc.fiAddress = ((string)jsonConv["fiAddress"]).Trim();
                stRegAcc.fiCity = ((string)jsonConv["fiCity"]).Trim();
                stRegAcc.fiZipCode = ((string)jsonConv["fiZipCode"]).Trim();
                stRegAcc.fiProvince = ((string)jsonConv["fiProvince"]).Trim();
                stRegAcc.fiMotherName = ((string)jsonConv["fiMotherName"]).Trim();
                stRegAcc.fiPassword = ((string)jsonConv["fiPassword"]).Trim().ToLower();
            }
            catch
            {
                // field tidak ditemukan atau formatnya salah
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not completed", "");
            }
            if((stRegAcc.fiGender!="M") && (stRegAcc.fiGender!="F"))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Gender should be 'M' or 'F'", "");
            }

            string userTypeMember = "USER";
            if (jsonConv.isExists("fiMemberType"))
            {
                string membType = ((string)jsonConv["fiMemberType"]).Trim().ToUpper();
                if (membType == "TOPLINE")
                {
                    userTypeMember = membType;
                }
                else if (membType == "FIRSTLINE")
                {
                    userTypeMember = membType;
                    // uplink ID di set ke TOP LINE

                    Hashtable topLine = localDB.getToplineInfo(out xError);
                    if (xError != null)
                    {
                        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Can not access database", "");
                    }
                    if (topLine == null)
                    {
                        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "495", "No Top line found", "");
                    }

                    stRegAcc.fiDistributor = (string)topLine["mobilephone"];
                    if ((int)topLine["userStatus"] != 2) // jika blm aktif
                    {
                        return HTTPRestDataConstruct.constructHTTPRestResponse(400, "462", "Topline hasn't been activated", "");
                    }
                }
            }

			ReformatPhoneNumber (ref stRegAcc.fiPhone);
			ReformatPhoneNumber (ref stRegAcc.fiDistributor);

            if (stRegAcc.fiPhone.Length < 5)
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "409", "Invalid phone number", "");
            if (stRegAcc.fiZipCode.Length != 5)
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "409", "Invalid zipcode length, 5 digits", "");

            if (stRegAcc.fiPhone == stRegAcc.fiDistributor)
            {
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "490", "User phone and distributor phone can not be equal", "");
            }

            // Generate ID dari nomor telepon
            string sTmp = commonSettings.getString("UserIdHeader") + stRegAcc.fiPhone;

            //sTmp = commonSettings.getString("UserIdHeader") + stRegAcc.fiPhone;           // 990628xxxx
            stRegAcc.fiEmail = sTmp + @"@" +commonSettings.getString("EmailDomain");

            // ==== Cukup cek di db table customer di db lokal saja untuk CEK di SANDRA 

            // CEk di lokal DB apakah account sudah ter register?
            if (localDB.isPhoneExistAndActive(stRegAcc.fiPhone, out xError))
            //if (localDB.isAccountExist(stRegAcc.fiPhone, out xError))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "491", "Mobile phone number has already registered", "");
            }
            if (xError != null)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Can not access database", "");
            }

            if (localDB.isAccountExist(stRegAcc.fiPhone, out xError))
            {
                // nah, boleh dibuang krn memang belum aktif
                localDB.removeUser(stRegAcc.fiPhone, out xError);
            }
            
//            if (stRegAcc.fiDistributor.Length == 0)
//            {
//                stRegAcc.fiDistributor = localDB.getFirstLinePhone(out xError);
//                if (xError != null)
//                {
//                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
//                }
//                if (stRegAcc.fiDistributor == "")
//                {
//                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "490", "Distributor not found", "");
//                }
//            }
//            else
//            {
//                // cek HP penyelia
//                //disini juga cek jika sim penyelia harus sudah aktif
//                if (!localDB.isPhoneExistAndActive(stRegAcc.fiDistributor, out xError))
//                //if (!localDB.isPhoneExist(stRegAcc.fiDistributor, out xError))
//                //if (!localDB.isDistributorExist(stRegAcc.fiDistributor, out xError))
//                {
//                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "490", "No active distributor found", "");
//                }
//            }
//			if (!localDB.isPhoneExistAndActive(stRegAcc.fiDistributor, out xError))
//			{
//				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "490", "No active distributor found", "");
//			}
//

            string activation_Code = generateActivationCode();

            jsonConv.Clear();
            jsonConv.Add("fiApplicationId",stRegAcc.fiApplicationId);
//            jsonConv.Add("fiUserId", sTmp);
//            jsonConv.Add("fiDistributor", stRegAcc.fiDistributor);
            jsonConv.Add("fiFirstName", stRegAcc.fiFirstName);
            jsonConv.Add("fiLastName", stRegAcc.fiLastName);
            jsonConv.Add("fiPhone", stRegAcc.fiPhone);
            jsonConv.Add("fiEmail", stRegAcc.fiUserEmail);
            jsonConv.Add("fiCardIDNumber", stRegAcc.fiCardIDNumber);
            jsonConv.Add("fiBirthPlace", stRegAcc.fiBirthPlace);
            jsonConv.Add("fiBirthDate", stRegAcc.fiBirthDate);
            jsonConv.Add("fiGender", stRegAcc.fiGender);
            jsonConv.Add("fiAddress", stRegAcc.fiAddress);
            jsonConv.Add("fiCity", stRegAcc.fiCity);
            jsonConv.Add("fiZipCode", stRegAcc.fiZipCode);
            jsonConv.Add("fiProvince", stRegAcc.fiProvince);
            jsonConv.Add("fiMotherName", stRegAcc.fiMotherName);
            //jsonConv.Add("fiMemberType", stRegAcc.fiMemberType);
            if (isWebRegistration)
            {
                //jsonConv.Add("fiActivationCode", activation_Code);
                activation_Code = "web://"+activation_Code;
            }
            else
                jsonConv.Add("fiActivationCode", activation_Code);
            jsonConv.Add("fiStatus", "PENDING");

            // daftarkan di database lokal
            string sDistributor = localDB.createNewCustomer(stRegAcc.fiApplicationId, sTmp, 
				                      stRegAcc.fiPassword, stRegAcc.fiDistributor, 
                                      stRegAcc.fiFirstName, stRegAcc.fiLastName, stRegAcc.fiEmail, 
                                      stRegAcc.fiUserEmail, stRegAcc.fiPhone, 
                                      stRegAcc.fiCardIDNumber, stRegAcc.fiBirthPlace, ttl, 
                                      stRegAcc.fiGender, stRegAcc.fiAddress, stRegAcc.fiCity, 
                                      stRegAcc.fiZipCode, stRegAcc.fiProvince,
                                      stRegAcc.fiMotherName, userTypeMember, activation_Code,
                                      clientData.ClientHost, isWebRegistration,
				                      out xError);
			if(xError != null)
            {
                // gagal insert 
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed to insert new customer, " + xError.Message + "\r\n" +xError.StackTrace);
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
            }
            switch (sDistributor)
            {
                case "-1":	//-- uplink hasn't been registered yet
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Distributor not registered", "");
                case "-2":	//-- uplink hasn't registered in sys_member
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Distributor not registered(2)", "");
                case "-3":	//-- uplink hasn't been activated yet
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Distributor hasn't been activated", "");
                case "-4":	//-- no valid FIRSTLINE
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "No FirstLine distributor", "");
                case "-5":	//-- Topline already exist
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "TopLine already exist", "");
                default:	// valid distributor
                    // kirim respon ke client
                    jsonConv.Add("fiDistributor", "+" + sDistributor);
                    if (isWebRegistration)
                    {
                        // Kirim SMS ke user dengan kode registrasi
                        // Kirimkan aja kode aktivasi, ntar di ubah saat mau kirim notifikasi
                        // ke penyelia
                        SMS.SendSMS("+" + stRegAcc.fiPhone, commonSettings.getString("WebRegSmsMessage") + " " + activation_Code.Substring(6));
                    }
                    string repl = jsonConv.JSONConstruct();
                    return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", repl);
            }

            // daftarkan di sandra nanti jika penyelia sudah mengaktifkan
            // jika penyelia sudah mengaktifkan, register ke sandra dan ubah status di db lokal menjadi aktif
            
        }

        private string AccountWebValidation(HTTPRestConstructor.HttpRestRequest clientData)
        {
            if(clientData.Body.Length==0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
            }
            if(!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }
            // TAMPUNG data json dari Client
            if ((!jsonConv.ContainsKey("fiPhone")) ||
//                (!jsonConv.ContainsKey("fiPassword")) ||
                (!jsonConv.ContainsKey("fiRegistrationCode")))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Mandatory field not found", "");
            }

            //string appID = "";
            string userPhone = "";
//            string userPassword = "";
            string registrationCode = "";

            try
            {
                userPhone = ((string)jsonConv["fiPhone"]).Trim();
//                userPassword = ((string)jsonConv["fiPassword"]).Trim();
                registrationCode = "web://"+((string)jsonConv["fiRegistrationCode"]).Trim();
            }
            catch
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "Invalid field type", "");
            }
            ReformatPhoneNumber(ref userPhone);

            if (userPhone.Length < 5)
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "409", "Invalid phone number", "");

            // Generate ID dari nomor telepon
			//string sTmp = commonSettings.getString("UserIdHeader") + userPhone;

            // cek jika di database sudah ada dan masih pending dan sudah registrasi via web
            //load registration data
            Hashtable dBuff = new Hashtable();
            if (localDB.loadCustomer(userPhone, ref dBuff, out xError) < 1)
            {
                if (xError != null)
                {
                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                }
                else
                {
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "410", "No user data found", "");
                }
            }

            int pid = (int)dBuff["pid"];
            int member_type = (int)dBuff["member_type"];
            int uplink_id = (int)dBuff["distributor_id"];
            string distributorPhone = localDB.getPhoneFromIndex(uplink_id,out xError);
            string no_qva = (string)dBuff["user_id"];
            string firstName = (string)dBuff["first_name"];
            string lastName = (string)dBuff["last_name"];
            //string group_id = (string)dBuff["user_id"];
            bool fweb_registration = (bool)dBuff["web_registration"];

            // cek sudah aktif belum?
            if (((int)dBuff["user_status"]) == 2)	// active
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "411", "User has already activated", "");
            }

            if (!fweb_registration)	// harus registrasi via web
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "411", "Invalid registration way", "");
            }

            if (((string)dBuff["activation_code"]) != registrationCode)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Wrong registration code", "");
            }

            // update registration code dengan activation code untuk aktivasi si penyelia
            string activation_Code = generateActivationCode();
            localDB.updateCustomerActivationCode(pid,activation_Code,out xError);


            // jika TOPLINE, langsung aktivasi saja
            Exception ExErr = null;
            Hashtable topline = localDB.getLoginInfoTopline(userPhone, out ExErr);
            if (topline == null)
            {
                // OK, semua sudah masuk persyaratan, sekarang kirim notifikasi ke penyelia
                // siapkan json untuk notifikasi ke agen
                jsonConv.Clear();
                jsonConv.Add("fiCustomerPhone", userPhone);
                jsonConv.Add("fiCustomerName", firstName + " " + lastName);
                jsonConv.Add("fiNotificationDateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                jsonConv.Add("fiActivationCode", activation_Code);

                string subJson = jsonConv.JSONConstruct();

                // Insert di tabel notifikasi saja
                localDB.insertNotificationQueue(userPhone, distributorPhone, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    "10", subJson, out xError);

                // dan reply sukses ke client
                jsonConv.Clear();
                jsonConv.Add("fiDistributor", distributorPhone);
                string repl = jsonConv.JSONConstruct();
                return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", repl);
            }
            else
            {
                // disini aktivasi topline
                string NewMemberQvaAccount = commonSettings.getString("UserIdHeader") + userPhone;
                string httpRepl = "";
                if (!prosesRegistrasi(userPhone, activation_Code, NewMemberQvaAccount,
                    clientData.ClientHost, ref httpRepl))
                {
                    return httpRepl;
                }
                jsonConv.Clear();
                jsonConv.Add("fiDistributor", userPhone);       // krn distributor dia juga
                string repl = jsonConv.JSONConstruct();
                return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", repl);
            }
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

        private bool prosesRegistrasi(string fiNewMemberPhone, string fiNewMemberActCode, 
            string NewMemberQvaAccount, string ClientHost, ref string hasil)
        {
            //load registration data
            Hashtable dBuff = new Hashtable();
            if (localDB.loadCustomer(fiNewMemberPhone, ref dBuff, out xError) < 1)
            {
                if (xError != null)
                {
                    hasil = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                    return false;
                }
                else
                {
					hasil = HTTPRestDataConstruct.constructHTTPRestResponse(400, "410", "No user data found", "");
                    return false;
                }
            }

            int pid = (int)dBuff["pid"];
            int uplink_id = (int)dBuff["distributor_id"];
            string no_qva = (string)dBuff["user_id"];
            //string group_id = (string)dBuff["user_id"];
            bool fweb_registration = (bool)dBuff["web_registration"];

            // cek sudah aktif belum?
            if (((int)dBuff["user_status"]) == 2)	// active
            {
                hasil = HTTPRestDataConstruct.constructHTTPRestResponse(400, "411", "User has already activated", "");
                return false;
            }

            if (((string)dBuff["activation_code"]) != fiNewMemberActCode)
            {
                hasil = HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Wrong Activation code", "");
                return false;
            }

            //if (fweb_registration)	// harus bukan registrasi via web
            //{
            //    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "411", "Invalid registration way", "");
            //}

            // registrasi ke serverHost sandra
            // request registration ke sandra
            string errCode = "";
            string errMsg = "";
            bool isError = false;

            if (!isRegisteredInSandra(NewMemberQvaAccount, out isError, ref errCode, ref errMsg))
            {
                if (isError)
                {
                    hasil = HTTPRestDataConstruct.constructHTTPRestResponse(400, errCode, errMsg, "");
                    return false;
                }

                // registrasi qva
                if (!requestQVARegistration(dBuff, ref errCode, ref errMsg))
                {
                    hasil = HTTPRestDataConstruct.constructHTTPRestResponse(400, errCode, errMsg, "");
                    return false;
                }
            }

            string tglReg = "";
            string bankAcc = "";
            string trxAllowed = "";
            // insert table customer dengan data dari reply qva dari jsonConv
            try
            {
                // pake try krn mun udah terregistrasi di qva, isina teu sesuai nu diharapkeun
                bankAcc = (string)jsonConv["ficoCustomerBankAccount"];
                trxAllowed = (string)jsonConv["ficoCustomerTrxAllowed"];
                tglReg = (string)jsonConv["ficoRegistrationDate"]; // "2013-04-18T14:34:18.816+07:00"
                tglReg = tglReg.Substring(0, 4) + "-" + tglReg.Substring(5, 2) + "-" + tglReg.Substring(8, 2) + " " +
                     tglReg.Substring(11, 2) + ":" + tglReg.Substring(14, 2) + ":" + tglReg.Substring(17, 6);
            }
            catch
            {
                tglReg = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                bankAcc = "-";
                trxAllowed = "CD";
            }
            //jsonConv
            if (!localDB.insertCustomer(pid, no_qva, uplink_id, tglReg, bankAcc,
                "1234", trxAllowed, "91111", ClientHost,
                fweb_registration, out xError))
            {
                if (xError != null)
                {
					LogWriter.write(this,LogWriter.logCodeEnum.ERROR, "Failed to insert Customer data: " + xError.Message);
                    hasil = HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
                    return false;
                }
                else
                {
					LogWriter.write(this,LogWriter.logCodeEnum.ERROR, "Failed to insert Customer data to database");
                    hasil = HTTPRestDataConstruct.constructHTTPRestResponse(410, "410", "No data", "");
                    return false;
                }
            }

            // update database jadi ACTIVE, faktor error sangat kecil disini
            localDB.updateCustomerStatus(fiNewMemberPhone, 2, out xError);
            return true;
        }

        private string AccountActivation(HTTPRestConstructor.HttpRestRequest clientData)
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
			string fiUserPhone = "";
            string fiPassword = "";
            string fiNewMemberActCode = "";
            string fiNewMemberPhone = "";
            try
            {
				fiApplicationID = ((string)jsonConv["fiApplicationId"]).Trim();
				fiUserPhone = ((string)jsonConv["fiPhone"]).Trim().ToUpper();
                fiPassword = ((string)jsonConv["fiPassword"]).Trim().ToLower();
                fiNewMemberActCode = ((string)jsonConv["fiNewMemberActCode"]).Trim();
                fiNewMemberPhone = ((string)jsonConv["fiNewMemberPhone"]).Trim();
            }
            catch
            {
                // field tidak ditemukan atau formatnya salah
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not completed", "");
            }
			ReformatPhoneNumber (ref fiUserPhone);
			ReformatPhoneNumber (ref fiNewMemberPhone);
			if ((fiUserPhone.Length == 0) || (fiPassword.Length == 0) || (fiNewMemberPhone.Length == 0) || (fiNewMemberActCode.Length == 0))
            {
                // field tidak ditemukan atau formatnya salah
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Invalid phone number", "");
            }
			if(fiUserPhone==fiNewMemberPhone)
            {
                // tidak boleh mengaktivasi diri sendiri
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "403", "Could not activate your self", "");
            }

            // cek dengan database, apakah password sama?
			if (!localDB.isUserPasswordEqual(fiUserPhone, fiPassword, out xError))
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

            string NewMemberQvaAccount = commonSettings.getString("UserIdHeader") + fiNewMemberPhone;
            string httpRepl = "";
            if (!prosesRegistrasi(fiNewMemberPhone, fiNewMemberActCode, NewMemberQvaAccount,
                clientData.ClientHost, ref httpRepl))
            {
                return httpRepl;
            }
            
            // reply sukses
            jsonConv.Clear();
			jsonConv.Add("fiNewMemberQvaAccount", NewMemberQvaAccount);
			jsonConv.Add("fiNewMemberPhone", "+" + fiNewMemberPhone);
            jsonConv.Add("fiStatus", "ACTIVE");

            // kirim respon ke client
            string repl = jsonConv.JSONConstruct();
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", repl);
        }

        public string ActivateFirstLine(HTTPRestConstructor.HttpRestRequest clientData)
        {
			return "";
//            if (clientData.Body.Length == 0)
//            {
//                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
//            }
//            if (!jsonConv.JSONParse(clientData.Body))
//            {
//                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
//            }
//            // TAMPUNG data json dari Client
//			string fiUserPhone = "";
//            string fiPassword = "";
//            try
//            {
//				fiUserPhone = ((string)jsonConv["fiUserPhone"]).Trim();
//                fiPassword = ((string)jsonConv["fiPassword"]).Trim().ToLower();
//            }
//            catch
//            {
//                // field tidak ditemukan atau formatnya salah
//                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not completed", "");
//            }
//			if ((fiUserPhone.Length == 0) || (fiPassword.Length == 0))
//            {
//                // field tidak ditemukan atau formatnya salah
//                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not completed", "");
//            }
//
//			// cek dengan database, apakah password sama?
//			if (!localDB.isUserPasswordEqual(fiUserPhone, fiPassword, out xError))
//			{
//				if (xError != null)
//				{
//					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
//				}
//				else
//				{
//					// password error
//					return HTTPRestDataConstruct.constructHTTPRestResponse(401, "401", "Login failed", "");
//				}
//			}
//			// password ok
//
//			// cek apakah firstLine?
//            if (!localDB.isFirstLine(fiUserId, out xError))
//            {
//                if (xError != null)
//                {
//                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
//                }
//                else
//                {
//                    // password error
//                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "491", "Not First Liner", "");
//                }
//            }
//
//
//            // registrasi ke serverHost sandra
//            //load registration data
//            Hashtable dBuff = new Hashtable();
//            if (localDB.loadCustomer(fiUserId, ref dBuff, out xError) < 1)
//            {
//                if (xError != null)
//                {
//                    return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
//                }
//                else
//                {
//                    return HTTPRestDataConstruct.constructHTTPRestResponse(410, "410", "Resource gone", "");
//                }
//            }
//
//            // request registration ke sandra
//            string errCode = "";
//            string errMsg = "";
//            if (!requestQVARegistration(dBuff, ref errCode, ref errMsg))
//            {
//                return HTTPRestDataConstruct.constructHTTPRestResponse(400, errCode, errMsg, "");
//            }
//
//            // update database jadi ACTIVE, faktor error sangat kecil disini
//            localDB.updateCustomerStatus(fiUserId, "ACTIVE", out xError);
//
//            // reply sukses
//            jsonConv.Clear();
//            jsonConv.Add("fiUserId", fiUserId);
//            jsonConv.Add("fiStatus", "ACTIVE");
//
//            // kirim respon ke client
//            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", jsonConv.JSONConstruct());
        }

        private string AccountEditRegistration(HTTPRestConstructor.HttpRestRequest clientData)
        {
            stRegisterAccount stRegAcc = new stRegisterAccount();
            if (clientData.Body.Length == 0)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
            }
            if (!jsonConv.JSONParse(clientData.Body))
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
            }
			string dtcl = "";
			DateTime ttl;
            try
            {
				dtcl = ((string)jsonConv["fiBirthDate"]).Trim();
                //// dd-MM-yyyy
                //DateTime ttl = new DateTime(int.Parse(dtcl.Substring(6, 4)), int.Parse(dtcl.Substring(3, 2)), int.Parse(dtcl.Substring(0, 2)));
                // yyyy-MM-dd
                ttl = new DateTime(int.Parse(dtcl.Substring(0, 4)), int.Parse(dtcl.Substring(5, 2)), int.Parse(dtcl.Substring(8, 2)));
            }
            catch
            {
                // field tidak ditemukan atau formatnya salah
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "BirthDate field not found or wrong format", "");
            }

            try
            {
                stRegAcc.fiDistributor = ((string)jsonConv["fiDistributor"]).Trim();
                stRegAcc.fiFirstName = ((string)jsonConv["fiFirstName"]).Trim();
                stRegAcc.fiLastName = ((string)jsonConv["fiLastName"]).Trim();
                stRegAcc.fiPhone = ((string)jsonConv["fiPhone"]).Trim();
                stRegAcc.fiEmail = ((string)jsonConv["fiEmail"]).Trim();
                stRegAcc.fiCardIDNumber = ((string)jsonConv["fiCardIDNumber"]).Trim();
                stRegAcc.fiBirthPlace = ((string)jsonConv["fiBirthPlace"]).Trim();
				stRegAcc.fiBirthDate = dtcl;
                stRegAcc.fiGender = ((string)jsonConv["fiGender"]).Trim();
                stRegAcc.fiAddress = ((string)jsonConv["fiAddress"]).Trim();
                stRegAcc.fiCity = ((string)jsonConv["fiCity"]).Trim();
//                stRegAcc.fiZipCode = ((string)jsonConv["fiZipCode"]).Trim();
                stRegAcc.fiProvince = ((string)jsonConv["fiProvince"]).Trim();
                stRegAcc.fiMotherName = ((string)jsonConv["fiMotherName"]).Trim();
//                stRegAcc.fiUserId = ((string)jsonConv["fiUserId"]).Trim().ToUpper();
                stRegAcc.fiPassword = ((string)jsonConv["fiPassword"]).Trim().ToLower();
            }
            catch
            {
                // field tidak ditemukan atau formatnya salah
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not found", "");
            }
			if (stRegAcc.fiPhone.Length <= 5)
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "409", "Bad phone number", "");

			ReformatPhoneNumber (ref stRegAcc.fiPhone);
			ReformatPhoneNumber (ref stRegAcc.fiDistributor);

			//  Yang harus disiapkan
            // siapkan data2 untuk registrasi ke sandra
            // konversi field client ka field sandra
			// Generate account QVA dari nomor telepon 
            string sTmp = commonSettings.getString("UserIdHeader") + stRegAcc.fiPhone;
            
            jsonConv.Clear();
            jsonConv.Add("ficoCustomerGroupId", "91111");
            jsonConv.Add("ficoCustomerId", sTmp);
            jsonConv.Add("ficoCustomerName", stRegAcc.fiFirstName + " " + stRegAcc.fiLastName);
            jsonConv.Add("ficoCustomerPhone", stRegAcc.fiPhone);
            jsonConv.Add("ficoCustomerPhone2", "-");
			jsonConv.Add("ficoCustomerEmail", sTmp + @"@" +commonSettings.getString("EmailDomain"));
            jsonConv.Add("ficoCustomerBirthDate", stRegAcc.fiBirthDate);
            jsonConv.Add("ficoCustomerBirthPlace", stRegAcc.fiBirthPlace);
            jsonConv.Add("ficoCustomerCity", stRegAcc.fiCity);
            jsonConv.Add("ficoCustomerCity2", "-");
            jsonConv.Add("ficoCustomerAddress", stRegAcc.fiAddress);
            jsonConv.Add("ficoCustomerTrxAllowed", "CD");
            jsonConv.Add("ficoCustomerAddress2", stRegAcc.fiProvince);
            jsonConv.Add("ficoCustomerAddress3", "-");
//			jsonConv.Add("ficoCustomerZipCode", stRegAcc.fiZipCode);
			jsonConv.Add("ficoCustomerZipCode", "-");
            jsonConv.Add("ficoCustomerZipCode2", "-");
			jsonConv.Add("ficoCustomerCustomField1", stRegAcc.fiEmail); // ini tidak boleh di edit
			jsonConv.Add("ficoCustomerCustomField2", "-");
            jsonConv.Add("ficoCustomerCustomField3", "-");
            jsonConv.Add("ficoCustomerCustomField4", "-");
            jsonConv.Add("ficoCustomerCustomField5", "-");
            jsonConv.Add("ficoCustomerCardNumber", "-");
            jsonConv.Add("ficoCustomerCardIdentityType", "KTP");
            jsonConv.Add("ficoCustomerIdentityCardNumber", stRegAcc.fiCardIDNumber);
            //jsonConv.Add("ficoCustomerIdentityCardValidDate", "01-01-2999");
            jsonConv.Add("ficoCustomerNpwp", "-");
            jsonConv.Add("ficoCustomerNickname", "-");
            jsonConv.Add("ficoCustomerRef1", "-");
            jsonConv.Add("ficoCustomerRef2", "-");
            jsonConv.Add("ficoCustomerRef3", "-");
            jsonConv.Add("ficoCustomerGender", stRegAcc.fiGender);
            jsonConv.Add("ficoCustomerMotherName", stRegAcc.fiMotherName);
            jsonConv.Add("ficoCustomerPassword", stRegAcc.fiPassword);
            jsonConv.Add("ficoCustomerUsername", commonSettings.getString("UserIdHeader") + stRegAcc.fiPhone);
            jsonConv.Add("source", "dam");

            // konek ke host sandra untuk registrasi
			//if (localIP) sandraHost = "127.0.0.1";
			using (SandraLibs sandra = new SandraLibs(commonSettings.getInt("QvaTimeOut"),
				(commonSettings.getString("SandraUseTcpMethod").Trim().ToLower() == "true"),
				commonSettings.getString("SandraHmsUserAuth"),
				commonSettings.getString("SandraHmsSecretKey"),
				commonSettings.getString("SandraAuthId")))
            {
//                if (sandra.EditCustomer(commonSettings.getString("SandraHost"), commonSettings.getInt("SandraPort"), jsonConv))
                if (sandra.EditCustomer(
					commonSettings.getString("SandraHost"), commonSettings.getInt("SandraPort"), 
					(commonSettings.getString("SandraIsHttps").ToLower() == "true"), jsonConv))
                {
					// TODO : disini simpan di database customer
                    jsonConv.Clear();
                    jsonConv.Add("fiUserId", sTmp);
                    jsonConv.Add("fiDistributor", stRegAcc.fiDistributor);
                    jsonConv.Add("fiFirstName", stRegAcc.fiFirstName);
                    jsonConv.Add("fiLastName", stRegAcc.fiLastName);
                    jsonConv.Add("fiPhone", stRegAcc.fiPhone);
                    jsonConv.Add("fiEmail", stRegAcc.fiEmail);
                    jsonConv.Add("fiCardIDNumber", stRegAcc.fiCardIDNumber);
                    jsonConv.Add("fiBirthPlace", stRegAcc.fiBirthPlace);
                    jsonConv.Add("fiBirthDate", stRegAcc.fiBirthDate);
                    jsonConv.Add("fiGender", stRegAcc.fiGender);
                    jsonConv.Add("fiAddress", stRegAcc.fiAddress);
                    jsonConv.Add("fiCity", stRegAcc.fiCity);
                    //jsonConv.Add("fiZipCode", stRegAcc.fiZipCode);    // tidak usah tampil
                    jsonConv.Add("fiProvince", stRegAcc.fiProvince);
                    jsonConv.Add("fiMotherName", stRegAcc.fiMotherName);
                    //jsonConv.Add("fiMemberType", stRegAcc.fiMemberType);
                    // masukkan di database

                    // kirim respon ke client
                    string repl = jsonConv.JSONConstruct();
                    //jsonConv.Dispose();
                    return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", repl);
                }
                else
                {
                    string sDbg = "Sandra not OK\r\n" +
                                    "Error code: " + sandra.LastError.ServerCode + "\r\n" +
                                    "Error message: " + sandra.LastError.ServerMessage;
					LogWriter.write(this,LogWriter.logCodeEnum.ERROR,"Failed on Sandra : " + sDbg);
                    return HTTPRestDataConstruct.constructHTTPRestResponse(503, "503", "Connection failed to host", "");
                }
            }
        }

		/// <summary>
		/// Frees the account registration. For Administrator to register free qva account
		/// </summary>
		/// <returns>The account registration.</returns>
		/// <param name="clientData">Client data.</param>
		/// <param name="isWebRegistration">If set to <c>true</c> is web registration.</param>
		private string Admin_AccountRegistration(HTTPRestConstructor.HttpRestRequest clientData)
		{
			if(clientData.Body.Length==0)
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "406", "No data to process", "");
			}
			if(!jsonConv.JSONParse(clientData.Body))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "407", "Invalid data format", "");
			}
			// TAMPUNG data json dari Client
			//DateTime ttl;
			try
			{
				string dtcl = (string)jsonConv["fiBirthDate"];
				//// dd-MM-yyyy
				//DateTime ttl = new DateTime(int.Parse(dtcl.Substring(6, 4)), int.Parse(dtcl.Substring(3, 2)), int.Parse(dtcl.Substring(0, 2)));
				// yyyy-MM-dd
				DateTime ttl = new DateTime(int.Parse(dtcl.Substring(0, 4)), int.Parse(dtcl.Substring(5, 2)), int.Parse(dtcl.Substring(8, 2)));
			}
			catch
			{
				// field tidak ditemukan atau formatnya salah
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field BirthDate not found or date format error", "");
			}

			string noAccountQVA = "";
			Hashtable dataBuf = new Hashtable ();
			string userPhone = "";
			string userLogin = "";
			string userPassword = "";

			try
			{
				userLogin = ((string)jsonConv["fiUserLogin"]).Trim().ToLower();
				userPassword = ((string)jsonConv["fiLoginPassword"]).Trim();

				noAccountQVA = ((string)jsonConv["fiQvaAccountNumber"]).Trim();
				dataBuf.Add("user_id", noAccountQVA);
				dataBuf.Add("email",  noAccountQVA + @"@" +commonSettings.getString("EmailDomain"));
				dataBuf.Add("user_password", ((string)jsonConv["fiPassword"]).Trim());
				dataBuf.Add("distributor_id", "-");
				dataBuf.Add("first_name", ((string)jsonConv["fiFirstName"]).Trim());
				dataBuf.Add("last_name", ((string)jsonConv["fiLastName"]).Trim());

				userPhone = ((string)jsonConv["fiPhone"]).Trim().Replace("-", "");

				dataBuf.Add("birth_place", ((string)jsonConv["fiBirthPlace"]).Trim());
				dataBuf.Add("birth_date", ((string)jsonConv["fiBirthDate"]).Trim());	// Harus ("MM-dd-yyyy")
				dataBuf.Add("card_id_number", ((string)jsonConv["fiCardIDNumber"]).Trim());
				dataBuf.Add("gender", ((string)jsonConv["fiGender"]).Trim());		// = ? "M" : "F");
				dataBuf.Add("address", ((string)jsonConv["fiAddress"]).Trim());
				dataBuf.Add("city", ((string)jsonConv["fiCity"]).Trim());
				dataBuf.Add("zipcode", ((string)jsonConv["fiZipCode"]).Trim());
				dataBuf.Add("province", ((string)jsonConv["fiProvince"]).Trim());
				dataBuf.Add("mother_name", ((string)jsonConv["fiMotherName"]).Trim());
			}
			catch
			{
				// field tidak ditemukan atau formatnya salah
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Field not completed", "");
			}
			if((((string)jsonConv["fiGender"]).Trim()!="M") && (((string)jsonConv["fiGender"]).Trim()!="F"))
			{
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "408", "Gender should be 'M' or 'F'", "");
			}

			ReformatPhoneNumber (ref userPhone);
			dataBuf.Add ("phone", userPhone);
			string zipcode = ((string)jsonConv["fiZipCode"]).Trim();

			if (userPhone.Length < 5)
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "409", "Invalid phone number", "");
			if (zipcode.Length != 5)
				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "409", "Invalid zipcode length, 5 digits", "");

			if(!localDB.isUserHasRights(userLogin, userPassword, "administrator-freereg", out xError))
				//if (!localDB.isRootPasswordEqual(RootPhone, RootPassw, out xError))
			{
				if (xError != null)
				{
					LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Server database error");
					return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
				}
				else
				{
					// password error
					LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Login failed using userId: " + userLogin);
					return HTTPRestDataConstruct.constructHTTPRestResponse(401, "410", "Invalid user or password", "");
				}
			}

//			// Cek login Administrator
//			if (userLogin != "administrator") {
//				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "410", "Invalid user or password", "");
//			}
//			if (!localDB.cekAdminLoginWeb (userPassword)) {
//				return HTTPRestDataConstruct.constructHTTPRestResponse(400, "410", "Invalid user or password", "");
//			}

			// ==== Cukup cek di SANDRA, krn gak masuk database lokal

			// registrasi ke serverHost sandra
			// request registration ke sandra
			string errCode = "";
			string errMsg = "";
			bool isError = false;
			string hasil = "";

			dataBuf.Add("member_type", "OTHER");

			if (!isRegisteredInSandra (noAccountQVA, out isError, ref errCode, ref errMsg)) {
				if (isError) {
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, errCode, errMsg, "");
				}

				// registrasi qva
				if (!requestQVARegistration (dataBuf, ref errCode, ref errMsg)) {
					return HTTPRestDataConstruct.constructHTTPRestResponse (400, errCode, errMsg, "");
				}
			} else {
				return HTTPRestDataConstruct.constructHTTPRestResponse (400, "444", "Account already exist...", "");
			}

			string tglReg = "";
			string bankAcc = "";
			string trxAllowed = "";
			// insert table customer dengan data dari reply qva dari jsonConv
			try
			{
				// pake try krn mun udah terregistrasi di qva, isina teu sesuai nu diharapkeun
				bankAcc = (string)jsonConv["ficoCustomerBankAccount"];
				trxAllowed = (string)jsonConv["ficoCustomerTrxAllowed"];
				tglReg = (string)jsonConv["ficoRegistrationDate"]; // "2013-04-18T14:34:18.816+07:00"
				tglReg = tglReg.Substring(0, 4) + "-" + tglReg.Substring(5, 2) + "-" + tglReg.Substring(8, 2) + " " +
				         tglReg.Substring(11, 2) + ":" + tglReg.Substring(14, 2) + ":" + tglReg.Substring(17, 6);
			}
			catch
			{
				tglReg = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
				bankAcc = "-";
				trxAllowed = "CD";
			}

			// reply sukses
			jsonConv.Clear();
			jsonConv.Add("ficoUserId", noAccountQVA);
			jsonConv.Add("ficoRegistrationDate", tglReg);

			// kirim respon ke client
			string repl = jsonConv.JSONConstruct();
			return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", repl);

		}

		private string HapusUMUM()
        {
            localDB.clearUMUM(out xError);
            if (xError != null)
            {
                return HTTPRestDataConstruct.constructHTTPRestResponse(400, "492", "Server database error", "");
            }
            return HTTPRestDataConstruct.constructHTTPRestResponse(200, "00", "Success", "");
        }

		public string Process(int reqPathCode, HTTPRestConstructor.HttpRestRequest clientData)
        {
            if (reqPathCode == commonSettings.getInt("CommandAccountActivateFirstLine"))
            {
                return ActivateFirstLine(clientData);
            }
			//else if (reqPathCode == commonSettings.getInt("CommandAccountFirstlineRegistration"))
			//{
			//    // Invoice Approval
			//    using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
			//    {
			//        return AccTransac.AccountFirstlineRegistration(clientData);
			//    }
			//}
            else if (reqPathCode == commonSettings.getInt("CommandAccountHapusUmum"))
            {
                return HapusUMUM();
            }
            else if (reqPathCode == commonSettings.getInt("CommandAccountRegistration"))
            {
                // registrasi account QVA
                // cek keberadaan userID di sandra dgn inquiry, harus belum ada baru bisa registrasi
                // cek keberadaan userID di db lokal, harus belum ada baru bisa registrasi
                // kl blm ada baru buat di db lokal dengan status pending
                return AccountRegistration(clientData, false);
            }
            else if (reqPathCode == commonSettings.getInt("CommandAccountWebRegistration"))
            {
                return AccountRegistration(clientData, true);
            }
            else if (reqPathCode == commonSettings.getInt("CommandAccountWebValidation"))
            {
                // validasi registrasi dari kode registrasi yg dari sms
                return AccountWebValidation(clientData);
            }
            else if (reqPathCode == commonSettings.getInt("CommandAccountUpdate"))
            {
                // update registrasi account QVA
                return AccountEditRegistration(clientData);
            }
            else if (reqPathCode == commonSettings.getInt("CommandAccountActivation"))
            {
                return AccountActivation(clientData);
            }
            else if (reqPathCode == commonSettings.getInt("CommandAccountTransfer"))
            {
                // Transfer
                using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
                {
                    return AccTransac.AccountTransfer(clientData);
                }
                //using (QvaTransactions QVA = new QvaTransactions(commonSettings.getString("SandraHost"), commonSettings.getInt("SandraPort"), localDB))
                //{
                //    return QVA.TransferBalance(clientData);
                //}
            }
			else if (reqPathCode == commonSettings.getInt("CommandAccountRootTransfer"))
			{
				// Transfer dari dan ke semua rekening oleh Root
				using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
				{
					return AccTransac.AccountRootTransfer(clientData);
				}
				//using (QvaTransactions QVA = new QvaTransactions(commonSettings.getString("SandraHost"), commonSettings.getInt("SandraPort"), localDB))
				//{
				//    return QVA.TransferBalance(clientData);
				//}
			}
			else if (reqPathCode == commonSettings.getInt("CommandAccountFREETransfer"))
			{
				// Transfer dari dan ke semua rekening oleh Root
				using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
				{
					return AccTransac.AccountFREETransfer(clientData);
				}
				//using (QvaTransactions QVA = new QvaTransactions(commonSettings.getString("SandraHost"), commonSettings.getInt("SandraPort"), localDB))
				//{
				//    return QVA.TransferBalance(clientData);
				//}
			}
            else if (reqPathCode == commonSettings.getInt("CommandAccountInquiry"))
            {
                // inquiry account QVA
                using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
                {
                    return AccTransac.AccountInquiry(clientData);
                }
                //return AccountInquiry(clientData);
            }
            else if (reqPathCode == commonSettings.getInt("CommandAccountCashInRequest"))
            {
                // CashIn request
                using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
                {
                    return AccTransac.AccountRequestTopUp(clientData);
                }
            }
            else if (reqPathCode == commonSettings.getInt("CommandAccountCashInApproval"))
            {
                // CashIn Approval
                using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
                {
                    return AccTransac.AccountTopUpApproval(clientData);
                }
            }
            else if (reqPathCode == commonSettings.getInt("CommandAccountCashOutRequest"))
            {
                // CashOut request
                using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
                {
                    return AccTransac.AccountRequestCashOut(clientData);
                }
            }
            else if (reqPathCode == commonSettings.getInt("CommandAccountCashOutApproval"))
            {
                // CashOut approval
                using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
                {
                    return AccTransac.AccountCashOutApproval(clientData);
                }
            }
            else if (reqPathCode == commonSettings.getInt("CommandAccountInvoice"))
            {
                // Send Invoice
                //return HTTPRestDataConstruct.constructHTTPRestResponse(200, "204", "Not implemented yet", "");
                using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
                {
                    return AccTransac.AccountSendInvoice(clientData);
                }
            }
            else if (reqPathCode == commonSettings.getInt("CommandAccountInvoiceApproval"))
            {
                // Invoice Approval
                using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
                {
                    return AccTransac.AccountInvoicePayment(clientData);
                }
            }
            else if (reqPathCode == commonSettings.getInt("CommandAccountLastTransactions"))
            {
				// AccountLastTransactionHistory
                using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
                {
                    return AccTransac.AccountLastTransactionHistory(clientData);
                }
            }
            else if (reqPathCode == commonSettings.getInt("CommandAccountDetailTransaction"))
            {
				// AccountGetDetailTransaction
                using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
                {
                    return AccTransac.AccountGetDetailTransaction(clientData);
                }
            }
			else if (reqPathCode == commonSettings.getInt("CommandAccountInquiryPenampung"))
			{
				// inquiry account QVA
				using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
				{
					return AccTransac.AccountInquiryPenampung(clientData);
				}
				//return AccountInquiry(clientData);
			}
			else if (reqPathCode == commonSettings.getInt("CommandAccountLastTransactionsPenampung"))
			{
				// AccountLastTransactionHistory
				using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
				{
					return AccTransac.AccountLastTransactionHistoryPenampung(clientData);
				}
			}
			else if (reqPathCode == commonSettings.getInt("CommandAccountCreateVirtualAccount"))
			{
				// AccountLastTransactionHistory
				return Admin_AccountRegistration (clientData);
			}
			else if (reqPathCode == 123456)
			{
				// AccountLastTransactionHistory
				// Transfer dari dan ke semua rekening oleh Root
				using (AccountTransactions AccTransac = new AccountTransactions(commonSettings))
				{
					return AccTransac.TestQvaConnection(clientData);
				}
			}
            else
            {
                // reject
                return HTTPRestDataConstruct.constructHTTPRestResponse(200, "204", "Not implemented", "");
            }
        }


    }
}


