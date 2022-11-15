using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace RegisterCertificate
{
    partial class RegisteRun : ServiceBase
    {
        //public static TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8080);
        //byte[] bytes = new Byte[1024];
        //public static string data = null;

        //public class SocketData
        //{
        //    public string type { get; set; }
        //    public string encode { get; set; }
        //    public string privateKey { get; set; }

        //}
        public RegisteRun()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Initialize();
        }



        protected override void OnStop()
        {
            //listener.Stop();
            // TODO: Add code here to perform any tear-down necessary to stop your service.
        }

        public void Initialize()
        {
            typeof(Loader).GetMethod("Setup").Invoke(null, new object[] { });
            typeof(IntervalProcess).GetMethod("Runs").Invoke(null, new object[] { });
        }
    }
}
