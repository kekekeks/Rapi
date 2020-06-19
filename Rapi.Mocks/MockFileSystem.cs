using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rapi.Mocks
{
    public class MockFileSystem : IRapiFileSystemRpc, IRapiFileStream
    {
        private readonly bool _unix;
        private readonly RapiPath _path;

        private MockFileSystemDirectory _root = new MockFileSystemDirectory("<root>");
        private char[] _separatorChars;

        public MockFileSystem(bool unix)
        {
            _unix = unix;
            _path = new RapiPath(new RapiPlatformInfo
            {
                IsLinux = unix,
                IsUnix = unix,
                IsWindows = !unix,
                IsOSX = false
            });
            if (_unix)
            {
                _root.Items["tmp"] = new MockFileSystemDirectory("tmp");
            }
            else
            {
                var c =  new MockFileSystemDirectory("C:");
                _root.Items["c:"] = c;
                c.Items["temp"] = new MockFileSystemDirectory("temp");
            }

            _separatorChars = unix ? "/".ToCharArray() : "\\/".ToCharArray();
        }

        string TransformKey(string name) => _unix ? name : name?.ToLowerInvariant();

        IMockFileSystemItem FindItem(string path)
        {
            if (!_path.IsPathRooted(path))
                return null;

            var parts = new Queue<string>(path.Split(_separatorChars, StringSplitOptions.RemoveEmptyEntries));
            if (parts.Count == 0)
                return null;
            IMockFileSystemItem current = _root;
            while (parts.Count > 0)
            {
                if (!(current is MockFileSystemDirectory dir))
                    return null;
                var name = TransformKey(parts.Dequeue());
                if (!dir.Items.TryGetValue(name, out var next))
                    return null;
                current = next;
            }

            return current;
        }

        MockFileSystemDirectory GetDir(string path) =>
            FindItem(path) is MockFileSystemDirectory d ? d : throw new DirectoryNotFoundException();
        
        MockFileSystemDirectory FindParentDir(string path)
        {
            path = _path.Combine(path);
            if (FindItem(path) is MockFileSystemDirectory d)
                return d;
            return null;
        }

        MockFileSystemDirectory GetParentDir(string path)
        {
            var r = FindParentDir(path);
            if(r==null)
                throw new DirectoryNotFoundException();
            return r;
        }
        
        public async Task<bool> FileExists(string file) => FindItem(file)?.IsDirectory == false;

        public async Task<bool> DirectoryExists(string file) => FindItem(file)?.IsDirectory == true;

        static byte[] Clone(byte[] data)
        {
            var r = new byte[data.Length];
            Buffer.BlockCopy(data, 0, r, 0, data.Length);
            return r;
        }
        
        public async Task<byte[]> ReadFileContents(string file)
        {
            if (!(FindItem(file) is MockFileSystemFile f))
                throw new FileNotFoundException();
            return Clone(f.Data);
        }



        public async Task WriteFileContents(string file, byte[] data) => WriteFileContentsUncloned(file, Clone(data));
        public void WriteFileContentsUncloned(string file, byte[] data)
        {
            if (FindItem(file) is MockFileSystemFile f)
                f.Data = data;
            else
            {
                var parent = GetParentDir(file);
                var item = new MockFileSystemFile(_path.GetFileName(file), data);
                parent.Items[TransformKey(item.Name)] = item;
            }
        }

        public async Task<List<string>> GetFiles(string path)
        {
            return GetDir(path).Items.OfType<MockFileSystemFile>()
                .Select(x => _path.Combine(path, x.Name)).ToList();
        }

        public async Task<List<string>> GetDirectories(string path)
        {
            return GetDir(path).Items.OfType<MockFileSystemDirectory>()
                .Select(x => _path.Combine(path, x.Name)).ToList();
        }

        public async Task CreateDirectory(string path)
        {
            var parts = new Queue<string>(path.Split(_separatorChars, StringSplitOptions.RemoveEmptyEntries));
            if (parts.Count == 0)
                throw new ArgumentException();
            var current = _root;
            while (parts.Count > 0)
            {
                var name = parts.Dequeue();
                var transformed = TransformKey(name);
                if (!current.Items.TryGetValue(transformed, out var item))
                {
                    var newDir = new MockFileSystemDirectory(name);
                    current.Items[transformed] = newDir;
                    current = newDir;
                }
                else if (item.IsDirectory)
                    current = (MockFileSystemDirectory) item;
                else
                    throw new IOException();
            }
        }

        public async Task CleanDirectory(string path)
        {
            GetDir(path).Items.Clear();
        }

        public async Task<RapiFileSystemInfo> GetFileSystemInfo()
        {
            if (_unix)
                return new RapiFileSystemInfo
                {
                    Drives = new List<string> {"/"},
                    TempDirectory = "/tmp"
                };
            return new RapiFileSystemInfo()
            {
                Drives = new List<string>
                {
                    "c:\\"
                },
                TempDirectory = "c:\\temp"
            };
        }

        public async Task Unzip(string archivePath, string toDirectory)
        {
            throw new System.NotImplementedException();
        }

        public async Task DeleteFile(string path)
        {
            throw new System.NotImplementedException();
        }

        public async Task CopyFile(string @from, string to)
        {
            WriteFileContents(to, ReadFileContents(from).Result);
        }

        public void Mount(string path, MockFileSystem other, string otherPath) =>
            GetParentDir(path).Items[TransformKey(other._root.Name)] = other.GetDir(otherPath);

        public async Task WriteFileContents(string file, Stream data)
        {
            var ms = new MemoryStream();
            data.CopyTo(ms);
            WriteFileContentsUncloned(file, ms.ToArray());
            
        }

        public async Task<Stream> ReadFileContentsStream(string file)
        {
            if (FindItem(file) is MockFileSystemFile f)
                return new MemoryStream(f.Data, false);
            throw new FileNotFoundException();
        }
    }

    class MockFileSystemDirectory : IMockFileSystemItem
    {
        public MockFileSystemDirectory(string name)
        {
            Name = name;
        }

        public ConcurrentDictionary<string, IMockFileSystemItem> Items { get; }
            = new ConcurrentDictionary<string, IMockFileSystemItem>();
        
        public bool IsDirectory => true;
        
        public string Name { get; }
    }

    class MockFileSystemFile : IMockFileSystemItem
    {
        public MockFileSystemFile(string name, byte[] data)
        {
            Name = name;
            Data = data;
        }

        public byte[] Data { get; set; }
        public bool IsDirectory { get; }
        public string Name { get; }
    }
    
    interface IMockFileSystemItem
    {
        bool IsDirectory { get; }
        string Name { get; }
    }
    
}