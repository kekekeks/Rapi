using System;
using System.Text;
using System.Threading.Tasks;
using CoreRPC.Binding.Default;
using CoreRPC.Routing;
using CoreRPC.Serialization;
using CoreRPC.Transport.Http;
using Newtonsoft.Json;
using Rapi;

namespace Sandbox
{
    class Program
    {
        class ConstExtractor : ITargetNameExtractor
        {
            public string Name { get; }

            public ConstExtractor(string name)
            {
                Name = name;
            }
            
            public string GetTargetName(Type interfaceType)
            {
                return Name;
            }
        }
        
        static async Task Main(string[] args)
        {
            var engine = new CoreRPC.Engine(new JsonMethodCallSerializer(true), new DefaultMethodBinder());
            var transport = new HttpClientTransport(args[0]);
            var sysInfoRpc = engine.CreateProxy<IRapiSystemInfoRpc>(transport, new ConstExtractor("SystemInfo"));
            var fs = engine.CreateProxy<IRapiFileSystemRpc>(transport, new ConstExtractor("FileSystem"));
            var proc = engine.CreateProxy<IRapiProcesses>(transport, new ConstExtractor("Processes"));

            var sysInfo = await sysInfoRpc.GetSystemInfo();
            var fsInfo = await fs.GetFileSystemInfo();
            
            Console.WriteLine(JsonConvert.SerializeObject(fsInfo));
            var path = new RapiPath(sysInfo.Platform);
            /*
            foreach(var f in await fs.GetFiles(fsInfo.TempDirectory))
                Console.WriteLine(f);
*/

            var tempFilePath = path.Combine(fsInfo.TempDirectory, "rapitest.txt");
            await fs.WriteFileContents(tempFilePath, Encoding.UTF8.GetBytes("Hello world!"));

            var shell = sysInfo.Platform.IsWindows
                ? @"cmd.exe"
                : "bash";

            var shellArgs = sysInfo.Platform.IsWindows ? new[]{"/D"} : null;

            var cat = sysInfo.Platform.IsWindows ? "type" : "cat";

            var encoding = Encoding.UTF8;
            var nl = sysInfo.Platform.IsWindows ? "\r\n" : "\n";
            
            async Task WaitForExit()
            {
                int? exitCode;
                while ((exitCode = (await proc.GetExitCode("test"))) == null)
                    await Task.Delay(100);
                foreach(var stderr in new []{false, true})
                {
                    var outBytes = await proc.GetOutput("test", stderr);

                    var s = outBytes == null ? null : encoding.GetString(outBytes);
                    Console.WriteLine((stderr ? "Stderr:\n" : "Stdout:\n") + s);
                }
                Console.WriteLine("Exit code " + exitCode);
            }

            foreach (var mode in new[] {0, 1, 2})
            {
                Console.WriteLine($"====================\nRunning mode {mode}\n====================");
                await proc.Start("test", new ProcessCreationOptions(shell, shellArgs)
                {
                    MergeStderr = mode == 2
                });
                await proc.WriteStdIn("test", encoding.GetBytes("echo test-test" + nl));
                await proc.WriteStdIn("test",
                    encoding.GetBytes(cat + " " + tempFilePath + (mode > 0 ? ">&2" : "") + nl));
                await Task.Delay(1000);
                await proc.CloseStdIn("test");
                await WaitForExit();
            }

            Console.WriteLine($"====================\nTesting the process kill feature\n====================");
            await proc.Start("test", new ProcessCreationOptions(shell, shellArgs));
            await Task.Delay(1000);
            await proc.Kill("test");
            await WaitForExit();

        }
    }
}