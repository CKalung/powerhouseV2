using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;

namespace LogToDBService
{
    public partial class ServiceWrapper : ServiceBase
    {
        TransactionLogInjector service;

        public ServiceWrapper()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            string applicationPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            //TransactionLogInjector service = new TransactionLogInjector(applicationPath, false);
            service = new TransactionLogInjector(applicationPath, false);
            service.onStart();
        }

        protected override void OnStop()
        {
            service.onStop();
        }

    }
    
}
