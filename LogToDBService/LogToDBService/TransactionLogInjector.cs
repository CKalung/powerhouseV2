using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Permissions;

namespace LogToDBService
{
    class TransactionLogInjector: IDisposable
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
        ~TransactionLogInjector()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            fWatch.Dispose();
            //TransactionLogProcessor.Dispose();
        }

        string appPath;
        string logPath="";
        //string donePath="";
        string errorPath="";

        FileSystemWatcher fWatch;
        ProcLogFile TransactionLogProcessor;
        object ProcessListSync = new object();
        const string fileFilter = "*.txt";
        const string fileExtenstion = ".txt";

        PublicSettings.Settings commonSettings;


        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public TransactionLogInjector(string AppPath, bool consoleMode)
        {
            appPath = AppPath;
            commonSettings = new PublicSettings.Settings();
            loadConfig();

            TransactionLogProcessor = new ProcLogFile(errorPath, commonSettings);
            fWatch = new FileSystemWatcher();
            fWatch.Path = logPath;
            fWatch.Filter = fileFilter;
            fWatch.NotifyFilter = (System.IO.NotifyFilters)((int)System.IO.NotifyFilters.FileName + (int)System.IO.NotifyFilters.LastWrite);
            fWatch.IncludeSubdirectories = true;
            fWatch.Changed += new FileSystemEventHandler(fWatch_Changed);
            fWatch.Created += new FileSystemEventHandler(fWatch_Changed);
            fWatch.Deleted += new FileSystemEventHandler(fWatch_Changed);
            fWatch.Error += new ErrorEventHandler(fWatch_Error);
        }

        void fWatch_Error(object sender, ErrorEventArgs e)
        {

            if (e.GetException().GetType() == typeof(InternalBufferOverflowException))
            {
                Console.WriteLine(("The file system watcher experienced an internal buffer overflow: " + e.GetException().Message));
            }
        }

        void fWatch_Changed(object sender, FileSystemEventArgs e)
        {
            lock (ProcessListSync)
            {
                TransactionLogProcessor.addFile(e.FullPath);
            }
        }

        public void loadConfig()
        {
            //            appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            using (CrossIniFile.INIFile a = new CrossIniFile.INIFile(appPath + "/config.ini"))
            {
                //listeningPort = a.GetValue("TCPServer", "PORT", "10039");
                logPath = a.GetValue("TransactionLog", "LogPath", appPath + "\\TransactionLogs" );
                //donePath = a.GetValue("TransactionLog", "InsertedLogPath", appPath + "\\TransactionLogOK");
                errorPath = a.GetValue("TransactionLog", "FailedLogPath", appPath + "\\TransactionLogError");
                if (!System.IO.Directory.Exists(logPath)) System.IO.Directory.CreateDirectory(logPath);
                if (!System.IO.Directory.Exists(errorPath)) System.IO.Directory.CreateDirectory(errorPath);

                commonSettings.DbHost = a.GetValue("PostgreDB", "Host", "127.0.0.1");
                commonSettings.DbPort =  a.GetValue("PostgreDB", "Port", 5432);
                commonSettings.DbUser = a.GetValue("PostgreDB", "Username", "postgres");
                commonSettings.DbPassw = a.GetValue("PostgreDB", "Password", "");
                commonSettings.DbName = a.GetValue("PostgreDB", "DBName", "");

            }
            Console.WriteLine("LogPath set to " + logPath);
        }

        
        public bool onStart()
        {
            Console.WriteLine("LogToDB Service started");
            fWatch.EnableRaisingEvents = true;

            string[] sFile = System.IO.Directory.GetFiles(logPath, fileFilter);
            foreach (string ln in sFile)
            {
                TransactionLogProcessor.addFile(ln);
            }
            TransactionLogProcessor.Start();

            return true;
        }

        public void onStop()
        {
            fWatch.EnableRaisingEvents = false;
            // tunggu hingga process selesai
            TransactionLogProcessor.Stop();
            //while (!TransactionLogProcessor.isDone) System.Threading.Thread.Sleep(100);
            Console.WriteLine("Stopping service... Done");
        }

    }
}
