using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

namespace Payment_Host_Interface
{
    public interface ITransactionInterface : IDisposable
    {

        bool productTransaction(string appID, string userId, string transactionReference, string providerProductCode,
           string providerAmount, ref string HttpReply, ref int traceNumber,
           ref string strJson, ref DateTime trxTime, ref string strRecJson, ref DateTime trxRecTime,
           ref string failedReason, ref bool canReversal, ref bool isSuccessPayment, 
            int transactionType, string trxNumber);

        bool productInquiry(string appID, string userId, string customerNumber,
            string productCode, int adminFee, bool fIncludeAdminFee, ref int productAmount, ref string HttpReply,
            ref int traceNumber, ref string strJson, ref DateTime trxTime, ref string strRecJson,
            ref DateTime trxRecTime, string trxNumber);

    }
}
