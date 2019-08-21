using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rapi
{
    public interface IRapiProcesses
    {
        Task Start(string id, ProcessCreationOptions options);
        Task<int?> GetExitCode(string id);
        Task Kill(string id);
        Task WriteStdIn(string id, byte[] data);
        Task CloseStdIn(string id);
        Task<byte[]> GetOutput(string id, bool stderr);
    }
    
    public class ProcessCreationOptions
    {
        public string Path { get; set; }
        public Dictionary<string, string> Environment { get; set; }
        public string WorkingDirectory { get; set; }
        public string[] Arguments { get; set; }
        public bool MergeStderr { get; set; }

        public ProcessCreationOptions() { }

        public ProcessCreationOptions(string path, string[] arguments)
        {
            Path = path;
            Arguments = arguments;
        }
    }
}