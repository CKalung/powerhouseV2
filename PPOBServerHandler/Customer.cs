using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PPOBServerHandler
{
    public class CustomerJson: IDisposable
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
        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
        }
        ~CustomerJson()
        {
            this.Dispose(false);
        }
        #endregion

        public string ficoCustomerGroupId { get; set; }
        public string ficoCustomerId { get; set; }
        public string ficoCustomerName { get; set; }
        public string ficoCustomerPhone { get; set; }
        public string ficoCustomerPhone2 { get; set; }
        public string ficoCustomerEmail { get; set; }
        public string ficoCustomerBirthDate { get; set; }
        public string ficoCustomerBirthPlace { get; set; }
        public string ficoCustomerCity { get; set; }
        public string ficoCustomerCity2 { get; set; }
        public string ficoCustomerAddress { get; set; }
        public string ficoCustomerTrxAllowed { get; set; }
        public string ficoCustomerAddress2 { get; set; }
        public string ficoCustomerAddress3 { get; set; }
        public string ficoCustomerZipCode { get; set; }
        public string ficoCustomerZipCode2 { get; set; }
        public string ficoCustomerCustomField1 { get; set; }
        public string ficoCustomerCustomField2 { get; set; }
        public string ficoCustomerCustomField3 { get; set; }
        public string ficoCustomerCustomField4 { get; set; }
        public string ficoCustomerCustomField5 { get; set; }
        public string ficoCustomerCardNumber { get; set; }
        public string ficoCustomerCardIdentityType { get; set; }
        public string ficoCustomerIdentityCardNumber { get; set; }
        public string ficoCustomerIdentityCardValidDate { get; set; }
        public string ficoCustomerNpwp { get; set; }
        public string ficoCustomerNickname { get; set; }
        public string ficoCustomerRef1 { get; set; }
        public string ficoCustomerRef2 { get; set; }
        public string ficoCustomerRef3 { get; set; }
        public string ficoCustomerGender { get; set; }
        public string ficoCustomerMotherName { get; set; }
        public string ficoCustomerPassword { get; set; }
        public string ficoCustomerUsername { get; set; }
        public string source { get; set; }
    }
}
