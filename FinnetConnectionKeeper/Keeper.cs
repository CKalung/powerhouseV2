using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using FinnetHandler;


namespace FinnetConnectionKeeper
{
    public class Keeper: IDisposable
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
        ~Keeper()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            Finnet.Dispose();
        }

        private Thread FinnetThread;
        private FinnetTransactions Finnet;

        bool fExit = true;
        bool fExited = false;
       PublicSettings.Settings commonSettings;

        public Keeper(PublicSettings.Settings CommonSettings)
        {
            commonSettings = CommonSettings;
            FinnetThread = new Thread(new ThreadStart(FinnetKeeper));
            Finnet = new FinnetTransactions(commonSettings);
        }

        public void Stop()
        {
            fExit = true;
            while (!fExited)
            {
                Thread.Sleep(100);
            }
        }

        public void Start()
        {
            fExited = false;
            fExit = false;
            FinnetThread.Start();
        }

        private void FinnetKeeper()
        {
            //int ctr = 600;       // 10 = 1 detik, 600 = 1 menit
            int ctr = 1200;       // 10 = 1 detik, 600 = 1 menit
            int cnt = 10;       // 1 detik pertama

            // Sign ON
            bool fSignOn = Finnet.productSignOn();
            if (fSignOn) cnt = ctr;
            while (!fExit)
            {
                try
                {
                    // do any background work
                    Thread.Sleep(100);
                    cnt--;
                    if (cnt > 0) continue;
                    cnt = ctr;
                    if (!fSignOn)
                    {
                        fSignOn = Finnet.productSignOn();
                        if (!fSignOn)
                        {
                            cnt = 50;   // delay 5 detik sebelum signon lagi
                            continue;
                        }
                        cnt = ctr;
                        continue;
                    }
                    // Kirim echo test
                    if (!Finnet.productEchoTest())
                    {
                        fSignOn = false;
                        cnt = 30;
                        continue;
                    }
                }
                catch //(Exception ex)
                {
                    // log errors
                }
            }
            Finnet.productSignOff();
            fExited = true;
        }



    }
}
