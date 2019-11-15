using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rapi
{
    public interface IRapiSystemInfoRpc
    {
        Task<RapiSystemInfo> GetSystemInfo();
    }

    public class RapiSystemInfo
    {
        public RapiPlatformInfo Platform { get; set; }
        public List<RapiNetworkInterfaceInfo> NetworkInterfaces { get; set; }
    }

    public class RapiNetworkAddressV4Info
    {
        public string Address { get; set; }
        public string Netmask { get; set; }
    }
    
    public class RapiNetworkInterfaceInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        // ReSharper disable once InconsistentNaming
        public List<RapiNetworkAddressV4Info> IPv4Addresses { get; set; }
    }

    public class RapiPlatformInfo
    {
        public bool IsWindows { get; set; }
        public bool IsUnix { get; set; }
        public bool IsLinux { get; set; }
        public bool IsOSX { get; set; }
    }
}