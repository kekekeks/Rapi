using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Rapi.Tests
{
    public sealed class RapiTestHost : IDisposable
    {
        public sealed class RapiTestConfiguration
        {
            public RapiSftpCredentials Sftp { get; set; }
        }

        private const string ConfigName = "config.json";
        private const string ConfigLocalName = "config.local.json";
        
        private static readonly RapiTestConfiguration Config;
        private static int _globalPort = 5000;
        
        static RapiTestHost()
        {
            var json = new JObject();
            var assembly = typeof(RapiTestHost).Assembly.Location;
            var directory = Path.GetDirectoryName(assembly);

            foreach (var name in new[] { ConfigName, ConfigLocalName })
            {
                var path = Path.Combine(directory, name);
                if (!File.Exists(path)) continue;

                var text = File.ReadAllText(path);
                var content = JsonConvert.DeserializeObject<JObject>(text);
                json.Merge(content, new JsonMergeSettings
                {
                    MergeNullValueHandling = MergeNullValueHandling.Ignore,
                    MergeArrayHandling = MergeArrayHandling.Replace
                });
            }

            Config = json.ToObject<RapiTestConfiguration>();
        }
        
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly int _port = NextPort();
        private readonly string _urls;
        
        public RapiTestHost()
        {
            _urls = $"http://127.0.0.1:{_port}";
            
            // Clean temporary directory named 'rapi_test500N'.
            var temp = Path.GetTempPath();
            var test = Path.Combine(temp, DirectoryName);
            if (Directory.Exists(test))
                Directory.Delete(test, true);
            
            // Start rapi agent.
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<RapiAgent.Startup>()
                .UseUrls(_urls)
                .Build();
                
#pragma warning disable 4014
            host.RunAsync(_cancellation.Token);
#pragma warning restore 4014
        }

        public string Address => _urls + "/rpc";

        public string DirectoryName => "rapi_test" + _port;

        public RapiTestConfiguration Configuration => Config;
        
        public void Dispose() => _cancellation.Cancel();

        private static int NextPort() => Interlocked.Increment(ref _globalPort);
    }
}