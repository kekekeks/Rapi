using System.IO;
using System.Threading.Tasks;
using CoreRPC.Transport;

namespace Rapi.Mocks
{
    class Request : IRequest
    {
        private TaskCompletionSource<Stream> _tcs = new();

        public Request(Stream data)
        {
            Data = data;
        }

        public Task RespondAsync(Stream data)
        {
            _tcs.TrySetResult(data);
            return Task.CompletedTask;
        }

        public Stream Data { get; }
        public object Context { get; }
        public Task<Stream> Finished => _tcs.Task;
    }
    
    
    class InProcTransport : IClientTransport
    {
        private readonly IRequestHandler _handler;

        public InProcTransport(IRequestHandler handler)
        {
            _handler = handler;
        }

        public async Task<Stream> SendMessageAsync(Stream message)
        {
            private TaskCompletionSource<Stream> _tcs = new TaskCompletionSource<Stream>();

            public Request(Stream data)
            {
                Data = data;
            }

            public Task RespondAsync(Stream data)
            {
                _tcs.TrySetResult(data);
                return Task.CompletedTask;
                    
            }

            public Stream Data { get; }
            public object Context { get; }
            public Task<Stream> Finished => _tcs.Task;
        }
            
        public Task<Stream> SendMessageAsync(Stream message)
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