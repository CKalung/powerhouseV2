using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using LOG_Handler;
using StaticCommonLibrary;

namespace PPOBDatabase
{

            //    dbc.SetQuery("select ip, port FROM devices");
            //dbc.AddDataTbl("devices");
            //DeviceHandler.devAddress[] returnValue = null;
            //if (dbc.DbQuery("devices") > 0) // ada data dan tidak terjadi error.
            //{
            //    DataTable record = dbc.GetResultData("devices");
            //    dbc.RemoveDataTbl("devices");
            //    returnValue = new DeviceHandler.devAddress[record.Rows.Count];
            //    for (int i = 0; i < record.Rows.Count; i++)
            //    {
            //        DataRow row = record.Rows[i];
            //        DeviceHandler.devAddress dev = new DeviceHandler.devAddress();
            //        dev.IP = row.ItemArray[0].ToString();
            //        dev.Port = int.Parse(row.ItemArray[1].ToString());
            //        returnValue[i] = dev;
            //    }
            //}
            //else
            //{
            //    returnValue = new DeviceHandler.devAddress[0];

            //}

//    Query notifikasi
//    INSERT INTO customer_notification
//(mobile_phone_number_from,mobile_phone_number_to,notification_time,notification_type_code,notification_value)
//VALUES
//('628xxxxxxxx','628xxxxxxxx','YYYY-MM-DD HH:mi:ss','lookup_ke_table_customer_notification_type','ISO_NOTIFICATION_UDAH_FORMAT_JSON');

//KETerangan :
//customer_notification"."mobile_phone_number_from" : Jika kosong artinya dari server
//customer_notification"."notification_value"    : isi nya JSON

//SELECT :
//SELECT mobile_phone_number_from,mobile_phone_number_to,notification_time,notification_type_code,notification_value FROM customer_notification
//WHERE condition=condition 
//ORDER BY notification_time ASC

//Kalau udah ka kirim, pindahkeun data ka customer_notification_sent



    public class PPOBdbLibs:IDisposable
    {
        #region IDisposable Members
        private bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(!this.disposed)
            {
                if(disposing)
                {
                    // Dispose managed resources.
                    disposeAll();
                }

                disposed = true;
            }
        }

        ~PPOBdbLibs()
        {
            Dispose(false);
        }

        #endregion

        private void disposeAll() 
        {
            //Exception exc;
            //localDB.RemoveDataTable(tbl_mpAccount, out exc);
            //localDB.RemoveDataTable(tbl_mpProduct, out exc);
            //localDB.RemoveDataTable(tbl_mpNotif, out exc);
            //localDB.RemoveDataTable(tbl_mpCash, out exc);
            localDB.Dispose();
        }


        PPOBDatabase.PostGresDB localDB;
        string tbl_mpAccount = "mp_account"; // sementara gini dulu
        string tbl_mpProduct = "mp_product"; // sementara gini dulu
        string tbl_mpNotif = "mp_notif"; // sementara gini dulu
        string tbl_mpCash = "mp_cashtrx"; // sementara gini dulu
        //PublicSettings.Settings commonSettings;

        //public PPOBdbLibs(PublicSettings.Settings CommonSettings)
        //{
        //    commonSettings = CommonSettings;
        //    localDB = new PPOBDatabase.PostGresDB();
        //    localDB.ConnectionString(commonSettings.DbHost, 
        //        commonSettings.DbPort, commonSettings.DbName, commonSettings.DbUser, 
        //        commonSettings.DbPassw);
        //    localDB.AddDataTable(tbl_mpAccount);
        //    localDB.AddDataTable(tbl_mpProduct);
        //    localDB.AddDataTable(tbl_mpNotif);
        //    localDB.AddDataTable(tbl_mpCash);
        //}
        //public PPOBdbLibs(PublicSettings.Settings CommonSettings)
        //{
        //    localDB = new PPOBDatabase.PostGresDB();
        //    localDB.ConnectionString(CommonSettings.DbHost,
        //        CommonSettings.DbPort, CommonSettings.DbName, CommonSettings.DbUser,
        //        CommonSettings.DbPassw);
        //    localDB.AddDataTable(tbl_mpAccount);
        //    localDB.AddDataTable(tbl_mpProduct);
        //    localDB.AddDataTable(tbl_mpNotif);
        //    localDB.AddDataTable(tbl_mpCash);
        //}

        public PPOBdbLibs(string DbHost, int DbPort, 
            string DbName, string DbUser, string DbPassw)
        {
            localDB = new PPOBDatabase.PostGresDB();
            localDB.ConnectionString(DbHost, DbPort, DbName, DbUser, DbPassw);
            localDB.AddDataTable(tbl_mpAccount);
            localDB.AddDataTable(tbl_mpProduct);
            localDB.AddDataTable(tbl_mpNotif);
            localDB.AddDataTable(tbl_mpCash);
        }

        //public int LoadDatabase()
        //{
        //    return 0;
        //}

        public bool setFirstLineMaxCycle()
        {
            //string sql = "ALTER SEQUENCE \"public\".\"person_first_line\" INCREMENT 1  MINVALUE 0  MAXVALUE 5 RESTART 2 CACHE 1 CYCLE;"
            string sql = "ALTER SEQUENCE \"public\".\"person_first_line\" INCREMENT 1  MINVALUE 0  MAXVALUE 5 CACHE 1 CYCLE;";
            Exception ExError = null;
            localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (ExError == null);
        }

        public string sqlInsertPerson(string first_name, string last_name, string user_email, string phone, string cardIDnumber,
            string birthPlace, DateTime birthDate, bool male_gender, string address, string city,
            string zipcode, string province, string motherName, string memberType, int roleType,
            string userStatus, string activationCode, string clientAddress)
        {
            string sql = "select insert_person(";
            sql += "'" + first_name + "'," + male_gender.ToString() + ",'" + address + "','" + phone + "','" +
                user_email + "','" + clientAddress + "',to_date('" + birthDate.ToString("yyyy-MM-dd") + "', 'YYYY-MM-DD'), '" +
                birthPlace + "',1,'" + cardIDnumber + "','" + motherName + "','" + province + "','" + last_name + "')";
            return sql;
        }

        public string sqlInsertSysUsers(string userID, string password, string distributor,
            string firstName, string last_name, string email, string user_email, string phone, string cardIDnumber,
            string birthPlace, DateTime birthDate, string gender, string address, string city,
            string zipcode, string province, string motherName, string memberType, int roleType,
            string userStatus, string activationCode, string clientAddress)
        {
            string sql = "INSERT INTO sys_users ( user_id, sgid, user_passwd, status, " +
                "is_active,lastin,lastout,pid) values (";
            sql += "'" + userID + "','" + gender + "','" + address + "','" + phone + "','" +
                user_email + "','" + clientAddress + "',to_date('" + birthDate.ToString("yyyy-MM-dd") + "', 'YYYY-MM-DD'), '" +
                birthPlace + "',1,'" + cardIDnumber + "','" + motherName + "','" + province + "','" + last_name + "')";
            return sql;
        }

        public string createNewCustomer(string AppID, string userID, string password,
            string uplinkPhone, string firstName, string lastName, string email, string userEmail,
            string phone, string cardIDnumber, string birthPlace, DateTime birthDate, string gender,
            string address, string city, string zipcode, string province, string motherName, string memberType,
            string activationCode, string clientHost, bool web_registration, out Exception ExError)
        {
            //string squery = "insert into mp_account (user_id, user_password, " +
            //                "distributor_phone, first_name, last_name, email, phone, card_id_number, " +
            //                "birth_place, birth_date, gender, address, city, zipcode, province, " +
            //                "mother_name, member_type, role_id, user_status, activation_code, user_email, registration_time) values (";  // diakhiri ")";
            //squery += "'" + userID + "','" + password + "','" + distributor + "','" +
            //            firstName + "','" + last_name + "','" + email + "','" + phone + "','" +
            //            cardIDnumber + "','" + birthPlace + "',to_date('" + birthDate.ToString("yyyy-MM-dd") + "', 'YYYY-MM-DD'), '" +
            //            gender + "','" + address + "','" + city + "','" + zipcode + "','" + province + "','" +
            //            motherName + "','" + memberType + "'," + roleType + ",'" + userStatus + "','" + activationCode + "','" + 
            //            user_email + "','"+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+ "')";
            //string squery = "insert into customer (no_qva, up_link, card_number, " +
            //                "name, status, registered_timestamp, phone, address, password, " +
            //                "username, bank_account, email, pin, custom_field1, custom_field2, " +
            //                "custom_field3, custom_field4, custom_field5, address2, address3, "+
            //                "city, ref1, ref2, ref3, trx_allowed, birth_date, zipcode, group_id,"+
            //                "create_by,modify_by,create_time,modify_time,host,last_host,"+
            //                "sys_member_id ) values (";  // diakhiri ")";
            //squery += "'" + userID + "','" + password + "','" + distributor + "','" +


            //select add_account(
            //        '001', 'vuser_id',
            //            'vfirst_name', 'vlast_name', 'vuser_email', 'vphone','vcard_id_number',
            //            'vbirth_place', to_date('2013-11-17', 'YYYY-MM-DD'), TRUE, 'vaddress text', 'vcity text',
            //            'vzipc', 'vprovince text', 'vmother_name text', 'FIRSTLINE', 
            //            'actvcode', 'vup_link_phone', 'vpassword text', 'vclient_host');

            //delete from person where name = 'vfirst_name';   -- sekali perintah hapus ini, terhapus juga di table sys_users dan sys_member
            //select * from person;
            //select * from sys_users;
            //select * from sys_member;

            // jika sukses, return nya nomor penyelia 
            // add_account(vsuper_distributor_id text, vuser_id text, vfirst_name text, vlast_name text, 
            // vuser_email text, vphone text, vcard_id_number text, vbirth_place text, 
            // vbirth_date date, vgender_male boolean, vaddress text, vcity text, 
            // vzipcode text, vprovince text, vmother_name text, member_type text, 
            // vactivation_code text, vup_link_phone text, vpassword text, vclient_host text)
            string squery = "SELECT add_account_all('";
            squery += AppID + "','" + userID + "','" + firstName + "','" + lastName + "','" +
                email + "','" + userEmail + "','" + phone + "','" + cardIDnumber + "','" + birthPlace + "'," +
                "to_date('" + birthDate.ToString("yyyy-MM-dd") + "', 'YYYY-MM-DD'), " +
                (gender == "M").ToString() + ",'" + address + "','" + city + "','" + zipcode + "','" +
                      province + "','" + motherName + "','" + memberType + "','" +
                activationCode + "','" + uplinkPhone + "','" + password + "','" + clientHost + "'," +
                web_registration.ToString() + ");";

            ExError = null;
            localDB.ExecQuerySql(squery, tbl_mpAccount, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + squery +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return "";
            }
            //if (localDB.GetDataItem(tbl_mpAccount, 0, 1) == null)
            //{
            LogWriter.show(this, "Query add account: " + squery);
            //}
            return (((string)localDB.GetDataItem(tbl_mpAccount, 0, "add_account_all")).Trim());
        }

