using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Rapi;

namespace RapiAgent
{
    public class FileSystemRpc : IRapiFileSystemRpc
    {
        public Task<bool> FileExists(string file)
        {
            return Task.FromResult(File.Exists(file));
        }

        public Task<byte[]> ReadFileContents(string file)
        {
            return File.ReadAllBytesAsync(file);
        }

        public Task WriteFileContents(string file, byte[] data)
        {
            return File.WriteAllBytesAsync(file, data);
        }

        public Task<List<string>> GetFiles(string s)
        {
            var rv = new List<string>();
            foreach(var f in Directory.GetFiles(s))
                rv.Add(f);
            return Task.FromResult(rv);
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