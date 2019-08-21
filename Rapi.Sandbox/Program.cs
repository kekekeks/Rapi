using System;
using System.Text;
using System.Threading.Tasks;
using CoreRPC.Transport.Http;
using Newtonsoft.Json;

namespace Rapi.Sandbox
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var conn = await RapiConnection.Connect(new HttpClientTransport(args[0]));
            Console.WriteLine(JsonConvert.SerializeObject(conn.SystemInfo));
            Console.WriteLine(JsonConvert.SerializeObject(await conn.FileSystem.GetFileSystemInfo())); 
            
            foreach (var file in await conn.FileSystem.GetFiles(conn.FileSystemInfo.TempDirectory))
                Console.WriteLine(file);

            var tempFilePath = conn.Path.Combine(conn.FileSystemInfo.TempDirectory, "rapitest.txt");
            await conn.FileSystem.WriteFileContents(tempFilePath, Encoding.UTF8.GetBytes("Hello world!"));

            var shell = conn.SystemInfo.Platform.IsWindows
                ? @"cmd.exe"
                : "bash";

            var shellArgs = conn.SystemInfo.Platform.IsWindows ? new[]{"/D"} : null;

            var cat = conn.SystemInfo.Platform.IsWindows ? "type" : "cat";

            var encoding = Encoding.UTF8;
            var nl = conn.SystemInfo.Platform.IsWindows ? "\r\n" : "\n";
            
            async Task WaitForExit()
            {
                int? exitCode;
                while ((exitCode = (await conn.Processes.GetExitCode("test"))) == null)
                    await Task.Delay(100);
                foreach(var stderr in new []{false, true})
                {
                    var outBytes = await conn.Processes.GetOutput("test", stderr);

                    var s = outBytes == null ? null : encoding.GetString(outBytes);
                    Console.WriteLine((stderr ? "Stderr:\n" : "Stdout:\n") + s);
                }
                Console.WriteLine("Exit code " + exitCode);
            }

            foreach (var mode in new[] {0, 1, 2})
            {
                Console.WriteLine($"====================\nRunning mode {mode}\n====================");
                await conn.Processes.Start("test", new ProcessCreationOptions(shell, shellArgs)
                {
                    MergeStderr = mode == 2
                });
                await conn.Processes.WriteStdIn("test", encoding.GetBytes("echo test-test" + nl));
                await conn.Processes.WriteStdIn("test",
                    encoding.GetBytes(cat + " " + tempFilePath + (mode > 0 ? ">&2" : "") + nl));
                await Task.Delay(1000);
                await conn.Processes.CloseStdIn("test");
                await WaitForExit();
            }

            Console.WriteLine("====================\nTesting the process kill feature\n====================");
            await conn.Processes.Start("test", new ProcessCreationOptions(shell, shellArgs));
            await Task.Delay(1000);
            await conn.Processes.Kill("test");
            await WaitForExit();
        }
    }
}