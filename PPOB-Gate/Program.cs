using System;
using System.Collections.Generic;
using System.Text;
//using System.ServiceProcess;
//using System.Configuration.Install;
using System.Reflection;
using System.IO;
using PPOBClientsManager;

namespace PPOB_Gate
{
    class Program
    {
        static string applicationName = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Replace(".vshost", "");
        static string applicationPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        static string applicationTitle = "MultiPayments Service";

        public Program()
        {
            //set initializers here
            //            devManager = new ClientsManager(applicationPath, TotalDevice);
        }

        static Clients_Manager service;
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
            service = new Clients_Manager(applicationPath, consoleMode);
            service.onStart();
            Console.WriteLine("PPOB Service Started...");
            if (consoleMode)
            {
                Console.WriteLine("<press ENTER key to exit...>");
                Console.Read();
                Console.Write("Please wait while stoping service... ");
                service.onStop();
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


//        static void Main(string[] args)
////        static void Main()
//        {
//            bool consoleMode = false;
//            if (Environment.CommandLine.ToLower().Contains("-debug"))
//            {
//                consoleMode = true;
//            }
////            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.sss+07:00"));
//            //if (Environment.CommandLine.Contains("-service"))
//            //{
//            //    if (ServiceCheck(true) == false)
//            //    {
//            //        ServiceController controller = new ServiceController(applicationName);
//            //        controller.Start();
//            //        return;
//            //    }

//            //    ServiceBase[] services = new ServiceBase[] { new ServiceWrapper() };
//            //    ServiceBase.Run(services);
//            //}
//            //else if (Environment.CommandLine.Contains("-noservice"))
//            //{
//            //    if (ServiceCheck(false))
//            //    {
//            //        ServiceController controller = new ServiceController(applicationName);
//            //        if (controller.Status == ServiceControllerStatus.Running) controller.Stop();
//            //        ServiceInstaller.UnInstallService(applicationName);
//            //    }
//            //}
//            //else
//            //{
//            Clients_Manager service = new Clients_Manager(applicationPath, consoleMode);
//                service.onStart();
//                if (consoleMode)
//                {
//                    Console.WriteLine("Client Manager Service Started...");
//                    Console.WriteLine("<press ENTER key to exit...>");
//                    Console.Read();
//                    Console.Write("Please wait while stoping service... ");
//                    service.onStop();
//                    Console.WriteLine();
//                    Console.WriteLine("Done");
//                    Console.WriteLine("Service Closed...");
//                }
//                else
//                {
//                    while (true)
//                    {
////                        Console.Write(".");
//                        System.Threading.Thread.Sleep(1000);
//                    }
//                }
////                Environment.Exit(0);
////            }
//        }

        //static bool ServiceCheck(bool autoInstall)
        //{
        //    bool installed = false;

        //    ServiceController[] controllers = ServiceController.GetServices();
        //    foreach (ServiceController con in controllers)
        //    {
        //        if (con.ServiceName == applicationName)
        //        {
        //            installed = true;
        //            break;
        //        }
        //    }

        //    if (installed) return true;

        //    if (autoInstall)
        //    {
        //        ServiceInstaller.InstallService("\"" + applicationPath + "\\" + applicationName + ".exe\" -service", applicationName, applicationTitle, true, false);
        //    }

        //    return false;
        //}

    }
}
