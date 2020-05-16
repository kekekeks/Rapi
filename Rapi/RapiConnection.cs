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
        public IRapiWebRequestRpc WebRequest { get; }
        public IRapiFileStream RapiFileStream { get; }
        public RapiSystemInfo SystemInfo { get; private set; }
        public RapiFileSystemInfo FileSystemInfo { get; private set; }
        public RapiPath Path { get; private set; }

        internal static CoreRPC.Engine CreateEngine() =>
            new CoreRPC.Engine(new JsonMethodCallSerializer(true), new DefaultMethodBinder());
        
        private RapiConnection(IClientTransport transport, IRapiFileStream rapiFileStream)
        {
            RapiFileStream = rapiFileStream;
            var engine = CreateEngine();
            SystemInfoRpc = engine.CreateProxy<IRapiSystemInfoRpc>(transport, new ConstTargetExtractor("SystemInfo"));
            FileSystem = engine.CreateProxy<IRapiFileSystemRpc>(transport, new ConstTargetExtractor("FileSystem"));
            Processes = engine.CreateProxy<IRapiProcesses>(transport, new ConstTargetExtractor("Processes"));
            Sftp = engine.CreateProxy<IRapiSftpRpc>(transport, new ConstTargetExtractor("Sftp"));
            WebRequest = engine.CreateProxy<IRapiWebRequestRpc>(transport, new ConstTargetExtractor("WebRequest"));
        }

        public static async Task<RapiConnection> Connect(IClientTransport transport, IRapiFileStream rapiFileStream)
        {
            var conn = new RapiConnection(transport, rapiFileStream);
            conn.SystemInfo = await conn.SystemInfoRpc.GetSystemInfo();
            conn.Path = new RapiPath(conn.SystemInfo.Platform);
            conn.FileSystemInfo = await conn.FileSystem.GetFileSystemInfo();
            return conn;
        }
    }
    
    class ConstTargetExtractor : ITargetNameExtractor
    {
        private readonly string _name;

        public ConstTargetExtractor(string name) => _name = name;

        public string GetTargetName(Type interfaceType) => _name;
    }
}