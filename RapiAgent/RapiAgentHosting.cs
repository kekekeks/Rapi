using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices;
using CoreRPC;
using CoreRPC.AspNetCore;
using CoreRPC.Binding.Default;
using CoreRPC.Routing;
using CoreRPC.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RapiAgent.Processes;
using RapiAgent.Rpc;

namespace RapiAgent
{
    internal static class RapiAgentHosting
    {
        public static IServiceCollection AddRapiAgentServices(this IServiceCollection services)
        {
            services.AddMvc(o => o.EnableEndpointRouting = false);

            services.AddSingleton<IProcessFactory>(_ => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new Win32ProcessFactory()
                : new UnixProcessFactory());

            services.AddHttpClient(RapiHttpClientNames.GrpcClient)
                .ConfigurePrimaryHttpMessageHandler(CreateOutboundHttpHandler);
            services.AddHttpClient(RapiHttpClientNames.WebRequest)
                .ConfigurePrimaryHttpMessageHandler(CreateOutboundHttpHandler);

            services.AddSingleton<RapiFileSystemRpc>();
            services.AddSingleton<RapiProcessesRpc>();
            services.AddSingleton<RapiSystemInfoRpc>();
            services.AddSingleton<RapiSftpRpc>();
            services.AddSingleton<RapiGrpcClientRpc>();
            services.AddSingleton<RapiWebRequestRpc>();

            return services;
        }

        public static IApplicationBuilder UseRapiAgentRpc(this IApplicationBuilder app)
        {
            var services = app.ApplicationServices;

            app.UseCoreRpc("/rpc", new Engine(
                    new JsonMethodCallSerializer(),
                    new DefaultMethodBinder())
                .CreateRequestHandler(new DictionaryTargetSelector
                {
                    ["FileSystem"] = services.GetRequiredService<RapiFileSystemRpc>(),
                    ["Processes"] = services.GetRequiredService<RapiProcessesRpc>(),
                    ["SystemInfo"] = services.GetRequiredService<RapiSystemInfoRpc>(),
                    ["Sftp"] = services.GetRequiredService<RapiSftpRpc>(),
                    ["GrpcClient"] = services.GetRequiredService<RapiGrpcClientRpc>(),
                    ["WebRequest"] = services.GetRequiredService<RapiWebRequestRpc>()
                }));

            return app;
        }

        private static HttpMessageHandler CreateOutboundHttpHandler() => new SocketsHttpHandler
        {
            UseCookies = false
        };
    }

    internal static class RapiHttpClientNames
    {
        public const string GrpcClient = "GrpcClient";
        public const string WebRequest = "WebRequest";
    }

    internal class DictionaryTargetSelector : Dictionary<string, object>, ITargetSelector
    {
        public object GetTarget(string target, object callContext) => this[target];
    }
}
