using System;
using System.Reflection;
using System.IO;

namespace PH_SmsService
{
	class MainClass
	{
		//static string applicationName = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Replace(".vshost", "");
		static string applicationPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

		public static void Main (string[] args)
		{
			bool consoleMode = false;
			if (Environment.CommandLine.ToLower().Contains("-debug"))
			{
				consoleMode = true;
			}
			SmsService sms = new SmsService(applicationPath, consoleMode);
			if (!sms.Start ()) {
				Console.WriteLine("Sms service failed to start...");
				return;
			}
			Console.WriteLine("Sms service started...");
			if (consoleMode)
			{
				Console.WriteLine("<press ENTER key to exit...>");
				Console.Read();
				Console.Write("Please wait while stoping service... ");
				sms.Stop();
				Console.WriteLine();
				Console.WriteLine("Done");
				Console.WriteLine("Sms service closed...");
				sms.Dispose();
				Environment.Exit(0);
			}
		}
	}
}
