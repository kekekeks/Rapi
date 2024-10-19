using System.Collections.Generic;
using CoreRPC;
using CoreRPC.Binding.Default;
using CoreRPC.Routing;
using CoreRPC.Serialization;
using CoreRPC.Transport;

namespace Rapi.Mocks
{
    public class MockRapiMachine
    {
        class DictionaryTargetSelector : Dictionary<string, object>, ITargetSelector
        {
            public object GetTarget(string target, object callContext) => this[target];
        }
        
        public MockFileSystem FileSystem { get; }
        public RapiSftpMock Sftp { get; }
        public RapiWebRequestMock WebRequest { get; } = new RapiWebRequestMock();
        public RapiProcessesMock Processes { get; } = new RapiProcessesMock();
        public MockRapiMachine(RapiSystemInfo info)
        {
            FileSystem = new MockFileSystem(info.Platform.IsUnix);
            FileStream = FileSystem;
            
            var engine = new Engine(
                    new JsonMethodCallSerializer(),
                    new DefaultMethodBinder())
                .CreateRequestHandler(new DictionaryTargetSelector
                {
                    ["FileSystem"] = FileSystem,
                    ["Processes"] = Processes,
                    ["SystemInfo"] = new MockSystemInfo(info),
                    ["Sftp"] = Sftp,
                    ["WebRequest"] = WebRequest
                });
            Transport = new InProcTransport(engine);
        }

        internal IClientTransport Transport { get; }
        internal IRapiFileStream FileStream { get; }
    }
}