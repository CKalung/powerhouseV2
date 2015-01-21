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
using PPOBClientsManager;

namespace PowerHouse
{
    public partial class ServiceWrapper : ServiceBase
    {
        Clients_Manager application;

        public ServiceWrapper()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            string applicationPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            application = new Clients_Manager(applicationPath, false);
            application.onStart();
        }

        protected override void OnStop()
        {
            application.onStop();
        }

    }
    
}
