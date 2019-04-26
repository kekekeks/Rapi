using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Rapi;

namespace RapiAgent
{
    class ProcessRpc : IRapiProcesses
    {
        class ProcessHelper
        {
            public IProcess Process { get; }
            public MemoryStream Stdout { get; set; }
            public MemoryStream Stderr { get; set; }
            public Task StdoutReader { get; }
            public Task StderrReader { get; }

            async Task Reader(Stream stream, MemoryStream ms)
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

            public ProcessHelper(IProcess process)
            {
                Process = process;
                Stdout = new MemoryStream();
                StdoutReader = Reader(process.StdoutOrMix, Stdout);
                if (process.Stderr != null)
                {
                    Stderr = new MemoryStream();
                    StderrReader = Reader(process.Stderr, Stderr);
                }
            }

            public async Task<byte[]> GetOutput(bool stderr)
            {
                var reader = stderr ? StderrReader : StdoutReader;
                var ms = stderr ? Stderr : Stdout;
                if (Process.ExitCode.IsCompleted)
                    await reader;
                lock (ms)
                    return ms.ToArray();
            }
        }
        
        private readonly Dictionary<string, ProcessHelper> _processes = new Dictionary<string, ProcessHelper>();
        private readonly IProcessFactory _factory;

        public ProcessRpc(IProcessFactory factory)
        {
            _factory = factory;
        }

        public Task Start(string id, ProcessCreationOptions options)
        {
            lock (_processes)
            {
                if(_processes.TryGetValue(id, out var proc))
                    proc.Process.Kill();
                _processes[id] = new ProcessHelper(_factory.Create(options));
            }
            return Task.CompletedTask;
        }



        public Task<int?> GetExitCode(string id)
        {
            lock (_processes)
                if (_processes.TryGetValue(id, out var proc))
                    return Task.FromResult<int?>(
                        proc.Process.ExitCode.IsCompleted ? proc.Process.ExitCode.Result : (int?) null);
            throw new KeyNotFoundException();
        }

        public Task Kill(string id)
        {
            lock (_processes)
                if (_processes.TryGetValue(id, out var proc))
                    proc.Process.Kill();
            return Task.CompletedTask;
        }

        public Task WriteStdIn(string id, byte[] data)
        {
            lock (_processes)
                if (_processes.TryGetValue(id, out var proc))
                    return proc.Process.StdIn.WriteAsync(data, 0, data.Length);
            throw new KeyNotFoundException();
        }

        public Task CloseStdIn(string id)
        {
            lock (_processes)
                if (_processes.TryGetValue(id, out var proc))
                {
                    proc.Process.StdIn.Dispose();
                    return Task.CompletedTask;
                }
            throw new KeyNotFoundException();
        }

        public Task<byte[]> GetOutput(string id, bool stderr)
        {
            lock (_processes)
                if (_processes.TryGetValue(id, out var proc))
                    return proc.GetOutput(stderr);
            throw new KeyNotFoundException();
        }
    }
}