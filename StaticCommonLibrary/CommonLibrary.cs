using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections;

namespace StaticCommonLibrary
{
    public static class ExceptionHelper
    {
        public static int LineNumber(this Exception e)
        {

            int linenum = 0;
            try
            {
                linenum = Convert.ToInt32(e.StackTrace.Substring(e.StackTrace.LastIndexOf(":line") + 5));
            }
            catch
            {
                //Stack trace is not available!
            }
            return linenum;
        }

        public static string getCompleteErrMsg(this Exception e)
        {
            return " at line: " + e.LineNumber().ToString() + "=> "+ e.Message + "\r\n" + e.StackTrace;
        }
    }

    public static class CommonLibrary
    {
        public class SessionStruct
        {
            public string SessionToken="";
            public string UserPhone="";
            public string UserId="";
			public string RandomChallenge="";
            DateTime lastLoginTime;
            //public string UserName="";
            public string Host = "";
            DateTime ResetTime;

            public SessionStruct()
            {
                lastLoginTime = DateTime.Now;
                ResetTime = lastLoginTime;
            }

            public DateTime LastLoginTime
            {
                get{
                    return lastLoginTime;
                }
            }
            public void ResetTimeOut()
            {
                ResetTime = DateTime.Now;
            }

            public int LoginMinutesInterval
            {
                get{
                    try{
                        return (Convert.ToInt32(DateTime.Now.Subtract(lastLoginTime).TotalMinutes));
                    }
                    catch{
                        return Int32.MaxValue;
                    }
                }
            }
            public int IdleMinutesInterval
            {
                get{
                    try{
                        return (Convert.ToInt32(DateTime.Now.Subtract(ResetTime).TotalMinutes));
                    }
                    catch{
                        return Int32.MaxValue;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        static private int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        static private char intToChar(int a)
        {
            if (a < 10) return (char)(a + 0x30);
            return (char)(a + 0x40);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static string generateToken()
        {
            Random random = new Random();
            random.Next(0, 32);
            string hasil = "";
            int rd = 0;
            for (int i = 0; i < 32; i++)
            {
                // Semua numerik dan char
                rd = random.Next(0, 35);
                if (rd < 10) hasil += (char)(rd + 0x30);
                else hasil += (char)(rd + 0x37);
            }
            return hasil;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static string generateToken(int jumlahChar)
        {
            Random random = new Random();
            random.Next(0, jumlahChar);
            string hasil = "";
            int rd = 0;
            for (int i = 0; i < jumlahChar; i++)
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

        static Hashtable sSessionList = new Hashtable();
        static Hashtable SessionList = Hashtable.Synchronized(sSessionList);    // thread safe
        static int sessionMinutesTimeOut = 15;
        static System.Object lockSessionObject = new System.Object();

        //[MethodImpl(MethodImplOptions.Synchronized)]
        public static int SessionMinutesTimeout
        {
            set{
                sessionMinutesTimeOut = value;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static string AddSessionItem(string userPhone, string UserId, string HostName)
        {
            lock (lockSessionObject)
            {
                SessionStruct aSession;
                if (SessionList.ContainsKey(userPhone))
                {
                    aSession = ((SessionStruct)(SessionList[userPhone]));
                    aSession.ResetTimeOut();
                }
                else
                {
                    aSession = new SessionStruct();
                }
                aSession.SessionToken = generateToken(32);  // generate 32 huruf
                aSession.UserId = UserId;
				aSession.RandomChallenge = generateToken (64);
                //aSession.UserName = UserName;
                aSession.Host = HostName;
                if (!SessionList.ContainsKey(userPhone))
                {
                    SessionList.Add(userPhone, aSession);
                }
                else
                {
                    SessionList[userPhone] = aSession;
                }
                return aSession.SessionToken;
            }
        }

		[MethodImpl(MethodImplOptions.Synchronized)]
		public static string GenerateRandomChallenge(string userPhone, string UserId, string HostName)
		{
			lock (lockSessionObject)
			{
				SessionStruct aSession;
				if (SessionList.ContainsKey(userPhone))
				{
					aSession = ((SessionStruct)(SessionList[userPhone]));
					aSession.ResetTimeOut();
				}
				else
				{
					aSession = new SessionStruct();
					aSession.UserId = UserId;
				}
				aSession.RandomChallenge = generateToken (64);
				if (!SessionList.ContainsKey(userPhone))
				{
					SessionList.Add(userPhone, aSession);
				}
				else
				{
					SessionList[userPhone] = aSession;
				}
				return aSession.RandomChallenge;
			}
		}

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static string RenewTokenSession(string userPhone)
        {
            lock (lockSessionObject)
            {
                if (SessionList.ContainsKey(userPhone))
                {
                    SessionStruct aSession = (SessionStruct)SessionList[userPhone];
                    aSession.SessionToken = generateToken(32);  // generate 32 huruf
                    SessionList[userPhone] = aSession;
                    return aSession.SessionToken;
                }
                else return "";
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool SetNewToken(string userPhone, string newToken)
        {
            lock (lockSessionObject)
            {
                if (SessionList.ContainsKey(userPhone))
                {
                    SessionStruct aSession = (SessionStruct)SessionList[userPhone];
                    aSession.SessionToken = newToken;  // generate 32 huruf
                    SessionList[userPhone] = aSession;
                    return true;
                }
                else return false;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void RefreshSessions()
        {
            List<string> rmv = new System.Collections.Generic.List<string>();
            lock (lockSessionObject)
            {
                foreach (DictionaryEntry aDic in SessionList)
                {
                    SessionStruct aSession = (SessionStruct)(aDic.Value);
                    if (aSession.IdleMinutesInterval >= sessionMinutesTimeOut) rmv.Add((string)aDic.Key);
                }
                foreach (string aKey in rmv)
                {
                    SessionList.Remove(aKey);
                }
                rmv.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool isSessionExist(string userPhone)
        {
            RefreshSessions();
            return SessionList.ContainsKey(userPhone);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool isSessionExist(string userPhone, string SessionToken)
        {
            RefreshSessions();
            if (SessionList.ContainsKey(userPhone))
            {
                try
                {
                    if (((SessionStruct)SessionList[userPhone]).SessionToken == SessionToken)
                    {
                        return true;
                    }
                    else return false;
                }
                catch
                {
                    return false;
                }
            }
            else return false;
        }

		[MethodImpl(MethodImplOptions.Synchronized)]
		public static bool SessionResetTimeOut(string userPhone)
		{
			bool hasil = false;
			RefreshSessions();
			lock (lockSessionObject)
			{
				if (SessionList.ContainsKey(userPhone))
				{
					((SessionStruct)(SessionList[userPhone])).ResetTimeOut();
					hasil = true;
				}
				else hasil = false;
			}
			return hasil;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public static void SessionRemove(string userPhone)
		{
			RefreshSessions();
			lock (lockSessionObject)
			{
				if (SessionList.ContainsKey(userPhone))
					SessionList.Remove(userPhone);
			}
		}

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static SessionStruct SessionGetInfo(string userPhone)
        {
            SessionStruct hasil = null;
            lock (lockSessionObject)
            {
                if (SessionList.ContainsKey(userPhone))
                {
                    hasil = (SessionStruct)(SessionList[userPhone]);
                }
            }
            return hasil;
        }

		[MethodImpl(MethodImplOptions.Synchronized)]
		public static bool isRandomChallengeMatch(string userPhone, string randomChallenge)
		{
			RefreshSessions();
			if (SessionList.ContainsKey(userPhone))
			{
				try
				{
					if (((SessionStruct)SessionList[userPhone]).RandomChallenge.ToLower () == randomChallenge.ToLower ())
						return true;
					else 
						return false;
				}
				catch
				{
					return false;
				}
			}
			else return false;
		}

    }

    //public static class CommonLibrary
    //{
    //    static string applicationPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

    //    static string referenceNumFileName = "ReferenceNumber.Seq";
    //    static string traceNumFileName = "TraceNumber.Seq";
    //    static string transactionNumFileName = "PPOBTrxNumber.Seq";

    //    [MethodImpl(MethodImplOptions.Synchronized)]
    //    public static string getNextTransactionNumberString()
    //    {
    //        long trxNum = 0;
    //        try
    //        {
    //            trxNum = long.Parse(System.IO.File.ReadAllText(applicationPath + "/" + transactionNumFileName));
    //            if (trxNum <= 0) trxNum = 1;
    //            else if (trxNum > 9999999999) trxNum = 1;
    //        }
    //        catch
    //        {
    //            trxNum = 1;
    //        }
    //        try
    //        {
    //            System.IO.File.WriteAllText(applicationPath + "/" + referenceNumFileName, (trxNum + 1).ToString());
    //        }
    //        catch { }
    //        return (DateTime.Now.ToString("yy") + trxNum.ToString().PadLeft(10, '0'));
    //    }

    //    [MethodImpl(MethodImplOptions.Synchronized)]
    //    public static string getNextProductReferenceNumberString()
    //    {
    //        long refNum = 0;
    //        try
    //        {
    //            refNum = long.Parse(System.IO.File.ReadAllText(applicationPath + "/" + referenceNumFileName));
    //            if (refNum <= 0) refNum = 1;
    //            else if (refNum > 999999999990) refNum = 1;
    //        }
    //        catch
    //        {
    //            refNum = 1;
    //        }
    //        try
    //        {
    //            System.IO.File.WriteAllText(applicationPath + "/" + referenceNumFileName, (refNum + 1).ToString());
    //        }
    //        catch { }
    //        return (refNum.ToString().PadLeft(12, '0'));
    //    }

    //    [MethodImpl(MethodImplOptions.Synchronized)]
    //    public static string getNextProductTraceNumberString()
    //    {
    //        int trcNum = 0;
    //        try
    //        {
    //            trcNum = int.Parse(System.IO.File.ReadAllText(applicationPath + "/" + referenceNumFileName));
    //            if (trcNum <= 0) trcNum = 1;
    //            else if (trcNum > 999999) trcNum = 1;
    //        }
    //        catch
    //        {
    //            trcNum = 1;
    //        }
    //        try
    //        {
    //            System.IO.File.WriteAllText(applicationPath + "/" + traceNumFileName, (trcNum + 1).ToString());
    //        }
    //        catch { }
    //        return (trcNum.ToString().PadLeft(6, '0'));
    //    }

    //    [MethodImpl(MethodImplOptions.Synchronized)]
    //    public static long getNextProductReferenceNumber()
    //    {
    //        long refNum = 0;
    //        try
    //        {
    //            refNum = long.Parse(System.IO.File.ReadAllText(applicationPath + "/" + referenceNumFileName));
    //            if (refNum <= 0) refNum = 1;
    //            else if (refNum > 999999999990) refNum = 1;
    //        }
    //        catch
    //        {
    //            refNum = 1;
    //        }
    //        try
    //        {
    //            System.IO.File.WriteAllText(applicationPath + "/" + referenceNumFileName, (refNum + 1).ToString());
    //        }
    //        catch { }
    //        return refNum;
    //    }

    //    [MethodImpl(MethodImplOptions.Synchronized)]
    //    public static int getNextProductTraceNumber()
    //    {
    //        int trcNum = 0;
    //        try
    //        {
    //            trcNum = int.Parse(System.IO.File.ReadAllText(applicationPath + "/" + referenceNumFileName));
    //            if (trcNum <= 0) trcNum = 1;
    //            else if (trcNum > 999999) trcNum = 1;
    //        }
    //        catch
    //        {
    //            trcNum = 1;
    //        }
    //        try
    //        {
    //            System.IO.File.WriteAllText(applicationPath + "/" + traceNumFileName, (trcNum + 1).ToString());
    //        }
    //        catch { }
    //        return trcNum;
    //    }
    //}
}
