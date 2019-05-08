using System;
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
    }

    public class RapiPlatformInfo
    {
        public bool IsWindows { get; set; }
        public bool IsUnix { get; set; }
        public bool IsLinux { get; set; }
        public bool IsOSX { get; set; }
    }
}