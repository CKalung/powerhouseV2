using System;
using System.Collections.Generic;
using System.Text;
//using System.ServiceProcess;
//using System.Configuration.Install;
using System.Reflection;
using System.IO;
using PhClientsManager;

namespace PowerHouse
{
	class Program
	{
		//static string applicationName = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Replace(".vshost", "");
		static string applicationPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
		//static string applicationTitle = "MultiPayments Service";

		public Program()
		{
			//set initializers here
			//            devManager = new ClientsManager(applicationPath, TotalDevice);
		}

		static ConnectionsModulesLoader service;
		//static TesLoader service;
		/// <summary>
		/// The main entry point for the application.
		/// </summary>        
		static void Main(string[] args)
		//        static void Main()
		{
			bool consoleMode = false;
			if (Environment.CommandLine.ToLower().Contains("-debug"))
			{
				consoleMode = true;
			}
			service = new ConnectionsModulesLoader(applicationPath, consoleMode);
			//service = new TesLoader(applicationPath, consoleMode);
			service.Start();
			Console.WriteLine("PowerHouse Service Started...");
			if (consoleMode)
			{
				Console.WriteLine("<press ENTER key to exit...>");
				Console.Read();
				Console.Write("Please wait while stoping service... ");
				service.Stop();
				Console.WriteLine();
				Console.WriteLine("Done");
				Console.WriteLine("Service Closed...");
				service.Dispose();
				Environment.Exit(0);
			}
			//else
			//{
			//while (true)
			//{
			//    //                        Console.Write(".");
			//    System.Threading.Thread.Sleep(1000);
			//}
			//}
		}
	}
}
