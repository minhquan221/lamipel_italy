using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RegisterCertificate
{
    class Program
    {
        static void Main()
        {
            RegisteRun myServ = new RegisteRun();
            myServ.Initialize();


            //ServiceBase[] ServicesToRun;
            //ServicesToRun = new ServiceBase[]
            //{
            //    new RegisteRun()
            //};
            //ServiceBase.Run(ServicesToRun);


        }
       
    }
}
