using System.Linq;
using System.Runtime.InteropServices;
using CoreRPC;
using CoreRPC.AspNetCore;
using CoreRPC.Binding.Default;
using CoreRPC.Routing;
using CoreRPC.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RapiAgent;
using RapiAgent.Config;
using RapiAgent.Config.Options;
using RapiAgent.Processes;
using RapiAgent.Rpc;

string? configPath = null;
for (var i = 0; i < args.Length - 1; i++)
    if (args[i] == "--config") { configPath = args[i + 1]; break; }

var builder = WebApplication.CreateBuilder(args);

if (configPath != null)
    builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: false);

builder.Services.AddMvc(o => o.EnableEndpointRouting = false);

var rapiOptions = builder.Configuration.Get<RapiOptions>() ?? new RapiOptions();
var rapiConfig = RapiConfig.Convert(rapiOptions);
builder.Services.AddSingleton(rapiConfig);

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