using System.Threading.Tasks;

namespace Rapi
{
    public interface IRapiConnectionFactory
    {
        Task<RapiConnection> Connect(string url);
    }

    public class DefaultRapiConnectionFactory : IRapiConnectionFactory
    {
        public Task<RapiConnection> Connect(string url)
        {
            return RapiConnection.Connect(url);
        }
    }
}