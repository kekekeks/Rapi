using System.Linq;
using System.Runtime.InteropServices;
using CoreRPC;
using CoreRPC.AspNetCore;
using CoreRPC.Binding.Default;
using CoreRPC.Routing;
using CoreRPC.Serialization;
using Microsoft.Extensions.Hosting;
using RapiAgent;
using RapiAgent.Processes;
using RapiAgent.Rpc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc(o => o.EnableEndpointRouting = false);

if (args.Contains("--service"))
    builder.Host.UseWindowsService();

var app = builder.Build();

var processFactory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    ? (IProcessFactory)new Win32ProcessFactory()
    : new UnixProcessFactory();

app.UseCoreRpc("/rpc", new Engine(
        new JsonMethodCallSerializer(),
        new DefaultMethodBinder())
    .CreateRequestHandler(new DictionaryTargetSelector
    {
        ["FileSystem"] = new RapiFileSystemRpc(),
        ["Processes"] = new RapiProcessesRpc(processFactory),
        ["SystemInfo"] = new RapiSystemInfoRpc(),
        ["Sftp"] = new RapiSftpRpc(),
        ["WebRequest"] = new RapiWebRequestRpc()
    }));

app.MapControllers();

await app.RunAsync();