# Getting Started

Deploy `RapiAgent` app to a remote (virtual) machine. Ideally, run it as a service and register using systemd or similar. Make sure to have at least .NET Core 2.2 SDK packages installed.

```sh
# Use appropriate urls instead of 0.0.0.0
dotnet run --urls http://0.0.0.0:5000 --service
```

# Test connectivity

The following commands could help checking if `RapiAgent` works:

```sh
# Basically this is what Rapi.Sandbox project does.
# Again, use appropriate urls instead of 0.0.0.0
cd ./Rapi.Sandbox
dotnet run http://0.0.0.0:5000/rpc
```

# Run tests

Run tests, ideally both on Windows and on Linux.

```sh
# To run tests on Linux, install OpenSSH server localy,
# then create a configuration file for SFTP client.
sudo apt-get install -y --no-install-recommends openssh-server
cd ./Rapi.Tests
echo '{
    "Sftp": {
        "Login": "YOUR_USER",
        "Password": "YOUR_PASSWORD",
        "Host": "localhost"
    }
}' > ./config.local.json
dotnet test
```

```powershell
# To run tests on Windows, install OpenSSH.
powershell
Get-WindowsCapability -Online | ? Name -like 'OpenSSH*'
Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0
Start-Service sshd
Set-Service -Name sshd -StartupType 'Automatic'
Get-NetFirewallRule -Name *ssh*
exit

# Run tests using cmd console.
cd ./Rapi.Tests
echo '{
    "Sftp": {
        "Login": "YOUR_USER",
        "Password": "YOUR_PASSWORD",
        "Host": "localhost"
    }
}' > ./config.local.json
dotnet test
```
