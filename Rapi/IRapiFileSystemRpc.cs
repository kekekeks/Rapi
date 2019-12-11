using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rapi
{
    public interface IRapiFileSystemRpc
    {
        Task<bool> FileExists(string file);
        Task<bool> DirectoryExists(string file);
        Task<byte[]> ReadFileContents(string file);
        Task WriteFileContents(string file, byte[] data);
        Task<List<string>> GetFiles(string path);
        Task<List<string>> GetDirectories(string path);
        Task CreateDirectory(string path);
        Task CleanDirectory(string path);
        Task<RapiFileSystemInfo> GetFileSystemInfo();
        Task Unzip(string archivePath, string toDirectory);
        Task DeleteFile(string path);
    }
    
    public class RapiFileSystemInfo
    {
        public string TempDirectory { get; set; }
        public Dictionary<int, string> SpecialFolders { get; set; }
        public List<string> Drives { get; set; }
    }
}