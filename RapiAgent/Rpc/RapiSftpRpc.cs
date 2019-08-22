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
            using (var sftp = new SftpClient(credentials.Host, credentials.Port, credentials.Login, credentials.Password))
            {
                sftp.HostKeyReceived += (sender, args) => args.CanTrust = true;
                sftp.Connect();
                try
                {
                    using (var stream = File.Create(to))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        var path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? $"/{from.Replace(@"\", "/")}"
                            : from;

                        sftp.DownloadFile(path, stream);
                        await stream.FlushAsync();
                    }
                }
                finally
                {
                    sftp.Disconnect();
                }
            }
        }

        public Task Upload(string from, string to, RapiSftpCredentials credentials)
        {
            using (var sftp = new SftpClient(credentials.Host, credentials.Port, credentials.Login, credentials.Password))
            {
                sftp.HostKeyReceived += (sender, args) => args.CanTrust = true;
                sftp.Connect();
                try
                {
                    using (var stream = File.OpenRead(from))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        var path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? $"/{to.Replace(@"\", "/")}"
                            : to;

                        sftp.UploadFile(stream, path);
                    }
                }
                finally
                {
                    sftp.Disconnect();
                }
            }
            return Task.CompletedTask;
        }
    }
}