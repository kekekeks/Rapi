using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rapi.Mocks
{
    public class RapiMockConnectionFactory : IRapiConnectionFactory
    {
        private ConcurrentDictionary<string, MockRapiMachine> _machines = new ConcurrentDictionary<string, MockRapiMachine>();
        public void Register(string url, MockRapiMachine machine)
        {
            _machines[url] = machine;
        }

        public Dictionary<string, MockRapiMachine> GetMachines() => _machines.ToDictionary(x => x.Key, x => x.Value);
        
        public Task<RapiConnection> Connect(string url)
        {
            var m = _machines[url];
            return RapiConnection.Connect(m.Transport, m.FileStream);
        }
    }
}