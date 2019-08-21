using System.Threading.Tasks;

namespace Rapi
{
    public interface IRapiSftpRpc
    {
        Task Download(string from, string to, RapiSftpCredentials credentials);
        Task Upload(string from, string to, RapiSftpCredentials credentials);
    }

    public class RapiSftpCredentials
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
    }
}