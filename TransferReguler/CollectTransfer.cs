using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PPOBHttpRestData;
//using Process_MPAccountAccess;
using LOG_Handler;

namespace TransferReguler
{
    public class CollectTransfer: IDisposable
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
        ~CollectTransfer()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            localDB.Dispose();
           // TransferSchedLog.Dispose();
        }

        // rekening ESCROW yang dapat digunakan, terserah escrow yang mana
    //911110001
    //911110002
    //911110003
    //911110004
    //911110005
    //911110006
        //string QVA_PENAMPUNG = "911110001";
        //string QVA_ESCROW_DAM = "911110002";
        //string QVA_ESCROW_QNB = "911110003";
        //string QVA_ESCROW_FINNET = "911110006";

        //VATransferSchedLog TransferSchedLog;
        PublicSettings.Settings commonSettings;
        PPOBDatabase.PPOBdbLibs localDB;
        //string sandraHost;
        //int sandraPort;

        //string logPath = "";
        //string dbHost = "";
        //int dbPort = 0;
        //string dbUser = "";
        //string dbPass = "";
        //string dbName = "";


        public CollectTransfer(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            //sandraHost = SandraHost;
            //sandraPort = SandraPort;
            //dbHost = DbHost; dbPort = DbPort; dbUser = DbUser; dbPass = DbPassw; dbName = DbName;
            //localDB = new PPOBDatabase.PPOBdbLibs(commonSettings);
            localDB = new PPOBDatabase.PPOBdbLibs(commonSettings.DbHost, commonSettings.DbPort,
                commonSettings.DbName, commonSettings.DbUser, commonSettings.DbPassw);
        }

        //public bool TransferPembayaranKeFinnet(int baseAmount, long TransactionRef_id,
        //                    ref string errCode, ref string errMessage)
        //{
        //    // transfer dari escrow penampung ke account pembeli 
        //    string channelId = "6014";
        //    string transCode = "400013";  // PENAMPUNG ke FINNET (REGULAR PELUNASAN)

        //    bool hasil = false;
        //    using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
        //    {
        //        hasil = QVA.Transfer(commonSettings.getString("QVA_PENAMPUNG_DEBIT"),
        //            commonSettings.getString("QVA_ESCROW_FINNET_KREDIT"), baseAmount, channelId, transCode,
        //            TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType.PPOB,
        //            "Transfer to Finnet", false,
        //            ref errCode, ref errMessage);
        //    }
        //    if (!hasil) return false;   // Gagal transfer

        //    return true;
        //}

        //public bool TransferFeeKeCustomer(string userId, int feeAmount,
        //    long TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType TransactionType, 
        //    string description, 
        //    ref string errorCode, ref string errorMessage)
        //{
        //    // transfer dari escrow penampung ke account pembeli 
        //    string channelId = "6014";
        //    string transCode = "400012";    // bagi hasil

        //    bool hasil = false;
        //    using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
        //    {
        //        hasil = QVA.Transfer(commonSettings.QVA_PENAMPUNG_DEBIT, userId, feeAmount, 
        //            channelId, transCode,
        //            TransactionRef_id, TransactionType, description, true,
        //            ref errorCode, ref errorMessage);
        //    }
        //    if (!hasil) return false;   // Gagal transfer

        //    return true;
        //}

