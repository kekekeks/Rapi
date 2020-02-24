using System.IO;
using System.Threading.Tasks;
using Rapi;

namespace RapiAgent.Processes
{
    internal class ProcessHelper
    {
        public IProcess Process { get; }
        public ProcessCreationOptions Options { get; }
        public MemoryStream Stdout { get; }
        public MemoryStream Stderr { get; }
        public Task StdoutReader { get; }
        public Task StderrReader { get; }

        public ProcessHelper(IProcess process, ProcessCreationOptions options)
        {
            Process = process;
            Options = options;
            Stdout = new MemoryStream();
            StdoutReader = Reader(process.StdoutOrMix, Stdout);
            if (process.Stderr == null) return;
                
            Stderr = new MemoryStream();
            StderrReader = Reader(process.Stderr, Stderr);

            if (options.CloseStdIn)
                process.StdIn.Dispose();
        }

        public async Task<byte[]> GetOutput(bool stderr)
        {
            var reader = stderr ? StderrReader : StdoutReader;
            var ms = stderr ? Stderr : Stdout;
            if (Process.ExitCode.IsCompleted)
            {
                if (reader == null)
                    return null;
                await reader;
            }
            lock (ms) return ms.ToArray();
        }

        private static async Task Reader(Stream stream, MemoryStream ms)
        {
            var buffer = new byte[1024];
            while (true)
            {
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    stream.Dispose();
                    return;
                }

                lock (ms)
                    ms.Write(buffer, 0, read);
            }
        }
    }
}