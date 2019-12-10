using System.Threading.Tasks;

namespace Rapi
{
    public interface IRapiSftpRpc
    {
        Task Download(string from, string to, RapiSftpCredentials credentials);
        Task Upload(string from, string to, RapiSftpCredentials credentials);
        
        Task StartDownload(string id, string from, string to, RapiSftpCredentials credentials);
        Task StartUpload(string id, string from, string to, RapiSftpCredentials credentials);
        Task<RapiSftpOperationStatusDto> TryGetStatus(string id);
        Task Complete(string id);
    }

    public class RapiSftpOperationStatusDto
    {
        public bool IsCompleted { get; set; }
        public string Exception { get; set; }
    }

    public class RapiSftpCredentials
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; } = 22;
    }
}