//		public bool ReversalToCustomer(string userId, string trxCode_sufix, int amount,
//            long TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType TransactionType, 
//            string description,
//            ref string errorCode, ref string errorMessage)
//        {
//            // transfer dari escrow penampung ke account pembeli 
//            string channelId = "6014";
//			//string transCode = commonSettings.getString ("QVA_TransxCode_ReversalToCustomer");	//"400015";
//			string transCode = commonSettings.getString ("TRANSFER_REVERSAL") + trxCode_sufix;
//
//            string errCode = ""; string errMessage = "";
//            bool hasil = false;
//			bool QvaReversalRequired = false;
//            using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
//            {
//				Disinii nih, harusnya langsung request Reversal ka QVA,jangan transfer biasa
//                hasil = QVA.Transfer(commonSettings.getString("QVA_PENAMPUNG_DEBIT"),
//					userId, amount, 
//                    channelId, transCode,
//                    TransactionRef_id, TransactionType, description, false,
//					ref errCode, ref errMessage,ref QvaReversalRequired);
//            }
//            if (!hasil) return false;   // Gagal transfer
//
//            return true;
//        }

		public bool TransferBackToCustomer(string userId, string trxCode_sufix, int amount,
            long TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType TransactionType, 
            string description,
            ref string errorCode, ref string errorMessage)
        {
            // transfer dari escrow penampung ke account pembeli 
            string channelId = "6014";
			//string transCode = commonSettings.getString ("QVA_TransxCode_ReversalToCustomer");	//"400015";
			string transCode = commonSettings.getString ("TRANSFER_REVERSAL") + trxCode_sufix;

            bool hasil = false;
			bool QvaReversalRequired = false;
			string qvaInvoiceNumber = "";
			bool fFORCE_TRANSFER = true;
            using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
            {
                hasil = QVA.Transfer(commonSettings.getString("QVA_PENAMPUNG_DEBIT"),
					userId, amount, 
                    channelId, transCode,
					TransactionRef_id, TransactionType, description, false, ref qvaInvoiceNumber,fFORCE_TRANSFER,
					ref errorCode, ref errorMessage,ref QvaReversalRequired);
            }
			return hasil;
        }

		public bool Reversal(long transactionRefId, string qvaInvoiceNumber,
			ref string errorCode, ref string errorMessage)
		{
			// transfer Reversal ke QVA
			string errCode = ""; string errMessage = "";
			bool hasil = false;
			using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
			{
				hasil = QVA.Reversal (transactionRefId, qvaInvoiceNumber, ref errCode, ref errMessage);
			}
			return hasil;
		}


		public bool PayFromCustomerEwallet(string trxCode_sufix, int PpobType, int productAmount,
			long TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType TransactionType,
			string description, ref string invoiceNumber, ref bool qvaReversalRequired,
			ref string errorCode, ref string errorMessage)
		{
			// transfer dari account pembeli ke escrow penampung
			// transactionType = 0 Pembayaran, 1 = Pembelian
			string channelId = "6014";
			string transCode = commonSettings.getString ("TRANSFER_FROM_EWALLET_TO_MAIN_ESCROW") + trxCode_sufix;

			errorCode = ""; errorMessage = "";
			bool hasil = false;

			using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
			{
				hasil = QVA.Transfer(commonSettings.getString("QVA_REKENING_TITIPAN"), 
					commonSettings.getString("QVA_PENAMPUNG_KREDIT"), productAmount, 
					channelId, transCode,
					TransactionRef_id, TransactionType, description, false, ref invoiceNumber,
					ref errorCode, ref errorMessage, ref qvaReversalRequired);
			}

			if (!hasil) return false;   // Gagal transfer

			return true;
		}

		public bool PayFromCustomer(string userId, string trxCode_sufix, int PpobType, int productAmount,
            long TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType TransactionType,
			string description, ref string invoiceNumber, ref bool qvaReversalRequired,
            ref string errorCode, ref string errorMessage)
        {
            // transfer dari account pembeli ke escrow penampung
            // transactionType = 0 Pembayaran, 1 = Pembelian
            string channelId = "6014";
            string transCode = commonSettings.getString ("TRANSFER_FROM_CUSTOMER_TO_MAIN_ESCROW") + trxCode_sufix;

//            if (PpobType == 0)
//            {
//                // pembayaran 
//				transCode = commonSettings.getString ("QVA_TransxCode_CustToEscrow_Bayar");	//"400011";
//            }
//            else
//            {
//                // pembelian
//				transCode = commonSettings.getString ("QVA_TransxCode_CustToEscrow_Beli");	//"400010";
//            }

			errorCode = ""; errorMessage = "";
            bool hasil = false;

            using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
            {
                hasil = QVA.Transfer(userId, commonSettings.getString("QVA_PENAMPUNG_KREDIT"), productAmount, 
                    channelId, transCode,
					TransactionRef_id, TransactionType, description, false, ref invoiceNumber,
					ref errorCode, ref errorMessage, ref qvaReversalRequired);
            }

            //Console.WriteLine("DEBUG NEPI Dieu mah berhasil");
            //Console.WriteLine("HASILna = " + hasil.ToString());

            if (!hasil) return false;   // Gagal transfer

            return true;
        }

		public bool TransferTopUpToCustomer(string userPhone, string customerPhone, string trxCode_sufix, int amount,
            long TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType TransactionType,
			string description, ref string qvaInvoiceNumber, ref bool qvaReversalRequired,
            ref string errorCode, ref string errorMessage)
        {
            string channelId = "6017";
			//string transCode = commonSettings.getString ("QVA_TransxCode_TopUpCustomer");		//"400014";
			string transCode = commonSettings.getString ("TRANSFER_FROM_CUSTOMER_TO_CUSTOMER") + trxCode_sufix;

            string userId = commonSettings.getString("UserIdHeader") + userPhone;
            string customerId = commonSettings.getString("UserIdHeader") + customerPhone;

            bool hasil = false;
            using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
            {
                hasil = QVA.Transfer(userId, customerId, amount, channelId, transCode,
					TransactionRef_id,TransactionType,description,false, ref qvaInvoiceNumber,
					ref errorCode, ref errorMessage, ref qvaReversalRequired);
            }
            if (!hasil) return false;   // Gagal transfer

            return true;
        }

		public bool TransferCashOutFromCustomer(string userPhone, string customerPhone, string trxCode_sufix, int amount,
            long TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType TransactionType,
			string description, ref string qvaInvoiceNumber, ref bool qvaReversalRequired,
            ref string errorCode, ref string errorMessage)
        {
            string channelId = "6017";
			//string transCode = commonSettings.getString ("QVA_TransxCode_CashOut");	//"400014";
			string transCode = commonSettings.getString ("TRANSFER_FROM_CUSTOMER_TO_CUSTOMER") + trxCode_sufix;

            string userId = commonSettings.getString("UserIdHeader") + userPhone;
            string customerId = commonSettings.getString("UserIdHeader") + customerPhone;

            bool hasil = false;
            using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
            {
                hasil = QVA.Transfer(customerId, userId, amount, channelId, transCode,
					TransactionRef_id, TransactionType, description, false, ref qvaInvoiceNumber,
					ref errorCode, ref errorMessage, ref qvaReversalRequired);
            }
            if (!hasil) return false;   // Gagal transfer

            return true;
        }

		public bool TransferGetAdminFeeFromCustomer(string customerPhone, string trxCode_sufix, int amount,
            long TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType TransactionType,
			string description, ref string qvaInvoiceNumber, ref bool qvaReversalRequired,
            ref string errorCode, ref string errorMessage)
        {
            string channelId = "6014";
			//string transCode = commonSettings.getString ("QVA_TransxCode_GetFeeFromCustomer");	//"400011";
			string transCode = commonSettings.getString ("TRANSFER_FROM_CUSTOMER_TO_MAIN_ESCROW") + trxCode_sufix;

            string userId = commonSettings.getString("UserIdHeader") + customerPhone;

            bool hasil = false;
            using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
            {
                hasil = QVA.Transfer(userId, commonSettings.getString("QVA_PENAMPUNG_KREDIT"), amount, 
                    channelId, transCode,
					TransactionRef_id, TransactionType, description, false, ref qvaInvoiceNumber,
					ref errorCode, ref errorMessage, ref qvaReversalRequired);
            }
            if (!hasil) return false;   // Gagal transfer

            return true;
        }

		public bool TransferGetAdminFeeFromMerchant(string merchantPhone, string trxCode_sufix, int amount,
            long TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType TransactionType,
			string description, ref string qvaInvoiceNumber, ref bool qvaReversalRequired,
            ref string errorCode, ref string errorMessage)
        {
            string channelId = "6014";
			//string transCode = commonSettings.getString ("QVA_TransxCode_GetFeeFromMerchant");	//"400011";
			string transCode = commonSettings.getString ("TRANSFER_FROM_CUSTOMER_TO_MAIN_ESCROW") + trxCode_sufix;

            string merchantId = commonSettings.getString("UserIdHeader") + merchantPhone;

            bool hasil = false;
            using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
            {
                hasil = QVA.Transfer(merchantId, commonSettings.getString("QVA_PENAMPUNG_KREDIT"), amount, 
                    channelId, transCode, 
					TransactionRef_id, TransactionType, description, false, ref qvaInvoiceNumber,
					ref errorCode, ref errorMessage, ref qvaReversalRequired);
            }
            if (!hasil) return false;   // Gagal transfer

            return true;
        }

		public bool TransferToNitrogenOwner(string trxCode_sufix, int amount,
            long TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType TransactionType,
			string description, ref string qvaInvoiceNumber, ref bool qvaReversalRequired,
            ref string errorCode, ref string errorMessage)
        {
            string channelId = "6014";
			//string transCode = commonSettings.getString ("QVA_TransxCode_ToNitrogenOwner");	//"400011";
			string transCode = commonSettings.getString ("TRANSFER_FROM_MAIN_ESCROW_TO_BILLER") + trxCode_sufix;

            bool hasil = false;
            using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
            {
                hasil = QVA.Transfer(commonSettings.getString("QVA_PENAMPUNG_DEBIT"),
                    commonSettings.getString("QVA_ESCROW_NITROGEN_KREDIT"), 
                    amount,
                    channelId, transCode,
					TransactionRef_id, TransactionType, description, false, ref qvaInvoiceNumber,
					ref errorCode, ref errorMessage, ref qvaReversalRequired);
            }
            if (!hasil) return false;   // Gagal transfer

            return true;
        }

		public bool TransferInvoiceFromCustomerToCustomer(string userPhone, string merchantPhone, string trxCode_sufix, int amount,
            long TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType TransactionType,
			string description, ref string qvaInvoiceNumber, ref bool qvaReversalRequired,
            ref string errorCode, ref string errorMessage)
        {
            string channelId = "6017";
			//string transCode = commonSettings.getString ("QVA_TransxCode_ToNitrogenOwner");	//"400011";
			string transCode = commonSettings.getString ("TRANSFER_FROM_CUSTOMER_TO_CUSTOMER") + trxCode_sufix;

            string userId = commonSettings.getString("UserIdHeader") + userPhone;
            string merchantId = commonSettings.getString("UserIdHeader") + merchantPhone;

            bool hasil = false;
            using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
            {
                hasil = QVA.Transfer(userId, merchantId, amount, channelId, transCode,
					TransactionRef_id, TransactionType, description, false, ref qvaInvoiceNumber,
					ref errorCode, ref errorMessage, ref qvaReversalRequired);
            }
            if (!hasil) return false;   // Gagal transfer

            return true;
        }

		public bool TransferInvoiceFromCustomerToPenampung(string userPhone, string trxCode_sufix, int amount,
			long TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType TransactionType,
			string description, ref string qvaInvoiceNumber, ref bool qvaReversalRequired,
			ref string errorCode, ref string errorMessage)
		{
			string channelId = "6017";
			string transCode = commonSettings.getString ("TRANSFER_FROM_CUSTOMER_TO_MAIN_ESCROW") + trxCode_sufix;

			string userId = commonSettings.getString("UserIdHeader") + userPhone;

			bool hasil = false;
			using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
			{
				hasil = QVA.Transfer(userId, commonSettings.getString("QVA_PENAMPUNG_KREDIT"), 
					amount, channelId, transCode,
					TransactionRef_id, TransactionType, description, false, ref qvaInvoiceNumber,
					ref errorCode, ref errorMessage, ref qvaReversalRequired);
			}
			if (!hasil) return false;   // Gagal transfer

			return true;
		}

		// TransferAccountToAccount
		public bool TransferAccountToAccount(string sourceAccount, string targetAccount, string trxCode_sufix, int amount,
			long TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType TransactionType,
			string description, ref string qvaInvoiceNumber, ref bool qvaReversalRequired,
			ref string errorCode, ref string errorMessage,
			string qvaSourceId="", string qvaTransCode=""
		)
		{
			string channelId = "6017";
			//string transCode = commonSettings.getString ("QVA_TransxCode_RootTransfer");	//"400014";
			string transCode = commonSettings.getString ("TRANSFER_FROM_CUSTOMER_TO_CUSTOMER") + trxCode_sufix;

			if (qvaTransCode != "")
				transCode = qvaTransCode;

			bool hasil = false;
			using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
			{
				hasil = QVA.Transfer(sourceAccount, targetAccount, amount, channelId, transCode,
					TransactionRef_id, TransactionType, description, false, ref qvaInvoiceNumber,
					ref errorCode, ref errorMessage, ref qvaReversalRequired);
			}
			if (!hasil) return false;   // Gagal transfer

			return true;
		}

		public bool TransferAccountToCustomer(string sourceAccount, string targetPhone, string trxCode_sufix, int amount,
			long TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType TransactionType,
			string description, ref string qvaInvoiceNumber, ref bool qvaReversalRequired,
			ref string errorCode, ref string errorMessage)
		{
			string channelId = "6017";
			//string transCode = commonSettings.getString ("QVA_TransxCode_RootTransfer");	//"400014";
			string transCode = commonSettings.getString ("TRANSFER_FROM_CUSTOMER_TO_CUSTOMER") + trxCode_sufix;

			string targetAccount = commonSettings.getString("UserIdHeader") + targetPhone;

			bool hasil = false;
			using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
			{
				hasil = QVA.Transfer(sourceAccount, targetAccount, amount, channelId, transCode,
					TransactionRef_id, TransactionType, description, false, ref qvaInvoiceNumber,
					ref errorCode, ref errorMessage, ref qvaReversalRequired);
			}
			if (!hasil) return false;   // Gagal transfer

			return true;
		}

		public bool TransferCustomerToCustomer(string userPhone, string destinationPhone, string trxCode_sufix, int amount,
            long TransactionRef_id, PPOBDatabase.PPOBdbLibs.eTransactionType TransactionType,
			string description, ref string qvaInvoiceNumber, ref bool qvaReversalRequired,
            ref string errorCode, ref string errorMessage)
        {
            string channelId = "6017";
			//string transCode = commonSettings.getString ("QVA_TransxCode_RootTransfer");	//"400014";
			string transCode = commonSettings.getString ("TRANSFER_FROM_CUSTOMER_TO_CUSTOMER") + trxCode_sufix;

            string userId = commonSettings.getString("UserIdHeader") + userPhone;
            string destinationId = commonSettings.getString("UserIdHeader") + destinationPhone;

            bool hasil = false;
            using (QvaTransactions QVA = new QvaTransactions(localDB, commonSettings))
            {
                hasil = QVA.Transfer(userId, destinationId, amount, channelId, transCode,
					TransactionRef_id, TransactionType, description, false, ref qvaInvoiceNumber,
					ref errorCode, ref errorMessage, ref qvaReversalRequired);
            }
            if (!hasil) return false;   // Gagal transfer

            return true;
        }


    }
}
