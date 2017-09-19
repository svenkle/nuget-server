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
### Install service from Command Line
* Open command promt as admin
* Chnage directory do where you installed NuGet Server
* Run **Svenkle.NuGetServer.Service.exe -i**

### Host configuration
You can configure many different features of the host using the **applicationhost.config** file located in **Host\\Website\\Configuration**.

### API Key, Package location etc.
You can customise all of the normal NuGet.Server functionality by editing the **Web.config** located in **Host\\Website**. Further documentation on the configuration settings can be found [here](https://docs.microsoft.com/en-us/nuget/hosting-packages/nuget-server).

## FAQ
### How is this different to the Cassini based NuGet Server?
It's simple! This one is free and open-source.

### Why do you bundle IIS Express as an MSI?
In order to increase support for servers that do not have IIS Express installed it must be bundled with the service. The Microsoft license agreement for IIS Express only allows distribution in .msi form.
