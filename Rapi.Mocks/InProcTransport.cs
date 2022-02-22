using System.Threading.Tasks;
using CoreRPC.Transport;

namespace Rapi.Mocks
{
    class InProcTransport : IClientTransport
    {
        private readonly IRequestHandler _handler;

        public InProcTransport(IRequestHandler handler)
        {
            _handler = handler;
        }

        class Request : IRequest
        {
            private TaskCompletionSource<byte[]> _tcs = new TaskCompletionSource<byte[]>();

            public Request(byte[] data)
            {
                Data = data;
            }

            public Task RespondAsync(byte[] data)
            {
                _tcs.TrySetResult(data);
                return Task.CompletedTask;
                    
            }

            public byte[] Data { get; }
            public object Context { get; }
            public Task<byte[]> Finished => _tcs.Task;
        }
            
        public Task<byte[]> SendMessageAsync(byte[] message)
        {
            return Task.Run(async () =>
            {
                var req = new Request(message);
                await _handler.HandleRequest(req);
                return await req.Finished;
            });
        }
    }
}