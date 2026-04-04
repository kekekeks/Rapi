using System.Threading.Tasks;
using Grpc.Core;
using NUnit.Framework;
using Rapi.Tests.Grpc;

namespace Rapi.Tests
{
    [TestFixture]
    public class RapiGrpcTests
    {
        private GrpcEchoHost _grpcHost = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _grpcHost = await GrpcEchoHost.Start();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (_grpcHost != null)
                await _grpcHost.DisposeAsync();
        }

        [Test]
        public async Task ShouldCallUnaryGrpcMethod()
        {
            var connection = await RapiConnection.Connect(RapiTestHost.Address);
            using var channel = connection.CreateGrpcChannel(_grpcHost.Address);
            var client = new TestGrpc.TestGrpcClient(channel);

            var response = await client.UnaryEchoAsync(new EchoRequest
            {
                Text = "Hello, gRPC!"
            }).ResponseAsync;

            Assert.That(response.Text, Is.EqualTo("Hello, gRPC!"));
        }

        [Test]
        public async Task ShouldForwardGrpcMetadata()
        {
            var connection = await RapiConnection.Connect(RapiTestHost.Address);
            using var channel = connection.CreateGrpcChannel(_grpcHost.Address);
            var client = new TestGrpc.TestGrpcClient(channel);

            var response = await client.UnaryEchoAsync(new EchoRequest
            {
                Text = "metadata"
            }, new Metadata
            {
                { "x-rapi-test", "forwarded" }
            }).ResponseAsync;

            Assert.That(response.Metadata, Is.EqualTo("forwarded"));
        }

        [Test]
        public async Task ShouldPropagateGrpcErrors()
        {
            var connection = await RapiConnection.Connect(RapiTestHost.Address);
            using var channel = connection.CreateGrpcChannel(_grpcHost.Address);
            var client = new TestGrpc.TestGrpcClient(channel);

            using var call = client.AlwaysFailAsync(new FailureRequest
            {
                StatusCode = (int)StatusCode.PermissionDenied,
                Detail = "Denied by test"
            });
            var exception = Assert.ThrowsAsync<RpcException>(async () => await call.ResponseAsync);

            Assert.That(exception!.StatusCode, Is.EqualTo(StatusCode.PermissionDenied));
            Assert.That(exception.Status.Detail, Is.EqualTo("Denied by test"));
        }
    }
}
