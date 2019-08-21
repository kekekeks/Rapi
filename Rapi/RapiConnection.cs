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
        public IRapiSftpRpc Sftp { get; }
        public RapiSystemInfo SystemInfo { get; private set; }
        public RapiFileSystemInfo FileSystemInfo { get; private set; }
        public RapiPath Path { get; private set; }

        private class ConstExtractor : ITargetNameExtractor
        {
            private readonly string _name;

            public ConstExtractor(string name) => _name = name;

            public string GetTargetName(Type interfaceType) => _name;
        }
        
        private RapiConnection(IClientTransport transport)
        {
            var engine = new CoreRPC.Engine(new JsonMethodCallSerializer(true), new DefaultMethodBinder());
            SystemInfoRpc = engine.CreateProxy<IRapiSystemInfoRpc>(transport, new ConstExtractor("SystemInfo"));
            FileSystem = engine.CreateProxy<IRapiFileSystemRpc>(transport, new ConstExtractor("FileSystem"));
            Processes = engine.CreateProxy<IRapiProcesses>(transport, new ConstExtractor("Processes"));
            Sftp = engine.CreateProxy<IRapiSftpRpc>(transport, new ConstExtractor("Sftp"));
        }

        public static async Task<RapiConnection> Connect(IClientTransport transport)
        {
            var conn = new RapiConnection(transport);
            conn.SystemInfo = await conn.SystemInfoRpc.GetSystemInfo();
            conn.Path = new RapiPath(conn.SystemInfo.Platform);
            conn.FileSystemInfo = await conn.FileSystem.GetFileSystemInfo();
            return conn;
        }
    }
}