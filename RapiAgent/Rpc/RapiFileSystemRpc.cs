using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Rapi;

namespace RapiAgent.Rpc
{
    internal class RapiFileSystemRpc : IRapiFileSystemRpc
    {
        public Task<bool> FileExists(string file) => Task.FromResult(File.Exists(file));

        public Task<bool> DirectoryExists(string file) => Task.FromResult(Directory.Exists(file));

        public Task<byte[]> ReadFileContents(string file) => File.ReadAllBytesAsync(file);

        public Task WriteFileContents(string file, byte[] data) => File.WriteAllBytesAsync(file, data);

        public Task<List<string>> GetFiles(string s)
        {
            var rv = Directory.GetFiles(s).ToList();
            return Task.FromResult(rv);
        }

        public Task<List<string>> GetDirectories(string path)
        {
            var rv = Directory.GetDirectories(path).ToList();
            return Task.FromResult(rv);
        }

        public Task CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
            return Task.CompletedTask;
        }

        public Task<RapiFileSystemInfo> GetFileSystemInfo()
        {
            return Task.FromResult(new RapiFileSystemInfo
            {
                TempDirectory = Path.GetTempPath(),
                Drives = Directory.GetLogicalDrives().ToList()
            });
        }
    }
}