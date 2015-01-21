using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using CitilinkLib;

namespace CitilinkTest
{
    class Program
    {
        static CitilinkProcs citiLib;

        static void writeToFileText(string mydata, string mypath, string myfile)
        {

            try
            {
                // save log to file
                if (!Directory.Exists(mypath))
                {
                    Directory.CreateDirectory(mypath);
                }
                //string Logtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                //if (!File.Exists(mypath + "/" + myfile))
                if (!File.Exists(mypath + "\\" + myfile))
                {
                    // Create a file to write to.                    
                    using (StreamWriter sw = File.CreateText(mypath + "/" + myfile))
                    //using (StreamWriter sw = File.CreateText(mypath + "\\" + myfile))
                    {
                        //sw.WriteLine(Logtime+" : "+mydata);
                        sw.WriteLine(mydata);
                        sw.Close();
                    }

                }
                else
                {
                    //using (StreamWriter sw = File.AppendText(mypath + "/" + myfile))
                    using (StreamWriter sw = File.AppendText(mypath + "\\" + myfile))
                    {
                        //sw.WriteLine(Logtime + " : " + mydata);
                        sw.WriteLine(mydata);
                    }
                }
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.StackTrace);
            }
        }

        static void Main2()
        {
            DateTime skrg = DateTime.Now;

            string DepartureBeginDate = skrg.AddDays(2).ToString("yyyy-MM-dd");
            string DepartureEndDate = skrg.AddDays(2).ToString("yyyy-MM-dd");
            string ArrivalBeginDate = skrg.AddDays(2).ToString("yyyy-MM-dd");
            string ArrivalEndDate = skrg.AddDays(2).ToString("yyyy-MM-dd");
            DepartureBeginDate = skrg.ToString("yyyy-MM-dd");
            DepartureEndDate = skrg.ToString("yyyy-MM-dd");
            ArrivalBeginDate = skrg.ToString("yyyy-MM-dd");
            ArrivalEndDate = skrg.ToString("yyyy-MM-dd");
            string DepartureStation = "MES";//"CGK";
            string ArrivalStation = "SUB";
            //string DepartureStation = "DPS";
            //string ArrivalStation = "CGK";
            short paxADT = 2;
            short paxCHD = 1;
            bool isError = false;
            JsonLibs.MyJsonLib availJson1;
            JsonLibs.MyJsonLib availJson2;

            CitilinkHandler ch = new CitilinkLib.CitilinkHandler("api_dam", "@pi_Dam0314","EXT");

            ch.agentSignature = "RQuVsUEKxto=|7pnLDwp+vFn1FrAo6XQwTFGoVn1PjzfMkBRQlNCEpmCmNgeGyQywfjQLvripWdcQy4j7wvWKHXAFifnzJ+OlcHO3Y94TDx7JbqNOPqzGRqtNZEHi/9vARZuPKk+TM0pS4OvjPf30ARk=";
            //ch.agentSignature = "vXRVseUJp4I=|GlEOZOicOg5+FSHG7SJ0mmInkxPZWAKFn3Xrihj/e5CheTtqUvQiuFu0n3Cu7UHYUJTtmSNTEeW+Z+6YdDKfi9l4wf4zuQ3vY9XsxBzgCkXJfBZCnvkhdYJb1fMbMOcWo47ZhnCTg5U=";
            //ch.isLoggedIn = true;

            availJson1 = ch.GetAvailability(DepartureStation, ArrivalStation, paxADT, paxCHD,
                DateTime.ParseExact(DepartureBeginDate, "yyyy-MM-dd", null),
                DateTime.ParseExact(DepartureEndDate, "yyyy-MM-dd", null),
                CitilinkHandler.InOutbound.Outbound, ref isError);

            if (isError)
            {
                Console.WriteLine(ch.lastErrorMessage);
                ch.CloseSession();
                ch.Dispose();
                Console.ReadLine();
                return;
            }

            //ch.CloseSession();
            //ch.isLoggedIn = true;
            //string logdir2 = "C:\\citylinklogs\\";
            //string ftime2 = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            //string logfile2 = "getavailability_" + ftime2 + ".txt";
            //writeToFileText(ch.agentSignature, logdir2, logfile2);
            //Process.Start("notepad.exe", logdir2 + "\\" + logfile2);

            availJson2 = ch.GetAvailability(ArrivalStation, DepartureStation, paxADT, paxCHD,
                DateTime.ParseExact(ArrivalBeginDate, "yyyy-MM-dd", null),
                DateTime.ParseExact(ArrivalEndDate, "yyyy-MM-dd", null),
                CitilinkHandler.InOutbound.Outbound, ref isError);

            if (isError)
            {
                Console.WriteLine(ch.lastErrorMessage);
                ch.CloseSession();
                ch.Dispose();
                Console.ReadLine();
                return;
            }

            // gabungkan json nya
            availJson1.AddArrayItem("Schedules", ((JsonLibs.MyJsonArray)(availJson2["Schedules"]))[0]);

            Console.WriteLine("Nepi");

            string logdir = "C:\\citylinklogs\\";
            string ftime = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            string logfile = "getavailability_" + ftime + ".txt";
            writeToFileText(availJson1.JSONConstruct(), logdir, logfile);
            Process.Start("notepad.exe", logdir + "\\" + logfile);

            //availJson2
            //ch.sellRequestALL(

            ch.CloseSession();
            ch.Dispose();
        }

