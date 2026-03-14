using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;

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
            if (args.Contains("--service")) 
                host.RunAsService();
            else
                host.Run();
        }
    }
}