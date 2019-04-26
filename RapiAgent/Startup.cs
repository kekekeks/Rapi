using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreRPC;
using CoreRPC.AspNetCore;
using CoreRPC.Binding.Default;
using CoreRPC.Routing;
using CoreRPC.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace RapiAgent
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCoreRpc("/rpc", new Engine(new JsonMethodCallSerializer(true),
                new DefaultMethodBinder()).CreateRequestHandler(new DictionaryTargetSelector
            {
                ["FileSystem"] = new FileSystemRpc(),
                ["Processes"] = new ProcessRpc(new UnixProcessFactory())
            }));
        }
    }

    class DictionaryTargetSelector : Dictionary<string, object>, ITargetSelector
    {
        public object GetTarget(string target, object callContext)
        {
            return this[target];
        }
    }
    
    class RpcNameAttribute : Attribute
    {
        public string Name { get; }

        public RpcNameAttribute(string name)
        {
            Name = name;
        }
    }
}