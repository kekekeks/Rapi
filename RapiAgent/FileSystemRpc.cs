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

        public Task<bool> DirectoryExists(string file)
        {
            return Task.FromResult(Directory.Exists(file));
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

        public Task<List<string>> GetDirectories(string path)
        {
            var rv = new List<string>();
            foreach(var f in Directory.GetDirectories(path))
                rv.Add(f);
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