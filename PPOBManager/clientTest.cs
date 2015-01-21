using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PPOBManager
{
    public class clientTest: IDisposable
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
        ~clientTest()
        {
            this.Dispose(false);
        }
        #endregion

        public string fiFirstName { get; set; }
        public string fiLastName { get; set; }
        public string fiPhone { get; set; }
        public string fiEmail { get; set; }
        public string fiBirthPlace { get; set; }
        public string fiBirthDate { get; set; }
        public string fiCity { get; set; }
        public string fiGender { get; set; }
        public string fiUsername { get; set; }
        public string fiPassword { get; set; }
        public string fiNull { get; set; }
    }
}
