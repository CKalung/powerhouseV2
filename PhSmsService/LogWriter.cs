using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;

namespace PH_SmsService
{
    public static class LogWriter
    {
        public enum logCodeEnum { INFO, DEBUG, WARNING, ERROR }

        //static logCodeEnum logCode = logCodeEnum.INFO;
        static string logPath = "";
        //static string transactionLogFile = "DMPlog";
        static string fileName = "DMP";

        public static bool ConsoleMode = false;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void setPath(string LogPath)
        {
            logPath = LogPath;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void showDEBUG(object classSource, string Message)
        {
            if (!ConsoleMode) return;
            DateTime skrg = DateTime.Now;
            string message = "[" + skrg.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] " +
                " DEBUG :: " +
                classSource.GetType().Namespace + "." +
                classSource.GetType().Name + ": \r\n" +
                Message + "\r\n\r\n";
            Console.WriteLine(message);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void show(object classSource, string Message)
        {
            if (!ConsoleMode) return;
            DateTime skrg = DateTime.Now;
            string message = "[" + skrg.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] " +
                " INFO :: " +
                classSource.GetType().Namespace + "." +
                classSource.GetType().Name + ": \r\n" +
                Message + "\r\n";
            Console.WriteLine(message);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void write(object classSource, logCodeEnum LogCode, string Message)
        {
            DateTime skrg = DateTime.Now;
            string message = "[" + skrg.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] " +
                LogCode.ToString() + " :: " +
                classSource.GetType().Namespace + "." +
                classSource.GetType().Name + ": \r\n" +
                Message + "\r\n\r\n";
            if (ConsoleMode) Console.WriteLine(message);

            if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);
			string newPath = Path.Combine(logPath, "SmsLOG-" + skrg.ToString("yyMM"));

            if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);
            //newPath += "/"+skrg.ToString("MMdd");
            newPath = Path.Combine(newPath, skrg.ToString("MMdd"));
            if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);

            string fln = fileName + "-" + skrg.ToString("yyMMddHH") + ".txt";
            //string path = newPath + "/" + fileName + "-" + skrg.ToString("yyMMddHH") + ".txt";
            string path = Path.Combine(newPath, fln);

            try
            {
                if (!File.Exists(path))
                {
                    using (StreamWriter sw = File.CreateText(path))
                    {
                        sw.WriteLine(message);
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(path))
                    {
                        sw.WriteLine(message);
                    }
                }
            }
            catch { }
        }

    }
}