        static void Main(string[] args)
        {
            //cekJson();
            //return;
            //string domain = "qiosku.com";
            //string hasil = "asli";
            //string email = hasil + @"@" + domain;
            //Console.WriteLine(email);
            //Console.ReadLine();
            //return;

            Main2();
            return;

            Console.WriteLine("Create New Session");
            citiLib = new CitilinkProcs();
            citiLib.OpenSession();

            // 1. Logon, response signature data
            Console.WriteLine("# LOGON =======================================================================");
            string ret = citiLib.Logon();
            if (ret == "")
            {
                Console.WriteLine("GAGAL LOGON");
                Console.ReadLine();
                return;
            }
            Console.WriteLine("Response : ");
            Console.WriteLine(ret);

            // 2. GetAvailability, response jadwal penerbangan journey
            Console.WriteLine("");
            Console.WriteLine("# GET Availability =======================================================================");

            DateTime skrg = DateTime.Now;
            //string fiApplicationId = "001";
            string fiOutboundBeginDate = skrg.AddDays(2).ToString("yyyy-MM-dd");
            string fiOutboundEndDate = skrg.AddDays(2).ToString("yyyy-MM-dd");
            string fiInboundBeginDate = skrg.AddDays(5).ToString("yyyy-MM-dd");
            string fiInboundEndDate = skrg.AddDays(5).ToString("yyyy-MM-dd");
            string fiDepartureStation = "MES";//"CGK";
            string fiArrivalStation = "SUB";
            short paxCount;
            int fiPaxCountADT = 2;
            int fiPaxCountCHD = 2;
            //int fiPaxCountINF = 1;
            //string fiInboundOutbound = "Both";
            //string fiProductCode = "PRD00037";
            //string fiPhone = "";

            paxCount = (short)(fiPaxCountADT + fiPaxCountCHD);

            //string departureStation = "CGK";
            //string arrivalStation = "DPS";
            //DateTime t1 = DateTime.Now.AddDays(1);
            //DateTime t2 = DateTime.Now.AddDays(1);
            DateTime td1;
            DateTime td2;
            DateTime ta1;
            DateTime ta2;
            try
            {
                td1 = DateTime.ParseExact(fiOutboundBeginDate, "yyyy-MM-dd", null);
                td2 = DateTime.ParseExact(fiOutboundEndDate, "yyyy-MM-dd", null);
                ta1 = DateTime.ParseExact(fiInboundBeginDate, "yyyy-MM-dd", null);
                ta2 = DateTime.ParseExact(fiInboundEndDate, "yyyy-MM-dd", null);
            }
            catch
            {
                // gagal parsing, format tanggal salah
                Console.WriteLine("Gagal parsing tanggal");
                return;
            }
            //string carrierCode = "QG";
            //string currencyCode = "IDR";
            //short maximumConnectingFlights = 0;
            //int minimumFarePrice = 0;
            //int maximumFarePrice = 0;
            //int nightsStay = 0;

            // set penumpang:
            //short paxCount = 4; // ADT & CHD
            //short infantCount = 0; // jumlah bayi            

            //bool includeAllotments = false;
            string[] priceTypes = new string[paxCount];
            string[] paxDiscountCode = new string[paxCount];

            int jmlAdt = fiPaxCountADT;
            int jmlChd = fiPaxCountCHD;
            for (int i = 0; i < paxCount; i++)
            {
                if (jmlAdt > 0)
                {
                    priceTypes[i] = "ADT"; // adult/dewasa
                    jmlAdt--;
                }
                else priceTypes[i] = "CHD"; // Child/anak2
                paxDiscountCode[i] = String.Empty;
            }

            ret = citiLib.GetAvailability(fiDepartureStation, fiArrivalStation, td1, td2, ta1, ta2,
                paxCount, priceTypes, paxDiscountCode, CitilinkProcs.InOutbound.Outbound);
            if (ret == "")
            {
                Console.WriteLine("Tidak dapat balasan;");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Response : ");
            //Console.WriteLine(ret);

            // deserialize json data
            //JsonLibs.MyJsonLib jh = new JsonLibs.MyJsonLib();
            //jh.JSONParse(ret);

            //jh.Dispose();
            //Console.WriteLine(djson);

            // tulis response ka file :
            if (ret != "")
            {
                string logdir = "C:\\citylinklogs\\";
                string ftime = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
                string logfile = "getavailability_" + ftime + ".json";
                writeToFileText(ret, logdir, logfile);
                Process.Start("notepad.exe", logdir + "\\" + logfile);
            }


            // last, close session
            Console.WriteLine("Close Session.");
            citiLib.CloseSession();



            Console.WriteLine("All Test Done.");
            // Console.ReadLine();
        }

        static string[] contactFields = { "fiContactFirstName", "fiContactLastName", "fiContactTitle", 
                                         "fiContactEmailAddress", "fiContactHomePhone", 
                                         "fiContactWorkPhone", "fiContactOtherPhone", "fiContactFax", 
                                         "fiContactCompanyName", "fiContactAddressLine1", 
                                         "fiContactAddressLine2", "fiContactAddressLine3", 
                                         "fiContactCity", "fiContactProvinceState", 
                                         "fiContactPostalCode", "fiCountryCode", "fiCultureCode", 
                                         "fiDistributionOption", "fiCustomerNumber", 
                                         "fiNotificationPreference", "fiSourceOrganization" };
        static string alljson = @"{""fiPassengers"": [{""InfantNationality"": """",""IsInfant"": false,""DOB"": ""1990-01-01"",""InfantWightType"": """",""LastName"": ""1"",""Nationality"": ""ID"",""Title"": ""Mr"",""InfantDOB"": """",""InfantTitle"": """",""InfantFirstName"": """",""InfantLastName"": """",""InfantGender"": """",""InfantMiddleName"": """",""Gender"": ""Male"",""FirstName"": ""Dewasa"",""PaxTypes"": ""ADT"",""WightType"": """"},{""InfantNationality"": """",""IsInfant"": false,""DOB"": ""1990-01-01"",""InfantWightType"": """",""LastName"": ""2"",""Nationality"": ""ID"",""Title"": ""Mr"",""InfantDOB"": """",""InfantTitle"": """",""InfantFirstName"": """",""InfantLastName"": """",""InfantGender"": """",""InfantMiddleName"": """",""Gender"": ""Male"",""FirstName"": ""Dewasa"",""PaxTypes"": ""ADT"",""WightType"": """"},{""InfantNationality"": """",""IsInfant"": false,""DOB"": ""2007-07-07"",""InfantWightType"": """",""LastName"": ""1"",""Nationality"": ""ID"",""Title"": ""Ms"",""InfantDOB"": """",""InfantTitle"": """",""InfantFirstName"": """",""InfantLastName"": """",""InfantGender"": """",""InfantMiddleName"": """",""Gender"": ""Female"",""FirstName"": ""Anak"",""PaxTypes"": ""CHD"",""WightType"": """"}],""fiContactPerson"": {""fiNotificationPreference"": """",""fiContactFirstName"": ""Amir"",""fiCountryCode"": ""ID"",""fiDistributionOption"": """",""fiContactOtherPhone"": ""08152362536"",""fiContactLastName"": ""Mahmud"",""fiContactProvinceState"": ""Jawa Barat"",""fiContactFax"": """",""fiCustomerNumber"": """",""fiCultureCode"": """",""fiContactEmailAddress"": ""amir@gmail.com"",""fiContactAddressLine2"": """",""fiContactAddressLine1"": ""Jl.Darmaga"",""fiContactTitle"": ""Mr"",""fiContactAddressLine3"": """",""fiSourceOrganization"": """",""fiContactCity"": ""Bogor"",""fiContactWorkPhone"": """",""fiContactHomePhone"": """",""fiContactPostalCode"": ""16680"",""fiContactCompanyName"": """"},""fiApplicationId"": ""000"",""fiProductCode"": ""PRD00037"",""fiSignature"": ""J1uXYGQY7zM=|lSU\/0\/5sOg4gQf5nsZdiyav2Bb7Ht5gCUQCUeEM3odxYAlKzR9hrQMR2uE5hvvbwsQrNUKF2TifDKqR7ru\/69ZlmWLwalOpnux\/ypqk9HbJtccTtuFnd\/cM\/FASlz8bosueV+45+2vk="",""fiPhone"": ""08112250588""}";

        static private bool checkMandatoryFields(string[] mandatoryFields, JsonLibs.MyJsonLib aJsonLib)
        {
            foreach (string aField in mandatoryFields)
            {
                if (!aJsonLib.ContainsKey(aField)) return false;
            }
            return true;
        }

        static private void cekJson()
        {
            JsonLibs.MyJsonLib tes = new JsonLibs.MyJsonLib();

            tes.JSONParse(alljson);

            JsonLibs.MyJsonLib ContactPerson = (JsonLibs.MyJsonLib)tes["fiContactPerson"];

            if (checkMandatoryFields(contactFields, ContactPerson))
            {
                Console.WriteLine("aya lengkap");
            }
            else
            {
                Console.WriteLine("beda");
            }

            Console.ReadLine();
            return;
        }
    }
}
