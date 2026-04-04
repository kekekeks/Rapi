using System;
using Grpc.Net.Client;

namespace Rapi
{
    public static class RapiGrpcChannelExtensions
    {
        public static GrpcChannel CreateGrpcChannel(this RapiConnection connection, string address,
            GrpcChannelOptions? options = null)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(address));

            options ??= new GrpcChannelOptions();
            if (options.HttpClient != null || options.HttpHandler != null)
                throw new InvalidOperationException(
                    "Rapi gRPC channels manage their own HTTP transport. Leave HttpClient and HttpHandler unset.");

            options.HttpHandler = new RapiGrpcMessageHandler(connection.GrpcClient);
            return GrpcChannel.ForAddress(address, options);
        }
    }
}
