# NuGet Server
NuGet Server is essentially a self-hosted wrapper of the [NuGet.Server](https://www.nuget.org/packages/NuGet.Server) package. NuGet Server is distributed with its own IIS Express instance and as such does not require a webserver to be installed on the machine.

## Requirements
* .NET 4.6 or higher
* Windows Server 2012, Windows Server 2016, Windows 7, Windows 8, Windows 8.1 or Windows 10

## Getting Started
### Install as a Windows Service
* Download **Install.msi** from the [Releases](https://github.com/svenkle/nuget-server/releases) page
* Run **Install.msi** as a user with Administrator privledges
* Complete all wizard steps
* Browse to http://localhost:8080

### Run from the Command Line
* Download **NuGetServer.zip** from the [Releases](https://github.com/svenkle/nuget-server/releases) page
* Unzip **NuGetServer.zip** to a location of your choosing
* Run **Svenkle.NuGetServer.Service.exe**

## Configuration
### Listening Port
You can alter the listening port by updating the **port** setting in **Svenkle.NuGetServer.Service.exe.config** located in the application root. Please note that server must run with Administrator privledges in order to listen on port **:80**

### API Key, Package Location etc.
You can customise all of the normal NuGet.Server functionality by editing the **Web.config** located in the Website folder under the application root. Further documentation on the configuration settings can be found [here](https://docs.microsoft.com/en-us/nuget/hosting-packages/nuget-server).

## FAQ
### Why is this better than the Cassini based NuGet Server?
It's Simple! This one is free and open-source.

### Why do you bundle IIS Express as an MSI?
In order to increase support for servers that do not have IIS Express installed it must be bundled with the service. The Microsoft license agreement for IIS Express only allows distribution in .msi form.
