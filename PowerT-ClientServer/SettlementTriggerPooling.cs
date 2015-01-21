using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PowerTClientServer
{
	public class SettlementTriggerPooling: IDisposable
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
		~SettlementTriggerPooling()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
		}

		private Thread ToPowerT_Thread;

		bool fExit = true;
		bool fExited = false;
		PublicSettings.Settings commonSettings;

		public SettlementTriggerPooling(PublicSettings.Settings CommonSettings)
		{
			commonSettings = CommonSettings;
			ToPowerT_Thread = new Thread(new ThreadStart(PoolingThread));
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

		private void SettlementToPowerT(){
			// cek table, mun aya kirim settlement ka Power-T
			// Mun beres, tandaan di table bahwa udah di settlement

		}

		private void PoolingThread()
		{
			int ctr = 300;       // 10 = 1 detik, 600 = 1 menit
			//int ctr = 1200;       // 10 = 1 detik, 600 = 1 menit
			int cnt = 10;       // 1 detik pertama

			cnt = 100;		// 10 detik pertama
			while (!fExit)
			{
				try
				{
					// do any background work
					Thread.Sleep(100);
					cnt--;
					if (cnt > 0) continue;
					cnt = ctr;
					if(fExit) break;

					SettlementToPowerT();

				}
				catch //(Exception ex)
				{
					// log errors
				}
			}
			fExited = true;
		}



	}
}
