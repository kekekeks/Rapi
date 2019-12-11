using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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

        public Task CleanDirectory(string path)
        {
            var directory = new DirectoryInfo(path);
            foreach (var file in directory.GetFiles()) 
                file.Delete();
            foreach (var dir in directory.GetDirectories()) 
                dir.Delete(true);
            return Task.CompletedTask;
        }

        public Task<RapiFileSystemInfo> GetFileSystemInfo()
        {
            var specialDirs = Enum.GetValues(typeof(Environment.SpecialFolder))
                .Cast<Environment.SpecialFolder>().Distinct()
                .ToDictionary(f => (int)f, Environment.GetFolderPath);
            return Task.FromResult(new RapiFileSystemInfo
            {
                TempDirectory = Path.GetTempPath(),
                Drives = Directory.GetLogicalDrives().ToList(),
                SpecialFolders = specialDirs
            });
        }

        public Task Unzip(string archivePath, string toDirectory)
        {
            ZipFile.ExtractToDirectory(archivePath, toDirectory);
            return Task.CompletedTask;
        }

        public Task DeleteFile(string path)
        {
            File.Delete(path);
            return Task.CompletedTask;
        }
    }
}