using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Rapi;

namespace RapiAgent.Rpc
{
    internal class RapiSystemInfoRpc : IRapiSystemInfoRpc
    {
        public Task<RapiSystemInfo> GetSystemInfo()
        {
            var plat = new RapiPlatformInfo
            {
                IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                IsOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
                IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            };
            plat.IsUnix = !plat.IsWindows;

            return Task.FromResult(new RapiSystemInfo
            {
                RapiVersion = 5,
                Platform = plat,
                NetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces().Select(ni =>
                    new RapiNetworkInterfaceInfo
                    {
                        Name = ni.Name,
                        Description = ni.Description,
                        IPv4Addresses = ni.GetIPProperties().UnicastAddresses
                            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                            .Select(a => new RapiNetworkAddressV4Info
                            {
                                Address = a.Address.ToString(),
                                Netmask = a.IPv4Mask.ToString()
                            }).ToList()
                    }).ToList()
            });
        }
    }
}