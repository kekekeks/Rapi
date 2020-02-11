using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CoreRPC.Transport;

namespace Rapi
{
    public class CoreRPCOverRapiTransport : IClientTransport
    {
        private readonly string _url;
        private readonly Dictionary<string, string> _headers;
        private readonly IRapiWebRequestRpc _proxy;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

        public CoreRPCOverRapiTransport(IClientTransport transport, string url, Dictionary<string, string> headers)
        {
            _proxy = RapiConnection.CreateEngine().CreateProxy<IRapiWebRequestRpc>(transport,
                new ConstTargetExtractor("WebRequest"));
            
            _url = url;
            _headers = headers;
        }
        
        
        public CoreRPCOverRapiTransport(RapiConnection connection, string url, Dictionary<string, string> headers)
        {
            _proxy = connection.WebRequest;
            _url = url;
            _headers = headers;
        }
        
        
        public async Task<byte[]> SendMessageAsync(byte[] message)
        {
            var resp = await _proxy.SendWebRequest(new RapiWebRequest
            {
                Body = message,
                Method = "POST",
                Uri = _url,
                Headers = _headers,
                Timeout = (int) Math.Ceiling(Timeout.TotalSeconds)
            });
            if (resp.Code != 200)
                throw new WebException("Server returned " + resp.Code);
            return resp.Data;
        }
        
    }
}