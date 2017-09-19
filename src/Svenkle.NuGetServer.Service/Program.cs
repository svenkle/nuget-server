using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Reflection;

using Microsoft.VisualBasic.FileIO;

using NLog;

namespace Svenkle.NuGetServer.Service
{
    public class Service : ServiceBase
    {
        private Process _hostRunnerProcess;
        private ProcessStartInfo _hostRunnerProcessStartInfo;
        private const string ConfigurationFilename = "Host\\Website\\Configuration\\applicationhost.config";
        private const string HostInstallerFilename = "Resources\\iisexpress.msi";
        private const string UserFolder = "Host\\Website\\User";
        private const string Host = "Host\\iisexpress.exe";
        private const string WorkingFolder = "NuGetServer";
        private const string HostFolder = "Host";
        private string _configurationFile;
        private string _hostInstaller;
        private string _hostArguments;
        private string _workingFolder;
        private string _userFolder;
        private string _rootFolder;
        private string _hostFolder;
        private string _host;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static void Main(string[] args)
        {
            var service = new Service();

            if (Environment.UserInteractive)
            {

                string parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                    case "-i":
                        ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                        break;
                    case "--uninstall":
                    case "-u":
                        ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                        break;
                    default:
                        Console.WriteLine($@"[{typeof(Service).Namespace}]");
                        logger.Info($@"[{typeof(Service).Namespace}]");
                        Console.WriteLine(@"Starting from command prompt");
                        logger.Info(@"Starting from command prompt");
                        service.OnStart(args);
                        Console.WriteLine(@"Started!");
                        logger.Info(@"Started");
                        Console.ReadLine();
                        service.Stop();
                        break;
                }
            }
            else
            {
                logger.Info(@"Starting as a service");
                Run(service);
            }
        }

        public Service()
        {
            this.ServiceName = "NuGetServer";
        }

        protected override void OnStart(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += ServiceOnUnhandledException;
            AppDomain.CurrentDomain.ProcessExit += ServiceOnProcessExit;

            _rootFolder = Path.GetDirectoryName(new Uri(typeof(Service).Assembly.CodeBase).LocalPath);
            _configurationFile = Path.Combine(_rootFolder, ConfigurationFilename);
            _hostInstaller = Path.Combine(_rootFolder, HostInstallerFilename);
            _workingFolder = Path.Combine(Path.GetTempPath(), WorkingFolder);
            _userFolder = Path.Combine(_rootFolder, UserFolder);
            _hostFolder = Path.Combine(_rootFolder, HostFolder);
            _host = Path.Combine(_rootFolder, Host);

            logger.Info($"rootFolder : {_rootFolder}");
            logger.Info($"configurationFile : {_configurationFile}");
            logger.Info($"hostInstaller : {_hostInstaller}");
            logger.Info($"workingFolder : {_workingFolder}");
            logger.Info($"userFolder : {_userFolder}");
            logger.Info($"host : {_host}");

            ExtractHost();
            ConfigureHost();
            StartHost();
        }

        protected override void OnStop()
        {
            StopHost();
        }

        private void ExtractHost()
        {
            logger.Info(@"Call ExtractHost");
            if (!File.Exists(_host))
            {
                logger.Info($"File exists : {_host}");
                // Use a VB Move command as C# doesn't allow moving between volumes
                // TODO: Potentially change this as the temp folder is not just for IIS
                ExtractWindowsInstallPackage(_hostInstaller);
                FileSystem.MoveDirectory(Path.Combine(_workingFolder, "WowOnly"), _hostFolder, true);
            }
        }

        private void StartHost()
        {
            logger.Info(@"Call StartHost");
            _hostRunnerProcess.Start();
            _hostRunnerProcess.BeginErrorReadLine();
        }

        private void StopHost()
        {
            logger.Info(@"Call StopHost");
            if (_hostRunnerProcess != null && !_hostRunnerProcess.HasExited)
            {
                logger.Info(@"Stoping service");
                _hostRunnerProcess.CancelErrorRead();
                _hostRunnerProcess.Kill();
                _hostRunnerProcess = null;
            }
        }

        private void ConfigureHost()
        {
            logger.Info(@"Call ConfigureHost");

            _hostArguments = $"/config:\"{_configurationFile}\" /systray:{false} /userhome:\"{_userFolder}\" /trace:error";
            logger.Info($"hostArguments = {_hostArguments}");

            _hostRunnerProcessStartInfo = new ProcessStartInfo(_host, _hostArguments)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = false
            };

            _hostRunnerProcess = new Process
            {
                StartInfo = _hostRunnerProcessStartInfo,
                EnableRaisingEvents = true
            };

            _hostRunnerProcess.Exited += HostRunnerOnExited;
            _hostRunnerProcess.ErrorDataReceived += HostRunnerOnErrorDataReceived;
        }

        private void HostRunnerOnExited(object sender, EventArgs eventArgs)
        {
            logger.Info(@"Call HostRunnerOnExited");
            Console.WriteLine(nameof(HostRunnerOnExited));
            logger.Info(nameof(HostRunnerOnExited));
            Stop();
        }

        private void ServiceOnProcessExit(object sender, EventArgs eventArgs)
        {
            logger.Info(@"Call ServiceOnProcessExit");
            Console.WriteLine(nameof(ServiceOnProcessExit));
            logger.Info(nameof(ServiceOnProcessExit));
            StopHost();
        }

        private void ExtractWindowsInstallPackage(string packageFilePath)
        {
            logger.Info(@"Call ExtractWindowsInstallPackage");
            logger.Info($"packageFilePath = {packageFilePath}");
            Process.Start("msiexec.exe", $"/a \"{packageFilePath}\" /qb targetdir=\"{_workingFolder}\" /quiet").WaitForExit();
        }

        private void HostRunnerOnErrorDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            logger.Info(@"Call HostRunnerOnErrorDataReceived");
            Console.WriteLine(dataReceivedEventArgs.Data);
            logger.Info($"dataReceivedEventArgs.Data = {dataReceivedEventArgs.Data}");
            StopHost();
        }

        private void ServiceOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Info(@"Call ExtractWindowsInstallPackage");
            Console.WriteLine((Exception)e.ExceptionObject);
            logger.Info($"Exception = {((Exception)e.ExceptionObject).Message}");
            logger.Info($"Exception = {((Exception)e.ExceptionObject).StackTrace}");
            StopHost();
        }
    }
}