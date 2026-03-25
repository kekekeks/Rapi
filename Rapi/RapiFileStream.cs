using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace Rapi
{
    public interface IRapiFileStream
    {
        Task WriteFileContents(string file, Stream data);
        Task<Stream> ReadFileContentsStream(string file);
    }
    
    public class RapiFileStream : IRapiFileStream
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private string url { get; }

        public RapiFileStream(string url)
        {
            this.url = url;
        }

        public async Task WriteFileContents(string file, Stream data)
        {
            using var streamContent = new StreamContent(data);
            var param = new Dictionary<string, string?> { {"path", file} };
            var path = QueryHelpers.AddQueryString($"{url}/filestream/write", param);
            var res = await httpClient.PostAsync(path, streamContent);
            if(!res.IsSuccessStatusCode) throw new Exception("Cannot upload file!");
        }

        public async Task<Stream> ReadFileContentsStream(string file)
        {
            var param = new Dictionary<string, string?> { {"path", file} };
            var path = QueryHelpers.AddQueryString($"{url}/filestream/read", param);
            return await httpClient.GetStreamAsync(path);
        }
    }
}