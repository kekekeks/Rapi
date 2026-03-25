using System;
using System.Collections.Generic;
using System.Linq;
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
        Task<byte[]?> GetOutput(string id, bool stderr);
        Task<ProcessCreationOptions?> TryGetCreationOptions(string id);
    }
    
    public class ProcessCreationOptions
    {
        public string? Path { get; set; }
        public Dictionary<string, string>? Environment { get; set; }
        public string? WorkingDirectory { get; set; }
        public string[]? Arguments { get; set; }
        public bool MergeStderr { get; set; }
        public bool CloseStdIn { get; set; }
        public string? DataToken { get; set; }

        public ProcessCreationOptions() { }

        public ProcessCreationOptions(string path, string[]? arguments)
        {
            Path = path;
            Arguments = arguments;
        }

        static bool CompareDic<TKey, TValue>(Dictionary<TKey, TValue>? left, Dictionary<TKey, TValue>? right) where TKey : notnull where TValue : IEquatable<TValue>
        {
            if (left == null || right == null)
                return right == left;
            if (left.Count != right.Count)
                return false;
            foreach(var k in left)
                if (!k.Value.Equals(right[k.Key]))
                    return false;
            return true;
        }
        
        public bool AreEqual(ProcessCreationOptions other) =>
            Path == other.Path
            && WorkingDirectory == other.WorkingDirectory
            && (Arguments == null ? other.Arguments == null : other.Arguments != null && Arguments.SequenceEqual(other.Arguments))
            && MergeStderr == other.MergeStderr
            && CloseStdIn == other.CloseStdIn
            && DataToken == other.DataToken
            && CompareDic(Environment, other.Environment);
    }
}