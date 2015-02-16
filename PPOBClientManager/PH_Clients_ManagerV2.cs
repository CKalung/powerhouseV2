using System;
using LOG_Handler;
using StaticCommonLibrary;

namespace PPOBClientsManager
{
	public class PH_Clients_ManagerV2 : IDisposable
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
		~PH_Clients_ManagerV2()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			//            FinnetKeeper.Dispose();
			//NitrogenOfflineProcessor.Dispose();
		}

		string appPath;

		public PH_Clients_ManagerV2(string AppPath, bool consoleMode)
		{
			LogWriter.ConsoleMode = consoleMode;
			appPath = AppPath;
			//loadConfig();
			//CommonLibrary.SessionMinutesTimeout = CommonConfigs.getInt("SessionMinutesTimeout");
			//MAX_CLIENT = TotClients;
			//NitrogenOfflineProcessor = new NitroClient(CommonConfigs);
		}

		ConnectionServerPluginHttp.HttpServerPlugin httpPlug;



		public bool onStart()
		{
			httpPlug = new ConnectionServerPluginHttp.HttpServerPlugin ();
			httpPlug.Start (appPath);
			return true;
		}

		public bool onStop()
		{
			try{
				httpPlug.Stop ();} catch{
			}
			if(httpPlug != null)
				httpPlug.Dispose ();
			return true;
		}

	}
}

