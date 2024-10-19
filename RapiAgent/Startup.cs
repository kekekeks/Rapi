using System.Collections.Generic;
using System.Runtime.InteropServices;
using CoreRPC;
using CoreRPC.AspNetCore;
using CoreRPC.Binding.Default;
using CoreRPC.Routing;
using CoreRPC.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RapiAgent.Processes;
using RapiAgent.Rpc;

namespace RapiAgent
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddRouting();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var processFactory = RuntimeInformation
                .IsOSPlatform(OSPlatform.Windows)
                    ? (IProcessFactory) new Win32ProcessFactory()
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

            app.UseRouting();
            app.UseEndpoints(c =>
            {
                c.MapControllers();
            });
        }
    }

    internal class DictionaryTargetSelector : Dictionary<string, object>, ITargetSelector
    {
        public object GetTarget(string target, object callContext) => this[target];
    }
}