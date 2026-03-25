using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Rapi.Mocks
{
    class MockSystemInfo : IRapiSystemInfoRpc
    {
        private readonly JObject _info;

        public MockSystemInfo(RapiSystemInfo info)
        {
            _info = JObject.FromObject(info);
        }
        public Task<RapiSystemInfo> GetSystemInfo()
        {
            return Task.FromResult(_info.ToObject<RapiSystemInfo>()!);
        }
    }
}