
using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;
using System.ComponentModel.Composition;

using LOG_Handler;
using PPOBManager;
using PPOBHttpRestData;
using StaticCommonLibrary;
using MyTcpClientServerV2;
using PHClientProtocolTranslatorInterface;

using PHClientHandlerInterface;

namespace PHClientHttpHandler
{
	[Export(typeof(IPhClientHandler))]
	public class PhClientHandlerTemplate : BaseClientHandlers {

		#region Disposable
		private bool disposed = false;
		public virtual void Dispose()
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
		~PhClientHandlerTemplate()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			dataHandler.Dispose ();
			if(CommonConfigs.localDb != null)
				CommonConfigs.localDb.Dispose ();
		}

		ControlCenter PPOBProcessor;
		PublicSettings.Settings CommonConfigs;


		protected PPOBDatabase.PPOBdbLibs localDB;

		public PhClientHandlerTemplate (string name, int indexConnection, string configFilePath)
			:base(name,indexConnection,configFilePath)
		{
			//configFilePath = ConfigFilePath;
			//commonSettings = CommonConfigs;
		}

		// abstract tuh HARUS di override, virtual boleh tidak
		public virtual void DataReceived(MyTcpStreamHandler.ConnectionStateObject State){
		}


	}
}

