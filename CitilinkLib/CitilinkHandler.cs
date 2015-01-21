using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSXML2;
using CitilinkLib.CitilinkBookingManager;
using CitilinkLib.CitilinkSessionManager;
using CitilinkLib.CitilinkAccountManager;
using LOG_Handler;
using StaticCommonLibrary;

namespace CitilinkLib
{
    public class CitilinkHandler : IDisposable
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
        ~CitilinkHandler()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            if (isSessionOpened) CloseSession();
        }

        private int contractVersion = 0; // pix heula
        private string agentName = "api_dam";
        private string agentPass = "@pi_Dam0314";      // yang lama "@pi_Dam1213";
        private string agentDomain = "EXT";
        public string agentSignature = "";
        //public string agentSignature = "RQuVsUEKxto=|7pnLDwp+vFn1FrAo6XQwTFGoVn1PjzfMkBRQlNCEpmCmNgeGyQywfjQLvripWdcQy4j7wvWKHXAFifnzJ+OlcHO3Y94TDx7JbqNOPqzGRqtNZEHi/9vARZuPKk+TM0pS4OvjPf30ARk=";
        //public string agentSignature = "vXRVseUJp4I=|GlEOZOicOg5+FSHG7SJ0mmInkxPZWAKFn3Xrihj/e5CheTtqUvQiuFu0n3Cu7UHYUJTtmSNTEeW+Z+6YdDKfi9l4wf4zuQ3vY9XsxBzgCkXJfBZCnvkhdYJb1fMbMOcWo47ZhnCTg5U=";

        public string lastErrorMessage = "";
        public bool isLoggedIn = false;
        bool isSessionOpened = false;

        // 1. Logon
        private SessionManagerClient clientManager;
        //private IBookingManager bookingAPI;

        // 2. init GetAvailability        
        private GetAvailabilityRequest requestAvailability;
        private GetAvailabilityResponse responseAvailability;

        // 3. init GetItineraryPrice        
		//PriceItineraryRequest priceItinRequest = new PriceItineraryRequest();

        // 4. init Sell reuqest
        //private SellRequest sellReq;
        //private SellResponse sellResp;

        // 5. sell request infant (bayi)
        // pakai SSR
        // ke skip heula

        // 6. init update pessenger
        //private UpdatePassengersRequest updatePassengersRequest;
        //private UpdatePassengersResponse updatePassengersResponse;


        //public CitilinkHandler()
        //{
        //    // pake konfigurasi default
        //}

        public CitilinkHandler(string AgentName, string AgentPassword,string DomainCode)
        {
            // Load Citilink Login Configuration
            agentName = AgentName;
            agentPass = AgentPassword;
            agentDomain = DomainCode;
        }

        private bool OpenSession()
        {
            LogWriter.show(this, "Opening Session");
            try
            {
                clientManager = new SessionManagerClient();
                //clientManager = new SessionManagerClient(
                //        System.ServiceModel.Channels.Binding bb,
                //        System.ServiceModel.EndpointAddress);
                isSessionOpened = true;
                return true;
            }
            catch (Exception ExError)
            {
                isSessionOpened = false;
                LogWriter.show(this, "Open Session failed");
                lastErrorMessage = ExError.getCompleteErrMsg();
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on Open Session to Citilink \r\n" +
                    "\r\nResult: " + ExError.getCompleteErrMsg());
                return false;
            }
        }

        public void CloseSession()
        {
            LogWriter.show(this, "Closing Session");
            try
            {
                clientManager.Close();
            }
            catch { }
            isSessionOpened = false;
            isLoggedIn = false;
        }

        public bool ChangeAgentPassword(string NewPassword)
        {
            if (!isSessionOpened) OpenSession();

            if (!isSessionOpened) return false;

            LogWriter.show(this, "Changing Agent Password");
            try
            {
                LogonRequestData logonData = new LogonRequestData();
                logonData.AgentName = agentName;
                logonData.DomainCode = agentDomain;
                logonData.Password = agentPass;
                clientManager.ChangePassword(0, logonData, NewPassword);
                agentPass =  NewPassword;
				LogWriter.show(this, "Citilink Password changed");
                return true;
            }
            catch (Exception ExError)
            {
                lastErrorMessage = ExError.getCompleteErrMsg();
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Change agent password to Citilink \r\n" +
                    "\r\nResult: " + ExError.getCompleteErrMsg());
                return false;
            }
        }

        private string Logon()
        {
            LogWriter.show(this, "LogOn...");
            agentSignature = "";
            try
            {
                LogonRequestData logOnReqData = new LogonRequestData();
                logOnReqData.DomainCode = agentDomain;
                logOnReqData.AgentName = agentName;
                logOnReqData.Password = agentPass;
                agentSignature = clientManager.Logon(contractVersion, logOnReqData);
                
                isLoggedIn = true;
                LogWriter.show(this, "LogOn OK");
            }
            catch (Exception e)
            {
                isLoggedIn = false;
                lastErrorMessage = e.getCompleteErrMsg();
                //Console.WriteLine(e.Message);
                LogWriter.show(this, "LogOn failed");
            }
            return agentSignature;
        }

        private void Logoff()
        {
            LogWriter.show(this, "LogOff...");
            isLoggedIn = false;
            try
            {
                clientManager.Logout(contractVersion, agentSignature);
                LogWriter.show(this, "LogOff OK");
            }
            catch (Exception e)
            {
                lastErrorMessage = e.getCompleteErrMsg();
                //Console.WriteLine(e.Message);
                LogWriter.show(this, "LogOff failed");
            }
        }

        public enum InOutbound { Inbound, Outbound, Both }

        public JsonLibs.MyJsonLib GetAvailability(string departureStation, string arrivalStation,
            short paxCountADT, short paxCountCHD,
            DateTime departureBeginDate, DateTime departureEndDate, InOutbound inOutBound,
            ref bool isError)
        {
            JsonLibs.MyJsonLib jh = new JsonLibs.MyJsonLib();
            jh.Clear();
            isError = false;

            if (!isSessionOpened)
            {
                if (!OpenSession())
                {
                    isError = true;
                    jh.Add("fiResponseCode", "78");
                    jh.Add("fiResponseMessage", "Can not open session");
                    this.lastErrorMessage = "Can not open session on get availability";
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink get availability failed \r\nCan not open session");
                    return jh;
                }
            }
            if (!isLoggedIn)
            {
                if (Logon() == "")
                {
                    isError = true;
                    jh.Add("fiResponseCode", "79");
                    jh.Add("fiResponseMessage", "LogOn failed");
                    return jh;
                }
            }

            //if (inOutBound == InOutbound.Both) return null;
            LogWriter.show(this, "Get availability");

            string carrierCode = "QG";
            string currencyCode = "IDR";
            short maximumConnectingFlights = 4; // 0;
            int minimumFarePrice = 0;
            int maximumFarePrice = 0;
            int nightsStay = 0;
            bool includeAllotments = false;

            short paxCount = (short)(paxCountADT + paxCountCHD);

            //string json = "";

            try
            {
                //Create an instance of BookingManagerClient
                IBookingManager bookingAPI = new BookingManagerClient();
                // Create an availability request and populate request data
                requestAvailability = new GetAvailabilityRequest();

                requestAvailability.ContractVersion = contractVersion;
                requestAvailability.Signature = agentSignature;

                requestAvailability.TripAvailabilityRequest = new TripAvailabilityRequest();
                requestAvailability.TripAvailabilityRequest.AvailabilityRequests = new AvailabilityRequest[1];
                AvailabilityRequest availabilityRequest = new AvailabilityRequest();

                availabilityRequest.DepartureStation = departureStation;
                availabilityRequest.ArrivalStation = arrivalStation;

                availabilityRequest.BeginDate = departureBeginDate; // YYYY-MM-DD HH:mi:ss
                availabilityRequest.EndDate = departureEndDate; // YYYY-MM-DD HH:mi:ss

                availabilityRequest.CarrierCode = carrierCode;
                availabilityRequest.FlightType = FlightType.All;
                availabilityRequest.PaxCount = paxCount;
                availabilityRequest.Dow = DOW.Daily;
                availabilityRequest.CurrencyCode = currencyCode;
                availabilityRequest.AvailabilityType = AvailabilityType.Default;
                availabilityRequest.MaximumConnectingFlights = maximumConnectingFlights;
                availabilityRequest.AvailabilityFilter = AvailabilityFilter.Default;
                availabilityRequest.FareClassControl = FareClassControl.LowestFareClass;
                availabilityRequest.MinimumFarePrice = minimumFarePrice;
                availabilityRequest.MaximumFarePrice = maximumFarePrice;
                availabilityRequest.SSRCollectionsMode = SSRCollectionsMode.None;
                //                availabilityRequest.InboundOutbound = InboundOutbound.Outbound;
                if (inOutBound == InOutbound.Inbound)
                {
                    availabilityRequest.InboundOutbound = InboundOutbound.Inbound;
                    // DepartureStations and ArrivalStations used to define market
                    //availabilityRequest.DepartureStations = new string[1];
                    //availabilityRequest.DepartureStations[0] = arrivalStation;
                    //availabilityRequest.ArrivalStations = new string[1];
                    //availabilityRequest.ArrivalStations[0] = departureStation;

                    availabilityRequest.DepartureStations = new string[1];
                    availabilityRequest.DepartureStations[0] = departureStation;
                    availabilityRequest.ArrivalStations = new string[1];
                    availabilityRequest.ArrivalStations[0] = arrivalStation;
                }
                if (inOutBound == InOutbound.Outbound)
                {
                    availabilityRequest.InboundOutbound = InboundOutbound.Outbound;
                    // DepartureStations and ArrivalStations used to define market
                    availabilityRequest.DepartureStations = new string[1];
                    availabilityRequest.DepartureStations[0] = departureStation;
                    availabilityRequest.ArrivalStations = new string[1];
                    availabilityRequest.ArrivalStations[0] = arrivalStation;

                }
                else
                {
                    availabilityRequest.InboundOutbound = InboundOutbound.Both;
                    // DepartureStations and ArrivalStations used to define market
                    availabilityRequest.DepartureStations = new string[2];
                    availabilityRequest.DepartureStations[0] = departureStation;
                    availabilityRequest.DepartureStations[1] = arrivalStation;
                    availabilityRequest.ArrivalStations = new string[2];
                    availabilityRequest.ArrivalStations[0] = arrivalStation;
                    availabilityRequest.ArrivalStations[1] = departureStation;

                }
                availabilityRequest.NightsStay = nightsStay;
                availabilityRequest.IncludeAllotments = includeAllotments;

                PaxPriceType[] myPriceTypes = new PaxPriceType[paxCount];
                short jmlAdt = paxCountADT;
                for (int i = 0; i < paxCount; i++)
                {
                    myPriceTypes[i] = new PaxPriceType();
                    if (jmlAdt > 0)
                    {
                        myPriceTypes[i].PaxType = "ADT";
                        jmlAdt--;
                    }
                    else myPriceTypes[i].PaxType = "CHD";
                    myPriceTypes[i].PaxDiscountCode = String.Empty;
                }
                availabilityRequest.PaxPriceTypes = myPriceTypes;

                requestAvailability.TripAvailabilityRequest.AvailabilityRequests[0] = availabilityRequest;
                responseAvailability = bookingAPI.GetAvailability(requestAvailability);

                if ((responseAvailability.GetTripAvailabilityResponse.Schedules.Length > 0) &&
                    (responseAvailability.GetTripAvailabilityResponse.Schedules[0].Length > 0))
                {
                    jh.Add("fiResponseCode", "00");
                    jh.Add("fiResponseMessage", "Success");

                    JourneyDateMarket[][] jdm = responseAvailability.GetTripAvailabilityResponse.Schedules;

                    for (int i = 0; i < jdm.Length; i++)
                    {
                        for (int j = 0; j < jdm[i].Length; j++)
                        {
                            jh.AddArrayItem("Schedules", getSchedule(jdm, i, j));
                        }
                    }

                }
                else
                {
                    isError = true;
                    lastErrorMessage = "No schedules found from Citilink";
                    jh.Add("fiResponseCode", "77");
                    jh.Add("fiResponseMessage", "No Data");
                }
                //return jh;
            }
            catch (Exception ex)
            {
                isError = true;
                this.lastErrorMessage = ex.Message; // = "Bad Data.\r\n" jika salah signature
                if (lastErrorMessage == "") lastErrorMessage = "Failed but no error message from Citilink";
                //Console.WriteLine(ex.Message);
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink get avalability error" + ex.getCompleteErrMsg());
                jh.Add("fiResponseCode", "99");
                jh.Add("fiResponseMessage", "Get avalability failed");
                //json = jh.JSONConstruct();
                //jh.Dispose();
            }

            return jh;
        }

        private JsonLibs.MyJsonLib getSchedule(JourneyDateMarket[][] jdm, int idi, int idj)
        {
            JsonLibs.MyJsonLib jl = new JsonLibs.MyJsonLib();

            jl.Add("DepartureDate", jdm[idi][idj].DepartureDate.ToString("yyyy-MM-dd"));
            //jl.Add("DepartureStation", jdm[idi][idj].DepartureStation);
            //jl.Add("ArrivalStation", jdm[idi][idj].ArrivalStation);

            jl.Add("IncludesTaxesAndFees", jdm[idi][idj].IncludesTaxesAndFees);

            int jLen = jdm[idi][idj].Journeys.Length;
            //Console.WriteLine("Jumlah Journey = " + jLen);

            JsonLibs.MyJsonArray ja = new JsonLibs.MyJsonArray();
            ja.Name = "Journeys";

            foreach (Journey jor in jdm[idi][idj].Journeys)
            {
                JsonLibs.MyJsonLib jlo = new JsonLibs.MyJsonLib();
                jlo.Add("JourneySellKey", jor.JourneySellKey);
                jlo.Add("State", jor.State.ToString());

                foreach (Segment seg in jor.Segments)
                {
                    jlo.AddArrayItem("Segments", getSegment(seg));
                }

                ja.Add(jlo);
            }
            jl.Add("Journeys", ja);

            return jl;
        }

        private JsonLibs.MyJsonLib getSegment(Segment seg)
        {
            JsonLibs.MyJsonLib jl = new JsonLibs.MyJsonLib();

            jl.Add("STD", seg.STD.ToString("yyyy-MM-dd HH:mm:ss"));
            jl.Add("STA", seg.STA.ToString("yyyy-MM-dd HH:mm:ss"));

            //jl.Add("SalesDate", seg.SalesDate.ToString("yyyy-MM-dd HH:mm:ss"));
            jl.Add("ArrivalStation", seg.ArrivalStation);
            jl.Add("DepartureStation", seg.DepartureStation);
            jl.Add("ActionStatusCode", seg.ActionStatusCode);
            //jl.Add("CabinOfService", seg.CabinOfService);
            //jl.Add("FlightDesignator", seg.FlightDesignator);

            JsonLibs.MyJsonLib jlfl = new JsonLibs.MyJsonLib();
            jlfl.Add("CarrierCode", seg.FlightDesignator.CarrierCode);
            jlfl.Add("FlightNumber", seg.FlightDesignator.FlightNumber);
            jlfl.Add("OpSuffix", seg.FlightDesignator.OpSuffix);

            jl.Add("FlightDesignator", jlfl);

            jl.Add("SegmentSellKey", seg.SegmentSellKey);
            //jl.Add("SegmentType", seg.SegmentType);
            jl.Add("State", seg.State.ToString());

            JsonLibs.MyJsonArray ja = new JsonLibs.MyJsonArray();
            ja.Name = "Fares";

            foreach (Fare fare in seg.Fares)
            {
                JsonLibs.MyJsonLib jlf = new JsonLibs.MyJsonLib();
                jlf.Add("FareSellKey", fare.FareSellKey);
                jlf.Add("ProductClass", fare.ProductClass);
                //jlf.Add("TravelClassCode", fare.TravelClassCode);
                jlf.Add("State", fare.State.ToString());
                jlf.Add("InboundOutbound", fare.InboundOutbound.ToString());
                jlf.Add("CarrierCode", fare.CarrierCode);
                jlf.Add("RuleNumber", fare.RuleNumber);
                jlf.Add("ClassOfService", fare.ClassOfService);

                JsonLibs.MyJsonArray paxFares = new JsonLibs.MyJsonArray();
                foreach (PaxFare paxfr in fare.PaxFares)
                {
                    paxFares.Add(getPaxFare(paxfr));
                    //jlf.AddArrayItem("PaxFares", getPaxFare(paxfr));
                }
                jlf.Add("PaxFares", paxFares);

                ja.Add(jlf);
            }
            jl.Add("Fares", ja);

            //JsonLibs.MyJsonArray legsArr = new JsonLibs.MyJsonArray();
            //for (int i = 0; i < seg.Legs.Length; i++)
            //{
            //    JsonLibs.MyJsonLib aLegJs = new JsonLibs.MyJsonLib();
            //    aLegJs.Add("STD", seg.Legs[i].STD.ToString("yyyy-MM-dd HH:mm:ss"));
            //    aLegJs.Add("STA", seg.Legs[i].STA.ToString("yyyy-MM-dd HH:mm:ss"));
            //    aLegJs.Add("CarrierCode", seg.Legs[i].FlightDesignator.CarrierCode);
            //    aLegJs.Add("FlightNumber", seg.Legs[i].FlightDesignator.FlightNumber);
            //    aLegJs.Add("DepartureStation", seg.Legs[i].DepartureStation);
            //    aLegJs.Add("ArrivalStation", seg.Legs[i].ArrivalStation);
            //    legsArr.Add(aLegJs);
            //}
            //jl.Add("Legs", legsArr);

            return jl;
        }

        private JsonLibs.MyJsonLib getPaxFare(PaxFare paxfare)
        {
            JsonLibs.MyJsonLib jl = new JsonLibs.MyJsonLib();

            jl.Add("PaxType", paxfare.PaxType);
            jl.Add("State", paxfare.State.ToString());
            jl.Add("PaxDiscountCode", paxfare.PaxDiscountCode);
            jl.Add("FareDiscountCode", paxfare.FareDiscountCode);

            foreach (BookingServiceCharge svChg in paxfare.ServiceCharges)
            {
                jl.AddArrayItem("ServiceCharges", getServiceCharge(svChg));
            }
            return jl;
        }

        private JsonLibs.MyJsonLib getServiceCharge(BookingServiceCharge svChg)
        {
            JsonLibs.MyJsonLib jl = new JsonLibs.MyJsonLib();

            jl.Add("Amount", svChg.Amount);
            jl.Add("ChargeType", svChg.ChargeType.ToString());
            jl.Add("CollectType", svChg.CollectType.ToString());
            jl.Add("State", svChg.State.ToString());
            jl.Add("CurrencyCode", svChg.CurrencyCode);
            jl.Add("ForeignAmount", svChg.ForeignAmount);
            jl.Add("ForeignCurrencyCode", svChg.ForeignCurrencyCode);
            jl.Add("ChargeCode", svChg.ChargeCode);

            return jl;
        }

        public JsonLibs.MyJsonLib getAvailabilityWrap(string departureStation, string arrivalStation,
            short paxCountADT, short paxCountCHD,
            DateTime departureBeginDate, DateTime departureEndDate,
            DateTime arrivalBeginDate, DateTime arrivalEndDate,
            InOutbound inOutBound,
            string clientSignature,
            ref bool isError)
        {
            try
            {
                isError = false;
                JsonLibs.MyJsonLib availJson1 = null;
                JsonLibs.MyJsonLib availJson2;

                CitilinkHandler ch = this;
                ch.agentSignature = clientSignature;
                if (clientSignature != "") ch.isLoggedIn = true;

                for (int iCoba = 0; iCoba < 2; iCoba++)
                {
                    availJson1 = ch.GetAvailability(departureStation, arrivalStation, paxCountADT, paxCountCHD,
                        departureBeginDate, departureEndDate,
                        CitilinkHandler.InOutbound.Outbound, ref isError);

                    if (isError)
                    {
                        if ((ch.lastErrorMessage == "Bad Data.\r\n") && (iCoba == 0))
                        {
                            // ulang get avalability 1 kali lagi diawali dgn log On
                            ch.isLoggedIn = false;
                            continue;
                        }
                        else if ((ch.lastErrorMessage.StartsWith("Session token authentication failure")) && (iCoba == 0))
                        {
                            // ulang get avalability 1 kali lagi diawali dgn log On
                            ch.isLoggedIn = false;
                            continue;
                        }

                        LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on get availability:\r\n" + ch.lastErrorMessage);
                        ch.CloseSession();
                        //ch.Dispose();
                        if (availJson1 != null) availJson1.Dispose();
                        return null;
                    }
                    else break;
                }
                //ch.CloseSession();    // hanya untuk nyoba
                //ch.isLoggedIn = true;

                if (inOutBound != InOutbound.Outbound)
                {
                    availJson2 = ch.GetAvailability(arrivalStation, departureStation, paxCountADT, paxCountCHD,
                        arrivalBeginDate, arrivalEndDate,
                        CitilinkHandler.InOutbound.Outbound, ref isError);

                    if (isError)
                    {
                        LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on get availability:\r\n" + ch.lastErrorMessage);
                        ch.CloseSession();
                        //ch.Dispose();
                        if (availJson1 != null) availJson1.Dispose();
                        if (availJson2 != null) availJson2.Dispose();
                        return null;
                    }

                    // gabungkan json nya
                    availJson1.AddArrayItem("Schedules", ((JsonLibs.MyJsonArray)(availJson2["Schedules"]))[0]);
                }

                availJson1.Add("fiSignature", ch.agentSignature);

                //string logdir = "C:\\citylinklogs\\";
                //string ftime = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
                //string logfile = "getavailability_" + ftime + ".txt";
                //writeToFileText(availJson1.JSONConstruct(), logdir, logfile);
                //Process.Start("notepad.exe", logdir + "\\" + logfile);

                ch.CloseSession();
                //ch.Dispose();
                isError = false;
                return availJson1;
            }
            catch (Exception ex)
            {
                //if (ch !=null) ch.Dispose();
                //if (availJson1 != null) availJson1.Dispose();
                //if (availJson2 != null) availJson2.Dispose();
                isError = true;
                this.lastErrorMessage = ex.Message; // = "Bad Data.\r\n" jika salah signature
                //Console.WriteLine(ex.Message);
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink sell request error" + ex.getCompleteErrMsg());

                JsonLibs.MyJsonLib availJson = new JsonLibs.MyJsonLib();
                availJson.Add("fiResponseCode", "99");
                availJson.Add("fiResponseMessage", ex.Message);
                return availJson;
            }
        }

        public struct SellKey
        {
            public string JourneySellKey;
            public string FareSellKey;
        }

        public JsonLibs.MyJsonLib sellRequest(CitilinkLib.CitilinkHandler.SellKey[] SellKeys,
            short paxCountADT, short paxCountCHD, string currencyCode, string clientSignature,
            bool keepOpenSession, ref bool isError, bool isReqAll)
        {
            JsonLibs.MyJsonLib sellReqJson = new JsonLibs.MyJsonLib();
            agentSignature = clientSignature;
            if (clientSignature != "") isLoggedIn = true;

            if (!isSessionOpened)
            {
                if (!OpenSession())
                {
                    isError = true;
                    sellReqJson.Add("fiResponseCode", "78");
                    sellReqJson.Add("fiResponseMessage", "Can not open session");
                    this.lastErrorMessage = "Can not open session on sell request";
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink sellrequest failed \r\nCan not open session");
                    return sellReqJson;
                }
            }

            try
            {

                // indicate that the we are selling a journey

                PaxPriceType[] myPriceTypes = new PaxPriceType[paxCountADT + paxCountCHD];
                short jmlAdt = paxCountADT;
                for (int i = 0; i < (paxCountADT + paxCountCHD); i++)
                {
                    myPriceTypes[i] = new PaxPriceType();
                    if (jmlAdt > 0)
                    {
                        myPriceTypes[i].PaxType = "ADT";
                        jmlAdt--;
                    }
                    else myPriceTypes[i].PaxType = "CHD";
                    myPriceTypes[i].PaxDiscountCode = String.Empty;
                }

                PointOfSale sourcePOS = new PointOfSale();
				sourcePOS.State = CitilinkBookingManager.MessageState.New; // fix heula new passenger
                sourcePOS.AgentCode = agentName;
                //sourcePOS.OrganizationCode = "0003000179";
                sourcePOS.DomainCode = "EXT";

                LogWriter.showDEBUG(this, "============= SELL REQUEST =================");
                SellKeyList[] sellKeyList = new SellKeyList[SellKeys.Length];
                for (int i = 0; i < SellKeys.Length; i++)
                {
                    SellKeyList skl = new SellKeyList();
                    skl.JourneySellKey = SellKeys[i].JourneySellKey; //"QG~ 852~ ~~CGK~25/12/2013 16:25~DPS~25/12/2013 19:15~";
                    skl.FareSellKey = SellKeys[i].FareSellKey; // "0~N~~N~RGFR~~1~X";

                    LogWriter.showDEBUG(this, "JourneySellKey[" + i.ToString() + "] = " + SellKeys[i].JourneySellKey + "\r\n" +
                    "FareSellKey[" + i.ToString() + "]    = " + SellKeys[i].FareSellKey);

                    sellKeyList[i] = skl;
                }

                SellJourneyByKeyRequestData sellJor = new SellJourneyByKeyRequestData();
                sellJor.SourcePOS = sourcePOS;
                sellJor.JourneySellKeys = sellKeyList;
                sellJor.ActionStatusCode = "NN";
                sellJor.CurrencyCode = currencyCode;
                sellJor.PaxCount = (short)(paxCountADT + paxCountCHD);
                sellJor.LoyaltyFilter = LoyaltyFilter.MonetaryOnly;
                sellJor.IsAllotmentMarketFare = false;
                sellJor.PaxPriceType = myPriceTypes;

                SellJourneyByKeyRequest sellJourneyByKeyRequest = new SellJourneyByKeyRequest();
                sellJourneyByKeyRequest.SellJourneyByKeyRequestData = sellJor;

                SellRequestData sellRequestData = new SellRequestData();
                sellRequestData.SellBy = SellBy.JourneyBySellKey;
                sellRequestData.SellJourneyByKeyRequest = sellJourneyByKeyRequest;

                SellRequest sellReq = new SellRequest();
                sellReq.ContractVersion = 0; // pix heula
                sellReq.Signature = agentSignature;
                sellReq.SellRequestData = sellRequestData;

                //Create an instance of BookingManagerClient
                IBookingManager bookingAPI = new BookingManagerClient();
                SellResponse sellResp = bookingAPI.Sell(sellReq);

                JsonLibs.MyJsonLib pnrAmountJson = new JsonLibs.MyJsonLib();
                Success sellSuccess = sellResp.BookingUpdateResponseData.Success;
                BookingSum PnrAmount = sellSuccess.PNRAmount;
                pnrAmountJson.Add("BalanceDue", PnrAmount.BalanceDue);
                pnrAmountJson.Add("AuthorizedBalanceDue", PnrAmount.AuthorizedBalanceDue);
                pnrAmountJson.Add("PassiveSegmentCount", (int)PnrAmount.PassiveSegmentCount);
                pnrAmountJson.Add("PointsBalanceDue", PnrAmount.PointsBalanceDue);
                pnrAmountJson.Add("SegmentCount", (int)PnrAmount.SegmentCount);
                pnrAmountJson.Add("TotalCost", PnrAmount.TotalCost);
                pnrAmountJson.Add("TotalPointCost", PnrAmount.TotalPointCost);

                JsonLibs.MyJsonLib BookingUpdSuccessResp = new JsonLibs.MyJsonLib();
                BookingUpdSuccessResp.Add("PNRAmount", pnrAmountJson);
                //BookingUpdSuccessResp.Add("RecordLocator", sellSuccess.RecordLocator);

                sellReqJson.Add("Success", BookingUpdSuccessResp);
                if (sellResp.BookingUpdateResponseData.Warning != null)
                    sellReqJson.Add("Warning", sellResp.BookingUpdateResponseData.Warning.WarningText);
                else
                    sellReqJson.Add("Warning", null);
                if (sellResp.BookingUpdateResponseData.Error != null)
                    sellReqJson.Add("Error", sellResp.BookingUpdateResponseData.Error.ErrorText);
                else
                    sellReqJson.Add("Error", null);

                //JsonLibs.MyJsonArray otherSvcJsArray = new JsonLibs.MyJsonArray();
                //if (sellResp.BookingUpdateResponseData.OtherServiceInformations != null)
                //{
                //    foreach (OtherServiceInformation svcInfo in sellResp.BookingUpdateResponseData.OtherServiceInformations)
                //    {
                //        JsonLibs.MyJsonLib svcInfoJson = new JsonLibs.MyJsonLib();
                //        svcInfoJson.Add("OsiSeverity", svcInfo.OsiSeverity.ToString());
                //        if (svcInfo.OSITypeCode != null)
                //            svcInfoJson.Add("OSITypeCode", svcInfo.OSITypeCode);
                //        svcInfoJson.Add("OsiSeverity", svcInfo.OsiSeverity.ToString());
                //        if (svcInfo.Text != null)
                //            svcInfoJson.Add("Text", svcInfo.Text);
                //        if (svcInfo.SubType != null)
                //            svcInfoJson.Add("SubType", svcInfo.SubType);

                //        otherSvcJsArray.Add(svcInfoJson);
                //    }
                //}
                //sellReqJson.Add("OtherServiceInformations", otherSvcJsArray);

                if (!isReqAll)
                {
                    sellReqJson.Add("fiResponseCode", "00");
                    sellReqJson.Add("fiResponseMessage", "Success");
                    sellReqJson.Add("fiSignature", agentSignature);
                }
                isError = false;
            }
            catch (Exception ex)
            {
                isError = true;
                this.lastErrorMessage = ex.Message; // = "Bad Data.\r\n" jika salah signature
                //Console.WriteLine(ex.Message);
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink sell request failed " + ex.getCompleteErrMsg());
                sellReqJson.Add("fiResponseCode", "99");
                sellReqJson.Add("fiResponseMessage", ex.Message);
            }
            if (!keepOpenSession)
            {
                try
                {
                    isSessionOpened = false;
                    CloseSession();
                }
                catch { }
            }
            return sellReqJson;
        }

        public struct InfSellReq
        {
            public string CarrierCode;
            public string FlightNumber;
            public DateTime STD;
            public string DepartureStation;
            public string ArrivalStation;
        }
        public JsonLibs.MyJsonLib sellRequestINF(int InfCount, string currencyCode,
            InfSellReq[] infSellReqData,
            string clientSignature, bool keepOpenSession, ref bool isError, bool isReqAll)
        {
            JsonLibs.MyJsonLib sellReqJson = new JsonLibs.MyJsonLib();
            agentSignature = clientSignature;
            if (clientSignature != "") isLoggedIn = true;

            if (!isSessionOpened)
            {
                if (!OpenSession())
                {
                    isError = true;
                    sellReqJson.Add("fiResponseCode", "78");
                    sellReqJson.Add("fiResponseMessage", "Can not open session");
                    this.lastErrorMessage = "Can not open session on sell request INF";
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink sellrequest INF failed \r\nCan not open session");
                    return sellReqJson;
                }
            }

            try
            {

                // indicate that the we are selling a journey
                SegmentSSRRequest[] segmentSSRReqs = new SegmentSSRRequest[infSellReqData.Length];
                for (int j = 0; j < infSellReqData.Length; j++)
                {
                    SegmentSSRRequest SegmentSSRreq = new SegmentSSRRequest();
                    InfSellReq infSellReq = infSellReqData[j];
                    FlightDesignator flightDesignator = new FlightDesignator();
                    flightDesignator.CarrierCode = infSellReq.CarrierCode;
                    flightDesignator.FlightNumber = infSellReq.FlightNumber;
                    flightDesignator.OpSuffix = String.Empty;

                    SegmentSSRreq.FlightDesignator = flightDesignator;
                    SegmentSSRreq.STD = infSellReq.STD;
                    SegmentSSRreq.DepartureStation = infSellReq.DepartureStation;
                    SegmentSSRreq.ArrivalStation = infSellReq.ArrivalStation;
                    PaxSSR[] paxSSRs = new PaxSSR[InfCount];
                    for (int i = 0; i < InfCount; i++)
                    {
                        PaxSSR aPaxSSR = new PaxSSR();
						aPaxSSR.State = CitilinkBookingManager.MessageState.New; // fix heula new passenger
                        aPaxSSR.ActionStatusCode = "NN";
                        aPaxSSR.ArrivalStation = infSellReq.ArrivalStation;
                        aPaxSSR.DepartureStation = infSellReq.DepartureStation;
                        aPaxSSR.PassengerNumber = (short)i;
                        aPaxSSR.SSRCode = "INF";
                        aPaxSSR.SSRNumber = (short)i;
                        aPaxSSR.SSRDetail = String.Empty;
                        aPaxSSR.FeeCode = "INF";
                        aPaxSSR.Note = String.Empty;
                        aPaxSSR.SSRValue = (short)i;

                        paxSSRs[i] = aPaxSSR;
                    }
                    SegmentSSRreq.PaxSSRs = paxSSRs;
                    segmentSSRReqs[j] = SegmentSSRreq;
                }

                SSRRequest ssrReq = new SSRRequest();
                ssrReq.CurrencyCode = currencyCode;
                ssrReq.CancelFirstSSR = false;
                ssrReq.SSRFeeForceWaiveOnSell = false;
                ssrReq.SegmentSSRRequests = segmentSSRReqs;

                SellSSR sellSSR = new SellSSR();
                sellSSR.SSRRequest = ssrReq;

                SellRequestData sellReqData = new SellRequestData();
                sellReqData.SellFee = null;
                sellReqData.SellBy = SellBy.SSR;
                sellReqData.SellJourneyByKeyRequest = null;
                sellReqData.SellJourneyRequest = null;
                sellReqData.SellSSR = sellSSR;

                SellRequest sellReq = new SellRequest();
                sellReq.ContractVersion = 0; // pix heula
                sellReq.Signature = agentSignature;
                sellReq.SellRequestData = sellReqData;

                //Create an instance of BookingManagerClient
                IBookingManager bookingAPI = new BookingManagerClient();
                SellResponse sellResp = bookingAPI.Sell(sellReq);

                JsonLibs.MyJsonLib BookingUpdSuccessResp = new JsonLibs.MyJsonLib();
                JsonLibs.MyJsonLib pnrAmountJson = new JsonLibs.MyJsonLib();
                Success sellSuccess = sellResp.BookingUpdateResponseData.Success;
                BookingSum PnrAmount = sellSuccess.PNRAmount;
                pnrAmountJson.Add("BalanceDue", PnrAmount.BalanceDue);
                pnrAmountJson.Add("AuthorizedBalanceDue", PnrAmount.AuthorizedBalanceDue);
                pnrAmountJson.Add("PassiveSegmentCount", (int)PnrAmount.PassiveSegmentCount);
                pnrAmountJson.Add("PointsBalanceDue", PnrAmount.PointsBalanceDue);
                pnrAmountJson.Add("SegmentCount", (int)PnrAmount.SegmentCount);
                pnrAmountJson.Add("TotalCost", PnrAmount.TotalCost);
                pnrAmountJson.Add("TotalPointCost", PnrAmount.TotalPointCost);

                BookingUpdSuccessResp.Add("PNRAmount", pnrAmountJson);
                //BookingUpdSuccessResp.Add("RecordLocator", sellSuccess.RecordLocator);

                sellReqJson.Add("Success", BookingUpdSuccessResp);
                if (sellResp.BookingUpdateResponseData.Warning != null)
                    sellReqJson.Add("Warning", sellResp.BookingUpdateResponseData.Warning.WarningText);
                else
                    sellReqJson.Add("Warning", null);
                if (sellResp.BookingUpdateResponseData.Error != null)
                    sellReqJson.Add("Error", sellResp.BookingUpdateResponseData.Error.ErrorText);
                else
                    sellReqJson.Add("Error", null);

                if (!isReqAll)
                {
                    sellReqJson.Add("fiResponseCode", "00");
                    sellReqJson.Add("fiResponseMessage", "Success");
                    sellReqJson.Add("fiSignature", agentSignature);
                }
                isError = false;
            }
            catch (Exception ex)
            {
                isError = true;
                this.lastErrorMessage = ex.Message; // = "Bad Data.\r\n" jika salah signature
                //Console.WriteLine(ex.Message);
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink sell request INF failed " + ex.getCompleteErrMsg());
                sellReqJson.Add("fiResponseCode", "99");
                sellReqJson.Add("fiResponseMessage", ex.Message);
            }
            if (!keepOpenSession)
            {
                try
                {
                    isSessionOpened = false;
                    CloseSession();
                }
                catch { }
            }
            return sellReqJson;
        }

        public JsonLibs.MyJsonLib sellRequestINF2(int infantCount, string currencyCode,
            string[] carrierCodes, string[] flightNumbers, DateTime[] STDs,
            string departureStation, string arrivalStation,
            string clientSignature, bool keepOpenSession, ref bool isError, bool isReqAll)
        {
            JsonLibs.MyJsonLib sellReqJson = new JsonLibs.MyJsonLib();
            agentSignature = clientSignature;
            if (clientSignature != "") isLoggedIn = true;

            if (!isSessionOpened)
            {
                if (!OpenSession())
                {
                    isError = true;
                    sellReqJson.Add("fiResponseCode", "78");
                    sellReqJson.Add("fiResponseMessage", "Can not open session");
                    this.lastErrorMessage = "Can not open session on sell request INF";
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink sellrequest INF failed \r\nCan not open session");
                    return sellReqJson;
                }
            }

            try
            {

                // indicate that the we are selling a journey
                SegmentSSRRequest[] segmentSSRReqs = new SegmentSSRRequest[carrierCodes.Length];
                for (int j = 0; j < carrierCodes.Length; j++)
                {
                    SegmentSSRRequest SegmentSSRreq = new SegmentSSRRequest();

                    FlightDesignator flightDesignator = new FlightDesignator();
                    flightDesignator.CarrierCode = carrierCodes[j];
                    flightDesignator.FlightNumber = flightNumbers[j];
                    flightDesignator.OpSuffix = String.Empty;

                    SegmentSSRreq.FlightDesignator = flightDesignator;
                    SegmentSSRreq.STD = STDs[j];
                    if (j == 0)
                    {
                        SegmentSSRreq.DepartureStation = departureStation;
                        SegmentSSRreq.ArrivalStation = arrivalStation;
                    }
                    else
                    {
                        SegmentSSRreq.DepartureStation = arrivalStation;
                        SegmentSSRreq.ArrivalStation = departureStation;
                    }
                    PaxSSR[] paxSSRs = new PaxSSR[infantCount];
                    for (int i = 0; i < infantCount; i++)
                    {
                        PaxSSR aPaxSSR = new PaxSSR();
						aPaxSSR.State = CitilinkBookingManager.MessageState.New; // fix heula new passenger
                        aPaxSSR.ActionStatusCode = "NN";
                        if (j == 0)
                        {
                            aPaxSSR.ArrivalStation = arrivalStation;
                            aPaxSSR.DepartureStation = departureStation;
                        }
                        else
                        {
                            aPaxSSR.ArrivalStation = departureStation;
                            aPaxSSR.DepartureStation = arrivalStation;
                        }
                        aPaxSSR.PassengerNumber = (short)i;
                        aPaxSSR.SSRCode = "INF";
                        aPaxSSR.SSRNumber = (short)i;
                        aPaxSSR.SSRDetail = String.Empty;
                        aPaxSSR.FeeCode = String.Empty;
                        aPaxSSR.Note = String.Empty;
                        aPaxSSR.SSRValue = (short)i;

                        paxSSRs[i] = aPaxSSR;
                    }
                    SegmentSSRreq.PaxSSRs = paxSSRs;
                    segmentSSRReqs[j] = SegmentSSRreq;
                }
                SSRRequest ssrReq = new SSRRequest();
                ssrReq.CurrencyCode = currencyCode;
                ssrReq.CancelFirstSSR = false;
                ssrReq.SSRFeeForceWaiveOnSell = false;
                ssrReq.SegmentSSRRequests = segmentSSRReqs;

                SellSSR sellSSR = new SellSSR();
                sellSSR.SSRRequest = ssrReq;

                SellRequestData sellReqData = new SellRequestData();
                sellReqData.SellFee = null;
                sellReqData.SellBy = SellBy.SSR;
                sellReqData.SellJourneyByKeyRequest = null;
                sellReqData.SellJourneyRequest = null;
                sellReqData.SellSSR = sellSSR;

                SellRequest sellReq = new SellRequest();
                sellReq.ContractVersion = 0; // pix heula
                sellReq.Signature = agentSignature;
                sellReq.SellRequestData = sellReqData;

                //Create an instance of BookingManagerClient
                IBookingManager bookingAPI = new BookingManagerClient();
                SellResponse sellResp = bookingAPI.Sell(sellReq);


                JsonLibs.MyJsonLib BookingUpdSuccessResp = new JsonLibs.MyJsonLib();
                JsonLibs.MyJsonLib pnrAmountJson = new JsonLibs.MyJsonLib();
                Success sellSuccess = sellResp.BookingUpdateResponseData.Success;
                BookingSum PnrAmount = sellSuccess.PNRAmount;
                pnrAmountJson.Add("BalanceDue", PnrAmount.BalanceDue);
                pnrAmountJson.Add("AuthorizedBalanceDue", PnrAmount.AuthorizedBalanceDue);
                pnrAmountJson.Add("PassiveSegmentCount", (int)PnrAmount.PassiveSegmentCount);
                pnrAmountJson.Add("PointsBalanceDue", PnrAmount.PointsBalanceDue);
                pnrAmountJson.Add("SegmentCount", (int)PnrAmount.SegmentCount);
                pnrAmountJson.Add("TotalCost", PnrAmount.TotalCost);
                pnrAmountJson.Add("TotalPointCost", PnrAmount.TotalPointCost);

                BookingUpdSuccessResp.Add("PNRAmount", pnrAmountJson);
                //BookingUpdSuccessResp.Add("RecordLocator", sellSuccess.RecordLocator);

                sellReqJson.Add("Success", BookingUpdSuccessResp);
                if (sellResp.BookingUpdateResponseData.Warning != null)
                    sellReqJson.Add("Warning", sellResp.BookingUpdateResponseData.Warning.WarningText);
                else
                    sellReqJson.Add("Warning", null);
                if (sellResp.BookingUpdateResponseData.Error != null)
                    sellReqJson.Add("Error", sellResp.BookingUpdateResponseData.Error.ErrorText);
                else
                    sellReqJson.Add("Error", null);

                //JsonLibs.MyJsonArray otherSvcJsArray = new JsonLibs.MyJsonArray();
                //if (sellResp.BookingUpdateResponseData.OtherServiceInformations != null)
                //{
                //    foreach (OtherServiceInformation svcInfo in sellResp.BookingUpdateResponseData.OtherServiceInformations)
                //    {
                //        JsonLibs.MyJsonLib svcInfoJson = new JsonLibs.MyJsonLib();
                //        svcInfoJson.Add("OsiSeverity", svcInfo.OsiSeverity.ToString());
                //        if (svcInfo.OSITypeCode != null)
                //            svcInfoJson.Add("OSITypeCode", svcInfo.OSITypeCode);
                //        svcInfoJson.Add("OsiSeverity", svcInfo.OsiSeverity.ToString());
                //        if (svcInfo.Text != null)
                //            svcInfoJson.Add("Text", svcInfo.Text);
                //        if (svcInfo.SubType != null)
                //            svcInfoJson.Add("SubType", svcInfo.SubType);

                //        otherSvcJsArray.Add(svcInfoJson);
                //    }
                //}
                //sellReqJson.Add("OtherServiceInformations", otherSvcJsArray);

                if (!isReqAll)
                {
                    sellReqJson.Add("fiResponseCode", "00");
                    sellReqJson.Add("fiResponseMessage", "Success");
                    sellReqJson.Add("fiSignature", agentSignature);
                }
                isError = false;
            }
            catch (Exception ex)
            {
                isError = true;
                this.lastErrorMessage = ex.Message; // = "Bad Data.\r\n" jika salah signature
                //Console.WriteLine(ex.Message);
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink sell request INF failed " + ex.getCompleteErrMsg());
                sellReqJson.Add("fiResponseCode", "99");
                sellReqJson.Add("fiResponseMessage", ex.Message);
            }
            if (!keepOpenSession)
            {
                try
                {
                    isSessionOpened = false;
                    CloseSession();
                }
                catch { }
            }
            return sellReqJson;
        }

        private JsonLibs.MyJsonLib getBookingFromState(string clientSignature, ref bool isError)
        {
            JsonLibs.MyJsonLib getBookFromStateJson = new JsonLibs.MyJsonLib();
            agentSignature = clientSignature;

            if (!isSessionOpened)
            {
                if (!OpenSession())
                {
                    isError = true;
                    //getBookFromStateJson.Add("fiResponseCode", "78");
                    //getBookFromStateJson.Add("fiResponseMessage", "Can not open session");
                    this.lastErrorMessage = "Can not open session on get booking from state";
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink get booking from state failed \r\nCan not open session");
                    getBookFromStateJson.Dispose();
                    return null;
                }
            }

            try
            {
                GetBookingFromStateRequest getBookFromStateReq = new GetBookingFromStateRequest();
                getBookFromStateReq.ContractVersion = 0;
                getBookFromStateReq.Signature = agentSignature;

                IBookingManager bookingAPI = new BookingManagerClient();
                GetBookingFromStateResponse bookFromStateResp = bookingAPI.GetBookingFromState(getBookFromStateReq);
                // cari infant
                bool fAdaInfant = false;
                //JsonLibs.MyJsonArray psgrArrInf = new JsonLibs.MyJsonArray();
                //JsonLibs.MyJsonArray psgrArr = new JsonLibs.MyJsonArray();

                // infant price detail
                //JsonLibs.MyJsonLib aPsgrINFJs = new JsonLibs.MyJsonLib();
                JsonLibs.MyJsonLib anInfDetailsJs = new JsonLibs.MyJsonLib();
                foreach (Passenger psgr in bookFromStateResp.BookingData.Passengers)
                {
                    if ((psgr.PassengerFees != null) && (psgr.PassengerFees.Length > 0))
                    {
                        // ada infant
                        if (!fAdaInfant)
                        {
                            foreach (PassengerFee psgrFee in psgr.PassengerFees)
                            {
                                if (!fAdaInfant)
                                {
                                    fAdaInfant = true;
                                    //aPsgrJs.Add("FlightReference", psgrFee.FlightReference);
                                    anInfDetailsJs.Add("PaxType", "INF");
                                    JsonLibs.MyJsonArray bookSvcCharges = new JsonLibs.MyJsonArray();
                                    foreach (BookingServiceCharge bookSvcChg in psgrFee.ServiceCharges)
                                    {
                                        JsonLibs.MyJsonLib aChgJs = new JsonLibs.MyJsonLib();
                                        aChgJs.Add("Amount", bookSvcChg.Amount);
                                        aChgJs.Add("ChargeType", bookSvcChg.ChargeType.ToString());
                                        aChgJs.Add("ChargeCode", bookSvcChg.ChargeCode);
                                        aChgJs.Add("ChargeDetail", bookSvcChg.ChargeDetail);
                                        aChgJs.Add("CurrencyCode", bookSvcChg.CurrencyCode);
                                        bookSvcCharges.Add(aChgJs);
                                    }
                                    anInfDetailsJs.Add("ServiceCharges", bookSvcCharges);

                                    break;
                                    //psgrArr.Add(aPsgrINFJs);
                                }
                            }
                        }
                    }
                }
                if (!fAdaInfant) anInfDetailsJs.Dispose();
                //getBookFromStateJson.Add("PassengersINF",psgrArrInf);
                //psgrArr.Add(anInfDetailsJs);

                // cari passengers adt dan chd
                //JsonLibs.MyJsonArray psgrArr = new JsonLibs.MyJsonArray();
                //JsonLibs.MyJsonArray aJourneyJs = new JsonLibs.MyJsonArray();
                JsonLibs.MyJsonArray aJourneyPriceJs = new JsonLibs.MyJsonArray();
                foreach (Journey jor in bookFromStateResp.BookingData.Journeys)
                {
                    // bisa dipastikan 1 segment dan 1 fare
                    JsonLibs.MyJsonArray aJourneyPrice = new JsonLibs.MyJsonArray();
                    foreach (Segment seg in jor.Segments)
                    {
                        JsonLibs.MyJsonLib aSegPrice = new JsonLibs.MyJsonLib();
                        aSegPrice.Add("DepartureStation", seg.DepartureStation);
                        aSegPrice.Add("ArrivalStation", seg.ArrivalStation);
                        aSegPrice.Add("STA", seg.STA.ToString("yyyy-MM-dd HH:mm:ss"));
                        aSegPrice.Add("STD", seg.STD.ToString("yyyy-MM-dd HH:mm:ss"));
                        aSegPrice.Add("CarrierCode", seg.FlightDesignator.CarrierCode);
                        aSegPrice.Add("FlightNumber", seg.FlightDesignator.FlightNumber);

                        JsonLibs.MyJsonArray SegDetails = new JsonLibs.MyJsonArray();
                        //if (fAdaInfant) JorDetails.Add(anInfDetailsJs);
                        foreach (PaxFare pax in seg.Fares[0].PaxFares)
                        {
                            JsonLibs.MyJsonLib aPaxDetailsJs = new JsonLibs.MyJsonLib();
                            aPaxDetailsJs.Add("PaxType", pax.PaxType);
                            JsonLibs.MyJsonArray bookSvcCharges = new JsonLibs.MyJsonArray();
                            foreach (BookingServiceCharge bookSvcChg in pax.ServiceCharges)
                            {
                                JsonLibs.MyJsonLib aChgJs = new JsonLibs.MyJsonLib();
                                aChgJs.Add("Amount", bookSvcChg.Amount);
                                aChgJs.Add("ChargeType", bookSvcChg.ChargeType.ToString());
                                aChgJs.Add("ChargeCode", bookSvcChg.ChargeCode);
                                aChgJs.Add("ChargeDetail", bookSvcChg.ChargeDetail);
                                aChgJs.Add("CurrencyCode", bookSvcChg.CurrencyCode);
                                bookSvcCharges.Add(aChgJs);
                            }
                            aPaxDetailsJs.Add("ServiceCharges", bookSvcCharges);

                            SegDetails.Add(aPaxDetailsJs);
                        }

                        aSegPrice.Add("SegmentPriceDetails", SegDetails);

                        //aJourneyPrice.Add("JourneyPriceDetails", aSegPrice);
                        aJourneyPrice.Add(aSegPrice);
                    }
                    aJourneyPriceJs.Add(aJourneyPrice);
                }

                if (fAdaInfant)
                {
                    getBookFromStateJson.Add("PriceDetailINF", anInfDetailsJs);
                }
                else
                {
                    getBookFromStateJson.Add("PriceDetailINF", null);
                }

                getBookFromStateJson.Add("PriceDetails", aJourneyPriceJs);
                
                isError = false;
            }
            catch (Exception ex)
            {
                isError = true;
                this.lastErrorMessage = ex.Message; // = "Bad Data.\r\n" jika salah signature
                //Console.WriteLine(ex.Message);
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink get booking from state failed " + ex.getCompleteErrMsg());
            }
            return getBookFromStateJson;
        }

        public JsonLibs.MyJsonLib sellRequestALL(CitilinkLib.CitilinkHandler.SellKey[] SellKeys,
            short paxCountADT, short paxCountCHD, short paxCountINF, string currencyCode,
            InfSellReq[] infSellReqs,
            string clientSignature, ref bool isError)
        {
            JsonLibs.MyJsonLib sellReqAll = new JsonLibs.MyJsonLib();

            JsonLibs.MyJsonLib sellReqAdtChd = sellRequest(SellKeys, paxCountADT, paxCountCHD,
                currencyCode, clientSignature, true, ref isError, true);
            if ((sellReqAdtChd == null) || (isError))
            {
                sellReqAll.Dispose();
                return null;
            }

            sellReqAll.Add("fiSellRequest", sellReqAdtChd);

            if (paxCountINF > 0)
            {
                //JsonLibs.MyJsonLib sellReqInf = sellRequestINF(infantCount, currencyCode,
                //    carrierCodes, flightNumbers, STDs, departureStation, arrivalStation,
                //    clientSignature, true, ref isError, true);

                JsonLibs.MyJsonLib sellReqInf = sellRequestINF(paxCountINF, currencyCode,
                    infSellReqs, clientSignature, true, ref isError, true);
                if ((sellReqInf == null) || (isError))
                {
                    sellReqAll.Dispose();
                    return null;
                }
                else
                    sellReqAll.Add("fiSellRequestINF", sellReqInf);
            }
            else
            {
                sellReqAll.Add("fiSellRequestINF", null);
            }

            if (!isError)
            {
                JsonLibs.MyJsonLib paymentDetails = getBookingFromState(clientSignature, ref isError);
                //JsonLibs.MyJsonArray paymentDetails = getBookingFromState(clientSignature, ref isError);
                if (paymentDetails != null)
                    sellReqAll.Add("fiPriceDetails", paymentDetails);
                else
                    sellReqAll.Add("fiPriceDetails", null);
            }
            try
            {
                CloseSession();
            }
            catch { }

            // SEMENTARA SAJO BOS>>>>>>
            //sellReqAll.Add("fiIWJR", 5000);
            //sellReqAll.Add("fiPercentTax", 10.0);
            //sellReqAll.Add("fiGateCost", 40000);
            //sellReqAll.Add("fiPercentAdminFee", 5.0);

            sellReqAll.Add("fiResponseCode", "00");
            sellReqAll.Add("fiResponseMessage", "Success");
            sellReqAll.Add("fiSignature", agentSignature);
            return sellReqAll;
        }

        public struct CitilinkPassenger
        {
            public string FirstName;
            public string MiddleName;
            public string LastName;
            public string Suffix;
            public string Title;
            public string Gender;
            public string WeightCategory;
            public string DOB;
            public string Nationality;
            public string PaxType;
            public bool IsInfant;
            public string InfantFirstName;
            public string InfantMiddleName;
            public string InfantLastName;
            public string InfantSuffix;
            public string InfantTitle;
            public string InfantGender;
            public string InfantDOB;
            public string InfantNationality;
        }
        public struct CitilinkContact
        {
            public string FirstName;
            public string LastName;
            public string Title;
            public string EmailAddress;
            public string HomePhone;
            public string WorkPhone;
            public string OtherPhone;
            public string Fax;
            public string CompanyName;
            public string AddressLine1;
            public string AddressLine2;
            public string AddressLine3;
            public string City;
            public string ProvinceState;
            public string PostalCode;
            public string CountryCode;
            public string CultureCode;
            public string DistributionOption;
            public string CustomerNumber;
            public string NotificationPreference;
            public string SourceOrganization;
            public string State;
        }

        private PassengerInfant[] getInfants(
                CitilinkPassenger[] citiPsgrs)
        {
            if ((citiPsgrs == null) || (citiPsgrs.Length == 0)) return null;

            PassengerInfant[] infants = new PassengerInfant[citiPsgrs.Length];

            for (int i = 0; i < citiPsgrs.Length; i++)
            {
                PassengerInfant anInfant = new PassengerInfant();
                anInfant.DOB = DateTime.ParseExact(citiPsgrs[i].InfantDOB, "yyyy-MM-dd", null); //DateTime.Now;
                if (citiPsgrs[i].InfantGender == "Male")
                    anInfant.Gender = Gender.Male;
                else if (citiPsgrs[i].InfantGender == "Female")
                    anInfant.Gender = Gender.Female;
                else
                    anInfant.Gender = Gender.Unmapped;

                anInfant.Nationality = citiPsgrs[i].InfantNationality;
                anInfant.ResidentCountry = String.Empty;

                // data nama  bayi
                BookingName[] infantNames = new BookingName[1];
                BookingName anInfantName = new BookingName();
                anInfantName.FirstName = citiPsgrs[i].InfantFirstName;
                anInfantName.MiddleName = citiPsgrs[i].InfantMiddleName;
                anInfantName.LastName = citiPsgrs[i].InfantLastName;
                anInfantName.Suffix = citiPsgrs[i].InfantSuffix;
                anInfantName.Title = citiPsgrs[i].InfantTitle;

                infantNames[0] = anInfantName;      // krn 1 bayi satu nama dulu
                anInfant.Names = infantNames;
                //aPassenger.Infant = anInfant;

                //PassengerInfant[] infants = new PassengerInfant[1]; // 1 bayi per penumpang
                infants[i] = anInfant;
                //aPassenger.PassengerInfants = infants;
                //aPassenger.PassengerInfants = null;
            }
            return infants;
        }

        private JsonLibs.MyJsonLib updatePassengers(
                CitilinkPassenger[] citiPsgrs, PassengerInfant[] Infants, string currencyCode,
                string clientSignature, ref bool isError, bool keepOpenSession)
        {
            JsonLibs.MyJsonLib updPsgrsJson = new JsonLibs.MyJsonLib();
            agentSignature = clientSignature;
            if (clientSignature != "") isLoggedIn = true;

            if (!isSessionOpened)
            {
                if (!OpenSession())
                {
                    isError = true;
                    updPsgrsJson.Add("fiResponseCode", "78");
                    updPsgrsJson.Add("fiResponseMessage", "Can not open session");
                    this.lastErrorMessage = "Can not open session on update passenger";
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink update passenger failed \r\nCan not open session");
                    return updPsgrsJson;
                }
            }
            if (!isLoggedIn)
            {
                if (Logon() == "")
                {
                    isError = true;
                    updPsgrsJson.Add("fiResponseCode", "79");
                    updPsgrsJson.Add("fiResponseMessage", "LogOn failed");
                    return updPsgrsJson;
                }
            }

            //string json = "";
            try
            {
                //Create an instance of BookingManagerClient

                bool fInfantsAdded = false;
                Passenger[] Passengers = new Passenger[citiPsgrs.Length];

                for (int i = 0; i < citiPsgrs.Length; i++)
                {
                    Passenger aPassenger = new Passenger();
                    aPassenger.PassengerPrograms = null;
                    aPassenger.CustomerNumber = String.Empty;
                    aPassenger.PassengerNumber = (short)i;
                    aPassenger.FamilyNumber = 0;
                    aPassenger.PaxDiscountCode = String.Empty;

                    if ((citiPsgrs[i].PaxType == "ADT") && (!fInfantsAdded))
                    {
                        fInfantsAdded = true;
                        if (Infants != null)
                        {
                            if (Infants.Length > 1)
                            {
                                aPassenger.PassengerInfants = Infants;
                                aPassenger.Infant = null;
                            }
                            else if (Infants.Length == 1)
                            {
                                aPassenger.PassengerInfants = null;
                                aPassenger.Infant = Infants[0];
                            }
                        }
                        else
                        {
                            aPassenger.PassengerInfants = null;
                            aPassenger.Infant = null;
                        }
                    }

                    // data nama passenger
                    BookingName[] bookingNames = new BookingName[1]; // fix dulu 1 nama utk 1 org
                    BookingName aBookingName = new BookingName();
                    aBookingName.FirstName = citiPsgrs[i].FirstName;
                    aBookingName.MiddleName = citiPsgrs[i].MiddleName;
                    aBookingName.LastName = citiPsgrs[i].LastName;
                    aBookingName.Suffix = citiPsgrs[i].Suffix;
                    aBookingName.Title = citiPsgrs[i].Title;

                    bookingNames[0] = aBookingName;  // karena cuma 1 nama per orang
                    aPassenger.Names = bookingNames;

                    PassengerInfo aPassengerInfo = new PassengerInfo();
                    aPassengerInfo.BalanceDue = 0;
                    if (citiPsgrs[i].Gender == "Male")
                        aPassengerInfo.Gender = Gender.Male;
                    else if (citiPsgrs[i].Gender == "Female")
                        aPassengerInfo.Gender = Gender.Female;
                    else
                        aPassengerInfo.Gender = Gender.Unmapped;

                    aPassengerInfo.Nationality = citiPsgrs[i].Nationality;
                    aPassengerInfo.ResidentCountry = String.Empty;
                    aPassengerInfo.TotalCost = 0;

                    if (citiPsgrs[i].WeightCategory == "Male")
                        aPassengerInfo.WeightCategory = WeightCategory.Male;
                    else if (citiPsgrs[i].WeightCategory == "Female")
                        aPassengerInfo.WeightCategory = WeightCategory.Female;
                    else if (citiPsgrs[i].WeightCategory == "Child")
                        aPassengerInfo.WeightCategory = WeightCategory.Child;
                    else
                        aPassengerInfo.WeightCategory = WeightCategory.Unmapped;

                    aPassenger.PassengerInfo = aPassengerInfo;

                    PassengerTypeInfo[] psgrTypeInfos = new PassengerTypeInfo[1];
                    psgrTypeInfos[0] = new PassengerTypeInfo();
					psgrTypeInfos[0].State = CitilinkBookingManager.MessageState.New; // fix heula new passenger
                    psgrTypeInfos[0].DOB = DateTime.ParseExact(citiPsgrs[i].DOB, "yyyy-MM-dd", null); //DateTime.Now;
                    psgrTypeInfos[0].PaxType = citiPsgrs[i].PaxType;
                    aPassenger.PassengerTypeInfos = psgrTypeInfos;

                    aPassenger.PassengerInfos = new PassengerInfo[1];
                    PassengerInfo aPsgInfo = new PassengerInfo();
					aPsgInfo.State = CitilinkBookingManager.MessageState.New; // fix heula new passenger
                    aPsgInfo.BalanceDue = 0;
                    if (citiPsgrs[i].Gender == "Male")
                        aPsgInfo.Gender = Gender.Male;
                    else if (citiPsgrs[i].Gender == "Female")
                        aPsgInfo.Gender = Gender.Female;
                    else
                        aPsgInfo.Gender = Gender.Unmapped;

                    aPsgInfo.Nationality = citiPsgrs[i].Nationality;
                    aPsgInfo.ResidentCountry = String.Empty;
                    aPsgInfo.TotalCost = 0;

                    if (citiPsgrs[i].WeightCategory == "Male")
                        aPsgInfo.WeightCategory = WeightCategory.Male;
                    else if (citiPsgrs[i].WeightCategory == "Female")
                        aPsgInfo.WeightCategory = WeightCategory.Female;
                    else if (citiPsgrs[i].WeightCategory == "Child")
                        aPsgInfo.WeightCategory = WeightCategory.Child;
                    else
                        aPsgInfo.WeightCategory = WeightCategory.Unmapped;

                    aPassenger.PassengerInfos[0] = aPsgInfo;

                    aPassenger.PassengerProgram = null;
                    aPassenger.PassengerFees = null;
                    aPassenger.PassengerAddresses = null;
                    aPassenger.PassengerTravelDocuments = null;
                    aPassenger.PassengerBags = null;
                    aPassenger.PassengerID = 0;

                    aPassenger.PseudoPassenger = false;

                    Passengers[i] = aPassenger;
                }


                UpdatePassengersRequestData updPsgrsReqData = new UpdatePassengersRequestData();
                updPsgrsReqData.WaiveNameChangeFee = false;
                updPsgrsReqData.Passengers = Passengers;

                UpdatePassengersRequest updPsgrsReq = new UpdatePassengersRequest();
                updPsgrsReq.ContractVersion = 0; // pix heula
                updPsgrsReq.Signature = agentSignature;
                updPsgrsReq.updatePassengersRequestData = updPsgrsReqData;

                IBookingManager bookingAPI = new BookingManagerClient();
                UpdatePassengersResponse updPsgrsResp = bookingAPI.UpdatePassengers(updPsgrsReq);

                if (updPsgrsResp.BookingUpdateResponseData.Warning != null)
                    updPsgrsJson.Add("Warning", updPsgrsResp.BookingUpdateResponseData.Warning.WarningText);
                else
                    updPsgrsJson.Add("Warning", null);
                if (updPsgrsResp.BookingUpdateResponseData.Error != null)
                    updPsgrsJson.Add("Error", updPsgrsResp.BookingUpdateResponseData.Error.ErrorText);
                else
                    updPsgrsJson.Add("Error", null);

                //updPsgrsResp.BookingUpdateResponseData.OtherServiceInformations[0].
                Success BookingUpdSuccessResp = updPsgrsResp.BookingUpdateResponseData.Success;
                BookingSum PnrAmount = BookingUpdSuccessResp.PNRAmount;

				//JsonLibs.MyJsonLib pnrAmountJson = new JsonLibs.MyJsonLib();
                //pnrAmountJson.Add("BalanceDue", PnrAmount.BalanceDue);
                //pnrAmountJson.Add("AuthorizedBalanceDue", PnrAmount.AuthorizedBalanceDue);
                //pnrAmountJson.Add("PassiveSegmentCount", (int)PnrAmount.PassiveSegmentCount);
                //pnrAmountJson.Add("PointsBalanceDue", PnrAmount.PointsBalanceDue);
                //pnrAmountJson.Add("SegmentCount", (int)PnrAmount.SegmentCount);
                //pnrAmountJson.Add("TotalCost", PnrAmount.TotalCost);
                //pnrAmountJson.Add("TotalPointCost", PnrAmount.TotalPointCost);

                //JsonLibs.MyJsonLib UpdSuccessJson = new JsonLibs.MyJsonLib();
                //UpdSuccessJson.Add("PNRAmount", pnrAmountJson);
                updPsgrsJson.Add("RecordLocator", BookingUpdSuccessResp.RecordLocator);

                //updPsgrsJson.Add("Success", UpdSuccessJson);
                updPsgrsJson.Add("fiResponseCode", "00");
                updPsgrsJson.Add("fiResponseMessage", "Success");
                //updPsgrsJson.Add("fiSignature", agentSignature);
                isError = false;

                if (!keepOpenSession)
                {
                    try
                    {
                        isSessionOpened = false;
                        CloseSession();
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                isError = true;
                this.lastErrorMessage = ex.Message; // = "Bad Data.\r\n" jika salah signature
                //Console.WriteLine(ex.Message);
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink update passengers failed " + ex.getCompleteErrMsg());
                //updPsgrsJson.Add("fiResponseCode", "99");
                //updPsgrsJson.Add("fiResponseMessage", ex.Message);
                try
                {
                    CloseSession();
                }
                catch { }
            }

            return updPsgrsJson;
        }

        private JsonLibs.MyJsonLib updateContact(
                CitilinkContact contactPsgr, string itenaryBackupEmail, string currencyCode,
                string clientSignature, ref bool isError, bool keepOpenSession)
        {
            JsonLibs.MyJsonLib updCtPsgrsJson = new JsonLibs.MyJsonLib();
            agentSignature = clientSignature;
            if (clientSignature != "") isLoggedIn = true;

            if (!isSessionOpened)
            {
                if (!OpenSession())
                {
                    isError = true;
                    updCtPsgrsJson.Add("fiResponseCode", "78");
                    updCtPsgrsJson.Add("fiResponseMessage", "Can not open session");
                    this.lastErrorMessage = "Can not open session on update contact";
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink update contact failed \r\nCan not open session");
                    return updCtPsgrsJson;
                }
            }
            if (!isLoggedIn)
            {
                if (Logon() == "")
                {
                    isError = true;
                    updCtPsgrsJson.Add("fiResponseCode", "79");
                    updCtPsgrsJson.Add("fiResponseMessage", "LogOn failed");
                    return updCtPsgrsJson;
                }
            }

            try
            {

                BookingName[] bookingNames = new BookingName[1];
                bookingNames[0] = new BookingName();
                bookingNames[0].FirstName = contactPsgr.FirstName;
                bookingNames[0].LastName = contactPsgr.LastName;
                bookingNames[0].MiddleName = "";
				bookingNames[0].State = CitilinkBookingManager.MessageState.New;
                bookingNames[0].Suffix = "";
                bookingNames[0].Title = contactPsgr.Title;

                BookingContact bookContact = new BookingContact();
                bookContact.Names = bookingNames;
                bookContact.AddressLine1 = contactPsgr.AddressLine1;
                bookContact.AddressLine2 = contactPsgr.AddressLine2;
                bookContact.AddressLine3 = contactPsgr.AddressLine3;
                bookContact.City = contactPsgr.City;
                bookContact.CompanyName = contactPsgr.CompanyName;
                bookContact.CountryCode = contactPsgr.CountryCode;
                bookContact.CultureCode = contactPsgr.CultureCode;
                bookContact.CustomerNumber = contactPsgr.CustomerNumber;
                //bookContact.DistributionOption = DistributionOption.None;// contactPsgr.DistributionOption;
                bookContact.DistributionOption = DistributionOption.Email;// contactPsgr.DistributionOption;
                bookContact.EmailAddress = contactPsgr.EmailAddress;
                if (bookContact.EmailAddress == "")
                {
                    bookContact.EmailAddress = itenaryBackupEmail;
                }
                bookContact.Fax = contactPsgr.Fax;
                bookContact.HomePhone = contactPsgr.HomePhone;
                bookContact.NotificationPreference = NotificationPreference.None;// contactPsgr.NotificationPreference;
                bookContact.OtherPhone = contactPsgr.OtherPhone;
                bookContact.PostalCode = contactPsgr.PostalCode;
                bookContact.ProvinceState = contactPsgr.ProvinceState;
                bookContact.SourceOrganization = contactPsgr.SourceOrganization;
				bookContact.State = CitilinkBookingManager.MessageState.New;
                bookContact.TypeCode = "P";
                bookContact.WorkPhone = contactPsgr.WorkPhone;

                BookingContact[] bookingContacts = new BookingContact[1];
                bookingContacts[0] = bookContact;

                UpdateContactsRequestData updateContactsReqData = new UpdateContactsRequestData();
                updateContactsReqData.BookingContactList = bookingContacts;

                UpdateContactsRequest updContactReq = new UpdateContactsRequest();
                updContactReq.ContractVersion = 0;
                updContactReq.Signature = agentSignature;
                updContactReq.updateContactsRequestData = updateContactsReqData;

                IBookingManager bookingAPI = new BookingManagerClient();
                UpdateContactsResponse updContactResp = bookingAPI.UpdateContacts(updContactReq);

                if (updContactResp.BookingUpdateResponseData.Warning != null)
                    updCtPsgrsJson.Add("Warning", updContactResp.BookingUpdateResponseData.Warning.WarningText);
                else
                    updCtPsgrsJson.Add("Warning", null);
                if (updContactResp.BookingUpdateResponseData.Error != null)
                    updCtPsgrsJson.Add("Error", updContactResp.BookingUpdateResponseData.Error.ErrorText);
                else
                    updCtPsgrsJson.Add("Error", null);

                //updPsgrsResp.BookingUpdateResponseData.OtherServiceInformations[0].
                Success BookingUpdSuccessResp = updContactResp.BookingUpdateResponseData.Success;
                BookingSum PnrAmount = BookingUpdSuccessResp.PNRAmount;

                JsonLibs.MyJsonLib pnrAmountJson = new JsonLibs.MyJsonLib();
                pnrAmountJson.Add("BalanceDue", PnrAmount.BalanceDue);
                pnrAmountJson.Add("AuthorizedBalanceDue", PnrAmount.AuthorizedBalanceDue);
                pnrAmountJson.Add("PassiveSegmentCount", (int)PnrAmount.PassiveSegmentCount);
                pnrAmountJson.Add("PointsBalanceDue", PnrAmount.PointsBalanceDue);
                pnrAmountJson.Add("SegmentCount", (int)PnrAmount.SegmentCount);
                pnrAmountJson.Add("TotalCost", PnrAmount.TotalCost);
                pnrAmountJson.Add("TotalPointCost", PnrAmount.TotalPointCost);

                //JsonLibs.MyJsonLib UpdSuccessJson = new JsonLibs.MyJsonLib();
                //UpdSuccessJson.Add("PNRAmount", pnrAmountJson);
                //UpdSuccessJson.Add("RecordLocator", BookingUpdSuccessResp.RecordLocator);

                //updCtPsgrsJson.Add("Success", UpdSuccessJson);
                updCtPsgrsJson.Add("fiResponseCode", "00");
                updCtPsgrsJson.Add("fiResponseMessage", "Success");
                //updCtPsgrsJson.Add("fiSignature", agentSignature);
                isError = false;

                //json = JsonConvert.SerializeObject(sellResponse.BookingUpdateResponseData);
                if (!keepOpenSession)
                {
                    try
                    {
                        isSessionOpened = false;
                        CloseSession();
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                isError = true;
                this.lastErrorMessage = ex.Message; // = "Bad Data.\r\n" jika salah signature
                //Console.WriteLine(ex.Message);
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink update passengers failed " + ex.getCompleteErrMsg());
                //updCtPsgrsJson.Add("fiResponseCode", "99");
                //updCtPsgrsJson.Add("fiResponseMessage", ex.Message);
                try
                {
                    CloseSession();
                }
                catch { }
            }

            return updCtPsgrsJson;
        }

        public JsonLibs.MyJsonLib updateContactAndPassengers(
                CitilinkContact contactPsgr, string itenaryBackupEmail, CitilinkPassenger[] citiPsgrs,
                CitilinkPassenger[] citiPsgrsINF,
                string currencyCode, string clientSignature,
                ref bool isError)
        {
            JsonLibs.MyJsonLib updContPass = new JsonLibs.MyJsonLib();

            JsonLibs.MyJsonLib updContact = updateContact(contactPsgr, itenaryBackupEmail, currencyCode,
                clientSignature, ref isError, true);
            if ((updContact == null) || (isError))
            {
                updContPass.Dispose();
                return null;
            }

            //updContPass.Add("fiContactPerson", updContact);
            updContact.Dispose();

            PassengerInfant[] infants = getInfants(citiPsgrsINF);

            JsonLibs.MyJsonLib updPassenger = updatePassengers(citiPsgrs, infants, currencyCode,
                clientSignature, ref isError, true);
            if ((updPassenger == null) || (isError))
            {
                updContPass.Dispose();
                return null;
            }

            //updContPass.Add("fiPassengers", updPassenger);
            updPassenger.Dispose();

            //if (citiPsgrsINF.Length > 0)
            //{
            //    JsonLibs.MyJsonLib updPassengerINF = updatePassengers(citiPsgrsINF, currencyCode,
            //        clientSignature, ref isError, false);
            //    if ((updPassengerINF == null) || (isError))
            //    {
            //        updContPass.Dispose();
            //        return null;
            //    }

            //    //updContPass.Add("fiPassengersINF", updPassengerINF);
            //    updPassengerINF.Dispose();
            //}

            // SEMENTARA SAJO BOS>>>>>>
            //updContPass.Add("fiIWJR", 5000);
            //updContPass.Add("fiPercentTax", 10.0);
            //updContPass.Add("fiGateCost", 40000);
            //updContPass.Add("fiPercentAdminFee", 5.0);

            updContPass.Add("fiResponseCode", "00");
            updContPass.Add("fiResponseMessage", "Success");
            updContPass.Add("fiSignature", agentSignature);
            return updContPass;
        }


        public JsonLibs.MyJsonLib addPayment(decimal TotalAmount, string currencyCode,
                string clientSignature, ref bool isError)
        {
            JsonLibs.MyJsonLib addPayJson = new JsonLibs.MyJsonLib();
            agentSignature = clientSignature;
            if (clientSignature != "") isLoggedIn = true;

            if (!isSessionOpened)
            {
                if (!OpenSession())
                {
                    isError = true;
                    addPayJson.Add("fiResponseCode", "78");
                    addPayJson.Add("fiResponseMessage", "Can not open session");
                    this.lastErrorMessage = "Can not open session on add payment";
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink add Payment failed \r\nCan not open session");
                    return addPayJson;
                }
            }

            try
            {

                AddPaymentToBookingRequestData addPaymentReqData = new AddPaymentToBookingRequestData();
				addPaymentReqData.MessageState = CitilinkBookingManager.MessageState.New; // pix heula New
                addPaymentReqData.WaiveFee = false;
                addPaymentReqData.ReferenceType = PaymentReferenceType.Session;
                addPaymentReqData.PaymentMethodType = RequestPaymentMethodType.AgencyAccount;
                //addPaymentReqData.PaymentMethodType = RequestPaymentMethodType.PrePaid;
                addPaymentReqData.PaymentMethodCode = "AG"; // pix
                addPaymentReqData.QuotedCurrencyCode = currencyCode;
                addPaymentReqData.QuotedAmount = TotalAmount;
                addPaymentReqData.Status = BookingPaymentStatus.New;
                addPaymentReqData.AccountNumberID = 0;
                addPaymentReqData.AccountNumber = "0014001793";//;"0003000179"; // pix heula
                addPaymentReqData.Expiration = DateTime.Parse("0001-01-01T00:00:00"); // pix heula
                addPaymentReqData.ParentPaymentID = 0;
                addPaymentReqData.Installments = 0;
                addPaymentReqData.PaymentText = String.Empty;
                addPaymentReqData.Deposit = false;
                addPaymentReqData.PaymentFields = null;
                addPaymentReqData.PaymentAddresses = null;
                addPaymentReqData.AgencyAccount = null;
                addPaymentReqData.CreditShell = null;
                addPaymentReqData.CreditFile = null;
                addPaymentReqData.PaymentVoucher = null;

                AddPaymentToBookingRequest addPaymentReq = new AddPaymentToBookingRequest();
                addPaymentReq.ContractVersion = 0;
                addPaymentReq.Signature = agentSignature;
                addPaymentReq.addPaymentToBookingReqData = addPaymentReqData;

                IBookingManager bookingAPI = new BookingManagerClient();
                AddPaymentToBookingResponse response = bookingAPI.AddPaymentToBooking(addPaymentReq);
                ValidationPayment vldtPayment = response.BookingPaymentResponse.ValidationPayment;
                //vldtPayment.PaymentValidationErrors[0].ErrorType.ToString();
                if (vldtPayment.PaymentValidationErrors.Length > 0)
                {
                    //Console.WriteLine(vldtPayment.PaymentValidationErrors[0].AttributeName + " " + vldtPayment.PaymentValidationErrors[0].ErrorDescription);
                    isError = true;
                    addPayJson.Add("fiResponseCode", "81");
                    addPayJson.Add("fiResponseMessage", vldtPayment.PaymentValidationErrors[0].ErrorDescription);
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink add Payment failed \r\n" + vldtPayment.PaymentValidationErrors[0].ErrorDescription);
                    return addPayJson;
                }
                if (vldtPayment.Payment.PaymentAddedToState)
                {
                    LogWriter.show(this,"Payment added to booking state: add payment complete");
                }
                //response.BookingPaymentResponse.ValidationPayment.Payment.ReferenceID
                //response.BookingPaymentResponse.ValidationPayment.Payment.

                Payment pay = vldtPayment.Payment;
                JsonLibs.MyJsonLib payJson = new JsonLibs.MyJsonLib();
                payJson.Add("State", pay.State.ToString());
                payJson.Add("Status", pay.Status.ToString());
                payJson.Add("AccountNumber", pay.AccountNumber);
                payJson.Add("AccountNumberID", pay.AccountNumberID);
                payJson.Add("AuthorizationStatus", pay.AuthorizationStatus.ToString());

                addPayJson.Add("fiPayment", payJson);
                addPayJson.Add("fiResponseCode", "00");
                addPayJson.Add("fiResponseMessage", "Success");

                //if (!keepOpenSession)
                //{
                    try
                    {
                        isSessionOpened = false;
                        CloseSession();
                    }
                    catch { }
                //}

            }
            catch (Exception ex)
            {
                isError = true;
                this.lastErrorMessage = ex.Message; // = "Bad Data.\r\n" jika salah signature
                //Console.WriteLine(ex.Message);
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink add Payment failed \r\n" + ex.getCompleteErrMsg());
                addPayJson.Add("fiResponseCode", "99");
                addPayJson.Add("fiResponseMessage", ex.Message);
                try
                {
                    CloseSession();
                }
                catch { }
            }

            return addPayJson;
        }

        //public JsonLibs.MyJsonLib addPaymentAndPriceDetail(decimal TotalAmount, string currencyCode,
        //        string clientSignature, ref bool isError)
        //{
        //    JsonLibs.MyJsonLib addPaymentJs = addPayment(TotalAmount, currencyCode,
        //        clientSignature, true, ref isError);

        //    if (!isError)
        //    {
        //        JsonLibs.MyJsonLib paymentDetails = getBookingFromState(clientSignature, ref isError);
        //        if (paymentDetails != null)
        //            addPaymentJs.Add("fiPriceDetails", paymentDetails);
        //        else
        //            addPaymentJs.Add("fiPriceDetails", null);
        //    }
        //    try
        //    {
        //        CloseSession();
        //    }
        //    catch { }

        //    return addPaymentJs;
        //}

        private bool sendItenary(string recordLocator, string signature, ref bool isError)
        {
            SendItineraryRequest sendItenaryReq = new SendItineraryRequest();
            sendItenaryReq.ContractVersion = 0;
            sendItenaryReq.Signature = signature;
            sendItenaryReq.RecordLocatorReqData = recordLocator;

            try
            {
                IBookingManager bookingAPI = new BookingManagerClient();
				//SendItineraryResponse sendItenaryResp = bookingAPI.SendItinerary(sendItenaryReq);
				bookingAPI.SendItinerary(sendItenaryReq);
                return true;
            }
            catch (Exception ex)
            {
                isError = true;
                this.lastErrorMessage = ex.Message;
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink send intenary failed \r\n" + ex.getCompleteErrMsg());
                return false;
            }
        }

        //public void getBookingFromStateForCommit(string clientSignature, ref bool isError)
        public bool getPaymentAmount(ref decimal TotalAmount, ref decimal TotalBaseAmount, string clientSignature)
        {
            decimal totalAmount = 0;
            decimal totalBaseAmount = 0;
            bool isError = false;
            try
            {
                agentSignature = clientSignature;
                GetBookingFromStateRequest getBookFromStateReq = new GetBookingFromStateRequest();
                getBookFromStateReq.ContractVersion = 0;
                getBookFromStateReq.Signature = agentSignature;

                IBookingManager bookingAPI = new BookingManagerClient();
                GetBookingFromStateResponse bookFromStateResp = bookingAPI.GetBookingFromState(getBookFromStateReq);

                // ambil jumlah dan tipe penumpangnya
                int paxAdt = 0;
                int paxChd = 0;
                int paxInf = 0;

				//bool fAdaInfant = false;
                decimal AdtBaseAmount = 0;
                decimal ChdBaseAmount = 0;
                decimal InfBaseAmount = 0;
                decimal AdtAmount = 0;
                decimal ChdAmount = 0;
                decimal InfAmount = 0;
                decimal totalInfAmount = 0;

                foreach (Passenger psg in bookFromStateResp.BookingData.Passengers)
                {
                    if(psg.PassengerTypeInfos[0].PaxType =="ADT") 
                    {
                        paxAdt++;
                        //if(psg.PassengerInfants != null) paxInf += psg.PassengerInfants.Length;
                    }
                    else if(psg.PassengerTypeInfos[0].PaxType =="CHD") paxChd++;

                    if ((psg.PassengerFees != null) && (psg.PassengerFees.Length > 0))
                    {
                        paxInf++;
                        // ada infant
                        //if (!fAdaInfant)
                        //{
                            foreach (PassengerFee psgrFee in psg.PassengerFees)
                            {
                                //if (!fAdaInfant)
                                //{
                                //    fAdaInfant = true;
                                    foreach (BookingServiceCharge svc in psgrFee.ServiceCharges)
                                    {
                                        if (svc.ChargeType == ChargeType.FarePrice)
                                        {
                                            InfBaseAmount += svc.Amount;
                                            totalInfAmount += svc.Amount;
                                        }
                                        else if ((svc.ChargeType == ChargeType.Discount) ||
                                                (svc.ChargeType == ChargeType.PromotionDiscount))
                                        {
                                            InfBaseAmount -= svc.Amount;
                                            totalInfAmount -= svc.Amount;
                                        }
                                        else
                                        {
                                            totalInfAmount += svc.Amount;
                                        }
                                    }
                                //    break;
                                //}
                            }
                        //}
                    }
                }

                foreach (Journey jor in bookFromStateResp.BookingData.Journeys)
                {
                    foreach (Segment seg in jor.Segments)
                    {
                        foreach (Fare fare in seg.Fares)
                        {
                            foreach (PaxFare pax in fare.PaxFares)
                            {
                                foreach (BookingServiceCharge svc in pax.ServiceCharges)
                                {
                                    if (pax.PaxType == "ADT")
                                    {
                                        if (svc.ChargeType == ChargeType.FarePrice)
                                        {
                                            AdtBaseAmount += svc.Amount;
                                            AdtAmount += svc.Amount;
                                        }
                                        else if ((svc.ChargeType == ChargeType.Discount) ||
                                                (svc.ChargeType == ChargeType.PromotionDiscount))
                                        {
                                            AdtBaseAmount -= svc.Amount;
                                            AdtAmount -= svc.Amount;
                                        }
                                        else
                                        {
                                            AdtAmount += svc.Amount;
                                        }
                                    }
                                    else if (pax.PaxType == "CHD")
                                    {
                                        if (svc.ChargeType == ChargeType.FarePrice)
                                        {
                                            ChdBaseAmount += svc.Amount;
                                            ChdAmount += svc.Amount;
                                        }
                                        else if ((svc.ChargeType == ChargeType.Discount) ||
                                                (svc.ChargeType == ChargeType.PromotionDiscount))
                                        {
                                            ChdBaseAmount -= svc.Amount;
                                            ChdAmount -= svc.Amount;
                                        }
                                        else
                                        {
                                            ChdAmount += svc.Amount;
                                        }
                                    }
                                    //else if ((pax.PaxType == "INF") && (!fAdaInfant))
                                    else if (pax.PaxType == "INF")
                                    {
                                        if (svc.ChargeType == ChargeType.FarePrice)
                                        {
                                            InfBaseAmount += svc.Amount;
                                            InfAmount += svc.Amount;
                                        }
                                        else if ((svc.ChargeType == ChargeType.Discount) ||
                                                (svc.ChargeType == ChargeType.PromotionDiscount))
                                        {
                                            InfBaseAmount -= svc.Amount;
                                            InfAmount -= svc.Amount;
                                        }
                                        else
                                        {
                                            InfAmount += svc.Amount;
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
                //totalBaseAmount = (paxAdt * AdtBaseAmount) + (paxChd * ChdBaseAmount) +
                //                    (paxInf * InfBaseAmount);
                totalBaseAmount = (paxAdt * AdtBaseAmount) + (paxChd * ChdBaseAmount);
                totalAmount = (paxAdt * AdtAmount) + (paxChd * ChdAmount) + totalInfAmount;
                                    //(paxInf * InfAmount);

                LogWriter.showDEBUG(this, "Jumlah INF: " + paxInf.ToString() + " : " + InfAmount);
                isError = false;
            }
            catch (Exception ex)
            {
                isError = true;
                this.lastErrorMessage = ex.Message; // = "Bad Data.\r\n" jika salah signature
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink getBookingFromState failed \r\n" + ex.getCompleteErrMsg());
            }
            TotalBaseAmount = totalBaseAmount;
            TotalAmount = totalAmount;
            return !isError;
        }

        public JsonLibs.MyJsonLib commitPayment(
                short PaxCount, string currencyCode, ref string dRecordLocator,
                string clientSignature, ref bool isError)
        {
            JsonLibs.MyJsonLib commitJson = new JsonLibs.MyJsonLib();
            agentSignature = clientSignature;
            if (clientSignature != "") isLoggedIn = true;

            if (!isSessionOpened)
            {
                if (!OpenSession())
                {
                    isError = true;
                    commitJson.Add("fiResponseCode", "78");
                    commitJson.Add("fiResponseMessage", "Can not open session");
                    this.lastErrorMessage = "Can not open session on commit payment";
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink commit failed \r\nCan not open session");
                    return commitJson;
                }
            }

            try
            {
                PointOfSale sourcePOS = new PointOfSale();
				sourcePOS.State = CitilinkBookingManager.MessageState.New; // fix heula new passenger
                sourcePOS.AgentCode = agentName;
                //sourcePOS.OrganizationCode = "0003000179";
                sourcePOS.DomainCode = "EXT";

                BookingCommitRequestData commitReqData = new BookingCommitRequestData();
                commitReqData.SourcePOS = sourcePOS;
				commitReqData.State = CitilinkBookingManager.MessageState.New;
                commitReqData.CurrencyCode = currencyCode;
                commitReqData.ReceivedBy = null;
                commitReqData.BookingComments = null;
                commitReqData.Passengers = null;
                //commitReqData.BookingID

                commitReqData.PaxCount = PaxCount;

                BookingCommitRequest commitReq = new BookingCommitRequest();
                commitReq.BookingCommitRequestData = commitReqData;
                commitReq.Signature = agentSignature;
                commitReq.ContractVersion = 0; // pix heula

                IBookingManager bookingAPI = new BookingManagerClient();
                BookingCommitResponse commitResp = bookingAPI.BookingCommit(commitReq);

                JsonLibs.MyJsonLib pnrAmountJson = new JsonLibs.MyJsonLib();
                Success commitSuccess = commitResp.BookingUpdateResponseData.Success;
                BookingSum PnrAmount = commitSuccess.PNRAmount;
                pnrAmountJson.Add("BalanceDue", PnrAmount.BalanceDue);
                pnrAmountJson.Add("AuthorizedBalanceDue", PnrAmount.AuthorizedBalanceDue);
                pnrAmountJson.Add("PassiveSegmentCount", (int)PnrAmount.PassiveSegmentCount);
                pnrAmountJson.Add("PointsBalanceDue", PnrAmount.PointsBalanceDue);
                pnrAmountJson.Add("SegmentCount", (int)PnrAmount.SegmentCount);
                pnrAmountJson.Add("TotalCost", PnrAmount.TotalCost);
                pnrAmountJson.Add("TotalPointCost", PnrAmount.TotalPointCost);

                JsonLibs.MyJsonLib commitSuccessResp = new JsonLibs.MyJsonLib();
                commitSuccessResp.Add("PNRAmount", pnrAmountJson);
                commitSuccessResp.Add("RecordLocator", commitSuccess.RecordLocator);

                dRecordLocator = commitSuccess.RecordLocator;

                commitJson.Add("Success", commitSuccessResp);
                if (commitResp.BookingUpdateResponseData.Warning != null)
                    commitJson.Add("Warning", commitResp.BookingUpdateResponseData.Warning.WarningText);
                else
                    commitJson.Add("Warning", null);
                if (commitResp.BookingUpdateResponseData.Error != null)
                    commitJson.Add("Error", commitResp.BookingUpdateResponseData.Error.ErrorText);
                else
                    commitJson.Add("Error", null);

                isError = false;

                commitJson.Add("fiResponseCode", "00");
                commitJson.Add("fiResponseMessage", "Success");

                sendItenary(commitSuccess.RecordLocator, agentSignature, ref isError);

                try
                {
                    // ============== getBooking ==========
                    bool fBookError = false;
                    JsonLibs.MyJsonLib booking = getBooking(commitSuccess.RecordLocator,
                        agentSignature, ref fBookError);
                    if (!fBookError)
                        commitJson.Add("Booking", booking);
                    else
                        commitJson.Add("Booking", null);
                    // ====================================
                }
                catch { }

                Logoff();
            }
            catch (Exception ex)
            {
                isError = true;
                this.lastErrorMessage = ex.Message; // = "Bad Data.\r\n" jika salah signature
                //Console.WriteLine(ex.Message);
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink commit failed \r\n" + ex.getCompleteErrMsg());
                commitJson.Add("fiResponseCode", "99");
                commitJson.Add("fiResponseMessage", ex.Message);
                Logoff();
            }

            try
            {
                CloseSession();
            }
            catch { }

            return commitJson;
        }

        private void getPassengersJson(Passenger[] Passengers,
            ref JsonLibs.MyJsonArray PassengerArr, ref JsonLibs.MyJsonArray PassengerArrINF)
        {
            //JsonLibs.MyJsonArray psgrs = new JsonLibs.MyJsonArray();
            PassengerArr.Clear();
            PassengerArrINF.Clear();
            //JsonLibs.MyJsonArray psgrsINF = new JsonLibs.MyJsonArray();
            //psgrs.Name = "Passengers";
            for (int i = 0; i < Passengers.Length; i++)
            {
                JsonLibs.MyJsonLib aPassengerJs = new JsonLibs.MyJsonLib();
                
                if (Passengers[i].Names.Length > 0)
                {
                    aPassengerJs.Add("FirstName", Passengers[i].Names[0].FirstName);
                    aPassengerJs.Add("LastName", Passengers[i].Names[0].LastName);
                    aPassengerJs.Add("MiddleName", Passengers[i].Names[0].MiddleName);
                    aPassengerJs.Add("Title", Passengers[i].Names[0].Title);
                    aPassengerJs.Add("Suffix", Passengers[i].Names[0].Suffix);
                }

                if (Passengers[i].PassengerAddresses.Length > 0)
                {
                    aPassengerJs.Add("AddressLine1", Passengers[i].PassengerAddresses[0].AddressLine1);
                    aPassengerJs.Add("AddressLine2", Passengers[i].PassengerAddresses[0].AddressLine2);
                    aPassengerJs.Add("AddressLine3", Passengers[i].PassengerAddresses[0].AddressLine3);
                    aPassengerJs.Add("City", Passengers[i].PassengerAddresses[0].City);
                    aPassengerJs.Add("CountryCode", Passengers[i].PassengerAddresses[0].CountryCode);
                    aPassengerJs.Add("Phone", Passengers[i].PassengerAddresses[0].Phone);
                    aPassengerJs.Add("PostalCode", Passengers[i].PassengerAddresses[0].PostalCode);
                    aPassengerJs.Add("ProvinceState", Passengers[i].PassengerAddresses[0].ProvinceState);
                    aPassengerJs.Add("StationCode", Passengers[i].PassengerAddresses[0].StationCode);
                }
                if (Passengers[i].PassengerTypeInfos.Length > 0)
                {
                    aPassengerJs.Add("DOB", Passengers[i].PassengerTypeInfos[0].DOB.ToString("yyyy-MM-dd HH:mm:ss"));
                }

                aPassengerJs.Add("PassengerID", Passengers[i].PassengerID);

                if ((Passengers[i].PassengerInfants != null) &&
                    (Passengers[i].PassengerInfants.Length > 0))
                {
                    PassengerInfant[] psgrInfants = Passengers[i].PassengerInfants;
                    //JsonLibs.MyJsonArray psgrInfantsJs = new JsonLibs.MyJsonArray();
                    for (int j = 0; j < psgrInfants.Length; j++)
                    {
                        PassengerInfant anInfant = psgrInfants[i];
                        JsonLibs.MyJsonLib anInfantJs = new JsonLibs.MyJsonLib();
                        anInfantJs.Add("DOB", anInfant.DOB.ToString("yyyy-MM-dd HH:mm:ss"));
                        anInfantJs.Add("Gender", anInfant.Gender.ToString());
                        if (anInfant.Names.Length > 0)
                        {
                            anInfantJs.Add("FirstName", anInfant.Names[0].FirstName);
                            anInfantJs.Add("LastName", anInfant.Names[0].LastName);
                            anInfantJs.Add("MiddleName", anInfant.Names[0].MiddleName);
                            anInfantJs.Add("Title", anInfant.Names[0].Title);
                            anInfantJs.Add("Suffix", anInfant.Names[0].Suffix);
                        }
                        anInfantJs.Add("Nationality", anInfant.Nationality);
                        anInfantJs.Add("ResidentCountry", anInfant.ResidentCountry);
                        //psgrInfantsJs.Add(anInfantJs);
                        PassengerArrINF.Add(anInfantJs);
                    }
                    //aPassengerJs.Add("PassengerInfants", psgrInfantsJs);
                }

                PassengerInfo psgrInfo = Passengers[i].PassengerInfo;
                aPassengerJs.Add("Gender", psgrInfo.Gender.ToString());
                aPassengerJs.Add("Nationality", psgrInfo.Nationality);
                aPassengerJs.Add("TotalCost", psgrInfo.TotalCost);

                aPassengerJs.Add("PassengerNumber", (int)Passengers[i].PassengerNumber);
                aPassengerJs.Add("PaxDiscountCode", Passengers[i].PaxDiscountCode);

                PassengerArr.Add(aPassengerJs);
            }
            //return psgrs;
        }

        public JsonLibs.MyJsonLib getBookingLengkap(
                string recordLocator,
                string clientSignature, ref bool isError)
        {
            JsonLibs.MyJsonLib getBookingJson = new JsonLibs.MyJsonLib();
            agentSignature = clientSignature;
            if (clientSignature != "") isLoggedIn = true;

            if (!isSessionOpened)
            {
                if (!OpenSession())
                {
                    isError = true;
                    this.lastErrorMessage = "Can not open session";
                    getBookingJson.Add("fiResponseCode", "78");
                    getBookingJson.Add("fiResponseMessage", "Can not open session for get booking");
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink get booking failed \r\nCan not open session");
                    return getBookingJson;
                }
            }

            try
            {
                // get booking
                GetByRecordLocator getByRecordLocator = new GetByRecordLocator();
                getByRecordLocator.RecordLocator = recordLocator;

                GetBookingRequestData getBookingReqData = new GetBookingRequestData();
                getBookingReqData.GetByRecordLocator = getByRecordLocator;
                getBookingReqData.GetBookingBy = GetBookingBy.RecordLocator;

                GetBookingRequest GetBookingReq = new GetBookingRequest();
                GetBookingReq.ContractVersion = 0;
                GetBookingReq.Signature = agentSignature;
                GetBookingReq.GetBookingReqData = getBookingReqData;

                IBookingManager bookingAPI = new BookingManagerClient();
                GetBookingResponse getBookingResp = bookingAPI.GetBooking(GetBookingReq);

                Booking booking = getBookingResp.Booking;
                BookingContact bk = booking.BookingContacts[0];
                JsonLibs.MyJsonLib aContact = new JsonLibs.MyJsonLib();
                aContact.Add("AddressLine1", bk.AddressLine1);
                aContact.Add("AddressLine2", bk.AddressLine2);
                aContact.Add("AddressLine3", bk.AddressLine3);
                aContact.Add("City", bk.City);
                aContact.Add("CustomerNumber", bk.CustomerNumber);
                aContact.Add("EmailAddress", bk.EmailAddress);
                aContact.Add("FirstName", bk.Names[0].FirstName);
                aContact.Add("LastName", bk.Names[0].LastName);
                aContact.Add("MiddleName", bk.Names[0].MiddleName);
                aContact.Add("Title", bk.Names[0].Title);
                aContact.Add("State", bk.Names[0].State.ToString());
                aContact.Add("HomePhone", bk.HomePhone);
                aContact.Add("OtherPhone", bk.OtherPhone);
                aContact.Add("ProvinceState", bk.ProvinceState);
                aContact.Add("State", bk.State.ToString());

                JsonLibs.MyJsonLib bookingSum = new JsonLibs.MyJsonLib();
                bookingSum.Add("AuthorizedBalanceDue", booking.BookingSum.AuthorizedBalanceDue);
                bookingSum.Add("BalanceDue", booking.BookingSum.BalanceDue);
                bookingSum.Add("PassiveSegmentCount", (int)booking.BookingSum.PassiveSegmentCount);
                bookingSum.Add("SegmentCount", (int)booking.BookingSum.SegmentCount);
                bookingSum.Add("PointsBalanceDue", booking.BookingSum.PointsBalanceDue);
                bookingSum.Add("TotalCost", booking.BookingSum.TotalCost);
                bookingSum.Add("TotalPointCost", booking.BookingSum.TotalPointCost);

                JsonLibs.MyJsonLib bookingInfo = new JsonLibs.MyJsonLib();
                bookingInfo.Add("BookingDate", booking.BookingInfo.BookingDate.ToString("yyyy-MM-dd HH:mm:ss"));
                bookingInfo.Add("BookingStatus", booking.BookingInfo.BookingStatus.ToString());
                bookingInfo.Add("BookingType", booking.BookingInfo.BookingType);
                bookingInfo.Add("CreatedDate", booking.BookingInfo.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));
                bookingInfo.Add("ExpiredDate", booking.BookingInfo.ExpiredDate.ToString("yyyy-MM-dd HH:mm:ss"));
                bookingInfo.Add("OwningCarrierCode", booking.BookingInfo.OwningCarrierCode);
                bookingInfo.Add("PaidStatus", booking.BookingInfo.PaidStatus.ToString());
                bookingInfo.Add("PriceStatus", booking.BookingInfo.PriceStatus.ToString());
                bookingInfo.Add("ProfileStatus", booking.BookingInfo.ProfileStatus.ToString());
                bookingInfo.Add("State", booking.BookingInfo.State.ToString());

                getBookingJson.Add("ContactPerson", aContact);
                getBookingJson.Add("BookingSum", bookingSum);
                getBookingJson.Add("BookingInfo", bookingInfo);

                getBookingJson.Add("BookingID", booking.BookingID);
                getBookingJson.Add("CurrencyCode", booking.CurrencyCode);
                getBookingJson.Add("GroupName", booking.GroupName);

                JsonLibs.MyJsonArray journeys = new JsonLibs.MyJsonArray();
                for (int i = 0; i < booking.Journeys.Length; i++)
                {
                    JsonLibs.MyJsonLib Journey = new JsonLibs.MyJsonLib();
                    Journey.Add("JourneySellKey", booking.Journeys[i].JourneySellKey);
                    Journey.Add("State", booking.Journeys[i].State.ToString());

                    JsonLibs.MyJsonArray Segments = new JsonLibs.MyJsonArray();
                    for (int j = 0; j < booking.Journeys[i].Segments.Length; j++)
                    {
                        JsonLibs.MyJsonLib aSegments = new JsonLibs.MyJsonLib();
                        Segment aSeg = booking.Journeys[i].Segments[j];

                        JsonLibs.MyJsonArray ja = new JsonLibs.MyJsonArray();
                        ja.Name = "Fares";
                        for (int k = 0; k < aSeg.Fares.Length; k++)
                        {
                            Fare fare = aSeg.Fares[k];
                            JsonLibs.MyJsonLib jlf = new JsonLibs.MyJsonLib();
                            jlf.Add("FareSellKey", fare.FareSellKey);
                            jlf.Add("ProductClass", fare.ProductClass);
                            //jlf.Add("TravelClassCode", fare.TravelClassCode);
                            jlf.Add("State", fare.State.ToString());
                            jlf.Add("InboundOutbound", fare.InboundOutbound.ToString());
                            jlf.Add("CarrierCode", fare.CarrierCode);
                            jlf.Add("RuleNumber", fare.RuleNumber);
                            jlf.Add("ClassOfService", fare.ClassOfService);

                            foreach (PaxFare paxfr in fare.PaxFares)
                            {
                                jlf.AddArrayItem("PaxFares", getPaxFare(paxfr));
                            }
                            ja.Add(jlf);
                        }

                        aSegments.Add("Fares", ja);
                        aSegments.Add("ActionStatusCode", aSeg.ActionStatusCode);
                        aSegments.Add("ArrivalStation", aSeg.ArrivalStation);
                        aSegments.Add("DepartureStation", aSeg.DepartureStation);
                        aSegments.Add("CabinOfService", aSeg.CabinOfService);
                        aSegments.Add("ChangeReasonCode", aSeg.ChangeReasonCode);
                        aSegments.Add("ChannelType", aSeg.ChannelType.ToString());
                        aSegments.Add("CarrierCode", aSeg.FlightDesignator.CarrierCode);
                        aSegments.Add("FlightNumber", aSeg.FlightDesignator.FlightNumber);
                        aSegments.Add("State", aSeg.State.ToString());
                        aSegments.Add("STA", aSeg.STA.ToString("yyyy-MM-dd HH:mm:ss"));
                        aSegments.Add("STD", aSeg.STD.ToString("yyyy-MM-dd HH:mm:ss"));
                        aSegments.Add("SalesDate", aSeg.SalesDate.ToString("yyyy-MM-dd HH:mm:ss"));
                        aSegments.Add("SegmentSellKey", aSeg.SegmentSellKey);
                        aSegments.Add("SegmentType", aSeg.SegmentType);

                        PaxSSR[] paxSSRs = aSeg.PaxSSRs;
                        if (paxSSRs != null)
                        {
                            JsonLibs.MyJsonArray paxSSRArr = new JsonLibs.MyJsonArray();
                            for (int l = 0; l < paxSSRs.Length; l++)
                            {
                                JsonLibs.MyJsonLib aPaxSSRjs = new JsonLibs.MyJsonLib();
                                PaxSSR aPaxSSR = paxSSRs[l];
                                aPaxSSRjs.Add("State", aPaxSSR.State.ToString());
                                aPaxSSRjs.Add("ActionStatusCode", aPaxSSR.ActionStatusCode);
                                aPaxSSRjs.Add("ArrivalStation", aPaxSSR.ArrivalStation);
                                aPaxSSRjs.Add("DepartureStation", aPaxSSR.DepartureStation);
                                aPaxSSRjs.Add("PassengerNumber", (int)aPaxSSR.PassengerNumber);
                                aPaxSSRjs.Add("SSRCode", aPaxSSR.SSRCode);
                                aPaxSSRjs.Add("SSRNumber", (int)aPaxSSR.SSRNumber);
                                //aPaxSSRjs.Add("SSRDetail",aPaxSSR.SSRDetail);
                                //aPaxSSR.FeeCode = String.Empty;
                                //aPaxSSR.Note = String.Empty;
                                aPaxSSRjs.Add("SSRValue", (int)aPaxSSR.SSRValue);

                                paxSSRArr.Add(aPaxSSRjs);
                            }
                            aSegments.Add("PaxSSRs", paxSSRArr);
                        }
                        else
                            aSegments.Add("PaxSSRs", null);

                        Segments.Add(aSegments);
                    }
                    Journey.Add("Segments", Segments);

                    Journey.Add("JourneySellKey", booking.Journeys[i].JourneySellKey);
                    journeys.Add(Journey);
                }
                getBookingJson.Add("Journeys", journeys);
                getBookingJson.Add("NumericRecordLocator", booking.NumericRecordLocator);
                getBookingJson.Add("PaxCount", (int)booking.PaxCount);

                JsonLibs.MyJsonArray passengers = new JsonLibs.MyJsonArray();
                JsonLibs.MyJsonArray passengersINF = new JsonLibs.MyJsonArray();
                if (booking.Passengers != null)
                {
                    getPassengersJson(booking.Passengers, ref passengers, ref passengersINF);
                }
                getBookingJson.Add("Passengers", passengers);
                getBookingJson.Add("PassengersINF", passengersINF);

                JsonLibs.MyJsonArray payments = new JsonLibs.MyJsonArray();
                for(int i = 0; i<booking.Payments.Length;i++)
                {
                    JsonLibs.MyJsonLib aPayment = new JsonLibs.MyJsonLib();
                    aPayment.Add("State", booking.Payments[i].State.ToString());
                    aPayment.Add("Status", booking.Payments[i].Status.ToString());
                    aPayment.Add("AccountNumber", booking.Payments[i].AccountNumber);
                    aPayment.Add("AccountNumberID", booking.Payments[i].AccountNumberID);
                    aPayment.Add("AuthorizationStatus", booking.Payments[i].AuthorizationStatus.ToString());
                    payments.Add(aPayment);
                }
                getBookingJson.Add("Payments", payments);

                JsonLibs.MyJsonLib pos = new JsonLibs.MyJsonLib();
                pos.Add("AgentCode",booking.POS.AgentCode);
                pos.Add("DomainCode",booking.POS.DomainCode);
                pos.Add("LocationCode",booking.POS.LocationCode);
                pos.Add("State",booking.POS.State.ToString());

                getBookingJson.Add("POS", pos);

                getBookingJson.Add("RecordLocator", booking.RecordLocator);

                JsonLibs.MyJsonLib sourcePos = new JsonLibs.MyJsonLib();
                sourcePos.Add("AgentCode",booking.SourcePOS.AgentCode);
                sourcePos.Add("DomainCode",booking.SourcePOS.DomainCode);
                sourcePos.Add("LocationCode",booking.SourcePOS.LocationCode);
                sourcePos.Add("State",booking.SourcePOS.State.ToString());

                getBookingJson.Add("SourcePOS", sourcePos);
                getBookingJson.Add("State", booking.State.ToString());
                getBookingJson.Add("SystemCode", booking.SystemCode);

                //                getBookingJson.Add("", booking.BookingContacts[0].);
                getBookingJson.Add("BookingChangeCode", booking.BookingChangeCode);

                Logoff();
            }
            catch (Exception ex)
            {
                isError = true;
                this.lastErrorMessage = ex.Message; // = "Bad Data.\r\n" jika salah signature
                //Console.WriteLine(ex.Message);
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink get booking \r\n" + ex.getCompleteErrMsg());
                //getBookingJson.Add("fiResponseCode", "99");
                getBookingJson.Add("fiInternalErrorMessage", ex.Message);
                try
                {
                    CloseSession();
                }
                catch { }
            }

            return getBookingJson;
        }

        public JsonLibs.MyJsonLib getBooking(
                string recordLocator,
                string clientSignature, ref bool isError)
        {
            isError = false;
            JsonLibs.MyJsonLib getBookingJson = new JsonLibs.MyJsonLib();
            agentSignature = clientSignature;
            if (clientSignature != "") isLoggedIn = true;

            if (!isSessionOpened)
            {
                if (!OpenSession())
                {
                    isError = true;
                    this.lastErrorMessage = "Can not open session";
                    getBookingJson.Add("fiResponseCode", "78");
                    getBookingJson.Add("fiResponseMessage", "Can not open session for get booking");
                    LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink get booking failed \r\nCan not open session");
                    return getBookingJson;
                }
            }

            try
            {
                // get booking
                GetByRecordLocator getByRecordLocator = new GetByRecordLocator();
                getByRecordLocator.RecordLocator = recordLocator;

                GetBookingRequestData getBookingReqData = new GetBookingRequestData();
                getBookingReqData.GetByRecordLocator = getByRecordLocator;
                getBookingReqData.GetBookingBy = GetBookingBy.RecordLocator;

                GetBookingRequest GetBookingReq = new GetBookingRequest();
                GetBookingReq.ContractVersion = 0;
                GetBookingReq.Signature = agentSignature;
                GetBookingReq.GetBookingReqData = getBookingReqData;

                IBookingManager bookingAPI = new BookingManagerClient();
                GetBookingResponse getBookingResp = bookingAPI.GetBooking(GetBookingReq);

                Booking booking = getBookingResp.Booking;
                BookingContact bk = booking.BookingContacts[0];
                JsonLibs.MyJsonLib aContact = new JsonLibs.MyJsonLib();
                aContact.Add("AddressLine1", bk.AddressLine1);
                aContact.Add("AddressLine2", bk.AddressLine2);
                aContact.Add("AddressLine3", bk.AddressLine3);
                aContact.Add("City", bk.City);
                aContact.Add("CustomerNumber", bk.CustomerNumber);
                aContact.Add("EmailAddress", bk.EmailAddress);
                aContact.Add("FirstName", bk.Names[0].FirstName);
                aContact.Add("LastName", bk.Names[0].LastName);
                aContact.Add("MiddleName", bk.Names[0].MiddleName);
                aContact.Add("Title", bk.Names[0].Title);
                aContact.Add("HomePhone", bk.HomePhone);
                aContact.Add("OtherPhone", bk.OtherPhone);
                aContact.Add("ProvinceState", bk.ProvinceState);

                JsonLibs.MyJsonLib bookingInfo = new JsonLibs.MyJsonLib();
                bookingInfo.Add("BookingDate", booking.BookingInfo.BookingDate.ToString("yyyy-MM-dd HH:mm:ss"));
                bookingInfo.Add("BookingStatus", booking.BookingInfo.BookingStatus.ToString());
                bookingInfo.Add("ExpiredDate", booking.BookingInfo.ExpiredDate.ToString("yyyy-MM-dd HH:mm:ss"));
                bookingInfo.Add("PaidStatus", booking.BookingInfo.PaidStatus.ToString());

                getBookingJson.Add("ContactPerson", aContact);
                getBookingJson.Add("BookingInfo", bookingInfo);

                getBookingJson.Add("BookingID", booking.BookingID);

                JsonLibs.MyJsonArray journeys = new JsonLibs.MyJsonArray();
                for (int i = 0; i < booking.Journeys.Length; i++)
                {
                    JsonLibs.MyJsonLib Journey = new JsonLibs.MyJsonLib();
                    JsonLibs.MyJsonArray Segments = new JsonLibs.MyJsonArray();
                    for (int j = 0; j < booking.Journeys[i].Segments.Length; j++)
                    {
                        JsonLibs.MyJsonLib aSegments = new JsonLibs.MyJsonLib();
                        Segment aSeg = booking.Journeys[i].Segments[j];
                        aSegments.Add("ArrivalStation", aSeg.ArrivalStation);
                        aSegments.Add("DepartureStation", aSeg.DepartureStation);
                        aSegments.Add("CarrierCode", aSeg.FlightDesignator.CarrierCode);
                        aSegments.Add("FlightNumber", aSeg.FlightDesignator.FlightNumber);
                        aSegments.Add("STA", aSeg.STA.ToString("yyyy-MM-dd HH:mm:ss"));
                        aSegments.Add("STD", aSeg.STD.ToString("yyyy-MM-dd HH:mm:ss"));
                        aSegments.Add("SalesDate", aSeg.SalesDate.ToString("yyyy-MM-dd HH:mm:ss"));
                        //aSegments.Add("SegmentSellKey", aSeg.SegmentSellKey);

                        Segments.Add(aSegments);
                    }
                    Journey.Add("Segments", Segments);
                    journeys.Add(Journey);
                }
                getBookingJson.Add("Journeys", journeys);
                getBookingJson.Add("NumericRecordLocator", booking.NumericRecordLocator);
                //getBookingJson.Add("PaxCount", (int)booking.PaxCount);

                JsonLibs.MyJsonArray passengers = new JsonLibs.MyJsonArray();
                JsonLibs.MyJsonArray passengersINF = new JsonLibs.MyJsonArray();
                if (booking.Passengers != null)
                {
                    getPassengersJson(booking.Passengers, ref passengers, ref passengersINF);
                }
                getBookingJson.Add("Passengers", passengers);
                getBookingJson.Add("PassengersINF", passengersINF);

                getBookingJson.Add("PaymentStatus", booking.Payments[0].Status.ToString());

                getBookingJson.Add("RecordLocator", booking.RecordLocator);

                //Logoff();
            }
            catch (Exception ex)
            {
                isError = true;
                this.lastErrorMessage = ex.Message; // = "Bad Data.\r\n" jika salah signature
                //Console.WriteLine(ex.Message);
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Citilink get booking \r\n" + ex.getCompleteErrMsg());
                //getBookingJson.Add("fiResponseCode", "99");
                getBookingJson.Add("fiInternalErrorMessage", ex.Message);
                //try
                //{
                //    CloseSession();
                //}
                //catch { }
            }

            return getBookingJson;
        }

    }
}
