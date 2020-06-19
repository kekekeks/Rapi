using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rapi.Mocks
{
    public class RapiSftpMock : IRapiSftpRpc
    {
        public class SftpOperation
        {
            public bool IsUpload { get; set; }
            public string From { get; set; }
            public string To { get; set; }
            internal bool AutoComplete;
            internal TaskCompletionSource<int> Tcs = new TaskCompletionSource<int>();
        }
        
        private Dictionary<string, SftpOperation> _operations = new Dictionary<string, SftpOperation>();

        Task CreateOp(string id, bool upload, string from, string to)
        {
            lock (_operations)
            {
                // TODO: compare
                if (id != null && _operations.ContainsKey(id))
                    throw new InvalidOperationException();
                var op = new SftpOperation()
                {
                    From = from,
                    To = to,
                    IsUpload = upload,
                    AutoComplete = id == null
                };
                _operations[id ?? Guid.NewGuid().ToString()] = op;
                return op.Tcs.Task;
            }
        }
        
        Task IRapiSftpRpc.Download(string @from, string to, RapiSftpCredentials credentials)
        {
            return CreateOp(null, false, from, to);
        }

        Task IRapiSftpRpc.Upload(string @from, string to, RapiSftpCredentials credentials)
        {
            return CreateOp(null, true, from, to);
        }

        Task IRapiSftpRpc.StartDownload(string id, string @from, string to, RapiSftpCredentials credentials)
        {
            CreateOp(id, false, from, to);
            return Task.CompletedTask;
        }

        Task IRapiSftpRpc.StartUpload(string id, string @from, string to, RapiSftpCredentials credentials)
        {
            CreateOp(id, true, from, to);
            return Task.CompletedTask;
        }

        async Task<RapiSftpOperationStatusDto> IRapiSftpRpc.TryGetStatus(string id)
        {
            lock (_operations)
            {
                if (!_operations.TryGetValue(id, out var op))
                    return null;

                return new RapiSftpOperationStatusDto
                {
                    IsCompleted = op.Tcs.Task.IsCompleted,
                    Exception = op.Tcs.Task.Exception?.ToString()
                };
            }
        }

        async Task IRapiSftpRpc.Complete(string id)
        {
            lock (_operations)
                _operations.Remove(id);
        }

        public List<SftpOperation> GetOperations()
        {
            lock (_operations)
                return _operations.Values.ToList();
        }

        public void Complete(string id)
        {
            lock (_operations)
                if(_operations.TryGetValue(id, out var op))
                {
                    op.Tcs.TrySetResult(0);
                    if (op.AutoComplete)
                        _operations.Remove(id);
                }
        }
        
        public void Fail(string id, Exception e)
        {
            lock (_operations)
                if(_operations.TryGetValue(id, out var op))
                {
                    op.Tcs.TrySetException(e);
                    if (op.AutoComplete)
                        _operations.Remove(id);
                }
        }
    }
}