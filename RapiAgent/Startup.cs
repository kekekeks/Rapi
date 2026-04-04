using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace RapiAgent
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRapiAgentServices();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRapiAgentRpc();

            app.UseRouting();
            app.UseEndpoints(c =>
            {
                c.MapControllers();
            });
        }
    }
}
