using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Runtime.CompilerServices;

namespace PublicSettings
{
    public class Settings
    {
        public string DbHost;
        public int DbPort;
        public string DbUser;
        public string DbPassw;
        public string DbName;

        //private Hashtable asyncSettingCollection;
        private Hashtable syncSettingCollection;

        private PPOBDatabase.PPOBdbLibs locDb;

        public Hashtable SettingCollection
        {
            get
            {
                return syncSettingCollection;
            }
            //set
            //{
            //    //asyncSettingCollection = value;
            //    //syncSettingCollection = Hashtable.Synchronized(asyncSettingCollection);
            //    syncSettingCollection = Hashtable.Synchronized(value);
            //}
        }

		[MethodImpl(MethodImplOptions.Synchronized)]
        public bool ReloadSettings()
        {
            try
            {
                Hashtable ttbl = locDb.getAppConfigurations();
                if (ttbl != null)
                {
                    syncSettingCollection = Hashtable.Synchronized(ttbl);
                    return true;
                }
                else return false;
            }
            catch
            {
                return false;
            }
        }

        public PPOBDatabase.PPOBdbLibs localDb
        {
			set { locDb = value; }
			get { return locDb; }
        }

        public bool isKeyExist(string key)
        {
            if (SettingCollection == null) return false;
            return (SettingCollection.ContainsKey(key)) ;
        }

        public int getInt(string key)
        {
            if (!isKeyExist(key)) return -1;
            try
            {
                return int.Parse(((string)SettingCollection[key]).Trim());
            }
            catch { return -1; }
        }

        public string getString(string key)
        {
            if (!isKeyExist(key)) return "";
            try
            {
                return ((string)SettingCollection[key]).Trim();
            }
            catch { return ""; }
        }

        public long getLong(string key)
        {
            if (!isKeyExist(key)) return -1;
            try
            {
                return long.Parse(((string)SettingCollection[key]).Trim());
            }
            catch { return -1; }
        }

    }

    //public class Settings2
    //{

    //    public string LogPath;
    //    public string DbHost;
    //    public int DbPort;
    //    public string DbUser;
    //    public string DbPassw;
    //    public string DbName;
    //    public string SandraHost;
    //    public int SandraPort;

    //    public int MAX_Client;
    //    public int ServerListenPort;

    //    public string httpRestServicePath;
    //    public string httpRestServiceAccountPath;
    //    public string httpRestServiceProductTransactionPath;
    //    public string httpRestServiceApplicationsPath;

    //    public string CashInProductCode;
    //    public string CashOutProductCode;
    //    //public string InvoiceProductCode;

    //    public string QVA_PENAMPUNG_KREDIT = "911110001";
    //    public string QVA_PENAMPUNG_DEBIT = "911110002";
    //    public string QVA_ESCROW_DAM_KREDIT = "911110003";
    //    //public string QVA_ESCROW_DAM_DEBIT = "911110003";
    //    public string QVA_ESCROW_QNB_KREDIT = "911110004";
    //    //public string QVA_ESCROW_QNB_DEBIT = "911110004";
    //    public string QVA_ESCROW_FINNET_KREDIT = "911110006";
    //    //public string QVA_ESCROW_FINNET_DEBIT = "911110006";

    //    public string UserIdHeader = "99";
    //    public string EmailDomain = "";

    //    public int CommandProductTransaction = 10001;

    //    public int CommandAccountActivateFirstLine = 0;
    //    public int CommandAccountHapusUmum = 0;
    //    public int CommandAccountRegistration = 0;
    //    public int CommandAccountUpdate = 0;
    //    public int CommandAccountInquiry = 0;
    //    public int CommandAccountActivation = 0;
    //    public int CommandAccountTransfer = 0;

    //    public int CommandAccountCashInRequest = 0;    // TopUp
    //    public int CommandAccountCashOutRequest = 0;
    //    public int CommandAccountCashInApproval = 0;
    //    public int CommandAccountCashOutApproval = 0;

    //    public int CommandApplicationLogin = 0;
    //    public int CommandApplicationHeartBeat = 0;
    //    public int CommandChangeUserPassword = 0;
    //    public int CommandResetUserPassword = 0;

    //    public string FinnetQueueHost = "";
    //    public int FinnetQueuePort = 0;

    //    public string FmQueueHost = "";
    //    public int FmQueuePort = 0;

    //    public string CommonTerminalID = "";
    //    public string CommonMerchantID = "";

    //    public int CommandAccountInvoice = 0;
    //    public int CommandAccountInvoiceApproval = 0;

    //    public int BillerPersadaTimeOut = 1210;
    //    public int BillerFmTimeOut = 1210;
    //    public int BillerFinnetTimeOut = 1210;

    //    public int QvaTimeOut = 1210;

    //    // TODO : ntar pake hashtable aja dgn key nama nya

    //    // rekening ESCROW yang dapat digunakan, terserah escrow yang mana
    //    //911110001
    //    //911110002
    //    //911110003
    //    //911110004
    //    //911110005
    //    //911110006

    //}
}
