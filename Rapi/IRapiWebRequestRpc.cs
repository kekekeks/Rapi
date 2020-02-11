using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rapi
{
    public interface IRapiWebRequestRpc
    {
        Task<RapiWebResponse> SendWebRequest(RapiWebRequest req);
    }

    public class RapiWebRequest
    {
        public string Uri { get; set; }
        public string Method { get; set; }
        public byte[] Body { get; set; }
        public int Timeout { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }

    public class RapiWebResponse
    {
        public int Code { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public byte[] Data { get; set; }
    }
}