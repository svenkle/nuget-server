using System;
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

		private static void Main(string[] args)
		{
			var service = new Service();
			if (Environment.UserInteractive)
			{
				Console.WriteLine($@"[{typeof(Service).Namespace}]");
				Console.WriteLine(@"Starting...");
				service.OnStart(args);
				Console.WriteLine(@"Started!");
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

			_rootFolder = Path.GetDirectoryName(new Uri(typeof(Service).Assembly.CodeBase).LocalPath);
			_configurationFile = Path.Combine(_rootFolder, ConfigurationFilename);
			_hostInstaller = Path.Combine(_rootFolder, HostInstallerFilename);
			_workingFolder = Path.Combine(Path.GetTempPath(), WorkingFolder);
			_userFolder = Path.Combine(_rootFolder, UserFolder);
			_hostFolder = Path.Combine(_rootFolder, HostFolder);
			_host = Path.Combine(_rootFolder, Host);

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
			if (!File.Exists(_host))
			{
				// Use a VB Move command as C# doesn't allow moving between volumes
				// TODO: Potentially change this as the temp folder is not just for IIS
				ExtractWindowsInstallPackage(_hostInstaller);
				FileSystem.MoveDirectory(Path.Combine(_workingFolder, "WowOnly"), _hostFolder, true);
			}
		}

		private void StartHost()
		{
			_hostRunnerProcess.Start();
			_hostRunnerProcess.BeginErrorReadLine();
		}

		private void StopHost()
		{
			if (_hostRunnerProcess != null && !_hostRunnerProcess.HasExited)
			{
				_hostRunnerProcess.CancelErrorRead();
				_hostRunnerProcess.Kill();
				_hostRunnerProcess = null;
			}
		}

		private void ConfigureHost()
		{
			_hostArguments = $"/config:\"{_configurationFile}\" /systray:{false} /userhome:\"{_userFolder}\" /trace:error";

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
			Console.WriteLine(nameof(HostRunnerOnExited));
			Stop();
		}

		private void ServiceOnProcessExit(object sender, EventArgs eventArgs)
		{
			Console.WriteLine(nameof(ServiceOnProcessExit));
			StopHost();
		}

		private void ExtractWindowsInstallPackage(string packageFilePath)
		{
			Process.Start("msiexec.exe", $"/a \"{packageFilePath}\" /qb targetdir=\"{_workingFolder}\" /quiet").WaitForExit();
		}

		private void HostRunnerOnErrorDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
		{
			Console.WriteLine(dataReceivedEventArgs.Data);
			StopHost();
		}

		private void ServiceOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Console.WriteLine((Exception)e.ExceptionObject);
			StopHost();
		}
	}
}
