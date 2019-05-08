using System;
using System.Threading.Tasks;
using CoreRPC.Binding.Default;
using CoreRPC.Routing;
using CoreRPC.Serialization;
using CoreRPC.Transport;

namespace Rapi
{
    public class RapiConnection
    {
        public IRapiFileSystemRpc FileSystem { get; }
        public IRapiSystemInfoRpc SystemInfoRpc { get; }
        public IRapiProcesses Processes { get; }
        public RapiSystemInfo SystemInfo { get; private set; }
        public RapiFileSystemInfo FileSystemInfo { get; private set; }
        public RapiPath Path { get; private set; }
        
        class ConstExtractor : ITargetNameExtractor
        {
            public string Name { get; }

            public ConstExtractor(string name)
            {
                Name = name;
            }
            
            public string GetTargetName(Type interfaceType)
            {
                return Name;
            }
        }
        
        RapiConnection(IClientTransport transport)
        {
            var engine = new CoreRPC.Engine(new JsonMethodCallSerializer(true), new DefaultMethodBinder());

            SystemInfoRpc = engine.CreateProxy<IRapiSystemInfoRpc>(transport, new ConstExtractor("SystemInfo"));
            FileSystem = engine.CreateProxy<IRapiFileSystemRpc>(transport, new ConstExtractor("FileSystem"));
            Processes = engine.CreateProxy<IRapiProcesses>(transport, new ConstExtractor("Processes"));
        }

        public static async Task<RapiConnection> Connect(IClientTransport transport)
        {
            var conn = new RapiConnection(transport)
            {

            };
            conn.SystemInfo = await conn.SystemInfoRpc.GetSystemInfo();
            conn.Path = new RapiPath(conn.SystemInfo.Platform);
            conn.FileSystemInfo = await conn.FileSystem.GetFileSystemInfo();
            return conn;
        }

    }
}