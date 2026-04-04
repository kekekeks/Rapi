using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Rapi.Tests.Grpc;

namespace Rapi.Tests
{
    internal sealed class TestGrpcService : TestGrpc.TestGrpcBase
    {
        public override Task<EchoResponse> UnaryEcho(EchoRequest request, ServerCallContext context)
        {
            var metadata = context.RequestHeaders.FirstOrDefault(header => header.Key == "x-rapi-test")?.Value ?? "";
            return Task.FromResult(new EchoResponse
            {
                Text = request.Text,
                Metadata = metadata
            });
        }

        public override Task<EchoResponse> AlwaysFail(FailureRequest request, ServerCallContext context)
        {
            throw new RpcException(new Status((StatusCode)request.StatusCode, request.Detail));
        }
    }
}
