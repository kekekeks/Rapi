using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Rapi.Tests
{
    [SetUpFixture]
    public sealed class RapiTestHost
    {
        public sealed class RapiTestConfiguration
        {
            public RapiSftpCredentials? Sftp { get; set; }
        }

        private const string ConfigName = "config.json";
        private const string ConfigLocalName = "config.local.json";

        public static RapiTestConfiguration Config { get; private set; } = new();
        private static int _globalPort = 5000;

        private CancellationTokenSource? _cancellation;
        private static int _port;
        private static string _urls = string.Empty;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var json = new JObject();
            var assembly = typeof(RapiTestHost).Assembly.Location;
            var directory = Path.GetDirectoryName(assembly) ?? ".";

            foreach (var name in new[] { ConfigName, ConfigLocalName })
            {
                var path = Path.Combine(directory, name);
                if (!File.Exists(path)) continue;

                var text = File.ReadAllText(path);
                var content = JsonConvert.DeserializeObject<JObject>(text);
                if (content != null)
                    json.Merge(content, new JsonMergeSettings
                    {
                        MergeNullValueHandling = MergeNullValueHandling.Ignore,
                        MergeArrayHandling = MergeArrayHandling.Replace
                    });
            }

            Config = json.ToObject<RapiTestConfiguration>() ?? new RapiTestConfiguration();

            _port = NextPort();
            _urls = $"http://127.0.0.1:{_port}";

            // Clean temporary directory named 'rapi_testN'.
            var temp = Path.GetTempPath();
            var test = Path.Combine(temp, DirectoryName);
            if (Directory.Exists(test))
                Directory.Delete(test, true);

            // Start rapi agent.
            _cancellation = new CancellationTokenSource();
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<RapiAgent.Startup>()
                .UseUrls(_urls)
                .Build();

#pragma warning disable 4014
            host.RunAsync(_cancellation.Token);
#pragma warning restore 4014
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
        }

        public static string Address => _urls + "/rpc";
        public static string DirectoryName => "rapi_test" + _port;

        private static int NextPort() => Interlocked.Increment(ref _globalPort);
    }
}
