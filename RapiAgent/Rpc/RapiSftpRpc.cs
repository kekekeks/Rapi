using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Rapi;
using Renci.SshNet;

namespace RapiAgent.Rpc
{
    internal class RapiSftpRpc : IRapiSftpRpc
    {
        public async Task Download(string from, string to, RapiSftpCredentials credentials)
        {
            from = GetSshNetFriendlyPath(from);
            using (var sftp = new SftpClient(credentials.Host, credentials.Port, credentials.Login, credentials.Password))
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
            using (var sftp = new SftpClient(credentials.Host, credentials.Port, credentials.Login, credentials.Password))
            {
                sftp.HostKeyReceived += (sender, args) => args.CanTrust = true;
                sftp.Connect();
                try
                {
                    to = GetSshNetFriendlyPath(to);
                    await UploadFile(sftp, from, to);
                }
                finally
                {
                    sftp.Disconnect();
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
    }
}