using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rapi
{
    public interface IRapiFileSystemRpc
    {
        Task<bool> FileExists(string file);
        Task<byte[]> ReadFileContents(string file);
        Task WriteFileContents(string file, byte[] data);
        Task<List<string>> GetFiles(string s);
        Task<RapiFileSystemInfo> GetFileSystemInfo();
    }

    public class RapiFileSystemInfo
    {
        public string TempDirectory { get; set; }
        public List<string> Drives { get; set; }
    }
}