using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace RegisterCertificate
{
    [RunInstaller(true)]
    public partial class RegisteInstaller : System.Configuration.Install.Installer
    {
        public RegisteInstaller()
        {
            InitializeComponent();
            var processInstaller = new ServiceProcessInstaller();
            var serviceInst = new System.ServiceProcess.ServiceInstaller();

            //set the privileges
            processInstaller.Account = ServiceAccount.LocalService;

            serviceInst.DisplayName = "Register ClientSide for Web LamipelVN";
            serviceInst.StartType = ServiceStartMode.Manual;

            //must be the same as what was set in Program's constructor
            serviceInst.ServiceName = "Register ClientSide for Web LamipelVN";

            serviceInst.Description = "Register ClientSide for Web LamipelVN";

            this.Installers.Add(processInstaller);
            this.Installers.Add(serviceInst);
        }
        
    }
}
