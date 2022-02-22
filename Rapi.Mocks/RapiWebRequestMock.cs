using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rapi.Mocks
{
    public class RapiWebRequestMock : IRapiWebRequestRpc
    {
        private Dictionary<RapiWebRequest, TaskCompletionSource<RapiWebResponse>> _requests =
            new Dictionary<RapiWebRequest, TaskCompletionSource<RapiWebResponse>>();

        Task<RapiWebResponse> IRapiWebRequestRpc.SendWebRequest(RapiWebRequest req)
        {
            lock (_requests)
            {
                var tcs = new TaskCompletionSource<RapiWebResponse>();
                _requests.Add(req, tcs);
                return tcs.Task;
            }
        }

        public List<RapiWebRequest> GetRequests()
        {
            lock (_requests)
                return _requests.Keys.ToList();
        }

        public void Complete(RapiWebRequest req, RapiWebResponse resp)
        {
            lock (_requests)
            {
                _requests[req].SetResult(resp);
                _requests.Remove(req);
            }
        }

        public void Error(RapiWebRequest req, Exception e)
        {
            lock (_requests)
            {
                _requests[req].SetException(e);
                _requests.Remove(req);
            }
        }
    }
}