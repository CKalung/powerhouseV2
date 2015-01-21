using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSXML2;
using CitilinkLib.CitilinkBookingManager;
using CitilinkLib.CitilinkSessionManager;
using LOG_Handler;
using StaticCommonLibrary;

namespace CitilinkLib
{
	public class CitilinkProcs: IDisposable
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
		~CitilinkProcs()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			CloseSession();
		}

        //private JsonLibs.MyJsonLib jsonH = new JsonLibs.MyJsonLib();
        private int contractVersion = 0; // pix heula
        private string agentName = "api_dam";
        private string agentPass = "@pi_Dam1213";
        private string agentDomain = "EXT";
        private string agentSignature = "";

        public string _journeySellKey = "";
        public string _fareSellKey = "";
        public decimal _Amount = 0;
        public string _currencyCode = "";
        public string _carrierCode = "";
        public string _flighNumber = "";

        public string _depatureStation = "";
        public string _arrivalStation = "";
        public DateTime _STD;
        public DateTime _STA;

        // 1. Logon
        private SessionManagerClient clientManager;
        private IBookingManager bookingAPI;

        // 2. init GetAvailability        
        private GetAvailabilityRequest requestAvailability;
        private GetAvailabilityResponse responseAvailability;

        // 3. init GetItineraryPrice        
		//PriceItineraryRequest priceItinRequest = new PriceItineraryRequest();

        // 4. init Sell reuqest
        private SellRequest sellRequest;
		private SellResponse sellResponse;

        // 5. sell request infant (bayi)
        // pakai SSR
        // ke skip heula

        // 6. init update pessenger
        private UpdatePassengersRequest updatePassengersRequest;
		private UpdatePassengersResponse updatePassengersResponse;

        //private JSONHandler jh;

		public CitilinkProcs()
		{
		}
		public CitilinkProcs(string AgentName, string DomainCode, string Password, int ContractVersion)
        {
			contractVersion = ContractVersion;
			agentName = AgentName;
			agentDomain = DomainCode;
			agentPass = Password;
        }

        public bool OpenSession()
        {
            try
            {
                clientManager = new SessionManagerClient();
                return true;
            }
            catch (Exception ExError)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, "Failed on Open Session to Citilink \r\n" + 
                    "\r\nResult: " + ExError.Message + ", at line: " + ExError.LineNumber().ToString());
                return false;
            }
        }

        public void CloseSession()
        {
            try
            {
                clientManager.Close();
            }
            catch { }
        }

        public string Logon() 
        {
            agentSignature = "";
            try
            {                
                LogonRequestData logOnReqData = new LogonRequestData();                
                logOnReqData.DomainCode = agentDomain;
                logOnReqData.AgentName = agentName;
                logOnReqData.Password = agentPass;
                agentSignature = clientManager.Logon(contractVersion, logOnReqData);                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            //return agentSignature;
            string strJson = "";
            using (JsonLibs.MyJsonLib jsonH = new JsonLibs.MyJsonLib())
            {
                jsonH.Clear();
                jsonH.Add("Signature", agentSignature);
                strJson = jsonH.JSONConstruct();
            }
            return strJson;
        }

        private JsonLibs.MyJsonArray getTrip(JourneyDateMarket[][] jdm, int idi)
        {
            JsonLibs.MyJsonArray ja = new JsonLibs.MyJsonArray();
            ja.Name = "Trip2";
            //for (int j = 0; j < responseAvailability.GetTripAvailabilityResponse.Schedules[idx].Length; j++)
            for (int j = 0; j < jdm[idi].Length; j++)
            {
                ja.Add(getSchedule(jdm, idi, j));
            }
            return ja;
        }
        private JsonLibs.MyJsonLib getSchedule(JourneyDateMarket[][] jdm, int idi, int idj)
        {
            JsonLibs.MyJsonLib jl = new JsonLibs.MyJsonLib();

            //Console.WriteLine("DepartureDate[" + i + "][" + j + "] = " + jdm[i][j].DepartureDate.ToString());
            jl.Add("DepartureDate", jdm[idi][idj].DepartureDate.ToString("yyyy-MM-dd HH:mm:ss"));
            //Console.WriteLine("DepartureStation[" + i + "][" + j + "] = " + jdm[i][j].DepartureStation.ToString());
            jl.Add("DepartureStation", jdm[idi][idj].DepartureStation);
            //Console.WriteLine("ArrivalStation[" + i + "][" + j + "] = " + jdm[i][j].ArrivalStation.ToString());
            jl.Add("ArrivalStation", jdm[idi][idj].ArrivalStation);

            jl.Add("IncludesTaxesAndFees", jdm[idi][idj].IncludesTaxesAndFees);

            //Console.WriteLine("Journeys[" + i + "][" + j + "] = " + jdm[i][j].Journeys); // array object
            int jLen = jdm[idi][idj].Journeys.Length;
            Console.WriteLine("Jumlah Journey = " + jLen);

            JsonLibs.MyJsonArray ja = new JsonLibs.MyJsonArray();
            ja.Name = "Journey";

            for (int k = 0; k < jLen; k++)
            {
                ja.Add(getJourney(jdm, idi, idj, k));
            }
            jl.Add("Journey", ja);

            return jl;
        }

        private JsonLibs.MyJsonLib getJourney(JourneyDateMarket[][] jdm, int idi, int idj, int idk)
        {
            JsonLibs.MyJsonLib jl = new JsonLibs.MyJsonLib();

            Journey jor = new Journey();
            jor.JourneySellKey = jdm[idi][idj].Journeys[idk].JourneySellKey;
            if (_journeySellKey == "")
            {
                //Console.WriteLine("jdm[" + i + "][" + j + "].Journeys[" + k + "].JourneySellKey = " + jdm[i][j].Journeys[k].JourneySellKey);
                _journeySellKey = jdm[idi][idj].Journeys[idk].JourneySellKey.ToString(); // ambil journeyKey terakhir buat testing                                    
                jl.Add("JourneySellKey", _journeySellKey);
            }

            jor.NotForGeneralUse = jdm[idi][idj].Journeys[idk].NotForGeneralUse;
            jl.Add("NotForGeneralUse", jor.NotForGeneralUse);
            jor.State = jdm[idi][idj].Journeys[idk].State;
            jl.Add("State", jor.State.ToString());

            int sLen = jdm[idi][idj].Journeys[idk].Segments.Length;

            JsonLibs.MyJsonArray ja = new JsonLibs.MyJsonArray();
            ja.Name = "Segment";

            for (int l = 0; l < sLen; l++)
            {
                ja.Add(getSegment(jdm, idi, idj, idk, l));
            }
            jl.Add("Segment", ja);

            return jl;
        }

        private JsonLibs.MyJsonLib getSegment(JourneyDateMarket[][] jdm, int idi, int idj, int idk, int idl)
        {
            JsonLibs.MyJsonLib jl = new JsonLibs.MyJsonLib();
            Segment seg = new Segment();

            seg.ActionStatusCode = jdm[idi][idj].Journeys[idk].Segments[idl].ActionStatusCode;
            seg.ArrivalStation = jdm[idi][idj].Journeys[idk].Segments[idl].ArrivalStation;
            seg.CabinOfService = jdm[idi][idj].Journeys[idk].Segments[idl].CabinOfService;
            seg.ChangeReasonCode = jdm[idi][idj].Journeys[idk].Segments[idl].ChangeReasonCode;
            seg.ChannelType = jdm[idi][idj].Journeys[idk].Segments[idl].ChannelType;
            seg.DepartureStation = jdm[idi][idj].Journeys[idk].Segments[idl].DepartureStation;

            jl.Add("ActionStatusCode", seg.ActionStatusCode);
            jl.Add("ArrivalStation", seg.ArrivalStation);
            jl.Add("CabinOfService", seg.CabinOfService);
            jl.Add("ChangeReasonCode", seg.ChangeReasonCode);
            jl.Add("ChannelType", (int)seg.ChannelType);
            jl.Add("DepartureStation", seg.DepartureStation);

            _depatureStation = jdm[idi][idj].Journeys[idk].Segments[idl].DepartureStation;
            Console.WriteLine("jdm[idi][idj].Journeys[idk].Segments[idl].DepartureStation = " + _depatureStation);

            _arrivalStation = jdm[idi][idj].Journeys[idk].Segments[idl].ArrivalStation;
            Console.WriteLine("jdm[idi][idj].Journeys[idk].Segments[idl].ArrivalStation = " + _arrivalStation);

            _STA = jdm[idi][idj].Journeys[idk].Segments[idl].STA;
            Console.WriteLine("jdm[idi][idj].Journeys[idk].Segments[idl].STA = " + _STA.ToString());

            _STD = jdm[idi][idj].Journeys[idk].Segments[idl].STD;
            Console.WriteLine("jdm[idi][idj].Journeys[idk].Segments[idl].STD = " + _STD.ToString());

            jl.Add("STD", _STD.ToString("yyyy-MM-dd HH-mm-ss"));
            jl.Add("STA", _STA.ToString("yyyy-MM-dd HH-mm-ss"));

            if (_carrierCode == "")
            {
                seg.FlightDesignator = jdm[idi][idj].Journeys[idk].Segments[idl].FlightDesignator;
                _carrierCode = jdm[idi][idj].Journeys[idk].Segments[idl].FlightDesignator.CarrierCode; // ambil carrier code terakhir buat testting doang
                Console.WriteLine("jdm[idi][idj].Journeys[idk].Segments[idl].FlightDesignator.CarrierCode = " + jdm[idi][idj].Journeys[idk].Segments[idl].FlightDesignator.CarrierCode);

                _flighNumber = jdm[idi][idj].Journeys[idk].Segments[idl].FlightDesignator.FlightNumber; // ambil fligh number terakhir buat testting doang
                Console.WriteLine("jdm[idi][idj].Journeys[idk].Segments[idl].FlightDesignator.FlightNumber = " + jdm[idi][idj].Journeys[idk].Segments[idl].FlightDesignator.FlightNumber);

                jl.Add("CarrierCode", _carrierCode);
                jl.Add("FlightNumber", _flighNumber);
            }

            seg.Fares = jdm[idi][idj].Journeys[idk].Segments[idl].Fares; // object array deui buset
            //if (_fareSellKey == "")
            //if(seg.Fares.Length>0)
            //{
            JsonLibs.MyJsonArray ja = new JsonLibs.MyJsonArray();
            ja.Name = "Fares";
            for (int a = 0; a < seg.Fares.Length; a++)
            {
                JsonLibs.MyJsonLib jlm = new JsonLibs.MyJsonLib();

                //if (_fareSellKey == "")
                //{
                _fareSellKey = seg.Fares[a].FareSellKey.ToString(); // ambil fareKey terakhir buat testing
                Console.WriteLine("jdm[" + idi + "][" + idj + "].Journeys[" + idk + "].Segments[" + idl + "].Fares[" + a + "].FareSellKey = " + seg.Fares[a].FareSellKey.ToString());

                _Amount = seg.Fares[a].PaxFares[0].ServiceCharges[0].Amount;
                Console.WriteLine("jdm[" + idi + "][" + idj + "].Journeys[" + idk + "].Segments[" + idl + "].Fares[" + a + "].Amount = " + _Amount.ToString());

                _currencyCode = seg.Fares[a].PaxFares[0].ServiceCharges[0].CurrencyCode;
                Console.WriteLine("jdm[" + idi + "][" + idj + "].Journeys[" + idk + "].Segments[" + idl + "].Fares[" + a + "].CurrencyCode = " + _currencyCode.ToString());

                jlm.Add("ClassOfService", seg.Fares[a].ClassOfService);
                jlm.Add("FareStatus", seg.Fares[a].FareStatus.ToString());
                jlm.Add("FareSellKey", _fareSellKey);
                jlm.Add("Amount", _Amount);
                jlm.Add("CurrencyCode", _currencyCode);
                jlm.Add("FareBasisCode", seg.Fares[a].FareBasisCode);
                jlm.Add("State", seg.Fares[a].State.ToString());
                jlm.Add("ClassType", seg.Fares[a].ClassType);
                jlm.Add("CarrierCode", seg.Fares[a].CarrierCode);

                JsonLibs.MyJsonArray jap = new JsonLibs.MyJsonArray();
                jap.Name = "PaxFares";
                for (int b = 0; b < seg.Fares[a].PaxFares.Length; b++)
                {
                    JsonLibs.MyJsonLib jlp = new JsonLibs.MyJsonLib();
                    PaxFare pf = seg.Fares[a].PaxFares[b];

                    jlp.Add("FareDiscountCode", pf.FareDiscountCode);
                    jlp.Add("PaxDiscountCode", pf.PaxDiscountCode);
                    jlp.Add("PaxType", pf.PaxType);
                    jlp.Add("State", pf.State.ToString());
                    //if (b == 0)
                    //{
                    JsonLibs.MyJsonArray jas = new JsonLibs.MyJsonArray();
                    jas.Name = "ServiceCharges";
                    for (int c = 0; c < pf.ServiceCharges.Length; c++)
                    {
                        JsonLibs.MyJsonLib jlsc = new JsonLibs.MyJsonLib();
                        BookingServiceCharge svChr = pf.ServiceCharges[c];
                        jlsc.Add("Amount", svChr.Amount);
                        jlsc.Add("ChargeType", svChr.ChargeType.ToString());
                        jlsc.Add("ChargeDetail", svChr.ChargeDetail);
                        jlsc.Add("CurrencyCode", svChr.CurrencyCode);
                        jlsc.Add("TicketCode", svChr.TicketCode);
                        jlsc.Add("ForeignAmount", svChr.ForeignAmount);
                        jlsc.Add("ForeignCurrencyCode", svChr.ForeignCurrencyCode);
                        jlsc.Add("State", svChr.State.ToString());

                        jas.Add(jlsc);
                    }
                    jlp.Add("ServiceCharges", jas);
                    //}
                    //else if(b==1)
                    //{

                    //}

                    jap.Add(jlp);
                }
                //}
                jlm.Add("PaxFares", jap);

                ja.Add(jlm);
                //}
            }
            jl.Add("Fares", ja);

            seg.International = jdm[idi][idj].Journeys[idk].Segments[idl].International;
            seg.Legs = jdm[idi][idj].Journeys[idk].Segments[idl].Legs;// object array deui buset
            seg.PaxBags = jdm[idi][idj].Journeys[idk].Segments[idl].PaxBags;// object array deui buset
            seg.PaxScores = jdm[idi][idj].Journeys[idk].Segments[idl].PaxScores;// object array deui buset
            seg.PaxSeatPreferences = jdm[idi][idj].Journeys[idk].Segments[idl].PaxSeatPreferences;// object array deui buset
            seg.PaxSeats = jdm[idi][idj].Journeys[idk].Segments[idl].PaxSeats;// object array deui buset
            seg.PaxSegments = jdm[idi][idj].Journeys[idk].Segments[idl].PaxSegments;// object array deui buset
            seg.PaxSSRs = jdm[idi][idj].Journeys[idk].Segments[idl].PaxSSRs;// object array deui buset
            seg.PaxTickets = jdm[idi][idj].Journeys[idk].Segments[idl].PaxTickets;// object array deui buset
            seg.PriorityCode = jdm[idi][idj].Journeys[idk].Segments[idl].PriorityCode;
            seg.SalesDate = jdm[idi][idj].Journeys[idk].Segments[idl].SalesDate;
            seg.SegmentSellKey = jdm[idi][idj].Journeys[idk].Segments[idl].SegmentSellKey;
            seg.SegmentType = jdm[idi][idj].Journeys[idk].Segments[idl].SegmentType;
            seg.STA = jdm[idi][idj].Journeys[idk].Segments[idl].STA;
            seg.State = jdm[idi][idj].Journeys[idk].Segments[idl].State;
            seg.STD = jdm[idi][idj].Journeys[idk].Segments[idl].STD;
            seg.XrefFlightDesignator = jdm[idi][idj].Journeys[idk].Segments[idl].XrefFlightDesignator;

            jl.Add("International", seg.International);
            jl.Add("SegmentSellKey", seg.SegmentSellKey);
            jl.Add("SegmentType", seg.SegmentType);
            jl.Add("STD", seg.STD);
            jl.Add("STA", seg.STA);


            return jl;
        }

        public enum InOutbound { Inbound, Outbound,Both}
        public string GetAvailability(string departureStation, string arrivalStation, 
            DateTime departureBeginDate, DateTime departureEndDate,
            DateTime arrivalBeginDate, DateTime arrivalEndDate,
            short paxCount, string[] priceTypes, string[] paxDiscountCode, 
            InOutbound inOutBound)
        {
            string carrierCode = "QG";
            string currencyCode = "IDR";
            short maximumConnectingFlights = 0;
            int minimumFarePrice = 0;
            int maximumFarePrice = 0;
            int nightsStay = 0;
            bool includeAllotments = false;

            string json = "";
            try
            {
                //Create an instance of BookingManagerClient
                bookingAPI = new BookingManagerClient();
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
                    availabilityRequest.DepartureStations = new string[1];
                    availabilityRequest.DepartureStations[0] = arrivalStation;
                    availabilityRequest.ArrivalStations = new string[2];
                    availabilityRequest.ArrivalStations[0] = departureStation;
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
                for (int i = 0; i < paxCount; i++)
                {
                    myPriceTypes[i] = new PaxPriceType();
                    myPriceTypes[i].PaxType = priceTypes[i].ToString();
                    myPriceTypes[i].PaxDiscountCode = String.Empty;
                }
                availabilityRequest.PaxPriceTypes = myPriceTypes;
                
                requestAvailability.TripAvailabilityRequest.AvailabilityRequests[0] = availabilityRequest;
                responseAvailability = bookingAPI.GetAvailability(requestAvailability);
                
                if (responseAvailability.GetTripAvailabilityResponse.Schedules.Length > 0 && 
                    responseAvailability.GetTripAvailabilityResponse.Schedules[0].Length > 0)
                {
                    JourneyDateMarket[][] jdm = responseAvailability.GetTripAvailabilityResponse.Schedules;

                    JsonLibs.MyJsonLib jh = new JsonLibs.MyJsonLib();
                    jh.Clear();

                    jh.Add("fiResponseCode","00");
                    jh.Add("fiResponseMessage","Success");
                    // START TEST PARSING NON-JSON. nanti hapus!!!

                    for (int i = 0; i < jdm.Length; i++)
                    {
                        for (int j = 0; j < jdm[i].Length; j++)
                        {
                            jh.AddArrayItem("Schedule", getSchedule(jdm, i, j));
                        }
                    }

                    //JsonLibs.MyJsonArray ja = new JsonLibs.MyJsonArray();
                    //ja.Name = "Trip";
                    //for (int i = 0; i < jdm.Length; i++)
                    //{
                    //    ja.Add(getTrip(jdm, i));
                    //}
                    //jh.Add("Trip", ja);

                    // END TEST PARSING NON-JSON
                    
                    //jh = new JSONHandler();
                    //json = JsonConvert.SerializeObject(jdm);
                    json = jh.JSONConstruct();
                    jh.Dispose();
                }                               
                return json;                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
           
            return json;                        
        }

        //public string GetItineraryPrice(string currencyCode, short paxCount, string[] priceTypes, string[] journeySellKey, string[] fareSellKey, string[] standbyPriorityCode)
        //{
        //    string json = "";
        //    try
        //    {                                
        //        priceItinRequest.Signature = agentSignature;
        //        priceItinRequest.ContractVersion = 0;

        //        priceItinRequest.ItineraryPriceRequest = new ItineraryPriceRequest();
        //        priceItinRequest.ItineraryPriceRequest.PriceItineraryBy = PriceItineraryBy.JourneyBySellKey;

        //        priceItinRequest.ItineraryPriceRequest.PriceItineraryBy = PriceItineraryBy.JourneyWithLegs;
        //        priceItinRequest.ItineraryPriceRequest.SellByKeyRequest = new SellJourneyByKeyRequestData();
        //        //priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.ActionStatusCode = "";
        //        priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.ActionStatusCode = "";
        //        priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.CurrencyCode = currencyCode;
        //        priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.PaxCount = paxCount;

        //        // joureny key list
        //        priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.JourneySellKeys = new SellKeyList[paxCount];
        //        for (int i = 0; i < paxCount; i++)
        //        {
        //            priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.JourneySellKeys[i].JourneySellKey = journeySellKey[i];
        //            priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.JourneySellKeys[i].FareSellKey = fareSellKey[i];
        //            priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.JourneySellKeys[i].StandbyPriorityCode = standbyPriorityCode[i];
        //        }

        //        // fax Price type
        //        priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.PaxPriceType = new PaxPriceType[paxCount];
        //        for (int i = 0; i < paxCount; i++)
        //        {
        //            priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.PaxPriceType[i].PaxType = priceTypes[i];
        //            priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.PaxPriceType[i].PaxDiscountCode = String.Empty;                    
        //        }

        //        // source pos
        //        priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.SourcePOS = new PointOfSale();
        //        priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.SourcePOS.AgentCode = agentName;
        //        priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.SourcePOS.OrganizationCode = String.Empty;
        //        priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.SourcePOS.DomainCode = agentDomain;
        //        priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.SourcePOS.LocationCode = String.Empty;
        //        priceItinRequest.ItineraryPriceRequest.SellByKeyRequest.SourcePOS.State = String.Empty;

        //        // type of sale


        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //    }
        //    return json;
        //}

        public string SellRequest(string journeySellKey, string fareSellKey, short paxCount, string currencyCode, string[] priceTypes, string[] paxDiscountCode)
        {            
            string json = "";
            try
            {
                //Create an instance of BookingManagerClient
                bookingAPI = new BookingManagerClient();
                sellRequest = new SellRequest();
                sellRequest.ContractVersion = 0; // pix heula
                sellRequest.Signature = agentSignature;

                // indicate that the we are selling a journey
                sellRequest.SellRequestData = new SellRequestData();
                sellRequest.SellRequestData.SellBy = SellBy.JourneyBySellKey;

                sellRequest.SellRequestData.SellJourneyByKeyRequest = new SellJourneyByKeyRequest();
                SellJourneyByKeyRequestData sellData = new SellJourneyByKeyRequestData();
                sellData.ActionStatusCode = "NN";

                SellKeyList skl = new SellKeyList();
                skl.JourneySellKey = journeySellKey; //"QG~ 852~ ~~CGK~25/12/2013 16:25~DPS~25/12/2013 19:15~";
                skl.FareSellKey = fareSellKey; // "0~N~~N~RGFR~~1~X";
                sellData.JourneySellKeys = new SellKeyList[1];
                sellData.JourneySellKeys[0] = skl;
                sellRequest.SellRequestData.SellJourneyByKeyRequest.SellJourneyByKeyRequestData = sellData;

                PaxPriceType[] myPriceTypes = new PaxPriceType[paxCount];
                for (int i = 0; i < paxCount; i++)
                {
                    myPriceTypes[i] = new PaxPriceType();
                    myPriceTypes[i].PaxType = priceTypes[i].ToString();
                    myPriceTypes[i].PaxDiscountCode = paxDiscountCode[i];
                }

                sellData.CurrencyCode = currencyCode;
                sellData.SourcePOS = new PointOfSale();
                sellData.SourcePOS.State = MessageState.New; // fix heula new passenger
                sellData.SourcePOS.AgentCode = "api_dam";
                //sellData.SourcePOS.OrganizationCode = "0003000179";
                sellData.SourcePOS.DomainCode = "EXT";
                sellData.PaxCount = paxCount;
                sellData.LoyaltyFilter = LoyaltyFilter.MonetaryOnly;                
                sellData.IsAllotmentMarketFare = false;

                JsonLibs.MyJsonLib jh = new JsonLibs.MyJsonLib();
                jh.Clear();

				//sellResponse = bookingAPI.Sell(sellRequest);
				bookingAPI.Sell(sellRequest);

                //jh = new JSONHandler();
                //json = JsonConvert.SerializeObject(sellResponse.BookingUpdateResponseData);

                //sellResponse.BookingUpdateResponseData harus di add ke json constructor dulu
                json = jh.JSONConstruct();
                jh.Dispose();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }                        
            return json;       
        }

        // khusus buat bayi
        public string SellRequestINF(int infantCount, string currencyCode, string carrierCode, string flighNumber, DateTime sTA, DateTime sTD, string departureStation, string arrivalStation)
        {
            string json = "";

            try
            {
                //Create an instance of BookingManagerClient
                bookingAPI = new BookingManagerClient();
                sellRequest = new SellRequest();
                sellRequest.ContractVersion = 0; // pix heula
                sellRequest.Signature = agentSignature;

                // indicate that the we are selling a journey
                sellRequest.SellRequestData = new SellRequestData();
                sellRequest.SellRequestData.SellBy = SellBy.SSR;

                sellRequest.SellRequestData.SellJourneyByKeyRequest = null;
                sellRequest.SellRequestData.SellJourneyRequest = null;

                SellSSR sellData = new SellSSR();
                sellData.SSRRequest = new SSRRequest();
                sellData.SSRRequest.SegmentSSRRequests = new SegmentSSRRequest[1];
                sellData.SSRRequest.SegmentSSRRequests[0] = new SegmentSSRRequest();
                sellData.SSRRequest.SegmentSSRRequests[0].FlightDesignator = new FlightDesignator();
                sellData.SSRRequest.SegmentSSRRequests[0].FlightDesignator.CarrierCode = carrierCode;
                sellData.SSRRequest.SegmentSSRRequests[0].FlightDesignator.FlightNumber = flighNumber;
                sellData.SSRRequest.SegmentSSRRequests[0].FlightDesignator.OpSuffix = String.Empty;

                sellData.SSRRequest.SegmentSSRRequests[0].STD = sTD;
                sellData.SSRRequest.SegmentSSRRequests[0].DepartureStation = departureStation;
                sellData.SSRRequest.SegmentSSRRequests[0].ArrivalStation = arrivalStation;

                sellData.SSRRequest.SegmentSSRRequests[0].PaxSSRs = new PaxSSR[infantCount];
                for (int i = 0; i < infantCount; i++)
                {
                    sellData.SSRRequest.SegmentSSRRequests[0].PaxSSRs[i] = new PaxSSR();
                    sellData.SSRRequest.SegmentSSRRequests[0].PaxSSRs[i].State = MessageState.New; // fix heula new passenger
                    sellData.SSRRequest.SegmentSSRRequests[0].PaxSSRs[i].ActionStatusCode = "NN";
                    sellData.SSRRequest.SegmentSSRRequests[0].PaxSSRs[i].ArrivalStation = arrivalStation;
                    sellData.SSRRequest.SegmentSSRRequests[0].PaxSSRs[i].DepartureStation = departureStation;
                    sellData.SSRRequest.SegmentSSRRequests[0].PaxSSRs[i].PassengerNumber = (short)i;
                    sellData.SSRRequest.SegmentSSRRequests[0].PaxSSRs[i].SSRCode = "INF";
                    sellData.SSRRequest.SegmentSSRRequests[0].PaxSSRs[i].SSRNumber = (short)i;
                    sellData.SSRRequest.SegmentSSRRequests[0].PaxSSRs[i].SSRDetail = String.Empty;
                    sellData.SSRRequest.SegmentSSRRequests[0].PaxSSRs[i].FeeCode = String.Empty;
                    sellData.SSRRequest.SegmentSSRRequests[0].PaxSSRs[i].Note = String.Empty;
                    sellData.SSRRequest.SegmentSSRRequests[0].PaxSSRs[i].SSRValue = (short)i;
                }


                sellData.SSRRequest.CurrencyCode = currencyCode;
                sellData.SSRRequest.CancelFirstSSR = false;
                sellData.SSRRequest.SSRFeeForceWaiveOnSell = false;
                sellRequest.SellRequestData.SellFee = null;

                sellRequest.SellRequestData.SellSSR = sellData;

                sellResponse = bookingAPI.Sell(sellRequest);

                JsonLibs.MyJsonLib jh = new JsonLibs.MyJsonLib();
                jh.Clear();

                //jh = new JSONHandler();
                //json = JsonConvert.SerializeObject(sellResponse.BookingUpdateResponseData);

                //sellResponse.BookingUpdateResponseData harus di add ke json constructor dulu
                json = jh.JSONConstruct();
                jh.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            //try
            //{
            //    //Create an instance of BookingManagerClient
            //    bookingAPI = new BookingManagerClient();
            //    sellRequest = new SellRequest();
            //    sellRequest.ContractVersion = 0; // pix heula
            //    sellRequest.Signature = agentSignature;

            //    // indicate that the we are selling a journey
            //    sellRequest.SellRequestData = new SellRequestData();
            //    sellRequest.SellRequestData.SellBy = SellBy.SSR;

            //    sellRequest.SellRequestData.SellJourneyByKeyRequest = null;
            //    sellRequest.SellRequestData.SellJourneyRequest = null;

            //    SellSSR sellData = new SellSSR();
            //    sellData.SSRRequest = new SSRRequest();
            //    sellData.SSRRequest.SegmentSSRRequests = new SegmentSSRRequest[infantCount];
            //    for (int i = 0; i < infantCount; i++)
            //    {
            //        sellData.SSRRequest.SegmentSSRRequests[i] = new SegmentSSRRequest();
            //        sellData.SSRRequest.SegmentSSRRequests[i].FlightDesignator = new FlightDesignator();
            //        sellData.SSRRequest.SegmentSSRRequests[i].FlightDesignator.CarrierCode = carrierCode;
            //        sellData.SSRRequest.SegmentSSRRequests[i].FlightDesignator.FlightNumber = flighNumber;
            //        sellData.SSRRequest.SegmentSSRRequests[i].FlightDesignator.OpSuffix = String.Empty;

            //        sellData.SSRRequest.SegmentSSRRequests[i].STD = sTD;
            //        sellData.SSRRequest.SegmentSSRRequests[i].DepartureStation = departureStation;
            //        sellData.SSRRequest.SegmentSSRRequests[i].ArrivalStation = arrivalStation;

            //        sellData.SSRRequest.SegmentSSRRequests[i].PaxSSRs = new PaxSSR[1];
            //        sellData.SSRRequest.SegmentSSRRequests[i].PaxSSRs[0] = new PaxSSR();
            //        sellData.SSRRequest.SegmentSSRRequests[i].PaxSSRs[0].State = MessageState.New; // fix heula new passenger
            //        sellData.SSRRequest.SegmentSSRRequests[i].PaxSSRs[0].ActionStatusCode = "NN";
            //        sellData.SSRRequest.SegmentSSRRequests[i].PaxSSRs[0].ArrivalStation = arrivalStation;
            //        sellData.SSRRequest.SegmentSSRRequests[i].PaxSSRs[0].DepartureStation = departureStation;
            //        sellData.SSRRequest.SegmentSSRRequests[i].PaxSSRs[0].PassengerNumber = 0;
            //        sellData.SSRRequest.SegmentSSRRequests[i].PaxSSRs[0].SSRCode = "INF";
            //        sellData.SSRRequest.SegmentSSRRequests[i].PaxSSRs[0].SSRNumber = (short)i;
            //        sellData.SSRRequest.SegmentSSRRequests[i].PaxSSRs[0].SSRDetail = String.Empty;
            //        sellData.SSRRequest.SegmentSSRRequests[i].PaxSSRs[0].FeeCode = String.Empty;
            //        sellData.SSRRequest.SegmentSSRRequests[i].PaxSSRs[0].Note = String.Empty;
            //        sellData.SSRRequest.SegmentSSRRequests[i].PaxSSRs[0].SSRValue = 0;
            //    }

            //    sellData.SSRRequest.CurrencyCode = currencyCode;
            //    sellData.SSRRequest.CancelFirstSSR = false;
            //    sellData.SSRRequest.SSRFeeForceWaiveOnSell = false;
            //    sellRequest.SellRequestData.SellFee = null;

            //    sellRequest.SellRequestData.SellSSR = sellData;

            //    sellResponse = bookingAPI.Sell(sellRequest);
            //    jh = new JSONHandler();
            //    json = JsonConvert.SerializeObject(sellResponse.BookingUpdateResponseData);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}
            return json;
        }

        public string UpdatePassengers(short paxCount, string currencyCode, 
            string[] passengerFirstNames,
            string[] passengerMiddleNames,
            string[] passengerLastNames,
            string[] passengerSuffix,
            string[] passengerTitle,
            string[] passengerGender,
            string[] passengerWightType,
            string[] passengerDOB,
            string[] passengerNationality,
            string[] passengerPaxTypes,
            bool[] passengerIsInfant,
            string[] passengerInfantFirstNames,
            string[] passengerInfantMiddleNames,
            string[] passengerInfantLastNames,
            string[] passengerInfantSuffix,
            string[] passengerInfantTitle,
            string[] passengerInfantGender,
            string[] passengerInfantWightType,
            string[] passengerInfantDOB,
            string[] passengerInfantNationality
            
            )
        {
            string json = "";
            try
            {
                JsonLibs.MyJsonLib jh = new JsonLibs.MyJsonLib();
                jh.Clear();

                //Create an instance of BookingManagerClient
                bookingAPI = new BookingManagerClient();
                updatePassengersRequest = new UpdatePassengersRequest();
                updatePassengersRequest.ContractVersion = 0; // pix heula
                updatePassengersRequest.Signature = agentSignature;
                
                updatePassengersRequest.updatePassengersRequestData = new UpdatePassengersRequestData();
                updatePassengersRequest.updatePassengersRequestData.Passengers = new Passenger[paxCount];

                for (int i = 0; i < paxCount; i++)
                {
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i] = new Passenger();
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerPrograms = null;
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].CustomerNumber = String.Empty;
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerNumber = (short)i;
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].FamilyNumber = 0;
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PaxDiscountCode = String.Empty;

                    // data nama passenger
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].Names = new BookingName[1];
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].Names[0] = new BookingName();
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].Names[0].FirstName = passengerFirstNames[i];
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].Names[0].MiddleName = passengerMiddleNames[i];
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].Names[0].LastName = passengerLastNames[i];
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].Names[0].Suffix = passengerSuffix[i];
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].Names[0].Title = passengerTitle[i];

                    if (passengerIsInfant != null)
                    {
                        if (passengerIsInfant[i])
                        {
                            updatePassengersRequest.updatePassengersRequestData.Passengers[i].Infant = new PassengerInfant();
                            updatePassengersRequest.updatePassengersRequestData.Passengers[i].Infant.DOB = DateTime.Parse(passengerInfantDOB[i]); //DateTime.Now;
                            if (passengerInfantGender[i] == "Male")
                                updatePassengersRequest.updatePassengersRequestData.Passengers[i].Infant.Gender = Gender.Male;
                            else if (passengerInfantGender[i] == "Female")
                                updatePassengersRequest.updatePassengersRequestData.Passengers[i].Infant.Gender = Gender.Female;
                            else
                                updatePassengersRequest.updatePassengersRequestData.Passengers[i].Infant.Gender = Gender.Unmapped;

                            updatePassengersRequest.updatePassengersRequestData.Passengers[i].Infant.Nationality = passengerInfantNationality[i];
                            updatePassengersRequest.updatePassengersRequestData.Passengers[i].Infant.ResidentCountry = String.Empty;
                            // data nama passenger bayi
                            updatePassengersRequest.updatePassengersRequestData.Passengers[i].Infant.Names = new BookingName[1];
                            updatePassengersRequest.updatePassengersRequestData.Passengers[i].Infant.Names[0] = new BookingName();
                            updatePassengersRequest.updatePassengersRequestData.Passengers[i].Infant.Names[0].FirstName = passengerInfantFirstNames[i];
                            updatePassengersRequest.updatePassengersRequestData.Passengers[i].Infant.Names[0].MiddleName = passengerInfantMiddleNames[i];
                            updatePassengersRequest.updatePassengersRequestData.Passengers[i].Infant.Names[0].LastName = passengerInfantLastNames[i];
                            updatePassengersRequest.updatePassengersRequestData.Passengers[i].Infant.Names[0].Suffix = passengerInfantSuffix[i];
                            updatePassengersRequest.updatePassengersRequestData.Passengers[i].Infant.Names[0].Title = passengerInfantTitle[i];
                        }
                    }

                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfo = new PassengerInfo();
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfo.BalanceDue = 0;
                    if(passengerGender[i] == "Male")
                        updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfo.Gender = Gender.Male;
                    else if (passengerGender[i] == "Female")
                        updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfo.Gender = Gender.Female;
                    else
                        updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfo.Gender = Gender.Unmapped;

                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfo.Nationality = passengerNationality[i];
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfo.ResidentCountry = String.Empty;
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfo.TotalCost = 0;

                    if (passengerWightType[i] == "Male")
                        updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfo.WeightCategory = WeightCategory.Male;
                    else if (passengerWightType[i] == "Female")
                        updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfo.WeightCategory = WeightCategory.Female;
                    else if (passengerWightType[i] == "Child")
                        updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfo.WeightCategory = WeightCategory.Child;
                    else
                        updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfo.WeightCategory = WeightCategory.Unmapped;

                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerProgram = null;
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerFees = null;
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerAddresses = null;
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerTravelDocuments = null;
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerBags = null;
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerID = 0;

                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerTypeInfos = new PassengerTypeInfo[1];
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerTypeInfos[0] = new PassengerTypeInfo();
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerTypeInfos[0].State = MessageState.New; // fix heula new passenger
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerTypeInfos[0].DOB = DateTime.Parse(passengerDOB[i].ToString()); //DateTime.Now;
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerTypeInfos[0].PaxType = passengerPaxTypes[i];

                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfos = new PassengerInfo[1];
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfos[0] = new PassengerInfo();
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfos[0].State = MessageState.New; // fix heula new passenger
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfos[0].BalanceDue = 0;
                    if (passengerGender[i] == "Male")
                        updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfos[0].Gender = Gender.Male;
                    else if (passengerGender[i] == "Female")
                        updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfos[0].Gender = Gender.Female;
                    else
                        updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfos[0].Gender = Gender.Unmapped;

                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfos[0].Nationality = passengerNationality[i];
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfos[0].ResidentCountry = String.Empty;
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfos[0].TotalCost = 0;

                    if (passengerWightType[i] == "Male")
                        updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfos[0].WeightCategory = WeightCategory.Male;
                    else if (passengerWightType[i] == "Female")
                        updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfos[0].WeightCategory = WeightCategory.Female;
                    else if (passengerWightType[i] == "Child")
                        updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfos[0].WeightCategory = WeightCategory.Child;
                    else
                        updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfos[0].WeightCategory = WeightCategory.Unmapped;
                                                                                
                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PseudoPassenger = false;
                    updatePassengersRequest.updatePassengersRequestData.WaiveNameChangeFee = false;

                    updatePassengersRequest.updatePassengersRequestData.Passengers[i].PassengerInfants = null;                    
                }

                
                updatePassengersResponse = bookingAPI.UpdatePassengers(updatePassengersRequest);
                //jh = new JSONHandler();
                //json = JsonConvert.SerializeObject(sellResponse.BookingUpdateResponseData);

                json = jh.JSONConstruct();
                jh.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }  
            return json;
        }

        public string AddPaymentRequest(string currencyCode, decimal tAmout)
        {
            string json = "";
            try
            {
                JsonLibs.MyJsonLib jh = new JsonLibs.MyJsonLib();
                jh.Clear();

                bookingAPI = new BookingManagerClient();
                AddPaymentToBookingRequest request = new AddPaymentToBookingRequest();
                AddPaymentToBookingRequestData payment = new AddPaymentToBookingRequestData();
                request.ContractVersion = 0; // pix heula
                request.Signature = agentSignature;
                payment.MessageState = MessageState.New; // pix heula New
                payment.WaiveFee = false;
                payment.ReferenceType = PaymentReferenceType.Session;
                payment.PaymentMethodType = RequestPaymentMethodType.AgencyAccount;
                payment.PaymentMethodCode = "AG"; // pix
                payment.QuotedCurrencyCode = currencyCode;
                payment.QuotedAmount = tAmout;
                payment.Status = BookingPaymentStatus.New;
                payment.AccountNumberID = 0;
                payment.AccountNumber = "0014001793";//;"0003000179"; // pix heula
                payment.Expiration = DateTime.Parse("0001-01-01T00:00:00"); // pix heula
                payment.ParentPaymentID = 0;
                payment.Installments = 0;
                payment.PaymentText = String.Empty;
                payment.Deposit = false;
                payment.PaymentFields = null;
                payment.PaymentAddresses = null;
                payment.AgencyAccount = null;
                payment.CreditShell = null;
                payment.CreditFile = null;
                payment.PaymentVoucher = null;
                request.addPaymentToBookingReqData = payment;
                AddPaymentToBookingResponse response;
                response = bookingAPI.AddPaymentToBooking(request);
                ValidationPayment validationPmt = response.BookingPaymentResponse.ValidationPayment;
                if (validationPmt.PaymentValidationErrors.Length > 0)
                {
                    Console.WriteLine(validationPmt.PaymentValidationErrors[0].AttributeName + " " + validationPmt.PaymentValidationErrors[0].ErrorDescription);
                    return json;
                }
                if (validationPmt.Payment.PaymentAddedToState)
                {
                    Console.WriteLine("Payment added to booking state: add payment complete");
                }

                //jh = new JSONHandler();
                //json = JsonConvert.SerializeObject(response.BookingPaymentResponse);
                json = jh.JSONConstruct();
                jh.Dispose();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return json;
        }

        public string CommitRequest(string currencyCode, string receivedBy, string commentText)
        {
			string json = "{}";
            try
            {
                JsonLibs.MyJsonLib jh = new JsonLibs.MyJsonLib();
                jh.Clear();

                bookingAPI = new BookingManagerClient();
                BookingCommitRequest request = new BookingCommitRequest();
                BookingCommitRequestData requestData = new BookingCommitRequestData();
				//Booking booking = new Booking();
                requestData.State = MessageState.New;
                requestData.CurrencyCode = currencyCode;
                requestData.ReceivedBy = new ReceivedByInfo();

                requestData.ReceivedBy.ReceivedBy = receivedBy;
                requestData.BookingComments = new BookingComment[1];
                requestData.BookingComments[0] = new BookingComment();
                requestData.BookingComments[0].CommentText = commentText;
                requestData.BookingComments[0].CommentType = CommentType.Itinerary;
                request.BookingCommitRequestData = requestData;
                request.Signature = agentSignature;
                request.ContractVersion = 0; // pix heula
				//BookingCommitResponse response = null;
				//response = bookingAPI.BookingCommit(request);
				bookingAPI.BookingCommit(request);

                //jh = new JSONHandler();
                //json = JsonConvert.SerializeObject(response.BookingUpdateResponseData);
                json = jh.JSONConstruct();
                jh.Dispose();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return json;            
        }

		public bool ChangePassword(string newPassword)
		{
			try
			{
				JsonLibs.MyJsonLib jh = new JsonLibs.MyJsonLib();
				jh.Clear();

				LogonRequestData data = new LogonRequestData();
				data.DomainCode = agentDomain;
				data.AgentName = agentName;
				data.Password = agentPass;

				//ChangePasswordRequest ChgPassReq = new ChangePasswordRequest(contractVersion, data, newPassword);

				clientManager.ChangePassword(contractVersion, data, newPassword);

				return true;

			}
			catch (Exception ex)
			{
				LogWriter.write(this, LogWriter.logCodeEnum.ERROR, ex.getCompleteErrMsg());
				//Console.WriteLine(ex.Message);
				return false;
			}

		}

    }    
}
