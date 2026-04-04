using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rapi
{
    public interface IRapiGrpcClient
    {
        Task<RapiGrpcResponse> SendGrpcRequest(RapiGrpcRequest request);
    }

    public class RapiGrpcRequest
    {
        public string? Uri { get; set; }
        public string? Method { get; set; }
        public byte[]? Body { get; set; }
        public int Timeout { get; set; }
        public int VersionMajor { get; set; } = 2;
        public int VersionMinor { get; set; }
        public int VersionPolicy { get; set; }
        public List<RapiGrpcHeader>? Headers { get; set; }
    }

    public class RapiGrpcResponse
    {
        public int Code { get; set; }
        public byte[]? Data { get; set; }
        public int VersionMajor { get; set; } = 2;
        public int VersionMinor { get; set; }
        public List<RapiGrpcHeader>? Headers { get; set; }
        public List<RapiGrpcHeader>? Trailers { get; set; }
    }

    public class RapiGrpcHeader
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
    }
}
