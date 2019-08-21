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
                Platform = plat
            });
        }
    }
}