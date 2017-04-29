using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using Microsoft.VisualBasic.FileIO;

namespace Svenkle.NuGetServer.Service
{
    public class Service : ServiceBase
    {
        private Process _hostRunnerProcess;
        private ProcessStartInfo _hostRunnerProcessStartInfo;
        private const string TempFolderName = "NuGetServer";
        private const string WebsiteFolderName = "Website";
        private const string HostRunnerFolderName = "IIS Express";
        private const string HostRunnerExecutableFilename = "iisexpress.exe";
        private const string HostRunnerInstallationFilename = "iisexpress.msi";
        private const string HostRunnerConfigurationFolderName = "user";
        private string _temporaryPath;
        private string _rootFolderPath;
        private string _hostFolderPath;
        private string _websiteFolderPath;
        private string _hostInstanceConfigurationPath;
        private string _hostInstanceFilePath;
        private string _hostRunnerArguments;
        private int _port;

        private static void Main(string[] args)
        {

            var service = new Service();
            if (Environment.UserInteractive)
            {
                Console.WriteLine($"{typeof(Service).Namespace}");
                service.OnStart(args);
                Console.ReadLine();
                service.Stop();
            }
            else
            {
                Run(service);
            }
        }

        protected override void OnStart(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += ServiceOnUnhandledException;
            AppDomain.CurrentDomain.ProcessExit += ServiceOnProcessExit;

            _temporaryPath = Path.Combine(Path.GetTempPath(), TempFolderName);
            _rootFolderPath = Path.GetDirectoryName(new Uri(typeof(Service).Assembly.CodeBase).LocalPath);
            _hostFolderPath = Path.Combine(_rootFolderPath, HostRunnerFolderName);
            _websiteFolderPath = Path.Combine(_rootFolderPath, WebsiteFolderName);
            _hostInstanceFilePath = Path.Combine(_hostFolderPath, HostRunnerExecutableFilename);
            _hostInstanceConfigurationPath = Path.Combine(_hostFolderPath, HostRunnerConfigurationFolderName);

            ExtractHostRunner();
            ConfigureHostRunner();
            StartHostRunner();
        }

        protected override void OnStop()
        {
            StopHostRunner();
        }

        private void StartHostRunner()
        {
            _hostRunnerProcess.Start();
            _hostRunnerProcess.BeginErrorReadLine();
        }

        private void StopHostRunner()
        {
            if (_hostRunnerProcess != null && !_hostRunnerProcess.HasExited)
            {
                _hostRunnerProcess.CancelErrorRead();
                _hostRunnerProcess.Kill();
                _hostRunnerProcess = null;
            }
        }

        private void ExtractHostRunner()
        {
            var hostRunnerResourceFilePath = ExtractResource(Resources.iisexpress, HostRunnerInstallationFilename);
            var extractedHostRunnerTemporaryFolder = ExtractWindowsInstallPackage(hostRunnerResourceFilePath);
            var extractedHostRunnerFolder = Path.Combine(extractedHostRunnerTemporaryFolder, "WowOnly");

            if (!Directory.Exists(_hostFolderPath))
                Directory.CreateDirectory(_hostFolderPath);

            if (!Directory.Exists(_hostInstanceConfigurationPath))
                Directory.CreateDirectory(_hostInstanceConfigurationPath);

            // Use a VB Move command as C# doesn't allow moving between volumes
            FileSystem.MoveDirectory(extractedHostRunnerFolder, _hostFolderPath, true);
        }

        private void ConfigureHostRunner()
        {
            if (!int.TryParse(ConfigurationManager.AppSettings["port"], out _port))
                _port = 8080;

            _hostRunnerArguments = $"/path:\"{_websiteFolderPath}\" /port:{_port} /systray:{true} /userhome:\"{_hostInstanceConfigurationPath}\" /trace:error";
            _hostRunnerProcessStartInfo = new ProcessStartInfo(_hostInstanceFilePath, _hostRunnerArguments)
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
            Console.WriteLine(nameof(HostRunnerOnExited));
            Stop();
        }

        private void ServiceOnProcessExit(object sender, EventArgs eventArgs)
        {
            Console.WriteLine(nameof(ServiceOnProcessExit));
            StopHostRunner();
        }

        private string ExtractWindowsInstallPackage(string packageFilePath)
        {
            var extractedFolderPath = Path.Combine(_temporaryPath, Path.GetFileNameWithoutExtension(packageFilePath));
            var process = Process.Start("msiexec.exe", $"/a \"{packageFilePath}\" /qb TARGETDIR=\"{extractedFolderPath}\" /quiet");
            process.WaitForExit();
            return extractedFolderPath;
        }

        private string ExtractResource(byte[] resourceData, string resourceName)
        {
            if (!Directory.Exists(_temporaryPath))
                Directory.CreateDirectory(_temporaryPath);

            var extractedFilePath = Path.Combine(_temporaryPath, resourceName);

            using (var stream = new MemoryStream(resourceData))
            using (var file = File.Create(extractedFilePath))
            {
                stream.CopyTo(file);
                return extractedFilePath;
            }
        }

        private void HostRunnerOnErrorDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            Console.WriteLine(dataReceivedEventArgs.Data);
            StopHostRunner();
        }

        private void ServiceOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine((Exception)e.ExceptionObject);
            StopHostRunner();
        }
    }
}