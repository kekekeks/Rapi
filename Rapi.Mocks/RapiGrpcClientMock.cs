using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rapi.Mocks
{
    public class RapiGrpcClientMock : IRapiGrpcClient
    {
        private Dictionary<RapiGrpcRequest, TaskCompletionSource<RapiGrpcResponse>> _requests =
            new Dictionary<RapiGrpcRequest, TaskCompletionSource<RapiGrpcResponse>>();

        Task<RapiGrpcResponse> IRapiGrpcClient.SendGrpcRequest(RapiGrpcRequest request)
        {
            lock (_requests)
            {
                var tcs = new TaskCompletionSource<RapiGrpcResponse>();
                _requests.Add(request, tcs);
                return tcs.Task;
            }
        }

        public List<RapiGrpcRequest> GetRequests()
        {
            lock (_requests)
                return _requests.Keys.ToList();
        }

        public void Complete(RapiGrpcRequest request, RapiGrpcResponse response)
        {
            lock (_requests)
            {
                _requests[request].SetResult(response);
                _requests.Remove(request);
            }
        }

        public void Error(RapiGrpcRequest request, Exception exception)
        {
            lock (_requests)
            {
                _requests[request].SetException(exception);
                _requests.Remove(request);
            }
        }
    }
}
