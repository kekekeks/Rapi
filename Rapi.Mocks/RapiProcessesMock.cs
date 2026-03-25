using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rapi.Mocks
{
    public class RapiProcessesMock : IRapiProcesses
    {
        private Dictionary<string, RapiProcessMock> _processes = new Dictionary<string, RapiProcessMock>();
        
        public async Task Start(string id, ProcessCreationOptions options)
        {
            lock (_processes)
            {
                if (_processes.TryGetValue(id, out var found))
                {
                    if(found.Options.DataToken != null && found.Options.DataToken == options.DataToken)
                        return;
                    Kill(id).Wait();
                }
                _processes[id] = new RapiProcessMock(options);

            }
        }

        RapiProcessMock GetProcess(string id)
        {
            lock (_processes)
                return _processes[id];
        }

        public async Task<int?> GetExitCode(string id)
        {
            return GetProcess(id).ExitCode;
        }

        public async Task Kill(string id)
        {
            lock(_processes[id])
                if (_processes.TryGetValue(id, out var p))
                    p.Exit(-1);
        }

        static byte[] Clone(byte[] data)
        {
            var r = new byte[data.Length];
            Buffer.BlockCopy(data, 0, r, 0, data.Length);
            return r;
        }
        
        public async Task WriteStdIn(string id, byte[] data)
        {
            GetProcess(id).Stdin.Enqueue(Clone(data));
        }

        public async Task CloseStdIn(string id)
        {
            GetProcess(id).StdInClosed = true;
        }

        public async Task<byte[]?> GetOutput(string id, bool stderr)
        {
            var p = GetProcess(id);
            var s = stderr ? p.Stderr : p.Stdout;
            lock (s)
                return s.ToArray();
        }

        public async Task<ProcessCreationOptions?> TryGetCreationOptions(string id)
        {
            lock(_processes)
                if (_processes.TryGetValue(id, out var p))
                    return p.Options;
            return null;
        }

        public Dictionary<string, RapiProcessMock> GetProcesses()
        {
            lock (_processes)
            {
                return _processes.ToDictionary(x => x.Key, x => x.Value);
            }
        }
        
        
        
        public class RapiProcessMock
        {
            public ProcessCreationOptions Options { get; }
            public int? ExitCode { get; private set; }
            public bool StdInClosed { get; internal set; }
            internal MemoryStream Stdout = new MemoryStream();
            internal MemoryStream Stderr = new MemoryStream();

            public RapiProcessMock(ProcessCreationOptions options)
            {
                Options = options;
                StdInClosed = options.CloseStdIn;
                if (options.MergeStderr)
                    Stderr = Stdout;

            }

            public void Exit(int i) => ExitCode = i;
            public ConcurrentQueue<byte[]> Stdin { get; } = new ConcurrentQueue<byte[]>();

            public void Write(bool stderr, byte[] data)
            {
                var s = stderr ? Stderr : Stdout;
                lock (s)
                    s.Write(data, 0, data.Length);
            }
        }
    }


}