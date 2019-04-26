using System;
using System.Text;
using System.Threading.Tasks;
using CoreRPC.Binding.Default;
using CoreRPC.Routing;
using CoreRPC.Serialization;
using CoreRPC.Transport.Http;
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
            var fs = engine.CreateProxy<IRapiFileSystemRpc>(transport, new ConstExtractor("FileSystem"));
            var proc = engine.CreateProxy<IRapiProcesses>(transport, new ConstExtractor("Processes"));
            
            foreach(var f in await fs.GetFiles("/tmp"))
                Console.WriteLine(f);
            await fs.WriteFileContents("/tmp/x", Encoding.UTF8.GetBytes("Hello world!"));

            await proc.Start("test", new ProcessCreationOptions("bash", null));
            await proc.WriteStdIn("test", Encoding.UTF8.GetBytes("cat /tmp/x\n"));
            await proc.CloseStdIn("test");
            while ((await proc.GetExitCode("test")) == null)
                await Task.Delay(100);
            var s = Encoding.UTF8.GetString(await proc.GetOutput("test", false));
            Console.WriteLine(s);
        }
    }
}