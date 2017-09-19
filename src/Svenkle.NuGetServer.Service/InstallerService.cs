using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

using NLog;

namespace Svenkle.NuGetServer.Service
{
    [RunInstaller(true)]
    public class InstallerService : Installer
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public InstallerService()
        {
            const string name = "NuGetServer";
            logger.Info($"Start Installing {name} service");

            var processInstaller = new ServiceProcessInstaller();
            var serviceInstaller = new ServiceInstaller();

            //set the privileges
            processInstaller.Account = ServiceAccount.LocalSystem;
            serviceInstaller.DisplayName = name;
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            //must be the same as what was set in Program's constructor
            serviceInstaller.ServiceName = name;
            this.Installers.Add(processInstaller);
            this.Installers.Add(serviceInstaller);

            logger.Info($"Done Installing {name} service");
        }
    }
}