        public string getPhoneFromIndex(int user_idx, out Exception ExError)
        {
            localDB.clearDataTable(tbl_mpAccount);
            ExError = null;
            string sql = "SELECT mobile_phone_number " +
                         "FROM person " +
                         "WHERE id = " + user_idx.ToString() + ";";
            int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
            if (i == 0) return "";
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return "";
            }
            return ((string)localDB.GetDataItem(tbl_mpAccount, 0, "mobile_phone_number")).Trim();
        }

        public bool updateCustomerActivationCode(int userId, string activationCode, out Exception ExError)
        {
            ExError = null;
            int i = localDB.ExecNonQuerySql(
                "UPDATE sys_member SET activation_code = '" + activationCode + 
                "' WHERE pid = " + userId.ToString() + ";", out ExError);
            return (i > 0);
        }

		public bool updateCitilinkPassword(string newCitilinkPassword, out Exception ExError)
		{
			ExError = null;
			int i = localDB.ExecNonQuerySql(
				"UPDATE configuration SET value = '" + newCitilinkPassword + 
				"' WHERE name = 'Citilink_AgenPassword';", out ExError);
			return (i > 0);
		}

        public int loadCustomer(string userPhone, ref Hashtable dataBuf, out Exception ExError)
        {
            dataBuf.Clear();
            localDB.clearDataTable(tbl_mpAccount);
            ExError = null;
			string sql = "SELECT a.id,c.user_id, c.user_passwd, b.up_link_id, a.name as first_name, a.last_name, b.email, " +
			             "a.mobile_phone_number as phone, a.card_id_number, a.birth_place, "+
                         //"to_char(a.birth_date, 'YYYY-MM-DD HH24:MI:SS') as birth_date, " +
                         "a.birth_date, " +
                         "a.gender, a.address, a.city, a.zipcode, a.province, a.mother_name, b.member_type_id, a.email_address, " +
			             "b.member_status_id, b.activation_code, a.web_registration " +
			             "FROM person a JOIN sys_member b ON a.id = b.pid JOIN sys_users c ON a.id=c.pid " +
			             "WHERE a.mobile_phone_number = '" + userPhone + "' LIMIT 1;";
			int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
            if(i==0) return 0;
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return 0;
            }
            dataBuf.Add("pid", ((int)localDB.GetDataItem(tbl_mpAccount, 0, "id")));
            dataBuf.Add("user_id", ((string)localDB.GetDataItem(tbl_mpAccount, 0, "user_id")).Trim());
            dataBuf.Add("user_password", ((string)localDB.GetDataItem(tbl_mpAccount, 0, "user_passwd")).Trim());
			dataBuf.Add("distributor_id", ((int)localDB.GetDataItem(tbl_mpAccount, 0, "up_link_id")));
            dataBuf.Add("first_name", ((string)localDB.GetDataItem(tbl_mpAccount, 0, "first_name")).Trim());
            dataBuf.Add("last_name", ((string)localDB.GetDataItem(tbl_mpAccount, 0, "last_name")).Trim());
            dataBuf.Add("email", ((string)localDB.GetDataItem(tbl_mpAccount, 0, "email")).Trim());
            dataBuf.Add("phone", ((string)localDB.GetDataItem(tbl_mpAccount, 0, "phone")).Trim());
            dataBuf.Add("card_id_number", ((string)localDB.GetDataItem(tbl_mpAccount, 0, "card_id_number")).Trim());
            dataBuf.Add("birth_place", ((string)localDB.GetDataItem(tbl_mpAccount, 0, "birth_place")).Trim());
            dataBuf.Add("birth_date", ((DateTime)localDB.GetDataItem(tbl_mpAccount, 0, "birth_date")).ToString("MM-dd-yyyy"));
            //dataBuf.Add("birth_date", (DateTime)localDB.GetDataItem(tbl_mpAccount, 0, "birth_date"));
            dataBuf.Add("gender", ((bool)localDB.GetDataItem(tbl_mpAccount, 0, "gender")) ? "M" : "F");
            dataBuf.Add("address", ((string)localDB.GetDataItem(tbl_mpAccount, 0, "address")).Trim());
            dataBuf.Add("city", ((string)localDB.GetDataItem(tbl_mpAccount, 0, "city")).Trim());
            dataBuf.Add("zipcode", ((string)localDB.GetDataItem(tbl_mpAccount, 0, "zipcode")).Trim());
            dataBuf.Add("province", ((string)localDB.GetDataItem(tbl_mpAccount, 0, "province")).Trim());
            dataBuf.Add("mother_name", ((string)localDB.GetDataItem(tbl_mpAccount, 0, "mother_name")).Trim());
			dataBuf.Add("member_type", ((int)localDB.GetDataItem(tbl_mpAccount, 0, "member_type_id")));
			//dataBuf.Add("role_id", ((int)localDB.GetDataItem(tbl_mpAccount, 0, "role_id")).ToString());
			dataBuf.Add("user_status", ((int)localDB.GetDataItem(tbl_mpAccount, 0, "member_status_id")));
			dataBuf.Add("activation_code", ((string)localDB.GetDataItem(tbl_mpAccount, 0, "activation_code")).Trim());
            dataBuf.Add("user_email", ((string)localDB.GetDataItem(tbl_mpAccount, 0, "email_address")).Trim());
            dataBuf.Add("web_registration", ((bool)localDB.GetDataItem(tbl_mpAccount, 0, "web_registration")));
            return 1;
        }

        public bool insertCustomer(int pid, string qva_number, int uplink_id, 
            string regTimeStamp, string bank_account, string pin, string trxAllowed,
            string group_id, string host, bool fweb_registration, out Exception ExError)
        {
            ExError = null;
            string sql = "INSERT INTO customer ( " +
                          "pid,no_qva,up_link_id,registered_timestamp," +
                          "bank_account,pin,trx_allowed,group_id,host, web_registration" +
                          ")" +
                          "VALUES (";
            sql += pid.ToString() + ",'" + qva_number + "'," + uplink_id.ToString() + ",'" +
                regTimeStamp + "','" + bank_account + "','" + pin + "','CD','" + group_id + "','" +
                host + "'," + fweb_registration.ToString() + ")";
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }

            return (i > 0);
        }

        public bool removeUser(string phoneNumber, out Exception ExError)
        {
            ExError = null;
            string sql = "DELETE FROM terminal_conn WHERE mobile_phone_number = '" + phoneNumber + "';";
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            sql = "DELETE FROM person WHERE mobile_phone_number = '" + phoneNumber + "';";
            i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

        public bool isPhoneExist(string mobilePhone, out Exception ExError)
        {
            ExError = null;
            //string sql = "select user_id from mp_account where phone = '" + phone + "'";
            //Console.WriteLine(sql);
            string sql = "SELECT id FROM person WHERE mobile_phone_number = '" + mobilePhone + "'";
            int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

        public bool isAccountExist(string mobilePhone, out Exception ExError)
        {
            ExError = null;
            string sql = "SELECT id FROM person WHERE mobile_phone_number = '" + mobilePhone + "'";
            int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

        public bool isAccountExistById(string qvaId, out Exception ExError)
        {
            ExError = null;
            string sql = "SELECT pid FROM customer WHERE no_qva = '" + qvaId + "' LIMIT 1";
            int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

        public bool clearUMUM(out Exception ExError)
        {
            ExError = null;
			string sql = "DELETE FROM person WHERE id NOT IN " +
			             "(SELECT a.pid FROM sys_member a WHERE a.member_type_id=2)" +
			             "AND person.persontype=1;";
			int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

        public bool addHeartBeatLog(string noHP, string clientHost, out Exception ExError)
        {
            ExError = null;
            //INSERT INTO terminal_conn (mobile_phone_number,conn_time,client_host_port) VALUES ('081218877246',NOW(),'10.90.20.10 3600')
            string sql = "INSERT INTO terminal_conn (mobile_phone_number,conn_time,client_host_port) VALUES ('"+noHP+"',NOW(),'"+clientHost+"')";
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            return (i > 0);
        }
		public bool updateCustomerStatusToActive(string userPhone, out Exception ExError)
		{
			return updateCustomerStatus (userPhone, 2, out ExError);
		}

		public bool updateCustomerStatus(string userPhone, int newStatusCode, out Exception ExError)
        {
            ExError = null;
            string sql = "UPDATE sys_member SET member_status_id = " + 
                newStatusCode.ToString() + 
                " WHERE pid = (SELECT id FROM person WHERE mobile_phone_number = '" + 
                userPhone + "');";
			int i = localDB.ExecNonQuerySql (sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

		public bool isPhoneExistAndActive(string userPhone, out Exception ExError)
        {
            ExError = null;
			if(!isUserPasswordValid(userPhone)) return false;
			string sql = "SELECT a.id FROM   person a JOIN sys_member b ON b.pid=a.id " +
			             "WHERE a.mobile_phone_number = '"+ userPhone +"' AND b.member_status_id = 2 LIMIT 1";
			int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

        public bool isDistributorExist(string distributorPhone, out Exception ExError)
        {
			return isPhoneExistAndActive (distributorPhone, out ExError);
        }

		// Apakah userPhone =
//		public bool isMemberPhoneEqual(string userPhone, string memberPhone, out Exception ExError)
//        {
//            ExError = null;
//            int i = localDB.ExecQuerySql("select user_id from mp_account where user_id = '" + userId + "' and phone = '" + memberPhone + "'", tbl_mpAccount, out ExError);
//            return (i > 0);
//        }

//		public bool isFirstLine(string userPhone, out Exception ExError)
//        {
//            ExError = null;
//			int i = localDB.ExecQuerySql("SELECT member_type_id from sys_member where user_id = '" + userId + "'", tbl_mpAccount, out ExError);
//            if (i == 0) return false;
//            string memberType = ((string)(localDB.GetDataItem(tbl_mpAccount, 0, "member_type"))).Trim();
//            if (memberType == "FIRSTLINE") return true;
//            else return false;
//        }

		/// <summary>
		/// Ises the user password valid. To banned SQL Injection method
		/// </summary>
		/// <returns><c>true</c>, if user password valid was ised, <c>false</c> otherwise.</returns>
		/// <param name="data">Data.</param>
		private bool isUserPasswordValid(string data)
		{
			if (data.Contains (" ") || data.Contains ("'") || data.Contains ("\"") || data.Contains ("\r") || data.Contains ("\n")
				|| data.Contains ("\t")|| data.Contains ("-"))
				return false;
			else
				return true;
		}
		private bool isUserPasswordValid(string username, string password){
			if (!isUserPasswordValid (username))
				return false;
			return isUserPasswordValid (password);
		}

		/// <summary>
		/// Ises the user has the rights.
		/// </summary>
		/// <returns><c>true</c>, if user has right was ised, <c>false</c> otherwise.</returns>
		/// <param name="userId">User identifier.</param>
		/// <param name="userPassword">User password.</param>
		/// <param name="rights">Rights. Seperti "administrator-freereg" atau "administrator-free_transfer"</param>
		/// <param name="ExError">Ex error.</param>
		public bool isUserHasRights(string userId, string userPassword, string rights, out Exception ExError){
//			SELECT c.module_id FROM sys_group a 
//			INNER JOIN sys_users b ON b.sgid = a.id
//			                       INNER JOIN sys_group_d c ON c.sgid = a.id
//			                       WHERE 
//			                       c.module_id = 'administrator-freereg' AND 
//			                       b.user_id = 'administrator' AND 
//			                       b.user_passwd = '81dc9bdb52d04dc20036dbd8313ed055';

			string sql = "SELECT c.module_id FROM sys_group a " +
			             "INNER JOIN sys_users b ON b.sgid = a.id " +
			             "INNER JOIN sys_group_d c ON c.sgid = a.id " +
			             "WHERE c.module_id = '" + rights + "' AND " +
			             "b.user_id = '" + userId + "' AND " +
			             "b.user_passwd = '"+ userPassword +"';";
			int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			return (i > 0);
		}

		public bool isRootPasswordEqual(string rootPhone, string rootpassword, out Exception ExError)
		{
			ExError = null;
			if(!isUserPasswordValid(rootPhone)) return false;
			string sql = "SELECT a.id FROM person a JOIN sys_users b ON b.pid=a.id " +
			             "JOIN sys_member c ON c.pid=a.id " +
			             "WHERE a.mobile_phone_number = '" + rootPhone + "' " +
			             "AND b.user_passwd = '" + rootpassword + "' " +
			             "AND c.member_type_id=0 AND c.member_status_id=2 LIMIT 1";
			int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
			//Console.WriteLine(sql + "\r\n ======= " + i.ToString());
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			if (i > 0)
				CommonLibrary.SessionResetTimeOut (rootPhone);
			return (i > 0);
		}

		public bool isUserPasswordEqual(string userPhone, string password, out Exception ExError)
        {
            ExError = null;
			if(!isUserPasswordValid(userPhone)) return false;
			if(!isUserPasswordValid(password)) return false;
			string sql = "SELECT a.id FROM person a JOIN sys_users b ON b.pid=a.id " +
			             "WHERE a.mobile_phone_number = '" + userPhone + "' " +
			             "AND b.user_passwd = '" + password + "' LIMIT 1";
			int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
            //Console.WriteLine(sql + "\r\n ======= " + i.ToString());
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
			if (i > 0)
				CommonLibrary.SessionResetTimeOut (userPhone);

            return (i > 0);
        }

        public bool changeUserPassword(string userPhone, string newPassword, out Exception ExError)
        {
            //select a.user_passwd from sys_users a JOIN person b ON a.pid = b.id
            //where b.mobile_phone_number = '6287777100357'
            //UPDATE sys_users SET user_passwd = '4124bc0a9335c27f086f24ba207a4912'
            //WHERE pid = (SELECT id FROM person WHERE mobile_phone_number = '6287777100357');
            ExError = null;
			if(!isUserPasswordValid(userPhone)) return false;
			if(!isUserPasswordValid(newPassword)) return false;
            string sql = "UPDATE sys_users SET user_passwd = '" + newPassword + "' " +
                " WHERE pid = (SELECT id FROM person WHERE mobile_phone_number = '" + userPhone + "');";
            int i = localDB.ExecNonQuerySql(sql , out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
			if (i > 0)
				CommonLibrary.SessionResetTimeOut (userPhone);
            return (i > 0);
        }

		public bool isActivationCodeEqual(string userPhone, string activationCode, out Exception ExError)
        {
            ExError = null;
			string sql = "SELECT a.id FROM person a JOIN sys_member b ON b.pid=a.id " +
			             "WHERE a.mobile_phone_number = '" + userPhone + "' " +
			             "AND b.activation_code = '" + activationCode + "' LIMIT 1";
			int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
			if (i > 0)
				CommonLibrary.SessionResetTimeOut (userPhone);
            return (i > 0);
        }

//        public string getFirstLinePhone(out Exception ExError)
//        {
//            ExError = null;
//            int i = localDB.ExecQuerySql("SELECT get_first_line_order()", tbl_mpAccount, out ExError);
//            if (ExError != null) return "";
//            if (i <= 0)
//            {
//                ExError = new Exception("No FirstLine distributor");
//                return "";
//            }
//            return ((string)(localDB.GetDataItem(tbl_mpAccount, 0, 1))).Trim();
//        }

		public string getUserStatus(string userPhone, out Exception ExError)
        {
            ExError = null;
			string sql = "SELECT c.name from member_status c where c.id = " +
			             "(SELECT b.member_status_id FROM person a JOIN sys_member b " +
			             "ON b.pid=a.id WHERE a.mobile_phone_number = '" + userPhone + "' LIMIT 1)";
            //Console.WriteLine("DEBUG TESxx " + (tbl_mpAccount == null).ToString());
            int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return "";
            }
            if (i <= 0)
            {
                return "";
            }
            string sAct = (string)(localDB.GetDataItem(tbl_mpAccount, 0, "name"));
            if (sAct == null) return "";
            return (sAct).Trim();
        }

        public Hashtable getToplineInfo(out Exception ExError)
        {
            ExError = null;
            string sql = "SELECT a.name as first_name, a.last_name as last_name,  " +
                 "b.member_status_id as user_status, a.mobile_phone_number, c.no_qva as no_qva " +
                 "FROM person a  " +
                 "JOIN sys_member b ON b.pid=a.id  " +
                 "JOIN customer c ON c.pid=a.id  " +
                 "WHERE b.member_type_id = 0 LIMIT 1;";

            int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return null;
            }
            if (i <= 0)
            {
                return null;
            }
            Hashtable hasil = new Hashtable();
            hasil.Add("userId", ((string)(localDB.GetDataItem(tbl_mpAccount, 0, "no_qva"))).Trim());
            hasil.Add("firstName", ((string)(localDB.GetDataItem(tbl_mpAccount, 0, "first_name"))).Trim());
            hasil.Add("lastName", ((string)(localDB.GetDataItem(tbl_mpAccount, 0, "last_name"))).Trim());
            hasil.Add("mobilephone", ((string)(localDB.GetDataItem(tbl_mpAccount, 0, "mobile_phone_number"))).Trim());
            hasil.Add("userStatus", ((int)(localDB.GetDataItem(tbl_mpAccount, 0, "user_status"))));
            return hasil;
        }

        public Hashtable getLoginInfoTopline(string ToplinePhone, out Exception ExError)
        {
            ExError = null;
            string sql = "SELECT a.name as first_name, a.last_name as last_name,  " +
                 "b.member_status_id as user_status " +
            //     -- , c.no_qva as no_qva
	             "FROM person a  " +
 	             "JOIN sys_member b ON b.pid=a.id  " +
            //     --	 JOIN customer c ON c.pid=a.id  " +
                 "WHERE a.mobile_phone_number = '" + ToplinePhone + "' " +
                 "AND b.member_type_id = 0 LIMIT 1;";

            int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return null;
            }
            if (i <= 0)
            {
                return null;
            }
            Hashtable hasil = new Hashtable();
            //hasil.Add("userId", ((string)(localDB.GetDataItem(tbl_mpAccount, 0, "no_qva"))).Trim());
            //hasil.Add("userId", sadfsadfs "99"+userPhone);
            hasil.Add("firstName", ((string)(localDB.GetDataItem(tbl_mpAccount, 0, "first_name"))).Trim());
            hasil.Add("lastName", ((string)(localDB.GetDataItem(tbl_mpAccount, 0, "last_name"))).Trim());
            hasil.Add("userStatus", ((int)(localDB.GetDataItem(tbl_mpAccount, 0, "user_status"))));
            return hasil;
        }

        public Hashtable getLoginInfoByUserPhone(string userPhone, out Exception ExError)
        {
            ExError = null;
			string sql = "SELECT a.name as first_name, a.last_name as last_name, " +
                         "b.member_status_id as user_status, c.no_qva as no_qva " +
                         "FROM person a " +
                         "JOIN sys_member b ON b.pid=a.id " +
			             "JOIN customer c ON c.pid=a.id " +
			             "WHERE a.mobile_phone_number = '" + userPhone + "' LIMIT 1";
			int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return null;
            }
            if (i <= 0)
            {
                return null;
            }
            Hashtable hasil = new Hashtable();
            hasil.Add("userId", ((string)(localDB.GetDataItem(tbl_mpAccount, 0, "no_qva"))).Trim());
			//hasil.Add("userId", sadfsadfs "99"+userPhone);
            hasil.Add("firstName", ((string)(localDB.GetDataItem(tbl_mpAccount, 0, "first_name"))).Trim());
            hasil.Add("lastName", ((string)(localDB.GetDataItem(tbl_mpAccount, 0, "last_name"))).Trim());
            hasil.Add("userStatus", ((int)(localDB.GetDataItem(tbl_mpAccount, 0, "user_status"))));
            return hasil;
        }

        //public bool getProviderProductInfo(string productCode, ref ProviderProductInfo ProviderProduct)
        //{
        //    Exception ExError = null;
        //    ProviderProduct = getProviderProductCode(productCode, out ExError);
        //    if (ExError != null) return false;
        //    else return true;
        //}

		public bool getPriceAndFeeProduct(string productCode, string providerCode, 
			decimal baseAmount, ref string ProductName, ref int SellPrice, 
			ref int AdminFee, ref int DistributorFee, 
			out Exception ExError)
		{
			try {
				string sql = "SELECT b.name as prd_name, "
				            + "d.name as fee_name, "
				            + "x.total_price as sell_price, "
				            + "x.base_price as base_price, "
				            + "c.value as fee_value, "
				            + "COALESCE(a.feetype, false) as feetype, "
				            + "COALESCE(a.percent_value,0) as percent_value, "
				            + "c.fee_type "

				            + "FROM sys_fee a "
				            + "INNER JOIN sys_fee_d c ON c.sfid=a.id "
				            + "INNER JOIN fee_component d ON d.id=c.fcid "
				            + "INNER JOIN product b ON b.id=a.product_id "
				            + "INNER JOIN cogs_price x ON x.product_id=a.product_id "
				            + "WHERE "
				            + "b.code='" + productCode.Trim () + "' "//          -- kode barang powerhouse
				            + "AND a.provider_code = '" + providerCode + "' "//    -- kode provider
				            + "AND a.is_active=TRUE "
				            + "AND a.end_fee_date IS NULL "
				            + "AND a.is_active = 't' "
				            + "AND a.end_fee_date IS NULL "
				            + "AND x.is_active = 't' "
				            + "AND c.fcid IN (1,6) "//                -- tipe fee
				             + "AND b.approval_status=3 "
				             + "AND a.approval_status=3 "
				             + "AND x.approval_status=3 "
				             + "ORDER BY d.id;";
		

				int i = localDB.ExecQuerySql (sql, tbl_mpAccount, out ExError);
				if (ExError != null) {
					LogWriter.showDEBUG (this, ExError.Message);
					return false;
				}
				if (i < 1) {
					return false;
				}

				ProductName = ((string)(localDB.GetDataItem (tbl_mpAccount, 0, "prd_name"))).ToString ();
				string sSellPrice = (localDB.GetDataItem (tbl_mpAccount, 0, "sell_price")).ToString ();
				decimal dSellPrice = decimal.Parse (sSellPrice);
				SellPrice = decimal.ToInt32 (Math.Floor (dSellPrice));

				string sFeeFixValue = localDB.GetDataItem (tbl_mpAccount, 0, "fee_value").ToString ();
				string sFeePercent = localDB.GetDataItem (tbl_mpAccount, 0, "percent_value").ToString ();
				bool bFeeType = ((bool)localDB.GetDataItem (tbl_mpAccount, 0, "feetype"));
				string sCustFeeFixValue = localDB.GetDataItem (tbl_mpAccount, 1, "fee_value").ToString ();
				string sCustFeePercent = localDB.GetDataItem (tbl_mpAccount, 1, "percent_value").ToString ();
				bool bCustFeeType = ((bool)localDB.GetDataItem (tbl_mpAccount, 1, "feetype"));

				if (bFeeType) {
					// percentage
					decimal percent = decimal.Parse (sFeePercent);
					AdminFee = decimal.ToInt32 (Math.Floor ((percent * baseAmount) / 100));
					//AdminFee = decimal.ToInt32(Math.Floor(iFeePercent * baseAmount));
				} else {
					// fix value
					decimal fixVal = decimal.Parse (sFeeFixValue);
					AdminFee = decimal.ToInt32 (Math.Floor (fixVal));
					//AdminFee = iFeeFixValue;
				}
				if (bCustFeeType) {
					// percentage
					decimal cpercent = decimal.Parse (sCustFeePercent);
					DistributorFee = decimal.ToInt32 (Math.Floor ((cpercent * baseAmount) / 100));
				} else {
					// fix value
					decimal cfixVal = decimal.Parse (sCustFeeFixValue);
					DistributorFee = decimal.ToInt32 (Math.Floor (cfixVal));
				}
				return true;
			} catch (Exception exErr) {
				ExError = exErr;
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Error on get price : "+exErr.getCompleteErrMsg ());
				return false;
			}

		}

		public bool getAdminFeeAndCustomerFee(string productCode, string appId,  //string providerCode, 
			decimal baseAmount, ref int AdminFee, ref int AgentFee, out Exception ExError)
        {
            string sql = "SELECT d.id as fee_id, d.name as fee_name, b.value as fee_value, " +
                            "COALESCE(a.feetype, false) as feetype," +
                            "COALESCE(a.percent_value,0) as percent_value, " +
                            "b.fee_type FROM sys_fee a " +
                            "INNER JOIN sys_fee_d b ON b.sfid=a.id " +
                            "INNER JOIN fee_component d ON b.fcid=d.id " +
                            "INNER JOIN product c ON c.id=a.product_id " +
                            "WHERE c.code = '" + productCode.Trim() + "' AND a.is_active='t' " +
				//"AND a.provider_code = '" + providerCode + "' " +
							"AND a.partner_code = '" + appId + "' " +
			             //                            "AND d.id = 1 " +
			             	"AND d.id IN (1,6) " +
                            "ORDER BY fee_id;";
            int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
            if (ExError != null)
            {
                LogWriter.showDEBUG(this, ExError.Message);
                return false;
            }
            if (i < 1)
            {
                return false;
            }

			string sFeeFixValue = localDB.GetDataItem(tbl_mpAccount, 0, "fee_value").ToString();
			string sFeePercent = localDB.GetDataItem(tbl_mpAccount, 0, "percent_value").ToString();
			bool bFeeType = ((bool)localDB.GetDataItem(tbl_mpAccount, 0, "feetype"));
			string sCustFeeFixValue = localDB.GetDataItem(tbl_mpAccount, 1, "fee_value").ToString();
			string sCustFeePercent = localDB.GetDataItem(tbl_mpAccount, 1, "percent_value").ToString();
			bool bCustFeeType = ((bool)localDB.GetDataItem(tbl_mpAccount, 1, "feetype"));

            if (bFeeType)
            {
                // percentage
                decimal percent = decimal.Parse(sFeePercent);
                AdminFee = decimal.ToInt32(Math.Floor((percent * baseAmount)/100));
                //AdminFee = decimal.ToInt32(Math.Floor(iFeePercent * baseAmount));
            }
            else
            {
                // fix value
                decimal fixVal = decimal.Parse(sFeeFixValue);
                AdminFee = decimal.ToInt32(Math.Floor(fixVal));
                //AdminFee = iFeeFixValue;
            }
			if (bCustFeeType)
			{
				// percentage
				decimal cpercent = decimal.Parse(sCustFeePercent);
				AgentFee = decimal.ToInt32(Math.Floor((cpercent * baseAmount)/100));
			}
			else
			{
				// fix value
				decimal cfixVal = decimal.Parse(sCustFeeFixValue);
				AgentFee = decimal.ToInt32(Math.Floor(cfixVal));
			}
            return true;
        }

		public bool getPercentAdminFee(string productCode, string providerCode, 
			ref decimal percentFee, out Exception ExError)
		{
			string sql = "SELECT d.id as fee_id, d.name as fee_name, b.value as fee_value, " +
				"COALESCE(a.feetype, false) as feetype," +
				"COALESCE(a.percent_value,0) as percent_value, " +
				"b.fee_type FROM sys_fee a " +
				"INNER JOIN sys_fee_d b ON b.sfid=a.id " +
				"INNER JOIN fee_component d ON b.fcid=d.id " +
				"INNER JOIN product c ON c.id=a.product_id " +
				"WHERE c.code = '" + productCode.Trim() + "' AND a.is_active='t' " +
				"AND a.provider_code = '" + providerCode + "' " +
				"AND d.id = 1 " +
				"ORDER BY fee_id;";
			int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, sql + "\r\n" +ExError.Message);
				return false;
			}
			if (i < 1)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.INFO, "Query return no data\r\n" + sql);
				return false;
			}

			string sFeePercent = localDB.GetDataItem(tbl_mpAccount, 0, "percent_value").ToString();
			bool bFeeType = ((bool)localDB.GetDataItem(tbl_mpAccount, 0, "feetype"));

			if (bFeeType)
			{
				// percentage
				percentFee = decimal.Parse(sFeePercent);
				//AdminFee = decimal.ToInt32(Math.Floor((percent * baseAmount)/100));
				return true;
			}
			else
			{
				LogWriter.write(this, LogWriter.logCodeEnum.INFO, "Query return feetype = false\r\n" + sql);
				return false;
			}
		}

		public bool getAdminFeeAndCustomerFee(string productCode, string appId, //string providerCode, 
			decimal baseAmount, ref int AdminFee, out Exception ExError)
		{
			string sql = "SELECT d.id as fee_id, d.name as fee_name, b.value as fee_value, " +
			             "COALESCE(a.feetype, false) as feetype," +
			             "COALESCE(a.percent_value,0) as percent_value, " +
			             "b.fee_type FROM sys_fee a " +
			             "INNER JOIN sys_fee_d b ON b.sfid=a.id " +
			             "INNER JOIN fee_component d ON b.fcid=d.id " +
			             "INNER JOIN product c ON c.id=a.product_id " +
			             "WHERE c.code = '" + productCode.Trim() + "' AND a.is_active='t' " +
						 //"AND a.provider_code = '" + providerCode + "' " +
						 "AND a.partner_code = '" + appId + "' " +
			             "AND d.id = 1 " +
			             "ORDER BY fee_id;";

			int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
			if (ExError != null)
			{
				LogWriter.showDEBUG(this, ExError.Message);
				return false;
			}
			if (i < 1)
			{
				LogWriter.showDEBUG (this, "No RESULT SQL: \r\n" + sql);
				return false;
			}

			string sFeeFixValue = localDB.GetDataItem(tbl_mpAccount, 0, "fee_value").ToString();
			string sFeePercent = localDB.GetDataItem(tbl_mpAccount, 0, "percent_value").ToString();
			bool bFeeType = ((bool)localDB.GetDataItem(tbl_mpAccount, 0, "feetype"));

			if (bFeeType)
			{
				// percentage
				decimal percent = decimal.Parse(sFeePercent);
				AdminFee = decimal.ToInt32(Math.Floor((percent * baseAmount)/100));
				//AdminFee = decimal.ToInt32(Math.Floor(iFeePercent * baseAmount));
			}
			else
			{
				// fix value
				decimal fixVal = decimal.Parse(sFeeFixValue);
				AdminFee = decimal.ToInt32(Math.Floor(fixVal));
				//AdminFee = iFeeFixValue;
			}
			return true;
		}

		public bool getAdminFeeAndCustomerFee(string productCode, string appId, //string providerCode, 
			decimal baseAmount, ref decimal AdminFee, out Exception ExError)
		{
			string sql = "SELECT d.id as fee_id, d.name as fee_name, b.value as fee_value, " +
				"COALESCE(a.feetype, false) as feetype," +
				"COALESCE(a.percent_value,0) as percent_value, " +
				"b.fee_type FROM sys_fee a " +
				"INNER JOIN sys_fee_d b ON b.sfid=a.id " +
				"INNER JOIN fee_component d ON b.fcid=d.id " +
				"INNER JOIN product c ON c.id=a.product_id " +
				"WHERE c.code = '" + productCode.Trim() + "' AND a.is_active='t' " +
				//"AND a.provider_code = '" + providerCode + "' " +
				"AND a.partner_code = '" + appId + "' " +
				"AND d.id = 1 " +
				"ORDER BY fee_id;";

			int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
			if (ExError != null)
			{
				LogWriter.showDEBUG(this, ExError.Message);
				return false;
			}
			if (i < 1)
			{
				LogWriter.showDEBUG (this, "No RESULT SQL: \r\n" + sql);
				return false;
			}

			string sFeeFixValue = localDB.GetDataItem(tbl_mpAccount, 0, "fee_value").ToString();
			string sFeePercent = localDB.GetDataItem(tbl_mpAccount, 0, "percent_value").ToString();
			bool bFeeType = ((bool)localDB.GetDataItem(tbl_mpAccount, 0, "feetype"));

			if (bFeeType)
			{
				// percentage
				decimal percent = decimal.Parse(sFeePercent);
				AdminFee = Math.Floor((percent * baseAmount)/100);
				//AdminFee = decimal.ToInt32(Math.Floor(iFeePercent * baseAmount));
			}
			else
			{
				// fix value
				decimal fixVal = decimal.Parse(sFeeFixValue);
				AdminFee = Math.Floor(fixVal);
				//AdminFee = iFeeFixValue;
			}
			return true;
		}

        //public bool getAdminFeeAndCustomerFee(string productCode, string providerCode, ref int AdminFee, 
        //    ref int DistributorFee, out Exception ExError)
        //{
        //    string sql = "SELECT d.id as fee_id, d.name as fee_name, b.value as fee_value, "+
        //                    "b.fee_type FROM sys_fee a "+
        //                    "INNER JOIN sys_fee_d b ON b.sfid=a.id "+
        //                    "INNER JOIN fee_component d ON b.fcid=d.id "+
        //                    "INNER JOIN product c ON c.id=a.product_id "+
        //                    "WHERE c.code = '"+productCode.Trim()+"' AND a.is_active='t' "+
        //                    "AND a.provider_code = '" + providerCode + "' " +
        //                    "AND d.id IN (1,6) "+
        //                    "ORDER BY fee_id;";
        //    int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
        //    //Console.WriteLine(sql);
        //    //Console.WriteLine("===== KONEKSI DATABASE " + localDB.ConnectionTest(out ExError));
        //    //Console.WriteLine("===== PRODUCT CODE " + productCode.Trim());
        //    //Console.WriteLine("===== HASIL " + i.ToString());
        //    //Console.WriteLine("===== ADA ERROR?? " + (ExError != null).ToString());
        //    if (ExError != null)
        //    {
        //        LogWriter.showDEBUG(this, ExError.Message);
        //        return false;
        //    }
        //    if (i <= 1)
        //    {
        //        return false;
        //    }
        //    //Console.WriteLine("CEK DATA 1");
        //    //string huntu = localDB.GetDataItem(tbl_mpAccount, 0, "fee_value").ToString();
            
        //    string sAdminFee = localDB.GetDataItem(tbl_mpAccount, 0, "fee_value").ToString();
        //    string sDistFee = localDB.GetDataItem(tbl_mpAccount, 1, "fee_value").ToString();
        //    //Console.WriteLine("CEK DATA 1a");
        //    AdminFee = int.Parse(sAdminFee);
        //    DistributorFee = int.Parse(sDistFee);
        //    //Console.WriteLine("CEK DATA 2");
        //    return true;
        //}

        public bool insertTransactionLogxxxxx(string product_code, string distributor_phone, string customerProductNumber,
            string amount, string trace_number, string trx_time, out Exception ExError)
        {
//            INSERT INTO 
//transaction(provider_code,provider_product_code,distributor_phone,reff_number,amount,trace_number,trx_time,fee_value,provider_code,cogs_price_id) 
//VALUES ('PRD00001','001001','6287777100357','0021005555015','20000','777190',NOW(),'2000','004',29);

            string sql = "INSERT INTO transaction (product_code,distributor_phone," +
                "reff_number,amount,trace_number,trx_time) VALUES ('" +
                product_code + "','" + distributor_phone + "','" + customerProductNumber + "','" + amount + "','" +
                trace_number + "','" + trx_time + "')";

            ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

        public enum ProductTypeEnum { PRODUCT, SERVICE }
        public class ProviderProductInfo
        {
            public string ProductName= "";
            public string ProviderCode = "";
            public string DestinationHost = "";
            public string DestinationPort = "";
            public string DestinationBasedir = "";
            public string DestinationCanonicalPathAuth = "";
            public string DestinationCanonicalPathInquiry = "";
            public string DestinationCanonicalPathTrx = "";
            public string DestinationCanonicalPathReversal = "";
            public string DestinationUsername = "";
            public string DestinationPassword = "";
            public string DestinationMethod = "";
            public string DestinationContentType = "";
            public string ProviderProductCode = "";
            public int CogsPriceId = 0;
            public int CurrentPrice = 0;
            public ProductTypeEnum ProductType = ProductTypeEnum.PRODUCT;
			public string PluginPath = "";
			public string TransactionCodeSufix = "";
			public int ProductGroupId = 0;
			public string QvaAccountCredit = "";
            public bool fIncludeFee = false;
        }

        public ProviderProductInfo getProviderProductInfo(string productCode, out Exception ExError)
		{
			//-- CONTOH QUERY UNTUK PRODUCT JASA TELKOM
			//"  WHERE a.code = '001001' AND a.cogs_id IS NOT NULL;";
			ExError = null;
			ProviderProductInfo hasil = new ProviderProductInfo ();

			string sql = "SELECT b.id as cogs_price_id, " +
			             "a.name as product_name," +
			             "b.prv_code as provider_code," +
			             "b.destination_host," +
			             "b.destination_port," +
			             "b.destination_basedir," +
			             "b.destination_canonicalpath_auth," +
			             "b.destination_canonicalpath_inquiry," +
			             "b.destination_canonicalpath_trx," +
			             "b.destination_canonicalpath_reversal," +
			             "b.destination_username," +
			             "b.destination_password," +
			             "b.destination_method," +
			             "b.destination_contenttype," +
			             "b.provider_product_code," +
			             "a.product_type," +
			             "COALESCE(b.total_price,0) as current_price, " +
			             "c.plugin_path," +
			             "b.include_fee," +
			             "COALESCE(d.qva_number_credit,'-') as qva_number_credit," +
			             //"d.qva_number_credit," +
						 "a.transaction_code as trx_sufix, " +
						 "x.id as product_group_id, x.name "+ //--- IEU

			             " FROM product a " +
//					     " INNER JOIN cogs_price b ON b.product_id=a.id" +
						 " INNER JOIN cogs_price b ON b.id=a.cogs_id" +
						 " AND b.is_active = TRUE AND b.end_price_date IS NULL" +
			             " INNER JOIN company c ON b.prv_code=c.comp_code " +
			             " INNER JOIN company d ON d.comp_code=a.product_owner " +
						 " INNER JOIN product_group x ON x.id=a.product_group_id " + // --- IEU

			             " WHERE a.code = '" + productCode + "' AND a.cogs_id IS NOT NULL " +
			             " AND b.approval_status = 3 " + //--tambah ieu
			             " AND c.approval_status = 3 " + //--tambah ieu
			             " AND d.approval_status = 3;";		//--tambah ieu

			//Console.WriteLine(sql);
			ExError = null;
			int i = localDB.ExecQuerySql (sql, tbl_mpProduct, out ExError);
			if (ExError != null) {
				LogWriter.write (this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
				"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber ().ToString ());
				return hasil;
			}
			if (i <= 0)
				return hasil;
			hasil.CogsPriceId = (((int)localDB.GetDataItem (tbl_mpProduct, 0, "cogs_price_id")));
			hasil.ProductName = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "product_name")).Trim ());
			hasil.ProviderCode = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "provider_code")).Trim ());
			hasil.DestinationHost = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "destination_host")).Trim ());
			hasil.DestinationBasedir = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "destination_basedir")).Trim ());
			hasil.DestinationCanonicalPathAuth = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "destination_canonicalpath_auth")).Trim ());
			hasil.DestinationCanonicalPathInquiry = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "destination_canonicalpath_inquiry")).Trim ());
			hasil.DestinationCanonicalPathTrx = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "destination_canonicalpath_trx")).Trim ());
			hasil.DestinationCanonicalPathReversal = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "destination_canonicalpath_reversal")).Trim ());
			hasil.DestinationUsername = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "destination_username")).Trim ());
			hasil.DestinationPassword = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "destination_password")).Trim ());
			hasil.DestinationMethod = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "destination_method")).Trim ());
			hasil.DestinationContentType = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "destination_contenttype")).Trim ());
			hasil.ProviderProductCode = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "provider_product_code")).Trim ());
			hasil.CurrentPrice = int.Parse (localDB.GetDataItem (tbl_mpProduct, 0, "current_price").ToString ().Replace (".00", ""));
			hasil.ProductType = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "product_type")).Trim () == "PRODUCT") 
                                ? ProductTypeEnum.PRODUCT : ProductTypeEnum.SERVICE;
			hasil.PluginPath = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "plugin_path")).Trim ());
			hasil.fIncludeFee = (((bool)localDB.GetDataItem (tbl_mpProduct, 0, "include_fee")));
			try{
			hasil.TransactionCodeSufix = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "trx_sufix")).Trim ());
			}
			catch
			{
				hasil.TransactionCodeSufix = "";
			}
			hasil.ProductGroupId = (((int)localDB.GetDataItem (tbl_mpProduct, 0, "product_group_id")));

			if (localDB.GetDataItem (tbl_mpProduct, 0, "qva_number_credit") != null) {
				hasil.QvaAccountCredit = (((string)localDB.GetDataItem (tbl_mpProduct, 0, "qva_number_credit")).Trim ());
			} else
				hasil.QvaAccountCredit = "";

			return hasil;
		}

        public bool insertCompleteInquiryLog(string product_code, string ProviderProductCode, 
            string distributor_phone, string customerProductNumber,
            string amount, string trace_number, string trx_time, int beginBalance, int endBalance,
            string json_iso_inquiry, string json_inquiry_send_time, string json_iso_inquiry_received,
            string json_iso_trx_inquiry_time, string json_iso_trx, string json_iso_trx_send_time,
            string json_iso_trx_received, string json_iso_trx_received_time, bool is_success,
            out Exception ExError)
        {
            string sql = "INSERT INTO inquiry " +
                "(product_code,provider_product_code,distributor_phone,reff_number,trace_number, " +
                "request_time,json_iso_inquiry,json_inquiry_send_time," +
                "json_iso_inquiry_received,json_iso_trx_inquiry_time, host) " +
                "VALUES ('";

            //ProviderProductInfo ProviderProduct = getProviderProductCode(product_code, out ExError);
            //if (ExError != null) return false;

            sql += product_code + "','" + ProviderProductCode + "','" +
                   distributor_phone + "','" + customerProductNumber + "'," + trace_number + ",'" +
                   trx_time + "', '" +
                   json_iso_inquiry + "','" + json_inquiry_send_time + "', '" +
                   json_iso_inquiry_received + "', '" + json_iso_trx_inquiry_time + "','localhost');";
            ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

//SELECT nextval('transaction_id_seq') as id; -- anu ppob
//SELECT nextval('transaction_cashin_id_seq') as id; -- anu cashin
//SELECT nextval('transaction_cashout_id_seq') as id; -- anu cashout
//SELECT nextval('transaction_invoice_id_seq') as id; -- anu invoice
//SELECT nextval('transaction_regular_id_seq') as id; -- anu transfer regular

        public long getTransactionReffIdSequence(out Exception ExError)
        {
            string sql = "SELECT NEXTVAL('transaction_id_seq');";
            ExError = null;
            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return 0;
            }
            if (i <= 0) return 0;
            return ((long)localDB.GetDataItem(tbl_mpProduct, 0, "nextval"));
        }
        public long getCashInReffIdSequence(out Exception ExError)
        {
            string sql = "SELECT NEXTVAL('transaction_cashin_id_seq');";
            ExError = null;
            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return 0;
            }
            if (i <= 0) return 0;
            return ((long)localDB.GetDataItem(tbl_mpProduct, 0, "nextval"));
        }
        public long getCashOutReffIdSequence(out Exception ExError)
        {
            string sql = "SELECT NEXTVAL('transaction_cashout_id_seq');";
            ExError = null;
            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return 0;
            }
            if (i <= 0) return 0;
            return ((long)localDB.GetDataItem(tbl_mpProduct, 0, "nextval"));
        }
        public long getInvoiceReffIdSequence(out Exception ExError)
        {
            string sql = "SELECT NEXTVAL('transaction_invoice_id_seq');";
            ExError = null;
            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return 0;
            }
            if (i <= 0) return 0;
            return ((long)localDB.GetDataItem(tbl_mpProduct, 0, "nextval"));
        }
        public long getRegulerTrxReffIdSequence(out Exception ExError)
        {
            string sql = "SELECT NEXTVAL('transaction_regular_id_seq');";
            ExError = null;
            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return 0;
            }
            if (i <= 0) return 0;
            return ((long)localDB.GetDataItem(tbl_mpProduct, 0, "nextval"));
        }



