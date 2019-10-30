using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rapi;
using RapiAgent.Processes;
using Renci.SshNet;

namespace RapiAgent.Rpc
{
    internal class RapiSftpRpc : IRapiSftpRpc
    {
        private static readonly RapiPath UnixPath = new RapiPath(new RapiPlatformInfo
        {
            IsLinux = true,
            IsUnix = true
        });

        class Operation
        {
            public bool IsUpload;
            public string From;
            public string To;
            public Task Task;
            public string Login;
        }

        private static readonly Dictionary<string, Operation> Operations = new Dictionary<string, Operation>(); 

        public async Task Download(string from, string to, RapiSftpCredentials credentials)
        {
            from = GetSshNetFriendlyPath(from);
            using (var sftp = new SftpClient(
                credentials.Host, 
                credentials.Port,
                credentials.Login,
                credentials.Password))
            {
                sftp.HostKeyReceived += (sender, args) => args.CanTrust = true;
                sftp.Connect();
                try
                {
                    var remoteEntry = sftp.Get(from);
                    if (remoteEntry.IsDirectory)
                        await DownloadDirectory(sftp, from, to);
                    else if (remoteEntry.IsRegularFile)
                        await DownloadFile(sftp, from, to);
                }
                finally
                {
                    sftp.Disconnect();
                }
            }
        }

        private static async Task DownloadDirectory(SftpClient sftp, string from, string to)
        {
            var remoteFiles = sftp.ListDirectory(from);
            foreach (var remoteFile in remoteFiles)
            {
                if (remoteFile.IsSymbolicLink ||
                    remoteFile.Name == "."    ||
                    remoteFile.Name == "..")
                    continue;

                if (remoteFile.IsDirectory)
                {
                    var localDirectoryPath = Path.Combine(to, remoteFile.Name);
                    var localDirectory = Directory.CreateDirectory(localDirectoryPath);
                    await DownloadDirectory(sftp, remoteFile.FullName, localDirectory.FullName);
                }
                else if (remoteFile.IsRegularFile)
                {
                    var localFilePath = Path.Combine(to, remoteFile.Name);
                    await DownloadFile(sftp, remoteFile.FullName, localFilePath);
                }
            }
        }

        private static Task DownloadFile(SftpClient sftp, string from, string to) => Task.Run(() =>
        {
            using (var stream = File.Create(to))
            {
                stream.Seek(0, SeekOrigin.Begin);
                sftp.DownloadFile(from, stream);
                stream.Flush();
            }
        });

        public async Task Upload(string from, string to, RapiSftpCredentials credentials)
        {
            to = GetSshNetFriendlyPath(to);
            using (var sftp = new SftpClient(
                credentials.Host, 
                credentials.Port, 
                credentials.Login, 
                credentials.Password))
            {
                sftp.HostKeyReceived += (sender, args) => args.CanTrust = true;
                sftp.Connect();
                try
                {
                    if (Directory.Exists(from))
                        await UploadDirectory(sftp, from, to);
                    else if (File.Exists(from))
                        await UploadFile(sftp, from, to);
                }
                finally
                {
                    sftp.Disconnect();
                }
            }
        }

        private static async Task UploadDirectory(SftpClient sftp, string from, string to)
        {
            foreach (var localPath in Directory.GetFileSystemEntries(from))
            {
                if (File.Exists(localPath))
                {
                    var localFileName = Path.GetFileName(localPath);
                    var remoteFilePath = UnixPath.Combine(to, localFileName);
                    await UploadFile(sftp, localPath, remoteFilePath);
                }
                else if (Directory.Exists(localPath))
                {
                    var localDirectoryName = Path.GetFileName(localPath);
                    var remoteDirectoryPath = UnixPath.Combine(to, localDirectoryName);
                    sftp.CreateDirectory(remoteDirectoryPath);
                    await UploadDirectory(sftp, localPath, remoteDirectoryPath);
                }
            }
        }

        private static Task UploadFile(SftpClient sftp, string from, string to) => Task.Run(() =>
        {
            using (var stream = File.OpenRead(from))
            {
                stream.Seek(0, SeekOrigin.Begin);
                sftp.UploadFile(stream, to);
            }
        });

        private static string GetSshNetFriendlyPath(string path)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? $"/{path.Replace(@"\", "/")}"
                : path;
        }

        void MatchOrStart(string id, bool upload, string from, string to, string login, Func<Task> cb)
        {
            lock (Operations)
            {
                if (Operations.TryGetValue(id, out var op))
                {
                    if (op.IsUpload == upload && op.From == from && op.To == to && op.Login == login)
                        return;
                    throw new InvalidOperationException("Operation with the same id but different parameters exists");
                }

                Operations[id] = new Operation
                {
                    Task = Task.Run(() => cb()),
                    From = from,
                    To = to,
                    IsUpload = upload,
                    Login = login
                };
            }
        }
        
        public async Task StartDownload(string id, string @from, string to, RapiSftpCredentials credentials)
        {
            MatchOrStart(id, false, from, to, credentials.Login, () => Download(from, to, credentials));
        }

        public async Task StartUpload(string id, string @from, string to, RapiSftpCredentials credentials)
        {
            MatchOrStart(id, true, from, to, credentials.Login, () => Upload(from, to, credentials));
        }

        public async Task<RapiSftpOperationStatusDto> TryGetStatus(string id)
        {
            lock (Operations)
            {
                if (Operations.TryGetValue(id, out var op))
                    return new RapiSftpOperationStatusDto
                    {
                        IsCompleted = op.Task.IsCompleted,
                        Exception = op.Task.Exception?.ToString()
                    };
                return null;
            }
        }

        public async Task Complete(string id)
        {
            lock (Operations)
            {
                if (Operations.TryGetValue(id, out var op))
                {
                    if (!op.Task.IsCompleted)
                        throw new InvalidOperationException("Operation is not completed");
                    Operations.Remove(id);
                }
            }
        }
    }
}