using System;
using System.IO;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using PHConnectionCollectorInterface;
using PHClientHttpHandler;

namespace PHClientHttpConnectionCollector
{
	public class PhHttpConnectionCollector : IConnectionCollector {
		#region Disposable
		private bool disposed = false;
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
		~PhHttpConnectionCollector()
		{
			this.Dispose(false);
		}
		#endregion

		private void disposeAll()
		{
			// disini dispose semua yang bisa di dispose
			CloseAllConnections ();
		}

		string name="";
		int indexConnection = 0;

		//List<PhHttpHandler> ConnectionList = new List<PhHttpHandler>();

		Hashtable ConnectionList = new Hashtable();



		public string Name { 
			get { return name; }
			set { name = value; }
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public long GetConnectionCount() {
			return ConnectionList.Count; 
		}

		public PhHttpConnectionCollector ()
		{
		}

		// Untuk sekarang... UNLIMITED CONNECTION COUNT !!!
		public void NewConnection(Stream stream, TcpClient client){
			if (ConnectionList.ContainsKey (indexConnection)) {
				if (client != null)
					client.Close();
				if (stream != null) {
					stream.Close();
					stream.Dispose ();
				}
				return;
			}
			PhHttpHandler HttpHandler = new PhHttpHandler (indexConnection);
			HttpHandler.onDisconnected += new PhHttpHandler.onDisconnectedEvent (onDisconnected);
			ConnectionList.Add (indexConnection, HttpHandler);
			((PhHttpHandler)(ConnectionList[indexConnection])).Start (stream, client);
			indexConnection++;
			if (indexConnection == int.MaxValue)
				indexConnection = 0;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		private void onDisconnected(int indexConn){
			if (ConnectionList.ContainsKey (indexConn)) {
				try{ ((PhHttpHandler)ConnectionList [indexConn]).Dispose (); }catch{
				}
				ConnectionList.Remove (indexConn);
			}
		}

		private void CloseAllConnections(){
			foreach (DictionaryEntry aConn in ConnectionList) {
				((PhHttpHandler)aConn.Value).Dispose ();
			}
			ConnectionList.Clear ();
		}
	}
}