//transaction_type :
//00 = PPOB
//01 = Cash In
//02 = Cash Out
//03 = Invoicing
//04 = Transfer Reqular

//INSERT INTO reversal
//(transaction_type,trx_ref_id,reversal_time,total_amount)
//VALUES
//(transaction_type,ambil_dari_table_log_transaksi,NOW()+7,total_amount)

        public enum eTransactionType { PPOB, CashIn, CashOut,Invoicing, TransferReguler, Nitrogen }
        private string eTransactionTypeToValue(eTransactionType ReversalType)
        {
            switch (ReversalType)
            {
                case eTransactionType.PPOB: return "00";
                case eTransactionType.CashIn: return "01";
                case eTransactionType.CashOut: return "02";
                case eTransactionType.Invoicing: return "03";
                case eTransactionType.TransferReguler: return "04";
                default: return "00";
            }
        }
        public bool insertReversalLog(eTransactionType transaction_type, long trx_ref_id,
            string reversal_time, int total_amount, bool isLastTransaction, bool isSuccess,
            string failedReason, out Exception ExError)
        {
            string sql = "INSERT INTO reversal " +
                "(transaction_type,trx_ref_id,reversal_time,total_amount,is_last_transaction,is_success,failed_reason)" +
                "VALUES ('";
            sql += eTransactionTypeToValue(transaction_type) + "'," + trx_ref_id + ",'" +
                   reversal_time + "'," + total_amount + "," + 
                   isLastTransaction.ToString().ToUpper() + "," +
                   isSuccess.ToString().ToUpper() + ",'" +
                   failedReason + "');";
            ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

		public bool insertQvaReversalLog(long transactionRefId, string qvaInvoiceNumber, bool isDone, 
				string errCode, string errMessage, out Exception ExError){
			string sql = "INSERT INTO transfer_reversal " +
			             "(invoice_number,ref_id,is_done,error_code,error_message)" +
			             "VALUES ('";
			sql += qvaInvoiceNumber + "'," + transactionRefId + "," + isDone.ToString()  + ",'" + 
			       errCode + "','" + errMessage + "');";
			ExError = null;
			int i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			return (i > 0);
		}

        public bool insertCompleteTransactionLog(long TransactionReffId, string product_code, string provider_product_code, 
            string distributor_phone, string customerProductNumber,
            string providerAmount, string trace_number, string trx_time,string feeAmount, string providerCode,
            int cogsPriceId, int beginBalance, int endBalance,
            string json_iso_inquiry, string json_inquiry_send_time, string json_iso_inquiry_received,
            string json_iso_trx_inquiry_time, string json_iso_trx, string json_iso_trx_send_time,
            string json_iso_trx_received, string json_iso_trx_received_time, bool is_success,
			string failedReason, string trxNumber, bool canReversal, bool include_fee, string SamCSN, string OutletCode, 
            out Exception ExError)
        {
			//strJson = "SmartCardLog: "+fiPurchaseLog; jika dari smartcard
			string sql = "INSERT INTO " +
			                      "transaction(id,product_code,provider_product_code,distributor_phone,reff_number," +
			                      "amount,trace_number,trx_time," +
			                      "fee_value,provider_code,cogs_price_id," +
			                      "begin_distributor_balance," +
			                      "end_distributor_balance," +
			                      "json_iso_inquiry," +
			                      "json_inquiry_send_time," +
			                      "json_iso_inquiry_received," +
			                      "json_iso_trx_inquiry_time," +
			                      "json_iso_trx," +
			                      "json_iso_trx_send_time," +
			                      "json_iso_trx_received," +
			                      "json_iso_trx_received_time," +
			                      "is_success, " +
			                      "failed_reason," +
			                      "trx_number,reversal_granted,include_fee";
//			if(SamCSN!="")
//				sql += ", sam_csn";
			if(OutletCode!="")
				sql += ", outlet_code";
			sql+= ") VALUES (";

            sql += TransactionReffId.ToString() + ",'" + product_code + "','" + provider_product_code + "','" + distributor_phone + "','" +
                customerProductNumber + "'," +
                providerAmount + ",'" + trace_number + "','" + trx_time + "'," +
                feeAmount + ",'" + providerCode + "'," + cogsPriceId.ToString() + "," +
                beginBalance.ToString() + "," + endBalance.ToString() + ",'" +
                json_iso_inquiry + "','" + json_inquiry_send_time + "','" +
                json_iso_inquiry_received + "','" + json_iso_trx_inquiry_time + "','" +
                json_iso_trx + "','" + json_iso_trx_send_time + "','" +
                json_iso_trx_received + "','" + json_iso_trx_received_time + "'," +
                is_success.ToString() + ",'" + failedReason + "','" + trxNumber + "'," +
				canReversal.ToString() +","+ include_fee.ToString();

//			if(SamCSN!="")
//				sql+= ",'"+ SamCSN.ToUpper () + "'";
			if(OutletCode!="")
				sql+= ",'"+ OutletCode.ToUpper () + "'";
			sql+= ");";

            LogWriter.showDEBUG(this, " ========================== SQL Insert Transaction \r\n"+ sql);
			//Console.WriteLine("insert trans Log" + sql);

            ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError);

			LogWriter.showDEBUG (this, "ERROR = " + (ExError != null));

            if (ExError != null)
            {
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "FATAL ERROR, failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
			LogWriter.showDEBUG (this, "DEBUG---- RETURN");
            return (i > 0);
        }

		public bool insertCompleteRegularTransactionLog(long TransactionReffId, string product_code, string provider_product_code, 
			string distributor_phone, string customerProductNumber,
			string providerAmount, string trace_number, string trx_time,string feeAmount, string providerCode,
			int cogsPriceId, int beginBalance, int endBalance,
			string json_iso_inquiry, string json_inquiry_send_time, string json_iso_inquiry_received,
			string json_iso_trx_inquiry_time, string json_iso_trx, string json_iso_trx_send_time,
			string json_iso_trx_received, string json_iso_trx_received_time, bool is_success,
			string failedReason, string trxNumber, bool canReversal, bool include_fee,
			out Exception ExError)
		{
			string sql = "INSERT INTO " +
			             "transaction_regular(id,product_code,provider_product_code,distributor_phone,reff_number,"+
			             "amount,trace_number,trx_time,"+
			             "fee_value,provider_code,cogs_price_id," +
			             "begin_distributor_balance,"+
			             "end_distributor_balance,"+
			             "json_iso_inquiry,"+
			             "json_inquiry_send_time,"+
			             "json_iso_inquiry_received,"+
			             "json_iso_trx_inquiry_time,"+
			             "json_iso_trx,"+
			             "json_iso_trx_send_time,"+
			             "json_iso_trx_received,"+
			             "json_iso_trx_received_time,"+
			             "is_success, " +
			             "failed_reason," +
			             "trx_number,reversal_granted,include_fee" + 
			             ") VALUES (";

			sql += TransactionReffId.ToString() + ",'" + product_code + "','" + provider_product_code + "','" + distributor_phone + "','" +
			       customerProductNumber + "'," +
			       providerAmount + ",'" + trace_number + "','" + trx_time + "'," +
			       feeAmount + ",'" + providerCode + "'," + cogsPriceId.ToString() + "," +
			       beginBalance.ToString() + "," + endBalance.ToString() + ",'" +
			       json_iso_inquiry + "','" + json_inquiry_send_time + "','" +
			       json_iso_inquiry_received + "','" + json_iso_trx_inquiry_time + "','" +
			       json_iso_trx + "','" + json_iso_trx_send_time + "','" +
			       json_iso_trx_received + "','" + json_iso_trx_received_time + "'," +
			       is_success.ToString() + ",'" + failedReason + "','" + trxNumber + "'," +
			       canReversal.ToString() +","+ include_fee.ToString() + ");";

			//LogWriter.showDEBUG(this, " ========================== SQL Insert Transaction \r\n"+ sql);
			//Console.WriteLine("insert trans Log" + sql);

			ExError = null;
			int i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			return (i > 0);
		}

        public bool getCustomerLastTransactionLog(string customerReferenceNumber, 
            ref string traceNumber, ref string trxTime, out Exception ExError)
        {
            string sql = "SELECT trace_number, trx_time FROM transaction " +
                    "WHERE reff_number = '" + customerReferenceNumber + "' ORDER BY trx_time DESC LIMIT 1";

            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            if (i <= 0) return false;

            traceNumber = ((string)(localDB.GetDataItem(tbl_mpProduct, 0, "trace_number"))).Trim();
            trxTime = ((DateTime)localDB.GetDataItem(tbl_mpProduct, 0, "trx_time")).ToString("MMddHHmmss");
            return true;
        }

        public bool isThisUserLastTransaction(string product_code, string provider_product_code,
            string distributor_phone, string customerProductNumber,
            string providerAmount, string feeAmount, string providerCode,
            int cogsPriceId, bool canReversal, ref long TrxReffId, out Exception ExError)
        {
            ExError = null;
            string sql = "SELECT * FROM transaction WHERE " +
                "distributor_phone = '" + distributor_phone + "' ORDER BY trx_time DESC LIMIT 1;";

            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            if (i <= 0) return false;

            //LogWriter.showDEBUG(this,
            //    product_code + ":" + 
            //    ((string)(localDB.GetDataItem(tbl_mpProduct, 0, "product_code"))).Trim() + "::" + 
            //    provider_product_code + "::" + 
            //    ((string)(localDB.GetDataItem(tbl_mpProduct, 0, "provider_product_code"))).Trim() + "::" +
            //    customerProductNumber.Replace("\\/", "/") + "::" + 
            //    ((string)(localDB.GetDataItem(tbl_mpProduct, 0, "reff_number"))).Trim() + "::" + 
            //    providerAmount + "::" + 
            //    ((localDB.GetDataItem(tbl_mpProduct, 0, "amount")).ToString().Trim().Replace(".00","")) + "::" + 
            //    feeAmount + "::" +
            //    ((localDB.GetDataItem(tbl_mpProduct, 0, "fee_value")).ToString().Trim().Replace(".00", "")) + "::" +
            //    providerCode + "::" +
            //    ((string)(localDB.GetDataItem(tbl_mpProduct, 0, "provider_code"))).Trim() + "::" +
            //    cogsPriceId.ToString() + "::" +
            //    ((int)(localDB.GetDataItem(tbl_mpProduct, 0, "cogs_price_id"))).ToString() + "::" +
            //    canReversal.ToString() + "::" +
            //    (localDB.GetDataItem(tbl_mpProduct, 0, "reversal_granted").ToString().ToLower()[0] == 't').ToString() + "::" 
            //    );

            if(product_code != 
                ((string)(localDB.GetDataItem(tbl_mpProduct, 0, "product_code"))).Trim()) 
                return false;
            if(provider_product_code != 
                ((string)(localDB.GetDataItem(tbl_mpProduct, 0, "provider_product_code"))).Trim()) 
                return false;
            if(customerProductNumber.Replace("\\/","/") != 
                ((string)(localDB.GetDataItem(tbl_mpProduct, 0, "reff_number"))).Trim()) 
                return false;
            if(providerAmount != 
                ((localDB.GetDataItem(tbl_mpProduct, 0, "amount")).ToString().Trim().Replace(".00","")))
                return false;
            if(feeAmount != 
                ((localDB.GetDataItem(tbl_mpProduct, 0, "fee_value")).ToString().Trim().Replace(".00","")))
                return false;
            if(providerCode != 
                ((string)(localDB.GetDataItem(tbl_mpProduct, 0, "provider_code"))).Trim()) 
                return false;
            if(cogsPriceId != 
                ((int)(localDB.GetDataItem(tbl_mpProduct, 0, "cogs_price_id")))) 
                return false;
            if(canReversal != 
                (localDB.GetDataItem(tbl_mpProduct, 0, "reversal_granted").ToString().ToLower()[0] == 't'))
                return false;
            TrxReffId = (long)(localDB.GetDataItem(tbl_mpProduct, 0, "id"));
            return true;
        }

        public bool updateUserLastTransactionLog(long TrxReffId, out Exception ExError)
        {
            ExError = null;
            //"UPDATE transfer_request_verification SET " +
            string sql = "UPDATE transaction SET " +
                "reversal_granted = FALSE " +
                "WHERE id = " + TrxReffId.ToString() + ";";

            ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

        public bool insertInvoicingLog(long TransactionReffId, string product_code, string ProviderProductCode,
            string distributor_phone, string customer_phone, string invoice_number,
            int amount, string invoiceDateTime, int adminFee, string providerCode, 
            int cogsPriceId, string invoiceDescription, string invoiceFooterNote, string trxNumber,
            out Exception ExError)
        {
            string sql = "INSERT INTO transaction_invoice " +
                "(id,product_code,provider_product_code,distributor_phone,reff_number,amount," +
                "trace_number,trx_time,fee_value,provider_code,cogs_price_id,notes,footer_notes,trx_number) " +
                "VALUES (" +
                TransactionReffId.ToString() +",'"+
                product_code + "','" +      // -- di ambil dari front end "fiProductCode"
                ProviderProductCode + "','" + //'INV001',     -- di ambil dari hasil query berdasarkan kode produk "fiProductCode"
                distributor_phone + "','" +
                customer_phone + "'," +
                amount.ToString() + ",'" +               //-- di ambil amount = (fiAmount - fee_hasil_query)
                invoice_number + "','" +    //-- di ambil "fiInvoiceNumber"
                invoiceDateTime + "'," +    //-- di ambil "fiInvoiceDateTime"
                adminFee.ToString() + ",'" +           //-- di ambil database
                providerCode + "'," +       //-- di ambil database
                cogsPriceId.ToString() + ",'" +        //-- di ambil database
                invoiceDescription + "','" +        //-- di ambil "fiDescription"
                invoiceFooterNote + "','" +         //-- di ambil "fiFooterNote"
                trxNumber +"');";        

            ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: "+ ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

        public bool insertQVATransferLog(string transfer_from_id, string transfer_to_id,
            int amount, string create_time, long TransactionRef_id, eTransactionType transaction_type,
			string description, bool is_fee, bool is_sent, string sent_time, string invoiceNumber, 
			string qva_trx_code,
            out Exception ExError)
        {
//INSERT INTO transaction_fee (transfer_from,transfer_to,amount,create_time,
//    ref_id,transaction_type,description,is_fee,is_sent,sent_time)
//VALUES 
//('transfer_ti','transfer_ka','amount_na','yyyy-mm-dd hh:mi:ss+7',
//null,'00','bere_keterangan_naon?','f','t',NOW());
            string sql = "INSERT INTO transaction_fee " +
                "(transfer_from,transfer_to,amount,create_time," +
			             "ref_id,transaction_type,description,invoice_number,is_fee,is_sent,sent_time,qva_trx_code) " +
                "VALUES ('" +
                transfer_from_id + "','" +
                transfer_to_id + "'," +
                amount + ",'" +
                create_time + "'," +
                TransactionRef_id + ",'" +
                eTransactionTypeToValue(transaction_type) + "','" +
                description + "','" +
                invoiceNumber + "'," +
                is_fee.ToString() + "," +
                is_sent.ToString() + ",'" +
			    sent_time + "','" +
	            qva_trx_code + "');";

            ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }


            //LOG_Handler.LogWriter.write(this, LOG_Handler.LogWriter.logCodeEnum.DEBUG,
            //    "Transfer berhasil, inject transfer log dgn query = " + sql);


            return (i > 0);
        }

		public bool insertQVATransferFailedLog(string transfer_from_id, string transfer_to_id,
			int amount, string create_time, long TransactionRef_id, eTransactionType transaction_type,
			string description, bool is_fee, bool is_sent, string sent_time, string invoiceNumber, 
			string qva_trx_code, bool reversal_required,
			out Exception ExError)
		{
//INSERT INTO transaction_fee (transfer_from,transfer_to,amount,create_time,
//    ref_id,transaction_type,description,is_fee,is_sent,sent_time)
//VALUES 
//('transfer_ti','transfer_ka','amount_na','yyyy-mm-dd hh:mi:ss+7',
//null,'00','bere_keterangan_naon?','f','t',NOW());
			string sql = "INSERT INTO transfer_failed " +
			             "(transfer_from,transfer_to,amount,create_time," +
			             "ref_id,transaction_type,description,invoice_number,is_fee,is_sent,sent_time,qva_trx_code, reversal_required) " +
			             "VALUES ('" +
			             transfer_from_id + "','" +
			             transfer_to_id + "'," +
			             amount + ",'" +
			             create_time + "'," +
			             TransactionRef_id + ",'" +
			             eTransactionTypeToValue(transaction_type) + "','" +
			             description + "','" +
			             invoiceNumber + "'," +
			             is_fee.ToString() + "," +
			             is_sent.ToString() + ",'" +
			             sent_time + "','" +
			             qva_trx_code +"'," +
			             reversal_required.ToString() + ");";

			ExError = null;
			int i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}

			return (i > 0);
		}

        public bool moveNotification(string mobile_phone_number_from,
            string mobile_phone_number_to, string notification_time,
            string notification_type_code, string notification_json_value,
            DateTime sent_time,
            out Exception ExError)
        {
            //KETerangan :
            //customer_notification"."mobile_phone_number_from" : Jika kosong artinya dari server
            //customer_notification"."notification_value"    : isi nya JSON

            string sql = "INSERT INTO " +
                "customer_notification_sent " +
                "(mobile_phone_number_from,mobile_phone_number_to,notification_time, " +
                "notification_type_code,notification_value, sent_time) VALUES ('";
            //('628xxxxxxxx','628xxxxxxxx','YYYY-MM-DD HH:mi:ss','lookup_ke_table_customer_notification_type','ISO_NOTIFICATION_UDAH_FORMAT_JSON');

            sql += mobile_phone_number_from + "','" + mobile_phone_number_to + "','" +
                notification_time + "','" + notification_type_code + "','" +
                notification_json_value + "','" + sent_time.ToString("yyyy-MM-dd HH:mm:ss") +"')";

			LogWriter.showDEBUG(this,"Query Insert ke Notifikasi_Sent: "+sql);
            ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if(i<=0) return false;

            sql = "DELETE FROM customer_notification WHERE " +
                "mobile_phone_number_to = '" + mobile_phone_number_to + "' " +
                " AND notification_time = '" + notification_time + "' " +
                " AND notification_type_code = '" + notification_type_code + "' ";

			LogWriter.showDEBUG(this,"Query Hapus Notifikasi: "+sql);
            ExError = null;
            i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

		public bool moveNotification(long notification_id, string mobile_phone_number_from,
			string mobile_phone_number_to, string notification_time,
			string notification_type_code, string notification_json_value,
			DateTime sent_time,
			out Exception ExError)
		{
			//KETerangan :
			//customer_notification"."mobile_phone_number_from" : Jika kosong artinya dari server
			//customer_notification"."notification_value"    : isi nya JSON

			string sql = "INSERT INTO " +
			             "customer_notification_sent " +
			             "(mobile_phone_number_from,mobile_phone_number_to,notification_time, " +
			             "notification_type_code,notification_value, sent_time) VALUES ('";
			//('628xxxxxxxx','628xxxxxxxx','YYYY-MM-DD HH:mi:ss','lookup_ke_table_customer_notification_type','ISO_NOTIFICATION_UDAH_FORMAT_JSON');

			sql += mobile_phone_number_from + "','" + mobile_phone_number_to + "','" +
			       notification_time + "','" + notification_type_code + "','" +
			       notification_json_value + "','" + sent_time.ToString("yyyy-MM-dd HH:mm:ss") +"')";

			//LogWriter.showDEBUG(this,"Query Insert ke Notifikasi_Sent: "+sql);
			ExError = null;
			int i = localDB.ExecNonQuerySql(sql, out ExError);
			if(i<=0) return false;

//			sql = "DELETE FROM customer_notification WHERE " +
//			      "mobile_phone_number_to = '" + mobile_phone_number_to + "' " +
//			      " AND notification_time = '" + notification_time + "' " +
//			      " AND notification_type_code = '" + notification_type_code + "' ";
			sql = "DELETE FROM customer_notification WHERE id = " + notification_id.ToString ();

			//LogWriter.showDEBUG(this,"Query Hapus Notifikasi: "+sql);
			ExError = null;
			i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			return (i > 0);
		}

        public bool insertNotificationQueue(string mobile_phone_number_from,
            string mobile_phone_number_to, string notification_time,
            string notification_type_code, string notification_json_value,
            out Exception ExError)
        {
            //KETerangan :
            //customer_notification"."mobile_phone_number_from" : Jika kosong artinya dari server
            //customer_notification"."notification_value"    : isi nya JSON

            string sql = "INSERT INTO " +
                "customer_notification " +
                "(mobile_phone_number_from,mobile_phone_number_to,notification_time, " +
                "notification_type_code,notification_value) VALUES ('";
            //('628xxxxxxxx','628xxxxxxxx','YYYY-MM-DD HH:mi:ss','lookup_ke_table_customer_notification_type','ISO_NOTIFICATION_UDAH_FORMAT_JSON');

            sql += mobile_phone_number_from + "','" + mobile_phone_number_to + "','" +
                notification_time + "','" + notification_type_code + "','" +
                notification_json_value + "')";

            ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

//        INSERT INTO transfer_request_verification 
//(id,requesttime,customerphone,agentphone,amount,adminfee,token,ttype,is_approved,approved_time)
//VALUES 
//(DEFAULT,NOW(),'628xxxxxx','628xxxxxx','100000','3500','123456','01 atau 02','FALSE',NOW());
//-- NOTE : is_approved,approved_time bisa di NULL keung heula

        public enum CashTrxType { CashIn = 1, CashOut = 2 }
        public bool insertCashTrxRequest(string requesttime, string customerphone, string agentphone,
            int amount, int adminfee, string token, CashTrxType ttype, bool is_approved, string approved_time)
        {
            string sql = "INSERT INTO transfer_request_verification " +
            "(id,request_time,customer_phone,agent_phone,amount,admin_fee,token,ttype," +
            "is_approved,approved_time) VALUES (DEFAULT,'";
            //(DEFAULT,NOW(),'628xxxxxx','628xxxxxx','100000','3500','123456','01 atau 02','FALSE',NOW());
            if (ttype == CashTrxType.CashIn)
            {
                sql = "INSERT INTO transfer_request_verification " +
                    "(id,request_time,customer_phone,agent_phone,amount,admin_fee,token,ttype," +
                    "is_approved,approved_time) VALUES (DEFAULT,'";
                sql += requesttime + "','" + customerphone + "','" + agentphone + "'," + amount + "," +
                    adminfee + ",'" + token + "','" + ((int)(ttype)).ToString().PadLeft(2, '0') + "'," +
                    is_approved.ToString();
                if (approved_time != "") sql += ",'" + approved_time + "')";
                else sql += ",null)";
            }
            else
            {
                sql += requesttime + "','" + customerphone + "','" + agentphone + "'," + amount + "," +
                    adminfee + ",'" + token + "','" + ((int)(ttype)).ToString().PadLeft(2, '0') + "'," + is_approved.ToString();
                if (approved_time != "") sql += ",'" + approved_time + "')";
                else sql += ",null)";
            }

            //Console.WriteLine("===************************");
            //Console.WriteLine(sql);

            Exception ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

        public bool isCashTrxRequestExists(string requesttime, string customerphone, string agentphone,
            int amount, string token, CashTrxType ttype)
        {
            string sql = "SELECT admin_fee FROM transfer_request_verification WHERE " +
                    "request_time = '"+requesttime+ "' AND customer_phone = '"+customerphone+
                    "' AND agent_phone = '" + agentphone + "' AND amount = " + amount.ToString() +
                    " AND Token = '" + token + "' " + 
                //" AND admin_fee = " + adminfee + " AND ttype = " + ((int)(ttype)).ToString().PadLeft(2, '0') +
                    " AND ttype = '" + ((int)(ttype)).ToString().PadLeft(2, '0') + "' " + 
                    " AND is_approved = FALSE;";
            //(DEFAULT,NOW(),'628xxxxxx','628xxxxxx','100000','3500','123456','01 atau 02','FALSE',NOW());
            if (ttype == CashTrxType.CashIn)
            {
                sql = "SELECT admin_fee FROM transfer_request_verification WHERE " +
                    "request_time = '" + requesttime + "' AND customer_phone = '" + customerphone +
                    "' AND agent_phone = '" + agentphone + "' AND amount = " + amount.ToString() +
                    //" AND admin_fee = " + adminfee + " AND ttype = " + ((int)(ttype)).ToString().PadLeft(2, '0') +
                    " AND ttype = '" + ((int)(ttype)).ToString().PadLeft(2, '0') + "' " + 
                    " AND is_approved = FALSE;";
            }

            //Console.WriteLine("+++++++++++++++   IS REQUESTEXISTS ++++++++++");
            //Console.WriteLine(sql);

            Exception ExError = null;
            ExError = null;
            int i = localDB.ExecQuerySql(sql, tbl_mpCash, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

        public bool approveCashTrxRequest(string requesttime, string customerphone, string agentphone,
            int amount, int adminfee, string token, CashTrxType ttype, bool is_approved, string approved_time)
        {
            string sql = "UPDATE transfer_request_verification SET " +
                "is_approved = " + is_approved.ToString() + ", approved_time = '" + approved_time + "' " +
            " WHERE request_time = '" + requesttime + "' AND customer_phone = '" + customerphone + "' AND " +
            " agent_phone = '" + agentphone + "' AND amount = " + amount.ToString() + " AND " +
            " admin_fee = " + adminfee.ToString() + " AND token = '" + token + "' AND "+
            " ttype = '" + ((int)(ttype)).ToString().PadLeft(2, '0') + "';";

            //Console.WriteLine("******* APPROVE *************");
            //Console.WriteLine(sql);
            Exception ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

        public class NotificationValues
        {
			public long id=0;
            public string mobile_phone_number_from = "";
            public string mobile_phone_number_to = "";
            public string notification_time="";
            public string notification_type_code = "";
            public string notification_value = "";

        }
        public NotificationValues getNotificationSchedule(string toPhoneNumber, out Exception ExError)
        {
			string sql = "SELECT id,mobile_phone_number_from,"+
            "notification_time,notification_type_code,"+
            "notification_value FROM customer_notification " +
            "WHERE mobile_phone_number_to='"+ toPhoneNumber.Trim() +"' " +
            "ORDER BY notification_time ASC LIMIT 1";

            NotificationValues hasil = new NotificationValues();
            ExError = null;
            int i = localDB.ExecQuerySql(sql, tbl_mpNotif, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return hasil;
            }
            if (i <= 0) return hasil;
			hasil.id = (((long)localDB.GetDataItem(tbl_mpNotif, 0, "id")));
			hasil.mobile_phone_number_from = (((string)localDB.GetDataItem(tbl_mpNotif, 0, "mobile_phone_number_from")).Trim());
            hasil.mobile_phone_number_to = toPhoneNumber.Trim();
            hasil.notification_time = (((DateTime)localDB.GetDataItem(tbl_mpNotif, 0, "notification_time")).ToString("MM-dd-yyyy HH:mm:ss"));
            hasil.notification_type_code = (((string)localDB.GetDataItem(tbl_mpNotif, 0, "notification_type_code")).Trim());
            hasil.notification_value = (((string)localDB.GetDataItem(tbl_mpNotif, 0, "notification_value")).Trim());
            return hasil;
        }

        public bool insertCashInLog(long TransactionReffId, string provider_code,string provider_product_code,
                string cogs_price_id, string distributor_phone, string customerPhone, int amount,
                int adminFee, string traceNum, string json_iso_trx,bool is_success, string json_iso_trx_send_time,
                string json_iso_trx_received, string json_iso_trx_received_time, string host,
                string product_code, string trxNumber,
                out Exception ExError)
        {

//            lamun di sisi product owner "QQ" => product_code : PRD00035     product name: Transfer Cash In
//lamun di sisi product owner "QQ" => product_code : PRD00036     product name: Transfer Cash Out
//[12/18/2013 11:46:45 AM] Deon Tom: [Wednesday, December 18, 2013 11:41 AM] Deon Tom: 

//<<< lamun di sisi product provider "DAKSA" => product_code : 111     product name: Transfer Cash In
//lamun di sisi product provider "DAKSA" => product_code : 112     product name: Transfer Cash Out


            // jang transfer cashin dan cashout
            //    INSERT INTO transfer_temp (id,customer_phone,loader_phone,transfer_date,token,is_success,is_del,host)
            //VALUES (DEFAULT,'62xxxxxx','62xxxxxx','YYYY-MM-DD HH:mi:ss +7','123456','f','f','localhost');

            string sql = "INSERT INTO transaction_cashin (id,provider_code, provider_product_code," +
                "cogs_price_id,distributor_phone,reff_number,amount,fee_value,trace_number, " +
                "trx_time,json_iso_trx, host, product_code, trx_number) VALUES (" +
                TransactionReffId.ToString() + ",'" + 
                provider_code + "','" + provider_product_code + "','" + cogs_price_id + "','" +
                distributor_phone + "','" + customerPhone + "'," + amount.ToString() + "," +
                adminFee + ",'" + traceNum + "','" + json_iso_trx_send_time + "','" +
                json_iso_trx + "','localhost','" + product_code + "','" + trxNumber + "');";        

            //Console.WriteLine("================== Log Cash In =================");
            //Console.WriteLine(sql);

            ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

        public bool insertCashOutLog(long TransactionReffId, string provider_code, string provider_product_code,
                string cogs_price_id, string distributor_phone, string customerPhone, int amount,
                int adminFee, string traceNum, string json_iso_trx, bool is_success, string json_iso_trx_send_time,
                string json_iso_trx_received, string json_iso_trx_received_time, string host,
                string product_code, string trxNumber,
                out Exception ExError)
        {

            string sql = "INSERT INTO transaction_cashout (id,provider_code, provider_product_code," +
                "cogs_price_id,distributor_phone,reff_number,amount,fee_value,trace_number, " +
                "trx_time,json_iso_trx, host, product_code, trx_number) VALUES (" +
                TransactionReffId.ToString() +",'"+
                provider_code + "','" + provider_product_code + "'," + cogs_price_id + ",'" +
                distributor_phone + "','" + customerPhone + "'," + amount.ToString() + "," +
                adminFee + ",'" + traceNum + "','" + json_iso_trx_send_time + "','" +
                json_iso_trx + "','localhost','" + product_code + "','" + trxNumber + "');";

            //Console.WriteLine("================== Log Cash Out =================");
            //Console.WriteLine(sql);

            ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

        public bool insertInvoiceSent(string app_id, string customerphone, string merchantphone,
            string providerProductCode, int amount, string invoiceNumber, string invoiceDateTime, 
            string invoiceDescription, string invoiceFooterNote, bool is_paid, 
            string paid_time)
        {
            string sql = "INSERT INTO invoice_sent " +
            "(request_time,app_id,customer_phone,merchant_phone,product_code,amount,invoice_number," +
            "invoice_datetime,description,footer_note,is_paid,paid_datetime) VALUES (DEFAULT,'";

            sql += app_id + "','" + customerphone + "','" + merchantphone + "','" + providerProductCode + "'," + 
                amount + ",'" + invoiceNumber + "','" + invoiceDateTime + "','" + invoiceDescription + "','" +
                invoiceFooterNote + "'," + is_paid.ToString();
            if (paid_time != "") sql += ",'" + paid_time + "')";
                else sql += ",null)";

            Exception ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

		public bool insertSmsToSend(string applicant, DateTime request_time, string destination_number,
			string message)
		{
			string sql = "INSERT INTO sms_send_request " +
			             "(applicant,request_time,destination_number,message) VALUES ('";

			sql += applicant + "','" + request_time.ToString("yyyy-MM-dd HH:mm:ss") + "','" + 
			       destination_number + "','" + message + "')";

			Exception ExError = null;
			int i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			return (i > 0);
		}

        public bool isInvoiceExist(string customerphone, string merchantphone,
            string providerProductCode, string invoiceNumber, string invoiceDateTime, 
            ref int dbAmount, ref string invDescription, ref string footerNote)
        {
            string sql = "SELECT amount, description, footer_note FROM invoice_sent WHERE " +
                    " customer_phone = '" + customerphone + "' " +
                    " AND merchant_phone = '" + merchantphone + "' " +
                    " AND product_code = '" + providerProductCode + "' " +
                    " AND invoice_number = '" + invoiceNumber + "' " +
                    " AND invoice_datetime = '" + invoiceDateTime + "' " +
                    " AND is_paid = FALSE;";

            //Console.WriteLine("+++++++++++++++   IS EXISTS ++++++++++");
            //Console.WriteLine(sql);

            Exception ExError = null;
            ExError = null;
            int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            if (i <= 0)
            {
                return false;
            }
            dbAmount = int.Parse(localDB.GetDataItem(tbl_mpAccount, 0, "amount").ToString());
            invDescription = ((string)localDB.GetDataItem(tbl_mpAccount, 0, "description")).Trim();
            footerNote = ((string)localDB.GetDataItem(tbl_mpAccount, 0, "footer_note")).Trim();
            return true;
        }

        public bool payInvoiceUpdate(string customerphone, string merchantphone,
            string providerProductCode, string invoiceNumber, string invoiceDateTime, int amount,
            bool isPaid, string paidTime)
        {
            string sql = "UPDATE invoice_sent SET " +
                "is_paid = " + isPaid.ToString() + ", paid_datetime = '" + paidTime + "' WHERE " +
                    " customer_phone = '" + customerphone +
                    "' AND merchant_phone = '" + merchantphone + "' " +
                    " AND amount = " + amount + " " +
                    " AND product_code = '" + providerProductCode + "' " +
                    " AND invoice_number = '" + invoiceNumber + "' " +
                    " AND invoice_datetime = '" + invoiceDateTime + "';";

            //Console.WriteLine("******* APPROVED *************");
            //Console.WriteLine(sql);
            Exception ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

//00 = PPOB
//01 = Cash In
//02 = Cash Out
//03 = Invoicing
        public string getProductTrxNumber(out Exception ExError)
        {
            string sql = "SELECT NEXTVAL('get_product_trx_number_seq');";
            ExError = null;
            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return "";
            }
            if (i <= 0) return "";
            return (DateTime.Now.ToString("yy") + "00" +
                localDB.GetDataItem(tbl_mpProduct, 0, "nextval").ToString().Trim().PadLeft(10, '0'));
        }

        public string getNextProductTraceNumberString(out Exception ExError)
        {
            string sql = "SELECT NEXTVAL('get_trace_number_seq');";
            ExError = null;
            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return "";
            }
            if (i <= 0) return "";
            return (
                localDB.GetDataItem(tbl_mpProduct, 0, "nextval").ToString().Trim().PadLeft(6, '0'));
        }

        public int getNextProductTraceNumber()
        {
            string sql = "SELECT NEXTVAL('get_trace_number_seq');";
            Exception ExError = null;
            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return -1;
            }
            if (i <= 0) return -1;
            return (int.Parse(localDB.GetDataItem(tbl_mpProduct, 0, "nextval").ToString()));
        }

        public long getNextProductReferenceNumber()
        {
            string sql = "SELECT NEXTVAL('get_reference_number_seq');";
            Exception ExError = null;
            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return -1;
            }
            if (i <= 0) return -1;
            return (long.Parse(localDB.GetDataItem(tbl_mpProduct, 0, "nextval").ToString()));
        }

        public string getInvoiceTrxNumber(out Exception ExError)
        {
            string sql = "SELECT NEXTVAL('get_invoice_trx_number_seq');";
            ExError = null;
            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return "";
            }
            if (i <= 0) return "";
            return (DateTime.Now.ToString("yy") + "03" + 
                localDB.GetDataItem(tbl_mpProduct, 0, "nextval").ToString().Trim().PadLeft(10,'0'));
        }

        public string getCashInTrxNumber(out Exception ExError)
        {
            string sql = "SELECT NEXTVAL('get_cashin_trx_number_seq');";
            ExError = null;
            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return "";
            }
            if (i <= 0) return "";
            return (DateTime.Now.ToString("yy") + "01" + 
                localDB.GetDataItem(tbl_mpProduct, 0, "nextval").ToString().Trim().PadLeft(10,'0'));
        }

        public string getCashOutTrxNumber(out Exception ExError)
        {
            string sql = "SELECT NEXTVAL('get_cashout_trx_number_seq');";
            ExError = null;
            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return "";
            }
            if (i <= 0) return "";
            return (DateTime.Now.ToString("yy") + "02" + 
                localDB.GetDataItem(tbl_mpProduct, 0, "nextval").ToString().Trim().PadLeft(10,'0'));
        }

//        Insert ka transaction FEE
//        transaction_type :
//00 = PPOB
//01 = Cash In
//02 = Cash Out
//03 = Invoicing

//INSERT INTO transaction_fee (transfer_from,transfer_to,amount,create_time,ref_id,transaction_type,description,is_fee,is_sent,sent_time)
//VALUES 
//('transfer_ti','transfer_ka','amount_na','yyyy-mm-dd hh:mi:ss+7',null,'00','bere_keterangan_naon?','f','t',NOW());


        public Hashtable getAppConfigurations()
        {
            Exception ExError;
            string sql = "SELECT * FROM configuration";
            ExError = null;
            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null) return null;
            if (i <= 0) return null;
            Hashtable hasil = new Hashtable();
            for (int j = 0; j < i; j++)
            {
                hasil.Add(localDB.GetDataSet.Tables[tbl_mpProduct].Rows[j]["name"],
                    localDB.GetDataSet.Tables[tbl_mpProduct].Rows[j]["value"]);
            }
            return hasil;
        }

        public bool TokoOnline_SaveOrder(string provideProductCode, string ownerPhone,
            string distributor_phone, int amount, DateTime order_time, string descr, string host, 
            string trx_number)
        {
            //INSERT INTO order_request (product_code,owner_phone,distributor_phone,amount,order_time,description,host)
            //VALUES ('PRD00055','081218877246','082218877123','150000',NOW(),'description','127.0.0.1')
            string sql = "INSERT INTO order_request "
                + "(product_code,owner_phone,distributor_phone,amount,order_time,description,host,trx_number) "
                + "VALUES ('";

            sql += provideProductCode + "','" + ownerPhone + "','" + distributor_phone + "'," + 
                amount + ",'" + order_time.ToString("yyyy-MM-dd HH:mm:ss") + "','" +
                descr + "','" + host + "','" + trx_number + "')";

            LogWriter.show(this, sql);
            Exception ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
            return (i > 0);
        }

		public bool Shops_SaveOrder_Item(long orderId, string itemProductCode, int qty, decimal harga){
			string sql = "INSERT INTO order_request_d (orid,outlet_product_code, qty,price) " 
				+ "VALUES ( "+orderId.ToString ()+", '"+itemProductCode
				+"',"+ qty.ToString () +", (SELECT price FROM customer_product WHERE code = '"
				+itemProductCode+"') )";

			LogWriter.show(this, sql);
			Exception ExError = null;
			int i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			return (i > 0);
		}

		private long getShopsNextOrderId(out Exception ExError)
		{
			string sql = "SELECT NEXTVAL('order_request_id_seq');";
			ExError = null;
			int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return 0;
			}
			if (i <= 0) return 0;
			return ((long)localDB.GetDataItem(tbl_mpProduct, 0, "nextval"));
		}

		public bool Shops_SaveOrder_Group(string groupProductCode, string ownerPhone, string buyerPhone, decimal harga, 
			DateTime orderTime, string deskripsi, string remark, string hostClient, string trxNumber, out long orderId){
//			id bigserial NOT NULL,
//			product_code character varying(30) NOT NULL,
//			owner_phone character varying(30) NOT NULL, -- nomor hape yang punya barang toko
//			distributor_phone character varying(30), -- nomor hape yang menjual barang
//			amount numeric(14,2) NOT NULL DEFAULT 0,
//			order_time timestamp(0) without time zone DEFAULT now(),
//			description text NOT NULL, -- isi detail produk
//			status smallint DEFAULT 0, -- 0 : order...
//			response_time timestamp(0) without time zone DEFAULT now(),
//			remark text,
//			created_time timestamp(0) without time zone NOT NULL DEFAULT now(),
//			host character varying(100),
//			trx_number character varying(14) NOT NULL,

			Exception ExError = null;
			orderId = getShopsNextOrderId(out ExError);
			string sql = "INSERT INTO order_request (id,product_code, owner_phone,buyer_phone,"
				+ "amount, order_time, description, remark, "
				+ "host, trx_number) VALUES ( ";

			sql+= orderId.ToString ()+", '" + groupProductCode + "','" + ownerPhone +"', '"
				+buyerPhone+"', " + harga.ToString () + ", '" + orderTime.ToString ("yyyy-MM-dd HH:mm:ss") + "', '"
				+ deskripsi + "', '" + remark + "', '" + hostClient + "', '" + trxNumber + "');";

			LogWriter.show(this, sql);
			int i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			return (i > 0);
		}

        //public bool isAnyOfflineTransactions(out Exception ExError)
        //{
        //    string sql = "SELECT COUNT(*) FROM offline_transactions";
        //    ExError = null;
        //    int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
        //    if (ExError != null)
        //    {
        //        LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
        //            "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
        //        return false;
        //    }
        //    if (i <= 0) return false;
        //    return true;
        //}

        public bool isOfflineTransactionExist(string userPhone, string appID, string productCode,
			DateTime trxDateTime, int quantity, ref string dbTraceNum, bool useLog, out Exception ExError)
        {
            string sql = "SELECT * FROM offline_transactions WHERE " +
                "application_id = '" + appID + "' AND agent_phone = '" + userPhone +
                "' AND product_code = '" + productCode + "' AND quantity = " + quantity +
                " AND trx_time = '" + trxDateTime.ToString() + "'";
            ExError = null;
			dbTraceNum = "";
            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null)
            {
                if (useLog)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                        "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                }
                return false;
            }
            if (i <= 0) return false;
			dbTraceNum = localDB.GetDataItem (tbl_mpProduct, 0, "trace_number").ToString ().Trim ();
            return true;
        }

        public bool getOneOfflineTransaction(bool scanDelayed, ref string userPhone, ref string appID,
            ref string productCode, ref DateTime trxDateTime, ref int quantity,
			ref int price, ref string dbTraceNum, out Exception ExError)
        {
            string sql = "";
            if (!scanDelayed)
            {
                sql = "SELECT * FROM offline_transactions WHERE "
                    + "is_trx_done = false ORDER BY trx_time LIMIT 1";
            }
            else
            {
                sql = "SELECT * FROM offline_transactions WHERE "
                    + "is_trx_done = true AND is_trx_success = false ORDER BY trx_time LIMIT 1";
            }

            return getOneOfflineTransactionCommon(sql, ref userPhone, ref appID, ref productCode,
				ref trxDateTime, ref quantity, ref price, ref dbTraceNum, !scanDelayed, out ExError);
        }

        private bool getOneOfflineTransactionCommon(string sql, ref string userPhone, 
            ref string appID, ref string productCode, ref DateTime trxDateTime, ref int quantity,
			ref int price, ref string dbTraceNum, bool useLog, out Exception ExError)
        {
            ExError = null;
            userPhone = "";
            int i = localDB.ExecQuerySql(sql, tbl_mpProduct, out ExError);
            if (ExError != null)
            {
                if (useLog)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                        "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                }
                return false;
            }
            if (i <= 0) return false;
            userPhone = ((string)localDB.GetDataItem(tbl_mpProduct, 0, "agent_phone")).Trim();
            appID = ((string)localDB.GetDataItem(tbl_mpProduct, 0, "application_id")).Trim();
            productCode = ((string)localDB.GetDataItem(tbl_mpProduct, 0, "product_code")).Trim();
            trxDateTime = (DateTime)localDB.GetDataItem(tbl_mpProduct, 0, "trx_time");
            quantity = (int)localDB.GetDataItem(tbl_mpProduct, 0, "quantity");
            //price = int.Parse(localDB.GetDataItem(tbl_mpProduct, 0, "price").ToString());
            price = (int)localDB.GetDataItem(tbl_mpProduct, 0, "price");
			dbTraceNum = localDB.GetDataItem (tbl_mpProduct, 0, "trace_number").ToString ().Trim ();
            return true;
        }

        public bool updateOfflineTransactionLog(string ref_userPhone, string ref_appID,
            string ref_productCode, DateTime ref_trxDateTime, int ref_quantity,
            bool fTrxDone, bool fTrxSuccess, string fTrxErrorCode, string errorMessage,
            bool useLog, out Exception ExError)
        {
            string sql = "update offline_transactions SET "
                + "is_trx_done = " + fTrxDone.ToString() + ", "
                + "is_trx_success = " + fTrxSuccess.ToString() + ", "
                + "trx_error_code = '" + fTrxErrorCode + "', "
                + "trx_error_message = '" + errorMessage + "' "
                + "WHERE " +
                "application_id = '" + ref_appID + "' AND agent_phone = '" + ref_userPhone +
                "' AND product_code = '" + ref_productCode + "' AND quantity = " + ref_quantity.ToString() +
                " AND trx_time = '" + ref_trxDateTime.ToString("yyyy-MM-dd HH:mm:ss") + "'";
            ExError = null;
            int i = localDB.ExecNonQuerySql(sql, out ExError); 
            if (ExError != null)
            {
                if (useLog)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                        "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                    return false;
                }
            }
            if (i <= 0) return false;
            return true;
        }
        public bool saveOfflineTransaction(string userPhone, string appID, string productCode, 
			DateTime trxDateTime, int quantity, int productPrice, string host, int trace_number,
            bool useLog, out Exception ExError)
        {
            ExError = null;
            string sql = "INSERT INTO offline_transactions ( " +
				"application_id,agent_phone,product_code,quantity,trx_time, price, host, trace_number)" +
                          "VALUES (";
            sql += "'" +appID + "','" + userPhone + "','" + productCode + "'," + quantity.ToString() + ",'" +
				trxDateTime.ToString("yyyy-MM-dd HH:mm:ss") + "',"+productPrice.ToString()+",'" + host +"',"+
				trace_number.ToString().PadLeft(6, '0')+")";
            int i = localDB.ExecNonQuerySql(sql, out ExError);
            if (ExError != null)
            {
                if (useLog)
                {
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
                        "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                }
                return false;
            }

            return (i > 0);
        }

		public bool cekAdminLoginWeb(string webPassword){
			return cekLoginWeb("administrator", webPassword);
		}

		public bool cekLoginWeb(string webUser, string webPassword){
			// webUser = administrator
			string sql = "SELECT a.user_id,a.sgid,b.name as real_name,a.pid FROM sys_users a " +
			             "INNER JOIN person b ON a.pid=b.id " +
			             "WHERE a.user_id='"+webUser+"' " +
			             "AND a.user_passwd='"+webPassword+"';";

			Exception ExError = null;
			int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
			if (ExError != null)
			{
				// log kan jika administrator login
				if (webUser == "administrator")
				{
					LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
						"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				}
				return false;
			}
			if (i <= 0) return false;
			return true;
		}

		public struct IdNameStruct
		{
			public string id;
			public string name;
		}

		public List<IdNameStruct> getProvince(out Exception ExError)
		{
			List<IdNameStruct> propList = new List<IdNameStruct> ();

			ExError = null;
			string sql = "SELECT id_prov as id, nama_prov as nama FROM all_provinsi;";

			//Console.WriteLine("DEBUG TESxx " + (tbl_mpAccount == null).ToString());
			int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return null;
			}
			if (i <= 0)
			{
				return null;
			}

			for (int j=0;j<i;j++){
				IdNameStruct prop = new IdNameStruct ();
				prop.id = (string)(localDB.GetDataItem (tbl_mpAccount, j, "id"));
				prop.name = (string)(localDB.GetDataItem (tbl_mpAccount, j, "nama"));
				propList.Add (prop);
			}

			return propList;
		}

		public List<IdNameStruct> getKabupatenKota(string idProvinsi, out Exception ExError)
		{
			List<IdNameStruct> KabKotList = new List<IdNameStruct> ();

			ExError = null;
			string sql = "SELECT id_kabkot as id, nama_kabkot as nama FROM all_kabkot WHERE id_prov = '"+idProvinsi+"';";

			//Console.WriteLine("DEBUG TESxx " + (tbl_mpAccount == null).ToString());
			int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return null;
			}
			if (i <= 0)
			{
				return null;
			}

			for (int j=0;j<i;j++){
				IdNameStruct kabkot = new IdNameStruct ();
				kabkot.id = (string)(localDB.GetDataItem (tbl_mpAccount, j, "id"));
				kabkot.name = (string)(localDB.GetDataItem (tbl_mpAccount, j, "nama"));
				KabKotList.Add (kabkot);
			}

			return KabKotList;
		}

		public List<IdNameStruct> getKecamatan(string idKabupatenKota, out Exception ExError)
		{
			List<IdNameStruct> KecamatanList = new List<IdNameStruct> ();

			ExError = null;
			string sql = "SELECT id_kec as id, nama_kec as nama FROM all_kecamatan WHERE id_kabkot = '"+idKabupatenKota+"';";

			//Console.WriteLine("DEBUG TESxx " + (tbl_mpAccount == null).ToString());
			int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return null;
			}
			if (i <= 0)
			{
				return null;
			}

			for (int j=0;j<i;j++){
				IdNameStruct kecamatan = new IdNameStruct ();
				kecamatan.id = (string)(localDB.GetDataItem (tbl_mpAccount, j, "id"));
				kecamatan.name = (string)(localDB.GetDataItem (tbl_mpAccount, j, "nama"));
				KecamatanList.Add (kecamatan);
			}

			return KecamatanList;
		}

		public List<IdNameStruct> getKelurahan(string idKecamatan, out Exception ExError)
		{
			List<IdNameStruct> KelurahanList = new List<IdNameStruct> ();

			ExError = null;
			string sql = "SELECT id_kel as id, nama_kel as nama FROM all_kelurahan WHERE id_kec = '"+idKecamatan+"';";

			//Console.WriteLine("DEBUG TESxx " + (tbl_mpAccount == null).ToString());
			int i = localDB.ExecQuerySql(sql, tbl_mpAccount, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return null;
			}
			if (i <= 0)
			{
				return null;
			}

			for (int j=0;j<i;j++){
				IdNameStruct kelurahan = new IdNameStruct ();
				kelurahan.id = (string)(localDB.GetDataItem (tbl_mpAccount, j, "id"));
				kelurahan.name = (string)(localDB.GetDataItem (tbl_mpAccount, j, "nama"));
				KelurahanList.Add (kelurahan);
			}

			return KelurahanList;
		}

		public bool insertVotes(string user_phone, string idPropinsi, string idKabupatenKota, 
			string idKecamatan, string idKelurahan, string idTps, int count1, int count2, out Exception ExError)
		{
			string sql = "INSERT INTO ivote_count ( user_phone, prov_id, kabkot_id, kec_id, kel_id, " + 
						"tps, count1, count2, counting_time, created_time, host ) " +
			             "values (";
			sql += "'" + user_phone + "','" + idPropinsi + "','" + idKabupatenKota + "','" 
			       	+ idKecamatan + "','" + idKelurahan + "','" + idTps + "'," 
			       	+ count1.ToString() + "," + count2.ToString() + ",'"
					+ DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss") + "','"
			       	+ DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss") + "'," 
			        + "'powerhouse');";

			int i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}

			return (i > 0);
		}

		public bool insertSurveyor(string user_phone, string idPropinsi, string idKabupatenKota, 
			string idKecamatan, string idKelurahan, out Exception ExError)
		{
			string sql = "INSERT INTO surveyor ( user_phone, prov_id, kabkot_id, kec_id, kel_id, " + 
			             "register_date, host ) " +
			             "values (";
			sql += "'" + user_phone + "','" + idPropinsi + "','" + idKabupatenKota + "','" 
			       + idKecamatan + "','" + idKelurahan + "','" 
			       + DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss") + "'," 
			       + "'powerhouse');";

			int i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}

			return (i > 0);
		}

		private bool querympPr(string Sql, ref int cntQueried, out Exception ExError){
			cntQueried = localDB.ExecQuerySql(Sql, tbl_mpProduct, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + Sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			return true;
		}

		private bool querympAc(string Sql, ref int cntQueried, out Exception ExError){
			//Console.WriteLine("DEBUG TESxx " + (tbl_mpAccount == null).ToString());
			cntQueried = localDB.ExecQuerySql(Sql, tbl_mpAccount, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + Sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			return true;
		}

		// SELECT * FROM ivote_last_count WHERE tps = 'id_tps';
		// SELECT SUM(count1) as wowo, SUM(count2) as joko FROM ivote_last_count;
		public bool getVoteResult(string idTps, 
			ref int NasionalCount1, ref int NasionalCount2, 
			ref int PropinsiCount1, ref int PropinsiCount2, 
			ref int KabKotaCount1, ref int KabKotaCount2, 
			ref int KecamatanCount1, ref int KecamatanCount2, 
			ref int KelurahanCount1, ref int KelurahanCount2, 
			ref int TpsCount1, ref int TpsCount2, 
			out Exception ExError)
		{
			ExError = null;
			if(idTps.Length!= 13) return false;

			string sql = "SELECT 'tps' as tipe, a.count1, a.count2 FROM ivote_last_count a WHERE a.tps = " + idTps + " ";
			sql += "UNION ALL ";
			sql += "SELECT 'kelurahan' as tipe, b.count1, b.count2 FROM ivote_kelurahan b WHERE b.kel_id = " + idTps.Substring (0, 10) + " ";
			sql += "UNION ALL ";
			sql += "SELECT 'kecamatan' as tipe, c.count1, c.count2 FROM ivote_kecamatan c WHERE c.kec_id = " + idTps.Substring (0, 7) + " ";
			sql += "UNION ALL ";
			sql += "SELECT 'kabkot' as tipe, d.count1, d.count2 FROM ivote_kabkota d WHERE d.kabkot_id = " + idTps.Substring (0, 4) + " ";
			sql += "UNION ALL ";
			sql += "SELECT 'provinsi' as tipe, e.count1, e.count2 FROM ivote_propinsi e WHERE e.prov_id = " + idTps.Substring (0, 2) + " ";
			sql += "UNION ALL ";
			sql += "SELECT 'nasional' as tipe, f.count1, f.count2 FROM ivote_nasional f";

			NasionalCount1 = 0;
			NasionalCount2 = 0;
			PropinsiCount1 = 0;
			PropinsiCount2 = 0;
			KabKotaCount1 = 0;
			KabKotaCount2 = 0;
			KecamatanCount1 = 0;
			KecamatanCount2 = 0;
			KelurahanCount1 = 0;
			KelurahanCount2 = 0;
			TpsCount1 = 0;
			TpsCount2 = 0;

			string tipe = "";
			int i = 0;
			if (!querympAc (sql,ref i, out ExError))
				return false;
			if (i > 0) {
//			tpsCount1 = ((int)localDB.GetDataItem (tbl_mpAccount, 0, "count1"));
//			tpsCount2 = ((int)localDB.GetDataItem (tbl_mpAccount, 0, "count2"));
				for (int j = 0; j < i; j++) {
					tipe = localDB.GetDataItem (tbl_mpAccount, j, "tipe").ToString ().Trim ();
					if (tipe == "tps") {
						TpsCount1 = int.Parse (localDB.GetDataItem (tbl_mpAccount, j, "count1").ToString ());
						TpsCount2 = int.Parse (localDB.GetDataItem (tbl_mpAccount, j, "count2").ToString ());
					} else if (tipe == "kelurahan") {
						KelurahanCount1 = int.Parse (localDB.GetDataItem (tbl_mpAccount, j, "count1").ToString ());
						KelurahanCount2 = int.Parse (localDB.GetDataItem (tbl_mpAccount, j, "count2").ToString ());
					} else if (tipe == "kecamatan") {
						KecamatanCount1 = int.Parse (localDB.GetDataItem (tbl_mpAccount, j, "count1").ToString ());
						KecamatanCount2 = int.Parse (localDB.GetDataItem (tbl_mpAccount, j, "count2").ToString ());
					} else if (tipe == "kabkot") {
						KabKotaCount1 = int.Parse (localDB.GetDataItem (tbl_mpAccount, j, "count1").ToString ());
						KabKotaCount2 = int.Parse (localDB.GetDataItem (tbl_mpAccount, j, "count2").ToString ());
					} else if (tipe == "provinsi") {
						PropinsiCount1 = int.Parse (localDB.GetDataItem (tbl_mpAccount, j, "count1").ToString ());
						PropinsiCount2 = int.Parse (localDB.GetDataItem (tbl_mpAccount, j, "count2").ToString ());
					} else if (tipe == "nasional") {
						NasionalCount1 = int.Parse (localDB.GetDataItem (tbl_mpAccount, j, "count1").ToString ());
						NasionalCount2 = int.Parse (localDB.GetDataItem (tbl_mpAccount, j, "count2").ToString ());
					}
				}
			}
			return true;
		}

		// Struktur TABLE ucard_information
//		card_number character varying NOT NULL,
//		csn character varying NOT NULL,
//		last_balance numeric NOT NULL,
//		register_date date NOT NULL,
//		created_by character varying NOT NULL,
//		created_time timestamp without time zone NOT NULL,
//		host character varying NOT NULL,
//		modified_by character varying,
//		modified_time timestamp without time zone,
//		last_host character varying,
//		status smallint NOT NULL DEFAULT 0, -- 0 : Candidate Card...

		/// <summary>
		/// Ises the card activated.
		/// </summary>
		/// <returns><c>true</c>, if card activated was ised, <c>false</c> otherwise.</returns>
		/// <param name="CardNumber">Card number.</param>
		/// <param name="failedReason">Failed reason.</param>
		/// <param name="ExError">Ex error.</param>
		public bool isCardActivated(string CardNumber, 
			ref decimal cardBalance, ref DateTime lastModified,
			ref string failedReason){
			Exception ExError = null;

			// BERESIN DISINI
//			status:
//			0 : Candidate Card
//			1 : Disactivate
//			2 : Active
//			3 : Blocked

			cardBalance = 0;
			string sql = "SELECT last_balance, created_time, modified_time, status FROM ucard_information WHERE card_number = '"
				+ CardNumber.Trim() + "'";
			int i=0;
			if (!querympPr (sql,ref i, out ExError))
				return false;
			if (ExError != null) {
				failedReason = "Failed to access user card in database";
				return false;
			}
			if (i <= 0) {
				failedReason = "User card is not registered";
				return false;
			}
			short status = short.Parse(localDB.GetDataItem (tbl_mpProduct, 0, "status").ToString());
			decimal lastBalance = ((decimal)localDB.GetDataItem (tbl_mpProduct, 0, "last_balance"));
			if (localDB.GetDataItem (tbl_mpProduct, 0, "modified_time") == null) {
				lastModified = (DateTime)localDB.GetDataItem(tbl_mpProduct, 0, "created_time");
			}else{
				lastModified = (DateTime)localDB.GetDataItem(tbl_mpProduct, 0, "modified_time");
			}

			switch(status){
			case 0:
				failedReason = "User card hasn't been distributed";
				return false;
			case 1:
				failedReason = "User card is not active";
				return false;
			case 3:
				failedReason = "User card is blocked";
				return false;
			case 2:
				cardBalance = lastBalance;
				failedReason = "";
				return true;
			default:
				failedReason = "Unconditional error";
				return false;
			}

		}

		/// <summary>
		/// Updates the card balance in db.
		/// </summary>
		/// <returns><c>true</c>, if card balance in db was updated, <c>false</c> otherwise.</returns>
		/// <param name="CardNumber">Card number.</param>
		/// <param name="Balance">Balance.</param>
		/// <param name="trxDateTime">Trx date time.</param>
		public bool updateCardBalanceInDb(string CardNumber, decimal Balance, DateTime trxDateTime){
			Exception ExError = null;
			string sql = "UPDATE ucard_information SET " +
				" last_balance = " + Balance.ToString() + ", " +
				" modified_by = 'PowerHouse', "+
				" modified_time = '"+trxDateTime.ToString("yyyy-MM-dd HH:mm:ss")+"', " +
				" last_host = 'PowerHouseHost' " +
				" WHERE card_number = '" + CardNumber + "';";

			int i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			return (i > 0);
		}

		/// <summary>
		/// Updates the card blocked.
		/// </summary>
		/// <returns><c>true</c>, if card blocked was updated, <c>false</c> otherwise.</returns>
		/// <param name="CardNumber">Card number.</param>
		public bool updateCardBlocked(string CardNumber){
			Exception ExError = null;
			string sql = "UPDATE ucard_information SET " +
				" status = 3" +
				" WHERE card_number = '" + CardNumber + "';";

			int i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			return (i > 0);
		}

		//********************** Product List Query ***************************
		//======================= START =======================================

		public bool getProductGroupList(string appID, bool isPembelian, ref Hashtable hasil, 
			out Exception ExError)
		{
//			PEMBELIAN
//			SELECT a.id,a.name
//			FROM product_group a
//			INNER JOIN product b ON b.product_group_id=a.id
//				INNER JOIN product_sdistributor c ON c.product_id=b.id
//				WHERE
//				a.is_purchase = 't'
//				AND c.sd_code = '121' -- kalau nitrogen
//				GROUP BY a.id,a.name
//			ORDER BY a.name;

//			PEMBAYARAN
//			SELECT a.id,a.name
//			FROM product_group a
//			INNER JOIN product b ON b.product_group_id=a.id
//				INNER JOIN product_sdistributor c ON c.product_id=b.id
//				WHERE
//				a.is_payment = 't'
//				AND c.sd_code = '121' -- kalau nitrogen
//				GROUP BY a.id,a.name
//			ORDER BY a.name;

			ExError = null;
//			string sql = "SELECT a.id,a.name FROM product_group a WHERE a.is_payment = 't' ORDER BY a.name ;";
//			if (isPembelian)
//				sql = "SELECT a.id,a.name FROM product_group a WHERE a.is_purchase = 't' ORDER BY a.name;";

			string sql = "SELECT a.id,a.name FROM product_group a ";
			sql += "INNER JOIN product b ON b.product_group_id=a.id ";
			sql += "INNER JOIN product_sdistributor c ON c.product_id=b.id ";
			if (isPembelian) 
				sql += "WHERE a.is_purchase = 't' ";
			else
				sql += "WHERE a.is_payment = 't' ";
			sql += "AND c.sd_code = '" + appID.Trim () + "' GROUP BY a.id,a.name ";
			sql += "ORDER BY a.name;";

			if(hasil==null)
				hasil = new Hashtable();
			hasil.Clear ();
			int id = 0;
			string name = "";
			int i = 0;
			if (!querympPr (sql, ref i, out ExError)) {
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			if (i > 0) {
				for (int j = 0; j < i; j++) {
					id = (int)localDB.GetDataItem(tbl_mpProduct, j, "id");
					name = localDB.GetDataItem (tbl_mpProduct, j, "name").ToString ().Trim ();
					hasil.Add (id,name);
				}
			}
			return true;
		}

		public class ProductDetailsStruct: IDisposable {
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
			~ProductDetailsStruct()
			{
				this.Dispose(false);
			}
			#endregion

			private void disposeAll()
			{
				// disini dispose semua yang bisa di dispose
			}

			//public int list_code=0;	// 0 = grouplist; 1 = productlist
			public string code="";
			public string name="";
			public string group_code="";
			public string group_name="";
			public int price=0;
			public string image_cover="";
			public bool is_negotiable=false;
			public string description=""; 
			public string specification="";
			public int target_age=0;
			public string brand="";
			public string product_year="";
			public bool is_new=true;
			public bool is_personal=true;
			public string seller_name="";
			public string seller_location="";
			public string seller_phone="";
		}

		public bool getShopsProductCategory(string appID, ref Hashtable hasil, 
			out Exception ExError){
			string sql = "SELECT DISTINCT a.id,a.name FROM product_category a "
				+ "INNER JOIN customer_product d ON d.pcid = a.id "
				+ "INNER JOIN mystore b ON b.id = d.msid "
				+ "INNER JOIN person c ON c.id = b.pid "
				+ "INNER JOIN sys_member e ON e.pid = c.id "
				+ "WHERE e.comp_code = '" + appID + "' "//-- application ID
				+ "GROUP BY a.id,a.name";

			if(hasil==null)
				hasil = new Hashtable();
			hasil.Clear ();
			int id = 0;
			string name = "";
			int i = 0;
			if (!querympPr (sql, ref i, out ExError)) {
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			if (i > 0) {
				for (int j = 0; j < i; j++) {
					id = (int)localDB.GetDataItem (tbl_mpProduct, j, "id");
					name = localDB.GetDataItem (tbl_mpProduct, j, "name").ToString ().Trim ();
					hasil.Add (id,name);
				}
			}
			return true;
		}

		public bool getShopsProductDetails(string appID, string outlet_id, string keyword, 
			int product_category_id, int pcOffset, int pcLength, ref List<ProductDetailsStruct> hasil, 
			out Exception ExError)
		{
			ExError = null;
			string sql = "SELECT a.code,a.name, b.code as outlet_id, ";
			sql += "a.cover AS bacover, "; //-- cover teh gambar leutik na
			sql += "a.price,a.is_negotiated,a.description, a.specification, ";
			sql += "a.target_age, a.brand_name, a.specific_year, ";
			sql += "a.is_new_product, ";		// -- barang bekas atau barang baru ?
			sql += "a.is_personal_product, ";	// -- barang pribadi atau barang perusahaan ?
			sql += "f.code as header_product_code, ";
			sql += "f.name as header_product_name ";
			sql += "FROM customer_product a ";
			sql += "INNER JOIN mystore b ON b.id = a.msid ";
			sql += "INNER JOIN person c ON c.id = b.pid ";
			sql += "INNER JOIN sys_member e ON e.pid = c.id ";
			sql += "INNER JOIN product_category d ON d.id = a.pcid ";
			sql += "INNER JOIN product f ON f.code = b.product_code ";
			sql += "WHERE e.comp_code = '" + appID + "' ";	//  -- applicationID
			if(outlet_id != "")
				sql += "AND b.code = '" + outlet_id + "' ";	//"TOKO0000000002' -- kode ouelet"
			if (keyword != "") {
				sql += "AND ( upper(a.name) LIKE upper('%%" + keyword + "%%') ";
				sql += "OR upper(a.description) LIKE upper('%%" + keyword + "%%') ";
				sql += "OR upper(a.specification) LIKE upper('%%" + keyword + "%%') ";
				sql += "OR upper(d.name) LIKE upper('%%" + keyword + "%%') ) ";
			}
			sql += "AND a.pcid = '" + product_category_id.ToString () + "' ";	// -- id product category
			sql += "ORDER BY a.name ASC ";
//			sql += "OFFSET " + pcOffset.ToString () + " ";
//			sql += "LIMIT " + pcLength.ToString ();

			if(pcLength>0)
				sql += "OFFSET " + pcOffset.ToString () + " LIMIT " + pcLength.ToString () + ";";

			if(hasil==null)
				hasil = new List<ProductDetailsStruct> ();
			int i = 0;
			if (!querympPr (sql, ref i, out ExError)) {
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			if (i > 0) {
				string sPrice="";
				decimal dPrice=0;
				for (int j = 0; j < i; j++) {
					ProductDetailsStruct anItem = new ProductDetailsStruct ();
					try{
					anItem.code = ((string)localDB.GetDataItem (tbl_mpProduct, j, "code")).Trim ();}catch{
					}
					try{
					anItem.name = ((string)localDB.GetDataItem (tbl_mpProduct, j, "name")).Trim ();}catch{
					}
					try{
					anItem.image_cover = ((string)localDB.GetDataItem (tbl_mpProduct, j, "bacover")).Trim ();}catch{
					}
					try{
					anItem.group_code = ((string)localDB.GetDataItem (tbl_mpProduct, j, "header_product_code")).Trim ();}catch{
					}
					try{
					anItem.group_name = ((string)localDB.GetDataItem (tbl_mpProduct, j, "header_product_name")).Trim ();}catch{
					}

					try{
					sPrice = (localDB.GetDataItem (tbl_mpProduct, j, "price")).ToString ();}catch{
					}
					try{
					dPrice = decimal.Parse (sPrice);}catch{
					}

					try{
					//SellPrice = decimal.ToInt32 (Math.Floor (dSellPrice));
					anItem.price = decimal.ToInt32 (Math.Floor (dPrice));}catch{
					}

					try{
					//anItem.price = (int)localDB.GetDataItem(tbl_mpProduct, j, "price");
					anItem.is_negotiable = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_negotiated");}catch{
					}
					try{
					anItem.description = ((string)localDB.GetDataItem (tbl_mpProduct, j, "description")).Trim ();}catch{
					}
					try{
					anItem.specification = ((string)localDB.GetDataItem (tbl_mpProduct, j, "specification")).Trim ();}catch{
					}
					try{
						anItem.target_age=(Int16)localDB.GetDataItem(tbl_mpProduct, j, "target_age");}catch{
					}
					try{
						anItem.brand = ((string)localDB.GetDataItem (tbl_mpProduct, j, "brand_name")).Trim ();}catch{
					}
					try{
						anItem.product_year = ((string)localDB.GetDataItem (tbl_mpProduct, j, "specific_year")).Trim ();}catch{
					}
					try{
						anItem.is_new = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_new_product");}catch{
					}
					try{
						anItem.is_personal = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_personal_product");}catch{
					}
					hasil.Add (anItem);
				}
			}
			return true;
		}

		public bool getShopProductDetailUncompleted(string company_code, string productcode, 
			ref ProductDetailsStruct hasil,
			out Exception ExError)
		{
			ExError = null;
			string sql = "SELECT a.name, ";
			sql += "encode(a.cover, 'base64') AS bacover, "; //-- cover teh gambar leutik na
			sql += "a.price,a.is_negotiated,a.description, a.specification, ";
			sql += "a.is_new_product, ";		// -- barang bekas atau barang baru ?
			sql += "a.target_age, a.brand_name, a.specific_year, ";
			sql += "a.is_personal_product ";	// -- barang pribadi atau barang perusahaan ?
			sql += "FROM customer_product a ";
			sql += "INNER JOIN mystore b ON b.id = a.msid ";
			sql += "INNER JOIN person c ON c.id = b.pid ";
			sql += "INNER JOIN sys_member e ON e.pid = c.id ";
			sql += "INNER JOIN product_category d ON d.id = a.pcid ";
			sql += "WHERE e.comp_code = '" + company_code + "' ";
			sql += "AND a.code = '" + productcode + "';";

			if(hasil.code==null)
				hasil = new ProductDetailsStruct();
			hasil.code = productcode;
			int i = 0;
			if (!querympPr (sql, ref i, out ExError)) {
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			if (i > 0) {
				hasil.name = ((string)localDB.GetDataItem (tbl_mpProduct, 0, "name")).Trim ();
				hasil.image_cover = ((string)localDB.GetDataItem (tbl_mpProduct, 0, "bacover")).Trim ();
				hasil.price = (int)localDB.GetDataItem(tbl_mpProduct, 0, "price");
				hasil.is_negotiable = (bool)localDB.GetDataItem (tbl_mpProduct, 0, "is_negotiated");
				hasil.description = ((string)localDB.GetDataItem (tbl_mpProduct, 0, "description")).Trim ();
				hasil.specification = ((string)localDB.GetDataItem (tbl_mpProduct, 0, "specification")).Trim ();
				hasil.target_age=(Int16)localDB.GetDataItem(tbl_mpProduct, 0, "target_age");
				hasil.brand = ((string)localDB.GetDataItem (tbl_mpProduct, 0, "brand_name")).Trim ();
				hasil.product_year = ((string)localDB.GetDataItem (tbl_mpProduct, 0, "specific_year")).Trim ();
				hasil.is_new = (bool)localDB.GetDataItem (tbl_mpProduct, 0, "is_new_product");
				hasil.is_personal = (bool)localDB.GetDataItem (tbl_mpProduct, 0, "is_personal_product");
			}
			return true;
		}

		public bool getPulsaPrefixList(string appID, string pulsaGroupCode, ref Hashtable hasil, 
			out Exception ExError){

			int iPulsaGrpCode = 0;
			ExError = null;
			try{
				iPulsaGrpCode = int.Parse (pulsaGroupCode);
			}catch{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query Pulsa Prefix List"  +
					"\r\nResult: Invalid PulsaGroupCode = \""+pulsaGroupCode+"\" in query prefix");
				return false;
			}
			// nu anyar
			//			SELECT a.prefix
			//			FROM product a
			//			INNER JOIN product_sdistributor b ON b.product_id=a.id
			//			WHERE a.product_group_id = '2'
			//			AND b.sd_code = '121' -- kalau nitrogen
			//			GROUP BY a.prefix
			//			ORDER BY a.prefix;

			//			string sql = "SELECT a.prefix as name FROM product a WHERE a.product_group_id = '"+iPulsaGrpCode.ToString ()
			//				+"' GROUP BY a.prefix ORDER BY a.prefix ASC;";
			string sql = "SELECT a.prefix as name FROM product a ";
			sql += "INNER JOIN product_sdistributor b ON b.product_id=a.id ";
			sql += "WHERE a.product_group_id = '" + iPulsaGrpCode.ToString () + "' ";
			sql += "AND b.sd_code = '"+appID+"' ";
			sql += "GROUP BY a.prefix ORDER BY a.prefix ASC;";

			if(hasil==null)
				hasil = new Hashtable();
			hasil.Clear ();
			int id = 0;
			string name = "";
			int i = 0;
			if (!querympPr (sql, ref i, out ExError)) {
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			if (i > 0) {
				for (int j = 0; j < i; j++) {
					id = (j+1);
					name = localDB.GetDataItem (tbl_mpProduct, j, "name").ToString ().Trim ();
					hasil.Add (id,name);
				}
			}
			return true;
		}

		public bool getPulsaProductList(string appID, string prefixPulsa, int pcOffset, int pcLength,
			ref List<ProductDetailsStruct> hasil, 
			out Exception ExError){

			// nu anyar
			//			SELECT a.code,a.name,COALESCE(b.total_price,0) as price
			//			FROM product a
			//			INNER JOIN cogs_price b ON b.id=a.cogs_id
			//			INNER JOIN product_sdistributor c ON c.product_id=a.id
			//			WHERE
			//			a.prefix = 'Pulsa Kartu Telkomsel'
			//			AND c.sd_code = '121' -- kalau nitrogen
			//			ORDER BY a.code
			//			OFFSET ?? LIMIT ??

//			string sql = "SELECT a.code,a.name,COALESCE(b.total_price,0) as price ";
//			sql += "FROM product a ";
//			sql += "LEFT JOIN cogs_price b ON b.id=a.cogs_id WHERE a.prefix = '" + prefixPulsa + "' ";

			string sql = "SELECT a.code,a.name,COALESCE(b.total_price,0) as price ";
			sql += "FROM product a ";
			sql += "INNER JOIN cogs_price b ON b.id=a.cogs_id ";
			sql += "INNER JOIN product_sdistributor c ON c.product_id=a.id ";
			sql += "WHERE a.prefix = '" + prefixPulsa + "' ";
			sql += "AND c.sd_code = '" + appID + "' ";
			sql += "ORDER BY a.code ASC ";

			//sql += "ORDER BY a.code OFFSET ?? LIMIT ??
			if(pcLength>0)
				sql += "OFFSET " + pcOffset.ToString () + " LIMIT " + pcLength.ToString () + ";";

			if(hasil==null)
				hasil = new List<ProductDetailsStruct> ();
			int i = 0;
			if (!querympPr (sql, ref i, out ExError)) {
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			if (i > 0) {
				string sPrice="";
				decimal dPrice=0;
				for (int j = 0; j < i; j++) {
					ProductDetailsStruct anItem = new ProductDetailsStruct ();
					anItem.code = ((string)localDB.GetDataItem (tbl_mpProduct, j, "code")).Trim ();
					anItem.name = ((string)localDB.GetDataItem (tbl_mpProduct, j, "name")).Trim ();
					//anItem.bacover = ((string)localDB.GetDataItem (tbl_mpProduct, j, "bacover")).Trim ();

					sPrice = (localDB.GetDataItem (tbl_mpProduct, j, "price")).ToString ();
					dPrice = decimal.Parse (sPrice);
					//SellPrice = decimal.ToInt32 (Math.Floor (dSellPrice));
					anItem.price = decimal.ToInt32 (Math.Floor (dPrice));
					//anItem.price = (int)localDB.GetDataItem(tbl_mpProduct, j, "price");

//					anItem.isNegotiable = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_negotiated");
//					anItem.description = ((string)localDB.GetDataItem (tbl_mpProduct, j, "description")).Trim ();
//					anItem.specification = ((string)localDB.GetDataItem (tbl_mpProduct, j, "specification")).Trim ();
//					anItem.target_age=(Int16)localDB.GetDataItem(tbl_mpProduct, j, "target_age");
//					anItem.brand = ((string)localDB.GetDataItem (tbl_mpProduct, j, "brand_name")).Trim ();
//					anItem.specyear = ((string)localDB.GetDataItem (tbl_mpProduct, j, "specific_year")).Trim ();
//					anItem.is_new = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_new_product");
//					anItem.is_personal = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_personal_product");
					hasil.Add (anItem);
				}
			}
			return true;
		}


		//getPurchaseProductList
		public bool getPurchaseProductList(string appID, int queryCode, int pcOffset, int pcLength,
			ref List<ProductDetailsStruct> hasil, 
			out Exception ExError){

			//			-- PEMBELIAN
			//			SELECT DISTINCT a.code,a.name,COALESCE(b.total_price,0) as price
			//			FROM product a
			//			INNER JOIN cogs_price b ON b.id=a.cogs_id AND b.is_active = 't'
			//			INNER JOIN cogs_price_d c ON c.crpid=b.id
			//			INNER JOIN product_sdistributor d ON d.product_id=a.id
			//			WHERE d.sd_code = '121' -- kalau nitrogen
			//			AND a.product_group_id = 'product_group_id_kiriman_dari_mobile'
			//			ORDER BY a.code


//			string sql = "SELECT a.code,a.name,COALESCE(b.total_price,0) as price FROM product a ";
//			sql += "INNER JOIN cogs_price b ON b.id=a.cogs_id ";
//			sql += "INNER JOIN cogs_price_d c ON c.crpid=b.id ";
//			sql += "WHERE a.product_group_id = '" + queryCode.ToString () + "' ";
//			sql += "ORDER BY a.code ASC ";

			string sql = "SELECT DISTINCT a.code,a.name,COALESCE(b.total_price,0) as price FROM product a ";
			sql += "INNER JOIN cogs_price b ON b.id=a.cogs_id AND b.is_active = 't' ";
			sql += "INNER JOIN cogs_price_d c ON c.crpid=b.id ";
			sql += "INNER JOIN product_sdistributor d ON d.product_id=a.id ";
			sql += "WHERE d.sd_code = '" + appID + "' ";
			sql += "AND a.product_group_id = '" + queryCode.ToString () + "' ";
			sql += "ORDER BY a.code ASC ";

			if(pcLength>0)
				sql += "OFFSET " + pcOffset.ToString () + " LIMIT " + pcLength.ToString () + ";";

			if(hasil==null)
				hasil = new List<ProductDetailsStruct> ();
			int i = 0;
			if (!querympPr (sql, ref i, out ExError)) {
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			if (i > 0) {
				string sPrice="";
				decimal dPrice=0;
				for (int j = 0; j < i; j++) {
					ProductDetailsStruct anItem = new ProductDetailsStruct ();
					anItem.code = ((string)localDB.GetDataItem (tbl_mpProduct, j, "code")).Trim ();
					anItem.name = ((string)localDB.GetDataItem (tbl_mpProduct, j, "name")).Trim ();
					//anItem.bacover = ((string)localDB.GetDataItem (tbl_mpProduct, j, "bacover")).Trim ();

					sPrice = (localDB.GetDataItem (tbl_mpProduct, j, "price")).ToString ();
					dPrice = decimal.Parse (sPrice);
					//SellPrice = decimal.ToInt32 (Math.Floor (dSellPrice));
					anItem.price = decimal.ToInt32 (Math.Floor (dPrice));
					//anItem.price = (int)localDB.GetDataItem(tbl_mpProduct, j, "price");

					//					anItem.isNegotiable = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_negotiated");
					//					anItem.description = ((string)localDB.GetDataItem (tbl_mpProduct, j, "description")).Trim ();
					//					anItem.specification = ((string)localDB.GetDataItem (tbl_mpProduct, j, "specification")).Trim ();
					//					anItem.target_age=(Int16)localDB.GetDataItem(tbl_mpProduct, j, "target_age");
					//					anItem.brand = ((string)localDB.GetDataItem (tbl_mpProduct, j, "brand_name")).Trim ();
					//					anItem.specyear = ((string)localDB.GetDataItem (tbl_mpProduct, j, "specific_year")).Trim ();
					//					anItem.is_new = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_new_product");
					//					anItem.is_personal = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_personal_product");
					hasil.Add (anItem);
				}
			}
			return true;
		}

		public bool getPaymentProductList(string appID, int queryCode, int pcOffset, int pcLength,
			ref List<ProductDetailsStruct> hasil, 
			out Exception ExError){

//			-- PEMBAYARAN
//			SELECT a.code,a.name,COALESCE(b.total_price,0) as price
//			FROM product a
//			INNER JOIN cogs_price b ON b.id=a.cogs_id
//			INNER JOIN product_sdistributor c ON c.product_id=a.id
//			WHERE b.id NOT IN ( SELECT x.crpid as id FROm cogs_price_d x )
//			AND a.product_group_id = 'product_group_id_kiriman_dari_mobile'
//			AND c.sd_code = '121' -- kalau nitrogen

//			string sql = "SELECT a.code,a.name,COALESCE(b.total_price,0) as price FROM product a ";
//			sql += "INNER JOIN cogs_price b ON b.id=a.cogs_id ";
//			sql += "WHERE b.id NOT IN ( SELECT x.crpid as id FROm cogs_price_d x ) ";
//			sql += "AND a.product_group_id = '" + queryCode.ToString () + "' ";
//			sql += "ORDER BY a.code ASC ";

			string sql = "SELECT a.code,a.name,COALESCE(b.total_price,0) as price FROM product a ";
			sql += "INNER JOIN cogs_price b ON b.id=a.cogs_id ";
			sql += "INNER JOIN product_sdistributor c ON c.product_id=a.id ";
			sql += "WHERE b.id NOT IN ( SELECT x.crpid as id FROm cogs_price_d x ) ";
			sql += "AND a.product_group_id = '" + queryCode.ToString () + "' ";
			sql += "AND c.sd_code = '" + appID + "' ";
			sql += "ORDER BY a.code ASC ";

			if(pcLength>0)
				sql += "OFFSET " + pcOffset.ToString () + " LIMIT " + pcLength.ToString () + ";";


			if(hasil==null)
				hasil = new List<ProductDetailsStruct> ();
			int i = 0;
			if (!querympPr (sql, ref i, out ExError)) {
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			if (i > 0) {
				string sPrice="";
				decimal dPrice=0;
				for (int j = 0; j < i; j++) {
					ProductDetailsStruct anItem = new ProductDetailsStruct ();
					anItem.code = ((string)localDB.GetDataItem (tbl_mpProduct, j, "code")).Trim ();
					anItem.name = ((string)localDB.GetDataItem (tbl_mpProduct, j, "name")).Trim ();
					//anItem.bacover = ((string)localDB.GetDataItem (tbl_mpProduct, j, "bacover")).Trim ();

					sPrice = (localDB.GetDataItem (tbl_mpProduct, j, "price")).ToString ();
					dPrice = decimal.Parse (sPrice);
					//SellPrice = decimal.ToInt32 (Math.Floor (dSellPrice));
					anItem.price = decimal.ToInt32 (Math.Floor (dPrice));
					//anItem.price = (int)localDB.GetDataItem(tbl_mpProduct, j, "price");

					//					anItem.isNegotiable = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_negotiated");
					//					anItem.description = ((string)localDB.GetDataItem (tbl_mpProduct, j, "description")).Trim ();
					//					anItem.specification = ((string)localDB.GetDataItem (tbl_mpProduct, j, "specification")).Trim ();
					//					anItem.target_age=(Int16)localDB.GetDataItem(tbl_mpProduct, j, "target_age");
					//					anItem.brand = ((string)localDB.GetDataItem (tbl_mpProduct, j, "brand_name")).Trim ();
					//					anItem.specyear = ((string)localDB.GetDataItem (tbl_mpProduct, j, "specific_year")).Trim ();
					//					anItem.is_new = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_new_product");
					//					anItem.is_personal = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_personal_product");
					hasil.Add (anItem);
				}
			}
			return true;
		}


		// ================ END OF Product List Query ====================

		public bool addCardTransactionLog(long transactionID, string samCSN,
			string cardPurchaseLog, int previousBalance, string sourceId, out Exception ExError){
			ExError = null;
			string sqlCheck = "SELECT sam_csn FROM ucard_transaction WHERE trx_id = " + transactionID.ToString ();
			string sql = "INSERT INTO ucard_transaction ( " +
				"trx_id,card_purchase_log,created_time, previous_balance";
			if(samCSN!="")
				sql += ", sam_csn";
			if(sourceId!="")
				sql += ", source_id";
			sql += ") VALUES (";
			sql += transactionID.ToString () + ",'" + cardPurchaseLog + "','" +
				DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss") + "'," + previousBalance.ToString ();
			if(samCSN!="")
				sql += ",'" + samCSN.ToUpper () + "'";
			if(sourceId!="")
				sql += ",'" + sourceId.ToUpper () + "'";
			sql += ")";

			int i = 0;
			if (!querympPr (sqlCheck, ref i, out ExError)) {
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			if (i > 0) {
				return true;
			}

			//LogWriter.showDEBUG(this, "---- DEBUG  SQL = " + sql);
			i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}

			return (i > 0);

		}

//		id bigserial NOT NULL,
//		trx_id bigint,
//		sam_csn character varying, -- sam csn aau nomor HP agen
//		card_purchase_log character varying, -- log dari transaksi kartu untuk di verifikasi baik lewat MAC atau signature
//		created_time timestamp without time zone NOT NULL,
//		CONSTRAINT ucard_transaction_pkey PRIMARY KEY (id),
//		CONSTRAINT ucard_transaction_fk FOREIGN KEY (trx_id)
//			REFERENCES transaction (id) MATCH SIMPLE
//			ON UPDATE CASCADE ON DELETE CASCADE
		/// <summary>
		/// Adds the card transaction log.
		/// </summary>
		/// <returns><c>true</c>, if card transaction log was added, <c>false</c> otherwise.</returns>
		/// <param name="transactionID">ID dari table transaction.</param>
		/// <param name="samCSN">Sam CSN.</param>
		/// <param name="cardPurchaseLog">Card purchase log.</param>
		/// <param name="previousBalance">Previous balance.</param>
		/// <param name="ExError">Exception error output.</param>
		public bool addCardTransactionLog(long transactionID, string samCSN,
			string cardPurchaseLog, int previousBalance, out Exception ExError){
			ExError = null;
			string sqlCheck = "SELECT sam_csn FROM ucard_transaction WHERE trx_id = " + transactionID.ToString ();
			string sql = "INSERT INTO ucard_transaction ( " +
			             "trx_id,card_purchase_log,created_time, previous_balance";
			if(samCSN!="")
				sql += ", sam_csn";
			sql += ") VALUES (";
			sql += transactionID.ToString () + ",'" + cardPurchaseLog + "','" +
			DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss") + "'," + previousBalance.ToString ();
			if(samCSN!="")
				sql += ",'" + samCSN.ToUpper ();
			sql += "')";
	
			int i = 0;
			if (!querympPr (sqlCheck, ref i, out ExError)) {
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			if (i > 0) {
				return true;
			}

			//LogWriter.showDEBUG(this, "---- DEBUG  SQL = " + sql);
			i = localDB.ExecNonQuerySql(sql, out ExError);
			if (ExError != null)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}

			return (i > 0);

		}

		public bool getNitrogenPrefixList(string nitrogenGroupCode, ref Hashtable hasil, 
			out Exception ExError){
			int iNitrogenGrpCode = 0;
			ExError = null;
			try{
				iNitrogenGrpCode = int.Parse (nitrogenGroupCode);
			}catch{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query Nitrogen Prefix List"  +
					"\r\nResult: Invalid NitrogenGroupCode = \""+nitrogenGroupCode+"\" in query prefix");
				return false;
			}
			string sql = "SELECT a.prefix as name FROM product a WHERE a.product_group_id = "+iNitrogenGrpCode.ToString ()
				+" GROUP BY a.prefix ORDER BY a.prefix ASC;";

			if(hasil==null)
				hasil = new Hashtable();
			hasil.Clear ();
			int id = 0;
			string name = "";
			int i = 0;
			if (!querympPr (sql, ref i, out ExError)) {
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			if (i > 0) {
				for (int j = 0; j < i; j++) {
					id = (j+1);
					name = localDB.GetDataItem (tbl_mpProduct, j, "name").ToString ().Trim ();
					hasil.Add (id,name);
				}
			}
			return true;
		}

		public bool getNitrogenProductList(string prefixNitrogen, int pcOffset, int pcLength,
			ref List<ProductDetailsStruct> hasil, 
			out Exception ExError){

//			-- QUERY PRODUCT NITORGEN
//			SELECT a.code,a.name,COALESCE(b.total_price,0) as price
//			FROM product a        
//			INNER JOIN cogs_price b ON b.id=a.cogs_id
//			INNER JOIN cogs_price_d c ON c.crpid=b.id
//			WHERE a.prefix = 'Nitrogen Isi Mobil Penuh';


			string sql = "SELECT a.code,a.name,COALESCE(b.total_price,0) as price ";
			sql += "FROM product a ";
			sql += "INNER JOIN cogs_price b ON b.id=a.cogs_id ";
			sql += "INNER JOIN cogs_price_d c ON c.crpid=b.id ";
			sql += "WHERE a.prefix = '" + prefixNitrogen + "' ";
			if(pcLength>0)
				sql += "ORDER BY a.code ASC OFFSET " + pcOffset.ToString () + " LIMIT " + pcLength.ToString () + ";";
			else
				sql += "ORDER BY a.code ASC;";

			if(hasil==null)
				hasil = new List<ProductDetailsStruct> ();
			int i = 0;
			if (!querympPr (sql, ref i, out ExError)) {
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			if (i > 0) {
				string sPrice="";
				decimal dPrice=0;
				for (int j = 0; j < i; j++) {
					ProductDetailsStruct anItem = new ProductDetailsStruct ();
					anItem.code = ((string)localDB.GetDataItem (tbl_mpProduct, j, "code")).Trim ();
					anItem.name = ((string)localDB.GetDataItem (tbl_mpProduct, j, "name")).Trim ();
					//anItem.bacover = ((string)localDB.GetDataItem (tbl_mpProduct, j, "bacover")).Trim ();

					sPrice = (localDB.GetDataItem (tbl_mpProduct, j, "price")).ToString ();
					dPrice = decimal.Parse (sPrice);
					//SellPrice = decimal.ToInt32 (Math.Floor (dSellPrice));
					anItem.price = decimal.ToInt32 (Math.Floor (dPrice));
					//anItem.price = (int)localDB.GetDataItem(tbl_mpProduct, j, "price");

					//					anItem.isNegotiable = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_negotiated");
					//					anItem.description = ((string)localDB.GetDataItem (tbl_mpProduct, j, "description")).Trim ();
					//					anItem.specification = ((string)localDB.GetDataItem (tbl_mpProduct, j, "specification")).Trim ();
					//					anItem.target_age=(Int16)localDB.GetDataItem(tbl_mpProduct, j, "target_age");
					//					anItem.brand = ((string)localDB.GetDataItem (tbl_mpProduct, j, "brand_name")).Trim ();
					//					anItem.specyear = ((string)localDB.GetDataItem (tbl_mpProduct, j, "specific_year")).Trim ();
					//					anItem.is_new = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_new_product");
					//					anItem.is_personal = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_personal_product");
					hasil.Add (anItem);
				}
			}
			return true;
		}

		public bool getNitrogenGroupProductList(string nitrogenGroupCode, int pcOffset, int pcLength,
			ref Hashtable hasil, 
			out Exception ExError){

			int iNitrogenGrpCode = 0;
			ExError = null;
			try{
				iNitrogenGrpCode = int.Parse (nitrogenGroupCode);
			}catch{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query Nitrogen Prefix List"  +
					"\r\nResult: Invalid NitrogenGroupCode = \""+nitrogenGroupCode+"\" in query prefix");
				return false;
			}
			//List<ProductDetailsStruct> aProduct;
			//			-- QUERY PRODUCT NITORGEN
			//			SELECT a.prefix, a.code,a.name,COALESCE(b.total_price,0) as price
			//			FROM product a        
			//			INNER JOIN cogs_price b ON b.id=a.cogs_id
			//			INNER JOIN cogs_price_d c ON c.crpid=b.id
			//			WHERE a.product_group_id=14 
			//			ORDER BY a.prefix,a.name ASC;

			string sql = "SELECT a.prefix, a.code,a.name,COALESCE(b.total_price,0) as price ";
			sql += "FROM product a ";
			sql += "INNER JOIN cogs_price b ON b.id=a.cogs_id ";
			sql += "INNER JOIN cogs_price_d c ON c.crpid=b.id ";
			sql += "WHERE a.product_group_id=" + iNitrogenGrpCode.ToString () + " ";
			sql += "ORDER BY a.prefix, a.name ASC ";
			if(pcLength>0)
				sql += "OFFSET " + pcOffset.ToString () + " LIMIT " + pcLength.ToString () + ";";

			Hashtable allNitroProduct = new Hashtable ();
//			if(hasil==null)
//				hasil = new List<ProductDetailsStruct> ();
			int i = 0;
			if (!querympPr (sql, ref i, out ExError)) {
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			if (i > 0) {
				string sPrice="";
				decimal dPrice=0;
				string prefix = "";
				for (int j = 0; j < i; j++) {
					ProductDetailsStruct anItem = new ProductDetailsStruct ();
					anItem.code = ((string)localDB.GetDataItem (tbl_mpProduct, j, "code")).Trim ();
					anItem.name = ((string)localDB.GetDataItem (tbl_mpProduct, j, "name")).Trim ();
					//anItem.bacover = ((string)localDB.GetDataItem (tbl_mpProduct, j, "bacover")).Trim ();

					sPrice = (localDB.GetDataItem (tbl_mpProduct, j, "price")).ToString ();
					dPrice = decimal.Parse (sPrice);
					//SellPrice = decimal.ToInt32 (Math.Floor (dSellPrice));
					anItem.price = decimal.ToInt32 (Math.Floor (dPrice));
					//anItem.price = (int)localDB.GetDataItem(tbl_mpProduct, j, "price");

					//					anItem.isNegotiable = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_negotiated");
					//					anItem.description = ((string)localDB.GetDataItem (tbl_mpProduct, j, "description")).Trim ();
					//					anItem.specification = ((string)localDB.GetDataItem (tbl_mpProduct, j, "specification")).Trim ();
					//					anItem.target_age=(Int16)localDB.GetDataItem(tbl_mpProduct, j, "target_age");
					//					anItem.brand = ((string)localDB.GetDataItem (tbl_mpProduct, j, "brand_name")).Trim ();
					//					anItem.specyear = ((string)localDB.GetDataItem (tbl_mpProduct, j, "specific_year")).Trim ();
					//					anItem.is_new = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_new_product");
					//					anItem.is_personal = (bool)localDB.GetDataItem (tbl_mpProduct, j, "is_personal_product");

					//hasil.Add (anItem);

					prefix = ((string)localDB.GetDataItem (tbl_mpProduct, j, "prefix")).Trim ();
					if (!allNitroProduct.ContainsKey (prefix))
						allNitroProduct.Add (prefix, new List<ProductDetailsStruct> ());

					((List<ProductDetailsStruct>)allNitroProduct [prefix]).Add (anItem);

				}
			}
			hasil = allNitroProduct;
			return true;
		}

		public struct OutletListStruct
		{
			public string code;
			public string name;
			public string phone;
		}

		public bool getOutletList(string applicationID, int pcOffset, int pcLength,
			ref List<OutletListStruct> hasil, 
			out Exception ExError){

			//		-- QUERY  GET outlet List : 
			//			SELECT
			//			a.name as outlet_name,
			//			a.mobile_phone_number as outlet_hp,
			//			c.code as outlet_code,
			//			c.address as outlet_address,
			//			c.phone as outlet_phone,
			//			c.fax as outlet_fax,
			//			c.abstraction,c.market_text,
			//			c.logo as outlet_logo, -- berupa URL
			//			c.picture as outlet_picture, -- berupa URL
			//			c.product_code -- kode produk pembungkus!!
			//			FROM person a
			//			INNER JOIN sys_member b ON b.pid=a.id
			//				INNER JOIN mystore c ON c.pid = a.id
			//				WHERE
			//				b.comp_code = '121' -- kode mitra N2
			//				AND b.member_status_id = 2 -- harus akun yg sudah aktif


			string sql = "SELECT c.code as outlet_code, a.name as outlet_name, a.mobile_phone_number as outlet_phone ";
			sql += "FROM person a ";
			sql += "INNER JOIN sys_member b ON b.pid=a.id ";
			sql += "INNER JOIN mystore c ON c.pid = a.id ";
			sql += "WHERE b.comp_code = '" + applicationID + "' AND b.member_status_id = 2";
			sql += "ORDER BY c.code ASC ";
			if(pcLength>0)
				sql += "OFFSET " + pcOffset.ToString () + " LIMIT " + pcLength.ToString () + ";";

			if(hasil==null)
				hasil = new List<OutletListStruct > ();

			int i = 0;
			if (!querympPr (sql, ref i, out ExError)) {
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on query: \r\n" + sql +
					"\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
				return false;
			}
			if (i > 0) {
				for (int j = 0; j < i; j++) {
					OutletListStruct anItem = new OutletListStruct ();
					anItem.code = ((string)localDB.GetDataItem (tbl_mpProduct, j, "outlet_code")).Trim ();
					anItem.name = ((string)localDB.GetDataItem (tbl_mpProduct, j, "outlet_name")).Trim ();
					anItem.phone = ((string)localDB.GetDataItem (tbl_mpProduct, j, "outlet_phone")).Trim ();

					hasil.Add (anItem);
				}
			}
			return true;
		}

    }
}
