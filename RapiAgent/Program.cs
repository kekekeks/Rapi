using Microsoft.AspNetCore.Hosting;

namespace RapiAgent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .UseUrls(args[0])
                .Build();
            
                host.Run();
        }
    }
}