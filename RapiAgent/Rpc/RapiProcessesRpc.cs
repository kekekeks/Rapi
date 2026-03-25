using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Rapi;
using RapiAgent.Processes;

namespace RapiAgent.Rpc
{
    internal class RapiProcessesRpc : IRapiProcesses
    {
        
        private readonly Dictionary<string, ProcessHelper> _processes = new Dictionary<string, ProcessHelper>();
        private readonly IProcessFactory _factory;

        public RapiProcessesRpc(IProcessFactory factory) => _factory = factory;

        public Task Start(string id, ProcessCreationOptions options)
        {
            lock (_processes)
            {
                if(_processes.TryGetValue(id, out var proc))
                    proc.Process.Kill();
                _processes[id] = new ProcessHelper(_factory.Create(options), options);

            }
            return Task.CompletedTask;
        }

        public Task<int?> GetExitCode(string id)
        {
            lock (_processes)
                if (_processes.TryGetValue(id, out var proc))
                    return Task.FromResult(
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
            async Task DoWrite(Stream s)
            {
                await s.WriteAsync(data);
                await s.FlushAsync();
            }
            lock (_processes)
                if (_processes.TryGetValue(id, out var proc))
                    return DoWrite(proc.Process.StdIn);
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

        public Task<byte[]?> GetOutput(string id, bool stderr)
        {
            lock (_processes)
                if (_processes.TryGetValue(id, out var proc))
                    return proc.GetOutput(stderr);
            throw new KeyNotFoundException();
        }

        public Task<ProcessCreationOptions?> TryGetCreationOptions(string id)
        {
            lock (_processes)
            {
                if (_processes.TryGetValue(id, out var proc))
                    return Task.FromResult<ProcessCreationOptions?>(proc.Options);
                return Task.FromResult<ProcessCreationOptions?>(null);
            }
        }
    }
}