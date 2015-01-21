using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Reflection;
using System.IO;
using System.Text;

namespace LogToDBService
{
    public class Program
    {
        static string applicationName = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Replace(".vshost", "");
        static string applicationPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        static string applicationTitle = "MultiPayments Log2DB Service";

        public Program()
        {
            //set initializers here
            //            devManager = new ClientsManager(applicationPath, TotalDevice);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>        
        static void Main(string[] args)
        //        static void Main()
        {
            //            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.sss+07:00"));
            if (Environment.CommandLine.Contains("-service"))
            {
                if (ServiceCheck(true) == false)
                {
                    ServiceController controller = new ServiceController(applicationName);
                    controller.Start();
                    return;
                }

                ServiceBase[] services = new ServiceBase[] { new ServiceWrapper() };
                ServiceBase.Run(services);
            }
            else if (Environment.CommandLine.Contains("-noservice"))
            {
                if (ServiceCheck(false))
                {
                    ServiceController controller = new ServiceController(applicationName);
                    if (controller.Status == ServiceControllerStatus.Running) controller.Stop();
                    ServiceInstaller.UnInstallService(applicationName);
                }
            }
            else
            {
                TransactionLogInjector service = new TransactionLogInjector(applicationPath, true);
                service.onStart();
                Console.WriteLine("Client Manager Service Started...");
                Console.WriteLine("<press ENTER key to exit...>");
                Console.Read();
                Console.Write("Please wait while stoping service... ");
                service.onStop();
                Console.WriteLine();
                Console.WriteLine("Done");
                Console.WriteLine("Service Closed...");
                Environment.Exit(0);
            }
        }

        static bool ServiceCheck(bool autoInstall)
        {
            bool installed = false;

            ServiceController[] controllers = ServiceController.GetServices();
            foreach (ServiceController con in controllers)
            {
                if (con.ServiceName == applicationName)
                {
                    installed = true;
                    break;
                }
            }

            if (installed) return true;

            if (autoInstall)
            {
                ServiceInstaller.InstallService("\"" + applicationPath + "\\" + applicationName + ".exe\" -service", applicationName, applicationTitle, true, false);
            }

            return false;
        }
    }
}
