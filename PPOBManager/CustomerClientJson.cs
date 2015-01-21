using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PPOBManager
{
    public class CustomerClientJson : IDisposable
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
        ~CustomerClientJson()
        {
            this.Dispose(false);
        }
        #endregion

        // id = 3 char nama depan + 3 charnama belakang + no HP selain (+) 
//- Nama depan
//- Nama belakang
//- no HP
//- No KTP
//- Tempat Lahir
//- Tanggal lahir
//- Kelamin
//- Alamat
//- Kota
//- RT
//- RW
//- Kab/Kota
//- Kodepos
//- Propinsi
//- Nama ibu kandung
//- Jenis anggota (Biasa / Distributor)

//- Username
//- Password
        public string fiCustomerFirstName { get; set; }
        public string fiCustomerLastName { get; set; }
        public string fiCustomerPhone { get; set; }
        public string fiCustomerPhone2 { get; set; }
        public string fiCustomerEmail { get; set; }
        public string fiCustomerBirthDate { get; set; }
        public string fiCustomerBirthPlace { get; set; }
        public string fiCustomerGender { get; set; }
        public string fiCustomerAddress1 { get; set; }
        public string fiCustomerAddress2 { get; set; }
        public string fiCustomerAddress3 { get; set; }
        public string fiCustomerCity { get; set; }
        public string fiCustomerProvince { get; set; }
        public string fiCustomerZipCode { get; set; }
        public string fiCustomerCustomField1 { get; set; }
        public string fiCustomerCustomField2 { get; set; }
        public string fiCustomerCustomField3 { get; set; }
        public string fiCustomerCustomField4 { get; set; }
        public string fiCustomerCustomField5 { get; set; }
        public string fiCustomerCardNumber { get; set; }
        public string fiCustomerCardIdentityType { get; set; }
        public string fiCustomerCardIdentityValidDate { get; set; }
        public string fiCustomerRef1 { get; set; }
        public string fiCustomerRef2 { get; set; }
        public string fiCustomerRef3 { get; set; }
        public string fiCustomerMotherName { get; set; }
        public string fiCustomerNickname { get; set; }
        public string fiCustomerPassword { get; set; }
        public string fiCustomerUsername { get; set; }
    }

}
